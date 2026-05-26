using UnityEngine;

namespace MRModuleEditor.Runtime.Interaction
{
    public enum InteractionAction
    {
        Select,
        HoverEnter,
        HoverExit,
        HoverProgress,
        RecenterWorld
    }

    public enum InteractionSource
    {
        Keyboard,
        HeadGaze,
        ControllerRay,
        HandRay,
        EyeGaze,
        Joystick,
        Unknown
    }

    public struct InteractionSignal
    {
        public InteractionAction action;
        public InteractionSource source;
        public string targetId;
        public int intPayload;
        public float floatPayload;
        public float time;

        public static InteractionSignal Select(
            InteractionSource source,
            string targetId,
            int intPayload)
        {
            return new InteractionSignal
            {
                action = InteractionAction.Select,
                source = source,
                targetId = targetId ?? "",
                intPayload = intPayload,
                floatPayload = 1f,
                time = Time.time
            };
        }

        public static InteractionSignal Hover(
            InteractionAction action,
            InteractionSource source,
            string targetId,
            int intPayload,
            float progress)
        {
            return new InteractionSignal
            {
                action = action,
                source = source,
                targetId = targetId ?? "",
                intPayload = intPayload,
                floatPayload = Mathf.Clamp01(progress),
                time = Time.time
            };
        }
    }
}