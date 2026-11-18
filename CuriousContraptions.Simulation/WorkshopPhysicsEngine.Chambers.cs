using System;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private MaterialQueryWorkspace _materialQueries = new MaterialQueryWorkspace();

    private void EvaluateChambers(Span<byte> world, PhysicsWorldLayout layout)
    {
        if (layout.ChamberRegions == 0) return;
        foreach (var collider in _colliders) WorkshopCollisionKernels.RefreshCollider(collider);
        for (var index = 0; index < layout.ChamberRegions; index++)
            EvaluateChamber(world, layout.ChamberRegionOffset(index));
    }

    private void EvaluateClearances(Span<byte> world, PhysicsWorldLayout layout)
    {
        for (var index = 0; index < layout.FiniteWorkActuators; index++)
        {
            var actuator = world.Slice(layout.FiniteWorkActuatorOffset(index), PhysicsGpuAbi.FiniteWorkActuatorBytes);
            var chamber = world.Slice(layout.ChamberRegionOffset((int)ReadU32(actuator, PhysicsGpuAbi.ActuatorRegionOffset)),
                PhysicsGpuAbi.ChamberRegionBytes);
            var clearance = FiniteWorkClearance.NotApplicable;
            if ((ChamberPhase)ReadU32(chamber, PhysicsGpuAbi.ChamberPhaseOffset) == ChamberPhase.Seated)
            {
                var payload = _allBodies[(int)ReadU32(chamber, PhysicsGpuAbi.ChamberCandidateOffset)];
                var frame = _allBodies[(int)ReadU32(chamber, PhysicsGpuAbi.ChamberFrameOffset)];
                clearance = PayloadExitClearance(payload, frame, ChamberShape(chamber, frame),
                    ReadF32(actuator, PhysicsGpuAbi.ActuatorPaddingDistanceOffset)) switch
                {
                    MaterialQueryResult.Clear => FiniteWorkClearance.Clear,
                    MaterialQueryResult.Intersecting => FiniteWorkClearance.Obstructed,
                    MaterialQueryResult.Unresolved => FiniteWorkClearance.GeometryUnresolved,
                    _ => throw new InvalidOperationException("Undefined material query result.")
                };
            }
            WriteU32(actuator, PhysicsGpuAbi.ActuatorClearanceOffset, (uint)clearance);
        }
    }

    private void EvaluateChamber(Span<byte> world, int offset)
    {
        var row = world.Slice(offset, PhysicsGpuAbi.ChamberRegionBytes);
        var frame = _allBodies[(int)ReadU32(row, PhysicsGpuAbi.ChamberFrameOffset)];
        var collider = _colliders[(int)ReadU32(row, PhysicsGpuAbi.ChamberColliderOffset)];
        if ((SensorParticipation)ReadU32(row, PhysicsGpuAbi.ChamberParticipationOffset) == SensorParticipation.Disabled ||
            collider.Participation == CollisionParticipation.Disabled)
        {
            WriteChamber(row, ChamberPhase.Empty, 0, PhysicsGpuAbi.NoBody, PhysicsGpuAbi.NoBody);
            return;
        }
        var region = ChamberShape(row, frame);
        var departing = ReadU32(row, PhysicsGpuAbi.ChamberDepartingOffset);
        var occupancy = ChamberOccupancy(world, row, region, frame, ref departing);
        if (occupancy.Unresolved)
        {
            WriteChamber(row, ChamberPhase.GeometryUnresolved, 0, PhysicsGpuAbi.NoBody, departing);
            return;
        }
        var phase = occupancy.Count switch
        {
            0 => ChamberPhase.Empty,
            1 => ChamberPhase.Loading,
            _ => ChamberPhase.Ambiguous
        };
        if (departing != PhysicsGpuAbi.NoBody) phase = ChamberPhase.Departing;
        else if (occupancy.Count == 1)
            phase = SeatInChamber(row, region, frame, _allBodies[(int)occupancy.Candidate]);
        if (phase == ChamberPhase.GeometryUnresolved)
            WriteChamber(row, phase, 0, PhysicsGpuAbi.NoBody, departing);
        else WriteChamber(row, phase, occupancy.Count, occupancy.Candidate, departing);
    }

    private readonly record struct Occupancy(uint Count, uint Candidate, bool Unresolved);

    private Occupancy ChamberOccupancy(ReadOnlySpan<byte> world, ReadOnlySpan<byte> row, HollowPartner region,
        SimBody frame, ref uint departing)
    {
        var count = 0u; var candidate = PhysicsGpuAbi.NoBody; var unresolved = false;
        var targetKind = (BodyTargetKind)ReadU32(row, PhysicsGpuAbi.ChamberTargetKindOffset);
        var target = ReadU32(row, PhysicsGpuAbi.ChamberTargetOffset);
        foreach (var body in _dynamicBodies)
        {
            if (body == frame || !PhysicsGpuAbi.MatchesTarget(world, targetKind, target, (uint)body.BodySlot)) continue;
            var overlap = BodyRegionOverlap(body, region);
            if ((uint)body.BodySlot == departing && overlap == MaterialQueryResult.Clear) departing = PhysicsGpuAbi.NoBody;
            if (overlap == MaterialQueryResult.Unresolved) { unresolved = true; continue; }
            if (overlap != MaterialQueryResult.Intersecting) continue;
            count++;
            candidate = count == 1 ? (uint)body.BodySlot : PhysicsGpuAbi.NoBody;
        }
        return new(count, candidate, unresolved);
    }

    private MaterialQueryResult BodyRegionOverlap(SimBody body, HollowPartner region)
    {
        var unresolved = false;
        foreach (var collider in _colliders)
        {
            if (collider.Body != body || collider.Participation == CollisionParticipation.Disabled) continue;
            var result = _materialQueries.SweepMaterial(WorkshopCollisionKernels.Partner(collider), region, Vector3.Zero);
            if (result == MaterialQueryResult.Intersecting) return result;
            unresolved |= result == MaterialQueryResult.Unresolved;
        }
        return unresolved ? MaterialQueryResult.Unresolved : MaterialQueryResult.Clear;
    }

    private ChamberPhase SeatInChamber(ReadOnlySpan<byte> row, HollowPartner region, SimBody frame, SimBody payload)
    {
        var allowance = ReadF32(row, PhysicsGpuAbi.ChamberAllowanceOffset);
        var minimum = new Vector2(float.PositiveInfinity); var maximum = new Vector2(float.NegativeInfinity);
        var inside = true; var unresolved = false;
        foreach (var collider in _colliders)
        {
            if (collider.Body != payload || collider.Participation == CollisionParticipation.Disabled) continue;
            var shape = WorkshopCollisionKernels.Partner(collider);
            var containment = _materialQueries.ContainsMaterial(region, shape, allowance);
            unresolved |= containment == MaterialContainmentResult.Unresolved;
            inside &= containment == MaterialContainmentResult.Inside;
            minimum = Vector2.Min(minimum, new(
                Vector3.Dot(MaterialSupport.Point(shape, -region.U0) - region.Position, region.U0),
                Vector3.Dot(MaterialSupport.Point(shape, -region.U2) - region.Position, region.U2)));
            maximum = Vector2.Max(maximum, new(
                Vector3.Dot(MaterialSupport.Point(shape, region.U0) - region.Position, region.U0),
                Vector3.Dot(MaterialSupport.Point(shape, region.U2) - region.Position, region.U2)));
        }
        if (unresolved) return ChamberPhase.GeometryUnresolved;
        var width = maximum - minimum;
        var low = ReadF32(row, PhysicsGpuAbi.ChamberMinimumWidthOffset);
        var high = ReadF32(row, PhysicsGpuAbi.ChamberMaximumWidthOffset);
        var carrierVelocity = frame.LinearVelocity + Vector3.Cross(frame.AngularVelocity, payload.CentreOfMass - frame.CentreOfMass);
        var relativeSpeed = (payload.LinearVelocity - carrierVelocity).Length();
        return inside && width.X >= low && width.X <= high && width.Y >= low && width.Y <= high &&
            relativeSpeed <= ReadF32(row, PhysicsGpuAbi.ChamberSpeedOffset) ? ChamberPhase.Seated : ChamberPhase.Loading;
    }


    private MaterialQueryResult PayloadExitClearance(SimBody payload, SimBody frame, HollowPartner region, float padding)
    {
        var rear = float.PositiveInfinity;
        foreach (var collider in _colliders)
        {
            if (collider.Body != payload || collider.Participation == CollisionParticipation.Disabled) continue;
            var support = MaterialSupport.Point(WorkshopCollisionKernels.Partner(collider), -region.U1);
            rear = MathF.Min(rear, Vector3.Dot(support - region.Position, region.U1));
        }
        // Translate the complete participating material until its rearmost support clears the muzzle.
        var distance = MathF.Max(0f, region.Hy - rear + padding);
        if (!float.IsFinite(distance)) return MaterialQueryResult.Unresolved;
        var translation = region.U1 * distance;
        var unresolved = false;
        foreach (var moving in _colliders)
        {
            if (moving.Body != payload || moving.Participation == CollisionParticipation.Disabled) continue;
            var shape = WorkshopCollisionKernels.Partner(moving);
            foreach (var obstacle in _colliders)
            {
                if (obstacle.Body == payload || obstacle.Body == frame ||
                    obstacle.Participation == CollisionParticipation.Disabled) continue;
                var result = _materialQueries.SweepMaterial(shape, WorkshopCollisionKernels.Partner(obstacle), translation);
                if (result == MaterialQueryResult.Intersecting) return result;
                unresolved |= result == MaterialQueryResult.Unresolved;
            }
        }
        return unresolved ? MaterialQueryResult.Unresolved : MaterialQueryResult.Clear;
    }

    private static HollowPartner ChamberShape(ReadOnlySpan<byte> row, SimBody frame)
    {
        var (position, rotation) = ReadPose(row, PhysicsGpuAbi.ChamberPoseOffset);
        var turn = Quaternion.Normalize(Quaternion.Multiply(frame.Rotation, rotation));
        return new(CuriousContraptions.Gpu.ColliderShapeKind.Cylinder, frame.Position + Vector3.Transform(position, frame.Rotation),
            Vector3.Transform(Vector3.UnitX, turn), Vector3.Transform(Vector3.UnitY, turn), Vector3.Transform(Vector3.UnitZ, turn),
            ReadF32(row, PhysicsGpuAbi.ChamberRadiusOffset), 0f, ReadF32(row, PhysicsGpuAbi.ChamberHalfLengthOffset), 0f);
    }

    private static void WriteChamber(Span<byte> row, ChamberPhase phase, uint count, uint candidate, uint departing)
    {
        WriteU32(row, PhysicsGpuAbi.ChamberPhaseOffset, (uint)phase);
        WriteU32(row, PhysicsGpuAbi.ChamberOccupancyOffset, count);
        WriteU32(row, PhysicsGpuAbi.ChamberCandidateOffset, candidate);
        WriteU32(row, PhysicsGpuAbi.ChamberDepartingOffset, departing);
    }
}
