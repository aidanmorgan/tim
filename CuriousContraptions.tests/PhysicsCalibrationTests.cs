using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

// Measures our solver, not the original game's physics. Keep reference-footage
// measurements separate until edition, geometry and capture timing are known.
[Collection<HeadlessCollection>]
public class PhysicsCalibrationTests(HeadlessFixture godot, ITestOutputHelper output)
{
    [Theory]
    [InlineData("ball", 0f)]
    [InlineData("ball", 1f)]
    [InlineData("ball", 2f)]
    [InlineData("bowling", 0f)]
    [InlineData("bowling", 1f)]
    [InlineData("bowling", 2f)]
    [InlineData("tennis", 0f)]
    [InlineData("tennis", 1f)]
    [InlineData("tennis", 2f)]
    public void IsolatedDropAndFirstReboundAreRepeatable(string kind, float pressure)
    {
        var world = new MachineWorld { Precision = 1 };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new MachineData
            {
                Gravity = 9.81f, Pressure = pressure,
                Parts = [new PartSpec { Id = "probe", Kind = kind, Position = [0, 6, 0] }]
            });
            var body = world.Bodies.Single();
            var contactHeight = Workbench.SurfaceY + body.Radius;
            var fallHeight = body.Position.Y - contactHeight;
            var bounce = body.Bounce;
            var samples = Capture(world);
            world.Restore();
            Assert.Equal(new Vector3(0, 6, 0), world.Bodies.Single().Position);
            Assert.Equal(Vector3.Zero, world.Bodies.Single().Velocity);
            var repeated = Capture(world);
            Assert.Equal(samples, repeated);

            var impact = Enumerable.Range(1, samples.Length - 1).First(i =>
                samples[i - 1].Velocity.Y < 0 && samples[i].Velocity.Y > 0);
            var apex = Enumerable.Range(impact + 1, samples.Length - impact - 1).First(i =>
                samples[i - 1].Velocity.Y > 0 && samples[i].Velocity.Y <= 0);
            var apexHeight = Math.Max(samples[apex - 1].Position.Y, samples[apex].Position.Y);
            var reboundRatio = (apexHeight - contactHeight) / fallHeight;
            Assert.InRange(reboundRatio, 0f, 1f);
            Assert.All(samples, sample =>
            {
                Assert.Equal(0f, sample.Position.X);
                Assert.Equal(0f, sample.Position.Z);
                Assert.True(sample.Visible);
            });
            // A zero-pressure, isolated vertical drop removes our drag term.
            // These are Newtonian sanity checks, not historical TIM constants.
            if (pressure == 0)
            {
                Assert.InRange(impact * MachineWorld.Tick,
                    MathF.Sqrt(2 * fallHeight / world.Gravity) - .02f,
                    MathF.Sqrt(2 * fallHeight / world.Gravity) + .02f);
                Assert.InRange(reboundRatio, bounce * bounce - .015f, bounce * bounce + .015f);
            }
            output.WriteLine(JsonSerializer.Serialize(new
            {
                evidence = "current-engine-baseline-not-TIM-reference",
                kind, pressure, gravity = world.Gravity, precision = world.Precision,
                initialCenterY = 6, contactCenterY = contactHeight,
                tickSeconds = MachineWorld.Tick, substeps = MachineWorld.Substeps,
                firstImpactTick = impact, firstApexTick = apex, apexCenterY = apexHeight,
                reboundHeightRatio = reboundRatio,
                samplingUncertaintySeconds = MachineWorld.Tick,
                identicalReplaySamples = samples.Length
            }));
        }
        finally { world.Free(); }
    }

    private readonly record struct Sample(Vector3 Position, Vector3 Velocity, bool Visible);

    private static Sample[] Capture(MachineWorld world)
    {
        world.Start();
        var body = world.Bodies.Single();
        var samples = new Sample[481];
        samples[0] = new(body.Position, body.Velocity, body.Visible);
        for (var tick = 1; tick < samples.Length; tick++)
        {
            world.Step();
            samples[tick] = new(body.Position, body.Velocity, body.Visible);
        }
        return samples;
    }
}

