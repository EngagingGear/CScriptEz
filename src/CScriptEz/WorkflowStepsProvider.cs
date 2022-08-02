using System.Collections.Generic;
using CScriptEz.CommandProcessors;
using CScriptEz.Steps;

namespace CScriptEz
{
    public class WorkflowStepsProvider : IWorkflowStepsProvider
    {
        private readonly IServiceResolver _resolver;

        public WorkflowStepsProvider(IServiceResolver resolver)
        {
            _resolver = resolver;
        }

        public IList<ICommandProcessor> GetCommandProcessingSteps()
        {
            var list = new List<ICommandProcessor>()
            {
                _resolver.Resolve<IClearCacheCommandProcessor>(),
                _resolver.Resolve<IClearStaleCommandProcessor>(),
            };
            return list;
        }

        public IList<IStepProcessor> GetScriptExecutionSteps()
        {
            var list = new List<IStepProcessor>
            {
                _resolver.Resolve<IScriptFileReader>(),
                _resolver.Resolve<IPreprocessor>(),
                _resolver.Resolve<IPackageProcessor>(),
                _resolver.Resolve<IScriptAnalyzer>(),
                _resolver.Resolve<IAnalyzedResultProcessor>(),
                _resolver.Resolve<IProgramFileGenerator>(),
                _resolver.Resolve<IAssemblyGenerator>(),
                _resolver.Resolve<IProgramFileExecutor>(),
            };
            return list;
        }
    }
}