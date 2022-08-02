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

        protected void Log(string message, bool messageOnNextLine = false)
        {
            if (messageOnNextLine)
            {
                Logger.LogInformation($"{GetType().Name}");
                Logger.LogInformation($"{Environment.NewLine}{message}");
            }
            else
            {
                Logger.LogInformation($"{GetType().Name}   {message}");
            }
        }

        protected void LogDelimiter()
        {
            Logger.LogInformation("=======================================================");
        }

        protected void LogTitle(string title)
        {
            LogDelimiter();
            Log(title);
        }

        protected void LogException(Exception e)
        {
            Log(e.Message, true);
            var inner = e.InnerException;
            while (inner != null)
            {
                Log(inner.Message, true);
                inner = inner.InnerException;
            }
        }
    }
}