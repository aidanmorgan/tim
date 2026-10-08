using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneJointDeclarationTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world); return world;
    }
    private static ImpactLeverPart Lever(MachineWorld world)=>
        (ImpactLeverPart)world.AddPart(new(){Id=ImpactLeverPart.CatalogId,Kind=ImpactLeverPart.CatalogId,Position=[0,8,0]});

    private enum ShaftRole { Input, Output }
    private static string ShaftId(ShaftRole role)=>role switch
    {
        ShaftRole.Input=>"transmission_input",
        ShaftRole.Output=>"transmission_output",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static ImpactLeverPart Shaft(MachineWorld scene,ShaftRole role)=>
        (ImpactLeverPart)scene.AddPart(new(){Id=ShaftId(role),Kind=ImpactLeverPart.CatalogId,
            Position=[role==ShaftRole.Input?-4:4,8,0]});

    [Theory]
    [InlineData(true,1)]
    [InlineData(true,-2)]
    [InlineData(false,1)]
    public void TransmissionBindsRegisteredGuidesAndSharesImpulseWithExactReplay(bool coupled,double ratio)
    {
        var scene=World();
        try
        {
            var first=Shaft(scene,ShaftRole.Input);var second=Shaft(scene,ShaftRole.Output);
            var input=Assert.Single(first.PhysicsJoints);var output=Assert.Single(second.PhysicsJoints);
            var link=new SceneTransmissionJoint(new(first,new JointSlot()),input.Key,output.Key,ratio,TransmissionEngagement.Engaged);
            // Dependency comes first deliberately; stable IDs retain supplied order.
            SceneJointDeclaration[] declarations=coupled?[link,output,input]:[output,input];
            var assembly=new ScenePhysicsAssembly(WorldGeometry.CapturePhysicsBodies(scene),declarations,[]);
            var a=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(input.Key)));
            var b=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(output.Key)));
            if(coupled)
            {
                var transmission=Assert.IsType<PhysicsTransmissionJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(link.Key)));
                Assert.Same(a,transmission.Input);Assert.Same(b,transmission.Output);
                Assert.Equal(new PhysicsJointId(0),transmission.Id);
                Assert.Equal(new PhysicsJointId(2),a.Id);
            }
            var world=new PhysicsWorld([],assembly.Objects.ToArray(),assembly.InitialJoints.ToArray(),new(default));
            var initial=world.Capture();
            double Speed(PhysicsFrameJoint joint)=>joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed;
            void Run()
            {
                world.ApplyImpulse(a.A.Id,new(0,.01,0),a.A.Center+new CollisionVector(1,0,0));
                world.Step([],[],.005);
                Assert.True(Speed(a)>0);
                if(coupled) Assert.InRange(Math.Abs(Speed(b)-ratio*Speed(a)),0,1e-8);
                else Assert.Equal(0,Speed(b));
            }
            Run();var final=world.Capture();
            world.Restore(initial);Run();
            Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        }
        finally {scene.Free();}
    }

    [Fact]
    public void TransmissionRejectsMissingCyclicAndNonAxialDependencies()
    {
        var scene=World();
        try
        {
            var first=Shaft(scene,ShaftRole.Input);var second=Shaft(scene,ShaftRole.Output);
            var input=Assert.IsType<SceneFrameJoint>(Assert.Single(first.PhysicsJoints));
            var output=Assert.IsType<SceneFrameJoint>(Assert.Single(second.PhysicsJoints));
            var bodies=WorldGeometry.CapturePhysicsBodies(scene);
            var key=new SceneJointKey(first,new JointSlot());
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SceneTransmissionJoint(key,input.Key,output.Key,1,(TransmissionEngagement)99));
            var link=new SceneTransmissionJoint(key,input.Key,output.Key,1,TransmissionEngagement.Engaged);
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(bodies,[link,input],[]));
            var cycle=new SceneTransmissionJoint(key,key,input.Key,1,TransmissionEngagement.Engaged);
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(bodies,[cycle,input],[]));
            var ball=new SceneFrameJoint(output.Key,FrameJointKind.BallSocket,output.A,output.LocalA,
                output.B,output.LocalB,output.Collision,null,JointTravelDirection.Both);
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(bodies,[link,input,ball],[]));
            Assert.Throws<ArgumentException>(()=>new SceneTransmissionJoint(key,input.Key,input.Key,1,TransmissionEngagement.Engaged));
            foreach(var ratio in new[]{0,double.NaN,double.PositiveInfinity})
                Assert.Throws<ArgumentOutOfRangeException>(()=>new SceneTransmissionJoint(key,input.Key,output.Key,ratio,TransmissionEngagement.Engaged));
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturedLeverPivotAndStopsUseSharedBodiesAndReplay(bool rotated)
    {
        var scene=World();
        try
        {
            var lever=Lever(scene);
            if(rotated) lever.Rotation=new(.3f,.4f,.5f);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var beam=assembly.Body(new(lever,ImpactLeverPart.BeamBody));
            var fixture=assembly.Body(new(lever,MachinePart.RootBody));
            var hinge=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(lever,ImpactLeverPart.PivotJoint))));
            Assert.Same(beam,hinge.A); Assert.Same(fixture,hinge.B);
            Assert.Equal(FrameJointKind.Hinge,hinge.Kind);
            Assert.Equal(ConnectedBodyCollision.Disabled,hinge.Collision);
            Assert.Equal(-ImpactLeverPart.LimitAngle,hinge.TravelRange!.Lower);
            Assert.Equal(ImpactLeverPart.LimitAngle,hinge.TravelRange.Upper);
            var objects=assembly.Objects.ToArray();
            var world=new PhysicsWorld([],objects,assembly.InitialJoints.ToArray(),new(default));
            var original=world.Capture();
            var direction=SceneGeometryAdapter.CaptureVector(-lever.Basis.Y)*3;
            var point=beam.Pose.TransformPoint(new(1.5,0,0));
            void Run()
            {
                world.ApplyImpulse(beam.Id,direction,point);
                for(var i=0;i<180;i++)
                {
                    world.Step([],[],1.0/120);
                    Assert.InRange(hinge.Error(1e-8),0,1.01e-7);
                    Assert.InRange(hinge.Travel.Error,-ImpactLeverPart.LimitAngle-1e-7,ImpactLeverPart.LimitAngle+1e-7);
                    Assert.InRange((beam.Center-fixture.Center).Length,0,1.01e-7);
                }
            }
            Run();
            Assert.InRange(hinge.Travel.Error,-ImpactLeverPart.LimitAngle-1e-7,-ImpactLeverPart.LimitAngle+1e-7);
            Assert.InRange(beam.AngularVelocity.Length,0,1e-7);
            var final=world.Capture();
            world.Restore(original); Run();
            Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(SceneGeometryAdapter.CaptureRigidPose(lever.Transform),fixture.Pose);
            Assert.Equal(lever.Transform,lever.BeamTransform); // No presenter ran in this declaration-only test.
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CatalogueBallContactDrivesDeclaredHingeWhileMissDoesNot(bool aimed)
    {
        var scene=World();
        try
        {
            var lever=Lever(scene);
            var ball=scene.AddPart(new(){Id="ball",Kind="ball",Position=[aimed?1.3f:4,10,0]});
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var beam=assembly.Body(new(lever,ImpactLeverPart.BeamBody));
            var payload=assembly.Body(new(ball,MachinePart.RootBody));
            var hinge=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(lever,ImpactLeverPart.PivotJoint))));
            var objects=assembly.Objects.ToArray();
            var world=new PhysicsWorld([],objects,assembly.InitialJoints.ToArray(),new(new(0,-9.81,0)));
            var before=world.Capture();
            bool Run()
            {
                var contact=false;
                for(var i=0;i<180;i++)
                {
                    world.Step([],[],1.0/120);
                    foreach(var impact in world.Impacts)
                        contact|=impact.Pair.A==beam.Id&&impact.Pair.B==payload.Id||
                            impact.Pair.B==beam.Id&&impact.Pair.A==payload.Id;
                    Assert.InRange(hinge.Error(1e-8),0,1.01e-7);
                }
                return contact;
            }
            Assert.Equal(aimed,Run());
            if(aimed) Assert.True(hinge.Travel.Error<-.1);
            else Assert.InRange(Math.Abs(hinge.Travel.Error),0,1e-8);
            var after=world.Capture();
            world.Restore(before);
            Assert.Equal(aimed,Run());
            Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,lever.ImpactCount); // No production callback shim is involved.
        }
        finally {scene.Free();}
    }

    [Fact]
    public void InitialTwistIsMeasuredAgainstAuthoredFrameNotRezeroed()
    {
        var scene=World();
        try
        {
            var lever=(ImpactLeverPart)scene.AddPart(new(){Id=ImpactLeverPart.CatalogId,Kind=ImpactLeverPart.CatalogId,
                Position=[0,8,0],Properties={
                    [PartParameterName.Of(ImpactLeverParameter.InitialAngle)]=(float)(.2*180/Math.PI)}});
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var hinge=Assert.IsType<PhysicsFrameJoint>(Assert.Single(assembly.InitialJoints.ToArray()));
            Assert.InRange(hinge.Travel.Error,.2-1e-7,.2+1e-7);
            Assert.Equal(-ImpactLeverPart.LimitAngle,hinge.TravelRange!.Lower);
            Assert.Equal(ImpactLeverPart.LimitAngle,hinge.TravelRange.Upper);
        }
        finally {scene.Free();}
    }

    [Fact]
    public void AxialImpulseCannotMoveOrSpinTheConstrainedBeam()
    {
        var scene=World();
        try
        {
            var lever=Lever(scene);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var beam=assembly.Body(new(lever,ImpactLeverPart.BeamBody));
            var before=beam.Snapshot();
            beam.ApplyImpulse(new(0,0,3),beam.Center);
            var hinge=Assert.Single(assembly.InitialJoints.ToArray());
            ImpulseSolver.Solve(hinge.VelocityConstraints(1e-7));
            Assert.Equal(before.Pose,beam.Pose);
            Assert.InRange(beam.LinearVelocity.Length,0,1e-12);
            Assert.InRange(beam.AngularVelocity.Length,0,1e-12);
        }
        finally {scene.Free();}
    }

    [Fact]
    public void DuplicateAndUnknownIdentitiesRejectWithoutFallbackBinding()
    {
        var scene=World();
        try
        {
            var lever=Lever(scene);
            var bodies=WorldGeometry.CapturePhysicsBodies(scene);
            var declaration=Assert.Single(lever.PhysicsJoints);
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly([..bodies,bodies[0]],[],[]));
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(bodies,[declaration,declaration],[]));
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(
                bodies.Where(b=>b.Geometry.Slot!=ImpactLeverPart.BeamBody),[declaration],[]));
            var assembly=new ScenePhysicsAssembly(bodies,[declaration],[]);
            Assert.Throws<ArgumentException>(()=>assembly.JointId(new(lever,new JointSlot())));
            var unknown=new BodySlot(p=>global::CuriousContraptions.Geometry.RigidPose.Identity,_=>new(PhysicsMotionType.Static,0,default,default,default),
                BodyQueryPolicy.Include,_=>new(0,0,0),_=>[]);
            Assert.Throws<ArgumentException>(()=>assembly.Body(new(lever,unknown)));
            Assert.Throws<ArgumentException>(()=>new SceneBodyKey(null,MachinePart.RootBody));
            Assert.Throws<ArgumentException>(()=>new SceneBodyKey(lever,Workbench.Body));
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(FrameJointKind.BallSocket)]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void CustomJointSlotsBindClosedKindsWithoutAComponentRegistry(FrameJointKind kind)
    {
        var scene=World();
        try
        {
            var lever=Lever(scene);
            var slot=new JointSlot();
            var frame=new JointFrame(default,RigidRotation.Identity);
            var declaration=new SceneFrameJoint(new(lever,slot),kind,new(lever,ImpactLeverPart.BeamBody),frame,
                new(lever,MachinePart.RootBody),frame,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
            var supplied=new SceneJointDeclaration[]{declaration};
            var assembly=new ScenePhysicsAssembly(WorldGeometry.CapturePhysicsBodies(scene),supplied,[]);
            supplied[0]=null!;
            var joint=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(lever,slot))));
            Assert.Equal(kind,joint.Kind);
            Assert.Same(assembly.Body(new(lever,ImpactLeverPart.BeamBody)),joint.A);
        }
        finally {scene.Free();}
    }
}
