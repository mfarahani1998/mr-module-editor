using System;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MRModuleEditor.Core.Models
{
    [Serializable]
    public class ModuleStep
    {
        public string id = "";
        public string type = "";
        public string title = "";
        public float durationSeconds = 0f;

        // Flexible per-step data, similar to a Python dict[str, Any]
        public Dictionary<string, JToken> parameters = new Dictionary<string, JToken>();

        public bool HasParameter(string key)
        {
            return parameters != null && parameters.ContainsKey(key);
        }

        public JToken GetToken(string key)
        {
            if (parameters == null)
            {
                return null;
            }

            return parameters.TryGetValue(key, out JToken value) ? value : null;
        }

        public string GetString(string key, string fallback = "")
        {
            JToken value = GetToken(key);
            if (value == null || value.Type == JTokenType.Null)
            {
                return fallback;
            }

            if (value.Type == JTokenType.String)
            {
                return value.Value<string>();
            }

            return value.ToString();
        }

        public int GetInt(string key, int fallback = 0)
        {
            JToken value = GetToken(key);
            if (value == null || value.Type == JTokenType.Null)
            {
                return fallback;
            }

            return int.TryParse(value.ToString(), out global::System.Int32 parsed) ? parsed : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            JToken value = GetToken(key);
            if (value == null || value.Type == JTokenType.Null)
            {
                return fallback;
            }

            return bool.TryParse(value.ToString(), out bool parsed) ? parsed : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            JToken value = GetToken(key);
            if (value == null || value.Type == JTokenType.Null)
            {
                return fallback;
            }

            float parsed;
            return float.TryParse(
                value.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out parsed)
                ? parsed
                : fallback;
        }
    }
}