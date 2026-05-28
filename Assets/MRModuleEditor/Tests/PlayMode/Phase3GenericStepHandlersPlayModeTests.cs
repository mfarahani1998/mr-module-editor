using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Interaction;
using MRModuleEditor.Runtime.SceneBinding;
using MRModuleEditor.Runtime.StepHandlers;
using MRModuleEditor.Runtime.UI;
using MRModuleEditor.Runtime.Variables;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MRModuleEditor.Tests.PlayMode
{
    public class Phase3GenericStepHandlersPlayModeTests
    {
        [UnityTest]
        public IEnumerator SetVariableStepHandler_WritesToRuntimeVariableStore()
        {
            GameObject services = new GameObject("Variable Services");
            RuntimeVariableStore store = services.AddComponent<RuntimeVariableStore>();

            RuntimeContext context = MakeContext(MakeDocument(), null, null, store, null);
            ModuleStep step = new ModuleStep
            {
                id = "step.set",
                type = "setVariable",
                title = "Set Variable"
            };
            step.parameters["variableKey"] = JToken.FromObject("demo.status");
            step.parameters["valueType"] = JToken.FromObject("String");
            step.parameters["stringValue"] = JToken.FromObject("ready");

            yield return new SetVariableStepHandler().Execute(step, context);

            Assert.AreEqual("ready", store.GetString("demo.status", ""));
            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator WaitForSignalStepHandler_CompletesWhenMatchingSignalArrives()
        {
            GameObject services = new GameObject("Interaction Services");
            InteractionContext interaction = services.AddComponent<InteractionContext>();
            RuntimeVariableStore store = services.AddComponent<RuntimeVariableStore>();

            RuntimeContext context = MakeContext(MakeDocument(), interaction, null, store, null);
            ModuleStep step = new ModuleStep
            {
                id = "step.waitSignal",
                type = "waitForSignal",
                title = "Wait For Signal"
            };
            step.parameters["action"] = JToken.FromObject("Select");
            step.parameters["targetId"] = JToken.FromObject("target.demo");
            step.parameters["intPayload"] = JToken.FromObject(2);
            step.parameters["variableKey"] = JToken.FromObject("demo.signalReceived");

            IEnumerator routine = new WaitForSignalStepHandler().Execute(step, context);
            Assert.IsTrue(routine.MoveNext());

            interaction.Emit(InteractionSignal.Select(InteractionSource.Keyboard, "target.demo", 2));

            float timeout = Time.time + 1f;
            while (routine.MoveNext() && Time.time < timeout)
            {
                yield return routine.Current;
            }

            bool received;
            Assert.IsTrue(context.Results.TryGetStepBool("step.waitSignal", "received", out received));
            Assert.IsTrue(received);
            Assert.AreEqual("true", store.GetString("demo.signalReceived", ""));

            Object.Destroy(services);
        }

        [UnityTest]
        public IEnumerator ShowCalloutStepHandler_UsesRuntimeCalloutService()
        {
            GameObject services = new GameObject("Callout Services");
            RuntimeCalloutService callouts = services.AddComponent<RuntimeCalloutService>();

            ModuleDocument document = MakeDocument();
            RuntimeContext context = MakeContext(document, null, null, null, callouts);
            ModuleStep step = new ModuleStep
            {
                id = "step.callout",
                type = "showCallout",
                title = "Callout",
                durationSeconds = 0f
            };
            step.parameters["text"] = JToken.FromObject("Look here.");
            step.parameters["anchorId"] = JToken.FromObject("anchor.head.default");

            yield return new ShowCalloutStepHandler().Execute(step, context);

            Assert.IsNotNull(callouts);
            Object.Destroy(services);
        }

        private static RuntimeContext MakeContext(
            ModuleDocument document,
            InteractionContext interaction,
            SceneBindingRegistry sceneBindings,
            RuntimeVariableStore variables,
            RuntimeCalloutService callouts)
        {
            return new RuntimeContext(
                document,
                "",
                sceneBindings,
                null,
                null,
                null,
                new RuntimeExecutionToken(1),
                () => false,
                () => false,
                null,
                null,
                interaction,
                variables,
                callouts);
        }

        private static ModuleDocument MakeDocument()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.phase3.playmode";
            document.title = "Phase 3 PlayMode";
            document.author = "Tests";
            document.estimatedDurationSeconds = 5;
            document.anchors.Add(new AnchorDefinition { id = "anchor.head.default", type = "head" });
            return document;
        }
    }
}
