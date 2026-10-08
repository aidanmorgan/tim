using System.Text.Json;
using CuriousContraptions.Presentation;
using Godot;
using CuriousContraptions.Bridge;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneAnimationRunTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private readonly record struct InstanceId(string Value);
    private static readonly CatalogueId FanCatalogue = new("fan");
    private static readonly InstanceId FanInstance = new("animation-fan");
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static FanPart AddFan(MachineWorld world, bool powered) =>
        (FanPart)world.AddPart(new()
        {
            Id = FanInstance.Value, Kind = FanCatalogue.Value,
            Properties = new() { [PartParameterName.Of(FanParameter.Powered)] = powered ? 1 : 0 }
        });
    private static string Saved(MachineWorld world) =>
        JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);

    [Fact]
    public void FanTicksDoNotWriteArtworkAndRenderDoesNotChangePhysicalState()
    {
        var world = World();
        try
        {
            var fan = AddFan(world, true);
            var rotor = Assert.Single(fan.RotationAnimations).Target;
            var baseline = rotor.Transform;
            var saved = Saved(world);
            world.Start();
            for (var i = 0; i < 12; i++) world.Step();
            Assert.Equal(baseline, rotor.Transform);
            var physical = world.StateSignature();
            world._Process(.1);
            var expected = baseline.Basis * new Basis(Vector3.Right, (float)(12 * (double)MachineWorld.Tick * 18));
            Assert.InRange((rotor.Basis.Y - expected.Y).Length(), 0, 1e-6f);
            Assert.Equal(physical, world.StateSignature());
            var shown = rotor.Transform;
            world.Running = false;
            world._Process(2);
            Assert.Equal(shown, rotor.Transform);
            world.Restore();
            Assert.Equal(saved, Saved(world));
            var restored = Assert.IsType<FanPart>(Assert.Single(world.Parts));
            Assert.Equal(baseline, Assert.Single(restored.RotationAnimations).Target.Transform);
            world._Process(.5);
            Assert.Equal(saved, Saved(world));
            world.Start();
            world.Step();
            world._Process(.01);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void InactiveControlAndLateActivationUsePublishedBoundary()
    {
        var world = World();
        try
        {
            var fan = AddFan(world, false);
            var rotor = Assert.Single(fan.RotationAnimations).Target;
            var baseline = rotor.Transform;
            world.Start();
            for (var i = 0; i < 6; i++) world.Step();
            world._Process(.05);
            Assert.Equal(baseline, rotor.Transform);
            fan.Active = true;
            world._Process(.05);
            Assert.Equal(baseline, rotor.Transform); // No feedback publication yet.
            world.Step();
            world._Process(.01);
            Assert.Equal(baseline, rotor.Transform); // Exact activation boundary.
            world.Step();
            world._Process(.01);
            var expected = new Basis(Vector3.Right, MachineWorld.Tick * 18);
            Assert.InRange((rotor.Basis.Y - expected.Y).Length(), 0, 1e-6f);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RemovedRunRejectsCallsAndRestoresItsSceneBaseline()
    {
        var world = World();
        try
        {
            var fan = AddFan(world, true);
            var rotor = Assert.Single(fan.RotationAnimations).Target;
            var baseline = rotor.Transform;
            world.Start();
            SceneAnimationRun run;
            using(var seed=world.ReadCommittedPoses())run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            world.Step();
            using(var read=world.ReadCommittedPoses())run.Publish(read);
            Assert.Equal(1, run.Present(.1).TransformWrites);
            Assert.NotEqual(baseline, rotor.Transform);
            using(var repeated=world.ReadCommittedPoses())
                Assert.Throws<ArgumentException>(() => run.Publish(repeated));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.Present(double.NaN));
            run.Remove();
            Assert.Equal(baseline, rotor.Transform);
            Assert.Throws<InvalidOperationException>(() => run.Present(.1));
            using(var read=world.ReadCommittedPoses())
                Assert.Throws<InvalidOperationException>(() => run.Publish(read));
            Assert.Throws<InvalidOperationException>(() => run.Remove());
        }
        finally { world.Free(); }
    }


    [Fact]
    public void FeedbackUsesCommittedActivityAndRejectsSkippedOrForeignRevisions()
    {
        var world = World();
        try
        {
            var fan = AddFan(world, false);
            var rotor = Assert.Single(fan.RotationAnimations).Target;
            var baseline = rotor.Transform;
            world.Start();
            SceneAnimationRun run;
            using(var seed=world.ReadCommittedPoses())run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            fan.Active=true;
            world.Step();
            fan.Active=false; // Unpublished mutation must not change the committed activation.
            using(var read=world.ReadCommittedPoses())run.Publish(read);
            run.Present(.01);
            Assert.Equal(baseline,rotor.Transform);
            fan.Active=true;
            world.Step();
            fan.Active=false;
            using(var read=world.ReadCommittedPoses())
            {
                run.Publish(read);
                Assert.Throws<ArgumentException>(()=>run.Publish(read));
            }
            run.Present(.01);
            var expected=new Basis(Vector3.Right,MachineWorld.Tick*18);
            Assert.InRange((rotor.Basis.Y-expected.Y).Length(),0,1e-6f);
            var shown=rotor.Transform;
            world.Step();
            world.Step();
            using(var skipped=world.ReadCommittedPoses())
                Assert.Throws<ArgumentException>(()=>run.Publish(skipped));
            run.Present(.01);
            Assert.Equal(shown,rotor.Transform);
            var declarations=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var foreign=new CommittedPoseBuffer(new(new(world.ControlGeneration.Value+1),new(2),2*(double)MachineWorld.Tick),declarations,[],[],[],[],[]);
            foreign.BeginWrite(0);
            foreign.Stage(new(new(world.ControlGeneration.Value+1),new(3),3*(double)MachineWorld.Tick),declarations,[],[],[],[],[]);
            foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.01);
            Assert.Equal(shown,rotor.Transform);
            run.Remove();
        }
        finally { world.Free(); }
    }



    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(0)]
    public void StopBetweenFramesHoldsCommittedPhase(int renderEvery)
    {
        var world=World();
        try
        {
            var fan=AddFan(world,true);
            var rotor=Assert.Single(fan.RotationAnimations).Target;
            var baseline=rotor.Transform;
            var saved=Saved(world);
            world.Start();
            for(var tick=1;tick<=6;tick++)
            {
                if(tick==6)fan.Active=false;
                world.Step();
                if(renderEvery>0&&tick%renderEvery==0)world.PresentFrame(.01,1);
            }
            world.PresentFrame(.01,1);
            var expected=baseline.Basis*new Basis(Vector3.Right,(float)(6*(double)MachineWorld.Tick*18));
            Assert.InRange((rotor.Basis.Y-expected.Y).Length(),0,1e-6f);
            var held=rotor.Transform;
            world.Step();world.PresentFrame(.1,1);
            Assert.Equal(held,rotor.Transform);
            world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    private partial class ActivityProbeFan : FanPart
    {
        public Action? Observe;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void FailedTickCannotActivateArtworkAndRetryUsesCommitTime(int failingSubstep)
    {
        var world=World();
        try
        {
            var fan=new ActivityProbeFan {Definition=world.Registry.Definitions[FanCatalogue.Value]};
            fan.Configure(new(){Id=FanInstance.Value,Kind=FanCatalogue.Value,
                Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=0}});
            world.AttachPart(fan);
            var rotor=Assert.Single(fan.RotationAnimations).Target;
            var baseline=rotor.Transform;
            var saved=Saved(world);
            world.Start();
            var calls=0;
            fan.Observe=()=>
            {
                fan.Active=true;
                if(++calls==failingSubstep)throw new InvalidOperationException("Injected animation feedback failure.");
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            world.PresentFrame(.1,1);
            Assert.Equal(baseline,rotor.Transform);
            Assert.False(fan.Active);
            fan.Observe=()=>fan.Active=true;
            world.Step();
            world.PresentFrame(.1,1);
            Assert.Equal(baseline,rotor.Transform);
            world.Step();
            world.PresentFrame(.1,1);
            var expected=new Basis(Vector3.Right,MachineWorld.Tick*18);
            Assert.InRange((rotor.Basis.Y-expected.Y).Length(),0,1e-6f);
            world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    private partial class PhysicalTargetFan : FanPart
    {
        public override IReadOnlyList<SceneRotationAnimation> RotationAnimations =>
            [base.RotationAnimations[0] with { Target=this }];
    }

    [Fact]
    public void FunctionalRootCannotBeBoundAsCosmeticArtwork()
    {
        var world=World();
        try
        {
            var fan=new PhysicalTargetFan {Definition=world.Registry.Definitions[FanCatalogue.Value]};
            fan.Configure(new(){Id=FanInstance.Value,Kind=FanCatalogue.Value});
            world.AttachPart(fan);
            Assert.Throws<ArgumentException>(world.Start);
        }
        finally {world.Free();}
    }

    [Fact]
    public void LoadAndConstructionRemovalLeaveNoAnimationTargets()
    {
        var world = World();
        try
        {
            var fan = AddFan(world, true);
            world.RemovePart(fan);
            AddFan(world, true);
            world.Start();
            world.Step();
            world._Process(.1);
            world.LoadMachine(new());
            Assert.Empty(world.Parts);
            world._Process(.1);
            world.Start();
            world.Step();
            world._Process(.1);
        }
        finally { world.Free(); }
    }
}
