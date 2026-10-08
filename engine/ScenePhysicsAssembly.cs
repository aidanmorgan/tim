using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions;

/// <summary>One run's stable scene identities and owned shared-engine bodies.
/// Scene capture is consumed once; subsequent physics must retain these objects.
/// Material-bearing collision objects retain those same owned bodies. Effects
/// and the enclosing world are supplied by the runtime.</summary>
public sealed class ScenePhysicsOverlapException(SceneBodyKey a,SceneBodyKey b) :
    InvalidOperationException($"Initial bodies overlap: {a.Owner?.Uid ?? "workbench"} / {b.Owner?.Uid ?? "workbench"}.");

public sealed class ScenePhysicsAssembly
{
    private readonly SceneWorldBodyDeclaration[] _declarations;
    private readonly PhysicsBody[] _bodies;
    private readonly Bridge.BodyPoseRead[] _presentationPoses;
    private readonly Bridge.BodyPublicationRead[] _publicationReads;
    private readonly PhysicsBodyId[] _queryOwners;
    private bool _presenting, _presentationRemoved;
    private readonly SceneAnimationAdapter _presenter;
    private readonly ScenePoseBatch _poseBatch;
    private ulong _presentationFrame;
    public IReadOnlyList<Node3D> PresentationTargets { get; }
    public SceneAnimationFrameWork LastPresentation { get; private set; }
    private readonly Bridge.PoseReferenceBinding[] _referenceBindings;
    private readonly ConditionalWeakTable<CompoundGeometry,BodyColliderGeometry>[] _queryGeometry;
    private readonly PhysicsObject[] _objects;
    private readonly PhysicsJoint[] _initialJoints;
    private readonly DrivenSurface[] _surfaces;
    public ReadOnlySpan<DrivenSurface> Surfaces=>_surfaces;
    private readonly IReadOnlyDictionary<SceneBodyKey,PhysicsBody> _byBody;
    private readonly IReadOnlyDictionary<SceneJointKey,PhysicsJointId> _jointIds;
    public ReadOnlySpan<SceneWorldBodyDeclaration> Declarations=>_declarations;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public ReadOnlySpan<PhysicsObject> Objects=>_objects;
    /// <summary>Construction bindings only. Runtime joints belong to PhysicsWorld.</summary>
    public ReadOnlySpan<PhysicsJoint> InitialJoints=>_initialJoints;

    public ScenePhysicsAssembly(IEnumerable<SceneWorldBodyDeclaration> declarations,IEnumerable<SceneJointDeclaration> joints,IEnumerable<SceneDrivenSurface> surfaces)
    {
        ArgumentNullException.ThrowIfNull(declarations); ArgumentNullException.ThrowIfNull(joints); ArgumentNullException.ThrowIfNull(surfaces);
        _declarations=declarations.ToArray();
        _bodies=new PhysicsBody[_declarations.Length];
        _presentationPoses=new Bridge.BodyPoseRead[_declarations.Length];
        _publicationReads=new Bridge.BodyPublicationRead[_declarations.Length];
        _queryOwners=new PhysicsBodyId[_declarations.Length];
        _referenceBindings=new Bridge.PoseReferenceBinding[_declarations.Length];
        _queryGeometry=new ConditionalWeakTable<CompoundGeometry,BodyColliderGeometry>[_declarations.Length];
        _objects=new PhysicsObject[_declarations.Length];
        var byBody=new Dictionary<SceneBodyKey,PhysicsBody>();
        for(var i=0;i<_declarations.Length;i++)
        {
            var declaration=_declarations[i];
            ArgumentNullException.ThrowIfNull(declaration);
            var key=new SceneBodyKey(declaration.Geometry.Owner,declaration.Geometry.Slot);
            var body=declaration.CreateBody(new(i));
            if(!byBody.TryAdd(key,body)) throw new ArgumentException("Duplicate scene body identity.");
            _bodies[i]=body;
            _queryGeometry[i]=new();
            _queryGeometry[i].Add(declaration.Geometry.Geometry,new(body.Id,declaration.Geometry.QueryGeometry));
            _objects[i]=new(body,declaration.Geometry.Geometry,declaration.Material) {InitialParticipation=declaration.InitialParticipation};
        }
        for(var i=0;i<_declarations.Length;i++)
        {
            var owner=_declarations[i].Geometry.Owner;
            if(owner is null) { _referenceBindings[i]=Bridge.PoseReferenceBinding.Fixed(RigidPose.Identity);_queryOwners[i]=new(i); continue; }
            if(!byBody.TryGetValue(new(owner.PhysicsOwner,MachinePart.RootBody),out var queryOwner))
                throw new ArgumentException("Scene body requires its physical owner's root query body.");
            _queryOwners[i]=queryOwner.Id;
            if(!byBody.TryGetValue(new(owner,MachinePart.RootBody),out var reference))
                throw new ArgumentException("Scene body requires its owner's root reference body.");
            _referenceBindings[i]=Bridge.PoseReferenceBinding.Attached(reference.Id,RigidPose.At(-SceneGeometryAdapter.CaptureVector(owner.LocalCenterOfMass)));
        }
        _byBody=new ReadOnlyDictionary<SceneBodyKey,PhysicsBody>(byBody);
        var supplied=joints.ToArray();
        _initialJoints=new PhysicsJoint[supplied.Length];
        var byJoint=new Dictionary<SceneJointKey,PhysicsJoint>();
        var indices=new Dictionary<SceneJointKey,int>();
        for(var i=0;i<supplied.Length;i++)
        {
            ArgumentNullException.ThrowIfNull(supplied[i]);
            if(!indices.TryAdd(supplied[i].Key,i)) throw new ArgumentException("Duplicate scene joint identity.");
        }
        var visiting=new HashSet<SceneJointKey>();
        PhysicsJoint Bind(SceneJointKey key)
        {
            if(byJoint.TryGetValue(key,out var bound)) return bound;
            if(!indices.TryGetValue(key,out var i)) throw new ArgumentException("Joint dependency is absent from this assembly.");
            if(!visiting.Add(key)) throw new ArgumentException("Scene joint dependencies contain a cycle.");
            var declaration=supplied[i];
            var dependencies=new Dictionary<SceneJointKey,PhysicsJoint>();
            foreach(var dependency in declaration.Dependencies)
                if(!dependencies.TryAdd(dependency,Bind(dependency)))
                    throw new ArgumentException("Duplicate joint dependency.");
            var joint=declaration.Bind(new(i),_byBody,new ReadOnlyDictionary<SceneJointKey,PhysicsJoint>(dependencies));
            if(joint is null||joint.Id!=new PhysicsJointId(i))
                throw new ArgumentException("Joint binding did not preserve its assigned identity.");
            foreach(var body in joint.Bodies)
                if(!byBody.Values.Contains(body)) throw new ArgumentException("Joint binding returned a foreign body.");
            byJoint.Add(declaration.Key,joint); _initialJoints[i]=joint;
            visiting.Remove(key);
            return joint;
        }
        foreach(var declaration in supplied) Bind(declaration.Key);
        _jointIds=new ReadOnlyDictionary<SceneJointKey,PhysicsJointId>(byJoint.ToDictionary(entry=>entry.Key,entry=>entry.Value.Id));
        _surfaces=surfaces.Select(surface=>(surface??throw new ArgumentException("Surface declaration cannot be null.")).Bind(new ReadOnlyDictionary<SceneJointKey,PhysicsJoint>(byJoint))).ToArray();
        var assets=new List<(PhysicsBodyId Body,ScenePoseAsset Asset)>();
        for(var i=0;i<_declarations.Length;i++)
            foreach(var asset in _declarations[i].Geometry.Slot.Presentation(_declarations[i].Geometry.Owner))
            {
                ArgumentNullException.ThrowIfNull(asset);
                ArgumentNullException.ThrowIfNull(asset.Target);
                ArgumentNullException.ThrowIfNull(asset.Map);
                if(!Enum.IsDefined(asset.Construction))throw new ArgumentException("Unsupported construction pose policy.");
                assets.Add((new(i),asset));
            }
        _presenter=new(Math.Max(1,assets.Count));
        var targets=new ScenePoseTarget[assets.Count];
        var nodes=new Node3D[assets.Count];
        var initialReads=CapturePresentationReads();
        for(var i=0;i<assets.Count;i++)
        {
            var (body,asset)=assets[i];
            var target=_presenter.Register(asset.Target);
            _presenter.ClaimPhysicalPose(target);
            nodes[i]=asset.Target;
            var reference=Bridge.PoseReferenceBinding.Attached(Body(asset.Reference.Body).Id,asset.Reference.Offset);
            var source=initialReads[body.Index];
            var read=new Bridge.BodyPoseRead(source.Id,source.MotionType,source.Pose,
                reference.Resolve(initialReads[reference.Body.Index].Pose));
            var map=asset.Construction==PoseConstructionPolicy.PreserveExact
                ?asset.Map.PreserveConstruction(read,asset.Target.Transform):asset.Map;
            targets[i]=new(target,body,reference,map);
        }
        PresentationTargets=Array.AsReadOnly(nodes);
        _poseBatch=new(_presenter,_bodies.Select(body=>body.Id).ToArray(),targets);
    }

    public MachinePart? Owner(PhysicsBodyId id)=>Key(id).Owner;
    public SceneBodyKey Key(PhysicsBodyId id)
    {
        if(id.Index<0||id.Index>=_declarations.Length) throw new ArgumentException("Unknown scene body ID.");
        var geometry=_declarations[id.Index].Geometry;
        return new(geometry.Owner,geometry.Slot);
    }


    internal SceneWorldBodyCapture[] CaptureQueryBodies(PhysicsWorld physics)
    {
        if(physics.Phase!=PhysicsWorldPhase.Idle)
            throw new InvalidOperationException("Queries require a committed physics state.");
        var result=new List<SceneWorldBodyCapture>();
        for(var i=0;i<_bodies.Length;i++)
        {
            var body=_bodies[i];
            var collider=physics.Collider(body.Id).Declaration;
            if(!_queryGeometry[i].TryGetValue(collider.Geometry,out var geometry))
                throw new InvalidOperationException("Collider replacement has no matching scene query declaration.");
            if(collider.Participation==CollisionParticipation.Disabled) continue;
            result.Add(new(body.Id,geometry.Value,_declarations[i].Geometry.Owner,_declarations[i].Geometry.Slot,body.Pose));
        }
        return result.ToArray();
    }

    internal BodyColliderGeometry CollisionGeometry(PhysicsWorld physics,SceneBodyKey key)
    {
        if(physics.Phase!=PhysicsWorldPhase.Idle)
            throw new InvalidOperationException("Geometry queries require a committed physics state.");
        var body=Body(key);
        var collider=physics.Collider(body.Id).Declaration;
        if(!_queryGeometry[body.Id.Index].TryGetValue(collider.Geometry,out var geometry))
            throw new InvalidOperationException("Collider replacement has no matching scene query declaration.");
        return geometry;
    }

    internal void ReplaceCollider(PhysicsWorld physics,SceneBodyKey key,BodyColliderGeometry geometry,
        ContactMaterial material,CollisionParticipation participation)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var body=Body(key);
        if(geometry.Body!=body.Id) throw new ArgumentException("Replacement geometry belongs to another core body.");
        // Commit shared collision first. Failed validation leaves query metadata unchanged.
        physics.ApplyColliderUpdates([new(body.Id,geometry.All.Geometry,material,participation)]);
        // Snapshots retain the geometry key, so rollback recovers its exact metadata.
        // Obsolete declarations without a live collider/snapshot can be collected.
        if(!_queryGeometry[body.Id.Index].TryGetValue(geometry.All.Geometry,out _))
            _queryGeometry[body.Id.Index].Add(geometry.All.Geometry,geometry);
    }

    /// <summary>Borrowed until the next capture; retained individual reads are values.
    /// This is a pose capture, not a complete gameplay transaction snapshot.</summary>
    public ReadOnlySpan<Bridge.BodyPoseRead> CapturePresentationReads()
    {
        if(_presenting||_presentationRemoved)
            throw new InvalidOperationException("Presentation capture is unavailable during submission or after removal.");
        for(var i=0;i<_bodies.Length;i++)
        {
            var body=_bodies[i];
            var reference=_referenceBindings[i];
            var frame=reference.Resolve(reference.Kind==Bridge.PoseReferenceKind.Body?_bodies[reference.Body.Index].Pose:RigidPose.Identity);
            _presentationPoses[i]=new(body.Id,body.MotionType,body.Pose,frame);
        }
        return _presentationPoses;
    }

    public PhysicsBodyId QueryOwnerId(SceneBodyKey key)=>_queryOwners[Body(key).Id.Index];

    /// <summary>Borrowed producer scratch; stage it before any subsequent capture.</summary>
    public ReadOnlySpan<Bridge.BodyPublicationRead> CapturePublicationReads(PhysicsWorld physics)
    {
        ArgumentNullException.ThrowIfNull(physics);
        if(physics.Phase!=PhysicsWorldPhase.Idle)
            throw new InvalidOperationException("Publication capture requires idle physics.");
        var poses=CapturePresentationReads();
        for(var i=0;i<_bodies.Length;i++)
        {
            var collider=physics.Collider(_bodies[i].Id);
            if(!_queryGeometry[i].TryGetValue(collider.Declaration.Geometry,out var geometry))
                throw new InvalidOperationException("Collider replacement has no matching query declaration.");
            _publicationReads[i]=new(poses[i],new(_queryOwners[i],collider.Revision,
                collider.Declaration.Participation,geometry.Geometry,geometry.For(TraceMedium.Light)),
                _declarations[_queryOwners[i].Index].Geometry.Owner?.Active==true
                    ?Bridge.OwnerActivity.Active:Bridge.OwnerActivity.Inactive,
                new(_bodies[i].LinearVelocity,_bodies[i].AngularVelocity));
        }
        return _publicationReads;
    }

    public void PresentCommitted(Bridge.PoseReadLease lease,double simulationTime
#if PLAYTEST
        ,PerformanceRecorder? performance=null
#endif
    )
    {
        if(_presenting||_presentationRemoved)
            throw new InvalidOperationException("Presentation binding is busy or removed.");
        if(lease.Count!=_presentationPoses.Length)throw new ArgumentException("Presentation topology does not match publication.");
        for(var i=0;i<_presentationPoses.Length;i++)
            _presentationPoses[i]=lease.SamplePose(simulationTime,i,_referenceBindings[i]);
        Submit(_presentationPoses
#if PLAYTEST
            ,performance
#endif
        );
    }

    private void Submit(ReadOnlySpan<Bridge.BodyPoseRead> reads
#if PLAYTEST
        ,PerformanceRecorder? performance=null
#endif
    )
    {
        var frame=checked(_presentationFrame+1);
        _presenting=true;
        try
        {
            _poseBatch.Queue(reads);
            LastPresentation=_presenter.Present(frame,0,0
#if PLAYTEST
                ,performance
#endif
            );
            _presentationFrame=frame;
        }
        finally { _presenting=false; }
    }

    public void RemovePresentation()
    {
        if(_presenting||_presentationRemoved)
            throw new InvalidOperationException("Presentation removal requires a live idle binding set.");
        _presenter.Reset();
        _presentationRemoved=true;
    }

    public PhysicsBody Body(SceneBodyKey key)=>_byBody.TryGetValue(key,out var body)
        ?body:throw new ArgumentException("Unknown scene body identity.",nameof(key));
    public PhysicsJointId JointId(SceneJointKey key)=>_jointIds.TryGetValue(key,out var id)
        ?id:throw new ArgumentException("Unknown scene joint identity.",nameof(key));
}
