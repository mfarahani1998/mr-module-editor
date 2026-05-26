using System.Collections.Generic;

namespace MRModuleEditor.Runtime.Flow
{
    public class RuntimeResultStore
    {
        private readonly Dictionary<string, int> intValues = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> boolValues = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> stringValues = new Dictionary<string, string>();

        public void Clear()
        {
            intValues.Clear();
            boolValues.Clear();
            stringValues.Clear();
        }

        public void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            intValues[key] = value;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return !string.IsNullOrWhiteSpace(key) && intValues.TryGetValue(key, out value);
        }

        public void SetBool(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            boolValues[key] = value;
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            return !string.IsNullOrWhiteSpace(key) && boolValues.TryGetValue(key, out value);
        }

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            stringValues[key] = value ?? "";
        }

        public bool TryGetString(string key, out string value)
        {
            value = "";
            return !string.IsNullOrWhiteSpace(key) && stringValues.TryGetValue(key, out value);
        }

        public void SetStepInt(string stepId, string resultName, int value)
        {
            string key;
            if (TryBuildStepKey(stepId, resultName, out key))
            {
                SetInt(key, value);
            }
        }

        public bool TryGetStepInt(string stepId, string resultName, out int value)
        {
            value = 0;

            string key;
            return TryBuildStepKey(stepId, resultName, out key)
                && TryGetInt(key, out value);
        }

        public void SetStepBool(string stepId, string resultName, bool value)
        {
            string key;
            if (TryBuildStepKey(stepId, resultName, out key))
            {
                SetBool(key, value);
            }
        }

        public bool TryGetStepBool(string stepId, string resultName, out bool value)
        {
            value = false;

            string key;
            return TryBuildStepKey(stepId, resultName, out key)
                && TryGetBool(key, out value);
        }

        public void SetStepString(string stepId, string resultName, string value)
        {
            string key;
            if (TryBuildStepKey(stepId, resultName, out key))
            {
                SetString(key, value);
            }
        }

        public bool TryGetStepString(string stepId, string resultName, out string value)
        {
            value = "";

            string key;
            return TryBuildStepKey(stepId, resultName, out key)
                && TryGetString(key, out value);
        }

        public void ClearStepResults(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                return;
            }

            string prefix = stepId + ".";
            RemoveKeysWithPrefix(intValues, prefix);
            RemoveKeysWithPrefix(boolValues, prefix);
            RemoveKeysWithPrefix(stringValues, prefix);
        }

        private static bool TryBuildStepKey(string stepId, string resultName, out string key)
        {
            key = "";

            if (string.IsNullOrWhiteSpace(stepId) || string.IsNullOrWhiteSpace(resultName))
            {
                return false;
            }

            key = stepId + "." + resultName;
            return true;
        }

        private static void RemoveKeysWithPrefix<T>(Dictionary<string, T> dictionary, string prefix)
        {
            List<string> keysToRemove = new List<string>();
            foreach (string key in dictionary.Keys)
            {
                if (key.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    keysToRemove.Add(key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                dictionary.Remove(keysToRemove[i]);
            }
        }
    }
}