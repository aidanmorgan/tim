using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Electrical;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

/// <summary>CAT-025 through the real compiler, worker device and Powered Gate motion: supplied output only while neither
/// condition holds, energy only from the bottom supply, settled evaluation order and refused nonmonotone feedback.</summary>
public sealed class ElectricalNorTests
{
    internal static readonly PartId Kind=new("electrical_nor");
    private const ulong Supply=7, Gate=8, Consumer=9;
    private static readonly bool[] NorTruth=[true,false,false,false]; // rows 00, 01, 10, 11 (CAT-025 R1)
    private static WorkshopConnection Link(ulong source,ulong target,WorkshopSocket input=WorkshopSocket.PowerIn)=>BothGateTests.Link(source,target,input);
    private static WorkshopBattery Battery(ulong id,float x,bool enabled)=>WorkshopInput.Battery(new(id),x,3,0,0,0,0,1,
        ElectricalSourceSettings.Default with {Enabled=enabled?ElectricalEnable.Enabled:ElectricalEnable.Disabled});
    private static ElectricalLogicRead Logic(WorkshopRead read,ulong owner=Gate)=>BothGateTests.Logic(read,owner);
    private static bool Powered(WorkshopRead read)=>read.Presentation.Actuators.ToArray().Single(row=>row.Owner.Value==Consumer).SupplyClosed;
    private static float BladeY(WorkshopRead read)
    {
        var body=Enumerable.Range(0,read.Bodies.Count).Select(i=>read.Bodies[i].Body)
            .Single(row=>row.Id==WorkshopPhysicsCompiler.GateBlade(new(Consumer)));
        return (body.Cell.Y+body.Local.Y)/16f;
    }
    /// <summary>The consumer blade has not left its rest height: the output never powered it, not even for one tick.</summary>
    private static void AssertBladeStill(WorkshopRead read,float rest)=>
        Assert.True(MathF.Abs(BladeY(read)-rest)<.01f,$"blade moved to {BladeY(read)} from {rest} at tick {read.Tick}");

    /// <summary>Two "power switches" (Battery → ImpactSwitch, Story 9.5b) are the conditions; a ball dropped on a switch closes it
    /// for good. Battery 7 is the separate bottom supply, gate 8 the Neither gate, Powered Gate 9 the consumer.</summary>
    internal static WorkshopConstruction SwitchMachine(ElectricalInputState closed,bool supplyEnabled,bool gateFirst=false,bool reverseLinks=false)
    {
        var parts=new List<IWorkshopInstance>{
            Battery(1,-16,true),Battery(2,-10,true),
            WorkshopInput.Generic(PartId.ImpactSwitch,new(3),-13,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch,new(4),-7,1,0,0,0,0,1)};
        if((closed & ElectricalInputState.FirstOnly)!=0) parts.Add(WorkshopInput.Generic(PartId.Basketball,new(5),-13,3,0,0,0,0,1));
        if((closed & ElectricalInputState.SecondOnly)!=0) parts.Add(WorkshopInput.Generic(PartId.Basketball,new(6),-7,3,0,0,0,0,1));
        parts.Add(Battery(Supply,-2,supplyEnabled));
        var gate=WorkshopInput.Generic(Kind,new(Gate),3,3,0,0,0,0,1);
        if(gateFirst) parts.Insert(0,gate); else parts.Add(gate);
        parts.Add(WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1));
        var links=new[]{Link(1,3),Link(2,4),Link(3,Gate,WorkshopSocket.FirstIn),Link(4,Gate,WorkshopSocket.SecondIn),Link(Supply,Gate),Link(Gate,Consumer)};
        if(reverseLinks) Array.Reverse(links);
        return new(new(2),WorkshopCadenceSettings.Default(),new(parts.ToArray()),Connections:new(links));
    }

    /// <summary>The balls latch their switches with the bottom supply Disabled; only then is the supply offered (or kept off).</summary>
    private static Task<List<List<WorkshopRead>>> LatchThenSupply(WorkshopConstruction construction,bool supply)=>
        WorkshopPhysicsEngineTests.RunPhases(construction,([],120),
            ([new(new(Supply),supply?ElectricalEnable.Enabled:ElectricalEnable.Disabled)],90));

    [Theory]
    [InlineData(ElectricalInputState.Neither,true,true)]
    [InlineData(ElectricalInputState.SecondOnly,true,false)]
    [InlineData(ElectricalInputState.FirstOnly,true,false)]
    [InlineData(ElectricalInputState.Both,true,false)]
    [InlineData(ElectricalInputState.Neither,false,false)]
    [InlineData(ElectricalInputState.SecondOnly,false,false)]
    [InlineData(ElectricalInputState.FirstOnly,false,false)]
    [InlineData(ElectricalInputState.Both,false,false)]
    public async Task SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer(ElectricalInputState closed,bool supply,bool expected)
    {
        var phases=await LatchThenSupply(SwitchMachine(closed,supplyEnabled:false),supply);
        var rest=phases[0][^1];
        Assert.All(phases[0],read=>{Assert.False(Logic(read).Supplied);Assert.False(Logic(read).Active);Assert.False(Powered(read));});
        Assert.Equal(closed,Logic(rest).Inputs); // both switches settled before the supply is offered
        Assert.All(phases[1],read=>
        {
            Assert.Equal(closed,Logic(read).Inputs);Assert.Equal(supply,Logic(read).Supplied);
            Assert.Equal(expected,Logic(read).Active);Assert.Equal(expected,Powered(read));
        });
        // The only true row really opens the consumer; every false row leaves it still on every tick, not only at the end.
        if(expected) Assert.True(BladeY(phases[1][^1])>BladeY(rest)+.2f);
        else Assert.All(phases[1],read=>AssertBladeStill(read,BladeY(rest)));
    }

    [Fact]
    public async Task TrueRowWithoutSupplyWireNeverCreatesPower()
    {
        var construction=SwitchMachine(ElectricalInputState.Neither,supplyEnabled:true);
        construction=construction with {Connections=construction.Connections.Without(Link(Supply,Gate))};
        var (built,reads,_)=await NoFixedCapacityTests.RunAndReset(construction,150);
        Assert.All(reads,read=>{Assert.Equal(ElectricalInputState.Neither,Logic(read).Inputs);Assert.False(Logic(read).Supplied);
            Assert.False(Logic(read).Active);Assert.False(Powered(read));AssertBladeStill(read,BladeY(built));});
    }

    [Fact]
    public async Task ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact()
    {
        var construction=SwitchMachine(ElectricalInputState.FirstOnly,supplyEnabled:true);
        var saved=new WorkshopSavedConstruction(construction,new(10));
        var bytes=WorkshopSaveCodec.Encode(saved);var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved,loaded);Assert.Equal(bytes,WorkshopSaveCodec.Encode(loaded));
        Assert.Equal(construction.Connections,loaded.Construction.Connections);
        Assert.Contains(loaded.Construction.Instances,instance=>instance.PartId==Kind && instance.Id==new GpuBodyId(Gate));
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,240);
        Assert.Equal(new(new(Gate),ElectricalInputState.Neither,false,false),Logic(built));
        Assert.All(reads,read=>
        {
            var neither=Logic(read).Inputs==ElectricalInputState.Neither;
            Assert.True(Logic(read).Supplied);Assert.Equal(neither,Logic(read).Active);Assert.Equal(neither,Powered(read));
        });
        var closedAt=reads.FindIndex(read=>Logic(read).Inputs==ElectricalInputState.FirstOnly);
        Assert.InRange(closedAt,1,reads.Count-60);
        Assert.True(BladeY(reads[closedAt-1])>BladeY(built)+.02f); // the 00 row really opened the consumer first
        Assert.All(reads.Skip(closedAt),read=>Assert.Equal(ElectricalInputState.FirstOnly,Logic(read).Inputs));
        Assert.True(BladeY(reads[^1])<BladeY(reads[closedAt-1])-.02f); // and one condition really closes it again
        NoFixedCapacityTests.AssertSameState(built,reset,"Neither gate Reset");
        Assert.True(built.Presentation.HasSameBits(reset.Presentation));
        var (_,replay,_)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,240);
        for(var i=0;i<reads.Count;i++) NoFixedCapacityTests.AssertSameState(reads[i],replay[i],"saved Neither gate replay");
    }

    /// <summary>Placement and wiring order must not change any committed result. In a false row a gate visited before its
    /// switch-fed inputs would see 00 and latch on; the 00 row is the positive control of the same machine.</summary>
    [Theory]
    [InlineData(ElectricalInputState.FirstOnly,false)]
    [InlineData(ElectricalInputState.SecondOnly,false)]
    [InlineData(ElectricalInputState.Both,false)]
    [InlineData(ElectricalInputState.Neither,true)]
    public async Task PlacementAndWiringOrderGiveIdenticalCommittedResults(ElectricalInputState closed,bool expected)
    {
        var variants=new[]{(false,false),(true,false),(false,true),(true,true)};
        var runs=new List<List<WorkshopRead>>();
        foreach(var (gateFirst,reverse) in variants)
        {
            var phases=await LatchThenSupply(SwitchMachine(closed,false,gateFirst,reverse),true);
            var rest=BladeY(phases[0][^1]);
            Assert.All(phases[1],read=>{Assert.Equal(closed,Logic(read).Inputs);Assert.Equal(expected,Logic(read).Active);Assert.Equal(expected,Powered(read));});
            if(expected) Assert.True(BladeY(phases[1][^1])>rest+.2f);
            else Assert.All(phases[1],read=>AssertBladeStill(read,rest));
            runs.Add(phases.SelectMany(rows=>rows).ToList());
        }
        foreach(var run in runs.Skip(1))
            for(var i=0;i<run.Count;i++)
            {
                Assert.Equal(runs[0][i].Presentation.Logic.ToArray(),run[i].Presentation.Logic.ToArray());
                Assert.Equal(Powered(runs[0][i]),Powered(run[i]));
                Assert.Equal(BladeY(runs[0][i]),BladeY(run[i]));
            }
    }

    /// <summary>CAT-025 boundary (CAT-026 O9): condition battery 1 feeds the Neither gate's first input directly and its second input
    /// through an Either gate (its first input only; supplied by battery 2). Enabled from the start, the output
    /// never powers the consumer on any tick; disabling it is the positive control, and re-enabling retracts on the first tick.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReconvergentConditionKeepsTheOutputOffFromTheFirstTick(bool gateFirst)
    {
        ulong either=gateFirst?Gate:6, nor=gateFirst?6:Gate;
        var parts=new List<IWorkshopInstance>{Battery(1,-8,true),Battery(2,-5,true),
            WorkshopInput.Generic(ElectricalOrTests.Kind,new(either),-1,3,-3,0,0,0,1),
            WorkshopInput.Generic(Kind,new(nor),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)};
        if(gateFirst) parts.Reverse();
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(parts.ToArray()),Connections:new(
            Link(1,nor,WorkshopSocket.FirstIn),Link(1,either,WorkshopSocket.FirstIn),Link(2,either),
            Link(either,nor,WorkshopSocket.SecondIn),Link(2,nor),Link(nor,Consumer)));
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,60);
        Assert.All(reads,read=>
        {
            Assert.Equal(ElectricalInputState.FirstOnly,Logic(read,either).Inputs);Assert.True(Logic(read,either).Active);
            Assert.Equal(ElectricalInputState.Both,Logic(read,nor).Inputs);Assert.True(Logic(read,nor).Supplied);
            Assert.False(Logic(read,nor).Active);Assert.False(Powered(read));AssertBladeStill(read,BladeY(built));
        });
        NoFixedCapacityTests.AssertSameState(built,reset,"reconvergent Neither gate Reset");
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([new(new(1),ElectricalEnable.Disabled)],90),
            ([new(new(1),ElectricalEnable.Enabled)],40));
        Assert.All(phases[0],read=>{Assert.Equal(ElectricalInputState.Neither,Logic(read,nor).Inputs);Assert.True(Logic(read,nor).Active);Assert.True(Powered(read));});
        Assert.True(BladeY(phases[0][^1])>BladeY(built)+.2f); // the control: the same consumer really opens on the 00 row
        Assert.All(phases[1],read=>
        {
            Assert.Equal(ElectricalInputState.FirstOnly,Logic(read,either).Inputs);Assert.True(Logic(read,either).Active);
            Assert.Equal(ElectricalInputState.Both,Logic(read,nor).Inputs);Assert.False(Logic(read,nor).Active);Assert.False(Powered(read));
        });
        Assert.True(BladeY(phases[1][^1])<BladeY(phases[0][^1])-.02f);
    }

    /// <summary>A source-free loop of monotone Both Gates is admitted and stays unpowered; the Neither gate it feeds settles after
    /// it, so its true 00 row passes the separate supply.</summary>
    [Fact]
    public async Task MonotoneLoopIsAdmittedStaysUnpoweredAndSettlesBeforeTheGateItFeeds()
    {
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(
            WorkshopInput.Generic(Kind,new(Gate),3,3,0,0,0,0,1),
            WorkshopInput.Generic(BothGateTests.Kind,new(4),-3,3,-3,0,0,0,1),
            WorkshopInput.Generic(BothGateTests.Kind,new(6),-6,3,-3,0,0,0,1),
            Battery(Supply,-2,true),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)),
            Connections:new(Link(4,6,WorkshopSocket.FirstIn),Link(4,6,WorkshopSocket.SecondIn),Link(4,6),
                Link(6,4,WorkshopSocket.FirstIn),Link(6,4,WorkshopSocket.SecondIn),Link(6,4),
                Link(4,Gate,WorkshopSocket.FirstIn),Link(6,Gate,WorkshopSocket.SecondIn),Link(Supply,Gate),Link(Gate,Consumer)));
        construction.Validate();
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,60);
        Assert.All(reads,read=>
        {
            Assert.False(Logic(read,4).Active);Assert.False(Logic(read,6).Active);
            Assert.Equal(ElectricalInputState.Neither,Logic(read).Inputs);Assert.True(Logic(read).Active);Assert.True(Powered(read));
        });
        Assert.True(BladeY(reads[^1])>BladeY(built)+.2f);
        NoFixedCapacityTests.AssertSameState(built,reset,"monotone loop Reset");
    }

    public enum FeedbackLoop { NorToNor, ThroughBothGate, ThroughOr, MixedWithNand, ThroughOpenSwitch, EnteringPowerIn }
    /// <summary>Each loop as an acyclic construction plus the one wire that closes it, and the ids the refusal must name.</summary>
    internal static (WorkshopConstruction Open, WorkshopConnection Closing, string Ids) LoopCase(FeedbackLoop loop)
    {
        IWorkshopInstance Nor(ulong id,float x)=>WorkshopInput.Generic(Kind,new(id),x,3,0,0,0,0,1);
        IWorkshopInstance Both(ulong id,float x)=>WorkshopInput.Generic(BothGateTests.Kind,new(id),x,3,-3,0,0,0,1);
        IWorkshopInstance Nand(ulong id,float x)=>WorkshopInput.Generic(ElectricalNandTests.Kind,new(id),x,3,-3,0,0,0,1);
        IWorkshopInstance Either(ulong id,float x)=>WorkshopInput.Generic(ElectricalOrTests.Kind,new(id),x,3,-3,0,0,0,1);
        var supply=Battery(1,-8,true);
        var (parts,links,closing,ids)=loop switch
        {
            FeedbackLoop.NorToNor => (new[]{supply,Nor(4,0),Nor(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.FirstIn),"4, 6"),
            FeedbackLoop.ThroughBothGate => (new[]{supply,Nor(4,0),Both(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn),Link(4,6,WorkshopSocket.SecondIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            // CAT-025 R6: Nor → Or → Nor.
            FeedbackLoop.ThroughOr => (new[]{supply,Nor(4,0),Either(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            FeedbackLoop.MixedWithNand => (new[]{supply,Nor(4,0),Nand(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.SecondIn)},
                Link(6,4,WorkshopSocket.FirstIn),"4, 6"),
            FeedbackLoop.ThroughOpenSwitch => (new[]{supply,Nor(4,0),WorkshopInput.Generic(PartId.ImpactSwitch,new(6),4,1,0,0,0,0,1)},
                new[]{Link(1,4),Link(4,6)},Link(6,4,WorkshopSocket.FirstIn),"4, 6"),
            FeedbackLoop.EnteringPowerIn => (new[]{supply,Nor(4,0),Both(6,4),Both(7,8)},
                new[]{Link(1,6),Link(1,7),Link(4,7,WorkshopSocket.FirstIn),Link(7,6,WorkshopSocket.FirstIn)},
                Link(6,4),"4, 6, 7"),
            _ => throw new ArgumentOutOfRangeException(nameof(loop))
        };
        return (new(new(2),WorkshopCadenceSettings.Default(),new(parts),Connections:new(links)),closing,ids);
    }

    [Theory]
    [InlineData(FeedbackLoop.NorToNor)]
    [InlineData(FeedbackLoop.ThroughBothGate)]
    [InlineData(FeedbackLoop.ThroughOr)]
    [InlineData(FeedbackLoop.MixedWithNand)]
    [InlineData(FeedbackLoop.ThroughOpenSwitch)]
    [InlineData(FeedbackLoop.EnteringPowerIn)]
    public async Task ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve(FeedbackLoop loop)
    {
        var (open,closing,ids)=LoopCase(loop);
        open.Validate(); // the control: the same parts without the closing wire are admitted
        var looped=open with {Connections=open.Connections.With(closing)};
        var refusal=EditRefusal.Is(WorkshopEditRejection.NonmonotoneFeedback,looped.Validate);
        Assert.Contains("parts "+ids+".",refusal.Message,StringComparison.Ordinal);
        Assert.StartsWith("Break the wire loop",refusal.Message,StringComparison.Ordinal);
        // Wire order does not change the refusal.
        EditRefusal.Is(WorkshopEditRejection.NonmonotoneFeedback,()=>(open with {Connections=new(looped.Connections.Reverse().ToArray())}).Validate());
        // A stored save holding the loop is refused before any instance is constructed.
        var saveRefusal=SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection,()=>WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(looped,10)));
        Assert.Equal(WorkshopEditRejection.NonmonotoneFeedback,Assert.IsType<WorkshopEditRejectedException>(saveRefusal.InnerException).Reason);
        Assert.Equal(open,WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(open,10)).Construction); // control
        // The simulation refuses the candidate and keeps its construction.
        await using var device=new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(),new(101,206));
        await using var simulation=new WorkshopSimulation(device,new WorkshopPhysicsEngineTests.StepClock(),WorkshopCadenceSettings.Default(),
            new(1),new WorkshopPhysicsEngineTests.NoInstallation(),GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Initialize()).Outcome);
        var before=simulation.Construction;
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected,WorkshopRejection.InvalidConstruction),await simulation.Construct(looped));
        Assert.Equal(before,simulation.Construction);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Construct(open)).Outcome);
    }

    [Fact]
    public void CompiledGraphRejectsNonmonotoneCycleAsAnInvariant()
    {
        var (open,closing,_)=LoopCase(FeedbackLoop.ThroughBothGate);
        var scene=WorkshopPhysicsCompiler.Compile(open,new(1,2));
        var edges=SupplyTopology.Create(open,DeclaredPhysics.Of).Edges;
        _=new CompiledSupplyGraph(scene.SupplyGraph.Nodes,edges,scene.Electrical.Sources,scene.Prismatics,scene.Bodies);
        var looped=edges.Append(new SupplyEdge(closing.Source,closing.Target,closing.Input)).ToArray();
        var invariant=Assert.Throws<ArgumentException>(()=>new CompiledSupplyGraph(scene.SupplyGraph.Nodes,looped,scene.Electrical.Sources,scene.Prismatics,scene.Bodies));
        Assert.Equal("A nonmonotone electrical gate lies on a directed cycle.",invariant.Message);
    }

    [Fact]
    public void LawMonotonicityAndUndefinedValuesAreClosed()
    {
        Assert.Equal(NorTruth,new[]{(false,false),(false,true),(true,false),(true,true)}
            .Select(row=>CompiledSupplyGraph.Evaluate(LogicGateOperation.Nor,row.Item1,row.Item2)).ToArray());
        Assert.False(CompiledSupplyGraph.IsMonotone(LogicGateOperation.Nor));
        Assert.False(SupplyCycles.Monotone(LogicGateOperation.Nor));
        foreach(var undefined in new[]{(LogicGateOperation)5,(LogicGateOperation)255})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.Evaluate(undefined,false,false));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.IsMonotone(undefined));
        }
        Assert.Equal(LogicGateOperation.Nor,DeclaredPhysics.Of(Kind).Electrical!.Supply!.Value.Operation);
        var construction=SwitchMachine(ElectricalInputState.Neither,true);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,()=>(construction with {Connections=construction.Connections.With(Link(1,Gate,(WorkshopSocket)255))}).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection,()=>(construction with {Connections=construction.Connections.With(
            new(new(3),WorkshopSocket.ActivationOut,new(Gate),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation))}).Validate());
        Assert.DoesNotContain(DeclaredPorts.Current.Of(Kind).ToArray(),port=>port.Domain==WorkshopConnectionDomain.Activation);
    }

    /// <summary>Supplemental battery-enabled machine (the conditions are batteries 1 and 2, the supply battery 3) for presentation
    /// reads.</summary>
    internal static WorkshopConstruction BatteryMachine()=>
        new(new(2),WorkshopCadenceSettings.Default(),new(
            Battery(1,-8,false),Battery(2,-5,false),Battery(3,-2,true),
            WorkshopInput.Generic(Kind,new(4),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)),
            Connections:new(Link(1,4,WorkshopSocket.FirstIn),Link(2,4,WorkshopSocket.SecondIn),Link(3,4),Link(4,Consumer)));

    /// <summary>Battery controls reaching each row in turn: 00, 10, 01, 11 supplied, then 00 without supply.</summary>
    internal static readonly (WorkshopElectricalControl[] Controls,ElectricalInputState Inputs,bool Supplied,bool Active)[] Rows=
    [
        ([],ElectricalInputState.Neither,true,true),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.FirstOnly,true,false),
        ([new(new(1),ElectricalEnable.Disabled),new(new(2),ElectricalEnable.Enabled)],ElectricalInputState.SecondOnly,true,false),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.Both,true,false),
        ([new(new(1),ElectricalEnable.Disabled),new(new(2),ElectricalEnable.Disabled),new(new(3),ElectricalEnable.Disabled)],ElectricalInputState.Neither,false,false)
    ];

    [Fact]
    public async Task CommittedReadsFollowEveryRowAndAdmissionIsNeutral()
    {
        var construction=BatteryMachine();
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,Rows.Select(row=>(row.Controls,3)).ToArray());
        for(var i=0;i<phases.Count;i++)
            Assert.All(phases[i],read=>Assert.Equal(new(new(4),Rows[i].Inputs,Rows[i].Supplied,Rows[i].Active),Logic(read,4)));
        var (built,_,_)=await NoFixedCapacityTests.RunAndReset(construction,1);
        Assert.Equal(new(new(4),ElectricalInputState.Neither,false,false),Logic(built,4));
        WorkshopMessages.ValidatePresentationOwners(built,construction);
        foreach(var read in phases.SelectMany(rows=>rows)) WorkshopMessages.ValidatePresentationOwners(read,construction);
        WorkshopRead Replace(WorkshopRead read,ElectricalLogicRead logic)=>read with {Presentation=new(read.Presentation.Actuators,read.Presentation.Loads,[logic])};
        // Rows contradicting the declared Nor law are refused at admission: a supplied 00 left off (an Or- or And-shaped row) and a
        // supplied 10 passed on (a Nand- or Or-shaped row).
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[0][^1],Logic(phases[0][^1],4) with {Active=false}),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[1][^1],Logic(phases[1][^1],4) with {Active=true}),construction));
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class ElectricalNorPresentationTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task PictogramSingleTruthDotAndThreeLampsFollowCommittedRowsThroughReset()
    {
        var construction=ElectricalNorTests.BatteryMachine();
        var rows=ElectricalNorTests.Rows;
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,rows.Select(row=>(row.Controls,2)).ToArray());
        var registry=new PartRegistry();registry.Load();
        var part=registry.Create(ElectricalNorTests.Kind);
        try
        {
            godot.Tree.Root.AddChild(part);
            var face=part.GetNode<Node3D>("Visual");
            // R9: the dots derive from the truth table, so Nor shows only the 00 dot.
            var dot=Assert.Single(face.GetChildren().OfType<MeshInstance3D>(),mesh=>mesh.Name.ToString().StartsWith("TruthDot",StringComparison.Ordinal));
            Assert.Equal("TruthDot00",dot.Name.ToString());
            Assert.Equal(new Vector3(-.21f,-.4f,.42f),dot.Position);
            Assert.Equal(.035f,Assert.IsType<SphereMesh>(dot.Mesh).Radius);
            Assert.Equal(new Color(.91f,.718f,.392f),Assert.IsType<StandardMaterial3D>(dot.GetActiveMaterial(0)).AlbedoColor);
            var strokes=part.GetNode<Node3D>("Visual/Pictogram").GetChildren().OfType<MeshInstance3D>().ToArray();
            // Counted from electrical_nor.tscn: two input stubs, four upper-curve, four lower-curve and four concave-back segments,
            // the bubble and the output stub.
            Assert.Equal(16,strokes.Length);
            var bubble=Assert.Single(strokes,stroke=>stroke.Mesh is TorusMesh);
            Assert.Equal(.18958f,bubble.Position.X,4); // icon cx 19, not Nand's cx 20
            Assert.All(strokes,stroke=>Assert.Equal(.4f,stroke.Position.Z));
            Assert.All(strokes,stroke=>Assert.Equal(new Color(.161f,.224f,.329f),Assert.IsType<StandardMaterial3D>(stroke.GetActiveMaterial(0)).AlbedoColor));
            Assert.Null(part.GetNodeOrNull("Visual/UpperPath")); // no Both Gate converging bars
            Color Colour(string node)=>Assert.IsType<StandardMaterial3D>(part.GetNode<MeshInstance3D>("Visual/"+node).MaterialOverride).AlbedoColor;
            var slate=new Color("#556573");var gold=new Color("#f7cb52");
            Color Lit(bool on)=>on?gold:slate;
            WorkshopDeclaredPresentation? evaluator=null;var time=0d;var publication=0UL;
            for(var i=0;i<rows.Length;i++)
            {
                var read=phases[i][^1];
                var response=BothGatePresentationTests.Response(read,++publication);
                if(evaluator is null){evaluator=new WorkshopDeclaredPresentation(construction,response);time=read.Capture!.Value.Time.Value/1_000_000_000d;}
                else evaluator.Accept(response);
                time+=.2;
                foreach(var sample in evaluator.Advance(time)) if(sample.Owner.Value==4) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
                Assert.Equal(Lit((rows[i].Inputs & ElectricalInputState.FirstOnly)!=0),Colour("FirstLamp"));
                Assert.Equal(Lit((rows[i].Inputs & ElectricalInputState.SecondOnly)!=0),Colour("SecondLamp"));
                Assert.Equal(Lit(rows[i].Active),Colour("OutputLamp"));
            }
            var (built,_,reset)=await NoFixedCapacityTests.RunAndReset(construction,2);
            evaluator=new WorkshopDeclaredPresentation(construction,BothGatePresentationTests.Response(reset,1));
            foreach(var sample in evaluator.Advance(time+1)) if(sample.Owner.Value==4) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
            Assert.True(built.Presentation.HasSameBits(reset.Presentation));
            foreach(var node in new[]{"FirstLamp","SecondLamp","OutputLamp"}) Assert.Equal(slate,Colour(node));
        }
        finally {part.Free();}
    }
}
