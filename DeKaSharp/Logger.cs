using System;
using System.Collections.Generic;
using System.Text;

namespace DeKaSharp
{
    internal static class Logger
    {
        private static Lock _logLocker = new();

        private static string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"logs.txt");

        static Logger()
        {
            using (File.Create(_logFilePath)) { };
        }

        public static void Log(string info)
        {
            lock(_logLocker)
            {
                using var fileStream = new FileStream(
                    _logFilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);

                using var writer = new StreamWriter(fileStream, Encoding.UTF8);

                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {info}";

                writer.WriteLine(line);

                writer.Flush();
            }
        }
    }
}
