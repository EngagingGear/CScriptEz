using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
            if (!HasDefinedNugetPackages(context))
            {
                RunInProcess(context);
            }
            else
            {
                RunOutOfProcess(context);
            }
        }

        private void RunInProcess(ExecutionContext context)
        {
            LogTitle("Executing assembly in process");

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

                File.WriteAllText(
                    Path.ChangeExtension("CScriptExecutor.exe", "runtimeconfig.json"),
                    GenerateRuntimeConfig()
                );

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
            
            catch (Exception exception)
            {
                Log($"Exception when preparing and executing program file, Error: {exception.Message}");
                Log($"Exception stack trace: {exception.StackTrace}");
            }
        }

        private void RunOutOfProcess(ExecutionContext context)
        {
            LogTitle("Executing assembly out of process");
            var assemblyGenerationResult = context.AssemblyGenerationResult;
            var localAssembliesPath = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty;
            var executorFileName = "CScriptExecutor.exe";
            var executorPath = Path.Combine(localAssembliesPath, executorFileName);
            var runtimeConfigPath = Path.ChangeExtension(executorPath, "runtimeconfig.json");
            try
            {
                if (File.Exists(executorPath))
                {
                    File.Delete(executorPath);
                }

                if (File.Exists(runtimeConfigPath))
                {
                    File.Delete(runtimeConfigPath);
                }

                File.WriteAllBytes(executorPath, assemblyGenerationResult.AssemblyContent);
                File.WriteAllText(runtimeConfigPath, GenerateRuntimeConfig());
                var argsString = context.ProcessedResult.Arguments.Count > 0
                    ? string.Join(" ", context.ProcessedResult.Arguments)
                    : string.Empty;

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    Arguments = string.IsNullOrWhiteSpace(argsString) ? $"{executorPath}" : $"{executorPath} {argsString}"
                };

                Log($"Invoking file: {executorPath} with generated code. Runtime configuration file: {runtimeConfigPath}");
                var process = new Process();
                using (process)
                {
                    process.StartInfo = psi;
                    process.Start();

                    while (!process.StandardOutput.EndOfStream)
                    {
                        var res = process.StandardOutput.ReadLine();
                        Console.WriteLine(res);
                        Log(res);
                    }
                }
            }

            catch (Exception exception)
            {
                Log($"Exception when preparing and executing program file, Error: {exception.Message}");
                Log($"Exception stack trace: {exception.StackTrace}");
            }

        }

        private string GenerateRuntimeConfig()
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(
                       stream,
                       new JsonWriterOptions() { Indented = true }
                   ))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("runtimeOptions");
                writer.WriteStartObject("framework");
                writer.WriteString("name", "Microsoft.NETCore.App");
                writer.WriteString(
                    "version",
                    "5.0.0"
                );
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private bool HasDefinedNugetPackages(ExecutionContext context)
        {
            var packages = context.PreprocessorResult.Packages;
            return packages != null && packages.Count != 0;
        }
    }
}