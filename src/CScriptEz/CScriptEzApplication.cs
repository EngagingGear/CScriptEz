using System;
using Microsoft.Extensions.Logging;

namespace CScriptEz
{
    public class CScriptEzApplication : ServiceBase, ICScriptEzApplication
    {
        private readonly IWorkflowStepsProvider _stepsProvider;

        public CScriptEzApplication(IWorkflowStepsProvider stepsProvider, ILoggerFactory loggerFactory): base(loggerFactory.CreateLogger<CScriptEzApplication>())
        {
            _stepsProvider = stepsProvider;
        }

        public void Run(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    Logger.LogError("Path to script file expected");
                    return;
                }

                var context = new ExecutionContext(args[0], args);

                var steps = _stepsProvider.GetSteps();
                foreach (var stepProcessor in steps)
                {
                    stepProcessor.Run(context);
                }
            }
            catch (Exception exception)
            {
                Log(
                    $"Exception when processing script. Error: {exception.Message}. StackTrace: {exception.StackTrace}");
            }
        }
    }
}