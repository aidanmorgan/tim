using Godot;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RenderCadenceTests(NativeSceneFixture godot)
{
    public enum Scenario { FreeFlight, Gate, Shutter, Airflow, Counterweight, SolarMotor }
    private enum Cadence { Headless, EveryTick, EveryFourthTick, TwicePerTick, CorruptedNodes }
    private readonly record struct CatalogueId(string Value);
    private readonly record struct PuzzleId(string Value);
    private readonly record struct ResourcePath(string Value);
    private static readonly CatalogueId Ball=new("ball"),Battery=new("battery"),Gate=new("powered_gate"),
        Shutter=new("beam_shutter"),Fan=new("fan");
    private static readonly ResourcePath Campaign=new("res://content/puzzles.json");
    private static readonly PuzzleId Counterweight=new("counterweight"),SolarMotor=new("solar_motor");
    private const int MaximumTicks=120;

    private static MachinePart Add(MachineWorld world,CatalogueId kind,FixturePartId id,Vector3 position)=>
        world.AddPart(new(){Id=FixtureParts.Id(id),Kind=kind.Value,Position=[position.X,position.Y,position.Z]});

    private static void Configure(MachineWorld world,Scenario scenario)
    {
        switch(scenario)
        {
            case Scenario.FreeFlight:
                Add(world,Ball,FixturePartId.First,new(0,3,0)).InitialVelocity=new(1,0,0);
                break;
            case Scenario.Gate:
            case Scenario.Shutter:
                world.Gravity=0;world.Pressure=0;
                var actuator=Add(world,scenario==Scenario.Gate?Gate:Shutter,FixturePartId.First,new(0,5,0));
                var battery=Add(world,Battery,FixturePartId.Second,new(-4,2,4));
                Assert.True(world.Connect(battery,actuator));
                break;
            case Scenario.Airflow:
                world.Gravity=0;
                world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Fan.Value,Position=[0,5,0],
                    Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=1}});
                Add(world,Ball,FixturePartId.Second,new(2,5,0));
                break;
            case Scenario.Counterweight:
            case Scenario.SolarMotor:
                var id=scenario==Scenario.Counterweight?Counterweight:SolarMotor;
                var puzzle=MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(Campaign.Value)).Single(p=>p.Id==id.Value);
                var data=MachineCodec.Clone(puzzle.CreateMachine());
                data.Parts.AddRange(puzzle.Solution);data.Connections=puzzle.SolutionConnections;
                world.LoadMachine(data);
                break;
            default:throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private sealed record Evidence(int Tick,bool Running,bool Won,string Signature,
        PhysicsBodySnapshot[] Bodies,PhysicsEnergyStoreState[] Energy,PhysicsMotorTotals[] Motors,
        PhysicsAngularTravelState[] Travel,MechanicalTransferTotals[] Transfers,MechanicalSourceTotals[] Sources,
        KeyValuePair<MachineEvent,int>[] Events,PhysicsImpact[] Impacts);

    private static Evidence Capture(MachineWorld world)
    {
        var state=world.Physics.Capture();
        return new(world.Ticks,world.Running,world.Won,world.StateSignature(),state.BodyStates.ToArray(),
            state.EnergyStates.ToArray(),state.MotorTotals.ToArray(),state.AngularTravelStates.ToArray(),
            state.TransferTotals.ToArray(),state.SourceTotals.ToArray(),world.Events.ToArray(),world.TickImpacts.ToArray());
    }
    private static void Equal(Evidence expected,Evidence actual)
    {
        Assert.Equal(expected.Tick,actual.Tick);Assert.Equal(expected.Running,actual.Running);Assert.Equal(expected.Won,actual.Won);
        Assert.Equal(expected.Signature,actual.Signature);Assert.Equal(expected.Bodies,actual.Bodies);
        Assert.Equal(expected.Energy,actual.Energy);Assert.Equal(expected.Motors,actual.Motors);
        Assert.Equal(expected.Travel,actual.Travel);Assert.Equal(expected.Transfers,actual.Transfers);
        Assert.Equal(expected.Sources,actual.Sources);Assert.Equal(expected.Events,actual.Events);
        Assert.Equal(expected.Impacts,actual.Impacts);
    }

    private Evidence[] Run(Scenario scenario,Cadence cadence)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            Configure(world,scenario);
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var initial=world.Physics.Capture().BodyStates.ToArray();
            var ropes=world.Ropes.Select(path=>new RopeVisual{Path=path,World=world}).ToArray();
            foreach(var rope in ropes)world.AddChild(rope);
            var samples=new List<Evidence>();
            void Frame(double fraction,double delta)
            {
                var before=Capture(world);
                world.PresentFrame(delta,fraction);
                foreach(var part in world.CollisionParts)part._Process(delta);
                foreach(var rope in ropes)rope._Process(delta);
                Equal(before,Capture(world));
            }
            for(var tick=0;tick<MaximumTicks&&world.Running;tick++)
            {
                if(cadence==Cadence.CorruptedNodes)
                    foreach(var part in world.CollisionParts)
                    {
                        part.Position+=new Vector3(7,3,-4);
                        part.RotateY(.17f);
                        part.Visible=false;
                    }
                world.Step();
                switch(cadence)
                {
                    case Cadence.Headless:
                    case Cadence.CorruptedNodes:break;
                    case Cadence.EveryTick:Frame(.6,MachineWorld.Tick);break;
                    case Cadence.EveryFourthTick:
                        if(tick%4==3)Frame(.4,4*MachineWorld.Tick);
                        break;
                    case Cadence.TwicePerTick:
                        Frame(.2,MachineWorld.Tick*.5);Frame(.8,MachineWorld.Tick*.5);
                        break;
                    default:throw new ArgumentOutOfRangeException(nameof(cadence));
                }
                samples.Add(Capture(world));
            }
            Assert.NotEmpty(samples);
            Assert.NotEqual(initial,samples[^1].Bodies);
            if(scenario==Scenario.FreeFlight)Assert.Contains(samples,s=>s.Impacts.Length>0);
            if(scenario==Scenario.Airflow)Assert.Contains(samples[^1].Energy,s=>s.ReleasedEnergy>0);
            if(scenario is Scenario.Gate or Scenario.Shutter or Scenario.SolarMotor)
                Assert.Contains(samples[^1].Motors,m=>m.SuppliedWork>0);
            if(scenario==Scenario.SolarMotor)
            {
                Assert.True(world.Parts.OfType<FlashlightPart>().Single().Active);
                Assert.True(world.Parts.OfType<SolarPanelPart>().Single().Irradiance>=SolarPanelPart.Threshold);
            }
            // Explicit diagnostic output boundary retains enum-typed selectors until serialization.
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                Scenario=scenario,Cadence=cadence,Ticks=samples.Count,
                Impacts=samples.Sum(s=>s.Impacts.Length),Won=world.Won,
                SuppliedWork=samples[^1].Motors.Sum(m=>m.SuppliedWork),
                ReleasedEnergy=samples[^1].Energy.Sum(e=>e.ReleasedEnergy)
            }));
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Restore();
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.All(world.Parts,p=>Assert.True(p.Visible));
            return samples.ToArray();
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Scenario.FreeFlight)]
    [InlineData(Scenario.Gate)]
    [InlineData(Scenario.Shutter)]
    [InlineData(Scenario.Airflow)]
    [InlineData(Scenario.Counterweight)]
    [InlineData(Scenario.SolarMotor)]
    public void SimulationAndResetAreExactlyIndependentOfRenderCadence(Scenario scenario)
    {
        var expected=Run(scenario,Cadence.Headless);
        foreach(var cadence in Enum.GetValues<Cadence>().Where(c=>c!=Cadence.Headless))
        {
            var actual=Run(scenario,cadence);
            Assert.Equal(expected.Length,actual.Length);
            for(var i=0;i<expected.Length;i++)Equal(expected[i],actual[i]);
        }
    }
}
