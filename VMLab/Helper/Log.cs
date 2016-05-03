using System;
using System.IO;
using VMLab.Model;

namespace VMLab.Helper
{
    public enum LogLevel
    {
        Debug,
        Info,
        Error,
        Warn
    }

    public interface ILog
    {
        void Debug(string message);
        void Debug(string message, Exception exception);
        void Info(string message);
        void Info(string message, Exception exception);
        void Warn(string message);
        void Warn(string message, Exception exception);
        void Error(string message);
        void Error(string message, Exception exception);
        void Fatal(string message);
        void Fatal(string message, Exception exception);
    }

    public class Log : ILog
    {
        private readonly string _logpath;
        private readonly IEnvironmentDetails _environment;

        public Log(IEnvironmentDetails environment)
        {
            var logfolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\VMLab";
            _environment = environment;

            if (!Directory.Exists(logfolder))
                Directory.CreateDirectory(logfolder);

            var time = DateTime.Now;
            _logpath =
                $"{logfolder}\\VMLab-OperatingLog-{time.Year:D4}{time.Month:D2}{time.Day:D2}{time.Hour:D2}{time.Minute:D2}.log";
        }

        private string getTimeStamp()
        {
            var time = DateTime.Now;

            return $"{time.Hour}:{time.Minute}";
        }

        public void Debug(string message)
        {
            File.AppendAllText(_logpath, $"[{getTimeStamp()}-Debug]:{message}{Environment.NewLine}");
        }

        public void Debug(string message, Exception exception)
        {
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Debug]{message}{Environment.NewLine}");
            File.AppendAllText(_logpath,$"[Exception]:{exception}{Environment.NewLine}");
        }

        public void Info(string message)
        {
            _environment.Cmdlet?.WriteVerbose(message);

            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Info]:{message}{Environment.NewLine}");
        }

        public void Info(string message, Exception exception)
        {
            _environment.Cmdlet?.WriteVerbose(message);

            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Info]{message}{Environment.NewLine}");
            File.AppendAllText(_logpath,$"[Exception]:{exception}{Environment.NewLine}");
        }

        public void Warn(string message)
        {
            _environment.Cmdlet?.WriteWarning(message);
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Warn]:{message}{Environment.NewLine}");
        }

        public void Warn(string message, Exception exception)
        {
            _environment.Cmdlet?.WriteWarning(message);
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Warn]{message}{Environment.NewLine}");
            File.AppendAllText(_logpath,$"[Exception]:{exception}{Environment.NewLine}");
        }

        public void Error(string message)
        {
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Error]:{message}{Environment.NewLine}");
        }

        public void Error(string message, Exception exception)
        {
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Error]{message}{Environment.NewLine}");
            File.AppendAllText(_logpath,$"[Exception]:{exception}{Environment.NewLine}");
        }

        public void Fatal(string message)
        {
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Fatal]:{message}{Environment.NewLine}");
        }

        public void Fatal(string message, Exception exception)
        {
            File.AppendAllText(_logpath,$"[{getTimeStamp()}-Fatal]{message}{Environment.NewLine}");
            File.AppendAllText(_logpath,$"[Exception]:{exception}{Environment.NewLine}");
        }
    }
}
