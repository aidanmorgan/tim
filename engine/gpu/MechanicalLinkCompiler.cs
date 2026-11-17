using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

internal static class MechanicalLinkCompiler
{
    public static IReadOnlyList<CompiledMechanicalLink> Compile(WorkshopConstruction construction, IPhysicsSceneDeclaration physics)
    {
        var links = new List<CompiledMechanicalLink>();
        foreach (var link in construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Mechanical))
        {
            var source = Resolve(link.Source, link.Output);
            var target = Resolve(link.Target, link.Input);
            if (source is { } a && target is { } b)
                links.Add(new(new(MechanicalLinkGeometry.TargetBase + (ulong)links.Count), a, b));
        }
        return links;

        CompiledMechanicalRim? Resolve(GpuBodyId owner, PortId id)
        {
            var part = (WorkshopGenericInstance)construction.Instances.Single(instance => instance.Id == owner);
            var port = DeclaredPorts.Current.Of(part.PartId).ToArray().Single(port => port.Id == id);
            if (port.MechanicalRim is not { } rim) return null;
            var joint = construction.Instances.Identities[owner].Hinges[port.Coordinate!.Value.Index].Joint;
            var support = physics.Revolutes.ToArray().Single(row => row.Id == joint).BodyA;
            return new(construction.Instances.Identities.Body(owner, rim.Support), joint, support,
                rim.Centre, rim.Frame, rim.Radius, rim.AnglePerCoordinate);
        }
    }
}
