using jafleet.Constants;
using jafleet.EF;
using jafleet.Line.Logics;
using Line.Messaging;
using Line.Messaging.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace jafleet.Line
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }

        public LineBotApp(LineMessagingClient lineMessagingClient)
        {
            this.messagingClient = lineMessagingClient;
        }

        /// <summary>
        /// フォローイベント
        /// （使用方法を返信して、ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnFollowAsync(FollowEvent ev)
        {
            var replyMessage1 = new TextMessage("フォローありがとうございます。\n" +
                                                "このアカウントのでは使用方法は以下の画像および説明をご確認ください。\n" +
                                                "・JA-Fleetサイトに登録されている飛行機は詳細情報と写真を検索できます\n" +
                                                "・JA-Fleetサイトに登録されていない場合は写真のみ検索できます\n" +
                                                "【検索例】\n" +
                                                "●JAレジ\n" +
                                                "801a,JA301J\n" +
                                                "（JAレジは、'JA'をつけなくてもOK）\n" +
                                                "●それ以外\n" +
                                                "80-1111,n501dn, A6-BLA\n" +
                                                "（省略不可）\n" +
                                                "※すべて大文字小文字区別せず");
            var replyMessage2 = new ImageMessage("https://line.ja-fleet.noobow.me/howtouse.jpg", "https://line.ja-fleet.noobow.me/howtouse.jpg");

            await messagingClient.ReplyMessageAsync(ev.ReplyToken, new List<ISendMessage> { replyMessage1,replyMessage2 });

            //ユーザーに返信してからログを処理
            string followDate = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME);
            string userId = ev.Source.UserId;

            (var profile, var profileImage) = await GetUserProfileAsync(userId);
            Log log = new Log
            {
                LogDate = followDate,
                LogType = LogType.LINE_FOLLOW,
                LogDetail = profile?.DisplayName,
                UserId = userId
            };
            //LINE_USERにユーザーを記録
            using (var context = new jafleetContext())
            {
                var lineuser = context.LineUser.SingleOrDefault(p => p.UserId == userId);
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
                        FollowDate = followDate
                    };
                    if(profileImage != null)
                    {
                        user.ProfileImage = profileImage;
                    }
                    context.LineUser.Add(user);
                }
                context.Log.Add(log);
                context.SaveChanges();
            }

        }

        /// <summary>
        /// アンフォローイベント
        /// （ユーザー情報とイベントログを記録）
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        protected override async Task OnUnfollowAsync(UnfollowEvent ev)
        {
            string unfollowDate = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME);
            string userId = ev.Source.UserId;

            Log log = new Log
            {
                LogDate = unfollowDate,
                LogType = LogType.LINE_UNFOLLOW,
                LogDetail = userId,
                UserId = userId
            };
            //LINE_USERにユーザーを記録
            using (var context = new jafleetContext())
            {
                var lineuser = context.LineUser.SingleOrDefault(p => p.UserId == userId);
                if (lineuser != null)
                {
                    //ユーザーがLINE_USERテーブルに存在する場合のみ処理
                    lineuser.UnfollowDate = unfollowDate;
                }
                context.Log.Add(log);
                context.SaveChanges();
            }
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
            string originalReg = userMessage.Split("\n")?[0];

            if (!upperedReg.StartsWith("JA"))
            {
                jaAddUpperedReg = "JA" + upperedReg;
            }

            ISendMessage replyMessage1 = null;
            ISendMessage replyMessage2 = null;

            AircraftView av = null;
            using (var context = new jafleetContext())
            {
                av = context.AircraftView.Where(p => p.RegistrationNumber == jaAddUpperedReg).FirstOrDefault();
            }

            if(av != null)
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

                replyMessage1 = new TextMessage(aircraftInfo);
                (string photolarge, string photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(jaAddUpperedReg);
                if (!string.IsNullOrEmpty(photosmall))
                {
                    replyMessage2 = new ImageMessage(photolarge, "https:" + photosmall);
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
                    replyMessage1 = new TextMessage("JA-Fleetにデータが登録されていないため、写真のみ検索しました。");
                    replyMessage2 = new ImageMessage(photolarge, "https:" + photosmall);
                }
                else
                {
                    replyMessage1 = new TextMessage("JA-Fleetにデータが登録されておらず、写真のみの検索でも見つかりませんでした。\n" +
                        "------------\n" +
                        "JA-Fleet登録データ：JAレジで運航中のもの、2018/09以降に抹消されてもの\n" +
                        "写真のみ検索：Jetphotosサイトに登録されているもの\n" +
                        "【検索例】\n" +
                        "●JAレジ\n" +
                        "801a,JA301J\n" +
                        "（JAレジは、'JA'をつけなくてもOK）\n" +
                        "●それ以外\n" +
                        "80-1111,n501dn, A6-BLA\n" +
                        "（省略不可）\n" +
                        "※すべて大文字小文字区別せず");
                }
            }


            if (replyMessage2 != null)
            {
                await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage1, replyMessage2 });
            }
            else
            {
                await messagingClient.ReplyMessageAsync(replyToken, new List<ISendMessage> { replyMessage1});
            }

            //ユーザーに返信してからログを処理
            Log log = new Log
            {
                LogDate = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME),
                LogType = LogType.LINE,
                LogDetail = originalReg,
                UserId = userId
            };

            //ユーザー情報を取得
            (var profile, var profileImage) = await GetUserProfileAsync(userId);

            using (var context = new jafleetContext())
            {
                //Log登録
                context.Log.Add(log);

                //LineUser登録または更新
                var lineuser = context.LineUser.SingleOrDefault(p => p.UserId == userId);
                if(lineuser != null)
                {
                    lineuser.UserName = profile?.DisplayName;
                    lineuser.ProfileImage = profileImage;
                    lineuser.LastAccess = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME);
                }
                else
                {
                    LineUser user = new LineUser
                    {
                        UserId = userId,
                        UserName = profile.DisplayName,
                        ProfileImage = profileImage,
                        LastAccess = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME)
                    };
                    context.LineUser.Add(user);
                }

                context.SaveChanges();
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
                var httpClient = new HttpClient();
                retprofileimage = await httpClient.GetByteArrayAsync(retprofile.PictureUrl);
            }

            return (retprofile, retprofileimage);
        }

    }
}