using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public interface IStepHandler
    {
        string StepType { get; }
        IEnumerator Execute(ModuleStep step, RuntimeContext context);
    }
}