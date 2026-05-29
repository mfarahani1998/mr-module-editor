using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Preview;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class RotateJointStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "rotateJoint"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "object.robot_preview");
            int jointIndex = step.GetInt("jointIndex", 0);
            float targetAngle = step.GetFloat("angleDegrees", 0f);
            bool showFrame = step.GetBool("showFrame", false);
            float duration = Mathf.Max(0.01f, StepParameterReader.GetDuration(step, 1f));

            RobotLiteRig rig;
            string error;
            if (!RobotLiteRigResolver.TryResolveRig(context, objectId, out rig, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null) context.LogError(error);
                yield break;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (showFrame)
            {
                rig.TrySetFrameVisible(jointIndex, true, out error);
            }

            float startAngle = rig.GetJointAngle(jointIndex);
            float elapsed = 0f;

            while (elapsed < duration)
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

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.Lerp(startAngle, targetAngle, t);

                if (!rig.TrySetJointAngle(jointIndex, angle, out error))
                {
                    if (context.LogError != null) context.LogError(error);
                    yield break;
                }

                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            if (!TryApplyFinalJointState(rig, jointIndex, targetAngle, showFrame, out error))
            {
                if (context.LogError != null) context.LogError(error);
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
            float targetAngle = step.GetFloat("angleDegrees", 0f);
            bool showFrame = step.GetBool("showFrame", false);

            RobotLiteRig rig;
            string error;
            if (!RobotLiteRigResolver.TryResolveRig(context, objectId, out rig, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            if (!TryApplyFinalJointState(rig, jointIndex, targetAngle, showFrame, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied final robot joint angle without playing the rotation animation.");
            return true;
        }

        private static bool TryApplyFinalJointState(
            RobotLiteRig rig,
            int jointIndex,
            float targetAngle,
            bool showFrame,
            out string error)
        {
            if (showFrame && !rig.TrySetFrameVisible(jointIndex, true, out error))
            {
                return false;
            }

            return rig.TrySetJointAngle(jointIndex, targetAngle, out error);
        }
    }
}
