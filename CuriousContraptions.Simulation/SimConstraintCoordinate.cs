using System;
using System.Numerics;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

/// <summary>A resolved scalar coordinate and its instantaneous body Jacobian.</summary>
internal sealed class SimConstraintCoordinate
{
    private SimJoint? _prismatic;
    private SimRevoluteJoint? _revolute;
    public SimBody A { get; private set; } = null!;
    public SimBody B { get; private set; } = null!;
    public ConstraintRowData Row { get; private set; }

    /// <summary>Rebinds this pooled coordinate to a loaded joint, so a tick with unchanged topology allocates nothing.</summary>
    public void Bind(SimJoint joint)
    { _prismatic = joint; _revolute = null; A = joint.A; B = joint.B; Row = default; }
    public void Bind(SimRevoluteJoint joint)
    { _revolute = joint; _prismatic = null; A = joint.A; B = joint.B; Row = default; }

    public void Prepare()
    {
        if (_prismatic is { } slider)
        {
            Row = slider.Rows[(int)PrismaticRowKind.Spring];
            return;
        }
        var joint = _revolute!;
        var axis = Vector3.Transform(Vector3.UnitZ, Quaternion.Multiply(joint.A.Rotation, joint.Qa));
        Row = new ConstraintRowData { Aa = -axis, Ab = axis };
    }

    public float Rate => ConstraintVelocity(A, Row.La, Row.Aa) + ConstraintVelocity(B, Row.Lb, Row.Ab);

    /// <summary>The coordinate rate at the open accepted-work window's start: the same row over the bodies' window-start rates
    /// (a static body has none).</summary>
    public float WindowStartRate => WindowRate(A, Row.La, Row.Aa) + WindowRate(B, Row.Lb, Row.Ab);

    private static float WindowRate(SimBody body, Vector3 linear, Vector3 angular) =>
        body.Motion == RigidMotionKind.Dynamic ? Vector3.Dot(linear, body.WindowV) + Vector3.Dot(angular, body.WindowW) : 0f;
}
