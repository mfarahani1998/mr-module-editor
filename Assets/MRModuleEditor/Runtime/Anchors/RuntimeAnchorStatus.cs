using System;
using UnityEngine;

namespace MRModuleEditor.Runtime.Anchors
{
    [Serializable]
    public class RuntimeAnchorStatus
    {
        public string anchorId = "";
        public string anchorType = "";
        public string targetObjectId = "";
        public string provider = "";
        public string fallbackAnchorId = "";
        public string state = "unknown";
        public bool calibrationRequired;
        public bool resolved;
        public bool usedFallback;
        public string message = "";
        public Vector3 position;

        public RuntimeAnchorStatus()
        {
        }

        public RuntimeAnchorStatus(
            string anchorId,
            string anchorType,
            string targetObjectId,
            bool resolved,
            string message,
            Vector3 position)
            : this(
                anchorId,
                anchorType,
                targetObjectId,
                "",
                "",
                "unknown",
                false,
                resolved,
                false,
                message,
                position)
        {
        }

        public RuntimeAnchorStatus(
            string anchorId,
            string anchorType,
            string targetObjectId,
            string provider,
            string fallbackAnchorId,
            string state,
            bool calibrationRequired,
            bool resolved,
            bool usedFallback,
            string message,
            Vector3 position)
        {
            this.anchorId = anchorId ?? "";
            this.anchorType = anchorType ?? "";
            this.targetObjectId = targetObjectId ?? "";
            this.provider = provider ?? "";
            this.fallbackAnchorId = fallbackAnchorId ?? "";
            this.state = state ?? "unknown";
            this.calibrationRequired = calibrationRequired;
            this.resolved = resolved;
            this.usedFallback = usedFallback;
            this.message = message ?? "";
            this.position = position;
        }
    }
}
