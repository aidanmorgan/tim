namespace CuriousContraptions.Gpu;

/// <summary>The engine's sockets. Each value equals the bundle's <c>PortSocket</c> of the same name, and is the wire and save
/// value.</summary>
public enum WorkshopSocket : uint { ActivationOut = 1, SetIn = 2, ResetIn = 3, ActivationIn = 4, FirstIn = 5, SecondIn = 6, Supply = 7, PowerIn = 8 }
