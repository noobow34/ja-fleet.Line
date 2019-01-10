using jafleet.Line.Models;
using Line.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace jafleet.Line.Manager
{
    public static class LineMessagingClientManager
    {
        private static LineMessagingClient _client;

        public static LineMessagingClient GetInstance()
        {
            if(_client == null)
            {
                var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
                string channelAccessToken = config.GetSection("LineSettings")["ChannelAccessToken"];
#if DEBUG
                _client = new LineMessagingClient(channelAccessToken,"http://localhost:8080");
#else
    _client = new LineMessagingClient(channelAccessToken);
#endif
            }
            return _client;
        }
    }
}
