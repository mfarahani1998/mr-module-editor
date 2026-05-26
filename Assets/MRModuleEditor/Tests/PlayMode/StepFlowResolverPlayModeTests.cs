using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Flow;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class StepFlowResolverPlayModeTests
    {
        [UnityTest]
        public IEnumerator StepFlowResolver_McqCorrect_UsesOnCorrectStepId()
        {
            ModuleDocument module = new ModuleDocument();
            ModuleStep step = MakeMcqStep();
            step.parameters["onCorrectStepId"] = JToken.FromObject("step.correct");
            step.parameters["onWrongStepId"] = JToken.FromObject("step.wrong");
            module.steps.Add(step);

            RuntimeContext context = MakeContext(module);
            context.Results.SetStepBool(step.id, "correct", true);

            StepFlowResolver resolver = new StepFlowResolver();
            string nextStepId = resolver.ResolveNextStepId(step, context, "step.default");

            Assert.AreEqual("step.correct", nextStepId);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StepFlowResolver_McqWrong_UsesOnWrongStepId()
        {
            ModuleDocument module = new ModuleDocument();
            ModuleStep step = MakeMcqStep();
            step.parameters["onCorrectStepId"] = JToken.FromObject("step.correct");
            step.parameters["onWrongStepId"] = JToken.FromObject("step.wrong");
            module.steps.Add(step);

            RuntimeContext context = MakeContext(module);
            context.Results.SetStepBool(step.id, "correct", false);

            StepFlowResolver resolver = new StepFlowResolver();
            string nextStepId = resolver.ResolveNextStepId(step, context, "step.default");

            Assert.AreEqual("step.wrong", nextStepId);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StepFlowResolver_GenericNextStepId_IsFallbackAfterMissingMcqBranch()
        {
            ModuleDocument module = new ModuleDocument();
            ModuleStep step = MakeMcqStep();
            step.parameters["nextStepId"] = JToken.FromObject("step.override");
            module.steps.Add(step);

            RuntimeContext context = MakeContext(module);
            context.Results.SetStepBool(step.id, "correct", true);

            StepFlowResolver resolver = new StepFlowResolver();
            string nextStepId = resolver.ResolveNextStepId(step, context, "step.default");

            Assert.AreEqual("step.override", nextStepId);
            yield return null;
        }

        private static ModuleStep MakeMcqStep()
        {
            ModuleStep step = new ModuleStep
            {
                id = "step.mcq",
                type = "mcq",
                title = "Question"
            };
            step.parameters["question"] = JToken.FromObject("Pick A.");
            step.parameters["choices"] = JToken.FromObject(new[] { "A", "B" });
            step.parameters["correctIndex"] = JToken.FromObject(0);
            return step;
        }

        private static RuntimeContext MakeContext(ModuleDocument module)
        {
            return new RuntimeContext(
                module,
                "",
                null,
                null,
                null,
                null,
                new RuntimeExecutionToken(1),
                () => false,
                () => false,
                null,
                null);
        }
    }
}