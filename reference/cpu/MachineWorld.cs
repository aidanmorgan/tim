using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace CuriousContraptions;

public enum MachineWorldPhase { Idle, Stepping, Replacing }

/// <summary>Gameplay clock and scene boundary for one persistent shared physics world.</summary>
public partial class MachineWorld : Node3D
{
    public MachineWorldPhase Phase { get; private set; }=MachineWorldPhase.Idle;
    public const float Tick = 1f / 120;
    public const int Substeps = 4;
    public const int MaximumAcousticEventsPerTick = 1024;
    public const int MaximumPendingOscillatorEvents=4096;
    private CommittedEventStream<SimulationOscillatorPulse>? _oscillatorEvents;
    internal CommittedEventStream<SimulationOscillatorPulse> OscillatorEvents=>_oscillatorEvents??throw new InvalidOperationException("Run has not started.");
    internal Presentation.AnimationImpulseRead ReadOscillatorFeedback(SceneOscillatorKey source,int bindingIndex)=>
        (_animations??throw new InvalidOperationException("Run has not started.")).ReadOscillatorFeedback(source,bindingIndex);
    public const float Quantum = 1f / 65536;
    private CommittedPoseBuffer? _committedPoses;
    private CompliantContactState[] _compliantReads=[];
    private SceneScalarObservations? _scalarObservations;
    private SceneBooleanObservations? _booleanObservations;
    private SceneEnumObservations? _enumObservations;
    private CommittedDisplayClock? _displayClock;
    public double DisplaySimulationTime { get; private set; }
    private enum PresentationProcessOrder { PhysicalParents=-100 }

    public PoseReadLease ReadCommittedPoses()
    {
        RequireNotReplacing();
        return (_committedPoses??throw new InvalidOperationException("Run has not published poses.")).Acquire();
    }
    private ScenePhysicsAssembly? _physicsAssembly;
    private SceneMechanicalTransferBindings? _transferBindings;
    public SceneMechanicalTransferBindings TransferBindings=>_transferBindings??
        throw new InvalidOperationException("Run has not started.");
    private PhysicsWorld? _physics;
    private SimulationTimers? _timers;
    private SimulationTransaction? _simulationTransaction;
    private Dictionary<SceneTimerKey,SimulationTimerId> _timerIds=new();
    private SceneTimerDeclaration[] _sceneTimers=[];
    public SimulationTimers Timers=>_timers??throw new InvalidOperationException("Run has not started.");
    public SimulationTimerState ReadTimer(SceneTimerKey key)=>Timers.Read(_timerIds[key]);
    public double TimerProgress(SceneTimerKey key)=>Timers.Progress(_timerIds[key]);
    public bool TriggerTimer(SceneTimerKey key)=>Timers.Trigger(_timerIds[key],Ticks);
    private SimulationOscillators? _oscillators;
    private Dictionary<SceneOscillatorKey,SimulationOscillatorId> _oscillatorIds=new();
    private SceneOscillatorDeclaration[] _sceneOscillators=[];
    private SimulationOscillatorInput[] _oscillatorInputs=[];
    public SimulationOscillators Oscillators=>_oscillators??throw new InvalidOperationException("Run has not started.");
    public SimulationOscillatorState ReadOscillator(SceneOscillatorKey key)=>Oscillators.Read(_oscillatorIds[key]);
    public double OscillatorProgress(SceneOscillatorKey key)=>Oscillators.Progress(_oscillatorIds[key]);
    private SimulationLatches? _latches;
    private Dictionary<SceneLatchKey,SimulationLatchId> _latchIds=new();
    public SimulationLatches Latches=>_latches??throw new InvalidOperationException("Run has not started.");
    public SimulationLatchState ReadLatch(SceneLatchKey key)=>Latches.Read(_latchIds[key]);
    public void SubmitLatch(SceneLatchKey key,SimulationLatchCommand command)=>Latches.Submit(_latchIds[key],command,Ticks);
    private SimulationCounters? _counters;
    private Dictionary<SceneCounterKey,SimulationCounterId> _counterIds=new();
    public SimulationCounters Counters=>_counters??throw new InvalidOperationException("Run has not started.");
    public SimulationCounterState ReadCounter(SceneCounterKey key)=>Counters.Read(_counterIds[key]);
    public SimulationCounterResult IncrementCounter(SceneCounterKey key)=>Counters.Increment(_counterIds[key]);
    private SceneImpactEffect[] _impactEffects=[];
    private readonly List<PhysicsWrenchCommand> _forces=new();
    private readonly List<PhysicsMotorCommand> _motors=new();
    private readonly List<AxialElasticLoad> _elasticLoads=new();
    private readonly List<AxialEffortLoad> _effortLoads=new();
    private readonly List<AxialDampingLoad> _dampingLoads=new();
    private readonly List<BodyDragLoad> _dragLoads=new();
    private readonly List<PlanarGuideLoad> _guideLoads=new();
    private readonly List<MechanicalTransferLoad> _transferLoads=new();
    public void AddTransferLoad(MechanicalTransferLoad load)
    {
        ArgumentNullException.ThrowIfNull(load);
        if(Phase!=MachineWorldPhase.Stepping)
            throw new InvalidOperationException("Transfer declarations require an active gameplay tick.");
        _transferLoads.Add(load);
    }
    private readonly List<RotaryCaptureDeclaration> _rotaryLoads=new();
    public void AddRotaryLoad(RotaryCaptureDeclaration load)
    {
        ArgumentNullException.ThrowIfNull(load);
        if(Phase!=MachineWorldPhase.Stepping)
            throw new InvalidOperationException("Rotary declarations require an active gameplay tick.");
        _rotaryLoads.Add(load);
    }
    private readonly List<PhysicsImpact> _tickImpacts=new();
    public ReadOnlySpan<PhysicsImpact> TickImpacts=>CollectionsMarshal.AsSpan(_tickImpacts);
    public PhysicsJoint CurrentJoint(SceneJointKey key)
    {
        var id=PhysicsAssembly.JointId(key);
        foreach(var joint in Physics.Joints) if(joint.Id==id) return joint;
        throw new InvalidOperationException("Scene joint is not attached to the live world.");
    }
    public void AddGuideLoad(SceneBodyKey body,SceneBodyKey frame,double minimumHeight,double maximumHeight,
        double halfX,double halfZ,double maximumAcceleration,double minimumSupportHeight)
        =>_guideLoads.Add(new(PhysicsAssembly.Body(body).Id,PhysicsAssembly.Body(frame).Id,
            minimumHeight,maximumHeight,halfX,halfZ,maximumAcceleration,minimumSupportHeight));
    public void AddDragLoad(SceneBodyKey key,double linearRate,double angularRate)
        =>_dragLoads.Add(new(PhysicsAssembly.Body(key).Id,linearRate,angularRate));
    public void AddEffortLoad(AxialEffortLoad load)=>_effortLoads.Add(load);
    public void AddDampingLoad(SceneJointKey key,double negativeCoefficient,double positiveCoefficient)
    {
        if(CurrentJoint(key) is not PhysicsFrameJoint frame)
            throw new ArgumentException("Damping loads require an axial frame joint.",nameof(key));
        _dampingLoads.Add(new(frame.Id,frame.Kind,negativeCoefficient,positiveCoefficient));
    }
    public void AddElasticLoad(SceneJointKey key,AxialElasticPotential potential)
    {
        if(CurrentJoint(key) is not PhysicsFrameJoint frame)
            throw new ArgumentException("Elastic loads require an axial frame joint.",nameof(key));
        _elasticLoads.Add(new(frame.Id,frame.Kind,potential));
    }
    public void DriveMotor(SceneJointKey joint,double targetSpeed,double maximumEffort,double availableWork,double maximumPower)
    {
        var current=CurrentJoint(joint);
        if(current is not PhysicsFrameJoint { Kind: FrameJointKind.Hinge or FrameJointKind.Slider })
            throw new ArgumentException("Motor commands require a live hinge or slider.",nameof(joint));
        _motors.Add(new(current.Id,targetSpeed,maximumEffort,availableWork,maximumPower));
    }
    public ScenePhysicsAssembly PhysicsAssembly=>_physicsAssembly??throw new InvalidOperationException("Run has not started.");
    internal bool HasPhysicsState=>_physics is not null;
    public BodyColliderGeometry CollisionGeometry(SceneBodyKey key)=>PhysicsAssembly.CollisionGeometry(Physics,key);
    public void ReplaceCollisionGeometry(SceneBodyKey key,BodyColliderGeometry geometry,
        ContactMaterial material,CollisionParticipation participation)
        =>PhysicsAssembly.ReplaceCollider(Physics,key,geometry,material,participation);
    public PhysicsWorld Physics=>_physics??throw new InvalidOperationException("Run has not started.");
    public PhysicsStepResult LastPhysicsStep { get; private set; }=new(0,0,0,0,0,0,0,0,0,default,default,default);
    public event Action? Solved;
    public PartRegistry Registry { get; } = new();
    private readonly List<MachinePart> _parts = new();
    public IReadOnlyList<MachinePart> Parts { get; }
    private readonly List<MachinePart> _bodies = new();
    public IReadOnlyList<MachinePart> Bodies { get; }
    public MachineWorld()
    {
        Parts=_parts.AsReadOnly();
        Bodies=_bodies.AsReadOnly();
        Connections=_connections.AsReadOnly();
        Ropes=_ropes.AsReadOnly();
    }
    public IEnumerable<MachinePart> CollisionParts
    {
        get
        {
            foreach (var part in Parts)
            {
                yield return part;
                foreach (var body in part.InternalBodies) yield return body;
            }
        }
    }
    private readonly List<ConnectionSpec> _connections = new();
    public IReadOnlyList<ConnectionSpec> Connections { get; }
    public IReadOnlyList<GoalSpec> Objectives { get; private set; } = Array.Empty<GoalSpec>();
    private readonly List<RopePath> _ropes = new();
    public IReadOnlyList<RopePath> Ropes { get; }
    public SortedDictionary<MachineEvent, int> Events { get; } = new();
    private List<PartSpec> _placementTargets = new();
    private List<PartAssistance.Correction> _corrections = new();
    internal IReadOnlyList<PartAssistance.Correction> PlacementCorrections=>_corrections;
    private bool _running;
    public bool Running
    {
        get=>_running;
        set {RequireNotReplacing();_running=value;}
    }
    private void RequireNotReplacing()
    {
        if(Phase==MachineWorldPhase.Replacing)
            throw new InvalidOperationException("World replacement must finish before this operation.");
    }
    public bool Won { get; private set; }
    public int Ticks { get; private set; }
    private float _precision=.45f, _gravity=9.81f, _pressure=1;
    private bool _realistic;
    public float Precision { get=>_precision; set {RequireIdle();_precision=value;} }
    public float Gravity { get=>_gravity; set {RequireIdle();_gravity=value;} }
    public float Pressure { get=>_pressure; set {RequireIdle();_pressure=value;} }
    public bool Realistic { get=>_realistic; set {RequireIdle();_realistic=value;} }
    private MachineData? _initial;
    public bool HasConstructionSnapshot => _initial is not null;
    private Presentation.SceneAnimationRun? _animations;
    public void EmitOccurrence(Presentation.SceneOccurrenceKey source,double strength)
    {
        if(Phase!=MachineWorldPhase.Stepping)throw new InvalidOperationException("Occurrences require an active simulation tick.");
        (_animations??throw new InvalidOperationException("Run has not started.")).Occurrences.Emit(source,strength);
    }
    internal Presentation.SceneOccurrenceRead ReadOccurrenceFeedback(Presentation.SceneOccurrenceKey source,int binding)=>
        (_animations??throw new InvalidOperationException("Run has not started.")).Occurrences.Read(source,binding);
    internal Presentation.SceneWavefrontRead ReadAcousticWavefronts(MachinePart part)=>
        (_animations??throw new InvalidOperationException("Run has not started.")).AcousticMotion.Wavefronts.Read(part);
    internal Presentation.AnimationOscillationRead ReadAcousticMotion(MachinePart part,int binding)=>
        (_animations??throw new InvalidOperationException("Run has not started.")).AcousticMotion.Read(part,binding);
    private Presentation.SceneAcousticRun? _acoustics;
    private ElectricalNetwork? _electrical;
    private OpticalPathVisual _opticalVisual=null!;
    private readonly Presentation.SceneOpticalPreviews _opticalPreviews=new();
    public IReadOnlyList<OpticalSegment> OpticalPaths { get; private set; }=[];
    public void SetOpticalPaths(IReadOnlyList<OpticalSegment> paths)
    {
        RequireNotReplacing();
        OpticalPaths=OpticalPathVisual.Merge(paths);
    }
    public override void _Ready()
    {
        ProcessPriority=(int)PresentationProcessOrder.PhysicalParents;
        Registry.Discover();
        _opticalVisual=new OpticalPathVisual {Name="OpticalPaths"};
        AddChild(_opticalVisual);
    }
    public override void _ExitTree()
    {
        _animations?.Occurrences.Remove();
        _oscillatorEvents?.Remove();_oscillatorEvents=null;
        _committedPoses?.Remove();
        _committedPoses=null;_displayClock=null;DisplaySimulationTime=0;
    }
    public override void _Process(double delta)=>PresentFrame(delta,Engine.GetPhysicsInterpolationFraction());
    internal void PresentFrame(double delta,double fraction)
    {
        RequireIdle();
        if(!double.IsFinite(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
#if PLAYTEST
        var recording=_committedPoses is not null;
        if(recording)_performance.BeginFrame(Ticks);
        var outcome=PerformanceOutcome.Failed;
#endif
        try
        {
            if(_committedPoses is not null)
            {
#if PLAYTEST
                _performance.Begin(PerformanceStage.PhysicalPresentation);
#endif
                using var poses=_committedPoses.Acquire();
                DisplaySimulationTime=_displayClock!.Select(poses,Running?DisplayPlayback.Running:DisplayPlayback.Stopped,fraction);
                _physicsAssembly!.PresentCommitted(poses,DisplaySimulationTime
#if PLAYTEST
                    ,_performance
#endif
                );
#if PLAYTEST
                _performance.End(PerformanceStage.PhysicalPresentation);
#endif
            }
#if PLAYTEST
            if(recording)_performance.Begin(PerformanceStage.Animation);
#endif
            if(_animations is { } animations)
            {
                animations.ConsumeOscillatorEvents(OscillatorEvents);
                animations.Present(delta
#if PLAYTEST
                    ,recording?_performance:null
#endif
                );
            }
#if PLAYTEST
            if(recording)_performance.End(PerformanceStage.Animation);
            if(recording)_performance.Begin(PerformanceStage.SceneSubmission);
#endif
            _animations?.PresentLightCones(this);
            _opticalVisual.Refresh(OpticalPaths);
            _opticalPreviews.Present(this);
#if PLAYTEST
            if(recording)_performance.End(PerformanceStage.SceneSubmission);
            outcome=PerformanceOutcome.Completed;
#endif
        }
        finally
        {
#if PLAYTEST
            if(recording)_performance.EndFrame(outcome);
#endif
        }
    }

    public void LoadMachine(MachineData input)
    {
        RequireIdle();
        Phase=MachineWorldPhase.Replacing;
        try {ReplaceMachine(input);}
        finally {Phase=MachineWorldPhase.Idle;}
    }

    private void ReplaceMachine(MachineData input)
    {
        var data = MachineCodec.Clone(input);
        var nextGeneration=new WorldGeneration(checked(ControlGeneration.Value+1));
        var candidates=CreateValidatedParts(data);
        List<MachinePart> bodies;
        IReadOnlyList<GoalSpec> objectives;
        try
        {
            foreach(var part in candidates)
            {
                part.EnsureConstructed();
                part.UpdateAssistance(Precision);
            }
            candidates.Sort((a,b)=>string.CompareOrdinal(a.Uid,b.Uid));
            bodies=candidates.Where(part=>part.Dynamic).Concat(candidates.SelectMany(part=>part.InternalBodies)).ToList();
            bodies.Sort((a,b)=>string.CompareOrdinal(a.Uid,b.Uid));
            objectives=Array.AsReadOnly(data.Goals.ToArray());
            _parts.EnsureCapacity(candidates.Count);
            _bodies.EnsureCapacity(bodies.Count);
            _connections.EnsureCapacity(data.Connections.Count);
        }
        catch
        {
            foreach(var part in candidates)part.Free();
            throw;
        }
        var oldControlRevision=ControlRevision;
        _committedPoses?.Remove();_committedPoses=null;_displayClock=null;DisplaySimulationTime=0;
        _acoustics?.Remove();
        _acoustics=null;
        _oscillatorEvents?.Remove();_oscillatorEvents=null;
        _electrical=null;_scalarObservations=null;_booleanObservations=null;_enumObservations=null;
        _animations?.Remove();
        _physicsAssembly?.RemovePresentation();
        _animations = null;
        _initial = null;
        _running = Won = false;
        OpticalPaths=[];
        _opticalVisual.Refresh([]);
        Ticks = 0;
        Events.Clear();
        _corrections.Clear();
        _simulationTransaction=null;_timers=null;_timerIds.Clear();_sceneTimers=[];
        _oscillators=null;_oscillatorIds.Clear();_sceneOscillators=[];_oscillatorInputs=[];
        _counters=null;_counterIds.Clear();
        _latches=null;_latchIds.Clear();
        _compliantReads=[];
        _physics=null; _physicsAssembly=null; _transferBindings=null; _forces.Clear(); _motors.Clear(); _elasticLoads.Clear(); _effortLoads.Clear(); _dampingLoads.Clear(); _dragLoads.Clear(); _guideLoads.Clear(); _transferLoads.Clear(); _rotaryLoads.Clear(); _tickImpacts.Clear(); _impactEffects=[];
        LastPhysicsStep=new(0,0,0,0,0,0,0,0,0,default,default,default);
        _placementTargets = data.PlacementTargets;
        foreach (var part in Parts) { RemoveChild(part); part.Free(); }
        _parts.Clear();
        _bodies.Clear();
        _connections.Clear();
        _connections.AddRange(data.Connections);
        _ropes.Clear();
        Objectives = objectives;
        _gravity = data.Gravity;
        _pressure = data.Pressure;
        foreach(var part in candidates)AddChild(part);
        _parts.AddRange(candidates);
        _bodies.AddRange(bodies);
        _binaryInputIds.Clear();_binaryInputs=[];
        _scalarInputIds.Clear();_scalarInputs=[];_scalarDeclarations=[];
        _controlCommands.AdvanceGeneration(nextGeneration,oldControlRevision);
    }

    public void ValidateMachine(MachineData data)
    {
        foreach(var part in CreateValidatedParts(data))part.Free();
    }

    private List<MachinePart> CreateValidatedParts(MachineData data)
    {
        foreach (var goal in data.Goals)
        {
            if(!Enum.IsDefined(goal.Type)||goal.Type==GoalKind.Unknown)
                throw new ArgumentException("Unsupported goal kind.");
            if (!float.IsFinite(goal.MinimumDelaySeconds) || goal.MinimumDelaySeconds < 0 || goal.MinimumDelaySeconds > 120)
                throw new ArgumentException("Goal delay must be finite and between zero and 120 seconds.");
        }
        // Reject unsupported mechanical graphs before replacing the current machine.
        var candidates = new List<MachinePart>();
        try
        {
            foreach (var entry in data.Parts) candidates.Add(Registry.Create(entry));
            ValidateInstanceIds(candidates);
            foreach (var link in data.Connections)
            {
                var source = candidates.SingleOrDefault(p => p.Uid == link.From);
                var target = candidates.SingleOrDefault(p => p.Uid == link.To);
                if (source == null || target == null ||
                    !ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _))
                    throw new ArgumentException("Invalid connection endpoints or socket type.");
            }
            MechanicalNetwork.Validate(candidates, data.Connections);
            RopeNetwork.Build(candidates, data.Connections);
        }
        catch
        {
            foreach(var candidate in candidates)candidate.Free();
            throw;
        }
        return candidates;
    }

    private static void ValidateInstanceIds(IEnumerable<MachinePart> parts)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (!ids.Add(part.Uid)) throw new ArgumentException("Duplicate instance ID: " + part.Uid);
            foreach (var role in part.InternalBodyRoles)
            {
                var id = part.InternalBodyId(role);
                if (!ids.Add(id)) throw new ArgumentException("Duplicate internal-body instance ID: " + id);
            }
        }
    }

    private void RequireIdle()
    {
        if(Phase!=MachineWorldPhase.Idle)
            throw new InvalidOperationException("Machine lifecycle changes require a completed gameplay tick.");
    }

    private void RequireConstruction()
    {
        RequireIdle();
        if(HasPhysicsState)
            throw new InvalidOperationException("Construction changes require Reset before editing captured physics.");
    }

    public MachinePart AddPart(PartSpec entry)
    {
        RequireConstruction();
        var part = Registry.Create(entry);
        try { AttachPart(part); return part; }
        catch { if(part.GetParent() is null) part.Free(); throw; }
    }

    /// <summary>Transfer a configured, unparented part into construction ownership.</summary>
    public void AttachPart(MachinePart part)
    {
        RequireConstruction();
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(part.Definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(part.Uid);
        if(part.GetParent() is not null) throw new ArgumentException("Part already has an owner.",nameof(part));
        ValidateInstanceIds(Parts.Append(part));
        part.EnsureConstructed();
        part.UpdateAssistance(Precision);
        AddChild(part);
        _parts.Add(part);
        _parts.Sort((a, b) => string.CompareOrdinal(a.Uid, b.Uid));
        if (part.Dynamic) _bodies.Add(part);
        _bodies.AddRange(part.InternalBodies);
        _bodies.Sort((a, b) => string.CompareOrdinal(a.Uid, b.Uid));
    }
    public void RemovePart(MachinePart part)
    {
        RequireConstruction();
        if(!Parts.Contains(part)) throw new ArgumentException("Part is not owned by this world.",nameof(part));
        _parts.Remove(part);
        _bodies.Remove(part);
        foreach (var body in part.InternalBodies) _bodies.Remove(body);
        _connections.RemoveAll(link => link.From == part.Uid || link.To == part.Uid);
        RemoveChild(part);
        part.Free();
    }
    /// <summary>Detached authored construction at an idle boundary; runtime state is not a save format.</summary>
    public MachineData Snapshot()
    {
        RequireIdle();
        if(HasPhysicsState)
            return MachineCodec.Clone(_initial??throw new InvalidOperationException("Captured physics requires retained construction."));
        return MachineCodec.Clone(new()
        {
        Parts = Parts.Select(p => p.Serialize()).ToList(), PlacementTargets = _placementTargets,
        Connections = Connections.ToList(), Goals = Objectives.ToList(), Gravity = Gravity, Pressure = Pressure
        });
    }
    public void Start()
    {
        RequireConstruction();
        var electrical=new ElectricalNetwork(this);
        MechanicalNetwork.Validate(Parts, Connections);
        var routes=RopeNetwork.Build(Parts,Connections);
        _ropes.Clear();
        _ropes.AddRange(routes);
        _initial = Snapshot();
        _corrections = PartAssistance.Prepare(Parts, _placementTargets, Precision);
        foreach (var part in Parts) part.UpdateAssistance(Precision);
        foreach (var part in Parts) part.PrepareForPhysicsCapture();
        var assembly=WorldGeometry.CapturePhysicsAssembly(this,Ropes);
        var transferBindings=new SceneMechanicalTransferBindings(assembly,Parts);
        var settings=new PhysicsWorldSettings(new(0,-Gravity,0));
        var effects=assembly.Objects.ToArray().Where(o=>assembly.Owner(o.Body.Id) is not null)
            .Select(o=>new SceneImpactEffect(assembly.Key(o.Body.Id),o.Body.Id)).ToArray();
        var physics=new PhysicsWorld(effects,assembly.Objects.ToArray(),assembly.InitialJoints.ToArray(),settings);
        if(physics.FindPlacementOverlap() is { } overlap)
            throw new ScenePhysicsOverlapException(assembly.Key(overlap.Pair.A),assembly.Key(overlap.Pair.B));
        physics.ReplaceSurfaces(assembly.Surfaces.ToArray());
        physics.InstallServos(Parts.SelectMany(part=>part.PhysicsServos)
            .Select(servo=>new PhysicsServoDeclaration(assembly.JointId(servo.Joint),servo.MaximumSpeed,
                servo.Acceleration,servo.MaximumEffort,servo.MaximumPower)));
        physics.InstallEnergyStores(Parts.SelectMany(part=>part.PhysicsEnergyStores)
            .Select(store=>new PhysicsEnergyStoreDeclaration(assembly.Body(store.Body).Id,store.Capacity,store.InitialEnergy)));
        physics.InstallPassageSensors(Parts.SelectMany(part=>part.PhysicsPassageSensors)
            .Select(sensor=>new PhysicsPassageSensor(assembly.Body(sensor.Frame).Id,sensor.Radius,sensor.RearmClearance)));
        physics.InstallContactLoadSensors(Parts.SelectMany(part=>part.PhysicsContactLoadSensors)
            .Select(sensor=>new PhysicsContactLoadSensor(assembly.Body(sensor.Frame).Id,sensor.Region,sensor.LocalNormal,sensor.MinimumAlignment,sensor.MinimumMass)));
        physics.InstallTiltSensors(Parts.SelectMany(part=>part.PhysicsTiltSensors)
            .Select(sensor=>new PhysicsTiltSensor(assembly.Body(sensor.Body).Id,sensor.LocalDirection,sensor.ThresholdCosine)));
        physics.InstallResidenceSensors(Parts.SelectMany(part=>part.PhysicsResidenceSensors(this))
            .Select(sensor=>new PhysicsResidenceSensor(new(assembly.Body(sensor.Frame).Id,assembly.Body(sensor.Body).Id),
                sensor.Region,sensor.MaximumSpeed,sensor.Dwell)));
        physics.ReplaceLoads(new()
        {
            Compliant=SceneCompliantSurfaces.Bind(Parts,assembly),
            Springs=Parts.SelectMany(part=>part.PhysicsSprings).Select(spring=>
            {
                var guide=(PhysicsFrameJoint)assembly.InitialJoints.ToArray().Single(j=>j.Id==assembly.JointId(spring.Guide));
                return new LatchedSpringLoad(guide.Id,assembly.JointId(spring.Transmission),
                    spring.Stiffness,(guide.TravelRange??throw new InvalidOperationException("Spring guide requires physical stops.")).Upper,spring.Stroke);
            }).ToArray()
        });
        var timerIds=new Dictionary<SceneTimerKey,SimulationTimerId>();
        var sceneTimers=new List<SceneTimerDeclaration>();
        var timerDeclarations=new List<SimulationTimerDeclaration>();
        foreach(var part in Parts)
        foreach(var timer in part.SimulationTimers)
        {
            if(timer.Key.Owner!=part)throw new ArgumentException("Timer declaration refers to a foreign owner.");
            var id=new SimulationTimerId(sceneTimers.Count);
            timerIds.Add(timer.Key,id);sceneTimers.Add(timer);
            timerDeclarations.Add(new(id,timer.DurationTicks(Tick),timer.Completion,timer.Boundary));
        }
        var timers=new SimulationTimers(timerDeclarations);
        var oscillatorIds=new Dictionary<SceneOscillatorKey,SimulationOscillatorId>();
        var sceneOscillators=new List<SceneOscillatorDeclaration>();
        var oscillatorDeclarations=new List<SimulationOscillatorDeclaration>();
        foreach(var part in Parts)
        foreach(var oscillator in part.SimulationOscillators)
        {
            if(oscillator.Key.Owner!=part)throw new ArgumentException("Oscillator declaration refers to a foreign owner.");
            var id=new SimulationOscillatorId(sceneOscillators.Count);
            oscillatorIds.Add(oscillator.Key,id);sceneOscillators.Add(oscillator);
            oscillatorDeclarations.Add(new(id,oscillator.IntervalTicks(Tick)));
        }
        var oscillators=new SimulationOscillators(oscillatorDeclarations);
        var counterIds=new Dictionary<SceneCounterKey,SimulationCounterId>();
        var counterDeclarations=new List<SimulationCounterDeclaration>();
        foreach(var part in Parts)
        foreach(var counter in part.SimulationCounters)
        {
            counter.Validate();
            if(counter.Key.Owner!=part)throw new ArgumentException("Counter declaration refers to a foreign owner.");
            var id=new SimulationCounterId(counterDeclarations.Count);
            counterIds.Add(counter.Key,id);counterDeclarations.Add(new(id,counter.Target));
        }
        var counters=new SimulationCounters(counterDeclarations);
        var latchIds=new Dictionary<SceneLatchKey,SimulationLatchId>();
        foreach(var part in Parts)
        foreach(var latch in part.SimulationLatches)
        {
            latch.Validate();
            if(latch.Key.Owner!=part)throw new ArgumentException("Latch declaration refers to a foreign owner.");
            latchIds.Add(latch.Key,new SimulationLatchId(latchIds.Count));
        }
        var latches=new SimulationLatches(latchIds.Values);
        var binaryIds=new Dictionary<SceneBinaryInputKey,Bridge.BinaryInputId>();
        var binaryStates=new List<SimulationState<Bridge.BinaryInputState>>();
        foreach(var part in Parts)
        foreach(var input in part.BinaryInputs)
        {
            if(input.Key.Owner!=part||input.Key.Slot is null||!Enum.IsDefined(input.Initial))
                throw new ArgumentException("Invalid binary input declaration.");
            binaryIds.Add(input.Key,new(binaryStates.Count));binaryStates.Add(new(input.Initial));
        }
        var scalarIds=new Dictionary<SceneScalarInputKey,Bridge.ScalarInputId>();
        var scalarStates=new List<SimulationState<double>>();
        var scalarDeclarations=new List<SceneScalarInputDeclaration>();
        foreach(var part in Parts)
        foreach(var input in part.ScalarInputs)
        {
            input.Validate();
            if(input.Key.Owner!=part)throw new ArgumentException("Scalar input refers to a foreign owner.");
            scalarIds.Add(input.Key,new(scalarStates.Count));scalarStates.Add(new(input.Initial));
            scalarDeclarations.Add(input);
        }
        var participants=new List<SimulationTransactionParticipant>
            {new TickObservablesCheckpoint(this),physics,timers,oscillators,counters,latches,_controlCommands};
        participants.AddRange(binaryStates);participants.AddRange(scalarStates);
        foreach(var part in CollisionParts)
        {
            participants.Add(part.BaseRuntimeCheckpoint);
            participants.AddRange(part.RuntimeState);
        }
        var simulationTransaction=new SimulationTransaction(participants);
        var acoustics = new Presentation.SceneAcousticRun(Parts,new(ControlGeneration,new(0),0),MaximumAcousticEventsPerTick);
        electrical.BindRuntime(new(assembly,physics,counters,counterIds,latches,latchIds,timers,timerIds,
            binaryIds.ToDictionary(pair=>pair.Key,pair=>binaryStates[pair.Value.Index])));
        electrical.BindPublication(assembly);
        var booleanObservations=new SceneBooleanObservations(Parts,assembly);
        var scalarObservations=new SceneScalarObservations(Parts,assembly,physics,timers,timerIds,oscillators,oscillatorIds);
        var poses=new CommittedPoseBuffer(new(ControlGeneration,new(0),0),assembly.CapturePublicationReads(physics),counters.PublicationReads,electrical.CapturePublicationReads(),scalarObservations.Capture(),timers.PublicationReads,booleanObservations.Capture());
        var compliantReads=new CompliantContactState[physics.Loads.Compliant.Count];
        physics.CopyCompliantContacts(compliantReads);poses.RegisterCompliantContacts(compliantReads);
        var enumObservations=new SceneEnumObservations(Parts,assembly);enumObservations.Register(poses);
        Presentation.SceneAnimationRun animations;
        using(var seed=poses.Acquire())animations=new(Parts,assembly,seed,counterIds,timerIds,oscillatorIds);
        _binaryInputIds=binaryIds;_binaryInputs=binaryStates.ToArray();
        _scalarInputIds=scalarIds;_scalarInputs=scalarStates.ToArray();_scalarDeclarations=scalarDeclarations.ToArray();
        _simulationTransaction=simulationTransaction;
        _latches=latches;_latchIds=latchIds;
        _counters=counters;_counterIds=counterIds;
        _timers=timers;_timerIds=timerIds;_sceneTimers=sceneTimers.ToArray();
        _oscillators=oscillators;_oscillatorIds=oscillatorIds;_sceneOscillators=sceneOscillators.ToArray();
        _oscillatorInputs=new SimulationOscillatorInput[_sceneOscillators.Length];
        _impactEffects=effects;
        _transferBindings=transferBindings;
        _physicsAssembly=assembly; _physics=physics; _tickImpacts.Clear();
        Ticks = 0;
        Won = false;
        Events.Clear();
#if PLAYTEST
        _performance.BeginRun();
#endif
        _compliantReads=compliantReads;
        _committedPoses=poses;_scalarObservations=scalarObservations;_booleanObservations=booleanObservations;_enumObservations=enumObservations;
        _displayClock=new(ControlGeneration);DisplaySimulationTime=0;
        _animations = animations;
        _oscillatorEvents=new(Presentation.SceneOscillatorFeedback.StreamId,new(ControlGeneration,new(0),0),MaximumPendingOscillatorEvents);
        _acoustics = acoustics;
        _electrical = electrical;
        Running = true;
    }
    public void Restore()
    {
        RequireIdle();
        if (_initial == null) return;
        LoadMachine(_initial);
    }

    public MachinePart? FindPart(string id) => _parts.Find(p => p.Uid == id);
    public ConnectionSpec? SuggestedConnection(MachinePart source, MachinePart target)
    {
        var options = ConnectionOptions(source, target);
        return options.Count == 1 ? options[0] : null;
    }
    public List<ConnectionSpec> ConnectionOptions(MachinePart source, MachinePart target)
    {
        var options = new List<ConnectionSpec>();
        if (source == target || !Parts.Contains(source) || !Parts.Contains(target)) return options;
        foreach (var output in source.ConnectionPorts)
        foreach (var input in target.ConnectionPorts)
        {
            var link = new ConnectionSpec { From = source.Uid, To = target.Uid,
                FromPort = output.Id, ToPort = input.Id, Type = output.Domain,
                RopeLength = output.Domain == ConnectionDomain.Rope
                    ? (source.Transform * output.LocalPosition).DistanceTo(target.Transform * input.LocalPosition) : null };
            if (!ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _)) continue;
            if (link.Type == ConnectionDomain.Mechanical &&
                !MechanicalNetwork.CanConnect(Parts, Connections.Append(link))) continue;
            if (link.Type == ConnectionDomain.Rope &&
                !RopeNetwork.CanConnect(Parts, Connections.Append(link))) continue;
            options.Add(link);
        }
        return options;
    }
    public bool Connect(MachinePart source, MachinePart target)
    {
        var link = SuggestedConnection(source, target);
        if (link == null) return false;
        var output = source.ConnectionPorts.Single(p => p.Id == link.FromPort);
        return Connect(source, link.FromPort!.Value, target, link.ToPort!.Value, output.Domain);
    }

    public bool Connect(MachinePart source, SocketId fromPort, MachinePart target,
        SocketId toPort, ConnectionDomain domain)
    {
        if (HasPhysicsState || !Parts.Contains(source) || !Parts.Contains(target)) return false;
        var link = new ConnectionSpec
        {
            From = source.Uid, To = target.Uid, Type = domain,
            FromPort = fromPort, ToPort = toPort
        };
        if (domain == ConnectionDomain.Rope)
        {
            var output = source.ConnectionPorts.SingleOrDefault(p => p.Id == fromPort);
            var input = target.ConnectionPorts.SingleOrDefault(p => p.Id == toPort);
            link = link with { RopeLength = (source.Transform * output.LocalPosition).DistanceTo(target.Transform * input.LocalPosition) };
        }
        if (!IsValidConnection(link)) return false;
        if (Connections.Any(c => c.From == link.From && c.To == link.To
            && c.Type == link.Type && ConnectionRules.TryResolve(c, source.ConnectionPorts,
                target.ConnectionPorts, out var a, out var b)
            && a.Id == fromPort && b.Id == toPort)) return false;
        if (domain == ConnectionDomain.Mechanical &&
            !MechanicalNetwork.CanConnect(Parts, Connections.Append(link))) return false;
        if (domain == ConnectionDomain.Rope &&
            !RopeNetwork.CanConnect(Parts, Connections.Append(link))) return false;
        _connections.Add(link);
        return true;
    }

    public bool Disconnect(ConnectionSpec link)
    {
        RequireConstruction();
        ArgumentNullException.ThrowIfNull(link);
        return _connections.Remove(link);
    }

    public bool IsValidConnection(ConnectionSpec link)
    {
        var source = FindPart(link.From);
        var target = FindPart(link.To);
        return source != null && target != null
            && ConnectionRules.TryResolve(link, source.ConnectionPorts, target.ConnectionPorts, out _, out _);
    }
    private enum ActivationDelivery { Receive, Emit }
    public void Activate(MachinePart source) => DispatchActivation(source, ActivationDelivery.Receive);
    internal void EmitActivation(MachinePart source) => DispatchActivation(source, ActivationDelivery.Emit);
    private void DispatchActivation(MachinePart source, ActivationDelivery delivery)
    {
        if (!Parts.Contains(source)) return;
        var pending = new Queue<(string Id, ActivationDelivery Delivery, ActivationCommand Command)>();
        var seen = new HashSet<(string, ActivationCommand)>();
        pending.Enqueue((source.Uid, delivery, ActivationCommand.Trigger));
        while (pending.TryDequeue(out var entry))
        {
            var id = entry.Id;
            if (!seen.Add((id, entry.Command))) continue;
            var part = FindPart(id);
            if (part == null) continue;
            if (entry.Delivery == ActivationDelivery.Receive &&
                part.HandleActivation(this, entry.Command) == ActivationDisposition.Deferred) continue;
            part.Active = true;
            Events.TryAdd(new(MachineEventKind.Activated, id), Ticks);
            foreach (var link in Connections)
                if (link.From == id && link.Type == ConnectionDomain.Activation && IsValidConnection(link))
                    pending.Enqueue((link.To, ActivationDelivery.Receive,
                        FindPart(link.To)!.ConnectionPorts.Single(p => p.Id == link.ToPort).Command));
        }
    }

#if PLAYTEST
    private readonly PerformanceRecorder _performance=new();
    internal PerformanceBatch DrainPerformance()=>_performance.Drain();
#endif
    public void Step()
    {
        RequireIdle();
        if (!Running) return;
        _acoustics!.RequireReady();OscillatorEvents.RequireWritable();_animations!.Occurrences.RequireReady();
        var poses=_committedPoses??throw new InvalidOperationException("Run has not installed pose publication.");
        poses.RequireWritable();
        var poseWritePending=false;
        var wasWon=Won;
        Phase=MachineWorldPhase.Stepping;
#if PLAYTEST
        _performance.BeginTick(Ticks);
        var performanceOutcome=PerformanceOutcome.Failed;
#endif
        try
        {
            poses.BeginWrite(Substeps);poseWritePending=true;
            var transaction=_simulationTransaction??throw new InvalidOperationException("Run has not installed simulation transactions.");
            transaction.Begin();
            try
            {
                var nextStamp=new Bridge.PoseReadStamp(ControlGeneration,new(checked(Ticks+1)),(double)(Ticks+1)*Tick);
                OscillatorEvents.Begin(nextStamp);_animations!.Occurrences.Begin(nextStamp);
                ApplyControlCommands();AdvanceTick(poses);
#if PLAYTEST
                _performance.Begin(PerformanceStage.Publication);
#endif
                var stamp=new PoseReadStamp(ControlGeneration,new(Ticks),(double)Ticks*Tick);
                _acoustics!.Stage(stamp,_animations!.AcousticMotion.Wavefronts);OscillatorEvents.Seal();_animations.Occurrences.Seal();
                _enumObservations!.Stage(poses);
                Physics.CopyCompliantContacts(_compliantReads);poses.StageCompliantContacts(_compliantReads);
                poses.Stage(stamp,PhysicsAssembly.CapturePublicationReads(Physics),Counters.PublicationReads,_electrical!.CapturePublicationReads(),_scalarObservations!.Capture(),Timers.PublicationReads,_booleanObservations!.Capture());
#if PLAYTEST
                _performance.End(PerformanceStage.Publication);
#endif
            }
            catch (Exception tickFailure)
            {
                _acoustics!.Discard();_animations!.Occurrences.Discard();
                if(OscillatorEvents.Phase is EventStreamPhase.Writing or EventStreamPhase.Staged or EventStreamPhase.Rejected)OscillatorEvents.Discard();
                try { transaction.Rollback(); }
                catch (Exception rollbackFailure)
                { throw new AggregateException("Simulation tick and rollback failed.",tickFailure,rollbackFailure); }
                throw;
            }
            transaction.Commit();
#if PLAYTEST
            _performance.Begin(PerformanceStage.Publication);
#endif
            poses.Publish();poseWritePending=false;
            OscillatorEvents.Commit();_animations!.Occurrences.Commit();
            using(var read=poses.Acquire())_animations!.Publish(read);
            _acoustics!.Publish(_animations!.AcousticMotion);
            _animations.Occurrences.Publish();
#if PLAYTEST
            _performance.End(PerformanceStage.Publication);
            performanceOutcome=PerformanceOutcome.Completed;
#endif
        }
        finally
        {
            if(poseWritePending)poses.Discard();
            Phase=MachineWorldPhase.Idle;
#if PLAYTEST
            _performance.EndTick(performanceOutcome);
#endif
        }
        // UI may load another level here, but never inside an unfinished tick.
        if(!wasWon&&Won) Solved?.Invoke();
    }

    private void AdvanceTimers(TimerBoundary boundary)
    {
        foreach(var elapsed in Timers.Advance(Ticks,boundary))
        {
            var declaration=_sceneTimers[elapsed.Id.Index];
            switch(declaration.Signal)
            {
                case TimerElapsedSignal.None:break;
                case TimerElapsedSignal.Activation:EmitActivation(declaration.Key.Owner);break;
                default:throw new InvalidOperationException("Unsupported timer signal.");
            }
        }
    }

    private void AdvanceOscillators()
    {
        for(var i=0;i<_sceneOscillators.Length;i++)
        {
            var declaration=_sceneOscillators[i];
            _oscillatorInputs[i]=new(new(i),declaration.Key.Owner.HasElectricalPower(declaration.PowerInput));
        }
        foreach(var pulse in Oscillators.Advance(Ticks,_oscillatorInputs))
        {
            OscillatorEvents.Append(pulse);
            EmitActivation(_sceneOscillators[pulse.Id.Index].Key.Owner);
        }
    }

    private void AdvanceTick(CommittedPoseBuffer publication)
    {
        var physics=Physics; var assembly=PhysicsAssembly;
        _tickImpacts.Clear();
#if PLAYTEST
        _performance.Begin(PerformanceStage.Networks);
#endif
        Latches.Advance(Ticks);
        AdvanceTimers(TimerBoundary.BeforeNetworks);
        foreach(var part in Parts) part.BeforeNetworks(this);
        LightNetwork.Solve(this);
        OpticalNetwork.Solve(this);
        AcousticNetwork.Solve(this);
        _electrical!.Solve();
#if PLAYTEST
        _performance.End(PerformanceStage.Networks);
#endif
        // Timer deadlines retain the existing post-network/pre-physics boundary.
        // Occurrences are owned by the timer world, consumed exactly once here.
        AdvanceTimers(TimerBoundary.BeforePhysics);
        AdvanceOscillators();
        const float delta=Tick/Substeps;
        for(var iteration=0;iteration<Substeps;iteration++)
        {
            _forces.Clear(); _motors.Clear(); _elasticLoads.Clear(); _effortLoads.Clear(); _dampingLoads.Clear(); _dragLoads.Clear(); _guideLoads.Clear(); _transferLoads.Clear(); _rotaryLoads.Clear();
            foreach(var part in Parts) part.PreparePhysics(this,delta);
            AirflowNetwork.Step(this);
            foreach(var declaration in assembly.Declarations)
            {
                var owner=declaration.Geometry.Owner;
                if(owner is null||declaration.Geometry.Slot!=MachinePart.RootBody||!owner.Dynamic) continue;
                var body=assembly.Body(new(owner,MachinePart.RootBody));
                var mass=1/body.InverseMass;
                _forces.Add(new(body.Id,new(0,owner.Buoyancy*Pressure*mass,0),default));
                _dragLoads.Add(new(body.Id,owner.Drag*Pressure,0));
            }
            var guideDistances=Ropes.Select(rope=>rope.GuideDistances(this)).ToArray();
            physics.ReplaceLoads(new()
            {
                Efforts=_effortLoads,Elastic=_elasticLoads,Damping=_dampingLoads,Drag=_dragLoads,
                Compliant=physics.Loads.Compliant,Guides=_guideLoads,Transfers=_transferLoads,Rotary=_rotaryLoads,Springs=physics.Loads.Springs
            });
#if PLAYTEST
            _performance.Begin(PerformanceStage.Physics);
#endif
            LastPhysicsStep=physics.Step(_forces.ToArray(),_motors.ToArray(),delta);
            publication.AppendMotion(physics.LastMotion??throw new InvalidOperationException("Successful physics step has no accepted motion."));
#if PLAYTEST
            _performance.RecordPhysicsStep(LastPhysicsStep);
            _performance.End(PerformanceStage.Physics);
#endif
            for(var ropeIndex=0;ropeIndex<Ropes.Count;ropeIndex++)
                Ropes[ropeIndex].AnimateGuides(this,guideDistances[ropeIndex]);
            _tickImpacts.AddRange(physics.Impacts.ToArray());
            foreach(var effect in _impactEffects) effect.Publish(this);
            foreach(var body in Bodies)
            {
                if(body.PhysicsOwner!=body) continue;
                var solved=assembly.Body(new(body,MachinePart.RootBody));
                var id=solved.Id;
                var collider=physics.Collider(id).Declaration;
                if(collider.Participation==CollisionParticipation.Disabled) continue;
                var center=solved.Pose.Center;
                if(center.Y < -5||center.Y>20||Math.Abs(center.X)>18||Math.Abs(center.Z)>12)
                {
                    body.Visible=false;
                    physics.ApplyColliderUpdates([new(id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
                    Events.TryAdd(new(MachineEventKind.Escaped,body.Uid),Ticks);
                }
            }
            foreach(var part in Parts) part.ObservePhysics(this,delta);
        }
        Ticks++;
        if (!Won && Objectives.Count > 0 && GoalsMet())
        {
            Won = true;
            Running = false;
        }
    }

    private bool GoalsMet() => Objectives.All(goal => goal.Type switch
    {
        GoalKind.Captured => Events.ContainsKey(new(MachineEventKind.Captured, goal.Target, goal.Body)),
        GoalKind.Activated => Events.ContainsKey(new(MachineEventKind.Activated, goal.Target)),
        GoalKind.Powered => Events.ContainsKey(new(MachineEventKind.Powered, goal.Target)),
        GoalKind.Turned => Events.ContainsKey(new(MachineEventKind.Turned, goal.Target)),
        GoalKind.ActivatedAfter => Events.TryGetValue(new(MachineEventKind.Activated, goal.Body), out var emittedTick)
            && Events.TryGetValue(new(MachineEventKind.Activated, goal.Target), out var receivedTick)
            && receivedTick - emittedTick >= Math.Ceiling(goal.MinimumDelaySeconds / Tick),
        GoalKind.PoweredAfter => Events.TryGetValue(new(MachineEventKind.Activated, goal.Body), out var triggerTick)
            && Events.TryGetValue(new(MachineEventKind.Powered, goal.Target), out var poweredTick)
            && poweredTick > triggerTick
            && poweredTick - triggerTick >= Math.Ceiling(goal.MinimumDelaySeconds / Tick),
        _ => throw new InvalidOperationException("Unsupported goal kind.")
    });

    public string StateSignature()
    {
        var state = new StringBuilder().Append(Ticks).Append(':').Append(Won);
        foreach (var part in Parts)
        {
            state.Append('|').Append(part.Uid).Append(':').Append(part.Active);
            if(!HasPhysicsState)
            {
                state.Append(':').Append(part.Visible);
                foreach (var value in new[] { part.Position.X, part.Position.Y, part.Position.Z, part.InitialVelocity.X, part.InitialVelocity.Y, part.InitialVelocity.Z })
                    state.Append(':').Append(value.ToString("R", CultureInfo.InvariantCulture));
            }
        }
        if(_physicsAssembly is not null)
            foreach(var body in _physicsAssembly.Bodies)
            {
                var pose=body.Pose;
                var collider=Physics.Collider(body.Id).Declaration;
                // Numeric enum encoding belongs to this diagnostic serialization boundary.
                state.Append('|').Append(body.Id.Index).Append(':').Append((int)body.MotionType)
                    .Append(':').Append((int)collider.Participation);
                foreach(var value in new[]{pose.Center.X,pose.Center.Y,pose.Center.Z,pose.Rotation.X,pose.Rotation.Y,
                    pose.Rotation.Z,pose.Rotation.W,body.LinearVelocity.X,body.LinearVelocity.Y,body.LinearVelocity.Z,
                    body.AngularMomentum.X,body.AngularMomentum.Y,body.AngularMomentum.Z,
                    body.AngularVelocity.X,body.AngularVelocity.Y,body.AngularVelocity.Z})
                    state.Append(':').Append(value.ToString("R",CultureInfo.InvariantCulture));
            }
        foreach (var pair in Events) state.Append('|').Append(pair.Key).Append(':').Append(pair.Value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state.ToString())));
    }
}

