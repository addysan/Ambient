using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bing.Speech;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace STT
{
    public class BingSttManager : ISttManager
    {

        /// <summary>
        /// Short phrase mode URL
        /// </summary>
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");

        /// <summary>
        /// The long dictation URL
        /// </summary>
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");


        private static readonly Uri ConversationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/conversation");  //"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1")

        private string SubscriptionKey
        {
            get
            {
                return "7b8324cd6ecc453da7a2a96a21b34ac1"; // ConfigurationManager.AppSettings["SubscriptionKey"];
            }
        }

        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        /// <summary>
        /// A completed task
        /// </summary>
        private static readonly Task CompletedTask = Task.FromResult(true);

        /// <summary>
        /// Cancellation token used to stop sending the audio.
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public bool Complete
        {
            get
            {
                return done;
            }
        }

        private List<string> outputStrings = new List<string>();
        public IList<string> Outputs
        {
            get
            {
                return outputStrings;
            }
        }

        bool done = false;

        public void Done()
        {
            done = true;
        }

        private SpeechClient speechClient = null;

        public void Init()
        {
         
        }

        public Action<RecognitionResult> Callback = null;
        public Action<RecognitionPartialResult> CallbackPartial = null;


        /// <summary>
        /// Invoked when the speech client receives a partial recognition hypothesis from the server.
        /// </summary>
        /// <param name="args">The partial response recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        public Task OnPartialResult(RecognitionPartialResult args)
        {
            if (CallbackPartial != null)
            {
                CallbackPartial(args);
                return CompletedTask;
            }

            //Console.WriteLine("--- Partial result received by OnPartialResult ---");

            // Print the partial response recognition hypothesis.
            Console.WriteLine(args.DisplayText);

            //Console.WriteLine();

            return CompletedTask;
        }


        public Task OnRecognitionResult(RecognitionResult args)
        {

            if (Callback != null)
            {
                Callback(args);
                return CompletedTask; 
            }

            var response = args;
            Console.WriteLine();

           // Console.WriteLine("--- Phrase result received by OnRecognitionResult ---");


            // Print the recognition status.
            //Console.WriteLine("***** Phrase Recognition Status = [{0}] ***", response.RecognitionStatus);


            if (response.Phrases != null)
            {
//                outputStrings.Add(response.Phrases.FirstOrDefault().DisplayText);
                foreach (var result in response.Phrases)
                {
                    if (!Outputs.Any())
                    {
                        var output = string.Format("[{0}:{1}]{2}", result.MediaTime, result.MediaDuration, result.DisplayText);
                        Outputs.Add(output);
                        Console.WriteLine(output);
                    }

                    // Print the recognition phrase display text.
                    //Console.WriteLine("{0} (Confidence:{1})", result.DisplayText, result.Confidence);
                }
            }
            else if (response.RecognitionStatus == RecognitionStatus.InitialSilenceTimeout || response.RecognitionStatus == RecognitionStatus.PhraseSilenceTimeout || response.RecognitionStatus == RecognitionStatus.BabbleTimeout)
            {
                Outputs.Add("...");
            }

            Console.WriteLine();
            return CompletedTask;
        }

        public void SendAudio(string wavFileName)
        {
            // create the preferences object
            var preferences = new Preferences(DefaultLocale, ConversationUrl, new CognitiveServicesAuthorizationProvider(SubscriptionKey));

            // Create a a speech client
            using (var speechClient = new SpeechClient(preferences))
            {
                speechClient.SubscribeToPartialResult(this.OnPartialResult);
                speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

                // create an audio content and pass it a stream.
                using (var audio = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
                {
                    var deviceMetadata = new DeviceMetadata(DeviceType.Near, DeviceFamily.Desktop, NetworkType.Ethernet, OsName.Windows, "1607", "Dell", "T3600");
                    var applicationMetadata = new ApplicationMetadata("SampleApp", "1.0.0");
                    var requestMetadata = new RequestMetadata(Guid.NewGuid(), deviceMetadata, applicationMetadata, "SampleAppService");

                    speechClient.RecognizeAsync(new SpeechInput(audio, requestMetadata), this.cts.Token).Wait();
                }
            }
        }
    }


    /// <summary>
    /// Cognitive Services Authorization Provider.
    /// </summary>
    public sealed class CognitiveServicesAuthorizationProvider : IAuthorizationProvider
    {
        /// <summary>
        /// The fetch token URI
        /// </summary>
        private const string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";

        /// <summary>
        /// The subscription key
        /// </summary>
        private readonly string subscriptionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CognitiveServicesAuthorizationProvider" /> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription identifier.</param>
        public CognitiveServicesAuthorizationProvider(string subscriptionKey)
        {
            /*
            if (subscriptionKey == null)
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentException(nameof(subscriptionKey));
            }*/

            this.subscriptionKey = subscriptionKey;
        }

        /// <summary>
        /// Gets the authorization token asynchronously.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the string parameter contains the next the authorization token.
        /// </returns>
        /// <remarks>
        /// This method should always return a valid authorization token at the time it is called.
        /// </remarks>
        public Task<string> GetAuthorizationTokenAsync()
        {
            return FetchToken(FetchTokenUri, this.subscriptionKey);
        }

        /// <summary>
        /// Fetches the token.
        /// </summary>
        /// <param name="fetchUri">The fetch URI.</param>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <returns>An access token.</returns>
        private static async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                var uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                using (var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false))
                {
                    return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }
    }

}
