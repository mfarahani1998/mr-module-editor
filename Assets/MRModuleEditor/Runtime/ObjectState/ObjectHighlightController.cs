using UnityEngine;

namespace MRModuleEditor.Runtime.ObjectState
{
    public class ObjectHighlightController : MonoBehaviour, IRuntimeResettable
    {
        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private Renderer[] cachedRenderers = new Renderer[0];
        private Vector3 originalLocalScale = Vector3.one;
        private bool hasOriginalLocalScale;
        private bool highlightActive;
        private float pulseAmplitude;
        private float pulseSeconds = 0.8f;

        public static ObjectHighlightController GetOrAdd(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            ObjectHighlightController controller = target.GetComponent<ObjectHighlightController>();
            if (controller == null)
            {
                controller = target.AddComponent<ObjectHighlightController>();
            }

            return controller;
        }

        public void Apply(string colorHex, float newPulseAmplitude, float newPulseSeconds)
        {
            if (!hasOriginalLocalScale)
            {
                originalLocalScale = transform.localScale;
                hasOriginalLocalScale = true;
            }

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            pulseAmplitude = Mathf.Max(0f, newPulseAmplitude);
            pulseSeconds = Mathf.Max(0.05f, newPulseSeconds);
            highlightActive = true;

            Color tint = ParseColor(colorHex, new Color(0.25f, 0.65f, 1f, 1f));
            ApplyTint(tint);
        }

        public void Clear()
        {
            if (hasOriginalLocalScale)
            {
                transform.localScale = originalLocalScale;
            }

            ClearTint();
            highlightActive = false;
            pulseAmplitude = 0f;
        }

        public void ResetRuntimeState()
        {
            Clear();
        }

        private void Update()
        {
            if (!highlightActive || !hasOriginalLocalScale || pulseAmplitude <= 0f)
            {
                return;
            }

            float normalized = Mathf.Sin((Time.time / pulseSeconds) * Mathf.PI * 2f) * 0.5f + 0.5f;
            float scale = 1f + normalized * pulseAmplitude;
            transform.localScale = originalLocalScale * scale;
        }

        private void ApplyTint(Color tint)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                if (RendererHasProperty(renderer, "_BaseColor")) propertyBlock.SetColor("_BaseColor", tint);
                if (RendererHasProperty(renderer, "_Color")) propertyBlock.SetColor("_Color", tint);
                if (RendererHasProperty(renderer, "_EmissionColor")) propertyBlock.SetColor("_EmissionColor", tint * 0.35f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearTint()
        {
            Renderer[] renderers = cachedRenderers;
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].SetPropertyBlock(null);
                }
            }
        }

        private static bool RendererHasProperty(Renderer renderer, string propertyName)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return false;
            }

            return renderer.sharedMaterial.HasProperty(propertyName);
        }

        private static Color ParseColor(string colorHex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
            {
                return fallback;
            }

            Color parsed;
            return ColorUtility.TryParseHtmlString(colorHex, out parsed) ? parsed : fallback;
        }
    }
}
