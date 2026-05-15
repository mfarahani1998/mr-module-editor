using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.SceneBinding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class SceneBindingRegistryPlayModeTests
    {
        [UnityTest]
        public IEnumerator Registry_ResolvesObjectByStableModuleObjectId()
        {
            GameObject services = new GameObject("Services");
            SceneBindingRegistry registry = services.AddComponent<SceneBindingRegistry>();

            GameObject robot = new GameObject("Robot Preview Test Object");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";

            yield return null;
            registry.Rebuild();

            ModuleDocument document = new ModuleDocument();
            document.moduleId = "module.test";
            document.title = "Test";
            document.objects.Add(new ModuleObject
            {
                id = "object.robot_preview",
                label = "Robot Preview",
                bindingKey = "RobotPreview"
            });

            GameObject result;
            string error;
            bool ok = registry.TryGetObjectByModuleObjectId(
                document,
                "object.robot_preview",
                out result,
                out error);

            Assert.IsTrue(ok, error);
            Assert.AreSame(robot, result);

            Object.Destroy(robot);
            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator Registry_RestoresBindableObjectsToCapturedRuntimeBaseline()
        {
            GameObject services = new GameObject("Services");
            SceneBindingRegistry registry = services.AddComponent<SceneBindingRegistry>();

            GameObject robot = new GameObject("Robot Preview Test Object");
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";
            robot.transform.localPosition = new Vector3(1f, 2f, 3f);
            robot.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            robot.transform.localScale = new Vector3(2f, 2f, 2f);

            yield return null;
            registry.Rebuild();
            registry.CaptureRuntimeBaseline();

            robot.transform.localPosition = new Vector3(9f, 9f, 9f);
            robot.transform.localRotation = Quaternion.Euler(20f, 30f, 40f);
            robot.transform.localScale = Vector3.one * 0.25f;
            robot.SetActive(false);

            registry.ResetBindableObjectsToRuntimeBaseline();

            Assert.IsTrue(robot.activeSelf);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), robot.transform.localPosition);
            Assert.AreEqual(new Vector3(2f, 2f, 2f), robot.transform.localScale);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 45f, 0f), robot.transform.localRotation), 0.01f);

            Object.Destroy(robot);
            Object.Destroy(services);
        }
    }
}
