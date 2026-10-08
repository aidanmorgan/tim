using System;

namespace CuriousContraptions.Physics;

/// <summary>Absolute quadrature tolerances: seconds, N s and joules.
/// Error estimates are adaptive Simpson estimates, not rigorous enclosures.</summary>
public sealed record GasTransitAccuracy
{
    public double Time { get; }
    public double Impulse { get; }
    public double Energy { get; }
    public int MaximumEvaluations { get; }

    public GasTransitAccuracy(double time, double impulse, double energy, int maximumEvaluations)
    {
        IdealGasMaterial.RequirePositive(time, nameof(time));
        IdealGasMaterial.RequirePositive(impulse, nameof(impulse));
        IdealGasMaterial.RequirePositive(energy, nameof(energy));
        if (maximumEvaluations < 5 || maximumEvaluations > 1000000)
            throw new ArgumentOutOfRangeException(nameof(maximumEvaluations));
        Time = time;
        Impulse = impulse;
        Energy = energy;
        MaximumEvaluations = maximumEvaluations;
    }
}

/// <summary>Finite rigid-reservoir discharge to an explicitly specified inventory.
/// Momentum and kinetic energy are in the stationary nozzle frame.
/// No world mutation, moving-source reaction or ambient sink is inferred.</summary>
public sealed record GasNozzleTransit
{
    public AdiabaticGasDischarge Discharge { get; }
    public double Duration { get; }
    public double MomentumImpulse { get; }
    public double PressureImpulse { get; }
    public double KineticEnergy { get; }
    public double OutletThermalEnthalpy => Discharge.OutletEnthalpy - KineticEnergy;
    public double ThrustImpulse => MomentumImpulse + PressureImpulse;
    public double TimeErrorEstimate { get; }
    public double MomentumErrorEstimate { get; }
    public double PressureErrorEstimate { get; }
    public double EnergyErrorEstimate { get; }
    public int Evaluations { get; }

    private GasNozzleTransit(AdiabaticGasDischarge discharge, Integral value, Integral error, int evaluations)
    {
        value.Validate();
        error.Validate();
        if (value.Kinetic > discharge.OutletEnthalpy)
            throw new InvalidOperationException("Nozzle kinetic energy exceeds transported total enthalpy.");
        Discharge = discharge;
        Duration = value.Time;
        MomentumImpulse = value.Momentum;
        PressureImpulse = value.Pressure;
        KineticEnergy = value.Kinetic;
        TimeErrorEstimate = error.Time;
        MomentumErrorEstimate = error.Momentum;
        PressureErrorEstimate = error.Pressure;
        EnergyErrorEstimate = error.Kinetic;
        Evaluations = evaluations;
    }

    public static GasNozzleTransit ToInventory(ConvergingGasNozzle nozzle, SealedGasState source,
        double backPressure, double remainingMass, GasTransitAccuracy accuracy)
    {
        ArgumentNullException.ThrowIfNull(nozzle);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accuracy);
        var initialFlow = nozzle.Evaluate(source, backPressure);
        var discharge = AdiabaticGasDischarge.Propose(source, remainingMass);
        if (remainingMass == source.Mass) return new(discharge, default, default, 0);
        if (initialFlow.Regime == GasNozzleRegime.NoFlow)
            throw new ArgumentException("A closed or balanced nozzle cannot discharge inventory.");
        var finalFlow = nozzle.Evaluate(discharge.After, backPressure);
        var gamma = source.Material.HeatCapacityRatio;
        var equilibriumRatio = backPressure == 0 ? 0 : Math.Pow(backPressure / source.Pressure, 1 / gamma);
        var remainingRatio = remainingMass / source.Mass;
        if (remainingRatio < equilibriumRatio)
            throw new ArgumentException("Requested discharge crosses pressure equalization.");
        // m/m0 = equilibriumRatio + s² regularizes dt/dm at equalization.
        var low = Math.Sqrt(remainingRatio - equilibriumRatio);
        var high = Math.Sqrt(1 - equilibriumRatio);
        if (!(high > low)) throw new ArgumentException("Transit interval is below numerical resolution.");
        var evaluations = 0;
        Integral Sample(double s)
        {
            if (++evaluations > accuracy.MaximumEvaluations)
                throw new InvalidOperationException("Nozzle quadrature evaluation budget exceeded.");
            if (s == 0)
            {
                // Continuous limit of 2*m0*s / massRate for subsonic discharge.
                var coefficient = nozzle.Area * Math.Sqrt(2 * gamma / (gamma - 1) * source.Density * source.Pressure);
                var slope = (gamma - 1) * Math.Pow(equilibriumRatio, gamma - 2);
                var time = 2 * source.Mass / (coefficient * equilibriumRatio * Math.Sqrt(slope));
                var limit = new Integral(time, 0, 0, 0);
                limit.Validate();
                return limit;
            }
            var state = s == high ? source : s == low ? discharge.After :
                AdiabaticGasDischarge.Propose(source, source.Mass * (equilibriumRatio + s * s)).After;
            var flow = s == high ? initialFlow : s == low ? finalFlow : nozzle.Evaluate(state, backPressure);
            if (flow.Regime == GasNozzleRegime.NoFlow)
                throw new InvalidOperationException("Positive transit coordinate has no representable nozzle flow.");
            var massDerivative = 2 * source.Mass * s;
            var timeDerivative = massDerivative / flow.MassRate;
            var value = new Integral(timeDerivative, massDerivative * flow.ExitSpeed,
                flow.PressureForce * timeDerivative, massDerivative * (.5 * flow.ExitSpeed * flow.ExitSpeed));
            value.Validate();
            return value;
        }
        static Integral Simpson(double left, double right, Integral a, Integral m, Integral b) =>
            (a + m.Scale(4) + b).Scale((right - left) / 6);
        (Integral Value, Integral Error) Refine(double left, double right, Integral a, Integral middle,
            Integral b, Integral coarse, double allocation, int depth)
        {
            var centre = (left + right) * .5;
            var lm = (left + centre) * .5;
            var rm = (centre + right) * .5;
            if (depth == 32 || lm == left || rm == right || allocation == 0)
                throw new InvalidOperationException("Nozzle quadrature refinement exhausted numerical resolution.");
            var fl = Sample(lm);
            var fr = Sample(rm);
            var first = Simpson(left, centre, a, fl, middle);
            var second = Simpson(centre, right, middle, fr, b);
            var fine = first + second;
            var correction = (fine - coarse).Scale(1.0 / 15);
            var error = correction.Absolute();
            if (error.Time <= accuracy.Time * allocation &&
                error.Momentum <= accuracy.Impulse * allocation &&
                error.Pressure <= accuracy.Impulse * allocation &&
                error.Kinetic <= accuracy.Energy * allocation)
                return (fine + correction, error);
            var l = Refine(left, centre, a, fl, middle, first, allocation * .5, depth + 1);
            var r = Refine(centre, right, middle, fr, b, second, allocation * .5, depth + 1);
            return (l.Value + r.Value, l.Error + r.Error);
        }
        (Integral Value, Integral Error) Segment(double left, double right, double allocation)
        {
            var a = Sample(left);
            var m = Sample((left + right) * .5);
            var b = Sample(right);
            return Refine(left, right, a, m, b, Simpson(left, right, a, m, b), allocation, 0);
        }
        var criticalRatio = Math.Pow(2 / (gamma + 1), gamma / (gamma - 1));
        var sonicRatio = equilibriumRatio / Math.Pow(criticalRatio, 1 / gamma);
        (Integral Value, Integral Error) result;
        if (sonicRatio > remainingRatio && sonicRatio < 1)
        {
            var split = Math.Sqrt(sonicRatio - equilibriumRatio);
            var lower = Segment(low, split, .5);
            var upper = Segment(split, high, .5);
            result = (lower.Value + upper.Value, lower.Error + upper.Error);
        }
        else result = Segment(low, high, 1);
        if (result.Value.Time <= 0 || result.Value.Momentum <= 0 || result.Value.Kinetic <= 0)
            throw new InvalidOperationException("Positive nozzle transit is below numerical resolution.");
        return new(discharge, result.Value, result.Error, evaluations);
    }


    /// <summary>Solve the finite transit for a requested elapsed time. The caller declares
    /// an admissible lower inventory bracket; crossing it rejects, never clamps or drops time.
    /// Root and quadrature error estimates share the explicit time/impulse/energy allowances.</summary>
    public static GasNozzleTransit AtDuration(ConvergingGasNozzle nozzle, SealedGasState source,
        double backPressure, double duration, double minimumMass, GasTransitAccuracy accuracy)
    {
        ArgumentNullException.ThrowIfNull(nozzle);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accuracy);
        if (!double.IsFinite(duration) || duration < 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        IdealGasMaterial.RequirePositive(minimumMass, nameof(minimumMass));
        if (minimumMass > source.Mass) throw new ArgumentOutOfRangeException(nameof(minimumMass));
        var initial = nozzle.Evaluate(source, backPressure);
        if (duration == 0 || initial.Regime == GasNozzleRegime.NoFlow)
            return new(AdiabaticGasDischarge.Propose(source, source.Mass), new(duration, 0, 0, 0), default, 0);
        var timeAllowance = Math.Min(accuracy.Time, Math.Min(
            .5 * accuracy.Impulse / Math.Max(initial.MomentumRate, initial.PressureForce),
            .5 * accuracy.Energy / initial.EnthalpyPower));
        IdealGasMaterial.RequirePositive(timeAllowance, nameof(accuracy));
        var evaluations = 0;
        GasNozzleTransit Evaluate(double mass)
        {
            var remaining = accuracy.MaximumEvaluations - evaluations;
            if (remaining < 5) throw new InvalidOperationException("Nozzle time inversion evaluation budget exceeded.");
            var result = ToInventory(nozzle, source, backPressure, mass,
                new(timeAllowance * .25, accuracy.Impulse * .5, accuracy.Energy * .5, remaining));
            evaluations += result.Evaluations;
            return result;
        }
        GasNozzleTransit? Accept(GasNozzleTransit candidate)
        {
            var timeError = Math.Abs(candidate.Duration - duration) + candidate.TimeErrorEstimate;
            if (timeError > timeAllowance) return null;
            // Fluxes decrease throughout rigid adiabatic blowdown. Initial rates bound
            // the extra integral uncertainty caused by the time inversion residual.
            return new(candidate.Discharge,
                new(duration, candidate.MomentumImpulse, candidate.PressureImpulse, candidate.KineticEnergy),
                new(timeError, candidate.MomentumErrorEstimate + initial.MomentumRate * timeError,
                    candidate.PressureErrorEstimate + initial.PressureForce * timeError,
                    candidate.EnergyErrorEstimate + initial.EnthalpyPower * timeError), evaluations);
        }
        var limit = Evaluate(minimumMass);
        if (duration > limit.Duration)
            throw new ArgumentOutOfRangeException(nameof(duration), "Requested time crosses the declared inventory bracket.");
        var accepted = Accept(limit);
        if (accepted is not null) return accepted;
        var low = minimumMass;
        var high = source.Mass;
        var middle = low * .5 + high * .5;
        for (var iteration = 0; iteration < 64; iteration++)
        {
            if (middle == low || middle == high)
                throw new InvalidOperationException("Nozzle time inversion exhausted numerical resolution.");
            var candidate = Evaluate(middle);
            accepted = Accept(candidate);
            if (accepted is not null) return accepted;
            if (candidate.Duration > duration) low = middle;
            else high = middle;
            // d(transit time)/dm = -1/massRate. Keep every Newton proposal
            // inside the proven inventory bracket; bisection safeguards the root.
            var rate = nozzle.Evaluate(candidate.Discharge.After, backPressure).MassRate;
            var next = middle + (candidate.Duration - duration) * rate;
            middle = double.IsFinite(next) && next > low && next < high
                ? next : low * .5 + high * .5;
        }
        throw new InvalidOperationException("Nozzle time inversion iteration budget exceeded.");
    }

    private readonly record struct Integral(double Time, double Momentum, double Pressure, double Kinetic)
    {
        public static Integral operator +(Integral a, Integral b) =>
            new(a.Time + b.Time, a.Momentum + b.Momentum, a.Pressure + b.Pressure, a.Kinetic + b.Kinetic);
        public static Integral operator -(Integral a, Integral b) =>
            new(a.Time - b.Time, a.Momentum - b.Momentum, a.Pressure - b.Pressure, a.Kinetic - b.Kinetic);
        public Integral Scale(double scale) => new(Time * scale, Momentum * scale, Pressure * scale, Kinetic * scale);
        public Integral Absolute() => new(Math.Abs(Time), Math.Abs(Momentum), Math.Abs(Pressure), Math.Abs(Kinetic));
        public void Validate()
        {
            ReadOnlySpan<double> values = stackalloc double[] { Time, Momentum, Pressure, Kinetic };
            foreach (var value in values)
                if (!double.IsFinite(value) || value < 0)
                    throw new InvalidOperationException("Nozzle integral exceeds its numerical range.");
        }
    }
}
