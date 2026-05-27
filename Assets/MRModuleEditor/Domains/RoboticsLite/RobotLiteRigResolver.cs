using MRModuleEditor.Runtime;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public static class RobotLiteRigResolver
    {
        public static bool TryResolveRig(
            RuntimeContext context,
            string objectId,
            out RobotLiteRig rig,
            out string error)
        {
            rig = null;
            error = "";

            if (context == null)
            {
                error = "RuntimeContext is missing.";
                return false;
            }

            GameObject robotObject;
            if (!context.TryResolveObject(objectId, out robotObject, out error))
            {
                return false;
            }

            rig = robotObject.GetComponentInChildren<RobotLiteRig>(true);
            if (rig == null)
            {
                error = "Object '" + objectId + "' does not have a RobotLiteRig component.";
                return false;
            }

            return true;
        }
    }
}