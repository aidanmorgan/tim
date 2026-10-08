using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ConstructionSnapshotTests(NativeSceneFixture godot)
{
    public enum CaptureState { Running, Advanced, Paused }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId BallCatalogue=new("ball"),ProbeCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observe;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Encode(MachineData data)=>JsonSerializer.Serialize(data,MachineJson.Default.MachineData);
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }

    [Theory]
    [InlineData(CaptureState.Running,false)]
    [InlineData(CaptureState.Running,true)]
    [InlineData(CaptureState.Advanced,false)]
    [InlineData(CaptureState.Advanced,true)]
    [InlineData(CaptureState.Paused,false)]
    [InlineData(CaptureState.Paused,true)]
    public void CapturedSaveUsesDetachedConstructionRegardlessOfRenderedPose(CaptureState state,bool alterPresentation)
    {
        var world=World();
        try
        {
            var ball=new BallPart();
            Attach(world,ball,BallCatalogue,FixturePartId.First,new(0,6,0));
            ball.InitialVelocity=Vector3.Right;
            var construction=Encode(world.Snapshot());
            world.Start();
            switch(state)
            {
                case CaptureState.Running:break;
                case CaptureState.Advanced:world.Step();break;
                case CaptureState.Paused:world.Step();world.Running=false;break;
                default:throw new ArgumentOutOfRangeException(nameof(state));
            }
            var physical=world.Physics.Capture();
            if(alterPresentation)ball.Position=new(7,9,2);
            var exported=world.Snapshot();
            Assert.Equal(construction,Encode(exported));
            Assert.Equal(physical.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            exported.Parts[0].Position[0]=123;
            exported.Parts[0].InitialVelocity[0]=456;
            Assert.Equal(construction,Encode(world.Snapshot()));
            world.LoadMachine(world.Snapshot());
            Assert.Equal(construction,Encode(world.Snapshot()));
            world.Start();
            var restored=Assert.Single(world.Parts);
            Assert.Equal(Vector3.Right,restored.InitialVelocity);
            world.Step();world.Restore();
            Assert.Equal(construction,Encode(world.Snapshot()));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TickCannotCaptureConstructionUntilItCommitsOrRollsBack(bool handleRejection)
    {
        var world=World();
        try
        {
            var probe=new Probe();
            Attach(world,probe,ProbeCatalogue,FixturePartId.First,new(-5,6,0));
            var ball=new BallPart();
            Attach(world,ball,BallCatalogue,FixturePartId.Second,new(0,6,0));
            ball.InitialVelocity=Vector3.Right;
            var construction=Encode(world.Snapshot());
            world.Start();
            var before=world.Physics.Capture();
            var attempts=0;
            probe.Observe=()=>
            {
                attempts++;
                if(handleRejection)Assert.Throws<InvalidOperationException>(()=>world.Snapshot());
                else world.Snapshot();
            };
            if(handleRejection)
            {
                world.Step();
                Assert.Equal(4,attempts);Assert.Equal(1,world.Ticks);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(world.Step);
                Assert.Equal(1,attempts);Assert.Equal(0,world.Ticks);
                Assert.Equal(before.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            }
            Assert.Equal(construction,Encode(world.Snapshot()));
            probe.Observe=null;
            world.Step();
            world.Restore();Assert.Equal(construction,Encode(world.Snapshot()));
        }
        finally {world.Free();}
    }
}
