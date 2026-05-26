using System.IO;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleJsonSerializerTests
    {
        private static string SamplePath
        {
            get
            {
                return Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Assets",
                    ModuleFilePaths.SampleModuleRelativePathFromAssets);
            }
        }

        [Test]
        public void SampleModuleFileExists()
        {
            Assert.IsTrue(File.Exists(SamplePath), "Missing sample file: " + SamplePath);
        }

        [Test]
        public void LoadSampleModule_ReturnsExpectedTitleAndSteps()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);

            Assert.AreEqual("0.1", document.schemaVersion);
            Assert.AreEqual("module.forward_kinematics_mini", document.moduleId);
            Assert.AreEqual("Forward Kinematics Mini Demo", document.title);
            Assert.AreEqual(13, document.steps.Count);
            AssertStep(document, "step.001", "text");
            AssertStep(document, "step.001.audio", "audio");
            AssertStep(document, "step.002", "image");
            AssertStep(document, "step.003", "wait");
            AssertStep(document, "step.004", "showObject");
            AssertStep(document, "step.005", "moveObject");
            AssertStep(document, "step.006", "text");
            AssertStep(document, "step.007", "showFrame");
            AssertStep(document, "step.008", "rotateJoint");
            AssertStep(document, "step.009", "text");
            AssertStep(document, "step.010", "mcq");
            AssertStep(document, "step.011", "text");
            AssertStep(document, "step.012", "text");
        }

        [Test]
        public void LoadSampleModule_ReadsFlexibleParameters()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);
            ModuleStep firstStep = AssertStep(document, "step.001", "text");
            ModuleStep mcqStep = AssertStep(document, "step.010", "mcq");

            Assert.AreEqual(
                "Welcome to the forward kinematics mini demo.",
                firstStep.GetString("text"));

            JArray choices = mcqStep.GetToken("choices") as JArray;
            Assert.IsNotNull(choices);
            Assert.AreEqual(4, choices.Count);
            Assert.AreEqual(1, mcqStep.GetInt("correctIndex", -1));
        }

        [Test]
        public void RoundTrip_PreservesStepTypeAndParameters()
        {
            ModuleDocument original = ModuleJsonSerializer.LoadFromFile(SamplePath);
            string json = ModuleJsonSerializer.Serialize(original);
            ModuleDocument copy = ModuleJsonSerializer.Deserialize(json);
            ModuleStep originalMcq = AssertStep(original, "step.010", "mcq");
            ModuleStep copyMcq = AssertStep(copy, "step.010", "mcq");

            Assert.AreEqual(original.title, copy.title);
            Assert.AreEqual(original.steps.Count, copy.steps.Count);
            Assert.AreEqual(originalMcq.type, copyMcq.type);
            Assert.AreEqual(
                "What does forward kinematics compute?",
                copyMcq.GetString("question"));
        }

        [Test]
        public void SampleModule_ReferencedImageAssetExistsOnDisk()
        {
            ModuleDocument document = ModuleJsonSerializer.LoadFromFile(SamplePath);
            string moduleDirectory = Path.GetDirectoryName(SamplePath);

            ModuleAsset imageAsset = document.assets.Find(asset => asset != null && asset.id == "asset.intro_image");
            Assert.IsNotNull(imageAsset, "Sample module is missing asset.intro_image.");

            string imagePath = Path.Combine(moduleDirectory, imageAsset.path);
            Assert.IsTrue(File.Exists(imagePath), "Missing sample image: " + imagePath);
        }

        private static ModuleStep AssertStep(ModuleDocument document, string id, string expectedType)
        {
            ModuleStep step = document.steps.Find(candidate => candidate != null && candidate.id == id);
            Assert.IsNotNull(step, "Missing step: " + id);
            Assert.AreEqual(expectedType, step.type, "Unexpected type for step: " + id);
            return step;
        }
    }
}