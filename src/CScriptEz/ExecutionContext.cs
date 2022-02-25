using CScriptEz.Steps.Results;

namespace CScriptEz
{
    public class ExecutionContext
    {
        public ExecutionContext(string filePath, string[] args)
        {
            FilePath = filePath;
            Args = args;
        }

        public string FilePath { get; }
        public string[] Args { get; }

        public ScriptInfo Script { get; set; }
        public PreprocessorResult PreprocessorResult { get; set; }
        public AnalyzerResult AnalyzerResult { get; set; }
        public ProcessedResult ProcessedResult { get; set; }
        public ProgramFileGenerationResult ProgramFileGenerationResult { get; set; }
        public AssemblyGenerationResult AssemblyGenerationResult { get; set; }
    }
}