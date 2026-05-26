using System;
using System.Collections.Generic;
using UnityEngine;

namespace MRModuleEditor.Runtime.Interaction
{
    public class InteractionContext : MonoBehaviour, IRuntimeResettable
    {
        private readonly List<InteractableTarget> activeTargets = new List<InteractableTarget>();

        public event Action<InteractionSignal> SignalEmitted;
        public event Action ActiveTargetsChanged;

        public int ActiveVersion { get; private set; }

        public int ActiveTargetCount
        {
            get
            {
                RemoveDestroyedTargets();
                return activeTargets.Count;
            }
        }

        public void RegisterTarget(InteractableTarget target)
        {
            if (target == null)
            {
                return;
            }

            RemoveDestroyedTargets();

            if (activeTargets.Contains(target))
            {
                return;
            }

            activeTargets.Add(target);
            target.MarkRegistered(this);
            BumpActiveTargetsVersion();
        }

        public void UnregisterTarget(InteractableTarget target)
        {
            if (target == null)
            {
                return;
            }

            bool removed = activeTargets.Remove(target);
            if (removed)
            {
                target.MarkUnregistered(this);
                BumpActiveTargetsVersion();
            }
        }

        public void ClearTargetsForGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return;
            }

            bool changed = false;
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                InteractableTarget target = activeTargets[i];
                if (target == null)
                {
                    activeTargets.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (target.GroupId == groupId)
                {
                    target.MarkUnregistered(this);
                    activeTargets.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                BumpActiveTargetsVersion();
            }
        }

        public void ClearAllTargets()
        {
            if (activeTargets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < activeTargets.Count; i++)
            {
                if (activeTargets[i] != null)
                {
                    activeTargets[i].MarkUnregistered(this);
                }
            }

            activeTargets.Clear();
            BumpActiveTargetsVersion();
        }

        public bool IsTargetActive(InteractableTarget target)
        {
            RemoveDestroyedTargets();
            return target != null && activeTargets.Contains(target);
        }

        public bool TryGetActiveTargetByPayload(int intPayload, out InteractableTarget result)
        {
            RemoveDestroyedTargets();

            for (int i = 0; i < activeTargets.Count; i++)
            {
                InteractableTarget target = activeTargets[i];
                if (target != null && target.IntPayload == intPayload)
                {
                    result = target;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TryEmitSelectByPayload(int intPayload, InteractionSource source)
        {
            InteractableTarget target;
            if (!TryGetActiveTargetByPayload(intPayload, out target))
            {
                return false;
            }

            EmitSelect(target, source);
            return true;
        }

        public void EmitSelect(InteractableTarget target, InteractionSource source)
        {
            if (!IsTargetActive(target))
            {
                return;
            }

            Emit(InteractionSignal.Select(source, target.TargetId, target.IntPayload));
        }

        public void EmitHoverEnter(InteractableTarget target, InteractionSource source)
        {
            EmitHover(target, InteractionAction.HoverEnter, source, 0f);
        }

        public void EmitHoverExit(InteractableTarget target, InteractionSource source)
        {
            EmitHover(target, InteractionAction.HoverExit, source, 0f);
        }

        public void EmitHoverProgress(InteractableTarget target, InteractionSource source, float progress)
        {
            EmitHover(target, InteractionAction.HoverProgress, source, progress);
        }

        public void Emit(InteractionSignal signal)
        {
            if (signal.time <= 0f)
            {
                signal.time = Time.time;
            }

            if (SignalEmitted != null)
            {
                SignalEmitted.Invoke(signal);
            }
        }

        public void ResetRuntimeState()
        {
            ClearAllTargets();
        }

        private void EmitHover(
            InteractableTarget target,
            InteractionAction action,
            InteractionSource source,
            float progress)
        {
            if (!IsTargetActive(target))
            {
                return;
            }

            Emit(InteractionSignal.Hover(
                action,
                source,
                target.TargetId,
                target.IntPayload,
                progress));
        }

        private void RemoveDestroyedTargets()
        {
            bool changed = false;
            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                if (activeTargets[i] == null)
                {
                    activeTargets.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                BumpActiveTargetsVersion();
            }
        }

        private void BumpActiveTargetsVersion()
        {
            ActiveVersion++;
            if (ActiveTargetsChanged != null)
            {
                ActiveTargetsChanged.Invoke();
            }
        }
    }
}