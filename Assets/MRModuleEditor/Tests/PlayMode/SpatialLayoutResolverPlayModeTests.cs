using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Anchors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class SpatialLayoutResolverPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResolvesTwoLayoutsAgainstSameHeadAnchor()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = Vector3.zero;
            cameraObject.transform.rotation = Quaternion.identity;

            GameObject anchorObject = new GameObject("Anchor Resolver");
            anchorObject.AddComponent<AnchorResolver>();

            GameObject resolverObject = new GameObject("Spatial Layout Resolver");
            SpatialLayoutResolver resolver = resolverObject.AddComponent<SpatialLayoutResolver>();

            ModuleDocument module = new ModuleDocument();
            module.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });

            module.layouts.Add(new LayoutDefinition
            {
                id = "layout.step.a",
                targetId = "step.a",
                anchorId = "anchor.head.default",
                position = new Vector3Data(-0.25f, 0f, 0f),
                rotationEuler = new Vector3Data(0f, 0f, 0f),
                scale = new Vector3Data(1f, 1f, 1f)
            });

            module.layouts.Add(new LayoutDefinition
            {
                id = "layout.step.b",
                targetId = "step.b",
                anchorId = "anchor.head.default",
                position = new Vector3Data(0.25f, 0f, 0f),
                rotationEuler = new Vector3Data(0f, 0f, 0f),
                scale = new Vector3Data(1f, 1f, 1f)
            });

            yield return null;

            Pose poseA;
            Vector3 scaleA;
            string errorA;
            bool okA = resolver.TryResolvePoseForTarget(
                module,
                "step.a",
                "anchor.head.default",
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                out poseA,
                out scaleA,
                out errorA);

            Pose poseB;
            Vector3 scaleB;
            string errorB;
            bool okB = resolver.TryResolvePoseForTarget(
                module,
                "step.b",
                "anchor.head.default",
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                out poseB,
                out scaleB,
                out errorB);

            Assert.IsTrue(okA, errorA);
            Assert.IsTrue(okB, errorB);
            Assert.AreNotEqual(poseA.position.x, poseB.position.x);
            Assert.AreEqual(Vector3.one, scaleA);
            Assert.AreEqual(Vector3.one, scaleB);

            Object.Destroy(cameraObject);
            Object.Destroy(anchorObject);
            Object.Destroy(resolverObject);
        }
    }
}