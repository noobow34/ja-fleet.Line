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
