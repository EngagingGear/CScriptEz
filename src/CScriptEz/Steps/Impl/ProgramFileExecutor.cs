using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class ProgramFileExecutor : ServiceBase, IProgramFileExecutor
    {
        public ProgramFileExecutor(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ProgramFileExecutor>())
        {
        }

        public void Run(ExecutionContext context)
        {
            LogTitle("Executing assembly");

            var assemblyGenerationResult = context.AssemblyGenerationResult;
            var args = context.ProcessedResult.Arguments.Count > 0
                ? context.ProcessedResult.Arguments.ToArray()
                : new object[] { null };

            try
            {
                var assembly = Assembly.Load(assemblyGenerationResult.AssemblyContent);
                if (assembly == null)
                {
                    Log("Cannot load assembly from compilation stream");
                    throw new Exception("Cannot load assembly from compilation stream");
                }

                if (assembly.EntryPoint == null)
                {
                    Log("Entry point not found in generated assembly");
                    throw new Exception("Entry point not found in generated assembly");
                }

                Log($"Invoking method: {assembly.EntryPoint.Name} of dynamic assembly: {assembly.GetName().Name}");

                try
                {
                    assembly.EntryPoint.Invoke(null, BindingFlags.NonPublic | BindingFlags.Static, null, args,
                        null);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Exception when invoking method: {assembly.EntryPoint.Name}");
                }
            }
            /*
            var programFileGenerationResult = context.ProgramFileGenerationResult;
            var additionalLibraries = context.PreprocessorResult.Libraries;
            var args = context.ProcessedResult.Arguments.Count > 0
                ? context.ProcessedResult.Arguments.ToArray()
                : new object[] { null };

            LogTitle("Executing Program File");
            var code = programFileGenerationResult.ProgramFileCode;

            try
            {
                var tree = SyntaxFactory.ParseSyntaxTree(code);
                var references = PrepareReferences(additionalLibraries);

                var compilation = CSharpCompilation.Create(
                    "CScriptExecutor.exe",
                    options: new CSharpCompilationOptions(
                        OutputKind.ConsoleApplication),
                    syntaxTrees: new[] { tree },
                    references: references);

                using var stream = new MemoryStream();

                var compileResult = compilation.Emit(stream);

                if (compileResult.Success)
                {
                    // Load the assembly into memory
                    stream.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(stream.ToArray());
                    if (assembly == null)
                    {
                        Log("Cannot load assembly from compilation stream");
                        throw new Exception("Cannot load assembly from compilation stream");
                    }

                    if (assembly.EntryPoint == null)
                    {
                        Log("Entry point not found in generated assembly");
                        throw new Exception("Entry point not found in generated assembly");
                    }


                    Log($"Invoking method: {assembly.EntryPoint.Name} of dynamic assembly: {assembly.GetName().Name}");

                    try
                    {
                        assembly.EntryPoint.Invoke(null, BindingFlags.NonPublic | BindingFlags.Static, null, args,
                            null);
                    }
                    catch (Exception e)
                    {
                        Log($"Exception when invoking method: {assembly.EntryPoint.Name}, Error: {e.Message}");
                        Log($"Exception stack trace: {e.StackTrace}");
                    }
                }
                else
                {
                    Log("Errors on compilation of program file:");
                    foreach (var diagnostic in compileResult.Diagnostics)
                    {
                        Log(diagnostic.ToString());
                    }
                }
            }
            */
            catch (Exception exception)
            {
                Log($"Exception when preparing and executing program file, Error: {exception.Message}");
                Log($"Exception stack trace: {exception.StackTrace}");
            }
        }


        private PortableExecutableReference[] PrepareReferences(List<string> additionalLibraries)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var references = new List<PortableExecutableReference>();

            references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.IO.FileSystem.dll")));

            if (additionalLibraries?.Count > 0)
            {
                references.AddRange(
                    additionalLibraries.Select(library =>
                        MetadataReference.CreateFromFile(Path.Combine(assemblyPath, library))));
            }

            return references.ToArray();
        }


        /*
        private void Compile(string programCode, HashSet<string> extraLibraries)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programCode);
            if (extraLibraries == null)
                extraLibraries = new HashSet<string>();
            extraLibraries.Add(typeof(object).Assembly.Location);
            extraLibraries.Add((typeof(ExpandoObject).Assembly.Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("netstandard")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("mscorlib")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Runtime")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.IO.Filesystem")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("Newtonsoft.Json")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.ObjectModel")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Collections")).Location));
            extraLibraries.Add((Assembly.Load(new AssemblyName("System.Linq")).Location));
            // Add standard libraries;
            var metadataReferences = new List<MetadataReference>();
            foreach (var lib in extraLibraries)
                metadataReferences.Add(MetadataReference.CreateFromFile(lib));
            var compilation = CSharpCompilation.Create(
                    _generatedAssemblyName,
                    new[] { syntaxTree },
                    metadataReferences,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (result.Success)
                {
                    // Load the assembly into memory
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    _assembly = Assembly.Load(memoryStream.ToArray());
                }
                else
                {
                    _compileFailed = true;
                    throw new TemplateEzException("Compile failed")
                    {
                        Code = programCode,
                        CompileErrors = result
                    };
                }
            }
        }
        */
    }
}