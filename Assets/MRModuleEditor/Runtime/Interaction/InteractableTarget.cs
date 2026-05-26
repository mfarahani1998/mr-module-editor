using UnityEngine;

namespace MRModuleEditor.Runtime.Interaction
{
    public class InteractableTarget : MonoBehaviour
    {
        [SerializeField]
        private string groupId = "";

        [SerializeField]
        private string targetId = "";

        [SerializeField]
        private int intPayload = -1;

        private InteractionContext registeredContext;

        public string GroupId
        {
            get { return groupId; }
        }

        public string TargetId
        {
            get { return targetId; }
        }

        public int IntPayload
        {
            get { return intPayload; }
        }

        public void Configure(string newGroupId, string newTargetId, int newIntPayload)
        {
            groupId = newGroupId ?? "";
            targetId = newTargetId ?? "";
            intPayload = newIntPayload;
        }

        internal void MarkRegistered(InteractionContext context)
        {
            registeredContext = context;
        }

        internal void MarkUnregistered(InteractionContext context)
        {
            if (registeredContext == context)
            {
                registeredContext = null;
            }
        }

        private void OnDisable()
        {
            if (registeredContext != null)
            {
                registeredContext.UnregisterTarget(this);
            }
        }

        private void OnDestroy()
        {
            if (registeredContext != null)
            {
                registeredContext.UnregisterTarget(this);
            }
        }
    }
}