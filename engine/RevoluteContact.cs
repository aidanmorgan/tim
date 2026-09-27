using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct RevoluteContactResult(Vector3 Velocity, double Impulse, double StopDissipation);

/// <summary>
/// Frictionless sphere/hinge normal impact. The pivot absorbs forbidden linear
/// momentum. Free contact shares the impulse with finite hinge inertia; an active
/// hard stop absorbs outward angular momentum without supplying restitution work.
/// Geometry, resting/friction constraints and simultaneous cargo contacts belong
/// to the world solver, not this single-contact velocity operation.
/// </summary>
public static class RevoluteContact
{
    /// <param name="inverseMass">Sphere/guide response along the unit contact normal; zero is constrained.</param>
    public static RevoluteContactResult Resolve(RevoluteJoint joint, Vector3 offset,
        Vector3 velocity, double inverseMass, Vector3 normal, float restitution)
    {
        ArgumentNullException.ThrowIfNull(joint);
        if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity));
        if (!double.IsFinite(inverseMass) || inverseMass < 0) throw new ArgumentOutOfRangeException(nameof(inverseMass));
        if (!float.IsFinite(restitution) || restitution < 0 || restitution > 1)
            throw new ArgumentOutOfRangeException(nameof(restitution));
        var inverse = joint.FreeInverseMassAlong(offset, normal); // validates geometry
        var arm = joint.Moment(offset, normal);
        var approach = Dot(velocity, normal) - arm * joint.AngularVelocity;
        if (approach >= 0) return new(velocity, 0, 0);
        var blocked = joint.AngularVelocity == 0 &&
            ((joint.Limit == HingeLimit.Lower && arm > 0) || (joint.Limit == HingeLimit.Upper && arm < 0));
        if (blocked) inverse = 0;
        if (inverseMass + inverse <= 0) throw new InvalidOperationException("Contact has no permitted motion response.");
        var impulse = -(1 + restitution) * approach / (inverseMass + inverse);
        var angularImpulse = -arm * impulse;
        var freeAngularVelocity = joint.AngularVelocity + angularImpulse / joint.Inertia;
        var nextVelocity = AddNormalImpulse(velocity, normal, impulse * inverseMass);
        // ApplyAngularImpulse validates finite energy before mutation.
        joint.ApplyAngularImpulse(angularImpulse);
        var stopped = !blocked && joint.AngularVelocity != freeAngularVelocity;
        var stopDissipation = stopped ? .5 * joint.Inertia * freeAngularVelocity * freeAngularVelocity : 0;
        if (stopped)
        {
            // Stop projection can remove the beam velocity that separated the
            // pair. Resolve any residual approach plastically against that stop;
            // do not apply restitution a second time.
            var residual = Dot(nextVelocity, normal);
            if (residual < 0)
            {
                if (inverseMass == 0) throw new InvalidOperationException("An immovable contact has residual approach.");
                var correction = -residual / inverseMass;
                nextVelocity = AddNormalImpulse(nextVelocity, normal, correction * inverseMass);
                impulse += correction;
            }
        }
        return new(nextVelocity, impulse, stopDissipation);
    }

    private static Vector3 AddNormalImpulse(Vector3 velocity, Vector3 normal, double change)
    {
        var next = new Vector3((float)(velocity.X + normal.X * change),
            (float)(velocity.Y + normal.Y * change), (float)(velocity.Z + normal.Z * change));
        if (!next.IsFinite()) throw new ArgumentOutOfRangeException(nameof(change), "Contact exceeds vector precision.");
        return next;
    }
    private static double Dot(Vector3 a, Vector3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
}
