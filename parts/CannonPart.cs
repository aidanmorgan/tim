using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum CannonPhase { Empty, Loading, Charging, Ready, Firing, Jammed }
public enum CannonShotResult { None, Fired, Empty, Uncharged, Unseated, Obstructed, Ambiguous, Busy, SpeedLimited, NoResponse }
public enum CannonParameter { Capacity, ChargePower }

/// <summary>Reloadable electrically charged toy launcher. The physical payload is never spawned or repositioned.</summary>
public partial class CannonPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<CannonParameter>(fields);
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_phaseState,_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(CannonPart owner) : SimulationTransactionParticipant
    {
        private CannonShotResult _lastShot, _loadState;
        private int _shotCount;
        private int? _triggerTick;
        private MachinePart? _payload, _departing, _lastPayload;
        protected override void CaptureCheckpoint()
        {
            _lastShot=owner.LastShot; _shotCount=owner.ShotCount;
            _triggerTick=owner._triggerTick; _loadState=owner._loadState;
            _payload=owner._payload; _departing=owner._departing; _lastPayload=owner.LastPayload;
        }
        protected override void RestoreCheckpoint()
        {
            owner.LastShot=_lastShot; owner.ShotCount=_shotCount;
            owner._triggerTick=_triggerTick; owner._loadState=_loadState;
            owner._payload=_payload; owner._departing=_departing; owner.LastPayload=_lastPayload;
        }
    }
    public const string CatalogId="cannon";
    public const float HalfLength=.55f;
    public const float BoreRadius=.48f;
    public const float MinimumPayloadWidth=.6f;
    public const float MaximumPayloadWidth=.84f;
    public const float MaximumLaunchSpeed=16;
    public const float MaximumRecoil=.16f;
    public const double RecoilCompressionSeconds=.08,RecoilReturnSeconds=.45;
    private static readonly Vector3 SleeveRestPosition=new(-.22f,0,0);
    public float RecoilOffset=>_recoilSleeve.Position.X-SleeveRestPosition.X;
    private MeshInstance3D _recoilSleeve=null!;
    public static readonly SceneOccurrenceSlot ShotOccurrence=new();
    private static readonly AnimationImpulseDefinition ShotPulse=new(RecoilCompressionSeconds+RecoilReturnSeconds,
        AnimationImpulseCurve.SmoothRiseFall,AnimationImpulseOverlap.HyperbolicTangentSum,
        AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,64,
        AnimationImpulseTiming.EventTimeRetainFirstFrame,RecoilCompressionSeconds/(RecoilCompressionSeconds+RecoilReturnSeconds));
    public override IReadOnlyList<SceneOccurrenceSlot> OccurrenceSources=>[ShotOccurrence];
    public override IReadOnlyList<SceneOccurrenceAnimation> OccurrenceAnimations=>
        [SceneOccurrenceAnimation.Translation(_recoilSleeve,new(this,ShotOccurrence),ShotPulse,-MaximumRecoil,AnimationTranslationAxis.X)];
    private readonly SimulationState<CannonPhase> _phaseState=new(CannonPhase.Empty);
    public CannonPhase Phase { get=>_phaseState.Value; private set=>_phaseState.Value=value; }
    public static readonly Bridge.EnumObservationSlot<CannonPhase> PhaseOutput=new(0);
    public override IReadOnlyList<SceneEnumObservation> EnumObservations=>[new SceneEnumObservation<CannonPhase>(PhaseOutput,_phaseState)];
    public CannonShotResult LastShot { get; private set; }
    public int ShotCount { get; private set; }
    public MachinePart? LastPayload { get; private set; }
    // Construction has no runtime reservoir. During Run/paused Run, never cache its ledger in the scene.
    private bool HasEnergyState=>GetParent() is MachineWorld {HasPhysicsState:true};
    private PhysicsEnergyStoreState EnergyState
    {
        get
        {
            if(GetParent() is not MachineWorld world||!world.HasPhysicsState)
                throw new InvalidOperationException("Energy state requires a captured physics world.");
            return world.Physics.EnergyStore(StoreOwner(world));
        }
    }
    private PhysicsBodyId StoreOwner(MachineWorld world)=>world.PhysicsAssembly.Body(new(this,RootBody)).Id;
    public double StoredEnergy=>HasEnergyState?EnergyState.Energy:0;
    public double AcceptedEnergy=>HasEnergyState?EnergyState.AcceptedEnergy:0;
    public double ReleasedEnergy=>HasEnergyState?EnergyState.ReleasedEnergy:0;
    public override IReadOnlyList<SceneEnergyStoreDeclaration> PhysicsEnergyStores=>
        [new(new(this,RootBody),ReadParameter(CannonParameter.Capacity),0)];
    private MachinePart? _payload,_departing;
    private int? _triggerTick;
    private CannonShotResult _loadState=CannonShotResult.Empty;
    public static readonly Bridge.ScalarObservationSlot ChargeOutput=new(0);
    public override IReadOnlyList<SceneEnergyStoreObservation> EnergyStoreObservations=>
        [new(ChargeOutput,new(this,RootBody),EnergyStoreQuantity.FillFraction)];
    public override IReadOnlyList<SceneScalarExtent> ScalarExtents=>
        [new(_chargeBar,new(this,ChargeOutput),Bridge.ScalarUnit.Dimensionless,0,1,
            new(ScalarExtentAxis.X,0,.001,0),ScalarExtentVisibility.Always)];
    private MeshInstance3D _chargeBar=null!,_flag=null!;
    private MeshInstance3D _indicator=null!;
    private static readonly EnumPresentationMap<CannonPhase,double> FlagAngles=new(new Dictionary<CannonPhase,double>
    {
        [CannonPhase.Empty]=-70*Math.PI/180,[CannonPhase.Loading]=-70*Math.PI/180,
        [CannonPhase.Charging]=-70*Math.PI/180,[CannonPhase.Ready]=0,
        [CannonPhase.Firing]=-70*Math.PI/180,[CannonPhase.Jammed]=-70*Math.PI/180
    });
    private static readonly EnumPresentationMap<CannonPhase,Color> PhaseColours=new(new Dictionary<CannonPhase,Color>
    {
        [CannonPhase.Empty]=new("#556573"),[CannonPhase.Loading]=new("#556573"),
        [CannonPhase.Charging]=new("#66b8c9"),[CannonPhase.Ready]=new("#f7cb52"),
        [CannonPhase.Firing]=new("#556573"),[CannonPhase.Jammed]=new("#556573")
    });
    public override IReadOnlyList<SceneEnumBinding> EnumBindings=>
        [new SceneEnumRotation<CannonPhase>(_flag,new(this,PhaseOutput),FlagAngles,AnimationRotationAxis.Z),
         new SceneEnumColour<CannonPhase>(_indicator,new(this,PhaseOutput),PhaseColours)];

    public override Physics.ContactMaterial InitialContactMaterial => new(0,.1,.3);
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.45f,-.55f,.55f)),
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(.15f,-.55f,.55f))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var capacity=parameters.Read(CannonParameter.Capacity);var power=parameters.Read(CannonParameter.ChargePower);
        if(!float.IsFinite(capacity)||capacity<1||capacity>100)throw new ArgumentException("Cannon capacity must be between 1 and 100.");
        if(!float.IsFinite(power)||power<1||power>100)throw new ArgumentException("Cannon charging power must be between 1 and 100.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        if(command!=ActivationCommand.Trigger)throw new ArgumentException("Unsupported cannon command.");
        // Coalesce pulses until the next fixed-tick boundary; never wait for a future load or charge.
        _triggerTick??=world.Ticks;
        return ActivationDisposition.Deferred;
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        InspectChamber(world);
        if(_triggerTick is { } tick && tick<world.Ticks)
        {
            _triggerTick=null;
            TryFire(world);
        }
    }
    private void InspectChamber(MachineWorld world)
    {
        var cannon=world.PhysicsAssembly.Body(new(this,RootBody));
        var chamber=new CylindricalRegion(HalfLength,BoreRadius);
        if(world.Physics.Collider(cannon.Id).Declaration.Participation==CollisionParticipation.Disabled)
        {_payload=null;_departing=null;_loadState=CannonShotResult.Empty;return;}
        bool Occupies(MachinePart part)
        {
            var body=world.PhysicsAssembly.Body(new(part,RootBody));
            var collider=world.Physics.Collider(body.Id).Declaration;
            return collider.Participation==CollisionParticipation.Enabled&&
                chamber.Overlaps(collider.Geometry,body.Pose,cannon.Pose);
        }
        if(_departing!=null&&!Occupies(_departing))_departing=null;
        var candidates=world.Bodies.Where(body=>body.PhysicsOwner==body).Where(Occupies).ToArray();
        _payload=null;
        if(_departing!=null){_loadState=CannonShotResult.Busy;return;}
        if(candidates.Length==0){_loadState=CannonShotResult.Empty;return;}
        if(candidates.Length!=1){_loadState=CannonShotResult.Ambiguous;return;}
        var body=candidates[0];
        var solved=world.PhysicsAssembly.Body(new(body,RootBody));
        var extent=CylindricalRegion.Measure(world.Physics.Collider(solved.Id).Declaration.Geometry,solved.Pose,cannon.Pose);
        var width=extent.Bounds.Maximum-extent.Bounds.Minimum;
        if(width.Y<MinimumPayloadWidth||width.Y>MaximumPayloadWidth||
            width.Z<MinimumPayloadWidth||width.Z>MaximumPayloadWidth||
            !chamber.Contains(extent,.002)||
            (solved.LinearVelocity-cannon.PointVelocity(solved.Center)).Length>1)
        {_loadState=CannonShotResult.Unseated;return;}
        _payload=body;_loadState=CannonShotResult.None;
    }
    private void TryFire(MachineWorld world)
    {
        if(_loadState!=CannonShotResult.None){LastShot=_loadState;return;}
        if(EnergyState.Energy<EnergyState.Capacity){LastShot=CannonShotResult.Uncharged;return;}
        var body=_payload!;
        var cannon=WorldGeometry.CaptureSpatialState(world,new(this,RootBody));
        var solved=world.PhysicsAssembly.Body(new(body,RootBody));
        var axis=cannon.Pose.Rotation.Apply(new(1,0,0));
        var geometry=world.Physics.Collider(solved.Id).Declaration.Geometry;
        var backward=solved.Pose.Rotation.Inverse().Apply(-axis);
        var rear=double.PositiveInfinity;
        for(var i=0;i<geometry.Count;i++)
        {
            var point=solved.Pose.TransformPoint(geometry[new(i)].Support(backward));
            rear=Math.Min(rear,cannon.Pose.InverseTransformPoint(point).X);
        }
        var travel=HalfLength+.02-rear;
        var clearance=WorldGeometry.Sweep(world,geometry,solved.Pose,axis*travel,this,body);
        if(clearance.Status!=WorldSweepStatus.Clear){LastShot=CannonShotResult.Obstructed;return;}
        var incoming=CollisionVector.Dot(solved.LinearVelocity,axis);
        if(incoming>=MaximumLaunchSpeed){LastShot=CannonShotResult.SpeedLimited;return;}
        var release=world.Physics.ReleaseEnergyStore(StoreOwner(world),solved.Id,axis,solved.Center,
            MaximumLaunchSpeed,double.MaxValue);
        if(release.Impulse==0){LastShot=CannonShotResult.NoResponse;return;}
        _departing=body;LastPayload=body;ShotCount++;
        world.EmitOccurrence(new(this,ShotOccurrence),1);
        LastShot=CannonShotResult.Fired;_loadState=CannonShotResult.Busy;_payload=null;
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        world.Physics.ChargeEnergyStore(StoreOwner(world),
            HasElectricalPower(SocketId.PowerIn)?ReadParameter(CannonParameter.ChargePower):0,delta);
        var energy=EnergyState;
        Phase=_loadState switch
        {
            CannonShotResult.Empty=>CannonPhase.Empty,
            CannonShotResult.Unseated=>CannonPhase.Loading,
            CannonShotResult.Ambiguous=>CannonPhase.Jammed,
            CannonShotResult.Busy=>CannonPhase.Firing,
            CannonShotResult.None=>energy.Energy==energy.Capacity?CannonPhase.Ready:CannonPhase.Charging,
            _=>throw new InvalidOperationException("Invalid chamber inspection state.")
        };

    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(new(0,-.75f,0),new(1.7f,.2f,1.4f),new("#293954"));
        AddBox(new(-.65f,0,0),new(.2f,1.16f,1.16f),new("#fff8e9"));
        var pose=Transform3D.Identity;
        Tubes.Add(new(pose,HalfLength,BoreRadius,.58f,false));
        PipeArt.Cylinder(Visual,pose,HalfLength,BoreRadius,.58f,new(.4f,.72f,.79f,.22f),false);
        // This moving sleeve stays entirely inside the fixed annular jacket.
        // It cannot change the open bore or apply a second impulse to the payload.
        _recoilSleeve=PipeArt.Cylinder(Visual,new(Basis.Identity,SleeveRestPosition),.07f,.50f,.565f,new("#556573"),true);
        foreach(var x in new[]{-.5f,.5f})
        {
            var collar=new Transform3D(Basis.Identity,new(x,0,0));
            Tubes.Add(new(collar,.05f,BoreRadius,.65f,true));
            PipeArt.Cylinder(Visual,collar,.05f,BoreRadius,.65f,new("#fff8e9"),true);
        }
        PartArt.Box(Visual,new(.8f,.11f,.07f),new("#293954"),new(0,-.55f,.62f));
        _chargeBar=PartArt.Box(Visual,new(.8f,.07f,.075f),new("#e8b764"),new(0,-.55f,.66f));
        _flag=PartArt.Box(Visual,new(.28f,.14f,.04f),new("#f7cb52"),new(-.55f,.75f,0));
        _indicator=PartArt.Sphere(Visual,.07f,new("#556573"),new(-.66f,.65f,.4f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.07f,new("#e8b764"),port.LocalPosition);
        _chargeBar.Scale=new(.001f,1,1);
    }
}
