using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ChimeTubeId { Right, Left, Front, Back }
public readonly record struct ChimeTube(ChimeTubeId Id,Vector3 Center,float Length,ToneBand Tone);
public readonly record struct ChimeStrike(ChimeTubeId Tube,Vector3 At,float Speed);

/// <summary>Constrained 3D sail/clapper pendulum. Only physical tube contact emits strikes.</summary>
public sealed class ChimePendulum
{
    public const float Length=1.5f;
    public const float ClapperFraction=.6f;
    public const float ClapperRadius=.12f;
    public const float TubeRadius=.065f;
    public const float Mass=.35f;
    public static readonly Vector3 Pivot=new(0,.9f,0);
    public static IReadOnlyList<ChimeTube> Tubes { get; }=Array.AsReadOnly(new[]
    {
        new ChimeTube(ChimeTubeId.Right,new(.4f,.2f,0),.9f,ToneBand.Mid),
        new ChimeTube(ChimeTubeId.Left,new(-.4f,.075f,0),1.15f,ToneBand.Low),
        new ChimeTube(ChimeTubeId.Front,new(0,.275f,.4f),.75f,ToneBand.High),
        new ChimeTube(ChimeTubeId.Back,new(0,.15f,-.4f),1f,ToneBand.Mid)
    });
    public Vector3 Offset { get; private set; }
    public Vector3 Velocity { get; private set; }
    private readonly HashSet<ChimeTubeId> _contacts=[];
    public ChimePendulum(Basis basis)=>Offset=basis*Vector3.Down*Length;
    public Vector3 LocalTip(Basis basis)=>Pivot+basis.Inverse()*Offset;
    public Vector3 LocalClapper(Basis basis)=>Pivot+basis.Inverse()*Offset*ClapperFraction;

    public IReadOnlyList<ChimeStrike> Step(Basis basis,Vector3 force,float gravity,float delta)
    {
        if(!force.IsFinite()||!float.IsFinite(gravity)||gravity<0||!float.IsFinite(delta)||delta<=0||delta>.01f)
            throw new ArgumentException("Chime step requires finite force/gravity and a positive physics-substep duration.");
        var strikes=new List<ChimeStrike>();
        Velocity+=(force/Mass+Vector3.Down*gravity)*delta;
        Velocity*=Mathf.Exp(-.9f*delta);
        Offset=(Offset+Velocity*delta).Normalized()*Length;
        Velocity-=Offset*(Velocity.Dot(Offset)/(Length*Length));
        foreach(var tube in Tubes)
        {
            var local=LocalClapper(basis);
            var nearest=new Vector3(tube.Center.X,Mathf.Clamp(local.Y,tube.Center.Y-tube.Length*.5f,tube.Center.Y+tube.Length*.5f),tube.Center.Z);
            var offset=local-nearest;var distance=offset.Length();
            var target=TubeRadius+ClapperRadius;
            if(distance>target+.025f)_contacts.Remove(tube.Id);
            if(distance>=target)continue;
            var normal=distance>1e-6f?offset/distance:-new Vector3(tube.Center.X,0,tube.Center.Z).Normalized();
            var worldNormal=basis*normal;
            var speed=-Velocity.Dot(worldNormal)*ClapperFraction;
            Offset+=worldNormal*((target-distance+.00001f)/ClapperFraction);
            Offset=Offset.Normalized()*Length;
            if(speed<=0)continue;
            Velocity+=worldNormal*(speed/ClapperFraction*1.35f);
            Velocity-=Offset*(Velocity.Dot(Offset)/(Length*Length));
            if(_contacts.Add(tube.Id)&&speed>=.08f)strikes.Add(new(tube.Id,local,speed));
        }
        return strikes;
    }
}
