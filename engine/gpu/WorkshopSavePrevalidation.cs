using System;
using System.Collections.Generic;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>The structural pre-validation of a stored construction save (Story 9A.2), run on its verified record before any
/// domain construction: every enum is defined, every number is finite and inside the range its part declares, compared as the exact
/// f32 the decoder passes on unchanged (Story 9A.44: no value is narrowed, so the first f32 above a declared maximum is refused),
/// every element id is one of the loaded bundle's elements (Story 9A.15), identities are nonzero and unique,
/// every reference resolves and every connection joins matching declared
/// ports. A violation is the player's typed refusal; it admits exactly what domain construction admits
/// (WorkshopSaveRejectionTests pins the parity lane by lane), so a domain guard that still fires afterwards is a defect.</summary>
internal static class WorkshopSavePrevalidation
{
    /// <summary>Checks <paramref name="saved"/> against the loaded bundle's <paramref name="elements"/>: every saved element id must be
    /// one of them (Story 9A.15).</summary>
    public static void Check(SavedConstruction saved, IReadOnlySet<PartId> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var record = saved.Construction ?? throw Refuse(WorkshopSaveRejection.MissingField, "The save holds no construction.");
        if (record.Revision == 0) throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved construction revision is zero.");
        CheckSettings(record);
        var parts = new Dictionary<GpuBodyId, PartId>(16);
        for (var i = 0; i < record.InstancesLength; i++)
        {
            var instance = record.Instances(i)!.Value;
            var part = CheckInstance(instance, elements);
            if (!parts.TryAdd(new(instance.Id), part))
                throw Refuse(WorkshopSaveRejection.DuplicateInstance, $"Two saved parts share identity {instance.Id}.");
        }
        CheckConnections(record, parts);
        CheckPuzzle(record, elements);
        foreach (var id in parts.Keys)
            if (saved.NextBodyId <= id.Value)
                throw Refuse(WorkshopSaveRejection.InvalidAllocator, "The save's next part identity is not above every saved identity.");
        if (saved.NextBodyId == 0) throw Refuse(WorkshopSaveRejection.InvalidAllocator, "The save's next part identity is zero.");
    }

    private static WorkshopSaveRejectedException Refuse(WorkshopSaveRejection reason, string detail) =>
        new(reason, "This save cannot be loaded: " + detail);

    private static void CheckSettings(ConstructionRecord record)
    {
        var s = record.Settings ?? throw Refuse(WorkshopSaveRejection.MissingField, "The save has no simulation settings.");
        var settings = new WorkshopCadenceSettings((SimulationCadence)s.Simulation, (PhysicalStepProfile)s.Physical,
            (AnimationCadence)s.Animation, (PresentationCadence)s.Presentation, new PulseRate(s.PresentationRateNumerator, s.PresentationRateDenominator));
        if (!Enum.IsDefined(settings.Simulation) || !Enum.IsDefined(settings.Physical) || !Enum.IsDefined(settings.Animation) || !Enum.IsDefined(settings.Presentation))
            throw Refuse(WorkshopSaveRejection.UndefinedEnum, "The save names an undefined cadence.");
        if (settings.Rejection() is { } reason) throw Refuse(WorkshopSaveRejection.OutOfRange, reason);
    }

    // ---------- instances ----------

    private static PartId CheckInstance(InstanceRecord instance, IReadOnlySet<PartId> elements)
    {
        var part = Element(instance.PartId, elements, $"Part {instance.Id}");
        if (instance.Id == 0) throw Refuse(WorkshopSaveRejection.OutOfRange, "A saved part has identity zero.");
        var cell = instance.Cell ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no cell.");
        var local = instance.Local ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no local position.");
        var rotation = instance.Rotation ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no rotation.");
        Axis(instance.Id, cell.X, local.X); Axis(instance.Id, cell.Y, local.Y); Axis(instance.Id, cell.Z, local.Z);
        Rotation(instance.Id, rotation);
        CheckParameters(part, instance);
        return part;
    }

    /// <summary>A saved element id: present, and one of the loaded bundle's elements; anything else is refused naming the id.</summary>
    private static PartId Element(string? id, IReadOnlySet<PartId> elements, string owner)
    {
        if (string.IsNullOrWhiteSpace(id)) throw Refuse(WorkshopSaveRejection.MissingField, $"{owner} names no element.");
        var part = new PartId(id);
        if (!elements.Contains(part))
            throw Refuse(WorkshopSaveRejection.UnknownElement, $"{owner} names the element '{id}', which this game does not have.");
        return part;
    }

    /// <summary>The canonical body axis rule (CanonicalBody): a finite local remainder in [-0.5, 0.5) of a cell inside the world.</summary>
    private static void Axis(ulong id, int cell, float local)
    {
        Finite(id, "local position", local);
        if (local < -0.5f || local >= 0.5f || cell < -CanonicalBody.MaximumCell || cell > CanonicalBody.MaximumCell ||
            (cell == CanonicalBody.MaximumCell && local > 0f) || (cell == -CanonicalBody.MaximumCell && local < 0f))
            throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} lies outside the world.");
    }

    /// <summary>The committed rotation rule (CanonicalRotation): finite unit-norm components in [-1, 1], not all zero.</summary>
    private static void Rotation(ulong id, Quat4f q)
    {
        Finite(id, "rotation", q.X); Finite(id, "rotation", q.Y); Finite(id, "rotation", q.Z); Finite(id, "rotation", q.W);
        var norm = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
        if (q.X < -1f || q.X > 1f || q.Y < -1f || q.Y > 1f || q.Z < -1f || q.Z > 1f || q.W < -1f || q.W > 1f ||
            (q.X == 0f && q.Y == 0f && q.Z == 0f && q.W == 0f) || norm < .99999f || norm > 1.00001f)
            throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} has no unit rotation.");
    }

    private static void Finite(ulong id, string field, float value)
    {
        if (!float.IsFinite(value)) throw Refuse(WorkshopSaveRejection.NonFinite, $"Part {id} has a non-finite {field}.");
    }

    /// <summary>Every instance carries its complete declared parameter set.</summary>
    private static void CheckParameters(PartId part, InstanceRecord r)
    {
        for (var i = 0; i < r.ParametersLength; i++)
            if (r.Parameters(i) is { } parameter) Finite(r.Id, parameter.Key, parameter.Value);
        try { _ = WorkshopAuthoringFlatBuffers.DecodeParameters(r, part); }
        catch (WireFaultException error) { throw Refuse(WorkshopSaveRejection.OutOfRange, error.Message); }
    }

    // ---------- connections and the puzzle ----------

    private static void CheckConnections(ConstructionRecord record, Dictionary<GpuBodyId, PartId> parts)
    {
        var links = new WorkshopConnection[record.ConnectionsLength];
        for (var i = 0; i < links.Length; i++)
        {
            var link = record.Connections(i)!.Value;
            var connection = new WorkshopConnection(new(link.Source), (WorkshopSocket)link.FromSocket, new(link.Target),
                (WorkshopSocket)link.ToSocket, (WorkshopConnectionDomain)link.Domain);
            if (!Enum.IsDefined(connection.Domain) || !Enum.IsDefined(connection.Output) || !Enum.IsDefined(connection.Input))
                throw Refuse(WorkshopSaveRejection.UndefinedEnum, "A saved connection names an undefined domain or socket.");
            if (!parts.ContainsKey(connection.Source) || !parts.ContainsKey(connection.Target))
                throw Refuse(WorkshopSaveRejection.UnresolvedReference, "A saved connection names a part the save does not hold.");
            links[i] = connection;
        }
        // The connection rules themselves (WorkshopConnections), over the saved part identities: their typed refusal is this one.
        try { new WorkshopConnections(links).Validate(id => parts.TryGetValue(id, out var part) ? part : null); }
        catch (WorkshopEditRejectedException refusal)
        {
            throw new WorkshopSaveRejectedException(WorkshopSaveRejection.InvalidConnection, "This save cannot be loaded: " + refusal.Message, refusal);
        }
    }

    /// <summary>An authored puzzle's record: defined enums, finite values, all three assistance profiles and a supported
    /// named goal. A Free construction carries no puzzle record. The lesson's own authored-settings rules stay its typed refusals.</summary>
    private static void CheckPuzzle(ConstructionRecord record, IReadOnlySet<PartId> elements)
    {
        if (record.Puzzle is not { } p) return;
        if (string.IsNullOrWhiteSpace(p.Id))
            throw Refuse(WorkshopSaveRejection.MissingField, "The saved puzzle lacks an authored identity.");
        if (!Enum.IsDefined((WorkshopPlacementMode)p.Placement) || !Enum.IsDefined((WorkshopGoalKind)p.GoalKind))
            throw Refuse(WorkshopSaveRejection.UndefinedEnum, "The saved puzzle names an undefined puzzle, placement or goal.");
        if (!float.IsFinite(p.Precision) || !float.IsFinite(p.GoalDelay))
            throw Refuse(WorkshopSaveRejection.NonFinite, "The saved puzzle has a non-finite precision or goal delay.");
        if (p.AssistanceLength != WorkshopAuthoringFlatBuffers.AssistanceKnotCount)
            throw Refuse(WorkshopSaveRejection.MissingField,
                $"The saved puzzle holds {p.AssistanceLength} assistance knots; a puzzle holds exactly nine.");
        var knots = new AssistanceKnot[WorkshopAuthoringFlatBuffers.AssistanceKnotCount];
        for (var i = 0; i < knots.Length; i++)
        {
            var k = p.Assistance(i)!.Value;
            if (!float.IsFinite(k.Precision) || !float.IsFinite(k.PositionWindow) || !float.IsFinite(k.RotationWindowDegrees) ||
                !float.IsFinite(k.MaximumPositionCorrection) || !float.IsFinite(k.MaximumRotationCorrectionDegrees) ||
                !float.IsFinite(k.Blend) || !float.IsFinite(k.CaptureMargin) || !float.IsFinite(k.CaptureSpeed) ||
                !float.IsFinite(k.CaptureDwell) || !float.IsFinite(k.GuideAcceleration) || !float.IsFinite(k.TriggerThreshold))
                throw Refuse(WorkshopSaveRejection.NonFinite, "A saved assistance knot has a non-finite value.");
            knots[i] = new(k.Precision, new(k.PositionWindow), k.RotationWindowDegrees, new(k.MaximumPositionCorrection),
                k.MaximumRotationCorrectionDegrees, new(k.Blend), new(k.CaptureMargin), new(k.CaptureSpeed), new(k.CaptureDwell),
                new(k.GuideAcceleration), k.TriggerThreshold);
        }
        // The authored knot rule the content reader applies (AssistanceProfile.HasAuthoredShape), on each of the three profiles.
        for (var first = 0; first < knots.Length; first += WorkshopAuthoringFlatBuffers.KnotsPerProfile)
            if (!new AssistanceProfile(knots[first], knots[first + 1], knots[first + 2]).HasAuthoredShape())
                throw Refuse(WorkshopSaveRejection.OutOfRange,
                    "A saved assistance profile has a negative value or knots not at precisions 0, 0.45 and 1.");
        var precision = p.Precision;
        var minimumGoalSeconds = p.GoalDelay;
        if (precision < 0f || precision > 1f || minimumGoalSeconds < 0f || minimumGoalSeconds > 120f)
            throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle precision or goal delay is outside its range.");
        if (p.InventoryPart is { } inventory) Element(inventory, elements, "The saved puzzle's inventory");
        // The named-goal rule (WorkshopGoal), over the fields the decoder maps into each goal kind.
        var admitted = (WorkshopGoalKind)p.GoalKind switch
        {
            WorkshopGoalKind.None => p.GoalBody == 0 && p.GoalTarget == 0 && minimumGoalSeconds == 0f,
            WorkshopGoalKind.Captured => p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget && minimumGoalSeconds == 0f,
            _ => p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget,
        };
        // A Captured goal may name a part the bench no longer holds: a lesson saved after its receiver was deleted keeps its goal
        // (CAT-062b, MissingReceiver), so the goal's identities are not saved-part references.
        if (!admitted) throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle goal is not a supported named goal.");
    }
}
