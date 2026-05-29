using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class ShowFrameStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "showFrame"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "object.robot_preview");
            int jointIndex = step.GetInt("jointIndex", 0);
            bool visible = step.GetBool("visible", true);
            float duration = StepParameterReader.GetDuration(step, 0.5f);

            string error;
            if (!TrySetFrameVisible(context, objectId, jointIndex, visible, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null) context.LogError(error);
                yield break;
            }

            if (duration > 0f)
            {
                yield return context.WaitRespectingPause(duration);
            }
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            string objectId = step.GetString("objectId", "object.robot_preview");
            int jointIndex = step.GetInt("jointIndex", 0);
            bool visible = step.GetBool("visible", true);

            string error;
            if (!TrySetFrameVisible(context, objectId, jointIndex, visible, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied robot frame visibility without waiting for the step duration.");
            return true;
        }

        private static bool TrySetFrameVisible(
            RuntimeContext context,
            string objectId,
            int jointIndex,
            bool visible,
            out string error)
        {
            RobotLiteRig rig;
            if (!RobotLiteRigResolver.TryResolveRig(context, objectId, out rig, out error))
            {
                return false;
            }

            return rig.TrySetFrameVisible(jointIndex, visible, out error);
        }
    }
}
