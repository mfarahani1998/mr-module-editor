using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MRModuleEditor.Authoring.Editor;
using MRModuleEditor.Core.Models;
using MRModuleEditor.Core.Validation;
using NUnit.Framework;

namespace MRModuleEditor.Tests.EditMode
{
    public class Phase4AuthoringWorkflowEditModeTests
    {
        [Test]
        public void ImportAssetFromSourceCopiesFileAndAddsModuleAsset()
        {
            string root = CreateTempRoot();
            try
            {
                string moduleFolder = Path.Combine(root, "Module");
                Directory.CreateDirectory(moduleFolder);
                string moduleJsonPath = Path.Combine(moduleFolder, "module.json");
                File.WriteAllText(moduleJsonPath, "{}");

                string sourceFolder = Path.Combine(root, "Source");
                Directory.CreateDirectory(sourceFolder);
                string sourcePath = Path.Combine(sourceFolder, "Safety Diagram.png");
                File.WriteAllText(sourcePath, "fake image bytes");

                ModuleDocument document = new ModuleDocument();
                document.assets = new List<ModuleAsset>();

                bool imported = EditorAssetImportUtility.TryImportAssetFromSource(
                    document,
                    moduleJsonPath,
                    sourcePath,
                    "image",
                    "asset.image",
                    out ModuleAsset importedAsset,
                    out string error);

                Assert.That(imported, Is.True, error);
                Assert.That(importedAsset, Is.Not.Null);
                Assert.That(importedAsset.type, Is.EqualTo("image"));
                Assert.That(importedAsset.path, Is.EqualTo("assets/images/Safety Diagram.png"));
                Assert.That(document.assets, Has.Count.EqualTo(1));
                Assert.That(File.Exists(Path.Combine(moduleFolder, "assets/images/Safety Diagram.png")), Is.True);
            }
            finally
            {
                DeleteIfExists(root);
            }
        }

        [Test]
        public void ImportAssetAlreadyInsideModuleAssetsReusesExistingFileWithoutCopying()
        {
            string root = CreateTempRoot();
            try
            {
                string moduleFolder = Path.Combine(root, "Module");
                string moduleAssetFolder = Path.Combine(moduleFolder, "assets/images");
                Directory.CreateDirectory(moduleAssetFolder);
                string moduleJsonPath = Path.Combine(moduleFolder, "module.json");
                File.WriteAllText(moduleJsonPath, "{}");

                string sourcePath = Path.Combine(moduleAssetFolder, "Intro.png");
                File.WriteAllText(sourcePath, "fake image bytes");

                ModuleDocument document = new ModuleDocument();
                document.assets = new List<ModuleAsset>();

                bool imported = EditorAssetImportUtility.TryImportAssetFromSource(
                    document,
                    moduleJsonPath,
                    sourcePath,
                    "image",
                    "asset.image",
                    out ModuleAsset importedAsset,
                    out string error);

                Assert.That(imported, Is.True, error);
                Assert.That(importedAsset, Is.Not.Null);
                Assert.That(importedAsset.path, Is.EqualTo("assets/images/Intro.png"));
                Assert.That(document.assets, Has.Count.EqualTo(1));
                Assert.That(File.Exists(Path.Combine(moduleAssetFolder, "Intro_2.png")), Is.False);

                bool importedAgain = EditorAssetImportUtility.TryImportAssetFromSource(
                    document,
                    moduleJsonPath,
                    sourcePath,
                    "image",
                    "asset.image",
                    out ModuleAsset importedAgainAsset,
                    out error);

                Assert.That(importedAgain, Is.True, error);
                Assert.That(importedAgainAsset, Is.SameAs(importedAsset));
                Assert.That(document.assets, Has.Count.EqualTo(1));
                Assert.That(File.Exists(Path.Combine(moduleAssetFolder, "Intro_2.png")), Is.False);
            }
            finally
            {
                DeleteIfExists(root);
            }
        }

        [Test]
        public void EditorValidationReportsMissingAssetFile()
        {
            string root = CreateTempRoot();
            try
            {
                string moduleFolder = Path.Combine(root, "Module");
                Directory.CreateDirectory(moduleFolder);
                string moduleJsonPath = Path.Combine(moduleFolder, "module.json");
                File.WriteAllText(moduleJsonPath, "{}");

                ModuleDocument document = MinimalValidDocument();
                document.assets.Add(new ModuleAsset
                {
                    id = "asset.missing_image",
                    type = "image",
                    path = "assets/images/missing.png",
                    label = "Missing Image"
                });

                List<ValidationIssue> issues = EditorModuleValidationUtility.CollectIssues(
                    document,
                    moduleJsonPath,
                    includeSceneBindingChecks: false);

                Assert.That(issues.Any(issue => issue.code == "asset.fileMissing"), Is.True);
            }
            finally
            {
                DeleteIfExists(root);
            }
        }

        [Test]
        public void EditorValidationReportsParentTraversalAssetPath()
        {
            string root = CreateTempRoot();
            try
            {
                string moduleFolder = Path.Combine(root, "Module");
                Directory.CreateDirectory(moduleFolder);
                string moduleJsonPath = Path.Combine(moduleFolder, "module.json");
                File.WriteAllText(moduleJsonPath, "{}");

                ModuleDocument document = MinimalValidDocument();
                document.assets.Add(new ModuleAsset
                {
                    id = "asset.bad_path",
                    type = "image",
                    path = "../outside.png",
                    label = "Bad Path"
                });

                List<ValidationIssue> issues = EditorModuleValidationUtility.CollectIssues(
                    document,
                    moduleJsonPath,
                    includeSceneBindingChecks: false);

                Assert.That(issues.Any(issue => issue.code == "asset.path.traversal"), Is.True);
            }
            finally
            {
                DeleteIfExists(root);
            }
        }

        [Test]
        public void DeleteManagedAssetFileRemovesCopiedFileButRejectsOutsidePath()
        {
            string root = CreateTempRoot();
            try
            {
                string moduleFolder = Path.Combine(root, "Module");
                Directory.CreateDirectory(Path.Combine(moduleFolder, "assets/images"));
                string moduleJsonPath = Path.Combine(moduleFolder, "module.json");
                File.WriteAllText(moduleJsonPath, "{}");

                string copiedAssetPath = Path.Combine(moduleFolder, "assets/images/intro.png");
                File.WriteAllText(copiedAssetPath, "fake image bytes");

                ModuleAsset asset = new ModuleAsset
                {
                    id = "asset.image.intro",
                    type = "image",
                    path = "assets/images/intro.png",
                    label = "Intro"
                };

                bool deleted = EditorAssetImportUtility.TryDeleteManagedAssetFile(
                    moduleJsonPath,
                    asset,
                    out string deletedPath,
                    out string deleteError);

                Assert.That(deleted, Is.True, deleteError);
                Assert.That(deletedPath.Replace("\\", "/"), Does.EndWith("assets/images/intro.png"));
                Assert.That(File.Exists(copiedAssetPath), Is.False);

                ModuleAsset outsideAsset = new ModuleAsset
                {
                    id = "asset.image.outside",
                    type = "image",
                    path = "../outside.png",
                    label = "Outside"
                };

                string ignoredResolvedPath;
                bool resolved = EditorAssetImportUtility.TryResolveManagedAssetFilePath(
                    moduleJsonPath,
                    outsideAsset,
                    out ignoredResolvedPath,
                    out string resolveError);

                Assert.That(resolved, Is.False);
                Assert.That(resolveError, Does.Contain(".."));
            }
            finally
            {
                DeleteIfExists(root);
            }
        }

        private static ModuleDocument MinimalValidDocument()
        {
            ModuleDocument document = new ModuleDocument();
            document.schemaVersion = "0.1";
            document.moduleId = "module.phase4_test";
            document.title = "Phase 4 Test";
            document.author = "Test";
            document.estimatedDurationSeconds = 5;
            document.anchors.Add(new AnchorDefinition
            {
                id = "anchor.head.default",
                type = "head"
            });
            document.steps.Add(new ModuleStep
            {
                id = "step.001",
                type = "wait",
                title = "Wait",
                durationSeconds = 0.1f
            });
            return document;
        }

        private static string CreateTempRoot()
        {
            return Path.Combine(Path.GetTempPath(), "MRModuleEditorPhase4Tests_" + Guid.NewGuid().ToString("N"));
        }

        private static void DeleteIfExists(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
