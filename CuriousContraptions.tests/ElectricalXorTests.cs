using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Electrical;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

/// <summary>CAT-027 through the real compiler, worker device and Powered Gate motion: supplied output while exactly one condition
/// holds, energy only from the bottom supply, settled reconvergence, and every loop through the gate refused.</summary>
public sealed class ElectricalXorTests
{
    internal static readonly PartId Kind=new("electrical_xor");
    private const ulong Supply=7, Gate=8, Consumer=9;
    private static readonly bool[] XorTruth=[false,true,true,false]; // rows 00, 01, 10, 11 (CAT-027 X1)
    private static WorkshopConnection Link(ulong source,ulong target,WorkshopSocket input=WorkshopSocket.PowerIn)=>BothGateTests.Link(source,target,input);
    private static WorkshopGenericInstance Battery(ulong id,float x,bool enabled)=>PlacedParts.Battery(new(id), (float)(x), 3, 0, enabled: enabled?ElectricalEnable.Enabled:ElectricalEnable.Disabled);
    private static ElectricalLogicRead Logic(WorkshopRead read,ulong owner=Gate)=>BothGateTests.Logic(read,owner);
    private static bool Powered(WorkshopRead read)=>read.Presentation.Actuators.ToArray().Single(row=>row.Owner.Value==Consumer).SupplyClosed;
    private static float BladeY(WorkshopRead read) =>
        3f + read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == Consumer).Coordinate;
    /// <summary>The consumer blade has not left its rest height: the output never powered it, not even for one tick.</summary>
    private static void AssertBladeStill(WorkshopRead read,float rest)=>
        Assert.True(MathF.Abs(BladeY(read)-rest)<.01f,$"blade moved to {BladeY(read)} from {rest} at tick {read.Tick}");

    /// <summary>Two "power switches" (Battery → ImpactSwitch, Story 9.5b) are the conditions; a ball dropped on a switch closes it
    /// for good. Battery 7 is the separate bottom supply, gate 8 the Exactly one gate, Powered Gate 9 the consumer.</summary>
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
    [InlineData(ElectricalInputState.Both,true,false)]   // low when the inputs are identical
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

    /// <summary>CAT-027 X5: a true row never powers once the supply wire, or the condition battery links, are removed.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemovingTheSupplyOrConditionLinksUnpowersATrueRow(bool removeSupply)
    {
        var construction=SwitchMachine(ElectricalInputState.FirstOnly,supplyEnabled:true);
        construction=construction with {Connections=removeSupply
            ? construction.Connections.Without(Link(Supply,Gate))
            : construction.Connections.Without(Link(1,3)).Without(Link(2,4))};
        var (built,reads,_)=await NoFixedCapacityTests.RunAndReset(construction,200);
        Assert.All(reads,read=>
        {
            Assert.Equal(!removeSupply,Logic(read).Supplied);
            Assert.False(Logic(read).Active);Assert.False(Powered(read));AssertBladeStill(read,BladeY(built));
        });
        // The control: the condition really closed (its contact conducts only when the condition battery feeds it).
        Assert.Equal(removeSupply?ElectricalInputState.FirstOnly:ElectricalInputState.Neither,Logic(reads[^1]).Inputs);
    }

    [Fact]
    public async Task ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact()
    {
        var construction=SwitchMachine(ElectricalInputState.FirstOnly,supplyEnabled:true);
        var saved=new WorkshopSavedConstruction(construction,new(10));
        var bytes=WorkshopSaveCodec.Encode(saved);var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved,loaded);
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
        Assert.True(BladeY(reads[^1])>BladeY(built)+.2f); // and exactly one condition really opens it
        NoFixedCapacityTests.AssertSameState(built,reset,"Exactly one gate Reset");
        Assert.True(built.Presentation.HasSameBits(reset.Presentation));
        var (_,replay,_)=await NoFixedCapacityTests.RunAndReset(loaded.Construction,240);
        for(var i=0;i<reads.Count;i++) NoFixedCapacityTests.AssertSameState(reads[i],replay[i],"saved Exactly one gate replay");
    }

    /// <summary>Placement and wiring order must not change any committed result, for every row. (The same-source reconvergence
    /// below is the case an evaluation-order defect would glitch for this gate.)</summary>
    [Theory]
    [InlineData(ElectricalInputState.Neither,false)]
    [InlineData(ElectricalInputState.FirstOnly,true)]
    [InlineData(ElectricalInputState.SecondOnly,true)]
    [InlineData(ElectricalInputState.Both,false)]
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

    /// <summary>CAT-027 X6/X7: battery 1 feeds the Exactly one gate's first input directly. Without a wire into the Either gate the
    /// gate passes the supply (the control); with battery 1 also wired into the Either gate, which feeds the second input, both
    /// inputs are high and the output stays off from the very first tick, whatever the placement or wire order.</summary>
    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public async Task ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick(bool gateFirst,bool reverseLinks)
    {
        ulong either=gateFirst?Gate:6, xor=gateFirst?6:Gate;
        var parts=new List<IWorkshopInstance>{Battery(1,-8,true),Battery(2,-5,true),
            WorkshopInput.Generic(ElectricalOrTests.Kind,new(either),-1,3,-3,0,0,0,1),
            WorkshopInput.Generic(Kind,new(xor),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)};
        if(gateFirst) parts.Reverse();
        var links=new[]{Link(1,xor,WorkshopSocket.FirstIn),Link(2,either),Link(either,xor,WorkshopSocket.SecondIn),Link(2,xor),Link(xor,Consumer)};
        if(reverseLinks) Array.Reverse(links);
        var single=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(parts.ToArray()),Connections:new(links));
        var (built,control,_)=await NoFixedCapacityTests.RunAndReset(single,90);
        Assert.All(control,read=>{Assert.Equal(ElectricalInputState.FirstOnly,Logic(read,xor).Inputs);Assert.True(Logic(read,xor).Active);Assert.True(Powered(read));});
        Assert.True(BladeY(control[^1])>BladeY(built)+.2f); // the control: the same consumer really opens on the 10 row
        var wired=links.Append(Link(1,either,WorkshopSocket.FirstIn)).ToArray();
        if(reverseLinks) Array.Reverse(wired);
        var reconvergent=single with {Connections=new(wired)};
        var (rest,reads,reset)=await NoFixedCapacityTests.RunAndReset(reconvergent,90);
        Assert.All(reads,read=>
        {
            Assert.Equal(new(new(either),ElectricalInputState.FirstOnly,true,true),Logic(read,either));
            Assert.Equal(new(new(xor),ElectricalInputState.Both,true,false),Logic(read,xor));
            Assert.False(Powered(read));AssertBladeStill(read,BladeY(rest));
        });
        NoFixedCapacityTests.AssertSameState(rest,reset,"reconvergent Exactly one gate Reset");
    }

    /// <summary>An Or↔Or loop feeding the Exactly one gate off the cycle is admitted and powers it: the refusal is limited to cycles
    /// that contain the nonmonotone gate.</summary>
    internal static WorkshopConstruction OrLoopFeedingXor(bool closed)
    {
        var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(
            Battery(1,-8,false),Battery(2,-5,true),
            WorkshopInput.Generic(ElectricalOrTests.Kind,new(4),-1,3,-3,0,0,0,1),
            WorkshopInput.Generic(ElectricalOrTests.Kind,new(6),-4,3,-6,0,0,0,1),
            WorkshopInput.Generic(Kind,new(Gate),3,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.PoweredGate,new(Consumer),9,3,0,0,0,0,1)),
            Connections:new(Link(4,6,WorkshopSocket.FirstIn),Link(1,4,WorkshopSocket.SecondIn),Link(2,4),Link(2,6),
                Link(6,Gate,WorkshopSocket.FirstIn),Link(2,Gate),Link(Gate,Consumer)));
        return closed?construction with {Connections=construction.Connections.With(OrLoopClosing)}:construction;
    }
    internal static readonly WorkshopConnection OrLoopClosing=Link(6,4,WorkshopSocket.FirstIn);

    [Fact]
    public async Task OrLoopFeedingTheGateIsAdmittedAndPowersIt()
    {
        var construction=OrLoopFeedingXor(true);
        construction.Validate();
        Assert.Equal(construction,WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(construction,10)).Construction);
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,([],40),([new(new(1),ElectricalEnable.Enabled)],90),
            ([new(new(1),ElectricalEnable.Disabled)],40));
        var rest=BladeY(phases[0][0]);
        Assert.All(phases[0],read=>{Assert.False(Logic(read,4).Active);Assert.False(Logic(read).Active);AssertBladeStill(read,rest);});
        Assert.All(phases[1],read=>
        {
            Assert.True(Logic(read,4).Active);Assert.True(Logic(read,6).Active);
            Assert.Equal(new(new(Gate),ElectricalInputState.FirstOnly,true,true),Logic(read));Assert.True(Powered(read));
        });
        Assert.True(BladeY(phases[1][^1])>rest+.2f);
        Assert.All(phases[2],read=>{Assert.False(Logic(read,4).Active);Assert.False(Logic(read).Active);Assert.False(Powered(read));});
    }

    public enum FeedbackLoop { XorToXor, WithOr, WithNand, ThroughOpenSwitch, EnteringPowerIn, OrLoopFeedingXor }
    /// <summary>Each loop as an acyclic construction plus the one wire that closes it, and the ids a refusal must name (null for the
    /// admitted monotone control).</summary>
    internal static (WorkshopConstruction Open, WorkshopConnection Closing, string? Ids) LoopCase(FeedbackLoop loop)
    {
        IWorkshopInstance Xor(ulong id,float x)=>WorkshopInput.Generic(Kind,new(id),(float)(x),3,0,0,0,0,1);
        IWorkshopInstance Either(ulong id,float x)=>WorkshopInput.Generic(ElectricalOrTests.Kind,new(id),(float)(x),3,-3,0,0,0,1);
        IWorkshopInstance Nand(ulong id,float x)=>WorkshopInput.Generic(ElectricalNandTests.Kind,new(id),(float)(x),3,-3,0,0,0,1);
        if(loop==FeedbackLoop.OrLoopFeedingXor) return (OrLoopFeedingXor(false),OrLoopClosing,null);
        var supply=Battery(1,-8,true);
        var (parts,links,closing,ids)=loop switch
        {
            FeedbackLoop.XorToXor => (new[]{supply,Xor(4,0),Xor(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.FirstIn),"4, 6"),
            // X9: Xor → Or → Xor.
            FeedbackLoop.WithOr => (new[]{supply,Xor(4,0),Either(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            FeedbackLoop.WithNand => (new[]{supply,Xor(4,0),Nand(6,4)},new[]{Link(1,4),Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4,WorkshopSocket.SecondIn),"4, 6"),
            // X8/X10: Xor output → switch PowerIn, switch Supply → Xor FirstIn, with the contact open.
            FeedbackLoop.ThroughOpenSwitch => (new[]{supply,Xor(4,0),WorkshopInput.Generic(PartId.ImpactSwitch,new(6),4,1,0,0,0,0,1)},
                new[]{Link(1,4),Link(4,6)},Link(6,4,WorkshopSocket.FirstIn),"4, 6"),
            FeedbackLoop.EnteringPowerIn => (new[]{supply,Xor(4,0),Either(6,4)},new[]{Link(1,6),Link(4,6,WorkshopSocket.FirstIn)},
                Link(6,4),"4, 6"),
            _ => throw new ArgumentOutOfRangeException(nameof(loop))
        };
        return (new(new(2),WorkshopCadenceSettings.Default(),new(parts),Connections:new(links)),closing,ids);
    }

    [Theory]
    [InlineData(FeedbackLoop.XorToXor)]
    [InlineData(FeedbackLoop.WithOr)]
    [InlineData(FeedbackLoop.WithNand)]
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
        EditRefusal.Is(WorkshopEditRejection.NonmonotoneFeedback,()=>(open with {Connections=new(looped.Connections.Reverse().ToArray())}).Validate());
        var saveRefusal=SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection,()=>WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Forged(looped,10)));
        Assert.Equal(WorkshopEditRejection.NonmonotoneFeedback,Assert.IsType<WorkshopEditRejectedException>(saveRefusal.InnerException).Reason);
        // X8: the refused Run leaves the saved machine unchanged.
        var saved=WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(open,new(10)));
        await using var device=new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()),new(101,206));
        await using var simulation=new WorkshopSimulation(device,new WorkshopPhysicsEngineTests.StepClock(),WorkshopCadenceSettings.Default(),
            new(1),new WorkshopPhysicsEngineTests.NoInstallation(),GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Initialize()).Outcome);
        var before=simulation.Construction;
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected,WorkshopRejection.InvalidConstruction),await simulation.Construct(looped));
        Assert.Equal(before,simulation.Construction);
        Assert.Equal(saved,WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(open,new(10))));
        Assert.Equal(open,WorkshopSaveCodec.Decode(SaveFixture.Elements,saved).Construction);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Construct(open)).Outcome);
    }

    [Fact]
    public void CompiledGraphRejectsAXorCycleAsAnInvariant()
    {
        var (open,closing,_)=LoopCase(FeedbackLoop.WithOr);
        var scene=WorkshopPhysicsCompiler.Compile(open,new(1,2));
        var edges=SupplyTopology.Create(open,DeclaredPhysics.Of).Edges;
        _=new CompiledSupplyGraph(scene.SupplyGraph.Nodes,edges,scene.Electrical.Sources,scene.Prismatics,scene.PrismaticDrives,scene.Bodies);
        var looped=edges.Append(new SupplyEdge(closing.Source,closing.Target,closing.Input)).ToArray();
        var invariant=Assert.Throws<ArgumentException>(()=>new CompiledSupplyGraph(scene.SupplyGraph.Nodes,looped,scene.Electrical.Sources,scene.Prismatics,scene.PrismaticDrives,scene.Bodies));
        Assert.Equal("A nonmonotone electrical gate lies on a directed cycle.",invariant.Message);
    }

    [Fact]
    public void LawMonotonicityAndUndefinedValuesAreClosed()
    {
        Assert.Equal(XorTruth,new[]{(false,false),(false,true),(true,false),(true,true)}
            .Select(row=>CompiledSupplyGraph.Evaluate(LogicGateOperation.Xor,row.Item1,row.Item2)).ToArray());
        Assert.False(CompiledSupplyGraph.IsMonotone(LogicGateOperation.Xor));
        Assert.False(SupplyCycles.Monotone(LogicGateOperation.Xor));
        foreach(var undefined in new[]{(LogicGateOperation)5,(LogicGateOperation)255})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.Evaluate(undefined,true,false));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CompiledSupplyGraph.IsMonotone(undefined));
        }
        Assert.Equal(LogicGateOperation.Xor,DeclaredPhysics.Of(Kind).Electrical!.Supply!.Value.Operation);
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

    /// <summary>Battery controls reaching each row in turn: 00, 10, 01, 11 supplied, then 01 without supply.</summary>
    internal static readonly (WorkshopElectricalControl[] Controls,ElectricalInputState Inputs,bool Supplied,bool Active)[] Rows=
    [
        ([],ElectricalInputState.Neither,true,false),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.FirstOnly,true,true),
        ([new(new(1),ElectricalEnable.Disabled),new(new(2),ElectricalEnable.Enabled)],ElectricalInputState.SecondOnly,true,true),
        ([new(new(1),ElectricalEnable.Enabled)],ElectricalInputState.Both,true,false),
        ([new(new(1),ElectricalEnable.Disabled),new(new(3),ElectricalEnable.Disabled)],ElectricalInputState.SecondOnly,false,false)
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
        // Rows contradicting the declared Xor law are refused at admission: a supplied 11 passed on (an Or-shaped row) and a supplied
        // 01 left off (an And-shaped row).
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[3][^1],Logic(phases[3][^1],4) with {Active=true}),construction));
        Assert.Throws<ArgumentException>(()=>WorkshopMessages.ValidatePresentationOwners(Replace(phases[2][^1],Logic(phases[2][^1],4) with {Active=false}),construction));
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class ElectricalXorPresentationTests(NativeSceneFixture godot)
{
    private static readonly string[] TruthDots=["TruthDot01","TruthDot10"];

    [Fact]
    public async Task PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset()
    {
        var construction=ElectricalXorTests.BatteryMachine();
        var rows=ElectricalXorTests.Rows;
        var phases=await WorkshopPhysicsEngineTests.RunPhases(construction,rows.Select(row=>(row.Controls,2)).ToArray());
        var registry=new PartRegistry();registry.Load();
        var part=registry.Create(ElectricalXorTests.Kind);
        try
        {
            godot.Tree.Root.AddChild(part);
            var face=part.GetNode<Node3D>("Visual");
            // X11: the dots derive from the truth table, so Xor shows the 01 and 10 dots.
            var dots=face.GetChildren().OfType<MeshInstance3D>().Where(mesh=>mesh.Name.ToString().StartsWith("TruthDot",StringComparison.Ordinal)).ToArray();
            Assert.Equal(TruthDots,dots.Select(dot=>dot.Name.ToString()).ToArray());
            Assert.Equal(new[]{-.07f,.07f},dots.Select(dot=>dot.Position.X).ToArray());
            Assert.All(dots,dot=>{Assert.Equal(-.4f,dot.Position.Y);
                Assert.Equal(.07f,dot.Mesh!.GetAabb().Size.X,4);
                Assert.Equal(new Color(.91f,.718f,.392f),Assert.IsType<StandardMaterial3D>(dot.GetActiveMaterial(0)).AlbedoColor);});
            var strokes=part.GetNode<Node3D>("Visual/Pictogram").GetChildren().OfType<MeshInstance3D>().ToArray();
            // Counted from electrical_xor.tscn: four extra-back-curve, four upper-curve, four lower-curve and four concave-back
            // segments and the output stub; no input stubs, no bubble.
            Assert.Equal(17,strokes.Length);
            Assert.DoesNotContain(strokes,stroke=>stroke.Name == "Bubble");
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
                foreach(var sample in evaluator.Advance(AnimationTestTime.Seconds(time))) if(sample.Owner.Value==4) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
                Assert.Equal(Lit((rows[i].Inputs & ElectricalInputState.FirstOnly)!=0),Colour("FirstLamp"));
                Assert.Equal(Lit((rows[i].Inputs & ElectricalInputState.SecondOnly)!=0),Colour("SecondLamp"));
                Assert.Equal(Lit(rows[i].Active),Colour("OutputLamp"));
            }
            var (built,_,reset)=await NoFixedCapacityTests.RunAndReset(construction,2);
            evaluator=new WorkshopDeclaredPresentation(construction,BothGatePresentationTests.Response(reset,1));
            foreach(var sample in evaluator.Advance(AnimationTestTime.Seconds(time+1))) if(sample.Owner.Value==4) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
            Assert.True(built.Presentation.HasSameBits(reset.Presentation));
            foreach(var node in new[]{"FirstLamp","SecondLamp","OutputLamp"}) Assert.Equal(slate,Colour(node));
        }
        finally {part.Free();}
    }
}
