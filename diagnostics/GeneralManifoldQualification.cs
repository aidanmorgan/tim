#if PLAYTEST
using System;
using System.Linq;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralManifoldProbe { FlatImpact, RotatedFaces, Clear }
internal sealed record GeneralManifoldReport(GeneralManifoldProbe Probe,ContactManifoldStatus Status,
    double[][] Points,double MaximumAnchorError,double LinearSpeed,double AngularSpeed,double Residual,
    bool ExactRestore,bool ExactReplay);
internal static class GeneralManifoldQualification
{
    internal static GeneralManifoldReport Run(GeneralManifoldProbe probe)
    {
        var groundPose=probe switch
        {
            GeneralManifoldProbe.FlatImpact=>RigidPose.At(new(0,-1,0)),
            GeneralManifoldProbe.RotatedFaces=>new RigidPose(new(0,-1,0),RigidRotation.FromRotationVector(new(0,Math.PI/4,0))),
            GeneralManifoldProbe.Clear=>RigidPose.At(new(3,-1,0)),
            _=>throw new ArgumentOutOfRangeException(nameof(probe))
        };
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,1,0)),new(0,-3,0),default,1,new InertiaTensor(2.0/3,2.0/3,2.0/3));
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,groundPose,default,default);
        var shape=new ConvexInstance(new ConvexBox(new(1,1,1)),AffineTransform.Identity);
        var manifold=ContactManifold.Query(new ConvexMotion(shape,body.CreateTrajectory(0,default)).At(0),
            new ConvexMotion(shape,ground.CreateTrajectory(0,default)).At(0));
        var points=manifold.Points.ToArray();
        var before=body.Snapshot();
        ImpulseSolveResult Solve()=>ImpulseSolver.Solve(points.Select(p=>
            new ContactConstraint(ContactKinematics.AtPoint(body,ground,(p.PointA+p.PointB)*.5,manifold.Normal),0,0,.5)).ToArray());
        var solved=Solve(); var after=body.Snapshot();
        var speed=body.LinearVelocity.Length; var spin=body.AngularVelocity.Length;
        body.Restore(before); var restored=body.Snapshot()==before;
        Solve(); var replayed=body.Snapshot()==after;
        return new(probe,manifold.Status,points.Select(p=>new[]{p.PointA.X,p.PointA.Y,p.PointA.Z,p.Separation}).ToArray(),
            points.Length==0?0:points.Max(p=>(p.PointA-p.PointB-manifold.Normal*p.Separation).Length),
            speed,spin,solved.MaximumResidual,restored,replayed);
    }
}
#endif
