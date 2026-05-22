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

        [Header("Style")]
        [SerializeField]
        private SpatialPanelStyle style;

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

        [Header("Image")]
        [SerializeField]
        private Vector2 imageSize = new Vector2(1.05f, 0.52f);

        [SerializeField]
        private Vector2 minimumImageSize = new Vector2(0.35f, 0.22f);

        [SerializeField]
        private bool preserveImageAspectRatio = true;

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

        private SpatialPanelStyle Style
        {
            get { return style == null ? SpatialPanelStyle.Fallback : style; }
        }

        private SpatialPanelStyle.ImagePanelStyle ImageStyle
        {
            get { return Style.ImagePanel; }
        }

        private SpatialPanelStyle.PanelChromeStyle Chrome
        {
            get { return ImageStyle.chrome; }
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
            imageSize = SpatialRenderUtility.ClampVector(imageSize, new Vector2(0.01f, 0.01f));
            minimumImageSize = SpatialRenderUtility.ClampVector(minimumImageSize, new Vector2(0.01f, 0.01f));
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

            titleText.text = SpatialRenderUtility.Wrap(step == null ? "" : step.title ?? "", GetEffectiveWrapCharacters(ImageStyle.title.characterSize));
            captionText.text = SpatialRenderUtility.Wrap(caption ?? "", GetEffectiveWrapCharacters(ImageStyle.caption.characterSize));

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

            if (!Style.HeadFollow.smoothFollow || !Application.isPlaying || !hasAppliedPose)
            {
                transform.position = targetPose.position;
                transform.rotation = targetPose.rotation;
                hasAppliedPose = true;
                return;
            }

            float t = 1f - Mathf.Exp(-Style.HeadFollow.followSharpness * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPose.position) > Style.HeadFollow.snapDistance)
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
                    SpatialRenderUtility.CreateTransparentColorMaterial(Chrome.panelColor, nameof(SpatialImagePanel)),
                    false,
                    SortingBackground);
            }

            if (accentBar == null)
            {
                accentBar = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Image Panel Accent",
                    SpatialRenderUtility.CreateTransparentColorMaterial(Style.AccentColor, nameof(SpatialImagePanel)),
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
                    ImageStyle.title.characterSize,
                    Style.TextFontSize,
                    ImageStyle.title.color,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    SortingText);
            }

            if (captionText == null)
            {
                captionText = SpatialRenderUtility.CreateText(
                    transform,
                    "Caption",
                    ImageStyle.caption.characterSize,
                    Style.TextFontSize,
                    ImageStyle.caption.color,
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
                titleText.fontSize = Style.TextFontSize;
                titleText.characterSize = ImageStyle.title.characterSize;
                titleText.color = ImageStyle.title.color;
            }

            if (captionText != null)
            {
                captionText.anchor = TextAnchor.UpperLeft;
                captionText.alignment = TextAlignment.Left;
                captionText.fontSize = Style.TextFontSize;
                captionText.characterSize = ImageStyle.caption.characterSize;
                captionText.color = ImageStyle.caption.color;
            }
        }

        private void UpdateMaterialColors()
        {
            SpatialRenderUtility.SetRendererColor(background, Chrome.panelColor);
            SpatialRenderUtility.SetRendererColor(accentBar, Style.AccentColor);
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
            float contentLeft = -actualSize.x * 0.5f + Chrome.padding.x + extraLeftInset;
            float contentTop = actualSize.y * 0.5f - Chrome.padding.y;
            float contentWidth = Mathf.Max(0.01f, actualSize.x - Chrome.padding.x * 2f - extraLeftInset);
            float contentCenterX = contentLeft + contentWidth * 0.5f;

            if (accentBar != null)
            {
                accentBar.SetActive(Chrome.showAccentBar);
                accentBar.transform.localPosition = new Vector3(
                    -actualSize.x * 0.5f + Chrome.padding.x * 0.5f,
                    0f,
                    LocalZAccent);
                accentBar.transform.localRotation = Quaternion.identity;
                accentBar.transform.localScale = new Vector3(
                    Style.AccentWidth,
                    Mathf.Max(0.01f, actualSize.y - Chrome.padding.y * 1.5f),
                    1f);
            }

            float cursorY = contentTop;
            titleText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
            if (hasTitle)
            {
                cursorY -= SpatialRenderUtility.CountLines(title) * ImageStyle.title.lineHeight;
                cursorY -= ImageStyle.titleImageGap;
            }

            Vector2 actualImageSize = CalculateImageDisplaySize(actualSize, title, caption, hasTitle, hasCaption);
            float imageCenterY = cursorY - actualImageSize.y * 0.5f;
            imageQuad.transform.localPosition = new Vector3(contentCenterX, imageCenterY, LocalZImage);
            imageQuad.transform.localRotation = Quaternion.identity;
            imageQuad.transform.localScale = new Vector3(actualImageSize.x, actualImageSize.y, 1f);
            cursorY = imageCenterY - actualImageSize.y * 0.5f;

            if (hasCaption)
            {
                cursorY -= ImageStyle.imageCaptionGap;
            }

            captionText.transform.localPosition = new Vector3(contentLeft, cursorY, Chrome.textDepthOffset);
        }

        private Vector2 CalculatePanelSize(string title, string caption, bool hasTitle, bool hasCaption)
        {
            float extraLeftInset = GetExtraLeftInset();
            Vector2 preferredImageSize = GetPreferredImageDisplaySize();

            float titleWidth = SpatialRenderUtility.LongestLineLength(title) * ImageStyle.title.characterSize * ImageStyle.estimatedCharacterWidth;
            float captionWidth = SpatialRenderUtility.LongestLineLength(caption) * ImageStyle.caption.characterSize * ImageStyle.estimatedCharacterWidth;
            float desiredContentWidth = Mathf.Max(preferredImageSize.x, Mathf.Max(titleWidth, captionWidth));
            float desiredWidth = desiredContentWidth + Chrome.padding.x * 2f + extraLeftInset;
            desiredWidth = Mathf.Clamp(desiredWidth, minimumPanelSize.x, maximumPanelSize.x);

            float desiredHeight = Chrome.padding.y * 2f + preferredImageSize.y;
            if (hasTitle)
            {
                desiredHeight += SpatialRenderUtility.CountLines(title) * ImageStyle.title.lineHeight;
                desiredHeight += ImageStyle.titleImageGap;
            }

            if (hasCaption)
            {
                desiredHeight += ImageStyle.imageCaptionGap;
                desiredHeight += SpatialRenderUtility.CountLines(caption) * ImageStyle.caption.lineHeight;
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
            float maxImageWidth = Mathf.Max(0.01f, actualPanelSize.x - Chrome.padding.x * 2f - extraLeftInset);

            float reservedHeight = Chrome.padding.y * 2f;
            if (hasTitle)
            {
                reservedHeight += SpatialRenderUtility.CountLines(title) * ImageStyle.title.lineHeight + ImageStyle.titleImageGap;
            }

            if (hasCaption)
            {
                reservedHeight += ImageStyle.imageCaptionGap + SpatialRenderUtility.CountLines(caption) * ImageStyle.caption.lineHeight;
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
            int result = Mathf.Max(1, Style.WrapCharacters);
            if (!autoSizePanel)
            {
                return result;
            }

            float contentWidth = maximumPanelSize.x - Chrome.padding.x * 2f - GetExtraLeftInset();
            float averageCharacterWidth = Mathf.Max(0.001f, characterSize * ImageStyle.estimatedCharacterWidth);
            int fitAtMaxWidth = Mathf.FloorToInt(contentWidth / averageCharacterWidth);

            if (fitAtMaxWidth > 0)
            {
                result = Mathf.Min(result, fitAtMaxWidth);
            }

            return Mathf.Max(12, result);
        }

        private float GetExtraLeftInset()
        {
            return Chrome.showAccentBar ? Style.AccentWidth + Chrome.accentGap : 0f;
        }

    }
}