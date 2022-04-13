using System.Collections.Generic;
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

        public IList<IStepProcessor> GetSteps()
        {
            var list = new List<IStepProcessor>
            {
                _resolver.Resolve<IScriptFileReader>(),
                _resolver.Resolve<IPreprocessor>(),
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