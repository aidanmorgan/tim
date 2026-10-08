using System;
using System.Numerics;

namespace CuriousContraptions.Physics;

internal static class PositiveDemandBudget
{
    internal static double Scale(double ceiling,ReadOnlySpan<double> demands)
    {
        if(!double.IsFinite(ceiling)||ceiling<0)throw new ArgumentOutOfRangeException(nameof(ceiling));
        var maximum=0.0;
        foreach(var demand in demands)
        {
            if(!double.IsFinite(demand)||demand<0)throw new ArgumentOutOfRangeException(nameof(demands));
            maximum=Math.Max(maximum,demand);
        }
        if(ceiling==0)return 0;
        if(maximum==0)return 1;
        // Canonical numeric reduction makes the common multiplier independent
        // of declaration order. Normalize before summation to avoid overflow.
        var ordered=demands.ToArray();Array.Sort(ordered);
        var sumUpper=0.0;
        foreach(var demand in ordered)
        {
            if(demand==0)continue;
            var normalized=DivideUp(demand,maximum);
            if(normalized==0)throw new InvalidOperationException("Source demand ratio is below the supported numeric range.");
            var sum=sumUpper+normalized;
            var added=sum-sumUpper;
            var error=(sumUpper-(sum-added))+(normalized-added);
            sumUpper=error>0?Math.BitIncrement(sum):sum;
        }
        // Upper bounds keep rounding from granting capacity beyond the rating.
        if(maximum<=DivideDown(ceiling,sumUpper))return 1;
        var scale=DivideDown(DivideDown(ceiling,maximum),sumUpper);
        if(!double.IsFinite(scale)||scale<=0)
            throw new InvalidOperationException("Source allocation is below the supported numeric range.");
        foreach(var demand in ordered)
            if(demand>0&&demand*scale==0)
                throw new InvalidOperationException("Allocated branch force is below the supported numeric range.");
        return Math.Min(1,scale);
    }

    // Compare the exact binary product with the numerator. Integer significands
    // avoid an FMA residual underflow near the subnormal boundary.
    private static int CompareProduct(double left,double right,double target)
    {
        static (ulong Significand,int Exponent) Decode(double value)
        {
            var bits=BitConverter.DoubleToUInt64Bits(value);
            var exponent=(int)((bits>>52)&0x7ff);
            var significand=bits&0x000fffffffffffffUL;
            return exponent==0?(significand,-1074):(significand|0x0010000000000000UL,exponent-1075);
        }
        static int Width(UInt128 value)
        {
            var high=(ulong)(value>>64);
            return high!=0?128-BitOperations.LeadingZeroCount(high):64-BitOperations.LeadingZeroCount((ulong)value);
        }
        var a=Decode(left);var b=Decode(right);var c=Decode(target);
        var product=(UInt128)a.Significand*b.Significand;
        var expected=(UInt128)c.Significand;
        if(product==0||expected==0)return product.CompareTo(expected);
        var productExponent=a.Exponent+b.Exponent;
        var highComparison=(Width(product)+productExponent).CompareTo(Width(expected)+c.Exponent);
        if(highComparison!=0)return highComparison;
        // Equal high bits bound the alignment shift by the significand widths.
        if(productExponent>c.Exponent)product<<=productExponent-c.Exponent;
        else expected<<=c.Exponent-productExponent;
        return product.CompareTo(expected);
    }
    private static double DivideDown(double numerator,double denominator)
    {
        var quotient=numerator/denominator;
        if(double.IsPositiveInfinity(quotient))return double.MaxValue;
        return CompareProduct(quotient,denominator,numerator)>0?Math.BitDecrement(quotient):quotient;
    }
    private static double DivideUp(double numerator,double denominator)
    {
        var quotient=numerator/denominator;
        if(numerator>0&&quotient==0)throw new InvalidOperationException("Source demand ratio is below the supported numeric range.");
        return CompareProduct(quotient,denominator,numerator)<0?Math.BitIncrement(quotient):quotient;
    }
}
