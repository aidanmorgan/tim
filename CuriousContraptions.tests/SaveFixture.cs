using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Hand-built stored saves, framed exactly as the save codec frames one (size-prefixed "CCWS" root), and the typed
/// assertion of a save's refusal with the reason that refused it.</summary>
internal static class SaveFixture
{
    /// <summary>The loaded bundle's elements, as the registry loads them (Story 9A.15): every bundle entry the typed undelivered set
    /// does not hold back. The save boundary validates every saved element id against this set.</summary>
    public static IReadOnlySet<PartId> Elements { get; } = PartRegistry.Read(RuntimeTestServices.Bundle, File.ReadAllText(
            Path.Combine(twodog.Engine.ResolveProjectDir(), "elements", "elements.bundle.json")))
        .Select(entry => new PartId(entry.Metadata.Id)).ToHashSet();

    public static byte[] Finish(FlatBufferBuilder builder, Offset<ConstructionRecord> record, ulong nextBodyId)
    {
        SavedConstruction.StartSavedConstruction(builder);
        SavedConstruction.AddNextBodyId(builder, nextBodyId);
        SavedConstruction.AddConstruction(builder, record);
        builder.FinishSizePrefixed(SavedConstruction.EndSavedConstruction(builder).Value, WorkshopSaveCodec.Identifier);
        return builder.SizedByteArray();
    }

    /// <summary>A save of <paramref name="construction"/> encoded without its domain validation (a forged or tampered save).</summary>
    public static byte[] Forged(WorkshopConstruction construction, ulong nextBodyId)
    {
        var builder = new FlatBufferBuilder(1024);
        return Finish(builder, Record(builder, construction), nextBodyId);
    }

    public static WorkshopSaveRejectedException Refused(WorkshopSaveRejection reason, Action decode)
    {
        var refusal = Assert.Throws<WorkshopSaveRejectedException>(decode);
        Assert.Equal(reason, refusal.Reason);
        return refusal;
    }

    internal readonly record struct Connection(ulong Source, ulong Target, uint Domain, uint Output, uint Input,
        float? RopeLength = null);

    /// <summary>Direct generated-wire fixture. Invalid field values are retained exactly; no production encoder bypass.</summary>
    public static Offset<ConstructionRecord> Record(FlatBufferBuilder builder, WorkshopConstruction construction,
        IReadOnlyList<Connection>? storedConnections = null)
    {
        var instances = construction.Instances.Select(instance => Instance(builder, instance)).ToArray();
        var instanceVector = ConstructionRecord.CreateInstancesVector(builder, instances);
        var puzzle = Puzzle(builder, construction.Puzzle);
        var connections = storedConnections ?? construction.Connections.Select(link => new Connection(link.Source.Value,
            link.Target.Value, (uint)link.Domain, Socket(construction, link.Source, link.Output),
            Socket(construction, link.Target, link.Input), link.RopeLength)).ToArray();
        VectorOffset? links = null;
        if (connections.Count != 0)
        {
            ConstructionRecord.StartConnectionsVector(builder, connections.Count);
            foreach (var link in connections.Reverse())
                ConnectionRecord.CreateConnectionRecord(builder, link.Source, link.Target, link.Domain, link.Output, link.Input,
                    link.RopeLength.HasValue, link.RopeLength.GetValueOrDefault());
            links = builder.EndVector();
        }
        var settings = construction.Settings;
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, construction.Revision.Value);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
            (uint)settings.Simulation, (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation,
            settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, instanceVector);
        if (links is { } encodedConnections) ConstructionRecord.AddConnections(builder, encodedConnections);
        if (puzzle is { } authored) ConstructionRecord.AddPuzzle(builder, authored);
        return ConstructionRecord.EndConstructionRecord(builder);
    }

    private static uint Socket(WorkshopConstruction construction, GpuBodyId owner, PortId port)
    {
        var instance = construction.Instances.Single(instance => instance.Id == owner);
        var ports = DeclaredPorts.Current.Of(instance.PartId).ToArray();
        return checked((uint)Array.FindIndex(ports, declared => declared.Id == port));
    }

    private static Offset<InstanceRecord> Instance(FlatBufferBuilder builder, IWorkshopInstance instance)
    {
        var part = builder.CreateString(instance.PartId.Value);
        VectorOffset? parameters = instance is WorkshopGenericInstance generic
            ? InstanceRecord.CreateParametersVector(builder, generic.Parameters.Select(pair =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray())
            : null;
        InstanceRecord.StartInstanceRecord(builder);
        InstanceRecord.AddPartId(builder, part);
        InstanceRecord.AddId(builder, instance.Id.Value);
        InstanceRecord.AddLocked(builder, instance.Locked);
        InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, instance.Cell.X, instance.Cell.Y, instance.Cell.Z));
        InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, instance.Local.X, instance.Local.Y, instance.Local.Z));
        InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));
        if (parameters is { } values) InstanceRecord.AddParameters(builder, values);
        return InstanceRecord.EndInstanceRecord(builder);
    }

    private static Offset<PuzzleRecord>? Puzzle(FlatBufferBuilder builder, WorkshopPuzzle puzzle)
    {
        if (!puzzle.Id.IsAuthored) return null;
        var id = builder.CreateString(puzzle.Id.Value);
        var inventory = puzzle.InventoryPart == default ? default : builder.CreateString(puzzle.InventoryPart.Value);
        var source = puzzle.Goal.SourceNode == default ? default : builder.CreateString(puzzle.Goal.SourceNode.Value);
        var target = puzzle.Goal.TargetNode == default ? default : builder.CreateString(puzzle.Goal.TargetNode.Value);
        AssistanceKnot[] knots =
        [
            puzzle.PrimaryAssistance.Forgiving, puzzle.PrimaryAssistance.Balanced, puzzle.PrimaryAssistance.Precise,
            puzzle.TargetAssistance.Forgiving, puzzle.TargetAssistance.Balanced, puzzle.TargetAssistance.Precise,
            puzzle.PlacementAssistance.Forgiving, puzzle.PlacementAssistance.Balanced, puzzle.PlacementAssistance.Precise
        ];
        PuzzleRecord.StartAssistanceVector(builder, knots.Length);
        foreach (var knot in knots.Reverse())
            AssistanceKnotRecord.CreateAssistanceKnotRecord(builder, knot.Precision, knot.PositionWindow.Value,
                knot.RotationWindowDegrees, knot.MaximumPositionCorrection.Value, knot.MaximumRotationCorrectionDegrees,
                knot.Blend.Value, knot.CaptureMargin.Value, knot.CaptureSpeed.Value, knot.CaptureDwell.Value,
                knot.GuideAcceleration.Value, knot.TriggerThreshold);
        var assistance = builder.EndVector();
        return PuzzleRecord.CreatePuzzleRecord(builder, id, (uint)puzzle.Placement, puzzle.Precision.Value, inventory,
            puzzle.InventoryCount, (uint)puzzle.Goal.Kind, puzzle.Goal.Body.Value, puzzle.Goal.Target.Value,
            puzzle.Goal.MinimumDuration.Value, source, target, assistance, puzzle.InteractionTarget.Value);
    }
}
