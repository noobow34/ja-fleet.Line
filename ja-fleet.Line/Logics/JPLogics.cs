using AngleSharp.Html.Parser;
using jafleet.Line.Manager;
using System;
using System.Threading.Tasks;

namespace jafleet.Line.Logics
{
    public class JPLogics
    {
        public static async Task<(string large, string small)> GetJetPhotosFromRegistrationNumberAsync(string regstrationNumber)
        {
            string jetphotoUrl = $"https://www.jetphotos.com/showphotos.php?keywords-type=reg&keywords={regstrationNumber}&search-type=Advanced&keywords-contain=0&sort-order=2";
            string photoUrlSmall = string.Empty;
            string photoUrlLarge = string.Empty;

            try
            {
                var parser = new HtmlParser();
                var serchPage = parser.ParseDocument(await HttpClientManager.GetInstance().GetStringAsync(jetphotoUrl));
                var photoLinkTag = serchPage.GetElementsByClassName("result__photoLink");
                var photoSmallTag = serchPage.GetElementsByClassName("result__photo");
                if (photoLinkTag.Length != 0)
                {
                    if (photoSmallTag.Length != 0)
                    {
                        photoUrlSmall = "https:" + photoSmallTag[0].GetAttribute("src");
                    }
                    photoUrlLarge = photoUrlSmall.Replace("/400/","/full/");
                }
            }
            catch (Exception)
            {

            }

            return (photoUrlLarge,photoUrlSmall);
        }
    }
}
