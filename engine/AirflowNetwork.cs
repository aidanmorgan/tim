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

public readonly record struct AirflowSample(Vector3 At,float Weight);

public static class AirflowNetwork
{
    private static readonly AirflowSample[] BodySample=[new(Vector3.Zero,1)];
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
        var targets=world.Parts.Select(p=>(Part:p,Samples:p.AirflowSamples))
            .Where(p=>p.Part.Dynamic||p.Samples.Count>0).ToArray();
        var forces=new Vector3[targets.Length];
        for(var i=0;i<targets.Length;i++)
        {
            var target=targets[i].Part;
            if(!target.Visible)continue;
            var samples=targets[i].Samples.Count>0?targets[i].Samples:BodySample;
            if(samples.Any(s=>!s.At.IsFinite()||!float.IsFinite(s.Weight)||s.Weight<=0)||Mathf.Abs(samples.Sum(s=>s.Weight)-1)>.0001f)
                throw new ArgumentException("Airflow sample weights must be positive, finite and sum to one.");
            foreach(var sample in samples)
            foreach(var source in sources)
                if(source.Part!=target)forces[i]+=Sample(world,source.Part,source.Emitter,target.Transform*sample.At,target)*sample.Weight;
        }
        for(var i=0;i<targets.Length;i++)
        {
            var target=targets[i].Part;
            if(!target.Visible)continue;
            if(target.Dynamic)target.Velocity+=forces[i]*delta/target.Mass;
            target.AirflowStep(world,forces[i],delta);
        }
    }
}
