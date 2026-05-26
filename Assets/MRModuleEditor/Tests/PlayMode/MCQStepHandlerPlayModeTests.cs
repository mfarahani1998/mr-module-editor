using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.StepHandlers;
using MRModuleEditor.Runtime.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class MCQStepHandlerPlayModeTests
    {
        [UnityTest]
        public IEnumerator MCQStepHandler_WaitsUntilAnAnswerIsSubmitted()
        {
            GameObject services = new GameObject("MCQ Test Services");
            RuntimeDisplayPanel display = services.AddComponent<RuntimeDisplayPanel>();

            ModuleDocument module = new ModuleDocument();
            ModuleStep step = new ModuleStep
            {
                id = "step.mcq.test",
                type = "mcq",
                title = "Quick Check"
            };
            step.parameters["question"] = JToken.FromObject("Which option should be selected?");
            step.parameters["choices"] = JToken.FromObject(new[] { "A", "B", "C", "D" });
            step.parameters["correctIndex"] = JToken.FromObject(1);
            module.steps.Add(step);

            string error = "";
            RuntimeContext context = new RuntimeContext(
                module,
                "",
                null,
                display,
                null,
                null,
                new RuntimeExecutionToken(1),
                () => false,
                () => false,
                null,
                message => error = message);

            MCQStepHandler handler = new MCQStepHandler();
            IEnumerator routine = handler.Execute(step, context);

            Assert.IsTrue(routine.MoveNext(), "The MCQ coroutine should yield while waiting for input, not finish on the first tick.");
            Assert.IsFalse(display.HasMcqAnswer);
            Assert.IsTrue(string.IsNullOrEmpty(error), error);

            yield return null;

            Assert.IsTrue(routine.MoveNext(), "The MCQ coroutine should continue waiting until an answer is submitted.");
            Assert.IsFalse(display.HasMcqAnswer);

            display.SubmitMcqAnswer(99);
            Assert.IsFalse(display.HasMcqAnswer, "Out-of-range answers must not complete the MCQ.");

            display.SubmitMcqAnswer(1);
            Assert.IsTrue(display.HasMcqAnswer);
            Assert.AreEqual(1, display.SelectedMcqAnswer);

            Assert.IsTrue(routine.MoveNext(), "After a valid answer, the handler should advance to feedback instead of staying in the wait loop.");

            int selectedIndex;
            bool correct;
            string selectedChoice;

            Assert.IsTrue(context.Results.TryGetStepInt(step.id, "selectedIndex", out selectedIndex));
            Assert.AreEqual(1, selectedIndex);

            Assert.IsTrue(context.Results.TryGetStepBool(step.id, "correct", out correct));
            Assert.IsTrue(correct);

            Assert.IsTrue(context.Results.TryGetStepString(step.id, "selectedChoice", out selectedChoice));
            Assert.AreEqual("B", selectedChoice);

            Object.Destroy(services);
        }
    }
}
