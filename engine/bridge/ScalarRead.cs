using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

public readonly record struct ScalarObservationSlot
{
    public int Index { get; }
    public ScalarObservationSlot(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum ScalarUnit { Dimensionless, GameIrradiance, GameOpticalPower, GameEnergy }
public readonly record struct ScalarReadKey(PhysicsBodyId Owner,ScalarObservationSlot Slot);

/// <summary>Discrete committed measurement in explicit units; not an interpolated physical state.</summary>
public readonly record struct ScalarRead(ScalarReadKey Key,ScalarUnit Unit,double Value)
{
    internal void Validate(ReadOnlySpan<BodyPublicationRead> bodies)
    {
        if(!Enum.IsDefined(Unit)||!double.IsFinite(Value)||Key.Owner.Index>=bodies.Length||
            bodies[Key.Owner.Index].Query.Owner!=Key.Owner)
            throw new ArgumentException("Scalar read requires a root owner, supported unit and finite value.");
    }
}
