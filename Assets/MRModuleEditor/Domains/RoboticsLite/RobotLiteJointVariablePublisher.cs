using System.Globalization;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Variables;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class RobotLiteJointVariablePublisher : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField]
        private RobotLiteRig rig;

        [SerializeField]
        private RuntimeVariableStore variableStore;

        [SerializeField]
        private string variableKey = "robot.joint3.angleText";

        [SerializeField]
        private int jointIndex = 2;

        [SerializeField]
        private string prefix = "Joint";

        [SerializeField]
        private float publishIntervalSeconds = 0.05f;

        private float nextPublishTime;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Update()
        {
            EnsureReferences();

            if (Time.time < nextPublishTime)
            {
                return;
            }

            nextPublishTime = Time.time + Mathf.Max(0.01f, publishIntervalSeconds);
            Publish();
        }

        public void ResetRuntimeState()
        {
            nextPublishTime = 0f;
            if (variableStore != null)
            {
                variableStore.SetString(variableKey, "");
            }
        }

        private void EnsureReferences()
        {
            if (rig == null)
            {
                rig = GetComponentInParent<RobotLiteRig>();
            }

            if (variableStore == null)
            {
                variableStore = FindFirstObjectByType<RuntimeVariableStore>(FindObjectsInactive.Include);
            }
        }

        private void Publish()
        {
            if (rig == null || variableStore == null || string.IsNullOrWhiteSpace(variableKey))
            {
                return;
            }

            float angle = rig.GetJointAngle(jointIndex);
            string text = prefix
                + " "
                + (jointIndex + 1).ToString(CultureInfo.InvariantCulture)
                + ": "
                + angle.ToString("0", CultureInfo.InvariantCulture)
                + "°";

            variableStore.SetString(variableKey, text);
        }
    }
}