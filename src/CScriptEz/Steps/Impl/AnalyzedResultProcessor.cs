using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class AnalyzedResultProcessor : ServiceBase, IAnalyzedResultProcessor
    {
        public AnalyzedResultProcessor(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<AnalyzedResultProcessor>())
        {
        }

        public void Run(ExecutionContext context)
        {
            LogTitle("Processing script:");

            var result = new ProcessedResult();

            if (context.Args?.Length > 1)
            {
                for (var i = 1; i < context.Args.Length - 1; i++)
                {
                    result.Arguments.Add(context.Args[i]);
                }
            }

            var root = context.PreprocessorResult.Root;
            var hasMethods = context.AnalyzerResult.HasMethods;

            foreach (var usingDirective in root.Usings)
            {
                result.Usings.Add(usingDirective);
                Log($"\tUsing directive: {usingDirective.Name}");
            }

            var scriptContent = new List<SyntaxNode>();

            foreach (var globalStatementSyntax in root.Members.OfType<GlobalStatementSyntax>())
            {
                if (!hasMethods)
                {
                    Log("Processing script content");
                    scriptContent.AddRange(globalStatementSyntax.ChildNodes().ToList());
                }
                else
                {
                    var localFunction = globalStatementSyntax.ChildNodes().First() as LocalFunctionStatementSyntax;
                    Log("Converting local function declaration to method declaration");
                    var method = localFunction.ToMethod();
                    result.Methods.Add(method);
                }
            }

            if (!hasMethods)
            {
                result.Methods.Add(scriptContent.ToEntryMethod());
            }

            context.ProcessedResult = result;
        }
    }
}