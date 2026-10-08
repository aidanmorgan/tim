using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum RotaryCaptureState { Passive, Driven }

/// <summary>One exposed branch sampled with the rotor at rest. LinearSpeed retains
/// carrier/nozzle relative motion; Alignment is the signed axis projection.</summary>
public sealed record RotaryCaptureBranch
{
    public MechanicalTransferId Id { get; }
    public JetTransferImpedance Impedance { get; }
    public double SourceSpeed { get; }
    public double LinearSpeed { get; }
    public double Alignment { get; }
    public RotaryCaptureBranch(MechanicalTransferId id,JetTransferImpedance impedance,
        double sourceSpeed,double linearSpeed,double alignment)
    {
        ArgumentNullException.ThrowIfNull(impedance);
        if(!double.IsFinite(alignment)||Math.Abs(alignment)>1)
            throw new ArgumentOutOfRangeException(nameof(alignment));
        impedance.Evaluate(sourceSpeed,linearSpeed);
        Id=id;Impedance=impedance;SourceSpeed=sourceSpeed;LinearSpeed=linearSpeed;Alignment=alignment;
    }
}

/// <summary>A material calibration, not a velocity command or committed work.
/// A driven setting must be composed with paired ports using this same pitch.</summary>
public sealed record RotaryCaptureSetting
{
    public RotaryCaptureState State { get; }
    public double Pitch { get; }
    public double TargetSpeed { get; }
    public double DampingCoefficient { get; }
    internal RotaryCaptureSetting(RotaryCaptureState state,double pitch,double targetSpeed,double damping)
    { State=state;Pitch=pitch;TargetSpeed=targetSpeed;DampingCoefficient=damping; }
    public AxialDampingLoad CreateDamping(PhysicsJointId joint)=>
        new(joint,FrameJointKind.Hinge,DampingCoefficient,DampingCoefficient);
}

/// <summary>Passive rotary capture with a bounded conversion ratio. The requested
/// unloaded response is retained by reducing pitch when necessary, never by
/// introducing negative damping. All branch evaluations use the existing local
/// transfer material; no source allowance is allocated or spent here.</summary>
public sealed record RotaryCaptureMaterial
{
    public double MaximumPitch { get; }
    public double SpeedPerForce { get; }
    public double MaximumSpeed { get; }
    public double CutInForce { get; }
    private readonly double _coastResistance;
    public RotaryCaptureMaterial(double maximumPitch,double speedPerForce,double maximumSpeed,double cutInForce)
    {
        foreach(var value in new[]{maximumPitch,speedPerForce,maximumSpeed,cutInForce})
            if(!double.IsFinite(value)||value<=0)throw new ArgumentOutOfRangeException(nameof(value));
        _coastResistance=maximumPitch/speedPerForce;
        if(!double.IsFinite(_coastResistance)||_coastResistance<=0)
            throw new ArgumentOutOfRangeException(nameof(speedPerForce),"Coast resistance exceeds the supported numeric range.");
        MaximumPitch=maximumPitch;SpeedPerForce=speedPerForce;MaximumSpeed=maximumSpeed;CutInForce=cutInForce;
    }

    /// <summary>Continuous bounded response before the separately applied cut-in.
    /// Exposed for captured control paths; this is not a velocity command.</summary>
    public double RequestedSpeed(double restForce)
    {
        if(!double.IsFinite(restForce))throw new ArgumentOutOfRangeException(nameof(restForce));
        var target=Math.Sign(restForce)*(Math.Abs(restForce)>=MaximumSpeed/SpeedPerForce?
            MaximumSpeed:Math.Abs(restForce)*SpeedPerForce);
        if(!double.IsFinite(target)||restForce!=0&&target==0)
            throw new InvalidOperationException("Rotary response exceeds numeric range.");
        return target;
    }

    public RotaryCaptureSetting Calibrate(IEnumerable<RotaryCaptureBranch> branches)
    {
        ArgumentNullException.ThrowIfNull(branches);
        var owned=branches.ToArray();
        if(owned.Any(branch=>branch is null))throw new ArgumentException("Capture branches cannot be null.",nameof(branches));
        if(owned.Select(branch=>branch.Id).Distinct().Count()!=owned.Length)
            throw new ArgumentException("Duplicate rotary capture branch identity.",nameof(branches));
        Array.Sort(owned,(a,b)=>a.Id.Index.CompareTo(b.Id.Index));
        double NetForce(double pitch,double speed)
        {
            double total=0;
            foreach(var branch in owned)
            {
                var surface=branch.LinearSpeed+pitch*branch.Alignment*speed;
                if(!double.IsFinite(surface))throw new InvalidOperationException("Rotary surface speed exceeds numeric range.");
                var force=branch.Impedance.Evaluate(branch.SourceSpeed,surface).Force;
                total+=branch.Alignment*force;
                if(!double.IsFinite(total))throw new InvalidOperationException("Rotary capture force exceeds numeric range.");
            }
            return total;
        }
        var rest=NetForce(0,0);
        if(Math.Abs(rest)<CutInForce)
            return new(RotaryCaptureState.Passive,MaximumPitch,0,_coastResistance);
        var sign=Math.Sign(rest);
        var target=RequestedSpeed(rest);
        if(!double.IsFinite(target)||target==0)throw new InvalidOperationException("Rotary response exceeds numeric range.");
        var pitch=MaximumPitch;
        var forceAtTarget=sign*NetForce(pitch,target);
        if(forceAtTarget<=0)
        {
            // sign*NetForce(p,target) is nonincreasing in p, including capped
            // and opposing branches. Keep the driven (strictly positive) side:
            // zero-force plateaus above free speed do not meet a target from rest.
            double lower=0,upper=MaximumPitch;
            const int maximumIterations=64;
            for(var iteration=0;iteration<maximumIterations;iteration++)
            {
                var middle=lower+(upper-lower)*.5;
                if(middle==lower||middle==upper)break;
                if(sign*NetForce(middle,target)>0)lower=middle;else upper=middle;
            }
            pitch=lower;
            if(pitch<=0)throw new InvalidOperationException("Required rotary ratio is below the supported numeric range.");
            forceAtTarget=sign*NetForce(pitch,target);
        }
        var damping=pitch*(forceAtTarget/Math.Abs(target));
        if(!double.IsFinite(damping)||damping<=0)
            throw new InvalidOperationException("Rotary resistance is below or above the supported numeric range.");
        return new(RotaryCaptureState.Driven,pitch,target,damping);
    }
}
