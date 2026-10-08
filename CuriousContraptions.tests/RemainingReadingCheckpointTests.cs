using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RemainingReadingCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId FanCatalogue=new("fan"),MillCatalogue=new("windmill"),
        PusherCatalogue=new("linear_pusher"),SupplyCatalogue=new("battery"),PulleyCatalogue=new("pulley");
    private partial class Probe : BatteryPart
    {
        public Action? Before,Observed;
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
            [new(SocketId.Supply,ElectricalSourceSignal.When(ElectricalContactSignal.OwnerActive))];
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=1};
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
    [Fact]
    public void FailedAirflowObservationRestoresForceAndRetryRecomputesIt()
    {
        var world=World();
        try
        {
            var fan=new FanPart();var mill=new WindmillPart();var probe=new Probe();
            Attach(world,fan,FanCatalogue,FixturePartId.First,new(-4,6,0));
            Attach(world,mill,MillCatalogue,FixturePartId.Second,new(-1,6,0));
            Attach(world,probe,SupplyCatalogue,FixturePartId.Third,new(-5,2,3));
            fan.Active=false;
            var construction=Saved(world);world.Start();world.Step();
            Assert.Equal(0,mill.AxialForce);
            var physics=world.Physics.Capture();
            probe.Before=()=>fan.Active=true;
            probe.Observed=()=>{Assert.True(mill.AxialForce>0);throw new InvalidOperationException();};
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,mill.AxialForce);Assert.False(fan.Active);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            probe.Observed=null;world.Step();
            Assert.True(mill.AxialForce>0);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
    [Fact]
    public void PusherPhaseAndPulleyAccumulatorRestoreAfterFailedTick()
    {
        var world=World();
        try
        {
            var pusher=new LinearPusherPart();var pulley=new PulleyPart();var supply=new Probe();
            Attach(world,pusher,PusherCatalogue,FixturePartId.First,new(0,6,0));
            Attach(world,pulley,PulleyCatalogue,FixturePartId.Second,new(-4,6,0));
            Attach(world,supply,SupplyCatalogue,FixturePartId.Third,new(-5,2,3));
            Assert.True(world.Connect(supply,SocketId.Supply,pusher,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(supply,SocketId.Supply,pusher,SocketId.ExtendIn,ConnectionDomain.Electrical));
            var construction=Saved(world);world.Start();world.Step();
            Assert.Equal(PusherPhase.Unpowered,pusher.Phase);
            pulley.AdvanceRope(.1);var angle=pulley.WheelAngle;
            var physics=world.Physics.Capture();
            supply.Before=()=>{supply.Active=true;pulley.AdvanceRope(.2);};
            supply.Observed=()=>
            {
                Assert.Equal(PusherPhase.Extending,pusher.Phase);
                Assert.NotEqual(angle,pulley.WheelAngle);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(PusherPhase.Unpowered,pusher.Phase);Assert.Equal(angle,pulley.WheelAngle);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            supply.Observed=null;world.Step();
            Assert.Equal(PusherPhase.Extending,pusher.Phase);Assert.NotEqual(angle,pulley.WheelAngle);
            var moving=world.Physics.Capture();
            world.Physics.Restore(physics);
            Assert.Equal(PusherPhase.Blocked,pusher.Phase); // inputs still extend, restored velocity is zero
            world.Physics.Restore(moving);
            Assert.Equal(PusherPhase.Extending,pusher.Phase); // no observer callback needed
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
