using Godot;
using System;

namespace CuriousContraptions;

public enum ToneBand { Low, Mid, High }

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
    public int EmissionTick { get; }
    public int ExpiresTick => EmissionTick+(int)Math.Ceiling((Range/Speed+Duration)/MachineWorld.Tick);

    public AcousticPulse(Vector3 origin,Vector3 direction,ToneBand tone,int emissionTick)
    {
        if(!origin.IsFinite()||!direction.IsFinite()||direction.LengthSquared()<1e-8f)
            throw new ArgumentException("Acoustic pulse needs a finite position and nonzero direction.");
        if(!Enum.IsDefined(tone)||emissionTick<0)throw new ArgumentOutOfRangeException(nameof(tone));
        Origin=origin;Direction=direction.Normalized();Tone=tone;EmissionTick=emissionTick;
    }

    public float Sample(Vector3 point,int tick)
    {
        if(!point.IsFinite()||tick<0)throw new ArgumentException("Acoustic sample requires finite position and nonnegative tick.");
        var offset=point-Origin;
        var distance=offset.Length();
        if(distance>Range||tick<EmissionTick||tick>=ExpiresTick)return 0;
        if(distance>1e-5f&&offset.Dot(Direction)/distance<ConeCosine)return 0;
        var localAge=(tick-EmissionTick)*MachineWorld.Tick-distance/Speed;
        if(localAge<0||localAge>=Duration)return 0;
        return 1/(1+.08f*distance*distance);
    }
}
