using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WoundSpringContactTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(.5f, .34f, 0f, false, false)]
    [InlineData(2f, .34f, 0f, false, false)]
    [InlineData(.5f, .34f, .061f, false, false)]
    [InlineData(.5f, .45f, 0f, false, false)]
    [InlineData(4f, .508f, 0f, false, false)]
    [InlineData(.5f, .34f, 0f, true, false)]
    // The lesson's own rotated launcher (authored −15°) and its authored payload, left in place.
    [InlineData(.5f, .34f, 0f, false, true)]
    public async Task RealHeadContactLaunchesLoadsAndGuideRimStopsOversizeEntry(
        float mass, float radius, float offset, bool stacked, bool rotated)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var template = TestBundle.Entry(new("ball"));
        var probe = template with { Metadata = template.Metadata with { Id = "unfamiliar_spring_payload" },
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(radius), Vector3.Zero, Quaternion.Identity,
                new(0f, .3f, 0f, .1f), 0f)],
            Mechanical = template.Mechanical! with { MassFrame = MassFrameDeclaration.Constant(mass, Vector3.Zero,
                new(.4f * mass * radius * radius), Quaternion.Identity, 0f, new(0f, -9.81f, 0f)) } };
        using var declared = DeclaredPhysics.Scoped([probe]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var lesson = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            new("wind_then_release"), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var spring = lesson.Instances.Single(instance => instance.PartId == new PartId("wound_spring")).Id;
        var payload = lesson.Puzzle.Goal.Body;
        var parts = rotated ? lesson.Instances.ToList() : lesson.Instances.Select(instance => instance.Id == spring
            ? (IWorkshopInstance)((WorkshopGenericInstance)instance with { Rotation = CanonicalRotation.Identity })
            : instance.Id == payload
                ? WorkshopInput.Generic(RuntimeTestServices.Bundle, new(probe.Metadata.Id), payload,
                    offset, radius > .41f ? 4.9f : 4.562f, 0, 0, 0, 0, 1)
                : instance).ToList();
        if (rotated) Assert.NotEqual(CanonicalRotation.Identity, ((WorkshopGenericInstance)parts.Single(part => part.Id == spring)).Rotation);
        var second = new GpuBodyId(1000);
        if (stacked) parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(probe.Metadata.Id), second,
            offset, 4.562f + 2f * radius + .002f, 0, 0, 0, 0, 1));
        var construction = lesson with { Instances = new(parts.ToArray()), Puzzle = default };
        await harness.Require(WorkshopCommandKind.Construct, construction); await harness.Run();
        var authored = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
        WorkshopRead? releaseOrigin = null;
        var sourceAtRelease = 0f;
        var availableAtRelease = 0f;
        PrismaticRetentionRead? chargeAtRelease = null;
        var allowance = SpringTestEnergy.Allowance(harness.Simulation.Scene.Physics);
        var peak = 0f;
        var secondPeak = 0f;
        var released = false;
        var lowestBeforeRelease = float.PositiveInfinity;
        for (var tick = 0; tick < 600; tick++)
        {
            var before = harness.Committed;
            var previousWork = SpringTestEnergy.SourceWork(harness);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.AdvanceOne()).Outcome);
            released |= Assert.Single(harness.Committed.Presentation.Retentions.ToArray()).RequestResult == RetentionRequestResult.Released;
            if (released && releaseOrigin is null)
            {
                releaseOrigin = before;
                chargeAtRelease = Assert.Single(before.Presentation.Retentions.ToArray());
                sourceAtRelease = previousWork;
                availableAtRelease = SpringTestEnergy.Measure(harness.Simulation.Scene.Physics, before, before);
            }
            if (releaseOrigin is { } origin)
            {
                var mechanical = SpringTestEnergy.Measure(harness.Simulation.Scene.Physics, harness.Committed, origin);
                var paidSinceRelease = SpringTestEnergy.SourceWork(harness) - sourceAtRelease;
                Assert.True(mechanical <= availableAtRelease + paidSinceRelease + allowance,
                    $"Release created energy: mechanical={mechanical}, stored/kinetic={availableAtRelease}, later source={paidSinceRelease}, allowance={allowance}");
            }
            if (!released)
            {
                Assert.True(harness.Committed.Bodies.TryGet(payload, out var waiting));
                lowestBeforeRelease = MathF.Min(lowestBeforeRelease, (waiting.Body.Cell.Y + waiting.Body.Local.Y) / 16f);
                continue;
            }
            Assert.True(harness.Committed.Bodies.TryGet(payload, out var body));
            peak = MathF.Max(peak, body.Body.Velocity.Y);
            if (stacked)
            {
                Assert.True(harness.Committed.Bodies.TryGet(second, out var upper));
                secondPeak = MathF.Max(secondPeak, upper.Body.Velocity.Y);
            }
        }
        Assert.True(released);
        // The motor wound and the ratchet retained useful charge until the independent release.
        Assert.NotNull(chargeAtRelease);
        Assert.Equal(RetentionMode.Retained, chargeAtRelease.Value.Mode);
        Assert.True(chargeAtRelease.Value.Compression > .1f, $"charge at release {chargeAtRelease.Value.Compression} m");
        output.WriteLine($"mass={mass}, radius={radius}, offset={offset}, stacked={stacked}: upward peak={peak}, upper={secondPeak}");
        if (radius <= .41f)
        {
            Assert.True(peak > 1f, "Real spring-head contact must give the payload useful upward motion.");
            if (stacked) Assert.True(secondPeak > 1f, "The upper load receives motion through payload contact.");
        }
        else
        {
            Assert.True(peak > .05f, "The oversized load must receive a real rim/head tap, not remain an unobserved fixture.");
            // The lower spherical cap may meet the rising head while the equator cannot enter the bore.
            Assert.True(lowestBeforeRelease > 4.4f, "The oversized sphere centre stays above the sleeve rim before release.");
        }
        await harness.Reset();
        Assert.Equal(authored, Assert.Single(harness.Committed.Presentation.Retentions.ToArray()));
        Assert.True(harness.Committed.Bodies.TryGet(payload, out var reset));
        Assert.Equal(parts.Single(part => part.Id == payload).Cell, reset.Body.Cell);
        Assert.Equal(parts.Single(part => part.Id == payload).Local, reset.Body.Local);
    }
}
