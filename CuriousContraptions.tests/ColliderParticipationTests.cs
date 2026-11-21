using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ColliderParticipationTests
{
    [Theory]
    [InlineData(CollisionParticipation.Enabled)]
    [InlineData(CollisionParticipation.Disabled)]
    public async Task AuthoredParticipationControlsContactWithoutChangingMassOrReset(CollisionParticipation participation)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var source = TestBundle.Entry(new("ball"));
        var entry = source with
        {
            Metadata = source.Metadata with { Id = "participating_payload" },
            Mechanical = source.Mechanical! with { AirflowReceivers = [] },
            Colliders = source.Colliders.Select(c => c with { Participation = participation }).ToArray()
        };
        Assert.Empty(BundleValidator.Validate([new(entry.Metadata.Id, entry)]));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        Assert.All(decoded.Colliders, c => Assert.Equal(participation, c.Participation));
        var expectedFrame = source.Mechanical!.MassFrame;
        var decodedFrame = decoded.Mechanical!.MassFrame;
        Assert.Equal(expectedFrame.Source.Stores, decodedFrame.Source.Stores);
        Assert.Equal(expectedFrame with { Source = expectedFrame.Source with { Stores = decodedFrame.Source.Stores } }, decodedFrame);
        using var physics = DeclaredPhysics.Scoped([decoded]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11), 0, 2, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([instance]));
        var scene = CompiledSceneFixture.Compile(construction);
        Assert.All(scene.Physics.Colliders.ToArray().Where(c => c.Body == instance.Id), c => Assert.Equal(participation, c.Participation));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        Assert.True(harness.Committed.Bodies.TryGet(instance.Id, out var initial));
        await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(instance.Id, out var result));
        var height = (result.Body.Cell.Y + result.Body.Local.Y) / 16f;
        // One second of gravity takes an unsupported body through the bench; an enabled
        // ball must remain above its support. These corridors distinguish contact from no contact.
        if (participation == CollisionParticipation.Disabled) Assert.True(height < LawScene.BenchY - .5f);
        else Assert.True(height > LawScene.BenchY + .1f);
        await harness.Reset();
        Assert.True(harness.Committed.Bodies.TryGet(instance.Id, out var reset));
        Assert.Equal(initial.Body.Cell, reset.Body.Cell);
        Assert.Equal(initial.Body.Local, reset.Body.Local);
        Assert.Equal(initial.Rotation, reset.Rotation);
        await harness.Run();
        await harness.Advance(120);
        Assert.True(harness.Committed.Bodies.TryGet(instance.Id, out var repeated));
        var repeatedHeight = (repeated.Body.Cell.Y + repeated.Body.Local.Y) / 16f;
        if (participation == CollisionParticipation.Disabled) Assert.True(repeatedHeight < LawScene.BenchY - .5f);
        else Assert.True(repeatedHeight > LawScene.BenchY + .1f);
    }

    [Theory]
    [InlineData(CollisionParticipation.Enabled)]
    [InlineData(CollisionParticipation.Disabled)]
    public async Task LateralGuideRequiresParticipatingPayloadMaterial(CollisionParticipation participation)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var source = TestBundle.Entry(new("ball"));
        var entry = source with
        {
            Metadata = source.Metadata with { Id = "guided_payload" },
            Mechanical = source.Mechanical! with { AirflowReceivers = [] },
            Colliders = source.Colliders.Select(c => c with { Participation = participation }).ToArray()
        };
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11), .08f, 2.2f, 0, 0, 0, 0, 1);
        var parameters = InstanceParameterCollection.Defaults(new PartId("basket")).ToDictionary();
        parameters[new("force_maximum_acceleration")] = 12f;
        var receiver = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("basket"), new(12), .2f, 1, 0, 0, 0, 0, 1, new(parameters));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([ball, receiver]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(1);
        Assert.True(harness.Committed.Bodies.TryGet(ball.Id, out var result));
        // The guide centre is .12 m to the right. One 120 Hz tick gives about
        // 12*.12/120=.012 m/s of lateral response; no material must give none.
        if (participation == CollisionParticipation.Disabled) Assert.Equal(0f, result.Body.Velocity.X);
        else Assert.InRange(result.Body.Velocity.X, .01f, .014f);
    }

    [Fact]
    public void CurrentBundleRequiresCanonicalParticipation()
    {
        var entry = TestBundle.Entry(new("ball"));
        var json = BundleSchema.WriteEntries([entry]);
        Assert.Contains("\"Participation\": \"Enabled\"", json);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Participation\": \"Enabled\"", "\"Participation\": \"Unknown\"", StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Participation\": \"Enabled\"", "\"Participation\": 0", StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Participation\": \"Enabled\"", "\"UnexpectedParticipation\": \"Enabled\"", StringComparison.Ordinal)));
        Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Colliders = entry.Colliders.Select(c => c with { Participation = (CollisionParticipation)255 }).ToArray()
        })]));
    }
}
