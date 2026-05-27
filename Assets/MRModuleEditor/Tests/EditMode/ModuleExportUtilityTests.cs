using System;
using System.Collections.Generic;
using System.IO;
using MRModuleEditor.Authoring.Editor;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Serialization;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class ModuleExportUtilityTests
    {
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(
                Path.GetTempPath(),
                "MRModuleEditorExportTests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(testRoot) && Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }

        [Test]
        public void ExportModuleFolder_CopiesFilesAndExcludesMetaFiles()
        {
            string sourceFolder = CreateSourceModuleFolder();
            string moduleJsonPath = Path.Combine(sourceFolder, "module.json");

            string outputRoot = Path.Combine(testRoot, "Output");

            string exportedFolder;
            string error;
            bool ok = ModuleExportUtility.TryExportModuleFolderToRoot(
                moduleJsonPath,
                outputRoot,
                false,
                out exportedFolder,
                out error);

            Assert.IsTrue(ok, error);

            Assert.IsTrue(File.Exists(Path.Combine(exportedFolder, "module.json")));
            Assert.IsTrue(File.Exists(Path.Combine(exportedFolder, "assets/images/intro.png")));
            Assert.IsTrue(File.Exists(Path.Combine(exportedFolder, "assets/audio/intro.mp3")));

            Assert.IsFalse(File.Exists(Path.Combine(exportedFolder, "module.json.meta")));
            Assert.IsFalse(File.Exists(Path.Combine(exportedFolder, "assets/images/intro.png.meta")));
            Assert.IsFalse(File.Exists(Path.Combine(exportedFolder, "assets/audio/intro.mp3.meta")));
        }

        [Test]
        public void FindMissingReferencedAssetFiles_ReturnsMissingRelativeAssets()
        {
            string sourceFolder = CreateSourceModuleFolder();
            string moduleJsonPath = Path.Combine(sourceFolder, "module.json");

            File.Delete(Path.Combine(sourceFolder, "assets/audio/intro.mp3"));

            List<string> missing = ModuleExportUtility.FindMissingReferencedAssetFiles(moduleJsonPath);

            Assert.AreEqual(1, missing.Count);
            Assert.IsTrue(missing[0].Contains("asset.narration_intro"));
            Assert.IsTrue(missing[0].Contains("assets/audio/intro.mp3"));
        }

        [Test]
        public void TryExportModuleFolderToExactTarget_RejectsTargetInsideSourceFolder()
        {
            string sourceFolder = CreateSourceModuleFolder();
            string moduleJsonPath = Path.Combine(sourceFolder, "module.json");

            string targetInsideSource = Path.Combine(sourceFolder, "Exported");

            string error;
            bool ok = ModuleExportUtility.TryExportModuleFolderToExactTarget(
                moduleJsonPath,
                targetInsideSource,
                true,
                out error);

            Assert.IsFalse(ok);
            Assert.IsTrue(error.Contains("inside the source module folder"));
        }

        [Test]
        public void TryExportModuleFolderToExactTarget_RejectsTargetThatContainsSourceFolder()
        {
            string sourceFolder = CreateSourceModuleFolder();
            string moduleJsonPath = Path.Combine(sourceFolder, "module.json");

            string dangerousTarget = Path.GetDirectoryName(sourceFolder);

            string error;
            bool ok = ModuleExportUtility.TryExportModuleFolderToExactTarget(
                moduleJsonPath,
                dangerousTarget,
                true,
                out error);

            Assert.IsFalse(ok);
            Assert.IsTrue(error.Contains("contains the source module folder"));
        }

        private string CreateSourceModuleFolder()
        {
            string sourceFolder = Path.Combine(testRoot, "Source/ForwardKinematicsMini");

            Directory.CreateDirectory(Path.Combine(sourceFolder, "assets/images"));
            Directory.CreateDirectory(Path.Combine(sourceFolder, "assets/audio"));
            Directory.CreateDirectory(Path.Combine(sourceFolder, "assets/models"));

            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.export_test";
            document.title = "Export Test";

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

            document.steps.Add(new ModuleStep
            {
                id = "step.001",
                type = "text",
                title = "Start",
                durationSeconds = 1f
            });

            string moduleJsonPath = Path.Combine(sourceFolder, "module.json");
            ModuleJsonSerializer.SaveToFile(document, moduleJsonPath);

            File.WriteAllText(Path.Combine(sourceFolder, "module.json.meta"), "module meta");
            File.WriteAllText(Path.Combine(sourceFolder, "assets/images/intro.png"), "fake image bytes");
            File.WriteAllText(Path.Combine(sourceFolder, "assets/images/intro.png.meta"), "image meta");
            File.WriteAllText(Path.Combine(sourceFolder, "assets/audio/intro.mp3"), "fake audio bytes");
            File.WriteAllText(Path.Combine(sourceFolder, "assets/audio/intro.mp3.meta"), "audio meta");

            return sourceFolder;
        }
    }
}