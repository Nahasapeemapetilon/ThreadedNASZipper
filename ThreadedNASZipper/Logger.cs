using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace ThreadedNASZipper
{
    public static class Logger
    {
        private static readonly object locker = new object();

        private static readonly Dictionary<int, StreamWriter> writers = new Dictionary<int, StreamWriter>();
        private static ConcurrentQueue<string> logEntriesQ = new ConcurrentQueue<string>();

        private static readonly Lazy<string> lazyLogPath = new Lazy<string>(() =>
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "logging.txt");
        });
        public static string logFilePath => lazyLogPath.Value;

        private static readonly Dictionary<int, List<string>> logEntries = new Dictionary<int, List<string>>();



        public static void Log(string message)
        {
            if (!IniSettings.LoggingEnable)
                return;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            lock (locker)
            {

                if (!logEntries.ContainsKey(threadId))
                {
                    List<string> entries = new List<string>();
                    logEntries.Add(threadId, entries);
                }
                logEntries[threadId].Add($"{DateTime.Now} [Thread {threadId}] {message}");
                logEntriesQ.Enqueue($"{DateTime.Now} [Thread {threadId}] {message}");
            }
        }

        public static void Close()
        {
            if (!IniSettings.LoggingEnable)
                return;
            lock (locker)
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    foreach (int threadId in logEntries.Keys)
                    {
                        foreach (string entry in logEntries[threadId])
                        {
                            writer.WriteLine(entry);
                        }
                    }
                    writer.Flush();
                }
                logEntries.Clear();

                //using (StreamWriter writer = new StreamWriter(logFilePath, true))
                //{
                //    while (logEntriesQ.Count > 0)
                //    {
                //        string entry = string.Empty;
                //        logEntriesQ.TryDequeue(out entry);
                //        if(entry != string.Empty)
                //            writer.WriteLine(entry);
                //    }
                //    writer.Flush();
                //}
            }
        }
    }

}
