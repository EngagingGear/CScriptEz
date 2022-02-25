using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CScriptEz
{
    public class ProcessedResult
    {
        public ProcessedResult()
        {
            Usings = new List<UsingDirectiveSyntax>();
            Arguments = new List<string>();
            Methods = new List<MethodDeclarationSyntax>();
        }

        public List<UsingDirectiveSyntax> Usings { get; }
        public List<string> Arguments { get; }
        public List<MethodDeclarationSyntax> Methods { get; }
    }
}