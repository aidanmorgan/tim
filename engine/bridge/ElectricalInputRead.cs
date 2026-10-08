using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

/// <summary>Run-local root body and enum-typed input socket, within a publication generation.</summary>
public readonly record struct ElectricalInputKey(PhysicsBodyId Owner,SocketId Port);
public enum ElectricalAvailability { Unavailable, Available }

/// <summary>Settled binary availability; no voltage, current or energy is implied.</summary>
public readonly record struct ElectricalInputRead(ElectricalInputKey Key,ElectricalAvailability Availability)
{
    internal void Validate(ReadOnlySpan<BodyPublicationRead> bodies)
    {
        if(!Enum.IsDefined(Key.Port)||!Enum.IsDefined(Availability)||Key.Owner.Index>=bodies.Length||
            bodies[Key.Owner.Index].Query.Owner!=Key.Owner)
            throw new ArgumentException("Electrical input must identify a root owner, defined socket and availability.");
    }
}
