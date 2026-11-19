using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class GasCapabilityTests
{
    private static readonly GasMaterial Material = new(2000f, 3000f, 1f, 1000f, 1f, 200000f);

    [Theory]
    [InlineData(AtmospherePreset.Standard)]
    [InlineData(AtmospherePreset.Vacuum)]
    public void PreparedSealedInventoryConservesMassAndAdiabaticEnergy(AtmospherePreset preset)
    {
        const float volume = .2f;
        var reference = IsobaricGas.Charge(Material, volume, 102325f, 300f);
        var ambient = AtmosphereBoundary.For(preset);
        var prepared = IsobaricGas.Prepare(Material, reference, ambient.Pressure, 1000f);
        Assert.Equal(reference.Mass, prepared.State.Mass);
        IsobaricGas.ValidateEquilibrium(Material, prepared.State, ambient.Pressure, 1000f);
        Assert.Throws<ArgumentException>(() => IsobaricGas.ValidateEquilibrium(Material,
            prepared.State with { Energy = prepared.State.Energy * .9f }, ambient.Pressure, 1000f));
        // Local mathematical contract: relative 0.002% allows f32 powers without accepting missing pressure work.
        Near(102325f * MathF.Pow(volume, 5f / 3f),
            (ambient.Pressure + 1000f) * MathF.Pow(prepared.State.Volume, 5f / 3f));
        // Independent equation-of-state proof: prepared internal energy must establish the selected pressure.
        Near(ambient.Pressure + 1000f, (Material.R / Material.Cv) * prepared.State.Energy / prepared.State.Volume);
        Near(reference.Energy, prepared.State.Energy + prepared.ElasticEnergyChange + prepared.BoundaryWorkOut);
        if (preset == AtmospherePreset.Vacuum)
        {
            Assert.True(prepared.State.Volume > reference.Volume);
            Assert.True(prepared.State.Energy < reference.Energy);
        }
        else Near(reference.Volume, prepared.State.Volume);
    }

    [Fact]
    public void FiniteIsobaricDischargeAccountsEnthalpyPressureWorkAndCarriedMomentum()
    {
        var initial = IsobaricGas.Charge(Material, .2f, 102325f, 300f);
        var velocity = new Vector3(2f, -3f, 1f);
        var loss = IsobaricGas.Discharge(initial, initial, 101325f, 1000f, 101325f,
            initial.Mass / 1000f, .25f, velocity);
        Near(initial.Mass * .25f, loss.MassRemoved);
        Near(initial.Mass * .75f, loss.After.Mass);
        Near(initial.Volume * .75f, loss.After.Volume);
        Near(initial.Energy * .75f, loss.After.Energy);
        Near(5000f * 300f * loss.MassRemoved, loss.EnthalpyOut);
        Near(loss.EnthalpyOut, initial.Energy - loss.After.Energy + loss.BoundaryWork + loss.ElasticReleased);
        Near(loss.MassRemoved * velocity.X, loss.MomentumOut.X);
        Near(loss.MassRemoved * velocity.Y, loss.MomentumOut.Y);
        Near(loss.MassRemoved * velocity.Z, loss.MomentumOut.Z);
        Near(.5f * loss.MassRemoved * 14f, loss.KineticEnergyOut);
        loss.After.Validate(Material);
    }

    [Theory]
    [InlineData(0f, 101325f)]
    [InlineData(1f, 102325f)]
    [InlineData(1f, 200000f)]
    public void ClosedConductanceOrNoPositivePressureDifferenceLosesNothing(float conductance, float outlet)
    {
        var initial = IsobaricGas.Charge(Material, .2f, 102325f, 300f);
        var result = IsobaricGas.Discharge(initial, initial, 101325f, 1000f, outlet, conductance, 1f, Vector3.One);
        Assert.Equal(initial, result.After);
        Assert.Equal(0f, result.MassRemoved);
        Assert.Equal(0f, result.EnthalpyOut);
        Assert.Equal(Vector3.Zero, result.MomentumOut);
    }

    [Fact]
    public void OversubscribedOutletEmptiesOnceAndLeavesTheDryRadius()
    {
        var initial = IsobaricGas.Charge(Material, .2f, 102325f, 300f);
        var result = IsobaricGas.Discharge(initial, initial, 101325f, 1000f, 0f, float.MaxValue, 1f, Vector3.Zero);
        Assert.Equal(initial.Mass, result.MassRemoved);
        Assert.Equal(GasInventoryPhase.Empty, result.After.Phase);
        Assert.Equal(0f, result.After.Mass);
        Assert.Equal(0f, result.After.Volume);
        Assert.Equal(0f, result.After.Energy);
        result.After.Validate(Material);
        Near(.0625f, IsobaricGas.OuterRadius(.0625f, result.After.Volume));
        var again = IsobaricGas.Discharge(initial, result.After, 101325f, 1000f, 0f, 1f, 1f, Vector3.Zero);
        Assert.Equal(0f, again.MassRemoved);
        Assert.Equal(0f, again.EnthalpyOut);
        Assert.Throws<ArgumentException>(() => IsobaricGas.Prepare(Material, result.After, 0f, 1000f));
    }

    [Fact]
    public void InvalidMaterialStateAndEnvironmentAreRefused()
    {
        Assert.Throws<ArgumentException>(() => (Material with { R = 0f }).Validate());
        Assert.Throws<ArgumentException>(() => (Material with { Cv = float.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (Material with { MaximumTemperature = .5f }).Validate());
        Assert.Throws<ArgumentException>(() => IsobaricGas.Charge(Material, .2f, 200001f, 300f));
        Assert.Throws<ArgumentException>(() => IsobaricGas.Charge(Material, .2f, 102325f, 1001f));
        Assert.Throws<ArgumentException>(() => new GasEnvelopeState(0f, 1f, 0f).Validate(Material));
        Assert.Throws<ArgumentOutOfRangeException>(() => AtmosphereBoundary.For((AtmospherePreset)999));
        Assert.Equal(0f, AtmosphereBoundary.For(AtmospherePreset.Vacuum).Pressure);
        Assert.Equal(1f, AtmosphereBoundary.For(AtmospherePreset.Standard).RelativePressure);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(300f)]
    public void InclusiveMaterialBoundsDoNotFaultProportionalRuntimeDepletion(float temperature)
    {
        var material = Material with { MinimumTemperature = 1, MaximumTemperature = 300 };
        var pressure = temperature == 1 ? 1000 : 102325;
        var initial = IsobaricGas.Charge(material, .2f, pressure, temperature);
        var state = initial;
        for (var step = 0; step < 600; step++)
        {
            state = IsobaricGas.Discharge(initial, state, pressure - 1000, 1000, pressure - 1000, initial.Mass / 1000, 1f / 480, Vector3.Zero).After;
            IsobaricGas.ValidateRuntime(initial, state);
        }
        Assert.Equal(default, state);
        Assert.Throws<ArgumentException>(() => IsobaricGas.Charge(material, .2f, 102325, 301));
        var partial = IsobaricGas.RemainingState(initial, initial.Mass / 2);
        Assert.Throws<ArgumentException>(() => IsobaricGas.ValidateRuntime(initial, partial with { Energy = partial.Energy * .9f }));
        Assert.Throws<ArgumentException>(() => IsobaricGas.ValidateRuntime(initial, partial with { Volume = partial.Volume * 1.1f }));
    }

    [Fact]
    public void CompleteThermalBudgetMustBeRepresentableBeforeAdmission()
    {
        var material = new GasMaterial(1, 1, 1, float.MaxValue, 1, 10);
        var overflowing = new GasStoreDeclaration(new(1), new(2), material, new(1, 1e38f, 2e38f), 1, 1, 1);
        Assert.Throws<ArgumentException>(() => overflowing.Validate());
        var admitted = overflowing with { Reference = new(1, 1e37f, 2e37f) };
        admitted.Validate();
        var initial = admitted.Prepared.State;
        var state = initial;
        for (var step = 0; step < 4; step++)
            state = IsobaricGas.Discharge(initial, state, 1, 1, 1, 1, .25f, Vector3.Zero).After;
        // Full exhaustion and conservation of the independently specified 2e37 internal + 2e37 pressure work.
        var total = IsobaricGas.Released(initial, state, 1, 1);
        Assert.Equal(default, state);
        Assert.True(float.IsFinite(total.EnthalpyOut));
        Near(4e37f, total.EnthalpyOut);
        Near(1e37f, total.BoundaryWork);
        Near(1e37f, total.ElasticReleased);
    }

    [Fact]
    public void RepresentableResidualIsRetainedAndTheLastUnrepresentableFractionIsAccounted()
    {
        var initial = new GasEnvelopeState(2, 1, 2);
        initial.Validate(new(1, 1, 1, 1, 2, 2));
        var noProgress = IsobaricGas.Discharge(initial, initial, 0, 1, 0, float.Epsilon, 1, Vector3.Zero);
        Assert.Equal(initial, noProgress.After);
        Assert.Equal(0, noProgress.MassRemoved);
        Assert.Equal(0, noProgress.EnthalpyOut);
        var residual = IsobaricGas.RemainingState(initial, 2 * float.Epsilon);
        Assert.True(residual.Mass > 0 && residual.Volume > 0 && residual.Energy > 0);
        var final = IsobaricGas.Discharge(initial, residual, 0, 1, 0, float.Epsilon, 1, Vector3.Zero);
        Assert.Equal(default, final.After);
        Assert.Equal(residual.Mass, final.MassRemoved);
        Assert.Equal(3, IsobaricGas.Released(initial, final.After, 0, 1).EnthalpyOut);
    }

    private static void Near(float expected, float actual) =>
        Assert.InRange(actual, expected - MathF.Max(1e-6f, MathF.Abs(expected) * .00002f),
            expected + MathF.Max(1e-6f, MathF.Abs(expected) * .00002f));
}
