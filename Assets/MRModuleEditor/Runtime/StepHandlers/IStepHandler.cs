using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public interface IStepHandler
    {
        string StepType { get; }

        /// <summary>
        /// Executes one module step. Long-running handlers must check
        /// context.IsCancellationRequested before mutating scene state and before
        /// committing a final target pose/value.
        /// </summary>
        IEnumerator Execute(ModuleStep step, RuntimeContext context);
    }
}
