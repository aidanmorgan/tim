using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpactRuntimeCheckpointTests(NativeSceneFixture godot)
{
    public enum ImpactKind { Lever, Spring, Bumper }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId LeverCatalogue=new("impact_lever"),SpringCatalogue=new("spring"),
        BumperCatalogue=new("bumper"),DetectorCatalogue=new("ball_detector"),TimerCatalogue=new("hold_timer"),
        BallCatalogue=new("ball"),ProbeCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observed;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(ImpactKind.Lever)]
    [InlineData(ImpactKind.Spring)]
    [InlineData(ImpactKind.Bumper)]
    public void FailedPhysicalImpactRestoresCounterAndRetryPublishesOnce(ImpactKind kind)
    {
        var world=World();
        try
        {
            MachinePart part;
            Func<int> count;
            switch(kind)
            {
                case ImpactKind.Lever:
                    var lever=new ImpactLeverPart();
                    Attach(world,lever,LeverCatalogue,FixturePartId.First,new(0,6,0));
                    part=lever;count=()=>lever.ImpactCount;break;
                case ImpactKind.Spring:
                    var spring=new SpringPart();
                    Attach(world,spring,SpringCatalogue,FixturePartId.First,new(0,6,0));
                    part=spring;count=()=>spring.HitCount;break;
                case ImpactKind.Bumper:
                    var bumper=new BumperPart();
                    Attach(world,bumper,BumperCatalogue,FixturePartId.First,new(0,6,0));
                    part=bumper;count=()=>bumper.HitCount;break;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
            var ball=new BallPart();
            Attach(world,ball,BallCatalogue,FixturePartId.Second,new(kind==ImpactKind.Lever?-1.2f:0,7,0));
            ball.InitialVelocity=new(0,-4,0);
            var probe=new Probe();
            Attach(world,probe,ProbeCatalogue,FixturePartId.Third,new(-5,6,0));
            var construction=Saved(world);
            world.Start();
            var failed=false;
            for(var tick=0;tick<60&&!failed;tick++)
            {
                var before=count();var active=part.Active;
                var physics=world.Physics.Capture();
                var events=world.Events.ToArray();
                var expected=before;
                probe.Observed=()=>
                {
                    if(count()==before)return;
                    expected=count();failed=true;
                    throw new InvalidOperationException();
                };
                try {world.Step();}
                catch(InvalidOperationException) when(failed)
                {
                    Assert.Equal(before,count());Assert.Equal(active,part.Active);
                    Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
                    Assert.Equal(events,world.Events.ToArray());
                    if(part is BumperPart rolledBack)
                    {
                        world.PresentFrame(.08,1);
                        Assert.Equal(1,world.ReadOccurrenceFeedback(new(rolledBack,BumperPart.ImpactOccurrence),0).Animation.Value);
                    }
                    probe.Observed=null;
                    world.Step();
                    Assert.Equal(expected,count());
                    Assert.Contains(world.Events,p=>p.Key.Kind==(kind==ImpactKind.Bumper?MachineEventKind.Bumped:MachineEventKind.Bounced));
                    if(part is BumperPart committed)
                    {
                        world.PresentFrame(.08,1);
                        Assert.InRange(world.ReadOccurrenceFeedback(new(committed,BumperPart.ImpactOccurrence),0).Animation.Value,1.1176,1.1224);
                    }
                }
            }
            Assert.True(failed);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FailedPassRestoresObservationCursorAndRetryDeliversActivation()
    {
        var world=World();
        try
        {
            var detector=new BallDetectorPart();
            var timer=new HoldTimerPart();
            var ball=new BallPart();
            var probe=new Probe();
            Attach(world,detector,DetectorCatalogue,FixturePartId.First,new(0,8,0));
            Attach(world,timer,TimerCatalogue,FixturePartId.Second,new(5,8,0));
            Attach(world,ball,BallCatalogue,FixturePartId.Third,new(-.6f,8,0));
            Attach(world,probe,ProbeCatalogue,FixturePartId.Fourth,new(-5,8,0));
            ball.InitialVelocity=new(120,0,0);
            Assert.True(world.Connect(detector,timer));
            var construction=Saved(world);
            world.Start();
            var physics=world.Physics.Capture();
            probe.Observed=()=>
            {
                if(detector.CrossingCount==0)return;
                Assert.True(detector.Pulse>0);
                Assert.True(detector.Active);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,detector.CrossingCount);Assert.Equal(0,detector.Pulse);Assert.False(detector.Active);
            Assert.Equal(SimulationTimerPhase.Ready,timer.State);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            probe.Observed=null;world.Step();
            Assert.Equal(1,detector.CrossingCount);Assert.True(detector.Pulse>0);
            Assert.Equal(SimulationTimerPhase.Counting,timer.State);
            world.Step();Assert.Equal(1,detector.CrossingCount);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
