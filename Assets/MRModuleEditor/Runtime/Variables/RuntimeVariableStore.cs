using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MRModuleEditor.Runtime.Variables
{
    public class RuntimeVariableStore : MonoBehaviour, IRuntimeResettable
    {
        private struct PendingStringUpdate
        {
            public readonly string key;
            public readonly string value;

            public PendingStringUpdate(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }

        private readonly Dictionary<string, string> stringValues = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly ConcurrentQueue<PendingStringUpdate> pendingStringUpdates = new ConcurrentQueue<PendingStringUpdate>();

        public event Action<string, string> StringChanged;

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            pendingStringUpdates.Enqueue(new PendingStringUpdate(key, value ?? ""));
        }

        public void SetFloat(string key, float value, string format = "0.###")
        {
            SetString(key, value.ToString(format, CultureInfo.InvariantCulture));
        }

        public void SetInt(string key, int value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetBool(string key, bool value)
        {
            SetString(key, value ? "true" : "false");
        }

        public bool TryGetString(string key, out string value)
        {
            FlushPendingUpdates();
            value = "";

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return stringValues.TryGetValue(key, out value);
        }

        public string GetString(string key, string fallback = "")
        {
            string value;
            return TryGetString(key, out value) ? value : fallback;
        }

        public void ClearKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            FlushPendingUpdates();
            if (stringValues.Remove(key) && StringChanged != null)
            {
                StringChanged.Invoke(key, "");
            }
        }

        public void ClearAll()
        {
            while (pendingStringUpdates.TryDequeue(out _))
            {
            }

            stringValues.Clear();
        }

        public void FlushPendingUpdates()
        {
            PendingStringUpdate update;
            while (pendingStringUpdates.TryDequeue(out update))
            {
                stringValues[update.key] = update.value;
                if (StringChanged != null)
                {
                    StringChanged.Invoke(update.key, update.value);
                }
            }
        }

        public void ResetRuntimeState()
        {
            ClearAll();
        }

        private void Update()
        {
            FlushPendingUpdates();
        }
    }
}