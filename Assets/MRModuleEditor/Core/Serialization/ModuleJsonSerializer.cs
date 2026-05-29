using System;
using System.IO;
using System.Collections.Generic;
using MRModuleEditor.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                throw new ArgumentException("Module JSON is empty.", nameof(json));
            }

            ModuleDocument document = JsonConvert.DeserializeObject<ModuleDocument>(json, Settings);
            if (document == null)
            {
                throw new InvalidDataException("Module JSON did not produce a ModuleDocument.");
            }

            EnsureListsAreNotNull(document);
            EnsureStepParametersAreNotNull(document);
            NormalizeDeprecatedSteps(document);
            return document;
        }

        public static string Serialize(ModuleDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            EnsureListsAreNotNull(document);
            EnsureStepParametersAreNotNull(document);
            return JsonConvert.SerializeObject(document, Settings);
        }

        public static ModuleDocument LoadFromFile(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("Path is empty.", nameof(absolutePath));
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Module JSON file not found.", absolutePath);
            }

            string json = File.ReadAllText(absolutePath);
            return Deserialize(json);
        }

        public static void SaveToFile(ModuleDocument document, string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                throw new ArgumentException("Path is empty.", nameof(absolutePath));
            }

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = Serialize(document);
            File.WriteAllText(absolutePath, json);
        }

        private static void NormalizeDeprecatedSteps(ModuleDocument document)
        {
            if (document == null || document.steps == null)
            {
                return;
            }

            for (int i = 0; i < document.steps.Count; i++)
            {
                ModuleStep step = document.steps[i];
                if (step == null || step.type != "waitForSignal")
                {
                    continue;
                }

                if (step.parameters == null)
                {
                    step.parameters = new Dictionary<string, JToken>();
                }

                step.type = "confirm";
                RenameParameter(step.parameters, "action", "signalAction");
                RenameParameter(step.parameters, "targetId", "signalTargetId");
                RenameParameter(step.parameters, "intPayload", "signalIntPayload");
                RenameParameter(step.parameters, "timeoutSeconds", "autoContinueAfterSeconds");
                RenameParameter(step.parameters, "variableKey", "resultVariableKey");

                // Preserve the removed signal-waiting step defaults. Its empty target filter
                // meant "any target"; a new Confirm step defaults to its own button target.
                if (!step.parameters.ContainsKey("signalAction"))
                {
                    step.parameters["signalAction"] = JToken.FromObject("Select");
                }

                if (!step.parameters.ContainsKey("signalTargetId"))
                {
                    step.parameters["signalTargetId"] = JToken.FromObject("");
                }

                if (!step.parameters.ContainsKey("signalIntPayload"))
                {
                    step.parameters["signalIntPayload"] = JToken.FromObject(-1);
                }

                if (!step.parameters.ContainsKey("completeOnSignal"))
                {
                    step.parameters["completeOnSignal"] = JToken.FromObject(true);
                }

                if (!step.parameters.ContainsKey("message"))
                {
                    step.parameters["message"] = JToken.FromObject("Continue when the interaction is complete.");
                }

                if (!step.parameters.ContainsKey("buttonLabel"))
                {
                    step.parameters["buttonLabel"] = JToken.FromObject("Continue");
                }
            }
        }

        private static void RenameParameter(Dictionary<string, JToken> parameters, string oldKey, string newKey)
        {
            if (parameters == null || !parameters.ContainsKey(oldKey))
            {
                return;
            }

            if (!parameters.ContainsKey(newKey))
            {
                parameters[newKey] = parameters[oldKey];
            }

            parameters.Remove(oldKey);
        }

        private static void EnsureListsAreNotNull(ModuleDocument document)
        {
            if (document.assets == null) document.assets = new System.Collections.Generic.List<ModuleAsset>();
            if (document.objects == null) document.objects = new System.Collections.Generic.List<ModuleObject>();
            if (document.anchors == null) document.anchors = new System.Collections.Generic.List<AnchorDefinition>();
            if (document.layouts == null) document.layouts = new System.Collections.Generic.List<LayoutDefinition>();
            if (document.steps == null) document.steps = new System.Collections.Generic.List<ModuleStep>();
        }

        private static void EnsureStepParametersAreNotNull(ModuleDocument document)
        {
            for (int i = 0; i < document.steps.Count; i++)
            {
                if (document.steps[i] != null && document.steps[i].parameters == null)
                {
                    document.steps[i].parameters = new System.Collections.Generic.Dictionary<string, Newtonsoft.Json.Linq.JToken>();
                }
            }
        }
    }
}