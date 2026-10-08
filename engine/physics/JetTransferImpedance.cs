using System;

namespace CuriousContraptions.Physics;

/// <summary>Local open-jet impedance result, in newtons and watts.
/// This is not a committed source allowance or a path-work certificate.</summary>
public readonly record struct JetTransferResponse(
    double Force,double SourcePower,double ReceiverPower,double DissipatedPower);

/// <summary>Unilateral linear impedance between source flow and receiver surface
/// speed. Paired mechanical ports must apply the same force and source reaction.</summary>
public sealed record JetTransferImpedance
{
    public double Conductance { get; }
    public double MaximumForce { get; }

    public JetTransferImpedance(double conductance,double maximumForce)
    {
        if(!double.IsFinite(conductance)||conductance<0)
            throw new ArgumentOutOfRangeException(nameof(conductance));
        if(!double.IsFinite(maximumForce)||maximumForce<0)
            throw new ArgumentOutOfRangeException(nameof(maximumForce));
        Conductance=conductance;MaximumForce=maximumForce;
    }

    public JetTransferResponse Evaluate(double sourceSpeed,double receiverSpeed)
    {
        if(!double.IsFinite(sourceSpeed)||sourceSpeed<0)
            throw new ArgumentOutOfRangeException(nameof(sourceSpeed));
        if(!double.IsFinite(receiverSpeed))
            throw new ArgumentOutOfRangeException(nameof(receiverSpeed));
        if(Conductance==0||MaximumForce==0||receiverSpeed>=sourceSpeed)return default;
        var slip=sourceSpeed-receiverSpeed;
        if(!double.IsFinite(slip))
            throw new InvalidOperationException("Jet slip exceeds numeric range.");
        // Compare before multiplying: a finite capped law must not overflow
        // merely because its uncapped extrapolation is unrepresentable.
        var force=slip>=MaximumForce/Conductance?MaximumForce:
            Math.Min(MaximumForce,Conductance*slip);
        var sourcePower=force*sourceSpeed;
        var receiverPower=force*receiverSpeed;
        var dissipatedPower=force*slip;
        if(!double.IsFinite(sourcePower)||!double.IsFinite(receiverPower)||!double.IsFinite(dissipatedPower))
            throw new InvalidOperationException("Jet transfer power exceeds numeric range.");
        return new(force,sourcePower,receiverPower,dissipatedPower);
    }
}
