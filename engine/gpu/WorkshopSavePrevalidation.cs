using System;
using System.Collections.Generic;
using CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

/// <summary>The structural pre-validation of a stored construction save (Story 9A.2), run on its verified record before any
/// domain construction: every enum is defined, every number is finite and inside the range its part declares (after the
/// binary16 narrowing the decoder applies, so a value the domain would round into range is admitted and one it would round out
/// of range is not), every element id is one of the loaded bundle's elements (Story 9A.15), identities are nonzero and unique,
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
        var parts = new Dictionary<GpuBodyId, PartId>();
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
        var genericParameters = false;
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
            case "switch":
                Within(id, "trigger threshold", r.Threshold, (Half)0, (Half)64);
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
            default:
                genericParameters = true;
                for (var i = 0; i < r.ParametersLength; i++)
                    if (r.Parameters(i) is { } parameter) Finite(id, parameter.Key, parameter.Value);
                try { _ = WorkshopWireFlatBuffers.DecodeParameters(r, part); }
                catch (WireFaultException error) { throw Refuse(WorkshopSaveRejection.OutOfRange, error.Message); }
                break;
        }
        if (!genericParameters && r.ParametersLength != 0)
            throw Refuse(WorkshopSaveRejection.OutOfRange, "Typed instance cannot carry generic parameters.");
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
    private static void CheckPuzzle(ConstructionRecord record, IReadOnlySet<PartId> elements)
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
        var minimumGoalSeconds = (Half)p.GoalDelay;
        if (!Half.IsFinite(precision) || precision < (Half)0 || precision > (Half)1 || !Half.IsFinite(minimumGoalSeconds) || minimumGoalSeconds < (Half)0 || minimumGoalSeconds > (Half)120)
            throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle precision or goal delay is outside its range.");
        if (p.InventoryPart is { } inventory) Element(inventory, elements, "The saved puzzle's inventory");
        // The named-goal rule (WorkshopGoal), over the fields the decoder maps into each goal kind.
        var admitted = (WorkshopGoalKind)p.GoalKind switch
        {
            WorkshopGoalKind.None => p.GoalBody == 0 && p.GoalTarget == 0 && p.GoalEventSource == 0 && minimumGoalSeconds == (Half)0,
            WorkshopGoalKind.Captured => p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget && p.GoalEventSource != 0 && minimumGoalSeconds == (Half)0,
            _ => p.GoalEventSource == 0 && p.GoalBody != 0 && p.GoalTarget != 0 && p.GoalBody != p.GoalTarget,
        };
        // A Captured goal may name a part the bench no longer holds: a lesson saved after its receiver was deleted keeps its goal
        // (CAT-062b, MissingReceiver), so the goal's identities are not saved-part references.
        if (!admitted) throw Refuse(WorkshopSaveRejection.OutOfRange, "The saved puzzle goal is not a supported named goal.");
    }
}
