using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                HttpClient client = new HttpClient();
                var serchPage = parser.Parse(await client.GetStringAsync(jetphotoUrl));
                var photoLinkTag = serchPage.GetElementsByClassName("result__photoLink");
                var photoSmallTag = serchPage.GetElementsByClassName("result__photo");
                if (photoLinkTag.Length != 0)
                {
                    if (photoSmallTag.Length != 0)
                    {
                        photoUrlSmall = photoSmallTag[0].GetAttribute("src");
                    }
                    string newestPhotoLink = photoLinkTag[0].GetAttribute("href");
                    var photoPage = parser.Parse(await client.GetStringAsync("https://www.jetphotos.com" + newestPhotoLink));
                    var photoTag2 = photoPage.GetElementsByClassName("large-photo__img");
                    if (photoTag2.Length != 0)
                    {
                        photoUrlLarge = photoTag2[0].GetAttribute("srcset");
                    }
                }
            }
            catch (Exception)
            {

            }

            return (photoUrlLarge,photoUrlSmall);
        }
    }
}
