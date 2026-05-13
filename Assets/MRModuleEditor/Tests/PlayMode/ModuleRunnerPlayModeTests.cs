using System.Collections;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.SceneBinding;
using MRModuleEditor.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class ModuleRunnerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ModuleRunner_LoadsDefaultModule()
        {
            GameObject services = new GameObject("RuntimeServices Test");
            services.AddComponent<RuntimeModuleLoader>();
            services.AddComponent<SceneBindingRegistry>();
            services.AddComponent<RuntimeDisplayPanel>();
            services.AddComponent<RuntimeControlPanel>();
            ModuleRunner runner = services.AddComponent<ModuleRunner>();

            GameObject robot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robot.name = "RobotPreview Test Object";
            BindableObject bindable = robot.AddComponent<BindableObject>();
            bindable.BindingKey = "RobotPreview";

            yield return null;

            bool loaded = runner.LoadModule();

            Assert.IsTrue(loaded, runner.LastError);
            Assert.IsNotNull(runner.CurrentModule);
            Assert.AreEqual(RuntimeRunnerState.Loaded, runner.State);

            Object.Destroy(robot);
            Object.Destroy(services);
        }
    }
}