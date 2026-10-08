using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public partial class WindChimesPart : MachinePart
{
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(WindChimesPart owner) : SimulationTransactionParticipant
    {
        private readonly List<AcousticPulse> _pulses = new();
        private int _savedPulseCount;
        private int _savedLastPulseTick;
        private Vector3 _savedLastAirForce;
        private ChimeTubeId _savedlastTube;
        protected override void CaptureCheckpoint()
        {
            _pulses.Clear(); _pulses.AddRange(owner._pulses);
            _savedPulseCount=owner.PulseCount;
            _savedLastPulseTick=owner.LastPulseTick;
            _savedLastAirForce=owner.LastAirForce;
            _savedlastTube=owner._lastTube;
        }
        protected override void RestoreCheckpoint()
        {
            owner._pulses.Clear(); owner._pulses.AddRange(_pulses);
            owner.PulseCount=_savedPulseCount;
            owner.LastPulseTick=_savedLastPulseTick;
            owner.LastAirForce=_savedLastAirForce;
            owner._lastTube=_savedlastTube;
        }
    }
    public const int MinimumIntervalTicks=24;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    public static readonly JointSlot Suspension=new();
    public static readonly BodySlot PendulumBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((WindChimesPart)p)._pendulum.Transform).Compose(
            global::CuriousContraptions.Geometry.RigidPose.At(new(0,-ChimeAssembly.CenterDistance,0))),
        _=>ChimeAssembly.PendulumDynamics,BodyQueryPolicy.Include,
        _=>new(.35,.08,.2),p=>[new(((WindChimesPart)p!)._pendulum,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,
            RigidPose.At(new(0,ChimeAssembly.CenterDistance,0))))]);
    private static readonly IReadOnlyDictionary<ChimeTubeId,BodySlot> TubeBodies=
        Enum.GetValues<ChimeTubeId>().ToDictionary(id=>id,id=>new BodySlot(
            p=>global::CuriousContraptions.Geometry.RigidPose.At(SceneGeometryAdapter.CaptureVector(ChimeAssembly.Tube(id).Center)),
            _=>new(PhysicsMotionType.Static,0,default,default,default),
            BodyQueryPolicy.Include,_=>new(1,.08,.2),_=>[]));
    public static BodySlot TubeBody(ChimeTubeId id)=>TubeBodies.TryGetValue(id,out var body)
        ?body:throw new ArgumentOutOfRangeException(nameof(id));
    public Transform3D PendulumTransform=>Transform*_pendulum.Transform;
    public Vector3 SailPosition=>PendulumTransform*(Vector3.Down*ChimeAssembly.Length);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints
    {
        get
        {
            var root=SceneGeometryAdapter.CaptureRigidPose(Transform);
            var body=PendulumBody.Pose(this);
            var pivot=root.TransformPoint(SceneGeometryAdapter.CaptureVector(ChimeAssembly.Pivot));
            return [new SceneFrameJoint(new(this,Suspension),FrameJointKind.BallSocket,
                new(this,PendulumBody),new(body.InverseTransformPoint(pivot),RigidRotation.Identity),
                new(this,RootBody),new(SceneGeometryAdapter.CaptureVector(ChimeAssembly.Pivot),RigidRotation.Identity),
                ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both)];
        }
    }
    public Vector3 LastAirForce { get; private set; }
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    public override IReadOnlyList<AirflowSample> AirflowSamples=>[new(ChimeAssembly.SailFromCenter,1,PendulumBody,AirflowResponse.BodyForce,AirflowReceiver,null)];
    private Node3D _pendulum=null!;
    private AudioStreamPlayer3D _audio=null!;
    private Presentation.SceneAcousticBinding? _playback;
    public override Presentation.SceneAcousticBinding? AcousticPlayback => _playback;
    private readonly Dictionary<ToneBand,AudioStreamWav> _tones=[];
    private readonly List<MeshInstance3D> _rings=[];
    public override SceneAcousticWavefronts? AcousticWavefronts=>new(_rings,AcousticPattern.Omnidirectional,.6f,0,1,.008f,.3f,SceneWavefrontOpacity.Strength,64);
    private ChimeTubeId _lastTube;
    public override void BeforeNetworks(MachineWorld world)
    {
        _pulses.RemoveAll(p=>world.Ticks>=p.ExpiresTick);
        Active=_pulses.Count>0&&world.Ticks-LastPulseTick<AcousticPulse.Duration/MachineWorld.Tick;
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        world.AddDragLoad(new(this,PendulumBody),ChimeAssembly.Damping,ChimeAssembly.Damping);
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        LastAirForce=AirflowNetwork.ReadReceiverForce(world,this,delta);
    }
    public override void ObserveContact(SceneContact contact,MachineWorld world)
    {
        foreach(var (id,slot) in TubeBodies)
        {
            if(contact.Self.Slot!=slot)continue;
            var clapper=contact.Other==new SceneBodyKey(this,PendulumBody);
            if(contact.ApproachSpeed<(clapper ? .08 : .8))return;
            var strengthSpeed=contact.ApproachSpeed*(clapper ? 1 : contact.OtherMass);
            var point=contact.Point;
            Ring(world,id,new((float)point.X,(float)point.Y,(float)point.Z),(float)strengthSpeed);
            return;
        }
    }
    private void Ring(MachineWorld world,ChimeTubeId id,Vector3 origin,float speed)
    {
        var strength=Mathf.Clamp(speed/1.5f,.08f,1);
        var simultaneous=world.Ticks==LastPulseTick;
        if(simultaneous)
        {
            var previous=_pulses[^1].Strength;
            if(strength<previous||(strength==previous&&id>=_lastTube))return;
            _pulses.RemoveAt(_pulses.Count-1);
        }
        else
        {
            if(world.Ticks-LastPulseTick<MinimumIntervalTicks)return;
            PulseCount++;LastPulseTick=world.Ticks;
        }
        var tone=ChimeAssembly.Tubes.Single(t=>t.Id==id).Tone;
        _lastTube=id;
        _pulses.Add(new(origin,Vector3.Up,tone,world.Ticks,AcousticPattern.Omnidirectional,strength));
        Active=true;
        // Committed playback consumes the final strongest tone after all contacts in this tick.
    }
    protected override void Build()
    {
        PickRadius=1.3f;
        AddBox(new(0,.93f,0),new(1.2f,.12f,1.2f),new("#fff8e9"),false);
        PartArt.Cylinder(Visual,.6f,.12f,new("#fff8e9"),new(0,.93f,0));
        PartArt.Sphere(Visual,.1f,new("#293954"),new(0,1.06f,0));
        foreach(var tube in ChimeAssembly.Tubes)
        {
            PartArt.Line(Visual,new(tube.Center.X,.9f,tube.Center.Z),new(tube.Center.X,.65f,tube.Center.Z),new("#293954"),.008f);
            PartArt.Cylinder(Visual,ChimeAssembly.TubeRadius,tube.Length,new("#e8b764"),tube.Center);
            PartArt.Ring(Visual,ChimeAssembly.TubeRadius,.008f,new("#293954"),tube.Center+Vector3.Down*(tube.Length*.5f));
            ConvexShapes.Add(new(new(ChimeAssembly.TubeGeometry(tube.Id),AffineTransform.Identity),TubeBody(tube.Id)));
        }
        _pendulum=new Node3D {Name="Pendulum",Position=ChimeAssembly.Pivot};Visual.AddChild(_pendulum);
        PartArt.Line(_pendulum,Vector3.Zero,Vector3.Down*ChimeAssembly.Length,new("#293954"),.01f);
        PartArt.Sphere(_pendulum,ChimeAssembly.ClapperRadius,new("#fff8e9"),Vector3.Down*(ChimeAssembly.Length*ChimeAssembly.ClapperFraction));
        var sail=PartArt.Box(_pendulum,new(.3f,.4f,.055f),new("#66b8c9"),Vector3.Down*ChimeAssembly.Length);
        sail.Basis=ChimeAssembly.SailBasis;
        Spheres.Add(new(ChimeAssembly.ClapperFromCenter,ChimeAssembly.ClapperRadius,PendulumBody));
        ConvexShapes.Add(new(new(new ConvexBox(SceneGeometryAdapter.CaptureVector(ChimeAssembly.SailHalf)),
            SceneGeometryAdapter.CaptureAffine(new(ChimeAssembly.SailBasis,ChimeAssembly.SailFromCenter))),PendulumBody));
        foreach(var tone in Enum.GetValues<ToneBand>())_tones[tone]=AcousticAudio.Create(tone,AcousticVoice.Bell);
        _audio=new AudioStreamPlayer3D {VolumeDb=-15,MaxDistance=20,MaxPolyphony=2};AddChild(_audio);
        _playback=new(_audio,_tones);
        for(var i=0;i<15;i++)
        {
            var ring=PartArt.Ring(Visual,1,.008f,new("#e8b764"));ring.Visible=false;
            ring.RotationDegrees=(i%3) switch {0=>Vector3.Zero,1=>new(90,0,0),_=>new(0,0,90)};
            ring.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;((TorusMesh)ring.Mesh).Rings=64;
            var material=(StandardMaterial3D)ring.MaterialOverride;
            material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            _rings.Add(ring);
        }
    }
}
