using System;
using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private void LoadRetention(Span<byte> span, SimMechanicalTransmission transmission)
    {
        var offset = transmission.Offset;
        var id = BinaryPrimitives.ReadUInt64LittleEndian(span[(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionOffset)..]);
        if (id == 0) { transmission.Retention = null; return; }
        var joint = _joints[checked((int)ReadU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionDrivenOffset))];
        var retained = transmission.Retention = transmission.RetentionSlot;
        retained.Id = new(id);
        retained.Joint = joint;
        retained.Transmission = transmission;
        joint.Retention = retained;
        retained.Mode = (RetentionMode)ReadU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionModeOffset);
        retained.Phase = (RetentionPhase)ReadU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionPhaseOffset);
        retained.Bound = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionBoundOffset);
        retained.Potential = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionPotentialOffset);
        retained.Accepted = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionAcceptedOffset);
        retained.Released = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionReleasedOffset);
        retained.ContactWork = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionContactWorkOffset);
        retained.GravityWork = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionGravityWorkOffset);
        var result = ReadU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRequestResultOffset);
        retained.Result = result == 0 ? null : (RetentionRequestResult)(result - 1);
    }

    /// <summary>Opens one accepted-work window at the current fixed pose (Design Notes, window-midpoint rule): every dynamic body's
    /// start rates, with empty contact and relation impulse accumulators.</summary>
    private void OpenWorkWindow()
    {
        foreach (var body in _dynamicBodies)
        {
            body.WindowV = body.LinearVelocity;
            body.WindowW = body.AngularVelocity;
            body.WindowContactTranslationalImpulse = Vector3.Zero;
            body.WindowContactAngularImpulse = Vector3.Zero;
        }
        foreach (var transmission in _transmissions) transmission.WindowImpulse = 0f;
    }

    /// <summary>Closes the window: each channel's whole window impulse is credited once against the window's start and end rates,
    /// so the channels of one body sum to its window kinetic-energy change whatever the solve order.</summary>
    private void CloseWorkWindow()
    {
        foreach (var body in _dynamicBodies)
            body.ContactWork += Vector3.Dot(body.WindowContactTranslationalImpulse, .5f * (body.WindowV + body.LinearVelocity)) +
                Vector3.Dot(body.WindowContactAngularImpulse, .5f * (body.WindowW + body.AngularVelocity));
        foreach (var transmission in _transmissions) MechanicalTransmissionSolver.CloseWorkWindow(transmission);
    }

    /// <summary>Resumes the open window committed by the previous tick for the bodies and channels a relation carries (the only ones
    /// any observation reads); every other dynamic body was opened at its committed velocity by <see cref="OpenWorkWindow"/>.</summary>
    private static void LoadWorkWindow(ReadOnlySpan<byte> span, SimMechanicalTransmission transmission)
    {
        var offset = transmission.Offset;
        transmission.WindowImpulse = ReadF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowImpulseOffset);
        LoadWindowStart(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowDrivenAVelocityOffset, transmission.Driven.A);
        LoadWindowStart(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowDrivenBVelocityOffset, transmission.Driven.B);
        if (transmission.Retention is null) return;
        transmission.Driven.B.WindowContactTranslationalImpulse = ReadVector(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowContactTranslationalImpulseOffset);
        transmission.Driven.B.WindowContactAngularImpulse = ReadVector(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowContactAngularOffset);
    }

    private static void LoadWindowStart(ReadOnlySpan<byte> span, int offset, SimBody body)
    {
        if (body.Motion != RigidMotionKind.Dynamic) return;
        body.WindowV = ReadVector(span, offset);
        body.WindowW = ReadVector(span, offset + 12);
    }

    private static void CommitWorkWindow(Span<byte> span, SimMechanicalTransmission transmission)
    {
        var offset = transmission.Offset;
        WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowImpulseOffset, transmission.WindowImpulse);
        CommitWindowStart(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowDrivenAVelocityOffset, transmission.Driven.A);
        CommitWindowStart(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowDrivenBVelocityOffset, transmission.Driven.B);
        var retained = transmission.Retention is not null;
        WriteVector(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowContactTranslationalImpulseOffset,
            retained ? transmission.Driven.B.WindowContactTranslationalImpulse : Vector3.Zero);
        WriteVector(span, offset + PhysicsGpuAbi.MechanicalTransmissionWindowContactAngularOffset,
            retained ? transmission.Driven.B.WindowContactAngularImpulse : Vector3.Zero);
    }

    private static void CommitWindowStart(Span<byte> span, int offset, SimBody body)
    {
        var dynamic = body.Motion == RigidMotionKind.Dynamic;
        WriteVector(span, offset, dynamic ? body.WindowV : Vector3.Zero);
        WriteVector(span, offset + 12, dynamic ? body.WindowW : Vector3.Zero);
    }

    private static void WriteVector(Span<byte> span, int offset, Vector3 value) => WriteVector(span, offset, value.X, value.Y, value.Z);

    private void CommitTransmission(Span<byte> span, SimMechanicalTransmission transmission)
    {
        var offset = transmission.Offset;
        transmission.Work += transmission.StepWork;
        transmission.PositiveWork += transmission.StepPositive;
        WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionWorkOffset, transmission.Work);
        WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionPositiveWorkOffset, transmission.PositiveWork);
        if (transmission.Retention is { } retained)
        {
            retained.Commit(_contacts);
            WriteU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionModeOffset, (uint)retained.Mode);
            WriteU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionPhaseOffset, (uint)retained.Phase);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRetentionBoundOffset, retained.Bound);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionCompressionOffset,
                MathF.Max(0f, -PrismaticMotorSolver.Coordinate(retained.Joint)));
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionPotentialOffset, retained.Potential);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionAcceptedOffset, retained.Accepted);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionReleasedOffset, retained.Released);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionContactWorkOffset, retained.ContactWork);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionGravityWorkOffset, retained.GravityWork);
            WriteU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionRequestResultOffset,
                retained.Result is { } result ? (uint)result + 1 : 0);
            WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset, transmission.Engaged ? 1f : 0f);
            WriteU32(span, offset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset,
                (uint)(transmission.Engaged ? TransmissionEngagementPhase.Engaged : TransmissionEngagementPhase.Open));
        }
        WriteF32(span, offset + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset, transmission.Impulse);
        CommitWorkWindow(span, transmission);
    }
}
