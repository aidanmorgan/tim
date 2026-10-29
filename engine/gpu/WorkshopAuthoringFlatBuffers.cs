using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Google.FlatBuffers;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>Main-owned authored content and save records.</summary>
public static class WorkshopAuthoringFlatBuffers
{
    /// <summary>Content uses the same current record constructor as wire input, after strict content/package admission.</summary>
    internal static IWorkshopInstance MapAuthoredInstance(AuthoredInstanceTemplate template, GpuBodyId id)
    {
        var builder = new FlatBufferBuilder(512);
        var part = builder.CreateString(template.Part.Value);
        var parameterOffsets = template.Parameters.Select(pair =>
            ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray();
        var parameters = InstanceRecord.CreateParametersVector(builder, parameterOffsets);
        InstanceRecord.StartInstanceRecord(builder);
        InstanceRecord.AddId(builder, id.Value);
        InstanceRecord.AddPartId(builder, part);
        InstanceRecord.AddLocked(builder, template.Locked);
        InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, template.Cell.X, template.Cell.Y, template.Cell.Z));
        InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, template.Local.X, template.Local.Y, template.Local.Z));
        InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, template.Rotation.X, template.Rotation.Y, template.Rotation.Z, template.Rotation.W));
        InstanceRecord.AddParameters(builder, parameters);
        builder.Finish(InstanceRecord.EndInstanceRecord(builder).Value);
        return MapInstance(InstanceRecord.GetRootAsInstanceRecord(new ByteBuffer(builder.SizedByteArray())));
    }
    internal static ByteBuffer OpenFrame(ReadOnlySpan<byte> bytes, string identifier, VerifyTableAction verify)
    {
        var result = WorkshopWireFlatBuffers.OpenFrame(bytes, identifier, verify);
        if (!CurrentPuzzleTable(new ByteBuffer(bytes.ToArray()), identifier))
            throw new WireFaultException(WireFaultReason.VerifierRejected, "Unsupported authored puzzle fields.");
        return result;
    }
    /// <summary>Current authored puzzle tables have ten fields and require their final assistance vector.
    /// Reject unknown table fields before decoding; never reinterpret a superseded table layout.</summary>
    private static bool CurrentPuzzleTable(ByteBuffer buffer, string identifier)
    {
        const int savedConstructionField = 1, constructionPuzzleField = 2;
        const int puzzleVtableBytes = 4 + 2 * 10;
        var constructionField = identifier switch
        {
            WorkshopSaveCodec.Identifier => savedConstructionField,
            _ => -1
        };
        if (constructionField < 0) return true;
        var root = checked(FlatBufferConstants.SizePrefixLength + buffer.GetInt(FlatBufferConstants.SizePrefixLength));
        var construction = Reference(root, constructionField);
        if (construction == 0) return true;
        var puzzle = Reference(construction, constructionPuzzleField);
        if (puzzle == 0) return true;
        var vtable = checked(puzzle - buffer.GetInt(puzzle));
        return buffer.GetUshort(vtable) == puzzleVtableBytes;

        int Reference(int table, int field)
        {
            var vtable = checked(table - buffer.GetInt(table));
            var slot = 4 + 2 * field;
            if (slot >= buffer.GetUshort(vtable)) return 0;
            var offset = buffer.GetUshort(vtable + slot);
            if (offset == 0) return 0;
            var reference = checked(table + offset);
            return checked(reference + buffer.GetInt(reference));
        }
    }

    /// <summary>The current <c>InstanceRecord</c> table's field count (schemas/workshop_authoring.fbs).</summary>
    internal const int InstanceRecordFields = 7;
    /// <summary>Current authoring-table vtable slots: saved construction (field 1) and its instances (field 3).</summary>
    internal const int SavedConstructionSlot = 6, ConstructionInstancesSlot = 10;

    /// <summary>Whether any instance record of the verified root's construction holds a present field in a slot the current
    /// <c>InstanceRecord</c> does not define (an older layout's or a forged record's). Such a field is refused at both decode
    /// boundaries, never silently ignored. Reads only vtable entries inside each verified vtable.</summary>
    internal static bool HoldsFieldBeyondTheInstanceTable(ByteBuffer bb, int constructionSlot)
    {
        var root = new Table(bb.GetInt(bb.Position) + bb.Position, bb);
        var field = root.__offset(constructionSlot);
        if (field == 0) return false;
        var construction = new Table(root.__indirect(field + root.bb_pos), bb);
        var instances = construction.__offset(ConstructionInstancesSlot);
        if (instances == 0) return false;
        var vector = construction.__vector(instances);
        for (var i = 0; i < construction.__vector_len(instances); i++)
        {
            var table = construction.__indirect(vector + i * 4);
            var vtable = table - bb.GetInt(table);
            var size = bb.GetUshort(vtable);
            for (var slot = InstanceRecordFields; 4 + 2 * slot + 2 <= size; slot++)
                if (bb.GetUshort(vtable + 4 + 2 * slot) != 0) return true;
        }
        return false;
    }

    /// <summary>Encode the current authored construction for main-owned persistence and authored-record fixtures.</summary>
    public static Offset<ConstructionRecord> EncodeConstructionRecord(FlatBufferBuilder builder, WorkshopConstruction construction, bool validate = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (validate) construction.Validate();

        Offset<PuzzleRecord>? puzzleOffset = null;
        if (construction.Puzzle.Id.IsAuthored)
        {
            var puzzle = construction.Puzzle;
            var puzzleIdOffset = builder.CreateString(puzzle.Id.Value);
            var invPartOffset = puzzle.InventoryPart != default ? builder.CreateString(puzzle.InventoryPart.Value) : default;

            var assistVec = EncodeAssistance(builder, puzzle);

            var goalBody = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.SourceNode.Value : puzzle.Goal.Body.Value;
            var goalTarget = puzzle.Goal.Kind == WorkshopGoalKind.ActivatedAfter ? puzzle.Goal.TargetNode.Value : puzzle.Goal.Target.Value;

            PuzzleRecord.StartPuzzleRecord(builder);
            PuzzleRecord.AddId(builder, puzzleIdOffset);
            PuzzleRecord.AddPlacement(builder, (uint)puzzle.Placement);
            PuzzleRecord.AddPrecision(builder, puzzle.Precision.Value);
            if (puzzle.InventoryPart != default) PuzzleRecord.AddInventoryPart(builder, invPartOffset);
            PuzzleRecord.AddInventoryCount(builder, puzzle.InventoryCount);
            PuzzleRecord.AddGoalKind(builder, (uint)puzzle.Goal.Kind);
            PuzzleRecord.AddGoalBody(builder, goalBody);
            PuzzleRecord.AddGoalTarget(builder, goalTarget);
            PuzzleRecord.AddGoalDelay(builder, puzzle.Goal.MinimumDuration.Value);
            PuzzleRecord.AddAssistance(builder, assistVec);
            puzzleOffset = PuzzleRecord.EndPuzzleRecord(builder);
        }

        var instanceOffsets = new Offset<InstanceRecord>[construction.Instances.Count];
        for (var i = 0; i < construction.Instances.Count; i++)
        {
            var instance = construction.Instances[i];
            var partIdOffset = builder.CreateString(instance.PartId.Value);
            VectorOffset? parameterVector = null;
            if (instance is WorkshopGenericInstance generic)
            {
                var parameters = generic.Parameters.Select(pair =>
                    ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray();
                parameterVector = InstanceRecord.CreateParametersVector(builder, parameters);
            }

            InstanceRecord.StartInstanceRecord(builder);
            if (parameterVector.HasValue) InstanceRecord.AddParameters(builder, parameterVector.Value);
            InstanceRecord.AddId(builder, instance.Id.Value);
            InstanceRecord.AddPartId(builder, partIdOffset);
            InstanceRecord.AddLocked(builder, instance.Locked);
            InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, instance.Cell.X, instance.Cell.Y, instance.Cell.Z));
            InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, instance.Local.X, instance.Local.Y, instance.Local.Z));
            InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W));


            instanceOffsets[i] = InstanceRecord.EndInstanceRecord(builder);
        }
        var instancesVector = ConstructionRecord.CreateInstancesVector(builder, instanceOffsets);

        VectorOffset? connectionsVector = null;
        if (construction.Connections.Count > 0)
        {
            ConstructionRecord.StartConnectionsVector(builder, construction.Connections.Count);
            for (var i = construction.Connections.Count - 1; i >= 0; i--)
            {
                var link = construction.Connections[i];
                ConnectionRecord.CreateConnectionRecord(builder, link.Source.Value, link.Target.Value, (uint)link.Domain, (uint)link.Output, (uint)link.Input);
            }
            connectionsVector = builder.EndVector();
        }

        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, construction.Revision.Value);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
            (uint)construction.Settings.Simulation, (uint)construction.Settings.Physical, (uint)construction.Settings.Animation,
            (uint)construction.Settings.Presentation, construction.Settings.PresentationRate.Numerator, construction.Settings.PresentationRate.Denominator));
        if (puzzleOffset.HasValue) ConstructionRecord.AddPuzzle(builder, puzzleOffset.Value);
        ConstructionRecord.AddInstances(builder, instancesVector);
        if (connectionsVector.HasValue) ConstructionRecord.AddConnections(builder, connectionsVector.Value);
        return ConstructionRecord.EndConstructionRecord(builder);
    }

    /// <summary>Decodes and validates a construction record (the Construct command's): the domain rules reject an inadmissible one.</summary>
    public static WorkshopConstruction DecodeConstructionRecord(ConstructionRecord cr)
    {
        var construction = MapConstructionRecord(cr);
        construction.Validate();
        return construction;
    }

    /// <summary>A puzzle record's assistance: the primary, target and placement profiles, three f32 knots each (Story 9A.44).</summary>
    internal const int KnotsPerProfile = 3;
    internal const int AssistanceKnotCount = 3 * KnotsPerProfile;

    private static VectorOffset EncodeAssistance(FlatBufferBuilder builder, WorkshopPuzzle puzzle)
    {
        AssistanceKnot[] knots =
        [
            puzzle.PrimaryAssistance.Forgiving, puzzle.PrimaryAssistance.Balanced, puzzle.PrimaryAssistance.Precise,
            puzzle.TargetAssistance.Forgiving, puzzle.TargetAssistance.Balanced, puzzle.TargetAssistance.Precise,
            puzzle.PlacementAssistance.Forgiving, puzzle.PlacementAssistance.Balanced, puzzle.PlacementAssistance.Precise,
        ];
        PuzzleRecord.StartAssistanceVector(builder, knots.Length);
        for (var i = knots.Length - 1; i >= 0; i--)
        {
            var k = knots[i];
            AssistanceKnotRecord.CreateAssistanceKnotRecord(builder, k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees,
                k.MaximumPositionCorrection.Value, k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value,
                k.CaptureSpeed.Value, k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold);
        }
        return builder.EndVector();
    }

    private static AssistanceProfile Profile(PuzzleRecord record, int first) =>
        new(Knot(record, first), Knot(record, first + 1), Knot(record, first + 2));

    private static AssistanceKnot Knot(PuzzleRecord record, int index)
    {
        var k = record.Assistance(index)!.Value;
        return new(k.Precision, new(k.PositionWindow), k.RotationWindowDegrees, new(k.MaximumPositionCorrection),
            k.MaximumRotationCorrectionDegrees, new(k.Blend), new(k.CaptureMargin), new(k.CaptureSpeed), new(k.CaptureDwell),
            new(k.GuideAcceleration), k.TriggerThreshold);
    }

    /// <summary>The record's values as a construction, without validating them (a save's pre-validation has already admitted them;
    /// a command's are validated by <see cref="DecodeConstructionRecord"/>). An absent required record or field is a
    /// <see cref="WireFaultException"/>; nothing is defaulted.</summary>
    internal static WorkshopConstruction MapConstructionRecord(ConstructionRecord cr)
    {
        var s = cr.Settings ?? throw new WireFaultException(WireFaultReason.MissingField, "A construction record requires its cadence settings.");
        var settings = new WorkshopCadenceSettings((SimulationCadence)s.Simulation, (PhysicalStepProfile)s.Physical,
            (AnimationCadence)s.Animation, (PresentationCadence)s.Presentation,
            new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));

        WorkshopPuzzle puzzle = default;
        if (cr.Puzzle is { } p)
        {
            var kind = (WorkshopGoalKind)p.GoalKind;
            var goal = kind == WorkshopGoalKind.ActivatedAfter
                ? new WorkshopGoal(kind, default, default, new(p.GoalBody), new(p.GoalTarget), new(p.GoalDelay))
                : new WorkshopGoal(kind, new(p.GoalBody), new(p.GoalTarget), MinimumDuration: new(p.GoalDelay));
            if (p.AssistanceLength != AssistanceKnotCount)
                throw new WireFaultException(WireFaultReason.MissingField, "An authored puzzle requires its nine assistance knots.");
            AssistanceProfile[] profiles = [Profile(p, 0), Profile(p, KnotsPerProfile), Profile(p, 2 * KnotsPerProfile)];
            // The authored knot rule (finite, non-negative, knots at 0, 0.45 and 1): a saved record is pre-validated against it.
            if (profiles.Any(profile => !profile.HasAuthoredShape()))
                throw new WireFaultException(WireFaultReason.OutOfRange,
                    "An authored puzzle's assistance knots must be finite, non-negative and at precisions 0, 0.45 and 1.");
            puzzle = new WorkshopPuzzle(new PuzzleId(p.Id ?? throw new WireFaultException(WireFaultReason.MissingField, "Missing puzzle identity.")), (WorkshopPlacementMode)p.Placement, new(p.Precision),
                p.InventoryPart != null ? new PartId(p.InventoryPart) : default, p.InventoryCount, goal,
                profiles[0], profiles[1], profiles[2]);
        }

        // No instance or connection limit (Story 9R.5b): the record's own vectors are the population.
        var instances = new IWorkshopInstance[cr.InstancesLength];
        for (var i = 0; i < instances.Length; i++) instances[i] = MapInstance(cr.Instances(i)!.Value);

        var links = new WorkshopConnection[cr.ConnectionsLength];
        for (var i = 0; i < links.Length; i++)
        {
            var conn = cr.Connections(i)!.Value;
            links[i] = new(new(conn.Source), (WorkshopSocket)conn.FromSocket, new(conn.Target), (WorkshopSocket)conn.ToSocket, (WorkshopConnectionDomain)conn.Domain);
        }

        return new WorkshopConstruction(new(cr.Revision), settings, new(instances), puzzle, new(links));
    }

    internal static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new WireFaultException(WireFaultReason.MissingField, $"An instance record requires its {field}.");

    internal static IWorkshopInstance MapInstance(InstanceRecord inst)
    {
        if (string.IsNullOrEmpty(inst.PartId)) throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its element.");
        var c = inst.Cell ?? throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its cell.");
        var l = Required(inst.Local, "local position");
        var r = inst.Rotation ?? throw new WireFaultException(WireFaultReason.MissingField, "An instance record requires its rotation.");
        var id = new GpuBodyId(inst.Id);
        var partId = new PartId(inst.PartId);
        var locked = inst.Locked;
        var cell = new CellOrigin(c.X, c.Y, c.Z);
        var local = new LocalPosition(l.X, l.Y, l.Z);
        var rotation = new CanonicalRotation(r.X, r.Y, r.Z, r.W);

        if (!DeclaredPhysics.Contains(partId) || DeclaredPhysics.Of(partId).Colliders.Count == 0)
            throw new WireFaultException(WireFaultReason.UnknownElement, $"Instance {inst.Id} names unknown physical element '{partId}'.");
        return new WorkshopGenericInstance(partId, id, cell, local, rotation, locked)
            { Parameters = DecodeParameters(inst, partId) };
    }

    internal static InstanceParameterCollection DecodeParameters(InstanceRecord record, PartId part)
    {
        try
        {
            var values = new List<KeyValuePair<ParameterId, float>>(4);
            for (var i = 0; i < record.ParametersLength; i++)
            {
                var parameter = record.Parameters(i) ?? throw new WireFaultException(WireFaultReason.MissingField, "Missing parameter.");
                values.Add(new(new(parameter.Key), parameter.Value));
            }
            var result = new InstanceParameterCollection(values);
            result.Validate(part);
            return result;
        }
        catch (ArgumentException error)
        { throw new WireFaultException(WireFaultReason.OutOfRange, "Invalid declared parameters.", error); }
    }

}
