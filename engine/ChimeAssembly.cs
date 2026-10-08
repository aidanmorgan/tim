using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum ChimeTubeId { Right, Left, Front, Back }
public readonly record struct ChimeTube(ChimeTubeId Id,Vector3 Center,float Length,ToneBand Tone);

/// <summary>Authored shape and mass declarations only. Motion and contact belong
/// to the shared rigid-body world, not a second pendulum integrator.</summary>
public static class ChimeAssembly
{
    public const float Length=1.5f;
    public const float ClapperFraction=.6f;
    public const float ClapperRadius=.12f;
    public const float TubeRadius=.065f;
    public const double ClapperMass=.2;
    public const double SailMass=.15;
    public const double Mass=ClapperMass+SailMass;
    public const double Damping=.9;
    public const float CenterDistance=(float)((ClapperMass*Length*ClapperFraction+SailMass*Length)/Mass);
    public static readonly Vector3 Pivot=new(0,.9f,0);
    public static readonly Vector3 SailHalf=new(.15f,.2f,.0275f);
    public static readonly Basis SailBasis=new(Vector3.Back,Mathf.DegToRad(35));
    public static readonly Vector3 SailFromCenter=new(0,CenterDistance-Length,0);
    public static readonly Vector3 ClapperFromCenter=new(0,CenterDistance-Length*ClapperFraction,0);
    public static IReadOnlyList<ChimeTube> Tubes { get; }=Array.AsReadOnly(new[]
    {
        new ChimeTube(ChimeTubeId.Right,new(.4f,.2f,0),.9f,ToneBand.Mid),
        new ChimeTube(ChimeTubeId.Left,new(-.4f,.075f,0),1.15f,ToneBand.Low),
        new ChimeTube(ChimeTubeId.Front,new(0,.275f,.4f),.75f,ToneBand.High),
        new ChimeTube(ChimeTubeId.Back,new(0,.15f,-.4f),1f,ToneBand.Mid)
    });
    public static ChimeTube Tube(ChimeTubeId id)
    {
        foreach(var tube in Tubes) if(tube.Id==id)return tube;
        throw new ArgumentOutOfRangeException(nameof(id));
    }
    public static BodyDynamics PendulumDynamics
    {
        get
        {
            var half=SceneGeometryAdapter.CaptureVector(SailHalf);
            var sail=new InertiaTensor(SailMass*(half.Y*half.Y+half.Z*half.Z)/3,
                SailMass*(half.X*half.X+half.Z*half.Z)/3,SailMass*(half.X*half.X+half.Y*half.Y)/3)
                .Rotated(SceneGeometryAdapter.CaptureRigidPose(new(SailBasis,default)).Rotation);
            var sphere=.4*ClapperMass*ClapperRadius*ClapperRadius;
            var shift=ClapperMass*ClapperFromCenter.Y*ClapperFromCenter.Y+
                SailMass*SailFromCenter.Y*SailFromCenter.Y;
            return new(PhysicsMotionType.Dynamic,Mass,
                new(sail.XX+sphere+shift,sail.YY+sphere,sail.ZZ+sphere+shift,sail.XY,sail.XZ,sail.YZ),
                default,default);
        }
    }
    /// <summary>Sixteen-sided convex cylinder: radial error below 0.0013 m.
    /// It uses the same support-mapped collision path as every other collider.</summary>
    public static ConvexGeometry TubeGeometry(ChimeTubeId id)
    {
        var tube=Tube(id);
        const int sides=16;
        var points=new CollisionVector[sides*2];
        for(var i=0;i<sides;i++)
        {
            var angle=i*Math.Tau/sides;
            var x=TubeRadius*Math.Cos(angle);var z=TubeRadius*Math.Sin(angle);
            points[i]=new(x,-tube.Length*.5,z);
            points[i+sides]=new(x,tube.Length*.5,z);
        }
        return new ConvexHull(points);
    }
}
