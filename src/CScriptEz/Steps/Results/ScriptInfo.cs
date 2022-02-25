using System;

namespace CScriptEz.Steps.Results
{
    public class ScriptInfo
    {
        public string FileName { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Content { get; set; }
        public string ContentHash { get; set; }

        public override string ToString()
        {
            return $"ScriptInfo: FileName: {FileName}, ModifiedDate: {ModifiedDate}";
        }
    }
}