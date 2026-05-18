using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class FaceCamera : MonoBehaviour
    {
        [SerializeField]
        private bool keepUpright = true;

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 direction = transform.position - camera.transform.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (keepUpright)
            {
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    return;
                }
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}