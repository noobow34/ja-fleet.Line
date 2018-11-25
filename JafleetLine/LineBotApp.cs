using jafleet.EF;
using jafleetline.Logics;
using Line.Messaging;
using Line.Messaging.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jafleetline
{
    internal class LineBotApp : WebhookApplication
    {
        private LineMessagingClient messagingClient { get; }
        private static NLog.Logger infologger = NLog.LogManager.GetLogger("infologger");

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
            if(userId != "U68e05e69b6acbaaf565bc616fdef695d")
            {
                infologger.Info($"LINE：{userMessage},{userId}");
            }
            Log log = new Log
            {
                LogDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ,LogType = "3"
                ,LogDetail = reg
                ,UserId = userId
            };

            if (!reg.StartsWith("JA"))
            {
                reg = "JA" + reg;
            }

            ISendMessage replyMessage1 = null;
            ISendMessage replyMessage2 = null;

            AircraftView av = null;
            using (var context = new jafleetContext())
            {
                context.Log.Add(log);
                context.SaveChanges();
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