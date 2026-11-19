using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Shared constant gauge-pressure envelope law. No element, timer or directional nozzle semantics.</summary>
public static class IsobaricGas
{
    public static GasEnvelopeState Charge(GasMaterial material, float volume, float pressure, float temperature)
    {
        material.Validate();
        GasMaterial.Positive(volume); GasMaterial.Positive(pressure); GasMaterial.Positive(temperature);
        var mass = pressure / material.R / temperature * volume;
        var result = new GasEnvelopeState(mass, volume, mass * material.Cv * temperature);
        result.Validate(material);
        return result;
    }

    public static GasPreparation Prepare(GasMaterial material, GasEnvelopeState reference,
        float boundaryPressure, float gaugePressure)
    {
        reference.Validate(material);
        if (reference.Phase != GasInventoryPhase.Charged) throw new ArgumentException("Initial gas inventory must be positive.");
        var referencePressure = reference.Energy / reference.Volume * (material.R / material.Cv);
        GasMaterial.Nonnegative(boundaryPressure); GasMaterial.Positive(gaugePressure);
        var pressure = boundaryPressure + gaugePressure;
        GasMaterial.Positive(pressure);
        var volume = reference.Volume * MathF.Pow(referencePressure / pressure, 1f / material.Gamma);
        var energy = reference.Energy * MathF.Pow(reference.Volume / volume, material.R / material.Cv);
        var state = new GasEnvelopeState(reference.Mass, volume, energy);
        state.Validate(material);
        ValidateAccounting(state, boundaryPressure, gaugePressure);
        var elasticChange = gaugePressure * (volume - reference.Volume);
        var boundaryWork = reference.Energy - energy - elasticChange;
        if (!float.IsFinite(boundaryWork) || !float.IsFinite(elasticChange))
            throw new ArgumentException("Nonfinite preparation work.");
        return new(state, boundaryWork, elasticChange);
    }

    /// <summary>Admission-only check; the runtime law preserves equilibrium without per-substep residual faults.</summary>
    public static void ValidateEquilibrium(GasMaterial material, GasEnvelopeState state,
        float boundaryPressure, float gaugePressure)
    {
        state.Validate(material);
        GasMaterial.Nonnegative(boundaryPressure); GasMaterial.Positive(gaugePressure);
        var target = boundaryPressure + gaugePressure;
        GasMaterial.Positive(target);
        if (state.Phase != GasInventoryPhase.Charged)
            throw new ArgumentException("Initial compliant gas inventory must be charged.");
        var pressure = (material.R / material.Cv) * state.Energy / state.Volume;
        // Constitutive admission tolerance permits f32 adiabatic powers, not a materially different pressure.
        if (!float.IsFinite(pressure) || MathF.Abs(pressure - target) > target * .00002f)
            throw new ArgumentException("Prepared gas state does not establish the declared envelope pressure.");
    }

    public static GasDischarge Discharge(GasEnvelopeState prepared, GasEnvelopeState state, float boundaryPressure,
        float gaugePressure, float outletPressure, float conductance, float seconds, Vector3 velocity)
    {
        ValidateRuntime(prepared, state);
        GasMaterial.Nonnegative(boundaryPressure); GasMaterial.Positive(gaugePressure);
        GasMaterial.Nonnegative(outletPressure); GasMaterial.Nonnegative(conductance); GasMaterial.Positive(seconds);
        if (!float.IsFinite(velocity.X) || !float.IsFinite(velocity.Y) || !float.IsFinite(velocity.Z))
            throw new ArgumentException("Gas exhaust requires finite carrier velocity.");
        var pressure = boundaryPressure + gaugePressure;
        GasMaterial.Positive(pressure);
        var difference = MathF.Max(0f, pressure - outletPressure);
        if (state.Mass == 0f || conductance == 0f || difference == 0f)
            return new(state, 0f, 0f, 0f, 0f, Vector3.Zero, 0f);
        // Log-domain allocation preserves f32 and avoids overflow before the finite-inventory cap.
        var requestedLog = MathF.Log(conductance) + MathF.Log(difference) + MathF.Log(seconds);
        var removed = requestedLog >= MathF.Log(state.Mass) ? state.Mass : MathF.Exp(requestedLog);
        if (removed == 0f) return new(state, 0f, 0f, 0f, 0f, Vector3.Zero, 0f);
        return Allocate(prepared, state, removed, boundaryPressure, gaugePressure, velocity);
    }

    /// <summary>One current rounded representation, anchored in the strictly admitted prepared inventory.</summary>
    public static GasEnvelopeState RemainingState(GasEnvelopeState prepared, float mass)
    {
        GasMaterial.Positive(prepared.Mass); GasMaterial.Positive(prepared.Volume); GasMaterial.Positive(prepared.Energy);
        GasMaterial.Nonnegative(mass);
        if (mass > prepared.Mass) throw new ArgumentException("Finite gas inventory cannot refill.");
        if (mass == 0f) return default;
        var fraction = mass / prepared.Mass;
        var volume = prepared.Volume * fraction;
        var energy = prepared.Energy * fraction;
        // No partially zero phase: the last unrepresentable residual joins the same outgoing allocation.
        return fraction == 0f || volume == 0f || energy == 0f ? default : new(mass, volume, energy);
    }

    public static void ValidateRuntime(GasEnvelopeState prepared, GasEnvelopeState state)
    {
        if (state != RemainingState(prepared, state.Mass))
            throw new ArgumentException("Gas inventory differs from its admitted proportional representation.");
    }

    /// <summary>Cumulative thermal accounting, bounded by the complete admitted discharge.</summary>
    public static GasThermalAccounting Released(GasEnvelopeState prepared, GasEnvelopeState state,
        float boundaryPressure, float gaugePressure)
    {
        ValidateRuntime(prepared, state);
        GasMaterial.Nonnegative(boundaryPressure); GasMaterial.Positive(gaugePressure);
        var volume = prepared.Volume - state.Volume;
        var boundary = boundaryPressure * volume;
        var elastic = gaugePressure * volume;
        return new((prepared.Energy - state.Energy + boundary) + elastic, boundary, elastic);
    }

    public static void ValidateAccounting(GasEnvelopeState prepared, float boundaryPressure, float gaugePressure)
    {
        var total = Released(prepared, default, boundaryPressure, gaugePressure);
        GasMaterial.Nonnegative(total.EnthalpyOut);
        GasMaterial.Nonnegative(total.BoundaryWork);
        GasMaterial.Nonnegative(total.ElasticReleased);
    }

    private static GasDischarge Allocate(GasEnvelopeState prepared, GasEnvelopeState state, float requested,
        float boundaryPressure, float gaugePressure, Vector3 velocity)
    {
        var after = RemainingState(prepared, state.Mass - requested);
        var removed = state.Mass - after.Mass;
        var beforeWork = Released(prepared, state, boundaryPressure, gaugePressure);
        var afterWork = Released(prepared, after, boundaryPressure, gaugePressure);
        var momentum = removed * velocity;
        var kinetic = .5f * removed * velocity.LengthSquared();
        return new(after, removed, afterWork.EnthalpyOut - beforeWork.EnthalpyOut,
            afterWork.BoundaryWork - beforeWork.BoundaryWork, afterWork.ElasticReleased - beforeWork.ElasticReleased,
            momentum, kinetic);
    }

    public static float OuterRadius(float dryRadius, float gasVolume)
    {
        GasMaterial.Positive(dryRadius); GasMaterial.Nonnegative(gasVolume);
        var radius = MathF.Cbrt(dryRadius * dryRadius * dryRadius + gasVolume * (3f / (4f * MathF.PI)));
        GasMaterial.Positive(radius);
        return radius;
    }
}
