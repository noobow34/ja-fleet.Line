using jafleet.Commons.EF;
using jafleet.Line.Manager;
using Line.Messaging.Webhooks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace jafleet.Line.Controllers
{
    public class CheckController : Controller
    {
        private readonly JafleetContext _context;
        private readonly IServiceScopeFactory _services;

        public CheckController(JafleetContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _services = serviceScopeFactory;
        }

        [HttpPost]
        public async Task<IActionResult> IndexAsync()
        {
            int count = _context.Aircrafts.Count();
            int randomIndex = new Random().Next(count);

            Aircraft a = _context.Aircrafts
                .Skip(randomIndex)
                .Take(1)
                .First();

            string body = $@"
{{
  ""events"": [
    {{
      ""replyToken"": ""dummyToken"",
      ""type"": ""message"",
      ""timestamp"": 1462629479859,
      ""isCheck"": ""true"",
      ""source"": {{
        ""type"": ""user"",
        ""userId"": """"
      }},
      ""message"": {{
        ""id"": ""325708"",
        ""type"": ""text"",
        ""text"": ""{a.RegistrationNumber}""
      }}
    }}
  ]
}}";

            var events = WebhookEventParser.Parse(body);

            var app = new LineBotApp(LineMessagingClientManager.GetInstance(), _context, _services);
            await app.RunAsync(events);

            return new OkResult();
        }
    }
}