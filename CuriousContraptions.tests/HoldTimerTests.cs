using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Signal;
using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

public sealed class HoldTimerTests
{
    internal static readonly PartId Kind = new("hold_timer");
    internal static WorkshopGenericInstance Part(float seconds = 2f) =>
        WorkshopInput.Generic(Kind, new(3), 3, 2, 0, 0, 0, 0, 1,
            new([new(new ParameterId("hold_seconds"), seconds)]));
    private static WorkshopConnection Signal(ulong from, ulong to) =>
        new(new(from), WorkshopSocket.ActivationOut, new(to), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConnection Power(ulong from, ulong to) =>
        new(new(from), WorkshopSocket.Supply, new(to), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
    internal static ActivationTimerState State(WorkshopRead read) =>
        Enumerable.Range(0, read.Timers.Count).Select(i => read.Timers[i]).Single(row => row.Node.Value == 3);
    private static ActuatorDriveRead Blade(WorkshopRead read) =>
        read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == 5);
    private static float Travel(WorkshopRead read) => Blade(read).Coordinate;
    private static bool Powered(WorkshopRead read) => read.Presentation.Actuators.ToArray().Single(row => row.Owner.Value == 5).SupplyClosed;

    internal static WorkshopConstruction Construction(float seconds, bool supply = true, bool delayed = false)
    {
        IWorkshopInstance[] parts =
        [
            WorkshopInput.Generic(PartId.Basketball,new(1),0,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(2),0,1,0,0,0,0,1),
            Part(seconds),
            PlacedParts.Battery(new(4), 6, 3, 0),
            WorkshopInput.Generic(PartId.PoweredGate,new(5),9,3,0,0,0,0,1),
            WorkshopInput.Generic(PartId.Delay,new(6),-3,2,0,0,0,0,1,
                new([new(new ParameterId("delay_seconds"),.1f)]))
        ];
        var links = delayed ? new WorkshopConnections(Signal(2,6),Signal(6,3),Power(3,5))
            : new WorkshopConnections(Signal(2,3),Power(3,5));
        if (supply) links = links.With(Power(4,3));
        return new(new(2),WorkshopCadenceSettings.Default(),new(parts),Connections:links);
    }

    [Theory]
    [InlineData(.1f,12)]
    [InlineData(2f,240)]
    [InlineData(12f,1440)]
    [InlineData(.1234567f,15)]
    public async Task PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply(float seconds, int ticks)
    {
        var construction = Construction(seconds);
        var (built,reads,reset) = await WorkshopIntegrationHarness.ObserveReset(construction,ticks+110);
        var first = reads.First(read => State(read).Phase == ActivationTimerPhase.Counting);
        var state = State(first);
        Assert.Equal((ulong)ticks,state.DueTick-state.StartedTick);
        Assert.All(reads.Where(read=>read.Tick.Value>state.StartedTick && read.Tick.Value<state.DueTick),read=>Assert.True(Powered(read)));
        var expired = reads.Single(read=>read.Tick.Value==state.DueTick);
        Assert.Equal(ActivationTimerPhase.Ready,State(expired).Phase);
        Assert.False(Powered(expired));
        Assert.All(reads.Where(read=>read.Tick.Value>=state.DueTick),read=>Assert.False(Powered(read)));
        Assert.All(reads,read=>Assert.Equal(ActivationPhase.Clear,
            Enumerable.Range(0,read.Activations.Count).Select(i=>read.Activations[i]).Single(row=>row.Owner.Value==3).Phase));
        NoFixedCapacityTests.AssertSameState(built,reset,"rearming timed contact Reset");

        var (_,control,_) = await WorkshopIntegrationHarness.ObserveReset(Construction(seconds,supply:false),ticks+110);
        Assert.Contains(control,read=>State(read).Phase==ActivationTimerPhase.Counting);
        Assert.All(control,read=>Assert.False(Powered(read)));
    }

    [Fact]
    public async Task DueDelayClosesContactBeforeSameTickConsumerIntegrationAndSaveResetIsExact()
    {
        const float seconds=.1234567f;
        var construction=Construction(seconds,delayed:true);
        var saved=new WorkshopSavedConstruction(construction,new(7));
        var loaded=WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved,loaded);
        var part=Assert.IsType<WorkshopGenericInstance>(loaded.Construction.Instances.Single(row=>row.Id.Value==3));
        Assert.True(F32Bits.Equal(seconds,part.Parameters[new("hold_seconds")]));
        var (built,reads,reset)=await WorkshopIntegrationHarness.ObserveReset(loaded.Construction,180);
        var first=reads.First(read=>State(read).Phase==ActivationTimerPhase.Counting);
        var delay=Enumerable.Range(0,first.Timers.Count).Select(i=>first.Timers[i]).Single(row=>row.Node.Value==6);
        Assert.Equal(delay.DueTick,first.Tick.Value);
        Assert.True(Powered(first));
        var before=reads.Single(read=>read.Tick.Value+1==first.Tick.Value);
        Assert.True(Blade(first).Speed>0f, "gate accelerates on the due boundary");
        Assert.True(Travel(first)>Travel(before), "gate blade starts moving on the due boundary");
        var (_,control,_)=await WorkshopIntegrationHarness.ObserveReset(Construction(seconds,supply:false,delayed:true),180);
        Assert.True(Travel(first)>Travel(control.Single(read=>read.Tick.Value==first.Tick.Value)), "same timer without supply cannot move the blade");
        Assert.False(Powered(reads.Single(read=>read.Tick.Value==State(first).DueTick)));
        NoFixedCapacityTests.AssertSameState(built,reset,"saved timed contact Reset");
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(13f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidDurationDoesNotAlterCanonicalPart(float value)
    {
        var valid=Part();
        Assert.Throws<ArgumentException>(()=>Part(value));
        Assert.Equal(2f,valid.Parameters[new("hold_seconds")]);
    }

    [Fact]
    public void BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput()
    {
        var declaration=new ActivationTimerDeclaration(new(1),12,TimerCompletionPolicy.Rearm,TimerBoundary.BeforeNetworks,TimerElapsedSignal.None);
        var first=new ActivationOccurrence(ActivationOccurrenceKind.Contact,new(2),new(0,0),
            new(new(2),new(3),new(4),new(0,0),new(1f)));
        var counting=ActivationTimerState.Ready(new(1)).Trigger(declaration,first,4);
        var busy=first with {Time=new(4,0),Cause=first.Cause with {Time=new(4,0)}};
        Assert.Equal(counting,counting.Trigger(declaration,busy,4));
        Assert.Equal(counting,counting.Complete(declaration,new(11),4,out var early));
        Assert.Null(early);
        var ready=counting.Complete(declaration,new(12),4,out var output);
        Assert.Null(output);
        Assert.Equal(ActivationTimerState.Ready(new(1)),ready);
        var atExpiry=first with {Time=new(48,0),Cause=first.Cause with {Time=new(48,0)}};
        var next=ready.Trigger(declaration,atExpiry,4);
        Assert.Equal(12UL,next.StartedTick); Assert.Equal(24UL,next.DueTick);
        Assert.Throws<ArgumentException>(()=>counting.Complete(declaration with {Node=new(99)},new(12),4,out _));
    }

    [Fact]
    public void ScheduledExpiryRetriggerAndPhysicalBypassShareOneChronologicalRouter()
    {
        var nodes = new ActivationNodeDeclaration[]
        {
            new(new(1),new(1),ActivationNodeKind.ContactSource,new(9)),
            new(new(2),new(2),ActivationNodeKind.Timer,default,12),
            new(new(3),new(3),ActivationNodeKind.Timer,default,12,Completion:TimerCompletionPolicy.Rearm,Boundary:TimerBoundary.BeforeNetworks,ElapsedSignal:TimerElapsedSignal.None),
            new(new(4),new(4),ActivationNodeKind.StickyFirstInput,default),
            new(new(5),new(5),ActivationNodeKind.ContactSource,new(10)),
            new(new(6),new(6),ActivationNodeKind.StateLatch,default)
        };
        var network = new ActivationNetwork(nodes,
        [
            new(new(1),new(2),WorkshopSocket.ActivationIn),new(new(1),new(3),WorkshopSocket.ActivationIn),
            new(new(2),new(3),WorkshopSocket.ActivationIn),new(new(2),new(4),WorkshopSocket.ActivationIn),
            new(new(2),new(6),WorkshopSocket.ResetIn),new(new(5),new(4),WorkshopSocket.ActivationIn),
            new(new(5),new(6),WorkshopSocket.SetIn)
        ], contacts: [new(3)]);
        var first = new ContactTriggerRead(new(9),new(1),new(99),1,new(100),0,0f,new(1f));
        var initial = network.Consume(network.Clear(),network.ClearTimers(),[first],new(0),[],memory:network.ClearMemory());
        var scheduled = network.AdvanceScheduled(initial,new(12));
        Assert.Equal(24UL,scheduled.State.Timers[1].DueTick);
        Assert.Equal(ActivationPhase.Clear,scheduled.State.Activations[3].Phase);
        Assert.True(network.ContactClosed(scheduled.State.Activations,scheduled.State.Timers,new(3)));
        Assert.Equal(12UL,initial.Timers[1].DueTick); // candidate did not mutate its source
        var physical = new ContactTriggerRead(new(10),new(5),new(99),1,new(100),47,0f,new(1f));
        var final = network.ConsumeScheduled(scheduled,[physical],new(12),[],[],default,[]);
        Assert.Equal(new ActivationNodeId(5),final.Activations[3].Emitter); // earlier physical bypass wins
        Assert.Equal(ActivationPhase.Clear,final.Activations[5].Phase); // scheduled Reset dominates physical Set
        Assert.Equal(24UL,final.Timers[1].DueTick); // retained due event did not deliver twice
        var retry = network.ConsumeScheduled(network.AdvanceScheduled(initial,new(12)),[physical],new(12),[],[],default,[]);
        Assert.Equal(final.Timers[1],retry.Timers[1]);
        Assert.Equal(final.Activations[3],retry.Activations[3]);
    }

    [Fact]
    public async Task SupplyLossNeverPausesAndRestorationAfterExpiryNeverReplays()
    {
        var phases=await WorkshopIntegrationHarness.ObservePhases(Construction(2f),
            ([],80),([new(new(4),ElectricalEnable.Disabled)],40),
            ([new(new(4),ElectricalEnable.Enabled)],40),([new(new(4),ElectricalEnable.Disabled)],200),
            ([new(new(4),ElectricalEnable.Enabled)],20));
        var counting=phases[0].First(read=>State(read).Phase==ActivationTimerPhase.Counting);
        var due=State(counting).DueTick;
        Assert.All(phases[1],read=>Assert.False(Powered(read)));
        Assert.All(phases[1],read=>Assert.Equal(due,State(read).DueTick));
        Assert.Contains(phases[2],Powered);
        var all=phases.SelectMany(reads=>reads).ToArray();
        Assert.Equal(ActivationTimerPhase.Ready,State(all.Single(read=>read.Tick.Value==due)).Phase);
        Assert.All(phases[4],read=>Assert.True(!Powered(read)&&State(read).Phase==ActivationTimerPhase.Ready));
    }

    [Fact]
    public async Task DistinctPhysicalAndDelayedTriggersProduceTwoWindowsWithoutChangingSourceAdmission()
    {
        var construction=Construction(.1f);
        var parts=construction.Instances.Select(instance=>instance.Id.Value==6
            ? (IWorkshopInstance)WorkshopInput.Generic(PartId.Delay,new(6),-3,2,0,0,0,0,1,new([new(new ParameterId("delay_seconds"),.2f)]))
            : instance).ToArray();
        construction=construction with {Instances=new(parts),Connections=construction.Connections.With(Signal(2,6)).With(Signal(6,3))};
        var (_,reads,_)=await WorkshopIntegrationHarness.ObserveReset(construction,180);
        var windows=reads.Where(read=>State(read).Phase==ActivationTimerPhase.Counting).Select(read=>State(read).StartedTick).Distinct().ToArray();
        Assert.Equal(2,windows.Length); Assert.Equal(windows[0]+24,windows[1]);
        Assert.All(reads.Where(read=>read.Tick.Value>=windows[0]+12&&read.Tick.Value<windows[1]),read=>Assert.False(Powered(read)));
    }


    [Fact]
    public async Task ActualCoordinatorRejectsForgedEarlyRearmBusyRestartAndForeignIdentity()
    {
        var construction=Construction(.2f);
        var (_,reads,_)=await WorkshopIntegrationHarness.ObserveReset(construction,180);
        var first=reads.First(read=>State(read).Phase==ActivationTimerPhase.Counting);
        var next=reads.Single(read=>read.Tick.Value==first.Tick.Value+1);
        await using var device=new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()),new(101,206));
        await using var simulation=new WorkshopSimulation(device,new WorkshopPhysicsEngineTests.StepClock(),WorkshopCadenceSettings.Default(),
            new(1),new WorkshopPhysicsEngineTests.NoInstallation(),GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied,(await simulation.Construct(construction)).Outcome);
        var validate=typeof(WorkshopSimulation).GetMethod("ValidateAdvancedRead",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        validate.Invoke(simulation,[first,next]);
        WorkshopRead Replace(WorkshopRead read,ActivationTimerState state) =>
            read with {Timers=new PhysicsTimerRead(Enumerable.Range(0,read.Timers.Count).Select(i=>read.Timers[i].Node==state.Node?state:read.Timers[i]).ToArray())};
        void Refused(WorkshopRead before,WorkshopRead forged)
        {
            var failure=Assert.Throws<System.Reflection.TargetInvocationException>(()=>validate.Invoke(simulation,[before,forged]));
            Assert.IsType<ArgumentException>(failure.InnerException);
        }
        var beforeStart=reads.Single(read=>read.Tick.Value+1==first.Tick.Value);
        validate.Invoke(simulation,[beforeStart,first]);
        var backTime=ActivationTime.Boundary(new(State(first).StartedTick-1),4);
        var backInput=State(first).Input with {Time=backTime,Cause=State(first).Input.Cause with {Time=backTime}};
        Refused(beforeStart,Replace(first,State(first) with {StartedTick=State(first).StartedTick-1,DueTick=State(first).DueTick-1,Input=backInput}));
        Refused(beforeStart,Replace(first,State(first) with {Input=backInput}));
        Refused(first,Replace(next,ActivationTimerState.Ready(new(3))));
        Refused(first,Replace(next,State(next) with {DueTick=State(next).DueTick+1}));
        Refused(first,Replace(next,State(next) with {StartedTick=State(next).StartedTick+1,DueTick=State(next).DueTick+1}));
        var foreign=Enumerable.Range(0,next.Timers.Count).Select(i=>next.Timers[i].Node.Value==3?next.Timers[i] with {Node=new(4)}:next.Timers[i]).ToArray();
        Refused(first,next with {Timers=new(foreign)});
        var beforeExpiry=reads.Single(read=>read.Tick.Value+1==State(first).DueTick);
        var expiry=reads.Single(read=>read.Tick.Value==State(first).DueTick);
        validate.Invoke(simulation,[beforeExpiry,expiry]);
        Refused(beforeExpiry,Replace(expiry,State(first) with {StartedTick=State(first).DueTick-1,DueTick=State(first).DueTick+23}));
        Assert.Equal(State(first),State(reads.Single(read=>read.Tick.Value==first.Tick.Value)));
    }


    [Fact]
    public async Task ActualDeviceKeepsEarlierPhysicalBypassAheadOfSameBoundaryDelay()
    {
        var original=Construction(.2f,delayed:true);
        var parts=original.Instances.ToArray().Concat(new IWorkshopInstance[]
        {
            WorkshopInput.Generic(PartId.Basketball,new(7),12,5.96f,0,0,0,0,1),
            WorkshopInput.Generic(PartId.ImpactSwitch, new(8),12,1,0,0,0,0,1),
            PlacedParts.Lamp(new(9),15,2,0,0,0,0,1)
        }).ToArray();
        var baseline=original with {Instances=new(parts),Connections=original.Connections.With(Signal(6,9)).With(Signal(8,9))};
        static ActivationLatch Node(WorkshopRead read,ulong id)=>
            Enumerable.Range(0,read.Activations.Count).Select(i=>read.Activations[i]).Single(row=>row.Owner.Value==id);
        // Unchanged physical geometry determines the two real contact boundaries.
        var (_,observed,_)=await WorkshopIntegrationHarness.ObserveReset(baseline,180);
        var first=observed.First(read=>Node(read,2).Phase==ActivationPhase.Latched);
        var second=observed.First(read=>Node(read,8).Phase==ActivationPhase.Latched);
        Assert.True(Node(second,8).EventOrdinal<second.Tick.Value*4, "fixture contact must precede the publication boundary");
        var ticks=second.Tick.Value-first.Tick.Value;
        Assert.InRange(ticks,12UL,1440UL);
        var seconds=(float)ticks/120f;
        Assert.Equal((float)ticks,MathF.Ceiling(seconds*120f));
        parts=parts.Select(part=>part.Id.Value==6
            ? WorkshopInput.Generic(PartId.Delay,new(6),-3,2,0,0,0,0,1,
                new([new(new ParameterId("delay_seconds"),seconds)])) : part).ToArray();
        var (_,reads,_)=await WorkshopIntegrationHarness.ObserveReset(baseline with {Instances=new(parts)},180);
        var due=reads.Single(read=>read.Tick.Value==second.Tick.Value);
        Assert.Equal(due.Tick.Value,Enumerable.Range(0,due.Timers.Count).Select(i=>due.Timers[i]).Single(row=>row.Node.Value==6).DueTick);
        Assert.Equal(ActivationPhase.Clear,Node(reads.Single(read=>read.Tick.Value+1==due.Tick.Value),9).Phase);
        Assert.True(Node(due,9).Emitter == new ActivationNodeId(8), $"Lamp={Node(due,9)}; physical={Node(due,8)}; boundary={due.Tick.Value}; first={first.Tick.Value}");
        Assert.Equal(ActivationOccurrenceKind.Contact,Node(due,9).Kind);
        Assert.True(Node(due,9).EventOrdinal<due.Tick.Value*4);
    }
    [Fact]
    public void ScheduledStageNeedsNoOrientationReadButPhysicalStageRequiresIt()
    {
        var network=new ActivationNetwork([new(new(1),new(1),ActivationNodeKind.OrientationSource,default,Sensor:new(1))],[]);
        var schedule=network.AdvanceScheduled(new(network.Clear(),network.ClearTimers(),network.ClearMemory()),new(1));
        Assert.Equal(ActivationPhase.Clear,schedule.State.Activations[0].Phase);
        var error=Assert.Throws<ArgumentException>(()=>network.ConsumeScheduled(schedule,[],new(1),[],[],default,[]));
        Assert.Equal("Orientation sensor read is missing.",error.Message);
    }

    /// <summary>CAT-033: the declared colliders in order: the body and foot boxes, then the parts that stand proud of the body
    /// (visible = solid): the face panel, the indicator, and the trigger, input and output port sockets.</summary>
    [Fact]
    public void TheHoldTimerDeclaresItsBodyFootAndProudPartsInOrder()
    {
        var colliders = DeclaredPhysics.Colliders(new PartId("hold_timer"));
        Assert.Equal(7, colliders.Count);
        TestBundle.AssertCollider(colliders[0], CuriousContraptions.Gpu.ColliderShapeKind.Box, 0f, new(.7f, .55f, .325f), new(0f, 0f, 0f));
        TestBundle.AssertCollider(colliders[1], CuriousContraptions.Gpu.ColliderShapeKind.Box, 0f, new(.8f, .07f, .425f), new(0f, -.62f, 0f));
        TestBundle.AssertCollider(colliders[2], CuriousContraptions.Gpu.ColliderShapeKind.Box, 0f, new(.59f, .36f, .0225f), new(0f, 0f, .35f));
        TestBundle.AssertCollider(colliders[3], CuriousContraptions.Gpu.ColliderShapeKind.Sphere, .07f, default, new(0f, -.22f, .4f));
        TestBundle.AssertCollider(colliders[4], CuriousContraptions.Gpu.ColliderShapeKind.Sphere, .08f, default, new(0f, .65f, .2f));
        TestBundle.AssertCollider(colliders[5], CuriousContraptions.Gpu.ColliderShapeKind.Sphere, .08f, default, new(-.78f, 0f, 0f));
        TestBundle.AssertCollider(colliders[6], CuriousContraptions.Gpu.ColliderShapeKind.Sphere, .08f, default, new(.78f, 0f, 0f));
    }

    [Fact]
    public void CompiledHoldGeometryMatchesItsDeclaredHousingFootAndProudParts()
    {
        var scene=WorkshopPhysicsCompiler.Compile(new(new(2),WorkshopCadenceSettings.Default(),new(Part())),new(101,206));
        // The housing and foot boxes, then the parts that stand proud of the housing (visible = solid): the face panel, the
        // indicator and the three ports.
        var shapes=scene.Colliders.ToArray().Where(row=>row.Body.Value==3).ToArray();
        Assert.Equal(7,shapes.Length);
        Assert.Contains(shapes,row=>row.Shape==CuriousContraptions.Gpu.ColliderShapeKind.Box && row.HalfExtents==new MetreVector(.7f,.55f,.325f) && row.Pose.Translation==new MetreVector(0f,0f,0f));
        Assert.Contains(shapes,row=>row.Shape==CuriousContraptions.Gpu.ColliderShapeKind.Box && row.HalfExtents==new MetreVector(.8f,.07f,.425f) && row.Pose.Translation==new MetreVector(0f,-.62f,0f));
        Assert.Contains(shapes,row=>row.Shape==CuriousContraptions.Gpu.ColliderShapeKind.Box && row.HalfExtents==new MetreVector(.59f,.36f,.0225f) && row.Pose.Translation==new MetreVector(0f,0f,.35f));
        Assert.Contains(shapes,row=>row.Shape==CuriousContraptions.Gpu.ColliderShapeKind.Sphere && row.Radius==new Metres(.07f) && row.Pose.Translation==new MetreVector(0f,-.22f,.4f));
        foreach(var port in new[]{new MetreVector(0f,.65f,.2f),new MetreVector(-.78f,0f,0f),new MetreVector(.78f,0f,0f)})
            Assert.Contains(shapes,row=>row.Shape==CuriousContraptions.Gpu.ColliderShapeKind.Sphere && row.Radius==new Metres(.08f) && row.Pose.Translation==port);
        foreach(var shape in shapes)
        {
            var material=scene.Materials.ToArray().Single(row=>row.Id==shape.Material);
            Assert.Equal(new Restitution(1f),material.Restitution);
            Assert.Equal(new FrictionCoefficient(.3f),material.Friction);
            Assert.Equal(new LinearSpeed(.1f),material.BounceThreshold);
        }
    }

    internal sealed class GatedRealTransport : IWorkshopGpuTransport
    {
        private readonly CuriousContraptions.Simulation.BrowserGpuTransport _real=new(new CuriousContraptions.Abstractions.EventRingPublication());
        public TaskCompletionSource? Entered, Release;
        public bool FailRead;
        public Task Initialize()=>_real.Initialize();
        public async Task Stage(ReadOnlyMemory<byte> input,WorkshopGpuOperation operation,PhysicsTickInputs inputs)
        {
            if(Release is { } gate && operation==WorkshopGpuOperation.Advance)
            { Entered!.TrySetResult(); await gate.Task; }
            await _real.Stage(input,operation,inputs);
        }
        public WorkshopTransportCandidate Read()
        {
            if(FailRead) { FailRead=false; throw new InvalidOperationException("Test-owned read refusal."); }
            return _real.Read();
        }
        public void PrepareEvents(byte[] batch)=>_real.PrepareEvents(batch);
        public void PublishEvents()=>_real.PublishEvents();
        public void Commit()=>_real.Commit();
        public void Discard()=>_real.Discard();
        public bool DeviceReady()=>_real.DeviceReady();
        public void Dispose()=>_real.Dispose();
    }

    [Fact]
    public async Task QueuedDiscardedAndFailedDueCandidatesRetainCommittedTimerAndDelivery()
    {
        var transport=new GatedRealTransport();
        await using var device=new WorkshopGpuDevice(transport,new(101,206));
        var construction=Construction(.1f,delayed:true);
        var profile=new WorkshopGpuProfile(SimulationCadence.Hz120,PhysicalStepProfile.Canonical480Hz,new(1));
        ulong sequence=1;
        var candidate=await device.Admit(construction,new(1),profile,new(sequence++));
        var committed=candidate.Read; device.Commit(candidate.Sequence); device.Discard(candidate.Sequence);
        var controls=new WorkshopTickControls([ElectricalEnable.Enabled]);
        ulong due=0;
        for(var tick=0;tick<100;tick++)
        {
            candidate=await device.Advance(committed,new(sequence++),controls);
            committed=candidate.Read; device.Commit(candidate.Sequence); device.Discard(candidate.Sequence);
            var delay=Enumerable.Range(0,committed.Timers.Count).Select(i=>committed.Timers[i]).Single(row=>row.Node.Value==6);
            if(delay.Phase==ActivationTimerPhase.Counting) due=delay.DueTick;
            if(due!=0&&committed.Tick.Value+1==due) break;
        }
        Assert.NotEqual(0UL,due); Assert.Equal(ActivationTimerPhase.Ready,State(committed).Phase);
        transport.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first=device.Advance(committed,new(sequence++),controls).AsTask();
        await transport.Entered.Task;
        var queued=device.Advance(committed,new(sequence++),controls).AsTask();
        Assert.False(queued.IsCompleted);
        transport.Release.SetResult();
        var discarded=await first;
        Assert.Equal(ActivationTimerPhase.Counting,State(discarded.Read).Phase);
        Assert.Equal(due,State(discarded.Read).StartedTick);
        device.Discard(discarded.Sequence);
        var retried=await queued;
        Assert.Equal(State(discarded.Read),State(retried.Read));
        device.Discard(retried.Sequence);
        transport.Release=null; transport.FailRead=true;
        await Assert.ThrowsAsync<InvalidOperationException>(async()=>await device.Advance(committed,new(sequence++),controls));
        var recovered=await device.Advance(committed,new(sequence++),controls);
        Assert.Equal(State(discarded.Read),State(recovered.Read));
        Assert.True(Powered(recovered.Read));
        device.Commit(recovered.Sequence); device.Discard(recovered.Sequence);
    }

}

[Collection<NativeSceneCollectionDefinition>]
public sealed class HoldTimerPresentationTests(NativeSceneFixture godot)
{
    private static WorkshopResponse Response(WorkshopRead read, ulong publication) =>
        new(default,WorkshopResponseKind.Read,new(WorkshopCommandOutcome.Applied,WorkshopRejection.None),
            WorkshopSimulationPhase.Running,read,new(101,206),new(publication),new(1),new(1),new(1),new(read.Tick.Value));

    [Fact]
    public async Task ActualCommittedWindowDrivesAnchoredBarAndIndependentLampAndHiddenCatchUp()
    {
        var construction=HoldTimerTests.Construction(.2f,supply:false);
        var (_,reads,_)=await WorkshopIntegrationHarness.ObserveReset(construction,180);
        var first=reads.First(read=>HoldTimerTests.State(read).Phase==ActivationTimerPhase.Counting);
        var due=HoldTimerTests.State(first).DueTick;
        var expired=reads.Single(read=>read.Tick.Value==due);
        var evaluator=new WorkshopDeclaredPresentation(construction,Response(first,1));
        var registry=new PartRegistry(); registry.Load();
        var part=registry.Create(HoldTimerTests.Kind); var untouched=registry.Create(HoldTimerTests.Kind);
        try
        {
            godot.Tree.Root.AddChild(part); godot.Tree.Root.AddChild(untouched);
            var bar=part.GetNode<Node3D>("Visual/Remaining");
            var indicator=part.GetNode<MeshInstance3D>("Visual/Indicator");
            var material=Assert.IsType<StandardMaterial3D>(indicator.MaterialOverride);
            var time=first.Capture!.Value.Time.Value/1_000_000_000d+.11;
            foreach(var sample in evaluator.Advance(AnimationTestTime.Seconds(time))) if(sample.Owner.Value==3) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
            Assert.True(bar.Visible); Assert.Equal(-.5f,bar.Position.X); Assert.Equal(1f,bar.Scale.X);
            Assert.Equal(.5f,bar.GetNode<Node3D>("Bar").Position.X);
            Assert.Equal(new Color("#f7cb52"),material.AlbedoColor);
            Assert.False(untouched.GetNode<Node3D>("Visual/Remaining").Visible); // no binding delivery: positive state is absent
            var target=WorkshopDeclaredPresentation.Targets(construction).First(row=>row.Owner.Value==3).Target;
            evaluator.SetVisible(target,AnimationProperty.UniformScale,false);
            evaluator.Accept(Response(expired,2));
            Assert.DoesNotContain(evaluator.Advance(AnimationTestTime.Seconds(time+.2)),sample=>sample.Owner.Value==3);
            evaluator.SetVisible(target,AnimationProperty.UniformScale,true);
            foreach(var sample in evaluator.Advance(AnimationTestTime.Seconds(time+.21))) if(sample.Owner.Value==3) part.ApplyDeclaredProperty(sample.Binding,sample.Value);
            Assert.False(bar.Visible); Assert.Equal(.001f,bar.Scale.X); Assert.Equal(-.5f,bar.Position.X);
            Assert.Equal(new Color("#556573"),material.AlbedoColor);
            Assert.Throws<ArgumentException>(()=>evaluator.Accept(Response(first,3)));
            Assert.False(bar.Visible); Assert.Equal(new Color("#556573"),material.AlbedoColor);
        }
        finally { part.Free(); untouched.Free(); }
    }
}
