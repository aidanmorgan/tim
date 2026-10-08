using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Physical port speed on captured trajectories, with a conservative
/// absolute curvature bound on each common geometric segment.</summary>
public abstract class MechanicalPortSpeedPath : TransferSpeedPath
{
    public abstract ScalarBoundarySample Sample(double time);

    public static MechanicalPortSpeedPath Capture(MechanicalPowerPort port,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths)
    {
        ArgumentNullException.ThrowIfNull(port);ArgumentNullException.ThrowIfNull(paths);
        port.Bind(bodies,joints);
        switch(port)
        {
            case AlignedPowerPort aligned:
                return new AlignedPowerSpeedPath(aligned,Capture(aligned.Port,bodies,joints,paths),bodies,paths);
            case CompositePowerPort composite:
                return new CompositeSpeedPath(composite.Components.Select(component=>Capture(component,bodies,joints,paths)).ToArray());
            case PointPowerPort point:
                return new PointSpeedPath(point,bodies[point.Body],bodies[point.Frame],joints,paths,
                    point.LocalPoint,point.FrameLocalAxis,1);
            case AngularPowerPort angular:
                return new AngularSpeedPath(angular,bodies[angular.Body],bodies[angular.Frame],joints,paths);
            case AxialPowerPort axial:
                var joint=(PhysicsFrameJoint)joints.Single(j=>j.Id==axial.Joint);
                return axial.Kind switch
                {
                    FrameJointKind.Slider=>new PointSpeedPath(axial,bodies[joint.A.Id],bodies[joint.B.Id],
                        joints,paths,joint.LocalA.Anchor,joint.LocalB.Orientation.Apply(new(0,0,1)),axial.Scale),
                    FrameJointKind.Hinge=>new HingeSpeedPath(axial,bodies[joint.A.Id],bodies[joint.B.Id],
                        joints,paths,joint.LocalA.Orientation,joint.LocalB.Orientation),
                    _=>throw new ArgumentOutOfRangeException(nameof(port))
                };
            default: throw new ArgumentException("Unsupported mechanical port geometry.",nameof(port));
        }
    }

    private sealed class CompositeSpeedPath : MechanicalPortSpeedPath
    {
        private readonly MechanicalPortSpeedPath[] _components;
        internal CompositeSpeedPath(MechanicalPortSpeedPath[] components)=>_components=components;
        public override double Duration=>_components.Min(path=>path.Duration);
        public override double SegmentEndAfter(double time)=>_components.Min(path=>path.SegmentEndAfter(time));
        public override double At(double time)
        {
            var value=_components.Sum(path=>path.At(time));
            if(!double.IsFinite(value))throw new InvalidOperationException("Composite port speed exceeds numeric range.");
            return value;
        }
        public override ScalarBoundarySample Sample(double time)
        {
            var value=0.0;var rate=0.0;
            foreach(var path in _components){var sample=path.Sample(time);value+=sample.Value;rate+=sample.Rate;}
            if(!double.IsFinite(value)||!double.IsFinite(rate))throw new InvalidOperationException("Composite port sample exceeds numeric range.");
            return new(value,rate);
        }
        public override ScalarBoundaryInterval Evaluate(double start,double end)
        {
            if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||end>SegmentEndAfter(start))
                throw new ArgumentOutOfRangeException(nameof(end));
            var curvature=0.0;
            foreach(var path in _components)
            {
                var bound=path.Evaluate(start,end).Curvature;
                if(bound>0)curvature=curvature==0?bound:Math.BitIncrement(curvature+bound);
            }
            if(!double.IsFinite(curvature))throw new InvalidOperationException("Composite port curvature exceeds numeric range.");
            return new(Sample(start),Sample(end),curvature,curvature);
        }
    }

    private abstract class TwoBodySpeedPath : MechanicalPortSpeedPath
    {
        private readonly MechanicalPowerPort _port;
        private readonly PhysicsJoint[] _joints;
        protected readonly PhysicsBody Body,Frame;
        protected readonly BodyTrajectory BodyPath,FramePath;
        public override double Duration=>Math.Min(BodyPath.Duration,FramePath.Duration);
    
        private protected TwoBodySpeedPath(MechanicalPowerPort port,PhysicsBody body,PhysicsBody frame,
            IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths)
        {
            _port=port;Body=body;Frame=frame;_joints=joints.ToArray();
            if(!paths.TryGetValue(body.Id,out var a)||a is null||
                !paths.TryGetValue(frame.Id,out var b)||b is null)
                throw new ArgumentException("Port speed requires both captured participant paths.");
            BodyPath=a;FramePath=b;ValidateSources();
        }
    
        private void ValidateSources()
        {
            BodyPath.ValidateSource(Body);FramePath.ValidateSource(Frame);
        }
    
        public override double At(double time)
        {
            ValidateSources();
            var sample=new Dictionary<PhysicsBodyId,PhysicsBody>
            {
                [Body.Id]=BodyPath.SampleBody(time),[Frame.Id]=FramePath.SampleBody(time)
            };
            var speed=_port.Bind(sample,_joints).Speed;
            if(!double.IsFinite(speed))throw new InvalidOperationException("Port speed exceeds numeric range.");
            return speed;
        }
    
        public override double SegmentEndAfter(double time)
        {
            ValidateSources();
            return Math.Min(BodyPath.SegmentEndAfter(time),FramePath.SegmentEndAfter(time));
        }
    
        public override ScalarBoundarySample Sample(double time)
        {
            var value=At(time);var rate=Derivative(time);
            if(!double.IsFinite(rate))throw new InvalidOperationException("Port speed derivative exceeds numeric range.");
            return new(value,rate);
        }
    
        public override ScalarBoundaryInterval Evaluate(double start,double end)
        {
            ValidateSources();
            if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||
                end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
            var curvature=CurvatureBound(start,end);
            if(curvature!=0)curvature=Math.BitIncrement(curvature*(1+1e-12));
            if(!double.IsFinite(curvature)||curvature<0)throw new InvalidOperationException("Port curvature exceeds numeric range.");
            return new(Sample(start),Sample(end),curvature,curvature);
        }
        private protected abstract double Derivative(double time);
        private protected abstract double CurvatureBound(double start,double end);
    }

    private sealed class PointSpeedPath : TwoBodySpeedPath
    {
        private readonly double _radius,_scale;
        private readonly CollisionVector _localPoint,_localAxis;
        internal PointSpeedPath(MechanicalPowerPort port,PhysicsBody body,PhysicsBody frame,
            IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,
            CollisionVector localPoint,CollisionVector localAxis,double scale):base(port,body,frame,joints,paths)
        {
            _localPoint=localPoint;_localAxis=localAxis;_radius=localPoint.Length;_scale=scale;
        }
        private protected override double Derivative(double time)
        {
            var a=BodyPath.At(time);var b=FramePath.At(time);
            var ra=a.Rotation.Apply(_localPoint);var rb=a.Center-b.Center+ra;
            var axis=b.Rotation.Apply(_localAxis);
            var va=BodyPath.LinearVelocityAt(time)-FramePath.LinearVelocityAt(time);
            var wa=BodyPath.PhysicalAngularVelocityAt(time);var wb=FramePath.PhysicalAngularVelocityAt(time);
            var raRate=CollisionVector.Cross(BodyPath.AngularVelocityAt(time),ra);
            var rbRate=va+raRate;
            var relative=va+CollisionVector.Cross(wa,ra)-CollisionVector.Cross(wb,rb);
            var relativeRate=BodyPath.LinearAccelerationAt(time)-FramePath.LinearAccelerationAt(time)+
                CollisionVector.Cross(BodyPath.PhysicalAngularAccelerationAt(time),ra)+CollisionVector.Cross(wa,raRate)-
                CollisionVector.Cross(FramePath.PhysicalAngularAccelerationAt(time),rb)-CollisionVector.Cross(wb,rbRate);
            return _scale*(CollisionVector.Dot(CollisionVector.Cross(FramePath.AngularVelocityAt(time),axis),relative)+
                CollisionVector.Dot(axis,relativeRate));
        }
        private protected override double CurvatureBound(double start,double end)
        {
            var h=end-start;
            var acceleration=BodyPath.LinearAccelerationBound+FramePath.LinearAccelerationBound;
            var velocity=Math.Max(
                (BodyPath.LinearVelocityAt(start)-FramePath.LinearVelocityAt(start)).Length,
                (BodyPath.LinearVelocityAt(end)-FramePath.LinearVelocityAt(end)).Length)+acceleration*h*.5;
            var distance=Math.Max(
                (BodyPath.At(start).Center-FramePath.At(start).Center).Length,
                (BodyPath.At(end).Center-FramePath.At(end).Center).Length)+acceleration*h*h/8+_radius;
            var wa=BodyPath.PhysicalAngularSpeedBound;var wb=FramePath.PhysicalAngularSpeedBound;
            var sa=BodyPath.AngularSpeedBound;var sb=FramePath.AngularSpeedBound;
            var aa=BodyPath.PhysicalAngularAccelerationBound;var ab=FramePath.PhysicalAngularAccelerationBound;
            var raRate=sa*_radius;var rbRate=velocity+raRate;
            var raSecond=(BodyPath.AngularAccelerationBound+sa*sa)*_radius;
            var rbSecond=acceleration+raSecond;
            var u=velocity+wa*_radius+wb*distance;
            var uRate=acceleration+aa*_radius+wa*raRate+ab*distance+wb*rbRate;
            var uSecond=BodyPath.LinearJerkBound+FramePath.LinearJerkBound+
                BodyPath.PhysicalAngularCurvatureBound*_radius+2*aa*raRate+wa*raSecond+
                FramePath.PhysicalAngularCurvatureBound*distance+2*ab*rbRate+wb*rbSecond;
            return Math.Abs(_scale)*((FramePath.AngularAccelerationBound+sb*sb)*u+2*sb*uRate+uSecond);
        }
    }

    private sealed class AngularSpeedPath : TwoBodySpeedPath
    {
        private readonly CollisionVector _axis;
        private readonly double _scale;
        internal AngularSpeedPath(AngularPowerPort port,PhysicsBody body,PhysicsBody frame,
            IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths):
            base(port,body,frame,joints,paths)
        {
            _axis=port.BodyLocalAxis;_scale=port.Scale;
        }
        private protected override double Derivative(double time)
        {
            var axis=BodyPath.At(time).Rotation.Apply(_axis);
            var relative=BodyPath.PhysicalAngularVelocityAt(time)-FramePath.PhysicalAngularVelocityAt(time);
            var acceleration=BodyPath.PhysicalAngularAccelerationAt(time)-FramePath.PhysicalAngularAccelerationAt(time);
            return _scale*(CollisionVector.Dot(CollisionVector.Cross(BodyPath.AngularVelocityAt(time),axis),relative)+
                CollisionVector.Dot(axis,acceleration));
        }
        private protected override double CurvatureBound(double start,double end)
        {
            var spin=BodyPath.PhysicalAngularSpeedBound+FramePath.PhysicalAngularSpeedBound;
            var acceleration=BodyPath.PhysicalAngularAccelerationBound+FramePath.PhysicalAngularAccelerationBound;
            var curvature=BodyPath.PhysicalAngularCurvatureBound+FramePath.PhysicalAngularCurvatureBound;
            var geometric=BodyPath.AngularSpeedBound;
            return Math.Abs(_scale)*((BodyPath.AngularAccelerationBound+geometric*geometric)*spin+
                2*geometric*acceleration+curvature);
        }
    }

    private sealed class HingeSpeedPath : TwoBodySpeedPath
    {
        private readonly RigidRotation _localA,_localB;
        private readonly double _scale;
        internal HingeSpeedPath(AxialPowerPort port,PhysicsBody body,PhysicsBody frame,
            IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,
            RigidRotation localA,RigidRotation localB):base(port,body,frame,joints,paths)
        {
            _localA=localA;_localB=localB;_scale=port.Scale;
        }
        private RigidRotation Relative(double time)=>
            (FramePath.At(time).Rotation*_localB).Inverse()*(BodyPath.At(time).Rotation*_localA);
        private double Denominator(double time)
        {
            var q=Relative(time);return q.W*q.W+q.Z*q.Z;
        }
        private protected override double Derivative(double time)
        {
            var frame=FramePath.At(time).Rotation*_localB;var q=Relative(time);
            var denominator=q.W*q.W+q.Z*q.Z;
            if(denominator<=1e-24)throw new InvalidOperationException("Antiparallel hinge frames have undefined twist.");
            var relative=frame.Inverse().Apply(BodyPath.AngularVelocityAt(time)-FramePath.AngularVelocityAt(time));
            var vector=new CollisionVector(q.X,q.Y,q.Z);
            var dv=(CollisionVector.Cross(relative,vector)+relative*q.W)*.5;
            var dw=-.5*CollisionVector.Dot(relative,vector);
            var nx=q.W*q.Y+q.Z*q.X;var ny=-q.W*q.X+q.Z*q.Y;
            var dx=dw*q.Y+q.W*dv.Y+dv.Z*q.X+q.Z*dv.X;
            var dy=-dw*q.X-q.W*dv.X+dv.Z*q.Y+q.Z*dv.Y;
            var dd=2*(q.W*dw+q.Z*dv.Z);
            var gradient=frame.Apply(new(nx/denominator,ny/denominator,1));
            var gradientRate=CollisionVector.Cross(FramePath.AngularVelocityAt(time),gradient)+
                frame.Apply(new((dx-nx*dd/denominator)/denominator,(dy-ny*dd/denominator)/denominator,0));
            return _scale*(CollisionVector.Dot(gradientRate,
                BodyPath.PhysicalAngularVelocityAt(time)-FramePath.PhysicalAngularVelocityAt(time))+
                CollisionVector.Dot(gradient,BodyPath.PhysicalAngularAccelerationAt(time)-FramePath.PhysicalAngularAccelerationAt(time)));
        }
        private protected override double CurvatureBound(double start,double end)
        {
            var spin=BodyPath.AngularSpeedBound+FramePath.AngularSpeedBound;
            var d=Math.BitDecrement(Math.Min(Denominator(start),Denominator(end))-
                spin*(end-start)*.5*(1+1e-12));
            if(!double.IsFinite(d)||d<=0)throw new InvalidOperationException("Hinge interval cannot exclude the twist singularity.");
            var q1=spin*.5;
            var q2=(BodyPath.AngularAccelerationBound+FramePath.AngularAccelerationBound)*.5+spin*spin*.25;
            var n1=2*Math.Sqrt(2)*q1;var n2=2*Math.Sqrt(2)*(q2+q1*q1);
            var d1=2*q1;var d2=2*(q1*q1+q2);
            var gradient=1/Math.Sqrt(d);
            var localRate=n1/d+d1/(d*d);
            var localSecond=n2/d+2*n1*d1/(d*d)+d2/(d*d)+2*d1*d1/(d*d*d);
            var frameSpin=FramePath.AngularSpeedBound;
            var gradientRate=frameSpin*gradient+localRate;
            var gradientSecond=(FramePath.AngularAccelerationBound+frameSpin*frameSpin)*gradient+
                2*frameSpin*localRate+localSecond;
            return Math.Abs(_scale)*(gradientSecond*(BodyPath.PhysicalAngularSpeedBound+FramePath.PhysicalAngularSpeedBound)+
                2*gradientRate*(BodyPath.PhysicalAngularAccelerationBound+FramePath.PhysicalAngularAccelerationBound)+
                gradient*(BodyPath.PhysicalAngularCurvatureBound+FramePath.PhysicalAngularCurvatureBound));
        }
    }
}
