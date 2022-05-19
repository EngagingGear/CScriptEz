using System.Collections.Generic;

namespace CScriptEz
{
    public class CommandDescriptor
    {
        public CommandDescriptor(string name, string fileName)
        {
            Name = name;
            FileName = fileName;
        }

        public string Name { get; }
        public string FileName { get; }
    }
}