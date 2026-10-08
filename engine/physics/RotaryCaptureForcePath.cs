using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum RotaryCaptureControlBoundary { PositiveCutIn, NegativeCutIn, PositiveTargetLimit, NegativeTargetLimit, MaximumPitchForce }

public readonly record struct RotaryCaptureForceInterval(double Start,double End,double RateBound);

/// <summary>Captured signed rest-force calibration path for a rotary receiver.
/// Exposure and supply membership are fixed at capture. The owner must restrict
/// the interval at field, source-flow and store-availability boundaries before
/// using these force, target and maximum-pitch feasibility certificates.</summary>
public sealed class RotaryCaptureForcePath
{
    private sealed record Branch(MechanicalTransferId Id,TransferSpeedPath Source,TransferSpeedPath Linear,
        JetTransferImpedance Impedance,JetTransferForcePath Force,PhysicsBody Rotor,PhysicsBody Nozzle,
        BodyTrajectory RotorPath,BodyTrajectory NozzlePath,CollisionVector Axis,CollisionVector Direction,double MaximumForce);
    private readonly Branch[] _branches;
    private readonly RotaryCaptureMaterial _material;
    public double Duration { get; }
    public IReadOnlyList<MechanicalTransferId> ActiveBranches { get; }

    public RotaryCaptureForcePath(RotaryCaptureDeclaration declaration,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsEnergyStoreState> stores,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double duration)
    {
        ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(paths);
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        // Preparation owns declaration, source, field and store validation.
        declaration.Prepare(bodies,joints,colliders,stores);
        var hinge=(PhysicsFrameJoint)joints.Single(joint=>joint.Id==declaration.Joint);
        var rotor=bodies[hinge.A.Id];var axis=hinge.LocalA.Orientation.Apply(new(0,0,1));
        BodyTrajectory Capture(PhysicsBody body)
        {
            if(!paths.TryGetValue(body.Id,out var path)||path is null||path.Duration<duration)
                throw new ArgumentException("Rotary force requires complete captured participant paths.",nameof(paths));
            path.ValidateSource(body);return path;
        }
        var rotorPath=Capture(rotor);
        var branches=new List<Branch>();
        foreach(var branch in declaration.Branches)
        {
            // Validate even inactive branch participants; omission is not a
            // substitute for a physical source/exposure switching boundary.
            var nozzle=bodies[branch.Field.Source];var nozzlePath=Capture(nozzle);
            Capture(bodies[branch.Field.Body]);
            var source=branch.Source.CaptureSpeed(bodies,joints,paths,duration);
            if(source.Duration<duration)throw new ArgumentException("Source path is shorter than the capture.");
            if(!branch.IsSupplied(bodies,joints,stores,colliders)||!branch.Field.IsExposed(bodies,colliders))continue;
            var linear=MechanicalPortSpeedPath.Capture(new PointPowerPort(branch.Field.Body,branch.Field.Source,
                branch.Field.LocalPoint,branch.Field.LocalDirection),bodies,joints,paths);
            branches.Add(new(branch.Id,source,linear,branch.Impedance,new(source,linear,branch.Impedance),rotor,nozzle,rotorPath,nozzlePath,
                axis,branch.Field.LocalDirection,branch.Impedance.MaximumForce));
        }
        _branches=branches.ToArray();_material=declaration.Material;Duration=duration;
        ActiveBranches=Array.AsReadOnly(_branches.Select(branch=>branch.Id).ToArray());
    }

    private void Validate(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration)throw new ArgumentOutOfRangeException(nameof(time));
        foreach(var branch in _branches)
        {
            branch.RotorPath.ValidateSource(branch.Rotor);branch.NozzlePath.ValidateSource(branch.Nozzle);
        }
    }

    public double At(double time)
    {
        Validate(time);var force=0.0;
        foreach(var branch in _branches)
        {
            var alignment=Alignment(branch,time);
            force+=alignment*branch.Force.At(time);
        }
        if(!double.IsFinite(force))throw new InvalidOperationException("Rotary force exceeds numeric range.");
        return force;
    }

    public double SegmentEndAfter(double time)
    {
        Validate(time);var end=Duration;
        foreach(var branch in _branches)
            end=Math.Min(end,Math.Min(branch.Force.SegmentEndAfter(time),
                Math.Min(branch.RotorPath.SegmentEndAfter(time),branch.NozzlePath.SegmentEndAfter(time))));
        return end;
    }

    public RotaryCaptureForceInterval Evaluate(double start,double end)
    {
        Validate(start);Validate(end);
        if(end<start||end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
        var rate=0.0;
        foreach(var branch in _branches)
        {
            // |alignment| <= 1, |alignment rate| <= sum of geometric spin
            // bounds. Force remains bounded by its declared material cap.
            rate+=branch.Force.Evaluate(start,end).RateBound+
                AlignmentRate(branch,start,end)*branch.MaximumForce;
        }
        if(rate>0)rate=Math.BitIncrement(rate*(1+1e-12));
        if(!double.IsFinite(rate)||rate<0)throw new InvalidOperationException("Rotary force rate exceeds numeric range.");
        return new(At(start),At(end),rate);
    }

    private static double Alignment(Branch branch,double time)=>
        Math.Clamp(CollisionVector.Dot(branch.RotorPath.At(time).Rotation.Apply(branch.Axis),
            branch.NozzlePath.At(time).Rotation.Apply(branch.Direction)),-1,1);

    private static double AlignmentRate(Branch branch,double start,double end)=>
        branch.RotorPath.DirectionSpeedBound(branch.Axis,start,end)+
        branch.NozzlePath.DirectionSpeedBound(branch.Direction,start,end);

    /// <summary>Certificate that every branch's alignment retains the initial
    /// net-force sign. Then target force cannot reverse: individual slip cuts
    /// own entry/exit of the zero-force plateau.</summary>
    public bool HasOneSidedAlignment(double duration)
    {
        Validate(duration);var sign=Math.Sign(At(0));
        if(sign==0)return false;
        for(double start=0;;)
        {
            var end=Math.Min(duration,SegmentEndAfter(start));
            foreach(var branch in _branches)
            {
                var lower=Math.Min(sign*Alignment(branch,start),sign*Alignment(branch,end))-
                    AlignmentRate(branch,start,end)*(end-start)*.5;
                if(!double.IsFinite(lower)||lower<0)return false;
            }
            if(end==duration)return true;
            if(end<=start)throw new InvalidOperationException("Rotary alignment segment made no progress.");
            start=end;
        }
    }

    private double TargetSlip(Branch branch,double time)
    {
        var surface=branch.Linear.At(time)+_material.MaximumPitch*Alignment(branch,time)*_material.RequestedSpeed(At(time));
        var slip=Math.Max(0,branch.Source.At(time))-surface;
        if(!double.IsFinite(surface)||!double.IsFinite(slip))
            throw new InvalidOperationException("Rotary target slip exceeds numeric range.");
        return slip;
    }

    private double TargetSlipRate(Branch branch,double start,double end,double restRate)
    {
        var spin=AlignmentRate(branch,start,end);
        var rate=branch.Source.AbsoluteRateBound(start,end)+branch.Linear.AbsoluteRateBound(start,end)+
            _material.MaximumPitch*(spin*_material.MaximumSpeed+_material.SpeedPerForce*restRate);
        if(rate>0)rate=Math.BitIncrement(rate*(1+1e-12));
        if(!double.IsFinite(rate)||rate<0)throw new InvalidOperationException("Rotary target slip rate exceeds numeric range.");
        return rate;
    }

    public double AtMaximumPitch(double time)
    {
        Validate(time);var force=0.0;var target=_material.RequestedSpeed(At(time));
        foreach(var branch in _branches)
        {
            var alignment=Alignment(branch,time);
            var surface=branch.Linear.At(time)+_material.MaximumPitch*alignment*target;
            if(!double.IsFinite(surface))throw new InvalidOperationException("Rotary target surface exceeds numeric range.");
            force+=alignment*branch.Impedance.Evaluate(Math.Max(0,branch.Source.At(time)),surface).Force;
        }
        if(!double.IsFinite(force))throw new InvalidOperationException("Rotary target force exceeds numeric range.");
        return force;
    }

    public RotaryCaptureForceInterval EvaluateMaximumPitch(double start,double end)
    {
        var rest=Evaluate(start,end);var rate=0.0;
        foreach(var branch in _branches)
            rate+=branch.Impedance.Conductance*TargetSlipRate(branch,start,end,rest.RateBound)+
                AlignmentRate(branch,start,end)*branch.MaximumForce;
        if(rate>0)rate=Math.BitIncrement(rate*(1+1e-12));
        if(!double.IsFinite(rate)||rate<0)throw new InvalidOperationException("Rotary target force rate exceeds numeric range.");
        return new(AtMaximumPitch(start),AtMaximumPitch(end),rate);
    }

    private static ScalarSweepResult DirectSweep(Func<double,double> sample,double threshold,double duration,double tolerance,double allowance)
    {
        if(!double.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
        if(!double.IsFinite(tolerance)||tolerance<=0)throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(!double.IsFinite(allowance)||allowance<0)throw new ArgumentOutOfRangeException(nameof(allowance));
        var initial=sample(0);
        var side=initial<threshold?MechanicalBoundarySide.Nonpositive:MechanicalBoundarySide.Nonnegative;
        var sign=side==MechanicalBoundarySide.Nonnegative?1.0:-1.0;
        double Gap(double t)=>sign*(sample(t)-threshold)+allowance;
        var g0=Gap(0);
        if(g0<=tolerance) return new(ScalarSweepStatus.Boundary,0,1);
        if(duration==0) return new(ScalarSweepStatus.Clear,0,1);
        var steps=Math.Max(128,(int)Math.Ceiling(duration/0.005));
        var dt=duration/steps;
        var prevTime=0.0;
        var prevGap=g0;
        for(var step=1;step<=steps;step++)
        {
            var curTime=step==steps?duration:step*dt;
            var curGap=Gap(curTime);
            if(curGap<=tolerance)
            {
                var low=prevTime;var high=curTime;
                for(var b=0;b<32;b++)
                {
                    var mid=(low+high)*.5;
                    var mg=Gap(mid);
                    if(mg<=tolerance) high=mid;
                    else low=mid;
                    if(high-low<=1e-11) break;
                }
                return new(ScalarSweepStatus.Boundary,high,step+32);
            }
            prevTime=curTime;
            prevGap=curGap;
        }
        return new(ScalarSweepStatus.Clear,duration,steps);
    }

    public ScalarSweepResult MaximumPitchSlipBoundary(MechanicalTransferId id,double duration=1,double tolerance=1e-8,double allowance=0)
    {
        var branch=_branches.SingleOrDefault(branch=>branch.Id==id)??
            throw new ArgumentException("Target slip requires an active captured branch.",nameof(id));
        return DirectSweep(t=>TargetSlip(branch,t),0,duration,tolerance,allowance);
    }

    public ScalarSweepResult Boundary(RotaryCaptureControlBoundary boundary,double duration=1,double tolerance=1e-7,double allowance=0)
    {
        var threshold=boundary switch
        {
            RotaryCaptureControlBoundary.MaximumPitchForce=>0,
            RotaryCaptureControlBoundary.PositiveCutIn=>_material.CutInForce,
            RotaryCaptureControlBoundary.NegativeCutIn=>-_material.CutInForce,
            RotaryCaptureControlBoundary.PositiveTargetLimit=>_material.MaximumSpeed/_material.SpeedPerForce,
            RotaryCaptureControlBoundary.NegativeTargetLimit=>-_material.MaximumSpeed/_material.SpeedPerForce,
            _=>throw new ArgumentOutOfRangeException(nameof(boundary))
        };
        if(!double.IsFinite(threshold))throw new InvalidOperationException("Rotary threshold exceeds numeric range.");
        return DirectSweep(boundary==RotaryCaptureControlBoundary.MaximumPitchForce?AtMaximumPitch:At,threshold,duration,tolerance,allowance);
    }
}
