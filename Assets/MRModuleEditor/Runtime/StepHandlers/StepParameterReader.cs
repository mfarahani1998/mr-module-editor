using MRModuleEditor.Core.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MRModuleEditor.Runtime.StepHandlers
{
    public static class StepParameterReader
    {
        public static float GetDuration(ModuleStep step, float fallbackSeconds)
        {
            if (step == null)
            {
                return fallbackSeconds;
            }

            if (step.durationSeconds > 0f)
            {
                return step.durationSeconds;
            }

            return step.GetFloat("durationSeconds", fallbackSeconds);
        }

        public static Vector3 GetVector3(ModuleStep step, string key, Vector3 fallback)
        {
            if (step == null)
            {
                return fallback;
            }

            JToken token = step.GetToken(key);
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }

            float x = ReadFloat(token["x"], fallback.x);
            float y = ReadFloat(token["y"], fallback.y);
            float z = ReadFloat(token["z"], fallback.z);
            return new Vector3(x, y, z);
        }

        public static string[] GetStringArray(ModuleStep step, string key)
        {
            JToken token = step.GetToken(key);
            JArray array = token as JArray;
            if (array == null)
            {
                return new string[0];
            }

            string[] result = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                result[i] = array[i] == null ? "" : array[i].ToString();
            }

            return result;
        }

        public static bool GetBool(ModuleStep step, string key, bool fallback)
        {
            if (step == null)
            {
                return fallback;
            }

            JToken token = step.GetToken(key);
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }

            bool parsed;
            return bool.TryParse(token.ToString(), out parsed) ? parsed : fallback;
        }

        private static float ReadFloat(JToken token, float fallback)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }

            float parsed;
            return float.TryParse(
                token.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out parsed)
                ? parsed
                : fallback;
        }
    }
}