using System.Collections;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Runtime;
using MRModuleEditor.Runtime.Anchors;
using MRModuleEditor.Runtime.Interaction;
using MRModuleEditor.Runtime.ObjectState;
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
        public IEnumerator ShowCalloutStepHandler_UsesSpatialTextPanel()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();

            GameObject services = new GameObject("Spatial UI Services");
            services.AddComponent<AnchorResolver>();
            services.AddComponent<SpatialLayoutResolver>();
            SpatialUIService spatialUI = services.AddComponent<SpatialUIService>();

            GameObject panelObject = new GameObject("Spatial Text Panel");
            SpatialTextPanel textPanel = panelObject.AddComponent<SpatialTextPanel>();

            ModuleDocument document = MakeDocument();
            RuntimeContext context = MakeContext(document, null, null, null, spatialUI);
            ModuleStep step = new ModuleStep
            {
                id = "step.callout",
                type = "showCallout",
                title = "Callout",
                durationSeconds = 0f
            };
            step.parameters["text"] = JToken.FromObject("Look here.");
            step.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            step.parameters["localOffset"] = JToken.FromObject(new { x = 0f, y = 0.55f, z = 0f });
            step.parameters["localEuler"] = JToken.FromObject(new { x = 0f, y = 0f, z = 0f });
            step.parameters["localScale"] = JToken.FromObject(new { x = 0.75f, y = 0.75f, z = 0.75f });

            yield return new ShowCalloutStepHandler().Execute(step, context);

            Assert.IsNotNull(textPanel);
            Assert.IsTrue(panelObject.activeSelf);
            Assert.AreEqual(0.75f, panelObject.transform.localScale.x, 0.001f);

            Object.Destroy(panelObject);
            Object.Destroy(services);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator HighlightObjectStepHandler_AddsControllerAndAppliesHighlight()
        {
            GameObject services = new GameObject("Binding Services");
            SceneBindingRegistry bindings = services.AddComponent<SceneBindingRegistry>();

            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Equipment Demo";

            BindableObject bindable = target.AddComponent<BindableObject>();
            bindable.BindingKey = "Equipment Demo";

            ModuleDocument document = MakeDocument();
            document.objects.Add(new ModuleObject
            {
                id = "object.equipment_demo",
                bindingKey = "Equipment Demo"
            });

            bindings.Rebuild();

            RuntimeContext context = MakeContext(document, null, bindings, null, null);

            ModuleStep step = new ModuleStep
            {
                id = "step.highlight",
                type = "highlightObject",
                title = "Highlight"
            };

            step.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            step.parameters["enabled"] = JToken.FromObject(true);
            step.parameters["colorHex"] = JToken.FromObject("#42A5FF");
            step.parameters["pulseAmplitude"] = JToken.FromObject(0.08f);
            step.parameters["pulseSeconds"] = JToken.FromObject(0.8f);
            step.parameters["clearOnComplete"] = JToken.FromObject(false);

            yield return new HighlightObjectStepHandler().Execute(step, context);

            Assert.IsNotNull(target.GetComponent<ObjectHighlightController>());

            Object.Destroy(target);
            Object.Destroy(services);
        }

        private static RuntimeContext MakeContext(
            ModuleDocument document,
            InteractionContext interaction,
            SceneBindingRegistry sceneBindings,
            RuntimeVariableStore variables,
            SpatialUIService spatialUI)
        {
            return new RuntimeContext(
                document,
                "",
                sceneBindings,
                null,
                null,
                spatialUI,
                new RuntimeExecutionToken(1),
                () => false,
                () => false,
                null,
                null,
                interaction,
                variables);
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
