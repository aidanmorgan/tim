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
public enum JointBoundary { Lower, Upper, Direction }
public enum JointSweepStatus { Clear, Boundary }
public readonly record struct JointSweepResult(JointSweepStatus Status,double Time,JointBoundary? Boundary,int Iterations);

/// <summary>Instantaneous free-axis coordinate and speed, not accumulated travel.
/// Hinge coordinates use the principal-angle branch; slider coordinates use metres.</summary>
public readonly record struct PhysicsAxialMotion(double Coordinate,double Speed);

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
    internal void ValidatePaths(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance,double velocityTolerance)
    {
        if(paths.Length!=_bodies.Length) throw new ArgumentException("Joint trajectories must match its ordered participants.");
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(tolerance)||tolerance<=0||!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        for(var i=0;i<paths.Length;i++)
        {
            ArgumentNullException.ThrowIfNull(paths[i]); paths[i].ValidateSource(_bodies[i]);
            if(duration>paths[i].Duration) throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }
    protected static PhysicsBody ReboundBody(PhysicsBody source,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if(!bodies.TryGetValue(source.Id,out var body)||body is null||body.Id!=source.Id)
            throw new ArgumentException("Rebinding requires the declared body identity.");
        return body;
    }
    public abstract ReadOnlySpan<PhysicsJoint> Dependencies { get; }
    public abstract PhysicsJoint Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies);
    public abstract IReadOnlyList<ConstraintGradient> BilateralVelocityGradients();
    public abstract IReadOnlyList<IImpulseConstraint> UnilateralVelocityConstraints(double activationTolerance);
    public IReadOnlyList<IImpulseConstraint> VelocityConstraints(double activationTolerance)=>
        CollectVelocityConstraints([this],activationTolerance);
    public static IReadOnlyList<IImpulseConstraint> CollectVelocityConstraints(
        IEnumerable<PhysicsJoint> joints,double activationTolerance)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if(!double.IsFinite(activationTolerance)||activationTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(activationTolerance));
        var declarations=joints.ToArray();
        foreach(var joint in declarations) ArgumentNullException.ThrowIfNull(joint);
        var bilateral=declarations.SelectMany(j=>j.BilateralVelocityGradients())
            .Select(g=>new ImpulseConstraint(g,0,double.NegativeInfinity,double.PositiveInfinity)).ToArray();
        var rows=new List<IImpulseConstraint>();
        if(bilateral.Length>0) rows.Add(new BilateralConstraintBlock(bilateral));
        foreach(var joint in declarations) rows.AddRange(joint.UnilateralVelocityConstraints(activationTolerance));
        return rows;
    }
    public abstract IReadOnlyList<ConstraintAcceleration> AccelerationConstraints(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,double positionTolerance,double velocityTolerance);
    public abstract JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance,double velocityTolerance);
    public abstract double Error(double queryTolerance);
    void IPositionConstraint.Project(double tolerance,PositionProjector projector)=>Project(tolerance,projector);
    internal abstract void Project(double tolerance,PositionProjector projector);

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

public enum JointTravelDirection { Both, Positive, Negative }

public sealed class PhysicsFrameJoint : PhysicsJoint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    public FrameJointKind Kind { get; }
    public JointTravelRange? TravelRange { get; }
    public JointTravelDirection Direction { get; }
    public JointFrame LocalA { get; }
    public JointFrame LocalB { get; }
    public JointFrame FrameA=>World(A,LocalA);
    public JointFrame FrameB=>World(B,LocalB);
    public PhysicsFrameJoint(PhysicsJointId id,FrameJointKind kind,PhysicsBody a,JointFrame localA,
        PhysicsBody b,JointFrame localB,ConnectedBodyCollision collision,JointTravelRange? travelRange,JointTravelDirection direction):base(id,[a,b],collision)
    {
        if(!Enum.IsDefined(direction)||direction!=JointTravelDirection.Both&&kind==FrameJointKind.BallSocket)
            throw new ArgumentException("Direction requires a defined policy and an axial joint.");
        if(!Enum.IsDefined(kind)||!localA.Orientation.IsValid||!localB.Orientation.IsValid)
            throw new ArgumentException("Joint kind and local frames must be valid.");
        if(travelRange is not null&&(kind==FrameJointKind.BallSocket||
            kind==FrameJointKind.Hinge&&(travelRange.Lower<=-Math.PI||travelRange.Upper>=Math.PI)))
            throw new ArgumentException("Travel bounds require a slider or a hinge interval strictly inside the principal-angle branch.");
        A=a; B=b; Kind=kind; LocalA=localA; LocalB=localB; TravelRange=travelRange; Direction=direction;
    }
    public override ReadOnlySpan<PhysicsJoint> Dependencies=>[];
    public override PhysicsJoint Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)=>
        new PhysicsFrameJoint(Id,Kind,ReboundBody(A,bodies),LocalA,ReboundBody(B,bodies),LocalB,Collision,TravelRange,Direction);
    private static JointFrame World(PhysicsBody body,JointFrame frame)=>
        new(body.Pose.TransformPoint(frame.Anchor),body.Pose.Rotation*frame.Orientation);
    public JointEquation Travel=>JointEquations.Travel(Kind,A,B,FrameA,FrameB);
    public PhysicsAxialMotion Motion
    {
        get
        {
            var travel=Travel; // Rejects non-axial frame kinds.
            return new(travel.Error,travel.Jacobian.Bind(A,B).Speed);
        }
    }
    private JointEquation[] Equations()
    {
        var equations=JointEquations.Frames(Kind,A,B,FrameA,FrameB);
        if(TravelRange is null) return equations;
        var travel=Travel; var error=TravelRange.Violation(travel.Error);
        return error==0?equations:[..equations,new(travel.Jacobian,error,travel.ConvectiveAcceleration)];
    }
    public override IReadOnlyList<ConstraintGradient> BilateralVelocityGradients()
    {
        var gradients=JointEquations.Frames(Kind,A,B,FrameA,FrameB)
            .Select(e=>e.Jacobian.Bind(A,B)).ToList();
        if(TravelRange is not null&&TravelRange.Lower==TravelRange.Upper)
            gradients.Add(Travel.Jacobian.Bind(A,B));
        return gradients;
    }
    public override IReadOnlyList<IImpulseConstraint> UnilateralVelocityConstraints(double activationTolerance)
    {
        if(!double.IsFinite(activationTolerance)||activationTolerance<=0) throw new ArgumentOutOfRangeException(nameof(activationTolerance));
        IReadOnlyList<IImpulseConstraint> rows=[];
        if(Direction!=JointTravelDirection.Both)
            rows=[..rows,new ImpulseConstraint(Travel.Jacobian.Bind(A,B),0,
                Direction==JointTravelDirection.Positive?0:double.NegativeInfinity,
                Direction==JointTravelDirection.Negative?0:double.PositiveInfinity)];
        if(TravelRange is null||TravelRange.Lower==TravelRange.Upper) return rows;
        var travel=Travel;
        return [..rows,..JointConstraints.Limits(A,B,travel.Jacobian,travel.Error,
            TravelRange.Lower,TravelRange.Upper,activationTolerance)];
    }
    public override IReadOnlyList<ConstraintAcceleration> AccelerationConstraints(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,double positionTolerance,double velocityTolerance)
    {
        if(!double.IsFinite(positionTolerance)||positionTolerance<=0||!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(positionTolerance));
        var stage=(PhysicsFrameJoint)Rebind(sample);
        var rows=JointEquations.Frames(Kind,stage.A,stage.B,stage.FrameA,stage.FrameB).Select(e=>
            new ConstraintAcceleration(e.Jacobian.Bind(stage.A,stage.B),e.ConvectiveAcceleration,AccelerationRelation.Equal)).ToList();
        if(Kind==FrameJointKind.BallSocket) return rows;
        // Activation belongs to committed interval-start state. A trial midpoint
        // must not create/remove stops while Newton differentiates its residual.
        var initial=Travel;var speed=initial.Jacobian.Bind(A,B).Speed;
        var travel=stage.Travel;var gradient=travel.Jacobian.Bind(stage.A,stage.B);
        if(TravelRange is not null&&TravelRange.Lower==TravelRange.Upper)
        {
            rows.Add(new(gradient,travel.ConvectiveAcceleration,AccelerationRelation.Equal));
            return rows;
        }
        if(Direction==JointTravelDirection.Positive&&speed<=velocityTolerance||
            TravelRange is not null&&initial.Error-TravelRange.Lower<=positionTolerance&&speed<=velocityTolerance)
            rows.Add(new(gradient,travel.ConvectiveAcceleration,AccelerationRelation.Nonnegative));
        if(Direction==JointTravelDirection.Negative&&speed>=-velocityTolerance||
            TravelRange is not null&&TravelRange.Upper-initial.Error<=positionTolerance&&speed>=-velocityTolerance)
            rows.Add(new(gradient,travel.ConvectiveAcceleration,AccelerationRelation.Nonpositive));
        return rows;
    }
    public override JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance,double velocityTolerance)
    {
        ValidatePaths(paths,duration,tolerance,velocityTolerance);
        return new(JointSweepStatus.Clear,duration,null,0);
    }
    public override double Error(double queryTolerance)=>Equations().Max(e=>Math.Abs(e.Error));
    internal override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        PositionEquations.Project(Equations().Select(e=>new PositionEquation(e.Jacobian.Bind(A,B),e.Error)).ToArray(),projector);
    }
}
