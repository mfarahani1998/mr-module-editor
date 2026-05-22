using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;
using UnityEngine.Serialization;

namespace MRModuleEditor.Runtime.UI
{
    public class SpatialImagePanel : MonoBehaviour
    {
        private const float LocalZBackground = 0f;
        private const float LocalZAccent = 0.005f;
        private const float LocalZImage = 0.01f;

        private const int SortingBackground = 0;
        private const int SortingAccent = 1;
        private const int SortingImage = 1;
        private const int SortingText = 2;

        [SerializeField]
        private SpatialLayoutResolver spatialLayoutResolver;

        [Header("Panel")]
        [SerializeField]
        private bool autoSizePanel = true;

        // Used as the fixed size when autoSizePanel is disabled.
        [SerializeField]
        private Vector2 panelSize = new Vector2(1.5f, 0.95f);

        [FormerlySerializedAs("defaultLocalOffset")]
        [SerializeField]
        private Vector3 panelLocalOffset = new Vector3(0f, -0.15f, 0f);

        [SerializeField]
        private bool applyPanelLocalOffsetToAuthoredLayouts = false;

        [SerializeField]
        private Vector2 minimumPanelSize = new Vector2(0.85f, 0.55f);

        [SerializeField]
        private Vector2 maximumPanelSize = new Vector2(1.8f, 1.35f);

        [SerializeField]
        private Vector2 padding = new Vector2(0.10f, 0.08f);

        [SerializeField]
        private float textDepthOffset = 0.02f;

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.88f);

        [SerializeField]
        private bool showAccentBar = false;

        [SerializeField]
        private float accentWidth = 0.035f;

        [SerializeField]
        private float accentGap = 0.08f;

        [SerializeField]
        private Color accentColor = new Color(0.28f, 0.68f, 1.0f, 0.95f);

        [Header("Image")]
        [SerializeField]
        private Vector2 imageSize = new Vector2(1.05f, 0.52f);

        [SerializeField]
        private Vector2 minimumImageSize = new Vector2(0.35f, 0.22f);

        [SerializeField]
        private bool preserveImageAspectRatio = true;

        [SerializeField]
        private float titleImageGap = 0.055f;

        [SerializeField]
        private float imageCaptionGap = 0.055f;

        [Header("Text")]
        [SerializeField]
        private int wrapCharacters = 50;

        [SerializeField]
        private int textFontSize = 32;

        [SerializeField]
        private float titleCharacterSize = 0.025f;

        [SerializeField]
        private float captionCharacterSize = 0.018f;

        [SerializeField]
        private float titleLineHeight = 0.12f;

        [SerializeField]
        private float captionLineHeight = 0.075f;

        // TextMesh is not layout-aware. This multiplier is an intentionally simple
        // approximation used to size primitive backgrounds from character counts.
        [SerializeField]
        private float estimatedCharacterWidth = 1.5f;

        [SerializeField]
        private Color titleColor = Color.white;

        [SerializeField]
        private Color captionColor = new Color(0.92f, 0.92f, 0.92f, 1f);

        [Header("Head Follow")]
        [SerializeField]
        private bool smoothFollow = true;

        [SerializeField]
        private float followSharpness = 16f;

        [SerializeField]
        private float snapDistance = 2.5f;

        private GameObject background;
        private GameObject accentBar;
        private GameObject imageQuad;
        private TextMesh titleText;
        private TextMesh captionText;
        private Material imageMaterial;
        private Texture2D currentTexture;
        private ModuleDocument currentModule;
        private ModuleStep currentStep;
        private string showingStepId = "";
        private bool hasAppliedPose;

        private SpatialLayoutResolver LayoutResolver
        {
            get
            {
                if (spatialLayoutResolver != null)
                {
                    return spatialLayoutResolver;
                }

                spatialLayoutResolver = FindFirstObjectByType<SpatialLayoutResolver>();
                return spatialLayoutResolver;
            }
        }

        private void Awake()
        {
            EnsureVisuals();
            Clear();
        }

        private void OnValidate()
        {
            panelSize = SpatialRenderUtility.ClampVector(panelSize, new Vector2(0.1f, 0.1f));
            minimumPanelSize = SpatialRenderUtility.ClampVector(minimumPanelSize, new Vector2(0.1f, 0.1f));
            maximumPanelSize = new Vector2(
                Mathf.Max(maximumPanelSize.x, minimumPanelSize.x),
                Mathf.Max(maximumPanelSize.y, minimumPanelSize.y));
            padding = SpatialRenderUtility.ClampVector(padding, Vector2.zero);
            imageSize = SpatialRenderUtility.ClampVector(imageSize, new Vector2(0.01f, 0.01f));
            minimumImageSize = SpatialRenderUtility.ClampVector(minimumImageSize, new Vector2(0.01f, 0.01f));
            wrapCharacters = Mathf.Max(1, wrapCharacters);
            textFontSize = Mathf.Max(1, textFontSize);
            titleCharacterSize = Mathf.Max(0.001f, titleCharacterSize);
            captionCharacterSize = Mathf.Max(0.001f, captionCharacterSize);
            titleLineHeight = Mathf.Max(0.001f, titleLineHeight);
            captionLineHeight = Mathf.Max(0.001f, captionLineHeight);
            estimatedCharacterWidth = Mathf.Max(0.001f, estimatedCharacterWidth);
            titleImageGap = Mathf.Max(0f, titleImageGap);
            imageCaptionGap = Mathf.Max(0f, imageCaptionGap);
            textDepthOffset = Mathf.Max(0.001f, textDepthOffset);
            accentWidth = Mathf.Max(0f, accentWidth);
            accentGap = Mathf.Max(0f, accentGap);
            followSharpness = Mathf.Max(0.01f, followSharpness);
            snapDistance = Mathf.Max(0.01f, snapDistance);

            if (background != null && imageQuad != null && titleText != null && captionText != null)
            {
                ApplyTextSettings();
                UpdateMaterialColors();
                UpdateVisualLayout();
            }
        }

        public void ShowImage(ModuleDocument module, ModuleStep step, Texture2D texture, string caption)
        {
            EnsureVisuals();

            currentModule = module;
            currentStep = step;
            currentTexture = texture;
            showingStepId = step == null ? "" : step.id;
            hasAppliedPose = false;

            titleText.text = SpatialRenderUtility.Wrap(step == null ? "" : step.title ?? "", GetEffectiveWrapCharacters(titleCharacterSize));
            captionText.text = SpatialRenderUtility.Wrap(caption ?? "", GetEffectiveWrapCharacters(captionCharacterSize));

            SpatialRenderUtility.SetMaterialTexture(imageMaterial, texture);
            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();

            gameObject.SetActive(true);
            ApplyAnchoredPose();
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
            currentTexture = null;
            hasAppliedPose = false;

            if (titleText != null) titleText.text = "";
            if (captionText != null) captionText.text = "";
            SpatialRenderUtility.SetMaterialTexture(imageMaterial, null);
            if (background != null) UpdateVisualLayout();

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            ApplyAnchoredPose();
        }

        private bool ApplyAnchoredPose()
        {
            if (currentModule == null || currentStep == null)
            {
                return false;
            }

            SpatialLayoutResolver resolver = LayoutResolver;
            if (resolver == null)
            {
                return false;
            }

            Pose targetPose;
            Vector3 targetScale;
            string error;

            string fallbackAnchorId = currentStep.GetString("anchorId", "anchor.head.default");
            bool ok = resolver.TryResolvePoseForStep(
                currentModule,
                currentStep,
                fallbackAnchorId,
                panelLocalOffset,
                Vector3.zero,
                Vector3.one,
                applyPanelLocalOffsetToAuthoredLayouts,
                out targetPose,
                out targetScale,
                out error);

            if (!ok)
            {
                return false;
            }

            ApplyResolvedPose(targetPose, targetScale);
            return true;
        }

        private void ApplyResolvedPose(Pose targetPose, Vector3 targetScale)
        {
            transform.localScale = targetScale;

            if (!smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPose.position;
                transform.rotation = targetPose.rotation;
                hasAppliedPose = true;
                return;
            }

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPose.position) > snapDistance)
            {
                transform.position = targetPose.position;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPose.position, t);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetPose.rotation, t);
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Image Panel Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(panelColor, nameof(SpatialImagePanel)),
                    false,
                    SortingBackground);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Image Panel Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(accentColor, nameof(SpatialImagePanel)),
                    false,
                    SortingAccent);
            }

            if (imageQuad == null)
            {
                imageMaterial = SpatialRenderUtility.CreateImageMaterial(nameof(SpatialImagePanel));
                imageQuad = SpatialRenderUtility.CreateQuad(transform, "Image", imageMaterial, false, SortingImage);
            }

            if (titleText == null)
            {
                titleText = SpatialRenderUtility.CreateText(
                    transform,
                    "Title",
                    titleCharacterSize,
                    textFontSize,
                    titleColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (captionText == null)
            {
                captionText = SpatialRenderUtility.CreateText(
                    transform,
                    "Caption",
                    captionCharacterSize,
                    textFontSize,
                    captionColor,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            ApplyTextSettings();
            UpdateMaterialColors();
            UpdateVisualLayout();
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

            if (captionText != null)
            {
                captionText.anchor = TextAnchor.UpperLeft;
                captionText.alignment = TextAlignment.Left;
                captionText.fontSize = textFontSize;
                captionText.characterSize = captionCharacterSize;
                captionText.color = captionColor;
            }
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, accentColor);
        }

        private void UpdateVisualLayout()
        {
            if (background == null || imageQuad == null || titleText == null || captionText == null)
            {
                return;
            }

            string title = titleText.text ?? "";
            string caption = captionText.text ?? "";
            bool hasTitle = !string.IsNullOrWhiteSpace(title);
            bool hasCaption = !string.IsNullOrWhiteSpace(caption);

            Vector2 actualSize = autoSizePanel ? CalculatePanelSize(title, caption, hasTitle, hasCaption) : panelSize;
            actualSize = new Vector2(Mathf.Max(0.1f, actualSize.x), Mathf.Max(0.1f, actualSize.y));

            background.transform.localPosition = new Vector3(0f, 0f, LocalZBackground);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(actualSize.x, actualSize.y, 1f);

            float extraLeftInset = GetExtraLeftInset();
            float contentLeft = -actualSize.x * 0.5f + padding.x + extraLeftInset;
            float contentTop = actualSize.y * 0.5f - padding.y;
            float contentWidth = Mathf.Max(0.01f, actualSize.x - padding.x * 2f - extraLeftInset);
            float contentCenterX = contentLeft + contentWidth * 0.5f;

            if (accentBar != null)
            {
                accentBar.SetActive(showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + padding.x * 0.5f,
                    0f,
                    LocalZAccent);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    accentWidth,
                    Mathf.Max(0.01f, actualSize.y - padding.y * 1.5f),
                    1f);
            }

            float cursorY = contentTop;
            titleText.transform.localPosition = new Vector3(contentLeft, cursorY, textDepthOffset);
            if (hasTitle)
            {
                cursorY -= SpatialRenderUtility.CountLines(title) * titleLineHeight;
                cursorY -= titleImageGap;
            }

            Vector2 actualImageSize = CalculateImageDisplaySize(actualSize, title, caption, hasTitle, hasCaption);
            float imageCenterY = cursorY - actualImageSize.y * 0.5f;
            imageQuad.transform.localPosition = new Vector3(contentCenterX, imageCenterY, LocalZImage);
            imageQuad.transform.localRotation = Quaternion.identity;
            imageQuad.transform.localScale = new Vector3(actualImageSize.x, actualImageSize.y, 1f);
            cursorY = imageCenterY - actualImageSize.y * 0.5f;

            if (hasCaption)
            {
                cursorY -= imageCaptionGap;
            }

            captionText.transform.localPosition = new Vector3(contentLeft, cursorY, textDepthOffset);
        }

        private Vector2 CalculatePanelSize(string title, string caption, bool hasTitle, bool hasCaption)
        {
            float extraLeftInset = GetExtraLeftInset();
            Vector2 preferredImageSize = GetPreferredImageDisplaySize();

            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * titleCharacterSize * estimatedCharacterWidth;
            float captionWidth = SpatialRenderUtility.LongestLineLength(caption) * captionCharacterSize * estimatedCharacterWidth;
            float desiredContentWidth = Mathf.Max(preferredImageSize.x, Mathf.Max(titleWidth, captionWidth));
            float desiredWidth = desiredContentWidth + padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            float desiredHeight = padding.y * 2f + preferredImageSize.y;
            if (hasTitle)
            {
                desiredHeight += SpatialRenderUtility.CountLines(title) * titleLineHeight;
                desiredHeight += titleImageGap;
            }

            if (hasCaption)
            {
                desiredHeight += imageCaptionGap;
                desiredHeight += SpatialRenderUtility.CountLines(caption) * captionLineHeight;
            }

            desiredHeight = Mathf.Clamp(desiredHeight, minimumPanelSize.y, maximumPanelSize.y);
            return new Vector2(desiredWidth, desiredHeight);
        }

        private Vector2 CalculateImageDisplaySize(
            Vector2 actualPanelSize,
            string title,
            string caption,
            bool hasTitle,
            bool hasCaption)
        {
            Vector2 preferredImageSize = GetPreferredImageDisplaySize();
            float extraLeftInset = GetExtraLeftInset();
            float maxImageWidth = Mathf.Max(0.01f, actualPanelSize.x - padding.x * 2f - extraLeftInset);

            float reservedHeight = padding.y * 2f;
            if (hasTitle)
            {
                reservedHeight += SpatialRenderUtility.CountLines(title) * titleLineHeight + titleImageGap;
            }

            if (hasCaption)
            {
                reservedHeight += imageCaptionGap + SpatialRenderUtility.CountLines(caption) * captionLineHeight;
            }

            float maxImageHeight = Mathf.Max(0.01f, actualPanelSize.y - reservedHeight);
            Vector2 displaySize = new Vector2(
                Mathf.Min(preferredImageSize.x, maxImageWidth),
                Mathf.Min(preferredImageSize.y, maxImageHeight));

            if (!preserveImageAspectRatio)
            {
                return SpatialRenderUtility.ClampVector(displaySize, minimumImageSize);
            }

            float aspect = GetTextureAspectRatio();
            if (aspect <= 0f)
            {
                aspect = Mathf.Max(0.01f, imageSize.x) / Mathf.Max(0.01f, imageSize.y);
            }

            if (displaySize.x / Mathf.Max(0.001f, displaySize.y) > aspect)
            {
                displaySize.x = displaySize.y * aspect;
            }
            else
            {
                displaySize.y = displaySize.x / aspect;
            }

            displaySize = new Vector2(
                Mathf.Max(minimumImageSize.x, displaySize.x),
                Mathf.Max(minimumImageSize.y, displaySize.y));

            displaySize.x = Mathf.Min(displaySize.x, maxImageWidth);
            displaySize.y = Mathf.Min(displaySize.y, maxImageHeight);
            return displaySize;
        }

        private Vector2 GetPreferredImageDisplaySize()
        {
            Vector2 preferredSize = imageSize;
            if (!preserveImageAspectRatio)
            {
                return SpatialRenderUtility.ClampVector(preferredSize, minimumImageSize);
            }

            float aspect = GetTextureAspectRatio();
            if (aspect <= 0f)
            {
                aspect = Mathf.Max(0.01f, imageSize.x) / Mathf.Max(0.01f, imageSize.y);
            }

            if (preferredSize.x / Mathf.Max(0.001f, preferredSize.y) > aspect)
            {
                preferredSize.x = preferredSize.y * aspect;
            }
            else
            {
                preferredSize.y = preferredSize.x / aspect;
            }

            return SpatialRenderUtility.ClampVector(preferredSize, minimumImageSize);
        }

        private float GetTextureAspectRatio()
        {
            if (currentTexture == null || currentTexture.width <= 0 || currentTexture.height <= 0)
            {
                return 0f;
            }

            return (float)currentTexture.width / currentTexture.height;
        }

        private int GetEffectiveWrapCharacters(float characterSize)
        {
            int result = Mathf.Max(1, wrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float contentWidth = maximumPanelSize.x - padding.x * 2f - GetExtraLeftInset();
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

        private float GetExtraLeftInset()
        {
            return showAccentBar ? accentWidth + accentGap : 0f;
        }

    }
}