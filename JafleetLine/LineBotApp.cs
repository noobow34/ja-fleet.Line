using jafleetline.EF;
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
            string reg = userMessage.ToUpper();
            infologger.Info("LINEF" + reg);
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
                    $" q‹ó‰ïĞ:{av.AirlineNameJpShort} \n " +
                    $" Œ^®:{av.TypeDetailName ?? av.TypeName} \n " +
                    $" »‘¢”Ô†:{av.SerialNumber} \n " +
                    $" “o˜^”NŒ“ú:{av.RegisterDate} \n " +
                    $" ‰^—pó‹µ:{av.Operation} \n " +
                    $" WiFi:{av.Wifi} \n " +
                    $" ”õl:{av.Remarks}";

                replyMessage1 = new TextMessage(aircraftInfo);
                (string photolarge, string photosmall) = await JPLogics.GetJetPhotosFromRegistrationNumberAsync(reg);
                if (!string.IsNullOrEmpty(photosmall))
                {
                    replyMessage2 = new ImageMessage(photolarge, "https:" + photosmall);
                }

            }
            else
            {
                replyMessage1 = new TextMessage("Œ©‚Â‚©‚è‚Ü‚¹‚ñ‚Å‚µ‚½B");
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