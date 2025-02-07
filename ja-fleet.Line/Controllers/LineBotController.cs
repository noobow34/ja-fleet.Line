using Line.Messaging.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using jafleet.Line.Manager;
using jafleet.Commons.EF;

namespace jafleet.Line.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LineBotController : Controller
    {
        private readonly JafleetContext _context;
        private readonly IServiceScopeFactory _services;
        public LineBotController(JafleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _services = serviceScopeFactory;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]JToken req)
        { 
            var events = WebhookEventParser.Parse(req.ToString());

            var app = new LineBotApp(LineMessagingClientManager.GetInstance(),_context, _services);
            await app.RunAsync(events);
            return new OkResult();
        }
    }
}
