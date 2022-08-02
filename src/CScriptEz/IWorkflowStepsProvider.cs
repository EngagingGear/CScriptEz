using System.Collections.Generic;
using CScriptEz.CommandProcessors;
using CScriptEz.Steps;

namespace CScriptEz
{
    public interface IWorkflowStepsProvider
    {
        IList<ICommandProcessor> GetCommandProcessingSteps();
        IList<IStepProcessor> GetScriptExecutionSteps();
    }
}