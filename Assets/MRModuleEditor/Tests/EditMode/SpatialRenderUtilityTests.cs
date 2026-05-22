using MRModuleEditor.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace MRModuleEditor.Tests.EditMode
{
    public class SpatialRenderUtilityTests
    {
        [Test]
        public void Wrap_SplitsLongTextWithoutDroppingWords()
        {
            string wrapped = SpatialRenderUtility.Wrap("alpha beta gamma delta", 10);

            Assert.IsTrue(wrapped.Contains("alpha"));
            Assert.IsTrue(wrapped.Contains("beta"));
            Assert.IsTrue(wrapped.Contains("gamma"));
            Assert.IsTrue(wrapped.Contains("delta"));
            Assert.IsTrue(wrapped.Contains("\n"));
        }

        [Test]
        public void CreateText_ConfiguresRendererSortingOrder()
        {
            GameObject root = new GameObject("Root");

            try
            {
                TextMesh text = SpatialRenderUtility.CreateText(
                    root.transform,
                    "Test Text",
                    0.02f,
                    32,
                    Color.white,
                    TextAnchor.UpperLeft,
                    TextAlignment.Left,
                    7);

                Renderer renderer = text.GetComponent<Renderer>();
                Assert.IsNotNull(renderer);
                Assert.AreEqual(7, renderer.sortingOrder);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}