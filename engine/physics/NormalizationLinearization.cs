using System;

namespace CuriousContraptions.Physics;

/// <summary>Writes the two coefficients of A*n(q) for every residual row.
/// The spans are ephemeral, cleared before each call, and must not escape.</summary>
public delegate (double U, double V) NormalizationSampleWriter(Span<double> u, Span<double> v);

/// <summary>Owned samples for the product linearization of A(x)*normalize(q(x)).
/// Other residual terms retain their original secants. No residual or physical
/// acceptance value is changed. Projection boundaries retain sampled A changes.</summary>
public sealed class NormalizationLinearization
{
    private enum Phase { Empty, Center, High, Low }
    private sealed class Sample(int dimension)
    {
        public readonly double[] U = new double[dimension];
        public readonly double[] V = new double[dimension];
        public double Q0, Q1, N0, N1, Length;
    }

    private readonly NormalizationSampleWriter _writer;
    private readonly Sample _center, _high, _low;
    private Phase _phase;
    public int Dimension { get; }

    public NormalizationLinearization(int dimension, NormalizationSampleWriter writer)
    {
        if (dimension < 1) throw new ArgumentOutOfRangeException(nameof(dimension));
        ArgumentNullException.ThrowIfNull(writer);
        Dimension = dimension;
        _writer = writer;
        _center = new(dimension); _high = new(dimension); _low = new(dimension);
    }

    private void Capture(Sample sample)
    {
        Array.Clear(sample.U); Array.Clear(sample.V);
        var (u, v) = _writer(sample.U, sample.V);
        var scale = Math.Max(Math.Abs(u), Math.Abs(v));
        if (!double.IsFinite(scale) || scale == 0)
            throw new InvalidOperationException("Normalization requires finite nonzero input.");
        var length = scale * Math.Sqrt((u / scale) * (u / scale) + (v / scale) * (v / scale));
        if (!double.IsFinite(length))
            throw new InvalidOperationException("Normalization length exceeds numeric range.");
        for (var row = 0; row < Dimension; row++)
            if (!double.IsFinite(sample.U[row]) || !double.IsFinite(sample.V[row]))
                throw new InvalidOperationException("Normalization coefficients must be finite.");
        sample.Q0 = u; sample.Q1 = v; sample.Length = length;
        sample.N0 = u / length; sample.N1 = v / length;
    }

    public void CaptureCenter()
    {
        _phase = Phase.Empty;
        Capture(_center); _phase = Phase.Center;
    }

    public void CaptureHigh()
    {
        if (_phase != Phase.Center) throw new InvalidOperationException("A center sample is required.");
        _phase = Phase.Empty;
        Capture(_high); _phase = Phase.High;
    }

    public void CaptureLow()
    {
        if (_phase != Phase.High) throw new InvalidOperationException("A high sample is required.");
        _phase = Phase.Empty;
        Capture(_low); _phase = Phase.Low;
    }

    public void CorrectColumn(double[,] jacobian, int column, double width)
    {
        ArgumentNullException.ThrowIfNull(jacobian);
        if (_phase != Phase.Low) throw new InvalidOperationException("Complete samples are required.");
        if (jacobian.GetLength(0) != Dimension || (uint)column >= (uint)jacobian.GetLength(1))
            throw new ArgumentException("Jacobian dimensions do not match the contribution.");
        if (!double.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        var widthExponent = Math.ILogB(width);
        var lengthExponent = Math.ILogB(_center.Length);
        var widthMantissa = Math.ScaleB(width,-widthExponent);
        var lengthMantissa = Math.ScaleB(_center.Length,-lengthExponent);
        Span<ulong> storage = stackalloc ulong[BinaryProductSum.StorageLength];
        for (var row = 0; row < Dimension; row++)
        {
            if (_center.U[row] == 0 && _center.V[row] == 0 &&
                _high.U[row] == 0 && _high.V[row] == 0 &&
                _low.U[row] == 0 && _low.V[row] == 0) continue;
            // Form the complete weighted numerator exactly before division.
            // Four factors suffice, including the tangential Dn contraction.
            var sum = new BinaryProductSum(storage);
            sum.Add(-_high.U[row],_high.N0,_center.Length);
            sum.Add(-_high.V[row],_high.N1,_center.Length);
            sum.Add(_low.U[row],_low.N0,_center.Length);
            sum.Add(_low.V[row],_low.N1,_center.Length);
            sum.Add(_high.U[row],_center.N0,_center.Length);
            sum.Add(-_low.U[row],_center.N0,_center.Length);
            sum.Add(_high.V[row],_center.N1,_center.Length);
            sum.Add(-_low.V[row],_center.N1,_center.Length);
            sum.Add(_center.U[row],_center.N1,_center.N1,_high.Q0);
            sum.Add(-_center.U[row],_center.N1,_center.N1,_low.Q0);
            sum.Add(-_center.U[row],_center.N1,_center.N0,_high.Q1);
            sum.Add(_center.U[row],_center.N1,_center.N0,_low.Q1);
            sum.Add(-_center.V[row],_center.N0,_center.N1,_high.Q0);
            sum.Add(_center.V[row],_center.N0,_center.N1,_low.Q0);
            sum.Add(_center.V[row],_center.N0,_center.N0,_high.Q1);
            sum.Add(-_center.V[row],_center.N0,_center.N0,_low.Q1);
            // An absent or algebraically zero correction must preserve the
            // original secant bits; do not round-trip it through a quotient.
            if (sum.IsZero) continue;
            sum.Add(jacobian[row,column],width,_center.Length);
            var numerator = sum.FinishScaled();
            var value = Math.ScaleB(numerator.Mantissa/widthMantissa/lengthMantissa,
                numerator.Exponent-widthExponent-lengthExponent);
            if (!double.IsFinite(value))
                throw new InvalidOperationException("Normalization derivative exceeds numeric range.");
            jacobian[row,column] = value;
        }
        _phase = Phase.Center;
    }
}
