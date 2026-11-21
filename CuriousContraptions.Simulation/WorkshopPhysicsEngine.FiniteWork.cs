using System;
using System.Numerics;
using System.Buffers.Binary;
using System.Collections.Generic;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private PrismaticTrialWorkspace _impulseTrial = new();

    public PhysicsInteractionSnapshot ApplyFiniteWork(ReadOnlyMemory<byte> source, Memory<byte> candidate,
        PhysicsTickInputs inputs, PhysicsInteractionSnapshot committedInteractions)
    {
        inputs.ValidateOperation(WorkshopGpuOperation.ApplyFiniteWork);
        var request = inputs.OwnedFiniteWork!.Value;
        var resolved = PhysicsGpuAbi.ValidateFiniteWorkRequest(source.Span, request);
        if (!PhysicsGpuAbi.ReadPhysicalRevision(source.Span).TryAdvance(out var revision))
            throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.IdentityExhausted);
        if (candidate.Length != source.Length) throw new ArgumentException("Physical candidate length differs.");
        source.CopyTo(candidate);
        var world = candidate.Span;
        var layout = PhysicsGpuAbi.Layout(world);
        PrepareStoreCommit(world, layout);
        _staticBodies.Clear(); _dynamicBodies.Clear(); _allBodies.Clear();
        ReadBodies(world, layout, (uint)layout.Bodies);
        ReadColliders(world, layout, (uint)layout.Colliders);
        ReadTensileRoutes(world, layout);
        ReadPrismatics(world, layout, inputs);
        BindExtrusions(world, layout);
        ReadRevolutes(world, layout, inputs, _surfacesResolved ? null : new Dictionary<RevoluteConstraintId, SimRevoluteJoint>());
        ReadTransmissions(world, layout);
        foreach (var body in _dynamicBodies) { body.V0 = body.LinearVelocity; body.W0 = body.AngularVelocity; }
        foreach (var transmission in _transmissions)
        {
            transmission.StepWork = 0f; transmission.StepPositive = 0f;
            transmission.Retention?.BeginSubstep();
        }
        _impulseComponent.Clear();
        ApplyFiniteWorkRequest(world, layout, resolved.Actuator, (uint)resolved.Payload);
        var actuator = world.Slice(layout.FiniteWorkActuatorOffset(resolved.Actuator), PhysicsGpuAbi.FiniteWorkActuatorBytes);
        if (ReadU32(actuator, PhysicsGpuAbi.ActuatorResultOffset) == (uint)FiniteWorkRequestResult.Fired + 1u)
        {
            foreach (var body in _dynamicBodies)
            {
                WriteVector(world, body.BodyOffset + PhysicsGpuAbi.BodyVelocityOffset, body.LinearVelocity);
                WriteVector(world, body.BodyOffset + PhysicsGpuAbi.BodyAngularVelocityOffset, body.AngularVelocity);
            }
            foreach (var transmission in _transmissions)
            {
                if (!(_impulseComponent.Contains(transmission.Driving.A) || _impulseComponent.Contains(transmission.Driving.B) ||
                    _impulseComponent.Contains(transmission.Driven.A) || _impulseComponent.Contains(transmission.Driven.B))) continue;
                transmission.Retention?.SampleRelaxedRate();
                CommitTransmission(world, transmission);
            }
        }
        EvaluateChambers(world, layout);
        EvaluateClearances(world, layout);
        BinaryPrimitives.WriteUInt64LittleEndian(world[PhysicsGpuAbi.HeaderPhysicalRevisionOffset..], revision.Value);
        return committedInteractions;
    }

    private void ApplyFiniteWorkRequests(Span<byte> world, PhysicsWorldLayout layout, PhysicsTickInputs inputs)
    {
        foreach (var request in inputs.PhysicalRequests)
            if (request.Target.Kind == PhysicalRequestTargetKind.FiniteWorkActuator)
                ApplyFiniteWorkRequest(world, layout, FiniteWorkSlot(world, layout, request.Target.Actuator));
    }

    private void ApplyFiniteWorkRequest(Span<byte> world, PhysicsWorldLayout layout, int slot, uint? requiredPayload = null)
    {
        var actuator = world.Slice(layout.FiniteWorkActuatorOffset(slot), PhysicsGpuAbi.FiniteWorkActuatorBytes);
        var regionOffset = layout.ChamberRegionOffset((int)ReadU32(actuator, PhysicsGpuAbi.ActuatorRegionOffset));
        EvaluateChamber(world, regionOffset);
        var chamber = world.Slice(regionOffset, PhysicsGpuAbi.ChamberRegionBytes);
        var candidate = ReadU32(chamber, PhysicsGpuAbi.ChamberCandidateOffset);
        var phase = (ChamberPhase)ReadU32(chamber, PhysicsGpuAbi.ChamberPhaseOffset);
        var result = phase switch
        {
            ChamberPhase.Empty => FiniteWorkRequestResult.Empty,
            ChamberPhase.Loading => FiniteWorkRequestResult.Unseated,
            ChamberPhase.Ambiguous => FiniteWorkRequestResult.Ambiguous,
            ChamberPhase.Departing => FiniteWorkRequestResult.Busy,
            ChamberPhase.GeometryUnresolved => FiniteWorkRequestResult.GeometryUnresolved,
            _ => (FiniteWorkRequestResult?)null
        };
        if (result is null && requiredPayload is { } required && candidate != required)
            result = FiniteWorkRequestResult.TargetMismatch;
        WriteU32(actuator, PhysicsGpuAbi.ActuatorPayloadOffset,
            result == FiniteWorkRequestResult.TargetMismatch ? requiredPayload!.Value : candidate);
        WriteF32(actuator, PhysicsGpuAbi.ActuatorDebitOffset, 0f);
        if (result is null)
        {
            var store = world.Slice(layout.EnergyStoreOffset((int)ReadU32(actuator, PhysicsGpuAbi.ActuatorStoreOffset)), PhysicsGpuAbi.EnergyStoreBytes);
            result = ReleaseStoredWork(actuator, chamber, store, candidate);
        }
        WriteU32(actuator, PhysicsGpuAbi.ActuatorResultOffset, (uint)result.Value + 1u);
    }

    private FiniteWorkRequestResult ReleaseStoredWork(Span<byte> actuator, Span<byte> chamber, Span<byte> store, uint candidate)
    {
        var balance = StoreBalance(store, 0);
        var energy = balance.Available;
        if (balance.CompareTo(ReadF32(store, PhysicsGpuAbi.EnergyStoreCapacityOffset)) < 0) return FiniteWorkRequestResult.Uncharged;
        var payload = _allBodies[(int)candidate];
        var frame = _allBodies[(int)ReadU32(chamber, PhysicsGpuAbi.ChamberFrameOffset)];
        var region = ChamberShape(chamber, frame);
        var speed = ReadF32(actuator, PhysicsGpuAbi.ActuatorSpeedOffset);
        var clearance = PayloadExitClearance(payload, frame, region, ReadF32(actuator, PhysicsGpuAbi.ActuatorPaddingDistanceOffset));
        if (clearance != MaterialQueryResult.Clear)
            return clearance == MaterialQueryResult.Unresolved ? FiniteWorkRequestResult.GeometryUnresolved : FiniteWorkRequestResult.Obstructed;
        if (AxialRate(payload, frame, region.U1) >= speed) return FiniteWorkRequestResult.SpeedLimited;
        var debit = SolvePaidImpulse(payload, frame, region.U1, energy, speed);
        if (!(debit > 0f)) return FiniteWorkRequestResult.NoResponse;
        StorePayment(store, 0, debit);
        WriteF32(actuator, PhysicsGpuAbi.ActuatorDebitOffset, debit);
        WriteU32(actuator, PhysicsGpuAbi.ActuatorCountOffset, checked(ReadU32(actuator, PhysicsGpuAbi.ActuatorCountOffset) + 1u));
        WriteChamber(chamber, ChamberPhase.Departing, ReadU32(chamber, PhysicsGpuAbi.ChamberOccupancyOffset), candidate, candidate);
        return FiniteWorkRequestResult.Fired;
    }

    private static ImpulseJacobian PaidRow(SimBody payload, SimBody frame, Vector3 axis) =>
        new(payload, axis, Vector3.Zero, frame, -axis, Vector3.Cross(payload.CentreOfMass - frame.CentreOfMass, -axis));

    private static float AxialRate(SimBody payload, SimBody frame, Vector3 axis) =>
        Vector3.Dot(payload.LinearVelocity - frame.LinearVelocity -
            Vector3.Cross(frame.AngularVelocity, payload.CentreOfMass - frame.CentreOfMass), axis);

    private float SolvePaidImpulse(SimBody payload, SimBody frame, Vector3 axis, float energy, float speed)
    {
        var dt = WorkshopConstraintSolver.SubstepSeconds;
        GuideWorkspace.Detach(_dynamicBodies);
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        PrepareBroadphase(dt);
        _impulseTrial.Capture(_dynamicBodies, _allBodies.Count, _joints, _revoluteJoints, _transmissions, _tensile, _candidatePairs);
        var initial = AxialRate(payload, frame, axis);
        var inverseMass = PaidRow(payload, frame, axis).Mass();
        if (!(inverseMass > 0f)) return 0f;
        var upper = MathF.Sqrt(energy / inverseMass) * MathF.Sqrt(2f);
        var feasible = 0f;
        var lower = 0f;
        var debit = 0f;
        while (float.IsFinite(upper))
        {
            var trial = PaidImpulseTrial(payload, frame, axis, upper, initial);
            if (!trial.Valid || trial.Work > energy || trial.Rate > speed) break;
            if (!(trial.Rate > initial))
            {
                RestorePaidTrial();
                return 0f;
            }
            if (trial.Work > 0f) { feasible = upper; debit = trial.Work; }
            lower = upper;
            upper *= 2f;
        }
        if (!float.IsFinite(upper)) upper = float.MaxValue;
        while (true)
        {
            var middle = lower + (upper - lower) * .5f;
            if (middle == lower || middle == upper) break;
            var trial = PaidImpulseTrial(payload, frame, axis, middle, initial);
            if (trial.Valid && trial.Work > 0f && trial.Work <= energy && trial.Rate > initial && trial.Rate <= speed)
            { feasible = middle; lower = middle; debit = trial.Work; }
            else if (trial.Valid && trial.Work <= 0f && trial.Rate > initial && trial.Rate <= speed) lower = middle;
            else upper = middle;
        }
        if (!(feasible > 0f)) { RestorePaidTrial(); return 0f; }
        _ = PaidImpulseTrial(payload, frame, axis, feasible, initial);
        CloseWorkWindow();
        OpenWorkWindow();
        RestoreUnrelatedImpulseState(payload, frame);
        _impulseTrial.RestoreConstraintImpulses(_joints, _revoluteJoints, _transmissions, _tensile);
        GuideWorkspace.Detach(_dynamicBodies);
        return debit;
    }

    private readonly record struct PaidTrial(float Work, float Rate, bool Valid);

    private PaidTrial PaidImpulseTrial(SimBody payload, SimBody frame, Vector3 axis, float impulse, float initial)
    {
        RestorePaidTrial();
        CloseWorkWindow();
        OpenWorkWindow();
        PrepareInstantaneousConstraints();
        PaidRow(payload, frame, axis).Apply(impulse);
        var dt = WorkshopConstraintSolver.SubstepSeconds;
        PrepareContacts(dt);
        foreach (var contact in _contacts) contact.LambdaN = 0f;
        foreach (var manifold in _manifolds)
            foreach (var patch in manifold.Patches) patch.Lambda = Vector3.Zero;
        var soft = SoftCoefficients.MakeSoft(WorkshopConstraintSolver.ContactHertz, WorkshopConstraintSolver.ContactDampingRatio, dt);
        for (var iteration = 0; iteration < WorkshopConstraintSolver.SolverIterations; iteration++)
        {
            PrismaticConstraintSolver.SweepConstraints(_joints, false);
            RevoluteConstraintSolver.SweepConstraints(_revoluteJoints, false);
            foreach (var transmission in _transmissions) MechanicalTransmissionSolver.Solve(transmission);
            foreach (var route in _tensile) TensileConstraintSolver.Solve(route, false);
            WorkshopConstraintSolver.SweepManifolds(_manifolds, ContactPass.Relax, soft, dt);
        }
        _guides.FlushReactions();
        RestoreUnrelatedImpulseState(payload, frame);
        var rate = AxialRate(payload, frame, axis);
        var work = impulse * (.5f * initial + .5f * rate);
        var valid = float.IsFinite(rate) && float.IsFinite(work);
        foreach (var body in _dynamicBodies)
            valid &= body.LinearVelocity.LengthSquared() <= WorkshopConstraintSolver.MaxLinearSpeed * WorkshopConstraintSolver.MaxLinearSpeed &&
                body.AngularVelocity.LengthSquared() <= WorkshopConstraintSolver.MaxAngularSpeed * WorkshopConstraintSolver.MaxAngularSpeed;
        return new(work, rate, valid);
    }

    private void RestorePaidTrial()
    {
        GuideWorkspace.Detach(_dynamicBodies);
        _impulseTrial.Restore(_dynamicBodies, _joints, _revoluteJoints, _transmissions, _tensile, 1f);
    }

    private void PrepareInstantaneousConstraints()
    {
        var dt = WorkshopConstraintSolver.SubstepSeconds;
        foreach (var joint in _joints)
        {
            Array.Clear(joint.Impulses);
            PrismaticConstraintSolver.PrepareJointRows(joint, dt);
            if (!joint.Holding)
            {
                ref var row = ref joint.Rows[(int)PrismaticRowKind.Spring];
                row.Minimum = row.Maximum = 0f;
            }
        }
        _guides.Prepare(_dynamicBodies, _joints, false, true);
        foreach (var joint in _revoluteJoints)
        {
            Array.Clear(joint.Impulses);
            RevoluteConstraintSolver.PrepareJointRows(joint, dt);
            // Finite torque and spring forces have zero impulse over an instantaneous transaction.
            for (var index = 0; index < RevoluteRows.Count; index++)
            {
                if ((RevoluteRowKind)index is not (RevoluteRowKind.Axial or RevoluteRowKind.Drive or RevoluteRowKind.Friction)) continue;
                ref var row = ref joint.Rows[index];
                row.Minimum = row.Maximum = 0f;
            }
        }
        foreach (var transmission in _transmissions)
        { transmission.Impulse = 0f; MechanicalTransmissionSolver.Prepare(transmission); }
        foreach (var route in _tensile)
        { route.Impulse = 0f; TensileConstraintSolver.Prepare(route, dt); }
    }
}
