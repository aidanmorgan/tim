using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

public enum WoundSpringParameter { Stiffness, Stroke, WindingLead }
public enum WoundSpringPhase { Idle, Winding, Armed, Releasing, Blocked }

/// <summary>Finite-work spring launcher with an anchored, one-way guided plunger.
/// No ammunition is created. The plunger is a real finite-mass internal physics body.</summary>
public partial class WoundSpringPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<WoundSpringParameter>(fields);
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_latchReleased,_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(WoundSpringPart owner) : SimulationTransactionParticipant
    {
        private bool _savedStarted;
        private int? _savedTriggerTick;
        private WoundSpringPhase _savedPhase;
        protected override void CaptureCheckpoint()
        {
            _savedStarted=owner._started;
            _savedTriggerTick=owner._triggerTick;
            _savedPhase=owner.Phase;
        }
        protected override void RestoreCheckpoint()
        {
            owner._started=_savedStarted;
            owner._triggerTick=_savedTriggerTick;
            owner.Phase=_savedPhase;
        }
    }
    public const string CatalogId = "wound_spring";
    public const float RestHeadY = .9f;
    public const float PlungerRadius = .32f;
    public const float PlungerMass = .5f;
    private GuidedPlunger _head = null!;
    private MeshInstance3D _coil = null!, _chargeMarker = null!;
    private Node3D _pulley = null!, _latch = null!;
    private bool _started;
    private readonly SimulationState<bool> _latchReleased=new(false);
    public static readonly Bridge.BooleanObservationSlot LatchReleasedOutput=new(0);
    private static readonly AnimationDefinition LatchTransition=new(0,-.65,.65/8,
        AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneBooleanObservation> BooleanObservations=>
        [new(LatchReleasedOutput,new(_latchReleased))];
    public override IReadOnlyList<SceneRotationAnimation> RotationAnimations=>
        [new(_latch,LatchTransition,AnimationRotationAxis.Z,
            SceneAnimationSignal.Boolean(new(this,LatchReleasedOutput)),SceneAnimationDrive.Endpoint)];
    private readonly List<MachinePart> _internalBodies = new();
    private int? _triggerTick;
    public WoundSpringPhase Phase { get; private set; }
    private PhysicsLatchedSpringState? RuntimeSpring=>GetParent() is MachineWorld {HasPhysicsState:true} world?
        world.Physics.Spring(world.PhysicsAssembly.JointId(new(this,PlungerGuide))):null;
    public SpringTriggerResult? LastTrigger=>RuntimeSpring?.LastTrigger;
    public int ReleaseCount=>RuntimeSpring?.ReleaseCount??0;
    public float LatchAngle => _latch.Rotation.Z;
    public double StoredEnergy=>RuntimeSpring?.Energy??0;
    public double AcceptedWork=>RuntimeSpring?.AcceptedWork??0;
    public double ReleasedWork=>RuntimeSpring?.ReleasedWork??0;
    public float Compression=>(float)(RuntimeSpring?.Compression??0);
    public MachinePart Plunger => _head;
    public static readonly JointSlot PlungerGuide=new(),ShaftJoint=new(),WindingJoint=new();
    public static readonly BodySlot ShaftBody=SceneRotaryShaft.Slot(p=>((WoundSpringPart)p)._pulley,.25,.26,.1);
    public override IReadOnlyList<SceneLatchedSpringDeclaration> PhysicsSprings=>
        [new(new(this,PlungerGuide),new(this,WindingJoint),ReadParameter(WoundSpringParameter.Stiffness),ReadParameter(WoundSpringParameter.Stroke))];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.DriveIn,ShaftJoint,-1)];
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints
    {
        get
        {
            var owner=SceneGeometryAdapter.CaptureRigidPose(Transform);
            var head=SceneGeometryAdapter.CaptureRigidPose(_head.Transform);
            // Shared frame joints use local Z as the free axis; this mechanism
            // declares local Y. The spherical head need not share the owner's basis.
            var axisFrame=global::CuriousContraptions.Geometry.RigidRotation.FromRotationVector(new(-Math.PI/2,0,0));
            var coordinate=global::CuriousContraptions.Geometry.CollisionVector.Dot(head.Center-owner.Center,owner.Rotation.Apply(new(0,1,0)));
            // Both guide anchors describe the same rail point in double precision.
            // A float scene center is not assumed to lie exactly on the rotated rail.
            var railPoint=owner.TransformPoint(new(0,coordinate,0));
            var headAnchor=head.InverseTransformPoint(railPoint);
            var range=new JointTravelRange(coordinate-ReadParameter(WoundSpringParameter.Stroke),coordinate);
            return
            [
                new SceneFrameJoint(new(this,PlungerGuide),Physics.FrameJointKind.Slider,
                    new(_head,RootBody),new(headAnchor,head.Rotation.Inverse()*owner.Rotation*axisFrame),
                    new(this,RootBody),new(default,axisFrame),
                    Physics.ConnectedBodyCollision.Disabled,range,JointTravelDirection.Negative),
                SceneRotaryShaft.Guide(this,ShaftJoint,ShaftBody,new(-.62f,-.55f,.5f)),
                new SceneTransmissionJoint(new(this,WindingJoint),new(this,ShaftJoint),new(this,PlungerGuide),ReadParameter(WoundSpringParameter.WindingLead),
                    TransmissionEngagement.Engaged)
            ];
        }
    }
    public override IReadOnlyList<MachinePart> InternalBodies => _internalBodies;
    public override IReadOnlyList<InternalBodyRole> InternalBodyRoles => [InternalBodyRole.Plunger];
    public override MachinePart InternalBody(InternalBodyRole role) => role switch
    {
        InternalBodyRole.Plunger => _head ?? throw new InvalidOperationException("Plunger has not been constructed."),
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, new(-.62f, -.55f, .55f)),
        new(SocketId.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, new(.62f, -.55f, .55f))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var stiffness = parameters.Read(WoundSpringParameter.Stiffness);
        var stroke = parameters.Read(WoundSpringParameter.Stroke);
        var lead = parameters.Read(WoundSpringParameter.WindingLead);
        if (!float.IsFinite(stiffness) || stiffness < 40 || stiffness > 240)
            throw new ArgumentException("Wound spring stiffness must be between 40 and 240.");
        if (!float.IsFinite(stroke) || stroke < .2f || stroke > .8f)
            throw new ArgumentException("Wound spring stroke must be between 0.2 and 0.8.");
        if (!float.IsFinite(lead) || lead < .025f || lead > .2f)
            throw new ArgumentException("Winding lead must be between 0.025 and 0.2 per radian.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported spring command.");
        _triggerTick ??= world.Ticks;
        return ActivationDisposition.Deferred;
    }
    protected override void PrepareConstruction()
    {
        _head.SetConstructionCoordinate(RestHeadY);
        _started = true;
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        if (_triggerTick is { } tick && tick < world.Ticks)
        {
            _triggerTick = null;
            world.Physics.ReleaseSpring(world.PhysicsAssembly.JointId(new(this,PlungerGuide)));
        }
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var state=world.Physics.Spring(world.PhysicsAssembly.JointId(new(this,PlungerGuide)));
        _latchReleased.Value=state.State==SpringLatchState.Releasing;
        if(state.State==SpringLatchState.Releasing)
            Phase=state.Compression<state.BeforeCompression-1e-9?WoundSpringPhase.Releasing:WoundSpringPhase.Blocked;
        else
            Phase=state.Compression>state.BeforeCompression+1e-9?WoundSpringPhase.Winding:
                !state.IsCharged?WoundSpringPhase.Idle:
                state.Declaration.Stroke-state.Compression>state.StopTolerance&&state.ConstraintObservation.OpposesWinding?
                    WoundSpringPhase.Blocked:WoundSpringPhase.Armed;
        Active=Phase is WoundSpringPhase.Winding or WoundSpringPhase.Releasing;
    }

    public override void _Process(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!_started) _head.SetConstructionCoordinate(RestHeadY);
    }
    private void InitialiseConstructionArt()
    {
        var height = RestHeadY - PlungerRadius + .8f;
        _coil.Scale = new(1, Mathf.Max(.1f, height) / 1.38f, 1);
        _chargeMarker.Position = new(.6f, RestHeadY - .1f, .14f);
    }
    private IReadOnlyList<ScenePoseAsset> PlungerPoseAssets=>
    [
        new(_coil,RootPoseReference,
            ScenePoseMap.AxisAffine(PoseReadSpace.Relative,PoseMapAxis.Y,RigidRotation.Identity,
                new(0,-.8f,0),default,new(1,(.8f-PlungerRadius)/1.38f,1),new(0,1/1.38f,0)),
            PoseConstructionPolicy.PreserveExact),
        new(_chargeMarker,RootPoseReference,
            ScenePoseMap.AxisAffine(PoseReadSpace.Relative,PoseMapAxis.Y,RigidRotation.Identity,
                new(.6f,-.1f,.14f),new(0,1,0),new(1,1,1),default),PoseConstructionPolicy.PreserveExact)
    ];
    protected override void Build()
    {
        PickRadius = 1.25f;
        AddBox(new(0, -1, 0), new(1.55f, .2f, 1.35f), new("#293954"));
        foreach (var x in new[] { -.6f, .6f })
            AddBox(new(x, -.05f, 0), new(.13f, 1.8f, .2f), new("#fff8e9"));
        for (var mark = 0; mark <= 4; mark++)
            PartArt.Box(Visual, new(.12f, .025f, .025f), new("#293954"),
                new(.6f, RestHeadY - .1f - ReadParameter(WoundSpringParameter.Stroke) * mark / 4, .112f));
        _chargeMarker = PartArt.Box(Visual, new(.19f, .045f, .035f), new("#f7cb52"));
        // Fixed guide sleeve holds an incoming ball above the physical rounded head.
        var pose = new Transform3D(new Basis(Vector3.Back, Mathf.Pi / 2), new(0, .5f, 0));
        Tubes.Add(new(pose, .9f, .41f, .49f, false));
        PipeArt.Cylinder(Visual, pose, .9f, .41f, .49f, new Color(.4f, .72f, .79f, .18f), false);
        _coil = PartArt.Mesh(Visual, CoilMesh(), new("#ccd9df"), new(0, -.8f, 0));
        _pulley = new Node3D { Position = new(-.62f, -.55f, .5f) }; Visual.AddChild(_pulley);
        SceneRotaryShaft.AddCollider(this,ShaftBody,.26,.1);
        var wheel = PartArt.Cylinder(_pulley, .26f, .1f, new("#fff8e9"));
        wheel.RotationDegrees = new(90, 0, 0);
        PartArt.Box(_pulley, new(.42f, .05f, .03f), new("#f7cb52"), new(0, 0, .065f));
        _latch = new Node3D { Position = new(.48f, .25f, .5f) }; Visual.AddChild(_latch);
        PartArt.Box(_latch, new(.22f, .1f, .1f), new("#f7cb52"));
        PartArt.Sphere(Visual, .07f, new("#f7cb52"), new(.62f, -.55f, .55f));
        _head = new GuidedPlunger { Mechanism = this, Definition = new PartDefinition { Id = CatalogId }, TopLevel = true };
        _head.Configure(new() { Id = InternalBodyId(InternalBodyRole.Plunger), Kind = CatalogId });
        AddChild(_head);
        _internalBodies.Add(_head);
        _head.SetConstructionCoordinate(RestHeadY);
        InitialiseConstructionArt();
    }
    private static ImmediateMesh CoilMesh()
    {
        const int segments = 96, sides = 8;
        var mesh = new ImmediateMesh(); mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        void Vertex(int step, int side)
        {
            var t = (float)step / segments;
            var angle = t * Mathf.Tau * 4;
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var tangent = new Vector3(-Mathf.Sin(angle) * .22f * Mathf.Tau * 4, 1.38f,
                Mathf.Cos(angle) * .22f * Mathf.Tau * 4).Normalized();
            var around = (float)side / sides * Mathf.Tau;
            var normal = radial * Mathf.Cos(around) + tangent.Cross(radial) * Mathf.Sin(around);
            mesh.SurfaceSetNormal(normal);
            mesh.SurfaceAddVertex(radial * .22f + Vector3.Up * (t * 1.38f) + normal * .025f);
        }
        for (var step = 0; step < segments; step++)
        for (var side = 0; side < sides; side++)
        {
            Vertex(step, side); Vertex(step + 1, side); Vertex(step + 1, side + 1);
            Vertex(step, side); Vertex(step + 1, side + 1); Vertex(step, side + 1);
        }
        mesh.SurfaceEnd(); return mesh;
    }

    private partial class GuidedPlunger : MachinePart
    {
        public override BodyEnvelope CollisionEnvelope=>BodyEnvelope.Sphere;
        public override BodyDynamics InitialBodyDynamics=>BodyDynamics.SolidSphere(Mass,Radius,
            SceneGeometryAdapter.CaptureVector(InitialVelocity),default);
        public WoundSpringPart Mechanism { get; init; } = null!;
        public Vector3 Axis => Mechanism.Basis.Y.Normalized();
        public override IReadOnlyList<ScenePoseAsset> RootPoseAssets=>Mechanism.PlungerPoseAssets;
        public override MachinePart PhysicsOwner => Mechanism;
        public void SetConstructionCoordinate(float value) => Position = Mechanism.Position + Axis * value;
        protected override void Build()
        {
            Dynamic = true; Radius = PlungerRadius; Mass = PlungerMass; Bounce = 0; Drag = 0;
            PartArt.Sphere(Visual, Radius, new("#fff8e9"));
        }
    }
}
