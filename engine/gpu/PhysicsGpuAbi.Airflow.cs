using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int AirflowOwnerOffset = 8;
    public const int AirflowFrameOffset = 12;
    public const int AirflowOriginOffset = 16;
    public const int AirflowAxisOffset = 28;
    public const int AirflowReachOffset = 40;
    public const int AirflowRadiusOffset = 44;
    public const int AirflowSpeedOffset = 48;
    public const int AirflowForceOffset = 52;
    public const int AirflowPowerOffset = 56;
    public const int AirflowPressureOffset = 60;
    public const int AirflowInputKindOffset = 64;
    public const int AirflowStoreOffset = 68;
    public const int AirflowElectricalOffset = 72;
    public const int AirflowCoordinateOffset = 76;
    public const int AirflowSenseOffset = 80;
    public const int AirflowConnectedOffset = 84;
    public const int AirflowExclusionStartOffset = 88;
    public const int AirflowExclusionCountOffset = 92;
    public const int AirflowEnableOffset = 96;
    public const int AirflowInitiallyEnabledOffset = 104;
    public const int AirflowAvailabilityOffset = 108;
    public const int AirflowObservedSpeedOffset = 112;
    public const int AirflowObservedPowerOffset = 116;
    public const int AirflowWorkOffset = 120;
    public const int AirflowLossOffset = 124;
    public const int AirflowCommitWorkOffset = 128;
    public const int AirflowCommitLossOffset = 132;
    private const int AirflowPaddingOffset = 136;
    public const int AirflowReceiverColliderOffset = 12;
    public const int AirflowSamplingOffset = 16;
    public const int AirflowSampleStartOffset = 20;
    public const int AirflowSampleCountOffset = 24;

    private static void AdmitAirflow(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        var topology = new SupplyTopology(scene.SupplyGraph.Nodes.ToArray(), scene.SupplyGraph.Edges.ToArray());
        var exclusion = 0;
        for (var i = 0; i < scene.AirflowSources.Length; i++)
        {
            var source = scene.AirflowSources[i];
            var row = data.Slice(scene.World.AirflowSourceOffset(i), AirflowSourceBytes);
            U64(row, RecordIdentityOffset, source.Id.Value);
            U32(row, AirflowOwnerOffset, BodySlot(scene, source.Owner));
            U32(row, AirflowFrameOffset, BodySlot(scene, source.Frame));
            AirflowVector(row, AirflowOriginOffset, source.Origin);
            AirflowVector(row, AirflowAxisOffset, source.Axis);
            F32(row, AirflowReachOffset, source.Reach);
            F32(row, AirflowRadiusOffset, source.TerminalRadius);
            F32(row, AirflowSpeedOffset, source.FlowSpeed);
            F32(row, AirflowForceOffset, source.MaximumForce);
            F32(row, AirflowPowerOffset, source.MaximumPower);
            F32(row, AirflowPressureOffset, source.PressureScale);
            U32(row, AirflowInputKindOffset, (uint)source.Input.Kind);
            WriteAirflowInput(row, source.Input, scene, topology);
            U32(row, AirflowExclusionStartOffset, checked((uint)exclusion));
            U32(row, AirflowExclusionCountOffset, checked((uint)source.InternalOccluders.Length));
            foreach (var collider in source.InternalOccluders)
                U32(data, scene.World.AirflowExclusionOffset(exclusion++), checked((uint)scene.Layout.GetColliderSlot(collider)));
            U64(row, AirflowEnableOffset, source.Enable?.Value ?? 0);
            U32(row, AirflowInitiallyEnabledOffset, source.InitiallyEnabled ? 1u : 0u);
        }
        var sample = 0;
        for (var i = 0; i < scene.AirflowReceivers.Length; i++)
        {
            var receiver = scene.AirflowReceivers[i];
            var row = data.Slice(scene.World.AirflowReceiverOffset(i), AirflowReceiverBytes);
            U64(row, RecordIdentityOffset, receiver.Id.Value);
            U32(row, AirflowOwnerOffset, BodySlot(scene, receiver.Owner));
            U32(row, AirflowReceiverColliderOffset, checked((uint)scene.Layout.GetColliderSlot(receiver.Collider)));
            U32(row, AirflowSamplingOffset, (uint)receiver.Sampling);
            U32(row, AirflowSampleStartOffset, checked((uint)sample));
            U32(row, AirflowSampleCountOffset, checked((uint)receiver.Samples.Length));
            foreach (var point in receiver.Samples)
            {
                var offset = scene.World.AirflowSampleOffset(sample++);
                AirflowVector(data, offset, point.Position);
                F32(data, offset + 12, point.Weight);
            }
        }
        AdmitRotaryAirflow(data, scene, sample, exclusion);
    }

    private static void WriteAirflowInput(Span<byte> row, ContinuousWorkInput input,
        IPhysicsSceneDeclaration scene, SupplyTopology topology)
    {
        U32(row, AirflowStoreOffset, input.Kind == ContinuousWorkInputKind.Store
            ? checked((uint)scene.Layout.GetEnergyStoreSlot(input.Store)) : NoBody);
        U32(row, AirflowCoordinateOffset, input.Kind == ContinuousWorkInputKind.Coordinate
            ? checked((uint)scene.Layout.GetRevoluteSlot(input.Coordinate.Value)) : NoBody);
        var electrical = NoBody;
        if (input.Kind == ContinuousWorkInputKind.Electrical)
        {
            var upstream = topology.UpstreamSources(input.Electrical);
            if (upstream.Length > 1) throw new ArgumentException("A continuous-work input has multiple potential sources.");
            if (upstream.Length == 1) electrical = checked((uint)scene.Layout.GetElectricalSourceSlot(upstream[0]));
        }
        U32(row, AirflowElectricalOffset, electrical);
        F32(row, AirflowSenseOffset, input.Sense);
        U32(row, AirflowConnectedOffset, input.Connected ? 1u : 0u);
    }

    private static void AirflowVector(Span<byte> row, int offset, Vector3 value) =>
        F32Vector(row, offset, value.X, value.Y, value.Z);

    public static AirflowSourceRead[] ReadAirflow(ReadOnlySpan<byte> world)
    {
        var layout = Layout(world);
        var values = new AirflowSourceRead[layout.AirflowSources];
        for (var i = 0; i < values.Length; i++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(i), AirflowSourceBytes);
            var owner = R32(row, AirflowOwnerOffset);
            if (owner >= layout.Bodies || R32(row, AirflowAvailabilityOffset) > (uint)AirflowAvailability.Available ||
                !AllZero(row[AirflowPaddingOffset..])) throw new ArgumentException("Invalid airflow owner, availability or padding.");
            var value = new AirflowSourceRead(new(R64(row, RecordIdentityOffset)),
                new(R64(world, layout.BodyOffset((int)owner))), (ContinuousWorkInputKind)R32(row, AirflowInputKindOffset),
                (AirflowAvailability)R32(row, AirflowAvailabilityOffset), RF(row, AirflowObservedSpeedOffset),
                RF(row, AirflowObservedPowerOffset), new(RF(row, AirflowWorkOffset)), new(RF(row, AirflowLossOffset)),
                new(RF(row, AirflowCommitWorkOffset)), new(RF(row, AirflowCommitLossOffset)));
            value.Validate();
            values[i] = value;
        }
        return values;
    }

    private static EnergyStoreDebit[] ReadAirflowStoreDebits(ReadOnlySpan<byte> world)
    {
        var layout = Layout(world);
        var payments = new System.Collections.Generic.List<EnergyStoreDebit>();
        for (var i = 0; i < layout.AirflowSources; i++)
        {
            var row = world.Slice(layout.AirflowSourceOffset(i), AirflowSourceBytes);
            if ((ContinuousWorkInputKind)R32(row, AirflowInputKindOffset) != ContinuousWorkInputKind.Store) continue;
            var slot = R32(row, AirflowStoreOffset);
            if (slot >= layout.EnergyStores) throw new ArgumentException("Unresolved airflow store.");
            var debit = RF(row, AirflowCommitWorkOffset);
            payments.Add(new(new(R64(world, layout.EnergyStoreOffset((int)slot))), new(debit)));
        }
        return payments.ToArray();
    }

    private static void ValidateAirflowCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        for (var i = 0; i < layout.AirflowSources; i++)
        {
            var offset = layout.AirflowSourceOffset(i);
            if (!candidate.Slice(offset, AirflowAvailabilityOffset).SequenceEqual(source.Slice(offset, AirflowAvailabilityOffset)))
                throw new ArgumentException("Airflow declaration changed during simulation.");
        }
        if (!candidate[layout.AirflowReceiversOffset..layout.CollidersOffset].SequenceEqual(
                source[layout.AirflowReceiversOffset..layout.CollidersOffset]))
            throw new ArgumentException("Airflow geometry bindings changed during simulation.");
        var before = ReadAirflow(source);
        var after = ReadAirflow(candidate);
        var steps = R32(candidate, HeaderSourceOrdinalOffset) - R32(source, HeaderSourceOrdinalOffset);
        for (var i = 0; i < after.Length; i++)
            ValidateAirflowAdvance(source.Slice(layout.AirflowSourceOffset(i), AirflowSourceBytes), before[i], after[i], steps);
    }

    private static void ValidateAirflowAdvance(ReadOnlySpan<byte> declaration,
        AirflowSourceRead before, AirflowSourceRead after, uint steps)
    {
        PhysicsReadValidation.ValidateSummary(before.Work.Value, after.Work.Value, after.CommitWork.Value, steps);
        PhysicsReadValidation.ValidateSummary(before.JetLoss.Value, after.JetLoss.Value, after.CommitLoss.Value, steps);
        var maximumPower = RF(declaration, AirflowPowerOffset);
        var maximumWork = maximumPower * (steps / (float)CuriousContraptions.Abstractions.PhysicalTime.Frequency);
        if (after.Work.Value < before.Work.Value || after.JetLoss.Value < before.JetLoss.Value ||
            after.CommitWork.Value > maximumWork + PhysicsReadValidation.Residual(maximumWork))
            throw new ArgumentException("Airflow cumulative work exceeded its elapsed power allowance or decreased.");
        if (after.FlowSpeed != 0 && after.FlowSpeed != RF(declaration, AirflowSpeedOffset) ||
            after.TransferPower > maximumPower + PhysicsReadValidation.Residual(maximumPower) ||
            after.FlowSpeed == 0 && after.TransferPower != 0)
            throw new ArgumentException("Airflow observation exceeded its declared flow or power rating.");
    }
}
