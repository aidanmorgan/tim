using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class DelayPresentationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Delay=new("delay"),Hold=new("hold_timer"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected Delay display failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Color Colour(SceneTimerColour lamp)=>((StandardMaterial3D)lamp.Target.MaterialOverride).AlbedoColor;
    private static double Progress(MachineWorld world,DelayPart delay)
    {
        using var read=world.ReadCommittedPoses();
        return read.ReadScalar(PoseSample.Current,new(world.PhysicsAssembly.QueryOwnerId(new(delay,MachinePart.RootBody)),
            DelayPart.ProgressOutput)).Value;
    }
    private static void AssertHand(Node3D hand,Transform3D baseline,double progress)
    {
        var expected=new Transform3D(baseline.Basis*new Basis(Vector3.Back,(float)Math.IEEERemainder(-Math.Tau*progress,Math.Tau)),baseline.Origin);
        Assert.Equal(expected,hand.Transform);
    }

    [Theory]
    [InlineData(.1f,1)]
    [InlineData(.1f,4)]
    [InlineData(1.13f,1)]
    [InlineData(1.13f,4)]
    public void CommittedProgressAndPhaseOwnDisplayThroughFailurePauseHideCompletionResetSave(float duration,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var delay=(DelayPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Delay.Value,Position=[0,5,0],
                Properties=new(){[PartParameterName.Of(DelayParameter.DelaySeconds)]=duration}});
            var hold=(HoldTimerPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Hold.Value,Position=[4,5,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[-4,5,0]});world.AttachPart(probe);
            Assert.True(world.Connect(delay,SocketId.ActivationOut,hold,SocketId.ActivationIn,ConnectionDomain.Activation));
            var hand=Assert.Single(delay.ScalarRotations).Target;var lamp=Assert.Single(delay.TimerColours);
            var baseline=hand.Transform;var pose=delay.Transform;var saved=Saved(world);
            world.Start();world.Step();world.PresentFrame(0,1);
            AssertHand(hand,baseline,0);Assert.Equal(lamp.Palette.Ready,Colour(lamp));
            world.Activate(delay);
            world.PresentFrame(.5,1); // Accepted live trigger has not committed yet.
            Assert.Equal(lamp.Palette.Ready,Colour(lamp));AssertHand(hand,baseline,0);
            world.Step();var due=delay.DueTick;
            Assert.Equal(lamp.Palette.Ready,Colour(lamp));Assert.Equal(baseline,hand.Transform);
            world.Running=false;
            var physical=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            world.PresentFrame(0,1);
            Assert.Equal(lamp.Palette.Counting,Colour(lamp));AssertHand(hand,baseline,Progress(world,delay));
            var displayed=hand.Transform;
            world.PresentFrame(100,1);
            Assert.Equal(displayed,hand.Transform);Assert.Equal(tick,world.Ticks);
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Running=true;
            var revision=world.ControlRevision;var progress=Progress(world,delay);
            probe.FailAt=probe.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(revision,world.ControlRevision);Assert.Equal(progress,Progress(world,delay));
            world.PresentFrame(.1,1);Assert.Equal(displayed,hand.Transform);Assert.Equal(lamp.Palette.Counting,Colour(lamp));
            probe.FailAt=0;delay.Visible=false;
            for(var i=0;i<3;i++)world.Step();
            world.PresentFrame(.1,1);Assert.Equal(displayed,hand.Transform);
            delay.Visible=true;world.PresentFrame(0,1);AssertHand(hand,baseline,Progress(world,delay));
            while(world.Ticks<due)world.Step();
            world.PresentFrame(0,1);displayed=hand.Transform;
            probe.FailAt=probe.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            world.PresentFrame(.1,1);
            Assert.Equal(displayed,hand.Transform);Assert.Equal(lamp.Palette.Counting,Colour(lamp));
            Assert.Equal(SimulationTimerPhase.Ready,hold.State);
            probe.FailAt=0;world.Step();
            Assert.Equal(lamp.Palette.Counting,Colour(lamp)); // Physics cannot write the material.
            Assert.Equal(SimulationTimerPhase.Counting,hold.State);
            world.PresentFrame(0,1);Assert.Equal(lamp.Palette.Finished,Colour(lamp));AssertHand(hand,baseline,1);
            world.Activate(delay);world.Step();world.PresentFrame(0,1);
            Assert.Equal(due,delay.DueTick);Assert.Equal(lamp.Palette.Finished,Colour(lamp));
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(DelayPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(baseline,Assert.Single(restored.ScalarRotations).Target.Transform);
            Assert.Equal(lamp.Palette.Ready,Colour(Assert.Single(restored.TimerColours)));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.Step();world.PresentFrame(.1,1);
            restored=(DelayPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            AssertHand(Assert.Single(restored.ScalarRotations).Target,baseline,0);
            Assert.Equal(lamp.Palette.Ready,Colour(Assert.Single(restored.TimerColours)));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    public enum BindingFault { Unit, Slot, Owner, Range, Angle, Axis, Physical, Duplicate, TimerOwner, TimerSlot, DuplicateColour }
    private partial class InvalidDelay : DelayPart
    {
        public BindingFault Fault;
        public override IReadOnlyList<SceneScalarRotation> ScalarRotations
        {
            get
            {
                var rotation=base.ScalarRotations[0];
                return Fault switch
                {
                    BindingFault.Unit=>[rotation with {Unit=ScalarUnit.GameIrradiance}],
                    BindingFault.Slot=>[rotation with {Source=new(this,new(99))}],
                    BindingFault.Owner=>[rotation with {Source=default}],
                    BindingFault.Range=>[rotation with {InputTo=0}],
                    BindingFault.Angle=>[rotation with {AngleTo=double.PositiveInfinity}],
                    BindingFault.Axis=>[rotation with {Axis=(AnimationRotationAxis)(-1)}],
                    BindingFault.Physical=>[rotation with {Target=this}],
                    BindingFault.Duplicate=>[rotation,rotation],
                    BindingFault.TimerOwner or BindingFault.TimerSlot or BindingFault.DuplicateColour=>[rotation],
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                };
            }
        }
        public override IReadOnlyList<SceneTimerColour> TimerColours
        {
            get
            {
                var lamp=base.TimerColours[0];
                return Fault switch
                {
                    BindingFault.TimerOwner=>[lamp with {Source=default}],
                    BindingFault.TimerSlot=>[lamp with {Source=new(this,new TimerSlot())}],
                    BindingFault.DuplicateColour=>[lamp,lamp],
                    BindingFault.Unit or BindingFault.Slot or BindingFault.Owner or BindingFault.Range or
                        BindingFault.Angle or BindingFault.Axis or BindingFault.Physical or BindingFault.Duplicate=>[lamp],
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                };
            }
        }
    }
    [Theory]
    [InlineData(BindingFault.Unit)]
    [InlineData(BindingFault.Slot)]
    [InlineData(BindingFault.Owner)]
    [InlineData(BindingFault.Range)]
    [InlineData(BindingFault.Angle)]
    [InlineData(BindingFault.Axis)]
    [InlineData(BindingFault.Physical)]
    [InlineData(BindingFault.Duplicate)]
    [InlineData(BindingFault.TimerOwner)]
    [InlineData(BindingFault.TimerSlot)]
    [InlineData(BindingFault.DuplicateColour)]
    public void UnsupportedBindingsRejectWithoutChangingConstruction(BindingFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var delay=new InvalidDelay {Definition=world.Registry.Definitions[Delay.Value],Fault=fault};
            delay.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Delay.Value,Position=[0,5,0]});world.AttachPart(delay);
            var saved=Saved(world);Assert.Throws<ArgumentException>(world.Start);
            Assert.False(world.Running);Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    public enum PublicationFault { ScalarUnit, TimerIdentity }
    [Theory]
    [InlineData(PublicationFault.ScalarUnit)]
    [InlineData(PublicationFault.TimerIdentity)]
    public void ForeignFeedbackRejectsBeforeDisplayAndValidCommitCanRetry(PublicationFault fault)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var delay=(DelayPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Delay.Value,Position=[0,5,0]});
            var hand=Assert.Single(delay.ScalarRotations).Target;var baseline=hand.Transform;var lamp=Assert.Single(delay.TimerColours);
            world.Start();SceneAnimationRun run;PoseReadStamp stamp;ScalarRead[] scalars;ElectricalInputRead[] electrical;SimulationTimerState[] timers;
            var timerId=world.ReadTimer(new(delay,DelayPart.Countdown)).Id;
            using(var seed=world.ReadCommittedPoses())
            {
                run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),
                    new Dictionary<SceneTimerKey,SimulationTimerId>{{new(delay,DelayPart.Countdown),timerId}},new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
                stamp=seed.Stamp(PoseSample.Current);scalars=new ScalarRead[seed.ScalarCount];seed.CopyScalars(PoseSample.Current,scalars);
                electrical=new ElectricalInputRead[seed.ElectricalInputCount];seed.CopyElectricalInputs(PoseSample.Current,electrical);
                timers=new SimulationTimerState[seed.TimerCount];seed.CopyTimers(PoseSample.Current,timers);
            }
            scalars[0]=scalars[0] with {Value=.5};
            timers[0]=new(timerId,SimulationTimerPhase.Counting,0,12);
            switch(fault)
            {
                case PublicationFault.ScalarUnit:scalars[0]=scalars[0] with {Unit=ScalarUnit.GameIrradiance};break;
                case PublicationFault.TimerIdentity:timers[0]=timers[0] with {Id=new(99)};break;
                default:throw new ArgumentOutOfRangeException(nameof(fault));
            }
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var foreign=new CommittedPoseBuffer(stamp,bodies,[],electrical,scalars,timers,[]);
            foreign.BeginWrite(0);foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[],electrical,scalars,timers,[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.1);Assert.Equal(baseline,hand.Transform);Assert.Equal(lamp.Palette.Ready,Colour(lamp));
            world.Activate(delay);world.Step();using(var read=world.ReadCommittedPoses())run.Publish(read);
            run.Present(0);AssertHand(hand,baseline,Progress(world,delay));Assert.Equal(lamp.Palette.Counting,Colour(lamp));
            run.Remove();Assert.Equal(baseline,hand.Transform);Assert.Equal(lamp.Palette.Ready,Colour(lamp));
            Assert.Throws<InvalidOperationException>(()=>run.Present(0));
        }
        finally{world.Free();}
    }

    [Fact]
    public void TimerPaletteRejectsUndefinedPhaseAndMalformedColours()
    {
        var palette=new TimerColourPalette(Colors.Black,Colors.Gray,Colors.White);
        Assert.Equal(Colors.Black,palette.Read(SimulationTimerPhase.Ready));
        Assert.Equal(Colors.Gray,palette.Read(SimulationTimerPhase.Counting));
        Assert.Equal(Colors.White,palette.Read(SimulationTimerPhase.Finished));
        Assert.Throws<ArgumentOutOfRangeException>(()=>palette.Read((SimulationTimerPhase)(-1)));
        Assert.Throws<ArgumentException>(()=>new TimerColourPalette(new(float.NaN,0,0),Colors.Gray,Colors.White));
        Assert.Throws<ArgumentException>(()=>new TimerColourPalette(Colors.Black,new(0,0,0,2),Colors.White));
        Assert.Throws<ArgumentException>(()=>new TimerColourPalette(Colors.Black,Colors.Gray,new(0,float.PositiveInfinity,0)));
    }
}
