using Microsoft.CognitiveServices.SpeechRecognition;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STT
{
    public class OxfordSttManager : ISttManager
    {

        private DataRecognitionClient dataClient = null;

        public RecognitionStatus Status = RecognitionStatus.None;

        public bool Complete
        {
            get
            {
                return Status == RecognitionStatus.EndOfDictation;
            }
        }


        public List<string> outputList = new List<string>();

        public IList<string> Outputs
        {
            get
            {
                return outputList;
            }
        }

        private string AuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["AuthenticationUri"];
            }
        }

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

        public void Init()
        {
            this.dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                          SpeechRecognitionMode.LongDictation,
                          this.DefaultLocale,
                          this.SubscriptionKey);

            this.dataClient.AuthenticationUri = this.AuthenticationUri;

            this.dataClient.OnResponseReceived += this.OnDataDictationResponse;
            this.dataClient.OnPartialResponseReceived += this.OnPartialResponse;
            this.dataClient.OnConversationError += this.OnConversationError;

            Outputs.Clear();
            Status = RecognitionStatus.None;
        }

        public void Done()
        {
            this.dataClient.EndAudio();
        }

        /// <summary>
        /// Sends the audio helper.
        /// </summary>
        /// <param name="wavFileName">Name of the wav file.</param>
        public void SendAudio(string wavFileName)
        {
            using (FileStream fileStream = new FileStream(wavFileName, FileMode.Open, FileAccess.Read))
            {
                // Note for wave files, we can just send data from the file right to the server.
                // In the case you are not an audio file in wave format, and instead you have just
                // raw data (for example audio coming over bluetooth), then before sending up any 
                // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
                // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
                int bytesRead = 0;
                byte[] buffer = new byte[1024];

                do
                {
                    // Get more Audio data to send into byte buffer.
                    bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                    // Send of audio data to service. 
                    this.dataClient.SendAudio(buffer, bytesRead);
                }
                while (bytesRead > 0);
            }
        }



        private void OnConversationError(object sender, SpeechErrorEventArgs e)
        {
            var result = e;
        }

        private void OnPartialResponse(object sender, PartialSpeechResponseEventArgs e)
        {
            var result = e;
            Console.WriteLine(e.PartialResult);
        }

        private void OnDataDictationResponse(object sender, SpeechResponseEventArgs e)
        {
            Status = e.PhraseResponse.RecognitionStatus;
            Console.WriteLine("[{0}]", Status);

            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.RecognitionSuccess)
            {
                //Console.WriteLine("-->" + e.PhraseResponse.Results.First().DisplayText);

                Outputs.Add(e.PhraseResponse.Results.First().DisplayText);

                if (e.PhraseResponse.Results.Skip(1).Any())
                {
                    Console.WriteLine("---->" + e.PhraseResponse.Results.Skip(1).First().DisplayText);
                }
            }
            else
            {
                //Console.WriteLine("[{0}]", e.PhraseResponse.RecognitionStatus);
            }
        }
    }


}
