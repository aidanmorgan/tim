using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Shape = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions.Tests;

public sealed class ChamberOccupancyTests
{
    public enum LoadCase { Seated, Small, Protruding, DisabledFrame, DisabledPayload, DisabledProtrusion, EnabledProtrusion, HollowGap, RotatedCompound }

    [Theory]
    [InlineData(.3f, 0f, ChamberPhase.Seated)]
    [InlineData(.42f, 0f, ChamberPhase.Seated)]
    [InlineData(.299f, 0f, ChamberPhase.Loading)]
    [InlineData(.421f, 0f, ChamberPhase.Loading)]
    [InlineData(.35f, 1f, ChamberPhase.Seated)]
    [InlineData(.35f, 1.01f, ChamberPhase.Loading)]
    public void WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries(float radius, float speed, ChamberPhase expected)
    {
        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), radius, 1f, new(0, 0, 0, 0),
            new(speed, 0f, 0f), Vector3.Zero, 0f, 0f);
        fixture.FiniteWork(frame, payload, 45f, 0f);
        var world = fixture.Admit();
        var candidate = new byte[world.Admitted.Length];
        var interactions = new CuriousContraptions.Simulation.WorkshopPhysicsEngine().Advance(
            world.Admitted, candidate, new([], []), PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var chamber = Assert.Single(PhysicsGpuAbi.ReadChamberRegions(candidate));
        Assert.Equal(expected, chamber.Phase);
        Assert.Equal(1u, chamber.Occupancy);
        Assert.Equal(payload, chamber.Candidate);
        Assert.Equal(0f, Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate)).Released.Value);
    }

    [Theory]
    [InlineData(LoadCase.Seated, ChamberPhase.Seated, 1u)]
    [InlineData(LoadCase.Small, ChamberPhase.Loading, 1u)]
    [InlineData(LoadCase.Protruding, ChamberPhase.Loading, 1u)]
    [InlineData(LoadCase.DisabledFrame, ChamberPhase.Empty, 0u)]
    [InlineData(LoadCase.DisabledPayload, ChamberPhase.Empty, 0u)]
    [InlineData(LoadCase.DisabledProtrusion, ChamberPhase.Seated, 1u)]
    [InlineData(LoadCase.EnabledProtrusion, ChamberPhase.Loading, 1u)]
    [InlineData(LoadCase.HollowGap, ChamberPhase.Empty, 0u)]
    [InlineData(LoadCase.RotatedCompound, ChamberPhase.Seated, 1u)]
    public async Task CurrentMaterialDeterminesOccupancyAndSeatingThroughTransport(LoadCase load, ChamberPhase phase, uint count)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var (frameEntry, payloadEntry) = Entries(load);
        Assert.Empty(BundleValidator.Validate([new(frameEntry.Metadata.Id, frameEntry), new(payloadEntry.Metadata.Id, payloadEntry)]));
        using var physics = DeclaredPhysics.Scoped([frameEntry, payloadEntry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([
            (new PartId(frameEntry.Metadata.Id), frameEntry.Ports), (new PartId(payloadEntry.Metadata.Id), payloadEntry.Ports)]));
        var frame = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(frameEntry.Metadata.Id), new(11), 0, 3, 0, 0, 0, 0, 1, WallFixture.Default);
        var turn = load == LoadCase.RotatedCompound ? Quaternion.CreateFromAxisAngle(Vector3.UnitY, .3f) : Quaternion.Identity;
        var payload = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(payloadEntry.Metadata.Id), new(12),
            0, 3, 0, turn.X, turn.Y, turn.Z, turn.W);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([frame, payload]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(1);
        AssertRegion(harness.Committed);
        await harness.Reset();
        Assert.Equal(ChamberPhase.Empty, harness.Committed.ChamberRegions[0].Phase);
        await harness.Run();
        await harness.Advance(1);
        AssertRegion(harness.Committed);

        void AssertRegion(WorkshopRead read)
        {
            Assert.Equal(1, read.ChamberRegions.Count);
            var region = read.ChamberRegions[0];
            Assert.Equal(phase, region.Phase); Assert.Equal(count, region.Occupancy);
            Assert.Equal(count == 1 ? payload.Id : default, region.Candidate);
            Assert.Equal(default, region.Departing);
        }
    }

    private static (BundleEntry Frame, BundleEntry Payload) Entries(LoadCase load)
    {
        var wall = TestBundle.Entry(new("wall"));
        var frame = wall with
        {
            Metadata = wall.Metadata with { Id = "chamber_frame" },
            Colliders = wall.Colliders.Select(c => c with
            {
                Offset = new(0, -2, 0),
                Participation = load == LoadCase.DisabledFrame ? CollisionParticipation.Disabled : CollisionParticipation.Enabled
            }).ToArray(),
            Mechanical = wall.Mechanical! with
            {
                ChamberRegions = [new(new(1), new(0), new(0), Vector3.Zero, Quaternion.Identity, .48f, .55f, .6f, .84f, .001f, 1f)]
            }
        };
        var ball = TestBundle.Entry(new("ball"));
        var sphere = ball.Colliders[0] with
        {
            HalfExtents = new(load == LoadCase.Small ? .28f : .35f),
            Offset = load == LoadCase.Protruding ? new(.35f, 0, 0) : Vector3.Zero,
            Participation = load == LoadCase.DisabledPayload ? CollisionParticipation.Disabled : CollisionParticipation.Enabled
        };
        ColliderDescriptor[] children = load switch
        {
            LoadCase.DisabledProtrusion or LoadCase.EnabledProtrusion => [sphere, sphere with
            {
                Offset = new(1, 0, 0),
                Participation = load == LoadCase.DisabledProtrusion ? CollisionParticipation.Disabled : CollisionParticipation.Enabled
            }],
            LoadCase.HollowGap => [sphere with { ShapeKind = Shape.Tube, Radius = .7f, HalfExtents = new(.6f, .25f, 0) }],
            LoadCase.RotatedCompound => [
                sphere with { ShapeKind = Shape.Box, HalfExtents = new(.15f, .2f, .3f), Offset = new(-.15f, 0, 0) },
                sphere with { ShapeKind = Shape.Box, HalfExtents = new(.15f, .2f, .3f), Offset = new(.15f, 0, 0) }],
            _ => [sphere]
        };
        return (frame, ball with
        {
            Metadata = ball.Metadata with { Id = "chamber_payload" },
            Colliders = children,
            Mechanical = ball.Mechanical! with
            {
                MassFrame = MassFrameDeclaration.Constant(1, Vector3.Zero, new(.1f), Quaternion.Identity, 0, Vector3.Zero)
            }
        });
    }
}
