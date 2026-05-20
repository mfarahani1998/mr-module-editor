using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialTextPanel : MonoBehaviour
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [Header("Panel")]
        [SerializeField]
        private bool autoSizePanel = true;

        // Used as the fixed size when autoSizePanel is disabled.
        [SerializeField]
        private Vector2 panelSize = new Vector2(3.2f, 0.58f);

        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, -1f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [SerializeField]
        private Vector2 minimumPanelSize = new Vector2(0.8f, 0.25f);

        [SerializeField]
        private Vector2 maximumPanelSize = new Vector2(1.5f, 0.6f);

        [SerializeField]
        private Vector2 padding = new Vector2(0.18f, 0.08f);

        [SerializeField]
        private float textDepthOffset = 0.01f;

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.86f);

        [SerializeField]
        private bool showAccentBar = true;

        [SerializeField]
        private float accentWidth = 0.035f;

        [SerializeField]
        private float accentGap = 0.10f;

        [SerializeField]
        private Color accentColor = new Color(0.28f, 0.68f, 1.0f, 0.95f);

        [Header("Text")]
        [SerializeField]
        private int wrapCharacters = 50;

        [SerializeField]
        private int textFontSize = 32;

        [SerializeField]
        private float titleCharacterSize = 0.035f;

        [SerializeField]
        private float bodyCharacterSize = 0.025f;

        [SerializeField]
        private float titleLineHeight = 0.29f;

        [SerializeField]
        private float bodyLineHeight = 0.18f;

        [SerializeField]
        private float titleBodyGap = 0.035f;

        // TextMesh is not layout-aware. This multiplier is an intentionally simple
        // approximation used to size the primitive background from character counts.
        [SerializeField]
        private float estimatedCharacterWidth = 2.95f;

        [SerializeField]
        private Color titleColor = Color.white;

        [SerializeField]
        private Color bodyColor = new Color(0.92f, 0.92f, 0.92f, 1f);

        [Header("Head Follow")]
        [SerializeField]
        private bool smoothFollow = true;

        [SerializeField]
        private float followSharpness = 16f;

        [SerializeField]
        private float snapDistance = 2.5f;

        private GameObject background;
        private GameObject accentBar;
        private TextMesh titleText;
        private TextMesh bodyText;

        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";
        private bool hasAppliedPose;

        private void Awake()
        {
            EnsureVisuals();
            Clear();
        }

        private void OnValidate()
        {
            panelSize = ClampVector(panelSize, new Vector2(0.1f, 0.1f));
            minimumPanelSize = ClampVector(minimumPanelSize, new Vector2(0.1f, 0.1f));
            maximumPanelSize = new Vector2(
                Mathf.Max(maximumPanelSize.x, minimumPanelSize.x),
                Mathf.Max(maximumPanelSize.y, minimumPanelSize.y));
            padding = ClampVector(padding, Vector2.zero);
            wrapCharacters = Mathf.Max(1, wrapCharacters);
            textFontSize = Mathf.Max(1, textFontSize);
            titleCharacterSize = Mathf.Max(0.001f, titleCharacterSize);
            bodyCharacterSize = Mathf.Max(0.001f, bodyCharacterSize);
            titleLineHeight = Mathf.Max(0.001f, titleLineHeight);
            bodyLineHeight = Mathf.Max(0.001f, bodyLineHeight);
            titleBodyGap = Mathf.Max(0f, titleBodyGap);
            estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            accentWidth = Mathf.Max(0f, accentWidth);
            accentGap = Mathf.Max(0f, accentGap);
            followSharpness = Mathf.Max(0.01f, followSharpness);
            snapDistance = Mathf.Max(0.01f, snapDistance);

            if (background != null && titleText != null && bodyText != null)
            {
                ApplyTextSettings();
                UpdateMaterialColors();
                UpdateVisualLayout();
            }
        }

        public void ShowText(ModuleDocument module, ModuleStep step, string body)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            showingStepId = step == null ? "" : step.id;

            titleText.text = step == null ? "" : step.title ?? "";
            bodyText.text = Wrap(body ?? "", GetEffectiveWrapCharacters());

            ApplyTextSettings();
            UpdateVisualLayout();

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
            hasAppliedPose = false;

            if (titleText != null) titleText.text = "";
            if (bodyText != null) bodyText.text = "";
            if (background != null) UpdateVisualLayout();

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

            if (layout == null || applyPanelLocalOffsetToAuthoredLayouts)
            {
                localPosition += panelLocalOffset;
            }
            
            Quaternion localRotation = Quaternion.Euler(localEuler);
            Vector3 targetPosition = anchorPose.position + anchorPose.rotation * localPosition;
            Quaternion targetRotation = anchorPose.rotation * localRotation;

            transform.localScale = localScale;

            if (!smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                hasAppliedPose = true;
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) > snapDistance)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = CreateQuad("Spatial Panel Background", panelColor, 0);
            }

            if (accentBar == null)
            {
                accentBar = CreateQuad("Spatial Panel Accent", accentColor, 1);
            }

            if (titleText == null)
            {
                titleText = CreateText("Title", titleCharacterSize, titleColor, 2);
            }

            if (bodyText == null)
            {
                bodyText = CreateText("Body", bodyCharacterSize, bodyColor, 2);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
        }

        private GameObject CreateQuad(string objectName, Color color, int sortingOrder)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;

            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = MakeMaterial(color);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = sortingOrder;
            }

            return quad;
        }

        private TextMesh CreateText(string objectName, float characterSize, Color color, int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = Vector3.zero;
            textObject.transform.localRotation = Quaternion.identity;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.fontSize = textFontSize;
            textMesh.characterSize = characterSize;
            textMesh.color = color;
            textMesh.text = "";

            Renderer renderer = textObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = sortingOrder;
            }

            return textMesh;
        }

        private void ApplyTextSettings()
        {
            if (titleText != null)
            {
                titleText.anchor = TextAnchor.UpperLeft;
                titleText.alignment = TextAlignment.Left;
                titleText.fontSize = textFontSize;
                titleText.characterSize = titleCharacterSize;
                titleText.color = titleColor;
            }

            if (bodyText != null)
            {
                bodyText.anchor = TextAnchor.UpperLeft;
                bodyText.alignment = TextAlignment.Left;
                bodyText.fontSize = textFontSize;
                bodyText.characterSize = bodyCharacterSize;
                bodyText.color = bodyColor;
            }
        }

        private void UpdateMaterialColors()
        {
            SetRendererColor(background, panelColor);
            SetRendererColor(accentBar, accentColor);
        }

        private void UpdateVisualLayout()
        {
            if (background == null || titleText == null || bodyText == null)
            {
                return;
            }

            string title = titleText.text ?? "";
            string body = bodyText.text ?? "";
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            bool hasBody = !string.IsNullOrWhiteSpace(body);

            Vector2 actualSize = autoSizePanel ? CalculatePanelSize(title, body, hasTitle, hasBody) : panelSize;
            actualSize = new Vector2(Mathf.Max(0.1f, actualSize.x), Mathf.Max(0.1f, actualSize.y));

            background.transform.localPosition = Vector3.zero;
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(actualSize.x, actualSize.y, 1f);

            float extraLeftInset = showAccentBar ? accentWidth + accentGap : 0f;
            float left = -actualSize.x * 0.5f + padding.x + extraLeftInset;
            float top = actualSize.y * 0.5f - padding.y;

            if (accentBar != null)
            {
                accentBar.SetActive(showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + padding.x * 0.5f,
                    0f,
                    textDepthOffset * 0.5f);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    accentWidth,
                    Mathf.Max(0.01f, actualSize.y - padding.y * 1.5f),
                    1f);
            }

            titleText.transform.localPosition = new Vector3(left, top, textDepthOffset);

            float bodyTop = top;
            if (hasTitle)
            {
                bodyTop -= CountLines(title) * titleLineHeight;
            }

            if (hasTitle && hasBody)
            {
                bodyTop -= titleBodyGap;
            }

            bodyText.transform.localPosition = new Vector3(left, bodyTop, textDepthOffset);
        }

        private Vector2 CalculatePanelSize(string title, string body, bool hasTitle, bool hasBody)
        {
            float extraLeftInset = showAccentBar ? accentWidth + accentGap : 0f;

            float titleWidth = LongestLineLength(title) * titleCharacterSize * estimatedCharacterWidth;
            float bodyWidth = LongestLineLength(body) * bodyCharacterSize * estimatedCharacterWidth;
            float desiredWidth = Mathf.Max(titleWidth, bodyWidth) + padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            int titleLineCount = hasTitle ? CountLines(title) : 0;
            int bodyLineCount = hasBody ? CountLines(body) : 0;

            float desiredHeight = padding.y * 2f;
            if (hasTitle)
            {
                desiredHeight += titleLineCount * titleLineHeight;
            }

            if (hasTitle && hasBody)
            {
                desiredHeight += titleBodyGap;
            }

            if (hasBody)
            {
                desiredHeight += bodyLineCount * bodyLineHeight;
            }

            if (!hasTitle && !hasBody)
            {
                desiredHeight += bodyLineHeight;
            }

            desiredHeight = Mathf.Clamp(desiredHeight, minimumPanelSize.y, maximumPanelSize.y);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private int GetEffectiveWrapCharacters()
        {
            int result = Mathf.Max(1, wrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float extraLeftInset = showAccentBar ? accentWidth + accentGap : 0f;
            float contentWidth = maximumPanelSize.x - padding.x * 2f - extraLeftInset;
            float averageCharacterWidth = Mathf.Max(0.001f, bodyCharacterSize * estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

        private static Material MakeMaterial(Color color)
        {
            return SpatialMaterialUtility.CreateColorMaterial(color, nameof(SpatialTextPanel));
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
        }

        private static void SetRendererColor(GameObject target, Color color)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            SetMaterialColor(renderer.sharedMaterial, color);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
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

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] paragraphs = text.Split('\n');
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0)
                {
                    builder.Append('\n');
                }

                string paragraph = paragraphs[p];
                if (string.IsNullOrWhiteSpace(paragraph))
                {
                    continue;
                }

                string[] words = paragraph.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                int currentLineLength = 0;

                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];

                    while (word.Length > maxCharactersPerLine)
                    {
                        if (currentLineLength > 0)
                        {
                            builder.Append('\n');
                            currentLineLength = 0;
                        }

                        builder.Append(word.Substring(0, maxCharactersPerLine));
                        word = word.Substring(maxCharactersPerLine);

                        if (word.Length > 0)
                        {
                            builder.Append('\n');
                        }
                    }

                    if (word.Length == 0)
                    {
                        continue;
                    }

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
            }

            return builder.ToString();
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    count++;
                }
            }

            return count;
        }

        private static int LongestLineLength(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int longest = 0;
            int current = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    longest = Mathf.Max(longest, current);
                    current = 0;
                }
                else if (c != '\r')
                {
                    current++;
                }
            }

            return Mathf.Max(longest, current);
        }

        private static Vector2 ClampVector(Vector2 value, Vector2 minimum)
        {
            return new Vector2(Mathf.Max(minimum.x, value.x), Mathf.Max(minimum.y, value.y));
        }
    }
}