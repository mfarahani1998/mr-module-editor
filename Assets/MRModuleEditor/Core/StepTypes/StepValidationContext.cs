using System.Collections.Generic;
using MRModuleEditor.Core.Models;

namespace MRModuleEditor.Core.StepTypes
{
    public sealed class StepValidationContext
    {
        public StepValidationContext(
            ModuleDocument document,
            HashSet<string> objectIds,
            HashSet<string> assetIds,
            Dictionary<string, string> assetTypesById,
            HashSet<string> anchorIds,
            HashSet<string> stepIds)
        {
            Document = document;
            ObjectIds = objectIds ?? new HashSet<string>();
            AssetIds = assetIds ?? new HashSet<string>();
            AssetTypesById = assetTypesById ?? new Dictionary<string, string>();
            AnchorIds = anchorIds ?? new HashSet<string>();
            StepIds = stepIds ?? new HashSet<string>();
        }

        public ModuleDocument Document { get; private set; }
        public HashSet<string> ObjectIds { get; private set; }
        public HashSet<string> AssetIds { get; private set; }
        public Dictionary<string, string> AssetTypesById { get; private set; }
        public HashSet<string> AnchorIds { get; private set; }
        public HashSet<string> StepIds { get; private set; }
    }
}
