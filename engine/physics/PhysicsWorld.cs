using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum PhysicsWorldPhase { Idle, Stepping }
public sealed record PhysicsObject
{
    public PhysicsBody Body { get; }
    public CompoundGeometry Geometry { get; }
    public ContactMaterial Material { get; }
    /// <summary>Construction declaration only; runtime participation belongs to PhysicsWorld.</summary>
    public CollisionParticipation InitialParticipation
    {
        get;
        init => field=Enum.IsDefined(value)?value:throw new ArgumentOutOfRangeException(nameof(value));
    } = CollisionParticipation.Enabled;
    public PhysicsObject(PhysicsBody body,CompoundGeometry geometry,ContactMaterial material)
    {
        ArgumentNullException.ThrowIfNull(body); ArgumentNullException.ThrowIfNull(geometry);
        Body=body; Geometry=geometry; Material=material;
    }
}
public sealed class PhysicsWorldSettings
{
    public CollisionVector Gravity { get; }
    public double MaximumStep { get; }
    public double MaximumPenetration { get; }
    public double PositionTolerance { get; }
    public double VelocityTolerance { get; }
    public double AccelerationTolerance { get; }
    /// <summary>Absolute joule error allowed per motor prediction interval.</summary>
    public double MotorWorkTolerance { get; }
    public int MaximumEvents { get; }
    public int MaximumSubsteps { get; }
    public int MaximumMotionIntervals { get; }
    public PhysicsWorldSettings(CollisionVector gravity,double maximumStep=1.0/120,double maximumPenetration=1e-6,
        double positionTolerance=1e-7,double velocityTolerance=1e-8,int maximumEvents=256,int maximumSubsteps=4096,
        double accelerationTolerance=1e-9,double motorWorkTolerance=1e-10,int maximumMotionIntervals=8192)
    {
        if(!gravity.IsFinite||!double.IsFinite(maximumStep)||maximumStep<=0||
            !double.IsFinite(positionTolerance)||positionTolerance<ConvexDistance.DefaultTolerance||
            !double.IsFinite(maximumPenetration)||maximumPenetration<=positionTolerance+ConvexDistance.DefaultTolerance||
            !double.IsFinite(velocityTolerance)||velocityTolerance<=0||!double.IsFinite(accelerationTolerance)||accelerationTolerance<=0||!double.IsFinite(motorWorkTolerance)||motorWorkTolerance<=0||maximumEvents<1||maximumSubsteps<1||maximumMotionIntervals<1)
            throw new ArgumentException("World limits must be finite, positive and precision-consistent.");
        Gravity=gravity; MaximumStep=maximumStep; MaximumPenetration=maximumPenetration;
        AccelerationTolerance=accelerationTolerance;MotorWorkTolerance=motorWorkTolerance;
        PositionTolerance=positionTolerance; VelocityTolerance=velocityTolerance;
        MaximumEvents=maximumEvents; MaximumSubsteps=maximumSubsteps;MaximumMotionIntervals=maximumMotionIntervals;
    }
}
public readonly record struct ColliderPairKey(PhysicsBodyId A,ColliderChildId ChildA,PhysicsColliderRevision RevisionA,
    PhysicsBodyId B,ColliderChildId ChildB,PhysicsColliderRevision RevisionB);
public readonly record struct PhysicsImpact(ColliderPairKey Pair,double Time,ConvexSeparationResult Separation);
public readonly record struct PhysicsOverlap(ColliderPairKey Pair,ConvexSeparationResult Separation);
/// <summary>Two prescribed bodies cannot satisfy nonpenetration without changing
/// their declared motion. The enclosing step rejects atomically.</summary>
public sealed class PrescribedMotionConflictException(PhysicsImpact conflict):
    InvalidOperationException("Prescribed rigid motion intersects another immovable or prescribed collider.")
{
    public PhysicsImpact Conflict { get; }=conflict;
}
public readonly record struct PhysicsAngularTravelState(PhysicsJointId Joint,AngularPathTravel Travel);
public readonly record struct PhysicsJointStop(PhysicsJointId Joint,JointBoundary Boundary,double Time);
public sealed record PhysicsStepResult(int Substeps,int Events,int SweepIterations,int VelocityIterations,int PositionIterations,
    int PredictionCalls,int PredictionMidpoints,int PredictionNewtonIterations,int MaximumPredictionCoordinates,PhysicsSpatialWork SpatialWork,PredictionConstraintWork PredictionConstraints,ImpulseCorrectionWork ImpulseCorrections);

/// <summary>World-owned bodies, convex-child contact graph and shared event clock.
/// Body ownership is immutable; joint and collider declarations are transactional. Every successful motion interval
/// is certified before all bodies commit it; a failed step restores body/cache state.</summary>
public sealed class PhysicsWorld : SimulationTransactionParticipant
{
    private Snapshot? _transactionCheckpoint;
    private PhysicsMotionHistory? _lastMotion;
    public PhysicsMotionHistory? LastMotion { get {RequireIdle();return _lastMotion;} }
    protected override void CaptureCheckpoint() => _transactionCheckpoint=Capture();
    protected override void RestoreCheckpoint() => Restore(_transactionCheckpoint??
        throw new InvalidOperationException("Physics transaction has no checkpoint."));

    internal sealed record Pair(int A,int B,ColliderChildId ChildA,ColliderChildId ChildB,
        ConvexInstance ShapeA,ConvexInstance ShapeB,PersistentContactPair Contact,ContactPositionConstraint Position,ColliderPairKey Key);
    private readonly PhysicsObject[] _objects;
    private readonly Dictionary<PhysicsBodyId,int> _objectIndices;
    private readonly CollisionParticipation[] _participation;
    private readonly PhysicsColliderRevision[] _colliderRevisions;
    private readonly Dictionary<ColliderPairKey,Pair> _pairs=new();
    private readonly BodyBoundsTree _bodyBounds;
    private PhysicsSpatialWork _spatialWork;
    private bool _measureSpatialWork;
    public int RetainedContactPairs=>_pairs.Count;
    private PhysicsJoint[] _joints;
    private Dictionary<PhysicsJointId,AngularPathTravel> _angularTravel=new();
    public AngularPathTravel AngularTravel(PhysicsJointId joint)
    {
        RequireIdle();
        return _angularTravel.TryGetValue(joint,out var travel)?travel:
            throw new ArgumentException("Angular travel requires a current hinge joint.",nameof(joint));
    }
    private PhysicsAngularTravelState[] AngularTravelStates()=>_angularTravel.OrderBy(entry=>entry.Key.Index)
        .Select(entry=>new PhysicsAngularTravelState(entry.Key,entry.Value)).ToArray();
    private DrivenSurface[] _surfaces=[];
    private PhysicsLoadSet _loads=new();
    private Dictionary<PhysicsJointId,PhysicsLatchedSpringState> _springs=new();
    public ReadOnlySpan<PhysicsLatchedSpringState> Springs=>_springs.Values.OrderBy(state=>state.Declaration.Guide.Index).ToArray();
    public PhysicsLatchedSpringState Spring(PhysicsJointId guide)=>
        _springs.TryGetValue(guide,out var state)?state:throw new ArgumentException("Unknown spring guide.",nameof(guide));
    private Dictionary<PhysicsResidenceKey,PhysicsResidenceSensor> _residenceSensors=new();
    private Dictionary<PhysicsResidenceKey,PhysicsResidenceState> _residenceStates=new();
    public ReadOnlySpan<PhysicsResidenceState> ResidenceStates=>_residenceStates.Values
        .OrderBy(state=>state.Key.Frame.Index).ThenBy(state=>state.Key.Body.Index).ToArray();
    private Dictionary<PhysicsBodyId,PhysicsPassageSensor> _passageSensors=new();
    private Dictionary<PhysicsPassageKey,PhysicsPassageState> _passageStates=new();
    private PhysicsPassageEvent[] _passageEvents=[];
    public ReadOnlySpan<PhysicsPassageState> PassageStates=>_passageStates.Values
        .OrderBy(state=>state.Key.Frame.Index).ThenBy(state=>state.Key.Body.Index).ToArray();
    public ReadOnlySpan<PhysicsPassageEvent> PassageEvents=>_passageEvents;
    private Dictionary<PhysicsBodyId,PhysicsContactLoadSensor> _contactLoadSensors=new();
    public ReadOnlySpan<PhysicsContactLoadSensor> ContactLoadSensors=>_contactLoadSensors.Values.OrderBy(sensor=>sensor.Frame.Index).ToArray();
    public PhysicsContactLoadReading ContactLoad(PhysicsBodyId frame)
    {
        RequireIdle();
        if(!_contactLoadSensors.TryGetValue(frame,out var sensor))throw new ArgumentException("Unknown contact-load sensor.",nameof(frame));
        return sensor.Measure(_objects[_objectIndices[frame]].Body,BodyContacts(frame));
    }
    private Dictionary<PhysicsBodyId,PhysicsTiltState> _tilts=new();
    public ReadOnlySpan<PhysicsTiltState> TiltStates=>_tilts.Values.OrderBy(state=>state.Declaration.Body.Index).ToArray();
    public PhysicsTiltState TiltState(PhysicsBodyId body)=>
        _tilts.TryGetValue(body,out var state)?state:throw new ArgumentException("Unknown tilt sensor.",nameof(body));
    private Dictionary<MechanicalSourceId,MechanicalTransferSource> _sourceBindings=new();
    private Dictionary<PhysicsGasNodeId,PhysicsGasNode> _gasNodes=new();
    public ReadOnlySpan<PhysicsGasNode> GasNodes=>_gasNodes.Values.OrderBy(node=>node.Id.Index).ToArray();
    private Dictionary<PhysicsBodyId,PhysicsEnergyStoreState> _energyStores=new();
    public ReadOnlySpan<PhysicsEnergyStoreState> EnergyStores=>_energyStores.Values.OrderBy(store=>store.Owner.Index).ToArray();
    private Dictionary<CompliantContactKey,CompliantContactState> _compliantContacts=new();
    private PhysicsCompliantEntry[] _compliantEntries=[];
    public ReadOnlySpan<CompliantContactState> CompliantContacts=>_compliantContacts.Values
        .OrderBy(contact=>contact.Key.Body.Index).ThenBy(contact=>contact.Key.Frame.Index).ToArray();
    public void CopyCompliantContacts(Span<CompliantContactState> target)
    {
        if(target.Length!=_loads.Compliant.Count)
            throw new ArgumentException("Contact target requires the complete declared topology.");
        for(var i=0;i<target.Length;i++)target[i]=_compliantContacts[_loads.Compliant[i].Key];
    }
    public ReadOnlySpan<PhysicsCompliantEntry> CompliantEntries=>_compliantEntries;
    public PhysicsLoadSet Loads=>_loads;
    public ReadOnlySpan<DrivenSurface> Surfaces=>_surfaces;
    private int[][] _jointBodies;
    private readonly PhysicsWorldSettings _settings;
    private readonly PhysicsImpactEffect[] _effects;
    private PhysicsImpact[] _impacts=[];
    private PhysicsJointStop[] _jointStops=[];
    private Dictionary<PhysicsJointId,PhysicsServoState> _servos=new();
    public ReadOnlySpan<PhysicsServoState> Servos=>_servos.Values.OrderBy(state=>state.Declaration.Joint.Index).ToArray();
    public PhysicsServoState Servo(PhysicsJointId joint)
    {
        RequireIdle();
        return _servos.TryGetValue(joint,out var state)?state:
            throw new ArgumentException("Unknown servo joint.",nameof(joint));
    }
    public void InstallServos(IEnumerable<PhysicsServoDeclaration> declarations)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(declarations);
        var proposed=new Dictionary<PhysicsJointId,PhysicsServoState>(_servos);
        foreach(var declaration in declarations)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            if(_joints.SingleOrDefault(j=>j.Id==declaration.Joint) is not PhysicsFrameJoint guide||
                guide.Kind==FrameJointKind.BallSocket||guide.TravelRange is not { } range||
                range.Lower>=range.Upper||guide.Direction!=JointTravelDirection.Both||
                !proposed.TryAdd(declaration.Joint,new(declaration,guide,PhysicsServoMode.Hold,0,guide.Motion.Coordinate)))
                throw new ArgumentException("Servo requires a unique owned, bidirectional bounded hinge or slider.");
        }
        var before=Capture();
        try
        {
            _servos=proposed;
            ApplyDeclarations([],_joints.Select(j=>_servos.TryGetValue(j.Id,out var state)?ServoGuide(state):j));
        }
        catch {RestoreState(before);throw;}
    }
    public void SetServoMode(PhysicsJointId joint,PhysicsServoMode mode)
    {
        RequireIdle();
        if(!Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(mode));
        var state=Servo(joint);
        if(state.Mode==mode)return;
        var before=Capture();
        try
        {
            state=state with {Mode=mode,CommandSpeed=mode==PhysicsServoMode.Hold?0:state.CommandSpeed,
                HoldCoordinate=mode==PhysicsServoMode.Hold?state.Guide.Motion.Coordinate:state.HoldCoordinate};
            _servos[joint]=state;
            ApplyDeclarations([],_joints.Select(j=>j.Id==joint?ServoGuide(state):j));
        }
        catch {RestoreState(before);throw;}
    }
    private static PhysicsFrameJoint ServoGuide(PhysicsServoState state)
    {
        var guide=state.Guide;
        return new(guide.Id,guide.Kind,guide.A,guide.LocalA,guide.B,guide.LocalB,
            guide.Collision,state.ActiveRange,JointTravelDirection.Both);
    }

    public TransferStepError TransferError { get; private set; }
    public TransferResidualTotals TransferResidualUse { get; private set; }
    public TransferResidualTotals TransferResidualTotal { get; private set; }
    private MechanicalSourceTotals[] _sourceUse=[];
    private Dictionary<MechanicalSourceId,MechanicalSourceTotals> _sourceTotals=new();
    public ReadOnlySpan<MechanicalSourceTotals> SourceUse=>_sourceUse;
    public ReadOnlySpan<MechanicalSourceTotals> SourceTotals=>
        _sourceTotals.Values.OrderBy(value=>value.Source.Index).ToArray();
    private MechanicalTransferBodyImpulse[] _transferBodyImpulses=[];
    public ReadOnlySpan<MechanicalTransferBodyImpulse> TransferBodyImpulses=>_transferBodyImpulses;
    private MechanicalTransferTotals[] _transferUse=[];
    private Dictionary<MechanicalTransferId,MechanicalTransferTotals> _transferTotals=new();
    public ReadOnlySpan<MechanicalTransferTotals> TransferUse=>_transferUse;
    public ReadOnlySpan<MechanicalTransferTotals> TransferTotals=>
        _transferTotals.Values.OrderBy(value=>value.Transfer.Index).ToArray();
    private PhysicsMotorUse[] _motorUse=[];
    private Dictionary<PhysicsJointId,PhysicsMotorTotals> _motorTotals=new();
    public ReadOnlySpan<PhysicsMotorTotals> MotorTotals=>_motorTotals.Values.OrderBy(total=>total.Joint.Index).ToArray();
    public PhysicsMotorTotals MotorTotal(PhysicsJointId joint)
    {
        RequireIdle();
        if(_motorTotals.TryGetValue(joint,out var total)) return total;
        if(_joints.SingleOrDefault(value=>value.Id==joint) is not PhysicsFrameJoint frame||
            frame.Kind==FrameJointKind.BallSocket)
            throw new ArgumentException("Motor accounting requires a known hinge or slider.",nameof(joint));
        return new(joint,0,0,0,0,0,0);
    }
    public PhysicsWorldPhase Phase { get; private set; }
    public double Time { get; private set; }
    public ulong StepIndex { get; private set; }
    public ReadOnlySpan<PhysicsImpact> Impacts=>_impacts;
    public ReadOnlySpan<PhysicsJointStop> JointStops=>_jointStops;
    public ReadOnlySpan<PhysicsMotorUse> MotorUse=>_motorUse;
    public ReadOnlySpan<PhysicsJoint> Joints=>_joints;

    public PhysicsWorld(IEnumerable<PhysicsImpactEffect> effects,IEnumerable<PhysicsObject> objects,IEnumerable<PhysicsJoint> joints,PhysicsWorldSettings settings)
    {
        ArgumentNullException.ThrowIfNull(objects); ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(joints); ArgumentNullException.ThrowIfNull(effects);
        var suppliedEffects=effects.ToArray();
        if(suppliedEffects.Any(e=>e is null)) throw new ArgumentException("World effects cannot be null.",nameof(effects));
        _effects=suppliedEffects.OrderBy(e=>e.Body.Index).ToArray();
        if(_effects.Select(e=>e.Body).Distinct().Count()!=_effects.Length)
            throw new ArgumentException("Each body must have one unambiguous impact effect.",nameof(effects));
        var declarations=joints.ToArray();
        if(declarations.Any(j=>j is null)) throw new ArgumentException("World joints cannot be null.",nameof(joints));
        _joints=declarations.OrderBy(j=>j.Id.Index).ToArray();
        _angularTravel=_joints.OfType<PhysicsFrameJoint>().Where(j=>j.Kind==FrameJointKind.Hinge)
            .ToDictionary(j=>j.Id,_=>default(AngularPathTravel));
        if(_joints.Select(j=>j.Id).Distinct().Count()!=_joints.Length) throw new ArgumentException("World joint identities must be unique.");
        ValidateJointDependencies(_joints);
        var supplied=objects.ToArray();
        if(supplied.Any(o=>o is null)) throw new ArgumentException("World objects cannot be null.",nameof(objects));
        _objects=supplied.OrderBy(o=>o.Body.Id.Index).ToArray(); _settings=settings;
        if(_objects.Select(o=>o.Body.Id).Distinct().Count()!=_objects.Length) throw new ArgumentException("World body identities must be unique.");
        var owned=_objects.ToDictionary(o=>o.Body.Id,o=>o.Body);
        if(_effects.Any(e=>!owned.ContainsKey(e.Body)))
            throw new ArgumentException("Impact effect refers to a body not owned by this world.",nameof(effects));
        foreach(var joint in _joints)
            foreach(var body in joint.Bodies)
                if(!owned.TryGetValue(body.Id,out var worldBody)||worldBody!=body)
                    throw new ArgumentException("Joint refers to a body state not owned by this world.");
        _objectIndices=_objects.Select((o,i)=>(o.Body.Id,i)).ToDictionary(p=>p.Id,p=>p.i);
        _participation=_objects.Select(o=>o.InitialParticipation).ToArray();
        _colliderRevisions=new PhysicsColliderRevision[_objects.Length];
        _jointBodies=_joints.Select(j=>j.Bodies.ToArray().Select(b=>_objectIndices[b.Id]).ToArray()).ToArray();
        _bodyBounds=new(_objects);
        // Check the entire set before claiming any participant.
        foreach(var item in _objects)item.Body.RequireUnowned();
        foreach(var item in _objects)item.Body.AttachToWorld();
    }


    /// <summary>First deterministic solid overlap in the current declarations,
    /// beyond the world's penetration limit. Static scenery and shared prescribed
    /// frames are excluded; this does not mutate contacts or advance the clock.</summary>
    public PhysicsOverlap? FindPlacementOverlap()
    {
        RequireIdle();
        foreach(var (a,b) in BodyCandidates(i=>_participation[i]==CollisionParticipation.Enabled?
            CompoundBoundsTree.Swept(_objects[i].Geometry.Tree.Root,
                _objects[i].Body.CreateTrajectory(0,default),0):null,BodyPairDomain.Moving,0).Pairs)
        {
            var first=_objects[a];var second=_objects[b];
            if(SharesPrescribedFrame(first.Body,second.Body)||CollisionSuppressed(a,b,_joints))continue;
            var overlap=FindOverlap(
                new(first.Geometry,first.Body.CreateTrajectory(0,default)),
                new(second.Geometry,second.Body.CreateTrajectory(0,default)),_settings.MaximumPenetration);
            if(overlap is not { } found)continue;
            return new(new(first.Body.Id,found.ChildA,_colliderRevisions[a],
                second.Body.Id,found.ChildB,_colliderRevisions[b]),found.Separation);
        }
        return null;
    }

    private bool CollisionSuppressed(int a,int b,IEnumerable<PhysicsJoint> joints)=>
        joints.Any(j=>j.Collision==ConnectedBodyCollision.Disabled&&j.Connects(_objects[a].Body,_objects[b].Body));

    /// <summary>Replace the complete constraint declaration while idle without
    /// replacing bodies or resetting the event clock. New constraints must already
    /// fit their current poses; runtime latching cannot teleport a body into place.</summary>
    public void ReplaceJoints(IEnumerable<PhysicsJoint> joints)
    {
        RequireIdle();
        ApplyDeclarations([],joints);
    }

    /// <summary>Atomically replace all state-dependent force declarations while idle.
    /// A rejected family leaves every previously installed family unchanged.</summary>
    public void ReplaceLoads(PhysicsLoadSet loads)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(loads);
        var bodies=_objects.ToDictionary(o=>o.Body.Id,o=>o.Body);
        var colliders=_objects.ToDictionary(o=>o.Body.Id,o=>Collider(o.Body.Id).Declaration);
        foreach(var load in loads.Efforts)load.Validate(load.Resolve(_joints),bodies,colliders);
        foreach(var load in loads.Elastic)load.Resolve(_joints);
        if(loads.Gas.Select(load=>load.Node).Distinct().Count()!=loads.Gas.Count)
            throw new ArgumentException("A gas inventory cannot drive multiple independent chamber coordinates.",nameof(loads));
        if(StepIndex!=0&&!loads.Gas.SequenceEqual(_loads.Gas))
            throw new InvalidOperationException("Gas chamber bindings cannot change after simulation begins.");
        foreach(var load in loads.Gas)_=load.Bind(_joints,_gasNodes);
        foreach(var load in loads.Damping)load.Resolve(_joints);
        var prepared=loads.PrepareRotary(bodies,_joints,colliders,_energyStores);
        MechanicalTransferSource.ValidateAll(prepared.Transfers,bodies,_joints,_energyStores,colliders);
        var bindings=new Dictionary<MechanicalSourceId,MechanicalTransferSource>(_sourceBindings);
        foreach(var load in prepared.Transfers)
        {
            if(bindings.TryGetValue(load.Source.Id,out var binding)&&!binding.SameBinding(load.Source))
                throw new ArgumentException("An installed source identity cannot change its physical supply binding.");
            bindings[load.Source.Id]=load.Source;
        }
        foreach(var load in prepared.Transfers)
            if(_transferTotals.TryGetValue(load.Id,out var prior)&&prior.Source!=load.Source.Id)
                throw new ArgumentException("An accounted transfer cannot change source identity.");
        foreach(var load in loads.Drag)load.Resolve(bodies);
        foreach(var load in loads.Compliant)load.Validate(bodies);
        foreach(var load in loads.Guides)load.Validate(bodies,colliders);
        var contacts=new Dictionary<CompliantContactKey,CompliantContactState>();
        var previous=_loads.Compliant.ToDictionary(load=>load.Key);
        foreach(var load in loads.Compliant)
        {
            CompliantContactState state;
            if(_compliantContacts.TryGetValue(load.Key,out var retained))
            {
                if(previous[load.Key].InitialState!=load.InitialState)
                    throw new ArgumentException("An installed contact's initial condition cannot be rewritten.");
                state=retained;
            }
            else state=CompliantContactState.Create(load,bodies,colliders);
            if(!contacts.TryAdd(load.Key,state))throw new ArgumentException("Duplicate compliant contact identity.");
        }
        var springs=new Dictionary<PhysicsJointId,PhysicsLatchedSpringState>();
        var transmissions=new HashSet<PhysicsJointId>();
        foreach(var load in loads.Springs)
        {
            PhysicsLatchedSpringState state;
            if(_springs.TryGetValue(load.Guide,out var retained))
            {
                if(!retained.Declaration.SameLaw(load))throw new ArgumentException("Installed spring law cannot be rewritten.");
                state=retained;
            }
            else
            {
                var (guide,_)=load.Resolve(_joints);
                var compression=load.RestCoordinate-guide.Travel.Error;
                state=new(load,SpringLatchState.Latched,compression,compression,0,0,0,_settings.PositionTolerance,null);
                state=state.Measure(guide.Travel.Error);
            }
            load.ValidateBinding(_joints,state.State);
            if(!springs.TryAdd(load.Guide,state)||!transmissions.Add(load.Transmission)||
                loads.Elastic.Any(elastic=>elastic.Joint==load.Guide))
                throw new ArgumentException("Spring ownership and elastic declarations must be unique.");
        }
        _loads=loads;_compliantContacts=contacts;_springs=springs;_sourceBindings=bindings;
    }

    private void RefreshCompliantContacts(double time,List<PhysicsCompliantEntry> entries)
    {
        if(_loads.Compliant.Count==0)return;
        var bodies=_objects.ToDictionary(item=>item.Body.Id,item=>item.Body);
        var colliders=_objects.ToDictionary(item=>item.Body.Id,item=>Collider(item.Body.Id).Declaration);
        foreach(var load in _loads.Compliant)
        {
            var updated=_compliantContacts[load.Key].Refresh(load,bodies,colliders,time);
            _compliantContacts[load.Key]=updated.State;
            if(updated.Entry is { } entry)entries.Add(entry);
        }
    }

    /// <summary>Replace material transmission declarations without changing
    /// geometric poses. Contact caches belong to the old velocity map and expire.</summary>
    public void ReplaceSurfaces(IEnumerable<DrivenSurface> surfaces)
    {
        RequireIdle(); ArgumentNullException.ThrowIfNull(surfaces);
        var supplied=surfaces.ToArray();
        if(supplied.Any(s=>s is null)) throw new ArgumentException("Driven surfaces cannot be null.");
        foreach(var surface in supplied)
        {
            if(!_joints.Contains(surface.Drive))
                throw new ArgumentException("Driven surface must reference the current owned hinge declaration.");
            foreach(var body in surface.Drive.Bodies)
                if(!_objectIndices.TryGetValue(body.Id,out var index)||_objects[index].Body!=body)
                    throw new ArgumentException("Driven surface refers to a foreign body state.");
        }
        for(var i=0;i<supplied.Length;i++)
        for(var j=i+1;j<supplied.Length;j++)
            if(supplied[i].Carrier==supplied[j].Carrier&&
                CollisionVector.Dot(supplied[i].LocalNormal,supplied[j].LocalNormal)>=1-1e-8)
                throw new ArgumentException("A material face cannot have competing transmission declarations.");
        if(_surfaces.SequenceEqual(supplied)) return;
        _surfaces=supplied; _pairs.Clear();
        foreach(var state in Springs)_springs[state.Declaration.Guide]=state with {ConstraintObservation=default};
    }

    /// <summary>Read actual current contact geometry without modifying caches.
    /// Reports do not infer contact from scene distances or part radii.</summary>
    public PhysicsSurfaceContact[] SurfaceContacts()
    {
        RequireIdle();
        var contacts=new List<PhysicsSurfaceContact>();
        foreach(var pair in Order(_pairs.Values))
        {
            if(_participation[pair.A]!=CollisionParticipation.Enabled||_participation[pair.B]!=CollisionParticipation.Enabled||
                CollisionSuppressed(pair.A,pair.B,_joints)) continue;
            var a=_objects[pair.A].Body; var b=_objects[pair.B].Body;
            foreach(var gap in ContactGap.Query(a,pair.ShapeA,b,pair.ShapeB,ConvexSweep.ContactDistance,ConvexDistance.DefaultTolerance))
            foreach(var surface in _surfaces)
                if(surface.Matches(a,b,gap.Normal))
                    contacts.Add(new(surface.Drive.Id,surface.Carrier==a?b.Id:a.Id,gap.Point,surface.Direction,surface.Speed));
        }
        return contacts.ToArray();
    }

    /// <summary>Read current geometric contacts for one owned body without filling solver caches.
    /// A is the requested body; normals point from B toward A. Uses the solver's contact distance.
    /// Disabled and joint-suppressed pairs are excluded; this is not a support-force report.</summary>
    public ContactGap[] BodyContacts(PhysicsBodyId id)
    {
        RequireIdle();
        if(!_objectIndices.TryGetValue(id,out var index)) throw new ArgumentException("Unknown body identity.",nameof(id));
        if(_participation[index]==CollisionParticipation.Disabled) return [];
        var first=_objects[index];
        var motion=new CompoundMotion(first.Geometry,first.Body.CreateTrajectory(0,default));
        var result=new List<ContactGap>();
        for(var other=0;other<_objects.Length;other++)
        {
            if(other==index||_participation[other]==CollisionParticipation.Disabled||CollisionSuppressed(index,other,_joints)) continue;
            var second=_objects[other];
            if(first.Body.MotionType!=PhysicsMotionType.Dynamic&&second.Body.MotionType!=PhysicsMotionType.Dynamic) continue;
            var target=new CompoundMotion(second.Geometry,second.Body.CreateTrajectory(0,default));
            foreach(var pair in ChildCandidates(motion,target,0,ConvexSweep.ContactDistance).Pairs)
                result.AddRange(ContactGap.Query(first.Body,first.Geometry[pair.A],second.Body,second.Geometry[pair.B],
                    ConvexSweep.ContactDistance,ConvexDistance.DefaultTolerance));
        }
        return result.ToArray();
    }

    private static void ValidateJointDependencies(PhysicsJoint[] joints)
    {
        foreach(var joint in joints)
        foreach(var dependency in joint.Dependencies)
            if(!joints.Contains(dependency))
                throw new ArgumentException("Joint dependency must reference a current owned declaration.",nameof(joints));
    }

    private PhysicsJoint[] ValidateJoints(IEnumerable<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        var proposed=joints.ToArray();
        if(proposed.Any(j=>j is null)) throw new ArgumentException("World joints cannot be null.",nameof(joints));
        proposed=proposed.OrderBy(j=>j.Id.Index).ToArray();
        if(proposed.Select(j=>j.Id).Distinct().Count()!=proposed.Length)
            throw new ArgumentException("World joint identities must be unique.",nameof(joints));
        ValidateJointDependencies(proposed);
        foreach(var joint in proposed)
        {
            foreach(var body in joint.Bodies)
                if(!_objectIndices.TryGetValue(body.Id,out var index)||_objects[index].Body!=body)
                    throw new ArgumentException("Joint refers to a body state not owned by this world.",nameof(joints));
            if(Array.IndexOf(_joints,joint)>=0) continue;
            var error=joint.Error(_settings.PositionTolerance);
            if(!double.IsFinite(error)||error>_settings.PositionTolerance||error<0)
                throw new InvalidOperationException("New runtime constraint does not fit the current body poses.");
        }
        if(_surfaces.Any(surface=>!proposed.Contains(surface.Drive)))
            throw new ArgumentException("Remove or replace driven surfaces before changing their hinge declaration.");
        foreach(var state in _servos.Values)state.ValidateBinding(proposed);
        foreach(var state in _springs.Values)state.Declaration.ValidateBinding(proposed,state.State);
        foreach(var load in _loads.Elastic)load.Resolve(proposed);
        foreach(var load in _loads.Gas)
            if(load.Resolve(proposed)!=load.Resolve(_joints))
                throw new InvalidOperationException("A bound gas chamber slider cannot be replaced.");
        foreach(var load in _loads.Efforts)load.Validate(load.Resolve(proposed),
            _objects.ToDictionary(o=>o.Body.Id,o=>o.Body),
            _objects.ToDictionary(o=>o.Body.Id,o=>Collider(o.Body.Id).Declaration));
        foreach(var load in _loads.Damping)load.Resolve(proposed);
        var bodies=_objects.ToDictionary(o=>o.Body.Id,o=>o.Body);
        var colliders=_objects.ToDictionary(o=>o.Body.Id,o=>Collider(o.Body.Id).Declaration);
        var prepared=_loads.PrepareRotary(bodies,proposed,colliders,_energyStores);
        MechanicalTransferSource.ValidateAll(prepared.Transfers,bodies,proposed,_energyStores,colliders);
        return proposed;
    }

    private PhysicsJoint[] ChangedJoints(ReadOnlySpan<PhysicsJointChange> changes)
    {
        var proposed=_joints.ToDictionary(j=>j.Id);
        var seen=new HashSet<PhysicsJointId>();
        foreach(var change in changes)
        {
            if(!seen.Add(change.Id)) throw new ArgumentException("Joint changes must address distinct identities.",nameof(changes));
            switch(change.Kind)
            {
                case PhysicsJointChangeKind.Attach:
                    if(change.Declaration is null||change.Declaration.Id!=change.Id||!proposed.TryAdd(change.Id,change.Declaration))
                        throw new ArgumentException("Attach requires a valid declaration with a new identity.",nameof(changes));
                    break;
                case PhysicsJointChangeKind.Replace:
                    if(change.Declaration is null||change.Declaration.Id!=change.Id||!proposed.ContainsKey(change.Id))
                        throw new ArgumentException("Replace requires a valid declaration with an existing identity.",nameof(changes));
                    proposed[change.Id]=change.Declaration;
                    break;
                case PhysicsJointChangeKind.Detach:
                    if(change.Declaration is not null||!proposed.Remove(change.Id))
                        throw new ArgumentException("Detach requires an existing identity.",nameof(changes));
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(changes));
            }
        }
        return proposed.Values.OrderBy(j=>j.Id.Index).ToArray();
    }

    public PhysicsColliderState Collider(PhysicsBodyId body)
    {
        if(!_objectIndices.TryGetValue(body,out var index)) throw new ArgumentException("Body is not owned by this world.",nameof(body));
        var item=_objects[index];
        return new(new(body,item.Geometry,item.Material,_participation[index]),_colliderRevisions[index]);
    }

    /// <summary>Apply a complete validated batch while idle. Unchanged declarations
    /// are no-ops; changed bodies invalidate only their own contact pairs. Callers
    /// needing to undo the edit and its following step capture before this call.</summary>
    public void ApplyColliderUpdates(ReadOnlySpan<PhysicsColliderUpdate> updates)
    {
        RequireIdle();
        ApplyDeclarations(updates,_joints);
    }

    private void ApplyDeclarations(ReadOnlySpan<PhysicsColliderUpdate> updates,IEnumerable<PhysicsJoint> joints)
    {
        var proposedJoints=ValidateJoints(joints);
        var bindings=proposedJoints.Select(j=>j.Bodies.ToArray().Select(b=>_objectIndices[b.Id]).ToArray()).ToArray();
        var changes=new List<(int Index,PhysicsObject Object,CollisionParticipation Participation,PhysicsColliderRevision Revision)>();
        var seen=new HashSet<PhysicsBodyId>();
        foreach(var update in updates)
        {
            if(!seen.Add(update.Body)) throw new ArgumentException("Collider update body identities must be unique.",nameof(updates));
            if(!_objectIndices.TryGetValue(update.Body,out var index)) throw new ArgumentException("Collider update addresses a foreign body.",nameof(updates));
            if(update.Geometry is null||!Enum.IsDefined(update.Participation)) throw new ArgumentException("Collider update is uninitialised or invalid.",nameof(updates));
            var old=_objects[index];
            if(ReferenceEquals(old.Geometry,update.Geometry)&&old.Material==update.Material&&_participation[index]==update.Participation) continue;
            changes.Add((index,new(old.Body,update.Geometry,update.Material),update.Participation,
                new(checked(_colliderRevisions[index].Value+1))));
        }
        var changed=new HashSet<PhysicsBodyId>(changes.Select(c=>c.Object.Body.Id));
        var proposed=(PhysicsObject[])_objects.Clone();
        var participation=(CollisionParticipation[])_participation.Clone();
        foreach(var change in changes)
        {
            proposed[change.Index]=change.Object;
            participation[change.Index]=change.Participation;
        }
        ValidateDeclarationOverlap(proposed,participation,changed,proposedJoints);
        foreach(var change in changes)
        {
            _objects[change.Index]=change.Object;
            _participation[change.Index]=change.Participation;
            _colliderRevisions[change.Index]=change.Revision;
        }
        if(changes.Count>0||!_joints.SequenceEqual(proposedJoints))
            foreach(var state in Springs)_springs[state.Declaration.Guide]=state with {ConstraintObservation=default};
        _joints=proposedJoints; _jointBodies=bindings;
        _angularTravel=_joints.OfType<PhysicsFrameJoint>().Where(j=>j.Kind==FrameJointKind.Hinge)
            .ToDictionary(j=>j.Id,j=>_angularTravel.GetValueOrDefault(j.Id));
        foreach(var pair in _pairs.Values.Where(p=>changed.Contains(p.Key.A)||changed.Contains(p.Key.B)||
            CollisionSuppressed(p.A,p.B,proposedJoints)).ToArray()) _pairs.Remove(pair.Key);
    }

    private void ValidateDeclarationOverlap(PhysicsObject[] proposed,CollisionParticipation[] participation,
        HashSet<PhysicsBodyId> changed,PhysicsJoint[] joints)
    {
        foreach(var (a,b) in BodyCandidates(i=>participation[i]==CollisionParticipation.Enabled?
            CompoundBoundsTree.Swept(proposed[i].Geometry.Tree.Root,
                proposed[i].Body.CreateTrajectory(0,default),0):null,BodyPairDomain.Moving,0).Pairs)
        {
            var first=proposed[a]; var second=proposed[b];
            if(SharesPrescribedFrame(first.Body,second.Body))continue;
            if(!changed.Contains(first.Body.Id)&&!changed.Contains(second.Body.Id)&&!CollisionSuppressed(a,b,_joints)) continue;
            if(participation[a]==CollisionParticipation.Disabled||participation[b]==CollisionParticipation.Disabled||
                CollisionSuppressed(a,b,joints)) continue;
            var motionA=new CompoundMotion(first.Geometry,first.Body.CreateTrajectory(0,default));
            var motionB=new CompoundMotion(second.Geometry,second.Body.CreateTrajectory(0,default));
            if(FindOverlap(motionA,motionB,_settings.MaximumPenetration) is not null)
                throw new InvalidOperationException("Declaration change would introduce an overlapping solid.");
        }
    }

    private Pair PairFor(int a,int b,ColliderChildId ia,ColliderChildId ib)
    {
        var first=_objects[a]; var second=_objects[b];
        var key=new ColliderPairKey(first.Body.Id,ia,_colliderRevisions[a],second.Body.Id,ib,_colliderRevisions[b]);
        if(_pairs.TryGetValue(key,out var pair)) return pair;
        var material=ContactMaterial.Combine(first.Material,second.Material);
        var shapeA=first.Geometry[ia]; var shapeB=second.Geometry[ib];
        pair=new(a,b,ia,ib,shapeA,shapeB,new(first.Body,shapeA,second.Body,shapeB,material,.01,.2,
            _surfaces.Where(s=>s.Carrier==first.Body||s.Carrier==second.Body)),
            new(first.Body,shapeA,second.Body,shapeB),key);
        _pairs.Add(key,pair);
        return pair;
    }
    private CompoundOverlapResult? FindOverlap(CompoundMotion a,CompoundMotion b,double maximumPenetration)
    {
        var overlap=CompoundCollision.FindOverlap(a,b,maximumPenetration,out var candidates);
        if(_measureSpatialWork)_spatialWork=_spatialWork.Add(candidates);
        return overlap;
    }
    private BodyCandidateResult BodyCandidates(Func<int,CollisionBounds?> bounds,BodyPairDomain domain,double margin)
    {
        var result=_bodyBounds.Query(bounds,domain,margin);
        if(_measureSpatialWork)_spatialWork=_spatialWork.Add(result);
        return result;
    }
    private CompoundCandidateResult ChildCandidates(CompoundMotion a,CompoundMotion b,double duration,double margin)
    {
        var result=CompoundCollision.Candidates(a,b,duration,margin);
        if(_measureSpatialWork)_spatialWork=_spatialWork.Add(result);
        return result;
    }
    private IEnumerable<Pair> CandidatePairs(Func<PhysicsBody,IRigidTrajectory> path,double duration,double margin)
    {
        // Capture each body path once, then reuse immutable local hierarchies.
        var motions=_objects.Select((o,i)=>_participation[i]==CollisionParticipation.Enabled?
            new CompoundMotion(o.Geometry,path(o.Body)):default).ToArray();
        foreach(var (a,b) in BodyCandidates(i=>_participation[i]==CollisionParticipation.Enabled?
            CompoundBoundsTree.Swept(_objects[i].Geometry.Tree.Root,motions[i].Trajectory,duration):null,
            BodyPairDomain.Dynamic,margin).Pairs)
        {
            if(_participation[a]==CollisionParticipation.Disabled||_participation[b]==CollisionParticipation.Disabled||
                CollisionSuppressed(a,b,_joints)) continue;
            foreach(var children in ChildCandidates(motions[a],motions[b],duration,margin).Pairs)
                yield return PairFor(a,b,children.A,children.B);
        }
    }
    private IEnumerable<ContactPositionConstraint> PositionContacts(Func<PhysicsBody,ConfigurationTrajectory> path,double duration)=>
        CandidatePairs(body=>path(body),duration,ConvexDistance.DefaultTolerance).Select(p=>p.Position);
    private IEnumerable<IPositionConstraint> Positions()=>PositionContacts(body=>new(body.Pose,default,default),0)
        .Cast<IPositionConstraint>().Concat(_joints);
    private Pair[] CurrentPairs()
    {
        var near=CandidatePairs(body=>body.CreateTrajectory(0,default),0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance).ToArray();
        // Previously supported pairs must observe release even outside the skin.
        return Order(near.Concat(_pairs.Values.Where(p=>p.Contact.Contacts.Length>0)).Distinct()).ToArray();
    }
    private static IOrderedEnumerable<Pair> Order(IEnumerable<Pair> pairs)=>pairs.OrderBy(p=>p.A).ThenBy(p=>p.B)
        .ThenBy(p=>p.ChildA.Index).ThenBy(p=>p.ChildB.Index);

    private static bool SharesPrescribedFrame(PhysicsBody a,PhysicsBody b)=>
        a.PrescribedMotion is { } first&&b.PrescribedMotion is { } second&&
        ReferenceEquals(first.Path,second.Path)&&first.Time==second.Time;

    private int ValidatePrescribedTravel(BodyTrajectory[] paths,double duration,double startTime)
    {
        return 0;
    }

    private IReadOnlyDictionary<PhysicsBodyId,BodyWrench> PrepareWrenches(ReadOnlySpan<PhysicsWrenchCommand> commands)
    {
        var loads=_objects.ToDictionary(o=>o.Body.Id,o=>new BodyWrench(
            o.Body.MotionType==PhysicsMotionType.Dynamic?_settings.Gravity/o.Body.InverseMass:default,default));
        foreach(var command in commands)
        {
            if(!_objectIndices.TryGetValue(command.Body,out var index)||
                _objects[index].Body.MotionType!=PhysicsMotionType.Dynamic)
                throw new ArgumentException("Force commands require a world-owned dynamic body.",nameof(commands));
            var prior=loads[command.Body];
            loads[command.Body]=new(prior.Force+command.Force,prior.Torque+command.Torque);
        }
        return loads;
    }

    public PhysicsStepResult Step(ReadOnlySpan<PhysicsWrenchCommand> wrenches,ReadOnlySpan<PhysicsMotorCommand> motors,double duration)
    {
        RequireIdle();
        if(!double.IsFinite(duration)||duration<=0||!double.IsFinite(Time+duration)||Time+duration<=Time)
            throw new ArgumentOutOfRangeException(nameof(duration));
        var countValue=Math.Ceiling(duration/_settings.MaximumStep);
        if(countValue<1||countValue>_settings.MaximumSubsteps) throw new ArgumentOutOfRangeException(nameof(duration),"World substep budget exceeded.");
        var count=(int)countValue; var step=duration/count;
        var loads=PrepareWrenches(wrenches);
        var external=motors.ToArray();
        if(external.Any(command=>_servos.ContainsKey(command.Joint)))
            throw new ArgumentException("Servo-owned joints cannot receive direct motor commands.",nameof(motors));
        var plannedServos=Servos.ToArray().Select(state=>state.Plan(duration)).ToArray();
        var commands=external.Concat(plannedServos.Where(state=>state.Mode!=PhysicsServoMode.Hold)
            .Select(state=>state.Motor(duration))).OrderBy(m=>m.Joint.Index).ToArray();
        if(commands.Select(c=>c.Joint).Distinct().Count()!=commands.Length) throw new ArgumentException("Only one motor command per joint is allowed.");
        var budgets=new PhysicsMotorBudget[commands.Length];
        for(var i=0;i<commands.Length;i++)
        {
            var joint=_joints.SingleOrDefault(j=>j.Id==commands[i].Joint);
            if(joint is not PhysicsFrameJoint frame||frame.Kind==FrameJointKind.BallSocket)
                throw new ArgumentException("Motor command must address a world-owned hinge or slider.");
            budgets[i]=new(frame,commands[i],duration);
        }
        var nextIndex=checked(StepIndex+1);
        var gasPotentials=_loads.Gas.ToDictionary(load=>load.Node,load=>load.Bind(_joints,_gasNodes));
        var before=Capture();
        Phase=PhysicsWorldPhase.Stepping;
        Dictionary<TransferBodyKey,MechanicalTransferBodyImpulse>? transferBodyImpulses=null;
        Dictionary<MechanicalTransferId,MechanicalTransferTotals>? transferUse=null;
        Dictionary<MechanicalSourceId,MechanicalSourceTotals>? sourceUse=null;
        var transferResidual=default(TransferResidualTotals);
        var transferError=new TransferStepError(_loads.TransferWorkTolerance);
        var impacts=new List<PhysicsImpact>();
        var stops=new List<PhysicsJointStop>();
        var compliantEntries=new List<PhysicsCompliantEntry>();
        var passages=new List<PhysicsPassageEvent>();
        var motion=new List<PhysicsMotionInterval>();
        var motionIntervals=0;
        var events=0; var sweeps=0; var velocityIterations=0; var positionIterations=0;
        var predictionConstraints=default(PredictionConstraintWork);
        var impulseCorrections=default(ImpulseCorrectionWork);
        var predictionCalls=0;var predictionMidpoints=0;var predictionNewtonIterations=0;var maximumPredictionCoordinates=0;
        try
        {
            _spatialWork=default;_measureSpatialWork=true;
            foreach(var state in plannedServos)_servos[state.Declaration.Joint]=state;
            foreach(var state in Springs)_springs[state.Declaration.Guide]=state with {BeforeCompression=state.Compression,ConstraintObservation=default};
            for(var substep=0;substep<count;substep++)
            {
                double elapsed=0;
                while(true)
                {
                    var projector=new PositionProjector(_objects.Select(o=>o.Body),PositionContacts,_settings.MaximumPenetration);
                    positionIterations+=PositionSolver.Solve(Positions,projector,_settings.PositionTolerance).Iterations;
                    var currentPairs=CurrentPairs();
                    foreach(var pair in currentPairs) pair.Contact.Prepare(step);
                    var effectInputs=CaptureImpactEffects(currentPairs,Time+substep*step+elapsed);
                    var constraints=currentPairs.SelectMany(p=>p.Contact.PreparedContacts.ToArray()).Select(p=>(IImpulseConstraint)p.Constraint)
                        .Concat(PhysicsJoint.CollectVelocityConstraints(_joints,_settings.PositionTolerance)).ToArray();
                    foreach(var pair in currentPairs) pair.Contact.WarmStart();
                    var velocityWork=ImpulseSolver.Solve(constraints,tolerance:_settings.VelocityTolerance);
                    velocityIterations+=velocityWork.Iterations;
                    impulseCorrections=impulseCorrections.Add(velocityWork.Corrections);
                    foreach(var pair in currentPairs) pair.Contact.Complete(_settings.VelocityTolerance);
                    if(effectInputs.Length>0)
                    {
                        ApplyImpactEffects(effectInputs,budgets);
                        // Effects may drive a body into another constraint. Re-solve
                        // before certifying any remaining motion, even at the endpoint.
                        continue;
                    }
                    if(RefreshSprings())continue;
                    var priorEntries=compliantEntries.Count;
                    RefreshCompliantContacts(Time+substep*step+elapsed,compliantEntries);
                    events+=compliantEntries.Count-priorEntries;
                    if(events>_settings.MaximumEvents)throw new InvalidOperationException("World compliant-entry event budget exceeded.");
                    RefreshPassages();
                    if(elapsed>=step) break;
                    var activeLoads=_loads with {Compliant=_loads.Compliant
                        .Where(load=>_compliantContacts[load.Key].Phase==CompliantContactPhase.Engaged).ToArray()};
                    var remaining=step-elapsed;
                    var activeMotorBudgets=budgets.Where(budget=>budget.Joint is not null).ToArray();
                    var motorSupplies=activeMotorBudgets.Select(budget=>budget.PredictionSupply(_settings.MotorWorkTolerance)).ToArray();
                    var support=AccelerationSolver.Predict(activeLoads,_objects.ToDictionary(o=>o.Body.Id,o=>Collider(o.Body.Id).Declaration),_objects.Select(o=>o.Body),_joints,currentPairs.Select(p=>p.Contact).ToArray(),
                        loads,remaining,duration,
                        _settings.PositionTolerance,_settings.VelocityTolerance,_settings.AccelerationTolerance,motorSupplies,_energyStores, gasPotentials);
                    predictionConstraints=predictionConstraints.Add(support.ConstraintWork);
                    predictionCalls=checked(predictionCalls+1);
                    predictionMidpoints=checked(predictionMidpoints+support.MidpointEvaluations);
                    predictionNewtonIterations=checked(predictionNewtonIterations+support.NewtonIterations);
                    maximumPredictionCoordinates=Math.Max(maximumPredictionCoordinates,support.Coordinates);
                    var paths=_objects.Select(o=>support.Trajectories[o.Body.Id]).ToArray();
                    var travel=support.Duration;
                    if(travel<=0||elapsed+travel<=elapsed)
                        throw new InvalidOperationException("World collision event made no temporal progress.");
                    transferError=transferError.Add(support.Transfers,support.TransferResidualWork);
                    if(support.TransferResidualWork is { } residual)
                        transferResidual=transferResidual.Add(new(1,residual));
                    foreach(var report in support.Transfers)
                    {
                        if(travel!=support.Duration)throw new InvalidOperationException("Transfer report requires its complete accepted interval.");
                        transferUse??=new();
                        var prior=transferUse.TryGetValue(report.Transfer,out var value)?value:
                            new(report.Transfer,report.Source,0,default,default,default,0);
                        transferUse[report.Transfer]=prior.Add(report);
                    }
                    foreach(var report in support.TransferBodyImpulses)
                    {
                        if(travel!=support.Duration)throw new InvalidOperationException("Transfer body impulse requires its complete accepted interval.");
                        transferBodyImpulses??=new();
                        transferBodyImpulses[report.Key]=transferBodyImpulses.TryGetValue(report.Key,out var prior)?prior.Add(report):report;
                    }
                    if(support.Transfers.Count>0)
                    {
                        sourceUse??=new();
                        foreach(var group in support.Transfers.GroupBy(report=>report.Source))
                        {
                            var interval=MechanicalSourceTotals.Interval(group.Key,group);
                            var prior=sourceUse.TryGetValue(group.Key,out var value)?value:new(group.Key,0,default,0);
                            sourceUse[group.Key]=prior.Add(interval);
                        }
                    }
                    if(support.Transfers.Count>0)
                    {
                        var storedSources=activeLoads.TransferSources
                            .Where(source=>source.Kind==TransferSupplyKind.StoredFlow).DistinctBy(source=>source.Id)
                            .ToDictionary(source=>source.Id,source=>source.StoredFlow.StoreOwner);
                        foreach(var group in support.Transfers.Where(report=>storedSources.ContainsKey(report.Source))
                            .GroupBy(report=>storedSources[report.Source]).OrderBy(group=>group.Key.Index))
                        {
                            var work=group.Sum(report=>report.SourceExtraction.Supplied+report.SourceExtraction.SuppliedErrorBound);
                            _energyStores[group.Key]=_energyStores[group.Key].Debited(work);
                        }
                    }
                    if(motionIntervals>=_settings.MaximumMotionIntervals)
                        throw new InvalidOperationException("Accepted motion interval capacity exceeded.");
                    motion.EnsureCapacity(checked(motion.Count+_objects.Length));
                    var motionStart=Time+substep*step+elapsed;
                    var motionEnd=travel>=remaining
                        ?(substep==count-1?before.Time+duration:Time+(substep+1)*step)
                        :Time+substep*step+(elapsed+travel);
                    for(var i=0;i<_objects.Length;i++)
                        motion.Add(new(_objects[i].Body.Id,motionStart,motionEnd,travel,paths[i].PosePath));
                    motionIntervals++;
                    sweeps+=ValidatePrescribedTravel(paths,travel,Time+substep*step+elapsed);
                    var priorPassages=passages.Count;
                    sweeps+=AdvancePassages(paths,travel,Time+substep*step+elapsed,passages);
                    events+=passages.Count-priorPassages;
                    if(events>_settings.MaximumEvents)throw new InvalidOperationException("World passage event budget exceeded.");
                    var tilt=AdvanceTilts(paths,travel,Time+substep*step+elapsed);
                    sweeps+=tilt.Iterations;events+=tilt.Events;
                    if(events>_settings.MaximumEvents)throw new InvalidOperationException("World tilt event budget exceeded.");
                    var residenceBefore=_residenceSensors.Values.ToDictionary(sensor=>sensor.Key,ResidenceEligible);
                    AdvanceAngularTravel(paths,travel);
                    for(var i=0;i<activeMotorBudgets.Length;i++)activeMotorBudgets[i].Commit(support.Motors[i]);
                    foreach(var observation in support.SpringConstraints)
                        _springs[observation.Guide]=_springs[observation.Guide] with {ConstraintObservation=observation.Observation};
                    for(var i=0;i<_objects.Length;i++) _objects[i].Body.CommitTrajectory(paths[i],travel);
                    AdvanceResidence(travel,residenceBefore);
                    elapsed=travel>=remaining?step:elapsed+travel;
                    if(support.Boundary!=ForcePredictionBoundary.IntervalEnd&&++events>_settings.MaximumEvents)
                        throw new InvalidOperationException("World support event budget exceeded.");
                }
            }
            foreach(var chamber in _loads.Gas.OrderBy(load=>load.Node.Index))
            {
                var prior=_gasNodes[chamber.Node];
                var state=gasPotentials[chamber.Node].State(chamber.Resolve(_joints).Travel.Error);
                _gasNodes[chamber.Node]=new(prior.Id,prior.Owner,state);
            }
            _compliantEntries=compliantEntries.ToArray();
            _passageEvents=passages.OrderBy(p=>p.Time).ThenBy(p=>p.Key.Frame.Index).ThenBy(p=>p.Key.Body.Index).ToArray();
            Time=before.Time+duration; StepIndex=nextIndex; _impacts=impacts.ToArray(); _jointStops=stops.ToArray(); _motorUse=budgets.Select(b=>b.Report).ToArray();
            TransferError=transferError;
            TransferResidualUse=transferResidual;
            TransferResidualTotal=TransferResidualTotal.Add(transferResidual);
            _sourceUse=sourceUse is null?Array.Empty<MechanicalSourceTotals>():
                sourceUse.Values.OrderBy(value=>value.Source.Index).ToArray();
            foreach(var use in _sourceUse)
            {
                var prior=_sourceTotals.TryGetValue(use.Source,out var value)?value:new(use.Source,0,default,0);
                _sourceTotals[use.Source]=prior.Add(use);
            }
            _transferBodyImpulses=transferBodyImpulses is null?Array.Empty<MechanicalTransferBodyImpulse>():
                transferBodyImpulses.Values.OrderBy(value=>value.Key.Transfer.Index)
                    .ThenBy(value=>value.Key.Role).ThenBy(value=>value.Key.Body.Index).ToArray();
            _transferUse=transferUse is null?Array.Empty<MechanicalTransferTotals>():
                transferUse.Values.OrderBy(value=>value.Transfer.Index).ToArray();
            foreach(var use in _transferUse)
            {
                var prior=_transferTotals.TryGetValue(use.Transfer,out var value)?value:
                    new(use.Transfer,use.Source,0,default,default,default,0);
                _transferTotals[use.Transfer]=prior.Add(use);
            }
            foreach(var use in _motorUse)
            {
                var previous=_motorTotals.TryGetValue(use.Joint,out var total)?total:new(use.Joint,0,0,0,0,0,0);
                _motorTotals[use.Joint]=previous.Add(use);
            }
            _lastMotion=new(before.Time,Time,StepIndex,motion.ToArray(),
                _objects.Select(o=>new PhysicsMotionEndpoint(o.Body.Id,o.Body.Pose)).ToArray());
            Phase=PhysicsWorldPhase.Idle;
            return new(count,events,sweeps,velocityIterations,positionIterations,
                predictionCalls,predictionMidpoints,predictionNewtonIterations,maximumPredictionCoordinates,_spatialWork,predictionConstraints,impulseCorrections.Add(predictionConstraints.Corrections));
        }
        catch
        {
            RestoreState(before);
            Phase=PhysicsWorldPhase.Idle;
            throw;
        }
        finally { _measureSpatialWork=false; }
    }

    private readonly record struct ImpactInput(Pair Pair,PhysicsImpact Impact,
        PhysicsBodySnapshot BeforeA,PhysicsBodySnapshot BeforeB,double Approach);

    private ImpactInput[] CaptureImpactEffects(Pair[] pairs,double time)
    {
        var inputs=new List<ImpactInput>();
        foreach(var pair in pairs)
        {
            // Observe onset of every contact episode, not only the child that
            // won a sweep tie. Capture all input before any warm-start impulse.
            if(pair.Contact.Contacts.Length!=0||pair.Contact.PreparedContacts.Length==0||
                !_effects.Any(e=>e.Body==pair.Key.A||e.Body==pair.Key.B)) continue;
            var a=_objects[pair.A].Body; var b=_objects[pair.B].Body;
            var separation=ConvexSeparation.Query(new ConvexPose(pair.ShapeA,a.Pose),
                new ConvexPose(pair.ShapeB,b.Pose));
            var relative=a.PointVelocity(separation.PointA)-b.PointVelocity(separation.PointB);
            var approach=-CollisionVector.Dot(relative,separation.Normal);
            if(approach<=0) continue;
            inputs.Add(new(pair,new(pair.Key,time,separation),a.Snapshot(),b.Snapshot(),approach));
        }
        return inputs.ToArray();
    }

    private void ApplyImpactEffects(ImpactInput[] inputs,PhysicsMotorBudget[] budgets)
    {
        var commands=new List<(PhysicsBody Target,PhysicsImpactImpulse Command)>();
        var colliders=new List<PhysicsColliderUpdate>();
        var joints=new List<PhysicsJointChange>();
        foreach(var input in inputs)
        {
            var pair=input.Pair; var a=_objects[pair.A].Body; var b=_objects[pair.B].Body;
            var context=new PhysicsImpactContext(input.Impact,new(input.BeforeA,a.Snapshot(),a.InverseMass,a.AngularVelocity),
                new(input.BeforeB,b.Snapshot(),b.InverseMass,b.AngularVelocity),input.Approach);
            foreach(var effect in _effects)
            {
                if(effect.Body!=pair.Key.A&&effect.Body!=pair.Key.B) continue;
                var returned=effect.OnImpact(context)??throw new InvalidOperationException("Impact effect returned null commands.");
                colliders.AddRange(returned.Colliders.ToArray());
                joints.AddRange(returned.Joints.ToArray());
                foreach(var command in returned.Impulses)
                {
                    var target=command.Body==a.Id?a:command.Body==b.Id?b:null;
                    if(target is null||target.MotionType!=PhysicsMotionType.Dynamic)
                        throw new InvalidOperationException("An impact impulse must address a dynamic body in that impact.");
                    if(!command.Impulse.IsFinite||!command.Point.IsFinite)
                        throw new InvalidOperationException("Impact commands must be finite.");
                    commands.Add((target,command));
                }
            }
        }
        // All callbacks see the same coupled response, without earlier effect
        // commands changing later inputs. Validate the batch before mutation.
        var proposed=ChangedJoints(joints.ToArray());
        foreach(var budget in budgets) budget.ValidateBinding(proposed.SingleOrDefault(j=>j.Id==budget.JointId));
        ApplyDeclarations(colliders.ToArray(),proposed);
        foreach(var budget in budgets) budget.Bind(_joints.SingleOrDefault(j=>j.Id==budget.JointId));
        foreach(var (target,command) in commands) target.CommitImpulse(command.Impulse,command.Point);
    }

    private ImpulseResponseConstraint[] ActuationResponses(IEnumerable<ConstraintGradient> contactNormals)
    {
        var jointRows=PhysicsJoint.CollectVelocityConstraints(_joints,_settings.PositionTolerance);
        var responses=jointRows.SelectMany(row=>row.ScalarRows).Select(row=>
        {
            if(row.TargetSpeed!=0||row.Softness!=0)
                throw new InvalidOperationException("Actuation response requires homogeneous hard joint constraints.");
            var relation=(row.MinimumImpulse,row.MaximumImpulse) switch
            {
                (double.NegativeInfinity,double.PositiveInfinity)=>ImpulseResponseRelation.Equal,
                (0,double.PositiveInfinity)=>ImpulseResponseRelation.Nonnegative,
                (double.NegativeInfinity,0)=>ImpulseResponseRelation.Nonpositive,
                _=>throw new InvalidOperationException("Unsupported actuation joint impulse interval.")
            };
            return new ImpulseResponseConstraint(row.Gradient,relation);
        });
        return responses.Concat(contactNormals.Select(normal=>
            new ImpulseResponseConstraint(normal,ImpulseResponseRelation.Nonnegative))).ToArray();
    }

    public SpringTriggerResult ReleaseSpring(PhysicsJointId guide)
    {
        RequireIdle();
        var state=Spring(guide);
        var result=state.State==SpringLatchState.Releasing?SpringTriggerResult.AlreadyReleased:
            !state.IsCharged?SpringTriggerResult.Empty:SpringTriggerResult.Released;
        var before=Capture();
        try
        {
            state=state with {LastTrigger=result};
            if(result==SpringTriggerResult.Released)
                state=state with {State=SpringLatchState.Releasing,ReleaseCount=checked(state.ReleaseCount+1)};
            _springs[guide]=state;
            if(result==SpringTriggerResult.Released)ApplySpringPolicy(state);
            return result;
        }
        catch {RestoreState(before);throw;}
    }

    private void ApplySpringPolicy(PhysicsLatchedSpringState state)
    {
        var load=state.Declaration;var (guide,transmission)=load.Resolve(_joints);
        var replacement=new PhysicsFrameJoint(guide.Id,guide.Kind,guide.A,guide.LocalA,guide.B,guide.LocalB,
            guide.Collision,new(load.RestCoordinate-load.Stroke,load.RestCoordinate),
            state.State==SpringLatchState.Latched?JointTravelDirection.Negative:JointTravelDirection.Positive);
        var coupling=new PhysicsTransmissionJoint(transmission.Id,transmission.Input,replacement,transmission.Ratio,
            state.State==SpringLatchState.Latched?TransmissionEngagement.Engaged:TransmissionEngagement.Open);
        ApplyDeclarations([],_joints.Select(j=>j.Id==guide.Id?(PhysicsJoint)replacement:j.Id==coupling.Id?coupling:j));
    }

    private bool RefreshSprings()
    {
        var changed=false;
        foreach(var previous in Springs)
        {
            var coordinate=previous.Declaration.Resolve(_joints).Guide.Travel.Error;
            var state=previous.Measure(coordinate);
            var latch=state.State==SpringLatchState.Releasing&&!state.IsCharged;
            if(latch)state=state with {State=SpringLatchState.Latched};
            _springs[state.Declaration.Guide]=state;
            if(latch){ApplySpringPolicy(state);changed=true;}
        }
        return changed;
    }

    public void InstallContactLoadSensors(IEnumerable<PhysicsContactLoadSensor> declarations)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(declarations);
        var sensors=new Dictionary<PhysicsBodyId,PhysicsContactLoadSensor>(_contactLoadSensors);
        foreach(var sensor in declarations)
        {
            ArgumentNullException.ThrowIfNull(sensor);
            if(!_objectIndices.ContainsKey(sensor.Frame)||!sensors.TryAdd(sensor.Frame,sensor))
                throw new ArgumentException("Contact-load sensor requires a unique owned frame.",nameof(declarations));
        }
        _contactLoadSensors=sensors;
    }

    public void InstallTiltSensors(IEnumerable<PhysicsTiltSensor> declarations)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(declarations);
        var states=new Dictionary<PhysicsBodyId,PhysicsTiltState>(_tilts);
        foreach(var sensor in declarations)
        {
            ArgumentNullException.ThrowIfNull(sensor);
            if(!_objectIndices.TryGetValue(sensor.Body,out var index)||_objects[index].Body.MotionType==PhysicsMotionType.Static)
                throw new ArgumentException("Tilt sensor requires an owned moving body.",nameof(declarations));
            var reference=_objects[index].Body.Pose.Rotation.Apply(sensor.LocalDirection);
            if(!states.TryAdd(sensor.Body,new(sensor,reference,PhysicsTiltPhase.Waiting,null)))
                throw new ArgumentException("Duplicate tilt sensor.",nameof(declarations));
        }
        _tilts=states;
    }
    private (int Iterations,int Events) AdvanceTilts(BodyTrajectory[] paths,double duration,double start)
    {
        var iterations=0;var events=0;
        foreach(var state in TiltStates)
        {
            var index=_objectIndices[state.Declaration.Body];
            if(state.Phase!=PhysicsTiltPhase.Waiting||_participation[index]==CollisionParticipation.Disabled)continue;
            var currentDirection=paths[index].At(duration).Rotation.Apply(state.Declaration.LocalDirection);
            var dot=CollisionVector.Dot(currentDirection,state.ReferenceDirection);
            iterations++;
            if(dot>state.Declaration.ThresholdCosine+PhysicsTiltSensor.CosineTolerance)continue;
            _tilts[state.Declaration.Body]=state with {Phase=PhysicsTiltPhase.Triggered,TriggerTime=start+duration};
            events++;
        }
        return (iterations,events);
    }

    public void InstallResidenceSensors(IEnumerable<PhysicsResidenceSensor> declarations)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(declarations);
        var sensors=new Dictionary<PhysicsResidenceKey,PhysicsResidenceSensor>(_residenceSensors);
        var states=new Dictionary<PhysicsResidenceKey,PhysicsResidenceState>(_residenceStates);
        foreach(var sensor in declarations)
        {
            ArgumentNullException.ThrowIfNull(sensor);
            if(!_objectIndices.TryGetValue(sensor.Key.Body,out var body)||
                !_objectIndices.ContainsKey(sensor.Key.Frame)||_objects[body].Body.MotionType!=PhysicsMotionType.Dynamic||
                !sensors.TryAdd(sensor.Key,sensor))
                throw new ArgumentException("Residence declaration requires a unique owned dynamic target and owned frame.",nameof(declarations));
            states.Add(sensor.Key,new(sensor.Key,PhysicsResidencePhase.Outside,0));
        }
        _residenceSensors=sensors;_residenceStates=states;
    }

    private bool ResidenceEligible(PhysicsResidenceSensor sensor)=>
        Collider(sensor.Key.Body).Declaration.Participation==CollisionParticipation.Enabled&&
        Collider(sensor.Key.Frame).Declaration.Participation==CollisionParticipation.Enabled&&
        sensor.Contains(_objects[_objectIndices[sensor.Key.Body]].Body,_objects[_objectIndices[sensor.Key.Frame]].Body);

    private void AdvanceResidence(double duration,IReadOnlyDictionary<PhysicsResidenceKey,bool> before)
    {
        foreach(var state in ResidenceStates)
        {
            if(state.Phase==PhysicsResidencePhase.Captured)continue;
            var sensor=_residenceSensors[state.Key];
            var eligible=ResidenceEligible(sensor);
            var elapsed=eligible&&before[state.Key]?Math.Min(sensor.Dwell,state.Elapsed+duration):0;
            var phase=!eligible?PhysicsResidencePhase.Outside:
                elapsed>=sensor.Dwell?PhysicsResidencePhase.Captured:PhysicsResidencePhase.Dwelling;
            _residenceStates[state.Key]=new(state.Key,phase,elapsed);
        }
    }

    public void InstallPassageSensors(IEnumerable<PhysicsPassageSensor> declarations)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(declarations);
        var sensors=new Dictionary<PhysicsBodyId,PhysicsPassageSensor>(_passageSensors);
        var states=new Dictionary<PhysicsPassageKey,PhysicsPassageState>(_passageStates);
        foreach(var supplied in declarations)
        {
            var sensor=new PhysicsPassageSensor(supplied.Frame,supplied.Radius,supplied.RearmClearance);
            if(!_objectIndices.ContainsKey(sensor.Frame)||!sensors.TryAdd(sensor.Frame,sensor))
                throw new ArgumentException("Sensor frame must be owned and not already installed.",nameof(declarations));
            foreach(var item in _objects)
            {
                if(item.Body.MotionType!=PhysicsMotionType.Dynamic||item.Body.Id==sensor.Frame)continue;
                var key=new PhysicsPassageKey(sensor.Frame,item.Body.Id);
                states.Add(key,new(key,PhysicsPassagePhase.Unarmed,0,Collider(key.Body).Revision,Collider(key.Frame).Revision));
            }
        }
        _passageSensors=sensors;_passageStates=states;
    }

    private void RefreshPassages()
    {
        if(_passageStates.Count==0)return;
        foreach(var state in PassageStates)
        {
            var key=state.Key;var sensor=_passageSensors[key.Frame];
            var body=_objects[_objectIndices[key.Body]].Body;var frame=_objects[_objectIndices[key.Frame]].Body;
            var collider=Collider(key.Body);var frameCollider=Collider(key.Frame);
            var phase=state.Phase;
            if(collider.Revision!=state.BodyRevision||frameCollider.Revision!=state.FrameRevision)
                phase=PhysicsPassagePhase.Unarmed;
            if(collider.Declaration.Participation==CollisionParticipation.Disabled||
                frameCollider.Declaration.Participation==CollisionParticipation.Disabled)
                phase=PhysicsPassagePhase.Unarmed;
            else
            {
                var direction=body.Pose.Rotation.Inverse().Apply(frame.Pose.Rotation.Apply(new(1,0,0)));
                var maximum=double.NegativeInfinity;
                var geometry=collider.Declaration.Geometry;
                for(var i=0;i<geometry.Count;i++)
                    maximum=Math.Max(maximum,frame.Pose.InverseTransformPoint(
                        body.Pose.TransformPoint(geometry[new(i)].Support(direction))).X);
                if(maximum<=-sensor.RearmClearance)phase=PhysicsPassagePhase.Armed;
                // Keep history inside the sweep's tolerance corridor so a crossing split
                // across steps is not lost. Projection beyond it is not swept motion.
                else if(frame.Pose.InverseTransformPoint(body.Center).X>2*_settings.PositionTolerance)
                    phase=PhysicsPassagePhase.Unarmed;
            }
            _passageStates[key]=state with {Phase=phase,BodyRevision=collider.Revision,FrameRevision=frameCollider.Revision};
        }
    }

    private int AdvancePassages(BodyTrajectory[] paths,double duration,double start,List<PhysicsPassageEvent> events)
    {
        if(_passageStates.Count==0)return 0;
        var iterations=0;
        foreach(var state in PassageStates)
        {
            if(state.Phase!=PhysicsPassagePhase.Armed)continue;
            var key=state.Key;var sensor=_passageSensors[key.Frame];
            var body=paths[_objectIndices[key.Body]];var frame=paths[_objectIndices[key.Frame]];
            var path=new PassageBoundaryPath(body,frame);
            // Require the new side of the plane, not a stationary/tangent point within its tolerance.
            var crossing=path.Sweep(duration,_settings.PositionTolerance,2*_settings.PositionTolerance);
            iterations+=crossing.Iterations;
            if(!crossing.Crossed)continue;
            var bodyPose=body.At(crossing.Time);var framePose=frame.At(crossing.Time);
            var extent=CylindricalRegion.Measure(Collider(key.Body).Declaration.Geometry,bodyPose,framePose,_settings.PositionTolerance);
            var kind=extent.RadialUpperBound<=sensor.Radius+_settings.PositionTolerance?
                PhysicsPassageEventKind.Passed:PhysicsPassageEventKind.OutsideAperture;
            _passageStates[key]=state with {Phase=PhysicsPassagePhase.Unarmed,
                PassedCount=kind==PhysicsPassageEventKind.Passed?checked(state.PassedCount+1):state.PassedCount};
            events.Add(new(key,kind,start+crossing.Time,bodyPose,framePose,extent));
        }
        return iterations;
    }

    /// <summary>Atomically install reservoirs with their declared initial balances. Existing identities cannot
    /// be reinstalled or resized; restoring a snapshot restores both membership and accounting.</summary>
    public void InstallEnergyStores(IEnumerable<PhysicsEnergyStoreDeclaration> declarations)
    {
        RequireIdle(); ArgumentNullException.ThrowIfNull(declarations);
        var proposed=new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(_energyStores);
        foreach(var declaration in declarations)
        {
            if(!_objectIndices.ContainsKey(declaration.Owner))
                throw new ArgumentException("Energy reservoir owner is not owned by this world.",nameof(declarations));
            var state=new PhysicsEnergyStoreState(declaration.Owner,declaration.Capacity,
                declaration.InitialEnergy,declaration.InitialEnergy,0,0);
            if(!proposed.TryAdd(declaration.Owner,state))
                throw new ArgumentException("Energy reservoir identity is already installed.",nameof(declarations));
            if(StepIndex!=0&&declaration.InitialEnergy!=0)
                throw new InvalidOperationException("Initial reservoir energy can only be installed before simulation begins.");
        }
        _energyStores=proposed;
    }

    /// <summary>Atomically install finite construction inventories. Runtime matter creation is forbidden;
    /// future transport must debit a declared source. This registry does not yet apply pressure forces.</summary>
    public void InstallGasNodes(ReadOnlySpan<PhysicsGasNode> nodes)
    {
        RequireIdle();
        if(StepIndex!=0)
            throw new InvalidOperationException("Initial gas can only be installed before simulation begins.");
        var proposed=new Dictionary<PhysicsGasNodeId,PhysicsGasNode>(_gasNodes);
        foreach(var node in nodes)
        {
            ArgumentNullException.ThrowIfNull(node);
            if(!_objectIndices.ContainsKey(node.Owner))
                throw new ArgumentException("Gas node owner is not owned by this world.",nameof(nodes));
            if(!proposed.TryAdd(node.Id,node))
                throw new ArgumentException("Gas node identity is already installed.",nameof(nodes));
        }
        _gasNodes=proposed;
    }

    public PhysicsGasNode GasNode(PhysicsGasNodeId id)=>
        _gasNodes.TryGetValue(id,out var node)?node:
            throw new ArgumentException("Gas node is not installed in this world.",nameof(id));

    public PhysicsEnergyStoreState EnergyStore(PhysicsBodyId owner)=>
        _energyStores.TryGetValue(owner,out var store)?store:
            throw new ArgumentException("Energy reservoir is not installed in this world.",nameof(owner));

    /// <returns>Actual accepted external work; capacity overflow is not stored or counted.</returns>
    public double ChargeEnergyStore(PhysicsBodyId owner,double power,double seconds)
    {
        RequireIdle();
        var previous=EnergyStore(owner);
        var charged=previous.Charged(power,seconds);
        _energyStores[owner]=charged;
        return charged.Energy-previous.Energy;
    }

    /// <summary>Atomic finite-work transfer from an owned reservoir to the shared
    /// constrained body response. Braking dissipation does not recharge the reservoir.</summary>
    public PoweredImpulseResult ReleaseEnergyStore(PhysicsBodyId owner,PhysicsBodyId body,
        CollisionVector axis,CollisionVector point,double targetSpeed,double maximumImpulse)
    {
        RequireIdle();
        var store=EnergyStore(owner);
        var before=Capture();
        try
        {
            var result=ApplyPoweredImpulse(body,axis,point,targetSpeed,maximumImpulse,store.Energy);
            _energyStores[owner]=store.Debited(result.SuppliedWork);
            return result;
        }
        catch
        {
            RestoreState(before);
            throw;
        }
    }

    /// <summary>Apply finite-work actuation at a world-space point using the live
    /// joint and contact-normal response. Initial constraint velocities must be
    /// feasible. This does not advance time or solve subsequent friction/impacts:
    /// those remain part of Step. Failure restores the entire world snapshot.</summary>
    public PoweredImpulseResult ApplyPoweredImpulse(PhysicsBodyId id,CollisionVector axis,CollisionVector point,
        double targetSpeed,double maximumImpulse,double availableWork)
    {
        RequireIdle();
        if(!_objectIndices.TryGetValue(id,out var index)||
            _objects[index].Body.MotionType!=PhysicsMotionType.Dynamic)
            throw new ArgumentException("Powered impulses require a world-owned dynamic body.",nameof(id));
        if(!axis.IsFinite||Math.Abs(axis.LengthSquared-1)>1e-10||!point.IsFinite)
            throw new ArgumentException("Powered impulse axis must be unit length and its point finite.");
        if(!double.IsFinite(targetSpeed)||!double.IsFinite(maximumImpulse)||maximumImpulse<0||
            !double.IsFinite(availableWork)||availableWork<0)
            throw new ArgumentException("Powered impulse budgets and target speed must be finite and valid.");
        var body=_objects[index].Body;
        var gradient=new ConstraintGradient([new(body,axis,CollisionVector.Cross(point-body.Center,axis))]);
        var before=Capture();
        Phase=PhysicsWorldPhase.Stepping;
        try
        {
            var normals=CurrentPairs().SelectMany(pair=>ContactGap.Query(_objects[pair.A].Body,pair.ShapeA,
                _objects[pair.B].Body,pair.ShapeB,ConvexSweep.ContactDistance,ConvexDistance.DefaultTolerance))
                .Select(gap=>gap.Gradient);
            return ConstrainedPoweredImpulse.Apply(gradient,targetSpeed,maximumImpulse,availableWork,
                ActuationResponses(normals),_settings.VelocityTolerance);
        }
        catch
        {
            RestoreState(before);
            throw;
        }
        finally { Phase=PhysicsWorldPhase.Idle; }
    }

    /// <summary>Apply an external instantaneous load to an owned dynamic body.
    /// Constraint/contact response is resolved by the next shared step.</summary>
    public void ApplyImpulse(PhysicsBodyId id,CollisionVector impulse,CollisionVector point)
    {
        RequireIdle();
        if(!_objectIndices.TryGetValue(id,out var index))
            throw new ArgumentException("Impulse target is not owned by this world.",nameof(id));
        var body=_objects[index].Body;
        if(body.MotionType!=PhysicsMotionType.Dynamic)
            throw new ArgumentException("External impulses require a dynamic target.",nameof(id));
        body.CommitImpulse(impulse,point);
    }

    /// <summary>Apply a world-space angular impulse without a linear impulse.
    /// Constraint/contact response is resolved by the next shared step.</summary>
    public void ApplyAngularImpulse(PhysicsBodyId id,CollisionVector impulse)
    {
        RequireIdle();
        if(!_objectIndices.TryGetValue(id,out var index))
            throw new ArgumentException("Impulse target is not owned by this world.",nameof(id));
        var body=_objects[index].Body;
        if(body.MotionType!=PhysicsMotionType.Dynamic)
            throw new ArgumentException("External impulses require a dynamic target.",nameof(id));
        body.CommitVelocity(body.AfterImpulse(default,impulse));
    }

    private void AdvanceAngularTravel(BodyTrajectory[] paths,double duration)
    {
        foreach(var joint in _joints.OfType<PhysicsFrameJoint>())
        {
            if(joint.Kind!=FrameJointKind.Hinge)continue;
            var increment=AngularPathMeasure.Measure(paths[_objectIndices[joint.A.Id]],joint.LocalA.Orientation,
                paths[_objectIndices[joint.B.Id]],joint.LocalB.Orientation,duration,duration*1e-8);
            var previous=_angularTravel[joint.Id];
            var total=new AngularPathTravel(previous.Winding+increment.Winding,previous.Distance+increment.Distance,
                previous.DistanceError+increment.DistanceError);
            if(!double.IsFinite(total.Winding)||!double.IsFinite(total.Distance)||!double.IsFinite(total.DistanceError))
                throw new InvalidOperationException("Accumulated angular travel exceeds numeric range.");
            _angularTravel[joint.Id]=total;
        }
    }

    private void RequireIdle()
    {
        if(Phase!=PhysicsWorldPhase.Idle) throw new InvalidOperationException("World mutation requires an idle phase.");
    }
    public Snapshot Capture()
    {
        RequireIdle();
        return new(this,_objects,_joints,_surfaces,_loads,Servos.ToArray(),Springs.ToArray(),TiltStates.ToArray(),ContactLoadSensors.ToArray(),EnergyStores.ToArray(),GasNodes.ToArray(),_residenceSensors.Values.ToArray(),ResidenceStates.ToArray(),_passageSensors.Values.OrderBy(sensor=>sensor.Frame.Index).ToArray(),PassageStates.ToArray(),_passageEvents,_compliantContacts.Values.ToArray(),_compliantEntries,_participation,_colliderRevisions,_objects.Select(o=>o.Body.Snapshot()).ToArray(),Order(_pairs.Values).Select(p=>(p,p.Contact.Capture())).ToArray(),
            Time,StepIndex,_impacts,_jointStops,_motorUse,MotorTotals.ToArray(),_transferUse,TransferTotals.ToArray(),_transferBodyImpulses,_sourceUse,SourceTotals.ToArray(),TransferError,TransferResidualUse,TransferResidualTotal,_sourceBindings.Values.OrderBy(source=>source.Id.Index).ToArray(),AngularTravelStates(),_effects.Select(e=>e.Capture()??throw new InvalidOperationException("Effect snapshot cannot be null.")).ToArray(),_lastMotion);
    }
    public void Restore(Snapshot snapshot)
    {
        RequireIdle(); ArgumentNullException.ThrowIfNull(snapshot);
        if(snapshot.Owner!=this) throw new ArgumentException("Snapshot belongs to another world.",nameof(snapshot));
        RestoreState(snapshot);
    }
    private void RestoreState(Snapshot snapshot)
    {
        _lastMotion=snapshot.Motion;
        _joints=(PhysicsJoint[])snapshot.Joints.Clone();
        _surfaces=(DrivenSurface[])snapshot.Surfaces.Clone();
        _loads=snapshot.Loads;
        _sourceBindings=snapshot.SourceBindingData.ToDictionary(source=>source.Id);
        _servos=snapshot.ServoData.ToDictionary(state=>state.Declaration.Joint);
        _springs=snapshot.SpringData.ToDictionary(state=>state.Declaration.Guide);
        _tilts=snapshot.TiltData.ToDictionary(state=>state.Declaration.Body);
        _contactLoadSensors=snapshot.ContactLoadSensorData.ToDictionary(sensor=>sensor.Frame);
        _residenceSensors=snapshot.ResidenceSensorData.ToDictionary(sensor=>sensor.Key);
        _residenceStates=snapshot.ResidenceStateData.ToDictionary(state=>state.Key);
        _passageSensors=snapshot.PassageSensorData.ToDictionary(sensor=>sensor.Frame);
        _passageStates=snapshot.PassageStateData.ToDictionary(state=>state.Key);
        _passageEvents=(PhysicsPassageEvent[])snapshot.PassageEventData.Clone();
        _energyStores=snapshot.EnergyData.ToDictionary(store=>store.Owner);
        _gasNodes=snapshot.GasData.ToDictionary(node=>node.Id);
        _compliantContacts=snapshot.CompliantData.ToDictionary(state=>state.Key);
        _compliantEntries=(PhysicsCompliantEntry[])snapshot.CompliantEntryData.Clone();
        _jointBodies=_joints.Select(j=>j.Bodies.ToArray().Select(b=>_objectIndices[b.Id]).ToArray()).ToArray();
        Array.Copy(snapshot.Objects,_objects,_objects.Length);
        Array.Copy(snapshot.Participation,_participation,_participation.Length);
        Array.Copy(snapshot.ColliderRevisions,_colliderRevisions,_colliderRevisions.Length);
        for(var i=0;i<_objects.Length;i++) _objects[i].Body.RestoreState(snapshot.Bodies[i]);
        for(var i=0;i<_effects.Length;i++) _effects[i].Restore(snapshot.EffectData[i]);
        _pairs.Clear();
        foreach(var (pair,state) in snapshot.Pairs)
        {
            pair.Contact.Restore(state); _pairs.Add(pair.Key,pair);
        }
        Time=snapshot.Time; StepIndex=snapshot.StepIndex; _impacts=(PhysicsImpact[])snapshot.ImpactData.Clone();
        _jointStops=(PhysicsJointStop[])snapshot.StopData.Clone();
        TransferError=snapshot.TransferError;
        TransferResidualUse=snapshot.TransferResidualUse;
        TransferResidualTotal=snapshot.TransferResidualTotal;
        _sourceUse=(MechanicalSourceTotals[])snapshot.SourceUseData.Clone();
        _sourceTotals=snapshot.SourceTotalData.ToDictionary(value=>value.Source);
        _transferBodyImpulses=(MechanicalTransferBodyImpulse[])snapshot.TransferBodyImpulseData.Clone();
        _transferUse=(MechanicalTransferTotals[])snapshot.TransferUseData.Clone();
        _transferTotals=snapshot.TransferTotalData.ToDictionary(value=>value.Transfer);
        _motorUse=(PhysicsMotorUse[])snapshot.MotorData.Clone();
        _motorTotals=snapshot.MotorTotalData.ToDictionary(total=>total.Joint);
        _angularTravel=snapshot.AngularTravelData.ToDictionary(state=>state.Joint,state=>state.Travel);
    }
    public sealed class Snapshot
    {
        internal PhysicsWorld Owner { get; }
        public PhysicsMotionHistory? Motion { get; }
        internal PhysicsObject[] Objects { get; }
        internal PhysicsJoint[] Joints { get; }
        internal DrivenSurface[] Surfaces { get; }
        internal PhysicsLoadSet Loads { get; }
        internal PhysicsServoState[] ServoData { get; }
        public ReadOnlySpan<PhysicsServoState> Servos=>ServoData;
        internal PhysicsContactLoadSensor[] ContactLoadSensorData { get; }
        public ReadOnlySpan<PhysicsContactLoadSensor> ContactLoadSensors=>ContactLoadSensorData;
        internal PhysicsTiltState[] TiltData { get; }
        public ReadOnlySpan<PhysicsTiltState> TiltStates=>TiltData;
        internal PhysicsLatchedSpringState[] SpringData { get; }
        public ReadOnlySpan<PhysicsLatchedSpringState> Springs=>SpringData;
        internal PhysicsResidenceSensor[] ResidenceSensorData { get; }
        internal PhysicsResidenceState[] ResidenceStateData { get; }
        public ReadOnlySpan<PhysicsResidenceState> ResidenceStates=>ResidenceStateData;
        internal PhysicsPassageSensor[] PassageSensorData { get; }
        internal PhysicsPassageState[] PassageStateData { get; }
        internal PhysicsPassageEvent[] PassageEventData { get; }
        public ReadOnlySpan<PhysicsPassageState> PassageStates=>PassageStateData;
        public ReadOnlySpan<PhysicsPassageEvent> PassageEvents=>PassageEventData;
        internal PhysicsGasNode[] GasData { get; }
        public ReadOnlySpan<PhysicsGasNode> GasStates=>GasData;
        internal PhysicsEnergyStoreState[] EnergyData { get; }
        public ReadOnlySpan<PhysicsEnergyStoreState> EnergyStates=>EnergyData;
        internal CompliantContactState[] CompliantData { get; }
        internal PhysicsCompliantEntry[] CompliantEntryData { get; }
        public ReadOnlySpan<CompliantContactState> CompliantStates=>CompliantData;
        internal CollisionParticipation[] Participation { get; }
        internal PhysicsColliderRevision[] ColliderRevisions { get; }
        internal PhysicsBodySnapshot[] Bodies { get; }
        internal (Pair Pair,PersistentContactPair.Snapshot State)[] Pairs { get; }
        internal PhysicsImpact[] ImpactData { get; }
        internal PhysicsJointStop[] StopData { get; }
        public TransferStepError TransferError { get; }
        public TransferResidualTotals TransferResidualUse { get; }
        public TransferResidualTotals TransferResidualTotal { get; }
        internal MechanicalTransferSource[] SourceBindingData { get; }
        internal MechanicalSourceTotals[] SourceUseData { get; }
        internal MechanicalSourceTotals[] SourceTotalData { get; }
        public ReadOnlySpan<MechanicalSourceTotals> SourceUse=>SourceUseData;
        public ReadOnlySpan<MechanicalSourceTotals> SourceTotals=>SourceTotalData;
        internal MechanicalTransferBodyImpulse[] TransferBodyImpulseData { get; }
        public ReadOnlySpan<MechanicalTransferBodyImpulse> TransferBodyImpulses=>TransferBodyImpulseData;
        internal MechanicalTransferTotals[] TransferUseData { get; }
        internal MechanicalTransferTotals[] TransferTotalData { get; }
        public ReadOnlySpan<MechanicalTransferTotals> TransferUse=>TransferUseData;
        public ReadOnlySpan<MechanicalTransferTotals> TransferTotals=>TransferTotalData;
        internal PhysicsMotorUse[] MotorData { get; }
        internal PhysicsAngularTravelState[] AngularTravelData { get; }
        public ReadOnlySpan<PhysicsAngularTravelState> AngularTravelStates=>AngularTravelData;
        internal PhysicsMotorTotals[] MotorTotalData { get; }
        public ReadOnlySpan<PhysicsMotorTotals> MotorTotals=>MotorTotalData;
        internal PhysicsImpactEffectState[] EffectData { get; }
        public double Time { get; }
        public ulong StepIndex { get; }
        public ReadOnlySpan<PhysicsBodySnapshot> BodyStates=>Bodies;
        internal Snapshot(PhysicsWorld owner,PhysicsObject[] objects,PhysicsJoint[] joints,DrivenSurface[] surfaces,PhysicsLoadSet loads,PhysicsServoState[] servos,PhysicsLatchedSpringState[] springs,PhysicsTiltState[] tilts,PhysicsContactLoadSensor[] contactLoadSensors,PhysicsEnergyStoreState[] energy,PhysicsGasNode[] gas,PhysicsResidenceSensor[] residenceSensors,PhysicsResidenceState[] residenceStates,PhysicsPassageSensor[] passageSensors,PhysicsPassageState[] passageStates,PhysicsPassageEvent[] passageEvents,CompliantContactState[] compliant,PhysicsCompliantEntry[] compliantEntries,CollisionParticipation[] participation,
            PhysicsColliderRevision[] colliderRevisions,PhysicsBodySnapshot[] bodies,(Pair Pair,PersistentContactPair.Snapshot State)[] pairs,
            double time,ulong stepIndex,PhysicsImpact[] impacts,PhysicsJointStop[] stops,PhysicsMotorUse[] motors,PhysicsMotorTotals[] motorTotals,MechanicalTransferTotals[] transferUse,MechanicalTransferTotals[] transferTotals,MechanicalTransferBodyImpulse[] transferBodyImpulses,MechanicalSourceTotals[] sourceUse,MechanicalSourceTotals[] sourceTotals,TransferStepError transferError,TransferResidualTotals transferResidualUse,TransferResidualTotals transferResidualTotal,MechanicalTransferSource[] sourceBindings,PhysicsAngularTravelState[] angularTravel,PhysicsImpactEffectState[] effects,PhysicsMotionHistory? motion)
        {
            Motion=motion;
            TransferError=transferError;
            TransferResidualUse=transferResidualUse;TransferResidualTotal=transferResidualTotal;
            SourceBindingData=(MechanicalTransferSource[])sourceBindings.Clone();
            SourceUseData=(MechanicalSourceTotals[])sourceUse.Clone();
            SourceTotalData=(MechanicalSourceTotals[])sourceTotals.Clone();
            TransferBodyImpulseData=(MechanicalTransferBodyImpulse[])transferBodyImpulses.Clone();
            TransferUseData=(MechanicalTransferTotals[])transferUse.Clone();
            TransferTotalData=(MechanicalTransferTotals[])transferTotals.Clone();
            ServoData=(PhysicsServoState[])servos.Clone();
            MotorTotalData=(PhysicsMotorTotals[])motorTotals.Clone();
            AngularTravelData=(PhysicsAngularTravelState[])angularTravel.Clone();
            SpringData=(PhysicsLatchedSpringState[])springs.Clone();
            TiltData=(PhysicsTiltState[])tilts.Clone();
            ContactLoadSensorData=(PhysicsContactLoadSensor[])contactLoadSensors.Clone();
            ResidenceSensorData=(PhysicsResidenceSensor[])residenceSensors.Clone();
            ResidenceStateData=(PhysicsResidenceState[])residenceStates.Clone();
            PassageSensorData=(PhysicsPassageSensor[])passageSensors.Clone();
            PassageStateData=(PhysicsPassageState[])passageStates.Clone();
            PassageEventData=(PhysicsPassageEvent[])passageEvents.Clone();
            EnergyData=(PhysicsEnergyStoreState[])energy.Clone();
            GasData=(PhysicsGasNode[])gas.Clone();
            CompliantData=(CompliantContactState[])compliant.Clone();CompliantEntryData=(PhysicsCompliantEntry[])compliantEntries.Clone();
            Joints=(PhysicsJoint[])joints.Clone(); Surfaces=(DrivenSurface[])surfaces.Clone(); Loads=loads;
            Objects=(PhysicsObject[])objects.Clone(); Participation=(CollisionParticipation[])participation.Clone();
            ColliderRevisions=(PhysicsColliderRevision[])colliderRevisions.Clone();
            EffectData=(PhysicsImpactEffectState[])effects.Clone();
            Owner=owner; Bodies=bodies; Pairs=pairs; Time=time; StepIndex=stepIndex; ImpactData=(PhysicsImpact[])impacts.Clone(); StopData=(PhysicsJointStop[])stops.Clone(); MotorData=(PhysicsMotorUse[])motors.Clone();
        }
    }
}
