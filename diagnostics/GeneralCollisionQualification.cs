#if PLAYTEST
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralCollisionProbe { PureRotation, OpposingBodies, CompoundPassage, CompoundWall }
internal sealed record GeneralCollisionReport(GeneralCollisionProbe Probe,ConvexSweepStatus Status,double Time,int Queries);
internal static class GeneralCollisionQualification
{
    internal static GeneralCollisionReport Run(GeneralCollisionProbe probe)
    {
        if(probe==GeneralCollisionProbe.PureRotation)
        {
            var beam=new ConvexMotion(new(new ConvexBox(new(2,.05,.05)),Transform3D.Identity),default,default,new(0,0,120));
            var pose=new Transform3D(Basis.Identity,new(1.5f*Mathf.Cos(.4f),1.5f*Mathf.Sin(.4f),0));
            var obstacle=new ConvexMotion(new(new ConvexBox(new(.05,.05,.05)),pose),CollisionVector.From(pose.Origin),default,default);
            var hit=ConvexSweep.Cast(beam,obstacle,1.0/120);
            return new(probe,hit.Status,hit.Time,hit.Iterations);
        }
        if(probe==GeneralCollisionProbe.OpposingBodies)
        {
            var shape=new ConvexSphere(.5);
            var a=new ConvexMotion(new(shape,new(Basis.Identity,new(-2,0,0))),new(-2,0,0),new(400,0,0),default);
            var b=new ConvexMotion(new(shape,new(Basis.Identity,new(2,0,0))),new(2,0,0),new(-400,0,0),default);
            var hit=ConvexSweep.Cast(a,b,.005);
            return new(probe,hit.Status,hit.Time,hit.Iterations);
        }
        if(probe is not (GeneralCollisionProbe.CompoundPassage or GeneralCollisionProbe.CompoundWall))
            throw new System.ArgumentOutOfRangeException(nameof(probe));
        var children=new System.Collections.Generic.List<ConvexInstance>();
        foreach(var side in new[]{-1,1})
        {
            children.Add(new(new ConvexBox(new(1,.1,1.2)),new(Basis.Identity,new(3,side*1.1f,0))));
            children.Add(new(new ConvexBox(new(1,1,.1)),new(Basis.Identity,new(3,0,side*1.1f))));
        }
        var height=probe==GeneralCollisionProbe.CompoundWall?1.1f:0;
        var moving=new CompoundMotion(new([new(new ConvexSphere(.25),Transform3D.Identity)]),
            new(Basis.Identity,new(0,height,0)),new(0,height,0),new(6,0,0),default);
        var passage=new CompoundMotion(new(children.ToArray()),Transform3D.Identity,default,default,default);
        var result=CompoundCollision.Cast(moving,passage,1);
        return new(probe,result.Status,result.Time,result.NarrowPhaseCalls);
    }
}
#endif
