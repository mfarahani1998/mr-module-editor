namespace MRModuleEditor.Runtime.Interaction
{
    public interface IInteractionProvider
    {
        InteractionSource Source { get; }
        bool ProviderEnabled { get; }
    }
}