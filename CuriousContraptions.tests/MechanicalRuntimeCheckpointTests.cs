using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class MechanicalRuntimeCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId BellowsCatalogue=new("bellows"),
        SpringCatalogue=new("wound_spring"),ProbeCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Before,Observed;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    private readonly record struct PumpRead(BellowsPhase Phase,float Compression,float Force,double Impulse,int Strokes,bool Active);
    private static PumpRead Read(BellowsPart part)=>new(part.Phase,part.Compression,part.EmissionForce,part.EmittedImpulse,part.StrokeCount,part.Active);
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void BellowsFailedStrokeRestoresHistoryAndRetryCountsOnce(int failedSubstep)
    {
        var world=World();
        try
        {
            var pump=new BellowsPart();
            var probe=new Probe();
            Attach(world,pump,BellowsCatalogue,FixturePartId.First,new(0,6,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Second,new(-4,6,0));
            var construction=Saved(world);
            world.Start();
            var joint=(PhysicsFrameJoint)world.CurrentJoint(new(pump,BellowsPart.PlateGuide));
            var axis=joint.FrameB.Orientation.Apply(new(0,0,1));
            world.Physics.ApplyImpulse(joint.A.Id,-axis*.2,joint.A.Center);
            var initial=Read(pump);
            var physics=world.Physics.Capture();
            var events=world.Events.ToArray();
            var substep=0;
            probe.Observed=()=>
            {
                Assert.True(pump.Compression>0);
                Assert.True(pump.EmittedImpulse>0);
                Assert.Equal(1,pump.StrokeCount);
                if(++substep==failedSubstep)throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(failedSubstep,substep);
            Assert.Equal(initial,Read(pump));
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(events,world.Events.ToArray());
            probe.Observed=null;
            world.Step();
            Assert.Equal(1,pump.StrokeCount);
            Assert.True(pump.Compression>0);
            Assert.True(pump.EmittedImpulse>0);
            Assert.Contains(world.Events,p=>p.Key.Kind==MachineEventKind.Activated);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SpringPendingTriggerIsRestoredOrDiscardedAtTheTickBoundary(bool pendingBeforeTick)
    {
        var world=World();
        try
        {
            var spring=new WoundSpringPart();
            var probe=new Probe();
            Attach(world,spring,SpringCatalogue,FixturePartId.First,new(0,6,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Second,new(-4,6,0));
            var construction=Saved(world);
            world.Start();
            if(pendingBeforeTick)world.Activate(spring);
            world.Step();
            Assert.Null(spring.LastTrigger);
            if(!pendingBeforeTick)probe.Before=()=>world.Activate(spring);
            var phase=spring.Phase;
            probe.Observed=()=>
            {
                Assert.Equal(pendingBeforeTick?SpringTriggerResult.Empty:(SpringTriggerResult?)null,spring.LastTrigger);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Null(spring.LastTrigger);
            Assert.Equal(phase,spring.Phase);
            Assert.Equal(0,spring.ReleaseCount);
            probe.Before=null;probe.Observed=null;
            world.Step();world.Step();
            Assert.Equal(pendingBeforeTick?SpringTriggerResult.Empty:(SpringTriggerResult?)null,spring.LastTrigger);
            Assert.Equal(0,spring.ReleaseCount);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
