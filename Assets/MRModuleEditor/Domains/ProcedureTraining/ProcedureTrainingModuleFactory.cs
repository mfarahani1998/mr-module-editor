using MRModuleEditor.Core.Layouts;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Domains.ProcedureTraining
{
    public static class ProcedureTrainingModuleFactory
    {
        public static ModuleDocument CreateEquipmentSafetyProcedureMini()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.procedure_training.equipment_safety_mini";
            document.title = "Equipment Safety Procedure Mini";
            document.description = "Phase 6 sample that proves a non-robotics ProcedureTraining domain pack can define, validate, and run its own step types.";
            document.author = "MR Module Editor";
            document.estimatedDurationSeconds = 180;

            document.objects.Add(new ModuleObject
            {
                id = "object.equipment_demo",
                label = "Equipment Demo",
                bindingKey = "Equipment Demo"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head",
                provider = AnchorProviderIds.Simulator,
                displayName = "Head Default",
                calibrationStatus = AnchorCalibrationStatuses.Ready
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.world.table",
                type = "world",
                provider = AnchorProviderIds.Simulator,
                displayName = "World Table",
                calibrationRequired = true,
                calibrationStatus = AnchorCalibrationStatuses.Approximate,
                fallbackAnchorId = "anchor.head.default"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.object.equipment",
                type = "object",
                targetObjectId = "object.equipment_demo",
                provider = AnchorProviderIds.Simulator,
                displayName = "Equipment Object",
                fallbackAnchorId = "anchor.head.default",
                calibrationStatus = AnchorCalibrationStatuses.Ready
            });

            document.layouts.Add(Layout(
                "layout.object.equipment_demo.world",
                "object.equipment_demo",
                "anchor.world.table",
                Vec(0.45f, 0f, 1.35f),
                Vec(0f, 0f, 0f),
                Vec(0.45f, 0.45f, 0.45f)));

            document.layouts.Add(Layout(
                "layout.step.001.welcome.head_panel",
                "step.001.welcome",
                "anchor.head.default",
                Vec(0f, -0.2f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.002.item.object_callout",
                "step.002.item",
                "anchor.object.equipment",
                Vec(0f, 0.75f, 0f),
                Vec(0f, 0f, 0f),
                Vec(0.75f, 0.75f, 0.75f)));

            document.layouts.Add(Layout(
                "layout.step.003.safety.head_panel",
                "step.003.safety",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.005.check.head_panel",
                "step.005.check",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            ModuleStep welcome = Step("step.001.welcome", "text", "Procedure Training Intro", 4f);
            welcome.parameters["text"] = JToken.FromObject("This Phase 6 sample uses a non-robotics ProcedureTraining domain pack. It still reuses the platform's generic panels, anchors, validation, and scene bindings.");
            welcome.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(welcome);

            ModuleStep item = Step("step.002.item", "showProcedureItem", "Locate Guard Door", 4f);
            item.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            item.parameters["itemId"] = JToken.FromObject("guard-door");
            item.parameters["instruction"] = JToken.FromObject("Find the guard door area on the equipment. The domain step reveals and highlights the procedure item.");
            item.parameters["highlight"] = JToken.FromObject(true);
            item.parameters["colorHex"] = JToken.FromObject("#FFD54F");
            item.parameters["pulseAmplitude"] = JToken.FromObject(0.05f);
            item.parameters["pulseSeconds"] = JToken.FromObject(0.8f);
            item.parameters["clearHighlightOnComplete"] = JToken.FromObject(false);
            item.parameters["resultVariableKey"] = JToken.FromObject("procedure.currentItem");
            document.steps.Add(item);

            ModuleStep safety = Step("step.003.safety", "checkSafetyPoint", "Guard Door Safety Check", 0f);
            safety.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            safety.parameters["safetyPointId"] = JToken.FromObject("guard-door-closed");
            safety.parameters["prompt"] = JToken.FromObject("Confirm that the guard door is closed before the learner proceeds.");
            safety.parameters["buttonLabel"] = JToken.FromObject("Guard Door Checked");
            safety.parameters["status"] = JToken.FromObject("Checked");
            safety.parameters["autoCompleteAfterSeconds"] = JToken.FromObject(6f);
            safety.parameters["resultVariableKey"] = JToken.FromObject("procedure.guardDoor.status");
            safety.parameters["highlightOnComplete"] = JToken.FromObject(true);
            safety.parameters["highlightColorHex"] = JToken.FromObject("#66BB6A");
            document.steps.Add(safety);

            ModuleStep callout = Step("step.004.callout", "showCallout", "Why This Matters", 4f);
            callout.parameters["text"] = JToken.FromObject("ProcedureTraining adds domain meaning, while showCallout remains a generic MR teaching primitive.");
            callout.parameters["anchorId"] = JToken.FromObject("anchor.object.equipment");
            callout.parameters["localOffset"] = Vector(0f, 0.95f, 0f);
            callout.parameters["localEuler"] = Vector(0f, 0f, 0f);
            callout.parameters["localScale"] = Vector(0.75f, 0.75f, 0.75f);
            callout.parameters["clearOnComplete"] = JToken.FromObject(true);
            document.steps.Add(callout);

            ModuleStep check = Step("step.005.check", "mcq", "Quick Procedure Check", 0f);
            check.parameters["question"] = JToken.FromObject("Which part of the system should stay generic as domains grow?");
            check.parameters["choices"] = new JArray(
                "The core validator should hard-code every domain step",
                "Domain packs should register their own definitions and handlers",
                "All domain behavior should be copied into RoboticsLite");
            check.parameters["correctIndex"] = JToken.FromObject(1);
            check.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(check);

            ModuleStep complete = Step("step.006.complete", "text", "Phase 6 Complete", 4f);
            complete.parameters["text"] = JToken.FromObject("ProcedureTraining sample complete. The new domain step names live in the domain folder, not in platform core validation/editor files.");
            complete.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(complete);

            return document;
        }

        private static ModuleStep Step(string id, string type, string title, float durationSeconds)
        {
            ModuleStep step = new ModuleStep();
            step.id = id;
            step.type = type;
            step.title = title;
            step.durationSeconds = durationSeconds;
            return step;
        }

        private static JObject Vector(float x, float y, float z)
        {
            JObject value = new JObject();
            value["x"] = x;
            value["y"] = y;
            value["z"] = z;
            return value;
        }

        private static Vector3Data Vec(float x, float y, float z)
        {
            return new Vector3Data(x, y, z);
        }

        private static LayoutDefinition Layout(string id, string targetId, string anchorId, Vector3Data position, Vector3Data rotationEuler, Vector3Data scale)
        {
            LayoutDefinition layout = new LayoutDefinition();
            layout.id = id;
            layout.targetId = targetId;
            layout.anchorId = anchorId;
            layout.position = position;
            layout.rotationEuler = rotationEuler;
            layout.scale = scale;
            ApplyLayoutDefaults(layout);
            return layout;
        }

        private static void ApplyLayoutDefaults(LayoutDefinition layout)
        {
            if (layout == null)
            {
                return;
            }

            bool targetIsObject = (layout.targetId ?? "").StartsWith("object.", System.StringComparison.Ordinal);
            if (targetIsObject)
            {
                layout.faceUser = false;
                layout.followMode = LayoutFollowModes.Fixed;
                layout.readabilityProfile = LayoutReadabilityProfiles.WorldObject;
            }
            else
            {
                layout.faceUser = true;
                layout.followMode = LayoutFollowModes.FollowAnchor;
                layout.readabilityProfile = IsObjectAnchored(layout)
                    ? LayoutReadabilityProfiles.ObjectCallout
                    : LayoutReadabilityProfiles.HeadPanel;
            }

            layout.deviceProfile = LayoutDeviceProfiles.Simulator;
        }

        private static bool IsObjectAnchored(LayoutDefinition layout)
        {
            return layout != null
                && (layout.anchorId ?? "").IndexOf(".object", System.StringComparison.Ordinal) >= 0;
        }

    }
}
