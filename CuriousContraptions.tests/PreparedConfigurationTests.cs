using Godot;
using System.Text.Json;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PreparedConfigurationTests(NativeSceneFixture godot)
{
    public enum Failure { None, Throw, Null }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Motor=new("motor"),WoundSpring=new("wound_spring");
    private sealed record PreparedTorque(float Value);
    private partial class Probe : MotorPart
    {
        public Failure FailureMode;
        public float Prepared=>ReadParameterState<PreparedTorque>().Value;
        protected override PartParameterState PrepareParameterState(PartParameterValues parameters)
        {
            var state=PartParameterState.Create(new PreparedTorque(parameters.Read(MotorParameter.Torque)));
            return FailureMode switch
            {
                Failure.None=>state,Failure.Throw=>throw new InvalidOperationException("Injected preparation failure."),
                Failure.Null=>null!,_=>throw new ArgumentOutOfRangeException(nameof(FailureMode))
            };
        }
    }
    private partial class BodyProbe : WoundSpringPart
    {
        public bool RejectLookup;
        public override MachinePart InternalBody(InternalBodyRole role) =>
            RejectLookup?throw new InvalidOperationException("Injected internal body resolution failure."):base.InternalBody(role);
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=catalogue.Value,Position=[0,6,0]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(Failure.Throw)]
    [InlineData(Failure.Null)]
    public void FailedPreparationPreservesPriorAggregateAndAllConstruction(Failure failure)
    {
        var world=World();
        try
        {
            var part=new Probe();Attach(world,part,Motor);
            var saved=Saved(world);var torque=part.Prepared;
            var replacement=part.Serialize();
            replacement.Id=FixtureParts.Id(FixturePartId.Second);
            replacement.Position=[3,4,5];replacement.Locked=true;replacement.InitialVelocity=[1,2,3];
            replacement.Properties[PartParameterName.Of(MotorParameter.Torque)]=torque/2;
            part.FailureMode=failure;
            if(failure==Failure.Throw)Assert.Throws<InvalidOperationException>(()=>part.Configure(replacement));
            else Assert.Throws<ArgumentNullException>(()=>part.Configure(replacement));
            Assert.Equal(torque,part.Prepared);Assert.Equal(saved,Saved(world));
            part.FailureMode=Failure.None;
            part.Configure(replacement);
            Assert.Equal(torque/2,part.Prepared);
            var accepted=Saved(world);
            world.Start();world.Step();world.Restore();
            Assert.Equal(accepted,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void InternalBodyResolutionFailurePrecedesSharedMutation()
    {
        var world=World();
        try
        {
            var part=new BodyProbe();Attach(world,part,WoundSpring);
            var saved=Saved(world);
            var replacement=part.Serialize();
            replacement.Id=FixtureParts.Id(FixturePartId.Second);
            replacement.Locked=true;replacement.InitialVelocity=[1,2,3];
            replacement.InternalBodies[0].InitialVelocity=[3,2,1];
            part.RejectLookup=true;
            Assert.Throws<InvalidOperationException>(()=>part.Configure(replacement));
            part.RejectLookup=false;
            Assert.Equal(saved,Saved(world));
            world.Start();world.Step();world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void AggregateRejectsNullUninitializedAndMismatchedTypes()
    {
        Assert.Throws<ArgumentNullException>(()=>PartParameterState.Create<PreparedTorque>(null!));
        Assert.Throws<InvalidOperationException>(()=>PartParameterState.Empty.Read<PreparedTorque>());
        var state=PartParameterState.Create(new PreparedTorque(5));
        Assert.Equal(5,state.Read<PreparedTorque>().Value);
        Assert.Throws<InvalidOperationException>(()=>state.Read<OpticalLogicControl>());
    }
}
