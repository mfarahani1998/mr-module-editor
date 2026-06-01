using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class LayoutDefinition
    {
        public string id = "";
        public string targetId = ""; // step id, object id, or panel id later
        public string anchorId = "";
        public Vector3Data position = new Vector3Data();
        public Vector3Data rotationEuler = new Vector3Data();
        public Vector3Data scale = new Vector3Data(1f, 1f, 1f);

        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool faceUser = false;

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string followMode = ""; // fixed, followAnchor, smoothFollow

        [DefaultValue(0f)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float visibilityRange = 0f;

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string readabilityProfile = "";

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string deviceProfile = "";
    }
}
