using System.Diagnostics.CodeAnalysis;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Web;

/// <summary>Fixed public provider selection at the web host composition boundary.</summary>
internal static class RuntimeProviders
{
    public static IGameBootstrap GameBootstrap()
    {
        try
        {
            return ConstructGameBootstrap(Type.GetType("CuriousContraptions.GameBootstrap, CuriousContraptions"));
        }
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
        {
            throw new EngineProviderException(EngineProviderId.GameBootstrap, EngineProviderRefusal.ProviderUnavailable, error);
        }
    }

    internal static IGameBootstrap ConstructGameBootstrap(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type? provider)
    {
        if (provider is null)
            throw new EngineProviderException(EngineProviderId.GameBootstrap, EngineProviderRefusal.ProviderUnavailable);
        if (!typeof(IGameBootstrap).IsAssignableFrom(provider) || provider.IsAbstract || provider.GetConstructor(Type.EmptyTypes) is null)
            throw new EngineProviderException(EngineProviderId.GameBootstrap, EngineProviderRefusal.ProviderContractMismatch);
        try { return (IGameBootstrap)Activator.CreateInstance(provider)!; }
        catch (Exception error)
        {
            throw new EngineProviderException(EngineProviderId.GameBootstrap, EngineProviderRefusal.ProviderConstructionFailed, error);
        }
    }
}
