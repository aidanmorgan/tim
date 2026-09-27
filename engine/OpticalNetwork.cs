using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct OpticalEmitter(Vector3 At,Vector3 Direction,float Range,Vector3 Power);
public readonly record struct OpticalTarget(Vector3 At,Vector3 Normal,float Radius);
public readonly record struct OpticalReflector(OpticalTarget Surface,float Reflectivity);
public readonly record struct OpticalSegment(Vector3 From,Vector3 To,Vector3 Power,string OriginPart);
public sealed record OpticalTrace(IReadOnlyList<OpticalSegment> Segments,MachinePart? Receiver,Vector3 Power);

/// <summary>Narrow-ray optics in linear RGB game-power units. All outputs commit together.
/// Emitters read the preceding electrical snapshot, so optical/electrical feedback advances one tick.</summary>
public static class OpticalNetwork
{
    public const int MaximumReflections=16;
    private const float Epsilon=.0001f;

    // Pure geometry query shared by simulation and non-activating selected-mirror aim previews.
    public static OpticalTrace Trace(MachineWorld world,MachinePart emitter,OpticalEmitter source)
    {
        var surfaces=world.Parts.Where(p=>p.Visible&&(p.OpticalTarget.HasValue||p.OpticalReflector.HasValue))
            .OrderBy(p=>p.Uid,StringComparer.Ordinal).ToArray();
        var origin=emitter.Transform*source.At;
        var direction=(emitter.Basis*source.Direction).Normalized();
        var remaining=source.Range;
        var power=source.Power;
        var originPart=emitter.Uid;
        var segments=new List<OpticalSegment>();
        for(var bounce=0;bounce<=MaximumReflections&&remaining>Epsilon;bounce++)
        {
            // Only the initial emitter is ignored; reflected rays can hit it or the mirror's mount.
            var distance=LightNetwork.Trace(world,origin,direction,remaining,bounce==0?emitter:null);
            MachinePart? hit=null;
            var normal=Vector3.Zero;
            foreach(var candidate in surfaces)
            {
                if(bounce==0&&candidate==emitter)continue;
                var target=candidate.OpticalReflector is {} reflector?reflector.Surface:candidate.OpticalTarget!.Value;
                var n=(candidate.Basis*target.Normal).Normalized();
                var facing=direction.Dot(n);
                if(facing>=-Epsilon)continue;
                var centre=candidate.Transform*target.At;
                var along=(centre-origin).Dot(n)/facing;
                if(along<Epsilon||along>=distance)continue;
                if((origin+direction*along-centre).LengthSquared()>target.Radius*target.Radius)continue;
                distance=along;hit=candidate;normal=n;
            }
            var end=origin+direction*distance;
            segments.Add(new(origin,end,power,originPart));
            if(hit==null)return new(segments,null,Vector3.Zero);
            if(hit.OpticalReflector is not {} mirror)return new(segments,hit,power);
            if(!float.IsFinite(mirror.Reflectivity)||mirror.Reflectivity<0||mirror.Reflectivity>1)
                throw new InvalidOperationException("Passive mirror reflectivity must be between zero and one.");
            if(bounce==MaximumReflections)break;
            power*=mirror.Reflectivity;
            if(power.LengthSquared()<1e-8f)break;
            direction=(direction-2*direction.Dot(normal)*normal).Normalized();
            remaining-=distance+Epsilon;
            origin=end+direction*Epsilon;
            originPart=hit.Uid;
        }
        return new(segments,null,Vector3.Zero);
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
            if(trace.Receiver!=null)readings[trace.Receiver]+=trace.Power;
        }
        foreach(var (part,power) in readings)part.ReceiveOpticalPower(power);
    }
}
