using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialTextPanel : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private Vector2 panelSize = new Vector2(1.6f, 0.55f);

        [SerializeField]
        private int wrapCharacters = 42;

        private GameObject background;
        private TextMesh titleText;
        private TextMesh bodyText;

        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";

        private void Awake()
        {
            EnsureVisuals();
            Clear();
        }

        public void ShowText(ModuleDocument module, ModuleStep step, string body)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;

            titleText.text = step == null ? "" : step.title;
            bodyText.text = Wrap(body ?? "", wrapCharacters);

            gameObject.SetActive(true);
        }

        public void ClearIfShowingStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || showingStepId != stepId)
            {
                return;
            }

            Clear();
        }

        public void Clear()
        {
            showingStepId = "";
            currentModule = null;
            currentStep = null;

            if (titleText != null) titleText.text = "";
            if (bodyText != null) bodyText.text = "";

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (currentModule == null || currentStep == null)
            {
                return;
            }

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (anchorResolver == null)
            {
                return;
            }

            LayoutDefinition layout = FindLayoutForTarget(currentModule, currentStep.id);
            string anchorId = layout == null ? "" : layout.anchorId;
            if (string.IsNullOrWhiteSpace(anchorId))
            {
                anchorId = currentStep.GetString("anchorId", "anchor.head.default");
            }

            Pose anchorPose;
            string error;
            if (!anchorResolver.TryResolveAnchor(currentModule, anchorId, out anchorPose, out error))
            {
                return;
            }

            ApplyPose(anchorPose, layout);
        }

        private void ApplyPose(Pose anchorPose, LayoutDefinition layout)
        {
            Vector3 localPosition = layout == null
                ? Vector3.zero
                : RuntimeLayoutApplier.ToVector3(layout.position, Vector3.zero);

            Vector3 localEuler = layout == null
                ? Vector3.zero
                : RuntimeLayoutApplier.ToVector3(layout.rotationEuler, Vector3.zero);

            Vector3 localScale = layout == null
                ? Vector3.one
                : RuntimeLayoutApplier.ToVector3(layout.scale, Vector3.one);

            Quaternion localRotation = Quaternion.Euler(localEuler);
            transform.position = anchorPose.position + anchorPose.rotation * localPosition;
            transform.rotation = anchorPose.rotation * localRotation;
            transform.localScale = localScale;
        }

        private void EnsureVisuals()
        {
            if (background != null && titleText != null && bodyText != null)
            {
                return;
            }

            background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Spatial Panel Background";
            background.transform.SetParent(transform, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);

            Renderer renderer = background.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MakeMaterial(new Color(0.06f, 0.06f, 0.06f, 0.85f));
            }

            titleText = CreateText("Title", new Vector3(-0.74f, 0.19f, 0.02f), 0.035f);
            bodyText = CreateText("Body", new Vector3(-0.74f, 0.07f, 0.02f), 0.027f);
        }

        private TextMesh CreateText(string name, Vector3 localPosition, float characterSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.identity;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = Color.white;
            textMesh.text = "";
            return textMesh;
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static LayoutDefinition FindLayoutForTarget(ModuleDocument module, string targetId)
        {
            if (module == null || module.layouts == null || string.IsNullOrWhiteSpace(targetId))
            {
                return null;
            }

            for (int i = 0; i < module.layouts.Count; i++)
            {
                LayoutDefinition layout = module.layouts[i];
                if (layout != null && layout.targetId == targetId)
                {
                    return layout;
                }
            }

            return null;
        }

        private static string Wrap(string text, int maxCharactersPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharactersPerLine <= 0)
            {
                return text ?? "";
            }

            string[] words = text.Split(' ');
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int currentLineLength = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (currentLineLength > 0 && currentLineLength + word.Length + 1 > maxCharactersPerLine)
                {
                    builder.Append('\n');
                    currentLineLength = 0;
                }
                else if (currentLineLength > 0)
                {
                    builder.Append(' ');
                    currentLineLength++;
                }

                builder.Append(word);
                currentLineLength += word.Length;
            }

            return builder.ToString();
        }
    }
}