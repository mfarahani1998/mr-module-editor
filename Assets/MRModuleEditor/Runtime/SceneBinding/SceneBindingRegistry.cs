using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using UnityEngine;

namespace MRModuleEditor.Runtime.SceneBinding
{
    public class SceneBindingRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, BindableObject> byBindingKey =
            new Dictionary<string, BindableObject>();

        private void Awake()
        {
            Rebuild();
        }

        [ContextMenu("Rebuild Binding Registry")]
        public void Rebuild()
        {
            byBindingKey.Clear();

            BindableObject[] bindables = FindObjectsByType<BindableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bindables.Length; i++)
            {
                BindableObject bindable = bindables[i];
                if (bindable == null || string.IsNullOrWhiteSpace(bindable.BindingKey))
                {
                    continue;
                }

                bindable.CaptureRuntimeBaselineIfNeeded();

                if (byBindingKey.ContainsKey(bindable.BindingKey))
                {
                    Debug.LogWarning("Duplicate bindingKey found in scene: " + bindable.BindingKey);
                    continue;
                }

                byBindingKey.Add(bindable.BindingKey, bindable);
            }

            Debug.Log("SceneBindingRegistry registered " + byBindingKey.Count + " bindable object(s).");
        }

        public void CaptureRuntimeBaseline()
        {
            foreach (KeyValuePair<string, BindableObject> pair in byBindingKey)
            {
                if (pair.Value != null)
                {
                    pair.Value.CaptureRuntimeBaseline();
                }
            }
        }

        public void ResetBindableObjectsToRuntimeBaseline()
        {
            foreach (KeyValuePair<string, BindableObject> pair in byBindingKey)
            {
                if (pair.Value != null)
                {
                    pair.Value.ResetToRuntimeBaseline();
                }
            }
        }

        public bool TryGetByBindingKey(string bindingKey, out GameObject result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(bindingKey))
            {
                return false;
            }

            BindableObject bindable;
            if (!byBindingKey.TryGetValue(bindingKey, out bindable) || bindable == null)
            {
                return false;
            }

            result = bindable.BoundGameObject;
            return result != null;
        }

        public bool TryGetObjectByModuleObjectId(
            ModuleDocument module,
            string objectId,
            out GameObject result,
            out string error)
        {
            result = null;
            error = "";

            if (module == null)
            {
                error = "ModuleDocument is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(objectId))
            {
                error = "objectId is empty.";
                return false;
            }

            ModuleObject moduleObject = null;
            for (int i = 0; i < module.objects.Count; i++)
            {
                if (module.objects[i] != null && module.objects[i].id == objectId)
                {
                    moduleObject = module.objects[i];
                    break;
                }
            }

            if (moduleObject == null)
            {
                error = "No ModuleObject with id '" + objectId + "' exists in the module.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(moduleObject.bindingKey))
            {
                error = "ModuleObject '" + objectId + "' has an empty bindingKey.";
                return false;
            }

            if (!TryGetByBindingKey(moduleObject.bindingKey, out result))
            {
                error = "No scene BindableObject with bindingKey '" + moduleObject.bindingKey + "' was found.";
                return false;
            }

            return true;
        }
    }
}
