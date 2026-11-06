using System.Diagnostics.CodeAnalysis;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements;

/// <summary>Fixed public provider selection at the bundle command composition boundary.</summary>
internal static class RuntimeProviders
{
    public static IBundleServices Bundle()
    {
        try
        {
            return ConstructBundle(Type.GetType("CuriousContraptions.Bundle.BundleServices, CuriousContraptions.Bundle"));
        }
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            throw new EngineProviderException(EngineProviderId.Bundle, EngineProviderRefusal.ProviderUnavailable, error);
        }
    }

    internal static IBundleServices ConstructBundle(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? provider)
    {
        if (provider is null)
            throw new EngineProviderException(EngineProviderId.Bundle, EngineProviderRefusal.ProviderUnavailable);
        if (!typeof(IBundleServices).IsAssignableFrom(provider) || provider.IsAbstract || provider.GetConstructor(Type.EmptyTypes) is null)
            throw new EngineProviderException(EngineProviderId.Bundle, EngineProviderRefusal.ProviderContractMismatch);
        try { return (IBundleServices)Activator.CreateInstance(provider)!; }
        catch (Exception error)
        {
            throw new EngineProviderException(EngineProviderId.Bundle, EngineProviderRefusal.ProviderConstructionFailed, error);
        }
    }
}
