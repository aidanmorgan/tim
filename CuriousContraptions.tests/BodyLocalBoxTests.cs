using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BodyLocalBoxTests(NativeSceneFixture godot)
{
    public enum Component { Gate, Shutter, Motor, Conveyor, Windmill, Lever, WoundSpring }
    private static (string Catalog,BodySlot Slot) Declaration(Component component)=>component switch
    {
        Component.Gate=>("powered_gate",PoweredGatePart.BladeBody),
        Component.Shutter=>("beam_shutter",BeamShutterPart.BladeBody),
        Component.Motor=>("motor",MotorPart.ShaftBody),
        Component.Conveyor=>("conveyor",ConveyorPart.ShaftBody),
        Component.Windmill=>("windmill",WindmillPart.RotorBody),
        Component.Lever=>(ImpactLeverPart.CatalogId,ImpactLeverPart.BeamBody),
        Component.WoundSpring=>(WoundSpringPart.CatalogId,WoundSpringPart.ShaftBody),
        _=>throw new ArgumentOutOfRangeException(nameof(component))
    };
    private static Vector3 Scene(CollisionVector point)=>new((float)point.X,(float)point.Y,(float)point.Z);

    [Theory]
    [InlineData(Component.Gate)]
    [InlineData(Component.Shutter)]
    [InlineData(Component.Motor)]
    [InlineData(Component.Conveyor)]
    [InlineData(Component.Windmill)]
    [InlineData(Component.Lever)]
    [InlineData(Component.WoundSpring)]
    public void EveryBodySlotPreservesEachAuthoredBoxOffset(Component component)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var declaration=Declaration(component);
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=declaration.Catalog,
                Position=[0,8,0],Orientation=PartOrientation.FromEulerDegrees(20,30,40)});
            var key=new SceneBodyKey(part,declaration.Slot);
            SceneWorldBodyCapture Body()=>Assert.Single(WorldGeometry.CaptureBodies(world),
                body=>body.Owner==part&&body.Slot==declaration.Slot);
            var before=Body();
            var frozen=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var first=new Vector3(4,0,0);
            var second=new Vector3(-4,0,0);
            var half=Vector3.One*.125f;
            part.Boxes.Add(new(first,half,declaration.Slot));
            part.Boxes.Add(new(second,half,declaration.Slot));
            var changed=Body();
            Assert.NotSame(before.Geometry,changed.Geometry);
            Assert.Equal(before.Geometry.Count+2,changed.Geometry.Count);
            foreach(var at in new[]{first,second})
                Assert.Single(Enumerable.Range(0,changed.Geometry.Count).Select(index=>changed.Geometry[new(index)]),child=>
                    child.Geometry is ConvexBox box&&box.Half==SceneGeometryAdapter.CaptureVector(half)&&
                    child.Pose.Origin==SceneGeometryAdapter.CaptureVector(at));
            var origin=Scene(changed.Pose.TransformPoint(new(4,0,-1)));
            var direction=Scene(changed.Pose.Rotation.Apply(new(0,0,2)));
            Assert.Equal(WorldSweepStatus.Clear,frozen.Sweep(new CompoundGeometry([new(new ConvexSphere(.03f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction)).Status);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.03f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction));
            Assert.Equal(WorldSweepStatus.Contact,hit.Status);
            QueryAssertions.Owner(world,part,hit.Body);
            Assert.InRange(hit.Distance,.844f,.846f);
            world.Start();
            Assert.Same(changed.Geometry,world.CollisionGeometry(key).Geometry);
            Assert.Equal(hit,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.03f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction)));
            var physical=world.PhysicsAssembly.Body(key);
            Assert.Equal(changed.Pose,physical.Pose);
            FixtureParts.PresentCaptured(world);
            Assert.Equal(first,part.Boxes[^2].At);
            Assert.Equal(second,part.Boxes[^1].At);
            Assert.Equal(hit,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.03f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction)));
        }
        finally {world.Free();}
    }
}
