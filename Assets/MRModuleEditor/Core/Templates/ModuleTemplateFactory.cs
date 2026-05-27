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