using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class WorkshopContactBlockTests
{
    public enum SupportShape { Cap, Face, NarrowFace, Line, Point }

    public static TheoryData<SupportShape, float, float, float, SimulationCadence> Supports()
    {
        var data = new TheoryData<SupportShape, float, float, float, SimulationCadence>();
        foreach (var shape in Enum.GetValues<SupportShape>())
            foreach (var radius in new[] { .1f, .4f })
                foreach (var mass in new[] { .25f, 8f })
                    foreach (var drop in new[] { 0f, .002f })
                        foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz120, SimulationCadence.Hz240 })
                            data.Add(shape, radius, mass, drop, cadence);
        return data;
    }

    /// <summary>All contact dimensions, including a nearly dependent narrow face, must carry
    /// their declared weight with penetration <= 0.5 mm on every tick, including settling.
    /// After four seconds settling, check every tick for another six seconds: horizontal excursion from the fixed reference within the size-based
    /// rest envelope, and no angular displacement exceeding that same spatial allowance.</summary>
    [Theory]
    [MemberData(nameof(Supports))]
    public void FlatSupportedBodiesCarryTheirWeightAcrossScales(SupportShape shape, float radius, float mass, float drop, SimulationCadence cadence)
    {
        var scene = new LawScene();
        var material = new LawMaterial(0f, .1f, .5f, 0f);
        var half = new Vector3(radius, .1f, shape == SupportShape.NarrowFace ? radius / 100f : radius);
        var restHeight = shape is SupportShape.Line or SupportShape.Point ? radius : .1f;
        var position = new Vector3(0, LawScene.BenchY + restHeight + drop, 0);
        var rotation = shape == SupportShape.Line
            ? Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2) : Quaternion.Identity;
        var id = shape switch
        {
            SupportShape.Cap or SupportShape.Line => scene.Cylinder(position, radius, .1f, mass, material, rotation),
            SupportShape.Face or SupportShape.NarrowFace => scene.Box(position, half, mass, material),
            SupportShape.Point => scene.Sphere(position, radius, mass, material, default, default, 0f, LawScene.Gravity),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var size = shape switch
        {
            SupportShape.Line => 2f * MathF.Min(radius, .1f),
            SupportShape.NarrowFace => 2f * half.Z,
            _ => 2f * radius,
        };
        var allowance = MathF.Min(.00005f, MathF.Max(.000005f, .0001f * size));
        var world = scene.Admit(cadence);
        var reference = world.Initial(id);
        RestingObservation? rest = null;
        var axes = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
        var ticks = 0;
        world.Step(10 * world.TicksPerSecond, (tick, state) =>
        {
            ticks++;
            var body = state[id];
            Assert.True(float.IsFinite(body.Position.Y) && double.IsFinite(body.Speed) && double.IsFinite(body.Spin));
            Assert.True(body.Position.Y >= LawScene.BenchY + restHeight - .0005f,
                $"tick {tick}: support height {body.Position.Y}, expected at least {LawScene.BenchY + restHeight - .0005f}");
            if (tick == 4 * world.TicksPerSecond)
            {
                reference = body;
                rest = new(size, reference.Position);
            }
            if (tick < 4 * world.TicksPerSecond) return;
            Assert.InRange(body.Position.Y, LawScene.BenchY + restHeight - .0005f,
                LawScene.BenchY + restHeight + .0005f);
            rest!.Observe(body.Position);
            Assert.True(rest.IsResting, $"tick {tick}: excursion {rest.PeakMetres}, allowance {allowance}, resolved {rest.Resolved}");
            foreach (var axis in axes)
            {
                var turned = Vector3.Distance(Vector3.Transform(axis, body.Rotation),
                    Vector3.Transform(axis, reference.Rotation));
                Assert.True(turned * MathF.Max(radius, .1f) <= allowance,
                    $"tick {tick}: axis {axis}, introduced angular displacement {turned}");
            }
        });
        Assert.Equal(10 * world.TicksPerSecond, ticks);
    }
}
