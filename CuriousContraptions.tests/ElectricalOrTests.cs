using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Electrical;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

/// <summary>CAT-026 through the real compiler, worker device and Powered Gate motion: supplied output while either condition holds,
/// energy only from the bottom supply, monotone loops admitted at their least fixed point every tick, and loops mixing a nonmonotone
/// gate still refused.</summary>
public sealed class ElectricalOrTests
{
    internal static readonly PartId Kind=new("electrical_or");
    private const ulong Supply=7, Gate=8, Consumer=9;
    private static readonly bool[] OrTruth=[false,true,true,true]; // rows 00, 01, 10, 11 (CAT-026 O1)
    private static WorkshopConnection Link(ulong source,ulong target,WorkshopSocket input=WorkshopSocket.PowerIn)=>BothGateTests.Link(source,target,input);
    private static WorkshopGenericInstance Battery(ulong id,float x,bool enabled)=>PlacedParts.Battery(new(id), x, 3, 0, enabled: enabled?ElectricalEnable.Enabled:ElectricalEnable.Disabled);
    private static ElectricalLogicRead Logic(WorkshopRead read,ulong owner=Gate)=>BothGateTests.Logic(read,owner);
    private static bool Powered(WorkshopRead read)=>read.Presentation.Actuators.ToArray().Single(row=>row.Owner.Value==Consumer).SupplyClosed;
    private static float BladeY(WorkshopRead read) =>
        3f + read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == Consumer).Coordinate;
    /// <summary>The consumer blade has not left its rest height: the output never powered it, not even for one tick.</summary>
    private static void AssertBladeStill(WorkshopRead read,float rest)=>
        Assert.True(MathF.Abs(BladeY(read)-rest)<.01f,$"blade moved to {BladeY(read)} from {rest} at tick {read.Tick}");

    /// <summary>Two "power switches" (Battery → ImpactSwitch, Story 9.5b) are the conditions; a ball dropped on a switch closes it
    /// for good. Battery 7 is the separate bottom supply, gate 8 the Either gate, Powered Gate 9 the consumer.</summary>
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
    [InlineData(ElectricalInputState.Neither,true,false)]
    [InlineData(ElectricalInputState.SecondOnly,true,true)]
    [InlineData(ElectricalInputState.FirstOnly,true,true)]
    [InlineData(ElectricalInputState.Both,true,true)]
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
        // Each true row really opens the consumer; every false row leaves it still on every tick, not only at the end.
        if(expected) Assert.True(BladeY(phases[1][^1])>BladeY(rest)+.2f);
        else Assert.All(phases[1],read=>AssertBladeStill(read,BladeY(rest)));
    }

    [Fact]
    public async Task TrueRowWithoutSupplyWireNeverCreatesPower()
    {
        var construction=SwitchMachine(ElectricalInputState.Both,supplyEnabled:true);
        construction=construction with {Connections=construction.Connections.Without(Link(Supply,Gate))};
        var (built,reads,_)=await NoFixedCapacityTests.RunAndReset(construction,200);
        Assert.Equal(ElectricalInputState.Both,Logic(reads[^1]).Inputs); // the control: both conditions really hold
        Assert.All(reads,read=>{Assert.False(Logic(read).Supplied);Assert.False(Logic(read).Active);Assert.False(Powered(read));
            AssertBladeStill(read,BladeY(built));});
    }

    [Fact]
    public async Task ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact()
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
            var first=Logic(read).Inputs==ElectricalInputState.FirstOnly;
            Assert.True(Logic(read).Supplied);Assert.Equal(first,Logic(read).Active);Assert.Equal(first,Powered(read));
        });
        var closedAt=reads.FindIndex(read=>Logic(read).Inputs==ElectricalInputState.FirstOnly);
        Assert.InRange(closedAt,1,reads.Count-90);
        Assert.All(reads.Take(closedAt),read=>AssertBladeStill(read,BladeY(built))); // the 00 row never powered the consumer
        Assert.All(reads.Skip(closedAt),read=>Assert.Equal(ElectricalInputState.FirstOnly,Logic(read).Inputs));
        Assert.True(BladeY(reads[^1])>BladeY(built)+.2f); // and one condition really opens it
        NoFixedCapacityTests.AssertSameState(built,reset,"Either gate Reset");
        Assert.True(built.Presentation.HasSameBits(reset.Presentation));
        var (_,replay,_)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,240);
        for(var i=0;i<reads.Count;i++) NoFixedCapacityTests.AssertSameState(reads[i],replay[i],"saved Either gate replay");
    }

    /// <summary>Placement and wiring order must not change any committed result, for every row.</summary>
    [Theory]
    [InlineData(ElectricalInputState.Neither,false)]
    [InlineData(ElectricalInputState.FirstOnly,true)]
    [InlineData(ElectricalInputState.SecondOnly,true)]
    [InlineData(ElectricalInputState.Both,true)]
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

    /// <summary>CAT-026 O6/O7 (CAT-013 A4): two Either gates feed each other's first input, both supplied by battery 2; condition
    /// battery 1 feeds one second input. The loop is admitted and restarts from unpowered every tick: it never latches itself on,
    /// the condition powers both on the first tick, and removing the condition or the supply retracts both on the next.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick(bool swapIds)
    {
        ulong a=swapIds?6UL:4UL, b=swapIds?4UL:6UL;
        var parts=new List<IWorkshopInstance>{Battery(1,-8,false),Battery(2,-5,true),
            WorkshopInput.Generic(Kind,new(a),-1,3,-3,0,0,0,1),WorkshopInput.Generic(Kind,new(b),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)};
        if(swapIds) parts.Reverse();
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(parts.ToArray()),Connections:new(
            Link(a,b,WorkshopSocket.FirstIn),Link(b,a,WorkshopSocket.FirstIn),Link(1,a,WorkshopSocket.SecondIn),
            Link(2,a),Link(2,b),Link(b,Consumer)));
        construction.Validate();
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],60),
            ([new(new(1),ElectricalEnable.Enabled)],90),([new(new(1),ElectricalEnable.Disabled)],60),
            ([new(new(1),ElectricalEnable.Enabled)],60),([new(new(2),ElectricalEnable.Disabled)],60));
        var rest=BladeY(phases[0][0]);
        void Off(WorkshopRead read,bool condition,bool supplied)
        {
            Assert.Equal(new(new(a),condition?ElectricalInputState.SecondOnly:ElectricalInputState.Neither,supplied,false),Logic(read,a));
            Assert.Equal(new(new(b),ElectricalInputState.Neither,supplied,false),Logic(read,b));
            Assert.False(Powered(read));
        }
        void On(WorkshopRead read)
        {
            Assert.Equal(new(new(a),ElectricalInputState.Both,true,true),Logic(read,a));
            Assert.Equal(new(new(b),ElectricalInputState.FirstOnly,true,true),Logic(read,b));
            Assert.True(Powered(read));
        }
        Assert.All(phases[0],read=>{Off(read,false,true);AssertBladeStill(read,rest);}); // no self-latching from tick 0
        Assert.All(phases[1],On);
        Assert.True(BladeY(phases[1][^1])>rest+.2f);
        Assert.All(phases[2],read=>Off(read,false,true)); // the condition is forgotten on the very next tick
        Assert.True(BladeY(phases[2][^1])<BladeY(phases[1][^1])-.02f);
        Assert.All(phases[3],On);
        Assert.All(phases[4],read=>Off(read,true,false)); // losing the supply retracts both as well, condition still held
    }

    /// <summary>CAT-026 O8: a loop closed only through an open Impact Switch. The Either gate passes its supply through its other
    /// input; a Both Gate in the same place does not.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ContactLoopKeepsAnEitherGateOnThroughItsOtherInput(bool either)
    {
        var kind=either?Kind:BothGateTests.Kind;
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(
            Battery(1,-8,true),Battery(2,-5,true),
            WorkshopInput.Generic(kind,new(Gate),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch,new(6),-1,1,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)),
            Connections:new(Link(Gate,6),Link(6,Gate,WorkshopSocket.FirstIn),Link(1,Gate,WorkshopSocket.SecondIn),Link(2,Gate),Link(Gate,Consumer)));
        construction.Validate();
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(construction,120);
        Assert.All(reads,read=>
        {
            Assert.Equal(new(new(Gate),ElectricalInputState.SecondOnly,true,either),Logic(read));
            Assert.Equal(either,Powered(read));
        });
        if(either) Assert.True(BladeY(reads[^1])>BladeY(built)+.2f);
        else Assert.All(reads,read=>AssertBladeStill(read,BladeY(built)));
        NoFixedCapacityTests.AssertSameState(built,reset,"contact loop Reset");
    }

    public enum FeedbackLoop { OrToOr, WithNand, WithNor, WithXor, ThroughOpenSwitch, EnteringPowerIn }
    /// <summary>Each loop as an acyclic construction plus the one wire that closes it, and the ids a refusal must name (null when
    /// the loop is monotone and admitted).</summary>
    internal static (WorkshopConstruction Open, WorkshopConnection Closing, string? Ids) LoopCase(FeedbackLoop loop)
    {
        IWorkshopInstance Either(ulong id,float x)=>WorkshopInput.Generic(Kind,new(id),x,3,0,0,0,0,1);
        IWorkshopInstance Nand(ulong id,float x)=>WorkshopInput.Generic(ElectricalNandTests.Kind,new(id),x,3,-3,0,0,0,1);
        IWorkshopInstance Nor(ulong id,float x)=>WorkshopInput.Generic(ElectricalNorTests.Kind,new(id),x,3,-3,0,0,0,1);
        IWorkshopInstance Xor(ulong id,float x)=>WorkshopInput.Generic(ElectricalXorTests.Kind,new(id),x,3,-3,0,0,0,1);
        var supply=Battery(1,-8,true);
        var (parts,links,closing,ids)=loop switch
        {
            FeedbackLoop.OrToOr => (new[]{supply,Either(4,0),Either(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.FirstIn),(string?)null),
            FeedbackLoop.WithNand => (new[]{supply,Either(4,0),Nand(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            FeedbackLoop.WithNor => (new[]{supply,Either(4,0),Nor(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            // CAT-026 O10 / CAT-027 X9: Xor → Or → Xor.
            FeedbackLoop.WithXor => (new[]{supply,Either(4,0),Xor(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            FeedbackLoop.ThroughOpenSwitch => (new[]{supply,Either(4,0),WorkshopInput.Generic(PartId.ImpactSwitch,new(6),4,1,0,0,0,0,1),Nor(7,8)},
                new[]{Link(1,4),Link(1,7),Link(4,6),Link(6,7,WorkshopSocket.FirstIn)},Link(7,4,WorkshopSocket.FirstIn),"4, 6, 7"),
            FeedbackLoop.EnteringPowerIn => (new[]{supply,Nand(4,0),Either(6,4)},new[]{Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4),"4, 6"),
            _ => throw new ArgumentOutOfRangeException(nameof(loop))
        };
        return (new(new(2),WorkshopCadenceSettings.Default(),new(parts),Connections:new(links)),closing,ids);
    }

    [Theory]
    [InlineData(FeedbackLoop.WithNand)]
    [InlineData(FeedbackLoop.WithNor)]
    [InlineData(FeedbackLoop.WithXor)]
    [InlineData(FeedbackLoop.ThroughOpenSwitch)]
    [InlineData(FeedbackLoop.EnteringPowerIn)]
    public async Task LoopsMixingANonmonotoneGateAreRefusedAtomicallyBeforeAnySolve(FeedbackLoop loop)
    {
        var (open,closing,ids)=LoopCase(loop);
        open.Validate(); // the control: the same parts without the closing wire are admitted
        var looped=open with {Connections=open.Connections.With(closing)};
        var refusal=EditRefusal.Is(WorkshopEditRejection.NonmonotoneFeedback,looped.Validate);
        Assert.Contains("parts "+ids+".",refusal.Message,StringComparison.Ordinal);
        Assert.StartsWith("Break the wire loop",refusal.Message,StringComparison.Ordinal);
        EditRefusal.Is(WorkshopEditRejection.NonmonotoneFeedback,()=>(open with {Connections=new(looped.Connections.Reverse().ToArray())}).Validate());
        var saveRefusal=SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection,()=>WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(looped,10)));
        Assert.Equal(WorkshopEditRejection.NonmonotoneFeedback,Assert.IsType<WorkshopEditRejectedException>(saveRefusal.InnerException).Reason);
        Assert.Equal(open,WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(open,10)).Construction); // control
        await using var device=new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()),new(101,206));
        await using var simulation=new WorkshopSimulation(device,new WorkshopPhysicsEngineTests.StepClock(),WorkshopCadenceSettings.Default(),
            new(1),new WorkshopPhysicsEngineTests.NoInstallation(),GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Initialize()).Outcome);
        var before=simulation.Construction;
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected,WorkshopRejection.InvalidConstruction),await simulation.Construct(looped));
        Assert.Equal(before,simulation.Construction);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Construct(open)).Outcome);
    }

    [Fact]
    public async Task PureOrLoopIsAdmittedBySaveAndConstruct()
    {
        var (open,closing,ids)=LoopCase(FeedbackLoop.OrToOr);
        Assert.Null(ids);
        var looped=open with {Connections=open.Connections.With(closing)};
        looped.Validate();
        Assert.Equal(looped,WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(looped,10)).Construction);
        var (built,reads,reset)=await NoFixedCapacityTests.RunAndReset(looped,30);
        Assert.All(reads,read=>{Assert.False(Logic(read,4).Active);Assert.False(Logic(read,6).Active);}); // no condition: no power
        NoFixedCapacityTests.AssertSameState(built,reset,"pure Or loop Reset");
    }

    [Fact]
    public void CompiledGraphAdmitsAnOrCycleAndRejectsAMixedOne()
    {
        foreach(var (loop,admitted) in new[]{(FeedbackLoop.OrToOr,true),(FeedbackLoop.WithNor,false)})
        {
            var (open,closing,_)=LoopCase(loop);
            var scene=WorkshopPhysicsCompiler.Compile(open,new(1,2));
            var edges=SupplyTopology.Create(open,DeclaredPhysics.Of).Edges;
            var looped=edges.Append(new SupplyEdge(closing.Source,closing.Target,closing.Input)).ToArray();
            CompiledSupplyGraph Build()=>new(scene.SupplyGraph.Nodes,looped,scene.Electrical.Sources,scene.Prismatics,scene.PrismaticDrives,scene.Bodies);
            if(admitted) _=Build();
            else Assert.Equal("A nonmonotone electrical gate lies on a directed cycle.",Assert.Throws<ArgumentException>(Build).Message);
        }
    }

    [Fact]
    public void LawMonotonicityAndUndefinedValuesAreClosed()
    {
        Assert.Equal(OrTruth,new[]{(false,false),(false,true),(true,false),(true,true)}
            .Select(row=>CompiledSupplyGraph.Evaluate(LogicGateOperation.Or,row.Item1,row.Item2)).ToArray());
        Assert.True(CompiledSupplyGraph.IsMonotone(LogicGateOperation.Or));
        Assert.True(SupplyCycles.Monotone(LogicGateOperation.Or));
        foreach(var undefined in new[]{(LogicGateOperation)5,(LogicGateOperation)255})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.Evaluate(undefined,true,false));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.IsMonotone(undefined));
        }
        Assert.Equal(LogicGateOperation.Or,DeclaredPhysics.Of(Kind).Electrical!.Supply!.Value.Operation);
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

    /// <summary>Battery controls reaching each row in turn: 00, 10, 01, 11 supplied, then 11 without supply.</summary>
    internal static readonly (WorkshopElectricalControl[] Controls,ElectricalInputState Inputs,bool Supplied,bool Active)[] Rows=
    [
        ([],ElectricalInputState.Neither,true,false),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.FirstOnly,true,true),
        ([new(new(1),ElectricalEnable.Disabled),new(new(2),ElectricalEnable.Enabled)],ElectricalInputState.SecondOnly,true,true),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.Both,true,true),
        ([new(new(3),ElectricalEnable.Disabled)],ElectricalInputState.Both,false,false)
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
        // Rows contradicting the declared Or law are refused at admission: a supplied 01 left off (an And-shaped row) and a supplied
        // 00 passed on (a Nand- or Nor-shaped row).
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[2][^1],Logic(phases[2][^1],4) with {Active=false}),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[0][^1],Logic(phases[0][^1],4) with {Active=true}),construction));
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class ElectricalOrPresentationTests(NativeSceneFixture godot)
{
    private static readonly string[] TruthDots=["TruthDot01","TruthDot10","TruthDot11"];

    [Fact]
    public async Task PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset()
    {
        var construction=ElectricalOrTests.BatteryMachine();
        var rows=ElectricalOrTests.Rows;
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,rows.Select(row=>(row.Controls,2)).ToArray());
        var registry=new PartRegistry();registry.Load();
        var part=registry.Create(ElectricalOrTests.Kind);
        try
        {
            godot.Tree.Root.AddChild(part);
            var face=part.GetNode<Node3D>("Visual");
            // O11: the dots derive from the truth table, so Or shows the 01, 10 and 11 dots.
            var dots=face.GetChildren().OfType<MeshInstance3D>().Where(mesh=>mesh.Name.ToString().StartsWith("TruthDot",StringComparison.Ordinal)).ToArray();
            Assert.Equal(TruthDots,dots.Select(dot=>dot.Name.ToString()).ToArray());
            Assert.Equal(new[]{-.07f,.07f,.21f},dots.Select(dot=>dot.Position.X).ToArray());
            Assert.All(dots,dot=>{Assert.Equal(-.4f,dot.Position.Y);Assert.Equal(.42f,dot.Position.Z);
                Assert.Equal(.035f,Assert.IsType<SphereMesh>(dot.Mesh).Radius);
                Assert.Equal(new Color(.91f,.718f,.392f),Assert.IsType<StandardMaterial3D>(dot.GetActiveMaterial(0)).AlbedoColor);});
            var strokes=part.GetNode<Node3D>("Visual/Pictogram").GetChildren().OfType<MeshInstance3D>().ToArray();
            // Counted from electrical_or.tscn: two input stubs, four upper-curve, four lower-curve and four concave-back segments and
            // the output stub; no bubble.
            Assert.Equal(15,strokes.Length);
            Assert.DoesNotContain(strokes,stroke=>stroke.Mesh is TorusMesh);
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
