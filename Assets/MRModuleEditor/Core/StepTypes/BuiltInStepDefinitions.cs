using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.StepTypes
{
    public static class BuiltInStepDefinitions
    {
        public static void Register(StepCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            catalog.Register(new StepTypeDefinition(
                "text",
                "Text",
                "Content",
                "Shows instruction text on the runtime display/spatial panel.",
                true,
                3f,
                new[]
                {
                    new StepParameterDefinition("anchorId", "Fallback Anchor ID", StepParameterKind.AnchorId, false, "anchor.head.default"),
                    new StepParameterDefinition("text", "Text", StepParameterKind.MultilineString, true, "New instruction text.")
                }));

            catalog.Register(new StepTypeDefinition(
                "image",
                "Image",
                "Content",
                "Shows an image asset with an optional caption.",
                true,
                3f,
                new[]
                {
                    new StepParameterDefinition("assetId", "Image Asset", StepParameterKind.AssetId, true, "asset.intro_image").WithAssetType("image"),
                    new StepParameterDefinition("caption", "Caption", StepParameterKind.MultilineString, false, "Image caption.")
                }));

            catalog.Register(new StepTypeDefinition(
                "audio",
                "Audio",
                "Content",
                "Plays an audio asset, optionally showing a caption while it plays.",
                false,
                0f,
                new[]
                {
                    new StepParameterDefinition("assetId", "Audio Asset", StepParameterKind.AssetId, true, "asset.narration_intro").WithAssetType("audio"),
                    new StepParameterDefinition("waitForCompletion", "Wait For Completion", StepParameterKind.Bool, false, true),
                    new StepParameterDefinition("volume", "Volume", StepParameterKind.Float, false, 1f),
                    new StepParameterDefinition("loop", "Loop", StepParameterKind.Bool, false, false),
                    new StepParameterDefinition("spatialBlend", "Spatial Blend", StepParameterKind.Float, false, 0f),
                    new StepParameterDefinition("caption", "Caption", StepParameterKind.MultilineString, false, "Optional caption shown while narration plays."),
                    new StepParameterDefinition("anchorId", "Fallback Anchor ID", StepParameterKind.AnchorId, false, "anchor.head.default")
                },
                ValidateAudio));

            catalog.Register(new StepTypeDefinition(
                "wait",
                "Wait",
                "Flow",
                "Pauses for Duration Seconds.",
                false,
                1f,
                new StepParameterDefinition[0]));

            catalog.Register(new StepTypeDefinition(
                "confirm",
                "Confirm",
                "Flow",
                "Shows a learner-paced acknowledgement step.",
                true,
                0f,
                new[]
                {
                    new StepParameterDefinition("message", "Message", StepParameterKind.MultilineString, true, "Read this, then continue when you are ready."),
                    new StepParameterDefinition("buttonLabel", "Button Label", StepParameterKind.String, false, "Continue"),
                    new StepParameterDefinition("autoContinueAfterSeconds", "Auto Continue After Seconds", StepParameterKind.Float, false, 0f),
                    new StepParameterDefinition("anchorId", "Fallback Anchor ID", StepParameterKind.AnchorId, false, "anchor.head.default")
                }));


            catalog.Register(new StepTypeDefinition(
                "setVariable",
                "Set Variable",
                "Flow",
                "Writes a simple string, float, int, or bool value into RuntimeVariableStore.",
                false,
                0f,
                new[]
                {
                    new StepParameterDefinition("variableKey", "Variable Key", StepParameterKind.String, true, "demo.status"),
                    new StepParameterDefinition("valueType", "Value Type", StepParameterKind.Choice, false, "String").WithChoices("String", "Float", "Int", "Bool"),
                    new StepParameterDefinition("stringValue", "String Value", StepParameterKind.String, false, "ready"),
                    new StepParameterDefinition("floatValue", "Float Value", StepParameterKind.Float, false, 0f),
                    new StepParameterDefinition("intValue", "Int Value", StepParameterKind.Int, false, 0),
                    new StepParameterDefinition("boolValue", "Bool Value", StepParameterKind.Bool, false, true)
                }));

            catalog.Register(new StepTypeDefinition(
                "waitForSignal",
                "Wait For Signal",
                "Interaction",
                "Waits for a runtime InteractionContext signal such as Select or HoverEnter.",
                false,
                0f,
                new[]
                {
                    new StepParameterDefinition("action", "Action", StepParameterKind.Choice, false, "Select").WithChoices("Any", "Select", "HoverEnter", "HoverExit", "HoverProgress"),
                    new StepParameterDefinition("targetId", "Target ID Filter", StepParameterKind.String, false, ""),
                    new StepParameterDefinition("intPayload", "Int Payload Filter (-1 = any)", StepParameterKind.Int, false, -1),
                    new StepParameterDefinition("timeoutSeconds", "Timeout Seconds (0 = wait forever)", StepParameterKind.Float, false, 0f),
                    new StepParameterDefinition("variableKey", "Result Variable Key", StepParameterKind.String, false, "")
                }));

            catalog.Register(new StepTypeDefinition(
                "showObject",
                "Show Object",
                "Objects",
                "Shows or hides a bound scene object.",
                false,
                0f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.robot_preview"),
                    new StepParameterDefinition("visible", "Visible", StepParameterKind.Bool, false, true)
                }));


            catalog.Register(new StepTypeDefinition(
                "highlightObject",
                "Highlight Object",
                "Objects",
                "Applies a reversible visual emphasis to a bound scene object.",
                false,
                0f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.equipment_demo"),
                    new StepParameterDefinition("enabled", "Enabled", StepParameterKind.Bool, false, true),
                    new StepParameterDefinition("colorHex", "Color Hex", StepParameterKind.String, false, "#42A5FF"),
                    new StepParameterDefinition("pulseAmplitude", "Pulse Amplitude", StepParameterKind.Float, false, 0.08f),
                    new StepParameterDefinition("pulseSeconds", "Pulse Seconds", StepParameterKind.Float, false, 0.8f),
                    new StepParameterDefinition("clearOnComplete", "Clear On Complete", StepParameterKind.Bool, false, false)
                }));

            catalog.Register(new StepTypeDefinition(
                "showCallout",
                "Show Callout",
                "Objects",
                "Shows a small text callout attached to a head, world, or object anchor.",
                true,
                0f,
                new[]
                {
                    new StepParameterDefinition("text", "Text", StepParameterKind.MultilineString, true, "Look here."),
                    new StepParameterDefinition("anchorId", "Anchor", StepParameterKind.AnchorId, false, "anchor.head.default"),
                    new StepParameterDefinition("localOffset", "Local Offset", StepParameterKind.Vector3, false, MakeVector(0f, 0.55f, 0f)),
                    new StepParameterDefinition("localEuler", "Local Euler", StepParameterKind.Vector3, false, MakeVector(0f, 0f, 0f)),
                    new StepParameterDefinition("localScale", "Local Scale", StepParameterKind.Vector3, false, MakeVector(1f, 1f, 1f)),
                    new StepParameterDefinition("clearOnComplete", "Clear On Complete", StepParameterKind.Bool, false, false)
                }));

            catalog.Register(new StepTypeDefinition(
                "moveObject",
                "Move Object",
                "Objects",
                "Moves a bound scene object to an absolute pose or by a relative delta.",
                false,
                2f,
                new[]
                {
                    new StepParameterDefinition("objectId", "Object", StepParameterKind.ObjectId, true, "object.robot_preview"),
                    new StepParameterDefinition("isRelative", "Is Relative", StepParameterKind.Bool, false, false),
                    new StepParameterDefinition("position", "Target Local Position", StepParameterKind.Vector3, false, MakeVector(0f, 0f, 1.5f)).VisibleWhenBool("isRelative", false),
                    new StepParameterDefinition("rotationEuler", "Target Local Rotation Euler", StepParameterKind.Vector3, false, MakeVector(0f, 45f, 0f)).VisibleWhenBool("isRelative", false),
                    new StepParameterDefinition("positionDelta", "Relative Position", StepParameterKind.Vector3, false, MakeVector(0f, 0f, 0f)).VisibleWhenBool("isRelative", true),
                    new StepParameterDefinition("rotationEulerDelta", "Relative Rotation Euler", StepParameterKind.Vector3, false, MakeVector(0f, 0f, 0f)).VisibleWhenBool("isRelative", true)
                }));

            catalog.Register(new StepTypeDefinition(
                "mcq",
                "MCQ",
                "Assessment",
                "Shows a multiple-choice question and stores the selected answer.",
                true,
                0f,
                new[]
                {
                    new StepParameterDefinition("question", "Question", StepParameterKind.MultilineString, true, "What does forward kinematics compute?"),
                    new StepParameterDefinition("choices", "Choices", StepParameterKind.StringArray, false, new JArray("Joint angles from pose", "End-effector pose from joint angles")),
                    new StepParameterDefinition("correctIndex", "Correct Index", StepParameterKind.Int, false, 1),
                    new StepParameterDefinition("anchorId", "Fallback Anchor ID", StepParameterKind.AnchorId, false, "anchor.head.default")
                },
                ValidateMcq));
        }

        private static void ValidateAudio(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            if (step.GetBool("waitForCompletion", true) && step.GetBool("loop", false))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "audio.loopWait.invalid",
                    "Audio step cannot both loop and wait for completion, because the step would never finish.",
                    location));
            }
        }

        private static void ValidateMcq(ModuleStep step, StepValidationContext context, string location, List<ValidationIssue> issues)
        {
            string question = step.GetString("question", "");
            if (string.IsNullOrWhiteSpace(question))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.question.missing",
                    "MCQ step is missing question.",
                    location));
            }

            JToken choicesToken = step.GetToken("choices");
            JArray choices = choicesToken as JArray;
            if (choices == null || choices.Count == 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.choices.empty",
                    "MCQ step must contain at least one choice.",
                    location));
                return;
            }

            int correctIndex = step.GetInt("correctIndex", -1);
            if (correctIndex < 0 || correctIndex >= choices.Count)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "mcq.correctIndex.invalid",
                    "MCQ correctIndex is outside the choices array.",
                    location));
            }
        }

        private static JObject MakeVector(float x, float y, float z)
        {
            JObject vector = new JObject();
            vector["x"] = x;
            vector["y"] = y;
            vector["z"] = z;
            return vector;
        }
    }
}
