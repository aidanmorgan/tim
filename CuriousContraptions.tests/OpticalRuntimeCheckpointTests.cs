using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalRuntimeCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId SolarCatalogue=new("solar_panel"),
        ReceiverCatalogue=new("light_receiver"),CombinerCatalogue=new("beam_combiner"),
        ProbeCatalogue=new("battery");
    private partial class FailureProbe : BatteryPart
    {
        public Action? Observed;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    public static IEnumerable<object[]> Cases=>
        from colour in Enum.GetValues<OpticalColour>()
        from initiallyLit in new[]{false,true}
        select new object[]{colour,initiallyLit};
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [MemberData(nameof(Cases))]
    public void FailedTickRestoresEveryReadingAndRetryCommits(OpticalColour colour,bool initiallyLit)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var solar=new SolarPanelPart();
            var receiver=new LightReceiverPart {Colour=colour};
            var combiner=new BeamCombinerPart();
            var probe=new FailureProbe();
            Attach(world,solar,SolarCatalogue,FixturePartId.First,new(-8,6,0));
            Attach(world,receiver,ReceiverCatalogue,FixturePartId.Second,new(-4,6,0));
            Attach(world,combiner,CombinerCatalogue,FixturePartId.Third,new(0,6,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Fourth,new(4,6,0));
            var construction=Saved(world);
            world.Start();
            var beam=OpticalColours.Mask(colour);
            void Supply(bool lit)
            {
                solar.ReceiveLight(lit?SolarPanelPart.Threshold:0);
                receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>
                    {[OpticalPortId.Main]=lit?beam:Vector3.Zero});
                combiner.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>
                    {[OpticalPortId.First]=lit?Vector3.Right:Vector3.Zero,
                     [OpticalPortId.Second]=lit?Vector3.Up:Vector3.Zero,
                     [OpticalPortId.Third]=lit?Vector3.Back:Vector3.Zero});
                combiner.ReceiveOpticalOutputPower(lit?Vector3.One*BeamCombinerPart.Retention:Vector3.Zero);
            }
            void Check(bool lit)
            {
                Assert.Equal(lit?SolarPanelPart.Threshold:0,solar.Irradiance);
                Assert.Equal(lit,new ElectricalSourceBinding(solar,Assert.Single(solar.ElectricalSources).Signal).Read());
                Assert.Equal(lit?beam:Vector3.Zero,receiver.ReceivedPower);
                Assert.Equal(lit,receiver.Matches);
                Assert.Equal(lit,receiver.Active);
                Assert.Equal(lit?Vector3.One:Vector3.Zero,combiner.InputPower);
                Assert.Equal(lit?Vector3.One*BeamCombinerPart.Retention:Vector3.Zero,combiner.OutputPower);
                Assert.Equal(lit,combiner.Active);
            }
            Supply(initiallyLit);
            Check(initiallyLit);
            var ticks=world.Ticks;
            probe.Observed=()=>
            {
                // Actual network evaluation has no emitters and must clear every reading.
                Check(false);
                Supply(!initiallyLit);
                Check(!initiallyLit);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(ticks,world.Ticks);
            Check(initiallyLit);
            probe.Observed=null;
            world.Step();
            Assert.Equal(ticks+1,world.Ticks);
            Check(false);
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
