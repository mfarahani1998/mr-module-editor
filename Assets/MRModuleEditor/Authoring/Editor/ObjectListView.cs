using MRModuleEditor.Core.Models;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class ObjectListView
    {
        public static bool Draw(ModuleDocument document)
        {
            if (document == null)
            {
                return false;
            }

            EditorModuleDataUtility.EnsureLists(document);
            bool changed = false;

            EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);

            for (int i = 0; i < document.objects.Count; i++)
            {
                ModuleObject moduleObject = document.objects[i];
                if (moduleObject == null)
                {
                    moduleObject = new ModuleObject();
                    document.objects[i] = moduleObject;
                    changed = true;
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();

                moduleObject.id = EditorGUILayout.TextField("ID", moduleObject.id);
                moduleObject.label = EditorGUILayout.TextField("Label", moduleObject.label);
                moduleObject.bindingKey = EditorGUILayout.TextField("Binding Key", moduleObject.bindingKey);

                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                }

                EditorGUILayout.HelpBox(
                    "bindingKey connects this module object to a scene BindableObject. " +
                    "For the sample robot, the scene binding key is RobotPreview.",
                    MessageType.None);

                bool remove = GUILayout.Button("Remove Object");
                EditorGUILayout.EndVertical();

                if (remove)
                {
                    document.objects.RemoveAt(i);
                    return true;
                }
            }

            if (GUILayout.Button("Add Object"))
            {
                document.objects.Add(new ModuleObject
                {
                    id = EditorModuleDataUtility.MakeUniqueId(document, "object.new"),
                    label = "New Object",
                    bindingKey = "NewBindingKey"
                });
                return true;
            }

            return changed;
        }
    }
}