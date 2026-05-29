using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.UI;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ShowCalloutStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "showCallout"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            bool usedSpatialTextPanel;
            bool usedDisplayPanel;
            SpatialUIService spatialUI;
            string error;
            if (!TryShowCallout(step, context, out spatialUI, out usedSpatialTextPanel, out usedDisplayPanel, out error))
            {
                if (context.LogError != null)
                {
                    context.LogError(error);
                }

                yield break;
            }

            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            float durationSeconds = StepParameterReader.GetDuration(step, 0f);

            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);

                if (context.IsCancellationRequested)
                {
                    yield break;
                }

                if (clearOnComplete)
                {
                    ClearCallout(step, context, spatialUI, usedSpatialTextPanel, usedDisplayPanel);
                }
            }
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            bool usedSpatialTextPanel;
            bool usedDisplayPanel;
            SpatialUIService spatialUI;
            string error;
            if (!TryShowCallout(step, context, out spatialUI, out usedSpatialTextPanel, out usedDisplayPanel, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            float durationSeconds = StepParameterReader.GetDuration(step, 0f);
            if (durationSeconds > 0f && clearOnComplete)
            {
                ClearCallout(step, context, spatialUI, usedSpatialTextPanel, usedDisplayPanel);
                preparationContext.MarkApplied(step, stepIndex, "Applied final callout state: cleared after its configured duration.");
                return true;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied persistent callout UI state without waiting for the step duration.");
            return true;
        }

        private static bool TryShowCallout(
            ModuleStep step,
            RuntimeContext context,
            out SpatialUIService spatialUI,
            out bool usedSpatialTextPanel,
            out bool usedDisplayPanel,
            out string error)
        {
            spatialUI = context.SpatialUI;
            usedSpatialTextPanel = false;
            usedDisplayPanel = false;
            error = "";

            if (context.IsCancellationRequested)
            {
                error = "The current module execution has been cancelled.";
                return false;
            }

            string text = step.GetString("text", "");
            Vector3 localOffset = StepParameterReader.GetVector3(step, "localOffset", new Vector3(0f, 0.55f, 0f));
            Vector3 localEuler = StepParameterReader.GetVector3(step, "localEuler", Vector3.zero);
            Vector3 localScale = StepParameterReader.GetVector3(step, "localScale", Vector3.one);

            if (spatialUI == null)
            {
                spatialUI = Object.FindFirstObjectByType<SpatialUIService>(FindObjectsInactive.Include);
            }

            usedSpatialTextPanel = spatialUI != null && spatialUI.CanShowText;

            if (usedSpatialTextPanel)
            {
                spatialUI.ShowText(context.Module, step, text, localOffset, localEuler, localScale);
                return true;
            }

            if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, text);
                usedDisplayPanel = true;
                return true;
            }

            error = "showCallout needs SpatialUIService with a SpatialTextPanel, or RuntimeDisplayPanel fallback.";
            return false;
        }

        private static void ClearCallout(
            ModuleStep step,
            RuntimeContext context,
            SpatialUIService spatialUI,
            bool usedSpatialTextPanel,
            bool usedDisplayPanel)
        {
            if (usedSpatialTextPanel && spatialUI != null)
            {
                spatialUI.ClearStep(step.id);
            }
            else if (usedDisplayPanel && context.DisplayPanel != null)
            {
                context.DisplayPanel.Clear();
            }
        }
    }
}
