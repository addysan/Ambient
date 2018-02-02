using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;
using System.Diagnostics;
using Microsoft.Bing.Speech;

namespace STT
{


    class Program : INeedToLog
    {

        public static ISttManager GetSTT()
        {
            //return  new OxfordSttManager();
            return new BingSttManager();
        }

        static void Main(string[] args)
        {
            /*
            for (int i = 5; i <= 50; i++)
            {
                sst.SendAudio(@"D:\dev\raspberrypi\node_streaming_server\segments\output" + i + ".wav");
            }*/

            ConversationalStt(@"D:\dev\ambient\itai0.wav");

                /*
            var sst = GetSTT();
            sst.SendAudio(@"D:\dev\ambient\raspberrypi\node_streaming_server\meeting.wav");
            sst.Done();*/

            //SttBySpeaker(@"D:\dev\ambient\raspberrypi\node_streaming_server\Segments\SegmentsBySpeaker.csv");

            Console.ReadLine();
        }

        public static void ConversationalStt(string fullrecordingPath)
        {
            var stt = new BingSttManager();

            RecognitionPhrase phrase = null;
            RecognitionStatus status = RecognitionStatus.None;

            var phrases = new List<Tuple<long,RecognitionPhrase>>();
            var phrasesPartial = new List<Tuple<long, RecognitionPhrase>>();

            long start = 0;
            var skip = new TimeSpan(0, 0, 2);

            var length = WavFileUtils.WavFileLength(fullrecordingPath);

            stt.Callback = (RecognitionResult r) =>
            {
                phrase = r.Phrases.FirstOrDefault();
                status = r.RecognitionStatus;
            };

            stt.CallbackPartial = (RecognitionPartialResult r) =>
            {
                phrasesPartial.Add(new Tuple<long, RecognitionPhrase>(start, r));
                stt.LogIt("Partial,{0},{1},{2}", start, r.MediaDuration, r.DisplayText);
            };

            do
            {
                var startSpan = new TimeSpan(start);

                WavFileUtils.TrimWavFile(fullrecordingPath, "temp.wav", startSpan, TimeSpan.Zero);

                stt.SendAudio("temp.wav");

                if (status == RecognitionStatus.Success)
                {
                    phrases.Add(new Tuple<long, RecognitionPhrase>(start, phrase));
                    stt.LogIt("Final,{0},{1},{2}", start, phrase.MediaDuration, phrase.DisplayText);
                    start += (long)(phrase.MediaTime + phrase.MediaDuration);
                }
                else
                {
                    start += skip.Ticks;
                    status = RecognitionStatus.Success;
                }
            }
            while (start <= length.Subtract(skip).Ticks);

        }


        public static void SttBySpeaker(string speakerFile)
        {

            var lines = System.IO.File.ReadAllLines(speakerFile);

            foreach(var line in lines)
            {
                var split = line.Split(':');
                if (split.Length != 2)
                {
                    Console.WriteLine("invalid line: {0}", line);
                    continue;
                }

                var speaker = split[0].Trim();
                var segments = split[1].Trim().Split(',');
                var files = segments.Select(s => @"D:\dev\ambient\raspberrypi\node_streaming_server\Segments\output" + s + ".wav").ToArray();
                var timestamp = new TimeSpan(0, 0, int.Parse(segments.First()) * 5).ToString();

                var sst = GetSTT();
                string tempFile = null;
                bool pending = false;
                try
                {
                    sst.Init();
                    tempFile = WavFileUtils.MergeWavFiles(files);
                    sst.SendAudio(tempFile);
                    sst.Done();
                    pending = true;
                }
                finally
                {
                    if (tempFile != null)
                    {
                        File.Delete(tempFile);
                    }
                }

                Stopwatch sw = Stopwatch.StartNew();
                while (pending && !sst.Complete)
                {
                    Task.Delay(500).Wait();
                }

                sst.LogIt("({0}) {1}: '{2}'", timestamp, speaker, String.Concat(sst.Outputs));
            }

        }
    }

}
