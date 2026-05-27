using System.Collections;
using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class ConfirmStepHandlerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ModuleRunner_ConfirmStep_WaitsForDisplayPanelConfirmation()
        {
            string directory = Path.Combine(Application.temporaryCachePath, "MRModuleEditorConfirmTests");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "module.json").Replace("\\", "/");
            ModuleJsonSerializer.SaveToFile(MakeConfirmModule(), path);

            GameObject services = new GameObject("Confirm Runner Test Services");
            RuntimeModuleLoader loader = services.AddComponent<RuntimeModuleLoader>();
            RuntimeDisplayPanel display = services.AddComponent<RuntimeDisplayPanel>();
            ModuleRunner runner = services.AddComponent<ModuleRunner>();

            loader.LoadMode = ModuleLoadMode.AbsolutePath;
            loader.AbsoluteModulePath = path;

            yield return null;

            Assert.IsTrue(runner.LoadModule(), runner.LastError);
            runner.Play();

            float timeout = Time.time + 2f;
            while (runner.CurrentStepIndex != 0 && Time.time < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(0, runner.CurrentStepIndex);
            Assert.AreEqual(RuntimeRunnerState.Playing, runner.State);

            yield return null;
            display.SubmitConfirmation();

            timeout = Time.time + 2f;
            while (runner.State == RuntimeRunnerState.Playing && Time.time < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(RuntimeRunnerState.Completed, runner.State, runner.LastError);

            Object.Destroy(services);
            File.Delete(path);
        }

        private static ModuleDocument MakeConfirmModule()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.confirm_playmode";
            document.title = "Confirm PlayMode Test";
            document.author = "Tests";
            document.estimatedDurationSeconds = 5;

            ModuleStep confirm = new ModuleStep
            {
                id = "step.001",
                type = "confirm",
                title = "Ready Check",
                durationSeconds = 0f
            };
            confirm.parameters["message"] = JToken.FromObject("Press continue.");
            confirm.parameters["buttonLabel"] = JToken.FromObject("Continue");
            document.steps.Add(confirm);

            return document;
        }
    }
}
