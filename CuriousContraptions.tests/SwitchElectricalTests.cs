using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class SwitchElectricalTests
{
    private static WorkshopConnection Power(ulong source,ulong target)=>new(new(source),WorkshopSocket.Supply,new(target),WorkshopSocket.PowerIn,WorkshopConnectionDomain.Electrical);
    private static WorkshopConnection Signal(ulong source,ulong target)=>new(new(source),WorkshopSocket.ActivationOut,new(target),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Machine(float x=0f,float threshold=.8f)
    {
        return new(new(2),WorkshopCadenceSettings.Default(),new(
            WorkshopInput.Generic(PartId.Basketball,new(1),x,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch,new(2),0,1,0,0,0,0,1,new([new(new ParameterId("trigger_threshold"),threshold)])),
            WorkshopInput.Battery(new(3),-4,3,0,0,0,0,1,ElectricalSourceSettings.Default),
            WorkshopInput.Generic(PartId.PoweredGate,new(4),9,3,0,0,0,0,1),
            WorkshopInput.Lamp(new(5),4,2,0,0,0,0,1)),
            Connections:new(Power(3,2),Power(2,4),Signal(2,5)));
    }
    private static ActivationLatch Activation(WorkshopRead read,ulong owner)=>
        Enumerable.Range(0,read.Activations.Count).Select(i=>read.Activations[i]).Single(row=>row.Owner.Value==owner);
    private static bool Powered(WorkshopRead read)=>read.Presentation.Actuators.ToArray().Single(row=>row.Owner.Value==4).SupplyClosed;
    private static float BladeY(WorkshopRead read)
    {
        var body=Enumerable.Range(0,read.Bodies.Count).Select(i=>read.Bodies[i].Body).Single(row=>row.Id==WorkshopPhysicsCompiler.GateBlade(new(4)));
        return (body.Cell.Y+body.Local.Y)/16f;
    }

    [Fact]
    public async Task PhysicalImpactDrivesBothDomainsAndExactSaveReset()
    {
        var construction=Machine();var saved=new WorkshopSavedConstruction(construction,new(6));
        var bytes=WorkshopSaveCodec.Encode(saved);var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved,loaded);Assert.Equal(bytes,WorkshopSaveCodec.Encode(loaded));
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,180);
        var hit=reads.First(read=>Activation(read,2).Phase==ActivationPhase.Latched);
        Assert.False(Powered(hit)); // contact observed after this physical integration
        Assert.All(reads.Where(read=>read.Tick.Value>hit.Tick.Value),read=>Assert.True(Powered(read)));
        Assert.All(reads.Where(read=>read.Tick.Value>=hit.Tick.Value),read=>Assert.Equal(ActivationPhase.Latched,Activation(read,5).Phase));
        Assert.True(BladeY(reads[^1])>BladeY(built)+.2f);
        Assert.Single(reads.Where(read=>Activation(read,2).Phase==ActivationPhase.Latched).Select(read=>Activation(read,2).Occurrence).Distinct());
        NoFixedCapacityTests.AssertSameState(built,reset,"electrical contact Reset");
        var (_,replay,_)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,180);
        for(var i=0;i<reads.Count;i++) NoFixedCapacityTests.AssertSameState(reads[i],replay[i],"saved electrical replay");
    }

    public enum Control { Miss, BelowThreshold, MissingSourceWire, MissingConsumerWire, DisabledSource }
    [Theory]
    [InlineData(Control.Miss)]
    [InlineData(Control.BelowThreshold)]
    [InlineData(Control.MissingSourceWire)]
    [InlineData(Control.MissingConsumerWire)]
    [InlineData(Control.DisabledSource)]
    public async Task UnqualifiedOrUnsuppliedContactCannotMoveConsumer(Control control)
    {
        var construction=Machine(control==Control.Miss?2f:0f,control==Control.BelowThreshold?64f:.8f);
        if(control==Control.MissingSourceWire) construction=construction with {Connections=construction.Connections.Without(Power(3,2))};
        if(control==Control.MissingConsumerWire) construction=construction with {Connections=construction.Connections.Without(Power(2,4))};
        if(control==Control.DisabledSource) construction=construction with {Instances=new(construction.Instances.Select(instance=>instance is WorkshopBattery battery
            ? (IWorkshopInstance)(battery with {Settings=battery.Settings with {Enabled=ElectricalEnable.Disabled}}):instance).ToArray())};
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,180);
        Assert.All(reads,read=>Assert.False(Powered(read)));
        Assert.True(MathF.Abs(BladeY(reads[^1])-BladeY(built))<.01f);
        if(control is Control.MissingSourceWire or Control.MissingConsumerWire or Control.DisabledSource) Assert.Contains(reads,read=>Activation(read,2).Phase==ActivationPhase.Latched && Activation(read,5).Phase==ActivationPhase.Latched);
        else Assert.All(reads,read=>Assert.Equal(ActivationPhase.Clear,Activation(read,2).Phase));
        NoFixedCapacityTests.AssertSameState(built,reset,"unpowered contact Reset");
    }

    [Fact]
    public async Task SupplyLossAndRestorationDoNotEraseOrReplayImpactMemory()
    {
        var phases=await WorkshopPhysicsEngineTests.RunPhases(Machine(),([],100),([new(new(3),ElectricalEnable.Disabled)],30),
            ([new(new(3),ElectricalEnable.Enabled)],30));
        Assert.Contains(phases[0],Powered);Assert.All(phases[1],read=>Assert.False(Powered(read)));
        Assert.All(phases[2],read=>Assert.True(Powered(read)));
        var first=Activation(phases[0][^1],2);
        Assert.All(phases[1].Concat(phases[2]),read=>Assert.Equal(first,Activation(read,2)));
        Assert.All(phases[1].Concat(phases[2]),read=>Assert.Equal(ActivationPhase.Latched,Activation(read,5).Phase));
    }

    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(true,true,false)]
    [InlineData(true,false,true)]
    public async Task TwoContactChainsAndCyclesAreSourceSeededAndOrderIndependent(bool cycle,bool noSource,bool reverse)
    {
        var construction=Machine();
        var parts=construction.Instances.Concat(new IWorkshopInstance[]{
            WorkshopInput.Generic(PartId.ImpactSwitch,new(6),-8,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Basketball,new(7),-8,3,0,0,0,0,1)}).ToArray();
        var links=new List<WorkshopConnection>{Power(2,6),Power(6,4),Signal(2,5)};
        if(!noSource) links.Add(Power(3,2));
        if(cycle) links.Add(Power(6,2));
        if(reverse)
        {
            GpuBodyId Reidentify(GpuBodyId id)=>id.Value switch {2=>new(6),6=>new(2),_=>id};
            parts=parts.Select(part=>part is WorkshopGenericInstance generic && generic.PartId==PartId.ImpactSwitch
                ? (IWorkshopInstance)(generic with {Id=Reidentify(generic.Id)}):part).ToArray();
            links=links.Select(link=>link with {Source=Reidentify(link.Source),Target=Reidentify(link.Target)}).ToList();
            Array.Reverse(parts);links.Reverse();
        }
        construction=construction with {Instances=new(parts),Connections=new(links.ToArray())};
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,180);
        Assert.Contains(reads,read=>Activation(read,2).Phase==ActivationPhase.Latched && Activation(read,6).Phase==ActivationPhase.Latched);
        if(noSource) {Assert.All(reads,read=>Assert.False(Powered(read)));Assert.True(MathF.Abs(BladeY(reads[^1])-BladeY(built))<.01f);}
        else {Assert.True(Powered(reads[^1]));Assert.True(BladeY(reads[^1])>BladeY(built)+.2f);}
        Assert.All(reads.Where(Powered),read=>Assert.True(Activation(read,2).Phase==ActivationPhase.Latched && Activation(read,6).Phase==ActivationPhase.Latched));
        NoFixedCapacityTests.AssertSameState(built,reset,"cyclic contact Reset");
    }

    [Fact]
    public async Task SeededCycleLosesAndRestoresPowerWithoutChangingEitherLatch()
    {
        var construction=Machine();
        construction=construction with {Instances=new(construction.Instances.Concat(new IWorkshopInstance[]{
            WorkshopInput.Generic(PartId.ImpactSwitch,new(6),-8,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Basketball,new(7),-8,3,0,0,0,0,1)}).ToArray()),
            Connections=new(Power(3,2),Power(2,6),Power(6,2),Power(6,4),Signal(2,5))};
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],100),([new(new(3),ElectricalEnable.Disabled)],30),
            ([new(new(3),ElectricalEnable.Enabled)],30));
        Assert.True(Powered(phases[0][^1]));Assert.All(phases[1],read=>Assert.False(Powered(read)));Assert.All(phases[2],read=>Assert.True(Powered(read)));
        foreach(var owner in new ulong[]{2,6})
        {
            var latched=Activation(phases[0][^1],owner);Assert.Equal(ActivationPhase.Latched,latched.Phase);
            Assert.All(phases[1].Concat(phases[2]),read=>Assert.Equal(latched,Activation(read,owner)));
        }
    }

    [Fact]
    public void CyclicClockProvenanceTerminatesAndMultipleSourcesRemainRejectedForAccounting()
    {
        var construction=Machine();
        var second=WorkshopInput.Generic(PartId.ImpactSwitch,new(6),-8,1,0,0,0,0,1);
        var clock=WorkshopInput.Clock(new(7),6,1,0,0,0,0,1,1f);
        construction=construction with {Instances=new(construction.Instances.Concat(new IWorkshopInstance[]{second,clock}).ToArray()),
            Connections=new(Power(2,6),Power(6,2),Power(6,7))};
        var network=WorkshopActivationCompiler.Compile(construction);
        Assert.Equal(default,network.Nodes.ToArray().Single(node=>node.Owner==clock.Id).PowerSource);
        var sourced=construction with {Connections=construction.Connections.With(Power(3,2))};
        Assert.Equal(new GpuBodyId(3),WorkshopActivationCompiler.Compile(sourced).Nodes.ToArray().Single(node=>node.Owner==clock.Id).PowerSource);
        var battery=WorkshopInput.Battery(new(8),-6,4,0,0,0,0,1,ElectricalSourceSettings.Default);
        var ambiguous=sourced with {Instances=new(sourced.Instances.Concat(new IWorkshopInstance[]{battery}).ToArray()),Connections=sourced.Connections.With(Power(8,6))};
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied,()=>WorkshopActivationCompiler.Compile(ambiguous));
        var bumper=WorkshopInput.Generic(PartId.PinballBumper,new(9),12,1,0,0,0,0,1);
        var storage=ambiguous with {Instances=new(ambiguous.Instances.Concat(new IWorkshopInstance[]{bumper}).ToArray()),Connections=ambiguous.Connections.With(Power(6,9))};
        // Physical compile is isolated from activation compilation; it must reject the storage binding itself.
        storage=storage with {Instances=new(storage.Instances.Where(instance=>instance.Id!=clock.Id).ToArray()),Connections=storage.Connections.Without(clock.Id)};
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied,()=>WorkshopPhysicsCompiler.Compile(storage,new(1,2)));
        var direct=storage with {Connections=new(Power(3,9))};
        Assert.Single(WorkshopPhysicsCompiler.Compile(direct,new(1,2)).Electrical.Bindings.ToArray());
        var indirect=storage with {Connections=new(Power(3,2),Power(2,9))};
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied,()=>WorkshopPhysicsCompiler.Compile(indirect,new(1,2)));
    }
}
