using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Normal geometry and material-surface velocity maps. Tangent maps may
/// include a finite-mass transmission body as well as the two geometric owners.
/// The transpose of each map applies the reaction to every participating body.</summary>
public sealed class ContactKinematics
{
    public CollisionVector Normal { get; }
    public CollisionVector U { get; }
    public CollisionVector V { get; }
    public ConstraintGradient NormalGradient { get; }
    public ConstraintGradient TangentU { get; }
    public ConstraintGradient TangentV { get; }
    private readonly PhysicsBody[] _bodies;
    private readonly ulong[] _revisions;
    public ReadOnlySpan<PhysicsBody> Bodies => _bodies;
    public CollisionVector Slip
    {
        get { ValidatePose(); return U * TangentU.Speed + V * TangentV.Speed; }
    }
    /// <summary>Physical material slip contracted from immutable captured motion.</summary>
    public CollisionVector SlipAlong(IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double time)
    {
        ValidatePose();
        return U*TangentU.SpeedAlong(paths,time)+V*TangentV.SpeedAlong(paths,time);
    }

    internal void ValidatePose()
    {
        for (var i = 0; i < _bodies.Length; i++)
            if (_bodies[i].PoseRevision != _revisions[i])
                throw new InvalidOperationException("Contact material geometry is stale after a participant pose change.");
    }

    public static (CollisionVector U, CollisionVector V) Axes(CollisionVector normal)
    {
        if (!normal.IsFinite || Math.Abs(normal.LengthSquared - 1) > 1e-10)
            throw new ArgumentException("Contact normal must be finite and unit length.", nameof(normal));
        var seed = Math.Abs(normal.X) < .5773502691896258 ? new CollisionVector(1, 0, 0) :
            Math.Abs(normal.Y) < .5773502691896258 ? new CollisionVector(0, 1, 0) : new CollisionVector(0, 0, 1);
        var tangent = CollisionVector.Cross(normal, seed);
        var u = tangent / tangent.Length;
        return (u, CollisionVector.Cross(normal, u));
    }

    public ContactKinematics(CollisionVector normal, ConstraintGradient normalGradient,
        ConstraintGradient tangentU, ConstraintGradient tangentV)
    {
        ArgumentNullException.ThrowIfNull(normalGradient);
        ArgumentNullException.ThrowIfNull(tangentU);
        ArgumentNullException.ThrowIfNull(tangentV);
        (U, V) = Axes(normal);
        Normal = normal; NormalGradient = normalGradient; TangentU = tangentU; TangentV = tangentV;
        // Constructing the union checks that identities do not refer to different
        // states across different directions, including zero-mass carriers.
        _bodies = new ConstraintGradient([..normalGradient.Terms, ..tangentU.Terms, ..tangentV.Terms]).Bodies.ToArray();
        _revisions = _bodies.Select(body => body.PoseRevision).ToArray();
    }

    public static ContactKinematics AtPoint(PhysicsBody a, PhysicsBody b, CollisionVector point, CollisionVector normal)
    {
        var (u, v) = Axes(normal);
        return new(normal, ConstraintJacobian.AtPoint(a, b, point, normal).Bind(a, b),
            ConstraintJacobian.AtPoint(a, b, point, u).Bind(a, b),
            ConstraintJacobian.AtPoint(a, b, point, v).Bind(a, b));
    }

    public ContactKinematics Rebind(System.Collections.Generic.IReadOnlyDictionary<PhysicsBodyId, PhysicsBody> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        ValidatePose();
        ConstraintGradient RebindGradient(ConstraintGradient gradient) => new(gradient.Terms.ToArray().Select(term =>
        {
            if (!states.TryGetValue(term.Body.Id, out var body) || body is null || body.Id != term.Body.Id)
                throw new ArgumentException("Contact rebinding requires every declared participant.", nameof(states));
            if (body.Pose != term.Body.Pose)
                throw new ArgumentException("Contact rebinding requires the same geometric pose.", nameof(states));
            return new ConstraintTerm(body, term.Linear, term.Angular);
        }).ToArray());
        return new(Normal, RebindGradient(NormalGradient), RebindGradient(TangentU), RebindGradient(TangentV));
    }
}
