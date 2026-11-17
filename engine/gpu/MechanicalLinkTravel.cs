using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Periodic distance of three f32 operands without ever forming their overflowing floating product.</summary>
public static class MechanicalLinkTravel
{
    private readonly record struct Binary(uint Mantissa, int Exponent, bool Negative);
    private static Binary Decode(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value);
        var exponent = (int)((bits >> 23) & 255);
        return new((bits & 0x7fffff) | (exponent == 0 ? 0u : 0x800000u),
            exponent == 0 ? -149 : exponent - 150, (bits >> 31) != 0);
    }

    /// <summary>Exact product modulo the canonical polygon length, rounded once to nearest-even f32; rounded period folds to zero.</summary>
    public static float Distance(float radius, float scale, float coordinate, float period)
    {
        if (!float.IsFinite(radius) || !float.IsFinite(scale) || !float.IsFinite(coordinate) ||
            !float.IsFinite(period) || period <= 0f)
            throw new ArgumentException("Periodic distance requires finite operands and a positive period.");
        var a = Decode(radius); var b = Decode(scale); var c = Decode(coordinate); var p = Decode(period);
        var numerator = (UInt128)a.Mantissa * b.Mantissa * c.Mantissa; // At most 72 bits.
        if (numerator == 0) return 0f;
        var exponent = a.Exponent + b.Exponent + c.Exponent;
        var negative = a.Negative ^ b.Negative ^ c.Negative;
        var shift = exponent - p.Exponent;
        if (shift >= 0)
        {
            numerator %= p.Mantissa;
            // At most 565 iterations for finite f32 inputs. Each remainder is < 2^24.
            for (var i = 0; i < shift; i++) numerator = (numerator << 1) % p.Mantissa;
            if (negative && numerator != 0) numerator = p.Mantissa - numerator;
            exponent = p.Exponent;
        }
        else
        {
            var modulusBits = 32 - BitOperations.LeadingZeroCount(p.Mantissa) - shift;
            if (modulusBits > 128)
            {
                // Product is < 2^72 while the modulus exceeds 2^128: no positive wrap.
                // Its negative complement rounds to the period, which canonically folds to zero.
                if (negative) return 0f;
            }
            else
            {
                var modulus = (UInt128)p.Mantissa << -shift;
                numerator %= modulus;
                if (negative && numerator != 0) numerator = modulus - numerator;
            }
        }
        var rounded = Round(numerator, exponent);
        return rounded >= period ? 0f : rounded;
    }

    private static float Round(UInt128 integer, int exponent)
    {
        if (integer == 0) return 0f;
        var bits = 128 - (int)UInt128.LeadingZeroCount(integer);
        var targetExponent = Math.Max(-149, exponent + bits - 24);
        var shift = targetExponent - exponent;
        if (shift >= 128) return 0f;
        if (shift > 0)
        {
            var whole = integer >> shift;
            var remainder = integer - (whole << shift);
            var half = (UInt128)1 << (shift - 1);
            if (remainder > half || (remainder == half && (whole & 1) != 0)) whole++;
            integer = whole;
        }
        else targetExponent = exponent;
        return MathF.ScaleB((float)integer, targetExponent);
    }
}
