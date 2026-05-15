using System.Collections;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public class MoveObjectStepHandler : IStepHandler
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

            Vector3 targetPosition = StepParameterReader.GetVector3(step, "position", startPosition);
            Vector3 targetEuler = StepParameterReader.GetVector3(step, "rotationEuler", transform.localEulerAngles);
            Quaternion targetRotation = Quaternion.Euler(targetEuler);

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
    }
}
