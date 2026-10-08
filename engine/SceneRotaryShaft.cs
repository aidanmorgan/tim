using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Declarations for a finite-inertia local-Z shaft. Slots are created
/// once by the owning component; solved body pose is the visual authority.</summary>
public static class SceneRotaryShaft
{
    public static BodySlot Slot(Func<MachinePart,Node3D> visual,double mass,double radius,double width)
    {
        ArgumentNullException.ThrowIfNull(visual);
        if(!double.IsFinite(mass)||mass<=0||!double.IsFinite(radius)||radius<=0||!double.IsFinite(width)||width<=0)
            throw new ArgumentException("Shaft mass and dimensions must be finite and positive.");
        var axial=mass*radius*radius/2;
        var transverse=mass*(3*radius*radius+width*width)/12;
        return new(p=>SceneGeometryAdapter.CaptureRigidPose(visual(p).Transform),
            _=>new(PhysicsMotionType.Dynamic,mass,new(transverse,transverse,axial),default,default),
            BodyQueryPolicy.ExcludeFromStaticQueries,p=>p!.InitialContactMaterial,
            p=>[new(visual(p!),p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity))]);
    }
    public static SceneFrameJoint Guide(MachinePart owner,JointSlot joint,BodySlot body,Vector3 center)=>
        new(new(owner,joint),FrameJointKind.Hinge,new(owner,body),new(default,RigidRotation.Identity),
            new(owner,MachinePart.RootBody),new(SceneGeometryAdapter.CaptureVector(center),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
    public static void AddCollider(MachinePart owner,BodySlot body,double radius,double width)
    {
        ArgumentNullException.ThrowIfNull(owner);ArgumentNullException.ThrowIfNull(body);
        if(!double.IsFinite(radius)||radius<=0||!double.IsFinite(width)||width<=0)
            throw new ArgumentException("Shaft dimensions must be finite and positive.");
        var vertices=new List<CollisionVector>();
        for(var i=0;i<24;i++)
        foreach(var sign in new[]{-1,1})
        {
            var angle=Math.Tau*i/24;
            vertices.Add(new(Math.Cos(angle)*radius,Math.Sin(angle)*radius,sign*width/2));
        }
        owner.ConvexShapes.Add(new(new(new ConvexHull(vertices.ToArray()),AffineTransform.Identity),body));
    }
}
