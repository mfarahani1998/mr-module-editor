using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.Variables;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class RuntimeVariableCallout : MonoBehaviour, IRuntimeResettable
    {
        [Header("Services")]
        [SerializeField]
        private ModuleRunner moduleRunner;

        [SerializeField]
        private AnchorResolver anchorResolver;

        [SerializeField]
        private RuntimeVariableStore variableStore;

        [Header("Variable")]
        [SerializeField]
        private string variableKey = "robot.joint3.angleText";

        [SerializeField]
        private string fallbackText = "";

        [SerializeField]
        private bool hideWhenTextIsEmpty = true;

        [Header("Placement")]
        [SerializeField]
        private string anchorId = "anchor.object.robot";

        [SerializeField]
        private Vector3 localOffset = new Vector3(0f, 1.2f, 0f);

        [SerializeField]
        private Vector3 localEuler = Vector3.zero;

        [SerializeField]
        private Vector3 localScale = new Vector3(0.75f, 0.75f, 0.75f);

        [Header("Visuals")]
        [SerializeField]
        private Vector2 panelSize = new Vector2(1.35f, 0.28f);

        [SerializeField]
        private Color panelColor = new Color(0f, 0f, 0f, 0.72f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private int fontSize = 64;

        [SerializeField]
        private float characterSize = 0.045f;

        [SerializeField]
        private int wrapCharacters = 32;

        [SerializeField]
        private float textDepthOffset = -0.01f;

        private GameObject background;
        private TextMesh textMesh;
        private string lastRenderedText = null;

        private void Awake()
        {
            EnsureServices();
            EnsureVisuals();
            SetVisualsActive(false);
        }

        private void OnValidate()
        {
            panelSize = SpatialRenderUtility.ClampVector(panelSize, new Vector2(0.1f, 0.1f));
            fontSize = Mathf.Max(1, fontSize);
            characterSize = Mathf.Max(0.001f, characterSize);
            wrapCharacters = Mathf.Max(1, wrapCharacters);

            if (background != null || textMesh != null)
            {
                ApplyVisualSettings();
            }
        }

        private void LateUpdate()
        {
            EnsureServices();
            EnsureVisuals();

            UpdateTextFromVariable();
            ApplyAnchoredPose();
        }

        public void ResetRuntimeState()
        {
            lastRenderedText = null;
            if (textMesh != null)
            {
                textMesh.text = "";
            }

            SetVisualsActive(false);
        }

        private void EnsureServices()
        {
            if (moduleRunner == null)
            {
                moduleRunner = FindFirstObjectByType<ModuleRunner>();
            }

            if (anchorResolver == null)
            {
                anchorResolver = FindFirstObjectByType<AnchorResolver>();
            }

            if (variableStore == null)
            {
                variableStore = FindFirstObjectByType<RuntimeVariableStore>(FindObjectsInactive.Include);
            }
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Callout Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(panelColor, nameof(RuntimeVariableCallout)),
                    false,
                    0);
            }

            if (textMesh == null)
            {
                textMesh = SpatialRenderUtility.CreateText(
                    transform,
                    "Callout Text",
                    characterSize,
                    fontSize,
                    textColor,
                    TextAnchor.MiddleCenter,
                    TextAlignment.Center,
                    2);
            }

            ApplyVisualSettings();
        }

        private void ApplyVisualSettings()
        {
            if (background != null)
            {
                SpatialRenderUtility.SetRendererColor(background, panelColor);
                background.transform.localPosition = Vector3.zero;
                background.transform.localRotation = Quaternion.identity;
                background.transform.localScale = new Vector3(panelSize.x, panelSize.y, 1f);
            }

            if (textMesh != null)
            {
                textMesh.fontSize = fontSize;
                textMesh.characterSize = characterSize;
                textMesh.color = textColor;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.transform.localPosition = new Vector3(0f, 0f, textDepthOffset);
                textMesh.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateTextFromVariable()
        {
            string text = fallbackText ?? "";

            if (variableStore != null)
            {
                string variableValue;
                if (variableStore.TryGetString(variableKey, out variableValue))
                {
                    text = variableValue ?? "";
                }
            }

            bool shouldShow = !hideWhenTextIsEmpty || !string.IsNullOrWhiteSpace(text);
            SetVisualsActive(shouldShow);

            if (!shouldShow || textMesh == null)
            {
                return;
            }

            if (text == lastRenderedText)
            {
                return;
            }

            lastRenderedText = text;
            textMesh.text = SpatialRenderUtility.Wrap(text, wrapCharacters);
        }

        private void SetVisualsActive(bool active)
        {
            if (background != null)
            {
                background.SetActive(active);
            }

            if (textMesh != null)
            {
                textMesh.gameObject.SetActive(active);
            }
        }

        private bool ApplyAnchoredPose()
        {
            if (moduleRunner == null || anchorResolver == null)
            {
                return false;
            }

            ModuleDocument module = moduleRunner.CurrentModule;
            if (module == null)
            {
                return false;
            }

            Pose anchorPose;
            string error;
            if (!anchorResolver.TryResolveAnchor(module, anchorId, out anchorPose, out error))
            {
                return false;
            }

            transform.position = anchorPose.position + anchorPose.rotation * localOffset;
            transform.rotation = anchorPose.rotation * Quaternion.Euler(localEuler);
            transform.localScale = localScale;
            return true;
        }
    }
}