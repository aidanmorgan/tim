using System.Collections.Generic;
using CuriousContraptions.Presentation;
using System;

namespace CuriousContraptions;

/// <summary>Part-declared body identity. Equality is token identity, scoped by the
/// owning part, never a name or a global component registry. Declare tokens once
/// as static readonly fields so rebuilding a part preserves its slot identity.
/// All collision children use coordinates local to this body's frame.
/// Pose providers must be pure and return a rigid pose local to the owning part.</summary>
public sealed class BodySlot
{
    private readonly Func<MachinePart,global::CuriousContraptions.Geometry.RigidPose> _pose;
    private readonly Func<MachinePart?,BodyDynamics> _dynamics;
    private readonly Func<MachinePart?,Physics.ContactMaterial> _material;
    private readonly Func<MachinePart?,IReadOnlyList<ScenePoseAsset>> _present;
    public BodyQueryPolicy QueryPolicy { get; }

    public BodySlot(Func<MachinePart,global::CuriousContraptions.Geometry.RigidPose> pose,Func<MachinePart?,BodyDynamics> dynamics,BodyQueryPolicy queryPolicy,Func<MachinePart?,Physics.ContactMaterial> material,Func<MachinePart?,IReadOnlyList<ScenePoseAsset>> present)
    {
        ArgumentNullException.ThrowIfNull(pose);
        ArgumentNullException.ThrowIfNull(dynamics);
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(present);
        if(!Enum.IsDefined(queryPolicy)) throw new ArgumentOutOfRangeException(nameof(queryPolicy));
        _pose=pose; _dynamics=dynamics; _material=material; _present=present; QueryPolicy=queryPolicy;
    }

    internal BodyDynamics Dynamics(MachinePart? owner)=>
        _dynamics(owner)??throw new InvalidOperationException("Body slot returned no dynamics declaration.");

    internal Physics.ContactMaterial Material(MachinePart? owner)=>_material(owner);

    internal IReadOnlyList<ScenePoseAsset> Presentation(MachinePart? owner)=>
        _present(owner)??throw new InvalidOperationException("Body slot returned no presentation declaration set.");

    internal global::CuriousContraptions.Geometry.RigidPose Pose(MachinePart owner)=>SceneGeometryAdapter.CaptureRigidPose(owner.Transform).Compose(_pose(owner));
}
