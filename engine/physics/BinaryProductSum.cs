using System;
using System.Numerics;

namespace CuriousContraptions.Physics;

/// <summary>Exact sum of finite binary64 products with at most four factors.
/// Fixed stack storage covers their complete exponent range, including subnormals.
/// Only the final scalar is rounded, including IEEE subnormal/zero results.
/// Storage covers four-factor exponents through 4096 plus fewer than 2^36
/// products (24 per participant, with an int-sized participant collection).
/// Storage is consumed by Finish.</summary>
internal ref struct BinaryProductSum
{
    private const int MinimumExponent=-4352;
    internal const int LimbCount=136;
    internal const int StorageLength=2*LimbCount;
    private Span<ulong> _positive,_negative;
    public BinaryProductSum(Span<ulong> storage)
    {
        if(storage.Length!=StorageLength)throw new ArgumentException("Invalid product accumulator storage.",nameof(storage));
        storage.Clear();_positive=storage[..LimbCount];_negative=storage[LimbCount..];
    }
    public void Add(double a,double b,double c=1,double d=1)
    {
        Span<double> factors=stackalloc double[4]{a,b,c,d};
        Span<ulong> product=stackalloc ulong[4];
        product.Clear();product[0]=1;
        var used=1;var exponent=0;var negative=false;
        foreach(var factor in factors)
            if(!double.IsFinite(factor))throw new ArgumentException("Constraint product factors must be finite.");
        if(a==0||b==0||c==0||d==0)return;
        foreach(var factor in factors)
        {
            var bits=BitConverter.DoubleToUInt64Bits(factor);
            negative^=(bits>>63)!=0;
            var encoded=(int)((bits>>52)&0x7ff);
            var significand=bits&0x000fffffffffffffUL;
            if(encoded==0)exponent-=1074;
            else {significand|=1UL<<52;exponent+=encoded-1023-52;}
            UInt128 carry=0;
            for(var i=0;i<used;i++)
            {
                var wide=(UInt128)product[i]*significand+carry;
                product[i]=(ulong)wide;carry=wide>>64;
            }
            if(carry!=0)product[used++]=(ulong)carry;
        }
        var offset=exponent-MinimumExponent;
        var index=offset/64;var shift=offset%64;
        var target=negative?_negative:_positive;
        for(var i=0;i<used;i++)
        {
            AddWord(target,index+i,product[i]<<shift);
            if(shift!=0)AddWord(target,index+i+1,product[i]>>(64-shift));
        }
    }
    private static void AddWord(Span<ulong> target,int index,ulong value)
    {
        while(value!=0)
        {
            if((uint)index>=(uint)target.Length)throw new InvalidOperationException("Constraint sum exceeds its participant range.");
            var before=target[index];target[index]=before+value;
            value=target[index]<before?1UL:0;index++;
        }
    }
    /// <summary>Exact cancellation test without consuming accumulator storage.</summary>
    public bool IsZero => _positive.SequenceEqual(_negative);

    private enum ResultRange { Binary64, Scaled }

    public double Finish() => Finish(ResultRange.Binary64).Mantissa;

    /// <summary>Rounded normalized significand and unrestricted binary exponent.
    /// Used when a subsequent quotient has a representable result although its
    /// exact numerator is outside the binary64 range. Consumes the storage.</summary>
    public (double Mantissa,int Exponent) FinishScaled() => Finish(ResultRange.Scaled);

    private (double Mantissa,int Exponent) Finish(ResultRange range)
    {
        var top=LimbCount-1;
        while(top>=0&&_positive[top]==_negative[top])top--;
        if(top<0)return (0,0);
        var negative=_negative[top]>_positive[top];
        var magnitude=negative?_negative:_positive;
        var subtract=negative?_positive:_negative;
        ulong borrow=0;
        for(var i=0;i<LimbCount;i++)
        {
            var sub=subtract[i]+borrow;var wrapped=sub<subtract[i];
            var before=magnitude[i];magnitude[i]=before-sub;
            borrow=wrapped||before<sub?1UL:0;
        }
        top=LimbCount-1;
        while(magnitude[top]==0)top--;
        var highest=top*64+63-BitOperations.LeadingZeroCount(magnitude[top]);
        var shift=range==ResultRange.Scaled?highest-52:Math.Max(highest-52,-1074-MinimumExponent);
        var word=shift/64;var bit=shift%64;
        var significand=magnitude[word]>>bit;
        if(bit!=0&&word+1<LimbCount)significand|=magnitude[word+1]<<(64-bit);
        var guardIndex=shift-1;
        var guard=(magnitude[guardIndex/64]&(1UL<<(guardIndex%64)))!=0;
        var sticky=false;
        for(var i=0;i<guardIndex/64;i++)sticky|=magnitude[i]!=0;
        var remainder=guardIndex%64;
        if(remainder!=0)sticky|=(magnitude[guardIndex/64]&((1UL<<remainder)-1))!=0;
        if(guard&&(sticky||(significand&1)!=0))significand++;
        if(range==ResultRange.Scaled)
            return (Math.ScaleB(negative?-(double)significand:significand,-52),MinimumExponent+shift+52);
        var value=Math.ScaleB((double)significand,MinimumExponent+shift);
        if(!double.IsFinite(value))
            throw new InvalidOperationException("Constraint sum is outside the representable binary64 result range.");
        return (negative?-value:value,0);
    }
}
