using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SlidingBladeOwnershipTests(NativeSceneFixture godot)
{
    public enum BladeKind { PoweredGate, BeamShutter }
    private static string Kind(BladeKind kind)=>kind switch
    {
        BladeKind.PoweredGate=>"powered_gate",BladeKind.BeamShutter=>"beam_shutter",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static BodySlot Slot(BladeKind kind)=>kind switch
    {
        BladeKind.PoweredGate=>PoweredGatePart.BladeBody,BladeKind.BeamShutter=>BeamShutterPart.BladeBody,
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(BladeKind.PoweredGate,false,false)]
    [InlineData(BladeKind.PoweredGate,true,false)]
    [InlineData(BladeKind.PoweredGate,false,true)]
    [InlineData(BladeKind.PoweredGate,true,true)]
    [InlineData(BladeKind.BeamShutter,false,false)]
    [InlineData(BladeKind.BeamShutter,true,false)]
    [InlineData(BladeKind.BeamShutter,false,true)]
    [InlineData(BladeKind.BeamShutter,true,true)]
    public void PresentationNeverRewritesConstructionAndQueriesUseSolvedBlade(BladeKind kind,bool clearProxies,bool paused)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var id=FixtureParts.Id(FixturePartId.First);
            var gate=world.AddPart(new(){Id=id,Kind=Kind(kind),Position=[0,8,0],
                Orientation=PartOrientation.FromEulerDegrees(20,30,40)});
            var boxes=gate.Boxes.ToArray();
            var bladeBox=Assert.Single(boxes,box=>box.Body==Slot(kind));
            Assert.Equal(Vector3.Zero,bladeBox.At);
            var visible=gate.GetChildren().SelectMany(node=>node.GetChildren())
                .OfType<MeshInstance3D>().Single(mesh=>mesh.Mesh is BoxMesh box&&box.Size==bladeBox.Half*2);
            var initialVisual=visible.Transform;
            var construction=Saved(world);
            var from=gate.Transform*new Vector3(-2,0,0);
            var direction=gate.Basis.X.Normalized();
            world.Start();
            var key=new SceneBodyKey(gate,Slot(kind));
            var body=world.PhysicsAssembly.Body(key);
            var geometry=world.CollisionGeometry(key);
            var collider=world.Physics.Collider(body.Id);
            var before=world.Physics.Capture();
            var frozen=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var oldHit=frozen.Sweep(new CompoundGeometry([new(new ConvexSphere(.01f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(from)),SceneGeometryAdapter.CaptureVector(direction*4));
            QueryAssertions.Owner(world,gate,oldHit.Body);
            float Ray(TraceMedium medium)=>WorldGeometry.Trace(medium,world,from,direction,4,null);
            foreach(var medium in Enum.GetValues<TraceMedium>())Assert.InRange(Ray(medium),1.9f,2);
            world.Physics.ApplyImpulse(body.Id,body.Pose.Rotation.Apply(new(0,SlidingBlade.Mass*2,0)),body.Center);
            world.Physics.Step([],[],.5);
            var after=world.Physics.Capture();
            Assert.Equal(initialVisual,visible.Transform); // Physics does not need rendered motion.
            foreach(var medium in Enum.GetValues<TraceMedium>())Assert.Equal(4,Ray(medium));
            if(paused)world.Running=false;
            if(clearProxies)gate.Boxes.Clear();
            FixtureParts.PresentCaptured(world);
            var rendered=SceneGeometryAdapter.CaptureRigidPose(gate.Transform*visible.Transform);
            Assert.InRange((rendered.Center-body.Center).Length,0,1e-6);
            Assert.InRange((rendered.Rotation.Inverse()*body.Pose.Rotation).RotationVector().Length,0,1e-6);
            Assert.NotEqual(initialVisual,visible.Transform);
            if(clearProxies)Assert.Empty(gate.Boxes);
            else Assert.Equal(boxes,gate.Boxes.ToArray());
            Assert.Same(geometry,world.CollisionGeometry(key));
            Assert.Equal(collider,world.Physics.Collider(body.Id));
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(oldHit,frozen.Sweep(new CompoundGeometry([new(new ConvexSphere(.01f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(from)),SceneGeometryAdapter.CaptureVector(direction*4)));
            foreach(var medium in Enum.GetValues<TraceMedium>())Assert.Equal(4,Ray(medium));
            world.Physics.Restore(before);
            foreach(var medium in Enum.GetValues<TraceMedium>())Assert.InRange(Ray(medium),1.9f,2);
            world.Physics.Restore(after);
            FixtureParts.PresentCaptured(world);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(construction,Saved(world));
            Assert.Equal(boxes,world.FindPart(id)!.Boxes.ToArray());
        }
        finally {world.Free();}
    }
}
