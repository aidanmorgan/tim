using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements.Tests;

public sealed class ParameterizedMassTests
{
    private static BundleEntry Entry() => BundleSchema.Entry(new Fixture
    {
        KinematicMode = KinematicMode.Dynamic,
        Footprint = new(1, 1, 0, 0),
        Parameters = [
            new("length_input", ParameterUnit.Metre, ParameterKind.Real, .125f, 2f, .5f, []),
            new("load_input", ParameterUnit.Kilogram, ParameterKind.Real, .25f, 8f, 2f, []),
            new("acceleration_input", ParameterUnit.MetrePerSecondSquared, ParameterKind.Real, 0f, 16f, 8f, []),
            new("damping_input", ParameterUnit.InverseSecond, ParameterKind.Real, 0f, .125f, .04f, [])
        ],
        MassFrame = new(new(new ParameterId("load_input"), 0, 1, 0, 0), MassVector.Constant(Vector3.Zero),
            new(PrincipalMomentOperation.ScaledMassLengthSquared, default,
                new(new ParameterId("load_input"), 0), new(new ParameterId("length_input"), 0), new(.2f, .3f, .4f)),
            Quaternion.Identity, new(new ParameterId("damping_input"), 0, 1, 0, 0),
            new(new ParameterId("acceleration_input"), new(0, 16, 0), new(1, -1, 0)))
    });

    private static IReadOnlyList<BundleError> Check(BundleEntry entry) => BundleValidator.Validate([new("unfamiliar", entry)]);

    [Fact]
    public void SharedFactorMomentsAndCorrelatedObliqueAccelerationAdmitTheirWholeDomains()
    {
        var entry = Entry();
        Assert.Empty(Check(entry));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        foreach (var mass in new[] { .25f, 8f })
        foreach (var length in new[] { .125f, 2f })
        {
            var parameters = new Dictionary<ParameterId, float>
            {
                [new("load_input")] = mass, [new("length_input")] = length,
                [new("acceleration_input")] = 4, [new("damping_input")] = .04f
            };
            var frame = decoded.Mechanical!.MassFrame.Evaluate(parameters);
            Assert.Equal(new Vector3(4, 12, 0), frame.Gravity);
            Assert.Equal(.04f, frame.LinearDrag);
            var expected = new Vector3(.2f, .3f, .4f) * (mass * length * length);
            Assert.Equal(expected, frame.PrincipalMoments);
        }
        Assert.Empty(Check(entry with { Metadata = entry.Metadata with { Parameters = entry.Metadata.Parameters.Reverse().ToArray() } }));
    }

    [Fact]
    public void PhysicalDeclarationsRefuseBadDomainsReferencesAndInactiveFields()
    {
        var entry = Entry();
        var frame = entry.Mechanical!.MassFrame;
        var badFrames = new[]
        {
            frame with { PrincipalMoments = frame.PrincipalMoments with { Coefficients = new(1, 1, 3) } },
            frame with { PrincipalMoments = frame.PrincipalMoments with { Operation = (PrincipalMomentOperation)99 } },
            frame with { PrincipalMoments = frame.PrincipalMoments with { Polynomial = MassVector.Constant(Vector3.One) } },
            frame with { PrincipalMoments = frame.PrincipalMoments with { Mass = new(new ParameterId("missing"), 0) } },
            frame with { PrincipalMoments = frame.PrincipalMoments with { Length = new(new ParameterId("load_input"), 0) } },
            frame with { PrincipalMoments = frame.PrincipalMoments with { Mass = new(new ParameterId("load_input"), 2) } },
            frame with { Gravity = new(new ParameterId("acceleration_input"), new(0, 1, 0), Vector3.UnitX) },
            frame with { Gravity = new(null, Vector3.Zero, Vector3.UnitY) },
            frame with { Gravity = new(new ParameterId("length_input"), Vector3.Zero, Vector3.UnitY) },
            frame with { LinearDrag = new(new ParameterId("damping_input"), 0, 2, 0, 0) },
            frame with { LinearDrag = MassScalar.Constant(float.NaN) }
        };
        foreach (var invalid in badFrames)
            Assert.Contains(Check(entry with { Mechanical = entry.Mechanical with { MassFrame = invalid } }),
                error => error.Code == BundleErrorCode.OutOfRange);
        Assert.Empty(Check(entry with { Mechanical = entry.Mechanical with
            { MassFrame = frame with { Gravity = WorldAccelerationDeclaration.Constant(new(16, 0, 0)) } } }));
        foreach (var key in new[] { "load_input", "length_input", "acceleration_input", "damping_input" })
            Assert.True(RunControlBindings.IsBoundToNonLane(entry, key));
    }

    [Theory]
    [InlineData(MaterialComponent.Restitution, ParameterUnit.None, 1f, .7f, .7f, .3f, .04f, .5f)]
    [InlineData(MaterialComponent.Friction, ParameterUnit.None, 1f, .8f, .2f, .8f, .04f, .5f)]
    [InlineData(MaterialComponent.RollingResistance, ParameterUnit.None, .1f, .09f, .2f, .3f, .09f, .5f)]
    [InlineData(MaterialComponent.BounceThreshold, ParameterUnit.MetrePerSecond, 64f, 2.5f, .2f, .3f, .04f, 2.5f)]
    public void EachMaterialComponentBindsItsSuppliedValueAndPreservesOtherFields(MaterialComponent component,
        ParameterUnit unit, float maximum, float supplied, float restitution, float friction, float rolling, float threshold)
    {
        var original = new MaterialProperties(.2f, .3f, .04f, .5f);
        var shape = Fixture.Box(new(.5f)) with { Material = original };
        var entry = BundleSchema.Entry(new Fixture
        {
            Colliders = [shape],
            Parameters = [.. Fixture.SpringParameters, new("surface", unit, ParameterKind.Real, 0, maximum, maximum / 2, [])],
            Sliders = [Fixture.Slider(moving: [shape, shape with { Offset = new(1, 0, 0) }])],
            Footprint = new(2, 1, 1, 0)
        }) with { MaterialBindings = [new(new(1), component, new("surface"))] };
        Assert.Empty(Check(entry));
        var values = Fixture.SpringParameters.ToDictionary(p => p.Key, p => p.Default);
        values["surface"] = supplied;
        var services = new BundleServices();
        var selected = services.BodyColliders(entry, new(1), values);
        Assert.Equal(2, selected.Count);
        foreach (var collider in selected)
        {
            Assert.Equal(restitution, collider.Material.Restitution);
            Assert.Equal(friction, collider.Material.Friction);
            Assert.Equal(rolling, collider.Material.RollingResistance);
            Assert.Equal(threshold, collider.Material.BounceThreshold);
        }
        Assert.Equal(original, Assert.Single(services.BodyColliders(entry, new(0), values)).Material);
    }

    [Fact]
    public void MaterialBindingsSelectTheExactBodyAndRefuseDuplicateOrForeignWriters()
    {
        var entry = BundleSchema.Entry(new Fixture
        {
            Parameters = Fixture.SpringParameters.Append(new ParameterDescriptor("surface", ParameterUnit.None, ParameterKind.Real, 0, 1, .4f, [])).ToArray(),
            Sliders = [Fixture.Slider()], Footprint = new(2, 1, 1, 0)
        }) with { MaterialBindings = [new(new(1), MaterialComponent.Restitution, new("surface"))] };
        Assert.Empty(Check(entry));
        var services = new BundleServices();
        Assert.Equal(1f, Assert.Single(services.BodyColliders(entry, new(0))).Material.Restitution);
        Assert.Equal(.4f, Assert.Single(services.BodyColliders(entry, new(1))).Material.Restitution);
        foreach (var bindings in new MaterialParameterBinding[][]
        {
            [entry.MaterialBindings[0], entry.MaterialBindings[0]],
            [new(new(2), MaterialComponent.Restitution, new("surface"))],
            [new(new(1), (MaterialComponent)99, new("surface"))],
            [new(new(1), MaterialComponent.BounceThreshold, new("surface"))],
            [new(new(1), MaterialComponent.Restitution, new("missing"))]
        }) Assert.Contains(Check(entry with { MaterialBindings = bindings }), e => e.Code == BundleErrorCode.OutOfRange);
        Assert.True(RunControlBindings.IsBoundToNonLane(entry, "surface"));
    }
}
