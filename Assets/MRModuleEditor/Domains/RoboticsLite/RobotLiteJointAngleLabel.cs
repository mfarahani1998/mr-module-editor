using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.UI;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class RobotLiteJointAngleLabel : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField]
        private RobotLiteRig rig;

        [SerializeField]
        private int jointIndex = 2;

        [SerializeField]
        private Vector3 localOffset = new Vector3(0f, 0.35f, 0f);

        [SerializeField]
        private string prefix = "Joint";

        private TextMesh label;

        private void Awake()
        {
            if (rig == null)
            {
                rig = GetComponentInParent<RobotLiteRig>();
            }

            GameObject labelObject = new GameObject("Joint Angle Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localOffset;

            label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.035f;
            label.color = Color.white;

            labelObject.AddComponent<FaceCamera>();
        }

        private void Update()
        {
            if (label == null || rig == null)
            {
                return;
            }

            float angle = rig.GetJointAngle(jointIndex);
            label.text = prefix + " " + (jointIndex + 1) + ": " + angle.ToString("0") + "°";
        }

        public void ResetRuntimeState()
        {
            if (label != null)
            {
                label.text = "";
            }
        }
    }
}