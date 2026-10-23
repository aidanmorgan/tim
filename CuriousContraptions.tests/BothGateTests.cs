using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Electrical;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class BothGateTests
{
    internal static readonly PartId Kind=new("both_gate");
    internal static WorkshopConnection Link(ulong source,ulong target,WorkshopSocket input=WorkshopSocket.PowerIn)=>
        new(new(source),WorkshopSocket.Supply,new(target),input,WorkshopConnectionDomain.Electrical);
    internal static WorkshopConstruction Machine(ElectricalInputState inputs=ElectricalInputState.Both,bool supply=true)=>
        new(new(2),WorkshopCadenceSettings.Default(),new(
            Battery(1,-8,(inputs & ElectricalInputState.FirstOnly)!=0),
            Battery(2,-5,(inputs & ElectricalInputState.SecondOnly)!=0),
            Battery(3,-2,supply),
            WorkshopInput.Generic(Kind,new(4),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(5),9,3,0,0,0,0,1)),
            Connections:new(Link(1,4,WorkshopSocket.FirstIn),Link(2,4,WorkshopSocket.SecondIn),Link(3,4),Link(4,5)));
    private static WorkshopGenericInstance Battery(ulong id,float x,bool enabled)=>PlacedParts.Battery(new(id), x, 3, 0, enabled: enabled?ElectricalEnable.Enabled:ElectricalEnable.Disabled);
    internal static ElectricalLogicRead Logic(WorkshopRead read,ulong owner=4)=>read.Presentation.Logic.ToArray().Single(row=>row.Owner.Value==owner);
    private static ActuatorDriveRead Drive(WorkshopRead read)=>read.Presentation.Actuators.ToArray().Single(row=>row.Owner.Value==5);
    private static float BladeY(WorkshopRead read)
    {
        var body=Enumerable.Range(0,read.Bodies.Count).Select(i=>read.Bodies[i].Body)
            .Single(row=>row.Id==WorkshopPhysicsCompiler.SliderBody(new(5), 0));
        return (body.Cell.Y+body.Local.Y)/16f;
    }

    [Theory]
    [InlineData(ElectricalInputState.Neither,false)]
    [InlineData(ElectricalInputState.FirstOnly,false)]
    [InlineData(ElectricalInputState.SecondOnly,false)]
    [InlineData(ElectricalInputState.Both,false)]
    [InlineData(ElectricalInputState.Neither,true)]
    [InlineData(ElectricalInputState.FirstOnly,true)]
    [InlineData(ElectricalInputState.SecondOnly,true)]
    [InlineData(ElectricalInputState.Both,true)]
    public async Task SeparateConditionsAndEnergyDriveActualConsumer(ElectricalInputState inputs,bool supply)
    {
        var construction=Machine(inputs,supply);var expected=inputs==ElectricalInputState.Both && supply;
        var saved=new WorkshopSavedConstruction(construction,new(6));
        var bytes=WorkshopSaveCodec.Encode(saved);var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved,loaded);Assert.Equal(bytes,WorkshopSaveCodec.Encode(loaded));
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,120);
        Assert.Equal(new(new(4),ElectricalInputState.Neither,false,false),Logic(built));
        Assert.All(reads,read=>
        {
            Assert.Equal(inputs,Logic(read).Inputs);Assert.Equal(supply,Logic(read).Supplied);
            Assert.Equal(expected,Logic(read).Active);Assert.Equal(expected,Drive(read).SupplyClosed);
        });
        if(expected) Assert.True(BladeY(reads[^1])>BladeY(built)+.2f);
        else Assert.True(MathF.Abs(BladeY(reads[^1])-BladeY(built))<.01f);
        NoFixedCapacityTests.AssertSameState(built,reset,"Both gate Reset");
        Assert.True(built.Presentation.HasSameBits(reset.Presentation));
    }

    [Fact]
    public async Task MissingBottomWireNeverConvertsConditionsIntoEnergy()
    {
        var construction=Machine();construction=construction with {Connections=construction.Connections.Without(Link(3,4))};
        var (built,reads,_)=await NoFixedCapacityTests.RunAndReset(construction,120);
        Assert.All(reads,read=>{Assert.Equal(ElectricalInputState.Both,Logic(read).Inputs);Assert.False(Logic(read).Supplied);Assert.False(Drive(read).SupplyClosed);});
        Assert.True(MathF.Abs(BladeY(reads[^1])-BladeY(built))<.01f);
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task ChainedReconvergenceAndMutualCyclesRetractWithoutOrderDelay(bool cycle,bool reverse)
    {
        var construction=Machine();
        var parts=construction.Instances.Concat(new IWorkshopInstance[]{WorkshopInput.Generic(Kind,new(6),6,3,-3,0,0,0,1)}).ToArray();
        var links=construction.Connections.Where(link=>link.Target.Value!=5).ToList();
        links.AddRange([Link(4,6,WorkshopSocket.FirstIn),Link(4,6,WorkshopSocket.SecondIn),Link(3,6),Link(6,5)]);
        if(cycle) links.AddRange([Link(6,4,WorkshopSocket.FirstIn),Link(6,4,WorkshopSocket.SecondIn),Link(6,4)]);
        if(reverse)
        {
            GpuBodyId Id(GpuBodyId value)=>value.Value switch {4=>new(6),6=>new(4),_=>value};
            parts=parts.Select(part=>part is WorkshopGenericInstance generic && generic.PartId==Kind
                ? (IWorkshopInstance)(generic with {Id=Id(generic.Id)}):part).ToArray();
            links=links.Select(link=>link with {Source=Id(link.Source),Target=Id(link.Target)}).Reverse().ToList();
            Array.Reverse(parts);
        }
        construction=construction with {Instances=new(parts),Connections=new(links.ToArray())};
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],90),
            ([new(new(1),ElectricalEnable.Disabled),new(new(2),ElectricalEnable.Disabled),new(new(3),ElectricalEnable.Disabled)],10),
            ([new(new(1),ElectricalEnable.Enabled),new(new(2),ElectricalEnable.Enabled),new(new(3),ElectricalEnable.Enabled)],30));
        Assert.All(phases[0],read=>Assert.True(Drive(read).SupplyClosed));
        Assert.All(phases[1],read=>
        {
            Assert.False(Drive(read).SupplyClosed);
            Assert.All(read.Presentation.Logic.ToArray(),row=>Assert.Equal(new(row.Owner,ElectricalInputState.Neither,false,false),row));
        });
        Assert.All(phases[2],read=>Assert.True(Drive(read).SupplyClosed));
        Assert.True(BladeY(phases[0][^1])>BladeY(phases[0][0])+.2f);
        var noSource=construction with {Connections=new(construction.Connections.Where(link=>link.Source.Value is not (1 or 2 or 3)).ToArray())};
        var (_,unseeded,_)=await NoFixedCapacityTests.RunAndReset(noSource,10);
        Assert.All(unseeded,read=>Assert.False(Drive(read).SupplyClosed));
    }

    [Fact]
    public async Task OpeningPoweredContactRetractsMutualLoopWhileSourcesRemainEnabled()
    {
        var construction=HoldTimerTests.Construction(.5f);
        var parts=construction.Instances.Concat(new IWorkshopInstance[]{
            WorkshopInput.Generic(Kind,new(7),-6,3,-3,0,0,0,1),
            WorkshopInput.Generic(Kind,new(8),-9,3,-3,0,0,0,1)}).ToArray();
        var links=construction.Connections.Where(link=>link.Domain==WorkshopConnectionDomain.Activation).ToList();
        links.AddRange([Link(4,3),Link(4,7,WorkshopSocket.FirstIn),Link(4,7,WorkshopSocket.SecondIn),
            Link(3,7),Link(7,8,WorkshopSocket.FirstIn),Link(7,8,WorkshopSocket.SecondIn),Link(3,8),
            Link(8,7),Link(8,5)]);
        construction=construction with {Instances=new(parts),Connections=new(links.ToArray())};
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,180);
        var active=reads.First(read=>HoldTimerTests.State(read).Phase==ActivationTimerPhase.Counting);
        var due=HoldTimerTests.State(active).DueTick;
        Assert.Contains(reads,read=>Drive(read).SupplyClosed);
        Assert.True(BladeY(reads.Single(read=>read.Tick.Value==due-1))>BladeY(built)+.02f);
        foreach(var read in reads.Where(read=>read.Tick.Value>=due).Take(10))
        {
            Assert.Equal(ElectricalInputState.Both,Logic(read,7).Inputs);
            Assert.False(Logic(read,7).Supplied);Assert.False(Logic(read,7).Active);
            Assert.Equal(ElectricalInputState.Neither,Logic(read,8).Inputs);
            Assert.False(Drive(read).SupplyClosed);
        }
        Assert.Equal(10,reads.Count(read=>read.Tick.Value>=due && read.Tick.Value<due+10));
        NoFixedCapacityTests.AssertSameState(built,reset,"contact-opening cycle Reset");
    }

    [Fact]
    public async Task CommittedLatchSetResetAndSupplyRestorationDriveBothGate()
    {
        var construction=Machine();
        construction=construction with {Instances=new(construction.Instances.Concat(new IWorkshopInstance[]{
            PlacedParts.Latch(new(6),-9,3,3,0,0,0,1),
            PlacedParts.Clock(new(7),-6,3,3,0,0,0,1,1f),
            PlacedParts.Clock(new(8),-3,3,3,0,0,0,1,1.5f)}).ToArray()),
            Connections=construction.Connections.Without(Link(3,4)).With(Link(3,6)).With(Link(6,4))
                .With(Link(1,7)).With(Link(1,8))
                .With(new(new(7),WorkshopSocket.ActivationOut,new(6),WorkshopSocket.SetIn,WorkshopConnectionDomain.Activation))
                .With(new(new(8),WorkshopSocket.ActivationOut,new(6),WorkshopSocket.ResetIn,WorkshopConnectionDomain.Activation))};
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],140),
            ([new(new(3),ElectricalEnable.Disabled)],10),([new(new(3),ElectricalEnable.Enabled)],40));
        var reads=phases.SelectMany(rows=>rows).ToArray();
        WorkshopRead At(ulong tick)=>reads.Single(read=>read.Tick.Value==tick);
        Assert.False(Drive(At(120)).SupplyClosed);Assert.True(Drive(At(121)).SupplyClosed);
        Assert.True(BladeY(At(140))>BladeY(At(120))+.02f);
        Assert.All(phases[1],read=>{Assert.Equal(ElectricalInputState.Both,Logic(read).Inputs);Assert.False(Drive(read).SupplyClosed);});
        Assert.True(Drive(At(151)).SupplyClosed);
        Assert.True(Drive(At(180)).SupplyClosed);Assert.False(Drive(At(181)).SupplyClosed);
        Assert.All(reads.Where(read=>read.Tick.Value>=181),read=>Assert.False(Logic(read).Active));
    }

    [Fact]
    public void EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject()
    {
        var construction=Machine();
        var clock=PlacedParts.Clock(new(7),12,3,0,0,0,0,1,1f);
        construction=construction with {Instances=new(construction.Instances.Append(clock).ToArray()),Connections=construction.Connections.With(Link(4,7))};
        var topology=SupplyTopology.Create(construction,DeclaredPhysics.Of);
        Assert.Equal(new[]{new GpuBodyId(3)},topology.UpstreamSources(clock.Id));
        Assert.Equal(new GpuBodyId(3),WorkshopActivationCompiler.Compile(construction).Nodes.ToArray().Single(row=>row.Owner==clock.Id).PowerSource);
        var conditionOnly=construction with {Connections=construction.Connections.Without(Link(3,4))};
        Assert.Empty(SupplyTopology.Create(conditionOnly,DeclaredPhysics.Of).UpstreamSources(clock.Id));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.Evaluate((LogicGateOperation)255,true,true));
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,()=> (construction with {Connections=construction.Connections.With(Link(1,4,WorkshopSocket.FirstIn))}).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,()=> (construction with {Connections=construction.Connections.With(Link(1,4,(WorkshopSocket)255))}).Validate());
        Assert.DoesNotContain(DeclaredPorts.Current.Of(Kind).ToArray(),port=>port.Domain==WorkshopConnectionDomain.Activation);
    }
    [Fact]
    public async Task DeclaredReadAdmissionRejectsMissingForeignAndInconsistentRows()
    {
        var construction=Machine();var (built,reads,_)=await NoFixedCapacityTests.RunAndReset(construction,2);
        WorkshopMessages.ValidatePresentationOwners(built,construction);
        var read=reads[^1];var row=Logic(read);
        WorkshopMessages.ValidatePresentationOwners(read,construction);
        WorkshopRead Replace(params ElectricalLogicRead[] rows)=>read with {Presentation=new(read.Presentation.Actuators,read.Presentation.Loads,rows)};
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(row with {Owner=new(99)}),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(row with {Active=false}),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(row with {Inputs=ElectricalInputState.Neither}),construction));
        Assert.Throws<ArgumentException>(()=>new PhysicsPresentationRead([],[],[row with {Inputs=(ElectricalInputState)255}]));
        Assert.Throws<ArgumentException>(()=>new PhysicsPresentationRead([],[],[row,row]));
        var response=BothGatePresentationTests.Response(read,1) with {Construction=construction};
        var decoded=WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
        Assert.True(read.Presentation.HasSameBits(decoded.Read.Presentation));
        Assert.Throws<ArgumentException>(()=>WorkshopWireFlatBuffers.EncodeResponse(response with {Read=Replace(row with {Active=false})}));
    }

    [Fact]
    public async Task DiscardAndFailedReadNeverPublishCandidatePower()
    {
        var transport=new HoldTimerTests.GatedRealTransport();
        await using var device=new WorkshopGpuDevice(transport,new(101,206));
        var profile=new WorkshopGpuProfile(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1));
        var admitted=await device.Admit(Machine(),new(1),profile,new(1));
        var committed=admitted.Read;device.Commit(admitted.Sequence);device.Discard(admitted.Sequence);
        var on=new WorkshopTickControls([ElectricalEnable.Enabled,ElectricalEnable.Enabled,ElectricalEnable.Enabled]);
        var off=new WorkshopTickControls([ElectricalEnable.Disabled,ElectricalEnable.Disabled,ElectricalEnable.Disabled]);
        var candidate=await device.Advance(committed,new(2),on);
        Assert.True(Logic(candidate.Read).Active);device.Discard(candidate.Sequence);
        transport.FailRead=true;
        await Assert.ThrowsAsync<InvalidOperationException>(async()=>await device.Advance(committed,new(3),on));
        var recovery=await device.Advance(committed,new(4),off);
        Assert.Equal(1UL,recovery.Read.Tick.Value);Assert.False(Logic(recovery.Read).Active);
        Assert.Equal(ElectricalInputState.Neither,Logic(committed).Inputs);Assert.False(Logic(committed).Active);
        device.Commit(recovery.Sequence);device.Discard(recovery.Sequence);
    }

}


[Collection<NativeSceneCollectionDefinition>]
public sealed class BothGatePresentationTests(NativeSceneFixture godot)
{
    internal static WorkshopResponse Response(WorkshopRead read,ulong publication)=>
        new(default,WorkshopResponseKind.Read,new(WorkshopCommandOutcome.Applied,WorkshopRejection.None),
            WorkshopSimulationPhase.Running,read,new(101,206),new(publication),new(1),new(1),new(1),new(read.Tick.Value));

    [Fact]
    public async Task ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset()
    {
        var construction=BothGateTests.Machine();
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],2),
            ([new(new(3),ElectricalEnable.Disabled)],2),([new(new(1),ElectricalEnable.Disabled)],2));
        var first=phases[0][^1];var noSupply=phases[1][^1];var secondOnly=phases[2][^1];
        var evaluator=new WorkshopDeclaredPresentation(construction,Response(first,1));
        var registry=new PartRegistry();registry.Load();
        var part=registry.Create(BothGateTests.Kind);var control=registry.Create(BothGateTests.Kind);
        try
        {
            godot.Tree.Root.AddChild(part);godot.Tree.Root.AddChild(control);
            Color Colour(MachinePart owner,string node)=>Assert.IsType<StandardMaterial3D>(owner.GetNode<MeshInstance3D>("Visual/"+node).MaterialOverride).AlbedoColor;
            void Apply(double time)
            { foreach(var sample in evaluator.Advance(time)) if(sample.Owner.Value==4) part.ApplyDeclaredProperty(sample.Binding,sample.Value); }
            var start=first.Capture!.Value.Time.Value/1_000_000_000d;
            Apply(start);Apply(start+.025);
            var quarter=Colour(part,"OutputLamp");
            Assert.InRange(quarter.R, float.Lerp(85f/255f,247f/255f,.103515625f)-.00001f,float.Lerp(85f/255f,247f/255f,.103515625f)+.00001f);
            Apply(start+.05);
            Assert.InRange(Colour(part,"OutputLamp").R,float.Lerp(85f/255f,247f/255f,.5f)-.00001f,float.Lerp(85f/255f,247f/255f,.5f)+.00001f);
            var time=start+.11;
            Apply(time);
            Assert.Equal(new Color("#f7cb52"),Colour(part,"FirstLamp"));Assert.Equal(new Color("#f7cb52"),Colour(part,"SecondLamp"));Assert.Equal(new Color("#f7cb52"),Colour(part,"OutputLamp"));
            Assert.Equal(new Color("#556573"),Colour(control,"OutputLamp")); // No property delivery cannot fake a powered lamp.
            var target=WorkshopDeclaredPresentation.Targets(construction).First(row=>row.Owner.Value==4).Target;
            evaluator.SetVisible(target,AnimationProperty.ColourRed,false);evaluator.Accept(Response(noSupply,2));
            Assert.DoesNotContain(evaluator.Advance(time+.2),sample=>sample.Owner.Value==4);
            evaluator.SetVisible(target,AnimationProperty.ColourRed,true);Apply(time+.31);
            Assert.Equal(new Color("#f7cb52"),Colour(part,"FirstLamp"));Assert.Equal(new Color("#f7cb52"),Colour(part,"SecondLamp"));Assert.Equal(new Color("#556573"),Colour(part,"OutputLamp"));
            evaluator.Accept(Response(secondOnly,3));Apply(time+.5);
            Assert.Equal(new Color("#556573"),Colour(part,"FirstLamp"));Assert.Equal(new Color("#f7cb52"),Colour(part,"SecondLamp"));
            Assert.Throws<ArgumentException>(()=>evaluator.Accept(Response(first,4)));
            Assert.Equal(new Color("#556573"),Colour(part,"OutputLamp"));
            var paused=Response(secondOnly,3) with {Kind=WorkshopResponseKind.Acknowledgement,Sequence=new(1),
                Publication=default,Projection=new(2),Phase=WorkshopSimulationPhase.Paused};
            evaluator.ValidateRebind(construction,paused);evaluator.Rebind(construction,paused);Apply(time+.6);
            Assert.Equal(new Color("#556573"),Colour(part,"FirstLamp"));Assert.Equal(new Color("#f7cb52"),Colour(part,"SecondLamp"));
            var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(new(construction,new(6))));
            var (built,_,reset)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,2);
            evaluator=new WorkshopDeclaredPresentation(loaded.Construction,Response(reset,1));Apply(time+1);
            Assert.Equal(ElectricalInputState.Neither,BothGateTests.Logic(built).Inputs);
            foreach(var node in new[]{"FirstLamp","SecondLamp","OutputLamp"}) Assert.Equal(new Color("#556573"),Colour(part,node));
        }
        finally {part.Free();control.Free();}
    }
}
