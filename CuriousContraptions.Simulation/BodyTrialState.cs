using System.Numerics;

namespace CuriousContraptions.Simulation;

/// <summary>Physical and accepted-work state restored together when an ordinary solver trial is rejected.</summary>
internal readonly record struct BodyTrialState(
    BodyTrialPose Pose, BodyTrialRates Rates, BodyTrialMass Mass,
    BodyTrialWindow Window, BodyTrialRestitution Restitution)
{
    public static BodyTrialState Capture(SimBody body) => new(
        new(body.CellX, body.CellY, body.CellZ, new(body.LocalX, body.LocalY, body.LocalZ),
            body.Position, body.Rotation, body.TickStartRotation, body.TickRelativeRotation, body.Cm0, body.Q0),
        new(body.LinearVelocity, body.AngularVelocity, body.V0, body.W0, body.PreSolveKinetic, body.MotionV, body.MotionW),
        new(body.Mass, body.InvMass, body.ComLocal, body.Principal, body.InvI,
            body.PrincipalX, body.PrincipalY, body.PrincipalZ, body.CentreOfMass),
        new(body.ContactWork, body.WindowV, body.WindowW, body.WindowContactTranslationalImpulse,
            body.WindowContactAngularImpulse),
        new(body.RelaxedV, body.RelaxedW, body.RelaxedContactTranslationalImpulse, body.RelaxedContactAngularImpulse,
            body.RestitutionRoot, body.RestitutionStamp, body.EnergyA, body.EnergyB, body.EnergyC,
            body.EnergyBudget, body.EnergyScale));

    public void Restore(SimBody body)
    {
        Pose.Restore(body);
        Rates.Restore(body);
        Mass.Restore(body);
        Window.Restore(body);
        Restitution.Restore(body);
        // Every replay rebuilds the guide factors from the restored rows, with empty pending reactions.
        body.Guide = null;
    }
}

internal readonly record struct BodyTrialPose(int CellX, int CellY, int CellZ, Vector3 Local, Vector3 Position,
    Quaternion Rotation, Quaternion TickStart, Quaternion Relative, Vector3 Cm0, Quaternion Q0)
{
    public void Restore(SimBody body)
    {
        body.CellX = CellX; body.CellY = CellY; body.CellZ = CellZ;
        body.LocalX = Local.X; body.LocalY = Local.Y; body.LocalZ = Local.Z;
        body.Px = Position.X; body.Py = Position.Y; body.Pz = Position.Z;
        body.Qx = Rotation.X; body.Qy = Rotation.Y; body.Qz = Rotation.Z; body.Qw = Rotation.W;
        body.TickStartRotation = TickStart; body.TickRelativeRotation = Relative;
        body.Cm0 = Cm0; body.Q0 = Q0;
    }
}

internal readonly record struct BodyTrialRates(Vector3 Velocity, Vector3 Spin, Vector3 V0, Vector3 W0, float PreSolveKinetic,
    Vector3 MotionV, Vector3 MotionW)
{
    public void Restore(SimBody body)
    {
        body.Vx = Velocity.X; body.Vy = Velocity.Y; body.Vz = Velocity.Z;
        body.Wx = Spin.X; body.Wy = Spin.Y; body.Wz = Spin.Z;
        body.V0 = V0; body.W0 = W0; body.PreSolveKinetic = PreSolveKinetic;
        body.MotionV = MotionV; body.MotionW = MotionW;
    }
}

internal readonly record struct BodyTrialMass(float Mass, float InverseMass, Vector3 ComLocal, Quaternion Principal,
    Vector3 InverseInertia, Vector3 PrincipalX, Vector3 PrincipalY, Vector3 PrincipalZ, Vector3 Centre)
{
    public void Restore(SimBody body)
    {
        body.Mass = Mass; body.InvMass = InverseMass; body.ComLocal = ComLocal; body.Principal = Principal;
        body.InvI = InverseInertia; body.PrincipalX = PrincipalX; body.PrincipalY = PrincipalY; body.PrincipalZ = PrincipalZ;
        body.Cmx = Centre.X; body.Cmy = Centre.Y; body.Cmz = Centre.Z;
    }
}

internal readonly record struct BodyTrialWindow(float ContactWork, Vector3 Velocity, Vector3 Spin,
    Vector3 LinearImpulse, Vector3 AngularImpulse)
{
    public void Restore(SimBody body)
    {
        body.ContactWork = ContactWork; body.WindowV = Velocity; body.WindowW = Spin;
        body.WindowContactTranslationalImpulse = LinearImpulse; body.WindowContactAngularImpulse = AngularImpulse;
    }
}

internal readonly record struct BodyTrialRestitution(Vector3 Velocity, Vector3 Spin,
    Vector3 LinearImpulse, Vector3 AngularImpulse, SimBody Root, int Stamp,
    float A, float B, float C, float Budget, float Scale)
{
    public void Restore(SimBody body)
    {
        body.RelaxedV = Velocity; body.RelaxedW = Spin;
        body.RelaxedContactTranslationalImpulse = LinearImpulse; body.RelaxedContactAngularImpulse = AngularImpulse;
        body.RestitutionRoot = Root; body.RestitutionStamp = Stamp;
        body.EnergyA = A; body.EnergyB = B; body.EnergyC = C; body.EnergyBudget = Budget; body.EnergyScale = Scale;
    }
}
