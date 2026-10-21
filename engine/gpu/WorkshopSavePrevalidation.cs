using System;
using System.Collections.Generic;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>The structural pre-validation of a stored construction save (Story 9A.2), run on its verified record before any
/// domain construction: every enum is defined, every number is finite and inside the range its part declares (after the
/// binary16 narrowing the decoder applies, so a value the domain would round into range is admitted and one it would round out
/// of range is not), identities are nonzero and unique, every reference resolves and every connection joins matching declared
/// ports. A violation is the player's typed refusal; it admits exactly what domain construction admits
/// (WorkshopSaveRejectionTests pins the parity lane by lane), so a domain guard that still fires afterwards is a defect.</summary>
internal static class WorkshopSavePrevalidation
{
    public static void Check(SavedConstruction saved)
    {
        var record = saved.Construction ?? throw Refuse(WorkshopSaveRejection.MissingField, "The save holds no construction.");
        if (record.Revision == 0) throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved construction revision is zero.");
        CheckSettings(record);
        var parts = new Dictionary<GpuBodyId, PartId>();
        for (var i = 0; i < record.InstancesLength; i++)
        {
            var instance = record.Instances(i)!.Value;
            var part = CheckInstance(instance);
            if (!parts.TryAdd(new(instance.Id), part))
                throw Refuse(WorkshopSaveRejection.DuplicateInstance, $"Two saved parts share identity {instance.Id}.");
        }
        CheckConnections(record, parts);
        CheckPuzzle(record);
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

    private static PartId CheckInstance(InstanceRecord instance)
    {
        if (string.IsNullOrEmpty(instance.PartId)) throw Refuse(WorkshopSaveRejection.MissingField, "A saved part names no element.");
        if (!Decodable.Contains(instance.PartId))
            throw Refuse(WorkshopSaveRejection.UnknownElement, $"No delivered element is called '{instance.PartId}'.");
        var part = new PartId(instance.PartId);
        if (instance.Id == 0) throw Refuse(WorkshopSaveRejection.OutOfRange, "A saved part has identity zero.");
        var cell = instance.Cell ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no cell.");
        var local = instance.Local ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no local position.");
        var rotation = instance.Rotation ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {instance.Id} has no rotation.");
        Axis(instance.Id, cell.X, local.X); Axis(instance.Id, cell.Y, local.Y); Axis(instance.Id, cell.Z, local.Z);
        Rotation(instance.Id, rotation);
        CheckParameters(part, instance);
        return part;
    }

    /// <summary>Every element a save record decodes to (the construction record's decode arms).</summary>
    private static readonly HashSet<string> Decodable = new(StringComparer.Ordinal)
    {
        "ball", "bowling", "basket", "ramp", "wall", "switch", "delay", "spring", "battery", "bumper", "domino", "lamp", "latch", "pipe",
        "pipe_bend_45", "pipe_bend_90", "funnel", "powered_gate", "ball_detector", "pressure_plate", "counter", "clock",
    };

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

    private static void Within(ulong id, string field, float value, float minimum, float maximum)
    {
        Finite(id, field, value);
        if (value < minimum || value > maximum)
            throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} {field} {value:R} is outside [{minimum:R}, {maximum:R}].");
    }

    /// <summary>A binary16 lane: the f32 the record holds, narrowed as the decoder narrows it, inside the declared Half range.</summary>
    private static Half Narrow(ulong id, string field, float value)
    {
        Finite(id, field, value);
        var narrowed = (Half)value;
        if (!Half.IsFinite(narrowed)) throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} {field} {value:R} exceeds binary16.");
        return narrowed;
    }

    private static void Within(ulong id, string field, float value, Half minimum, Half maximum)
    {
        var narrowed = Narrow(id, field, value);
        if (narrowed < minimum || narrowed > maximum)
            throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} {field} {value:R} is outside [{minimum}, {maximum}].");
    }

    private static void Declared(ulong id, string field, float value, Half declared)
    {
        if (!HalfBits.Equal(Narrow(id, field, value), declared))
            throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} {field} differs from its element's declared material.");
    }

    private static Vec3f Required(ulong id, Vec3f? value, string field) =>
        value ?? throw Refuse(WorkshopSaveRejection.MissingField, $"Part {id} has no {field}.");

    /// <summary>Each kind's declared parameters: the ranges and declared materials of its domain record.</summary>
    private static void CheckParameters(PartId part, InstanceRecord r)
    {
        var id = r.Id;
        switch (part.Value)
        {
            case "ball" or "bowling":
                var material = BallMaterial.For(part);
                Declared(id, "radius", r.Radius, material.Radius.Value); Declared(id, "mass", r.Mass, material.Mass.Value);
                Declared(id, "bounce", r.Bounce, material.Bounce.Value); Declared(id, "drag", r.Drag, material.Drag.Value);
                Declared(id, "buoyancy", r.Buoyancy, material.Buoyancy.Value);
                break;
            case "basket":
                Within(id, "capture margin", r.Margin, (Half)0, (Half)1);
                Within(id, "capture speed limit", r.SpeedLimit, (Half)(1.0 / 1024), (Half)64);
                Within(id, "capture dwell", r.Dwell, (Half)(1.0 / 1024), (Half)30);
                if (!Enum.IsDefined((SensorParticipation)r.Participation))
                    throw Refuse(WorkshopSaveRejection.UndefinedEnum, $"Part {id} has an undefined sensor participation.");
                CheckForceRegion(id, Required(id, r.ForceMin, "force region minimum"), Required(id, r.ForceMax, "force region maximum"),
                    r.SupportHeight, r.SupportMargin, r.MaxAcceleration);
                break;
            case "ramp":
                Within(id, "length", r.Length, (Half).125, (Half)4); Within(id, "width", r.Width, (Half).125, (Half)4);
                break;
            case "wall":
                Within(id, "width", r.Width, WallDimensions.Minimum.Width.Value, WallDimensions.Maximum.Width.Value);
                Within(id, "height", r.Height, WallDimensions.Minimum.Height.Value, WallDimensions.Maximum.Height.Value);
                Within(id, "thickness", r.Thickness, WallDimensions.Minimum.Thickness.Value, WallDimensions.Maximum.Thickness.Value);
                break;
            case "switch":
                Within(id, "trigger threshold", r.Threshold, (Half)0, (Half)64);
                break;
            case "delay":
                Within(id, "duration", r.Duration, (Half).1, (Half)12);
                break;
            case "spring":
                Within(id, "stiffness", r.Stiffness, 120f, 1200f); Within(id, "damping", r.Damping, 0f, 8f);
                Within(id, "initial compression", r.InitialCompression, 0f, .2f);
                break;
            case "battery":
                Within(id, "capacity", r.Capacity, 60f, ElectricalSupply.MaximumCapacity);
                Within(id, "maximum power", r.MaximumPower, 10f, 480f);
                Within(id, "initial fraction", r.InitialFraction, 0f, 1f);
                if (!Enum.IsDefined((ElectricalEnable)r.Enabled))
                    throw Refuse(WorkshopSaveRejection.UndefinedEnum, $"Part {id} has an undefined enable state.");
                break;
            case "bumper":
                Within(id, "strength", r.Strength, 0f, 20f);
                Finite(id, "reference mass", r.ReferenceMass); Finite(id, "preload", r.Preload);
                if (r.ReferenceMass != 1f || r.Preload != BumperWork.FromCanonicalStrength(r.Strength).Preload.Value)
                    throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} preload differs from its declared calibration.");
                break;
            case "domino":
                var extents = Required(id, r.HalfExtents, "half extents");
                var domino = DominoMaterial.Default;
                Declared(id, "half extents", extents.X, domino.HalfExtents.X); Declared(id, "half extents", extents.Y, domino.HalfExtents.Y);
                Declared(id, "half extents", extents.Z, domino.HalfExtents.Z); Declared(id, "mass", r.Mass, domino.Mass.Value);
                Declared(id, "bounce", r.Bounce, domino.Bounce.Value); Declared(id, "friction", r.Friction, domino.Friction.Value);
                break;
            case "pipe":
                Within(id, "length", r.Length, PipeLength.Minimum, PipeLength.Maximum);
                break;
            case "pressure_plate":
                Within(id, "minimum mass", r.Threshold, .1f, 16f);
                break;
            case "counter":
                if (r.TargetCount == 0) throw Refuse(WorkshopSaveRejection.MissingField, $"Counter {id} has no target count.");
                if (r.TargetCount > WorkshopCounter.MaximumTargetCount)
                    throw Refuse(WorkshopSaveRejection.OutOfRange, $"Counter {id} target count {r.TargetCount} is outside [{WorkshopCounter.MinimumTargetCount}, {WorkshopCounter.MaximumTargetCount}].");
                break;
            case "clock":
                Within(id, "interval", r.Threshold, WorkshopClock.MinIntervalSeconds, WorkshopClock.MaxIntervalSeconds);
                break;
        }
    }

    /// <summary>The receiver's planar guide (PlanarGuideDeclaration): bounds within 16 m that enclose a volume, a support height
    /// within 16 m, a support margin in [0, 1] and an acceleration in [0, 12].</summary>
    private static void CheckForceRegion(ulong id, Vec3f minimum, Vec3f maximum, float height, float margin, float acceleration)
    {
        var low = new[] { minimum.X, minimum.Y, minimum.Z };
        var high = new[] { maximum.X, maximum.Y, maximum.Z };
        for (var axis = 0; axis < 3; axis++)
        {
            Within(id, "force region minimum", low[axis], (Half)(-16), (Half)16);
            Within(id, "force region maximum", high[axis], (Half)(-16), (Half)16);
        }
        for (var axis = 0; axis < 3; axis++)
            if ((Half)low[axis] >= (Half)high[axis])
                throw Refuse(WorkshopSaveRejection.OutOfRange, $"Part {id} force region encloses no volume.");
        Within(id, "support height", height, (Half)(-16), (Half)16);
        Within(id, "support margin", margin, (Half)0, (Half)1);
        Within(id, "guide acceleration", acceleration, (Half)0, (Half)12);
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
    private static void CheckPuzzle(ConstructionRecord record)
    {
        if (record.Puzzle is not { } p) return;
        var puzzle = (WorkshopPuzzleId)p.Id;
        if (!Enum.IsDefined(puzzle) || !Enum.IsDefined((WorkshopPlacementMode)p.Placement) || !Enum.IsDefined((WorkshopGoalKind)p.GoalKind))
            throw Refuse(WorkshopSaveRejection.UndefinedEnum, "The saved puzzle names an undefined puzzle, placement or goal.");
        if (puzzle == WorkshopPuzzleId.Free) throw Refuse(WorkshopSaveRejection.OutOfRange, "A Free Workshop save carries puzzle settings.");
        if (!float.IsFinite(p.Precision) || !float.IsFinite(p.GoalDelay))
            throw Refuse(WorkshopSaveRejection.NonFinite, "The saved puzzle has a non-finite precision or goal delay.");
        if (p.RawAssistanceLength != WorkshopPuzzleWire.AssistanceBytes)
            throw Refuse(WorkshopSaveRejection.MissingField, "The saved puzzle lacks its three assistance profiles.");
        var precision = (Half)p.Precision;
        var delay = (Half)p.GoalDelay;
        if (!Half.IsFinite(precision) || precision < (Half)0 || precision > (Half)1 || !Half.IsFinite(delay) || delay < (Half)0 || delay > (Half)120)
            throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle precision or goal delay is outside its range.");
        if (p.InventoryPart is { } inventory && !Decodable.Contains(inventory))
            throw Refuse(WorkshopSaveRejection.UnknownElement, $"The saved puzzle's inventory names no delivered element '{inventory}'.");
        // The named-goal rule (WorkshopGoal), over the fields the decoder maps into each goal kind.
        var admitted = (WorkshopGoalKind)p.GoalKind switch
        {
            WorkshopGoalKind.None => p.GoalBody == 0 && p.GoalTarget == 0 && p.GoalEventSource == 0 && delay == (Half)0,
            WorkshopGoalKind.Captured => p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget && p.GoalEventSource != 0 && delay == (Half)0,
            _ => p.GoalEventSource == 0 && p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget,
        };
        // A Captured goal may name a part the bench no longer holds: a lesson saved after its receiver was deleted keeps its goal
        // (CAT-062b, MissingReceiver), so the goal's identities are not saved-part references.
        if (!admitted) throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle goal is not a supported named goal.");
    }
}
