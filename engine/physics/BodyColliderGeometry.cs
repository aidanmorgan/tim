using System;
using CuriousContraptions.Physics;
namespace CuriousContraptions;

/// <summary>Installed core-local geometry binding. Body is a runtime-local ID, never a wire/save identity.</summary>
public sealed class BodyColliderGeometry
{
    public PhysicsBodyId Body { get; }
    public BodyQueryGeometry Value { get; }
    public CompoundGeometry Geometry => Value.Geometry;
    public ReadOnlySpan<ColliderQueryChild> Children => Value.Children;
    public ColliderQueryChild this[ColliderChildId child] => Value[child];
    internal ColliderQuerySet All => Value.All;
    public BodyColliderGeometry(PhysicsBodyId body, BodyQueryGeometry value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Body = body; Value = value;
    }
    public BodyColliderGeometry WithChild(ColliderChildId child, ColliderQueryChild replacement)
    {
        var changed = Value.WithChild(child, replacement);
        return ReferenceEquals(changed, Value) ? this : new(Body, changed);
    }
    internal CompoundGeometry? For(TraceMedium medium) => Value.For(medium);
}
