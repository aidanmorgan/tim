using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BaseballTests(NativeSceneFixture godot, Xunit.ITestOutputHelper output)
{
    private static readonly PartId Baseball = new("baseball");
    private static readonly PartId Basketball = new("ball");
    private static readonly PartId TennisBall = new("tennis");

    public enum Payload { Baseball, Basketball, TennisBall }

    private static PartId PayloadPart(Payload payload) => payload switch
    {
        Payload.Baseball => Baseball,
        Payload.Basketball => Basketball,
        Payload.TennisBall => TennisBall,
        _ => throw new ArgumentOutOfRangeException(nameof(payload))
    };

    private static WorkshopGenericInstance Ball(float y = 3f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, Baseball, new(1), 0, y, 0, 0, 0, 0, 1);
    private static WorkshopConstruction Scene(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));
    private static CanonicalBody Body(WorkshopIntegrationHarness h)
    {
        Assert.True(h.Committed.Bodies.TryGet(new(1), out var body));
        return body.Body;
    }
    private static float Y(CanonicalBody body) => (body.Cell.Y + body.Local.Y) / 16f;

    [Fact]
    public async Task IdenticalDropDistinguishesEveryRetainedPreset()
    {
        // Each uses the same centre release at 3 m above the same bench.
        // A 15 cm peak separation is 29% of Baseball's 0.52 m diameter: visibly distinct,
        // and enough to select a different catch height without pinning impact time.
        var peaks = new Dictionary<PartId, float>();
        foreach (var id in new PartId[] { Baseball, Basketball, new("bowling"), TennisBall })
        {
            var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, id, new(1), 0, 3, 0, 0, 0, 0, 1);
            await using var h = await WorkshopIntegrationHarness.Running(Scene(instance));
            var rising = false;
            var peak = float.NegativeInfinity;
            for (var tick = 0; tick < 600; tick++)
            {
                await h.Advance(1);
                var body = Body(h);
                if (!rising && body.Velocity.Y > .1f) rising = true;
                if (!rising) continue;
                peak = MathF.Max(peak, Y(body));
                if (body.Velocity.Y <= 0) break;
            }
            Assert.True(rising, $"{id}: no rebound within the five-second stall guard.");
            peaks.Add(id, peak);
            output.WriteLine($"{id.Value}: first rebound centre peak {peak} m");
        }
        foreach (var control in peaks.Where(pair => pair.Key != Baseball))
            Assert.True(MathF.Abs(peaks[Baseball] - control.Value) >= .15f,
                $"Baseball and {control.Key} must have a visible catch-height distinction.");
        // The retained buoyant preset rises on release rather than returning to the bench.
        await using var balloon = await WorkshopIntegrationHarness.Running(Scene(
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("balloon"), new(1), 0, 3, 0, 0, 0, 0, 1)));
        await balloon.Advance(120);
        Assert.True(Y(Body(balloon)) > 3.15f);
        Assert.True(peaks[Baseball] < 2.85f);
        output.WriteLine($"balloon: centre after one second {Y(Body(balloon))} m, ascending");
    }

    [Fact]
    public async Task UnpoweredDropSettlesWithoutFreeRebound()
    {
        foreach (var release in new[] { 3f, WorkshopConstruction.WorkbenchSurface.Value + .26f })
        {
            await using var h = await WorkshopIntegrationHarness.Running(Scene(Ball(release)));
            for (var tick = 0; tick < 1200; tick++)
            {
                await h.Advance(1);
                var body = Body(h);
                // A 1 cm upward excursion beyond release would be a visible free launch.
                Assert.True(Y(body) <= release + .01f);
                if (tick < 960) continue;
                // In the last two seconds, 1 mm of vertical support alignment and
                // 5 cm/s of total speed exclude sinking, floating or continuing to move.
                Assert.InRange(Y(body) - (WorkshopConstruction.WorkbenchSurface.Value + .26f), -.001f, .001f);
                Assert.InRange(new Vector3(body.Velocity.X, body.Velocity.Y, body.Velocity.Z).Length(), 0, .05f);
            }
        }
    }

    [Fact]
    public void SceneAndIconExposeBandedSphereMatchingContactGeometry()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(Baseball);
        try
        {
            godot.Tree.Root.AddChild(part); part.EnsureConstructed();
            var sphere = Assert.IsType<SphereMesh>(part.GetNode<MeshInstance3D>("Visual/Sphere").Mesh);
            Assert.Equal(.26f, sphere.Radius); Assert.Equal(.52f, sphere.Height);
            Assert.NotNull(WorkshopIcons.Element(part.Definition.IconPath));
            var band = Assert.IsType<TorusMesh>(part.GetNode<MeshInstance3D>("Visual/Band").Mesh);
            Assert.Equal(.242f, band.InnerRadius); Assert.Equal(.27f, band.OuterRadius);
            var visualChildren = part.GetNode("Visual").GetChildren().OfType<MeshInstance3D>()
                .Select(node => node.Name.ToString()).Order().ToArray();
            Assert.Equal(["Band", "Sphere"], visualChildren);
            var colliders = TestBundle.Entry(Baseball).Colliders;
            Assert.Equal(2, colliders.Count);
            var sphereCollider = Assert.Single(colliders, value => value.ShapeKind == CuriousContraptions.Abstractions.ColliderShapeKind.Sphere);
            var torusCollider = Assert.Single(colliders, value => value.ShapeKind == CuriousContraptions.Abstractions.ColliderShapeKind.Torus);
            Assert.Equal(.26f, sphereCollider.HalfExtents.X);
            Assert.Equal((.242f + .27f) * .5f, torusCollider.Radius);
            Assert.Equal((.27f - .242f) * .5f, torusCollider.HalfExtents.Y);
            Assert.Equal(sphereCollider.Material, torusCollider.Material);
            Assert.Empty(TestBundle.Entry(Baseball).Presentation.Bodies);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { part.GetParent()?.RemoveChild(part); part.Free(); }
    }

    [Fact]
    public async Task CommittedPresentationFollowsMovingBodyAndReset()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(Baseball);
        part.AuthoredId = new(1); part.Position = new(0, 3, 0);
        var root = new Node3D();
        await using var h = await WorkshopIntegrationHarness.Building(Scene(Ball()));
        var presentation = new WorkshopPhysicalPresentation();
        void Apply()
        {
            Assert.True(h.Committed.Bodies.TryGet(new(1), out var read));
            presentation.Apply([part], root.GlobalTransform,
                new PresentedBodySet([new(read.Body.Id, read.Body.Cell, read.Body.Local, read.Rotation)]));
            // One millimetre is the visible body/contact alignment allowance.
            Assert.InRange(MathF.Abs(part.Position.Y - Y(Body(h))), 0, .001f);
        }
        try
        {
            godot.Tree.Root.AddChild(root); root.AddChild(part); part.EnsureConstructed();
            Apply();
            await h.Run(); await h.Advance(30); Apply();
            Assert.True(part.Position.Y < 2.8f);
            await h.Require(WorkshopCommandKind.Pause);
            var paused = part.Transform;
            Assert.Equal(WorkshopRejection.WrongPhase, (await h.AdvanceOne()).Reason); Apply();
            Assert.Equal(paused, part.Transform);
            await h.Reset(); Apply();
            Assert.Equal(new Vector3(0, 3, 0), part.Position);
            Assert.Equal(Quaternion.Identity, part.Quaternion);
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            part.GetParent()?.RemoveChild(part); part.Free();
            root.GetParent()?.RemoveChild(root); root.Free();
        }
    }

    [Fact]
    public async Task FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters()
    {
        var authored = Scene(Ball());
        var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, authored, new(1, 2));
        var body = Assert.Single(compiled.Bodies.ToArray(), value => value.Id == new GpuBodyId(1));
        Assert.Equal(.15f, body.Mass.Value);
        // Solid-sphere inertia is a local mathematical contract: 0.01% rejects
        // a wrong mass/radius exponent without asserting a simulated trajectory.
        foreach (var moment in new[] { body.MassProperties.X, body.MassProperties.Y, body.MassProperties.Z })
            Assert.InRange(MathF.Abs(moment.Moment - .4f * .15f * .26f * .26f), 0, 4.056e-7f);
        var colliders = compiled.Colliders.ToArray().Where(value => value.Body == body.Id).ToArray();
        Assert.Equal(2, colliders.Length);
        var sphereCollider = Assert.Single(colliders, value => value.Shape == CuriousContraptions.Gpu.ColliderShapeKind.Sphere);
        var torusCollider = Assert.Single(colliders, value => value.Shape == CuriousContraptions.Gpu.ColliderShapeKind.Torus);
        Assert.Equal(.26f, sphereCollider.Radius.Value);
        Assert.Equal(sphereCollider.Material, torusCollider.Material);
        var material = Assert.Single(compiled.Materials.ToArray(), value => value.Id == sphereCollider.Material);
        Assert.Equal(.35f, material.Restitution.Value);
        Assert.Equal(.3f, material.Friction.Value);
        Assert.Equal(.03f, material.RollingResistance.Value);
        Assert.Equal(.1f, material.BounceThreshold.Value);
        Assert.Equal(.04f, Assert.Single(compiled.AmbientLoads.ToArray()).LinearDrag);
        // The Baseball is declared a capture-target payload.
        Assert.True(DeclaredPhysics.IsPayload(new PartId("baseball")));
        await using var h = await WorkshopIntegrationHarness.Building(authored);
        var epoch = h.Committed.Epoch;
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var invalid = Ball() with { Local = new(0, value, 0) };
            var bytes = SaveFixture.Forged(Scene(invalid), 2);
            SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () =>
                WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
                    RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => h.Execute(WorkshopCommandKind.Construct, Scene(invalid)));
            Assert.Equal(authored, h.Construction); Assert.Equal(epoch, h.Committed.Epoch);
        }
        foreach (var identity in new[] { "baseball", "unknown_baseball" })
        {
            var bytes = PayloadInstanceTests.GenericSave(identity, [("foreign", 1f)]);
            var error = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
                RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
            Assert.Equal(identity == "baseball" ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.UnknownElement, error.Reason);
            var record = WorkshopSaveCodec.Open(RuntimeTestServices.Core.Codecs.Commands, bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(identity == "baseball" ? WireFaultReason.OutOfRange : WireFaultReason.UnknownElement,
                Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
            Assert.Equal(authored, h.Construction); Assert.Equal(epoch, h.Committed.Epoch);
        }
    }

    [Fact]
    public async Task RunPauseStepResetSaveLoadRestoresAuthoredBaseball()
    {
        var authored = Scene(Ball());
        var saved = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(authored, new(2)));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, saved).Construction;
        var instance = Assert.IsType<WorkshopGenericInstance>(Assert.Single(loaded.Instances));
        Assert.Equal(Baseball, instance.PartId); Assert.Empty(instance.Parameters);
        foreach (var construction in new[] { authored, loaded })
        {
            await using var h = await WorkshopIntegrationHarness.Running(construction);
            await h.Advance(30); Assert.True(Y(Body(h)) < 2.8f);
            await h.Require(WorkshopCommandKind.Pause);
            var tick = h.Committed.Tick; var paused = Body(h);
            Assert.Equal(WorkshopRejection.WrongPhase, (await h.AdvanceOne()).Reason);
            Assert.Equal(tick, h.Committed.Tick); Assert.Equal(paused.Local, Body(h).Local);
            await h.Require(WorkshopCommandKind.Step);
            Assert.Equal(tick.Value + 1, h.Committed.Tick.Value);
            Assert.True(Y(Body(h)) < Y(paused));
            await h.Require(WorkshopCommandKind.Resume); await h.Advance(30);
            Assert.True(Y(Body(h)) < 2f);
            await h.Reset(); await h.Reset();
            Assert.Equal(3f, Y(Body(h))); Assert.Equal(0f, Body(h).Velocity.Y);
            await h.Run(); await h.Advance(30); Assert.True(Y(Body(h)) < 2.8f);
        }
    }

    [Fact]
    public async Task TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball()
    {
        var speeds = new Dictionary<PartId, float>();
        foreach (var id in new[] { Baseball, TennisBall, Basketball })
        {
            var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, id, new(2), 3, 5, 0, 0, 0, 0, 1);
            await using var h = await WorkshopIntegrationHarness.Running(
                TennisBallTests.Scene(FanIntegrationTests.Fan(powered: true), cargo));
            await h.Advance(12);
            Assert.True(h.Committed.Bodies.TryGet(cargo.Id, out var read));
            var forwardSpeed = read.Body.Velocity.X;
            Assert.True(float.IsFinite(forwardSpeed) && forwardSpeed > 0,
                $"{id}: the powered Fan must produce finite forward motion, observed {forwardSpeed} m/s.");
            speeds.Add(id, forwardSpeed);
            output.WriteLine($"{id}: velocity X after 12 ticks = {forwardSpeed} m/s");
        }
        Assert.True(speeds[Baseball] >= 1.5f * speeds[TennisBall]);
        Assert.True(speeds[Baseball] >= 3f * speeds[Basketball]);
    }

    [Theory]
    [InlineData(.5f, Payload.Baseball, 1, false)]
    [InlineData(.5f, Payload.Baseball, 2, false)]
    [InlineData(.5f, Payload.TennisBall, 2, true)]
    [InlineData(.25f, Payload.Baseball, 2, true)]
    [InlineData(.25f, Payload.Baseball, 1, false)]
    [InlineData(.1f, Payload.Baseball, 1, true)]
    public async Task TwoBaseballsLeaveADefaultPlateOff(float minimumMass, Payload payload, int count, bool expected)
    {
        var payloadId = PayloadPart(payload);
        var plateY = WorkshopConstruction.WorkbenchSurface.Value + .22f;
        var parts = new List<IWorkshopInstance> { PlacedParts.Plate(new(1), 0, plateY, 0, minimumMass) };
        for (var i = 0; i < count; i++)
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, payloadId, new((ulong)i + 2),
                count == 1 ? 0 : i == 0 ? -.5f : .5f, plateY + .34f, 0, 0, 0, 0, 1));
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(parts.ToArray()));
        var loaded = false;
        for (var tick = 0; tick < 2400 && !loaded; tick++)
        {
            await h.Advance(1);
            var state = Assert.Single(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]),
                activation => activation.Owner == new GpuBodyId(1));
            loaded = state.Phase == ActivationPhase.Latched;
        }
        Assert.Equal(expected, loaded);
        if (!expected)
        {
            for (var i = 0; i < count; i++)
            {
                Assert.True(h.Committed.Bodies.TryGet(new((ulong)i + 2), out var ball));
                Assert.InRange(Y(ball.Body) - (WorkshopConstruction.WorkbenchSurface.Value + .56f), -.001f, .001f);
                Assert.InRange(new System.Numerics.Vector3(ball.Body.Velocity.X, ball.Body.Velocity.Y, ball.Body.Velocity.Z).Length(), 0, .05f);
            }
        }
    }

    [Theory]
    [InlineData(Payload.Basketball, true)]
    [InlineData(Payload.Baseball, false)]
    public async Task BaseballNeverSeatsInTheCannon(Payload payload, bool expectedSeated)
    {
        var q = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, MathF.PI / 2f);
        var cannon = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("cannon"), new(1), 0, 3, 0, q.X, q.Y, q.Z, q.W);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, PayloadPart(payload), new(2), 0, 5f, 0, 0, 0, 0, 1);
        var clock = PlacedParts.Clock(new(3), -4, 3, 0, 0, 0, 0, 1, 2f);
        var battery = PlacedParts.Battery(new(4), -7, 3, 0);
        var wires = new WorkshopConnection[]
        {
            new(new(4), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(3), PortId.ActivationOut, new(1), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(4), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical)
        };
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(cannon, ball, clock, battery), Connections: new(wires));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var everSeated = false;
        for (var tick = 0; tick < 600; tick++)
        {
            await h.Advance(1);
            everSeated |= h.Committed.ChamberRegions[0].Phase == ChamberPhase.Seated;
        }
        Assert.Equal(expectedSeated, everSeated);
        if (!expectedSeated)
        {
            Assert.Equal(ChamberPhase.Loading, h.Committed.ChamberRegions[0].Phase);
            Assert.Equal(1u, h.Committed.ChamberRegions[0].Occupancy);
        }
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(3f, false)]
    public async Task AnAlignedBaseballIsCapturedAndAShiftedOneIsNot(float x, bool expected)
    {
        var payload = WorkshopInput.Generic(RuntimeTestServices.Bundle, Baseball, new(1), x, 1.1f, 0, 0, 0, 0, 1);
        var receiver = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("basket"), new(2), 0, 1, 0, 0, 0, 0, 1);
        await using var h = await WorkshopIntegrationHarness.Running(Scene(payload, receiver));
        await h.Advance(240);

        var captures = h.Committed.Captures;
        var latched = Enumerable.Range(0, captures.Count).Select(index => captures[index])
            .Any(capture => capture.Target == payload.Id && capture.Phase == CaptureLatchPhase.Latched);
        Assert.Equal(expected, latched);

        var events = h.Records.Where(record => record.Kind == SimulationEventKind.Captured);
        if (!expected)
        {
            Assert.Empty(events);
            return;
        }

        var captured = Assert.Single(events);
        Assert.Equal(new GpuEventSourceId(receiver.Id.Value), captured.Source);
        Assert.Equal(payload.Id, new GpuBodyId(captured.Other));
        const ulong minimumDwellTicks = 42; // The default 0.35 s dwell at 120 Hz.
        Assert.True(captured.Tick >= minimumDwellTicks, $"Capture at tick {captured.Tick} bypassed the 0.35 s dwell.");
    }
}
