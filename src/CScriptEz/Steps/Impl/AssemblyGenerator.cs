using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CScriptEz.Data;
using CScriptEz.Data.Models;
using CScriptEz.Steps.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;

namespace CScriptEz.Steps.Impl
{
    public class AssemblyGenerator : ServiceBase, IAssemblyGenerator
    {
        private readonly ICScriptEzDbContextFactory _dbContextFactory;

        public AssemblyGenerator(ICScriptEzDbContextFactory dbContextFactory, ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<AssemblyGenerator>())
        {
            _dbContextFactory = dbContextFactory;
        }

        public void Run(ExecutionContext context)
        {
            byte[] assemblyBytes = null;

            LogTitle("Generating Assembly");

            try
            {
                var programFileGenerationResult = context.ProgramFileGenerationResult;
                var additionalLibraries = context.PreprocessorResult.Libraries;
                var cached = context.PreprocessorResult.Cached;
                var scriptInfo = context.Script;
                var code = programFileGenerationResult.ProgramFileCode;
                if (cached)
                {
                    assemblyBytes = GetCachedAssemblyBytes(scriptInfo);
                    if (assemblyBytes != null)
                    {
                        Log("Using cached assembly");
                        context.AssemblyGenerationResult = new AssemblyGenerationResult
                        {
                            AssemblyContent = assemblyBytes
                        };
                        return;
                    }
                }

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
                    assemblyBytes = stream.ToArray();
                }
                else
                {
                    Log("Errors on compilation of program file:");
                    foreach (var diagnostic in compileResult.Diagnostics)
                    {
                        Log(diagnostic.ToString());
                    }

                    throw new CScriptEzException("Error on compilation program file");
                }

                if (cached)
                {
                    SaveAssemblyBytes(scriptInfo, assemblyBytes);
                }
            }
            catch (Exception exception)
            {
                Log($"Exception when preparing and executing program file, Error: {exception.Message}");
                Log($"Exception stack trace: {exception.StackTrace}");
                throw;
            }

            context.AssemblyGenerationResult = new AssemblyGenerationResult
            {
                AssemblyContent = assemblyBytes
            };
        }

        private PortableExecutableReference[] PrepareReferences(List<string> additionalLibraries)
        {
            var standardAssembliesPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var localAssembliesPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            var references = new List<PortableExecutableReference>();

            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "mscorlib.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "netstandard.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "Microsoft.CSharp.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Private.CoreLib.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Collections.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Console.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Core.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Data.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Globalization.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.IO.FileSystem.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Linq.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Net.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.ObjectModel.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, "System.Runtime.dll")));
            
            references.Add(MetadataReference.CreateFromFile(Path.Combine(localAssembliesPath, "EPPlus.dll")));
            references.Add(MetadataReference.CreateFromFile(Path.Combine(localAssembliesPath, "CsvHelper.dll")));

            // references.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "Newtonsoft.Json")));

            if (additionalLibraries?.Count > 0)
            {
                references.AddRange(
                    additionalLibraries.Select(library =>
                        MetadataReference.CreateFromFile(Path.Combine(standardAssembliesPath, library))));
            }

            return references.ToArray();
        }

        private byte[] GetCachedAssemblyBytes(ScriptInfo scriptInfo)
        {
            Log($"Trying to restore assembly bytes for script: {scriptInfo}");

            using var dbContext = _dbContextFactory.Create();
            var file = dbContext.Files.FirstOrDefault(x => x.FileName == scriptInfo.FileName);
            if (file == null)
            {
                Log($"No saved assembly information found in database for script: {scriptInfo}");
                return null;
            }

            if (file.SourceHash != scriptInfo.ContentHash)
            {
                return null;
            }

            var fileData = dbContext.FilesData.Include(x => x.FileModel)
                .FirstOrDefault(x => x.FileModel.Id == file.Id);

            return fileData?.Data;
        }

        private void SaveAssemblyBytes(ScriptInfo scriptInfo, byte[] data)
        {
            Log($"Saving assembly bytes for script: {scriptInfo}");
            using var dbContext = _dbContextFactory.Create();
            var file = dbContext.Files.FirstOrDefault(x => x.FileName == scriptInfo.FileName);
            if (file == null)
            {
                file = new FileModel()
                {
                    FileName = scriptInfo.FileName,
                    ModifiedDate = scriptInfo.ModifiedDate,
                    SourceHash = scriptInfo.ContentHash,
                };
                dbContext.Files.Add(file);
                dbContext.SaveChanges();
                file = dbContext.Files.FirstOrDefault(x => x.FileName == scriptInfo.FileName);
            }

            var fileData = dbContext.FilesData.Include(x => x.FileModel)
                .FirstOrDefault(x => x.FileModel.Id == file.Id);

            if (fileData == null)
            {
                fileData = new FileDataModel()
                {
                    FileModel = file,
                    Data = data
                };
                dbContext.FilesData.Add(fileData);
            }
            else
            {
                fileData.Data = data;
            }
            dbContext.SaveChanges();
        }
    }
}