using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{

    public interface INeedToLog
    {
    }

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


        public static void LogIt(this INeedToLog me, string line)
        {
            Console.WriteLine(line);
            File.AppendAllText(System.Diagnostics.Process.GetCurrentProcess().ProcessName + "_output.txt", line + Environment.NewLine);
        }

        public static void LogIt(this INeedToLog me, string line, params object[] args)
        {
            LogIt(me, string.Format(line, args));
        }

    }
}
