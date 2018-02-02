using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class Meeting
    {
        public IList<Participant> Participants { get; set; }
        public MeetingTranscript Transcript { get; set; }
        public MeetingInfo Info { get; set; }
        public string PathToAudio { get; set; }

        public void Save(string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Meeting Load(string filename)
        {
            string json = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<Meeting>(json);
        }
    }

    public class Participant
    {
        public static Participant Unknown = new Participant { Id = Guid.Empty, Name = "unknown" };

        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    public class MeetingInfo
    {
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class MeetingTranscript
    {
        public IList<MeetingTranscriptSection> Sections { get; set; }
    }

    public class MeetingTranscriptSection
    {
        public Participant Speaker { get; set; }

        public string Text { get; set; }

        public TimeSpan Offset { get; set; }
        public TimeSpan Length { get; set; }

        public double Sentiment { get; set; }

        public IList<MeetingTranscriptSection> SubSections { get; set; }
    }

}
