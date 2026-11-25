using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.BundleElements.Tests;

public sealed class InventoryMassTests
{
    private static BundleEntry Entry()
    {
        var entry = FixtureBake.Entry(new Fixture
        {
            KinematicMode = KinematicMode.Dynamic, Footprint = new(1, 1, 0, 0),
            MassFrame = new(new(BodyMassSourceKind.CenteredInventory, MassScalar.Constant(2), default,
                [new(41), new(9)], new(.2f, .3f, .4f)), MassVector.Constant(Vector3.Zero),
                Quaternion.Identity, MassScalar.Constant(.4f), WorldAccelerationDeclaration.Constant(Vector3.Zero))
        });
        var material = new GasMaterial(2000, 3000, 1, 1000, 1, 200000);
        return entry with { Mechanical = entry.Mechanical! with { GasStores = [
            new(new(9), new(0), material, MassScalar.Constant(.2f), 102325, 300, 1000, .001f),
            new(new(41), new(0), material, MassScalar.Constant(.3f), 102325, 300, 1000, .002f)] } };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactReorderedStoresResolveOneMassAndAnisotropicDryInertia(bool reverse)
    {
        var entry = Entry();
        if (reverse) entry = entry with { Mechanical = entry.Mechanical! with { GasStores = entry.Mechanical.GasStores.Reverse().ToArray() } };
        Assert.Empty(BundleValidator.Validate([new("unfamiliar", entry)]));
        var mechanical = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements).Mechanical!;
        var parameters = new Dictionary<ParameterId, float>();
        var context = GasMassReferences.Resolve(new(0), mechanical.GasStores, parameters);
        var resolved = mechanical.MassFrame.Source.Resolve(parameters, context);
        var dry = 2f - 102325f / 2000f / 300f * .5f;
        Assert.Equal(2f, resolved.InitialMass);
        // Local mass algebra: this margin excludes omission of either finite inventory, not a trajectory golden.
        Assert.InRange(MathF.Abs(resolved.DryMass - dry), 0, .000001f);
        Assert.InRange(Vector3.Distance(resolved.PrincipalMoments, new Vector3(.2f, .3f, .4f) * dry), 0, .000001f);
    }

    [Fact]
    public void MissingForeignDuplicateAndInactiveMassReferencesRefuse()
    {
        var entry = Entry();
        var source = entry.Mechanical!.MassFrame.Source;
        foreach (var invalid in new[] {
            source with { Stores = [new(41), new(41)] },
            source with { Stores = [new(99)] },
            source with { DeclaredMoments = PrincipalMomentDeclaration.Constant(Vector3.One) },
            source with { DryMomentPerMass = new(1, 1, 3) },
            source with { InitialMass = MassScalar.Constant(.001f) },
            source with { Kind = (BodyMassSourceKind)99 }
        })
        {
            var changed = entry with { Mechanical = entry.Mechanical with { MassFrame = entry.Mechanical.MassFrame with { Source = invalid } } };
            Assert.Contains(BundleValidator.Validate([new("invalid", changed)]), e => e.Code == BundleErrorCode.OutOfRange);
        }
        var foreign = entry with { Mechanical = entry.Mechanical with
            { GasStores = entry.Mechanical.GasStores.Select(s => s with { Body = new(1) }).ToArray() } };
        Assert.Contains(BundleValidator.Validate([new("foreign", foreign)]), e => e.Code == BundleErrorCode.OutOfRange);
    }

    [Fact]
    public void EntireParameterDomainMustRetainPositiveDryMass()
    {
        var entry = Entry();
        var source = entry.Mechanical!.MassFrame.Source with { InitialMass = new(new ParameterId("total"), 0, 1, 0, 0) };
        entry = entry with {
            Metadata = entry.Metadata with { Parameters = [new("total", ParameterUnit.Kilogram, ParameterKind.Real, .01f, 2, 2, [])] },
            Mechanical = entry.Mechanical with { MassFrame = entry.Mechanical.MassFrame with { Source = source } }
        };
        Assert.Contains(BundleValidator.Validate([new("depleted", entry)]), e => e.Code == BundleErrorCode.OutOfRange);
    }
}
