using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using EnumStringValues;
using jafleet.Commons.Constants;
using jafleet.Commons.EF;
using jafleet.Line.Constants;
using jafleet.Line.Logics;
using jafleet.Line.Manager;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Noobow.Commons.Constants;
using Noobow.Commons.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace jafleet.Line
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        private readonly jafleetContext _context;
        private readonly IServiceScopeFactory _services;

        public LineBotApp(LineMessagingClient lineMessagingClient,jafleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            this.messagingClient = lineMessagingClient;
            _context = context;
            _services = serviceScopeFactory;
        }

        /// <summary>
        /// フォローイベント
        /// （使用方法を返信して、ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnFollowAsync(FollowEvent ev)
        {
            await messagingClient.ReplyMessageAsync(ev.ReplyToken, ReplayMessage.FOLLOW_MESSAGE);

            //ユーザーに返信してからログを処理
            DateTime? followDate = DateTime.Now;
            string userId = ev.Source.UserId;

            var profile = await GetUserProfileAsync(userId);
            Log log = new Log
            {
                LogDate = followDate,
                LogType = LogType.LINE_FOLLOW,
                LogDetail = profile?.DisplayName,
                UserId = userId
            };
            _context.Log.Add(log);
            //LINE_USERにユーザーを記録
            var lineuser = _context.LineUser.SingleOrDefault(p => p.UserId == userId);
            if(lineuser != null)
            {
                //すでに存在するユーザーの場合（再フォローの場合など）
                lineuser.FollowDate = followDate;
            }
            else
            {
                //新規ユーザー（普通こっち）
                LineUser user = new LineUser
                {
                    UserId = userId,
                    UserName = profile?.DisplayName,
                    FollowDate = followDate,
                    ProfileUpdateTime = followDate
                };
                _context.LineUser.Add(user);
            }
            _context.SaveChanges();

        }

        /// <summary>
        /// アンフォローイベント
        /// （ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnUnfollowAsync(UnfollowEvent ev)
        {
            await Task.Run(() =>
            {
                DateTime? unfollowDate = DateTime.Now;
                string userId = ev.Source.UserId;

                //LINE_USERにユーザーを記録
                using var serviceScope = _services.CreateScope();
                using var context = serviceScope.ServiceProvider.GetService<jafleetContext>();
                var lineuser = _context.LineUser.SingleOrDefault(p => p.UserId == userId);
                if (lineuser != null)
                {
                    //ユーザーがLINE_USERテーブルに存在する場合
                    lineuser.UnfollowDate = unfollowDate;
                }
                else
                {
                    //ユーザーがLINE_USERテーブルに存在しない場合（初期のユーザーなど）
                    var unfollowedUser = new LineUser
                    {
                        UserId = userId,
                        UnfollowDate = unfollowDate
                    };
                }

                Log log = new Log
                {
                    LogDate = unfollowDate,
                    LogType = LogType.LINE_UNFOLLOW,
                    LogDetail = (lineuser?.UserName) ?? userId,
                    UserId = userId
                };

                _context.Log.Add(log);
                _context.SaveChanges();
            });

        }

        /// <summary>
        /// メッセージ発生
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// テキストメッセージ処理
        /// </summary>
        /// <param name="replyToken"></param>
        /// <param name="userMessage"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task HandleTextAsync(string replyToken, string userMessage, string userId)
        {
            string upperedReg = userMessage.Split("\n")?[0].ToUpper();
            string jaAddUpperedReg = upperedReg;
            string firstLine = userMessage.Split("\n")?[0];
            var replay = new List<ISendMessage>();

            var compareTarget = DateTime.Now;

            if (userMessage.Contains(CommandConstant.MESSAGE))
            {
                await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage>() { ReplayMessage.SEND_MESSAGE });
                string messageBody = userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty);
                var m = new Message
                {
                    Sender = userId,
                    MessageDetail = messageBody,
                    MessageType = Commons.Constants.MessageType.LINE,
                    RecieveDate = DateTime.Now
                };
                _context.Messages.Add(m);
                _context.SaveChanges();
                await SlackUtil.PostAsync(SlackChannelEnum.jafleet.GetStringValue(), "【JA-Fleet from LINE】\n" +
                                    "ユーザー：" + (_context.LineUser.Find(userId)?.UserName ?? userId) + "\n" +
                                    userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty));
            }
            else if (userMessage.Contains(CommandConstant.HOWTOSEARCH))
            {
                await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage>() { ReplayMessage.HOWTO_SEARCH });
            }
            else
            {
                //通常の利用（レジ）
                if (!upperedReg.StartsWith("JA"))
                {
                    jaAddUpperedReg = "JA" + upperedReg;
                }

                AircraftView av = null;

                av = _context.AircraftView.Where(p => p.RegistrationNumber == jaAddUpperedReg).FirstOrDefault();

                if (av != null)
                {
                    //JA-Fleetのデータベースで見つかった場合
                    string aircraftInfo = $"{av.RegistrationNumber} \n " +
                        $" 航空会社:{av.AirlineNameJpShort} \n " +
                        $" 型式:{av.TypeDetailName ?? av.TypeName} \n " +
                        $" 製造番号:{av.SerialNumber} \n " +
                        $" 登録年月日:{av.RegisterDate} \n " +
                        $" 運用状況:{av.Operation} \n " +
                        $" コンフィグ:{av.SeatConfig}\n " +
                        $" WiFi:{av.Wifi} \n " +
                        $" 特別塗装:{av.SpecialLivery} \n " + 
                        $" 備考:{av.Remarks}";

                    replay.Add(new TextMessage(aircraftInfo));

                    //自分がアクセスした場合は必ずキャッシュを更新する
                    if(userId == LineUserIdConstant.NOOBWO)
                    {
                        try
                        {
                            _ = await HttpClientManager.GetInstance().GetAsync($"http://localhost:5000/Aircraft/Photo/{jaAddUpperedReg}?force=true");
                        }
                        catch
                        {

                        }
                    }

                    string photolarge = null;
                    string photosmall = null;
                    if (!string.IsNullOrEmpty(av.LinkUrl))
                    {
                        if (av.LinkUrl.Contains("airliners"))
                        {
                            //LinkUrlがJetphotosなら写真を取得
                            (photolarge, photosmall) = await JPLogics.GetJetPhotosFromJetphotosUrl(av.LinkUrl);
                        }
                    }
                    else
                    {
                        var photo = _context.AircraftPhoto.Where(p => p.RegistrationNumber == jaAddUpperedReg).SingleOrDefault();
                        if (photo != null && DateTime.Now.Date == photo.LastAccess.Date)
                        {
                            //既存のURLから取得
                            if (!string.IsNullOrEmpty(photo.PhotoDirectUrl))
                            {
                                photolarge = photo.PhotoDirectUrl;
                                photosmall = photo.PhotoDirectUrl;
                            }
                            else
                            {
                                (photolarge, photosmall) = await JPLogics.GetJetPhotosFromJetphotosUrl(photo.PhotoUrl);
                            }
                        }
                        else
                        {
                            string photoUrl = $"https://www.airliners.net/search?registrationActual={jaAddUpperedReg}&sortBy=datePhotographedYear&sortOrder=desc&perPage=1";
                            IBrowsingContext bContext = BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithXPath());
                            var htmlDocument = await bContext.OpenAsync(photoUrl);
                            var photos = htmlDocument.Body.SelectNodes(@"//*[@id='layout-page']/div[2]/section/section/section/div/section[2]/div/div[1]/div/div[1]/div[1]/div[1]/a");
                            if (photos.Count != 0)
                            {
                                //写真があった場合
                                string photoNumber = photos[0].TextContent.Replace("\n", string.Empty).Replace(" ", string.Empty).Replace("#", string.Empty);
                                string newestPhotoLink = $"https://www.airliners.net/photo/{photoNumber}";
                                (photolarge, photosmall) = await JPLogics.GetJetPhotosFromJetphotosUrl(newestPhotoLink);
                                _ = Task.Run(() =>
                                {
                                    //写真がないという情報を登録する
                                    using var serviceScope = _services.CreateScope();
                                    using var context = serviceScope.ServiceProvider.GetService<jafleetContext>();
                                    if (photo != null)
                                    {
                                        photo.PhotoUrl = newestPhotoLink;
                                        photo.LastAccess = DateTime.Now;
                                        photo.PhotoDirectUrl = photolarge;
                                        _context.AircraftPhoto.Update(photo);
                                    }
                                    else
                                    {
                                        context.AircraftPhoto.Add(new AircraftPhoto { RegistrationNumber = jaAddUpperedReg, PhotoUrl = newestPhotoLink, LastAccess = DateTime.Now, PhotoDirectUrl = photolarge });
                                    }
                                    context.SaveChanges();
                                });
                            }
                            else
                            {
                                _ = Task.Run(() =>
                                {
                                    //写真がないという情報を登録する
                                    using var serviceScope = _services.CreateScope();
                                    using var context = serviceScope.ServiceProvider.GetService<jafleetContext>();
                                    if (photo != null)
                                    {
                                        photo.PhotoUrl = null;
                                        photo.LastAccess = DateTime.Now;
                                        context.AircraftPhoto.Update(photo);
                                    }
                                    else
                                    {
                                        context.AircraftPhoto.Add(new AircraftPhoto { RegistrationNumber = jaAddUpperedReg, PhotoUrl = null, LastAccess = DateTime.Now });
                                    }
                                    context.SaveChanges();
                                });
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(photosmall))
                    {
                        replay.Add(new ImageMessage(photolarge, photosmall));
                    }
                }
                else
                {
                    //JA-Fleetのデータベースで見つからなかった場合、写真のみ検索

                    //まずはそのまま検索
                    string photolarge;
                    string photosmall;
                    bool found = false;
                    (photolarge, photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(upperedReg);
                    if (!string.IsNullOrEmpty(photosmall))
                    {
                        //そのまま検索で見つかった
                        found = true;
                    }
                    else
                    {
                        //見つからなければJA付きで検索
                        (photolarge, photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(jaAddUpperedReg);
                        if (!string.IsNullOrEmpty(photosmall))
                        {
                            //JA付きの検索で見つかった
                            found = true;
                        }
                    }

                    if (found)
                    {
                        replay.Add(new ImageMessage(photolarge, photosmall));
                    }
                    else
                    {
                        replay.Add(ReplayMessage.NOT_FOUND);
                    }
                }

                await messagingClient.ReplyMessageAsync(replyToken, replay);

                var processDate = DateTime.Now;
                //ユーザーに返信してからログを処理
                Log log = new Log
                {
                    LogDate = processDate,
                    LogType = LogType.LINE,
                    LogDetail = firstLine,
                    UserId = userId
                };

                //Log登録
                _context.Log.Add(log);

                //LineUser登録または更新
                var lineuser = _context.LineUser.SingleOrDefault(p => p.UserId == userId);
                if (lineuser != null)
                {
                    //ユーザーのレコードがある
                    lineuser.LastAccess = processDate;
                    if(lineuser.ProfileUpdateTime == null ||(DateTime.Now - lineuser.ProfileUpdateTime) > new TimeSpan(7, 0, 0, 0))
                    {
                        //前回アクセスから1週間以上
                        var profile = await GetUserProfileAsync(userId);
                        lineuser.UserName = profile.DisplayName;
                        lineuser.ProfileUpdateTime = processDate;
                    }
                }
                else
                {
                    //ユーザーのレコードがない
                    //ユーザー情報を取得
                    var profile = await GetUserProfileAsync(userId);
                    LineUser user = new LineUser
                    {
                        UserId = userId,
                        UserName = profile.DisplayName,
                        LastAccess = processDate,
                        ProfileUpdateTime = processDate
                    };
                    _context.LineUser.Add(user);
                }
                _context.SaveChanges();
            }

        }

        /// <summary>
        /// プロフィールとプロフィール画像を取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns></returns>
        private async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            UserProfile retprofile;

            retprofile = await messagingClient.GetUserProfileAsync(userId);

            return retprofile;
        }

    }
}