using System;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class AnchorDefinition
    {
        public string id = "";
        public string type = "";            // head, world, object
        public string targetObjectId = "";  // only used for object anchors
    }
}