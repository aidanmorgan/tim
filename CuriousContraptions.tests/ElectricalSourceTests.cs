using System.Text.Json;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalSourceTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Solar=new("solar_panel"),Load=new("powered_gate");
    private partial class Source : BatteryPart
    {
        public int Reads;
        public bool RejectRead,Fail;
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources
        {
            get
            {
                if(RejectRead)throw new InvalidOperationException("Source declarations must not be polled.");
                Reads++;return base.ElectricalSources;
            }
        }
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(Fail)throw new InvalidOperationException("Injected source transaction failure.");}
    }
    public enum InvalidOutput { Missing, Undefined, Duplicate }
    private partial class InvalidSource : BatteryPart
    {
        public InvalidOutput Fault;
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>Fault switch
        {
            InvalidOutput.Missing=>[new(SocketId.PowerIn,ElectricalSourceSignal.BinaryInput(new(this,EnableInput)))],
            InvalidOutput.Undefined=>[new((SocketId)999,ElectricalSourceSignal.BinaryInput(new(this,EnableInput)))],
            InvalidOutput.Duplicate=>[..base.ElectricalSources,..base.ElectricalSources],
            _=>throw new ArgumentOutOfRangeException(nameof(Fault))
        };
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static MachinePart AttachLoad(MachineWorld world,MachinePart source)
    {
        var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Load.Value,Position=[4,5,0]});
        Assert.True(world.Connect(source,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));return load;
    }

    [Theory]
    [InlineData(0f,false)]
    [InlineData(.5f,false)]
    [InlineData(.50000006f,true)]
    [InlineData(1f,true)]
    public void BatteryInitialBoundaryCommandsRollbackAndResetUseCapturedDeclarations(float enabled,bool expected)
    {
        var world=World();
        try
        {
            var source=new Source {Definition=world.Registry.Definitions[Battery.Value]};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[-4,5,0],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=enabled}});
            world.AttachPart(source);var load=AttachLoad(world,source);var saved=Saved(world);
            var construction=new ElectricalNetwork(world);construction.Solve();
            Assert.Equal(expected,load.HasElectricalPower(SocketId.PowerIn));
            world.Start();var reads=source.Reads;source.RejectRead=true;
            world.Step();Assert.Equal(expected,load.HasElectricalPower(SocketId.PowerIn));
            var requested=expected?BinaryInputState.Disabled:BinaryInputState.Enabled;
            var command=world.QueueBinaryInput(new(source,BatteryPart.EnableInput),requested);
            source.Fail=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(expected,load.HasElectricalPower(SocketId.PowerIn));Assert.False(world.TryPeekControlResult(out _));
            source.Fail=false;world.Step();Assert.Equal(!expected,load.HasElectricalPower(SocketId.PowerIn));
            Assert.True(world.TryPeekControlResult(out var result));Assert.Equal(command,result.Sequence);
            Assert.Equal(CommandOutcome.Applied,result.Outcome);world.AcknowledgeControlResult(result.Id);
            Assert.Equal(reads,source.Reads);
            world.Restore();Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            world.Start();world.Step();
            Assert.Equal(expected,world.FindPart(FixtureParts.Id(FixturePartId.Second))!.HasElectricalPower(SocketId.PowerIn));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    [Fact]
    public void SolarThresholdAdjacentValuesAndRejectedReadingPreserveSettledInputs()
    {
        var world=World();
        try
        {
            var solar=(SolarPanelPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Solar.Value,Position=[-4,5,0]});
            var load=AttachLoad(world,solar);var plan=new ElectricalNetwork(world);
            solar.ReceiveLight(MathF.BitDecrement(SolarPanelPart.Threshold));plan.Solve();Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            solar.ReceiveLight(SolarPanelPart.Threshold);plan.Solve();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            foreach(var invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                solar.ReceiveLight(invalid);Assert.Throws<InvalidOperationException>(plan.Solve);
                Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            }
            solar.ReceiveLight(MathF.BitIncrement(SolarPanelPart.Threshold));plan.Solve();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            solar.ReceiveLight(0);plan.Solve();Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            solar.ReceiveLight(SolarPanelPart.Threshold);
            var transaction=new SimulationTransaction(solar.RuntimeState);transaction.Begin();
            solar.ReceiveLight(0);transaction.Rollback();plan.Solve();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(InvalidOutput.Missing)]
    [InlineData(InvalidOutput.Undefined)]
    [InlineData(InvalidOutput.Duplicate)]
    public void UndeclaredAndDuplicateSourceSocketsReject(InvalidOutput fault)
    {
        var world=World();
        try
        {
            var source=new InvalidSource {Definition=world.Registry.Definitions[Battery.Value],Fault=fault};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value});world.AttachPart(source);
            if(fault==InvalidOutput.Duplicate)Assert.Throws<ArgumentException>(()=>new ElectricalNetwork(world));
            else Assert.Throws<InvalidOperationException>(()=>new ElectricalNetwork(world));
        }
        finally{world.Free();}
    }

    [Fact]
    public void InvalidAndForeignSignalsRejectAndRuntimeBindingRetriesAtomically()
    {
        var world=World();
        try
        {
            var source=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[-4,5,0]});
            var load=AttachLoad(world,source);world.Start();
            Assert.Throws<ArgumentException>(()=>ElectricalSourceSignal.BinaryInput(default));
            Assert.Throws<ArgumentNullException>(()=>ElectricalSourceSignal.AtLeast(null!,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ElectricalSourceSignal.AtLeast(new(0),float.NaN));
            Assert.Throws<ArgumentException>(()=>new ElectricalSourceBinding(source,default));
            Assert.Throws<ArgumentException>(()=>new ElectricalSourceBinding(source,ElectricalSourceSignal.AtLeast(new(0),1)));
            Assert.Throws<ArgumentException>(()=>new ElectricalSourceBinding(source,ElectricalSourceSignal.BinaryInput(new(load,BatteryPart.EnableInput))));
            var cells=new Dictionary<SceneBinaryInputKey,SimulationState<BinaryInputState>>();
            var runtime=new ElectricalRuntime(world.PhysicsAssembly,world.Physics,
                world.Counters,new Dictionary<SceneCounterKey,SimulationCounterId>(),
                world.Latches,new Dictionary<SceneLatchKey,SimulationLatchId>(),
                world.Timers,new Dictionary<SceneTimerKey,SimulationTimerId>(),cells);
            var plan=new ElectricalNetwork(world);
            Assert.Throws<ArgumentException>(()=>plan.BindRuntime(runtime));
            cells.Add(new(source,BatteryPart.EnableInput),new((BinaryInputState)999));
            Assert.Throws<InvalidOperationException>(()=>plan.BindRuntime(runtime));
            var cell=cells[new(source,BatteryPart.EnableInput)];cell.Value=BinaryInputState.Disabled;
            plan.BindRuntime(runtime);plan.Solve();Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            cell.Value=BinaryInputState.Enabled;plan.Solve();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            cell.Value=(BinaryInputState)999;Assert.Throws<InvalidOperationException>(plan.Solve);
            Assert.True(load.HasElectricalPower(SocketId.PowerIn));
        }
        finally{world.Free();}
    }
}
