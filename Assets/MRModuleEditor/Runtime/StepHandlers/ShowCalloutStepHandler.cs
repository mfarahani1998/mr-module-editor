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
            string text = step.GetString("text", "");
            string anchorId = step.GetString("anchorId", "anchor.head.default");
            Vector3 localOffset = StepParameterReader.GetVector3(step, "localOffset", new Vector3(0f, 0.55f, 0f));
            Vector3 localEuler = StepParameterReader.GetVector3(step, "localEuler", Vector3.zero);
            Vector3 localScale = StepParameterReader.GetVector3(step, "localScale", Vector3.one);
            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            float durationSeconds = StepParameterReader.GetDuration(step, 0f);

            RuntimeCalloutService callouts = context.Callouts;
            if (callouts == null)
            {
                callouts = Object.FindFirstObjectByType<RuntimeCalloutService>(FindObjectsInactive.Include);
            }

            if (callouts != null)
            {
                callouts.ShowCallout(context.Module, step, text, anchorId, localOffset, localEuler, localScale);
            }
            else if (context.SpatialUI != null)
            {
                context.SpatialUI.ShowText(context.Module, step, text);
            }
            else if (context.DisplayPanel != null)
            {
                context.DisplayPanel.ShowText(step.title, text);
            }
            else if (context.LogError != null)
            {
                context.LogError("showCallout needs RuntimeCalloutService, SpatialUIService, or RuntimeDisplayPanel.");
                yield break;
            }

            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);

                if (clearOnComplete)
                {
                    if (callouts != null)
                    {
                        callouts.ClearStep(step.id);
                    }
                    else if (context.SpatialUI != null)
                    {
                        context.SpatialUI.ClearStep(step.id);
                    }
                }
            }
        }
    }
}
