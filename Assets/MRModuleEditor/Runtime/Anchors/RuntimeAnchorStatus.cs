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
        public bool resolved;
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
        {
            this.anchorId = anchorId ?? "";
            this.anchorType = anchorType ?? "";
            this.targetObjectId = targetObjectId ?? "";
            this.resolved = resolved;
            this.message = message ?? "";
            this.position = position;
        }
    }
}
