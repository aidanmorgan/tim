using System;

namespace CuriousContraptions.Physics;

public enum CollisionParticipation { Enabled, Disabled }
public readonly record struct PhysicsColliderRevision(ulong Value);

/// <summary>Complete replacement of one body's collision declaration. Disabling
/// collision does not disable forces, motion or joints on that body.</summary>
public readonly record struct PhysicsColliderUpdate
{
    public PhysicsBodyId Body { get; }
    public CompoundGeometry Geometry { get; }
    public ContactMaterial Material { get; }
    public CollisionParticipation Participation { get; }
    public PhysicsColliderUpdate(PhysicsBodyId body,CompoundGeometry geometry,ContactMaterial material,
        CollisionParticipation participation)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if(!Enum.IsDefined(participation)) throw new ArgumentOutOfRangeException(nameof(participation));
        Body=body; Geometry=geometry; Material=material; Participation=participation;
    }
}
public readonly record struct PhysicsColliderState(PhysicsColliderUpdate Declaration,PhysicsColliderRevision Revision);
