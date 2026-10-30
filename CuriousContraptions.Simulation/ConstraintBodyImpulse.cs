using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal static class ConstraintBodyImpulse
{
    public static float ConstraintMass(SimBody body, Vector3 linear, Vector3 angular)
    {
        if (body.Motion != RigidMotionKind.Dynamic) return 0f;
        return body.InvMass * Vector3.Dot(linear, linear) + WorkshopCollisionKernels.InverseInertiaCoupling(body, angular, angular);
    }

    public static float ConstraintVelocity(SimBody body, Vector3 linear, Vector3 angular) =>
        Vector3.Dot(linear, body.LinearVelocity) + Vector3.Dot(angular, body.AngularVelocity);

    public static void ApplyBodyImpulse(SimBody body, Vector3 linear, Vector3 angular, float impulse)
    {
        if (body.Motion != RigidMotionKind.Dynamic) return;
        body.Vx += linear.X * impulse * body.InvMass;
        body.Vy += linear.Y * impulse * body.InvMass;
        body.Vz += linear.Z * impulse * body.InvMass;
        var dw = WorkshopCollisionKernels.ApplyInvInertia(body, angular * impulse);
        body.Wx += dw.X;
        body.Wy += dw.Y;
        body.Wz += dw.Z;
    }

}
