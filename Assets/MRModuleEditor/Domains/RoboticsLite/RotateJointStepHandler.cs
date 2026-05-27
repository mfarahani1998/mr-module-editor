using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.StepHandlers;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class RotateJointStepHandler : IStepHandler
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

            if (!rig.TrySetJointAngle(jointIndex, targetAngle, out error))
            {
                if (context.LogError != null) context.LogError(error);
            }
        }
    }
}
