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
            string reg = userMessage.Split("\n")?[0].ToUpper();
            string originalReg = userMessage.Split("\n")?[0];

            if (!reg.StartsWith("JA"))
            {
                reg = "JA" + reg;
            }

            ISendMessage replyMessage1 = null;
            ISendMessage replyMessage2 = null;

            AircraftView av = null;
            using (var context = new jafleetContext())
            {
                av = context.AircraftView.Where(p => p.RegistrationNumber == reg).FirstOrDefault();
            }

            if(av != null)
            {
                string aircraftInfo = $"{av.RegistrationNumber} \n " +
                    $" 航空会社:{av.AirlineNameJpShort} \n " +
                    $" 型式:{av.TypeDetailName ?? av.TypeName} \n " +
                    $" 製造番号:{av.SerialNumber} \n " +
                    $" 登録年月日:{av.RegisterDate} \n " +
                    $" 運用状況:{av.Operation} \n " +
                    $" WiFi:{av.Wifi} \n " +
                    $" 備考:{av.Remarks}";

                replyMessage1 = new TextMessage(aircraftInfo);
                (string photolarge, string photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(reg);
                if (!string.IsNullOrEmpty(photosmall))
                {
                    replyMessage2 = new ImageMessage(photolarge, "https:" + photosmall);
                }

            }
            else
            {
                replyMessage1 = new TextMessage("見つかりませんでした。\n" +
                    "----\n" +
                    "検索できるのはJA-Fleetサイトと同じ範囲であり、2018/09以前に抹消された機体や海外の機体は検索できませんm(_ _)m\n" +
                    "----\n" +
                    "【検索方法】\n" +
                    "メッセージの1行目のみ検索対象になります。" +
                    "OK→JA801A・ja801a・801a・801A（JAはなくてもOK、大文字小文字区別せず）\n" +
                    "NG→B787・N115AN（型式名や海外の機体は検索できない）");
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
                LogDetail = reg,
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