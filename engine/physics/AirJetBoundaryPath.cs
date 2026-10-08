using System;

namespace CuriousContraptions.Physics;

public enum AirJetBoundary { Inlet, Outlet, Rim, Occlusion }
public readonly record struct AirJetSweepResult(ScalarSweepStatus Status,double Time,AirJetBoundary? Boundary,int Iterations);

/// <summary>A cylindrical field surface on captured source/receiver motion.
/// Positive values remain on the initial side. The rim uses squared radial
/// distance divided by twice the radius, avoiding a singularity on the axis.</summary>
internal sealed class AirJetBoundaryPath : ScalarBoundaryPath
{
    private readonly AirJetGeometry _jet;
    private readonly BodyTrajectory _source;
    private readonly BodyTrajectory _receiver;
    private readonly AirJetBoundary _boundary;
    private readonly double _sign;
    public override double Duration=>Math.Min(_source.Duration,_receiver.Duration);

    public AirJetBoundaryPath(AirJetGeometry jet,BodyTrajectory source,BodyTrajectory receiver,AirJetBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(jet);ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(receiver);
        if(boundary is not (AirJetBoundary.Inlet or AirJetBoundary.Outlet or AirJetBoundary.Rim))throw new ArgumentOutOfRangeException(nameof(boundary));
        _jet=jet;_source=source;_receiver=receiver;_boundary=boundary;
        _sign=Sample(0).Value<0?-1:1;
    }
    public override double SegmentEndAfter(double time)=>
        Math.Min(_source.SegmentEndAfter(time),_receiver.SegmentEndAfter(time));

    private ScalarBoundarySample Sample(double time)
    {
        var source=_source.At(time);var receiver=_receiver.At(time);
        var sourceArm=source.Rotation.Apply(_jet.LocalOrigin);
        var receiverArm=receiver.Rotation.Apply(_jet.LocalPoint);
        var offset=receiver.Center-source.Center+receiverArm-sourceArm;
        var axis=source.Rotation.Apply(_jet.LocalDirection);
        var axisRate=CollisionVector.Cross(_source.AngularVelocityAt(time),axis);
        var velocity=_receiver.LinearVelocityAt(time)-_source.LinearVelocityAt(time)+
            CollisionVector.Cross(_receiver.AngularVelocityAt(time),receiverArm)-
            CollisionVector.Cross(_source.AngularVelocityAt(time),sourceArm);
        var along=CollisionVector.Dot(offset,axis);
        var rate=CollisionVector.Dot(velocity,axis)+CollisionVector.Dot(offset,axisRate);
        var radial=offset-axis*along;
        var radialRate=velocity-axisRate*along-axis*rate;
        return _boundary switch
        {
            AirJetBoundary.Inlet=>new(along,rate),
            AirJetBoundary.Outlet=>new(_jet.Reach-along,-rate),
            AirJetBoundary.Rim=>new((_jet.Width*_jet.Width-radial.LengthSquared)/(2*_jet.Width),
                -CollisionVector.Dot(radial,radialRate)/_jet.Width),
            _=>throw new InvalidOperationException("Unsupported jet boundary.")
        };
    }
    public override ScalarBoundaryInterval Evaluate(double start,double end)
    {
        var h=end-start;
        var acceleration=_source.LinearAccelerationBound+_receiver.LinearAccelerationBound;
        var distance=Math.Max((_receiver.At(start).Center-_source.At(start).Center).Length,
            (_receiver.At(end).Center-_source.At(end).Center).Length)+acceleration*h*h/8+
            _jet.LocalOrigin.Length+_jet.LocalPoint.Length;
        var speed=Math.Max((_receiver.LinearVelocityAt(start)-_source.LinearVelocityAt(start)).Length,
            (_receiver.LinearVelocityAt(end)-_source.LinearVelocityAt(end)).Length)+acceleration*h*.5+
            _source.AngularSpeedBound*_jet.LocalOrigin.Length+_receiver.AngularSpeedBound*_jet.LocalPoint.Length;
        var sourceSpin=_source.AngularSpeedBound;
        var sourceTurn=_source.AngularAccelerationBound+sourceSpin*sourceSpin;
        acceleration+=sourceTurn*_jet.LocalOrigin.Length+
            (_receiver.AngularAccelerationBound+_receiver.AngularSpeedBound*_receiver.AngularSpeedBound)*_jet.LocalPoint.Length;
        var alongSpeed=speed+sourceSpin*distance;
        var alongAcceleration=acceleration+2*sourceSpin*speed+sourceTurn*distance;
        var curvature=alongAcceleration;
        if(_boundary==AirJetBoundary.Rim)
        {
            var radialSpeed=speed+sourceSpin*distance+alongSpeed;
            var radialAcceleration=acceleration+sourceTurn*distance+2*sourceSpin*alongSpeed+alongAcceleration;
            curvature=(radialSpeed*radialSpeed+distance*radialAcceleration)/_jet.Width;
        }
        var first=Sample(start);var last=Sample(end);
        return new(new(_sign*first.Value,_sign*first.Rate),new(_sign*last.Value,_sign*last.Rate),curvature,curvature);
    }
}
