using Godot;

namespace CuriousContraptions.Tests;

internal enum FixturePartId { First, Second, Third, Fourth }

/// <summary>Explicit instance-ID boundary for configured custom native scene fixtures.</summary>
internal static class FixtureParts
{
    internal static string Id(FixturePartId id)=>id switch
    {
        FixturePartId.First=>"fixture_first",FixturePartId.Second=>"fixture_second",
        FixturePartId.Third=>"fixture_third",FixturePartId.Fourth=>"fixture_fourth",
        _=>throw new ArgumentOutOfRangeException(nameof(id))
    };
    // Explicit native fixture boundary for manually stepped solver snapshots.
    internal static void PresentCaptured(MachineWorld world)
    {
        var buffer=new Bridge.CommittedPoseBuffer(new(world.ControlGeneration,new(world.Ticks),world.Ticks*MachineWorld.Tick),
            world.PhysicsAssembly.CapturePublicationReads(world.Physics),world.Counters.PublicationReads,[],[],world.Timers.PublicationReads,[]);
        using var lease=buffer.Acquire();
        world.PhysicsAssembly.PresentCommitted(lease,lease.Stamp(Bridge.PoseSample.Current).SimulationTime);
    }
    internal static void PresentUnowned(ScenePhysicsAssembly assembly)
    {
        foreach(var body in assembly.Bodies)body.RequireUnowned();
        var poses=assembly.CapturePresentationReads();
        var publication=new Bridge.BodyPublicationRead[poses.Length];
        for(var i=0;i<poses.Length;i++)
        {
            var declaration=assembly.Declarations[i];
            var geometry=declaration.Geometry.QueryGeometry;
            publication[i]=new(poses[i],new(assembly.QueryOwnerId(assembly.Key(poses[i].Id)),new(0),
                declaration.InitialParticipation,geometry.Geometry,geometry.For(TraceMedium.Light)),Bridge.OwnerActivity.Inactive,new(assembly.Bodies[i].LinearVelocity,assembly.Bodies[i].AngularVelocity));
        }
        var buffer=new Bridge.CommittedPoseBuffer(new(new(1),new(0),0),publication,[],[],[],[],[]);
        using var lease=buffer.Acquire();
        assembly.PresentCommitted(lease,lease.Stamp(Bridge.PoseSample.Current).SimulationTime);
    }
    internal static void ConfigureParameter<TParameter>(MachinePart part,TParameter parameter,float value)
        where TParameter:struct,Enum
    {
        var specification=part.Serialize();
        specification.Properties[PartParameterName.Of(parameter)]=value;
        part.Configure(specification);
    }
    internal static void Attach(MachineWorld world,MachinePart part,FixturePartId id)
    {
        var transform=part.Transform;
        part.Definition=new PartDefinition();
        part.Configure(new(){Id=Id(id)});
        part.Transform=transform;
        world.AttachPart(part);
    }
}
