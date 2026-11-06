using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuPassageSensorId(ulong Value);
public readonly record struct PassageSequence(ulong Value);

public readonly record struct PassageSensorDeclaration(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    Metres Radius,
    Metres RearmClearance)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0) throw new ArgumentException("Invalid passage sensor identity.");
        Rotation.Validate();
        PhysicsDeclarationBounds.Range(Radius.Value, 0.01f, 10f);
        PhysicsDeclarationBounds.Range(RearmClearance.Value, 0.001f, 10f);
    }
}

public enum PassageSensorPhase : uint { Unarmed, Armed, Fired }

public readonly record struct PassageSensorRead(
    GpuPassageSensorId Id,
    GpuBodyId Frame,
    GpuBodyId Body,
    GpuColliderId Collider,
    PassageSensorPhase Phase,
    uint EventOrdinal,
    float EventPhase,
    LinearSpeed Speed,
    PassageSequence Sequence);

public readonly record struct PassagePairKey(GpuPassageSensorId Sensor, GpuBodyId Body);
public readonly record struct PassageBodyState(GpuPassageSensorId Sensor, GpuBodyId Body,
    PassageSensorPhase Phase, ulong PassedCount);
