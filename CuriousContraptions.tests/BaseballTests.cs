using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BaseballTests(NativeSceneFixture godot, Xunit.ITestOutputHelper output)
{
    private static readonly PartId Baseball = new("baseball");
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
    private static float X(CanonicalBody body) => (body.Cell.X + body.Local.X) / 16f;
    private static float X(LawBody body) => body.Position.X;

    [Fact]
    public async Task IdenticalDropDistinguishesEveryRetainedPreset()
    {
        // Each uses the same centre release at 3 m above the same bench.
        // A 15 cm peak separation is 30% of Baseball's diameter: visibly distinct,
        // and enough to select a different catch height without pinning impact time.
        var peaks = new Dictionary<PartId, float>();
        foreach (var id in new PartId[] { Baseball, new("ball"), new("bowling"), new("tennis") })
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
    public void MatchedSizeImpactChangesPayloadRouteByMass()
    {
        // Target restitution 1 times striker restitution .35 gives pair e=.35.
        // At 2 m/s, central collision with e=.35 and a .5 kg target gives
        // v_target=(1+.35)*m/(m+.5)*2: 1.526 versus 1.112 m/s.
        // A receiver entrance at x=1.3 m after one second therefore distinguishes
        // the masses with 10 cm clearance, allowing the declared 4%/s drag.
        var heavy = Impact(Baseball, .65f, 2f, 0);
        var light = Impact(new("tennis"), .35f, 2f, 0);
        var matched = Impact(new("tennis"), .65f, 2f, 0);
        Assert.True(X(heavy) > 1.4f);
        Assert.True(X(light) < 1.2f);
        Assert.InRange(MathF.Abs(X(heavy) - X(matched)), 0, .02f);
        Assert.InRange(MathF.Abs(X(Impact(Baseball, .65f, 0, 0))), 0, .01f);
        Assert.InRange(MathF.Abs(X(Impact(Baseball, .65f, -2, 0))), 0, .01f);
        Assert.InRange(MathF.Abs(X(Impact(Baseball, .65f, 2, 1))), 0, .01f);
        output.WriteLine($"One-second payload x: Baseball {X(heavy)} m; light control {X(light)} m; mass-matched control {X(matched)} m");
    }

    private static LawBody Impact(PartId striker, float mass, float speed, float miss)
    {
        var parameters = striker == Baseball ? null : new InstanceParameterCollection([
            new(new("mass"), mass), new(new("radius"), .25f), new(new("bounce"), .35f),
            new(new("drag"), .04f), new(new("buoyancy"), 0f)]);
        var first = WorkshopInput.Generic(RuntimeTestServices.Bundle, striker, new(1), -.51f, 10, miss, 0, 0, 0, 1, parameters);
        var target = TennisBallTests.Ball(2, 0, 10, mass: .5f, bounce: 1f);
        var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, Scene(first, target), new(1, 2));
        // Velocity is the typed production physics input; geometry, inertia and materials
        // are all from the real package compiler. Both bodies fall together in free flight.
        var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies.ToArray().Select(body => body.Id == new GpuBodyId(1)
                ? body with { Velocity = new(speed, 0, 0) } : body).ToArray(),
            compiled.Colliders, compiled.Materials, compiled.Sensors, compiled.Guides, ambientLoads: compiled.AmbientLoads);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        return world.Step(120)[new(2)];
    }

    [Fact]
    public async Task UnpoweredDropSettlesWithoutFreeRebound()
    {
        foreach (var release in new[] { 3f, WorkshopConstruction.WorkbenchSurface.Value + .25f })
        {
            await using var h = await WorkshopIntegrationHarness.Running(Scene(Ball(release)));
            for (var tick = 0; tick < 1200; tick++)
            {
                await h.Advance(1);
                var body = Body(h);
                // A 1 cm upward excursion beyond release would be a visible free launch.
                Assert.True(Y(body) <= release + .01f);
                if (tick < 960) continue;
                // Final two seconds: 1 mm support alignment and 5 cm/s exclude
                // visible sinking, floating or continued bouncing.
                Assert.InRange(Y(body) - (WorkshopConstruction.WorkbenchSurface.Value + .25f), -.001f, .001f);
                Assert.InRange(MathF.Abs(body.Velocity.Y), 0, .05f);
                Assert.InRange(MathF.Abs(X(body)), 0, .001f);
            }
        }
    }

    [Fact]
    public void SceneAndIconExposeSeamedSphereMatchingContactGeometry()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var part = registry.Create(Baseball);
        try
        {
            godot.Tree.Root.AddChild(part); part.EnsureConstructed();
            var sphere = Assert.IsType<SphereMesh>(part.GetNode<MeshInstance3D>("Visual/Sphere").Mesh);
            Assert.Equal(.25f, sphere.Radius); Assert.Equal(.5f, sphere.Height);
            Assert.NotNull(WorkshopIcons.Element(part.Definition.IconPath));
            foreach (var name in new[] { "SeamLeft", "SeamRight" })
            {
                var seam = part.GetNode<MeshInstance3D>($"Visual/{name}");
                Assert.IsType<TorusMesh>(seam.Mesh);
                Assert.Equal(.125f, MathF.Abs(seam.Position.X));
            }
            var collider = Assert.Single(TestBundle.Entry(Baseball).Colliders);
            Assert.Equal(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, collider.ShapeKind);
            Assert.Equal(.25f, collider.HalfExtents.X);
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
        Assert.Equal(.65f, body.Mass.Value);
        // Solid-sphere inertia is a local mathematical contract: 0.01% rejects
        // a wrong mass/radius exponent without asserting a simulated trajectory.
        foreach (var moment in new[] { body.MassProperties.X, body.MassProperties.Y, body.MassProperties.Z })
            Assert.InRange(MathF.Abs(moment.Moment - .4f * .65f * .25f * .25f), 0, .000001625f);
        var collider = Assert.Single(compiled.Colliders.ToArray(), value => value.Body == body.Id);
        Assert.Equal(.25f, collider.Radius.Value);
        var material = Assert.Single(compiled.Materials.ToArray(), value => value.Id == collider.Material);
        Assert.Equal(.35f, material.Restitution.Value); Assert.Equal(.3f, material.Friction.Value);
        Assert.Equal(.04f, Assert.Single(compiled.AmbientLoads.ToArray()).LinearDrag);
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
}
