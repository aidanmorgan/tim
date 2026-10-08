using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Projects an existing work coordinate by the signed alignment of two
/// authoritative axes. The same gain scales speed and every reaction term.
/// This is directional work conversion, not an exposure or supply declaration.</summary>
public sealed record AlignedPowerPort : MechanicalPowerPort
{
    public MechanicalPowerPort Port { get; }
    public PhysicsBodyId A { get; }
    public PhysicsBodyId B { get; }
    public CollisionVector LocalAxisA { get; }
    public CollisionVector LocalAxisB { get; }
    public AlignedPowerPort(MechanicalPowerPort port,PhysicsBodyId a,PhysicsBodyId b,
        CollisionVector localAxisA,CollisionVector localAxisB)
    {
        ArgumentNullException.ThrowIfNull(port);
        static CollisionVector Normalize(CollisionVector axis)
        {
            var length=axis.Length;
            if(!axis.IsFinite||!double.IsFinite(length)||length<=0)
                throw new ArgumentException("Alignment requires finite nonzero axes.");
            return axis/length;
        }
        Port=port;A=a;B=b;LocalAxisA=Normalize(localAxisA);LocalAxisB=Normalize(localAxisB);
    }
    public override ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints)
    {
        var a=Resolve(A,bodies);var b=Resolve(B,bodies);
        var gain=CollisionVector.Dot(a.Pose.Rotation.Apply(LocalAxisA),b.Pose.Rotation.Apply(LocalAxisB));
        return new(Port.Bind(bodies,joints).Terms.ToArray().Select(term=>
            new ConstraintTerm(term.Body,term.Linear*gain,term.Angular*gain)).ToArray());
    }
}

/// <summary>Product rule for a work-coordinate speed and signed axis alignment.
/// Orientation derivatives use geometric spin; the inner port retains physical spin.</summary>
internal sealed class AlignedPowerSpeedPath : MechanicalPortSpeedPath
{
    private readonly AlignedPowerPort _port;
    private readonly MechanicalPortSpeedPath _inner;
    private readonly PhysicsBody _a,_b;
    private readonly BodyTrajectory _pathA,_pathB;
    internal AlignedPowerSpeedPath(AlignedPowerPort port,MechanicalPortSpeedPath inner,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths)
    {
        _port=port;_inner=inner;_a=bodies[port.A];_b=bodies[port.B];
        if(!paths.TryGetValue(port.A,out var a)||a is null||!paths.TryGetValue(port.B,out var b)||b is null)
            throw new ArgumentException("Alignment requires captured axis participant paths.");
        _pathA=a;_pathB=b;ValidateSources();
    }
    private void ValidateSources(){_pathA.ValidateSource(_a);_pathB.ValidateSource(_b);}
    public override double Duration=>Math.Min(_inner.Duration,Math.Min(_pathA.Duration,_pathB.Duration));
    public override double SegmentEndAfter(double time)
    {
        ValidateSources();
        return Math.Min(_inner.SegmentEndAfter(time),Math.Min(_pathA.SegmentEndAfter(time),_pathB.SegmentEndAfter(time)));
    }
    public override double At(double time)=>Sample(time).Value;
    public override ScalarBoundarySample Sample(double time)
    {
        ValidateSources();
        var a=_pathA.At(time).Rotation.Apply(_port.LocalAxisA);
        var b=_pathB.At(time).Rotation.Apply(_port.LocalAxisB);
        var gain=CollisionVector.Dot(a,b);
        var rate=CollisionVector.Dot(CollisionVector.Cross(_pathA.AngularVelocityAt(time),a),b)+
            CollisionVector.Dot(a,CollisionVector.Cross(_pathB.AngularVelocityAt(time),b));
        var inner=_inner.Sample(time);
        var value=gain*inner.Value;var derivative=rate*inner.Value+gain*inner.Rate;
        if(!double.IsFinite(value)||!double.IsFinite(derivative))
            throw new InvalidOperationException("Aligned port speed exceeds numeric range.");
        return new(value,derivative);
    }
    public override ScalarBoundaryInterval Evaluate(double start,double end)
    {
        ValidateSources();
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||end>SegmentEndAfter(start))
            throw new ArgumentOutOfRangeException(nameof(end));
        var inner=_inner.Evaluate(start,end);var h=end-start;
        var speed=Math.Max(Math.Abs(inner.Start.Value),Math.Abs(inner.End.Value))+inner.Curvature*h*h/8;
        var rate=Math.Max(Math.Abs(inner.Start.Rate),Math.Abs(inner.End.Rate))+inner.Curvature*h*.5;
        var spin=_pathA.AngularSpeedBound+_pathB.AngularSpeedBound;
        var gainCurvature=_pathA.AngularAccelerationBound+_pathB.AngularAccelerationBound+spin*spin;
        var curvature=gainCurvature*speed+2*spin*rate+inner.Curvature;
        if(curvature!=0)curvature=Math.BitIncrement(curvature*(1+1e-12));
        if(!double.IsFinite(curvature)||curvature<0)
            throw new InvalidOperationException("Aligned port curvature exceeds numeric range.");
        return new(Sample(start),Sample(end),curvature,curvature);
    }
}
