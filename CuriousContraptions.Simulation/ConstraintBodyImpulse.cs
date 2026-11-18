using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal static class ConstraintBodyImpulse
{
    public static float ConstraintMass(SimBody body, Vector3 linear, Vector3 angular)
    {
        return new ImpulseJacobian(body, linear, angular).Mass();
    }

    public static float ConstraintMass(SimBody a, Vector3 la, Vector3 aa, SimBody b, Vector3 lb, Vector3 ab) =>
        new ImpulseJacobian(a, la, aa, b, lb, ab).Mass();

    public static bool Coupled(SimBody a, SimBody b, SimBody c, SimBody d) =>
        SharesResponse(a, c) || SharesResponse(a, d) || SharesResponse(b, c) || SharesResponse(b, d);

    private static bool SharesResponse(SimBody a, SimBody b) =>
        a.Motion == RigidMotionKind.Dynamic && b.Motion == RigidMotionKind.Dynamic &&
        (a == b || (a.Guide is not null && a.Guide == b.Guide));

    public static float ConstraintVelocity(SimBody body, Vector3 linear, Vector3 angular) =>
        Vector3.Dot(linear, body.LinearVelocity) + Vector3.Dot(angular, body.AngularVelocity);

    public static void ApplyBodyImpulse(SimBody body, Vector3 linear, Vector3 angular, float impulse)
    {
        if (body.Motion != RigidMotionKind.Dynamic) return;
        if (body.Guide is { } guide) { guide.Apply(new ImpulseJacobian(body, linear, angular), impulse); return; }
        body.Vx += linear.X * impulse * body.InvMass;
        body.Vy += linear.Y * impulse * body.InvMass;
        body.Vz += linear.Z * impulse * body.InvMass;
        var dw = WorkshopCollisionKernels.ApplyInvInertia(body, angular * impulse);
        body.Wx += dw.X;
        body.Wy += dw.Y;
        body.Wz += dw.Z;
    }

}
