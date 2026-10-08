using System;

namespace CuriousContraptions.Physics;

/// <summary>Finite total force and power ratings for one mechanical source.
/// Pure stage allocation only: this neither owns energy nor certifies path work.</summary>
public sealed record MechanicalSourceRating
{
    public double MaximumForce { get; }
    public double MaximumPower { get; }

    public MechanicalSourceRating(double maximumForce,double maximumPower)
    {
        if(!double.IsFinite(maximumForce)||maximumForce<0)
            throw new ArgumentOutOfRangeException(nameof(maximumForce));
        if(!double.IsFinite(maximumPower)||maximumPower<0)
            throw new ArgumentOutOfRangeException(nameof(maximumPower));
        MaximumForce=maximumForce;MaximumPower=maximumPower;
    }

    /// <summary>Apply this same multiplier to every branch action, reaction and
    /// loss. Inputs are all nonnegative branch force demands for this source.</summary>
    public double EffortScale(double sourceSpeed,ReadOnlySpan<double> demands)
    {
        if(!double.IsFinite(sourceSpeed)||sourceSpeed<0)
            throw new ArgumentOutOfRangeException(nameof(sourceSpeed));
        var maximum=0.0;
        foreach(var demand in demands)
        {
            if(!double.IsFinite(demand)||demand<0)throw new ArgumentOutOfRangeException(nameof(demands));
            maximum=Math.Max(maximum,demand);
        }
        if(MaximumForce==0||MaximumPower==0)return 0;
        if(maximum==0)return 1;
        var ceiling=sourceSpeed==0?MaximumForce:Math.Min(MaximumForce,MaximumPower/sourceSpeed);
        if(ceiling==0)throw new InvalidOperationException("Positive source rating is below the supported force range.");
        return PositiveDemandBudget.Scale(ceiling,demands);
    }
}
