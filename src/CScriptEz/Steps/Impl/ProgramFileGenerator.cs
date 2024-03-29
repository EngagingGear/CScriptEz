﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class ProgramFileGenerator : ServiceBase, IProgramFileGenerator
    {
        public ProgramFileGenerator(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ProgramFileGenerator>())
        {
        }

        public void Run(ExecutionContext context)
        {
            LogTitle("Generating Program File");

            var processedResult = context.ProcessedResult;

            var members = processedResult.Methods.Select(x => (MemberDeclarationSyntax)x).ToArray();

            var classDeclaration = SyntaxFactory
                .ClassDeclaration("Program")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(members);

            var arguments = SyntaxFactory.ParseAttributeArgumentList("(\".NETCoreApp, Version = v5.0\", FrameworkDisplayName=\"\")");
            var name = SyntaxFactory.ParseName("assembly: TargetFramework");
            var attribute = SyntaxFactory.Attribute(name, arguments);

            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            var namespaceDeclaration = SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("CScriptEzNs"))
                .AddAttributeLists(attributeList)
                .NormalizeWhitespace()
                .AddMembers(classDeclaration);

            var syntaxFactory = SyntaxFactory.CompilationUnit()
                .AddUsings(processedResult.Usings.ToArray())
                .AddMembers(namespaceDeclaration);

            var code = syntaxFactory.NormalizeWhitespace().ToFullString();
            PrintGeneratedCode(code);

            var result = new ProgramFileGenerationResult(code);
            context.ProgramFileGenerationResult = result;
        }

        private void PrintGeneratedCode(string code)
        {
            Log(code, true);
        }
    }
}