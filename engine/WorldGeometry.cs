using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public readonly record struct SceneWorldBodyCapture
{
    internal BodyQueryGeometry QueryGeometry { get; }
    public PhysicsBodyId Id { get; }
    public BodySlot Slot { get; }
    public MachinePart? Owner { get; }
    public CompoundGeometry Geometry=>QueryGeometry.All.Geometry;
    public RigidPose Pose { get; }
    internal SceneWorldBodyCapture(PhysicsBodyId id,BodyQueryGeometry geometry,MachinePart? owner,BodySlot slot,RigidPose pose)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        Id=id; QueryGeometry=geometry; Owner=owner; Slot=slot; Pose=pose;
    }
}

public sealed record SceneWorldBodyDeclaration
{
    public SceneWorldBodyCapture Geometry { get; }
    public BodyDynamics Dynamics { get; }
    public ContactMaterial Material { get; }
    public PrescribedBodyMotion? PrescribedMotion { get; init; }
    public CollisionParticipation InitialParticipation { get; }
    public SceneWorldBodyDeclaration(SceneWorldBodyCapture geometry,BodyDynamics dynamics,ContactMaterial material)
    {
        ArgumentNullException.ThrowIfNull(geometry.QueryGeometry);
        ArgumentNullException.ThrowIfNull(geometry.Geometry);
        ArgumentNullException.ThrowIfNull(dynamics);
        Geometry=geometry; Dynamics=dynamics; Material=material;
        InitialParticipation=geometry.Owner is not { } owner||owner.Visible&&owner.PhysicsOwner.Visible
            ?CollisionParticipation.Enabled:CollisionParticipation.Disabled;
    }
    public PhysicsBody CreateBody(PhysicsBodyId id)=>Dynamics.CreateBody(id,Geometry.Pose,PrescribedMotion);
}

/// <summary>Direct-path geometry query. Air cannot pass through transparent solid proxies.</summary>
public static class WorldGeometry
{
    private static readonly ConditionalWeakTable<MachinePart,SceneCollisionGeometry> SceneGeometry=new();
    private static readonly BodyQueryGeometry WorkbenchGeometry=new(Workbench.Body.QueryPolicy,
        new[]{Workbench.Deck,Workbench.Base}.Select(box=>new ColliderQueryChild(
            new(new ConvexBox(SceneGeometryAdapter.CaptureVector(box.Half)),new(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(box.At))),
            SweepSurfaceKind.Box,true,ColliderQuerySource.PartProxy)).ToArray());

    /// <summary>Nearest fixed-orientation compound contact against committed solids and the workbench.
    /// Equal-distance contacts use workbench first, then ordinal part ID, body declaration order and child declaration order.
    /// Initial overlaps take priority over touching contacts. Both ignored parts are excluded entirely.
    /// ExcludeBodies omits dynamic spheres and hinged boxes; other declared solid proxies remain queryable.
    /// ExcludeDynamicBodies retains hinged boxes but omits dynamic spheres.
    /// Penetration is the nonnegative separation depth along the returned world-space normal.</summary>
    public static WorldSweepResult Sweep(MachineWorld world, CompoundGeometry geometry, RigidPose pose,
        CollisionVector displacement, MachinePart? ignoredOwner = null, MachinePart? ignoredBody = null,
        SweepBodyMode bodies = SweepBodyMode.IncludeBodies)
    {
        return CaptureSweep(world, ignoredOwner, ignoredBody, bodies).Sweep(geometry, pose, displacement);
    }

    /// <summary>Immutable solid geometry for repeated queries at one simulation instant.
    /// Re-capture after committed body/collider changes. During a run, scene presentation is not query authority.
    /// Changing body velocity alone does not invalidate captured solid geometry.</summary>
    public static WorldSweepSnapshot CaptureSweep(MachineWorld world, MachinePart? ignoredOwner,
        MachinePart? ignoredBody, SweepBodyMode bodies)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!Enum.IsDefined(bodies)) throw new ArgumentOutOfRangeException(nameof(bodies));
        var surfaces=new List<PreparedColliderGroup>();
        foreach(var body in CaptureBodies(world))
        {
            var part=body.Owner;
            if(part is not null && (part==ignoredOwner||part.PhysicsOwner==ignoredOwner||
                part==ignoredBody||part.PhysicsOwner==ignoredBody)) continue;
            var solids=body.QueryGeometry.Solids(bodies);
            if(solids is not null) surfaces.Add(new(solids,body.Pose,
                part is null?SweepObstacleKind.Workbench:SweepObstacleKind.Part,body.Id));
        }
        return new(surfaces.ToArray());
    }

    /// <summary>Stable body-local declarations and captured poses for production
    /// physics ownership. Moving a scene body does not rebuild its local shape.
    /// Workbench has no part owner; every other slot is scoped by its owner.</summary>
    public static SceneWorldBodyCapture[] CaptureBodies(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.HasPhysicsState
            ?world.PhysicsAssembly.CaptureQueryBodies(world.Physics)
            :CaptureConstructionBodies(world).Where(b=>b.Owner is not { } owner||owner.Visible&&owner.PhysicsOwner.Visible).ToArray();
    }

    /// <summary>Snapshot one explicitly anchored participant. Live body ownership is mandatory;
    /// construction previews use its authored slot pose. Presentation is never a runtime source.</summary>
    public static BodySpatialState CaptureSpatialState(MachineWorld world,SceneBodyKey key)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(key.Owner is null||!world.CollisionParts.Contains(key.Owner))
            throw new ArgumentException("Spatial participant must belong to this scene.",nameof(key));
        if(!world.HasPhysicsState)
            return new(key.Slot.Pose(key.Owner),key.Owner.Visible&&key.Owner.PhysicsOwner.Visible
                ?CollisionParticipation.Enabled:CollisionParticipation.Disabled);
        if(world.Physics.Phase!=PhysicsWorldPhase.Idle)
            throw new InvalidOperationException("Spatial snapshots require committed physics state.");
        var body=world.PhysicsAssembly.Body(key);
        return new(body.Pose,world.Physics.Collider(body.Id).Declaration.Participation);
    }

    private static SceneWorldBodyCapture[] CaptureConstructionBodies(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var result=new List<SceneWorldBodyCapture> {new(new(0),WorkbenchGeometry,null,Workbench.Body,RigidPose.Identity)};
        foreach(var part in world.CollisionParts
            .OrderBy(p=>p.Uid,StringComparer.Ordinal))
        {
            var geometry=Geometry(part);
            SceneGeometryAdapter.CaptureRigidPose(part.Transform);
            foreach(var body in geometry.Bodies) result.Add(new(new(result.Count),body.QueryGeometry,part,body.Slot,body.Pose(part)));
        }
        return result.ToArray();
    }

    /// <summary>Capture initial physical declarations for one run. A runtime must
    /// retain the created bodies; this is not a per-step synchronization API.</summary>
    public static SceneWorldBodyDeclaration[] CapturePhysicsBodies(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(world.HasPhysicsState)
            throw new InvalidOperationException("Body capture requires construction; runtime uses the existing owned bodies.");
        return CaptureConstructionBodies(world).Select(body=>
        {
            var dynamics=body.Slot.Dynamics(body.Owner);
            var correction=world.PlacementCorrections.SingleOrDefault(c=>c.Part==body.Owner?.PhysicsOwner);
            if(correction is null||dynamics.Motion==PhysicsMotionType.Dynamic)
                return new SceneWorldBodyDeclaration(body,dynamics,body.Slot.Material(body.Owner));
            if(dynamics.Motion!=PhysicsMotionType.Static)
                throw new InvalidOperationException("Assistance cannot replace an independently driven body.");
            var frame=correction.Path.StartPose;
            var local=new RigidPose(frame.InverseTransformPoint(body.Pose.Center),frame.Rotation.Inverse()*body.Pose.Rotation);
            var motion=new PrescribedBodyMotion(correction.Path,local,0);
            var geometry=new SceneWorldBodyCapture(body.Id,body.QueryGeometry,body.Owner,body.Slot,motion.At(0));
            return new SceneWorldBodyDeclaration(geometry,new(PhysicsMotionType.Kinematic,0,default,default,default),
                body.Slot.Material(body.Owner)) {PrescribedMotion=motion};
        }).ToArray();
    }

    public static ScenePhysicsAssembly CapturePhysicsAssembly(MachineWorld world,IReadOnlyList<RopePath> routes)
    {
        ArgumentNullException.ThrowIfNull(world);
        var bodies=CapturePhysicsBodies(world);
        ArgumentNullException.ThrowIfNull(routes);
        var ropes=RopeNetwork.DeclarePhysics(routes,bodies);
        var parts=world.CollisionParts
            .OrderBy(p=>p.Uid,StringComparer.Ordinal).ToArray();
        return new(bodies,parts.SelectMany(p=>p.PhysicsJoints).Concat(ropes).Concat(MechanicalNetwork.Declare(world.Parts,world.Connections)),parts.SelectMany(p=>p.PhysicsSurfaces));
    }

    private static SceneCollisionGeometry Geometry(MachinePart part)
    {
        if(!SceneGeometry.TryGetValue(part,out var geometry)||!geometry.Matches(part))
        {
            geometry=new(part);
            SceneGeometry.Remove(part); SceneGeometry.Add(part,geometry);
        }
        return geometry;
    }

    public static float Trace(TraceMedium medium, MachineWorld world, Vector3 origin, Vector3 direction, float range,
        MachinePart? emitter, MachinePart? receiver = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(!Enum.IsDefined(medium)) throw new ArgumentOutOfRangeException(nameof(medium));
        if(!origin.IsFinite()||!direction.IsFinite()||direction.LengthSquared()==0||!float.IsFinite(range)||range<0)
            throw new ArgumentException("A trace requires finite origin, nonzero direction and nonnegative range.");
        var trace=new PointTrace(SceneGeometryAdapter.CaptureVector(origin),SceneGeometryAdapter.CaptureVector(direction),range);
        foreach(var body in CaptureBodies(world))
        {
            var part=body.Owner;
            if(part is not null && (part==emitter||part.PhysicsOwner==emitter||
                part==receiver||part.PhysicsOwner==receiver)) continue;
            var collider=body.QueryGeometry.For(medium);
            if(collider is not null) trace.Test(collider,body.Pose);
        }
        return (float)trace.Closest;
    }
}
