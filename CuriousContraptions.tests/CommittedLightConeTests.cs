using Godot;
using CuriousContraptions.Physics;
using CuriousContraptions.Bridge;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CommittedLightConeTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Torch=new("flashlight"),Ball=new("ball");

    [Fact]
    public void ConeIgnoresUnpublishedColliderAndNodeChangesAndResetRestoresConstruction()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var torch=(FlashlightPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Torch.Value,Position=[0,5,0]});
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Ball.Value,Position=[1.6f,5,.3f],InitialVelocity=[0,0,1]});
            var construction=world.Snapshot();
            torch.Active=true;var source=torch.LightSource!.Value;
            world.Start();
            var initial=LightConeVisual.Sample(world,torch,source,1);
            Assert.Contains(initial,r=>r.Distance<2);
            Assert.Contains(initial,r=>r.Distance==source.Range);
            var visual=new LightConeVisual();torch.AddChild(visual);
            visual.Refresh(world,torch,source);
            var vertices=visual.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Assert.Equal(LightConeVisual.ShellCount*LightConeVisual.Sectors*9,vertices.Length);
            var key=new SceneBodyKey(ball,MachinePart.RootBody);
            var geometry=world.CollisionGeometry(key);
            var original=world.Physics.Capture();
            world.Physics.Step([],[],MachineWorld.Tick);
            Assert.NotEqual(original.BodyStates.ToArray().Select(s=>s.Pose),
                world.Physics.Capture().BodyStates.ToArray().Select(s=>s.Pose));
            Assert.Equal(initial,LightConeVisual.Sample(world,torch,source,1));
            world.Physics.Restore(original);
            var collider=world.Physics.Collider(world.PhysicsAssembly.Body(key).Id);
            var transparent=geometry.WithChild(new(0),geometry[new(0)] with {Opaque=false});
            world.ReplaceCollisionGeometry(key,transparent,collider.Declaration.Material,CollisionParticipation.Disabled);
            torch.Position+=new Vector3(8,0,0);ball.Position+=new Vector3(0,0,8);
            Assert.Equal(initial,LightConeVisual.Sample(world,torch,source,1));
            visual.Refresh(world,torch,source);
            Assert.Equal(vertices,visual.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array());
            using(var held=world.ReadCommittedPoses())
            {
                Assert.Throws<InvalidOperationException>(()=>LightConeVisual.Sample(world,torch,source,1));
                Assert.Throws<InvalidOperationException>(()=>visual.Refresh(world,torch,source));
                Assert.Equal(vertices,visual.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array());
            }
            world.Step();world.PresentFrame(0,1);
            Assert.All(LightConeVisual.Sample(world,torch,source,1),r=>Assert.Equal(source.Range,r.Distance));
            visual.Refresh(world,torch,source);
            Assert.NotEqual(vertices,visual.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array());
            visual.Free();
            world.Restore();
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(construction,MachineJson.Default.MachineData),
                System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            torch=(FlashlightPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(initial,LightConeVisual.Sample(world,torch,source,1));
            world.Start();
            Assert.Equal(initial,LightConeVisual.Sample(world,torch,source,1));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(TraceMedium.Light)]
    [InlineData(TraceMedium.Sound)]
    [InlineData(TraceMedium.Air)]
    public void WorkbenchQueriesSurviveSaveLoadWithNewGeneration(TraceMedium medium)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            world.Start();
            var saved=world.Snapshot();
            var read=world.ReadCommittedPoses();
            var stamp=read.Stamp(PoseSample.Current);
            var expected=2-Workbench.SurfaceY;
            Assert.InRange(Math.Abs(read.Trace(read.Stamp(PoseSample.Current).SimulationTime,medium,new(0,2,0),new(0,-1,0),10)-expected),0,1e-6);
            Assert.Equal(10,read.Trace(read.Stamp(PoseSample.Current).SimulationTime,medium,new(20,2,0),new(0,-1,0),10));
            world.LoadMachine(saved);
            Assert.Throws<InvalidOperationException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,medium,new(0,2,0),new(0,-1,0),10));
            read.Dispose();
            Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
            world.Start();
            using var restored=world.ReadCommittedPoses();
            Assert.NotEqual(stamp.Generation,restored.Stamp(PoseSample.Current).Generation);
            Assert.Equal(0,restored.Stamp(PoseSample.Current).Revision.Value);
            Assert.Equal(restored.Stamp(PoseSample.Previous),restored.Stamp(PoseSample.Current));
            Assert.InRange(Math.Abs(restored.Trace(restored.Stamp(PoseSample.Current).SimulationTime,medium,new(0,2,0),new(0,-1,0),10)-expected),0,1e-6);
        }
        finally {world.Free();}
    }
}
