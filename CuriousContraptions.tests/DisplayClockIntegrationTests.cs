using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class DisplayClockIntegrationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private readonly record struct ResourcePath(string Value);
    private readonly record struct PuzzleId(string Value);
    private static readonly CatalogueId Torch=new("flashlight"),Ball=new("ball"),Battery=new("battery");
    private static readonly ResourcePath Campaign=new("res://content/puzzles.json");
    private static readonly PuzzleId Counterweight=new("counterweight");

    private partial class Probe : BatteryPart
    {
        public Action? Observe;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }

    [Fact]
    public void FailedTickPreservesTheDisplayedFrameAndHostCallbackUsesTheSameClock()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});
            world.AttachPart(probe);world.Start();world.Step();world._Process(0);
            var phase=Engine.GetPhysicsInterpolationFraction();
            Assert.Equal(MachineWorld.Tick*phase,world.DisplaySimulationTime);
            world.PresentFrame(0,.75);
            var shown=ball.Transform;var time=world.DisplaySimulationTime;
            var state=world.Physics.Capture().BodyStates.ToArray();
            var observations=0;
            probe.Observe=()=>
            {
                if(++observations==MachineWorld.Substeps)throw new InvalidOperationException("Injected final-substep failure.");
            };
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            Assert.Equal(shown,ball.Transform);Assert.Equal(time,world.DisplaySimulationTime);
            Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());
            world.PresentFrame(0,.75);
            Assert.Equal(shown,ball.Transform);Assert.Equal(time,world.DisplaySimulationTime);
            probe.Observe=null;world.Step();world.PresentFrame(0,.5);
            Assert.Equal(MachineWorld.Tick*1.5,world.DisplaySimulationTime);
        }
        finally {world.Free();}
    }

    [Fact]
    public void BodyAndConeShareTheClockAcrossPauseResumeBurstAndReset()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var torch=(FlashlightPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Torch.Value,Position=[0,5,0]});
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Ball.Value,Position=[1.6f,5,.3f],InitialVelocity=[0,0,1]});
            torch.Active=true;var source=torch.LightSource!.Value;
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var initial=LightConeVisual.Sample(world,torch,source,1);
            world.Step();
            var state=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(1d/60,.5);
            Assert.True(world.ProcessPriority<torch.ProcessPriority);
            Assert.Equal(MachineWorld.Tick*.5,world.DisplaySimulationTime);
            var rays=LightConeVisual.Sample(world,torch,source,1);
            Assert.NotEqual(initial,rays);
            var ballId=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id;
            var torchId=world.PhysicsAssembly.Body(new(torch,MachinePart.RootBody)).Id;
            using(var read=world.ReadCommittedPoses())
            {
                var pose=read.SampleAcceptedPose(ballId,world.DisplaySimulationTime).ToScene();
                Assert.Equal(pose,ball.Transform);
                var emitter=read.SampleAcceptedPose(torchId,world.DisplaySimulationTime).ToScene();
                foreach(var ray in rays)
                {
                    var expected=read.Trace(world.DisplaySimulationTime,TraceMedium.Light,
                        SceneGeometryAdapter.CaptureVector(emitter*source.At),
                        SceneGeometryAdapter.CaptureVector((emitter.Basis*ray.Direction).Normalized()),source.Range,torchId);
                    Assert.Equal((float)expected,ray.Distance);
                }
                Assert.Throws<InvalidOperationException>(()=>world.Step());
                Assert.Equal(MachineWorld.Tick*.5,world.DisplaySimulationTime);
            }
            Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Running=false;world.PresentFrame(0,0);
            Assert.Equal(MachineWorld.Tick,world.DisplaySimulationTime);
            world.Running=true;world.PresentFrame(0,0);
            Assert.Equal(MachineWorld.Tick,world.DisplaySimulationTime);
            world.Step();world.Step();world.PresentFrame(0,.25);
            Assert.Equal(MachineWorld.Tick*2.25,world.DisplaySimulationTime);
            world.Restore();
            Assert.Equal(0,world.DisplaySimulationTime);
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();world.PresentFrame(0,.5);
            Assert.Equal(0,world.DisplaySimulationTime);
            Assert.Equal(new Vector3(1.6f,5,.3f),world.FindPart(FixtureParts.Id(FixturePartId.Second))!.Position);
        }
        finally {world.Free();}
    }

    [Fact]
    public void RopeKnotsAndPhysicalParentsUseOneInteriorTimestamp()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle=MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(Campaign.Value))
                .Single(p=>p.Id==Counterweight.Value);
            var data=MachineCodec.Clone(puzzle.CreateMachine());
            data.Parts.AddRange(puzzle.Solution);data.Connections=puzzle.SolutionConnections;
            world.LoadMachine(data);world.Start();world.Step();
            var path=Assert.Single(world.Ropes);
            var rope=new RopeVisual {Path=path,World=world};world.AddChild(rope);
            world.PresentFrame(0,.5);rope._Process(0);
            var knots=rope.GetChildren().OfType<MeshInstance3D>().Where(m=>m.Mesh is SphereMesh).ToArray();
            Assert.Equal(path.Sockets.Count,knots.Length);
            using var read=world.ReadCommittedPoses();
            for(var i=0;i<knots.Length;i++)
            {
                var socket=path.Sockets[i];
                var id=world.PhysicsAssembly.Body(new(socket.Part,MachinePart.RootBody)).Id;
                var pose=read.SampleAcceptedPose(id,world.DisplaySimulationTime).ToScene();
                Assert.Equal(pose*socket.Port.LocalPosition,knots[i].Position);
                Assert.Equal(pose,socket.Part.Transform);
            }
        }
        finally {world.Free();}
    }

    [Fact]
    public void TickBurstsDoNotSubmitPosesAndDiagnosticsReadAuthoritativeState()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            Assert.Throws<InvalidOperationException>(()=>PlaytestPart.CaptureRuntime(world,ball));
            var construction=ball.Transform;world.Start();
            for(var i=0;i<5;i++)
            {
                world.Step();
                Assert.Equal(construction,ball.Transform);Assert.Equal(0,world.DisplaySimulationTime);
            }
            var diagnostic=PlaytestPart.CaptureRuntime(world,ball);
            Assert.InRange(Math.Abs(diagnostic.Position[0]-5*MachineWorld.Tick),0,1e-8);
            var committed=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(0,.5);
            Assert.InRange(Math.Abs(ball.Position.X-4.5*MachineWorld.Tick),0,1e-8);
            Assert.Equal(committed,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(diagnostic.Position,PlaytestPart.CaptureRuntime(world,ball).Position);
        }
        finally {world.Free();}
    }

#if PLAYTEST
    [Fact]
    public void HostRecordsFramesSeparatelyFromTicksAndRetainsFailedFrameEvidence()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            world.Start();world.Step();world.Step();
            var ticks=world.DrainPerformance();
            Assert.DoesNotContain(ticks.Samples,s=>s.Stage==PerformanceStage.GameplayFrame);
            Assert.Equal(2, ticks.Samples.Count(s=>s.Stage==PerformanceStage.GameplayTick));
            world.PresentFrame(0,.25);world.PresentFrame(0,.5);
            var frames=world.DrainPerformance();
            Assert.Empty(frames.Counters);Assert.Equal(8,frames.Samples.Length);
            Assert.All(frames.Samples,s=>
            {
                Assert.True(PerformanceTopology.IsFrameStage(s.Stage));
                Assert.Equal(s.Stage==PerformanceStage.SceneSubmission?4:1,s.Calls);Assert.Equal(2,s.Tick);
            });
            var report=PerformanceTools.PerformanceAudit.Analyze(PerformanceTools.PerformanceScenario.SparseCatalogue,
                [PlaytestPerformanceBatch.Capture(ticks),PlaytestPerformanceBatch.Capture(frames)],2);
            var run=Assert.Single(report.Runs);
            Assert.Equal(PerformanceTools.TickRecordCoverage.Complete,run.Coverage);
            Assert.Equal(2,run.ObservedTicks);Assert.Equal(2,run.ObservedFrames);
            using(var held=world.ReadCommittedPoses())
                Assert.Throws<InvalidOperationException>(()=>world.PresentFrame(0,.75));
            var failed=world.DrainPerformance().Samples;
            Assert.Equal(2,failed.Length);
            Assert.All(failed,s=>Assert.Equal(PerformanceOutcome.Failed,s.Outcome));
            Assert.Contains(failed,s=>s.Stage==PerformanceStage.GameplayFrame);
            Assert.Contains(failed,s=>s.Stage==PerformanceStage.PhysicalPresentation);
        }
        finally {world.Free();}
    }
#endif
}
