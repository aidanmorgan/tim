using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class DeclaredPresentationTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GateCueUsesActualCommittedDriveAndAppliesNativeMaterial(bool connected)
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(gate, battery))
        {
            Connections = connected
                ? new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical))
                : new()
        };
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 2);
        var read = reads[1];
        var drive = Assert.Single(read.Presentation.Actuators.ToArray());
        Assert.Equal(connected, drive.SupplyClosed);
        Assert.Equal(gate.Id, drive.Owner);
        var endpoint = Response(read, 1);
        var roundTrip = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint with { Construction = construction }));
        Assert.True(read.Presentation.HasSameBits(roundTrip.Read.Presentation));
        Assert.Equal(construction, roundTrip.Construction);
        var foreign = read with { Presentation = new([drive with { Owner = new(999) }], []) };
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(Response(foreign, 2) with { Construction = construction }));
        var evaluator = new WorkshopDeclaredPresentation(construction, endpoint);
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(PartId.PoweredGate);
        var controlPart = registry.Create(PartId.PoweredGate);
        try
        {
            godot.Tree.Root.AddChild(part);
            godot.Tree.Root.AddChild(controlPart);
            var controlIndicator = controlPart.GetNode<MeshInstance3D>("Visual/Visual_Indicator");
            var controlMaterial = Assert.IsType<StandardMaterial3D>(controlIndicator.MaterialOverride);
            var authoredMaterial = Assert.IsType<StandardMaterial3D>(controlIndicator.Mesh.SurfaceGetMaterial(0));
            var authoredColour = authoredMaterial.AlbedoColor;
            foreach (var sample in evaluator.Advance(read.Capture!.Value.Time.Value / 1_000_000_000d))
                if (sample.Owner == gate.Id) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
            var indicator = part.GetNode<MeshInstance3D>("Visual/Visual_Indicator");
            var material = Assert.IsType<StandardMaterial3D>(indicator.MaterialOverride);
            Assert.NotSame(controlMaterial, material);
            Assert.NotSame(authoredMaterial, material);
            Assert.Equal(new Color("#556573"), controlMaterial.AlbedoColor);
            Assert.Equal(authoredColour, authoredMaterial.AlbedoColor);
            Assert.Equal(authoredMaterial.EmissionEnabled, material.EmissionEnabled);
            Assert.Equal(new Color(drive.Blocked ? "#e8b764" : connected ? "#f7cb52" : "#556573"), material.AlbedoColor);
            evaluator.Accept(Response(reads[2], 2));
            Assert.Throws<ArgumentException>(() => evaluator.Accept(endpoint));
            var next = Response(reads[2], 3);
            Assert.Throws<ArgumentException>(() => evaluator.Accept(next with { Read = reads[2] with { Revision = read.Revision } }));
            Assert.Throws<ArgumentException>(() => evaluator.Accept(next with { Session = new(1, 2) }));
            Assert.Throws<ArgumentException>(() => evaluator.Accept(next with { MasterGeneration = new(2) }));
            Assert.Throws<ArgumentException>(() => evaluator.Accept(next with { Read = reads[2] with { Epoch = new(99) } }));
            Assert.Throws<ArgumentException>(() => evaluator.Accept(Response(reads[2], 3) with { Projection = new(2) }));
        }
        finally { part.Free(); controlPart.Free(); }
    }

    /// <summary>C-Gate (CAT-051 fact 14, Q3): a blade held at rest on a ball shows the ochre Blocked cue from its committed read, over
    /// the wire; the same gate before the ball holds it shows the slate unpowered cue.</summary>
    [Fact]
    public async Task TheGateCueIsOchreWhileABallHoldsTheBladeAtRest()
    {
        var construction = LatchSupplyFixtureTests.Machine(9, payload: true);
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 360);
        var gate = construction.Instances.Single(instance => instance.PartId == PartId.PoweredGate).Id;
        Color Cue(WorkshopRead read)
        {
            var endpoint = Response(read, 1);
            var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint with { Construction = construction }));
            var evaluator = new WorkshopDeclaredPresentation(construction, decoded with { Construction = null });
            var registry = new PartRegistry(); registry.Load();
            var part = registry.Create(PartId.PoweredGate);
            try
            {
                godot.Tree.Root.AddChild(part);
                foreach (var sample in evaluator.Advance(read.Capture!.Value.Time.Value / 1_000_000_000d))
                    if (sample.Owner == gate) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
                return Assert.IsType<StandardMaterial3D>(part.GetNode<MeshInstance3D>("Visual/Visual_Indicator").MaterialOverride).AlbedoColor;
            }
            finally { part.Free(); }
        }
        var held = reads[^1].Presentation.Actuators.ToArray().Single(row => row.Owner == gate);
        Assert.True(held.Blocked && held.MovingContact && !held.SupplyClosed, $"guard: the ball holds the unpowered blade at rest, {held}");
        Assert.Equal(new Color("#e8b764"), Cue(reads[^1]));
        Assert.False(reads[0].Presentation.Actuators.ToArray().Single(row => row.Owner == gate).Blocked);
        Assert.Equal(new Color("#556573"), Cue(reads[0]));
    }

    [Fact]
    public void SinglePrecisionEndpointSurvivesProductionBatchAndControlCodec()
    {
        const float endpoint = .42000002f;
        Assert.NotEqual(endpoint, (float)(Half)endpoint);
        var batch = new AnimationBatch(1);
        var handle = batch.Register(new(new(1), AnimationProperty.LocalRotationAngle),
            new(new(new AnimationRadians(-endpoint)), new(new AnimationRadians(endpoint)), new(.125f),
                AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
        batch.Start(handle); batch.Advance(1, 0, 0); batch.Advance(2, .125, 0);
        Assert.Equal(BitConverter.SingleToUInt32Bits(endpoint), batch.Read(handle).Value.CanonicalBits);
        var control = new WorkshopAnimationControl(new(WorkshopDeclaredPresentation.TargetBase), new(1), 1, 1,
            AnimationControlKind.Endpoint, true, endpoint, endpoint, .125f, AnimationCurve.Linear,
            Property: AnimationProperty.LocalRotationAngle);
        var bytes = WorkshopAnimationWire.Control(new(1, 2), new(1), new(1), control);
        Assert.Equal(control, WorkshopAnimationWire.ReadControl(bytes, new(1, 2), new(1), new(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopAnimationWire.Value(AnimationProperty.LocalRotationAngle, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopAnimationWire.Value(AnimationProperty.UniformScale, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopAnimationWire.Value(AnimationProperty.ColourRed, 1.01f));
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ClockTicksDrivePendulumPulseAndCounterLamps(bool powered)
    {
        var battery = PlacedParts.Battery(new(1), -3, 3, 0, enabled: powered ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var clock = PlacedParts.Clock(new(2), 0, 3, 0, 0, 0, 0, 1, 1f);
        var counter = WorkshopInput.Counter(new(3), 3, 3, 0, 0, 0, 0, 1, 3);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(battery, clock, counter),
            Connections: new(
                new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 150);
        var evaluator = new WorkshopDeclaredPresentation(construction, Response(reads[1], 1));
        var network = WorkshopActivationCompiler.Compile(construction);
        var registry = new PartRegistry(); registry.Load();
        var clockPart = registry.Create(PartId.Clock); var counterPart = registry.Create(PartId.Counter);
        try
        {
            godot.Tree.Root.AddChild(clockPart); godot.Tree.Root.AddChild(counterPart);
            var pivot = clockPart.GetNode<Node3D>("Visual/PendulumPivot");
            Apply(evaluator.Advance(Time(reads[1])));
            Assert.Equal(0f, pivot.Rotation.Z); // installation at a nonzero master origin does not finish the follow
            var maximumSwing = 0f; var pulseSeen = false;
            for (var i = 2; i < reads.Count; i++)
            {
                evaluator.Accept(Response(reads[i], (ulong)i));
                Apply(evaluator.Advance(Time(reads[i])));
                maximumSwing = MathF.Max(maximumSwing, MathF.Abs(pivot.Rotation.Z));
                pulseSeen |= Material(clockPart, "Visual/Visual_Indicator").AlbedoColor.R > .9f;
            }
            Assert.Equal(powered ? 1u : 0u, network.Memory(reads[^1].Memory, counter.Id).Count);
            Assert.Equal(powered, pulseSeen);
            if (powered) Assert.InRange(maximumSwing, .35f, .42f);
            else Assert.Equal(0f, maximumSwing);
            Assert.Equal(new Color(powered ? "#f7cb52" : "#556573"), Material(counterPart, "Visual/DotLamp_0").AlbedoColor);
            Assert.Equal(new Color("#556573"), Material(counterPart, "Visual/DotLamp_1").AlbedoColor);
            // Nanosecond capture stamps truncate each tick; settle beyond the exact .25 second duration.
            Apply(evaluator.Advance(Time(reads[^1]) + .001));
            Assert.Equal(new Color("#556573"), Material(clockPart, "Visual/Visual_Indicator").AlbedoColor);
            void Apply(IReadOnlyList<DeclaredPresentationSample> samples)
            {
                foreach (var sample in samples)
                    if (sample.Owner == clock.Id) clockPart.ApplyDeclaredProperty(sample.Binding, sample.Value);
                    else if (sample.Owner == counter.Id) counterPart.ApplyDeclaredProperty(sample.Binding, sample.Value);
            }
        }
        finally { clockPart.Free(); counterPart.Free(); }
    }

    [Theory]
    [InlineData(false, .5f, ContactLoadPhase.Empty)]
    [InlineData(true, 2f, ContactLoadPhase.Underweight)]
    [InlineData(true, .5f, ContactLoadPhase.Loaded)]
    public async Task PlateNativeColourFollowsActualContactLoad(bool occupied, float minimum, ContactLoadPhase expected)
    {
        var y = (double)WorkshopConstruction.WorkbenchSurface.Value + .22;
        var plate = PlacedParts.Plate(new(1), 0, y, 0, minimum);
        var ball = WorkshopInput.Generic(PartId.Basketball,new(2), 0, y + .08 + .35, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            occupied ? new(plate, ball) : new(plate));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 120);
        Assert.Equal(expected, Assert.Single(reads[^1].Presentation.Loads.ToArray()).Phase);
        var evaluator = new WorkshopDeclaredPresentation(construction, Response(reads[1], 1));
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(PartId.PressurePlate);
        try
        {
            godot.Tree.Root.AddChild(part);
            for (var i = 1; i < reads.Count; i++)
            {
                if (i > 1) evaluator.Accept(Response(reads[i], (ulong)i));
                foreach (var sample in evaluator.Advance(Time(reads[i])))
                    if (sample.Owner == plate.Id) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
            }
            var colour = new Color(expected switch
            {
                ContactLoadPhase.Empty => "#556573",
                ContactLoadPhase.Underweight => "#e8b764",
                ContactLoadPhase.Loaded => "#f7cb52",
                _ => throw new ArgumentOutOfRangeException(nameof(expected))
            });
            for (var stud = 0; stud < 4; stud++)
                Assert.Equal(colour, Material(part, $"Visual/Stud_{stud}").AlbedoColor);
        }
        finally { part.Free(); }
    }


    [Fact]
    public async Task CommittedClockAndCounterUseExactDeclaredTimesAtNonzeroOrigin()
    {
        var battery = PlacedParts.Battery(new(1), -3, 3, 0);
        var clock = PlacedParts.Clock(new(2), 0, 3, 0, 0, 0, 0, 1, 1f);
        var counter = WorkshopInput.Counter(new(3), 3, 3, 0, 0, 0, 0, 1, 3);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(battery, clock, counter),
            Connections: new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(clock.Id, WorkshopSocket.ActivationOut, counter.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 120);
        var follow = new WorkshopDeclaredPresentation(construction, Response(reads[30], 1));
        var start = Time(reads[30]);
        Assert.Equal(0f, Scalar(follow.Advance(start), clock.Id, 0));
        Assert.Equal((float)(.42f * (1 - Math.Exp(-1))), Scalar(follow.Advance(start + 1d / 30), clock.Id, 0));
        var pulse = new WorkshopDeclaredPresentation(construction, Response(reads[120], 1));
        start = Time(reads[120]);
        Assert.Equal(247f / 255f, Scalar(pulse.Advance(start), clock.Id, 1));
        var atQuarterCounter = pulse.Advance(start + (double).1f / 4);
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .103515625f), Scalar(atQuarterCounter, counter.Id, 0));
        var atHalfCounter = pulse.Advance(start + (double).1f / 2);
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .5f), Scalar(atHalfCounter, counter.Id, 0));
        Assert.Equal(247f / 255f, Scalar(pulse.Advance(start + (double).1f), counter.Id, 0));
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .5f), Scalar(pulse.Advance(start + .125), clock.Id, 1));
        Assert.Equal(85f / 255f, Scalar(pulse.Advance(start + .25), clock.Id, 1));

        var saved = new WorkshopSavedConstruction(construction, new(4));
        var restored = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        var replay = await WorkshopPhysicsEngineTests.Run(restored.Construction, 120);
        var replayPresentation = new WorkshopDeclaredPresentation(restored.Construction, Response(replay[120], 1));
        Assert.Equal(247f / 255f, Scalar(replayPresentation.Advance(Time(replay[120]) + .1), counter.Id, 0));
    }

    [Fact]
    public async Task PlateCubicRateEightAndResetUseCommittedSimulation()
    {
        var y = (double)WorkshopConstruction.WorkbenchSurface.Value + .22;
        var plate = PlacedParts.Plate(new(1), 0, y, 0, .5f);
        var ball = WorkshopInput.Generic(PartId.Basketball,new(2), 0, y + .43, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(plate, ball));
        var (_, reads, reset) = await PressurePlateTests.RunAndReset(construction, 120);
        var loaded = reads.Last(read => read.Presentation.Loads[0].Phase == ContactLoadPhase.Loaded);
        var presentation = new WorkshopDeclaredPresentation(construction, Response(loaded, 1));
        var start = Time(loaded);
        Assert.Equal(85f / 255f, Scalar(presentation.Advance(start), plate.Id, 0));
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .15625f),
            Scalar(presentation.Advance(start + .03125), plate.Id, 0));
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .5f),
            Scalar(presentation.Advance(start + .0625), plate.Id, 0));
        Assert.Equal(247f / 255f, Scalar(presentation.Advance(start + .125), plate.Id, 0));
        var resetPresentation = new WorkshopDeclaredPresentation(construction, Response(reset, 2) with { Projection = new(2) });
        Assert.Equal(85f / 255f, Scalar(resetPresentation.Advance(Time(reset)), plate.Id, 0));
        Assert.Throws<ArgumentException>(() => resetPresentation.Accept(Response(loaded, 3) with { Projection = new(2) }));
        Assert.Equal(85f / 255f, Scalar(resetPresentation.Advance(Time(reset) + 1), plate.Id, 0));
    }

    private static float Scalar(IReadOnlyList<DeclaredPresentationSample> samples, GpuBodyId owner, int binding) =>
        BitConverter.UInt32BitsToSingle(Assert.Single(samples, s => s.Owner == owner && s.Binding == binding).Value.CanonicalBits);

    [Fact]
    public async Task CommittedClockRetainsHiddenAndSkippedFirstFrameAndResetCancelsIt()
    {
        var battery = PlacedParts.Battery(new(1), -3, 3, 0);
        var clock = PlacedParts.Clock(new(2), 0, 3, 0, 0, 0, 0, 1, .125f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(battery, clock),
            Connections: new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 30);
        var pulseRead = reads[15];
        var start = Time(pulseRead);
        var skipped = new WorkshopDeclaredPresentation(construction, Response(pulseRead, 1));
        Assert.Equal(247f / 255f, Scalar(skipped.Advance(start + 1), clock.Id, 1));
        Assert.Equal(85f / 255f, Scalar(skipped.Advance(start + 1.01), clock.Id, 1));
        var hidden = new WorkshopDeclaredPresentation(construction, Response(reads[1], 1));
        var target = WorkshopDeclaredPresentation.Targets(construction).Single(t => t.Owner == clock.Id && t.Binding == 1).Target;
        hidden.SetVisible(target, AnimationProperty.ColourRed, false);
        hidden.Accept(Response(pulseRead, 2));
        Assert.DoesNotContain(hidden.Advance(start + 1), sample => sample.Owner == clock.Id);
        hidden.SetVisible(target, AnimationProperty.ColourRed, true);
        Assert.Equal(247f / 255f, Scalar(hidden.Advance(start + 1.01), clock.Id, 1));
        Assert.Equal(85f / 255f, Scalar(hidden.Advance(start + 1.02), clock.Id, 1));

        var overlap = new WorkshopDeclaredPresentation(construction, Response(pulseRead, 1));
        overlap.Advance(start);
        overlap.Accept(Response(reads[30], 2));
        Assert.Equal(247f / 255f, Scalar(overlap.Advance(Time(reads[30])), clock.Id, 1));
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .5f),
            Scalar(overlap.Advance(Time(reads[30]) + .125), clock.Id, 1));

        var reset = new WorkshopDeclaredPresentation(construction, Response(reads[0], 1) with { Projection = new(2) });
        Assert.Equal(85f / 255f, Scalar(reset.Advance(start + 2), clock.Id, 1));
        Assert.Throws<ArgumentException>(() => reset.Accept(Response(pulseRead, 2)));
        Assert.Throws<ArgumentException>(() => hidden.SetVisible(target, AnimationProperty.ColourBlue, false));
    }

    [Fact]
    public async Task CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases()
    {
        var setClock = PlacedParts.Clock(new(1), -3, 0, 0, 0, 0, 0, 1, 1f);
        var resetClock = PlacedParts.Clock(new(2), -3, 3, 0, 0, 0, 0, 1, 1.5f);
        var latch = WorkshopInput.Latch(new(3), 0, 0, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(4), -6, 0, 0);
        var lamp = PlacedParts.Lamp(new(5), 3, 0, 0, 0, 0, 0, 1);
        var delay = WorkshopInput.Generic(PartId.Delay, new(6), 3, 3, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new ParameterId("delay_seconds"), (float).5f)]));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(setClock, resetClock, latch, battery, lamp, delay),
            Connections: new(
                new(battery.Id, WorkshopSocket.Supply, setClock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new(battery.Id, WorkshopSocket.Supply, resetClock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
                new(setClock.Id, WorkshopSocket.ActivationOut, latch.Id, WorkshopSocket.SetIn, WorkshopConnectionDomain.Activation),
                new(resetClock.Id, WorkshopSocket.ActivationOut, latch.Id, WorkshopSocket.ResetIn, WorkshopConnectionDomain.Activation),
                new(latch.Id, WorkshopSocket.ActivationOut, lamp.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation),
                new(setClock.Id, WorkshopSocket.ActivationOut, delay.Id, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 180);
        var registry = new PartRegistry(); registry.Load();
        var latchPart = registry.Create(PartId.Latch);
        var lampPart = registry.Create(PartId.SignalLamp);
        var delayPart = registry.Create(PartId.Delay);
        try
        {
            godot.Tree.Root.AddChild(latchPart); godot.Tree.Root.AddChild(lampPart); godot.Tree.Root.AddChild(delayPart);
            var evaluator = new WorkshopDeclaredPresentation(construction, Response(reads[120], 1));
            var origin = Time(reads[120]);
            var quarter = evaluator.Advance(origin + .125 / 4);
            Assert.Equal(-.1982421875f, Scalar(quarter, latch.Id, 0));
            Apply(quarter);
            Assert.Equal(-.1982421875f, latchPart.GetNode<Node3D>("Visual/Rocker").Rotation.Z, 6);
            Apply(evaluator.Advance(origin + .16));
            Assert.Equal(new Color("#f7cb52"), Material(latchPart, "Visual/IndicatorLamp").AlbedoColor);
            Assert.Equal(new Color("#fff0a5"), Material(lampPart, "Visual/Bulb").AlbedoColor);
            Assert.Equal(new Color("#e9b24c"), Material(lampPart, "Visual/Bulb").Emission);
            evaluator.Advance(Time(reads[180]));
            evaluator.Accept(Response(reads[180], 2));
            var falling = evaluator.Advance(Time(reads[180]) + .125 / 4);
            Assert.Equal(.1982421875f, Scalar(falling, latch.Id, 0));
            Apply(evaluator.Advance(Time(reads[180]) + .16));
            Assert.Equal(new Color("#556573"), Material(latchPart, "Visual/IndicatorLamp").AlbedoColor);
            Assert.Equal(new Color("#556573"), Material(lampPart, "Visual/Bulb").AlbedoColor);
            Assert.Equal(Colors.Black, Material(lampPart, "Visual/Bulb").Emission);
            foreach (var (tick, expected, progress) in new[] { (0, "#556573", 0f), (150, "#e8b764", .5f), (180, "#f7cb52", 1f) })
            {
                var at = new WorkshopDeclaredPresentation(construction, Response(reads[tick], 1));
                var samples = at.Advance(Time(reads[tick]));
                Assert.Equal(-MathF.Tau * progress, Scalar(samples, delay.Id, 0));
                Apply(samples);
                Assert.Equal(new Color(expected), Material(delayPart, "Visual/Indicator").AlbedoColor);
            }
            latchPart.ResetPhysicalVisuals(); lampPart.ResetPhysicalVisuals(); delayPart.ResetPhysicalVisuals();
            Assert.Equal(-.25f, latchPart.GetNode<Node3D>("Visual/Rocker").Rotation.Z, 6);
            Assert.Equal(new Color("#556573"), Material(delayPart, "Visual/Indicator").AlbedoColor);
            void Apply(IReadOnlyList<DeclaredPresentationSample> samples)
            {
                foreach (var sample in samples)
                    if (sample.Owner == latch.Id) latchPart.ApplyDeclaredProperty(sample.Binding, sample.Value);
                    else if (sample.Owner == lamp.Id) lampPart.ApplyDeclaredProperty(sample.Binding, sample.Value);
                    else if (sample.Owner == delay.Id) delayPart.ApplyDeclaredProperty(sample.Binding, sample.Value);
            }
        }
        finally { latchPart.Free(); lampPart.Free(); delayPart.Free(); }
    }

    [Theory]
    [InlineData(0f, false, 0)]
    [InlineData(.25f, true, 1)]
    [InlineData(.5f, true, 2)]
    [InlineData(.75f, true, 3)]
    [InlineData(1f, true, 4)]
    // Story 9A.39: the gauge's full scale is the instance's own declared capacity, not the package default.
    [InlineData(.75f, true, 3, 60f, 10f)]
    [InlineData(.5f, true, 2, 14400f, 480f)]
    [InlineData(.25f, true, 1, 1234.5f, 33f)]
    public async Task BatterySupplyAndEachChargeBoundaryUseActualCommittedRead(float fraction, bool available, int lit,
        float capacity = 3600f, float power = 120f)
    {
        var battery = PlacedParts.Battery(new(1), 0, 3, 0, capacity, power, fraction);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(battery));
        var phases = await WorkshopPhysicsEngineTests.RunPhases(construction, ([], 1),
            ([new(battery.Id, ElectricalEnable.Disabled)], 1));
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(PartId.Battery);
        try
        {
            godot.Tree.Root.AddChild(part);
            for (var phase = 0; phase < phases.Count; phase++)
            {
                var read = phases[phase][0];
                var presentation = new WorkshopDeclaredPresentation(construction, Response(read, 1));
                foreach (var sample in presentation.Advance(Time(read)))
                    part.ApplyDeclaredProperty(sample.Binding, sample.Value);
                Assert.Equal(new Color(available && phase == 0 ? "#f7cb52" : "#556573"), Material(part, "Visual/Terminal").AlbedoColor);
                for (var mark = 0; mark < 4; mark++)
                    Assert.Equal(new Color(mark < lit ? "#f7cb52" : "#556573"), Material(part, $"Visual/Mark_{mark}").AlbedoColor);
            }
            part.ResetPhysicalVisuals();
            Assert.Equal(new Color("#556573"), Material(part, "Visual/Terminal").AlbedoColor);
        }
        finally { part.Free(); }
    }

    [Fact]
    public async Task SameWorldPrepareCommitRetainsConsumedHiddenAndInProgressChannels()
    {
        var battery = PlacedParts.Battery(new(1), -3, 3, 0);
        var clock = PlacedParts.Clock(new(2), 0, 3, 0, 0, 0, 0, 1, .125f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(battery, clock),
            Connections: new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, clock.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        var reads = await WorkshopPhysicsEngineTests.Run(construction, 15);
        var endpoint = Response(reads[15], 1) with { Construction = construction };
        var start = Time(reads[15]);
        var consumed = new WorkshopDeclaredPresentation(construction, endpoint);
        consumed.Advance(start); consumed.Advance(start + .3);
        var rebound = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(endpoint with
        {
            Sequence = new(1), Kind = WorkshopResponseKind.Acknowledgement, Projection = new(2), Publication = default
        }));
        consumed.ValidateRebind(construction, rebound);
        // An aborted Prepare has not changed accepted identity or resurrected a consumed occurrence.
        Assert.Equal(85f / 255f, Scalar(consumed.Advance(start + .4), clock.Id, 1));
        Assert.Throws<ArgumentException>(() => consumed.Accept(rebound));
        consumed.Rebind(construction, rebound);
        Assert.Equal(85f / 255f, Scalar(consumed.Advance(start + .5), clock.Id, 1));
        consumed.Rebind(construction, rebound with { Projection = new(3) });
        Assert.Equal(85f / 255f, Scalar(consumed.Advance(start + .6), clock.Id, 1));

        var hidden = new WorkshopDeclaredPresentation(construction, endpoint);
        var target = WorkshopDeclaredPresentation.Targets(construction).Single(t => t.Owner == clock.Id && t.Binding == 1).Target;
        hidden.SetVisible(target, AnimationProperty.ColourRed, false);
        hidden.ValidateRebind(construction, rebound);
        hidden.Rebind(construction, rebound);
        Assert.DoesNotContain(hidden.Advance(start + 1), sample => sample.Owner == clock.Id);
        hidden.SetVisible(target, AnimationProperty.ColourRed, true);
        Assert.Equal(247f / 255f, Scalar(hidden.Advance(start + 1.01), clock.Id, 1));

        var progressing = new WorkshopDeclaredPresentation(construction, endpoint);
        progressing.Advance(start);
        progressing.Advance(start + .0625);
        progressing.Rebind(construction, rebound);
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .5f), Scalar(progressing.Advance(start + .125), clock.Id, 1));
        Assert.Throws<ArgumentException>(() => progressing.ValidateRebind(construction,
            rebound with { Projection = new(3), Read = rebound.Read with { Epoch = new(99) } }));
        Assert.Throws<ArgumentException>(() => progressing.ValidateRebind(construction,
            rebound with { Projection = new(3), Session = new(7, 8) }));
        Assert.Equal(float.Lerp(85f / 255f, 247f / 255f, .25f), Scalar(progressing.Advance(start + .1875), clock.Id, 1));
    }

    private static double Time(WorkshopRead read) => read.Capture!.Value.Time.Value / 1_000_000_000d;
    private static StandardMaterial3D Material(MachinePart part, string path) =>
        Assert.IsType<StandardMaterial3D>(part.GetNode<MeshInstance3D>(path).MaterialOverride);

    private static WorkshopResponse Response(WorkshopRead read, ulong publication) =>
        new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, new(101, 206), new(publication), new(1), new(1), new(1), new(read.Tick.Value));
}
