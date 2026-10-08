using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class EnergyObservationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Cannon=new("cannon"),Battery=new("battery"),Ball=new("ball");
    public enum Fault { None, ForeignBody, MissingStore, UndefinedQuantity, Duplicate }
    private partial class Reservoir:CannonPart
    {
        public Fault Fault;
        public MachinePart? Foreign;
        public int Visits,FailAt;
        public override IReadOnlyList<SceneEnergyStoreObservation> EnergyStoreObservations
        {
            get
            {
                var rows=Enum.GetValues<EnergyStoreQuantity>().Select((q,i)=>new SceneEnergyStoreObservation(
                    new(i),new(this,RootBody),q)).ToArray();
                return Fault switch
                {
                    Fault.None=>rows,
                    Fault.ForeignBody=>[new(new(0),new(Foreign!,RootBody),EnergyStoreQuantity.FillFraction)],
                    Fault.MissingStore=>[new(new(0),new(this,RootBody),EnergyStoreQuantity.FillFraction)],
                    Fault.UndefinedQuantity=>[new(new(0),new(this,RootBody),(EnergyStoreQuantity)999)],
                    Fault.Duplicate=>[rows[0],rows[0]],
                    _=>throw new ArgumentOutOfRangeException()
                };
            }
        }
        public override IReadOnlyList<SceneEnergyStoreDeclaration> PhysicsEnergyStores=>
            Fault==Fault.MissingStore?[]:base.PhysicsEnergyStores;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected energy observation failure.");}
    }
    private Reservoir Add(MachineWorld world,bool powered)
    {
        var part=new Reservoir{Definition=world.Registry.Definitions[Cannon.Value]};
        var spec=new PartSpec{Id=FixtureParts.Id(FixturePartId.First),Kind=Cannon.Value,Position=[0,5,0]};
        spec.Properties[PartParameterName.Of(CannonParameter.Capacity)]=10;
        spec.Properties[PartParameterName.Of(CannonParameter.ChargePower)]=10;
        part.Configure(spec);world.AttachPart(part);
        var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-5,1,0]});
        part.Foreign=battery;
        if(powered)Assert.True(world.Connect(battery,SocketId.Supply,part,SocketId.PowerIn,ConnectionDomain.Electrical));
        return part;
    }
    private static string Saved(MachineWorld world)=>System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static double Read(MachineWorld world,MachinePart part,EnergyStoreQuantity quantity)
    {
        var declaration=part.EnergyStoreObservations.Single(d=>d.Quantity==quantity);
        using var lease=world.ReadCommittedPoses();
        var value=lease.ReadScalar(PoseSample.Current,new(world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),declaration.Slot));
        Assert.Equal(quantity==EnergyStoreQuantity.FillFraction?ScalarUnit.Dimensionless:ScalarUnit.GameEnergy,value.Unit);
        return value.Value;
    }
    public static IEnumerable<object[]> Cases()
    {foreach(var powered in new[]{false,true})for(var substep=1;substep<=MachineWorld.Substeps;substep++)yield return [powered,substep];}
    [Theory]
    [MemberData(nameof(Cases))]
    public void ChargePublishesAllQuantitiesAtomicallyAndFramesNeverReadLiveEnergy(bool powered,int substep)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world,powered);var bar=Assert.Single(part.ScalarExtents).Target;
            var baseline=bar.Transform;var saved=Saved(world);world.Start();
            Assert.Equal(0,Read(world,part,EnergyStoreQuantity.Energy));
            Assert.Equal(10,Read(world,part,EnergyStoreQuantity.Capacity));
            Assert.Equal(0,Read(world,part,EnergyStoreQuantity.ReleasedEnergy));
            part.FailAt=substep;var before=world.Physics.Capture();Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,world.Ticks);Assert.Equal(before.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            Assert.Equal(0,Read(world,part,EnergyStoreQuantity.AcceptedEnergy));
            world.PresentFrame(.1,1);Assert.Equal(baseline,bar.Transform);
            part.FailAt=0;world.Step();var fraction=Read(world,part,EnergyStoreQuantity.FillFraction);
            Assert.Equal(part.StoredEnergy/10,fraction);
            Assert.Equal(part.AcceptedEnergy,Read(world,part,EnergyStoreQuantity.AcceptedEnergy));
            Assert.Equal(powered,fraction>0);Assert.Equal(baseline,bar.Transform);
            world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(bar.Scale.X-Math.Max(.001,fraction)),0,1e-6);
            var displayed=bar.Transform;var body=world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody));
            world.Physics.ChargeEnergyStore(body,1,1);world.PresentFrame(.1,1);
            Assert.Equal(fraction,Read(world,part,EnergyStoreQuantity.FillFraction));Assert.Equal(displayed,bar.Transform);
            part.Visible=false;world.Step();world.PresentFrame(.1,1);Assert.Equal(displayed,bar.Transform);
            part.Visible=true;world.Running=false;var physical=world.Physics.Capture();world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(bar.Scale.X-Read(world,part,EnergyStoreQuantity.FillFraction)),0,1e-6);
            Assert.Equal(physical.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            var producer=new SceneScalarObservations(world.Parts,world.PhysicsAssembly,world.Physics,world.Timers,
                new Dictionary<SceneTimerKey,SimulationTimerId>(),world.Oscillators,new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            for(var i=0;i<100;i++)producer.Capture();var bytes=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);
            world.Restore();Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            world.Start();world.PresentFrame(0,1);
            var restored=(CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.ScalarExtents).Target.Transform);
        }
        finally{world.Free();}
    }
    [Fact]
    public void RealShotPublishesReleasedEnergyAndChargeDropBeforeArtworkChanges()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world,true);
            world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Ball.Value,Position=[0,5,0]});
            var saved=Saved(world);var bar=Assert.Single(part.ScalarExtents).Target;
            world.Start();for(var i=0;i<130;i++)world.Step();
            Assert.Equal(1,Read(world,part,EnergyStoreQuantity.FillFraction));
            world.PresentFrame(0,1);Assert.InRange(Math.Abs(bar.Scale.X-1),0,1e-6);
            world.Activate(part);world.Step();world.Step();Assert.Equal(CannonShotResult.Fired,part.LastShot);
            var released=Read(world,part,EnergyStoreQuantity.ReleasedEnergy);
            Assert.True(released>0);Assert.Equal(part.ReleasedEnergy,released);
            Assert.Equal(part.StoredEnergy,Read(world,part,EnergyStoreQuantity.Energy));
            Assert.InRange(Math.Abs(bar.Scale.X-1),0,1e-6);
            world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(bar.Scale.X-Math.Max(.001,Read(world,part,EnergyStoreQuantity.FillFraction))),0,1e-6);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(Fault.ForeignBody)]
    [InlineData(Fault.MissingStore)]
    [InlineData(Fault.UndefinedQuantity)]
    [InlineData(Fault.Duplicate)]
    public void InvalidReservoirDeclarationsRejectBeforeRun(Fault fault)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world,false);part.Fault=fault;var saved=Saved(world);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);Assert.Equal(saved,Saved(world));
            part.Fault=Fault.None;world.Start();Assert.Equal(10,Read(world,part,EnergyStoreQuantity.Capacity));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(ScalarExtentAxis.X)]
    [InlineData(ScalarExtentAxis.Y)]
    [InlineData(ScalarExtentAxis.Z)]
    public void ExtentsNormalizeConstructionFractionAndRestoreExactBaseline(ScalarExtentAxis axis)
    {
        var basis=Basis.FromEuler(new(.2f,.3f,.4f)).Scaled(new(2,3,4));
        var baseline=new Transform3D(basis,new(5,6,7));
        var definition=new ScalarExtentDefinition(axis,.4,.001,.25);
        Assert.Equal(baseline,definition.Compose(baseline,.25));
        var full=definition.Compose(baseline,1);
        var index=axis switch{ScalarExtentAxis.X=>0,ScalarExtentAxis.Y=>1,ScalarExtentAxis.Z=>2,_=>throw new ArgumentOutOfRangeException()};
        Assert.InRange((full.Basis[index]-baseline.Basis[index]*4).Length(),0,1e-5);
        var anchor=baseline.Origin+baseline.Basis[index]*(float)(.4*(.25-1)/.25);
        Assert.InRange((full.Origin-anchor).Length(),0,1e-5);
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,-.01,1.01})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarExtentDefinition(axis,0,.001,invalid));
        using var node=new Node3D{Transform=baseline};var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        adapter.ClaimScalarExtent(target,definition);adapter.QueueScalarExtent(target,1,true);adapter.Present(1,0,0);
        Assert.Equal(full,node.Transform);adapter.Reset();Assert.Equal(baseline,node.Transform);
    }
}
