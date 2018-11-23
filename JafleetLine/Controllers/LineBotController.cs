using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using NoobowNotifier.Models;

namespace NoobowNotifier.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private static LineMessagingClient lineMessagingClient;
        AppSettings appsettings;
        public LineBotController(IOptions<AppSettings> options)
        {
            appsettings = options.Value;
#if DEBUG
            lineMessagingClient = new LineMessagingClient(appsettings.LineSettings.ChannelAccessToken,
                "http://localhost:8080");
#else
    lineMessagingClient = new LineMessagingClient(appsettings.LineSettings.ChannelAccessToken);
#endif
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JToken req)
        { 
            var events = WebhookEventParser.Parse(req.ToString());

            var app = new LineBotApp(lineMessagingClient);
            await app.RunAsync(events);
            return new OkResult();
        }
    }
}
