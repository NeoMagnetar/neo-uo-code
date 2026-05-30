using System;
using System.IO;

namespace Server.Custom.AIGM
{
    public static class AIGMExecutionLog
    {
        private static readonly object Sync = new object();

        public static void Write(string format, params object[] args)
        {
            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMExecution.log");
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string line = DateTime.UtcNow.ToString("o") + " " + String.Format(format, args);

                lock (Sync)
                {
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }
    }
}
