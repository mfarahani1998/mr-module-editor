using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.SceneBinding;
using UnityEngine;

namespace MRModuleEditor.Domains.RoboticsLite
{
    [RequireComponent(typeof(BindableObject))]
    public class RobotLiteRig : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField]
        private Transform[] jointTransforms = new Transform[0];

        [SerializeField]
        private Vector3[] jointAxes = new Vector3[0];

        [SerializeField]
        private RobotLiteFrameGizmo[] frameGizmos = new RobotLiteFrameGizmo[0];

        [SerializeField]
        private float frameAxisLength = 0.50f;

        [SerializeField]
        private float frameAxisWidth = 0.05f;

        [SerializeField]
        private bool warnIfJointsAreNotNested = true;

        private Quaternion[] initialLocalRotations = new Quaternion[0];
        private float[] currentAngles = new float[0];
        private bool initialized;
        private bool warnedAboutHierarchy;

        public int JointCount
        {
            get { return jointTransforms == null ? 0 : jointTransforms.Length; }
        }

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            WarnIfHierarchyLooksWrong();
        }

        public void ConfigureForTests(
            Transform[] joints,
            Vector3[] axes,
            RobotLiteFrameGizmo[] gizmos = null)
        {
            jointTransforms = joints ?? new Transform[0];
            jointAxes = axes ?? new Vector3[0];
            frameGizmos = gizmos ?? new RobotLiteFrameGizmo[0];
            initialized = false;
            Initialize();
        }

        public void Initialize()
        {
            int count = JointCount;
            initialLocalRotations = new Quaternion[count];
            currentAngles = new float[count];

            for (int i = 0; i < count; i++)
            {
                Transform joint = jointTransforms[i];
                initialLocalRotations[i] = joint == null
                    ? Quaternion.identity
                    : joint.localRotation;
                currentAngles[i] = 0f;
            }

            EnsureFrameArraySize(count);
            initialized = true;
        }

        public float GetJointAngle(int index)
        {
            EnsureInitialized();
            if (!IsValidJointIndex(index))
            {
                return 0f;
            }

            return currentAngles[index];
        }

        public Transform GetJointTransform(int index)
        {
            if (!IsValidJointIndex(index))
            {
                return null;
            }

            return jointTransforms[index];
        }

        public bool TrySetJointAngle(int index, float angleDegrees, out string error)
        {
            EnsureInitialized();
            error = "";

            if (!IsValidJointIndex(index))
            {
                error = "RobotLiteRig has no joint at index " + index + ".";
                return false;
            }

            Transform joint = jointTransforms[index];
            if (joint == null)
            {
                error = "RobotLiteRig joint " + index + " is not assigned.";
                return false;
            }

            Vector3 axis = GetJointAxis(index);
            joint.localRotation = initialLocalRotations[index] * Quaternion.AngleAxis(angleDegrees, axis);
            currentAngles[index] = angleDegrees;
            return true;
        }

        public bool TrySetFrameVisible(int index, bool visible, out string error)
        {
            EnsureInitialized();
            error = "";

            if (!IsValidJointIndex(index))
            {
                error = "RobotLiteRig has no frame/joint at index " + index + ".";
                return false;
            }

            RobotLiteFrameGizmo gizmo = GetOrCreateFrameGizmo(index);
            if (gizmo == null)
            {
                error = "Could not create frame gizmo for joint " + index + ".";
                return false;
            }

            gizmo.SetSize(frameAxisLength, frameAxisWidth);
            gizmo.SetVisible(visible);
            return true;
        }

        public void HideAllFrames()
        {
            EnsureInitialized();

            for (int i = 0; i < frameGizmos.Length; i++)
            {
                if (frameGizmos[i] != null)
                {
                    frameGizmos[i].SetVisible(false);
                }
            }
        }

        public bool ValidateKinematicHierarchy(out string warning)
        {
            warning = "";

            if (jointTransforms == null || jointTransforms.Length <= 1)
            {
                return true;
            }

            for (int i = 1; i < jointTransforms.Length; i++)
            {
                Transform parentJoint = jointTransforms[i - 1];
                Transform childJoint = jointTransforms[i];

                if (parentJoint == null || childJoint == null)
                {
                    continue;
                }

                if (!childJoint.IsChildOf(parentJoint))
                {
                    warning = "RobotLiteRig joints are not nested as a kinematic chain. "
                        + "Joint " + i + " ('" + childJoint.name + "') should be under joint "
                        + (i - 1) + " ('" + parentJoint.name + "') so parent rotations move child links.";
                    return false;
                }
            }

            return true;
        }

        private void WarnIfHierarchyLooksWrong()
        {
            if (!warnIfJointsAreNotNested || warnedAboutHierarchy)
            {
                return;
            }

            string warning;
            if (!ValidateKinematicHierarchy(out warning))
            {
                Debug.LogWarning(warning, this);
                warnedAboutHierarchy = true;
            }
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        public void ResetRuntimeState()
        {
            ResetRig();
        }

        public void ResetRig()
        {
            EnsureInitialized();

            for (int i = 0; i < JointCount; i++)
            {
                Transform joint = jointTransforms[i];
                if (joint != null)
                {
                    joint.localRotation = initialLocalRotations[i];
                }

                if (currentAngles != null && i < currentAngles.Length)
                {
                    currentAngles[i] = 0f;
                }
            }

            HideAllFrames();
        }

        private bool IsValidJointIndex(int index)
        {
            return jointTransforms != null && index >= 0 && index < jointTransforms.Length;
        }

        private Vector3 GetJointAxis(int index)
        {
            if (jointAxes == null || index < 0 || index >= jointAxes.Length)
            {
                return Vector3.up;
            }

            Vector3 axis = jointAxes[index];
            if (axis.sqrMagnitude < 0.0001f)
            {
                return Vector3.up;
            }

            return axis.normalized;
        }

        private RobotLiteFrameGizmo GetOrCreateFrameGizmo(int index)
        {
            EnsureFrameArraySize(JointCount);

            if (frameGizmos[index] != null)
            {
                frameGizmos[index].SetSize(frameAxisLength, frameAxisWidth);
                return frameGizmos[index];
            }

            Transform joint = jointTransforms[index];
            if (joint == null)
            {
                return null;
            }

            GameObject frameObject = new GameObject("FrameGizmo_Joint" + index);
            frameObject.transform.SetParent(joint, false);
            frameObject.transform.localPosition = Vector3.zero;
            frameObject.transform.localRotation = Quaternion.identity;
            frameObject.transform.localScale = Vector3.one;

            RobotLiteFrameGizmo gizmo = frameObject.AddComponent<RobotLiteFrameGizmo>();
            gizmo.SetSize(frameAxisLength, frameAxisWidth);
            frameGizmos[index] = gizmo;
            return gizmo;
        }

        private void EnsureFrameArraySize(int count)
        {
            if (frameGizmos != null && frameGizmos.Length == count)
            {
                return;
            }

            RobotLiteFrameGizmo[] next = new RobotLiteFrameGizmo[count];
            if (frameGizmos != null)
            {
                int copyCount = Mathf.Min(frameGizmos.Length, next.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    next[i] = frameGizmos[i];
                }
            }

            frameGizmos = next;
        }
    }
}
