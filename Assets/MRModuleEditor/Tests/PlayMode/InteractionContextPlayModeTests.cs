using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime.Interaction;
using MRModuleEditor.Runtime.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class InteractionContextPlayModeTests
    {
        [UnityTest]
        public IEnumerator InteractionContext_EmitsSelectForRegisteredPayload()
        {
            GameObject contextObject = new GameObject("Interaction Context");
            InteractionContext context = contextObject.AddComponent<InteractionContext>();

            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            InteractableTarget target = targetObject.AddComponent<InteractableTarget>();
            target.Configure("step.test", "step.test.choice.1", 1);

            bool received = false;
            InteractionSignal receivedSignal = default(InteractionSignal);
            context.SignalEmitted += signal =>
            {
                received = true;
                receivedSignal = signal;
            };

            context.RegisterTarget(target);
            bool emitted = context.TryEmitSelectByPayload(1, InteractionSource.Keyboard);

            yield return null;

            Assert.IsTrue(emitted);
            Assert.IsTrue(received);
            Assert.AreEqual(InteractionAction.Select, receivedSignal.action);
            Assert.AreEqual(InteractionSource.Keyboard, receivedSignal.source);
            Assert.AreEqual("step.test.choice.1", receivedSignal.targetId);
            Assert.AreEqual(1, receivedSignal.intPayload);

            Object.Destroy(targetObject);
            Object.Destroy(contextObject);
        }

        [UnityTest]
        public IEnumerator InteractionContext_ClearTargetsForGroup_RemovesOnlyThatGroup()
        {
            GameObject contextObject = new GameObject("Interaction Context");
            InteractionContext context = contextObject.AddComponent<InteractionContext>();

            GameObject targetAObject = new GameObject("Target A");
            InteractableTarget targetA = targetAObject.AddComponent<InteractableTarget>();
            targetA.Configure("group.a", "group.a.choice.0", 0);

            GameObject targetBObject = new GameObject("Target B");
            InteractableTarget targetB = targetBObject.AddComponent<InteractableTarget>();
            targetB.Configure("group.b", "group.b.choice.0", 0);

            context.RegisterTarget(targetA);
            context.RegisterTarget(targetB);
            Assert.AreEqual(2, context.ActiveTargetCount);

            context.ClearTargetsForGroup("group.a");
            Assert.AreEqual(1, context.ActiveTargetCount);
            Assert.IsFalse(context.IsTargetActive(targetA));
            Assert.IsTrue(context.IsTargetActive(targetB));

            yield return null;

            Object.Destroy(targetAObject);
            Object.Destroy(targetBObject);
            Object.Destroy(contextObject);
        }

        [UnityTest]
        public IEnumerator SpatialMCQPanel_SubmitsAnswerFromInteractionSignal()
        {
            GameObject contextObject = new GameObject("Interaction Context");
            InteractionContext context = contextObject.AddComponent<InteractionContext>();

            GameObject panelObject = new GameObject("Spatial MCQ Panel");
            SpatialMCQPanel panel = panelObject.AddComponent<SpatialMCQPanel>();

            ModuleDocument module = new ModuleDocument();
            ModuleStep step = new ModuleStep
            {
                id = "step.mcq.test",
                type = "mcq",
                title = "Quick Check"
            };
            step.parameters["question"] = JToken.FromObject("Pick B.");
            step.parameters["choices"] = JToken.FromObject(new[] { "A", "B", "C", "D" });
            step.parameters["correctIndex"] = JToken.FromObject(1);
            module.steps.Add(step);

            yield return null;

            panel.ShowMCQ(
                module,
                step,
                "Pick B.",
                new[] { "A", "B", "C", "D" },
                1);

            yield return null;

            Assert.IsFalse(panel.HasAnswer);
            Assert.AreEqual(4, context.ActiveTargetCount);

            bool emitted = context.TryEmitSelectByPayload(1, InteractionSource.Keyboard);
            Assert.IsTrue(emitted);

            yield return null;

            Assert.IsTrue(panel.HasAnswer);
            Assert.AreEqual(1, panel.SelectedAnswer);

            Object.Destroy(panelObject);
            Object.Destroy(contextObject);
        }
    }
}