using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Bundle;

/// <summary>Whole-domain bounds for sums of the current univariate mass polynomials.</summary>
internal sealed class MassPolynomialDomain(IReadOnlyList<ParameterDescriptor> parameters)
{
    public void Range(MassScalar value, float minimum, float maximum, bool exclusiveMaximum = false)
    {
        var bounds = Bounds((value, 1f));
        if (bounds.Min < minimum || (exclusiveMaximum ? bounds.Max >= maximum : bounds.Max > maximum))
            throw new ArgumentException("Mass polynomial leaves its physical domain.");
    }

    public (float Min, float Max) Bounds(params (MassScalar Value, float Sign)[] terms)
    {
        float minimum = 0, maximum = 0;
        foreach (var group in terms.GroupBy(term => term.Value.Parameter))
        {
            var polynomial = new MassScalar(group.Key, group.Sum(t => t.Sign * t.Value.C0),
                group.Sum(t => t.Sign * t.Value.C1), group.Sum(t => t.Sign * t.Value.C2),
                group.Sum(t => t.Sign * t.Value.C3));
            var domain = group.Key is { } key ? Parameter(key) : default;
            var values = new Dictionary<ParameterId, float>();
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            foreach (var x in Extrema(polynomial, domain.Minimum, domain.Maximum))
            {
                if (group.Key is { } parameter) values[parameter] = x;
                float value = 0;
                foreach (var term in group) value += term.Sign * term.Value.Evaluate(values);
                if (!float.IsFinite(value)) throw new ArgumentException("Mass-domain overflow.");
                low = MathF.Min(low, value); high = MathF.Max(high, value);
            }
            minimum += low; maximum += high;
        }
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum)) throw new ArgumentException("Mass-domain overflow.");
        return (minimum, maximum);
    }

    private ParameterDescriptor Parameter(ParameterId key)
    {
        var matches = parameters.Where(p => p.Key == key.Value && p.Kind == ParameterKind.Real).ToArray();
        if (matches.Length != 1) throw new ArgumentException("Unresolved real polynomial parameter.");
        return matches[0];
    }

    private static List<float> Extrema(MassScalar polynomial, float minimum, float maximum)
    {
        var points = new List<float>(8) { minimum, maximum };
        var scale = MathF.Max(MathF.Abs(polynomial.C1), MathF.Max(MathF.Abs(polynomial.C2), MathF.Abs(polynomial.C3)));
        if (!float.IsFinite(scale)) throw new ArgumentException("Invalid mass coefficients.");
        if (scale == 0) return points;
        var a = 3 * (polynomial.C3 / scale); var b = 2 * (polynomial.C2 / scale); var c = polynomial.C1 / scale;
        void Add(float x)
        {
            if (!float.IsFinite(x) || x < minimum || x > maximum) return;
            points.Add(x);
            if (float.BitDecrement(x) >= minimum) points.Add(float.BitDecrement(x));
            if (float.BitIncrement(x) <= maximum) points.Add(float.BitIncrement(x));
        }
        if (a == 0) { if (b != 0) Add(-c / b); return points; }
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return points;
        var q = -.5f * (b + MathF.CopySign(MathF.Sqrt(discriminant), b));
        if (q == 0) Add(-b / (2 * a));
        else { Add(q / a); Add(c / q); }
        return points;
    }
}
