using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SpeechToTextWPFSample
{

    public static class extensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }

    public class WolframAlphaClient
    {

        private string appId { get; set; }

        public WolframAlphaClient(string appId)
        {
            this.appId = appId;
        }

        public async Task<String> Query(string question)
        {

            HttpClient client;
            HttpClientHandler handler;

            var cookieContainer = new CookieContainer();
            handler = new HttpClientHandler() { CookieContainer = new CookieContainer(), UseProxy = false };
            client = new HttpClient(handler);


            var uri = new Uri(@"http://api.wolframalpha.com/v1/result?appid=" + appId + "&i=" + HttpUtility.UrlEncode(question));

            var request = new HttpRequestMessage(HttpMethod.Post, uri);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            Console.WriteLine("Response status code: [{0}]", response.StatusCode);

            try
            {
                if (response != null && response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    // error!
                    //this.Error(new GenericEventArgs<Exception>(new Exception(String.Format("Service returned {0}", responseMessage.Result.StatusCode))));
                }
            }
            catch (Exception e)
            {
                //this.Error(new GenericEventArgs<Exception>(e.GetBaseException()));
            }
            finally
            {
                response.Dispose();
                request.Dispose();
            }

            return null;

        }
    
    }
}
