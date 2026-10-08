using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public sealed record RotaryCaptureTransfer
{
    public MechanicalTransferId Id { get; }
    public MechanicalTransferSource Source { get; }
    public AirJetGeometry Field { get; }
    public JetTransferImpedance Impedance { get; }
    public double WorkTolerance { get; }
    public RotaryCaptureTransfer(MechanicalTransferId id,MechanicalTransferSource source,
        AirJetGeometry field,JetTransferImpedance impedance,double workTolerance)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(impedance);
        if(!double.IsFinite(workTolerance)||workTolerance<=0||workTolerance*.25==0)
            throw new ArgumentOutOfRangeException(nameof(workTolerance));
        Id=id;Source=source;Field=field;Impedance=impedance;WorkTolerance=workTolerance;
    }

    internal bool IsSupplied(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        var flow=Source.Speed(Source.Bind(bodies,joints));
        return Source.Participates(bodies,colliders)&&flow>0&&Source.Rating.MaximumForce>0&&Source.Rating.MaximumPower>0&&
            (Source.Kind!=TransferSupplyKind.StoredFlow||stores[Source.StoredFlow.StoreOwner].Energy>0);
    }
}

/// <summary>Immutable preparation for one authoritative sampled configuration.
/// The owner must prepare again at control/field boundaries; this is not a
/// certificate that settings remain valid over a future trajectory.</summary>
public sealed record RotaryCapturePreparation
{
    public RotaryCaptureSetting Setting { get; }
    public IReadOnlyList<MechanicalTransferLoad> Transfers { get; }
    public AxialDampingLoad Damping { get; }
    internal RotaryCapturePreparation(RotaryCaptureSetting setting,MechanicalTransferLoad[] transfers,
        AxialDampingLoad damping)
    { Setting=setting;Transfers=Array.AsReadOnly(transfers);Damping=damping; }
}

/// <summary>Typed receiver declaration. The hinge identifies the physical rotor
/// and carrier; sample fields identify nozzle reaction owners. All equations and
/// current-pose calibration remain in the graphics-independent physics layer.</summary>
public sealed record RotaryCaptureDeclaration
{
    public PhysicsJointId Joint { get; }
    public RotaryCaptureMaterial Material { get; }
    public double ForceTolerance { get; }
    public IReadOnlyList<RotaryCaptureTransfer> Branches { get; }
    private static readonly CollisionVector LocalZ=new(0,0,1);
    private static readonly JetTransferImpedance Disengaged=new(0,0);
    public RotaryCaptureDeclaration(PhysicsJointId joint,RotaryCaptureMaterial material,
        IEnumerable<RotaryCaptureTransfer> branches,double forceTolerance)
    {
        ArgumentNullException.ThrowIfNull(material);ArgumentNullException.ThrowIfNull(branches);
        if(!double.IsFinite(forceTolerance)||forceTolerance<=0||forceTolerance*.125==0||
            forceTolerance>=material.CutInForce*.125||forceTolerance>=material.MaximumSpeed/material.SpeedPerForce*.125)
            throw new ArgumentOutOfRangeException(nameof(forceTolerance),"Control bands must remain distinct and representable.");
        ForceTolerance=forceTolerance;
        var owned=branches.ToArray();
        if(owned.Any(branch=>branch is null)||owned.Select(branch=>branch.Id).Distinct().Count()!=owned.Length)
            throw new ArgumentException("Rotary capture requires distinct non-null branches.",nameof(branches));
        Joint=joint;Material=material;
        Branches=Array.AsReadOnly(owned.OrderBy(branch=>branch.Id.Index).ToArray());
    }

    public RotaryCapturePreparation Prepare(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(joints);
        ArgumentNullException.ThrowIfNull(colliders);ArgumentNullException.ThrowIfNull(stores);
        if(joints.SingleOrDefault(joint=>joint.Id==Joint) is not PhysicsFrameJoint declared||
            declared.Kind!=FrameJointKind.Hinge)
            throw new ArgumentException("Rotary capture requires its owned hinge.",nameof(joints));
        var hinge=(PhysicsFrameJoint)declared.Rebind(bodies);
        var axis=hinge.LocalA.Orientation.Apply(LocalZ);
        MechanicalTransferLoad Load(RotaryCaptureTransfer branch,double pitch,JetTransferImpedance impedance)=>
            new NozzleCoupledJetReceiver(hinge.A.Id,axis,pitch).CreateLoad(
                branch.Id,branch.Source,branch.Field,impedance,branch.WorkTolerance);
        foreach(var branch in Branches)
            if(branch.Field.Body!=hinge.B.Id)
                throw new ArgumentException("Rotary capture samples must belong to the hinge carrier.");
        var validation=Branches.Select(branch=>Load(branch,Material.MaximumPitch,branch.Impedance)).ToArray();
        MechanicalTransferSource.ValidateAll(validation,bodies,joints,stores,colliders);
        var inputs=new List<RotaryCaptureBranch>();
        var supplied=new HashSet<MechanicalTransferId>();
        foreach(var branch in Branches)
        {
            var flow=branch.Source.Speed(branch.Source.Bind(bodies,joints));
            if(!branch.IsSupplied(bodies,joints,stores,colliders))continue;
            supplied.Add(branch.Id);
            if(!branch.Field.IsExposed(bodies,colliders))continue;
            var direction=bodies[branch.Field.Source].Pose.Rotation.Apply(branch.Field.LocalDirection);
            var alignment=CollisionVector.Dot(hinge.A.Pose.Rotation.Apply(axis),direction);
            // Rotation arithmetic can round a unit-vector dot just outside its
            // mathematical interval. Normalization belongs to this geometric boundary.
            alignment=Math.Clamp(alignment,-1,1);
            var linear=new PointPowerPort(branch.Field.Body,branch.Field.Source,
                branch.Field.LocalPoint,branch.Field.LocalDirection).Bind(bodies,joints).Speed;
            inputs.Add(new(branch.Id,branch.Impedance,flow,linear,alignment));
        }
        var setting=Material.Calibrate(inputs);
        var transfers=Branches.Select(branch=>Load(branch,setting.Pitch,
            setting.State==RotaryCaptureState.Driven&&supplied.Contains(branch.Id)?branch.Impedance:Disengaged)).ToArray();
        return new(setting,transfers,setting.CreateDamping(Joint));
    }
}
