using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Immutable compiled occlusion geometry at one authored scene pose.
/// All media share support-mapped convex queries; transparency is declaration
/// metadata, never a different intersection algorithm.</summary>
internal sealed class SceneTraceGeometry
{
    private readonly Transform3D _pose;
    private readonly BoxProxy[] _boxes;
    private readonly SphereProxy[] _spheres;
    private readonly TubeProxy[] _tubes;
    private readonly BendProxy[] _bends;
    private readonly FrustumProxy[] _frustums;
    private readonly (Transform3D Pose,Vector3 Half)[] _hinges;
    private readonly bool _dynamic;
    private readonly float _radius;
    private readonly CompoundGeometry? _opaque,_air;
    private static readonly HollowGeometrySettings HollowPrecision=new(.005);

    internal SceneTraceGeometry(MachinePart part)
    {
        _pose=part.Transform; _dynamic=part.Dynamic; _radius=part.Radius;
        _boxes=part.Boxes.ToArray(); _spheres=part.Spheres.ToArray(); _tubes=part.Tubes.ToArray();
        _bends=part.Bends.ToArray(); _frustums=part.Frustums.ToArray();
        _hinges=part.HingedBodies.Select(h=>(h.Pose,h.Half)).ToArray();
        var opaque=new List<ConvexInstance>(); var air=new List<ConvexInstance>();
        void Add(ConvexGeometry geometry,Transform3D pose,bool blocksLight)
        {
            var child=new ConvexInstance(geometry,pose);
            air.Add(child); if(blocksLight) opaque.Add(child);
        }
        void Hollow(CompoundGeometry geometry,Transform3D pose,bool blocksLight)
        {
            for(var i=0;i<geometry.Count;i++)
            {
                var child=geometry[new(i)];
                Add(child.Geometry,pose*child.Pose,blocksLight);
            }
        }
        foreach(var box in _boxes) Add(new ConvexBox(CollisionVector.From(box.Half)),_pose*new Transform3D(Basis.Identity,box.At),box.Opaque);
        foreach(var sphere in _spheres) Add(new ConvexSphere(sphere.Radius),_pose*new Transform3D(Basis.Identity,sphere.At),true);
        foreach(var hinge in _hinges) Add(new ConvexBox(CollisionVector.From(hinge.Half)),hinge.Pose,true);
        foreach(var tube in _tubes) Hollow(HollowGeometry.Tube(tube.HalfLength,tube.InnerRadius,tube.OuterRadius,HollowPrecision).Geometry,_pose*tube.Pose,tube.Opaque);
        foreach(var bend in _bends) Hollow(HollowGeometry.Bend(bend.Radius,bend.Sweep,bend.InnerRadius,bend.OuterRadius,HollowPrecision).Geometry,_pose*bend.Pose,false);
        foreach(var frustum in _frustums) Hollow(HollowGeometry.Frustum(frustum.HalfLength,frustum.InletRadius,frustum.OutletRadius,frustum.Thickness,HollowPrecision).Geometry,_pose*frustum.Pose,false);
        if(_dynamic) Add(new ConvexSphere(_radius),_pose,true);
        _opaque=opaque.Count==0?null:new(opaque.ToArray());
        _air=air.Count==0?null:new(air.ToArray());
    }
    internal bool Matches(MachinePart part)=>_pose==part.Transform&&_dynamic==part.Dynamic&&_radius==part.Radius&&
        _boxes.SequenceEqual(part.Boxes)&&_spheres.SequenceEqual(part.Spheres)&&_tubes.SequenceEqual(part.Tubes)&&
        _bends.SequenceEqual(part.Bends)&&_frustums.SequenceEqual(part.Frustums)&&
        _hinges.SequenceEqual(part.HingedBodies.Select(h=>(h.Pose,h.Half)));
    internal CompoundGeometry? For(TraceMedium medium)=>medium switch
    {
        TraceMedium.Light or TraceMedium.Sound=>_opaque,
        TraceMedium.Air=>_air,
        _=>throw new ArgumentOutOfRangeException(nameof(medium))
    };
}
