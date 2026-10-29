using System.Text.Json;
using System.Text.Json.Nodes;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 behaviour identity against the D5 outcome baseline: Fixtures/outcomes-f1ad3bd1.json was captured natively on
/// the half-precision-era engine at f1ad3bd1 before any 9A.44 edit. Every authored lesson at precisions 0, 0.45 and 1, and every element
/// fixture placed alone, runs on the production simulation over the worker transport. Preserve goals and typed event ownership,
/// kinds and causal order. Lessons solve within a finite stall guard and replay their semantic observations after Save/Load;
/// Reset restores authored state. Historical completion ticks and ordinals are not gameplay requirements (owner, 28 October 2026).
/// Standalone parts retain event semantics and deterministic replay; physical support, drift and energy belong to their behavioral
/// suites. Timer, delay and cadence timing is tested by those features.</summary>
public sealed class F32BehaviourIdentityTests(ITestOutputHelper output)
{
    private const int LessonTicks = 1200; // Ten seconds at 120 Hz: a stall guard, not an exact completion-time requirement.
    private const int ElementTicks = 240;
    private static readonly Lazy<JsonObject> Baseline = new(() => JsonNode.Parse(File.ReadAllText(Path.Combine(
        twodog.Engine.ResolveProjectDir(), "CuriousContraptions.tests/Fixtures/outcomes-f1ad3bd1.json")))!.AsObject());

    private enum EventCategory { Capture, Activation }
    private readonly record struct EventState(EventCategory Category, GpuBodyId Owner, GpuBodyId Target,
        ActivationNodeId Node, CaptureLatchPhase? Capture, ActivationPhase? Activation, ActivationOccurrenceKind? Kind,
        ActivationNodeId Emitter, GpuBodyId CauseBody, GpuBodyId CauseOwner);
    private readonly record struct ObservedEvent(EventState State, long Tick, uint Ordinal, uint CauseOrdinal);

    public static TheoryData<string, float> Lessons()
    {
        var data = new TheoryData<string, float>();
        foreach (var lesson in LaunchLessonSolveTests.Lessons)
            foreach (var precision in new[] { 0f, .45f, 1f }) data.Add(lesson.Value, precision);
        return data;
    }

    public static TheoryData<string> Elements()
    {
        var data = new TheoryData<string>();
        foreach (var row in Baseline.Value["Elements"]!.AsArray()) data.Add((string)row!["Id"]!);
        return data;
    }

    [Fact]
    public void TheBaselineCoversEveryAuthoredLessonAtThreePrecisions() =>
        Assert.Equal(Lessons().Count, Baseline.Value["Lessons"]!.AsArray().Count);

    [Theory]
    [MemberData(nameof(Lessons))]
    public async Task ALessonPreservesGoalsAndCausalEventOrderAndReplaysAfterSaveLoad(string lesson, float precision)
    {
        var expected = Baseline.Value["Lessons"]!.AsArray().Single(row =>
            (string)row!["Id"]! == lesson && Math.Abs((double)row["Precision"]! - precision) <= 1.0 / 2048)!;
        var bench = PuzzleCatalogue.CreateSolution(new(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(precision));
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, LessonTicks);
        var (goalTick, events) = Outcome(run, bench);
        Assert.Equal((bool)expected["GoalMet"]!, goalTick is not null);
        output.WriteLine($"{lesson}/{precision}: goal={goalTick is not null}, tick={goalTick}, baseline={(int?)expected["GoalTick"]}");
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Compare(expected["Events"]!.AsArray().Select(e => Historical((string)e!, bench, scene)).ToArray(), events);
        Assert.NotNull(goalTick);
        Assert.InRange(goalTick.Value, 1, LessonTicks);
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");

        var saved = new WorkshopSavedConstruction(bench, new(bench.Instances.Max(instance => instance.Id.Value) + 1));
        var reloaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, reloaded);
        var (replayBuilt, replay, replayReset) = await WorkshopIntegrationHarness.ObserveReset(reloaded.Construction, LessonTicks);
        var (replayGoal, replayEvents) = Outcome(replay, reloaded.Construction);
        Assert.NotNull(replayGoal);
        Assert.InRange(replayGoal.Value, 1, LessonTicks);
        Compare(events, replayEvents);
        NoFixedCapacityTests.AssertSameState(replayBuilt, replayReset, "reloaded Reset");
    }

    [Theory]
    [MemberData(nameof(Elements))]
    public async Task AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays(string element)
    {
        var expected = Baseline.Value["Elements"]!.AsArray().Single(row => (string)row!["Id"]! == element)!;
        var bench = new WorkshopConstruction(new ConstructionRevision(2), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(PackageRuntimeParityTests.Placed(new PartId(element))));
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, ElementTicks);
        var (_, events) = Outcome(run, bench);
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        Compare(expected["Events"]!.AsArray().Select(e => Historical((string)e!, bench, scene)).ToArray(), events);
        NoFixedCapacityTests.AssertSameState(built, reset, "standalone Reset");
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(bench, ElementTicks);
        Compare(events, Outcome(replay, bench).Events);
    }

    private static void Compare(IReadOnlyList<ObservedEvent> expected, IReadOnlyList<ObservedEvent> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++) Assert.Equal(expected[i].State, actual[i].State);
    }

    [Fact]
    public void EventComparisonPreservesOwnersTargetsKindsAndOrderAcrossTimingChanges()
    {
        var state = new EventState(EventCategory.Capture, new(7), new(2), default, CaptureLatchPhase.Clear, null, null, default, default, default);
        var first = new ObservedEvent(state, 1, 0, 0);
        var last = new ObservedEvent(state with { Capture = CaptureLatchPhase.Latched }, 335, 1340, 0);
        ObservedEvent[] expected = [first, last];
        Compare(expected, [first, last with { Tick = 356, Ordinal = 1424, CauseOrdinal = 1400 }]);
        foreach (var changed in new[] { last with { State = last.State with { Owner = new(8) } },
            last with { State = last.State with { Target = new(3) } },
            last with { State = last.State with { Capture = CaptureLatchPhase.Clear } },
            last with { State = last.State with { Kind = ActivationOccurrenceKind.Contact } },
            last with { State = last.State with { Emitter = new(4) } },
            last with { State = last.State with { CauseOwner = new(8) } } })
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Compare(expected, [first, changed]));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Compare(expected, [last, first]));
    }

    // External historical fixture boundary only. Authored owner/body/node identities retain their
    // meaning; obsolete allocated sensor/collider slots are never decoded or mapped into current IDs.
    private static ObservedEvent Historical(string text, WorkshopConstruction bench, PhysicsSceneDeclaration scene)
    {
        var colon = text.IndexOf(':');
        var tick = long.Parse(text.AsSpan(5, colon - 5), System.Globalization.CultureInfo.InvariantCulture);
        var value = text[(colon + 2)..];
        if (value.StartsWith("capture ", StringComparison.Ordinal))
        {
            Assert.Equal(WorkshopGoalKind.Captured, bench.Puzzle.Goal.Kind);
            var fields = value.Split(' ');
            var phaseText = fields.Single(field => field.StartsWith("phase=", StringComparison.Ordinal))[6..];
            Assert.True(Enum.TryParse<CaptureLatchPhase>(phaseText, out var phase) && Enum.IsDefined(phase));
            var ordinal = uint.Parse(fields.Single(field => field.StartsWith("ordinal=", StringComparison.Ordinal))[8..], System.Globalization.CultureInfo.InvariantCulture);
            return new(CaptureState(bench.Puzzle.Goal, phase), tick, ordinal, 0);
        }
        Assert.StartsWith("activation ", value);
        using var document = JsonDocument.Parse(value[11..]);
        var row = document.RootElement;
        ulong Id(string name) => row.GetProperty(name).GetProperty("Value").GetUInt64();
        var activation = EnumValue<ActivationPhase>(row.GetProperty("Phase").GetUInt32());
        var kind = activation == ActivationPhase.Clear ? (ActivationOccurrenceKind?)null : EnumValue<ActivationOccurrenceKind>(row.GetProperty("Kind").GetUInt32());
        var body = new GpuBodyId(Id("Body"));
        var state = new EventState(EventCategory.Activation, new(Id("Owner")), default, new(Id("Node")), null,
            activation, kind, new(Id("Emitter")), body, kind is { } cause ? CauseOwner(cause, body, scene) : default);
        return new(state, tick, row.GetProperty("EventOrdinal").GetUInt32(), row.GetProperty("CauseOrdinal").GetUInt32());
    }

    private static T EnumValue<T>(uint value) where T : struct, Enum
    {
        var typed = (T)Enum.ToObject(typeof(T), value);
        Assert.True(Enum.IsDefined(typed));
        return typed;
    }

    private static GpuBodyId CauseOwner(ActivationOccurrenceKind kind, GpuBodyId body, PhysicsSceneDeclaration scene) => kind switch
    {
        ActivationOccurrenceKind.Contact or ActivationOccurrenceKind.TimerElapsed => Assert.Single(scene.Triggers.ToArray()).Owner,
        ActivationOccurrenceKind.Orientation => Assert.Single(scene.OrientationSensors.ToArray(), sensor => sensor.Body == body).Body,
        _ => throw new ArgumentException("The retained baseline has no such cause kind.")
    };

    private static EventState CaptureState(WorkshopGoal goal, CaptureLatchPhase phase) =>
        new(EventCategory.Capture, goal.Target, goal.Body, default, phase, null, null, default, default, default);

    private static (int? GoalTick, List<ObservedEvent> Events) Outcome(
        List<WorkshopRead> run, WorkshopConstruction bench)
    {
        int? goalTick = null;
        var events = new List<ObservedEvent>();
        var previous = new Dictionary<(EventCategory, GpuBodyId, ActivationNodeId, GpuBodyId), ObservedEvent>();
        var scene = WorkshopPhysicsCompiler.Compile(bench, new(1, 2));
        var identities = bench.Instances.Identities;
        foreach (var read in run)
        {
            if (goalTick is null && bench.Puzzle.Goal.Kind != WorkshopGoalKind.None &&
                WorkshopGoalEvaluator.Occurrence(bench.Puzzle.Goal, read.Captures, read.Activations, identities) is not null)
                goalTick = (int)read.Tick.Value;
            if (bench.Puzzle.Goal.Kind == WorkshopGoalKind.Captured)
            {
                var goal = bench.Puzzle.Goal;
                Assert.True(identities.TryCapture(goal.Target, goal.Body, out var policy));
                for (var i = 0; i < read.Captures.Count; i++)
                {
                    Assert.Equal(policy.Sensor, read.Captures[i].Sensor);
                    Assert.Equal(goal.Body, read.Captures[i].Target);
                }
                var exists = read.Captures.TryGet(new(policy.Sensor, goal.Body), out var capture);
                Changed(new(CaptureState(goal, exists ? capture.Phase : CaptureLatchPhase.Clear), (long)read.Tick.Value,
                    exists ? capture.EventOrdinal : 0, 0));
            }
            else Assert.Equal(0, read.Captures.Count);
            for (var i = 0; i < read.Activations.Count; i++)
            {
                var row = read.Activations[i]; row.Validate();
                GpuBodyId owner = default;
                ActivationOccurrenceKind? kind = null;
                if (row.Phase == ActivationPhase.Latched)
                {
                    kind = row.Kind; owner = CauseOwner(row.Kind, row.Body, scene);
                    var source = row.Kind == ActivationOccurrenceKind.Orientation ? identities[owner].Orientation.Value : identities[owner].Trigger.Value;
                    Assert.Equal(source, row.Source.Value);
                    Assert.Equal(owner, Assert.Single(scene.Colliders.ToArray(), collider => collider.Id == row.Collider).Body);
                    if (row.Kind is ActivationOccurrenceKind.Contact or ActivationOccurrenceKind.TimerElapsed)
                        Assert.True(row.ApproachSpeed.Value >= Assert.Single(scene.Triggers.ToArray()).Threshold.Value);
                    if (row.Kind == ActivationOccurrenceKind.Orientation) Assert.Equal(identities[owner].Colliders[0], row.Collider);
                }
                Changed(new(new(EventCategory.Activation, row.Owner, default, row.Node, null, row.Phase, kind, row.Emitter, row.Body, owner),
                    (long)read.Tick.Value, row.EventOrdinal, row.CauseOrdinal));
            }
        }
        return (goalTick, events);

        void Changed(ObservedEvent current)
        {
            var key = (current.State.Category, current.State.Owner, current.State.Node, current.State.Target);
            if (previous.TryGetValue(key, out var prior) && prior.State == current.State && prior.Ordinal == current.Ordinal && prior.CauseOrdinal == current.CauseOrdinal) return;
            events.Add(current); previous[key] = current;
        }
    }
}
