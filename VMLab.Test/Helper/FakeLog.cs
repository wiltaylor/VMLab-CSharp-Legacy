using System;
using VMLab.Helper;

namespace VMLab.Test.Helper
{
    public class FakeLog : ILog
    {
        public void Debug(string message)
        {
            //stubbed out
        }

        public void Debug(string message, Exception exception)
        {
            //stubbed out
        }

        public void Info(string message)
        {
            //stubbed out
        }

        public void Info(string message, Exception exception)
        {
            //stubbed out
        }

        public void Warn(string message)
        {
            //stubbed out
        }

        public void Warn(string message, Exception exception)
        {
            //stubbed out
        }

        public void Error(string message)
        {
            //stubbed out
        }

        public void Error(string message, Exception exception)
        {
            //stubbed out
        }

        public void Fatal(string message)
        {
            //stubbed out
        }

        public void Fatal(string message, Exception exception)
        {
            //stubbed out
        }
    }
}