using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalPlanTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Contact=new("switch"),Load=new("powered_gate");
    private partial class CountingSwitch : SwitchPart
    {
        public int PortReads,GateReads,RouteReads;
        public bool RejectDeclarationRead;
        public override IEnumerable<ConnectionPort> ConnectionPorts
        { get {PortReads++;return base.ConnectionPorts;} }
        public override IEnumerable<ElectricalGate> ElectricalGates
        { get {GateReads++;return base.ElectricalGates;} }
        public override IEnumerable<ElectricalRoute> ElectricalRoutes
        {
            get
            {
                if(RejectDeclarationRead)throw new InvalidOperationException("Declarations must not be polled during solve.");
                RouteReads++;return base.ElectricalRoutes;
            }
        }
    }
    private partial class Supply : BatteryPart
    {
        public readonly SimulationState<float> Sample=new(1);
        public bool Available {get=>Sample.Value>=.5f;set=>Sample.Value=value?1:0;}
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[Sample];
        public int Visits,FailAt;
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
            [new(SocketId.Supply,ElectricalSourceSignal.AtLeast(Sample,.5f))];
        public override void ObservePhysics(MachineWorld world,float delta)
        { if(++Visits==FailAt)throw new InvalidOperationException("Injected electrical transaction failure."); }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static void Wire(MachineWorld world,MachinePart source,MachinePart target)=>
        Assert.True(world.Connect(source,SocketId.Supply,target,SocketId.PowerIn,ConnectionDomain.Electrical));
    private static (Supply Source,CountingSwitch Contact,MachinePart Load) Install(MachineWorld world)
    {
        var source=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
        source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[-4,5,0]});
        world.AttachPart(source);
        var contact=new CountingSwitch {Definition=world.Registry.Definitions[Contact.Value]};
        contact.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Contact.Value,Position=[0,5,0]});
        world.AttachPart(contact);
        var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Load.Value,Position=[4,5,0]});
        Wire(world,source,contact);Wire(world,contact,load);return(source,contact,load);
    }
    [Fact]
    public void CapturedContactsAreNotPolledAndRejectedSourceCannotPartiallyCommit()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var (source,contact,load)=Install(world);contact.Active=true;
            var plan=new ElectricalNetwork(world);
            var ports=contact.PortReads;var gates=contact.GateReads;var routes=contact.RouteReads;
            contact.RejectDeclarationRead=true;
            plan.Solve();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            source.Sample.Value=float.NaN;
            Assert.Throws<InvalidOperationException>(()=>plan.Solve());
            Assert.True(load.HasElectricalPower(SocketId.PowerIn));Assert.True(contact.HasElectricalPower(SocketId.PowerIn));
            source.Available=false;plan.Solve();
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));Assert.False(contact.HasElectricalPower(SocketId.PowerIn));
            source.Available=true;contact.Active=false;plan.Solve();
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));Assert.True(contact.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(ports,contact.PortReads);Assert.Equal(gates,contact.GateReads);Assert.Equal(routes,contact.RouteReads);
            for(var i=0;i<100;i++)plan.Solve();
            var before=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)plan.Solve();
            var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Console.WriteLine($"Electrical scene adapter: 3 parts, 1 contact, 1000 solves, {allocated} managed bytes.");
            Assert.Equal(0,allocated);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void RunFailureRollbackResetAndSavedReloadUseFreshCompiledPlans(int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var (source,contact,load)=Install(world);
            var initial=Saved(world);world.Start();var ports=contact.PortReads;var gates=contact.GateReads;
            world.Step();Assert.True(contact.HasElectricalPower(SocketId.PowerIn));
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(ports,contact.PortReads);Assert.Equal(gates,contact.GateReads);
            world.Activate(contact);world.Step();Assert.True(load.HasElectricalPower(SocketId.PowerIn));
            var ticks=world.Ticks;var revision=world.ControlRevision;
            source.Available=false;source.FailAt=source.Visits+substep;
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            Assert.True(load.HasElectricalPower(SocketId.PowerIn));Assert.True(contact.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(ticks,world.Ticks);Assert.Equal(revision,world.ControlRevision);
            source.FailAt=0;world.Step();Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            world.Restore();Assert.Equal(initial,Saved(world));
            Assert.All(world.Parts,part=>Assert.False(part.HasElectricalPower(SocketId.PowerIn)));
            var saved=world.Snapshot();world.LoadMachine(saved);Assert.Equal(initial,Saved(world));
            world.Start();world.Step();
            var newContact=world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            var newLoad=world.FindPart(FixtureParts.Id(FixturePartId.Third))!;
            Assert.True(newContact.HasElectricalPower(SocketId.PowerIn));Assert.False(newLoad.HasElectricalPower(SocketId.PowerIn));
            world.Activate(newContact);world.Step();Assert.True(newLoad.HasElectricalPower(SocketId.PowerIn));
            world.Restore();Assert.Equal(initial,Saved(world));
        }
        finally{world.Free();}
    }
}
