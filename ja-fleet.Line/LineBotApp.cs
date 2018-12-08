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

        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message.Type)
            {
                case EventMessageType.Text:
                    await HandleTextAsync(ev.ReplyToken, ((TextEventMessage)ev.Message).Text, ev.Source.UserId);
                    break;
            }
        }

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
                        "JA-Fleet登録データ：JAレジで運航中のもの、2018/09以降に抹消されてもの\n" +
                        "写真のみ検索：Jetphotosサイトに登録されているもの\n" +
                        "【検索例】\n" +
                        "●JAレジ\n" +
                        "801a,JA301J（JAレジは、'JA'をつけなくてもOK）\n" +
                        "●それ以外\n" +
                        "80-1111,n501dn, A6-BLA（省略不可）\n" +
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

            Log log = new Log
            {
                LogDate = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME),
                LogType = LogType.LINE,
                LogDetail = originalReg,
                UserId = userId
            };

            var profile = await messagingClient.GetUserProfileAsync(userId);
            var httpClient = new HttpClient();
            var profileImage = await httpClient.GetByteArrayAsync(profile.PictureUrl);
            LineUser user = new LineUser
            {
                UserId = userId,
                UserName = profile.DisplayName,
                ProfileImage = profileImage,
                LastAccess = DateTime.Now.ToString(DBConstant.SQLITE_DATETIME)
            };
            using (var context = new jafleetContext())
            {
                //Log登録
                context.Log.Add(log);

                //LineUser登録または更新
                if(context.LineUser.Where(p => p.UserId == userId).Count() != 0)
                {
                    context.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
                else
                {
                    context.LineUser.Add(user);
                }

                context.SaveChanges();
            }

        }

        private string GetFileExtension(string mediaType)
        {
            switch (mediaType)
            {
                case "image/jpeg":
                    return ".jpeg";
                case "audio/x-m4a":
                    return ".m4a";
                case "video/mp4":
                    return ".mp4";
                default:
                    return "";
            }
        }
    }
}