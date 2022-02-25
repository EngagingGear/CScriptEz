using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CScriptEz
{
    public class PreprocessorResult
    {
        public PreprocessorResult(CompilationUnitSyntax root)
        {
            Root = root;
            Libraries = new List<string>();
        }

        public CompilationUnitSyntax Root { get; }
        public List<string> Libraries { get; }
        public bool Cached { get; set; }
    }
}