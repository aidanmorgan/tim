using System;

namespace CuriousContraptions.Physics;

/// <summary>Completed simultaneous impulse-correction work. Failed solves do
/// not publish counters; wall-clock stage measurements still include their cost.</summary>
public readonly record struct ImpulseCorrectionWork(long Factorizations,long Trials,int MaximumCoordinates)
{
    internal ImpulseCorrectionWork Add(ImpulseCorrectionWork other)
    {
        Validate();other.Validate();
        return new(checked(Factorizations+other.Factorizations),checked(Trials+other.Trials),
            Math.Max(MaximumCoordinates,other.MaximumCoordinates));
    }
    public void Validate()
    {
        if(Factorizations<0||Trials<Factorizations||MaximumCoordinates<0||
            Factorizations==0&&(Trials!=0||MaximumCoordinates!=0)||
            Factorizations>0&&(MaximumCoordinates==0||(Trials-1)/32>=Factorizations))
            throw new ArgumentOutOfRangeException(nameof(ImpulseCorrectionWork));
    }
}
