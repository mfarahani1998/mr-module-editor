using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    public sealed class AnchorResolveResult
    {
        public string requestedAnchorId = "";
        public string effectiveAnchorId = "";
        public string provider = "";
        public string state = "unknown";
        public bool resolved;
        public bool usedFallback;
        public Pose pose = new Pose(Vector3.zero, Quaternion.identity);
        public string message = "";

        public static AnchorResolveResult Failed(string requestedAnchorId, string message)
        {
            return new AnchorResolveResult
            {
                requestedAnchorId = requestedAnchorId ?? "",
                effectiveAnchorId = requestedAnchorId ?? "",
                resolved = false,
                state = "lost",
                message = message ?? ""
            };
        }
    }
}
