using System.Collections.Generic;
using Utils;

namespace STT
{
    interface ISttManager : INeedToLog
    {
        void Done();
        void Init();
        void SendAudio(string wavFileName);
        IList<string> Outputs { get; }
        bool Complete { get; }
    }
}