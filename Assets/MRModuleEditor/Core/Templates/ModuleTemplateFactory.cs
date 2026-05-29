using MRModuleEditor.Core.Models;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.Templates
{
    public static class ModuleTemplateFactory
    {
        public static ModuleDocument CreateEmptyModule()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.new_module";
            document.title = "New Module";
            document.description = "";
            document.author = "Your Name";
            document.estimatedDurationSeconds = 60;

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.world.table",
                type = "world"
            });

            return document;
        }

        public static ModuleDocument CreateGenericBlankLesson()
        {
            ModuleDocument document = CreateEmptyModule();
            document.moduleId = "module.new_mr_lesson";
            document.title = "New MR Lesson";
            document.description = "A small starter lesson that validates without assets or scene object bindings.";
            document.author = "Your Name";
            document.estimatedDurationSeconds = 60;

            document.layouts.Add(Layout(
                "layout.step.001.welcome.head_panel",
                "step.001.welcome",
                "anchor.head.default",
                Vec(0f, -0.2f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.002.ready.head_panel",
                "step.002.ready",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.003.check.head_panel",
                "step.003.check",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            ModuleStep welcome = Step("step.001.welcome", "text", "Welcome", 4f);
            welcome.parameters["text"] = JToken.FromObject("Write the first instruction for your lesson here.");
            welcome.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(welcome);

            ModuleStep ready = Step("step.002.ready", "confirm", "Ready Check", 0f);
            ready.parameters["message"] = JToken.FromObject("Select Continue when you are ready to start.");
            ready.parameters["buttonLabel"] = JToken.FromObject("Continue");
            ready.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(ready);

            ModuleStep check = Step("step.003.check", "mcq", "Quick Check", 0f);
            check.parameters["question"] = JToken.FromObject("Replace this question with a checkpoint for your lesson.");
            check.parameters["choices"] = new JArray("Option A", "Option B", "Option C");
            check.parameters["correctIndex"] = JToken.FromObject(0);
            check.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(check);

            ModuleStep complete = Step("step.004.complete", "text", "Complete", 4f);
            complete.parameters["text"] = JToken.FromObject("Lesson complete. Replace this with your summary.");
            complete.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(complete);

            return document;
        }

        public static ModuleDocument CreateEquipmentOrientationMini()
        {
            ModuleDocument document = CreateEmptyModule();
            document.moduleId = "module.equipment_orientation_mini";
            document.title = "Equipment Orientation Mini";
            document.description = "A non-robotics starter module that uses generic MR learning features.";
            document.author = "Your Name";
            document.estimatedDurationSeconds = 90;

            document.objects.Add(new ModuleObject
            {
                id = "object.equipment_demo",
                label = "Equipment Demo",
                bindingKey = "Equipment Demo"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.object.equipment",
                type = "object",
                targetObjectId = "object.equipment_demo"
            });

            document.layouts.Add(Layout(
                "layout.object.equipment_demo.world",
                "object.equipment_demo",
                "anchor.world.table",
                Vec(0f, 0f, 1.5f),
                Vec(0f, 0f, 0f),
                Vec(0.4f, 0.4f, 0.4f)));

            document.layouts.Add(Layout(
                "layout.step.001.welcome.head_panel",
                "step.001.welcome",
                "anchor.head.default",
                Vec(0f, -0.2f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.002.ready.head_panel",
                "step.002.ready",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.008.check.head_panel",
                "step.008.check",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            ModuleStep welcome = Step("step.001.welcome", "text", "Welcome", 4f);
            welcome.parameters["text"] = JToken.FromObject("This mini-module demonstrates generic MR learning features without using RoboticsLite.");
            welcome.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(welcome);

            ModuleStep ready = Step("step.002.ready", "confirm", "Ready Check", 0f);
            ready.parameters["message"] = JToken.FromObject("Look at the equipment model. Select Continue when you are ready.");
            ready.parameters["buttonLabel"] = JToken.FromObject("Continue");
            ready.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(ready);

            ModuleStep showEquipment = Step("step.003.showEquipment", "showObject", "Show Equipment", 0f);
            showEquipment.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            showEquipment.parameters["visible"] = JToken.FromObject(true);
            document.steps.Add(showEquipment);

            ModuleStep highlight = Step("step.004.highlight", "highlightObject", "Highlight Main Equipment", 3f);
            highlight.parameters["objectId"] = JToken.FromObject("object.equipment_demo");
            highlight.parameters["enabled"] = JToken.FromObject(true);
            highlight.parameters["colorHex"] = JToken.FromObject("#42A5FF");
            highlight.parameters["pulseAmplitude"] = JToken.FromObject(0.08f);
            highlight.parameters["pulseSeconds"] = JToken.FromObject(0.8f);
            highlight.parameters["clearOnComplete"] = JToken.FromObject(false);
            document.steps.Add(highlight);

            ModuleStep callout = Step("step.005.callout", "showCallout", "Object Callout", 4f);
            callout.parameters["text"] = JToken.FromObject("This is the inspection area.");
            callout.parameters["anchorId"] = JToken.FromObject("anchor.object.equipment");
            callout.parameters["localOffset"] = Vector(0f, 0.7f, 0f);
            callout.parameters["localEuler"] = Vector(0f, 0f, 0f);
            callout.parameters["localScale"] = Vector(0.75f, 0.75f, 0.75f);
            callout.parameters["clearOnComplete"] = JToken.FromObject(true);
            document.steps.Add(callout);

            ModuleStep setVariable = Step("step.006.setVariable", "setVariable", "Record Status", 0f);
            setVariable.parameters["variableKey"] = JToken.FromObject("equipment.status");
            setVariable.parameters["valueType"] = JToken.FromObject("String");
            setVariable.parameters["stringValue"] = JToken.FromObject("inspection area found");
            document.steps.Add(setVariable);

            ModuleStep signalAwareConfirm = Step("step.007.signalGate", "confirm", "Signal-Aware Confirm", 0f);
            signalAwareConfirm.parameters["message"] = JToken.FromObject("Select Continue, or let the short auto-continue timeout advance the module.");
            signalAwareConfirm.parameters["buttonLabel"] = JToken.FromObject("Continue");
            signalAwareConfirm.parameters["autoContinueAfterSeconds"] = JToken.FromObject(5f);
            signalAwareConfirm.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            signalAwareConfirm.parameters["completeOnSignal"] = JToken.FromObject(true);
            signalAwareConfirm.parameters["signalAction"] = JToken.FromObject("Select");
            signalAwareConfirm.parameters["signalTargetId"] = JToken.FromObject("{stepId}.confirm");
            signalAwareConfirm.parameters["signalIntPayload"] = JToken.FromObject(-1);
            signalAwareConfirm.parameters["resultVariableKey"] = JToken.FromObject("equipment.signalGate.received");
            document.steps.Add(signalAwareConfirm);

            ModuleStep check = Step("step.008.check", "mcq", "Quick Check", 0f);
            check.parameters["question"] = JToken.FromObject("What did the callout identify?");
            check.parameters["choices"] = new JArray("The inspection area", "A software menu", "The headset battery");
            check.parameters["correctIndex"] = JToken.FromObject(0);
            check.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(check);

            ModuleStep complete = Step("step.009.complete", "text", "Complete", 4f);
            complete.parameters["text"] = JToken.FromObject("Generic MR feature demo complete.");
            complete.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(complete);

            return document;
        }

        public static ModuleDocument CreateForwardKinematicsMini()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.forward_kinematics_mini";
            document.title = "Forward Kinematics Mini Demo";
            document.description = "A tiny sample that proves runtime step execution, MR-style layout, robotics-lite joint motion, frame display, and MCQ feedback.";
            document.author = "Your Name";
            document.estimatedDurationSeconds = 420;

            document.assets.Add(new ModuleAsset
            {
                id = "asset.intro_image",
                type = "image",
                path = "assets/images/intro.png",
                label = "Intro Image"
            });

            document.assets.Add(new ModuleAsset
            {
                id = "asset.narration_intro",
                type = "audio",
                path = "assets/audio/intro.mp3",
                label = "Intro Narration"
            });

            document.objects.Add(new ModuleObject
            {
                id = "object.robot_preview",
                label = "Robot Preview",
                bindingKey = "RobotPreview"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.world.table",
                type = "world"
            });

            document.layouts.Add(Layout(
                "layout.object.robot_world",
                "object.robot_preview",
                "anchor.world.table",
                Vec(-0.5f, -2f, 2f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.001.head_panel",
                "step.001",
                "anchor.head.default",
                Vec(0f, -0.9f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.001.audio_caption_head_panel",
                "step.001.audio",
                "anchor.head.default",
                Vec(0f, -0.55f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.002.head_panel",
                "step.002",
                "anchor.head.default",
                Vec(0f, -0.25f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.006.robot_callout",
                "step.006",
                "anchor.object.robot",
                Vec(0f, -0.75f, 0f),
                Vec(0f, 0f, 0f),
                Vec(0.75f, 0.75f, 0.75f)));

            document.layouts.Add(Layout(
                "layout.step.009.fk_callout",
                "step.009",
                "anchor.object.robot",
                Vec(0f, 1.5f, 0f),
                Vec(0f, 0f, 0f),
                Vec(0.75f, 0.75f, 0.75f)));

            document.layouts.Add(Layout(
                "layout.step.010.head_mcq",
                "step.010",
                "anchor.head.default",
                Vec(0f, -0.15f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.011.review_head_panel",
                "step.011",
                "anchor.head.default",
                Vec(0f, -0.55f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.layouts.Add(Layout(
                "layout.step.012.summary_head_panel",
                "step.012",
                "anchor.head.default",
                Vec(0f, -0.55f, 0f),
                Vec(0f, 0f, 0f),
                Vec(1f, 1f, 1f)));

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.object.robot",
                type = "object",
                targetObjectId = "object.robot_preview"
            });

            ModuleStep welcome = Step("step.001", "text", "Welcome", 10f);
            welcome.parameters["text"] = JToken.FromObject("Welcome to the forward kinematics mini demo.");
            welcome.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(welcome);

            ModuleStep introConfirm = Step("step.001.confirm", "confirm", "Ready Check", 0f);
            introConfirm.parameters["message"] = JToken.FromObject("When the welcome panel is readable and the scene feels centered, press Continue.");
            introConfirm.parameters["buttonLabel"] = JToken.FromObject("Continue");
            introConfirm.parameters["autoContinueAfterSeconds"] = JToken.FromObject(0f);
            introConfirm.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(introConfirm);
            
            ModuleStep introAudio = Step("step.001.audio", "audio", "Intro Narration", 0f);
            introAudio.parameters["assetId"] = JToken.FromObject("asset.narration_intro");
            introAudio.parameters["waitForCompletion"] = JToken.FromObject(true);
            introAudio.parameters["caption"] = JToken.FromObject(
                "Narration: forward kinematics computes the end-effector pose from known joint values.");
            introAudio.parameters["volume"] = JToken.FromObject(1f);
            introAudio.parameters["loop"] = JToken.FromObject(false);
            introAudio.parameters["spatialBlend"] = JToken.FromObject(0f);
            introAudio.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(introAudio);

            ModuleStep image = Step("step.002", "image", "Module Overview", 10f);
            image.parameters["assetId"] = JToken.FromObject("asset.intro_image");
            image.parameters["caption"] = JToken.FromObject("This placeholder image proves that the runtime can resolve an image asset from the module folder.");
            document.steps.Add(image);

            ModuleStep wait = Step("step.003", "wait", "Pause", 5f);
            document.steps.Add(wait);

            ModuleStep showRobot = Step("step.004", "showObject", "Show Robot", 0f);
            showRobot.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            showRobot.parameters["visible"] = JToken.FromObject(true);
            document.steps.Add(showRobot);

            ModuleStep moveRobot = Step("step.005", "moveObject", "Move Robot Preview", 3f);
            moveRobot.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            moveRobot.parameters["isRelative"] = JToken.FromObject(true);
            moveRobot.parameters["positionDelta"] = Vector(1f, 0f, 0f);
            moveRobot.parameters["rotationEulerDelta"] = Vector(0f, 45f, 0f);
            document.steps.Add(moveRobot);

            ModuleStep concept = Step("step.006", "text", "Robot Callout", 5f);
            concept.parameters["text"] = JToken.FromObject("This callout follows the robot object through anchor.object.robot.");
            concept.parameters["anchorId"] = JToken.FromObject("anchor.object.robot");
            document.steps.Add(concept);

            ModuleStep showFrame = Step("step.007", "showFrame", "Show End-Effector Frame", 1.5f);
            showFrame.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            showFrame.parameters["jointIndex"] = JToken.FromObject(2);
            showFrame.parameters["visible"] = JToken.FromObject(true);
            document.steps.Add(showFrame);

            ModuleStep rotateJoint = Step("step.008", "rotateJoint", "Rotate Joint 3", 2.5f);
            rotateJoint.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            rotateJoint.parameters["jointIndex"] = JToken.FromObject(2);
            rotateJoint.parameters["angleDegrees"] = JToken.FromObject(50f);
            rotateJoint.parameters["showFrame"] = JToken.FromObject(true);
            document.steps.Add(rotateJoint);

            ModuleStep fkIdea = Step("step.009", "text", "Forward Kinematics Idea", 5f);
            fkIdea.parameters["text"] = JToken.FromObject("Forward kinematics uses the joint angles to compute the end-effector pose.");
            fkIdea.parameters["anchorId"] = JToken.FromObject("anchor.object.robot");
            document.steps.Add(fkIdea);

            ModuleStep mcq = Step("step.010", "mcq", "Quick Check", 0f);
            mcq.parameters["question"] = JToken.FromObject("What does forward kinematics compute?");
            mcq.parameters["choices"] = new JArray(
                "Joint angles from pose",
                "End-effector pose from joint angles",
                "Motor current",
                "Camera calibration");
            mcq.parameters["correctIndex"] = JToken.FromObject(1);
            mcq.parameters["onCorrectStepId"] = JToken.FromObject("step.012");
            mcq.parameters["onWrongStepId"] = JToken.FromObject("step.011");
            document.steps.Add(mcq);

            ModuleStep review = Step("step.011", "text", "Short Review", 6f);
            review.parameters["text"] = JToken.FromObject(
                "Inverse kinematics estimates joint angles for a desired pose. " +
                "Forward kinematics goes the other direction: it computes the end-effector pose from joint angles.");
            review.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            review.parameters["nextStepId"] = JToken.FromObject("step.012");
            document.steps.Add(review);

            ModuleStep summary = Step("step.012", "text", "Summary", 6f);
            summary.parameters["text"] = JToken.FromObject(
                "Forward kinematics maps joint values to the robot end-effector pose. " +
                "After this summary, the demo resets the robot to its home pose.");
            summary.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(summary);

            ModuleStep resetRobot = Step("step.013", "resetRobot", "Reset Robot to Home", 0.5f);
            resetRobot.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            document.steps.Add(resetRobot);

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

        private static LayoutDefinition Layout(
            string id,
            string targetId,
            string anchorId,
            Vector3Data position,
            Vector3Data rotationEuler,
            Vector3Data scale)
        {
            LayoutDefinition layout = new LayoutDefinition();
            layout.id = id;
            layout.targetId = targetId;
            layout.anchorId = anchorId;
            layout.position = position;
            layout.rotationEuler = rotationEuler;
            layout.scale = scale;
            return layout;
        }

        private static Vector3Data Vec(float x, float y, float z)
        {
            return new Vector3Data(x, y, z);
        }
    }
}