using MRModuleEditor.Runtime;
using UnityEngine;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    [DisallowMultipleComponent]
    public class ProcedureItemMarker : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField]
        private string itemId = "";

        [SerializeField]
        private string displayName = "";

        [SerializeField]
        private bool completed;

        [SerializeField]
        private string lastSafetyPointId = "";

        [SerializeField]
        private string lastStatus = "";

        public string ItemId
        {
            get { return itemId; }
            set { itemId = value ?? ""; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName; }
            set { displayName = value ?? ""; }
        }

        public bool Completed
        {
            get { return completed; }
        }

        public string LastSafetyPointId
        {
            get { return lastSafetyPointId; }
        }

        public string LastStatus
        {
            get { return lastStatus; }
        }

        public void MarkShown(string newItemId)
        {
            if (!string.IsNullOrWhiteSpace(newItemId))
            {
                itemId = newItemId;
            }
        }

        public void MarkSafetyPoint(string safetyPointId, string status)
        {
            lastSafetyPointId = safetyPointId ?? "";
            lastStatus = status ?? "";
            completed = true;
        }

        public void ResetRuntimeState()
        {
            completed = false;
            lastSafetyPointId = "";
            lastStatus = "";
        }
    }
}
