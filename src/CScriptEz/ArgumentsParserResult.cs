using System.Collections.Generic;

namespace CScriptEz
{
    public class ArgumentsParserResult
    {
        public ArgumentsParserResult(string[] rawArgs, string scriptFileName, string[] scriptArgs, IList<CommandDescriptor> commands = null)
        {
            ScriptFileName = scriptFileName;
            ScriptArgs = scriptArgs;
            RawArgs = rawArgs;
            Commands = commands ?? new List<CommandDescriptor>();
        }

        public ArgumentsParserResult(string[] rawArgs, IList<CommandDescriptor> commands)
        {
            Commands = commands;
            RawArgs = rawArgs;
        }

        public string[] RawArgs { get; }
        public string ScriptFileName { get; }
        public string[] ScriptArgs { get; }
        public IList<CommandDescriptor> Commands { get; }
    }
}