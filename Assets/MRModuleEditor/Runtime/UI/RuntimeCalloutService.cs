using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using UnityEngine;

namespace MRModuleEditor.Runtime.UI
{
    public class RuntimeCalloutService : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField]
        private AnchorResolver anchorResolver;

        [Header("Visuals")]
        [SerializeField]
        private Vector2 panelSize = new Vector2(1.25f, 0.32f);

        [SerializeField]
        private Color panelColor = new Color(0.03f, 0.03f, 0.03f, 0.82f);

        [SerializeField]
        private Color textColor = Color.white;

        [SerializeField]
        private int fontSize = 56;

        [SerializeField]
        private float characterSize = 0.035f;

        [SerializeField]
        private int wrapCharacters = 36;

        [SerializeField]
        private float textDepthOffset = 0.02f;

        private GameObject background;
        private TextMesh textMesh;
        private ModuleDocument currentModule;
        private string showingStepId = "";
        private string anchorId = "anchor.head.default";
        private Vector3 localOffset = new Vector3(0f, 0.55f, 0f);
        private Vector3 localEuler = Vector3.zero;
        private Vector3 localScale = Vector3.one;

        private AnchorResolver AnchorResolver
        {
            get
            {
                if (anchorResolver == null)
                {
                    anchorResolver = FindFirstObjectByType<AnchorResolver>();
                }

                return anchorResolver;
            }
        }

        private void Awake()
        {
            EnsureVisuals();
            ClearAll();
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

        public void ShowCallout(
            ModuleDocument module,
            ModuleStep step,
            string text,
            string newAnchorId,
            Vector3 newLocalOffset,
            Vector3 newLocalEuler,
            Vector3 newLocalScale)
        {
            EnsureVisuals();

            currentModule = module;
            showingStepId = step == null ? "" : step.id;
            anchorId = string.IsNullOrWhiteSpace(newAnchorId) ? "anchor.head.default" : newAnchorId;
            localOffset = newLocalOffset;
            localEuler = newLocalEuler;
            localScale = newLocalScale == Vector3.zero ? Vector3.one : newLocalScale;

            textMesh.text = SpatialRenderUtility.Wrap(text ?? "", wrapCharacters);
            SetVisualsActive(true);
            ApplyAnchoredPose();
        }

        public void ClearStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || showingStepId != stepId)
            {
                return;
            }

            ClearAll();
        }

        public void ClearAll()
        {
            currentModule = null;
            showingStepId = "";
            if (textMesh != null)
            {
                textMesh.text = "";
            }

            SetVisualsActive(false);
        }

        public void ResetRuntimeState()
        {
            ClearAll();
        }

        private void LateUpdate()
        {
            ApplyAnchoredPose();
        }

        private bool ApplyAnchoredPose()
        {
            if (currentModule == null)
            {
                return false;
            }

            AnchorResolver resolver = AnchorResolver;
            if (resolver == null)
            {
                return false;
            }

            Pose anchorPose;
            string error;
            if (!resolver.TryResolveAnchor(currentModule, anchorId, out anchorPose, out error))
            {
                return false;
            }

            transform.position = anchorPose.position + anchorPose.rotation * localOffset;
            transform.rotation = anchorPose.rotation * Quaternion.Euler(localEuler);
            transform.localScale = localScale;
            return true;
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = SpatialRenderUtility.CreateQuad(
                    transform,
                    "Callout Background",
                    SpatialRenderUtility.CreateTransparentColorMaterial(panelColor, nameof(RuntimeCalloutService)),
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

        private void SetVisualsActive(bool active)
        {
            if (background != null) background.SetActive(active);
            if (textMesh != null) textMesh.gameObject.SetActive(active);
        }
    }
}
