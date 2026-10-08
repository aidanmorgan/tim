using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalControlCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId LaserCatalogue=new("laser"),ProbeCatalogue=new("battery");
    private static CatalogueId GateCatalogue(LogicGateKind kind)=>kind switch
    {
        LogicGateKind.And=>new("optical_and"),LogicGateKind.Or=>new("optical_or"),
        LogicGateKind.Xor=>new("optical_xor"),LogicGateKind.Nor=>new("optical_nor"),
        LogicGateKind.Nand=>new("optical_nand"),_=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private partial class Probe : BatteryPart
    {
        public Action? Before,Observed;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
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
    public static IEnumerable<object[]> TruthRows()=>LogicGateTests.TruthRows();

    [Fact]
    public void GateCatalogueBoundaryIsCanonicalAndRejectsUndefined()
    {
        Assert.Equal(new[]{"optical_and","optical_or","optical_xor","optical_nor","optical_nand"},
            Enum.GetValues<LogicGateKind>().Select(kind=>GateCatalogue(kind).Value));
        Assert.Throws<ArgumentOutOfRangeException>(()=>GateCatalogue((LogicGateKind)999));
    }

    [Theory]
    [MemberData(nameof(TruthRows))]
    public void FailedTickRestoresPendingInputsCurrentDecisionAndOutput(
        LogicGateKind kind,bool first,bool second,bool expected)
    {
        var world=World();
        try
        {
            var gate=new OpticalLogicPart {Operation=kind};
            var probe=new Probe();
            Attach(world,gate,GateCatalogue(kind),FixturePartId.First,new(0,6,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Second,new(4,6,0));
            var construction=Saved(world);
            world.Start();
            var initial=gate.IsOpen;
            gate.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>
                {[OpticalPortId.First]=first?Vector3.One:Vector3.Zero,
                 [OpticalPortId.Second]=second?Vector3.One:Vector3.Zero});
            gate.ReceiveOpticalOutputPower(Vector3.One*.4f);
            probe.Observed=()=>
            {
                Assert.Equal(expected,gate.IsOpen);
                Assert.False(gate.First);Assert.False(gate.Second);
                Assert.Equal(Vector3.Zero,gate.OutputPower);
                Assert.False(gate.Active);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(first,gate.First);Assert.Equal(second,gate.Second);
            Assert.Equal(initial,gate.IsOpen);
            Assert.Equal(Vector3.One*.4f,gate.OutputPower);Assert.True(gate.Active);
            probe.Observed=null;
            world.Step();
            Assert.Equal(expected,gate.IsOpen);
            Assert.False(gate.First);Assert.False(gate.Second);
            Assert.Equal(Vector3.Zero,gate.OutputPower);Assert.False(gate.Active);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaserOwnsPathsAndRestoresActivationAfterFailure(bool initiallyEnabled)
    {
        var world=World();
        try
        {
            var laser=new LaserPart();
            var probe=new Probe();
            Attach(world,laser,LaserCatalogue,FixturePartId.First,new(0,6,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Second,new(-4,6,0));
            Assert.True(world.Connect(probe,laser));
            var construction=Saved(world);
            world.Start();
            if(initiallyEnabled)world.Activate(laser);
            var segment=new OpticalSegment(Vector3.Zero,Vector3.Right,Vector3.One,laser.OpticalIdentity);
            var incoming=new List<OpticalSegment>{segment};
            laser.ReceiveOpticalPath(incoming);
            incoming.Clear();
            Assert.Equal(segment,Assert.Single(laser.BeamPath));
            Assert.Throws<NotSupportedException>(()=>((IList<OpticalSegment>)laser.BeamPath).Clear());
            var path=laser.BeamPath;
            probe.Before=()=>world.Activate(laser);
            probe.Observed=()=>
            {
                Assert.True(laser.Enabled);
                Assert.NotSame(path,laser.BeamPath);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(initiallyEnabled,laser.Enabled);
            Assert.Same(path,laser.BeamPath);
            Assert.Equal(segment,Assert.Single(laser.BeamPath));Assert.True(laser.Active);
            probe.Observed=null;
            world.Step();world.Step();
            Assert.True(laser.Enabled);Assert.NotEmpty(laser.BeamPath);Assert.True(laser.Active);
            Assert.Equal(segment,Assert.Single(path));
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
