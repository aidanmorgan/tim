using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsJointId
{
    public int Index { get; }
    public PhysicsJointId(int index)
    {
        if(index<0) throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum ConnectedBodyCollision { Enabled, Disabled }

/// <summary>Immutable body-local declaration, persistent across world steps.
/// Fresh equations are rebuilt after pose changes.</summary>
public abstract class PhysicsJoint : IPositionConstraint
{
    public PhysicsJointId Id { get; }
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public ConnectedBodyCollision Collision { get; }
    protected PhysicsJoint(PhysicsJointId id,IEnumerable<PhysicsBody> bodies,ConnectedBodyCollision collision)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        var owned=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var body in bodies)
        {
            ArgumentNullException.ThrowIfNull(body);
            if(owned.TryGetValue(body.Id,out var prior)&&prior!=body)
                throw new ArgumentException("Joint identity refers to multiple body states.");
            owned[body.Id]=body;
        }
        _bodies=owned.Values.OrderBy(b=>b.Id.Index).ToArray();
        if(_bodies.Length<2||!Enum.IsDefined(collision)||!_bodies.Any(b=>b.MotionType==PhysicsMotionType.Dynamic))
            throw new ArgumentException("Joint requires distinct bodies, a dynamic participant and an explicit collision policy.");
        Id=id; Collision=collision;
    }
    internal int BodyIndex(PhysicsBody body)
    {
        var index=Array.IndexOf(_bodies,body);
        return index>=0?index:throw new ArgumentException("Foreign joint participant.");
    }
    internal bool Connects(PhysicsBody a,PhysicsBody b)=>Array.IndexOf(_bodies,a)>=0&&Array.IndexOf(_bodies,b)>=0;
    internal void ValidatePaths(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance)
    {
        if(paths.Length!=_bodies.Length) throw new ArgumentException("Joint trajectories must match its ordered participants.");
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(tolerance)||tolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        for(var i=0;i<paths.Length;i++)
        {
            ArgumentNullException.ThrowIfNull(paths[i]); paths[i].ValidateSource(_bodies[i]);
            if(duration>paths[i].Duration) throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }
    public abstract IReadOnlyList<IImpulseConstraint> VelocityConstraints(double activationTolerance);
    public abstract JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance);
    public abstract double Error(double queryTolerance);
    public abstract void Project(double tolerance,PositionProjector projector);

}

/// <summary>Finite lower/upper travel bounds, in length units for sliders and
/// radians for hinges. Null on a frame declaration means explicitly unbounded.</summary>
public sealed record JointTravelRange
{
    public double Lower { get; }
    public double Upper { get; }
    public JointTravelRange(double lower,double upper)
    {
        if(!double.IsFinite(lower)||!double.IsFinite(upper)||lower>upper)
            throw new ArgumentException("Joint travel bounds must be finite and ordered.");
        Lower=lower; Upper=upper;
    }
    public double Violation(double coordinate)=>coordinate-Math.Clamp(coordinate,Lower,Upper);
}

public sealed class PhysicsFrameJoint : PhysicsJoint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    public FrameJointKind Kind { get; }
    public JointTravelRange? TravelRange { get; }
    public JointFrame LocalA { get; }
    public JointFrame LocalB { get; }
    public JointFrame FrameA=>World(A,LocalA);
    public JointFrame FrameB=>World(B,LocalB);
    public PhysicsFrameJoint(PhysicsJointId id,FrameJointKind kind,PhysicsBody a,JointFrame localA,
        PhysicsBody b,JointFrame localB,ConnectedBodyCollision collision,JointTravelRange? travelRange):base(id,[a,b],collision)
    {
        if(!Enum.IsDefined(kind)||!localA.Orientation.IsValid||!localB.Orientation.IsValid)
            throw new ArgumentException("Joint kind and local frames must be valid.");
        if(travelRange is not null&&(kind==FrameJointKind.BallSocket||
            kind==FrameJointKind.Hinge&&(travelRange.Lower<=-Math.PI||travelRange.Upper>=Math.PI)))
            throw new ArgumentException("Travel bounds require a slider or a hinge interval strictly inside the principal-angle branch.");
        A=a; B=b; Kind=kind; LocalA=localA; LocalB=localB; TravelRange=travelRange;
    }
    private static JointFrame World(PhysicsBody body,JointFrame frame)=>
        new(body.Pose.TransformPoint(frame.Anchor),body.Pose.Rotation*frame.Orientation);
    public JointEquation Travel=>JointEquations.Travel(Kind,A,B,FrameA,FrameB);
    private JointEquation[] Equations()
    {
        var equations=JointEquations.Frames(Kind,A,B,FrameA,FrameB);
        if(TravelRange is null) return equations;
        var travel=Travel; var error=TravelRange.Violation(travel.Error);
        return error==0?equations:[..equations,new(travel.Jacobian,error)];
    }
    public override IReadOnlyList<IImpulseConstraint> VelocityConstraints(double activationTolerance)
    {
        if(!double.IsFinite(activationTolerance)||activationTolerance<=0) throw new ArgumentOutOfRangeException(nameof(activationTolerance));
        IReadOnlyList<IImpulseConstraint> rows=Kind switch
        {
            FrameJointKind.BallSocket=>JointConstraints.BallSocket(A,B,FrameA,FrameB),
            FrameJointKind.Hinge=>JointConstraints.Hinge(A,B,FrameA,FrameB),
            FrameJointKind.Slider=>JointConstraints.Slider(A,B,FrameA,FrameB),
            _=>throw new InvalidOperationException("Undefined frame joint.")
        };
        if(TravelRange is null) return rows;
        var travel=Travel;
        return [..rows,..JointConstraints.Limits(A,B,travel.Jacobian,travel.Error,
            TravelRange.Lower,TravelRange.Upper,activationTolerance)];
    }
    public override JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance)=>
        JointBoundarySweep.Frame(this,paths,duration,tolerance);
    public override double Error(double queryTolerance)=>Equations().Max(e=>Math.Abs(e.Error));
    public override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        PositionEquations.Project(Equations().Select(e=>new PositionEquation(e.Jacobian.Bind(A,B),e.Error)).ToArray(),projector);
    }
}
