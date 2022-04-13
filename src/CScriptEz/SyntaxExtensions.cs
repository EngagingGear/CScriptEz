using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CScriptEz
{
    public static class SyntaxExtensions
    {
        private const string EntryMethodName = "Main";
        private const string EntryMethodAnnotation = "MainMethod";

        public static MethodDeclarationSyntax ToMethod(this LocalFunctionStatementSyntax local)
        {
            var isStatic = local.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword));
            var parameters = local.ParameterList;

            var methodType = local.ReturnType;
            var identifier = local.Identifier;

            var annotations = string.Equals(identifier.ValueText, EntryMethodName, StringComparison.OrdinalIgnoreCase)
                ? new SyntaxAnnotation(EntryMethodAnnotation)
                : new SyntaxAnnotation();

            var methodDeclaration = SyntaxFactory
                .MethodDeclaration(methodType, identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .WithParameterList(parameters)
                .WithBody(local.Body)
                .WithAdditionalAnnotations(annotations);

            return methodDeclaration;
        }

        public static MethodDeclarationSyntax ToEntryMethod(this List<SyntaxNode> content)
        {
            var args = SyntaxFactory.ParameterList();
            var p = SyntaxFactory.Parameter(SyntaxFactory.Identifier("args"));
            p = p.WithType(SyntaxFactory.ParseTypeName("string[]"));
            args = args.AddParameters(p);

            var methodDeclaration = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), SyntaxFactory.Identifier(EntryMethodName))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .WithParameterList(args)
                .WithBody(SyntaxFactory.Block(content.Select(x => (StatementSyntax)x)))
                .WithAdditionalAnnotations(new SyntaxAnnotation(EntryMethodAnnotation));

            return methodDeclaration;
        }
    }
}