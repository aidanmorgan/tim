using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ActuatorBodyGeometryTests(NativeSceneFixture godot)
{
    private static BodyDynamics Static(MachinePart? owner)=>
        new(PhysicsMotionType.Static,0,default,default,default);
    public enum Actuator { Gate, Shutter }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0}; godot.Tree.Root.AddChild(world); return world;
    }
    private static string Catalogue(Actuator kind)=>kind switch
    {
        Actuator.Gate=>"powered_gate",Actuator.Shutter=>"beam_shutter",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static SceneWorldBodyCapture Body(MachineWorld world,MachinePart part,BodySlot slot)=>
        WorldGeometry.CaptureBodies(world).Single(b=>b.Owner==part&&b.Slot==slot);

    private static BodySlot Blade(Actuator kind)=>kind switch
    {
        Actuator.Gate=>PoweredGatePart.BladeBody,Actuator.Shutter=>BeamShutterPart.BladeBody,
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };

    [Theory]
    [InlineData(Actuator.Gate,false)]
    [InlineData(Actuator.Gate,true)]
    [InlineData(Actuator.Shutter,false)]
    [InlineData(Actuator.Shutter,true)]
    public void PoweredBladeHasIndependentPoseAndStableShapeThroughoutTravelAndReset(Actuator kind,bool rotated)
    {
        var world=World();
        try
        {
            var name=Catalogue(kind); // Explicit current catalogue/instance boundary.
            var part=world.AddPart(new(){Id=name,Kind=name,Position=[0,8,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-6,2,4]});
            if(rotated) part.Rotation=new(.3f,.7f,-.2f);
            Assert.True(world.Connect(battery,part));
            var construction=part.Transform; var boxes=part.Boxes.ToArray();
            var fixture=Body(world,part,MachinePart.RootBody); var blade=Body(world,part,Blade(kind));
            Assert.Equal(AffineTransform.Identity,blade.Geometry[new(0)].Pose);
            Assert.Single(part.Boxes,b=>b.Body==Blade(kind));
            var capture=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.ExcludeBodies);
            var origin=part.Transform*new Vector3(-2,0,0); var direction=part.Basis.X;
            var closed=capture.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction*4));
            QueryAssertions.Owner(world,part,closed.Body); Assert.Equal(SweepSurfaceKind.Box,closed.Surface);
            world.Start();
            for(var i=0;i<180;i++)
            {
                world.Step();world.PresentFrame(0,1);
                var current=Body(world,part,Blade(kind));
                var fixedBody=Body(world,part,MachinePart.RootBody);
                Assert.Same(blade.Geometry,current.Geometry); Assert.Same(fixture.Geometry,fixedBody.Geometry);
                Assert.Equal(fixture.Pose,fixedBody.Pose);
                var proxy=part.Boxes.Single(b=>b.Body==Blade(kind));
                Assert.Equal(Vector3.Zero,proxy.At);
                Assert.Equal(boxes,part.Boxes.ToArray());
                var visual=part.GetChildren().SelectMany(node=>node.GetChildren()).OfType<MeshInstance3D>()
                    .Single(mesh=>mesh.Mesh is BoxMesh box&&box.Size==proxy.Half*2);
                var rendered=SceneGeometryAdapter.CaptureRigidPose(part.Transform*visual.Transform);
                Assert.InRange((rendered.Center-current.Pose.Center).Length,0,1e-6);
                Assert.InRange((rendered.Rotation.Inverse()*current.Pose.Rotation).RotationVector().Length,0,1e-6);
                var actual=world.PhysicsAssembly.Body(new(part,Blade(kind)));
                Assert.InRange((actual.Pose.Center-current.Pose.Center).Length,0,1e-6);
            }
            var opened=Body(world,part,Blade(kind));
            Assert.True((opened.Pose.Center-blade.Pose.Center).Length>1.2);
            Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction*4)).Status);
            Assert.Equal(4,WorldGeometry.Trace(TraceMedium.Light,world,origin,direction,4,null));
            Assert.Equal(closed,capture.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction*4)));
            world.Restore();
            part=world.FindPart(name)!;
            Assert.Equal(construction,part.Transform);
            Assert.Equal(boxes,part.Boxes.ToArray());
            Assert.Equal(blade.Pose,Body(world,part,Blade(kind)).Pose);
            var restored=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(direction*4));
            Assert.Equal(closed.Distance,restored.Distance); Assert.Equal(closed.Normal,restored.Normal);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Actuator.Gate)]
    [InlineData(Actuator.Shutter)]
    public void UnpoweredBladeRetainsClosedPoseAndObstruction(Actuator kind)
    {
        var world=World();
        try
        {
            var name=Catalogue(kind);
            var part=world.AddPart(new(){Id=name,Kind=name,Position=[0,8,0]});
            var before=Body(world,part,Blade(kind));
            world.Start();
            for(var i=0;i<60;i++)world.Step();
            var after=Body(world,part,Blade(kind));
            Assert.Equal(before.Pose,after.Pose); Assert.Same(before.Geometry,after.Geometry);
            QueryAssertions.Owner(world,part,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-2,8,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*4)).Body);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Actuator.Gate)]
    [InlineData(Actuator.Shutter)]
    public void SharedBladeMotorHasFiniteSupplyAndReturnSpringClosesAfterPowerRemoval(Actuator kind)
    {
        var world=World();
        try
        {
            var name=Catalogue(kind);
            var part=world.AddPart(new(){Id=name,Kind=name,Position=[0,8,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-6,2,4]});
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,part));
            var initial=world.Snapshot();
            string Run()
            {
                world.Start();
                supply.SetAndSettle(SimulationLatchPhase.On);
                part=world.Parts.Single(p=>p is PoweredGatePart or BeamShutterPart);
                var slot=Blade(kind);
                var body=world.PhysicsAssembly.Body(new(part,slot));
                var joint=Assert.IsType<PhysicsFrameJoint>(Assert.Single(world.Physics.Joints.ToArray()));
                Assert.Same(body,joint.A);
                Assert.Equal(FrameJointKind.Slider,joint.Kind);
                var work=0.0;
                for(var i=0;i<180;i++)
                {
                    world.Step();
                    var use=Assert.Single(world.Physics.MotorUse.ToArray());
                    Assert.Equal(joint.Id,use.Joint);
                    Assert.InRange(use.SuppliedWork,0,120.0*MachineWorld.Tick/MachineWorld.Substeps);
                    Assert.InRange(use.AbsoluteImpulse,0,60.0*MachineWorld.Tick/MachineWorld.Substeps);
                    work+=use.SuppliedWork;
                    Assert.InRange(joint.Travel.Error,-1e-7,joint.TravelRange!.Upper+1e-7);
                }
                Assert.True(work>0);
                Assert.True(joint.Travel.Error>1.2);
                supply.SetAndSettle(SimulationLatchPhase.Off);
                for(var i=0;i<240;i++)
                {
                    world.Step();
                    Assert.Empty(world.Physics.MotorUse.ToArray());
                }
                Assert.InRange(Math.Abs(joint.Travel.Error),0,1e-6);
                Assert.InRange(body.LinearVelocity.Length,0,1e-6);
                return world.StateSignature();
            }
            var first=Run();
            world.Restore();
            Assert.Equal(initial.Connections,world.Connections);
            Assert.Equal(initial.Parts.Select(p=>p.Id),world.Snapshot().Parts.Select(p=>p.Id));
            var second=Run();
            Assert.Equal(first,second);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(Actuator.Gate,false)]
    [InlineData(Actuator.Gate,true)]
    [InlineData(Actuator.Shutter,false)]
    [InlineData(Actuator.Shutter,true)]
    public void ReturnSpringUsesOwnedOrientationInsteadOfPresentation(Actuator kind,bool rotated)
    {
        var world=World();
        try
        {
            var name=Catalogue(kind);
            var part=world.AddPart(new(){Id=name,Kind=name,Position=[0,8,0]});
            if(rotated) part.Rotation=new(.3f,.7f,-.2f);
            var transform=part.Transform;
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-6,2,4]});
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,part));
            var connections=world.Connections.ToArray();
            PhysicsBodySnapshot[] Run(bool corruptPresentation)
            {
                world.Start();
                supply.SetAndSettle(SimulationLatchPhase.On);
                part=world.FindPart(name)!;
                for(var i=0;i<60;i++) world.Step();
                supply.SetAndSettle(SimulationLatchPhase.Off);
                var blade=world.PhysicsAssembly.Body(new(part,Blade(kind)));
                var before=blade.Center;
                for(var i=0;i<40;i++)
                {
                    if(corruptPresentation)
                    {
                        part.RotateZ(.7f);
                        part.Position+=new Vector3(2,1,3);
                    }
                    world.Step();
                    Assert.Empty(world.Physics.MotorUse.ToArray());
                }
                Assert.True((blade.Center-before).Length>.1);
                return world.Physics.Capture().BodyStates.ToArray();
            }
            var expected=Run(false);
            world.Restore();
            Assert.Equal(transform,world.FindPart(name)!.Transform);
            Assert.Equal(connections,world.Connections);
            Assert.Equal(expected,Run(true));
            world.Restore();
            Assert.Equal(transform,world.FindPart(name)!.Transform);
            Assert.Equal(connections,world.Connections);
        }
        finally {world.Free();}
    }

    [Fact]
    public void ArbitrarySlotsGroupCompoundChildrenWithoutGlobalRegistration()
    {
        using var scene=new GeometryQueryScene();
        scene.Part.Position=new(0,8,0);
        var movingPose=new Transform3D(Basis.Identity,new(0,2,0));
        var moving=new BodySlot(p=>SceneGeometryAdapter.CaptureRigidPose(movingPose),Static,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
        var excluded=new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,Static,BodyQueryPolicy.ExcludeFromStaticQueries,_=>new(0,0,0),_=>[]);
        scene.Part.Boxes.Add(new(Vector3.Left,Vector3.One*.2f,moving));
        scene.Part.Boxes.Add(new(Vector3.Right,Vector3.One*.2f,moving));
        scene.Part.Boxes.Add(new(Vector3.Zero,Vector3.One*.2f,excluded));
        var before=Body(scene.World,scene.Part,moving);
        Assert.Equal(2,before.Geometry.Count);
        Assert.Equal(2,WorldGeometry.CaptureBodies(scene.World).Count(b=>b.Owner==scene.Part));
        Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(scene.World,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-2,8,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*4),bodies:SweepBodyMode.ExcludeBodies).Status);
        QueryAssertions.Owner(scene.World,scene.Part,WorldGeometry.Sweep(scene.World,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-2,8,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*4),bodies:SweepBodyMode.IncludeBodies).Body);
        movingPose=new(new Basis(Vector3.Up,.4f),new(0,3,0));
        var after=Body(scene.World,scene.Part,moving);
        Assert.Same(before.Geometry,after.Geometry);
        Assert.NotEqual(before.Pose,after.Pose);
        Assert.Equal(SceneGeometryAdapter.CaptureRigidPose(scene.Part.Transform).Compose(SceneGeometryAdapter.CaptureRigidPose(movingPose)),after.Pose);
        movingPose=new(Basis.Identity.Scaled(new(2,1,1)),default);
        Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureBodies(scene.World));
    }

    [Fact]
    public void MissingSlotRejectsInsteadOfBecomingFixture()
    {
        using var scene=new GeometryQueryScene();
        scene.Part.Boxes.Add(new(default,Vector3.One,null!));
        Assert.Throws<ArgumentNullException>(()=>WorldGeometry.CaptureBodies(scene.World));
    }

    [Fact]
    public void TransparentBodyPoseStillValidatesWhenItsCachedShapeHasNotChanged()
    {
        using var scene=new GeometryQueryScene();
        scene.Part.Position=new(0,8,0);
        var pose=Transform3D.Identity;
        var probe=new BodySlot(p=>SceneGeometryAdapter.CaptureRigidPose(pose),Static,BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
        scene.Part.Boxes.Add(new(default,Vector3.One,probe,false));
        var shape=Body(scene.World,scene.Part,probe).Geometry;
        pose=new(Basis.Identity,Vector3.Up);
        Assert.Same(shape,Body(scene.World,scene.Part,probe).Geometry);
        pose=new(Basis.Identity,new(float.NaN,0,0));
        Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureBodies(scene.World));
        Assert.Throws<ArgumentException>(()=>WorldGeometry.Trace(TraceMedium.Light,scene.World,new(-3,8,0),Vector3.Right,6,null));
    }
}
