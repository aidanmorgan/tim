using System;

namespace CuriousContraptions.Physics;

/// <summary>Unilateral spring with a finite force-producing stroke. Beyond the
/// stroke its force is constant, so potential remains continuous and grows linearly.</summary>
public sealed record CompressionSpringPotential
{
    public double Stiffness { get; }
    public double MaximumStroke { get; }
    public CompressionSpringPotential(double stiffness,double maximumStroke)
    {
        if(!double.IsFinite(stiffness)||stiffness<=0)throw new ArgumentOutOfRangeException(nameof(stiffness));
        if(!double.IsFinite(maximumStroke)||maximumStroke<=0)throw new ArgumentOutOfRangeException(nameof(maximumStroke));
        Stiffness=stiffness;MaximumStroke=maximumStroke;
    }
    public double Energy(double compression)
    {
        if(!double.IsFinite(compression))throw new ArgumentOutOfRangeException(nameof(compression));
        var depth=Math.Max(0,compression);var elastic=Math.Min(depth,MaximumStroke);
        var energy=Stiffness*(.5*elastic*elastic+MaximumStroke*(depth-elastic));
        if(!double.IsFinite(energy))throw new ArgumentOutOfRangeException(nameof(compression));
        return energy;
    }
    public double IntervalForce(double first,double last)
    {
        if(!double.IsFinite(first)||!double.IsFinite(last))throw new ArgumentOutOfRangeException(nameof(first));
        var low=Math.Min(first,last);var high=Math.Max(first,last);
        var mean=0d;
        if(low==high)mean=Math.Clamp(low,0,MaximumStroke);
        else if(low>=MaximumStroke)mean=MaximumStroke;
        else if(high>0)
        {
            var width=high-low;
            if(!double.IsFinite(width))throw new ArgumentOutOfRangeException(nameof(last));
            var begin=Math.Max(0,low);var end=Math.Min(high,MaximumStroke);
            if(end>begin)mean=(.5*begin+.5*end)*((end-begin)/width);
            mean+=MaximumStroke*(Math.Max(0,high-Math.Max(low,MaximumStroke))/width);
        }
        var force=Stiffness*mean;
        if(!double.IsFinite(force))throw new ArgumentOutOfRangeException(nameof(last));
        return force;
    }
}
