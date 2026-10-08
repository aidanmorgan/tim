using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticEmitterCheckpointTests(NativeSceneFixture godot)
{
    public enum EmitterKind { Speaker, Bell, Chimes }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId SpeakerCatalogue=new("speaker"),BellCatalogue=new("bell"),
        ChimesCatalogue=new("wind_chimes"),SupplyCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Before,Observed;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
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
    [InlineData(EmitterKind.Speaker)]
    [InlineData(EmitterKind.Bell)]
    [InlineData(EmitterKind.Chimes)]
    public void FailedEmissionRestoresQueueCooldownAndRetry(EmitterKind kind)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var probe=new Probe();
            MachinePart emitter;Func<int> count,last;
            Action strike;
            switch(kind)
            {
                case EmitterKind.Speaker:
                    var speaker=new SpeakerPart();
                    Attach(world,speaker,SpeakerCatalogue,FixturePartId.First,new(0,6,0));
                    emitter=speaker;count=()=>speaker.PulseCount;last=()=>speaker.LastPulseTick;
                    strike=()=>world.Activate(speaker);break;
                case EmitterKind.Bell:
                    var bell=new BellPart();
                    Attach(world,bell,BellCatalogue,FixturePartId.First,new(0,6,0));
                    emitter=bell;count=()=>bell.PulseCount;last=()=>bell.LastPulseTick;
                    strike=()=>bell.ObserveContact(new(new(bell,MachinePart.RootBody),new(probe,MachinePart.RootBody),
                        default,default,2,1),world);break;
                case EmitterKind.Chimes:
                    var chimes=new WindChimesPart();
                    Attach(world,chimes,ChimesCatalogue,FixturePartId.First,new(0,6,0));
                    emitter=chimes;count=()=>chimes.PulseCount;last=()=>chimes.LastPulseTick;
                    strike=()=>chimes.ObserveContact(new(new(chimes,WindChimesPart.TubeBody(ChimeTubeId.Right)),
                        new(probe,MachinePart.RootBody),default,default,2,1),world);break;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
            Attach(world,probe,SupplyCatalogue,FixturePartId.Second,new(-5,6,0));
            if(kind==EmitterKind.Speaker)Assert.True(world.Connect(probe,emitter));
            var audio=emitter.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            var construction=Saved(world);world.Start();
            if(kind==EmitterKind.Speaker){strike();world.Step();}
            else probe.Before=strike;
            var tick=last();
            probe.Observed=()=>
            {
                Assert.Equal(1,count());Assert.Single(emitter.AcousticPulses);
                Assert.False(audio.Playing);
                Assert.True(emitter.Active);throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,count());Assert.Equal(tick,last());
            Assert.Empty(emitter.AcousticPulses);Assert.False(emitter.Active);Assert.False(audio.Playing);
            probe.Observed=null;world.Step();
            Assert.Equal(1,count());Assert.Single(emitter.AcousticPulses);
            Assert.True(audio.Playing);audio.Stop();
            var emitted=emitter.AcousticPulses.ToArray();
            probe.Before=null;
            // Failure after normal queue maintenance must preserve a retained immutable pulse.
            probe.Observed=()=>throw new InvalidOperationException();
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(emitted,emitter.AcousticPulses.ToArray());
            Assert.Equal(1,count());
            Assert.False(audio.Playing);
            probe.Observed=null;world.Step();
            Assert.Equal(1,count());Assert.False(audio.Playing);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
