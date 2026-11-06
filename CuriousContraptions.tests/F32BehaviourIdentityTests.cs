using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Current catalogue outcomes through production simulation: goals and causal signal ownership,
/// finite stall guards, authored Reset and semantic replay after Save/Load. Allocated identities and
/// unwired-node populations are derived from current declarations, never migration-era transcripts.</summary>
public sealed class F32BehaviourIdentityTests(ITestOutputHelper output)
{
    private const int LessonTicks = 1200; // Ten seconds at 120 Hz: a stall guard, not an exact completion-time requirement.
    private const int ElementTicks = 240;

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
        foreach (var part in SaveFixture.Elements.OrderBy(part => part.Value)) data.Add(part.Value);
        return data;
    }

    [Fact]
    public void EveryCurrentLessonAndElementHasAnExecutedScenario()
    {
        Assert.Equal(PuzzleCatalogue.Playable.OrderBy(id => id.Value), LaunchLessonSolveTests.Lessons.OrderBy(id => id.Value));
        foreach (var part in SaveFixture.Elements) Assert.Equal(part, PackageRuntimeParityTests.Placed(part).PartId);
    }

    [Theory]
    [MemberData(nameof(Lessons))]
    public async Task ALessonPreservesGoalsAndCausalEventOrderAndReplaysAfterSaveLoad(string lesson, float precision)
    {
        var bench = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(lesson), new(2), WorkshopCadenceSettings.Default(), new(1), new(precision));
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, LessonTicks);
        var (goalTick, events) = Outcome(run, bench);
        output.WriteLine($"{lesson}/{precision}: goal={goalTick is not null}, tick={goalTick}");
        AssertGoalEvents(bench, events);
        Assert.NotNull(goalTick);
        Assert.InRange(goalTick.Value, 1, LessonTicks);
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");

        var saved = new WorkshopSavedConstruction(bench, new(bench.Instances.Max(instance => instance.Id.Value) + 1));
        var reloaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, reloaded);
        var (replayBuilt, replay, replayReset) = await WorkshopIntegrationHarness.ObserveReset(reloaded.Construction, LessonTicks);
        var (replayGoal, replayEvents) = Outcome(replay, reloaded.Construction);
        Assert.NotNull(replayGoal);
        Assert.InRange(replayGoal.Value, 1, LessonTicks);
        AssertGoalEvents(reloaded.Construction, replayEvents);
        NoFixedCapacityTests.AssertSameState(replayBuilt, replayReset, "reloaded Reset");
    }

    [Theory]
    [MemberData(nameof(Elements))]
    public async Task AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays(string element)
    {
        var bench = new WorkshopConstruction(new ConstructionRevision(2), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(PackageRuntimeParityTests.Placed(new PartId(element))));
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(bench, ElementTicks);
        var (_, events) = Outcome(run, bench);
        AssertStandaloneControl(events);
        NoFixedCapacityTests.AssertSameState(built, reset, "standalone Reset");
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(bench, ElementTicks);
        AssertStandaloneControl(Outcome(replay, bench).Events);
    }

    private static void AssertStandaloneControl(IReadOnlyList<ObservedEvent> events)
    {
        // No wires, external payload or supply: logical memory/timers/counters cannot activate.
        // A lone dynamic body's own physical orientation sensor may still observe its fall.
        Assert.All(events.Where(item => item.State.Activation == ActivationPhase.Latched),
            item => Assert.Equal(ActivationOccurrenceKind.Orientation, item.State.Kind));
    }

    private static void AssertGoalEvents(WorkshopConstruction bench, IReadOnlyList<ObservedEvent> events)
    {
        var goal = bench.Puzzle.Goal;
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            var captured = Assert.Single(events, item => item.State.Category == EventCategory.Capture &&
                item.State.Capture == CaptureLatchPhase.Latched);
            Assert.Equal(CaptureState(goal, CaptureLatchPhase.Latched), captured.State);
            Assert.Contains(events, item => item.State.Capture == CaptureLatchPhase.Clear && item.Tick <= captured.Tick);
            return;
        }
        Assert.Equal(WorkshopGoalKind.ActivatedAfter, goal.Kind);
        var source = bench.Instances.Identities[goal.Body].Signals[goal.SourceNode].Node;
        var target = bench.Instances.Identities[goal.Target].Signals[goal.TargetNode].Node;
        var start = Assert.Single(events, item => item.State.Node == source && item.State.Activation == ActivationPhase.Latched);
        var finish = Assert.Single(events, item => item.State.Node == target && item.State.Activation == ActivationPhase.Latched);
        Assert.Equal(goal.Body, start.State.Owner);
        Assert.Equal(goal.Target, finish.State.Owner);
        Assert.True(start.Tick <= finish.Tick, "The selected source activates before its goal target.");
        Assert.True(start.Ordinal <= finish.Ordinal, "The target cannot precede its causal source.");
        Assert.Equal(start.CauseOrdinal, finish.CauseOrdinal);
        Assert.Equal(start.State.CauseBody, finish.State.CauseBody);
        Assert.Equal(start.State.CauseOwner, finish.State.CauseOwner);
    }

    private static GpuBodyId CauseOwner(ActivationLatch row, IPhysicsSceneDeclaration scene) => row.Kind switch
    {
        ActivationOccurrenceKind.Contact or ActivationOccurrenceKind.TimerElapsed =>
            Assert.Single(scene.Triggers.ToArray(), sensor => sensor.Id.Value == row.Source.Value).Owner,
        ActivationOccurrenceKind.Orientation =>
            Assert.Single(scene.OrientationSensors.ToArray(), sensor => sensor.Id.Value == row.Source.Value && sensor.Body == row.Body).Body,
        _ => throw new ArgumentException("These catalogue scenarios produce contact, timer or orientation causes only.")
    };

    private static EventState CaptureState(WorkshopGoal goal, CaptureLatchPhase phase) =>
        new(EventCategory.Capture, goal.Target, goal.Body, default, phase, null, null, default, default, default);

    private static (int? GoalTick, List<ObservedEvent> Events) Outcome(
        List<WorkshopRead> run, WorkshopConstruction bench)
    {
        int? goalTick = null;
        var events = new List<ObservedEvent>();
        var previous = new Dictionary<(EventCategory, GpuBodyId, ActivationNodeId, GpuBodyId), ObservedEvent>();
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, bench, new(1, 2));
        var identities = bench.Instances.Identities;
        var declaredNodes = bench.Instances.SelectMany(instance =>
            (DeclaredPhysics.Of(instance.PartId).Signal?.Nodes ?? []).Select(node =>
                (Node: identities[instance.Id].Signals[node.Id].Node, Owner: instance.Id))).OrderBy(row => row.Node.Value).ToArray();
        foreach (var read in run)
        {
            Assert.Equal(declaredNodes.Length, read.Activations.Count);
            for (var i = 0; i < declaredNodes.Length; i++)
                Assert.Equal(declaredNodes[i], (read.Activations[i].Node, read.Activations[i].Owner));
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
                    kind = row.Kind; owner = CauseOwner(row, scene);
                    Assert.Contains(declaredNodes, node => node.Node == row.Emitter);
                    Assert.Equal(owner, Assert.Single(scene.Colliders.ToArray(), collider => collider.Id == row.Collider).Body);
                    if (row.Kind is ActivationOccurrenceKind.Contact or ActivationOccurrenceKind.TimerElapsed)
                        Assert.True(row.ApproachSpeed.Value >= Assert.Single(scene.Triggers.ToArray(), sensor => sensor.Id.Value == row.Source.Value).Threshold.Value);
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
