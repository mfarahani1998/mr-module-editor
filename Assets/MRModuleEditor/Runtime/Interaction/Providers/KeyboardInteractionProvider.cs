using UnityEngine;

namespace MRModuleEditor.Runtime.Interaction.Providers
{
    public class KeyboardInteractionProvider : MonoBehaviour, IInteractionProvider
    {
        [SerializeField]
        private InteractionContext interactionContext;

        [SerializeField]
        private bool enableNumberKeys = true;

        [SerializeField]
        private int maxNumberKeys = 4;

        public InteractionSource Source
        {
            get { return InteractionSource.Keyboard; }
        }

        public bool ProviderEnabled
        {
            get { return isActiveAndEnabled && enableNumberKeys; }
        }

        private InteractionContext Context
        {
            get
            {
                if (interactionContext == null)
                {
                    interactionContext = FindFirstObjectByType<InteractionContext>(FindObjectsInactive.Include);
                }

                return interactionContext;
            }
        }

        private void OnValidate()
        {
            maxNumberKeys = Mathf.Clamp(maxNumberKeys, 1, 9);
        }

        private void Update()
        {
            if (!ProviderEnabled)
            {
                return;
            }

            InteractionContext context = Context;
            if (context == null || context.ActiveTargetCount == 0)
            {
                return;
            }

            int count = Mathf.Clamp(maxNumberKeys, 1, 9);
            for (int i = 0; i < count; i++)
            {
                if (IsNumberKeyDown(i))
                {
                    context.TryEmitSelectByPayload(i, Source);
                    return;
                }
            }
        }

        private static bool IsNumberKeyDown(int zeroBasedIndex)
        {
            switch (zeroBasedIndex)
            {
                case 0:
                    return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case 1:
                    return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case 2:
                    return Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case 3:
                    return Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                case 4:
                    return Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
                case 5:
                    return Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
                case 6:
                    return Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7);
                case 7:
                    return Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8);
                case 8:
                    return Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);
                default:
                    return false;
            }
        }
    }
}