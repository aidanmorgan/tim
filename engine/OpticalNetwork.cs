using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum OpticalInteraction { Absorb, Mirror, Split }
public readonly record struct OpticalEmitter(Vector3 At,Vector3 Direction,float Range,Vector3 Power);
public readonly record struct OpticalTarget(Vector3 At,Vector3 Normal,float Radius);
public readonly record struct OpticalSurface(OpticalTarget Aperture,OpticalInteraction Interaction);
public readonly record struct OpticalSegment(Vector3 From,Vector3 To,Vector3 Power,string OriginPart);
public readonly record struct OpticalReception(MachinePart Receiver,Vector3 Power);
public sealed record OpticalTrace(IReadOnlyList<OpticalSegment> Segments,IReadOnlyList<OpticalReception> Receptions);

/// <summary>Bounded narrow-ray optics; all receivers commit together after source snapshots.
/// Passive branches divide rather than duplicate power. No electrical feedback within a trace.</summary>
public static class OpticalNetwork
{
    public const int MaximumInteractions=16;
    public const int MaximumSegments=128;
    public const float MirrorRetention=.95f;
    private const float Epsilon=.0001f;
    private readonly record struct Ray(Vector3 Origin,Vector3 Direction,float Remaining,Vector3 Power,int Depth,string OriginPart);

    public static OpticalTrace Trace(MachineWorld world,MachinePart emitter,OpticalEmitter source)
    {
        var surfaces=world.Parts.Where(p=>p.Visible&&p.OpticalSurface.HasValue)
            .OrderBy(p=>p.Uid,StringComparer.Ordinal).ToArray();
        var segments=new List<OpticalSegment>();
        var receptions=new List<OpticalReception>();
        var pending=new Queue<Ray>();
        pending.Enqueue(new(emitter.Transform*source.At,(emitter.Basis*source.Direction).Normalized(),
            source.Range,source.Power,0,emitter.Uid));
        while(pending.TryDequeue(out var ray)&&segments.Count<MaximumSegments)
        {
            if(ray.Remaining<=Epsilon||ray.Power.LengthSquared()<1e-8f)continue;
            var distance=LightNetwork.Trace(world,ray.Origin,ray.Direction,ray.Remaining,ray.Depth==0?emitter:null);
            MachinePart? hit=null;
            var normal=Vector3.Zero;
            foreach(var candidate in surfaces)
            {
                if(ray.Depth==0&&candidate==emitter)continue;
                var surface=candidate.OpticalSurface!.Value;
                var target=surface.Aperture;
                var n=(candidate.Basis*target.Normal).Normalized();
                var facing=ray.Direction.Dot(n);
                if(surface.Interaction==OpticalInteraction.Split)
                {
                    if(Mathf.Abs(facing)<Epsilon)continue; // Splitter coating works from either side.
                }
                else if(facing>=-Epsilon)continue;
                var centre=candidate.Transform*target.At;
                var along=(centre-ray.Origin).Dot(n)/facing;
                if(along<Epsilon||along>=distance)continue;
                if((ray.Origin+ray.Direction*along-centre).LengthSquared()>target.Radius*target.Radius)continue;
                distance=along;hit=candidate;normal=n;
            }
            var end=ray.Origin+ray.Direction*distance;
            segments.Add(new(ray.Origin,end,ray.Power,ray.OriginPart));
            if(hit==null)continue;
            var interaction=hit.OpticalSurface!.Value.Interaction;
            if(interaction==OpticalInteraction.Absorb){receptions.Add(new(hit,ray.Power));continue;}
            if(ray.Depth==MaximumInteractions)continue;
            var remaining=ray.Remaining-distance-Epsilon;
            var reflected=(ray.Direction-2*ray.Direction.Dot(normal)*normal).Normalized();
            void Branch(Vector3 direction,Vector3 power)=>pending.Enqueue(
                new(end+direction*Epsilon,direction,remaining,power,ray.Depth+1,hit.Uid));
            switch(interaction)
            {
                case OpticalInteraction.Mirror: Branch(reflected,ray.Power*MirrorRetention);break;
                case OpticalInteraction.Split:
                    Branch(ray.Direction,ray.Power*.5f);
                    Branch(reflected,ray.Power*.5f);
                    break;
                default: throw new InvalidOperationException("Unsupported optical interaction.");
            }
        }
        return new(segments,receptions);
    }
    public static void Solve(MachineWorld world)
    {
        var emitters=world.Parts.Where(p=>p.Visible&&p.OpticalSource.HasValue)
            .Select(p=>(Part:p,Source:p.OpticalSource!.Value))
            .OrderBy(p=>p.Part.Uid,StringComparer.Ordinal).ToArray();
        var readings=world.Parts.ToDictionary(p=>p,_=>Vector3.Zero);
        foreach(var part in world.Parts)part.ReceiveOpticalPath([]);
        foreach(var (part,source) in emitters)
        {
            var trace=Trace(world,part,source);
            part.ReceiveOpticalPath(trace.Segments);
            foreach(var reception in trace.Receptions)readings[reception.Receiver]+=reception.Power;
        }
        foreach(var (part,power) in readings)part.ReceiveOpticalPower(power);
    }
}
