using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>One-sided compliant-contact declaration and potential law. The world
/// owns engagement history and selects active laws for prediction; components
/// only declare potential participants and observe committed contact reports.</summary>
public sealed record CompliantContactLoad
{
    public CompliantContactKey Key=>new(Body,Frame);
    public CompliantContactInitialState InitialState { get; }
    public PhysicsBodyId Body { get; }
    public PhysicsBodyId Frame { get; }
    public double HalfX { get; }
    public double HalfZ { get; }
    public double RestHeight { get; }
    public double MaximumStroke { get; }
    public double Stiffness { get; }
    public double DampingRatio { get; }
    public CompressionSpringPotential Potential { get; }
    public CompliantContactLoad(PhysicsBodyId body,PhysicsBodyId frame,
        double halfX,double halfZ,double restHeight,double maximumStroke,double stiffness,double dampingRatio,CompliantContactInitialState initialState)
    {
        if(!Enum.IsDefined(initialState))throw new ArgumentOutOfRangeException(nameof(initialState));
        InitialState=initialState;
        if(body==frame)throw new ArgumentException("Compliant contact requires distinct participants.");
        if(!double.IsFinite(halfX)||halfX<=0||!double.IsFinite(halfZ)||halfZ<=0||
            !double.IsFinite(restHeight)||!double.IsFinite(maximumStroke)||maximumStroke<=0||
            !double.IsFinite(stiffness)||stiffness<=0||!double.IsFinite(dampingRatio)||dampingRatio<0)
            throw new ArgumentOutOfRangeException(nameof(stiffness));
        Body=body;Frame=frame;HalfX=halfX;HalfZ=halfZ;
        RestHeight=restHeight;MaximumStroke=maximumStroke;Stiffness=stiffness;DampingRatio=dampingRatio;
        Potential=new(stiffness,maximumStroke);
    }
    public void Validate(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if(!bodies.TryGetValue(Body,out var body)||body.MotionType!=PhysicsMotionType.Dynamic||!bodies.ContainsKey(Frame))
            throw new ArgumentException("Compliant contact requires its owned dynamic load and frame.");
    }
    public (BodyWrench Body,BodyWrench Frame) Evaluate(CompoundGeometry geometry,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> start,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> end)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        Validate(start);Validate(bodies);Validate(end);
        var body=bodies[Body];var frame=bodies[Frame];
        var sample=SupportFootprint.Sample(geometry,body.Pose,frame.Pose);
        var depth=RestHeight-sample.LowestPoint.Y;
        if(depth<=0||!sample.Fits(HalfX,HalfZ))return (default,default);
        var point=frame.Pose.TransformPoint(sample.LowestPoint);
        var normal=frame.Pose.Rotation.Apply(new(0,1,0));
        var bodyLever=CollisionVector.Cross(point-body.Center,normal);
        var frameLever=CollisionVector.Cross(point-frame.Center,normal);
        var inverseMass=body.InverseMass+CollisionVector.Dot(bodyLever,body.InverseInertia(bodyLever));
        if(frame.MotionType==PhysicsMotionType.Dynamic)
            inverseMass+=frame.InverseMass+CollisionVector.Dot(frameLever,frame.InverseInertia(frameLever));
        var speed=CollisionVector.Dot(body.PointVelocity(point)-frame.PointVelocity(point),normal);
        var damping=2*DampingRatio*Math.Sqrt(Stiffness/inverseMass);
        var initialDepth=RestHeight-SupportFootprint.Sample(geometry,start[Body].Pose,start[Frame].Pose).LowestPoint.Y;
        var finalDepth=RestHeight-SupportFootprint.Sample(geometry,end[Body].Pose,end[Frame].Pose).LowestPoint.Y;
        var magnitude=Math.Max(0,Potential.IntervalForce(initialDepth,finalDepth)-damping*speed);
        var force=normal*magnitude;
        return (new(force,CollisionVector.Cross(point-body.Center,force)),
            new(-force,CollisionVector.Cross(point-frame.Center,-force)));
    }
}
