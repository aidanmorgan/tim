using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Browser-only authored body capture. Providers are resolved before numeric query capture.</summary>
internal sealed class SceneBodyGeometryDeclaration
{
    internal BodySlot Slot { get; }
    internal BodyQueryGeometry QueryGeometry { get; }
    internal SceneBodyGeometryDeclaration(BodySlot slot, ReadOnlySpan<ColliderQueryChild> children)
    {
        ArgumentNullException.ThrowIfNull(slot);
        Slot = slot; QueryGeometry = new(slot.QueryPolicy, children);
    }
    internal RigidPose Pose(MachinePart owner) => Slot.Pose(owner);
}

/// <summary>Immutable body-local shape declarations, independent of captured scene
/// poses. Each hinge/body owns its geometry rather than being flattened into a
/// fixed fixture. Media and ownership remain metadata, never shape dispatch.</summary>
internal sealed class SceneCollisionGeometry
{
    private readonly BoxProxy[] _boxes;
    private readonly SphereProxy[] _spheres;
    private readonly ConvexProxy[] _convex;
    private readonly TubeProxy[] _tubes;
    private readonly BendProxy[] _bends;
    private readonly FrustumProxy[] _frustums;
    private readonly BodyEnvelope _envelope;
    private readonly float _radius;
    private readonly SceneBodyGeometryDeclaration[] _bodies;
    internal ReadOnlySpan<SceneBodyGeometryDeclaration> Bodies=>_bodies;
    internal const double MaximumSurfaceError=.005;
    private static readonly HollowGeometrySettings HollowPrecision=new(MaximumSurfaceError);

    private static BoxProxy[] ShapeDeclarations(IEnumerable<BoxProxy> boxes)=>boxes.Select(box=>
    {
        ArgumentNullException.ThrowIfNull(box.Body);
        if(!box.At.IsFinite()) throw new ArgumentException("Box position must be finite.");
        return box;
    }).ToArray();

    internal SceneCollisionGeometry(MachinePart part)
    {
        // Reject invalid scene transforms even if a part declares no solid children.
        SceneGeometryAdapter.CaptureRigidPose(part.Transform);
        _envelope=part.CollisionEnvelope; _radius=part.Radius;
        if(!Enum.IsDefined(_envelope)) throw new ArgumentOutOfRangeException(nameof(part));
        _boxes=ShapeDeclarations(part.Boxes); _spheres=part.Spheres.ToArray(); _convex=part.ConvexShapes.ToArray();
        foreach(var shape in _convex)
        {
            ArgumentNullException.ThrowIfNull(shape.Body);
            ArgumentNullException.ThrowIfNull(shape.Shape.Geometry);
        } _tubes=part.Tubes.ToArray();
        _bends=part.Bends.ToArray(); _frustums=part.Frustums.ToArray();
        var fixtures=new List<ColliderQueryChild>();
        void Add(ConvexGeometry geometry,Transform3D pose,bool opaque,SweepSurfaceKind surface)=>
            fixtures.Add(new(new(geometry,SceneGeometryAdapter.CaptureAffine(pose)),surface,opaque,ColliderQuerySource.PartProxy));
        void Hollow(CompoundGeometry geometry,Transform3D pose,bool opaque,SweepSurfaceKind surface)
        {
            for(var i=0;i<geometry.Count;i++)
            {
                var child=geometry[new(i)];
                Add(child.Geometry,pose*child.Pose.ToScene(),opaque,surface);
            }
        }
        foreach(var box in _boxes.Where(b=>b.Body==MachinePart.RootBody)) Add(new ConvexBox(SceneGeometryAdapter.CaptureVector(box.Half)),new Transform3D(Basis.Identity,box.At),box.Opaque,SweepSurfaceKind.Box);
        foreach(var sphere in _spheres.Where(s=>s.Body==MachinePart.RootBody)) Add(new ConvexSphere(sphere.Radius),new Transform3D(Basis.Identity,sphere.At),true,SweepSurfaceKind.Sphere);
        foreach(var shape in _convex.Where(s=>s.Body==MachinePart.RootBody))
            fixtures.Add(new(shape.Shape,SweepSurfaceKind.Convex,shape.Opaque,ColliderQuerySource.PartProxy));
        foreach(var tube in _tubes) Hollow(HollowGeometry.Tube(tube.HalfLength,tube.InnerRadius,tube.OuterRadius,HollowPrecision).Geometry,tube.Pose,tube.Opaque,SweepSurfaceKind.Tube);
        foreach(var bend in _bends) Hollow(HollowGeometry.Bend(bend.Radius,bend.Sweep,bend.InnerRadius,bend.OuterRadius,HollowPrecision).Geometry,bend.Pose,false,SweepSurfaceKind.Bend);
        foreach(var frustum in _frustums) Hollow(HollowGeometry.Frustum(frustum.HalfLength,frustum.InletRadius,frustum.OutletRadius,frustum.Thickness,HollowPrecision).Geometry,frustum.Pose,false,SweepSurfaceKind.Frustum);
        var bodies=new List<SceneBodyGeometryDeclaration>();
        if(_envelope==BodyEnvelope.Sphere)
            fixtures.Add(new(new(new ConvexSphere(_radius),AffineTransform.Identity),SweepSurfaceKind.Body,true,ColliderQuerySource.BodyEnvelope));
        if(fixtures.Count>0) bodies.Add(new(MachinePart.RootBody,fixtures.ToArray()));
        foreach(var sphere in _spheres) ArgumentNullException.ThrowIfNull(sphere.Body);
        var slots=_boxes.Select(b=>b.Body).Concat(_spheres.Select(s=>s.Body)).Concat(_convex.Select(s=>s.Body))
            .Where(slot=>slot!=MachinePart.RootBody).Distinct();
        foreach(var slot in slots)
        {
            var children=_boxes.Where(b=>b.Body==slot).Select(box=>new ColliderQueryChild(
                new(new ConvexBox(SceneGeometryAdapter.CaptureVector(box.Half)),new(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(box.At))),
                SweepSurfaceKind.Box,box.Opaque,ColliderQuerySource.PartProxy))
                .Concat(_spheres.Where(s=>s.Body==slot).Select(sphere=>new ColliderQueryChild(
                    new(new ConvexSphere(sphere.Radius),new(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(sphere.At))),
                    SweepSurfaceKind.Sphere,true,ColliderQuerySource.PartProxy)))
                .Concat(_convex.Where(s=>s.Body==slot).Select(shape=>new ColliderQueryChild(
                    shape.Shape,SweepSurfaceKind.Convex,shape.Opaque,ColliderQuerySource.PartProxy))).ToArray();
            bodies.Add(new(slot,children));
        }
        if(bodies.Select(b=>b.Slot).Distinct().Count()!=bodies.Count)
            throw new ArgumentException("A body slot cannot have multiple geometry declarations.");
        _bodies=bodies.ToArray();
    }
    internal bool Matches(MachinePart part)=>_envelope==part.CollisionEnvelope&&_radius==part.Radius&&
        _boxes.SequenceEqual(ShapeDeclarations(part.Boxes))&&_spheres.SequenceEqual(part.Spheres)&&_convex.SequenceEqual(part.ConvexShapes)&&_tubes.SequenceEqual(part.Tubes)&&
        _bends.SequenceEqual(part.Bends)&&_frustums.SequenceEqual(part.Frustums);
}
