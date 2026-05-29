using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Preview;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class MoveObjectStepHandler : IStepHandler, IPreviewPreparationStepHandler
    {
        public string StepType
        {
            get { return "moveObject"; }
        }

        public IEnumerator Execute(ModuleStep step, RuntimeContext context)
        {
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            string objectId = step.GetString("objectId", "");

            GameObject target;
            string error;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                if (!context.IsCancellationRequested && context.LogError != null) context.LogError(error);
                yield break;
            }

            float duration = StepParameterReader.GetDuration(step, 1f);
            duration = Mathf.Max(0.01f, duration);

            Transform transform = target.transform;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 targetPosition;
            Quaternion targetRotation;
            ResolveTargetTransform(step, transform, out targetPosition, out targetRotation);

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
                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
        }

        public bool PrepareForPreview(
            ModuleStep step,
            RuntimeContext context,
            PreviewPreparationContext preparationContext,
            int stepIndex)
        {
            string error;
            if (!TryApplyFinalTransform(step, context, out error))
            {
                preparationContext.AddError(step, stepIndex, error);
                return false;
            }

            preparationContext.MarkApplied(step, stepIndex, "Applied final object transform without playing the movement animation.");
            return true;
        }

        private static bool TryApplyFinalTransform(ModuleStep step, RuntimeContext context, out string error)
        {
            error = "";
            if (context.IsCancellationRequested)
            {
                error = "The current module execution has been cancelled.";
                return false;
            }

            string objectId = step.GetString("objectId", "");

            GameObject target;
            if (!context.TryResolveObject(objectId, out target, out error))
            {
                return false;
            }

            Vector3 targetPosition;
            Quaternion targetRotation;
            ResolveTargetTransform(step, target.transform, out targetPosition, out targetRotation);
            target.transform.localPosition = targetPosition;
            target.transform.localRotation = targetRotation;
            return true;
        }

        private static void ResolveTargetTransform(
            ModuleStep step,
            Transform transform,
            out Vector3 targetPosition,
            out Quaternion targetRotation)
        {
            targetPosition = StepParameterReader.GetVector3(step, "position", transform.localPosition);
            Vector3 targetEuler = StepParameterReader.GetVector3(step, "rotationEuler", transform.localEulerAngles);
            targetRotation = Quaternion.Euler(targetEuler);

            if (StepParameterReader.GetBool(step, "isRelative", false))
            {
                targetPosition += StepParameterReader.GetVector3(step, "positionDelta", Vector3.zero);
                Vector3 rotationDeltaEuler = StepParameterReader.GetVector3(step, "rotationEulerDelta", Vector3.zero);
                targetRotation *= Quaternion.Euler(rotationDeltaEuler);
            }
        }
    }
}
