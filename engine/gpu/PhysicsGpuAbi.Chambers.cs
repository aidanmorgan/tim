using System;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int ChamberRegionBytes = 128;
    public const int ChamberFrameOffset = 8, ChamberColliderOffset = 12, ChamberTargetKindOffset = 16, ChamberTargetOffset = 20;
    public const int ChamberPoseOffset = 24, ChamberRadiusOffset = 52, ChamberHalfLengthOffset = 56;
    public const int ChamberMinimumWidthOffset = 60, ChamberMaximumWidthOffset = 64, ChamberAllowanceOffset = 68;
    public const int ChamberSpeedOffset = 72, ChamberParticipationOffset = 76, ChamberDeclarationBytes = 80;
    public const int ChamberPhaseOffset = 80, ChamberOccupancyOffset = 84, ChamberCandidateOffset = 88, ChamberDepartingOffset = 92;
    public const int ChamberPaddingOffset = 96;
    public const int FiniteWorkActuatorBytes = 64;
    public const int ActuatorStoreOffset = 8, ActuatorRegionOffset = 12, ActuatorSpeedOffset = 16, ActuatorPaddingDistanceOffset = 20;
    public const int ActuatorDeclarationBytes = 24, ActuatorCountOffset = 24, ActuatorResultOffset = 28;
    public const int ActuatorPayloadOffset = 32, ActuatorDebitOffset = 36, ActuatorClearanceOffset = 40, ActuatorPaddingOffset = 44;

    private static void AdmitChambers(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        foreach (var region in scene.ChamberRegions)
        {
            var row = data.Slice(scene.Layout.GetChamberRegionOffset(region.Id), ChamberRegionBytes);
            U64(row, RecordIdentityOffset, region.Id.Value);
            U32(row, ChamberFrameOffset, BodySlot(scene, region.Frame));
            U32(row, ChamberColliderOffset, (uint)scene.Layout.GetColliderSlot(region.ParticipationCollider));
            U32(row, ChamberTargetKindOffset, (uint)region.Targets.Kind);
            U32(row, ChamberTargetOffset, region.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, region.Targets.Body) : NoBody);
            Pose(row, ChamberPoseOffset, region.Pose);
            F32(row, ChamberRadiusOffset, region.Radius); F32(row, ChamberHalfLengthOffset, region.HalfLength);
            F32(row, ChamberMinimumWidthOffset, region.MinimumWidth); F32(row, ChamberMaximumWidthOffset, region.MaximumWidth);
            F32(row, ChamberAllowanceOffset, region.ContainmentAllowance); F32(row, ChamberSpeedOffset, region.MaximumRelativeSpeed);
            U32(row, ChamberParticipationOffset, (uint)region.Participation);
            U32(row, ChamberCandidateOffset, NoBody); U32(row, ChamberDepartingOffset, NoBody);
        }
        foreach (var actuator in scene.FiniteWorkActuators)
        {
            var row = data.Slice(scene.Layout.GetFiniteWorkActuatorOffset(actuator.Id), FiniteWorkActuatorBytes);
            U64(row, RecordIdentityOffset, actuator.Id.Value);
            U32(row, ActuatorStoreOffset, (uint)scene.Layout.GetEnergyStoreSlot(actuator.Store));
            U32(row, ActuatorRegionOffset, (uint)scene.Layout.GetChamberRegionSlot(actuator.Region));
            F32(row, ActuatorSpeedOffset, actuator.TargetSpeed.Value);
            F32(row, ActuatorPaddingDistanceOffset, actuator.ExitPadding);
            U32(row, ActuatorPayloadOffset, NoBody);
        }
    }

    /// <summary>Resolves only exact installed identities, before a no-time candidate can mutate.</summary>
    public static (int Actuator, int Payload) ValidateFiniteWorkRequest(ReadOnlySpan<byte> world, OwnedFiniteWorkRequest request)
    {
        Header(world);
        if (request.Epoch.Value != R64(world, HeaderEpochOffset))
            throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.StaleEpoch);
        var layout = Layout(world);
        var actuator = -1;
        for (var index = 0; index < layout.FiniteWorkActuators; index++)
            if (R64(world, layout.FiniteWorkActuatorOffset(index)) == request.Actuator.Value) actuator = index;
        if (actuator < 0) throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.UnknownActuator);
        var payload = -1;
        for (var index = 0; index < layout.Bodies; index++)
            if (R64(world, layout.BodyOffset(index)) == request.Payload.Value) payload = index;
        if (payload < 0) throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.ForeignTarget);
        if ((RigidMotionKind)R32(world, layout.BodyOffset(payload) + BodyMotionOffset) != RigidMotionKind.Dynamic)
            throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.NonDynamicTarget);
        var region = layout.ChamberRegionOffset((int)R32(world, layout.FiniteWorkActuatorOffset(actuator) + ActuatorRegionOffset));
        if ((uint)payload == R32(world, region + ChamberFrameOffset) ||
            !MatchesTarget(world, (BodyTargetKind)R32(world, region + ChamberTargetKindOffset),
                R32(world, region + ChamberTargetOffset), (uint)payload))
            throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.ForeignTarget);
        return (actuator, payload);
    }

    public static ChamberRegionDeclaration ReadChamberDeclaration(ReadOnlySpan<byte> data, int index)
    {
        var layout = Layout(data);
        var row = data.Slice(layout.ChamberRegionOffset(index), ChamberRegionBytes);
        var frame = ChamberBody(data, layout, R32(row, ChamberFrameOffset), false);
        var colliderSlot = R32(row, ChamberColliderOffset);
        if (colliderSlot >= layout.Colliders) throw new ArgumentException("Unknown chamber participation collider.");
        var colliderOffset = layout.ColliderOffset((int)colliderSlot);
        if (R32(data, colliderOffset + ColliderBodyOffset) != R32(row, ChamberFrameOffset))
            throw new ArgumentException("Chamber frame does not own its collider.");
        var rawTargetKind = R32(row, ChamberTargetKindOffset);
        if (rawTargetKind < (uint)BodyTargetKind.NamedBody || rawTargetKind > (uint)BodyTargetKind.AllPayloads)
            throw new ArgumentException("Undefined chamber target policy.");
        var targetKind = (BodyTargetKind)rawTargetKind;
        var target = ChamberBody(data, layout, R32(row, ChamberTargetOffset), true);
        var poseOffset = ChamberPoseOffset;
        var pose = new RigidLocalPose(new(RF(row, poseOffset), RF(row, poseOffset + 4), RF(row, poseOffset + 8)),
            new(RF(row, poseOffset + 12), RF(row, poseOffset + 16), RF(row, poseOffset + 20), RF(row, poseOffset + 24)));
        var declaration = new ChamberRegionDeclaration(new(R64(row, RecordIdentityOffset)), frame,
            new(R64(data, colliderOffset)), new(targetKind, target), pose, RF(row, ChamberRadiusOffset), RF(row, ChamberHalfLengthOffset),
            RF(row, ChamberMinimumWidthOffset), RF(row, ChamberMaximumWidthOffset), RF(row, ChamberAllowanceOffset),
            RF(row, ChamberSpeedOffset), (SensorParticipation)R32(row, ChamberParticipationOffset));
        declaration.Validate();
        return declaration;
    }

    public static FiniteWorkActuatorDeclaration ReadFiniteWorkActuatorDeclaration(ReadOnlySpan<byte> data, int index)
    {
        var layout = Layout(data);
        var row = data.Slice(layout.FiniteWorkActuatorOffset(index), FiniteWorkActuatorBytes);
        var store = R32(row, ActuatorStoreOffset); var region = R32(row, ActuatorRegionOffset);
        if (store >= layout.EnergyStores || region >= layout.ChamberRegions) throw new ArgumentException("Invalid actuator reference.");
        var declaration = new FiniteWorkActuatorDeclaration(new(R64(row, RecordIdentityOffset)),
            new(R64(data, layout.EnergyStoreOffset((int)store))), new(R64(data, layout.ChamberRegionOffset((int)region))),
            new(RF(row, ActuatorSpeedOffset)), RF(row, ActuatorPaddingDistanceOffset));
        declaration.Validate();
        return declaration;
    }

    public static ChamberRegionRead[] ReadChamberRegions(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Invalid chamber world.");
        var layout = Layout(data);
        var result = new ChamberRegionRead[layout.ChamberRegions];
        ulong previous = 0;
        for (var i = 0; i < result.Length; i++)
        {
            var declaration = ReadChamberDeclaration(data, i);
            var row = data.Slice(layout.ChamberRegionOffset(i), ChamberRegionBytes);
            if (declaration.Id.Value <= previous || !AllZero(row[ChamberPaddingOffset..]))
                throw new ArgumentException("Invalid chamber order or padding.");
            previous = declaration.Id.Value;
            if (R32(row, ChamberPhaseOffset) > (uint)ChamberPhase.GeometryUnresolved) throw new ArgumentException("Undefined chamber phase.");
            var value = new ChamberRegionRead(declaration.Id, declaration.Frame, (ChamberPhase)R32(row, ChamberPhaseOffset),
                R32(row, ChamberOccupancyOffset), ChamberBody(data, layout, R32(row, ChamberCandidateOffset), true),
                ChamberBody(data, layout, R32(row, ChamberDepartingOffset), true));
            value.Validate(); result[i] = value;
        }
        return result;
    }

    public static FiniteWorkActuatorRead[] ReadFiniteWorkActuators(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Invalid actuator world.");
        var layout = Layout(data);
        var result = new FiniteWorkActuatorRead[layout.FiniteWorkActuators];
        ulong previous = 0;
        for (var i = 0; i < result.Length; i++)
        {
            var declaration = ReadFiniteWorkActuatorDeclaration(data, i);
            var row = data.Slice(layout.FiniteWorkActuatorOffset(i), FiniteWorkActuatorBytes);
            var request = R32(row, ActuatorResultOffset);
            var clearance = R32(row, ActuatorClearanceOffset);
            if (declaration.Id.Value <= previous || !AllZero(row[ActuatorPaddingOffset..]) ||
                clearance > (uint)FiniteWorkClearance.GeometryUnresolved ||
                request > byte.MaxValue || request != 0 && !Enum.IsDefined((FiniteWorkRequestResult)(request - 1u)))
                throw new ArgumentException("Invalid actuator order, result or padding.");
            previous = declaration.Id.Value;
            var value = new FiniteWorkActuatorRead(declaration.Id, declaration.Store, declaration.Region,
                R32(row, ActuatorCountOffset), request == 0 ? null : (FiniteWorkRequestResult)(request - 1u),
                ChamberBody(data, layout, R32(row, ActuatorPayloadOffset), true), new(RF(row, ActuatorDebitOffset)), (FiniteWorkClearance)clearance);
            value.Validate();
            var chamber = layout.ChamberRegionOffset((int)R32(row, ActuatorRegionOffset));
            if (((ChamberPhase)R32(data, chamber + ChamberPhaseOffset) == ChamberPhase.Seated) ==
                (value.Clearance == FiniteWorkClearance.NotApplicable))
                throw new ArgumentException("Actuator clearance differs from current chamber eligibility.");
            result[i] = value;
        }
        return result;
    }

    private static GpuBodyId ChamberBody(ReadOnlySpan<byte> data, PhysicsWorldLayout layout, uint slot, bool optionalDynamic)
    {
        if (optionalDynamic && slot == NoBody) return default;
        if (slot >= layout.Bodies) throw new ArgumentException("Unknown chamber body.");
        var offset = layout.BodyOffset((int)slot);
        if (optionalDynamic && (RigidMotionKind)R32(data, offset + BodyMotionOffset) != RigidMotionKind.Dynamic)
            throw new ArgumentException("A chamber payload must be dynamic.");
        return new(R64(data, offset));
    }

    private static void ValidateChamberCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        _ = ReadChamberRegions(candidate);
        var before = ReadFiniteWorkActuators(source); var after = ReadFiniteWorkActuators(candidate);
        for (var i = 0; i < layout.ChamberRegions; i++)
        {
            var offset = layout.ChamberRegionOffset(i);
            if (!source.Slice(offset, ChamberDeclarationBytes).SequenceEqual(candidate.Slice(offset, ChamberDeclarationBytes)))
                throw new ArgumentException("Chamber declaration changed.");
        }
        for (var i = 0; i < layout.FiniteWorkActuators; i++)
        {
            var offset = layout.FiniteWorkActuatorOffset(i);
            if (!source.Slice(offset, ActuatorDeclarationBytes).SequenceEqual(candidate.Slice(offset, ActuatorDeclarationBytes)) ||
                after[i].OccurrenceCount < before[i].OccurrenceCount ||
                (ulong)after[i].OccurrenceCount > (ulong)before[i].OccurrenceCount + 1UL)
                throw new ArgumentException("Actuator declaration or coalesced occurrence count changed illegally.");
        }
    }
}
