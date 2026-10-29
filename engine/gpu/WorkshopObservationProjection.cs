using System;
using System.Globalization;

namespace CuriousContraptions.Gpu;

// The genuine external object boundary; production and native boundary doubles share the projection.
internal interface IWorkshopObservationObject<T> : IDisposable where T : IWorkshopObservationObject<T>
{
    static abstract T Object();
    static abstract T Array();
    void Push(T item);
    void SetProperty(string name, T value);
    void SetProperty(string name, string? value);
    void SetProperty(string name, bool value);
    void SetProperty(string name, int value);
    void SetProperty(string name, uint value);
    void SetProperty(string name, float value);
    void SetMilliseconds(double value);
}

internal static class WorkshopObservationProjection
{
    internal static int EnumValue<T>(T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new ArgumentException("Undefined observation enum.");
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }
    internal static int? OccurrenceKind(ActivationOccurrence value)
    {
        if (value == default) return null;
        value.Validate();
        return EnumValue(value.Kind);
    }
    private static string Exact(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Exact(Int128 value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Exact(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    private static T Cell<T>(CellOrigin cell) where T : IWorkshopObservationObject<T>
    { var value = T.Object(); value.SetProperty("x", cell.X); value.SetProperty("y", cell.Y); value.SetProperty("z", cell.Z); return value; }
    private static T Vector<T>(float x, float y, float z) where T : IWorkshopObservationObject<T>
    { var value = T.Object(); value.SetProperty("x", x); value.SetProperty("y", y); value.SetProperty("z", z); return value; }
    internal static T Construction<T>(WorkshopAuthoredScene authored) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        var command = authored.Command;
        value.SetProperty("sessionLow", Exact(command.Session.Low)); value.SetProperty("sessionHigh", Exact(command.Session.High));
        value.SetProperty("sequence", Exact(command.Sequence.Value)); value.SetProperty("kind", EnumValue(command.Kind));
        value.SetProperty("expectedRevision", EnumValue(command.RevisionKind)); value.SetProperty("authorityRevision", Exact(command.Revision.Value));
        value.SetProperty("epoch", Exact(command.Epoch.Value)); value.SetProperty("projection", Exact(command.Projection.Value));
        value.SetProperty("cadence", Exact(command.Cadence.Value));
        var source = authored.Construction;
        value.SetProperty("revision", Exact(source.Revision.Value));
        using var puzzle = Puzzle<T>(source.Puzzle); value.SetProperty("puzzle", puzzle);
        value.SetProperty("simulation", EnumValue(source.Settings.Simulation)); value.SetProperty("physical", EnumValue(source.Settings.Physical));
        value.SetProperty("animation", EnumValue(source.Settings.Animation)); value.SetProperty("presentation", EnumValue(source.Settings.Presentation));
        value.SetProperty("presentationNumerator", source.Settings.PresentationRate.Numerator);
        value.SetProperty("presentationDenominator", source.Settings.PresentationRate.Denominator);
        using var instances = T.Array();
        foreach (var instance in source.Instances)
        {
            using var item = T.Object(); item.SetProperty("id", Exact(instance.Id.Value));
            item.SetProperty("part", instance.PartId.Value); item.SetProperty("locked", instance.Locked);
            using var cell = Cell<T>(instance.Cell);
            using var local = Vector<T>(instance.Local.X, instance.Local.Y, instance.Local.Z);
            using var rotation = Vector<T>(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z);
            rotation.SetProperty("w", instance.Rotation.W);
            item.SetProperty("cell", cell); item.SetProperty("local", local); item.SetProperty("rotation", rotation);
            using var parameters = T.Array();
            foreach (var parameter in DeclaredPhysics.Parameters(instance))
            { using var entry = T.Object(); entry.SetProperty("id", parameter.Key); entry.SetProperty("value", parameter.Value); parameters.Push(entry); }
            item.SetProperty("parameters", parameters); instances.Push(item);
        }
        value.SetProperty("instances", instances);
        using var connections = T.Array();
        foreach (var connection in source.Connections)
        {
            using var item = T.Object(); item.SetProperty("source", Exact(connection.Source.Value)); item.SetProperty("output", EnumValue(connection.Output));
            item.SetProperty("target", Exact(connection.Target.Value)); item.SetProperty("input", EnumValue(connection.Input)); item.SetProperty("domain", EnumValue(connection.Domain)); connections.Push(item);
        }
        value.SetProperty("connections", connections);
        return value;
    }


    private static T Puzzle<T>(WorkshopPuzzle source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object(); value.SetProperty("id", source.Id.Value);
        value.SetProperty("placement", EnumValue(source.Placement)); value.SetProperty("precision", source.Precision.Value);
        value.SetProperty("inventoryPart", source.InventoryPart == default ? null : source.InventoryPart.Value); value.SetProperty("inventoryCount", source.InventoryCount);
        using var goal = T.Object(); goal.SetProperty("kind", EnumValue(source.Goal.Kind));
        goal.SetProperty("body", Exact(source.Goal.Body.Value)); goal.SetProperty("target", Exact(source.Goal.Target.Value));
        goal.SetProperty("sourceNode", Exact(source.Goal.SourceNode.Value)); goal.SetProperty("targetNode", Exact(source.Goal.TargetNode.Value));
        goal.SetProperty("minimumDuration", source.Goal.MinimumDuration.Value); value.SetProperty("goal", goal);
        using var primary = Assistance<T>(source.PrimaryAssistance); value.SetProperty("primary", primary);
        using var target = Assistance<T>(source.TargetAssistance); value.SetProperty("target", target);
        using var placement = Assistance<T>(source.PlacementAssistance); value.SetProperty("placementAssistance", placement);
        return value;
    }
    private static T Assistance<T>(AssistanceProfile source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        using var forgiving = Knot<T>(source.Forgiving); value.SetProperty("forgiving", forgiving);
        using var balanced = Knot<T>(source.Balanced); value.SetProperty("balanced", balanced);
        using var precise = Knot<T>(source.Precise); value.SetProperty("precise", precise);
        return value;
    }
    private static T Knot<T>(AssistanceKnot source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("precision", source.Precision); value.SetProperty("positionWindow", source.PositionWindow.Value);
        value.SetProperty("rotationWindowDegrees", source.RotationWindowDegrees);
        value.SetProperty("maximumPositionCorrection", source.MaximumPositionCorrection.Value);
        value.SetProperty("maximumRotationCorrectionDegrees", source.MaximumRotationCorrectionDegrees);
        value.SetProperty("blend", source.Blend.Value); value.SetProperty("captureMargin", source.CaptureMargin.Value);
        value.SetProperty("captureSpeed", source.CaptureSpeed.Value); value.SetProperty("captureDwell", source.CaptureDwell.Value);
        value.SetProperty("guideAcceleration", source.GuideAcceleration.Value); value.SetProperty("triggerThreshold", source.TriggerThreshold);
        return value;
    }
    internal static T Response<T>(WorkshopResponse response) where T : IWorkshopObservationObject<T>
    {
        var read = response.Read;
        var value = T.Object();
        value.SetProperty("epoch", Exact(read.Epoch.Value)); value.SetProperty("tick", Exact(read.Tick.Value));
        value.SetProperty("revision", Exact(read.Revision.Value)); value.SetProperty("phase", WorkshopObservationProjection.EnumValue(response.Phase));
        value.SetProperty("cadence", Exact(response.Cadence.Value)); value.SetProperty("projection", Exact(response.Projection.Value));
        SampleFields(value, new(read.Epoch, read.Tick, read.Bodies, read.Captures));
        using var electrical = T.Array();
        for (var i = 0; i < read.Electrical.Count; i++)
        {
            var source = read.Electrical[i]; using var row = T.Object();
            row.SetProperty("id", Exact(source.Id.Value)); row.SetProperty("owner", Exact(source.Owner.Value));
            row.SetProperty("remaining", source.Remaining.Value); row.SetProperty("debit", source.Debit.Value);
            row.SetProperty("enabled", WorkshopObservationProjection.EnumValue(source.Enabled)); electrical.Push(row);
        }
        value.SetProperty("electrical", electrical);
        value.SetProperty("sequence", Exact(response.Sequence.Value)); value.SetProperty("publication", Exact(response.Publication.Value));
        value.SetProperty("masterGeneration", Exact(response.MasterGeneration.Value)); value.SetProperty("sourcePulse", Exact(response.SourcePulse.Value));
        value.SetProperty("outcome", WorkshopObservationProjection.EnumValue(response.Result.Outcome)); value.SetProperty("reason", WorkshopObservationProjection.EnumValue(response.Result.Reason));
        if (read.Capture is { } stamp) { using var capture = Stamp<T>(stamp); value.SetProperty("capture", capture); }
        using var activations = T.Array();
        for (var i = 0; i < read.Activations.Count; i++)
        {
            var source = read.Activations[i]; using var row = T.Object();
            row.SetProperty("node", Exact(source.Node.Value));
            row.SetProperty("owner", Exact(source.Owner.Value));
            row.SetProperty("phase", WorkshopObservationProjection.EnumValue(source.Phase));
            row.SetProperty("source", Exact(source.Source.Value));
            row.SetProperty("body", Exact(source.Body.Value));
            row.SetProperty("collider", Exact(source.Collider.Value));
            row.SetProperty("ordinal", source.EventOrdinal);
            row.SetProperty("eventPhase", source.EventPhase);
            row.SetProperty("approachSpeed", source.ApproachSpeed.Value);
            if (WorkshopObservationProjection.OccurrenceKind(source.Occurrence) is { } occurrenceKind) row.SetProperty("kind", occurrenceKind);
            row.SetProperty("emitter", Exact(source.Emitter.Value));
            row.SetProperty("causeOrdinal", source.CauseOrdinal);
            row.SetProperty("causePhase", source.CausePhase);
            activations.Push(row);
        }
        value.SetProperty("activations", activations);
        using var timers = T.Array();
        for (var i = 0; i < read.Timers.Count; i++)
        {
            var source = read.Timers[i]; using var row = T.Object();
            row.SetProperty("node", Exact(source.Node.Value));
            row.SetProperty("phase", WorkshopObservationProjection.EnumValue(source.Phase));
            row.SetProperty("startedTick", Exact(source.StartedTick));
            row.SetProperty("dueTick", Exact(source.DueTick));
            using var input = Occurrence<T>(source.Input); row.SetProperty("input", input);
            timers.Push(row);
        }
        value.SetProperty("timers", timers);
        using var contactWorks = T.Array();
        for (var i = 0; i < read.ContactWorks.Count; i++)
        {
            var source = read.ContactWorks[i]; using var row = T.Object();
            row.SetProperty("id", Exact(source.Id.Value));
            row.SetProperty("owner", Exact(source.Owner.Value));
            row.SetProperty("count", source.OccurrenceCount);
            row.SetProperty("remaining", source.RemainingEnergy.Value);
            row.SetProperty("supplied", source.SuppliedEnergy.Value);
            contactWorks.Push(row);
        }
        value.SetProperty("contactWorks", contactWorks);
        using var memory = T.Array();
        for (var i = 0; i < read.Memory.Count; i++)
        {
            var source = read.Memory[i]; using var row = T.Object();
            row.SetProperty("node", Exact(source.Node.Value));
            row.SetProperty("kind", WorkshopObservationProjection.EnumValue(source.Kind));
            row.SetProperty("count", source.Count);
            row.SetProperty("ordinal", source.Last.Ordinal);
            row.SetProperty("eventPhase", source.Last.Phase);
            row.SetProperty("phase", WorkshopObservationProjection.EnumValue(source.Phase));
            row.SetProperty("dueTick", Exact(source.DueTick));
            memory.Push(row);
        }
        value.SetProperty("memory", memory);
        using var actuators = T.Array();
        foreach (var source in read.Presentation.Actuators)
        {
            using var row = T.Object();
            row.SetProperty("joint", Exact(source.Joint.Value));
            row.SetProperty("owner", Exact(source.Owner.Value));
            row.SetProperty("supplyClosed", source.SupplyClosed);
            row.SetProperty("movingContact", source.MovingContact);
            row.SetProperty("coordinate", source.Coordinate);
            row.SetProperty("speed", source.Speed);
            row.SetProperty("lower", source.Lower);
            row.SetProperty("upper", source.Upper);
            row.SetProperty("blocked", source.Blocked);
            actuators.Push(row);
        }
        value.SetProperty("actuators", actuators);
        using var loads = T.Array();
        foreach (var source in read.Presentation.Loads)
        {
            using var row = T.Object();
            row.SetProperty("sensor", Exact(source.Sensor.Value));
            row.SetProperty("frame", Exact(source.Frame.Value));
            row.SetProperty("region", Exact(source.Region.Value));
            row.SetProperty("mass", source.MeasuredMass);
            row.SetProperty("phase", WorkshopObservationProjection.EnumValue(source.Phase));
            loads.Push(row);
        }
        value.SetProperty("loads", loads);
        using var logic = T.Array();
        foreach (var source in read.Presentation.Logic)
        {
            using var row = T.Object();
            row.SetProperty("owner", Exact(source.Owner.Value));
            row.SetProperty("inputs", WorkshopObservationProjection.EnumValue(source.Inputs));
            row.SetProperty("supplied", source.Supplied);
            row.SetProperty("active", source.Active);
            logic.Push(row);
        }
        value.SetProperty("logic", logic);
        return value;
    }

    private static T Stamp<T>(WorkshopClockStamp source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("domain", WorkshopObservationProjection.EnumValue(source.Domain));
        value.SetProperty("generation", Exact(source.Generation.Value));
        value.SetProperty("time", Exact(source.Time.Value));
        value.SetProperty("uncertainty", Exact(source.Uncertainty.Value));
        return value;
    }
    private static T Occurrence<T>(ActivationOccurrence source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        if (WorkshopObservationProjection.OccurrenceKind(source) is { } kind) value.SetProperty("kind", kind);
        value.SetProperty("emitter", Exact(source.Emitter.Value));
        value.SetProperty("ordinal", source.Time.Ordinal);
        value.SetProperty("eventPhase", source.Time.Phase);
        value.SetProperty("source", Exact(source.Cause.Source.Value));
        value.SetProperty("body", Exact(source.Cause.Body.Value));
        value.SetProperty("collider", Exact(source.Cause.Collider.Value));
        value.SetProperty("causeOrdinal", source.Cause.Time.Ordinal);
        value.SetProperty("causePhase", source.Cause.Time.Phase);
        value.SetProperty("approachSpeed", source.Cause.ApproachSpeed.Value);
        return value;
    }

    private static T Interval<T>(ClockInterval source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("lower", Exact(source.Lower));
        value.SetProperty("upper", Exact(source.Upper));
        return value;
    }
    private static T Probe<T>(ClockProbe source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("sessionLow", Exact(source.Session.Low));
        value.SetProperty("sessionHigh", Exact(source.Session.High));
        value.SetProperty("generation", Exact(source.Generation.Value));
        value.SetProperty("requesterGeneration", Exact(source.RequesterGeneration.Value));
        value.SetProperty("role", WorkshopObservationProjection.EnumValue(source.RequesterRole));
        value.SetProperty("sequence", Exact(source.Sequence.Value));
        value.SetProperty("sent", Exact(source.RequesterSent.Value));
        return value;
    }
    private static T Reply<T>(ClockReply source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("masterReceived", Exact(source.MasterReceived.Value));
        value.SetProperty("masterSent", Exact(source.MasterSent.Value));
        value.SetProperty("received", Exact(source.RequesterReceived.Value));
        value.SetProperty("derivation", source.Derivation.Value);
        using var probe = Probe<T>(source.Probe); value.SetProperty("probe", probe);
        return value;
    }
    private static T Peer<T>(WorkshopClockPeer source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("sessionLow", Exact(source.Session.Low));
        value.SetProperty("sessionHigh", Exact(source.Session.High));
        value.SetProperty("generation", Exact(source.Generation.Value));
        value.SetProperty("requesterGeneration", Exact(source.RequesterGeneration.Value));
        value.SetProperty("role", WorkshopObservationProjection.EnumValue(source.RequesterRole));
        value.SetProperty("origin", Exact(source.MasterOrigin.Value));
        value.SetProperty("uncertainty", Exact(source.Uncertainty.Value));
        return value;
    }
    private static T Durations<T>(System.Collections.Immutable.ImmutableArray<double> source) where T : IWorkshopObservationObject<T>
    {
        var values = T.Array();
        foreach (var duration in source) { using var item = T.Object(); item.SetMilliseconds(duration); values.Push(item); }
        return values;
    }
    private static T Endpoint<T>(PresentationEndpoint source) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object(); value.SetProperty("tick", Exact(source.Tick.Value));
        using var capture = Stamp<T>(source.Capture); value.SetProperty("capture", capture);
        if (source.Mapped is { } mapped)
        {
            using var interval = Interval<T>(mapped.BrowserInterval); value.SetProperty("interval", interval);
            value.SetProperty("receipt", Exact(mapped.Receipt.Value)); value.SetProperty("probe", Exact(mapped.Probe.Value));
        }
        return value;
    }
    internal static T Diagnostic<T>(WorkshopDiagnostic source, WorkshopAuthoredScene? authored = null) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object(); value.SetProperty("category", WorkshopObservationProjection.EnumValue(source.Category));
        switch (source)
        {
            case WorkshopFaultDiagnostic record:
            {
                value.SetProperty("kind", WorkshopObservationProjection.EnumValue(record.Kind));
                value.SetProperty("site", WorkshopObservationProjection.EnumValue(record.Site));
                value.SetProperty("exceptionType", record.ExceptionType);
                value.SetProperty("detail", record.Detail);
                break;
            }
            case ClockRecalibrationDiagnostic record:
            {
                value.SetProperty("cause", WorkshopObservationProjection.EnumValue(record.Cause));
                value.SetProperty("count", Exact(record.Count));
                value.SetProperty("stateBefore", WorkshopObservationProjection.EnumValue(record.StateBefore));
                value.SetProperty("displayEpoch", Exact(record.DisplayEpoch));
                value.SetProperty("at", Exact(record.At.Value));
                value.SetProperty("recoveryStarted", Exact(record.RecoveryStarted.Value));
                value.SetProperty("recoverySends", record.RecoverySends);
                using var peer = Peer<T>(record.Peer); value.SetProperty("peer", peer);
                if (record.Derivation is { } derivation) value.SetProperty("derivation", derivation.Value);

                if (record.Pending is { } pending) { using var item = Probe<T>(pending); value.SetProperty("pending", item); }
                if (record.Reply is { } reply) { using var item = Reply<T>(reply); value.SetProperty("reply", item); }
                using var slots = T.Array();
                foreach (var slot in record.Slots) { using var item = Interval<T>(slot.Interval);
                    item.SetProperty("receipt", Exact(slot.Receipt.Value)); item.SetProperty("sequence", Exact(slot.Sequence.Value)); slots.Push(item); }
                value.SetProperty("slots", slots);
                break;
            }
            case ClockReplyRejectedDiagnostic record:
            {
                value.SetProperty("role", WorkshopObservationProjection.EnumValue(record.Role));
                value.SetProperty("count", Exact(record.Count));
                value.SetProperty("length", record.Length);
                value.SetProperty("received", Exact(record.Received.Value));
                value.SetProperty("detail", record.Detail);
                break;
            }
            case ClockLateRenewalDiagnostic record:
            {
                value.SetProperty("role", WorkshopObservationProjection.EnumValue(record.Role));
                value.SetProperty("count", Exact(record.Count));
                value.SetProperty("lapseAge", Exact(record.LapseAge));
                value.SetProperty("exchanges", record.Exchanges);
                value.SetProperty("renewedAt", Exact(record.RenewedAt.Value));
                value.SetProperty("displayEpoch", Exact(record.DisplayEpoch));
                using var retained = Interval<T>(record.Retained); value.SetProperty("retained", retained);
                using var fresh = Interval<T>(record.Fresh); value.SetProperty("fresh", fresh);
                if (record.Derivation is { } derivation) value.SetProperty("derivation", derivation.Value);
                break;
            }
            case ClockWideOffsetDiagnostic record:
            {
                value.SetProperty("offset", Exact(record.Offset));
                value.SetProperty("admittedLower", Exact(record.AdmittedLower));
                value.SetProperty("admittedUpper", Exact(record.AdmittedUpper));
                value.SetProperty("declaredUncertainty", Exact(record.DeclaredUncertainty));
                value.SetProperty("count", Exact(record.Count));
                break;
            }
            case ClockDerivationDiagnostic record:
            {
                value.SetProperty("report", WorkshopObservationProjection.EnumValue(record.Report));
                value.SetProperty("outcome", WorkshopObservationProjection.EnumValue(record.Outcome));
                value.SetProperty("epoch", record.Epoch.Value);
                value.SetProperty("correction", Exact(record.Correction));
                value.SetProperty("qualified", record.Qualified);
                value.SetProperty("startupAttempts", record.StartupAttempts);
                value.SetProperty("startupOutliers", record.StartupOutliers);
                value.SetProperty("startupInconclusive", record.StartupInconclusive);
                value.SetProperty("startupUnanswered", record.StartupUnanswered);
                value.SetProperty("checks", Exact(record.Checks));
                value.SetProperty("slews", Exact(record.Slews));
                value.SetProperty("rederivations", Exact(record.Rederivations));
                value.SetProperty("inconclusive", Exact(record.Inconclusive));
                value.SetProperty("unanswered", Exact(record.Unanswered));
                value.SetProperty("checkRetries", Exact(record.CheckRetries));
                if (record.PendingCorrection is { } pendingCorrection) value.SetProperty("pendingCorrection", Exact(pendingCorrection));
                if (record.AdmittedLower is { } admittedLower) value.SetProperty("admittedLower", Exact(admittedLower));
                if (record.AdmittedUpper is { } admittedUpper) value.SetProperty("admittedUpper", Exact(admittedUpper));

                if (record.Exchange is { } exchange) { using var item = T.Object(); item.SetProperty("before", Exact(exchange.Before));
                    item.SetProperty("master", Exact(exchange.Master)); item.SetProperty("after", Exact(exchange.After));
                    item.SetProperty("epoch", exchange.Epoch.Value); value.SetProperty("exchange", item); }
                break;
            }
            case AnimationDeferralDiagnostic record:
            {
                value.SetProperty("cause", WorkshopObservationProjection.EnumValue(record.Cause));
                value.SetProperty("count", Exact(record.Count));
                break;
            }
            case WorkshopDebtDiagnostic record:
            {
                value.SetProperty("tick", Exact(record.CommittedTick.Value));
                value.SetProperty("reason", WorkshopObservationProjection.EnumValue(record.Reason));
                value.SetProperty("oldestUnreceipted", Exact(record.OldestUnreceipted.Value));
                value.SetProperty("ageMilliseconds", Exact(record.AgeMilliseconds));
                break;
            }
            case WorkshopTickFailureDiagnostic record:
            {
                value.SetProperty("outcome", WorkshopObservationProjection.EnumValue(record.Result.Outcome));
                value.SetProperty("reason", WorkshopObservationProjection.EnumValue(record.Result.Reason));
                value.SetProperty("fault", WorkshopObservationProjection.EnumValue(record.Fault));
                break;
            }
            case WorkshopTransferDiagnostic record:
            {
                using var response = Response<T>(record.Response); value.SetProperty("response", response);
                break;
            }
            case WorkshopCaptureDiagnostic record:
            {
                value.SetProperty("kind", WorkshopObservationProjection.EnumValue(record.Kind));
                value.SetProperty("stage", WorkshopObservationProjection.EnumValue(record.Stage));
                value.SetProperty("identity", Exact(record.Identity));
                value.SetProperty("epoch", Exact(record.Epoch.Value));
                value.SetProperty("cadence", WorkshopObservationProjection.EnumValue(record.Profile.Cadence));
                value.SetProperty("physical", WorkshopObservationProjection.EnumValue(record.Profile.Physical));
                value.SetProperty("revision", Exact(record.Profile.Revision.Value));
                value.SetProperty("end", WorkshopObservationProjection.EnumValue(record.End));
                value.SetProperty("status", WorkshopObservationProjection.EnumValue(record.Status));
                value.SetProperty("expectedCount", record.ExpectedCount);
                value.SetProperty("count", record.Count);
                value.SetProperty("chunk", record.Chunk);
                value.SetProperty("chunks", record.Chunks);
                value.SetProperty("first", record.First);

                using var samples = T.Array();
                foreach (var sample in record.Samples) { using var item = TraceSample<T>(sample); samples.Push(item); }
                value.SetProperty("samples", samples);
                using var durations = Durations<T>(record.Milliseconds); value.SetProperty("durations", durations);
                if (record.Scene is { } scene)
                {
                    if (authored is not { } original || original.Scene != scene)
                        throw new ArgumentException("Capture has no original authored scene association.");
                    using var item = Construction<T>(original); value.SetProperty("construction", item);
                }
                break;
            }
            case WorkshopClockSampleDiagnostic record:
            {
                using var probe = Probe<T>(record.Probe); value.SetProperty("probe", probe);
                if (record.Reply is { } reply) { using var item = Reply<T>(reply); value.SetProperty("reply", item); }
                using var offset = Interval<T>(record.Offset); value.SetProperty("offset", offset);
                value.SetProperty("outcome", WorkshopObservationProjection.EnumValue(record.Outcome)); value.SetProperty("received", Exact(record.Received.Value));
                break;
            }
            case WorkshopAnimationDiagnostic record:
            {
                var sample = record.Sample;
                value.SetProperty("cadence", Exact(record.Cadence.Value)); value.SetProperty("kind", WorkshopObservationProjection.EnumValue(record.Kind));
                value.SetProperty("target", Exact(sample.Target.Value)); value.SetProperty("world", Exact(sample.World.Value));
                value.SetProperty("generation", Exact(sample.Generation)); value.SetProperty("pulse", Exact(sample.Pulse.Value));
                value.SetProperty("appliedAt", Exact(sample.AppliedAt.Value)); value.SetProperty("value", sample.Value);
                value.SetProperty("ordinal", sample.EventOrdinal); value.SetProperty("eventPhase", sample.EventPhase);
                value.SetProperty("property", WorkshopObservationProjection.EnumValue(sample.Property));
                break;
            }
            case WorkshopPresentationDiagnostic record:
            {
                var sample = record.Sample; var scene = record.Scene; var evidence = sample.Evidence;
                value.SetProperty("cadence", Exact(record.Cadence.Value)); value.SetProperty("pulse", Exact(record.Pulse.Value));
                value.SetProperty("selected", record.Selected); value.SetProperty("appliedAt", Exact(record.AppliedAt.Value));
                value.SetProperty("quality", WorkshopObservationProjection.EnumValue(sample.Quality)); value.SetProperty("frame", Exact(scene.Frame));
                value.SetProperty("positionWrites", scene.PositionWrites); value.SetProperty("partCount", scene.PartCount);
                value.SetProperty("simulationOrdinal", Exact(scene.SimulationTime.Ordinal)); value.SetProperty("simulationPhase", scene.SimulationTime.Phase);
                value.SetProperty("sampleOrdinal", Exact(sample.SimulationTime.Time.Ordinal)); value.SetProperty("samplePhase", sample.SimulationTime.Time.Phase);
                if (sample.DisplayWall is { } wall) value.SetProperty("displayWall", Exact(wall.Value));
                value.SetProperty("selectedAt", Exact(evidence.SelectedAt.Value)); value.SetProperty("clockEpoch", Exact(evidence.ClockEpoch));
                value.SetProperty("displayEpoch", Exact(evidence.DisplayEpoch)); value.SetProperty("world", Exact(evidence.WorldEpoch.Value));
                using var position = Vector<T>(scene.X, scene.Y, scene.Z); value.SetProperty("position", position);
                using var rotation = Vector<T>(scene.Qx, scene.Qy, scene.Qz); rotation.SetProperty("w", scene.Qw); value.SetProperty("rotation", rotation);
                if (evidence.Latest is { } latest) { using var endpoint = Endpoint<T>(latest); value.SetProperty("latest", endpoint); }
                if (evidence.Before is { } before) { using var endpoint = Endpoint<T>(before); value.SetProperty("before", endpoint); }
                if (evidence.After is { } after) { using var endpoint = Endpoint<T>(after); value.SetProperty("after", endpoint); }
                using var bodies = T.Array();
                for (var i = 0; i < sample.Bodies.Count; i++)
                {
                    var body = sample.Bodies[i]; using var item = T.Object(); item.SetProperty("id", Exact(body.Id.Value));
                    using var cell = Cell<T>(body.Cell); item.SetProperty("cell", cell);
                    using var local = Vector<T>(body.Local.X, body.Local.Y, body.Local.Z); item.SetProperty("local", local);
                    using var orientation = Vector<T>(body.Rotation.X, body.Rotation.Y, body.Rotation.Z);
                    orientation.SetProperty("w", body.Rotation.W); item.SetProperty("rotation", orientation); bodies.Push(item);
                }
                value.SetProperty("bodies", bodies); break;
            }
            default: throw new ArgumentException("Unsupported diagnostic payload.", nameof(source));
        }
        return value;
    }
    internal static T TraceSample<T>(WorkshopTraceSample sample) where T : IWorkshopObservationObject<T>
    {
        var value = T.Object();
        value.SetProperty("epoch", Exact(sample.Epoch.Value)); value.SetProperty("tick", Exact(sample.Tick.Value));
        SampleFields(value, sample);
        return value;
    }
    private static void SampleFields<T>(T value, WorkshopTraceSample sample) where T : IWorkshopObservationObject<T>
    {
        using var bodies = T.Array();
        for (var i = 0; i < sample.Bodies.Count; i++)
        {
            var body = sample.Bodies[i]; using var row = T.Object();
            row.SetProperty("id", Exact(body.Body.Id.Value));
            using var cell = Cell<T>(body.Body.Cell);
            using var local = Vector<T>(body.Body.Local.X, body.Body.Local.Y, body.Body.Local.Z);
            using var velocity = Vector<T>(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z);
            using var rotation = Vector<T>(body.Rotation.X, body.Rotation.Y, body.Rotation.Z);
            rotation.SetProperty("w", body.Rotation.W);
            using var angular = Vector<T>(body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            using var centre = Vector<T>(body.LocalCentreOfMass.X, body.LocalCentreOfMass.Y, body.LocalCentreOfMass.Z);
            row.SetProperty("angularVelocity", angular); row.SetProperty("localCentreOfMass", centre);
            row.SetProperty("cell", cell); row.SetProperty("local", local); row.SetProperty("velocity", velocity); row.SetProperty("rotation", rotation);
            bodies.Push(row);
        }
        value.SetProperty("bodies", bodies);
        using var captures = T.Array();
        for (var i = 0; i < sample.Captures.Count; i++)
        {
            var capture = sample.Captures[i]; using var row = T.Object();
            row.SetProperty("sensor", Exact(capture.Sensor.Value)); row.SetProperty("target", Exact(capture.Target.Value));
            row.SetProperty("phase", WorkshopObservationProjection.EnumValue(capture.Phase)); row.SetProperty("ordinal", capture.EventOrdinal);
            row.SetProperty("eventPhase", capture.EventPhase); captures.Push(row);
        }
        value.SetProperty("captures", captures);
    }
}
