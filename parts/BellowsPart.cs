using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum BellowsPhase { Ready, Compressing, Held, Refilling }
public enum BellowsParameter { Force, Reach, Width }

/// <summary>A finite-mass, spring-return pump plate. Shared physics owns every
/// stroke and contact; the shared ideal-jet supply samples current compression velocity.</summary>
public partial class BellowsPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<BellowsParameter>(fields);
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(BellowsPart owner) : SimulationTransactionParticipant
    {
        private BellowsPhase _savedPhase;
        private float _savedCompression;
        private float _savedEmissionForce;
        private double _savedEmittedImpulse;
        private int _savedStrokeCount;
        private double _savedPreviousCoordinate;
        private bool _savedInStroke;
        protected override void CaptureCheckpoint()
        {
            _savedPhase=owner.Phase;
            _savedCompression=owner.Compression;
            _savedEmissionForce=owner.EmissionForce;
            _savedEmittedImpulse=owner.EmittedImpulse;
            _savedStrokeCount=owner.StrokeCount;
            _savedPreviousCoordinate=owner._previousCoordinate;
            _savedInStroke=owner._inStroke;
        }
        protected override void RestoreCheckpoint()
        {
            owner.Phase=_savedPhase;
            owner.Compression=_savedCompression;
            owner.EmissionForce=_savedEmissionForce;
            owner.EmittedImpulse=_savedEmittedImpulse;
            owner.StrokeCount=_savedStrokeCount;
            owner._previousCoordinate=_savedPreviousCoordinate;
            owner._inStroke=_savedInStroke;
        }
    }
    public const float RestHeight=.45f;
    public const float MaximumStroke=.4f;
    public const double PlateMass=.5;
    public const double ReturnPreload=8;
    public const double ReturnStiffness=20;
    public const double RefillDamping=4;
    public const double ReferenceCompressionSpeed=.8;
    private const double TravelTolerance=1e-7;
    private const double SpeedTolerance=1e-6;
    private static readonly Vector3 PlateHalf=new(.7f,.06f,.55f);
    private static readonly Vector3 NozzleAt=new(.96f,-.12f,0);
    public static readonly JointSlot PlateGuide=new();
    public static readonly BodySlot PlateBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((BellowsPart)p)._plate.Transform),
        _=>new(PhysicsMotionType.Dynamic,PlateMass,
            new(PlateMass*(PlateHalf.Y*PlateHalf.Y+PlateHalf.Z*PlateHalf.Z)/3,
                PlateMass*(PlateHalf.X*PlateHalf.X+PlateHalf.Z*PlateHalf.Z)/3,
                PlateMass*(PlateHalf.X*PlateHalf.X+PlateHalf.Y*PlateHalf.Y)/3),default,default),
        BodyQueryPolicy.Include,p=>p!.InitialContactMaterial,
        p=>((BellowsPart)p!).PoseAssets);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints
    {
        get
        {
            var owner=SceneGeometryAdapter.CaptureRigidPose(Transform);
            var plate=PlateBody.Pose(this);
            var axis=RigidRotation.FromRotationVector(new(-Math.PI/2,0,0));
            var at=new CollisionVector(0,RestHeight,0);
            return [new SceneFrameJoint(new(this,PlateGuide),FrameJointKind.Slider,
                new(this,PlateBody),new(plate.InverseTransformPoint(owner.TransformPoint(at)),
                    plate.Rotation.Inverse()*owner.Rotation*axis),
                new(this,RootBody),new(at,axis),ConnectedBodyCollision.Disabled,
                new(-MaximumStroke,0),JointTravelDirection.Both)];
        }
    }
    public BellowsPhase Phase { get; private set; }
    public float Compression { get; private set; }
    public float EmissionForce { get; private set; }
    public double EmittedImpulse { get; private set; }
    public int StrokeCount { get; private set; }
    public Transform3D PlateTransform=>Transform*_plate.Transform;
    private double _previousCoordinate;
    private bool _inStroke;
    private Node3D _plate=null!;
    private Node3D _folds=null!;
    public override ContactMaterial InitialContactMaterial=>new(0,.1,.3);
    private SceneMechanicalSourceKey SupplyKey=>new(new(this,RootBody),AirflowNetwork.SourceSlot);
    public override IReadOnlyList<SceneMechanicalSourceKey> PhysicsTransferSources=>[SupplyKey];
    public override AirflowEmitter? CreateAirflowSource(MachineWorld world)=>
        new(NozzleAt,Vector3.Right,ReadParameter(BellowsParameter.Reach),
            ReadParameter(BellowsParameter.Width),
            new(world.TransferBindings.Source(SupplyKey),new AxialPowerPort(world.PhysicsAssembly.JointId(new(this,PlateGuide)),
                FrameJointKind.Slider,-AirflowNetwork.ReferenceFlowSpeed/ReferenceCompressionSpeed),
                new(ReadParameter(BellowsParameter.Force),ReadParameter(BellowsParameter.Force)*AirflowNetwork.ReferenceFlowSpeed))
                {ParticipationBody=world.PhysicsAssembly.Body(new(this,PlateBody)).Id},
            new(ReadParameter(BellowsParameter.Force)/AirflowNetwork.ReferenceFlowSpeed,ReadParameter(BellowsParameter.Force)));

    protected override void ValidateParameters(PartParameterValues parameters)
    {
        foreach(var key in Enum.GetValues<BellowsParameter>())
            if(!float.IsFinite(parameters.Read(key)))
                throw new ArgumentException("Bellows parameters must be finite.");
        if(parameters.Read(BellowsParameter.Force)<=0||parameters.Read(BellowsParameter.Force)>40||
            parameters.Read(BellowsParameter.Reach)<=0||parameters.Read(BellowsParameter.Reach)>12||
            parameters.Read(BellowsParameter.Width)<=0||parameters.Read(BellowsParameter.Width)>4)
            throw new ArgumentException("Bellows airflow parameters are outside supported bounds.");
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(this,PlateGuide));
        var coordinate=joint.Travel.Error;
        _previousCoordinate=coordinate;
        world.AddElasticLoad(new(this,PlateGuide),new(ReturnStiffness,ReturnPreload/ReturnStiffness));
        world.AddDampingLoad(new(this,PlateGuide),0,RefillDamping);
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(this,PlateGuide));
        var coordinate=joint.Travel.Error;
        var compression=Math.Max(0,-coordinate);
        var compressionSpeed=(_previousCoordinate-coordinate)/delta;
        Compression=(float)compression;
        // Compression-derived diagnostic, not committed transfer effort.
        // Its source-observation contract still requires forward migration.
        EmissionForce=compressionSpeed>SpeedTolerance
            ?ReadParameter(BellowsParameter.Force)*(float)Math.Min(1,compressionSpeed/ReferenceCompressionSpeed):0;
        EmittedImpulse+=EmissionForce*delta;
        if(compression<=TravelTolerance)
        {
            Phase=BellowsPhase.Ready;
            _inStroke=false;
        }
        else
        {
            if(!_inStroke)
            {
                _inStroke=true;
                StrokeCount++;
                world.Events.TryAdd(new(MachineEventKind.Activated,Uid),world.Ticks);
            }
            Phase=compressionSpeed>SpeedTolerance?BellowsPhase.Compressing:
                compressionSpeed < -SpeedTolerance?BellowsPhase.Refilling:BellowsPhase.Held;
        }
        Active=EmissionForce>0;
    }
    private IReadOnlyList<ScenePoseAsset> PoseAssets =>
    [
        new(_plate,RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity)),
        new(_folds,RootPoseReference,ScenePoseMap.AxisAffine(PoseReadSpace.Relative,PoseMapAxis.Y,RigidRotation.Identity,
            new(0,-.3f,0),default,new(1,.3f/.75f,1),new(0,1/.75f,0)))
    ];
    protected override void Build()
    {
        PickRadius=.95f;
        Boxes.Add(new(Vector3.Zero,PlateHalf,PlateBody));
        AddBox(new(0,-.4f,0),new(1.5f,.16f,1.2f),new("#293954"));
        _plate=new Node3D {Name="PressPlate",Position=new(0,RestHeight,0)};Visual.AddChild(_plate);
        PartArt.Box(_plate,PlateHalf*2,new("#fff8e9"));
        PartArt.Box(_plate,new(.7f,.025f,.35f),new("#e8b764"),new(0,.07f,0));
        _folds=new Node3D {Name="Accordion",Position=new(0,-.3f,0)};Visual.AddChild(_folds);
        for(var i=0;i<5;i++)
        {
            var y=(i+.5f)*.15f;
            PartArt.Box(_folds,new(1.2f,.1f,.92f),new("#66b8c9"),new(0,y,0));
            PartArt.Box(_folds,new(1.28f,.035f,1),new("#fff8e9"),new(0,y+.05f,0));
        }
        AddBox(new(.73f,-.12f,0),new(.45f,.19f,.24f),new("#e8b764"));
        var mouth=PartArt.Ring(Visual,.12f,.025f,new("#293954"),NozzleAt);
        mouth.RotationDegrees=new(0,0,90);
    }
}
