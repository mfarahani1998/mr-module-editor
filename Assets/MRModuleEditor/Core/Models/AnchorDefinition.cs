using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class AnchorDefinition
    {
        public string id = "";
        public string type = "";            // head, world, object
        public string targetObjectId = "";  // only used for object anchors

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string provider = "";        // simulator, manual, marker, spatial later

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string displayName = "";

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string fallbackAnchorId = "";

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool calibrationRequired = false;

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string calibrationStatus = ""; // unknown, ready, approximate, lost

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string notes = "";
    }
}
