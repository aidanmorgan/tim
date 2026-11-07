using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ComposableSignalNodesTests
{
    private static readonly PartId Part = new("composed_signal");
    private static SignalEndpoint Endpoint(string node, SignalTerminal terminal) => new(new(node), terminal);
    private static SignalEdge Edge(string source, string target, SignalTerminal input = SignalTerminal.Input) =>
        new(Endpoint(source, SignalTerminal.Output), Endpoint(target, input));
    private static ParameterDescriptor Seconds(string key, float value) =>
        new(key, ParameterUnit.Second, ParameterKind.Real, .01f, 4f, value, []);
    private static BundleEntry Entry(bool reverse = false, bool connected = true, bool cosmetics = true)
    {
        var clock = TestBundle.Entry(new("clock"));
        SignalNodeDescriptor[] nodes =
        [
            new SignalOscillatorDescriptor(new("fast"), new("fast_seconds"), PortId.PowerIn),
            new SignalOscillatorDescriptor(new("slow"), new("slow_seconds"), PortId.PowerIn),
            new SignalPulseCountDescriptor(new("count"), new("target")),
            new SignalTimerDescriptor(new("delay"), new("delay_seconds"), TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation),
            new SignalTimerDescriptor(new("quiet"), new("quiet_seconds"), TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation),
            new SignalMemoryDescriptor(new("memory_a")),
            new SignalMemoryDescriptor(new("memory_b"))
        ];
        SignalEdge[] edges = [Edge("fast", "count"), Edge("slow", "count"), Edge("delay", "memory_a", SignalTerminal.Set)];
        if (connected) edges = [.. edges, Edge("fast", "delay")];
        var ports = clock.Ports.Where(port => port.Domain == SimulationDomain.Electrical).ToArray();
        return clock with
        {
            Metadata = clock.Metadata with { Id = Part.Value, PaletteOrder = 410, Parameters =
                [Seconds("fast_seconds", .1f), Seconds("slow_seconds", .2f), Seconds("delay_seconds", .05f), Seconds("quiet_seconds", .15f),
                 new("target", ParameterUnit.None, ParameterKind.WholeNumber, 1, MathF.BitDecrement(4294967296f), 12, [])] },
            Footprint = default, Mechanical = null, Colliders = [], ColliderBindings = [], Ports = ports,
            Signal = new(reverse ? nodes.Reverse().ToArray() : nodes, reverse ? edges.Reverse().ToArray() : edges, []),
            Events = [new(SimulationEventKind.TriggerActivated, new("fast")), new(SimulationEventKind.TriggerActivated, new("slow")),
                new(SimulationEventKind.TriggerActivated, new("delay")), new(SimulationEventKind.TriggerActivated, new("quiet")),
                new(SimulationEventKind.ThresholdReached, new("count")), new(SimulationEventKind.MemoryChanged, new("memory_a")),
                new(SimulationEventKind.MemoryChanged, new("memory_b"))],
            Presentation = new(1, PresentationAxes.None, cosmetics ?
                [Binding("Raw", "fast", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.Identity),
                 Binding("Wave", "fast", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.SineCyclesToUnit),
                 Binding("Timer", "delay", PresentationDriver.CommittedTimerProgress, PresentationValueMapping.SineCyclesToUnit)] : [], [], [], [])
        };
    }
    private static PresentationBinding Binding(string target, string node, PresentationDriver driver, PresentationValueMapping mapping) =>
        new(target, PresentationProperty.LocalX, driver, mapping, 0, 1, PresentationCurve.Linear, 0,
            PresentationOverlap.Replace, 0, PresentationTimerPhase.None, SignalNode: new(node));
    private static WorkshopConstruction Bench(bool supplied = true) => new(new(2), WorkshopCadenceSettings.Default(),
        new(PlacedParts.Battery(new(1), -4, 4, 0, enabled: supplied ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Part, new(2), 0, 4, 0, 0, 0, 0, 1)),
        Connections: new(new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
    private static ActivationNodeId Node(WorkshopConstruction bench, string local) => bench.Instances.Identities[new(2)].Signals[new(local)].Node;
    private static ActivationLatch Latch(WorkshopRead read, ActivationNodeId node) =>
        Assert.Single(Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]), row => row.Node == node);

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public async Task IndependentNodesRouteCountAndResetThroughProduction(bool reverse, bool connected, bool cosmetics)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entries = PartRegistry.Read(RuntimeTestServices.Bundle, BundleSchema.WriteEntries([Entry(reverse, connected, cosmetics)]));
        using var physics = DeclaredPhysics.Scoped(entries);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend(entries.Select(entry => (new PartId(entry.Metadata.Id), entry.Ports))));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        await harness.Advance(120); // One second: ten fast and five slow pulses; intentional clock law.
        var network = harness.Simulation.Scene.Activation;
        Assert.Equal(15u, network.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        Assert.Equal(connected ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(harness.Committed, Node(bench, "memory_a")).Phase);
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Node(bench, "memory_b")).Phase);
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Node(bench, "quiet")).Phase);
        Assert.Equal(10, harness.Of(SimulationEventKind.TriggerActivated).Count(record => record.Source == new GpuEventSourceId(Node(bench, "fast").Value)));
        Assert.Equal(5, harness.Of(SimulationEventKind.TriggerActivated).Count(record => record.Source == new GpuEventSourceId(Node(bench, "slow").Value)));
        var threshold = Assert.Single(harness.Of(SimulationEventKind.ThresholdReached));
        Assert.Equal(new GpuEventSourceId(Node(bench, "count").Value), threshold.Source);
        Assert.Equal(12u, threshold.Count);
        Assert.Equal(96UL, threshold.Tick); // Eight fast + four slow pulses, including distinct same-time inputs.
        await harness.Reset();
        Assert.All(Enumerable.Range(0, harness.Committed.Memory.Count), i => Assert.Equal(0u, harness.Committed.Memory[i].Count));
        Assert.All(Enumerable.Range(0, harness.Committed.Activations.Count), i => Assert.Equal(ActivationPhase.Clear, harness.Committed.Activations[i].Phase));
        Assert.Equal(bench.Instances, harness.Construction.Instances);
    }

    [Fact]
    public async Task RawPhaseAndWaveMappingAreSeparateAndUnpoweredSourcesRemainQuiet()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench(false);
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(12);
        var presentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), harness.Simulation.Scene, harness.Latest);
        var samples = presentation.Advance(new(harness.Committed.Tick.Value * 4));
        float Value(int binding) => BitConverter.UInt32BitsToSingle(Assert.Single(samples, sample => sample.Owner == new GpuBodyId(2) && sample.Binding == binding).Value.CanonicalBits);
        Assert.Equal(0f, Value(0));
        Assert.Equal(.5f, Value(1));
        Assert.Equal(.5f, Value(2));
        Assert.Empty(harness.Of(SimulationEventKind.TriggerActivated));
        Assert.Equal(0u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(1.5f)]
    [InlineData(4294967296f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CounterTargetRejectsValuesOutsideItsDiscreteDomain(float target) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopActivationCompiler.CounterTarget(target));

    [Fact]
    public void CountLawAdmitsRepresentationBoundaryAndSaturatesWithoutRepeatedCrossing()
    {
        var predecessor = MathF.BitDecrement(4294967296f);
        Assert.Equal(4294967040u, WorkshopActivationCompiler.CounterTarget(predecessor));
        var row = new ActivationMemory(new(71), ActivationNodeKind.Counter, uint.MaxValue - 1, new(4, 0), OscillatorPhase.Stopped, 0);
        var first = row.CountInput(uint.MaxValue, new(8, 0));
        Assert.True(first.Crossed);
        Assert.Equal(uint.MaxValue, first.Memory.Count);
        var next = first.Memory.CountInput(uint.MaxValue, new(8, 0));
        Assert.False(next.Crossed);
        Assert.Equal(uint.MaxValue, next.Memory.Count);
    }

    [Fact]
    public void InvalidLocalReferencesAndInstantaneousCyclesAreRefused()
    {
        var entry = Entry();
        void Refused(BundleEntry invalid) => Assert.NotEmpty(BundleValidator.Validate([new("composed", invalid)]));
        Refused(entry with { Signal = entry.Signal! with { Nodes = [.. entry.Signal!.Nodes, entry.Signal.Nodes[0]] } });
        Refused(entry with { Signal = entry.Signal! with { Edges = [Edge("missing", "count")] } });
        Refused(entry with { Signal = entry.Signal! with { Edges = [Edge("count", "count")] } });
        Refused(entry with { Signal = entry.Signal! with { Edges = [Edge("memory_a", "count")] } });
        Refused(entry with { Presentation = entry.Presentation with { Bindings = [Binding("bad", "missing", PresentationDriver.CommittedCount, PresentationValueMapping.Identity)] } });
        Refused(entry with { Presentation = entry.Presentation with { Bindings = [Binding("bad", "count", PresentationDriver.CommittedCount, (PresentationValueMapping)0)] } });
        var pair = entry with { Signal = new([new SignalPulseCountDescriptor(new("a"), new("target")),
            new SignalPulseCountDescriptor(new("b"), new("target"))], [Edge("a", "b"), Edge("b", "a")], []), Events = [], Presentation = entry.Presentation with { Bindings = [] } };
        Refused(pair);
    }

    [Fact]
    public async Task RunningSourcesKeepRawPhaseSeparateFromSharedWaveMapping()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry() with { Presentation = Entry().Presentation with { Bindings =
            [Binding("fast_raw", "fast", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.Identity),
             Binding("fast_wave", "fast", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.SineCyclesToUnit),
             Binding("slow_raw", "slow", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.Identity),
             Binding("slow_wave", "slow", PresentationDriver.CommittedOscillatorPhase, PresentationValueMapping.SineCyclesToUnit),
             Binding("timer_raw", "delay", PresentationDriver.CommittedTimerProgress, PresentationValueMapping.Identity),
             Binding("timer_wave", "delay", PresentationDriver.CommittedTimerProgress, PresentationValueMapping.SineCyclesToUnit)] } };
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        await harness.Require(WorkshopCommandKind.Construct, Bench());
        await harness.Run();
        await harness.Advance(3);
        float[] Values()
        {
            var presentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), harness.Simulation.Scene, harness.Latest);
            return presentation.Advance(new(harness.Committed.Tick.Value * 4)).Where(sample => sample.Owner == new GpuBodyId(2))
                .OrderBy(sample => sample.Binding).Select(sample => BitConverter.UInt32BitsToSingle(sample.Value.CanonicalBits)).ToArray();
        }
        // Declared periods12 and24 commits: phase is elapsed/period. This tests the local map's mathematical contract.
        var values = Values();
        Assert.Equal(.25f, values[0]); Assert.Equal(.125f, values[2]);
        Assert.InRange(values[1], .99999f, 1.00001f);
        var slowWave = .5 + .5 * Math.Sin(Math.PI / 4);
        Assert.InRange(values[3], (float)slowWave - .00001f, (float)slowWave + .00001f);
        await harness.Advance(11); // Delay starts at12 and finishes at18; at14 progress is one third.
        values = Values();
        Assert.InRange(values[4], 1f / 3 - .00001f, 1f / 3 + .00001f);
        var timerWave = .5 + .5 * Math.Sin(2 * Math.PI / 3);
        Assert.InRange(values[5], (float)timerWave - .00001f, (float)timerWave + .00001f);
    }

    [Fact]
    public async Task PublicAliasesOfAnInternalDeliveryCountOnce()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        PortId first = new("first_alias"), second = new("second_alias"), input = new("counter_input");
        var source = new PortDescriptor(first, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut,
            PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY);
        entry = entry with
        {
            Ports = [.. entry.Ports, source, source with { Id = second },
                new(input, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY)],
            Signal = entry.Signal! with { Ports =
                [new(first, Endpoint("fast", SignalTerminal.Output)), new(second, Endpoint("fast", SignalTerminal.Output)),
                 new(input, Endpoint("count", SignalTerminal.Input))] }
        };
        Assert.Empty(BundleValidator.Validate([new("aliases", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        bench = bench with { Connections = new([.. bench.Connections,
            new(new(2), first, new(2), input, WorkshopConnectionDomain.Activation),
            new(new(2), second, new(2), input, WorkshopConnectionDomain.Activation)]) };
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        await harness.Advance(24);
        Assert.Equal(3u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
    }

    [Fact]
    public async Task SameOwnerGoalsPauseStepResumeAndSaveLoadKeepExactNodeSelection()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        var goal = new WorkshopGoal(WorkshopGoalKind.ActivatedAfter, new(2), new(2), new("fast"), new("slow"));
        goal.Validate();
        Assert.Throws<ArgumentException>(() => (goal with { SourceNode = default }).Validate());
        Assert.Throws<ArgumentException>(() => (goal with { TargetNode = new("fast") }).Validate());
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        await harness.Advance(12);
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, harness.Committed, harness.Committed.Epoch, bench.Instances.Identities));
        await harness.Require(WorkshopCommandKind.Pause);
        var paused = harness.Committed.Tick;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Value + 1, harness.Committed.Tick.Value);
        await harness.Require(WorkshopCommandKind.Resume); await harness.Advance(11);
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, harness.Committed, harness.Committed.Epoch, bench.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal with { SourceNode = new("quiet") },
            harness.Committed, harness.Committed.Epoch, bench.Instances.Identities));
        var saved = new WorkshopSavedConstruction(bench, new(3));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Append(Part).ToHashSet(), WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded);
        await harness.Reset();
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, harness.Committed, harness.Committed.Epoch, bench.Instances.Identities));
        await harness.Require(WorkshopCommandKind.Construct, loaded.Construction with { Revision = new(3) });
        await harness.Run(); await harness.Advance(24);
        Assert.Equal(3u, harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, "count")).Count);
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, harness.Committed, harness.Committed.Epoch, loaded.Construction.Instances.Identities));
    }

    [Theory]
    [InlineData("memory_a", SignalContactState.Latched, false, true)]
    [InlineData("memory_b", SignalContactState.Latched, true, true)]
    [InlineData("delay", SignalContactState.TimerCounting, true, false)]
    public async Task ContactUsesOnlyItsDeclaredNode(string selected, SignalContactState state, bool early, bool late)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        var contact = TestBundle.Entry(new("latch"));
        entry = entry with
        {
            Ports = [.. entry.Ports, Assert.Single(contact.Ports, port => port.Id == PortId.Supply)],
            Electrical = contact.Electrical! with
            {
                Nodes = [.. entry.Electrical!.Nodes, .. contact.Electrical!.Nodes],
                SupplyGraph = contact.Electrical!.SupplyGraph! with
                {
                    Nodes = [.. contact.Electrical!.SupplyGraph!.Nodes.Select(node => node is SupplyContactDescriptor sc ? sc with { Control = new(new(selected), state) } : node)]
                }
            },
            Signal = entry.Signal! with { Edges = [.. entry.Signal!.Edges, Edge("fast", "memory_b", SignalTerminal.Set)] }
        };
        Assert.Empty(BundleValidator.Validate([new("selected_contact", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run();
        var contactNode = harness.Simulation.Scene.Activation.Contacts.Single(c => c.Owner.Value == 2).Contact;
        bool Closed() => harness.Simulation.Scene.Activation.ContactClosed(harness.Committed.Activations, harness.Committed.Timers, contactNode);
        Assert.False(Closed());
        await harness.Advance(14); // Fast pulse at12 sets B and starts delay; A waits for elapsed at18.
        Assert.Equal(early, Closed());
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Node(bench, "memory_a")).Phase);
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, Node(bench, "memory_b")).Phase);
        await harness.Advance(10);
        Assert.Equal(late, Closed());
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, Node(bench, "memory_a")).Phase);
        await harness.Reset();
        Assert.False(Closed());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FiniteTimerFeedbackRequiresASeedAndCompletesEachOneShotOnce(bool seeded)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        entry = entry with { Signal = entry.Signal! with { Edges =
            [.. entry.Signal!.Edges, Edge("delay", "quiet"), Edge("quiet", "delay")] } };
        Assert.Empty(BundleValidator.Validate([new("feedback", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench(seeded);
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Append(Part).ToHashSet(), WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(bench, new(3))));
        Assert.Equal(bench, loaded.Construction); // Positive-duration feedback remains valid through the same save graph admission.
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(60);
        foreach (var local in new[] { "delay", "quiet" })
        {
            var events = harness.Of(SimulationEventKind.TriggerActivated)
                .Where(row => row.Source == new GpuEventSourceId(Node(bench, local).Value)).ToArray();
            Assert.Equal(seeded ? 1 : 0, events.Length);
            Assert.Equal(seeded ? ActivationPhase.Latched : ActivationPhase.Clear, Latch(harness.Committed, Node(bench, local)).Phase);
            if (seeded) Assert.Equal(local == "delay" ? 18UL : 36UL, events[0].Tick); // Authored six- and eighteen-tick delays after the seed at12.
        }
    }
    [Theory]
    [InlineData("delay", "quiet", 6, 0)]
    [InlineData("quiet", "delay", 18, 1)]
    public async Task OnlyTheSelectedTimerChangesItsEventAndVisual(string selected, string inactive, int durationTicks, int binding)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        entry = entry with
        {
            Signal = entry.Signal! with { Edges = [Edge("fast", selected), Edge(selected, "memory_a", SignalTerminal.Set)] },
            Presentation = entry.Presentation with { Bindings =
                [Binding("FirstTimer", "delay", PresentationDriver.CommittedTimerProgress, PresentationValueMapping.Identity),
                 Binding("SecondTimer", "quiet", PresentationDriver.CommittedTimerProgress, PresentationValueMapping.Identity)] }
        };
        Assert.Empty(BundleValidator.Validate([new("two_timers", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(15); // Pulse at12; observe three commits into either declared delay.
        var timer = Assert.Single(Enumerable.Range(0, harness.Committed.Timers.Count).Select(i => harness.Committed.Timers[i]).ToArray(), row => row.Node == Node(bench, selected));
        Assert.Equal(ActivationTimerPhase.Counting, timer.Phase);
        Assert.Equal((ulong)durationTicks, timer.DueTick - timer.StartedTick);
        Assert.Equal(ActivationTimerState.Ready(Node(bench, inactive)),
            Assert.Single(Enumerable.Range(0, harness.Committed.Timers.Count).Select(i => harness.Committed.Timers[i]).ToArray(), row => row.Node == Node(bench, inactive)));
        var presentation = new WorkshopDeclaredPresentation(new CuriousContraptions.Presentation.AnimationServices(), harness.Simulation.Scene, harness.Latest);
        var samples = presentation.Advance(new(harness.Committed.Tick.Value * 4));
        float Value(int index) => BitConverter.UInt32BitsToSingle(Assert.Single(samples,
            sample => sample.Owner == new GpuBodyId(2) && sample.Binding == index).Value.CanonicalBits);
        // Local normalized-progress mapping contract; independent authored duration determines the fraction.
        Assert.InRange(Value(binding), 3f / durationTicks - .00001f, 3f / durationTicks + .00001f);
        Assert.Equal(0f, Value(1 - binding));
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Node(bench, "memory_a")).Phase);
        await harness.Advance(durationTicks - 3);
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, Node(bench, selected)).Phase);
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, Node(bench, "memory_a")).Phase);
        Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Node(bench, inactive)).Phase);
        Assert.Single(harness.Of(SimulationEventKind.TriggerActivated), row => row.Source == new GpuEventSourceId(Node(bench, selected).Value));
        Assert.DoesNotContain(harness.Of(SimulationEventKind.TriggerActivated), row => row.Source == new GpuEventSourceId(Node(bench, inactive).Value));
        await harness.Reset();
        Assert.All(Enumerable.Range(0, harness.Committed.Timers.Count).Select(i => harness.Committed.Timers[i]).ToArray(), row => Assert.Equal(ActivationTimerState.Ready(row.Node), row));
    }

    [Fact]
    public async Task BothOscillatorsLoseSharedSupplyAndRestartTheirOwnFreshIntervals()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(11);
        async Task Enable(ElectricalEnable enabled) => await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, new(1), RunControlValue.Of(enabled)));
        ActivationMemory Memory(string local) => harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, local));
        await Enable(ElectricalEnable.Disabled); await harness.Advance(25);
        foreach (var local in new[] { "fast", "slow" })
        {
            Assert.Equal(OscillatorPhase.Stopped, Memory(local).Phase);
            Assert.Equal(0u, Memory(local).Count);
            Assert.Equal(0UL, Memory(local).DueTick);
        }
        Assert.Empty(harness.Of(SimulationEventKind.TriggerActivated));
        await Enable(ElectricalEnable.Enabled); await harness.Advance(1);
        var restarted = harness.Committed.Tick.Value;
        Assert.Equal(restarted + 12, Memory("fast").DueTick);
        Assert.Equal(restarted + 24, Memory("slow").DueTick);
        await harness.Advance(11);
        Assert.Equal(0u, Memory("fast").Count); Assert.Equal(0u, Memory("slow").Count);
        await harness.Advance(1);
        Assert.Equal(1u, Memory("fast").Count); Assert.Equal(0u, Memory("slow").Count);
        await harness.Advance(11);
        Assert.Equal(1u, Memory("fast").Count); Assert.Equal(0u, Memory("slow").Count);
        await harness.Advance(1);
        Assert.Equal(2u, Memory("fast").Count); Assert.Equal(1u, Memory("slow").Count);
        Assert.Equal(2, harness.Of(SimulationEventKind.TriggerActivated).Count(row => row.Source == new GpuEventSourceId(Node(bench, "fast").Value)));
        Assert.Single(harness.Of(SimulationEventKind.TriggerActivated), row => row.Source == new GpuEventSourceId(Node(bench, "slow").Value));
        await harness.Reset();
        foreach (var local in new[] { "fast", "slow" })
        {
            Assert.Equal(OscillatorPhase.Stopped, Memory(local).Phase);
            Assert.Equal(0u, Memory(local).Count);
        }
    }

    private static BundleEntry PublicMemoryEntry()
    {
        var entry = Entry();
        PortDescriptor Port(string id, PortDirection direction, PortRole role) =>
            new(new(id), SimulationDomain.Signal, direction, role, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY);
        return entry with
        {
            Ports = [.. entry.Ports, Port("fast_out", PortDirection.Output, PortRole.ActivationOut),
                Port("slow_out", PortDirection.Output, PortRole.ActivationOut),
                Port("a_set", PortDirection.Input, PortRole.SetIn), Port("a_reset", PortDirection.Input, PortRole.ResetIn),
                Port("b_set", PortDirection.Input, PortRole.SetIn), Port("b_reset", PortDirection.Input, PortRole.ResetIn)],
            Signal = entry.Signal! with { Edges = [], Ports =
                [new(new("fast_out"), Endpoint("fast", SignalTerminal.Output)), new(new("slow_out"), Endpoint("slow", SignalTerminal.Output)),
                 new(new("a_set"), Endpoint("memory_a", SignalTerminal.Set)), new(new("a_reset"), Endpoint("memory_a", SignalTerminal.Reset)),
                 new(new("b_set"), Endpoint("memory_b", SignalTerminal.Set)), new(new("b_reset"), Endpoint("memory_b", SignalTerminal.Reset))] }
        };
    }

    [Theory]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 2UL, "memory_a", "a_set", "a_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 2UL, "memory_b", "b_set", "b_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 4UL, "memory_a", "a_set", "a_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 4UL, "memory_b", "b_set", "b_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 2UL, "memory_a", "a_set", "a_reset", false)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 2UL, "memory_b", "b_set", "b_reset", false)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 4UL, "memory_a", "a_set", "a_reset", false)]
    [InlineData(IndependentSignalCompositionTests.Composition.Bodyless, 4UL, "memory_b", "b_set", "b_reset", false)]
    [InlineData(IndependentSignalCompositionTests.Composition.Dynamic, 2UL, "memory_a", "a_set", "a_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Dynamic, 2UL, "memory_a", "a_set", "a_reset", false)]
    [InlineData(IndependentSignalCompositionTests.Composition.Jointed, 4UL, "memory_b", "b_set", "b_reset", true)]
    [InlineData(IndependentSignalCompositionTests.Composition.Jointed, 4UL, "memory_b", "b_set", "b_reset", false)]
    public async Task PublicSetResetSelectsOneMemoryAndOneOwner(IndependentSignalCompositionTests.Composition composition,
        ulong ownerValue, string memory, string setPort, string resetPort, bool resetConnected)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = IndependentSignalCompositionTests.Compose(PublicMemoryEntry(), composition);
        Assert.Empty(BundleValidator.Validate([new("public_memories", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        GpuBodyId owner = new(ownerValue), other = new(ownerValue == 2 ? 4UL : 2UL);
        var links = new List<WorkshopConnection>
        {
            new(new(1), PortId.Supply, owner, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(owner, new("fast_out"), owner, new(setPort), WorkshopConnectionDomain.Activation)
        };
        if (resetConnected) links.Add(new(owner, new("slow_out"), owner, new(resetPort), WorkshopConnectionDomain.Activation));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Battery(new(1), -4, 4, 0),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Part, new(2), 0, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Part, new(4), 4, 4, 0, 0, 0, 0, 1)), Connections: new(links.ToArray()));
        ActivationNodeId Id(GpuBodyId selectedOwner, string local) => bench.Instances.Identities[selectedOwner].Signals[new(local)].Node;
        var selected = Id(owner, memory);
        Assert.NotEqual(selected, Id(other, memory));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        var movingBody = composition == IndependentSignalCompositionTests.Composition.Jointed
            ? bench.Instances.Identities[owner].Hinges[0].Body : owner;
        var hasInitialBody = harness.Committed.Bodies.TryGet(movingBody, out var initialBody);
        if (composition != IndependentSignalCompositionTests.Composition.Bodyless) Assert.True(hasInitialBody);
        await harness.Run(); await harness.Advance(12);
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, selected).Phase);
        await harness.Advance(12); // Fast Set and slow Reset coincide; Reset must dominate only at its exact bound memory.
        Assert.Equal(resetConnected ? ActivationPhase.Clear : ActivationPhase.Latched, Latch(harness.Committed, selected).Phase);
        await harness.Advance(12);
        Assert.Equal(ActivationPhase.Latched, Latch(harness.Committed, selected).Phase);
        foreach (var quietOwner in new[] { owner, other })
            foreach (var local in new[] { "memory_a", "memory_b" })
                if (Id(quietOwner, local) != selected)
                    Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Id(quietOwner, local)).Phase);
        var events = harness.Of(SimulationEventKind.MemoryChanged).ToArray();
        Assert.All(events, row => Assert.Equal(new GpuEventSourceId(selected.Value), row.Source));
        Assert.Equal(resetConnected ? new uint[] { 1, 0, 1 } : [1u], events.Select(row => row.Level));
        if (composition != IndependentSignalCompositionTests.Composition.Bodyless)
        {
            await harness.Advance(24); // Keep the established half-second motion observation, after the signal assertions.
            Assert.True(harness.Committed.Bodies.TryGet(movingBody, out var moved));
            // Reuse the half-second 5 cm / 0.03 rad meaningful-motion criteria: they exclude a frozen owner
            // without constraining its incidental trajectory or arrival time.
            if (composition == IndependentSignalCompositionTests.Composition.Dynamic)
                Assert.True((initialBody.Body.Cell.Y + initialBody.Body.Local.Y - moved.Body.Cell.Y - moved.Body.Local.Y) / 16f > .05f);
            else
            {
                var before = new Quaternion(initialBody.Rotation.X, initialBody.Rotation.Y, initialBody.Rotation.Z, initialBody.Rotation.W);
                var after = new Quaternion(moved.Rotation.X, moved.Rotation.Y, moved.Rotation.Z, moved.Rotation.W);
                Assert.True(2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(before, after)), 0, 1)) > .03f);
            }
        }
        await harness.Reset();
        Assert.Equal(bench.Instances, harness.Construction.Instances);
        foreach (var quietOwner in new[] { owner, other })
            foreach (var local in new[] { "memory_a", "memory_b" })
                Assert.Equal(ActivationPhase.Clear, Latch(harness.Committed, Id(quietOwner, local)).Phase);
    }

    [Fact]
    public async Task InternalAndPublicEdgesCannotCloseAnInstantaneousCycle()
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry();
        entry = entry with
        {
            Signal = new([new SignalPulseCountDescriptor(new("a"), new("target")), new SignalPulseCountDescriptor(new("b"), new("target"))],
                [Edge("a", "b")], [new(new("a_in"), Endpoint("a", SignalTerminal.Input)), new(new("b_out"), Endpoint("b", SignalTerminal.Output))]),
            Ports = [.. entry.Ports,
                new(new("a_in"), SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY),
                new(new("b_out"), SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitY)],
            Events = [], Presentation = entry.Presentation with { Bindings = [] }
        };
        Assert.Empty(BundleValidator.Validate([new("open_graph", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        var before = harness.Committed;
        Assert.Equal(bench, WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Append(Part).ToHashSet(), SaveFixture.Forged(bench, 5)).Construction);
        var closing = new WorkshopConnection(new(2), new("b_out"), new(2), new("a_in"), WorkshopConnectionDomain.Activation);
        var invalid = bench with { Revision = new(3), Connections = bench.Connections.With(closing) };
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => invalid.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => harness.Command(WorkshopCommandKind.Construct, invalid));
        SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection,
            () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Append(Part).ToHashSet(), SaveFixture.Forged(invalid, 5)));
        var network = harness.Simulation.Scene.Activation;
        var refusal = Assert.Throws<SignalTopologyException>(() => new ActivationNetwork(network.Nodes,
            [.. network.Edges, new(Node(bench, "b"), Node(bench, "a"), PortRole.ActivationIn)]));
        Assert.Equal(SignalTopologyRejection.InstantaneousCycle, refusal.Reason);
        Assert.Equal(before.Revision, harness.Committed.Revision);
        Assert.Equal(before.Epoch, harness.Committed.Epoch);
        Assert.Equal(before.Tick, harness.Committed.Tick);
        Assert.Equal(bench, harness.Construction);
        await harness.Run(); await harness.Advance(1);
        Assert.Equal(0u, network.Memory(harness.Committed.Memory, Node(bench, "a")).Count);
        Assert.Equal(0u, network.Memory(harness.Committed.Memory, Node(bench, "b")).Count);
    }


    [Theory]
    [InlineData(false, false, IndependentSignalCompositionTests.Composition.Dynamic)]
    [InlineData(false, true, IndependentSignalCompositionTests.Composition.Jointed)]
    [InlineData(true, false, IndependentSignalCompositionTests.Composition.Dynamic)]
    [InlineData(true, false, IndependentSignalCompositionTests.Composition.Jointed)]
    [InlineData(true, true, IndependentSignalCompositionTests.Composition.Dynamic)]
    [InlineData(true, true, IndependentSignalCompositionTests.Composition.Jointed)]
    public async Task EachOscillatorUsesItsExactInputInsteadOfItsOwnersOtherSupply(bool secondConnected, bool reverse, IndependentSignalCompositionTests.Composition composition)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        PortId secondInput = new("other_power");
        var entry = Entry(reverse, connected: false, cosmetics: false);
        var power = Assert.Single(entry.Ports);
        entry = entry with
        {
            Ports = reverse ? [power with { Id = secondInput }, power] : [power, power with { Id = secondInput }],
            Signal = entry.Signal! with { Nodes = entry.Signal!.Nodes.Select(node =>
                node is SignalOscillatorDescriptor oscillator && node.Id == new SignalNodeId("slow")
                    ? oscillator with { SupplyInput = secondInput } : node).ToArray() },
            Electrical = entry.Electrical! with
            {
                SupplyGraph = new(
                    [.. entry.Electrical!.SupplyGraph!.Nodes, new SupplyConsumerDescriptor(new("slow_consumer"), [])],
                    [],
                    [.. entry.Electrical!.SupplyGraph!.Ports, new(secondInput, new(new("slow_consumer"), SupplyTerminal.PowerIn))])
            }
        };
        entry = IndependentSignalCompositionTests.Compose(entry, composition);
        var collider = Assert.Single(entry.Colliders);
        entry = entry with { Colliders = reverse
            ? [collider with { Offset = new(.5f, 0, 0) }, collider]
            : [collider, collider with { Offset = new(.5f, 0, 0) }] };
        Assert.Empty(BundleValidator.Validate([new("separate_inputs", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench() with { Instances = new(Bench().Instances.Append(
            PlacedParts.Battery(new(3), 4, 4, 0, enabled: ElectricalEnable.Disabled)).ToArray()) };
        var secondWire = new WorkshopConnection(new(3), PortId.Supply, new(2), secondInput, WorkshopConnectionDomain.Electrical);
        if (secondConnected) bench = bench with { Connections = bench.Connections.With(secondWire) };
        var saved = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements.Append(Part).ToHashSet(),
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(bench, new(4))));
        Assert.Equal(bench, saved.Construction);
        await harness.Require(WorkshopCommandKind.Construct, saved.Construction);
        var body = composition == IndependentSignalCompositionTests.Composition.Jointed
            ? bench.Instances.Identities[new(2)].Hinges[0].Body : new GpuBodyId(2);
        Assert.True(harness.Committed.Bodies.TryGet(body, out var initial));
        await harness.Run(); await harness.Advance(24);
        ActivationMemory Memory(string local) => harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, local));
        Assert.Equal(2u, Memory("fast").Count);
        Assert.Equal(0u, Memory("slow").Count);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, new(3), RunControlValue.Of(ElectricalEnable.Enabled)));
        await harness.Advance(25);
        Assert.Equal(4u, Memory("fast").Count);
        Assert.Equal(secondConnected ? 1u : 0u, Memory("slow").Count);
        Assert.Equal(secondConnected ? OscillatorPhase.Running : OscillatorPhase.Stopped, Memory("slow").Phase);
        Assert.True(harness.Committed.Bodies.TryGet(body, out var moved));
        // The controller must advance on a genuinely moving owner: 5 cm translation or 0.03 rad
        // rotation over this 49-tick window excludes a frozen physical owner without a trajectory golden.
        if (composition == IndependentSignalCompositionTests.Composition.Dynamic)
            Assert.True((initial.Body.Cell.Y + initial.Body.Local.Y - moved.Body.Cell.Y - moved.Body.Local.Y) / 16f > .05f);
        else
        {
            var before = new Quaternion(initial.Rotation.X, initial.Rotation.Y, initial.Rotation.Z, initial.Rotation.W);
            var after = new Quaternion(moved.Rotation.X, moved.Rotation.Y, moved.Rotation.Z, moved.Rotation.W);
            Assert.True(2 * MathF.Acos(Math.Clamp(MathF.Abs(Quaternion.Dot(before, after)), 0, 1)) > .03f);
        }
        var slowEvents = harness.Of(SimulationEventKind.TriggerActivated)
            .Where(record => record.Source == new GpuEventSourceId(Node(bench, "slow").Value)).ToArray();
        if (secondConnected)
        {
            Assert.Single(slowEvents);
            var selected = Latch(harness.Committed, Node(bench, "slow"));
            Assert.Equal(ActivationCauseKind.Node, selected.CauseKind);
            Assert.Equal(new GpuEventSourceId(Node(bench, "slow").Value), selected.Source);
            Assert.Equal(new GpuBodyId(2), selected.Body);
            Assert.Equal(default, selected.Collider);
            Assert.Equal(0f, selected.ApproachSpeed.Value);
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, new(3), RunControlValue.Of(ElectricalEnable.Disabled)));
            await harness.Advance(1);
            Assert.Equal(OscillatorPhase.Stopped, Memory("slow").Phase);
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, new(3), RunControlValue.Of(ElectricalEnable.Enabled)));
            await harness.Advance(1);
            Assert.Equal(harness.Committed.Tick.Value + 24, Memory("slow").DueTick);
            await harness.Advance(23);
            Assert.Equal(1u, Memory("slow").Count);
            await harness.Advance(1);
            Assert.Equal(2u, Memory("slow").Count);
        }
        else Assert.Empty(slowEvents);
        var duplicate = bench with { Connections = bench.Connections.With(
            new WorkshopConnection(new(1), PortId.Supply, new(2), secondInput, WorkshopConnectionDomain.Electrical)) };
        if (secondConnected) Assert.Throws<WorkshopEditRejectedException>(() => duplicate.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        await harness.Reset();
        Assert.Equal(0u, Memory("fast").Count); Assert.Equal(0u, Memory("slow").Count);
        Assert.Equal(OscillatorPhase.Stopped, Memory("slow").Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EffectiveContactAndGatePowerStopBothOscillatorsWithTheBatteryStillEnabled(bool throughGate)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(connected: false, cosmetics: false);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        static WorkshopConnection Power(ulong source, ulong target, PortId? input = null) =>
            new(new(source), PortId.Supply, new(target), input ?? PortId.PowerIn, WorkshopConnectionDomain.Electrical);
        static WorkshopConnection Pulse(ulong source, PortId input) =>
            new(new(source), PortId.ActivationOut, new(3), input, WorkshopConnectionDomain.Activation);
        var instances = new List<IWorkshopInstance>(Bench().Instances)
        {
            PlacedParts.Latch(new(3), 7, 4, 0, 0, 0, 0, 1),
            PlacedParts.Clock(new(4), 10, 4, 0, 0, 0, 0, 1, .1f),
            PlacedParts.Battery(new(5), 13, 4, 0, enabled: ElectricalEnable.Disabled),
            PlacedParts.Clock(new(6), 10, 7, 0, 0, 0, 0, 1, .1f),
            PlacedParts.Battery(new(7), 13, 7, 0, enabled: ElectricalEnable.Disabled)
        };
        var wires = new List<WorkshopConnection> { Power(1, 3), Power(5, 4), Power(7, 6), Pulse(4, PortId.SetIn), Pulse(6, PortId.ResetIn) };
        if (throughGate)
        {
            instances.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("both_gate"), new(8), 16, 4, 0, 0, 0, 0, 1));
            instances.Add(PlacedParts.Battery(new(9), 19, 4, 0));
            wires.AddRange([Power(1, 8), Power(3, 8, PortId.FirstIn), Power(9, 8, PortId.SecondIn), Power(8, 2)]);
        }
        else wires.Add(Power(3, 2));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(instances.ToArray()), Connections: new(wires.ToArray()));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(25);
        ActivationMemory Memory(string local) => harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, local));
        var contactNode = harness.Simulation.Scene.Activation.Contacts.Single(c => c.Owner.Value == 3).Contact;
        bool Closed() => harness.Simulation.Scene.Activation.ContactClosed(harness.Committed.Activations, harness.Committed.Timers, contactNode);
        async Task Switch(bool closed)
        {
            var battery = new GpuBodyId(closed ? 5UL : 7UL);
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, battery, RunControlValue.Of(ElectricalEnable.Enabled)));
            for (var ticks = 0; Closed() != closed && ticks < 26; ticks++) await harness.Advance(1);
            Assert.Equal(closed, Closed());
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: WorkshopIntegrationHarness.RunControl(harness.Simulation.Scene, battery, RunControlValue.Of(ElectricalEnable.Disabled)));
            await harness.Advance(1); // New physical/discrete contact state routes at the next tick.
            Assert.True(Enumerable.Range(0, harness.Committed.Electrical.Count).Select(i => harness.Committed.Electrical[i])
                .Single(source => source.Owner == new GpuBodyId(1)).Available);
        }
        Assert.Equal(OscillatorPhase.Stopped, Memory("fast").Phase);
        Assert.Equal(OscillatorPhase.Stopped, Memory("slow").Phase);
        await Switch(true);
        Assert.Equal(harness.Committed.Tick.Value + 12, Memory("fast").DueTick);
        Assert.Equal(harness.Committed.Tick.Value + 24, Memory("slow").DueTick);
        await harness.Advance(24);
        Assert.Equal(2u, Memory("fast").Count); Assert.Equal(1u, Memory("slow").Count);
        await Switch(false);
        var fast = Memory("fast").Count; var slow = Memory("slow").Count;
        await harness.Advance(30);
        Assert.Equal(fast, Memory("fast").Count); Assert.Equal(slow, Memory("slow").Count);
        Assert.Equal(OscillatorPhase.Stopped, Memory("fast").Phase);
        Assert.Equal(OscillatorPhase.Stopped, Memory("slow").Phase);
        await Switch(true);
        Assert.Equal(harness.Committed.Tick.Value + 12, Memory("fast").DueTick);
        Assert.Equal(harness.Committed.Tick.Value + 24, Memory("slow").DueTick);
        await harness.Advance(11);
        Assert.Equal(fast, Memory("fast").Count); Assert.Equal(slow, Memory("slow").Count);
        await harness.Advance(13);
        Assert.Equal(fast + 2, Memory("fast").Count); Assert.Equal(slow + 1, Memory("slow").Count);
        await harness.Reset();
        Assert.False(Closed());
        Assert.Equal(0u, Memory("fast").Count); Assert.Equal(0u, Memory("slow").Count);
    }

    [Theory]
    [InlineData("delay", false)]
    [InlineData("quiet", true)]
    public async Task SameKindEventCuesSelectTheirExactSiblingAndReset(string selected, bool reverse)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(reverse, cosmetics: false);
        PresentationBinding Cue(string target, string node, float from, float to) =>
            new(target, PresentationProperty.LocalX, PresentationDriver.Event, PresentationValueMapping.Identity,
                from, to, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 0, PresentationTimerPhase.None,
                Event: SimulationEventKind.TriggerActivated, SignalNode: new(node));
        PresentationBinding[] cues = [Cue("FirstCue", "delay", -2, 7), Cue("SecondCue", "quiet", 3, 11)];
        entry = entry with { Signal = entry.Signal! with { Edges = [Edge("fast", selected)] },
            Presentation = entry.Presentation with { Bindings = reverse ? cues.Reverse().ToArray() : cues } };
        Assert.Empty(BundleValidator.Validate([new("event_cues", entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench();
        await harness.Require(WorkshopCommandKind.Construct, bench);
        await harness.Run(); await harness.Advance(48);
        Assert.Single(harness.Of(SimulationEventKind.TriggerActivated), row => row.Source == new GpuEventSourceId(Node(bench, selected).Value));
        void AssertCues(bool active)
        {
            // Harness delivery calls the production AcceptEvents before accepting each committed read.
            var samples = harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4));
            for (var i = 0; i < entry.Presentation.Bindings.Count; i++)
            {
                var binding = entry.Presentation.Bindings[i];
                var value = BitConverter.UInt32BitsToSingle(Assert.Single(samples,
                    sample => sample.Owner == new GpuBodyId(2) && sample.Binding == i).Value.CanonicalBits);
                Assert.Equal(active && binding.SignalNode == new SignalNodeId(selected) ? binding.To : binding.From, value);
            }
        }
        AssertCues(true);
        await harness.Reset();
        AssertCues(false);
        await harness.Run(); await harness.Advance(48);
        AssertCues(true);
    }

    [Fact]
    public void ActuatorStateMapsSerializeOnlyAuthoredValuesAndRejectDerivedFlags()
    {
        var entry = TestBundle.Entry(new("powered_gate"));
        var json = BundleSchema.WriteEntries([entry]);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;
        // Inspect semantic fields, not serialized byte identity.
        var map = root.GetProperty("Elements")[0].GetProperty("Presentation").GetProperty("Bindings")[0].GetProperty("StateValues");
        Assert.Equal(["Blocked", "Powered", "Unpowered"], map.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(85f / 255f, map.GetProperty("Unpowered").GetSingle());
        Assert.Equal(247f / 255f, map.GetProperty("Powered").GetSingle());
        Assert.Equal(232f / 255f, map.GetProperty("Blocked").GetSingle());
        Assert.Equal(entry.Presentation.Bindings[0].StateValues,
            BundleSchema.Read(json).Elements[0].Presentation.Bindings[0].StateValues);
        var malformed = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        malformed["Elements"]![0]!["Presentation"]!["Bindings"]![0]!["StateValues"]!["IsFinite"] = true;
        Assert.Throws<System.Text.Json.JsonException>(() => BundleSchema.Read(malformed.ToJsonString()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothOscillatorsUsePostPhysicsAvailabilityWhenTheirEnabledSourceDepletes(bool enoughEnergy)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(connected: false, cosmetics: false);
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Part, entry.Ports)]));
        var bench = Bench() with { Instances = new(
            PlacedParts.Battery(new(1), -4, 4, 0, capacity: enoughEnergy ? 3600 : 60, initialCharge: enoughEnergy ? 1 : .001f),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, Part, new(2), 0, 4, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(3), 4, 3, 0, 20),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("bowling"), new(4), 4, 6, 0, 0, 0, 0, 1)) };
        bench = bench with { Connections = bench.Connections.With(
            new WorkshopConnection(new(1), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical)) };
        await harness.Require(WorkshopCommandKind.Construct, bench);
        var initial = harness.Committed.Electrical[0].Remaining;
        Assert.True(initial.Value > 0);
        await harness.Run();
        var exhausted = false;
        for (var tick = 0; tick < 240; tick++) // Two-second stall guard: payload spends stored work, then recharge exhausts the small source.
        {
            await harness.Advance(1);
            var source = harness.Committed.Electrical[0];
            Assert.Equal(ElectricalEnable.Enabled, source.Enabled);
            if (source.Remaining.Value != 0) continue;
            exhausted = true;
            Assert.True(source.Debit.Value > 0); // The observed boundary is the actual depletion tick.
            foreach (var local in new[] { "fast", "slow" })
                Assert.Equal(OscillatorPhase.Stopped,
                    harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, local)).Phase);
            break;
        }
        Assert.Equal(!enoughEnergy, exhausted);
        var before = harness.Of(SimulationEventKind.TriggerActivated).Count();
        await harness.Advance(48);
        if (enoughEnergy) Assert.True(harness.Of(SimulationEventKind.TriggerActivated).Count() > before);
        else Assert.Equal(before, harness.Of(SimulationEventKind.TriggerActivated).Count());
        await harness.Reset();
        Assert.Equal(initial, harness.Committed.Electrical[0].Remaining);
        foreach (var local in new[] { "fast", "slow" })
            Assert.Equal(OscillatorPhase.Stopped,
                harness.Simulation.Scene.Activation.Memory(harness.Committed.Memory, Node(bench, local)).Phase);
    }
}
