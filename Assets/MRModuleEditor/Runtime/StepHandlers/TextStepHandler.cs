using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class TextStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "text"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string text = step.GetString("text", "");
            float duration = StepParameterReader.GetDuration(step, 2f);

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, text);
            }

            if (context.SpatialUI != null && context.SpatialUI.CanShowText)
            {
                context.SpatialUI.ShowText(context.Module, step, text);
            }
            else if (context.SpatialTextPanel != null)
            {
                context.SpatialTextPanel.ShowText(context.Module, step, text);
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Text step: " + step.title);
            }

            yield return context.WaitRespectingPause(duration);

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ClearStep(step.id);
            }
            else if (context.SpatialTextPanel != null)
            {
                context.SpatialTextPanel.ClearIfShowingStep(step.id);
            }
        }
    }
}
