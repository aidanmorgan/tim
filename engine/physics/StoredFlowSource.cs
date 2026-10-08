using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Algebraic flow declaration powered by an owned finite store.
/// No rotor body or inertia is implied. Predictor integration remains required.</summary>
public sealed record StoredFlowSource
{
    public MechanicalSourceId Id { get; }
    public PhysicsBodyId StoreOwner { get; }
    public double Speed { get; }
    public MechanicalSourceRating Rating { get; }
    public TransferSpeedPath CaptureSpeed(double duration)=>new ConstantTransferSpeedPath(Speed,duration);
    public StoredFlowSource(MechanicalSourceId id,PhysicsBodyId storeOwner,double speed,MechanicalSourceRating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);
        if(!double.IsFinite(speed)||speed<0)throw new ArgumentOutOfRangeException(nameof(speed));
        Id=id;StoreOwner=storeOwner;Speed=speed;Rating=rating;
    }
}

public readonly record struct StoredFlowRequest(StoredFlowSource Source,double ForceDemand);
public readonly record struct StoredFlowAllowance(MechanicalSourceId Source,PhysicsBodyId StoreOwner,
    double Force,double Power,double Work);

/// <summary>Pure simultaneous allocation; never debits or mutates owned stores.</summary>
public static class StoredFlowAllocation
{
    public static IReadOnlyList<StoredFlowAllowance> Prepare(IReadOnlyList<StoredFlowRequest> requests,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,double duration)
    {
        ArgumentNullException.ThrowIfNull(requests);ArgumentNullException.ThrowIfNull(stores);
        if(!double.IsFinite(duration)||duration<=0)throw new ArgumentOutOfRangeException(nameof(duration));
        var ids=new HashSet<MechanicalSourceId>();
        var limited=new List<StoredFlowAllowance>(requests.Count);
        foreach(var request in requests.OrderBy(request=>request.Source?.Id.Index))
        {
            var source=request.Source;ArgumentNullException.ThrowIfNull(source);
            if(!ids.Add(source.Id))throw new ArgumentException("Duplicate stored flow source identity.");
            if(!stores.TryGetValue(source.StoreOwner,out var store)||store.Owner!=source.StoreOwner||store.Capacity<=0)
                throw new ArgumentException("Stored flow requires its owned energy reservoir.");
            var rated=request.ForceDemand*source.Rating.EffortScale(source.Speed,[request.ForceDemand]);
            var force=source.Speed==0?0:rated;
            var power=force*source.Speed;
            if(!double.IsFinite(power)||(force>0&&source.Speed>0&&power==0))
                throw new InvalidOperationException("Stored flow power exceeds supported numeric range.");
            limited.Add(new(source.Id,source.StoreOwner,force,power,0));
        }
        var result=new List<StoredFlowAllowance>(limited.Count);
        foreach(var group in limited.GroupBy(value=>value.StoreOwner).OrderBy(group=>group.Key.Index))
        {
            var energy=stores[group.Key].Energy;
            var ceiling=energy/duration;
            if(!double.IsFinite(ceiling)||(energy>0&&ceiling==0))
                throw new InvalidOperationException("Stored flow energy/time ratio exceeds supported numeric range.");
            var scale=PositiveDemandBudget.Scale(ceiling,group.Select(value=>value.Power).ToArray());
            double used=0;
            foreach(var value in group)
            {
                var force=value.Force*scale;var power=value.Power*scale;var work=power*duration;
                if(!double.IsFinite(work)||(scale>0&&value.Power>0&&(power==0||force==0||work==0)))
                    throw new InvalidOperationException("Stored flow allocation exceeds supported numeric range.");
                used+=work;
                result.Add(new(value.Source,value.StoreOwner,force,power,work));
            }
            if(!double.IsFinite(used)||used>energy)
                throw new InvalidOperationException("Stored flow allocation exceeds shared energy.");
        }
        return Array.AsReadOnly(result.OrderBy(value=>value.Source.Index).ToArray());
    }
}
