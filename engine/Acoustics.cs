using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum ToneBand { Low, Mid, High }
public enum AcousticPattern { Cone, Omnidirectional }

/// <summary>Immutable gameplay pulse. Audio playback never determines reception.</summary>
public sealed class AcousticPulse
{
    public const float Speed=12;
    public const float Range=8;
    public const float Duration=.15f;
    public const float ConeCosine=.819152f; // 35 degree half-angle
    public Vector3 Origin { get; }
    public Vector3 Direction { get; }
    public ToneBand Tone { get; }
    public AcousticPattern Pattern { get; }
    public float Strength { get; }
    public int EmissionTick { get; }
    public int ExpiresTick => EmissionTick+(int)Math.Ceiling((Range/Speed+Duration)/MachineWorld.Tick);

    public AcousticPulse(Vector3 origin,Vector3 direction,ToneBand tone,int emissionTick,AcousticPattern pattern,float strength)
    {
        if(!origin.IsFinite()||!direction.IsFinite()||direction.LengthSquared()<1e-8f)
            throw new ArgumentException("Acoustic pulse needs a finite position and nonzero direction.");
        if(!Enum.IsDefined(tone)||emissionTick<0)throw new ArgumentOutOfRangeException(nameof(tone));
        if(!Enum.IsDefined(pattern))throw new ArgumentOutOfRangeException(nameof(pattern));
        if(!float.IsFinite(strength)||strength<=0||strength>1)throw new ArgumentOutOfRangeException(nameof(strength));
        Origin=origin;Direction=direction.Normalized();Tone=tone;EmissionTick=emissionTick;Pattern=pattern;Strength=strength;
    }

    public float Sample(Vector3 point,int tick)
    {
        if(!point.IsFinite()||tick<0)throw new ArgumentException("Acoustic sample requires finite position and nonnegative tick.");
        var offset=point-Origin;
        var distance=offset.Length();
        if(distance>Range||tick<EmissionTick||tick>=ExpiresTick)return 0;
        if(Pattern==AcousticPattern.Cone&&distance>1e-5f&&offset.Dot(Direction)/distance<ConeCosine)return 0;
        var localAge=(tick-EmissionTick)*MachineWorld.Tick-distance/Speed;
        if(localAge<0||localAge>=Duration)return 0;
        return Strength/(1+.08f*distance*distance);
    }
}

/// <summary>Strongest direct arrival wins; opaque collision geometry blocks the direct path.</summary>
public static class AcousticNetwork
{
    public static void Solve(MachineWorld world)
    {
        var pulses=world.Parts.Where(p=>p.Visible).OrderBy(p=>p.Uid,StringComparer.Ordinal)
            .SelectMany(p=>p.AcousticPulses.Select(pulse=>(Source:p,Pulse:pulse))).ToArray();
        var readings=new List<(MachinePart Receiver,float Level)>();
        foreach(var receiver in world.Parts.Where(p=>p.AcousticTarget.HasValue))
        {
            var level=0f;
            if(receiver.Visible)
            {
                var point=receiver.Transform*receiver.AcousticTarget!.Value;
                foreach(var (source,pulse) in pulses)
                {
                    if(source==receiver)continue;
                    var strength=pulse.Sample(point,world.Ticks);
                    if(strength<=level)continue;
                    var offset=point-pulse.Origin;
                    var distance=offset.Length();
                    if(distance>1e-5f && WorldGeometry.Trace(TraceMedium.Sound,world,pulse.Origin,offset/distance,distance,source,receiver)<distance-.0001f)continue;
                    level=strength;
                }
            }
            readings.Add((receiver,level));
        }
        foreach(var (receiver,level) in readings)receiver.ReceiveAcousticLevel(level);
    }
}
