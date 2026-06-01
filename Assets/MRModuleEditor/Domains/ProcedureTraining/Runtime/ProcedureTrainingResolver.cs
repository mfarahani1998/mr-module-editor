using MRModuleEditor.Runtime;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    public static class ProcedureTrainingResolver
    {
        public static bool TryResolveProcedureObject(
            RuntimeContext context,
            string objectId,
            out GameObject target,
            out ProcedureItemMarker marker,
            out string error)
        {
            target = null;
            marker = null;
            error = "";

            if (context == null)
            {
                error = "RuntimeContext is missing.";
                return false;
            }

            if (!context.TryResolveObject(objectId, out target, out error))
            {
                return false;
            }

            marker = target.GetComponentInChildren<ProcedureItemMarker>(true);
            if (marker == null)
            {
                marker = target.AddComponent<ProcedureItemMarker>();
            }

            if (marker == null)
            {
                error = "Object '" + objectId + "' could not create a ProcedureItemMarker.";
                return false;
            }

            return true;
        }
    }
}
