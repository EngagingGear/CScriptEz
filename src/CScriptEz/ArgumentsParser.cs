using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CScriptEz
{
    public class ArgumentsParser : ServiceBase, IArgumentsParser
    {
        private const string CommandFlag = "--";
        private const string ClearCacheCommand = "clear-cache";
        private const string ClearStaleCommand = "clear-stale";

        public ArgumentsParser(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ArgumentsParser>())
        {
        }

        public ArgumentsParserResult Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new OperationCanceledException("Expect commands or script file");
            }
            Log($"Parsing arguments. {string.Join(" ", args)}");

            if (IsCommand(args[0]))
            {
                var commands = ParseCommands(args);
                return new ArgumentsParserResult(args, commands);
            }

            var scriptFileName = args[0];
            var scriptArgs = new string[args.Length - 1];
            if (args.Length > 1)
            {
                args.CopyTo(scriptArgs, 1);
            }

            Log($"Script will be processed and executed. Script File: {scriptFileName}, arguments: {string.Join(" ", scriptArgs)}");
            return new ArgumentsParserResult(args, scriptFileName, scriptArgs);
        }

        private bool IsCommand(string arg)
        {
            return arg.Trim().StartsWith(CommandFlag);
        }

        private IList<CommandDescriptor> ParseCommands(string[] args)
        {
            var commandDescription = args[0].Trim().Remove(0, CommandFlag.Length);
            string fileName = null;
            string commandName;
            if (string.Equals(commandDescription, ClearCacheCommand, StringComparison.OrdinalIgnoreCase))
            {
                commandName = KnownCommands.ClearCache;
                if (args.Length > 1)
                {
                    fileName = args[1];
                }
                Log($"ClearCache command recognized. FileName: {fileName}");
            }
            else if (string.Equals(commandDescription, ClearStaleCommand, StringComparison.OrdinalIgnoreCase))
            {
                commandName = KnownCommands.ClearStale;
                if (args.Length > 1)
                {
                    fileName = args[1];
                }
                Log($"ClearStale command recognized. FileName: {fileName}");
            }
            else
            {
                throw new ArgumentException($"Unknown command. {string.Join(" ", args)}");
            }

            var command = new CommandDescriptor(commandName, fileName);
            return new List<CommandDescriptor> { command };
        }
    }
}