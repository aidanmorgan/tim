using Godot;
using System;

namespace CuriousContraptions;

public enum HingeRole { Beam }

/// <summary>A part-owned, fixed-pivot box with finite rotational inertia. Joint
/// coordinates are local to the owner; editing the whole part carries its hinge.</summary>
public sealed class HingedBody
{
    public MachinePart Owner { get; }
    public HingeRole Role { get; }
    public RevoluteJoint Joint { get; }
    public Vector3 LocalPivot { get; }
    public Vector3 Half { get; }
    public float Restitution { get; }
    public Vector3 Pivot => Owner.Transform * LocalPivot;
    public Vector3 Axis => Owner.Basis.Z;
    public Transform3D LocalPose => new(new Basis(Vector3.Back, (float)Joint.Angle), LocalPivot);
    public Transform3D Pose => Owner.Transform * LocalPose;

    public HingedBody(MachinePart owner, HingeRole role, Vector3 localPivot, Vector3 half,
        double inertia, double lowerAngle, double upperAngle, double initialAngle, float restitution)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        if (!localPivot.IsFinite() || !half.IsFinite() || half.X <= 0 || half.Y <= 0 || half.Z <= 0)
            throw new ArgumentException("Hinge geometry must be finite and have positive dimensions.");
        if (!float.IsFinite(restitution) || restitution < 0 || restitution > 1)
            throw new ArgumentOutOfRangeException(nameof(restitution));
        Owner = owner; Role = role; LocalPivot = localPivot; Half = half; Restitution = restitution;
        Joint = new(Vector3.Back, inertia, lowerAngle, upperAngle, initialAngle);
    }

    public RotatingBoxHit Sweep(MachinePart body, double duration) =>
        RotatingBoxSweep.Cast(body.Position, body.Radius, body.Velocity, Pivot, Axis, Pose, Half,
            Joint.AngularVelocity, Math.Min(duration, Joint.TimeToLimit));

    public Vector3 PointVelocity(Vector3 point) =>
        Owner.Basis * Joint.PointVelocity(Owner.Basis.Transposed() * (point - Pivot));

    public void Resolve(MachinePart body, RotatingBoxHit hit, double remaining)
    {
        var response = body.InverseMassResponse(hit.Normal);
        var inverseMass = hit.Normal.Dot(response);
        if (hit.Penetration > 0)
        {
            if (inverseMass <= 0) throw new InvalidOperationException("Guided body overlaps a hinge with no positional response.");
            body.Position += response * ((hit.Penetration + .00001f) / inverseMass);
        }
        var inverseBasis = Owner.Basis.Transposed();
        var result = RevoluteContact.Resolve(Joint, inverseBasis * (hit.Point - Pivot),
            inverseBasis * body.Velocity, inverseMass, inverseBasis * hit.Normal, body.Bounce * Restitution);
        body.Velocity += response * (float)result.Impulse;
        body.ConstrainVelocity();
        // Implicit support projection: a rotating plane can curve into a body
        // whose instantaneous relative normal velocity is already zero. Use the
        // predicted contact Jacobian to remove inward velocity, without a bias
        // speed or another restitution impulse. Each projection is dissipative.
        var futureTime = Math.Min(remaining, Joint.TimeToLimit);
        if (futureTime <= 0) return;
        var futureBasis = Owner.Basis * new Basis(Vector3.Back, (float)(Joint.Angle + Joint.AngularVelocity * futureTime));
        var futureBody = body.Position + body.Velocity * (float)futureTime;
        var sample = SphereSweep.BoxSurface(futureBasis.Transposed() * (futureBody - Pivot), Half);
        if (sample.Distance > body.Radius + SphereSweep.ContactTolerance) return;
        var futureNormal = futureBasis * sample.Normal;
        var futurePoint = futureBody - futureNormal * sample.Distance;
        var futureResponse = body.InverseMassResponse(futureNormal);
        var support = RevoluteContact.Resolve(Joint, inverseBasis * (futurePoint - Pivot),
            inverseBasis * body.Velocity, futureNormal.Dot(futureResponse), inverseBasis * futureNormal, 0);
        body.Velocity += futureResponse * (float)support.Impulse;
        body.ConstrainVelocity();
    }
}
