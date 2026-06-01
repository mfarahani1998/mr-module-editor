using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public static class RuntimeAnchorStatusUtility
    {
        public static List<RuntimeAnchorStatus> Collect(ModuleDocument module, AnchorResolver resolver)
        {
            List<RuntimeAnchorStatus> result = new List<RuntimeAnchorStatus>();
            if (module == null)
            {
                return result;
            }

            if (module.anchors == null || module.anchors.Count == 0)
            {
                result.Add(new RuntimeAnchorStatus(
                    "",
                    "",
                    "",
                    false,
                    "The loaded module has no anchors.",
                    Vector3.zero));
                return result;
            }

            for (int i = 0; i < module.anchors.Count; i++)
            {
                AnchorDefinition anchor = module.anchors[i];
                if (anchor == null)
                {
                    result.Add(new RuntimeAnchorStatus(
                        "<null>",
                        "",
                        "",
                        false,
                        "Anchor entry is null.",
                        Vector3.zero));
                    continue;
                }

                if (resolver == null)
                {
                    result.Add(new RuntimeAnchorStatus(
                        anchor.id,
                        anchor.type,
                        anchor.targetObjectId,
                        false,
                        "AnchorResolver is missing from the runtime scene.",
                        Vector3.zero));
                    continue;
                }

                Pose pose;
                string error;
                bool resolved = resolver.TryResolveAnchor(module, anchor.id, out pose, out error);
                result.Add(new RuntimeAnchorStatus(
                    anchor.id,
                    anchor.type,
                    anchor.targetObjectId,
                    resolved,
                    resolved ? "Resolved" : error,
                    resolved ? pose.position : Vector3.zero));
            }

            return result;
        }

        public static int CountResolved(List<RuntimeAnchorStatus> statuses)
        {
            if (statuses == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < statuses.Count; i++)
            {
                RuntimeAnchorStatus status = statuses[i];
                if (status != null && status.resolved)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
