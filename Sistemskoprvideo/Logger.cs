using System;
using System.IO;

namespace Sistemskoprvideo
{
    internal class Logger
    {
        private object locker = new object();
        private string logFile = "log.txt";

        public void Log(string message)
        {
            lock (locker)
            {
                string text = $"[{DateTime.Now:HH:mm:ss}] {message}";

                Console.WriteLine(text);
                File.AppendAllText(logFile, text + Environment.NewLine);
            }
        }
    }
}