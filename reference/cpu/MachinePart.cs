using Godot;
using CuriousContraptions.Presentation;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum ActivationDisposition { Immediate, Deferred }
public enum BodyEnvelope { None, Sphere }

public readonly record struct BoxProxy(Vector3 At, Vector3 Half, BodySlot Body, bool Opaque = true);
public readonly record struct SphereProxy(Vector3 At, float Radius, BodySlot Body);
public readonly record struct ConvexProxy(Physics.ConvexInstance Shape,BodySlot Body,bool Opaque=true);
public readonly record struct SceneContact(SceneBodyKey Self,SceneBodyKey Other,
    global::CuriousContraptions.Geometry.CollisionVector Point,global::CuriousContraptions.Geometry.CollisionVector SelfLocalPoint,double ApproachSpeed,double OtherMass)
{
    public MachinePart OtherPart=>Other.Owner??throw new System.InvalidOperationException("A dynamic impact needs a scene owner.");
}

public partial class MachinePart : Node3D
{
    public static readonly BodySlot RootBody=new(p=>global::CuriousContraptions.Geometry.RigidPose.At(SceneGeometryAdapter.CaptureVector(p.LocalCenterOfMass)),p=>p!.InitialBodyDynamics,BodyQueryPolicy.Include,p=>p!.InitialContactMaterial,p=>[new(p!,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Reference,global::CuriousContraptions.Geometry.RigidPose.Identity),PoseConstructionPolicy.PreserveExact),..p!.RootPoseAssets]);
    public virtual IReadOnlyList<ScenePoseAsset> RootPoseAssets=>[];
    internal ScenePoseReference RootPoseReference=>new(new(this,RootBody),
        global::CuriousContraptions.Geometry.RigidPose.At(-SceneGeometryAdapter.CaptureVector(LocalCenterOfMass)));
    public virtual Vector3 LocalCenterOfMass=>Vector3.Zero;
    public virtual BodyEnvelope CollisionEnvelope=>BodyEnvelope.None;
    public virtual double PhysicsImpactCooldown=>0;
    public virtual Physics.PhysicsImpactImpulse[] PhysicsImpact(Physics.PhysicsImpactBody self,
        Physics.PhysicsImpactBody other,double speed)=>[];
    public virtual void ObserveContact(SceneContact contact,MachineWorld world) { }
    /// <summary>Finalize authored construction before shared-body capture; never a running-body synchronizer.</summary>
    public void PrepareForPhysicsCapture()
    {
        RequireConstructionEdit();
        PrepareConstruction();
    }
    protected virtual void PrepareConstruction() { }
    public virtual IReadOnlyList<Presentation.SceneRotationAnimation> RotationAnimations => [];
    public virtual IReadOnlyList<Presentation.SceneColourAnimation> ColourAnimations => [];
    public virtual IReadOnlyList<Presentation.SceneAngularVelocityAnimation> AngularVelocityAnimations => [];
    public virtual void PreparePhysics(MachineWorld world,float delta) { }
    public virtual void ObservePhysics(MachineWorld world,float delta) { }
    /// <summary>Construction has no simulated motion. Once captured, resolve the
    /// current owned joint on every read, including after replacement or Restore.</summary>
    protected Physics.PhysicsAxialMotion ReadAxialMotion(JointSlot slot)
    {
        if(GetParent() is not MachineWorld {HasPhysicsState:true} world) return default;
        if(world.CurrentJoint(new(this,slot)) is not Physics.PhysicsFrameJoint joint)
            throw new System.InvalidOperationException("Axial motion requires an owned frame joint.");
        return joint.Motion;
    }
    protected Physics.AngularPathTravel ReadAngularTravel(JointSlot slot)
    {
        if(GetParent() is not MachineWorld {HasPhysicsState:true} world)return default;
        return world.Physics.AngularTravel(world.CurrentJoint(new(this,slot)).Id);
    }
    public virtual BodyDynamics InitialBodyDynamics=>Dynamic
        ?throw new System.InvalidOperationException("A dynamic part must explicitly declare its mass properties.")
        :new(Physics.PhysicsMotionType.Static,0,default,default,default);
    public PartDefinition Definition { get; set; } = null!;
    public string Uid { get; private set; } = "";
    public OpticalPathOwner OpticalIdentity { get; private set; }
    public bool Locked { get; private set; }
    public IReadOnlyList<PartDifficulty> Difficulty { get; private set; } = System.Array.Empty<PartDifficulty>();
    public void SetDifficulty(IEnumerable<PartDifficulty> settings)
    {
        RequireConstructionEdit();
        Difficulty = CopyDifficulty(settings);
    }
    private static IReadOnlyList<PartDifficulty> CopyDifficulty(IEnumerable<PartDifficulty> settings)
    {
        System.ArgumentNullException.ThrowIfNull(settings);
        var copy=settings.ToArray();
        if(copy.Any(knot=>knot is null))
            throw new System.ArgumentException("Difficulty knots cannot be null.",nameof(settings));
        return System.Array.AsReadOnly(copy);
    }
    public PartDifficulty Assistance(float precision) => PartAssistance.Evaluate(Difficulty, precision);
    /// <summary>Initial condition consumed when the shared body is constructed; never a live velocity setter.</summary>
    public Vector3 InitialVelocity
    {
        get;
        set
        {
            if (!value.IsFinite()) throw new System.ArgumentOutOfRangeException(nameof(value));
            RequireConstructionEdit();
            field = value;
        }
    }
    protected void RequireConstructionEdit()
    {
        if (PhysicsOwner.GetParent() is MachineWorld { HasPhysicsState: true })
            throw new System.InvalidOperationException("Construction properties cannot change after Run; Reset first.");
    }
    public virtual IReadOnlyList<MachinePart> InternalBodies => [];
    public virtual MachinePart InternalBody(InternalBodyRole role) =>
        throw new System.ArgumentException("Part does not declare this internal body role.", nameof(role));
    private Dictionary<InternalBodyRole, Vector3> _configuredInternalVelocities = new();
    private (MachinePart Part,Vector3 Velocity)[] PrepareInternalInitialMotion(
        IReadOnlyDictionary<InternalBodyRole,Vector3> velocities)
    {
        return velocities.Select(entry =>
        {
            var part=InternalBody(entry.Key);
            System.ArgumentNullException.ThrowIfNull(part);
            part.RequireConstructionEdit();
            return (part,entry.Value);
        }).ToArray();
    }
    private void ApplyInternalInitialMotion()
    {
        foreach(var entry in PrepareInternalInitialMotion(_configuredInternalVelocities))
            entry.Part.InitialVelocity=entry.Velocity;
    }
    private static Vector3 ReadConstructionPosition(float[] values)
    {
        if(values is not {Length:3} || !float.IsFinite(values[0]) ||
            !float.IsFinite(values[1]) || !float.IsFinite(values[2]))
            throw new System.ArgumentException("Position must have exactly three finite components.",nameof(values));
        return new(values[0],values[1],values[2]);
    }
    private static Vector3 ReadInitialVelocity(float[] values)
    {
        if (values is not { Length: 3 } ||
            !float.IsFinite(values[0]) || !float.IsFinite(values[1]) || !float.IsFinite(values[2]))
            throw new System.ArgumentException("Initial velocity must have three finite components.", nameof(values));
        return new(values[0], values[1], values[2]);
    }
    public virtual IReadOnlyList<SceneJointDeclaration> PhysicsJoints => [];
    public virtual IReadOnlyList<SceneDrivenSurface> PhysicsSurfaces => [];
    public virtual IReadOnlyList<SceneLatchedSpringDeclaration> PhysicsSprings => [];
    public virtual IReadOnlyList<SceneCompliantSurface> PhysicsCompliantSurfaces => [];
    public virtual IReadOnlyList<SceneEnergyStoreDeclaration> PhysicsEnergyStores => [];
    public virtual IReadOnlyList<SceneMechanicalSourceKey> PhysicsTransferSources => [];
    public virtual IReadOnlyList<SceneMechanicalReceiverKey> PhysicsTransferReceivers =>
        AirflowSamples.Select(sample=>new SceneMechanicalReceiverKey(new(this,sample.Body),sample.Slot)).ToArray();
    public virtual IEnumerable<SceneResidenceSensorDeclaration> PhysicsResidenceSensors(MachineWorld world) => [];
    public virtual IReadOnlyList<ScenePassageSensorDeclaration> PhysicsPassageSensors => [];
    public virtual IReadOnlyList<SceneServoDeclaration> PhysicsServos => [];
    public virtual IReadOnlyList<SceneTimerDeclaration> SimulationTimers => [];
    public virtual IReadOnlyList<SceneOscillatorDeclaration> SimulationOscillators => [];
    public virtual IReadOnlyList<SceneCounterDeclaration> SimulationCounters => [];
    public virtual IReadOnlyList<SceneLatchDeclaration> SimulationLatches => [];
    public virtual IReadOnlyList<SceneBinaryInputDeclaration> BinaryInputs => [];
    public virtual IReadOnlyList<SceneScalarInputDeclaration> ScalarInputs => [];
    public virtual IReadOnlyList<SceneTiltSensorDeclaration> PhysicsTiltSensors => [];
    public virtual IReadOnlyList<SceneContactLoadSensorDeclaration> PhysicsContactLoadSensors => [];
    // Declared before _Ready so instance validation can be atomic before construction.
    public virtual IReadOnlyList<InternalBodyRole> InternalBodyRoles => [];
    public string InternalBodyId(InternalBodyRole role)
    {
        if (!System.Enum.IsDefined(role)) throw new System.ArgumentOutOfRangeException(nameof(role));
        foreach (var declared in InternalBodyRoles)
            if (declared == role)
                // Explicit boundary to the scene/diagnostic instance-ID namespace.
                return Uid + "_" + System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(role.ToString());
        throw new System.ArgumentException("Part does not declare this internal body role.", nameof(role));
    }
    public virtual MachinePart PhysicsOwner => this;
    public bool Dynamic { get; protected set; }
    public float Radius { get; protected set; } = .32f;
    public float Mass { get; protected set; } = 1;
    public float Bounce { get; protected set; } = .35f;
    public float Drag { get; protected set; } = .04f;
    public float Buoyancy { get; protected set; }
    public List<BoxProxy> Boxes { get; } = new();
    public List<TubeProxy> Tubes { get; } = new();
    public List<BendProxy> Bends { get; } = new();
    public List<FrustumProxy> Frustums { get; } = new();
    public List<SphereProxy> Spheres { get; } = new();
    public List<ConvexProxy> ConvexShapes { get; } = new();
    public bool Active { get; set; }
    private PartParameterValues _parameters=PartParameterValues.Empty;
    /// <summary>Detached serialization/diagnostics boundary; runtime callers use ReadParameter.</summary>
    public IReadOnlyDictionary<string,float> Properties => _parameters.Export();
    protected virtual PartParameterValues BindParameters(IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.BindEmpty(fields);
    public float ReadParameter<TParameter>(TParameter parameter) where TParameter : struct, System.Enum =>
        _parameters.Read(parameter);
    public float PickRadius { get; protected set; } = .65f;
    protected internal Node3D Visual { get; private set; } = null!;
    private MeshInstance3D _highlight = null!;
    private readonly HashSet<SocketId> _poweredInputs = new();
    public bool HasElectricalPower(SocketId port) => _poweredInputs.Contains(port);
    internal void ClearElectricalPower() => _poweredInputs.Clear();
    internal void SupplyElectricalPower(SocketId port) => _poweredInputs.Add(port);
    public virtual AirflowEmitter? CreateAirflowSource(MachineWorld world) => null;
    public static readonly MechanicalReceiverSlot AirflowReceiver=new();
    public virtual IReadOnlyList<AirflowSample> AirflowSamples => Dynamic
        ?[new(Vector3.Zero,1,RootBody,AirflowResponse.BodyForce,AirflowReceiver,null)]:[];
    public virtual IReadOnlyList<RotaryAirflowCapture> RotaryAirflowCaptures=>[];
    public virtual IReadOnlyList<AcousticPulse> AcousticPulses => [];
    public virtual IReadOnlyList<SceneOccurrenceSlot> OccurrenceSources=>[];
    public virtual IReadOnlyList<SceneOccurrenceAnimation> OccurrenceAnimations=>[];
    public virtual Presentation.SceneAcousticBinding? AcousticPlayback => null;
    public virtual IReadOnlyList<Presentation.SceneAcousticMotion> AcousticMotions=>[];
    public virtual Presentation.SceneAcousticWavefronts? AcousticWavefronts=>null;
    public virtual Vector3? AcousticTarget => null;
    public virtual void ReceiveAcousticLevel(float level) { }
    public virtual OpticalEmitter? OpticalSource => null;
    public virtual IReadOnlyList<OpticalSurface> OpticalSurfaces => [];
    public virtual OpticalOutlet? OpticalOutput => null;
    public virtual void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power) { }
    public virtual OpticalEmitter? OpticalPreviewSource => null;
    public virtual Presentation.SceneOpticalPreview? OpticalPreview => null;
    public virtual void ReceiveOpticalPath(IReadOnlyList<OpticalSegment> path) { }
    public virtual void ReceiveOpticalOutputPower(Vector3 power) { }
    public virtual LightEmitter? LightSource => null;
    public virtual IEnumerable<LightSample> LightSamples => [];
    public virtual void ReceiveLight(float irradiance) { }
    public virtual IReadOnlyList<Presentation.SceneScalarRotationAnimation> ScalarRotationAnimations=>[];
    public virtual IReadOnlyList<SceneScalarObservation> ScalarObservations=>[];
    public virtual IReadOnlyList<SceneEnergyStoreObservation> EnergyStoreObservations=>[];
    public virtual IReadOnlyList<SceneBooleanObservation> BooleanObservations=>[];
    public virtual IReadOnlyList<SceneEnumObservation> EnumObservations=>[];
    public virtual IReadOnlyList<Presentation.SceneEnumBinding> EnumBindings=>[];
    public virtual IReadOnlyList<SceneTimerObservation> TimerObservations=>[];
    public virtual IReadOnlyList<SceneOscillatorObservation> OscillatorObservations=>[];
    public virtual IReadOnlyList<Presentation.SceneOscillatorColour> OscillatorColourAnimations=>[];
    public virtual IReadOnlyList<Presentation.SceneColourFollow> FollowingColours=>[];
    public virtual IReadOnlyList<Presentation.SceneTranslationAnimation> TranslationAnimations=>[];
    public virtual IReadOnlyList<Presentation.SceneLightCone> LightCones=>[];
    public virtual IReadOnlyList<Presentation.SceneSpectralColour> SpectralColours=>[];
    public virtual IReadOnlyList<Presentation.SceneScalarRotation> ScalarRotations=>[];
    public virtual IReadOnlyList<Presentation.SceneTimerColour> TimerColours=>[];
    public virtual IReadOnlyList<Presentation.SceneScalarExtent> ScalarExtents=>[];
    public virtual IEnumerable<ElectricalSourceDeclaration> ElectricalSources => [];
    public virtual IEnumerable<ElectricalGate> ElectricalGates => [];
    public virtual IEnumerable<ElectricalRoute> ElectricalRoutes => [];
    public virtual IReadOnlyList<MechanicalBinding> MechanicalBindings=>[];
    public bool HasOutputSocket => System.Linq.Enumerable.Any(ConnectionPorts,
        p => p.Direction is PortDirection.Output or PortDirection.Bidirectional);
    public virtual RopeAttachmentKind RopeAttachment => RopeAttachmentKind.None;
    public virtual void AdvanceRope(double distance) { }
    public virtual Physics.ContactMaterial InitialContactMaterial => new(Dynamic?Bounce:1,.1,.3);
    public virtual ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new System.ArgumentException("Unsupported activation command.");
        return ActivationDisposition.Immediate;
    }
    public virtual bool CanSendActivation => false;
    public virtual bool CanReceiveActivation => false;

    // Activation is a latched command, not an electrical source.
    // New families expose distinct typed sockets for supply, control and drive.
    public virtual IEnumerable<ConnectionPort> ConnectionPorts
    {
        get
        {
            if (CanSendActivation) yield return new(SocketId.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, Vector3.Zero);
            if (CanReceiveActivation) yield return new(SocketId.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, Vector3.Zero);
        }
    }

    public void Configure(PartSpec specification)
    {
        RequireConstructionEdit();
        System.ArgumentNullException.ThrowIfNull(specification);
        var opticalIdentity=new OpticalPathOwner(specification.Id);
        var position=ReadConstructionPosition(specification.Position);
        var basis=SceneOrientation.Present(specification.Orientation);
        var difficulty=CopyDifficulty(specification.Difficulty);
        System.ArgumentNullException.ThrowIfNull(specification.Properties);
        var velocity = ReadInitialVelocity(specification.InitialVelocity);
        System.ArgumentNullException.ThrowIfNull(specification.InternalBodies);
        var internalVelocities = InternalBodyRoles.ToDictionary(role => role, _ => Vector3.Zero);
        var suppliedRoles = new HashSet<InternalBodyRole>();
        foreach (var body in specification.InternalBodies)
        {
            if (body is null || !System.Enum.IsDefined(body.Role) ||
                !internalVelocities.ContainsKey(body.Role) || !suppliedRoles.Add(body.Role))
                throw new System.ArgumentException("Internal body roles must be declared, defined and unique.", nameof(specification));
            internalVelocities[body.Role] = ReadInitialVelocity(body.InitialVelocity);
        }
        var fields=new Dictionary<string,float>();
        foreach(var pair in Definition.Parameters) fields.Add(pair.Key,pair.Value);
        foreach(var pair in specification.Properties) fields[pair.Key]=pair.Value;
        var parameters=BindParameters(fields);
        ValidateParameters(parameters);
        var parameterState=PrepareParameterState(parameters);
        System.ArgumentNullException.ThrowIfNull(parameterState);
        var internalMotion=_constructionPhase==ConstructionPhase.Built?PrepareInternalInitialMotion(internalVelocities):[];
        Uid = specification.Id;
        OpticalIdentity=opticalIdentity;
        Name = Uid;
        Locked = specification.Locked;
        InitialVelocity = velocity;
        _configuredInternalVelocities = internalVelocities;
        Difficulty=difficulty;
        _parameters=parameters;
        _parameterState=parameterState;
        Position=position;
        Basis=basis;
        foreach(var entry in internalMotion) entry.Part.InitialVelocity=entry.Velocity;
    }

    private enum ConstructionPhase { Unbuilt, Building, Built, Faulted }
    private ConstructionPhase _constructionPhase;
    public override void _Ready() => EnsureConstructed();
    internal void EnsureConstructed()
    {
        if(_constructionPhase==ConstructionPhase.Built)return;
        if(_constructionPhase!=ConstructionPhase.Unbuilt)
            throw new System.InvalidOperationException("Part construction is reentrant or previously failed.");
        RequireConstructionEdit();
        _constructionPhase=ConstructionPhase.Building;
        try
        {
            Visual = new Node3D { Name = "Visual" };
            AddChild(Visual);
            Build();
            foreach(var body in InternalBodies) body.EnsureConstructed();
            ApplyInternalInitialMotion();
            _highlight = PartArt.Ring(this, PickRadius, .025f, new("#efffbd"), new(0, 0, .02f));
            _highlight.RotationDegrees = new(90, 0, 0);
            _highlight.Visible = false;
            _constructionPhase=ConstructionPhase.Built;
        }
        catch
        {
            _constructionPhase=ConstructionPhase.Faulted;
            throw;
        }
    }
    protected void UpdateSelectionRadius(float radius)
    {
        PickRadius = radius;
        if (_highlight?.Mesh is TorusMesh ring)
        {
            ring.OuterRadius = radius + .025f;
            ring.InnerRadius = Mathf.Max(.001f, radius - .025f);
        }
    }
    public void ValidateParameters() => ValidateParameters(_parameters);
    /// <summary>Validate candidate data without modifying this part or runtime state.</summary>
    protected virtual void ValidateParameters(PartParameterValues parameters) { }
    private PartParameterState _parameterState=PartParameterState.Empty;
    protected T ReadParameterState<T>() where T:class => _parameterState.Read<T>();
    /// <summary>Allocate/prepare derived state without modifying the live part; commit only installs the returned aggregate.</summary>
    protected virtual PartParameterState PrepareParameterState(PartParameterValues parameters) => PartParameterState.Empty;
    protected static void RequireParameters<TParameter>(PartParameterValues parameters) where TParameter : struct, System.Enum
    {
        foreach(var parameter in System.Enum.GetValues<TParameter>()) _=parameters.Read(parameter);
    }
    protected void WriteParameter<TParameter>(TParameter parameter,float value) where TParameter : struct, System.Enum
    {
        RequireConstructionEdit();
        _parameters.Write(parameter,value);
    }
    protected virtual void Build() { }
    protected void AddBox(Vector3 at, Vector3 size, Color color, bool draw = true)
    {
        Boxes.Add(new(at, size * .5f, MachinePart.RootBody));
        if (draw) PartArt.Box(Visual, size, color, at);
    }
    // Fixed-tick control changes commit before optical/electrical network snapshots.
    public virtual void BeforeNetworks(MachineWorld world) { }
    public virtual void UpdateAssistance(float precision) { }
    public bool IsSelected { get; private set; }
    public void SetSelected(bool value) { IsSelected=value; _highlight.Visible=value; }
    public PartSpec Serialize() => new()
    {
        Id = Uid, Kind = Definition.Id, Locked = Locked,
        Position = [Position.X, Position.Y, Position.Z],
        InitialVelocity = [InitialVelocity.X, InitialVelocity.Y, InitialVelocity.Z],
        InternalBodies = InternalBodyRoles.Select(role =>
        {
            var velocity = InternalBody(role).InitialVelocity;
            return new InternalBodySpec { Role = role, InitialVelocity = [velocity.X, velocity.Y, velocity.Z] };
        }).ToList(),
        Orientation = SceneOrientation.Capture(Basis),
        Properties = new(Properties), Difficulty = Difficulty.ToList()
    };
}

