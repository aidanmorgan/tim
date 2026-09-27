using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum CannonPhase { Empty, Loading, Charging, Ready, Firing, Jammed }
public enum CannonShotResult { None, Fired, Empty, Uncharged, Unseated, Obstructed, Ambiguous, Busy, SpeedLimited }
public static class CannonParameters
{
    public const string Capacity="capacity";
    public const string ChargePower="charge_power";
}

/// <summary>Reloadable electrically charged toy launcher. The physical payload is never spawned or repositioned.</summary>
public partial class CannonPart : MachinePart
{
    public const string CatalogId="cannon";
    public const float HalfLength=.55f;
    public const float BoreRadius=.48f;
    public const float MinimumRadius=.3f;
    public const float MaximumRadius=.42f;
    public const float MaximumLaunchSpeed=16;
    public const float MaximumRecoil=.16f;
    public const double RecoilCompressionSeconds=.08,RecoilReturnSeconds=.45;
    public float RecoilOffset { get; private set; }
    private readonly List<double> _recoilAges=new();
    private Node3D _recoilSleeve=null!;
    public CannonPhase Phase { get; private set; }
    public CannonShotResult LastShot { get; private set; }
    public int ShotCount { get; private set; }
    public MachinePart? LastPayload { get; private set; }
    public double StoredEnergy=>_energy.Energy;
    public double AcceptedEnergy=>_energy.AcceptedEnergy;
    public double ReleasedEnergy=>_energy.ReleasedEnergy;
    private LaunchEnergyStore _energy=null!;
    private MachinePart? _payload,_departing;
    private int? _triggerTick;
    private CannonShotResult _loadState=CannonShotResult.Empty;
    private MeshInstance3D _chargeBar=null!,_flag=null!;
    private StandardMaterial3D _indicator=null!;

    public override float SurfaceBounce=>0;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.45f,-.55f,.55f)),
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(.15f,-.55f,.55f))
    ];
    public override void ValidateParameters()
    {
        var capacity=Properties[CannonParameters.Capacity];var power=Properties[CannonParameters.ChargePower];
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
        var inverse=Transform.AffineInverse();
        bool Occupies(MachinePart body)
        {
            var at=inverse*body.Position;
            return body.Visible&&Mathf.Abs(at.X)<HalfLength+body.Radius
                &&new Vector2(at.Y,at.Z).Length()<BoreRadius+body.Radius;
        }
        if(_departing!=null&&!Occupies(_departing))_departing=null;
        var candidates=world.Bodies.Where(body=>body.PhysicsOwner==body).Where(Occupies).ToArray();
        _payload=null;
        if(_departing!=null){_loadState=CannonShotResult.Busy;return;}
        if(candidates.Length==0){_loadState=CannonShotResult.Empty;return;}
        if(candidates.Length!=1){_loadState=CannonShotResult.Ambiguous;return;}
        var body=candidates[0];var local=inverse*body.Position;
        if(body.Radius<MinimumRadius||body.Radius>MaximumRadius
            ||Mathf.Abs(local.X)+body.Radius>HalfLength+.002f
            ||new Vector2(local.Y,local.Z).Length()+body.Radius>BoreRadius+.002f
            ||body.Velocity.Length()>1)
        {_loadState=CannonShotResult.Unseated;return;}
        _payload=body;_loadState=CannonShotResult.None;
    }
    private void TryFire(MachineWorld world)
    {
        if(_loadState!=CannonShotResult.None){LastShot=_loadState;return;}
        if(_energy.Energy<_energy.Capacity){LastShot=CannonShotResult.Uncharged;return;}
        var body=_payload!;
        var axis=Basis.X.Normalized();
        var at=Transform.AffineInverse()*body.Position;
        var path=axis*(HalfLength+body.Radius+.02f-at.X);
        var clearance=WorldGeometry.Sweep(world,body.Position,body.Radius,path,this,body);
        if(clearance.Status!=SphereSweepStatus.Clear){LastShot=CannonShotResult.Obstructed;return;}
        var incoming=body.Velocity.Dot(axis);
        var release=_energy.Release(body.Mass,incoming,MaximumLaunchSpeed);
        switch(release.Status)
        {
            case LaunchReleaseStatus.Released: break;
            case LaunchReleaseStatus.Empty: LastShot=CannonShotResult.Uncharged;return;
            case LaunchReleaseStatus.SpeedLimited: LastShot=CannonShotResult.SpeedLimited;return;
            case LaunchReleaseStatus.InsufficientPrecision:
                throw new InvalidOperationException("Cannon release cannot be represented at this precision.");
            default: throw new InvalidOperationException("Unsupported launch release status.");
        }
        body.Velocity+=axis*(release.ForwardSpeed-incoming);
        _departing=body;LastPayload=body;ShotCount++;
        _recoilAges.Add(0);
        LastShot=CannonShotResult.Fired;_loadState=CannonShotResult.Busy;_payload=null;
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        _energy.Charge(HasElectricalPower(SocketId.PowerIn)?Properties[CannonParameters.ChargePower]:0,delta);
        Phase=_loadState switch
        {
            CannonShotResult.Empty=>CannonPhase.Empty,
            CannonShotResult.Unseated=>CannonPhase.Loading,
            CannonShotResult.Ambiguous=>CannonPhase.Jammed,
            CannonShotResult.Busy=>CannonPhase.Firing,
            CannonShotResult.None=>_energy.Energy==_energy.Capacity?CannonPhase.Ready:CannonPhase.Charging,
            _=>throw new InvalidOperationException("Invalid chamber inspection state.")
        };
        var charge=(float)(_energy.Energy/_energy.Capacity);
        _chargeBar.Scale=new(Mathf.Max(.001f,charge),1,1);
        _flag.RotationDegrees=new(0,0,Phase==CannonPhase.Ready?0:-70);
        _indicator.AlbedoColor=Phase switch
        {
            CannonPhase.Ready=>new("#f7cb52"),
            CannonPhase.Charging=>new("#66b8c9"),
            CannonPhase.Empty or CannonPhase.Loading or CannonPhase.Firing or CannonPhase.Jammed=>new("#556573"),
            _=>throw new InvalidOperationException("Unsupported cannon phase.")
        };
    }
    public override void _Process(double delta)
    {
        static double Ease(double value)
        {
            var t=Math.Clamp(value,0,1);
            return t*t*t*(10+t*(-15+6*t));
        }
        var total=0.0;
        for(var i=_recoilAges.Count-1;i>=0;i--)
        {
            var age=_recoilAges[i]+delta;
            if(age>=RecoilCompressionSeconds+RecoilReturnSeconds){_recoilAges.RemoveAt(i);continue;}
            _recoilAges[i]=age;
            total+=age<RecoilCompressionSeconds?Ease(age/RecoilCompressionSeconds)
                :1-Ease((age-RecoilCompressionSeconds)/RecoilReturnSeconds);
        }
        // Smooth saturation lets repeated shots overlap without pose resets.
        // Only the guarded inner sleeve moves; bore, breech and jacket remain solid.
        RecoilOffset=-MaximumRecoil*(float)Math.Tanh(total);
        _recoilSleeve.Position=new(-.22f+RecoilOffset,0,0);
    }
    protected override void Build()
    {
        _energy=new(Properties[CannonParameters.Capacity]);
        PickRadius=1.1f;
        AddBox(new(0,-.75f,0),new(1.7f,.2f,1.4f),new("#293954"));
        AddBox(new(-.65f,0,0),new(.2f,1.16f,1.16f),new("#fff8e9"));
        var pose=Transform3D.Identity;
        Tubes.Add(new(pose,HalfLength,BoreRadius,.58f,false));
        PipeArt.Cylinder(Visual,pose,HalfLength,BoreRadius,.58f,new(.4f,.72f,.79f,.22f),false);
        // This moving sleeve stays entirely inside the fixed annular jacket.
        // It cannot change the open bore or apply a second impulse to the payload.
        _recoilSleeve=new Node3D{Position=new(-.22f,0,0)};
        Visual.AddChild(_recoilSleeve);
        PipeArt.Cylinder(_recoilSleeve,Transform3D.Identity,.07f,.50f,.565f,new("#556573"),true);
        foreach(var x in new[]{-.5f,.5f})
        {
            var collar=new Transform3D(Basis.Identity,new(x,0,0));
            Tubes.Add(new(collar,.05f,BoreRadius,.65f,true));
            PipeArt.Cylinder(Visual,collar,.05f,BoreRadius,.65f,new("#fff8e9"),true);
        }
        PartArt.Box(Visual,new(.8f,.11f,.07f),new("#293954"),new(0,-.55f,.62f));
        _chargeBar=PartArt.Box(Visual,new(.8f,.07f,.075f),new("#e8b764"),new(0,-.55f,.66f));
        _flag=PartArt.Box(Visual,new(.28f,.14f,.04f),new("#f7cb52"),new(-.55f,.75f,0));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.07f,new("#556573"),new(-.66f,.65f,.4f)).MaterialOverride;
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.07f,new("#e8b764"),port.LocalPosition);
        _chargeBar.Scale=new(.001f,1,1);
    }
}
