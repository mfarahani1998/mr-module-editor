using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.ObjectState;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class HighlightObjectStepHandler : IStepHandler
    {
        public string StepType
        {
            get { return "highlightObject"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            string objectId = step.GetString("objectId", "");
            bool enabled = step.GetBool("enabled", true);
            string colorHex = step.GetString("colorHex", "#42A5FF");
            float pulseAmplitude = Mathf.Max(0f, step.GetFloat("pulseAmplitude", 0.08f));
            float pulseSeconds = Mathf.Max(0.05f, step.GetFloat("pulseSeconds", 0.8f));
            bool clearOnComplete = step.GetBool("clearOnComplete", false);
            float durationSeconds = StepParameterReader.GetDuration(step, 0f);

            GameObject target;
            string error;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                if (context.LogError != null)
                {
                    context.LogError("highlightObject could not resolve object '" + objectId + "': " + error);
                }
                yield break;
            }

            ObjectHighlightController controller = ObjectHighlightController.GetOrAdd(target);
            if (controller == null)
            {
                yield break;
            }

            if (!enabled)
            {
                controller.Clear();
                yield break;
            }

            controller.Apply(colorHex, pulseAmplitude, pulseSeconds);

            if (durationSeconds > 0f)
            {
                yield return context.WaitRespectingPause(durationSeconds);
                if (clearOnComplete)
                {
                    controller.Clear();
                }
            }
        }
    }
}
