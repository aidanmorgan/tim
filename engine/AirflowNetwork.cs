using Godot;
using System;
using System.Linq;

namespace CuriousContraptions;

public static class FanParameters
{
    public const string Powered="powered";
    public const string Force="force";
    public const string Reach="reach";
    public const string Width="width";
}

/// <summary>A finite cylindrical jet in local space; force is a game unit, not a fluid solver.</summary>
public readonly record struct AirflowEmitter(Vector3 At,Vector3 Direction,float Reach,float Width,float Force);

public static class AirflowNetwork
{
    public static Vector3 Sample(MachineWorld world,MachinePart source,AirflowEmitter emitter,Vector3 point,MachinePart receiver)
    {
        var origin=source.Transform*emitter.At;
        var axis=(source.Basis*emitter.Direction).Normalized();
        var offset=point-origin;var along=offset.Dot(axis);
        if(along<=0||along>=emitter.Reach||(offset-axis*along).Length()>=emitter.Width)return Vector3.Zero;
        // Parallel streamlines across the jet cross-section; the receiving body cannot block itself.
        var start=point-axis*along;
        if(WorldGeometry.Trace(TraceMedium.Air,world,start,axis,along,source,receiver)<along-.0001f)return Vector3.Zero;
        return axis*emitter.Force*world.Pressure;
    }
    public static void Step(MachineWorld world,float delta)
    {
        var sources=world.Parts.Where(p=>p.Visible&&p.AirflowSource.HasValue)
            .OrderBy(p=>p.Uid,StringComparer.Ordinal).Select(p=>(Part:p,Emitter:p.AirflowSource!.Value)).ToArray();
        var targets=world.Parts.Where(p=>p.Dynamic||p.AirflowTarget.HasValue).ToArray();
        var forces=new Vector3[targets.Length];
        for(var i=0;i<targets.Length;i++)
        {
            var target=targets[i];
            if(!target.Visible)continue;
            var point=target.AirflowTarget is Vector3 at?target.Transform*at:target.Position;
            foreach(var source in sources)
                if(source.Part!=target)forces[i]+=Sample(world,source.Part,source.Emitter,point,target);
        }
        for(var i=0;i<targets.Length;i++)
        {
            var target=targets[i];
            if(!target.Visible)continue;
            if(target.Dynamic)target.Velocity+=forces[i]*delta/target.Mass;
            target.AirflowStep(world,forces[i],delta);
        }
    }
}
