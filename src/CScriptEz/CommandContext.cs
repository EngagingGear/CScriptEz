using System.Collections.Generic;

namespace CScriptEz
{
    public class CommandContext
    {
        public CommandContext(IList<CommandDescriptor> commands)
        {
            Commands = commands;
        }

        public IList<CommandDescriptor> Commands { get; }
    }
}