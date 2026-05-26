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
    public class ModuleRunnerFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator ModuleRunner_McqCorrectBranch_SkipsSlowRemediationStep()
        {
            string directory = Path.Combine(Application.temporaryCachePath, "MRModuleEditorFlowTests");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "module.json").Replace("\\", "/");
            ModuleJsonSerializer.SaveToFile(MakeBranchingModule(), path);

            GameObject services = new GameObject("Branch Runner Test Services");
            RuntimeModuleLoader loader = services.AddComponent<RuntimeModuleLoader>();
            RuntimeDisplayPanel display = services.AddComponent<RuntimeDisplayPanel>();
            ModuleRunner runner = services.AddComponent<ModuleRunner>();

            loader.LoadMode = ModuleLoadMode.AbsolutePath;
            loader.AbsoluteModulePath = path;

            yield return null;

            Assert.IsTrue(runner.LoadModule(), runner.LastError);
            runner.Play();

            float timeout = Time.time + 2f;
            while (runner.CurrentStepIndex != 1 && Time.time < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(1, runner.CurrentStepIndex, "Runner should reach the MCQ step.");

            display.SubmitMcqAnswer(0);

            timeout = Time.time + 2f;
            while (runner.State == RuntimeRunnerState.Playing && Time.time < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(RuntimeRunnerState.Completed, runner.State, runner.LastError);
            Assert.AreEqual(3, runner.CurrentStepIndex, "Correct branch should finish at the summary step and skip slow remediation.");

            Object.Destroy(services);
            File.Delete(path);
        }

        private static ModuleDocument MakeBranchingModule()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.branch_test";
            document.title = "Branch Test";
            document.author = "Tests";
            document.estimatedDurationSeconds = 10;

            ModuleStep intro = new ModuleStep
            {
                id = "step.001",
                type = "text",
                title = "Intro",
                durationSeconds = 0.05f
            };
            intro.parameters["text"] = JToken.FromObject("Intro.");
            document.steps.Add(intro);

            ModuleStep quiz = new ModuleStep
            {
                id = "step.002",
                type = "mcq",
                title = "Quiz",
                durationSeconds = 0f
            };
            quiz.parameters["question"] = JToken.FromObject("Pick A.");
            quiz.parameters["choices"] = JToken.FromObject(new[] { "A", "B" });
            quiz.parameters["correctIndex"] = JToken.FromObject(0);
            quiz.parameters["onCorrectStepId"] = JToken.FromObject("step.004");
            quiz.parameters["onWrongStepId"] = JToken.FromObject("step.003");
            document.steps.Add(quiz);

            ModuleStep remediation = new ModuleStep
            {
                id = "step.003",
                type = "text",
                title = "Slow Remediation",
                durationSeconds = 5f
            };
            remediation.parameters["text"] = JToken.FromObject("This should be skipped on a correct answer.");
            remediation.parameters["nextStepId"] = JToken.FromObject("step.004");
            document.steps.Add(remediation);

            ModuleStep summary = new ModuleStep
            {
                id = "step.004",
                type = "text",
                title = "Summary",
                durationSeconds = 0.05f
            };
            summary.parameters["text"] = JToken.FromObject("Summary.");
            document.steps.Add(summary);

            return document;
        }
    }
}