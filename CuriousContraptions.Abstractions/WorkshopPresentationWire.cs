using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Read-only scene values retained after the presentation frame was applied.</summary>
public readonly record struct PresentationScene(ulong Frame, uint PositionWrites, uint PartCount, PhysicalTime SimulationTime,
    float X, float Y, float Z, float Qx, float Qy, float Qz, float Qw);
