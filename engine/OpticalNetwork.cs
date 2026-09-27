using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct OpticalEmitter(Vector3 At,Vector3 Direction,float Range,Vector3 Power);
public readonly record struct OpticalTarget(Vector3 At,Vector3 Normal,float Radius);

/// <summary>Narrow-ray optics in linear RGB game-power units. All outputs commit together.
/// Emitters read the preceding electrical snapshot, so optical/electrical feedback advances one tick.</summary>
public static class OpticalNetwork
{
    public static void Solve(MachineWorld world)
    {
        var emitters=world.Parts.Where(p=>p.Visible&&p.OpticalSource.HasValue)
            .Select(p=>(Part:p,Source:p.OpticalSource!.Value,Transform:p.Transform))
            .OrderBy(p=>p.Part.Uid,StringComparer.Ordinal).ToArray();
        var targets=world.Parts.Where(p=>p.Visible&&p.OpticalTarget.HasValue)
            .OrderBy(p=>p.Uid,StringComparer.Ordinal).ToArray();
        var readings=new Dictionary<MachinePart,Vector3>();
        foreach(var part in world.Parts){readings[part]=Vector3.Zero;part.ReceiveBeamLength(0);}
        foreach(var (part,source,transform) in emitters)
        {
            var origin=transform*source.At;
            var direction=(transform.Basis*source.Direction).Normalized();
            var distance=LightNetwork.Trace(world,origin,direction,source.Range,part);
            MachinePart? hit=null;
            foreach(var receiver in targets)
            {
                if(receiver==part)continue;
                var target=receiver.OpticalTarget!.Value;
                var normal=(receiver.Basis*target.Normal).Normalized();
                var facing=direction.Dot(normal);
                if(facing>=-.0001f)continue;
                var centre=receiver.Transform*target.At;
                var along=(centre-origin).Dot(normal)/facing;
                if(along<0||along>distance)continue;
                if((origin+direction*along-centre).LengthSquared()>target.Radius*target.Radius)continue;
                // Disc surface is in front of its opaque housing; nearest surface absorbs the beam.
                distance=along;hit=receiver;
            }
            part.ReceiveBeamLength(distance);
            if(hit!=null)readings[hit]+=source.Power;
        }
        foreach(var (part,power) in readings)part.ReceiveOpticalPower(power);
    }
}
