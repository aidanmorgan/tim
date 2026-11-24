using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class TennisBallTests
{
    private static readonly PartId Tennis = new("tennis");
    internal static WorkshopGenericInstance Ball(ulong id = 1, float x = 0f, float y = 3f, float mass = .35f,
        float radius = .25f, float bounce = .85f, float drag = .04f, float buoyancy = 0f, float z = 0f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, Tennis, new(id), x, y, z, 0, 0, 0, 1,
            new([new(new("mass"), mass), new(new("radius"), radius), new(new("bounce"), bounce),
                new(new("drag"), drag), new(new("buoyancy"), buoyancy)]));
    internal static WorkshopConstruction Scene(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));
    private static CanonicalBody Body(WorkshopIntegrationHarness h, ulong id = 1)
    {
        Assert.True(h.Committed.Bodies.TryGet(new(id), out var body));
        return body.Body;
    }
    private static float Y(CanonicalBody body) => (body.Cell.Y + body.Local.Y) / 16f;

    [Theory]
    [InlineData(.125f, .125f)]
    [InlineData(2f, .5f)]
    [InlineData(1f / 1024f, 1f / 16f)]
    [InlineData(1024f, 16f)]
    public void IndependentMassAndRadiusCompileCorrectInertiaAndPersist(float mass, float radius)
    {
        var ball = Ball(y: 20f, mass: mass, radius: radius, bounce: .6f, drag: .08f, buoyancy: 2f);
        var construction = Scene(ball);
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == ball.Id);
        Assert.Equal(mass, body.Mass.Value);
        Assert.Equal(0f, body.LinearDrag.Value);
        Assert.Equal(-9.81f, body.Gravity.Y);
        var ambient = Assert.Single(scene.AmbientLoads.ToArray());
        Assert.Equal(.08f, ambient.LinearDrag);
        Assert.Equal(2f, ambient.Acceleration.Y);
        // This tests the kg·m² compilation contract; 0.01% excludes a wrong mass/radius exponent.
        var expected = .4 * mass * radius * radius;
        foreach (var moment in new[] { body.MassProperties.X, body.MassProperties.Y, body.MassProperties.Z })
            Assert.InRange(Math.Abs(moment.Moment - expected), 0, expected * .0001);
        var collider = Assert.Single(scene.Colliders.ToArray(), c => c.Body == ball.Id);
        Assert.Equal(radius, collider.Radius.Value);
        Assert.Equal(.6f, Assert.Single(scene.Materials.ToArray(), m => m.Id == collider.Material).Restitution.Value);
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(decoded.Construction.Instances));
        Assert.Equal(Tennis, loaded.PartId);
        foreach (var parameter in ball.Parameters) Assert.Equal(parameter.Value, loaded.Parameters[parameter.Key]);
    }

    [Fact]
    public async Task ConfiguredBuoyancyAndDragChangeMotionAndResetRestoresAuthoredState()
    {
        var original = Scene(Ball(1, -2, 10, drag: 0), Ball(2, 0, 10, drag: .125f), Ball(3, 2, 10, buoyancy: 16));
        await using var h = await WorkshopIntegrationHarness.Running(original);
        await h.Advance(120);
        Assert.True(Y(Body(h, 1)) < 6f);
        Assert.True(Y(Body(h, 3)) > 12f);
        // At one second the declared damping must remove at least0.3m/s from the downward flight.
        Assert.True(Body(h, 2).Velocity.Y > Body(h, 1).Velocity.Y + .3f);
        await h.Require(WorkshopCommandKind.Pause);
        var before = h.Committed.Tick;
        await h.Require(WorkshopCommandKind.Step);
        Assert.True(h.Committed.Tick.Value > before.Value);
        await h.Reset();
        foreach (var id in new ulong[] { 1, 2, 3 })
        {
            Assert.Equal(10f, Y(Body(h, id)));
            Assert.Equal(0f, Body(h, id).Velocity.Y);
        }
        Assert.Equal(original, h.Construction);
        await h.Run();
        await h.Advance(120);
        Assert.True(Y(Body(h, 3)) > 12f);
    }

    [Fact]
    public async Task TennisReboundsAboveMatchedHeavierBallsAndSettlesOnTheBench()
    {
        var construction = Scene(Ball(1, -2, drag: 0),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(2), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("bowling"), new(3), 2, 3, 0, 0, 0, 0, 1));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var rebounded = new bool[3];
        var peaks = new float[] { -1, -1, -1 };
        for (var tick = 0; tick < 2400; tick++)
        {
            await h.Advance(1);
            for (var i = 0; i < 3; i++)
            {
                var body = Body(h, (ulong)i + 1);
                rebounded[i] |= body.Velocity.Y > .1f;
                if (rebounded[i]) peaks[i] = MathF.Max(peaks[i], Y(body));
            }
            if (tick >= 2160)
            {
                // One millimetre excludes visible sinking/separation during the final two-second rest window.
                Assert.InRange(Y(Body(h)) - (WorkshopConstruction.WorkbenchSurface.Value + .25f), -.001f, .001f);
                Assert.InRange(MathF.Abs(Body(h).Velocity.Y), 0f, .05f);
            }
        }
        Assert.All(rebounded, Assert.True);
        Assert.True(peaks[0] > peaks[1] + .5f && peaks[1] > peaks[2] + .5f);
        var fall = 3f - (WorkshopConstruction.WorkbenchSurface.Value + .25f);
        // Height fraction distinguishes e=.85 from the superseded e=.78 without pinning contact timing.
        Assert.InRange((peaks[0] - (WorkshopConstruction.WorkbenchSurface.Value + .25f)) / fall, .70f, .75f);
    }

    [Theory]
    [InlineData(.25f)]
    [InlineData(1f)]
    public void ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload(float mass)
    {
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
            Scene(Ball(1, -.51f, 5, mass: mass, bounce: 1, drag: 0, buoyancy: 9.81f),
                Ball(2, 0, 5, mass: .5f, bounce: 1, drag: 0, buoyancy: 9.81f)), new(1, 2));
        var world = MovingWorld(scene, new(2, 0, 0));
        var result = world.Step(60);
        // Elastic, central collision: v_target = 2*m/(m+.5)*2.
        // A .05 m/s error moves a payload 2.5 cm off its half-second route;
        // keeping the default mass would miss either independently derived interval.
        var targetSpeed = 4f * mass / (mass + .5f);
        Assert.InRange(result[new(2)].Velocity.X, targetSpeed - .05f, targetSpeed + .05f);
        Assert.InRange(result[new(1)].Velocity.X, 2f - .5f / mass * targetSpeed - .05f,
            2f - .5f / mass * targetSpeed + .05f);
    }

    [Theory]
    [InlineData(.125f)]
    [InlineData(.5f)]
    public void ConfiguredRadiusChangesContactSpinAtFixedMass(float radius)
    {
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle,
            Scene(Ball(y: WorkshopConstruction.WorkbenchSurface.Value + radius + .01f,
                mass: .35f, radius: radius, bounce: 0, drag: 0, buoyancy: 9.81f)), new(1, 2));
        var world = MovingWorld(scene, new(1, -1, 0));
        var result = world.Step(30)[new(1)];
        // A solid sphere's sticking impulse has effective tangential mass 2m/7:
        // v_x=5/7 and omega_z=-5/(7r). Friction .3 supports that impulse.
        // These speed envelopes keep rolling travel within 1 cm over this quarter-second
        // and distinguish either radius from an unscaled moment or contact lever arm.
        Assert.InRange(result.Velocity.X, .68f, .75f);
        Assert.InRange(-result.Angular.Z * radius, .68f, .75f);
    }

    private static LawWorld MovingWorld(IPhysicsSceneDeclaration compiled, LinearVelocity initial)
    {
        // Initial velocity is a typed physics declaration input; all authored mass, geometry,
        // material and acceleration values still come from the production package compiler.
        var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies.ToArray().Select(body => body.Id == new GpuBodyId(1) ? body with { Velocity = initial } : body).ToArray(),
            compiled.Colliders, compiled.Materials, compiled.Sensors, compiled.Guides, ambientLoads: compiled.AmbientLoads);
        return new(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
    }

    [Fact]
    public void EveryConfiguredFieldIsRequiredAtSaveAndWireDecode()
    {
        var ball = Ball();
        foreach (var omitted in ball.Parameters)
        {
            var parameters = ball.Parameters.Where(p => p.Key != omitted.Key).Select(p => (p.Key.Value, p.Value)).ToArray();
            var bytes = PayloadInstanceTests.GenericSave("tennis", parameters);
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange,
                () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
                    RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
            var record = WorkshopSaveCodec.Open(RuntimeTestServices.Core.Codecs.Commands, bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange,
                Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
        }
    }

    [Theory]
    [InlineData("mass", 0f)]
    [InlineData("radius", .01f)]
    [InlineData("bounce", 1.01f)]
    [InlineData("drag", -.01f)]
    [InlineData("buoyancy", 17f)]
    [InlineData("unknown", 1f)]
    [InlineData("drag", float.NaN)]
    public void InvalidConfigurationCannotEnterConstruction(string key, float value)
    {
        var ball = Ball();
        var parameters = ball.Parameters.Where(p => p.Key != new ParameterId(key)).Append(new(new(key), value));
        Assert.Throws<ArgumentException>(() => (ball with { Parameters = new(parameters) }).Validate(RuntimeTestServices.Bundle));
        Assert.Equal(.35f, ball.Parameters[new("mass")]);
        var bytes = PayloadInstanceTests.GenericSave("tennis", parameters.Select(p => (p.Key.Value, p.Value)).ToArray());
        SaveFixture.Refused(float.IsFinite(value) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite,
            () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
                RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(RuntimeTestServices.Core.Codecs.Commands, bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange,
            Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
    }
}
