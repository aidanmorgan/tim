using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SoundMeterCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId MeterCatalogue=new("sound_meter"),SpeakerCatalogue=new("speaker"),
        SupplyCatalogue=new("battery"),CounterCatalogue=new("counter");
    private partial class Probe : BatteryPart
    {
        public Action? Observed;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
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
    [InlineData(false)]
    [InlineData(true)]
    public void FailedArrivalRestoresHysteresisAndRetryDeliversOnePoweredEdge(bool powered)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var speaker=new SpeakerPart();var meter=new SoundMeterPart();
            var supply=new Probe();var counter=new CounterPart();
            Attach(world,speaker,SpeakerCatalogue,FixturePartId.First,new(-3,6,0));
            Attach(world,meter,MeterCatalogue,FixturePartId.Second,new(1,6,0));
            Attach(world,supply,SupplyCatalogue,FixturePartId.Third,new(-4,2,3));
            Attach(world,counter,CounterCatalogue,FixturePartId.Fourth,new(3,2,3));
            Assert.True(world.Connect(supply,speaker));
            if(powered)Assert.True(world.Connect(supply,meter));
            Assert.True(world.Connect(meter,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            var construction=Saved(world);
            world.Start();world.Activate(speaker);
            var failed=false;
            for(var tick=0;tick<100&&!failed;tick++)
            {
                var level=meter.Level;var above=meter.AboveThreshold;var active=meter.Active;
                var count=meter.TriggerCount;var events=world.Events.ToArray();
                supply.Observed=()=>
                {
                    if(!meter.AboveThreshold)return;
                    Assert.Equal(powered?1:0,meter.TriggerCount);
                    failed=true;throw new InvalidOperationException();
                };
                try {world.Step();}
                catch(InvalidOperationException) when(failed)
                {
                    Assert.Equal(level,meter.Level);Assert.Equal(above,meter.AboveThreshold);
                    Assert.Equal(active,meter.Active);Assert.Equal(count,meter.TriggerCount);
                    Assert.Equal(0,counter.Count);Assert.Equal(events,world.Events.ToArray());
                    supply.Observed=null;world.Step();
                    Assert.True(meter.AboveThreshold);
                    Assert.Equal(powered?1:0,meter.TriggerCount);
                    Assert.Equal(powered?1:0,counter.Count);
                }
            }
            Assert.True(failed);
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(powered?1:0,meter.TriggerCount);
            Assert.Equal(meter.TriggerCount,counter.Count);
            Assert.False(meter.AboveThreshold);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
