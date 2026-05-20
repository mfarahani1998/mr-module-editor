using UnityEngine;
using UnityEngine.Rendering;

namespace MRModuleEditor.Runtime.UI
{
    internal static class SpatialMaterialUtility
    {
        private static readonly string[] ColorShaderNames =
        {
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "UI/Default",
            "Unlit/Color",
            "Unlit/Transparent"
        };

        private static readonly string[] TextureShaderNames =
        {
            "Universal Render Pipeline/Unlit",
            "Sprites/Default",
            "UI/Default",
            "Unlit/Texture",
            "Unlit/Transparent"
        };

        public static Material CreateColorMaterial(Color color, string owner)
        {
            Shader shader = FindFirstShader(ColorShaderNames);
            if (shader == null)
            {
                Debug.LogError("[MRModule] " + owner + " could not find a usable color shader. "
                    + "Add Universal Render Pipeline/Unlit or Sprites/Default to Project Settings > Graphics > Always Included Shaders.");
                return null;
            }

            Material material = new Material(shader);
            ConfigureTransparent(material);
            SetMaterialColor(material, color);
            return material;
        }

        public static Material CreateTextureMaterial(string owner)
        {
            Shader shader = FindFirstShader(TextureShaderNames);
            if (shader == null)
            {
                Debug.LogError("[MRModule] " + owner + " could not find a usable texture shader. "
                    + "Add Universal Render Pipeline/Unlit or Sprites/Default to Project Settings > Graphics > Always Included Shaders.");
                return null;
            }

            Material material = new Material(shader);
            ConfigureOpaque(material);
            SetMaterialColor(material, Color.white);
            return material;
        }

        public static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static Shader FindFirstShader(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }

        private static void ConfigureTransparent(Material material)
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

        private static void ConfigureOpaque(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.renderQueue = -1;
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 1);
        }
    }
}
