using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct MechanicalSourceId
{
    public int Index { get; }
    public MechanicalSourceId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public readonly record struct MechanicalTransferId
{
    public int Index { get; }
    public MechanicalTransferId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}

public enum TransferSupplyKind { Mechanical, StoredFlow }

/// <summary>One immutable source coordinate and shared force/power rating.</summary>
public sealed record MechanicalTransferSource
{
    public MechanicalSourceId Id { get; }
    public TransferSupplyKind Kind { get; }
    /// <summary>Optional owned body whose collider participation enables this supply.
    /// Changes are committed collider updates, never predicted motion thresholds.</summary>
    public PhysicsBodyId? ParticipationBody { get; init; }
    internal bool Participates(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        if(ParticipationBody is not { } id)return true;
        if(!bodies.TryGetValue(id,out var body)||body is null||body.Id!=id||
            !colliders.TryGetValue(id,out var collider)||collider.Body!=id||
            collider.Geometry is null||!Enum.IsDefined(collider.Participation))
            throw new ArgumentException("Supply participation requires an owned body and valid collider declaration.");
        return collider.Participation==CollisionParticipation.Enabled;
    }
    private readonly MechanicalPowerPort? _port;
    private readonly StoredFlowSource? _stored;
    public MechanicalPowerPort MechanicalPort=>_port??throw new InvalidOperationException("Source is not mechanical.");
    public StoredFlowSource StoredFlow=>_stored??throw new InvalidOperationException("Source is not stored flow.");
    public MechanicalSourceRating Rating { get; }
    public MechanicalTransferSource(MechanicalSourceId id,MechanicalPowerPort port,MechanicalSourceRating rating)
    {
        ArgumentNullException.ThrowIfNull(port);ArgumentNullException.ThrowIfNull(rating);
        Id=id;_port=port;Rating=rating;Kind=TransferSupplyKind.Mechanical;
    }

    public MechanicalTransferSource(StoredFlowSource stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        _stored=stored;Id=stored.Id;Rating=stored.Rating;Kind=TransferSupplyKind.StoredFlow;
    }
    internal bool SameBinding(MechanicalTransferSource other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if(Id!=other.Id||Kind!=other.Kind||ParticipationBody!=other.ParticipationBody)return false;
        return Kind switch
        {
            TransferSupplyKind.Mechanical=>MechanicalPort==other.MechanicalPort,
            TransferSupplyKind.StoredFlow=>StoredFlow.StoreOwner==other.StoredFlow.StoreOwner,
            _=>throw new InvalidOperationException("Unsupported transfer supply kind.")
        };
    }
    internal ConstraintGradient? Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints)
    {
        switch(Kind)
        {
            case TransferSupplyKind.Mechanical:return MechanicalPort.Bind(bodies,joints);
            case TransferSupplyKind.StoredFlow:
                if(!bodies.TryGetValue(StoredFlow.StoreOwner,out var owner)||owner.Id!=StoredFlow.StoreOwner)
                    throw new ArgumentException("Stored flow owner is not in the world.");
                return null;
            default:throw new InvalidOperationException("Unsupported transfer supply kind.");
        }
    }
    internal double Speed(ConstraintGradient? gradient)=>Kind switch
    {
        TransferSupplyKind.Mechanical=>(gradient??throw new ArgumentException("Mechanical source requires a gradient.")).Speed,
        TransferSupplyKind.StoredFlow=>StoredFlow.Speed,
        _=>throw new InvalidOperationException("Unsupported transfer supply kind.")
    };
    internal TransferSpeedPath CaptureSpeed(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration)=>Kind switch
    {
        TransferSupplyKind.Mechanical=>MechanicalPortSpeedPath.Capture(MechanicalPort,bodies,joints,paths),
        TransferSupplyKind.StoredFlow=>StoredFlow.CaptureSpeed(duration),
        _=>throw new InvalidOperationException("Unsupported transfer supply kind.")
    };

    public static void ValidateAll(IReadOnlyList<MechanicalTransferLoad> loads,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        ArgumentNullException.ThrowIfNull(loads);ArgumentNullException.ThrowIfNull(stores);
        if(loads.Count==0)return;
        var sources=new Dictionary<MechanicalSourceId,MechanicalTransferSource>();
        var branches=new HashSet<MechanicalTransferId>();
        foreach(var load in loads)
        {
            ArgumentNullException.ThrowIfNull(load);
            if(!branches.Add(load.Id))throw new ArgumentException("Duplicate mechanical transfer identity.");
            if(sources.TryGetValue(load.Source.Id,out var source)&&source!=load.Source)
                throw new ArgumentException("Mechanical source identity has inconsistent declarations.");
            sources[load.Source.Id]=load.Source;
            load.Validate(bodies,joints,colliders);
            if(load.Source.Kind==TransferSupplyKind.StoredFlow&&
                (!stores.TryGetValue(load.Source.StoredFlow.StoreOwner,out var store)||
                 store.Owner!=load.Source.StoredFlow.StoreOwner||store.Capacity<=0))
                throw new ArgumentException("Stored flow requires its installed energy reservoir.");
        }
    }

    public static MechanicalTransferEvaluation[] EvaluateAll(IReadOnlyList<MechanicalTransferLoad> loads,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> original,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<MechanicalSourceId,double> engagement)
    {
        ArgumentNullException.ThrowIfNull(engagement);
        ArgumentNullException.ThrowIfNull(original);ArgumentNullException.ThrowIfNull(paths);
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        ValidateAll(loads,bodies,joints,stores,colliders);
        var mechanicalIds=loads.Where(load=>load.Source.Kind==TransferSupplyKind.Mechanical)
            .Select(load=>load.Source.Id).ToHashSet();
        if(engagement.Count!=mechanicalIds.Count||engagement.Any(pair=>!mechanicalIds.Contains(pair.Key)||
            !double.IsFinite(pair.Value)||pair.Value<0||pair.Value>1))
            throw new ArgumentException("Engagement must specify one bounded multiplier per mechanical source.",nameof(engagement));
        if(loads.Count==0)return Array.Empty<MechanicalTransferEvaluation>();
        var results=new Dictionary<MechanicalTransferId,MechanicalTransferEvaluation>();
        var stored=new List<(StoredFlowSource Source,MechanicalTransferLoad[] Branches,MechanicalTransferEvaluation[] Values,double Demand)>();
        foreach(var group in loads.OrderBy(load=>load.Source.Id.Index).ThenBy(load=>load.Id.Index)
            .GroupBy(load=>load.Source.Id))
        {
            var branches=group.ToArray();
            var source=branches[0].Source;
            var evaluations=branches.Select(load=>load.EvaluateDemand(bodies,joints,colliders)).ToArray();
            if(source.Kind==TransferSupplyKind.StoredFlow)
            {
                var demand=evaluations.Sum(value=>value.Response.Force);
                if(!double.IsFinite(demand))throw new InvalidOperationException("Stored source demand exceeds numeric range.");
                stored.Add((source.StoredFlow,branches,evaluations,demand));continue;
            }
            var gradient=source.MechanicalPort.Bind(bodies,joints);
            var participants=new List<WrenchPathTerm>();
            foreach(var term in gradient.Terms)
            {
                if(!original.TryGetValue(term.Body.Id,out var body)||body is null||body.Id!=term.Body.Id||
                    !paths.TryGetValue(term.Body.Id,out var path)||path is null)
                    throw new ArgumentException("Source rating requires original bodies and captured paths.");
                participants.Add(new(body,path,new(term.Linear,term.Angular)));
            }
            var tolerance=branches.Min(load=>load.WorkTolerance)/Math.Max(1,source.Rating.MaximumForce);
            if(!double.IsFinite(tolerance)||tolerance<=0)
                throw new InvalidOperationException("Source work tolerance is below the supported numeric range.");
            // Held source Jacobian is the same one used in the paired forces.
            // Its positive unit power bounds extraction at every point of the path.
            var unit=WrenchPathWork.Measure(participants,duration,tolerance,0);
            var scale=source.Rating.EffortScale(unit.SuppliedPowerUpperBound,
                evaluations.Select(value=>value.Response.Force).ToArray());
            var allocated=ReconcileRating(evaluations.Select(value=>value.Scale(scale)).ToArray(),source.Rating,
                unit.SuppliedPowerUpperBound);
            for(var i=0;i<allocated.Length;i++)results.Add(branches[i].Id,allocated[i].Scale(engagement[source.Id]));
        }
        if(stored.Count>0)
        {
            var allocations=StoredFlowAllocation.Prepare(stored.Select(value=>new StoredFlowRequest(value.Source,value.Demand)).ToArray(),
                stores,duration).ToDictionary(value=>value.Source);
            foreach(var group in stored)
            {
                var scale=PositiveDemandBudget.Scale(allocations[group.Source.Id].Force,
                    group.Values.Select(value=>value.Response.Force).ToArray());
                var allocated=ReconcileRating(group.Values.Select(value=>value.Scale(scale)).ToArray(),
                    group.Source.Rating,group.Source.Speed);
                for(var i=0;i<allocated.Length;i++)results.Add(group.Branches[i].Id,allocated[i]);
            }
        }
        // Reconcile the actual rounded constant-work reports, not just the
        // aggregate pre-scaling estimate. Every receiver retains the same
        // reservoir multiplier; no branch or residual energy is discarded.
        foreach(var reservoir in stored.GroupBy(group=>group.Source.StoreOwner).OrderBy(group=>group.Key.Index))
        {
            var branches=reservoir.SelectMany(group=>group.Branches).OrderBy(load=>load.Source.Id.Index)
                .ThenBy(load=>load.Id.Index).ToArray();
            const int maximumCorrections=64;
            for(var correction=0;;correction++)
            {
                var work=branches.Select(load=>WrenchPathWork.Measure([],duration,load.WorkTolerance*.25,
                    results[load.Id].Response.SourcePower).Supplied).ToArray();
                var used=work.Sum();
                if(double.IsFinite(used)&&used<=stores[reservoir.Key].Energy)break;
                if(correction==maximumCorrections)
                    throw new InvalidOperationException("Stored branch work rounding exceeded its correction budget.");
                var scale=PositiveDemandBudget.Scale(stores[reservoir.Key].Energy,work);
                // Even an exact-real sum can round upward during committed
                // reduction; require a strict decrease when that sum exceeds.
                scale=Math.BitDecrement(scale);
                if(scale<=0)throw new InvalidOperationException("Stored branch work correction is below numeric range.");
                foreach(var load in branches)results[load.Id]=results[load.Id].Scale(scale);
            }
        }
        return loads.OrderBy(load=>load.Source.Id.Index).ThenBy(load=>load.Id.Index).Select(load=>results[load.Id]).ToArray();
    }

    private static MechanicalTransferEvaluation[] ReconcileRating(MechanicalTransferEvaluation[] values,
        MechanicalSourceRating rating,double unitPowerUpperBound)
    {
        const int maximumCorrections=64;
        for(var correction=0;;correction++)
        {
            var forces=values.Select(value=>value.Response.Force).ToArray();
            var powers=forces.Select(force=>force*unitPowerUpperBound).ToArray();
            var force=forces.Sum();var power=powers.Sum();
            var aggregatePower=force*unitPowerUpperBound;
            if(!double.IsFinite(force)||!double.IsFinite(power)||!double.IsFinite(aggregatePower))
                throw new InvalidOperationException("Rounded source allocation exceeds numeric range.");
            if(force<=rating.MaximumForce&&power<=rating.MaximumPower&&aggregatePower<=rating.MaximumPower)
                return values;
            if(correction==maximumCorrections)
                throw new InvalidOperationException("Source rating rounding exceeded its correction budget.");
            var scale=Math.Min(rating.EffortScale(unitPowerUpperBound,forces),
                PositiveDemandBudget.Scale(rating.MaximumPower,powers));
            scale=Math.BitDecrement(scale);
            if(scale<=0)throw new InvalidOperationException("Source rating correction is below numeric range.");
            for(var i=0;i<values.Length;i++)values[i]=values[i].Scale(scale);
        }
    }
}
