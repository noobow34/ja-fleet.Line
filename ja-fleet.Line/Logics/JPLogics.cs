using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using jafleet.Line.Manager;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace jafleet.Line.Logics
{
    public class JPLogics
    {
        public static async Task<(string large, string small)> GetJetPhotosFromRegistrationNumberAsync(string regstrationNumber)
        {
            string photoUrl = $"https://www.airliners.net/search?keywords={regstrationNumber}&sortBy=datePhotographedYear&sortOrder=desc&perPage=1";
            string photoUrlSmall = string.Empty;
            string photoUrlLarge = string.Empty;

            try
            {
                IBrowsingContext bContext = BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithXPath());
                var htmlDocument = await bContext.OpenAsync(photoUrl);
                var photos = htmlDocument.Body.SelectNodes(@"//*[@id='layout-page']/div[2]/section/section/section/div/section[2]/div/div[1]/div/div[1]/div[2]/div/a/img");
                if (photos.Count != 0)
                {
                    string photoNumber = photos[0].TextContent.Replace("\n", string.Empty).Replace(" ", string.Empty).Replace("#", string.Empty);
                    string newestPhotoLink = $"https://www.airliners.net/photo/{photoNumber}";
                    var htmlDocument2 = await bContext.OpenAsync(newestPhotoLink);
                    var photos2 = htmlDocument2.Body.SelectNodes(@"//*[@id='layout-page']/div[5]/section/section/section/div/div/div[1]/div/a[1]/img");
                    if (photos2.Count != 0)
                    {
                        Uri photoUri = new(((IHtmlImageElement)photos2[0]).Source);
                        photoUrlLarge = photoUri.OriginalString.Replace(photoUri.Query, string.Empty);
                        photoUrlSmall = photoUrlLarge;
                    }
                }
            }
            catch (Exception)
            {

            }

            return (photoUrlLarge,photoUrlSmall);
        }

        public static async Task<(string large, string small)> GetJetPhotosFromJetphotosUrl(string url)
        {
            string photoUrlSmall = string.Empty;
            string photoUrlLarge = string.Empty;
            var parser = new HtmlParser();

            try
            {
                IBrowsingContext bContext2 = BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithXPath());
                var htmlDocument2 = await bContext2.OpenAsync(url);
                var photos2 = htmlDocument2.Body.SelectNodes(@"//*[@id='layout-page']/div[5]/section/section/section/div/div/div[1]/div/a[1]/img");
                if (photos2.Count != 0)
                {
                    Uri photoUri = new(((IHtmlImageElement)photos2[0]).Source);
                    photoUrlLarge = photoUri.OriginalString.Replace(photoUri.Query, string.Empty);
                    photoUrlSmall = photoUrlLarge;
                }
            }
            catch (Exception)
            {

            }

            return (photoUrlLarge, photoUrlSmall);
        }
    }
}
