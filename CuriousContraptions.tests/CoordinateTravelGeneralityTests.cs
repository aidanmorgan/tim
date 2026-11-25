using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class CoordinateTravelGeneralityTests
{
    public enum Exposure { Both, First, Second }

    [Theory]
    [InlineData(false, Exposure.Both)]
    [InlineData(true, Exposure.Both)]
    [InlineData(false, Exposure.First)]
    [InlineData(true, Exposure.First)]
    [InlineData(false, Exposure.Second)]
    [InlineData(true, Exposure.Second)]
    public async Task DistinctObserversFollowExactMovingCoordinatesAndReset(bool reverse, Exposure exposure)
    {
        var entry = TestBundle.Entry(new("windmill"));
        var template = entry.Mechanical!.Hinges[0];
        IReadOnlyList<ColliderDescriptor> sphere =
            [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.05f), Vector3.Zero, Quaternion.Identity, new(0, .3f, 0, .1f), 0)];
        var hinges = new[] {
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, 0, 2) } },
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, -.25f, 0) } },
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, .25f, 0) } }
        };
        var firstIndex = reverse ? 2 : 1;
        var secondIndex = reverse ? 1 : 2;
        if (reverse) (hinges[1], hinges[2]) = (hinges[2], hinges[1]);
        var original = entry.Mechanical.RotaryAirflowCaptures[0];
        LocalColliderReference[] exclusions = [new(new(1), new(0)), new(new(2), new(0)), new(new(3), new(0))];
        var first = original with { Id = new(17), Coordinate = new(CoordinateKind.Revolute, firstIndex),
            Samples = [new(new(-.12f, -.27f, 0), .3f), new(new(-.12f, -.23f, 0), .7f)],
            InternalOccluders = exclusions, Response = MechanicalValue.Fixed(1), MaximumPitch = MechanicalValue.Fixed(.3f) };
        var second = original with { Id = new(53), Frame = new(secondIndex + 1),
            Coordinate = new(CoordinateKind.Revolute, secondIndex),
            Samples = [new(new(0, -.02f, 0), .7f), new(new(0, .02f, 0), .3f)],
            InternalOccluders = exclusions, Response = MechanicalValue.Fixed(1.5f), MaximumPitch = MechanicalValue.Fixed(.5f) };
        var part = new PartId("unfamiliar_capture_pair");
        entry = entry with { Metadata = entry.Metadata with { Id = part.Value },
            Footprint = new(4, 3, 0, 3),
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.05f), new(0, -2, 0), Quaternion.Identity, new(0, .3f, 0, .1f), 0)],
            Ports = [], Events = [new(SimulationEventKind.CoordinateTravelReached, CoordinateTravel: new LocalCoordinateTravelId(17)),
                new(SimulationEventKind.CoordinateTravelReached, CoordinateTravel: new LocalCoordinateTravelId(53)),
                new(SimulationEventKind.CoordinateTravelReached, CoordinateTravel: new LocalCoordinateTravelId(81))],
            // Physical body targets remain required; cosmetic evaluators are absent.
            Presentation = entry.Presentation with { Bindings = [], Bodies = [
                new("Visual/Unrelated", new(1), Vector3.Zero), new("Visual/First", new(2), Vector3.Zero),
                new("Visual/Second", new(3), Vector3.Zero)] },
            Mechanical = entry.Mechanical with { Hinges = hinges, MovingBodyColliders = [sphere, sphere, sphere],
                RotaryAirflowCaptures = reverse ? [second, first] : [first, second],
                CoordinateTravelObservers = reverse ? [new(new(81), new(CoordinateKind.Revolute, 0), .75f),
                    new(new(53), new(CoordinateKind.Revolute, secondIndex), 1.25f), new(new(17), new(CoordinateKind.Revolute, firstIndex), .5f)] :
                    [new(new(17), new(CoordinateKind.Revolute, firstIndex), .5f), new(new(53), new(CoordinateKind.Revolute, secondIndex), 1.25f),
                    new(new(81), new(CoordinateKind.Revolute, 0), .75f)] } };
        Assert.Empty(BundleValidator.Validate([new("capture pair", entry)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portsScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var pair = WindmillIntegrationTests.Windmill() with { PartId = part };
        var fan = FanIntegrationTests.Fan();
        if (exposure != Exposure.Both)
        {
            // At the rotor this cone is about0.12m wide in radius: it covers the selected
            // 0.02m samples and cannot reach the sibling hinge0.5m away.
            var parameters = fan.Parameters.ToDictionary();
            parameters[new("width")] = .2f;
            fan = WorkshopInput.Generic(RuntimeTestServices.Bundle, fan.PartId, fan.Id,
                0, exposure == Exposure.First ? 4.75f : 5.25f, 0, 0, 0, 0, 1, new(parameters));
        }
        var expectedCount = exposure == Exposure.Both ? 2 : 1;
        var parts = reverse ? new IWorkshopInstance[] { pair, fan } : [fan, pair];
        await h.Require(WorkshopCommandKind.Construct, TennisBallTests.Scene(parts));
        var ids = h.Construction.Instances.Identities[pair.Id];
        var initial = h.Committed.CoordinateTravelObservers;
        for (var run = 0; run < 2; run++)
        {
            var previousEvents = h.Of(SimulationEventKind.CoordinateTravelReached).Count();
            await h.Run(); await h.Advance(360);
            var occurrences = h.Of(SimulationEventKind.CoordinateTravelReached).Skip(previousEvents).ToArray();
            Assert.Equal(expectedCount, occurrences.Length);
            foreach (var (local, threshold, index, selected) in new[] {
                (17u, .5f, firstIndex, exposure != Exposure.Second),
                (53u, 1.25f, secondIndex, exposure != Exposure.First) })
            {
                var id = ids.CoordinateTravelObservers[new(local)];
                var physical = Assert.Single(h.Committed.Presentation.Actuators.ToArray(),
                    value => value.Drive.RevoluteJoint == ids.Hinges[index].Joint);
                var read = Enumerable.Range(0, h.Committed.CoordinateTravelObservers.Count)
                    .Select(i => h.Committed.CoordinateTravelObservers[i]).Single(value => value.Id == id);
                Assert.Equal(pair.Id, read.Owner);
                Assert.Equal(threshold, read.Threshold);
                if (!selected)
                {
                    // Below the earliest declared occurrence threshold: no gameplay-visible
                    // first-travel event is permitted on the passive physical sibling.
                    Assert.True(MathF.Abs(physical.Coordinate) < .5f);
                    Assert.Equal(CoordinateTravelPhase.Accumulating, read.Phase);
                    Assert.True(read.Travel < .5f);
                    Assert.DoesNotContain(occurrences, value => value.Source.Value == id.Value);
                    continue;
                }
                Assert.True(MathF.Abs(physical.Coordinate) >= threshold);
                Assert.Equal(CoordinateTravelPhase.Reached, read.Phase);
                var occurrence = Assert.Single(occurrences, value => value.Source.Value == id.Value);
                Assert.Equal(0ul, occurrence.Other);
                Assert.Equal(threshold, occurrence.Magnitude);
                Assert.Equal(1u, occurrence.Count);
            }
            var stationary = ids.CoordinateTravelObservers[new(81)];
            var reads = Enumerable.Range(0, h.Committed.CoordinateTravelObservers.Count)
                .Select(i => h.Committed.CoordinateTravelObservers[i]).ToArray();
            Assert.Equal(0, Assert.Single(reads, value => value.Id == stationary).Travel);
            Assert.DoesNotContain(occurrences, value => value.Source.Value == stationary.Value);
            await h.Advance(360);
            Assert.Equal(previousEvents + expectedCount, h.Of(SimulationEventKind.CoordinateTravelReached).Count());
            await h.Reset();
            for (var i = 0; i < initial.Count; i++) Assert.Equal(initial[i], h.Committed.CoordinateTravelObservers[i]);
        }
    }
}
