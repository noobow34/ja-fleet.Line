using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace jafleet.Line.Manager
{
    public class HttpClientManager
    {
        private static HttpClient _client = new HttpClient();

        public static HttpClient GetInstance()
        {
            return _client;
        }
    }
}
