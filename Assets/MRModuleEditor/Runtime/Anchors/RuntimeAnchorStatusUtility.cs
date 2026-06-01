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
                    "",
                    "",
                    AnchorCalibrationStatuses.Lost,
                    false,
                    false,
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
                        "",
                        "",
                        AnchorCalibrationStatuses.Lost,
                        false,
                        false,
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
                        AnchorProviderIds.Normalize(anchor.provider, anchor.type),
                        anchor.fallbackAnchorId,
                        AnchorCalibrationStatuses.Lost,
                        anchor.calibrationRequired,
                        false,
                        false,
                        "AnchorResolver is missing from the runtime scene.",
                        Vector3.zero));
                    continue;
                }

                AnchorResolveResult resolveResult;
                bool resolved = resolver.TryResolveAnchorWithStatus(module, anchor.id, out resolveResult);
                result.Add(new RuntimeAnchorStatus(
                    anchor.id,
                    anchor.type,
                    anchor.targetObjectId,
                    resolveResult == null ? AnchorProviderIds.Normalize(anchor.provider, anchor.type) : resolveResult.provider,
                    anchor.fallbackAnchorId,
                    resolveResult == null ? AnchorCalibrationStatuses.Lost : resolveResult.state,
                    anchor.calibrationRequired,
                    resolved,
                    resolveResult != null && resolveResult.usedFallback,
                    resolveResult == null ? "Anchor resolution failed." : resolveResult.message,
                    resolved && resolveResult != null ? resolveResult.pose.position : Vector3.zero));
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

        public static int CountResolvedWithoutFallback(List<RuntimeAnchorStatus> statuses)
        {
            if (statuses == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < statuses.Count; i++)
            {
                RuntimeAnchorStatus status = statuses[i];
                if (status != null && status.resolved && !status.usedFallback)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
