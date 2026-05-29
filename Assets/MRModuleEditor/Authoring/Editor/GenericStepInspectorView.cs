using System.Globalization;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.StepTypes;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class GenericStepInspectorView
    {
        public static void DrawParameters(ModuleDocument document, ModuleStep step, StepTypeDefinition definition)
        {
            if (step == null || definition == null)
            {
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new System.Collections.Generic.Dictionary<string, JToken>();
            }

            StepParameterDefinition[] parameters = definition.Parameters;
            if (parameters == null || parameters.Length == 0)
            {
                EditorGUILayout.HelpBox("This step type uses Duration Seconds only. No extra parameters are required.", MessageType.Info);
                return;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                StepParameterDefinition parameter = parameters[i];
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key))
                {
                    continue;
                }

                if (!IsVisible(step, parameter))
                {
                    continue;
                }

                DrawParameter(document, step, parameter);
            }
        }

        private static bool IsVisible(ModuleStep step, StepParameterDefinition parameter)
        {
            if (!parameter.HasVisibilityCondition)
            {
                return true;
            }

            return step.GetBool(parameter.VisibleWhenParameterKey, !parameter.VisibleWhenBoolValue) == parameter.VisibleWhenBoolValue;
        }

        private static void DrawParameter(ModuleDocument document, ModuleStep step, StepParameterDefinition parameter)
        {
            switch (parameter.Kind)
            {
                case StepParameterKind.String:
                    DrawString(step, parameter);
                    break;
                case StepParameterKind.MultilineString:
                    DrawMultilineString(step, parameter);
                    break;
                case StepParameterKind.Int:
                    DrawInt(step, parameter.Key, parameter.DisplayName, ReadDefaultInt(parameter, 0));
                    break;
                case StepParameterKind.Float:
                    DrawFloat(step, parameter.Key, parameter.DisplayName, ReadDefaultFloat(parameter, 0f));
                    break;
                case StepParameterKind.Bool:
                    DrawBool(step, parameter.Key, parameter.DisplayName, ReadDefaultBool(parameter, false));
                    break;
                case StepParameterKind.Vector3:
                    DrawVector3(step, parameter);
                    break;
                case StepParameterKind.StringArray:
                    DrawStringArray(step, parameter.Key, parameter.DisplayName);
                    break;
                case StepParameterKind.AssetId:
                    EditorIdDropdowns.DrawAssetIdDropdown(document, step, parameter.Key, parameter.DisplayName, parameter.ExpectedAssetType);
                    break;
                case StepParameterKind.ObjectId:
                    EditorIdDropdowns.DrawObjectIdDropdown(document, step, parameter.Key, parameter.DisplayName);
                    break;
                case StepParameterKind.AnchorId:
                    EditorIdDropdowns.DrawAnchorIdDropdown(document, step, parameter.Key, parameter.DisplayName, allowNone: !parameter.Required);
                    RemoveParameterIfBlank(step, parameter.Key);
                    break;
                case StepParameterKind.StepId:
                    EditorIdDropdowns.DrawStepIdDropdown(document, step, parameter.Key, parameter.DisplayName, allowNone: !parameter.Required);
                    RemoveParameterIfBlank(step, parameter.Key);
                    break;
                case StepParameterKind.Choice:
                    DrawChoice(step, parameter);
                    break;
                default:
                    EditorGUILayout.HelpBox("Unsupported parameter kind for " + parameter.Key + ": " + parameter.Kind, MessageType.Warning);
                    break;
            }
        }

        private static void DrawString(ModuleStep step, StepParameterDefinition parameter)
        {
            string next = EditorGUILayout.TextField(
                parameter.DisplayName,
                step.GetString(parameter.Key, ReadDefaultString(parameter, "")));
            step.parameters[parameter.Key] = JToken.FromObject(next);
        }

        private static void DrawMultilineString(ModuleStep step, StepParameterDefinition parameter)
        {
            EditorGUILayout.LabelField(parameter.DisplayName);
            string next = EditorGUILayout.TextArea(
                step.GetString(parameter.Key, ReadDefaultString(parameter, "")),
                GUILayout.MinHeight(60));
            step.parameters[parameter.Key] = JToken.FromObject(next);
        }

        private static void DrawBool(ModuleStep step, string key, string label, bool fallback)
        {
            bool next = EditorGUILayout.Toggle(label, step.GetBool(key, fallback));
            step.parameters[key] = JToken.FromObject(next);
        }

        private static void DrawInt(ModuleStep step, string key, string label, int fallback)
        {
            int next = EditorGUILayout.IntField(label, step.GetInt(key, fallback));
            step.parameters[key] = JToken.FromObject(next);
        }

        private static void DrawFloat(ModuleStep step, string key, string label, float fallback)
        {
            float next = EditorGUILayout.FloatField(label, step.GetFloat(key, fallback));
            step.parameters[key] = JToken.FromObject(next);
        }

        private static void DrawVector3(ModuleStep step, StepParameterDefinition parameter)
        {
            Vector3 current = ReadVector3(
                step.GetToken(parameter.Key),
                ReadDefaultVector3(parameter, Vector3.zero));
            Vector3 next = EditorGUILayout.Vector3Field(parameter.DisplayName, current);
            step.parameters[parameter.Key] = MakeVector(next);
        }

        private static void DrawStringArray(ModuleStep step, string key, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            JArray array = step.GetToken(key) as JArray;
            if (array == null)
            {
                array = new JArray();
            }

            int nextCount = Mathf.Max(0, EditorGUILayout.IntField("Item Count", array.Count));
            while (array.Count < nextCount) array.Add("");
            while (array.Count > nextCount) array.RemoveAt(array.Count - 1);

            for (int i = 0; i < array.Count; i++)
            {
                string value = array[i] == null ? "" : array[i].ToString();
                array[i] = EditorGUILayout.TextField("Item " + i, value);
            }

            step.parameters[key] = array;
        }

        private static void DrawChoice(ModuleStep step, StepParameterDefinition parameter)
        {
            string[] choices = parameter.Choices ?? new string[0];
            if (choices.Length == 0)
            {
                DrawString(step, parameter);
                return;
            }

            string currentValue = step.GetString(parameter.Key, ReadDefaultString(parameter, choices[0]));
            int currentIndex = System.Array.IndexOf(choices, currentValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = EditorGUILayout.Popup(parameter.DisplayName, currentIndex, choices);
            step.parameters[parameter.Key] = JToken.FromObject(choices[nextIndex]);
        }

        private static Vector3 ReadVector3(JToken token, Vector3 fallback)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }

            return new Vector3(
                ReadFloat(token["x"], fallback.x),
                ReadFloat(token["y"], fallback.y),
                ReadFloat(token["z"], fallback.z));
        }

        private static string ReadDefaultString(StepParameterDefinition parameter, string fallback)
        {
            if (parameter == null || parameter.DefaultValue == null)
            {
                return fallback;
            }

            return parameter.DefaultValue.Type == JTokenType.String
                ? parameter.DefaultValue.Value<string>()
                : parameter.DefaultValue.ToString();
        }

        private static Vector3 ReadDefaultVector3(StepParameterDefinition parameter, Vector3 fallback)
        {
            if (parameter == null || parameter.DefaultValue == null)
            {
                return fallback;
            }

            return ReadVector3(parameter.DefaultValue, fallback);
        }

        private static int ReadDefaultInt(StepParameterDefinition parameter, int fallback)
        {
            if (parameter == null || parameter.DefaultValue == null)
            {
                return fallback;
            }

            int parsed;
            return int.TryParse(parameter.DefaultValue.ToString(), out parsed) ? parsed : fallback;
        }

        private static float ReadDefaultFloat(StepParameterDefinition parameter, float fallback)
        {
            if (parameter == null || parameter.DefaultValue == null)
            {
                return fallback;
            }

            return ReadFloat(parameter.DefaultValue, fallback);
        }

        private static bool ReadDefaultBool(StepParameterDefinition parameter, bool fallback)
        {
            if (parameter == null || parameter.DefaultValue == null)
            {
                return fallback;
            }

            bool parsed;
            return bool.TryParse(parameter.DefaultValue.ToString(), out parsed) ? parsed : fallback;
        }

        private static float ReadFloat(JToken token, float fallback)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }

            float parsed;
            return float.TryParse(
                token.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out parsed)
                ? parsed
                : fallback;
        }

        private static JObject MakeVector(Vector3 value)
        {
            JObject vector = new JObject();
            vector["x"] = value.x;
            vector["y"] = value.y;
            vector["z"] = value.z;
            return vector;
        }

        private static void RemoveParameterIfBlank(ModuleStep step, string key)
        {
            if (step == null || step.parameters == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(step.GetString(key, "")))
            {
                step.parameters.Remove(key);
            }
        }
    }
}
