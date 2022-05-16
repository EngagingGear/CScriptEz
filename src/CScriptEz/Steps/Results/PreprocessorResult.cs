using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CScriptEz
{
    public class PreprocessorResult
    {
        public PreprocessorResult(CompilationUnitSyntax root)
        {
            Root = root;
            Libraries = new List<LibraryDescriptor>();
            Packages = new List<PackageDescriptor>();
        }

        public CompilationUnitSyntax Root { get; }
        public List<LibraryDescriptor> Libraries { get; }
        public List<PackageDescriptor> Packages { get; }
        public bool Cached { get; set; }
    }
}