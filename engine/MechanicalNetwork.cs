using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Authored belt topology binds to shared axial constraints. The shared
/// world alone integrates speeds, inertia, reactions and supplied work.</summary>
public static class MechanicalNetwork
{
    private readonly record struct Link(MachinePart From,MechanicalBinding Output,MachinePart To,MechanicalBinding Input);
    private static readonly IReadOnlyDictionary<SocketId,JointSlot> LinkSlots=
        Enum.GetValues<SocketId>().ToDictionary(port=>port,_=>new JointSlot());
    private static Link[] Resolve(IEnumerable<MachinePart> parts,IEnumerable<ConnectionSpec> links)
    {
        ArgumentNullException.ThrowIfNull(parts);ArgumentNullException.ThrowIfNull(links);
        var byId=parts.ToDictionary(p=>p.Uid,StringComparer.Ordinal);
        var bindings=new Dictionary<MachinePart,Dictionary<SocketId,MechanicalBinding>>();
        foreach(var part in byId.Values)
        {
            var declared=part.MechanicalBindings.ToDictionary(b=>b.Port);
            var sockets=part.ConnectionPorts.Where(p=>p.Domain==ConnectionDomain.Mechanical).ToArray();
            if(sockets.Length!=declared.Count||sockets.Any(p=>!declared.ContainsKey(p.Id)))
                throw new ArgumentException("Every mechanical socket must bind exactly one owned axial guide.");
            bindings.Add(part,declared);
        }
        var occupied=new HashSet<(MachinePart Owner,SocketId Port)>();
        var result=new List<Link>();
        foreach(var link in links.Where(l=>l.Type==ConnectionDomain.Mechanical))
        {
            if(!byId.TryGetValue(link.From,out var from)||!byId.TryGetValue(link.To,out var to)||
                !ConnectionRules.TryResolve(link,from.ConnectionPorts,to.ConnectionPorts,out var output,out var input))
                throw new ArgumentException("Invalid mechanical connection endpoints.");
            if(!occupied.Add((to,input.Id))) throw new ArgumentException("A mechanical input accepts one authored belt.");
            result.Add(new(from,bindings[from][output.Id],to,bindings[to][input.Id]));
        }
        return result.OrderBy(l=>l.To.Uid,StringComparer.Ordinal).ThenBy(l=>l.Input.Port).ToArray();
    }
    public static void Validate(IEnumerable<MachinePart> parts,IEnumerable<ConnectionSpec> links)=>Resolve(parts,links);
    public static bool CanConnect(IEnumerable<MachinePart> parts,IEnumerable<ConnectionSpec> links)
    {
        try {Validate(parts,links);return true;}
        catch(ArgumentException) {return false;}
    }
    public static IReadOnlyList<SceneJointDeclaration> Declare(IEnumerable<MachinePart> parts,IEnumerable<ConnectionSpec> links)=>
        Resolve(parts,links).Select(link=>(SceneJointDeclaration)new SceneTransmissionJoint(
            new(link.To,LinkSlots[link.Input.Port]),new(link.From,link.Output.Joint),new(link.To,link.Input.Joint),
            link.Input.CoordinatePerRadian/link.Output.CoordinatePerRadian,TransmissionEngagement.Engaged)).ToArray();
    public static PhysicsFrameJoint Shaft(MachineWorld world,MachinePart part,SocketId port)
    {
        var binding=Binding(part,port);
        if(world.CurrentJoint(new(part,binding.Joint)) is not PhysicsFrameJoint joint||joint.Kind==FrameJointKind.BallSocket)
            throw new InvalidOperationException("Mechanical socket requires an owned axial frame joint.");
        return joint;
    }
    private static MechanicalBinding Binding(MachinePart part,SocketId port)
    {
        if(!Enum.IsDefined(port)) throw new ArgumentOutOfRangeException(nameof(port));
        return part.MechanicalBindings.Single(b=>b.Port==port);
    }
    public static float Speed(MachineWorld world,MachinePart part,SocketId port)
    {
        var joint=Shaft(world,part,port);
        return (float)(joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed/Binding(part,port).CoordinatePerRadian);
    }
}
