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
    }
}