using System;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public readonly record struct PresentedBody(GpuBodyId Id, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation);

public interface IPhysicsMotionRead
{
    ReadOnlySpan<byte> Bytes { get; }
    int Count { get; }
    uint Substeps { get; }
    uint FirstOrdinal { get; }
    uint LastOrdinal { get; }
    void ValidateBinding(PhysicsBodyReadSet bodies, SimulationTick tick);
    bool TrySample(GpuBodyId body, PhysicalTime physicalTime, out PresentedBody pose);
}
