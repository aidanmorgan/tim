using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ActivationTimerTests
{
    private static (ActivationNetwork Network, PhysicsSceneDeclaration Scene, ContactTriggerRead First, ContactTriggerRead Second) Fixture(bool bypass = false)
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Basketball(new(1),0,4,0,0,0,0,1),
            WorkshopInput.Switch(new(2),0,2,0,0,0,0,1,ContactTriggerSettings.Default),
            WorkshopInput.Lamp(new(3),3,1,0,0,0,0,1),
            WorkshopInput.Delay(new(4),2,2,0,0,0,0,1,DelayDuration.Default),
            WorkshopInput.Switch(new(5),0,1,0,0,0,0,1,ContactTriggerSettings.Default));
        var links = new WorkshopConnections(Link(2,4),Link(5,4),Link(4,3));
        if (bypass) links = links.With(Link(5,3));
        var construction = new WorkshopConstruction(new(1),WorkshopCadenceSettings.Default(),instances,Connections:links);
        var scene = WorkshopPhysicsCompiler.Compile(construction,new(1,2));
        ContactTriggerRead Event(ulong owner,uint ordinal) => new(WorkshopPhysicsCompiler.ContactTrigger(new(owner)),
            new(owner),new(1),1,scene.Colliders.ToArray().First(c=>c.Body.Value==owner).Id,ordinal,(Half)0,new((Half)1));
        return (WorkshopActivationCompiler.Compile(construction),scene,Event(2,10),Event(5,20));
    }
    private static WorkshopConnection Link(ulong a,ulong b) => new(new(a),WorkshopSocket.ActivationOut,new(b),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation);
    private static ActivationLatch Node(PhysicsActivationRead read,ulong id)
    {
        for(var i=0;i<read.Count;i++) if(read[i].Node.Value==id)return read[i];
        throw new InvalidOperationException();
    }
    [Fact]
    public void FractionalStartCeilingAndExactDeadlineEmitDistinctTimerOccurrence()
    {
        var (network,scene,first,_) = Fixture();
        var clear = network.Clear(); var timers = network.ClearTimers();
        var counting = network.Consume(clear,timers,new[]{first},new(3));
        Assert.Equal(ActivationTimerPhase.Ready,timers[0].Phase);
        Assert.Equal(ActivationTimerPhase.Counting,counting.Timers[0].Phase);
        Assert.Equal(3UL,counting.Timers[0].StartedTick); Assert.Equal(123UL,counting.Timers[0].DueTick);
        var before = network.Consume(counting.Activations,counting.Timers,[],new(122));
        Assert.Equal(ActivationPhase.Clear,Node(before.Activations,3).Phase);
        var due = network.Consume(before.Activations,before.Timers,[],new(123));
        var lamp = Node(due.Activations,3);
        Assert.Equal(ActivationTimerPhase.Finished,due.Timers[0].Phase);
        Assert.Equal(ActivationOccurrenceKind.TimerElapsed,lamp.Kind);
        Assert.Equal(new ActivationNodeId(4),lamp.Emitter);
        Assert.Equal(492u,lamp.EventOrdinal); Assert.Equal((Half)0,lamp.EventPhase);
        Assert.Equal(first.EventOrdinal,lamp.CauseOrdinal); Assert.Equal(first.Id,lamp.Trigger);
        network.ValidateRead(due.Activations,due.Timers,new(123),4,scene);
        Assert.Throws<ArgumentException>(()=>network.ValidateRead(due.Activations,due.Timers,new(122),4,scene));
        Assert.Equal(ActivationTimerPhase.Ready,network.ClearTimers()[0].Phase);
    }
    [Fact]
    public void BusyAndFinishedInputsDoNotRestartOrReemit()
    {
        var (network,_,first,second) = Fixture();
        var counting = network.Consume(network.Clear(),network.ClearTimers(),new[]{first},new(3));
        var busy = network.Consume(counting.Activations,counting.Timers,new[]{second},new(5));
        Assert.Equal(counting.Timers[0],busy.Timers[0]);
        var finished = network.Consume(busy.Activations,busy.Timers,[],new(123));
        var fresh = network.Consume(network.Clear(),network.ClearTimers(),new[]{first},new(3));
        var freshFinished = network.Consume(fresh.Activations,fresh.Timers,[],new(123));
        var ignored = network.Consume(freshFinished.Activations,freshFinished.Timers,
            new[]{second with {EventOrdinal=500}},new(125));
        Assert.Equal(ActivationPhase.Latched,Node(ignored.Activations,5).Phase);
        Assert.Equal(freshFinished.Timers[0],ignored.Timers[0]);
        Assert.Equal(Node(freshFinished.Activations,3),Node(ignored.Activations,3));
    }
    [Fact]
    public void EarlierPhysicalBypassWinsOverDueAtSamePublicationBoundary()
    {
        var (network,scene,first,second) = Fixture(true);
        var counting = network.Consume(network.Clear(),network.ClearTimers(),new[]{first},new(3));
        var due = network.Consume(counting.Activations,counting.Timers,
            new[]{second with {EventOrdinal=491}},new(123));
        Assert.Equal(ActivationOccurrenceKind.Contact,Node(due.Activations,3).Kind);
        Assert.Equal(new ActivationNodeId(5),Node(due.Activations,3).Emitter);
        Assert.Equal(ActivationTimerPhase.Finished,due.Timers[0].Phase);
        network.ValidateRead(due.Activations,due.Timers,new(123),4,scene);
    }
    [Fact]
    public void SignedPhaseCeilingExactAndOverflowBoundaries()
    {
        foreach(var substeps in new uint[]{2,4,8})
        {
            var boundary=10*substeps;
            Assert.Equal(10UL,new ActivationTime(boundary,(Half)(-2048)).CeilingTick(substeps));
            Assert.Equal(10UL,new ActivationTime(boundary,(Half)0).CeilingTick(substeps));
            Assert.Equal(11UL,new ActivationTime(boundary,(Half).25).CeilingTick(substeps));
            Assert.Equal(10UL,new ActivationTime(boundary-1,(Half)2047).CeilingTick(substeps));
        }
        Assert.Throws<ArgumentException>(()=>new ActivationTime(0,(Half)(-1)).CeilingTick(4));
        var cause=new ActivationCause(new(1),new(1),new(1),new(uint.MaxValue,(Half)0),new((Half)1));
        var input=new ActivationOccurrence(ActivationOccurrenceKind.Contact,new(1),cause.Time,cause);
        Assert.Throws<OverflowException>(()=>ActivationTimerState.Ready(new(2)).Trigger(new(new(2),1440),input,4));
        Assert.Throws<ArgumentException>(()=>(input with {Kind=ActivationOccurrenceKind.TimerElapsed,Time=new(uint.MaxValue,(Half).25)}).Validate());
    }
    [Fact]
    public void CompleteWorkerResponseRoundTripsCountingAndFinishedTimerSections()
    {
        var (network,_,first,_) = Fixture();
        var counting=network.Consume(network.Clear(),network.ClearTimers(),new[]{first},new(3));
        var finished=network.Consume(counting.Activations,counting.Timers,[],new(123));
        foreach(var withContact in new[]{false,true})
        foreach(var (logical,tick) in new[]{(counting,3UL),(finished,123UL)})
        {
            var body=new CanonicalBody(new(1),1,tick,default,default,default);
            var bytes=new byte[PhysicsMotionRead.ByteLength]; var p=PhysicsMotionRead.HeaderBytes;
            void U(int offset,uint value)=>BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset),value);
            U(0,1);U(4,4);U(8,checked((uint)(tick-1)*4));U(12,checked((uint)tick*4));
            U(p,(uint)PhysicsMotionKind.FreePolynomial);U(p+4,checked((uint)(tick-1)*4));
            U(p+8,checked((uint)tick*4));U(p+12,checked((uint)(tick-1)*4));
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(p+22),BitConverter.HalfToUInt16Bits((Half)480));
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(p+62),BitConverter.HalfToUInt16Bits((Half)1));
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(p+24),1);
            var read=new WorkshopRead(new(1),new(tick),body,new(2),
                new(WorkshopClockDomain.SimulationMonotonic,new(1),new(100000000),new(100000)),
                CanonicalRotation.Identity,Motion:PhysicsMotionRead.Decode(bytes,body,new(tick)),
                Activations:logical.Activations,Timers:logical.Timers,
                ContactWorks:withContact ? new(new[]{new ContactWorkRead(new(99),new(8),new(1),1,new(17),8,(Half)512,
                    new((Half)4),new((Half)20),new((Half)12))}) : default);
            var response=new WorkshopResponse(default,WorkshopResponseKind.Read,new(WorkshopCommandOutcome.Applied,WorkshopRejection.None),
                WorkshopSimulationPhase.Running,read,new(1,2),new(1),new(1),new(1),new(1),new(tick));
            var encoded=WorkshopWire.Encode(response);
            var decoded=WorkshopWire.DecodeResponse(encoded);
            Assert.True(read.HasSameContent(decoded.Read));
            Assert.Equal(withContact ? 1 : 0, decoded.Read.ContactWorks.Count);
            Assert.Equal(logical.Timers[0],decoded.Read.Timers[0]);
            Assert.Equal(Node(logical.Activations,3),Node(decoded.Read.Activations,3));
        }
    }
    [Fact]
    public void MalformedTimerReadAndWireRejectWithoutChangingCommittedState()
    {
        var (network,scene,first,_) = Fixture();
        var counting=network.Consume(network.Clear(),network.ClearTimers(),new[]{first},new(3));
        var value=counting.Timers[0];
        foreach(var invalid in new[]{value with {DueTick=122},value with {StartedTick=2},
            value with {Phase=ActivationTimerPhase.Finished},value with {Node=new(99)}})
            Assert.Throws<ArgumentException>(()=>network.ValidateRead(counting.Activations,new(new[]{invalid}),new(3),4,scene));
        var bytes=new byte[WorkshopTimerWire.ByteLength];
        WorkshopTimerWire.Write(counting.Timers,bytes);
        Assert.Equal(value,WorkshopTimerWire.Read(bytes,1)[0]);
        foreach(var offset in new[]{8,12,44,86,WorkshopTimerWire.RecordBytes})
        {
            var changed=(byte[])bytes.Clone(); changed[offset]=255;
            Assert.Throws<ArgumentException>(()=>WorkshopTimerWire.Read(changed,1));
        }
        Assert.Throws<ArgumentException>(()=>network.Consume(counting.Activations,counting.Timers,new[]{first,first},new(4)));
        Assert.Equal(value,counting.Timers[0]);
    }
}
