namespace CuriousContraptions.Abstractions;

public enum EngineProviderId { Core, Animation, Bundle, GameBootstrap }
public enum EngineProviderRefusal { ProviderUnavailable, ProviderContractMismatch, ProviderConstructionFailed }

/// <summary>A startup boundary refusal before a session is installed.</summary>
public sealed class EngineProviderException : Exception
{
    public EngineProviderId Provider { get; }
    public EngineProviderRefusal Reason { get; }
    public EngineProviderException(EngineProviderId provider, EngineProviderRefusal reason, Exception? cause = null)
        : base($"{provider}: {reason}", cause)
    {
        if (!Enum.IsDefined(provider) || !Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        Provider = provider; Reason = reason;
    }
}

public interface IGameBootstrap
{
    IntPtr PluginsInitializer();
}
