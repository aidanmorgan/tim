using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;
namespace CuriousContraptions;

/// <summary>A finite-mass rigid plate on a passive elastic slider. Contact loads
/// the shared potential; no impact callback prescribes a payload velocity.</summary>
public partial class SpringPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<SpringParameter>(fields);
    public const float RestHeight=.14f;
    public const float MaximumStroke=.25f;
    public const double PlateMass=.25;
    private const float CoilHeight=.365f;
    private static readonly CollisionVector PlateHalf=new(.65,.075,.6);
    public static readonly JointSlot PlateGuide=new();
    public static readonly BodySlot PlateBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((SpringPart)p)._plate.Transform),
        _=>BodyDynamics.SolidBox(PlateMass,PlateHalf,default,default),
        BodyQueryPolicy.Include,p=>p!.InitialContactMaterial,
        p=>((SpringPart)p!).PoseAssets);
    private MeshInstance3D _plate=null!,_coil=null!;
    private AxialElasticPotential Elastic=>ReadParameterState<AxialElasticPotential>();
    private double Coordinate=>GetParent() is MachineWorld {HasPhysicsState:true}?
        ReadAxialMotion(PlateGuide).Coordinate:-ReadParameter(SpringParameter.InitialCompression);
    public float PlateOffset=>(float)Coordinate;
    public double StoredElasticEnergy=>Elastic.Energy(Coordinate);
    private readonly SimulationState<int> _count = new(0);
    public int HitCount => _count.Value;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_count];
    public override ContactMaterial InitialContactMaterial=>new(0,.05,.1);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints
    {
        get
        {
            var owner=SceneGeometryAdapter.CaptureRigidPose(Transform);
            var plate=PlateBody.Pose(this);
            var axis=RigidRotation.FromRotationVector(new(-Math.PI/2,0,0));
            var initial=new CollisionVector(0,RestHeight-ReadParameter(SpringParameter.InitialCompression),0);
            return [new SceneFrameJoint(new(this,PlateGuide),FrameJointKind.Slider,
                new(this,PlateBody),new(plate.InverseTransformPoint(owner.TransformPoint(initial)),
                    plate.Rotation.Inverse()*owner.Rotation*axis),
                new(this,RootBody),new(new(0,RestHeight,0),axis),
                ConnectedBodyCollision.Disabled,new(-MaximumStroke,0),JointTravelDirection.Both)];
        }
    }
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var stiffness=parameters.Read(SpringParameter.Stiffness);
        var damping=parameters.Read(SpringParameter.Damping);
        var compression=parameters.Read(SpringParameter.InitialCompression);
        if(!float.IsFinite(stiffness)||stiffness<120||stiffness>1200||
            !float.IsFinite(damping)||damping<0||damping>8||
            !float.IsFinite(compression)||compression<0||compression>.2f)
            throw new ArgumentException("Springboard requires stiffness 120–1200, damping 0–8 and initial compression 0–0.20.");
    }
    protected override PartParameterState PrepareParameterState(PartParameterValues parameters) =>
        PartParameterState.Create(new AxialElasticPotential(parameters.Read(SpringParameter.Stiffness),0));
    protected override void PrepareConstruction()
    {
        _plate.Position=new(0,RestHeight-ReadParameter(SpringParameter.InitialCompression),0);
        UpdateCoil();
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        world.AddElasticLoad(new(this,PlateGuide),Elastic);
        var damping=ReadParameter(SpringParameter.Damping);
        world.AddDampingLoad(new(this,PlateGuide),damping,damping);
    }
    private IReadOnlyList<ScenePoseAsset> PoseAssets =>
    [
        new(_plate,RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity)),
        new(_coil,RootPoseReference,ScenePoseMap.AxisAffine(PoseReadSpace.Relative,PoseMapAxis.Y,RigidRotation.Identity,
            new(0,-.3f,0),default,new(1,(CoilHeight-RestHeight)/CoilHeight,1),new(0,1/CoilHeight,0)))
    ];
    private void UpdateCoil()=>_coil.Scale=new(1,(float)((CoilHeight+Coordinate)/CoilHeight),1);
    public override void ObserveContact(SceneContact contact,MachineWorld world)
    {
        if(contact.Self.Slot!=PlateBody||contact.ApproachSpeed<.05)return;
        _count.Value++;
        world.Events.TryAdd(new(MachineEventKind.Bounced,Uid),world.Ticks);
    }
    protected override void Build()
    {
        PickRadius=.7f;
        AddBox(new(0,-.36f,0),new(1.3f,.12f,1.2f),new("#273446"));
        Boxes.Add(new(Vector3.Zero,new(.65f,.075f,.6f),PlateBody));
        _plate=PartArt.Box(Visual,new(1.3f,.15f,1.2f),Definition.Color,
            new(0,RestHeight-ReadParameter(SpringParameter.InitialCompression),0));
        _plate.Name="SpringPlate";
        _coil=PartArt.Mesh(Visual,BuildCoil(),new("#ccd9df"),new(0,-.3f,0));
        _coil.Name="SpringCoil";
        UpdateCoil();
    }
    private static ImmediateMesh BuildCoil()
    {
        const int segments = 96, sides = 8;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        void Vertex(int step, int side)
        {
            var t = (float)step / segments;
            var angle = t * Mathf.Tau * 3;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var tangent = new Vector3(-Mathf.Sin(angle) * .27f * Mathf.Tau * 3, CoilHeight,
                Mathf.Cos(angle) * .27f * Mathf.Tau * 3).Normalized();
            var around = (float)side / sides * Mathf.Tau;
            var normal = radial * Mathf.Cos(around) + tangent.Cross(radial) * Mathf.Sin(around);
            mesh.SurfaceSetNormal(normal);
            mesh.SurfaceAddVertex(radial * .27f + Vector3.Up * (t * CoilHeight) + normal * .025f);
        }
        for (var step = 0; step < segments; step++)
        for (var side = 0; side < sides; side++)
        {
            Vertex(step, side); Vertex(step + 1, side); Vertex(step + 1, side + 1);
            Vertex(step, side); Vertex(step + 1, side + 1); Vertex(step, side + 1);
        }
        mesh.SurfaceEnd();
        return mesh;
    }
}
