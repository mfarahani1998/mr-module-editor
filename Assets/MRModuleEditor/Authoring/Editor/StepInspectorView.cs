using System.Globalization;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MRModuleEditor.Authoring.Editor
{
    public static class StepInspectorView
    {
        private static readonly string[] StepTypes =
        {
            "text",
            "image",
            "wait",
            "showObject",
            "moveObject",
            "mcq",
            "showFrame",
            "rotateJoint"
        };

        public static void Draw(ModuleDocument document, int selectedStepIndex)
        {
            EditorGUILayout.LabelField("Step Inspector", EditorStyles.boldLabel);

            if (document == null || document.steps == null || document.steps.Count == 0)
            {
                EditorGUILayout.HelpBox("No steps yet. Add a step from the left panel.", MessageType.Info);
                return;
            }

            if (selectedStepIndex < 0 || selectedStepIndex >= document.steps.Count)
            {
                EditorGUILayout.HelpBox("Select a step from the left panel.", MessageType.Info);
                return;
            }

            ModuleStep step = document.steps[selectedStepIndex];
            if (step == null)
            {
                EditorGUILayout.HelpBox("Selected step is null.", MessageType.Error);
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new System.Collections.Generic.Dictionary<string, JToken>();
            }

            step.id = EditorGUILayout.TextField("ID", step.id);

            int currentTypeIndex = System.Array.IndexOf(StepTypes, step.type);

            if (currentTypeIndex < 0)
            {
                EditorGUILayout.HelpBox(
                    "Unknown step type: " + step.type + "\nChoose a valid type from the popup to repair this step.",
                    MessageType.Warning);
            }

            int shownTypeIndex = currentTypeIndex < 0 ? 0 : currentTypeIndex;
            int nextTypeIndex = EditorGUILayout.Popup("Type", shownTypeIndex, StepTypes);

            if (currentTypeIndex < 0 || nextTypeIndex != currentTypeIndex)
            {
                step.type = StepTypes[nextTypeIndex];
                EnsureDefaultsForType(step);
            }

            step.title = EditorGUILayout.TextField("Title", step.title);
            step.durationSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("Duration Seconds", step.durationSeconds));

            EditorGUILayout.Space(8);
            DrawSpecificFields(document, step);
        }

        public static void EnsureDefaultsForType(ModuleStep step)
        {
            if (step == null)
            {
                return;
            }

            if (step.parameters == null)
            {
                step.parameters = new System.Collections.Generic.Dictionary<string, JToken>();
            }

            if (step.type == "text")
            {
                SetIfMissing(step, "text", "New instruction text.");
                SetIfMissing(step, "anchorId", "anchor.head.default");
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Text";
                if (step.durationSeconds <= 0f) step.durationSeconds = 3f;
            }
            else if (step.type == "image")
            {
                SetIfMissing(step, "assetId", "asset.intro_image");
                SetIfMissing(step, "caption", "Image caption.");
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Image";
                if (step.durationSeconds <= 0f) step.durationSeconds = 3f;
            }
            else if (step.type == "wait")
            {
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Wait";
                if (step.durationSeconds <= 0f) step.durationSeconds = 1f;
            }
            else if (step.type == "showObject")
            {
                SetIfMissing(step, "objectId", "object.robot_preview");
                SetIfMissing(step, "visible", true);
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Show Object";
            }
            else if (step.type == "moveObject")
            {
                SetIfMissing(step, "objectId", "object.robot_preview");
                SetIfMissing(step, "isRelative", false);
                SetVectorIfMissing(step, "position", new Vector3(0f, 0f, 1.5f));
                SetVectorIfMissing(step, "rotationEuler", new Vector3(0f, 45f, 0f));
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Move Object";
                if (step.durationSeconds <= 0f) step.durationSeconds = 2f;
            }
            else if (step.type == "showFrame")
            {
                SetIfMissing(step, "objectId", "object.robot_preview");
                SetIfMissing(step, "jointIndex", 2);
                SetIfMissing(step, "visible", true);
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Show Frame";
                if (step.durationSeconds <= 0f) step.durationSeconds = 1f;
            }
            else if (step.type == "rotateJoint")
            {
                SetIfMissing(step, "objectId", "object.robot_preview");
                SetIfMissing(step, "jointIndex", 0);
                SetIfMissing(step, "angleDegrees", 50f);
                SetIfMissing(step, "showFrame", true);
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Rotate Joint";
                if (step.durationSeconds <= 0f) step.durationSeconds = 2f;
            }
            else if (step.type == "mcq")
            {
                SetIfMissing(step, "question", "What does forward kinematics compute?");
                if (!step.parameters.ContainsKey("choices"))
                {
                    step.parameters["choices"] = new JArray(
                        "Joint angles from pose",
                        "End-effector pose from joint angles");
                }
                SetIfMissing(step, "correctIndex", 1);
                if (string.IsNullOrWhiteSpace(step.title)) step.title = "Quick Check";
            }
        }

        private static void DrawSpecificFields(ModuleDocument document, ModuleStep step)
        {
            if (step.type == "text")
            {
                EditorIdDropdowns.DrawAnchorIdDropdown(document, step, "anchorId", "Fallback Anchor ID");
                EditorGUILayout.HelpBox(
                    "Text placement should normally come from the Layout section below. " +
                    "This anchorId is still useful as a fallback when no layout exists.",
                    MessageType.Info);
                DrawMultilineString(step, "text", "Text");
            }
            else if (step.type == "image")
            {
                EditorIdDropdowns.DrawAssetIdDropdown(document, step, "assetId", "Image Asset", "image");
                DrawMultilineString(step, "caption", "Caption");
            }
            else if (step.type == "wait")
            {
                EditorGUILayout.HelpBox("Wait uses Duration Seconds. No extra parameters are required.", MessageType.Info);
            }
            else if (step.type == "showObject")
            {
                EditorIdDropdowns.DrawObjectIdDropdown(document, step, "objectId", "Object");
                DrawBool(step, "visible", "Visible", true);
            }
            else if (step.type == "moveObject")
            {
                EditorIdDropdowns.DrawObjectIdDropdown(document, step, "objectId", "Object");
                DrawBool(step, "isRelative", "Is Relative", false);
                if (step.GetBool("isRelative", false))
                {
                    DrawVector3(step, "positionDelta", "Relative Position");
                    DrawVector3(step, "rotationEulerDelta", "Relative Rotation Euler");
                }
                else
                {
                    DrawVector3(step, "position", "Target Local Position");
                    DrawVector3(step, "rotationEuler", "Target Local Rotation Euler");
                }
            }
            else if (step.type == "showFrame")
            {
                EditorIdDropdowns.DrawObjectIdDropdown(document, step, "objectId", "Object");
                DrawInt(step, "jointIndex", "Joint Index", 0);
                DrawBool(step, "visible", "Visible", true);
            }
            else if (step.type == "rotateJoint")
            {
                EditorIdDropdowns.DrawObjectIdDropdown(document, step, "objectId", "Object");
                DrawInt(step, "jointIndex", "Joint Index", 0);
                DrawFloat(step, "angleDegrees", "Angle Degrees", 0f);
                DrawBool(step, "showFrame", "Show Frame", true);
            }
            else if (step.type == "mcq")
            {
                DrawMultilineString(step, "question", "Question");
                DrawStringArray(step, "choices", "Choices");
                DrawInt(step, "correctIndex", "Correct Index", 0);

                EditorGUILayout.Space(4);
                EditorIdDropdowns.DrawAnchorIdDropdown(
                    document,
                    step,
                    "anchorId",
                    "Fallback Anchor ID",
                    allowNone: true);
                EditorGUILayout.HelpBox(
                    "For MCQ placement, prefer the Layout section below. " +
                    "This optional anchorId is only a fallback when no matching layout exists.",
                    MessageType.Info);
            }
        }

        private static void DrawString(ModuleStep step, string key, string label)
        {
            string next = EditorGUILayout.TextField(label, step.GetString(key, ""));
            step.parameters[key] = JToken.FromObject(next);
        }

        private static void DrawMultilineString(ModuleStep step, string key, string label)
        {
            EditorGUILayout.LabelField(label);
            string next = EditorGUILayout.TextArea(step.GetString(key, ""), GUILayout.MinHeight(60));
            step.parameters[key] = JToken.FromObject(next);
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

        private static void DrawVector3(ModuleStep step, string key, string label)
        {
            Vector3 current = ReadVector3(step.GetToken(key));
            Vector3 next = EditorGUILayout.Vector3Field(label, current);
            step.parameters[key] = MakeVector(next);
        }

        private static void DrawStringArray(ModuleStep step, string key, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            JArray array = step.GetToken(key) as JArray;
            if (array == null)
            {
                array = new JArray();
            }

            int nextCount = Mathf.Max(0, EditorGUILayout.IntField("Choice Count", array.Count));
            while (array.Count < nextCount) array.Add("");
            while (array.Count > nextCount) array.RemoveAt(array.Count - 1);

            for (int i = 0; i < array.Count; i++)
            {
                string value = array[i] == null ? "" : array[i].ToString();
                array[i] = EditorGUILayout.TextField("Choice " + i, value);
            }

            step.parameters[key] = array;
        }

        private static Vector3 ReadVector3(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return Vector3.zero;
            }

            return new Vector3(
                ReadFloat(token["x"], 0f),
                ReadFloat(token["y"], 0f),
                ReadFloat(token["z"], 0f));
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

        private static void SetIfMissing(ModuleStep step, string key, string value)
        {
            if (!step.parameters.ContainsKey(key)) step.parameters[key] = JToken.FromObject(value);
        }

        private static void SetIfMissing(ModuleStep step, string key, bool value)
        {
            if (!step.parameters.ContainsKey(key)) step.parameters[key] = JToken.FromObject(value);
        }

        private static void SetIfMissing(ModuleStep step, string key, int value)
        {
            if (!step.parameters.ContainsKey(key)) step.parameters[key] = JToken.FromObject(value);
        }

        private static void SetVectorIfMissing(ModuleStep step, string key, Vector3 value)
        {
            if (!step.parameters.ContainsKey(key)) step.parameters[key] = MakeVector(value);
        }

        private static void SetIfMissing(ModuleStep step, string key, float value)
        {
            if (!step.parameters.ContainsKey(key)) step.parameters[key] = JToken.FromObject(value);
        }
    }
}