using System;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;

namespace TextAnalytics
{
    class Program
    {
        static void Main(string[] args)
        {

            // Create a client.
            ITextAnalyticsAPI client = new TextAnalyticsAPI();
            client.AzureRegion = AzureRegions.Westeurope;
            client.SubscriptionKey = "25978b82698b43c380155b99f3bd8196";

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var speakerFile = @"D:\dev\ambient\margin_call_transcript.txt";

            var lines = System.IO.File.ReadAllLines(speakerFile)
                .Where(l => l.StartsWith("Final"))
                .Select(l => l.Split(',').Last())
                .ToArray();

            var all = String.Join(" ", lines);

            if (all.Length > 5000)
            {
                all = all.Substring(0, 5000);
            }

            var phrases = lines
                .Select((l, i) => new MultiLanguageInput("en", i.ToString(), l))
                .ToArray();

            var allphrase = new MultiLanguageInput("en", "all", all);

            KeyPhraseBatchResult result2 = client.KeyPhrases(new MultiLanguageBatchInput(new[] { allphrase }));

            foreach (var document in result2.Documents)
            {
                Console.WriteLine("Document ID: {0} ", document.Id);

                Console.WriteLine("\t Key phrases:");

                foreach (string keyphrase in document.KeyPhrases)
                {
                    Console.WriteLine("\t\t" + keyphrase);
                }
            }


            SentimentBatchResult result3 = client.Sentiment(new MultiLanguageBatchInput(phrases));

            // Printing sentiment results
            foreach (var document in result3.Documents)
            {
                Console.WriteLine("Sentiment Score: {1:0.00}, doc: {0}", lines[int.Parse(document.Id)], document.Score);
            }

            foreach (var document in result3.Documents.Where(d => d.Score < 0.2 && lines[int.Parse(d.Id)].Length > 20))
            {
                Console.WriteLine("NEG Sentiment: {0}", lines[int.Parse(document.Id)]);
            }

            foreach (var document in result3.Documents.Where(d => d.Score > 0.8 && lines[int.Parse(d.Id)].Length > 20))
            {
                Console.WriteLine("POS Sentiment: {0}", lines[int.Parse(document.Id)]);
            }


        }
    }
}
