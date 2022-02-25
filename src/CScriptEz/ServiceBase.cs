using System;
using Microsoft.Extensions.Logging;

namespace CScriptEz
{
    public abstract class ServiceBase
    {
        protected readonly ILogger Logger;

        protected ServiceBase(ILogger logger)
        {
            Logger = logger;
        }

        protected void Log(string message)
        {
            Logger.LogInformation(message);
        }

        protected void LogDelimiter()
        {
            Logger.LogInformation("=======================================================");
        }

        public void LogTitle(string title)
        {
            LogDelimiter();
            Log(title);
        }
    }
}