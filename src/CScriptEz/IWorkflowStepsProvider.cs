using System.Collections.Generic;
using CScriptEz.Steps;

namespace CScriptEz
{
    public interface IWorkflowStepsProvider
    {
        IList<IStepProcessor> GetSteps();
    }
}