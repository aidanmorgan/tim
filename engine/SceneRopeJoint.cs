using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public readonly record struct SceneRopeAnchor
{
    public SceneBodyKey Body { get; }
    public CollisionVector LocalPosition { get; }
    public SceneRopeAnchor(SceneBodyKey body,CollisionVector localPosition)
    {
        ArgumentNullException.ThrowIfNull(body.Slot);
        if(!localPosition.IsFinite) throw new ArgumentException("Rope attachment must be finite.");
        Body=body; LocalPosition=localPosition;
    }
}

/// <summary>Ordered body-local point-guide route. This declares constraints;
/// it does not copy the old endpoint-only response or approximate round sheaves.</summary>
public sealed class SceneRopeJoint : SceneJointDeclaration
{
    private readonly SceneRopeAnchor[] _anchors;
    public ReadOnlySpan<SceneRopeAnchor> Anchors=>_anchors;
    public double MaximumLength { get; }
    public ConnectedBodyCollision Collision { get; }
    public SceneRopeJoint(SceneJointKey key,IEnumerable<SceneRopeAnchor> anchors,double maximumLength,
        ConnectedBodyCollision collision):base(key)
    {
        ArgumentNullException.ThrowIfNull(anchors);
        _anchors=anchors.ToArray();
        if(_anchors.Length<2||!double.IsFinite(maximumLength)||maximumLength<=0||!Enum.IsDefined(collision))
            throw new ArgumentException("Rope requires ordered attachments, positive length and an explicit collision policy.");
        foreach(var anchor in _anchors) ArgumentNullException.ThrowIfNull(anchor.Body.Slot);
        MaximumLength=maximumLength; Collision=collision;
    }
    public override ReadOnlySpan<SceneJointKey> Dependencies=>[];
    public override PhysicsJoint Bind(PhysicsJointId id,IReadOnlyDictionary<SceneBodyKey,PhysicsBody> bodies,
        IReadOnlyDictionary<SceneJointKey,PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        var anchors=new RopeAnchor[_anchors.Length];
        for(var i=0;i<anchors.Length;i++)
        {
            var anchor=_anchors[i];
            if(!bodies.TryGetValue(anchor.Body,out var body))
                throw new ArgumentException("Rope refers to a body absent from this assembly.");
            anchors[i]=new(body,anchor.LocalPosition);
        }
        return new PhysicsRopeJoint(id,new(anchors),MaximumLength,Collision);
    }
}
