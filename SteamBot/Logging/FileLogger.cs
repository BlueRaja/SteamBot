using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SteamBot.Logging
{
    class FileLogger : LoggerBase, IDisposable
    {
        private StreamWriter FileWriter;

        private bool Disposed = false;

        public FileLogger(JObject logParams)
            : base(logParams)
        {
            Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "logs"));
            string file = (string)logParams["LogFile"];
            FileWriter = File.AppendText(Path.Combine("logs", file != null ? file : "tempLog.log"));
            FileWriter.AutoFlush = true;
        }

        ~FileLogger()
        {
            FileDispose();
        }

        public override void LogMessage(LoggerParams lParams)
        {
            if (Disposed)
                throw new ObjectDisposedException("FileLogger");
            string formattedOutput = FormatLine(lParams);
            if (OutputLevel <= lParams.OutputLevel && FileWriter != null)
                FileWriter.WriteLine(formattedOutput);
        }

        public void Dispose()
        {
            FileDispose();
        }

        private void FileDispose()
        {
            if (!Disposed)
            {
                FileWriter.Dispose();
                FileWriter = null;
                Disposed = true;
            }
        }
    }
}
