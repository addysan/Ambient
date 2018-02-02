using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ProjectOxford.SpeakerRecognition;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract;
using Microsoft.ProjectOxford.SpeakerRecognition.Contract.Identification;

namespace SpeakerRecognition
{
    class Program
    {
        static void Main(string[] args)
        {

            var s1 = new Speaker(6, 7, 8, 14, 15, 34, 48, 49) { Label = "boss", Id = new Guid("dca43840-a978-4779-85f7-eee25b492366") };

            var s2 = new Speaker(17, 18, 22,23,24, 26, 31, 32) { Label = "analyst", Id = new Guid("d3668afa-1dab-4db5-b4a5-afdd78a0371e") };

            var s3 = new Speaker(2) { Label = "Itai", Id = new Guid("05af8df5-9977-465b-940c-a36f3fd5a46e") };

            foreach (var speaker in new [] { s1, s2, s3})
            {
                if (!speaker.IsEnrolled)
                {
                    Enroll(speaker, speaker == s3 ? @"D:\dev\ambient\itai{0}.wav" : @"D:\dev\raspberrypi\node_streaming_server\segments\output{0}.wav");
                }
            }


            var allFileNumbers = Enumerable.Range(3, 50-3);

            var recog = new SpeakerRecognition();
            foreach (var num in allFileNumbers)
            {
                var filename = string.Format(@"D:\dev\raspberrypi\node_streaming_server\segments\output{0}.wav", num);
                var result = recog.Identify(filename, new[] { s1.Id, s2.Id, s3.Id }).Result;

                var name = s1.Id == result.Item1
                                    ? s1.Label 
                                    : s2.Id == result.Item1
                                               ? s2.Label
                                               : "unknown";


                Log("{0} is spoken by {1} with confidence of {2}", num, name, result.Item2);

                Thread.Sleep(10000); // avoid throttling
            }


            Console.ReadLine();

        }

        public static void IdentifyUsingTranscript(string pathToTranscript, string pathToWav, Guid[] guids)
        {
            var lines = File.ReadAllLines(pathToTranscript);

            long start = 0;
            long position = 0;
            long prevPosition = 0;

            foreach (var line in lines)
            {
                var parts = line.Split(',');

                start = long.Parse(parts[1]);
                position = long.Parse(parts[2]);

            }



        }

        public static void Enroll(Speaker s, string segmentToPath, bool isShort = true)
        {
            var recog = new SpeakerRecognition();
            s.Id = recog.CreateProfile().Result;

            foreach (var segment in s.Segments)
            {
                var remain = recog.Enroll(s.Id, string.Format(segmentToPath, segment)).Result;
                //if (remain == 0)
                //{
                //    break;
                //}
            }
        }

        public static void Log(string line)
        {
            Console.WriteLine(line);
            File.AppendAllText(System.Diagnostics.Process.GetCurrentProcess().ProcessName + "_output.txt", line + Environment.NewLine);
        }

        public static void Log(string line, params object[] args)
        {
            Log(string.Format(line, args));
        }

    }

    public class Speaker
    {
        public Guid Id = Guid.Empty;

        public string Label;

        public List<int> Segments = new List<int>();

        public bool IsEnrolled
        {
            get { return Id != Guid.Empty; }
        }

        public Speaker(params int[] segments)
        {
            Segments.AddRange(segments);
        }
    }

    public class SpeakerRecognition
    {
        public string name = "speakerrecognition";
        public string key = "89c55b1c8eed451383f7a40757fafa2a";

        private SpeakerIdentificationServiceClient serviceClient;

        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        public SpeakerRecognition()
        {
            serviceClient = new SpeakerIdentificationServiceClient(this.key);

        }

        public async Task<Tuple<Guid, int>> Identify(string filename, Guid[] ids)
        {
            var audioStream = File.OpenRead(filename);

            var location = await serviceClient.IdentifyAsync(audioStream, ids, true);

            IdentificationOperation identifyResult = null;
            int numOfRetries = 10;
            TimeSpan timeBetweenRetries = TimeSpan.FromSeconds(5.0);
            while (numOfRetries > 0)
            {
                await Task.Delay(timeBetweenRetries);
                identifyResult = await serviceClient.CheckIdentificationStatusAsync(location);

                if (identifyResult.Status == Status.Succeeded)
                {
                    break;
                }
                else if (identifyResult.Status == Status.Failed)
                {
                    Console.WriteLine(identifyResult.Message);
                }
                numOfRetries--;
            }
            if (numOfRetries <= 0)
            {
                throw new EnrollmentException("Enrollment operation timeout.");
            }

            return new Tuple<Guid, int>(identifyResult.ProcessingResult.IdentifiedProfileId, (int)identifyResult.ProcessingResult.Confidence);
        }



        public async Task<Guid> CreateProfile()
        {
            CreateProfileResponse creationResponse = await serviceClient.CreateProfileAsync(DefaultLocale);
            Profile profile = await serviceClient.GetProfileAsync(creationResponse.ProfileId);
            return profile.ProfileId;
        }

        public async Task<double> Enroll(Guid ProfileId, string filename, bool isShort = true)
        {
            var audioStream = File.OpenRead(filename);

            var location = await serviceClient.EnrollAsync(audioStream, ProfileId, isShort);

            EnrollmentOperation enrollmentResult = null;
            int numOfRetries = 10;
            TimeSpan timeBetweenRetries = TimeSpan.FromSeconds(5.0);
            while (numOfRetries > 0)
            {
                await Task.Delay(timeBetweenRetries);
                enrollmentResult = await serviceClient.CheckEnrollmentStatusAsync(location);

                if (enrollmentResult.Status == Status.Succeeded)
                {
                    break;
                }
                else if (enrollmentResult.Status == Status.Failed)
                {
                    throw new EnrollmentException(enrollmentResult.Message);
                }
                numOfRetries--;
            }
            if (numOfRetries <= 0)
            {
                throw new EnrollmentException("Enrollment operation timeout.");
            }

            return enrollmentResult.ProcessingResult.RemainingEnrollmentSpeechTime;
        }

    }
}
