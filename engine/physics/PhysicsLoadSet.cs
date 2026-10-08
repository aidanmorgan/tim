using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Immutable complete set of state-dependent force declarations.
/// Init accessors copy caller collections; with-expressions share only immutable
/// declarations. The world validates the entire set before publishing it.</summary>
public sealed record PhysicsLoadSet
{
    private readonly IReadOnlyList<AxialEffortLoad> _efforts=Array.Empty<AxialEffortLoad>();
    public IReadOnlyList<AxialEffortLoad> Efforts
    {
        get=>_efforts;
        init=>_efforts=Copy(value);
    }
    private readonly IReadOnlyList<AxialElasticLoad> _elastic=Array.Empty<AxialElasticLoad>();
    public IReadOnlyList<AxialElasticLoad> Elastic
    {
        get=>_elastic;
        init=>_elastic=Copy(value);
    }
    private readonly IReadOnlyList<AxialGasLoad> _gas=Array.Empty<AxialGasLoad>();
    public IReadOnlyList<AxialGasLoad> Gas
    {
        get=>_gas;
        init=>_gas=Copy(value);
    }
    private readonly IReadOnlyList<AxialDampingLoad> _damping=Array.Empty<AxialDampingLoad>();
    public IReadOnlyList<AxialDampingLoad> Damping
    {
        get=>_damping;
        init=>_damping=Copy(value);
    }
    private readonly IReadOnlyList<BodyDragLoad> _drag=Array.Empty<BodyDragLoad>();
    public IReadOnlyList<BodyDragLoad> Drag
    {
        get=>_drag;
        init=>_drag=Copy(value);
    }
    private readonly IReadOnlyList<CompliantContactLoad> _compliant=Array.Empty<CompliantContactLoad>();
    public IReadOnlyList<CompliantContactLoad> Compliant
    {
        get=>_compliant;
        init=>_compliant=Copy(value);
    }
    private readonly IReadOnlyList<PlanarGuideLoad> _guides=Array.Empty<PlanarGuideLoad>();
    public IReadOnlyList<PlanarGuideLoad> Guides
    {
        get=>_guides;
        init=>_guides=Copy(value);
    }
    private readonly IReadOnlyList<LatchedSpringLoad> _springs=Array.Empty<LatchedSpringLoad>();
    public IReadOnlyList<LatchedSpringLoad> Springs
    {
        get=>_springs;
        init=>_springs=Copy(value);
    }
    private readonly IReadOnlyList<MechanicalTransferLoad> _transfers=Array.Empty<MechanicalTransferLoad>();
    public IReadOnlyList<MechanicalTransferLoad> Transfers
    {
        get=>_transfers;
        init=>_transfers=Copy(value);
    }
    private readonly IReadOnlyList<RotaryCaptureDeclaration> _rotary=Array.Empty<RotaryCaptureDeclaration>();
    public IReadOnlyList<RotaryCaptureDeclaration> Rotary
    {
        get=>_rotary;
        init=>_rotary=Copy(value);
    }

    internal IEnumerable<MechanicalTransferSource> TransferSources=>
        Transfers.Select(load=>load.Source).Concat(Rotary.SelectMany(load=>load.Branches.Select(branch=>branch.Source)));
    internal double TransferWorkTolerance=>Transfers.Select(load=>load.WorkTolerance)
        .Concat(Rotary.SelectMany(load=>load.Branches.Select(branch=>branch.WorkTolerance))).DefaultIfEmpty(0).Min();

    internal PhysicsLoadSet PrepareRotary(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores)
    {
        if(Rotary.Count==0)return this;
        if(Rotary.Select(load=>load.Joint).Distinct().Count()!=Rotary.Count)
            throw new ArgumentException("Duplicate rotary capture hinge identity.");
        var prepared=Rotary.OrderBy(load=>load.Joint.Index).Select(load=>load.Prepare(bodies,joints,colliders,stores)).ToArray();
        var result=this with
        {
            Rotary=[],
            Transfers=Transfers.Concat(prepared.SelectMany(load=>load.Transfers)).OrderBy(load=>load.Id.Index).ToArray(),
            Damping=Damping.Concat(prepared.Select(load=>load.Damping)).ToArray()
        };
        MechanicalTransferSource.ValidateAll(result.Transfers,bodies,joints,stores,colliders);
        return result;
    }

    public bool IsEmpty=>Gas.Count==0&&Rotary.Count==0&&Transfers.Count==0&&Springs.Count==0&&Efforts.Count==0&&Elastic.Count==0&&Damping.Count==0&&Drag.Count==0&&Compliant.Count==0&&Guides.Count==0;

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values) where T:class
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied=values.ToArray();
        foreach(var value in copied)ArgumentNullException.ThrowIfNull(value);
        return Array.AsReadOnly(copied);
    }
}
