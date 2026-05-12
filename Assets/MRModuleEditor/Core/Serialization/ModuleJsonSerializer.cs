using System;
using System.IO;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json;

namespace MRModuleEditor.Core.Serialization
{
    public static class ModuleJsonSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static ModuleDocument Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Input JSON string is empty.");
            }

            ModuleDocument document = JsonConvert.DeserializeObject<ModuleDocument>(json, Settings);
            EnsureListsAreNotNull(document);
            return document;
        }

        public static string Serialize(ModuleDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document), "ModuleDocument cannot be null.");
            }

            EnsureListsAreNotNull(document);
            return JsonConvert.SerializeObject(document, Settings);
        }

        public static ModuleDocument LoadFromFile(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("File path is empty.", "absolutePath");
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("File not found at path: " + absolutePath);
            }

            string json = File.ReadAllText(absolutePath);
            return Deserialize(json);
        }

        public static void SaveToFile(ModuleDocument document, string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("File path is empty.", "absolutePath");
            }

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = Serialize(document);
            File.WriteAllText(absolutePath, json);
        }

        private static void EnsureListsAreNotNull(ModuleDocument document)
        {
            if (document.assets == null) document.assets = new System.Collections.Generic.List<ModuleAsset>();
            if (document.objects == null) document.objects = new System.Collections.Generic.List<ModuleObject>();
            if (document.anchors == null) document.anchors = new System.Collections.Generic.List<AnchorDefinition>();
            if (document.layouts == null) document.layouts = new System.Collections.Generic.List<LayoutDefinition>();
            if (document.steps == null) document.steps = new System.Collections.Generic.List<ModuleStep>();
        }
    }
}