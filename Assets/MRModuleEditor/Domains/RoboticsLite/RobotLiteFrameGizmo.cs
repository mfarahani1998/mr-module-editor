using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Domains.RoboticsLite
{
    public class RobotLiteFrameGizmo : MonoBehaviour
    {
        [SerializeField]
        private float axisLength = 0.50f;

        [SerializeField]
        private float axisWidth = 0.05f;

        [SerializeField]
        private bool visibleOnAwake = false;

        private LineRenderer xAxis;
        private LineRenderer yAxis;
        private LineRenderer zAxis;
        private bool lastVisible;

        private void Awake()
        {
            EnsureVisuals();
            SetVisible(visibleOnAwake);
        }

        public void SetSize(float length, float width)
        {
            axisLength = Mathf.Max(0.01f, length);
            axisWidth = Mathf.Max(0.001f, width);
            EnsureVisuals();
            ApplyVisualSettings();
        }

        public void SetVisible(bool visible)
        {
            lastVisible = visible;
            EnsureVisuals();

            if (xAxis != null) xAxis.gameObject.SetActive(visible);
            if (yAxis != null) yAxis.gameObject.SetActive(visible);
            if (zAxis != null) zAxis.gameObject.SetActive(visible);
        }

        private void EnsureVisuals()
        {
            xAxis = EnsureAxis(xAxis, "X Axis", Vector3.right, Color.red);
            yAxis = EnsureAxis(yAxis, "Y Axis", Vector3.up, Color.green);
            zAxis = EnsureAxis(zAxis, "Z Axis", Vector3.forward, Color.blue);
        }

        private void ApplyVisualSettings()
        {
            ConfigureAxis(xAxis, Vector3.right, Color.red);
            ConfigureAxis(yAxis, Vector3.up, Color.green);
            ConfigureAxis(zAxis, Vector3.forward, Color.blue);
        }

        private LineRenderer EnsureAxis(LineRenderer cached, string name, Vector3 direction, Color color)
        {
            if (cached == null)
            {
                cached = FindAxis(name);
            }

            if (cached == null)
            {
                GameObject axisObject = new GameObject(name);
                axisObject.transform.SetParent(transform, false);
                cached = axisObject.AddComponent<LineRenderer>();
            }

            ConfigureAxis(cached, direction, color);
            return cached;
        }

        private LineRenderer FindAxis(string axisName)
        {
            Transform child = transform.Find(axisName);
            return child == null ? null : child.GetComponent<LineRenderer>();
        }

        private void ConfigureAxis(LineRenderer line, Vector3 direction, Color color)
        {
            if (line == null)
            {
                return;
            }

            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = axisWidth;
            line.endWidth = axisWidth;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            Material material = line.sharedMaterial;
            if (material == null)
            {
                material = MakeMaterial(color);
                if (material != null)
                {
                    line.sharedMaterial = material;
                }
            }

            ApplyMaterialColor(material, color);

            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, direction.normalized * axisLength);
        }

        private static void ApplyMaterialColor(Material material, Color color)
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

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            ApplyMaterialColor(material, color);
            return material;
        }
    }
}
