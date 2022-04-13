using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class Preprocessor : ServiceBase, IPreprocessor
    {
        private const string DirectiveMark = "@";
        private const string LibraryDirective = "library";
        private const string CacheDirective = "cached";
        private const char LibrariesDelimiter = ',';

        public Preprocessor(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<Preprocessor>())
        {
        }

        public void Run(ExecutionContext context)
        {
            LogTitle("Preprocessing script:");
            var code = context.Script.Content;
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();
            var commentTrivia = root
                .DescendantTrivia()
                .Where(x =>
                {
                    var isComment = x.IsKind(SyntaxKind.SingleLineCommentTrivia);
                    var text = x.ToFullString().TrimStart('/').Trim();
                    var isDirective = text.StartsWith(DirectiveMark);
                    return isComment && isDirective;
                })
                .ToImmutableList();

            var directives = new List<string>();

            var newRoot = root.ReplaceTrivia(commentTrivia, (comment, _) =>
            {
                var directive = GetDirective(comment);
                directives.Add(directive);
                return new SyntaxTrivia();
            });

            var libraries = new List<string>();
            var cache = false;

            foreach (var directive in directives.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (IsLibraryDirective(directive))
                {
                    Log($"Found library directive: {directive}");
                    libraries.AddRange(ParseLibraries(directive));
                }
                else if (IsCacheDirective(directive))
                {
                    Log($"Found cache directive: {directive}");
                    cache = true;
                }
            }

            var result = new PreprocessorResult(newRoot);
            result.Libraries.AddRange(libraries);
            result.Cached = cache;

            context.PreprocessorResult = result;
        }

        private bool IsLibraryDirective(string directive)
        {
            return directive.StartsWith(LibraryDirective);
        }

        private bool IsCacheDirective(string directive)
        {
            return directive.StartsWith(CacheDirective);
        }

        private List<string> ParseLibraries(string directive)
        {
            var result = new List<string>();
            var value = GetDirectiveValue(directive, LibraryDirective);
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            var values = value.Split(LibrariesDelimiter);
            result.AddRange(values.Where(x => !string.IsNullOrWhiteSpace(x)));
            return result;
        }

        private string GetDirective(SyntaxTrivia comment)
        {
            var text = comment.ToFullString().TrimStart('/').Trim();
            var isDirective = text.StartsWith(DirectiveMark);
            return !isDirective ? null : text.TrimStart('@').Trim();
        }

        private string GetDirectiveValue(string directive, string key)
        {
            return directive
                .Replace(key, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }
    }
}