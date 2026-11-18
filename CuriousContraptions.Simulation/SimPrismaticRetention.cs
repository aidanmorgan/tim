using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

/// <summary>Accepted state of one compressive coordinate. The solver owns all travel.</summary>
internal sealed class SimPrismaticRetention
{
    public PrismaticRetentionId Id;
    public SimJoint Joint = null!;
    public SimMechanicalTransmission Transmission = null!;
    public RetentionMode Mode;
    public RetentionPhase Phase;
    public RetentionRequestResult? Result;
    public float Bound, Potential, Accepted, Released, ContactWork, GravityWork;
    private float _startPotential, _startContactWork;
    /// <summary>This substep's relaxed slider-row rate over both bodies, sampled when the relax iterations finish.</summary>
    private float _relaxedRate;
    private Vector3 _startCentre;

    public void Request()
    {
        if (Mode == RetentionMode.Releasing) { Result = RetentionRequestResult.AlreadyReleased; return; }
        if (-PrismaticMotorSolver.Coordinate(Joint) <= PhysicalTolerance.ActuatorStopSlopMetres)
        { Result = RetentionRequestResult.Empty; return; }
        Result = RetentionRequestResult.Released;
        Mode = RetentionMode.Releasing;
        Bound = Math.Clamp(PrismaticMotorSolver.Coordinate(Joint), Joint.Lower, Joint.Upper);
        Transmission.Engaged = false;
        Transmission.Impulse = 0f;
        ClearStopImpulses();
    }

    public void BeginSubstep()
    {
        _startPotential = Energy(PrismaticMotorSolver.Coordinate(Joint));
        _startCentre = Joint.B.CentreOfMass;
        _startContactWork = Joint.B.ContactWork;
    }

    /// <summary>Samples the slider row's relaxed (unbiased) coordinate rate over both bodies, so a moving support counts.</summary>
    public void SampleRelaxedRate()
    {
        ref readonly var row = ref Joint.Rows[(int)PrismaticRowKind.Spring];
        _relaxedRate = ConstraintVelocity(Joint.A, row.La, row.Aa) + ConstraintVelocity(Joint.B, row.Lb, row.Ab);
    }

    public void Commit(List<ContactPoint> contacts)
    {
        var coordinate = PrismaticMotorSolver.Coordinate(Joint);
        Potential = Energy(coordinate);
        if (Mode == RetentionMode.Retained)
        {
            // The bound advances only on relaxed inward motion: solver overlap correction that relax reverses is not retained,
            // and accepted energy accrues only from the bound's own advance.
            var previous = Bound;
            if (_relaxedRate < 0f) Bound = MathF.Min(Bound, Math.Clamp(coordinate, Joint.Lower, Joint.Upper));
            Accepted += Energy(Bound) - Energy(previous);
        }
        else
        {
            Released += MathF.Max(0f, _startPotential - Potential);
            Bound = MathF.Max(Bound, Math.Clamp(coordinate, Joint.Lower, Joint.Upper));
            if (-coordinate <= PhysicalTolerance.ActuatorStopSlopMetres)
            {
                Mode = RetentionMode.Retained;
                Bound = Math.Clamp(coordinate, Joint.Lower, Joint.Upper);
                Transmission.Engaged = true;
                ClearStopImpulses();
            }
        }
        ContactWork += Joint.B.ContactWork - _startContactWork;
        GravityWork += Joint.B.Mass * Vector3.Dot(new(Joint.B.Gx, Joint.B.Gy, Joint.B.Gz), Joint.B.CentreOfMass - _startCentre);
        Observe(contacts, coordinate);
    }

    private float Energy(float coordinate)
    {
        var compression = MathF.Max(0f, -coordinate);
        return .5f * Joint.Stiffness * compression * compression;
    }

    private void ClearStopImpulses()
    {
        Joint.Impulses[(int)PrismaticRowKind.LowerStop] = 0f;
        Joint.Impulses[(int)PrismaticRowKind.UpperStop] = 0f;
    }

    private void Observe(List<ContactPoint> contacts, float coordinate)
    {
        ref readonly var row = ref Joint.Rows[(int)PrismaticRowKind.Spring];
        var speed = ConstraintVelocity(Joint.A, row.La, row.Aa) + ConstraintVelocity(Joint.B, row.Lb, row.Ab);
        Phase = Mode == RetentionMode.Releasing ? RetentionPhase.Releasing :
            -coordinate <= PhysicalTolerance.ActuatorStopSlopMetres ? RetentionPhase.Idle :
            speed < -ActuatorObservations.RestSpeed ? RetentionPhase.Winding : RetentionPhase.Armed;
        var direction = Mode == RetentionMode.Releasing ? 1f : -1f;
        var remaining = Mode == RetentionMode.Releasing ? -coordinate : coordinate - Joint.Lower;
        if (remaining <= PhysicalTolerance.ActuatorStopSlopMetres || MathF.Abs(speed) >= ActuatorObservations.RestSpeed ||
            (Mode == RetentionMode.Retained && Transmission.Impulse >= 0f)) return;
        var axial = new ImpulseJacobian(Joint.A, row.La, row.Aa, Joint.B, row.Lb, row.Ab);
        foreach (var contact in contacts)
            if (contact.LambdaN > 0f && direction * ImpulseJacobian.Coupling(axial,
                WorkshopConstraintSolver.ContactRow(contact, contact.Normal)) < 0f)
            { Phase = RetentionPhase.Blocked; return; }
    }
}
