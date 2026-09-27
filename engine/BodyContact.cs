using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Normal impulse/separation for free and guided masses. A guide absorbs
/// forbidden momentum; it must never grant extra velocity in a constrained axis.</summary>
public static class BodyContact
{
    public static void Resolve(MachinePart first, MachinePart second, Vector3 normal, float penetration, float restitution)
    {
        var a = first.InverseMassResponse(normal);
        // The second body receives the opposite impulse. A one-way guide's
        // response is directional, unlike an unconstrained scalar inverse mass.
        var b = -second.InverseMassResponse(-normal);
        var inverseMass = normal.Dot(a + b);
        if (inverseMass <= 0)
            throw new InvalidOperationException("Contact between immovable bodies cannot be resolved.");
        if (penetration > 0)
        {
            var correction = (penetration + .00001f) / inverseMass;
            first.Position += a * correction;
            second.Position -= b * correction;
        }
        var approach = (first.Velocity - second.Velocity).Dot(normal);
        if (approach >= 0) return;
        var impulse = -(1 + restitution) * approach / inverseMass;
        first.Velocity += a * impulse;
        second.Velocity -= b * impulse;
        first.ConstrainVelocity();
        second.ConstrainVelocity();
    }
}
