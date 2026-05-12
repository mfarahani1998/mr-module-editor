using System;
using System.Collections.Generic;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class ModuleDocument
    {
        public string schemaVersion = "0.1";
        public string moduleId = "";
        public string title = "";
        public string description = "";
        public string author = "";
        public int estimatedDurationSeconds = 0;

        public List<ModuleAsset> assets = new List<ModuleAsset>();
        public List<ModuleObject> objects = new List<ModuleObject>();
        public List<AnchorDefinition> anchors = new List<AnchorDefinition>();
        public List<LayoutDefinition> layouts = new List<LayoutDefinition>();
        public List<ModuleStep> steps = new List<ModuleStep>();
    }
}