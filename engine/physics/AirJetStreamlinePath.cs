using System;

namespace CuriousContraptions.Physics;

/// <summary>Finite parallel streamline joining the receiver sample to the
/// moving nozzle plane, on the same captured rigid paths as body integration.</summary>
internal sealed class AirJetStreamlinePath : SegmentTrajectory
{
    private readonly AirJetGeometry _geometry;
    private readonly BodyTrajectory _source,_receiver;
    private readonly PhysicsBody _sourceBody,_receiverBody;
    internal AirJetStreamlinePath(AirJetGeometry geometry,PhysicsBody source,PhysicsBody receiver,
        BodyTrajectory sourcePath,BodyTrajectory receiverPath)
    {
        _geometry=geometry;_sourceBody=source;_receiverBody=receiver;
        _source=sourcePath;_receiver=receiverPath;ValidateSources();
    }
    private void ValidateSources(){_source.ValidateSource(_sourceBody);_receiver.ValidateSource(_receiverBody);}
    public override double Duration=>Math.Min(_source.Duration,_receiver.Duration);
    public override CollisionSegment At(double time)
    {
        ValidateSources();return _geometry.SpatialSample(_source.At(time),_receiver.At(time)).Segment;
    }
    public override double SegmentEndAfter(double time)
    {
        ValidateSources();return Math.Min(_source.SegmentEndAfter(time),_receiver.SegmentEndAfter(time));
    }
    public override double SpeedBound(double start,double end)
    {
        ValidateSources();
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||end>SegmentEndAfter(start))
            throw new ArgumentOutOfRangeException(nameof(end));
        var h=end-start;
        var acceleration=_source.LinearAccelerationBound+_receiver.LinearAccelerationBound;
        var distance=Math.Max((_receiver.At(start).Center-_source.At(start).Center).Length,
            (_receiver.At(end).Center-_source.At(end).Center).Length)+acceleration*h*h/8+
            _geometry.LocalOrigin.Length+_geometry.LocalPoint.Length;
        var relative=Math.Max((_receiver.LinearVelocityAt(start)-_source.LinearVelocityAt(start)).Length,
            (_receiver.LinearVelocityAt(end)-_source.LinearVelocityAt(end)).Length)+acceleration*h*.5+
            _source.AngularSpeedBound*_geometry.LocalOrigin.Length+_receiver.AngularSpeedBound*_geometry.LocalPoint.Length;
        var endpoint=Math.Max(_receiver.LinearVelocityAt(start).Length,_receiver.LinearVelocityAt(end).Length)+
            _receiver.LinearAccelerationBound*h*.5+_receiver.AngularSpeedBound*_geometry.LocalPoint.Length;
        return endpoint+relative+2*_source.AngularSpeedBound*distance;
    }
}
