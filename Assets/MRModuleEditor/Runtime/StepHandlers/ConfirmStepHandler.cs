using System.Collections;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ConfirmStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "confirm"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string message = step.GetString("message", "Continue when you are ready.");
            string buttonLabel = step.GetString("buttonLabel", "Continue");
            float autoContinueAfterSeconds = step.GetFloat("autoContinueAfterSeconds", 0f);

            bool hasDebugConfirm = context.DisplayPanel != null && context.DisplayPanel.ShowDebugOverlay;
            bool hasSpatialConfirm = context.SpatialUI != null && context.SpatialUI.CanShowConfirm;

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowConfirm(step.title, message, buttonLabel);
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ShowConfirm(context.Module, step, message, buttonLabel);
            }

            if (context.LogInfo != null)
            {
                context.LogInfo("Confirm step: " + step.title);
            }

            if (!hasDebugConfirm && !hasSpatialConfirm && autoContinueAfterSeconds <= 0f)
            {
                autoContinueAfterSeconds = StepParameterReader.GetDuration(step, 1f);
            }

            float elapsed = 0f;
            while (true)
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

                if (hasDebugConfirm && context.DisplayPanel.HasConfirmation)
                {
                    break;
                }

                if (hasSpatialConfirm && context.SpatialUI.HasConfirmation)
                {
                    break;
                }

                if (autoContinueAfterSeconds > 0f)
                {
                    elapsed += UnityEngine.Time.deltaTime;
                    if (elapsed >= autoContinueAfterSeconds)
                    {
                        break;
                    }
                }

                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (context.SpatialUI != null)
            {
                context.SpatialUI.ClearStep(step.id);
            }
        }
    }
}
