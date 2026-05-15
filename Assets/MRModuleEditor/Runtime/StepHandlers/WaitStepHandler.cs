using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class WaitStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "wait"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            float duration = StepParameterReader.GetDuration(step, 1f);

            if (context.LogInfo != null)
            {
                context.LogInfo("Wait step for " + duration + " second(s).");
            }

            yield return context.WaitRespectingPause(duration);
        }
    }
}
