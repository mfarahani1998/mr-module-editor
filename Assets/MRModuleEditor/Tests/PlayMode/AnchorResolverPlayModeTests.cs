using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Templates;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.SceneBinding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class AnchorResolverPlayModeTests
    {
        [UnityTest]
        public IEnumerator AnchorResolver_ResolvesObjectAnchor()
        {
            ModuleDocument document = ModuleTemplateFactory.CreateForwardKinematicsMini();

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -2f);
            cameraObject.transform.rotation = Quaternion.identity;

            GameObject services = new GameObject("Services");
            SceneBindingRegistry registry = services.AddComponent<SceneBindingRegistry>();
            AnchorResolver resolver = services.AddComponent<AnchorResolver>();

            GameObject robot = new GameObject("Robot");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";
            robot.transform.position = new Vector3(0f, 0f, 1f);

            yield return null;
            registry.Rebuild();

            Pose pose;
            string error;
            bool ok = resolver.TryResolveAnchor(document, "anchor.object.robot", out pose, out error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual(robot.transform.position, pose.position);

            Object.Destroy(robot);
            Object.Destroy(services);
            Object.Destroy(cameraObject);
        }
    }
}