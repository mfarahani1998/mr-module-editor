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

            if (choices == null || choices.Length == 0)
            {
                if (context.LogError != null)
                {
                    context.LogError("MCQ step '" + step.id + "' has no answer choices.");
                }
                yield break;
            }

            if (correctIndex < 0 || correctIndex >= choices.Length)
            {
                if (context.LogError != null)
                {
                    context.LogError("MCQ step '" + step.id + "' has invalid correctIndex " + correctIndex + ".");
                }
                yield break;
            }

            bool useSpatialUI = context.SpatialUI != null && context.SpatialUI.CanShowMCQ;
            bool useDirectSpatialMcq = !useSpatialUI && context.SpatialMCQPanel != null;
            bool hasSpatialMcq = useSpatialUI || useDirectSpatialMcq;
            bool hasDebugMcq = context.DisplayPanel != null;

            if (!hasSpatialMcq && !hasDebugMcq)
            {
                if (context.LogError != null)
                {
                    context.LogError("MCQ needs either SpatialMCQPanel or RuntimeDisplayPanel.");
                }
                yield break;
            }

            if (useSpatialUI)
            {
                context.SpatialUI.ShowMCQ(context.Module, step, question, choices, correctIndex);
            }
            else
            {
                if (context.SpatialTextPanel != null)
                {
                    context.SpatialTextPanel.Clear();
                }

                if (useDirectSpatialMcq)
                {
                    context.SpatialMCQPanel.ShowMCQ(context.Module, step, question, choices, correctIndex);
                }
            }

            if (hasDebugMcq)
            {
                context.DisplayPanel.ShowMCQ(step.title, question, choices, correctIndex);
            }

            int selectedAnswer = -1;

            while (selectedAnswer < 0)
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

                if (useSpatialUI && context.SpatialUI.HasMCQAnswer)
                {
                    selectedAnswer = context.SpatialUI.SelectedMCQAnswer;
                    if (context.LogInfo != null)
                    {
                        context.LogInfo("MCQ answer selected from spatial panel: " + (selectedAnswer + 1));
                    }
                    break;
                }

                if (useDirectSpatialMcq && context.SpatialMCQPanel.HasAnswer)
                {
                    selectedAnswer = context.SpatialMCQPanel.SelectedAnswer;
                    if (context.LogInfo != null)
                    {
                        context.LogInfo("MCQ answer selected from spatial panel: " + (selectedAnswer + 1));
                    }
                    break;
                }

                if (hasDebugMcq && context.DisplayPanel.HasMcqAnswer)
                {
                    selectedAnswer = context.DisplayPanel.SelectedMcqAnswer;
                    if (context.LogInfo != null)
                    {
                        context.LogInfo("MCQ answer selected from debug panel: " + (selectedAnswer + 1));
                    }
                    break;
                }

                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            bool correct = selectedAnswer == correctIndex;
            string feedback = correct ? "Correct." : "Not quite.";

            if (useSpatialUI)
            {
                context.SpatialUI.ShowMCQFeedback(feedback);
            }
            else if (useDirectSpatialMcq)
            {
                context.SpatialMCQPanel.ShowFeedback(feedback);
            }

            if (hasDebugMcq)
            {
                context.DisplayPanel.ShowFeedback(feedback);
            }

            yield return context.WaitRespectingPause(1.5f);

            if (useSpatialUI)
            {
                context.SpatialUI.ClearStep(step.id);
            }
            else if (useDirectSpatialMcq)
            {
                context.SpatialMCQPanel.ClearIfShowingStep(step.id);
            }
        }
    }
}