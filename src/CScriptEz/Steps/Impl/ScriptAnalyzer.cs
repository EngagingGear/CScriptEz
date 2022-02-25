using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class ScriptAnalyzer : ServiceBase, IScriptAnalyzer
    {
        public ScriptAnalyzer(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ScriptAnalyzer>())
        {
        }

        public void Run(ExecutionContext context)
        {
            LogTitle("Analyzing script:");
            // var code = context.ScriptContent;
            // var tree = CSharpSyntaxTree.ParseText(code);
            // var root = tree.GetCompilationUnitRoot();
            var root = context.PreprocessorResult.Root;

            Log($"Three tree has {root.Members.Count} elements in it");
            Log($"Three tree is a {root.Kind()} node");
            Log($"Three tree has {root.Usings.Count} using statements.");


            var hasMainMethod = false;
            var hasMethods = false;
            var hasOtherContent = false;

            foreach (var member in root.Members)
            {
                if (!(member is GlobalStatementSyntax globalStatement))
                {
                    Log($"\t\tExpected node of kind GlobalStatementSyntax but was Kind: {member.Kind()}");
                    continue;
                }

                var list = globalStatement.ChildNodes().ToList();
                if (list.Count != 1 || !(list[0] is LocalFunctionStatementSyntax local))
                {
                    Log($"\t\tExpected node of kind LocalFunctionStatementSyntax but was Kind: {list[0].Kind()}");
                    hasOtherContent = true;
                }
                else
                {
                    hasMethods = true;
                    if (string.Equals(local.Identifier.ValueText, "Main", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMainMethod = true;
                    }
                }
            }

            var result = new AnalyzerResult
            {
                HasMethods = hasMethods,
                HasEntryMethod = hasMainMethod,
                HasContentOutsideOfMethods = hasOtherContent
            };

            if (result.HasMethods && result.HasContentOutsideOfMethods)
            {
                throw new CScriptEzException("Script has both methods and global statements.");
            }

            if (result.HasMethods && !result.HasEntryMethod)
            {
                throw new CScriptEzException("Main method was not found.");
            }

            context.AnalyzerResult = result;
        }
    }
}