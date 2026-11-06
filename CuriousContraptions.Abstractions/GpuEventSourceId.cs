namespace CuriousContraptions.Abstractions;

/// <summary>The exact compiled producer of an occurrence, independent of its logical owner and cause.</summary>
public readonly record struct GpuEventSourceId(ulong Value);
