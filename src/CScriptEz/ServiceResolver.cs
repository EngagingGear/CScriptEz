using System;
using System.ComponentModel.Design;
using CScriptEz.Data;
using CScriptEz.Steps;
using CScriptEz.Steps.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CScriptEz
{
    public class ServiceResolver : IServiceResolver, IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        public ServiceResolver()
        {
            var services = new ServiceCollection();
            RegisterLoggers(services);
            RegisterServices(services);
            services.AddSingleton<IServiceResolver>(factory => this);

            _serviceProvider = services.BuildServiceProvider();
        }

        public T Resolve<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        private void RegisterLoggers(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("Log.txt")
                .CreateLogger();

            services.AddLogging(logging =>
                {
                    //logging.AddConsole();
                    logging.AddSerilog();
                })
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);
        }

        private void RegisterServices(IServiceCollection services)
        {

            services.AddSingleton<IScriptFileReader, ScriptFileReader>();
            services.AddSingleton<IPreprocessor, Preprocessor>();
            services.AddSingleton<IPackageProcessor, PackageProcessor>();
            services.AddSingleton<IScriptAnalyzer, ScriptAnalyzer>();
            services.AddSingleton<IAnalyzedResultProcessor, AnalyzedResultProcessor>();
            services.AddSingleton<IProgramFileGenerator, ProgramFileGenerator>();
            services.AddSingleton<IAssemblyGenerator, AssemblyGenerator>();
            services.AddSingleton<IProgramFileExecutor, ProgramFileExecutor>();
            services.AddSingleton<ICScriptEzDbContextFactory, CScriptEzDbContextFactory>();
            
            services.AddSingleton<ICScriptEzApplication, CScriptEzApplication>();
            services.AddSingleton<IWorkflowStepsProvider, WorkflowStepsProvider>();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}