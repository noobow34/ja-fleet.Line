using jafleet.Commons.Constants;
using jafleet.Commons.EF;
using jafleet.Line.Constants;
using jafleet.Line.Logics;
using jafleet.Line.Manager;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Noobow.Commons.Utils;
using System;
using System.Collections.Generic;
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

            (var profile, var profileImage) = await GetUserProfileAsync(userId);
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
                if (profileImage != null)
                {
                    var lpi = new LineUserProfileImage
                    {
                        UserId = userId,
                        ProfileImage = profileImage,
                        UpdateTIme = followDate
                    };
                    _context.LineUserProfileImage.Add(lpi);
                }
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
                using (var serviceScope = _services.CreateScope())
                {
                    using (var context = serviceScope.ServiceProvider.GetService<jafleetContext>())
                    {
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
                    }
                }
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
                LineUtil.PushMe("【JA-Fleet from LINE】\n" +
                                    "ユーザー：" + (_context.LineUser.Find(userId)?.UserName ?? userId) + "\n" +
                                    userMessage.Replace(CommandConstant.MESSAGE + "\n", string.Empty), HttpClientManager.GetInstance());
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
                        $" WiFi:{av.Wifi} \n " +
                        $" 備考:{av.Remarks}";

                    replay.Add(new TextMessage(aircraftInfo));
                    (string photolarge, string photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(jaAddUpperedReg);
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
                        replay.Add(new ImageMessage(photolarge, "https:" + photosmall));
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
                        (var profile, var profileImage) = await GetUserProfileAsync(userId);
                        lineuser.UserName = profile.DisplayName;
                        lineuser.ProfileUpdateTime = processDate;
                        if(profileImage != null)
                        {
                            var lpi = _context.LineUserProfileImage.Single(pi => pi.UserId == userId);
                            if (lpi != null)
                            {
                                lpi.ProfileImage = profileImage;
                                lpi.UpdateTIme = processDate;
                            }
                            else
                            {
                                lpi = new LineUserProfileImage
                                {
                                    UserId = userId,
                                    ProfileImage = profileImage,
                                    UpdateTIme = processDate
                                };
                                _context.LineUserProfileImage.Add(lpi);
                            }
                        }
                    }
                }
                else
                {
                    //ユーザーのレコードがない
                    //ユーザー情報を取得
                    (var profile, var profileImage) = await GetUserProfileAsync(userId);
                    LineUser user = new LineUser
                    {
                        UserId = userId,
                        UserName = profile.DisplayName,
                        LastAccess = processDate,
                        ProfileUpdateTime = processDate
                    };
                    _context.LineUser.Add(user);
                    if(profileImage != null)
                    {
                        LineUserProfileImage lpi = new LineUserProfileImage
                        {
                            UserId = userId,
                            ProfileImage = profileImage,
                            UpdateTIme = processDate
                        };
                        _context.LineUserProfileImage.Add(lpi);
                    }
                }
                _context.SaveChanges();
            }

        }

        /// <summary>
        /// プロフィールとプロフィール画像を取得
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <returns></returns>
        private async Task<(UserProfile, byte[])> GetUserProfileAsync(string userId)
        {
            UserProfile retprofile;
            byte[] retprofileimage = null;

            retprofile = await messagingClient.GetUserProfileAsync(userId);
            if(retprofile?.PictureUrl != null)
            {
                retprofileimage = await HttpClientManager.GetInstance().GetByteArrayAsync(retprofile.PictureUrl);
            }

            return (retprofile, retprofileimage);
        }

    }
}