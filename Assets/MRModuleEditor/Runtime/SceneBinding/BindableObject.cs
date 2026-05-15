using UnityEngine;

namespace MRModuleEditor.Runtime.SceneBinding
{
    public class BindableObject : MonoBehaviour
    {
        [SerializeField]
        private string bindingKey = "";

        private bool hasRuntimeBaseline;
        private Transform baselineParent;
        private Vector3 baselineLocalPosition;
        private Quaternion baselineLocalRotation = Quaternion.identity;
        private Vector3 baselineLocalScale = Vector3.one;
        private bool baselineActiveSelf;

        public string BindingKey
        {
            get { return bindingKey; }
            set { bindingKey = value; }
        }

        public GameObject BoundGameObject
        {
            get { return gameObject; }
        }

        public void CaptureRuntimeBaseline()
        {
            Transform cachedTransform = transform;
            baselineParent = cachedTransform.parent;
            baselineLocalPosition = cachedTransform.localPosition;
            baselineLocalRotation = cachedTransform.localRotation;
            baselineLocalScale = cachedTransform.localScale;
            baselineActiveSelf = gameObject.activeSelf;
            hasRuntimeBaseline = true;
        }

        public void CaptureRuntimeBaselineIfNeeded()
        {
            if (!hasRuntimeBaseline)
            {
                CaptureRuntimeBaseline();
            }
        }

        public void ResetToRuntimeBaseline()
        {
            if (!hasRuntimeBaseline)
            {
                CaptureRuntimeBaseline();
            }

            Transform cachedTransform = transform;
            if (cachedTransform.parent != baselineParent)
            {
                cachedTransform.SetParent(baselineParent, false);
            }

            cachedTransform.localPosition = baselineLocalPosition;
            cachedTransform.localRotation = baselineLocalRotation;
            cachedTransform.localScale = baselineLocalScale;
            gameObject.SetActive(baselineActiveSelf);
        }
    }
}
