using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ParameterizedBodyIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnfamiliarObliqueBodyUsesExplicitMassLengthAndWorldAcceleration(bool reverse)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var original = TestBundle.Entry(new("tennis"));
        ParameterDescriptor[] parameters = [
            new("weight_input", ParameterUnit.Kilogram, ParameterKind.Real, .25f, 8, 2, []),
            new("size_input", ParameterUnit.Metre, ParameterKind.Real, .125f, 2, .5f, []),
            new("vector_input", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, 0, 16, 4, [])];
        var entry = original with
        {
            Metadata = original.Metadata with { Id = "oblique_payload", Parameters = reverse ? parameters.Reverse().ToArray() : parameters },
            ColliderBindings = [
                new(0, GeometryComponent.HalfExtentX, "size_input", 1, 0),
                new(0, GeometryComponent.HalfExtentY, "size_input", 1, 0),
                new(0, GeometryComponent.HalfExtentZ, "size_input", 1, 0)],
            MaterialBindings = [],
            Presentation = original.Presentation with { Parameters = [] },
            Mechanical = original.Mechanical! with { MassFrame = new(
                new(new ParameterId("weight_input"), 0, 1, 0, 0), MassVector.Constant(Vector3.Zero),
                new(PrincipalMomentOperation.ScaledMassLengthSquared, default, new(new ParameterId("weight_input"), 0),
                    new(new ParameterId("size_input"), 0), new(.2f, .3f, .4f)),
                Quaternion.Identity, MassScalar.Constant(0),
                new(new ParameterId("vector_input"), new(0, 16, 0), new(1, -1, 0))) }
        };
        Assert.Empty(BundleValidator.Validate([new("oblique", entry)]));
        using var scope = DeclaredPhysics.Scoped([Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements)]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(1), 0, 3, 0, 0, 0, 0, 1);
        var construction = TennisBallTests.Scene(instance);
        await h.Require(WorkshopCommandKind.Construct, construction);
        var body = Assert.Single(h.Simulation.Scene.Physics.Bodies.ToArray(), b => b.Id == instance.Id);
        Assert.Equal(.1f, body.MassProperties.X.Moment);
        Assert.Equal(.15f, body.MassProperties.Y.Moment);
        Assert.Equal(.2f, body.MassProperties.Z.Moment);
        await h.Run(); await h.Advance(120);
        Assert.True(h.Committed.Bodies.TryGet(instance.Id, out var moved));
        // Analytic displacement .5*a*t²; 2 cm protects the one-second flight route without timestep goldens.
        Assert.InRange((moved.Body.Cell.X + moved.Body.Local.X) / 16f, 1.98f, 2.02f);
        Assert.InRange((moved.Body.Cell.Y + moved.Body.Local.Y) / 16f, 8.98f, 9.02f);
        await h.Reset();
        Assert.True(h.Committed.Bodies.TryGet(instance.Id, out var reset));
        Assert.Equal(3f, (reset.Body.Cell.Y + reset.Body.Local.Y) / 16f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReorderedBindingsResolveBaseSliderAndHingeMaterialsByExactBody(bool reverse)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var original = TestBundle.Entry(new("wound_spring"));
        MaterialParameterBinding[] bindings = [
            new(new(2), MaterialComponent.Restitution, new("hinge_surface")),
            new(new(0), MaterialComponent.Restitution, new("base_surface")),
            new(new(1), MaterialComponent.Restitution, new("slider_surface"))];
        var entry = original with
        {
            Metadata = original.Metadata with { Id = "three_materials", Parameters = [.. original.Metadata.Parameters,
                new("base_surface", ParameterUnit.None, ParameterKind.Real, 0, 1, .2f, []),
                new("slider_surface", ParameterUnit.None, ParameterKind.Real, 0, 1, .4f, []),
                new("hinge_surface", ParameterUnit.None, ParameterKind.Real, 0, 1, .8f, [])] },
            MaterialBindings = reverse ? bindings.Reverse().ToArray() : bindings
        };
        Assert.Empty(BundleValidator.Validate([new("materials", entry)]));
        using var scope = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(1), 0, 3, 0, 0, 0, 0, 1);
        var construction = TennisBallTests.Scene(instance);
        await h.Require(WorkshopCommandKind.Construct, construction);
        var ids = construction.Instances.Identities[instance.Id];
        var scene = h.Simulation.Scene.Physics;
        foreach (var (body, expected) in new[] { (instance.Id, .2f), (ids.Sliders[0].Body, .4f), (ids.Hinges[0].Body, .8f) })
        {
            var shapes = scene.Colliders.ToArray().Where(c => c.Body == body).ToArray();
            Assert.NotEmpty(shapes);
            Assert.All(shapes, c => Assert.Equal(expected,
                Assert.Single(scene.Materials.ToArray(), m => m.Id == c.Material).Restitution.Value));
        }
        await h.Run(); await h.Advance(120); await h.Reset();
        Assert.Equal(construction, h.Construction);
    }
}
