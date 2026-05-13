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
            string text = step.GetString("text", "");
            float duration = StepParameterReader.GetDuration(step, 2f);

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, text);
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Text step: " + step.title);
            }

            yield return context.WaitRespectingPause(duration);
        }
    }
}