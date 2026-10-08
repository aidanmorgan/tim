using System;

namespace CuriousContraptions.Physics;

/// <summary>One finite energy reservoir with an explicit construction-time balance.
/// Electrical supply is external work; releasing the reservoir uses the shared constrained response.</summary>
public readonly record struct PhysicsEnergyStoreDeclaration
{
    public PhysicsBodyId Owner { get; }
    public double Capacity { get; }
    public double InitialEnergy { get; }
    public PhysicsEnergyStoreDeclaration(PhysicsBodyId owner,double capacity,double initialEnergy)
    {
        if(!double.IsFinite(capacity)||capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));
        if(!double.IsFinite(initialEnergy)||initialEnergy<0||initialEnergy>capacity)
            throw new ArgumentOutOfRangeException(nameof(initialEnergy));
        Owner=owner; Capacity=capacity; InitialEnergy=initialEnergy;
    }
}

/// <summary>Immutable observation of world-owned energy. Only world commands can charge or debit it.</summary>
public readonly record struct PhysicsEnergyStoreState
{
    public PhysicsBodyId Owner { get; }
    public double Capacity { get; }
    public double InitialEnergy { get; }
    public double Energy { get; }
    public double AcceptedEnergy { get; }
    public double ReleasedEnergy { get; }
    internal PhysicsEnergyStoreState(PhysicsBodyId owner,double capacity,double initialEnergy,double energy,double accepted,double released)
    {
        if(!double.IsFinite(initialEnergy)||initialEnergy<0||initialEnergy>capacity||
            !double.IsFinite(capacity)||capacity<=0||!double.IsFinite(energy)||energy<0||energy>capacity||
            !double.IsFinite(accepted)||accepted<0||!double.IsFinite(released)||released<0)
            throw new ArgumentException("Energy accounting must be finite and within its declared capacity.");
        Owner=owner; Capacity=capacity; InitialEnergy=initialEnergy; Energy=energy; AcceptedEnergy=accepted; ReleasedEnergy=released;
    }
    internal PhysicsEnergyStoreState Charged(double power,double seconds)
    {
        if(!double.IsFinite(power)||power<0)throw new ArgumentOutOfRangeException(nameof(power));
        if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
        var offered=power*seconds;
        if(!double.IsFinite(offered))throw new ArgumentOutOfRangeException(nameof(power),"Offered work exceeds numerical range.");
        var accepted=Math.Min(Capacity-Energy,offered);
        return new(Owner,Capacity,InitialEnergy,Energy+accepted,AcceptedEnergy+accepted,ReleasedEnergy);
    }
    internal PhysicsEnergyStoreState Debited(double suppliedWork)
    {
        if(!double.IsFinite(suppliedWork)||suppliedWork<0||suppliedWork>Energy)
            throw new ArgumentOutOfRangeException(nameof(suppliedWork));
        return new(Owner,Capacity,InitialEnergy,Energy-suppliedWork,AcceptedEnergy,ReleasedEnergy+suppliedWork);
    }
}
