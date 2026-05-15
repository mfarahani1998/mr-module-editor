using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class MCQStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "mcq"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string question = step.GetString("question", "");
            string[] choices = StepParameterReader.GetStringArray(step, "choices");
            int correctIndex = step.GetInt("correctIndex", -1);

            if (context.DisplayPanel == null)
            {
                if (context.LogError != null) context.LogError("RuntimeDisplayPanel is missing.");
                yield break;
            }

            if (context.SpatialTextPanel != null)
            {
                context.SpatialTextPanel.Clear();
            }

            context.DisplayPanel.ShowMCQ(step.title, question, choices, correctIndex);

            while (!context.DisplayPanel.HasMcqAnswer)
            {
                if (context.IsCancellationRequested)
                {
                    yield break;
                }

                if (context.IsPaused != null && context.IsPaused())
                {
                    yield return null;
                    continue;
                }

                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            bool correct = context.DisplayPanel.SelectedMcqAnswer == correctIndex;
            context.DisplayPanel.ShowFeedback(correct ? "Correct." : "Not quite.");

            yield return context.WaitRespectingPause(1.5f);
        }
    }
}
