using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialImagePanel : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private Vector2 panelSize = new Vector2(1.5f, 0.95f);

        [SerializeField]
        private Vector2 imageSize = new Vector2(1.05f, 0.52f);

        [SerializeField]
        private Vector3 defaultLocalOffset = new Vector3(0f, -0.15f, 0f);

        [SerializeField]
        private int wrapCharacters = 42;

        [SerializeField]
        private float titleCharacterSize = 0.025f;

        [SerializeField]
        private float captionCharacterSize = 0.015f;

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.88f);

        [SerializeField]
        private Color titleColor = Color.white;

        [SerializeField]
        private Color captionColor = new Color(0.92f, 0.92f, 0.92f, 1f);

        private GameObject background;
        private GameObject imageQuad;
        private TextMesh titleText;
        private TextMesh captionText;
        private Material imageMaterial;
        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";

        private void Awake()
        {
            EnsureVisuals();
            gameObject.SetActive(false);
        }

        public void ShowImage(ModuleDocument module, ModuleStep step, Texture2D texture, string caption)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;

            titleText.text = step == null ? "" : step.title ?? "";
            captionText.text = Wrap(caption ?? "", wrapCharacters);

            if (imageMaterial != null)
            {
                if (imageMaterial.HasProperty("_BaseMap")) imageMaterial.SetTexture("_BaseMap", texture);
                if (imageMaterial.HasProperty("_MainTex")) imageMaterial.SetTexture("_MainTex", texture);
            }

            gameObject.SetActive(true);
        }

        public void ClearIfShowingStep(string stepId)
        {
            if (!string.IsNullOrEmpty(stepId) && showingStepId == stepId)
            {
                Clear();
            }
        }

        public void Clear()
        {
            showingStepId = "";
            currentModule = null;
            currentStep = null;

            if (titleText != null) titleText.text = "";
            if (captionText != null) captionText.text = "";
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

            Vector3 localPosition = layout == null
                ? defaultLocalOffset
                : RuntimeLayoutApplier.ToVector3(layout.position, defaultLocalOffset);

            Vector3 localEuler = layout == null
                ? Vector3.zero
                : RuntimeLayoutApplier.ToVector3(layout.rotationEuler, Vector3.zero);

            Vector3 localScale = layout == null
                ? Vector3.one
                : RuntimeLayoutApplier.ToVector3(layout.scale, Vector3.one);

            transform.position = anchorPose.position + anchorPose.rotation * localPosition;
            transform.rotation = anchorPose.rotation * Quaternion.Euler(localEuler);
            transform.localScale = localScale;
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = CreateQuad("Image Panel Background", MakeColorMaterial(panelColor), false);
                background.transform.SetParent(transform, false);
                background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);
            }

            if (imageQuad == null)
            {
                imageMaterial = MakeImageMaterial();
                imageQuad = CreateQuad("Image", imageMaterial, false);
                imageQuad.transform.SetParent(transform, false);
                imageQuad.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                imageQuad.transform.localScale = new Vector3(imageSize.x, imageSize.y, 1f);
            }

            if (titleText == null)
            {
                titleText = CreateText("Title", titleCharacterSize, titleColor);
                titleText.transform.localPosition = new Vector3(-panelSize.x * 0.5f + 0.08f, panelSize.y * 0.5f - 0.05f, 0.02f);
            }

            if (captionText == null)
            {
                captionText = CreateText("Caption", captionCharacterSize, captionColor);
                captionText.transform.localPosition = new Vector3(-panelSize.x * 0.5f + 0.08f, -panelSize.y * 0.5f + 0.18f, 0.02f);
            }
        }

        private GameObject CreateQuad(string objectName, Material material, bool keepCollider)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;

            Collider collider = quad.GetComponent<Collider>();
            if (!keepCollider)
            {
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            else
            {
                // Use a BoxCollider instead of relying on the Quad mesh collider.
                // This makes gaze selection more reliable from either side.
                if (collider != null)
                {
                    Destroy(collider);
                }

                BoxCollider boxCollider = quad.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, 0.04f);
            }

            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return quad;
        }

        private TextMesh CreateText(string objectName, float characterSize, Color color)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = 32;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.text = "";

            Renderer renderer = textObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return textMesh;
        }

        private static Material MakeColorMaterial(Color color)
        {
            return SpatialMaterialUtility.CreateColorMaterial(color, nameof(SpatialImagePanel));
        }

        private static Material MakeImageMaterial()
        {
            return SpatialMaterialUtility.CreateTextureMaterial(nameof(SpatialImagePanel));
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

            string[] words = text.Replace("\r\n", "\n").Replace('\r', '\n').Split(' ');
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int lineLength = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (lineLength > 0 && lineLength + word.Length + 1 > maxCharactersPerLine)
                {
                    builder.Append('\n');
                    lineLength = 0;
                }

                if (lineLength > 0)
                {
                    builder.Append(' ');
                    lineLength++;
                }

                builder.Append(word);
                lineLength += word.Length;
            }

            return builder.ToString();
        }
    }
}