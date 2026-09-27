using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum OpticalPortId { Main, First, Second, Third, Carrier }
public enum OpticalInteraction { Absorb, Mirror, Split, Filter }
public readonly record struct OpticalEmitter(Vector3 At,Vector3 Direction,float Range,Vector3 Power);
public readonly record struct OpticalTarget(Vector3 At,Vector3 Normal,float Radius);
public readonly record struct OpticalSurface(OpticalPortId Id,OpticalTarget Aperture,OpticalInteraction Interaction,Vector3 Transmission);
public readonly record struct OpticalSegment(Vector3 From,Vector3 To,Vector3 Power,string OriginPart);
public readonly record struct OpticalReception(MachinePart Receiver,OpticalPortId Port,Vector3 Power);
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

    private readonly record struct SurfaceSample(MachinePart Part,OpticalSurface Surface,Transform3D Transform,bool Visible);
    private static SurfaceSample[] Snapshot(MachineWorld world)
    {
        var samples=new List<SurfaceSample>();
        foreach(var part in world.Parts.OrderBy(p=>p.Uid,StringComparer.Ordinal))
        {
            var ids=new HashSet<OpticalPortId>();
            foreach(var surface in part.OpticalSurfaces.OrderBy(s=>s.Id))
            {
                if(!Enum.IsDefined(surface.Id)||!ids.Add(surface.Id))
                    throw new InvalidOperationException("Optical apertures require unique supported port identities.");
                if(!Enum.IsDefined(surface.Interaction)||!surface.Aperture.At.IsFinite()||
                    !surface.Aperture.Normal.IsFinite()||surface.Aperture.Normal.LengthSquared()<Epsilon||
                    !float.IsFinite(surface.Aperture.Radius)||surface.Aperture.Radius<=0||
                    !surface.Transmission.IsFinite()||
                    surface.Transmission.X<0||surface.Transmission.Y<0||surface.Transmission.Z<0||
                    surface.Transmission.X>1||surface.Transmission.Y>1||surface.Transmission.Z>1)
                    throw new InvalidOperationException("Invalid optical aperture geometry or passive transmission.");
                samples.Add(new(part,surface,part.Transform,part.Visible));
            }
        }
        return samples.ToArray();
    }
    public static OpticalTrace Trace(MachineWorld world,MachinePart emitter,OpticalEmitter source)=>
        Trace(world,emitter,source,Snapshot(world));
    private static OpticalTrace Trace(MachineWorld world,MachinePart emitter,OpticalEmitter source,SurfaceSample[] surfaces)
    {
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
            OpticalSurface hitSurface=default;
            foreach(var candidate in surfaces)
            {
                if(!candidate.Visible||(ray.Depth==0&&candidate.Part==emitter))continue;
                var surface=candidate.Surface;
                var target=surface.Aperture;
                var n=(candidate.Transform.Basis*target.Normal).Normalized();
                var facing=ray.Direction.Dot(n);
                if(surface.Interaction is OpticalInteraction.Split or OpticalInteraction.Filter)
                {
                    if(Mathf.Abs(facing)<Epsilon)continue; // Splitter coating works from either side.
                }
                else if(facing>=-Epsilon)continue;
                var centre=candidate.Transform*target.At;
                var along=(centre-ray.Origin).Dot(n)/facing;
                if(along<Epsilon||along>=distance)continue;
                if((ray.Origin+ray.Direction*along-centre).LengthSquared()>target.Radius*target.Radius)continue;
                distance=along;hit=candidate.Part;hitSurface=surface;normal=n;
            }
            var end=ray.Origin+ray.Direction*distance;
            segments.Add(new(ray.Origin,end,ray.Power,ray.OriginPart));
            if(hit==null)continue;
            var interaction=hitSurface.Interaction;
            if(interaction==OpticalInteraction.Absorb){receptions.Add(new(hit,hitSurface.Id,ray.Power));continue;}
            if(ray.Depth==MaximumInteractions)continue;
            var remaining=ray.Remaining-distance-Epsilon;
            var reflected=(ray.Direction-2*ray.Direction.Dot(normal)*normal).Normalized();
            void Branch(Vector3 direction,Vector3 power)=>pending.Enqueue(
                new(end+direction*Epsilon,direction,remaining,power,ray.Depth+1,hit.Uid));
            switch(interaction)
            {
                case OpticalInteraction.Filter: Branch(ray.Direction,ray.Power*hitSurface.Transmission);break;
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
        var surfaces=Snapshot(world);
        var readings=world.Parts.ToDictionary(p=>p,p=>surfaces.Where(s=>s.Part==p)
            .ToDictionary(s=>s.Surface.Id,_=>Vector3.Zero));
        foreach(var part in world.Parts)part.ReceiveOpticalPath([]);
        foreach(var (part,source) in emitters)
        {
            var trace=Trace(world,part,source,surfaces);
            part.ReceiveOpticalPath(trace.Segments);
            foreach(var reception in trace.Receptions)readings[reception.Receiver][reception.Port]+=reception.Power;
        }
        foreach(var (part,power) in readings)part.ReceiveOpticalPower(power);
    }
}
