using System.Diagnostics.CodeAnalysis;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Animation.Worker;

/// <summary>Fixed public provider selection at the worker composition boundary.</summary>
internal static class RuntimeProviders
{
    public static IAnimationServices Animation()
    {
        try
        {
            return ConstructAnimation(Type.GetType("CuriousContraptions.Presentation.AnimationServices, CuriousContraptions.Animation"));
        }
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            throw new EngineProviderException(EngineProviderId.Animation, EngineProviderRefusal.ProviderUnavailable, error);
        }
    }

    internal static IAnimationServices ConstructAnimation(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? provider)
    {
        if (provider is null)
            throw new EngineProviderException(EngineProviderId.Animation, EngineProviderRefusal.ProviderUnavailable);
        if (!typeof(IAnimationServices).IsAssignableFrom(provider) || provider.IsAbstract || provider.GetConstructor(Type.EmptyTypes) is null)
            throw new EngineProviderException(EngineProviderId.Animation, EngineProviderRefusal.ProviderContractMismatch);
        try { return (IAnimationServices)Activator.CreateInstance(provider)!; }
        catch (Exception error)
        {
            throw new EngineProviderException(EngineProviderId.Animation, EngineProviderRefusal.ProviderConstructionFailed, error);
        }
    }
    public static ICoreServices Core()
    {
        try
        {
            return ConstructCore(Type.GetType("CuriousContraptions.Runtime.CoreServices, CuriousContraptions.Core"));
        }
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            throw new EngineProviderException(EngineProviderId.Core, EngineProviderRefusal.ProviderUnavailable, error);
        }
    }

    internal static ICoreServices ConstructCore(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? provider)
    {
        if (provider is null)
            throw new EngineProviderException(EngineProviderId.Core, EngineProviderRefusal.ProviderUnavailable);
        if (!typeof(ICoreServices).IsAssignableFrom(provider) || provider.IsAbstract || provider.GetConstructor(Type.EmptyTypes) is null)
            throw new EngineProviderException(EngineProviderId.Core, EngineProviderRefusal.ProviderContractMismatch);
        try { return (ICoreServices)Activator.CreateInstance(provider)!; }
        catch (Exception error)
        {
            throw new EngineProviderException(EngineProviderId.Core, EngineProviderRefusal.ProviderConstructionFailed, error);
        }
    }
}
