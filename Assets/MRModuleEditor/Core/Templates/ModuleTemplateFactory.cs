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
            document.description = "A tiny sample that proves runtime step execution, image display, object binding, object motion, and MCQ feedback.";
            document.author = "Your Name";
            document.estimatedDurationSeconds = 420;

            document.assets.Add(new ModuleAsset
            {
                id = "asset.intro_image",
                type = "image",
                path = "assets/images/intro.png",
                label = "Intro Image"
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

            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.object.robot",
                type = "object",
                targetObjectId = "object.robot_preview"
            });

            ModuleStep welcome = Step("step.001", "text", "Welcome", 3f);
            welcome.parameters["text"] = JToken.FromObject("Welcome to the forward kinematics mini demo.");
            welcome.parameters["anchorId"] = JToken.FromObject("anchor.head.default");
            document.steps.Add(welcome);

            ModuleStep image = Step("step.002", "image", "Module Overview", 3f);
            image.parameters["assetId"] = JToken.FromObject("asset.intro_image");
            image.parameters["caption"] = JToken.FromObject("This placeholder image proves that the runtime can resolve an image asset from the module folder.");
            document.steps.Add(image);

            ModuleStep wait = Step("step.003", "wait", "Pause", 1f);
            document.steps.Add(wait);

            ModuleStep showRobot = Step("step.004", "showObject", "Show Robot", 0f);
            showRobot.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            showRobot.parameters["visible"] = JToken.FromObject(true);
            document.steps.Add(showRobot);

            ModuleStep moveRobot = Step("step.005", "moveObject", "Move Robot Preview", 2f);
            moveRobot.parameters["objectId"] = JToken.FromObject("object.robot_preview");
            moveRobot.parameters["position"] = Vector(0f, 0f, 1.5f);
            moveRobot.parameters["rotationEuler"] = Vector(0f, 45f, 0f);
            document.steps.Add(moveRobot);

            ModuleStep concept = Step("step.006", "text", "Concept", 3f);
            concept.parameters["text"] = JToken.FromObject("Forward kinematics computes the end-effector pose from known joint values.");
            document.steps.Add(concept);

            ModuleStep mcq = Step("step.007", "mcq", "Quick Check", 0f);
            mcq.parameters["question"] = JToken.FromObject("What does forward kinematics compute?");
            mcq.parameters["choices"] = new JArray(
                "Joint angles from pose",
                "End-effector pose from joint angles",
                "Motor current",
                "Camera calibration");
            mcq.parameters["correctIndex"] = JToken.FromObject(1);
            document.steps.Add(mcq);

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
    }
}