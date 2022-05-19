using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CScriptEz
{
    public class CScriptEzApplication : ServiceBase, ICScriptEzApplication
    {
        private readonly IArgumentsParser _argumentsParser;
        private readonly IWorkflowStepsProvider _stepsProvider;

        public CScriptEzApplication(IWorkflowStepsProvider stepsProvider, IArgumentsParser argumentsParser, ILoggerFactory loggerFactory): base(loggerFactory.CreateLogger<CScriptEzApplication>())
        {
            _stepsProvider = stepsProvider;
            _argumentsParser = argumentsParser;
        }

        public void Run(string[] args)
        {
            try
            {
                var parsedArguments = _argumentsParser.Parse(args);

                if (IsScriptExecution(parsedArguments))
                {
                    ProcessCommands(parsedArguments);
                }
                else
                {
                    ProcessScriptExecution(parsedArguments);
                }
            }
            catch (Exception exception)
            {
                Log(
                    $"Exception when processing script. StackTrace: {exception.StackTrace}");
                LogException(exception);
            }
        }

        private bool IsScriptExecution(ArgumentsParserResult parsedArguments)
        {
            return string.IsNullOrWhiteSpace(parsedArguments.ScriptFileName);
        }

        private void ProcessCommands(ArgumentsParserResult parsedArguments)
        {
            var context = new CommandContext(parsedArguments.Commands);
            var steps = _stepsProvider.GetCommandProcessingSteps();
            foreach (var stepProcessor in steps)
            {
                stepProcessor.Run(context);
            }
        }

        private void ProcessScriptExecution(ArgumentsParserResult parsedArguments)
        {
            var context = new ExecutionContext(parsedArguments.ScriptFileName, parsedArguments.RawArgs);

            var steps = _stepsProvider.GetScriptExecutionSteps();
            foreach (var stepProcessor in steps)
            {
                stepProcessor.Run(context);
            }
        }
    }
}