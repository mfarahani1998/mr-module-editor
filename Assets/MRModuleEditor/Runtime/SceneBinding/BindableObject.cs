using UnityEngine;

namespace MRModuleEditor.Runtime.SceneBinding
{
    public class BindableObject : MonoBehaviour
    {
        [SerializeField]
        private string bindingKey = "";

        public string BindingKey
        {
            get { return bindingKey; }
            set { bindingKey = value; }
        }

        public GameObject BoundGameObject
        {
            get { return gameObject; }
        }
    }
}