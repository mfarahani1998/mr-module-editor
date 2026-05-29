using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.UI;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class ShowCalloutStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "showCallout"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string text = step.GetString("text", "");
            Vector3 localOffset = StepParameterReader.GetVector3(step, "localOffset", new Vector3(0f, 0.55f, 0f));
            Vector3 localEuler = StepParameterReader.GetVector3(step, "localEuler", Vector3.zero);
            Vector3 localScale = StepParameterReader.GetVector3(step, "localScale", Vector3.one);
            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            float durationSeconds = StepParameterReader.GetDuration(step, 0f);

            SpatialUIService spatialUI = context.SpatialUI;
            if (spatialUI == null)
            {
                spatialUI = Object.FindFirstObjectByType<SpatialUIService>(FindObjectsInactive.Include);
            }

            bool usedSpatialTextPanel = spatialUI != null && spatialUI.CanShowText;
            bool usedDisplayPanel = false;

            if (usedSpatialTextPanel)
            {
                spatialUI.ShowText(context.Module, step, text, localOffset, localEuler, localScale);
            }
            else if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, text);
                usedDisplayPanel = true;
            }
            else
            {
                if (context.LogError != null)
                {
                    context.LogError("showCallout needs SpatialUIService with a SpatialTextPanel, or RuntimeDisplayPanel fallback.");
                }

                yield break;
            }

            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);

                if (context.IsCancellationRequested)
                {
                    yield break;
                }

                if (clearOnComplete)
                {
                    if (usedSpatialTextPanel)
                    {
                        spatialUI.ClearStep(step.id);
                    }
                    else if (usedDisplayPanel)
                    {
                        context.DisplayPanel.Clear();
                    }
                }
            }
        }
    }
}
