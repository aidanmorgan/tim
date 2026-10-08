using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ScenePlungerBindingTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var scene=new MachineWorld(); godot.Tree.Root.AddChild(scene); return scene;
    }
    private static WoundSpringPart Spring(MachineWorld scene,bool rotated)=>
        (WoundSpringPart)scene.AddPart(new(){Id=WoundSpringPart.CatalogId,Kind=WoundSpringPart.CatalogId,
            Position=[0,8,0],Orientation = rotated?PartOrientation.FromEulerDegrees(23,37,19):PartOrientation.FromEulerDegrees(0,0,0)});
    private static PhysicsWorld Physics(ScenePhysicsAssembly assembly)
    {
        return new([],assembly.Objects.ToArray(),assembly.InitialJoints.ToArray(),new(default));
    }
    private static PhysicsFrameJoint Release(PhysicsWorld world,PhysicsFrameJoint guide,double upper)
    {
        var released=new PhysicsFrameJoint(guide.Id,guide.Kind,guide.A,guide.LocalA,guide.B,guide.LocalB,
            guide.Collision,new(guide.Travel.Error,upper),JointTravelDirection.Positive);
        var winding=Assert.Single(world.Joints.ToArray().OfType<PhysicsTransmissionJoint>(),
            joint=>joint.Output.Id==guide.Id);
        var open=new PhysicsTransmissionJoint(winding.Id,winding.Input,released,winding.Ratio,TransmissionEngagement.Open);
        world.ReplaceJoints(world.Joints.ToArray().Select(joint=>joint.Id==guide.Id?(PhysicsJoint)released:
            joint.Id==winding.Id?open:joint));
        return released;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LatchedFiniteMassHeadAllowsOnlyInwardWindingAndCouplesShaft(bool rotated)
    {
        var scene=World();
        try
        {
            var spring=Spring(scene,rotated);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var head=assembly.Body(new(spring.Plunger,MachinePart.RootBody));
            var fixture=assembly.Body(new(spring,MachinePart.RootBody));
            var guide=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.PlungerGuide))));
            Assert.Equal(FrameJointKind.Slider,guide.Kind);
            Assert.Equal(PhysicsMotionType.Dynamic,head.MotionType);
            Assert.Equal(1.0/WoundSpringPart.PlungerMass,head.InverseMass);
            Assert.Same(head,guide.A); Assert.Same(fixture,guide.B);
            Assert.Equal(JointTravelDirection.Negative,guide.Direction);
            Assert.InRange(guide.TravelRange!.Upper-guide.TravelRange.Lower,.199999,.800001);
            var winding=Assert.IsType<PhysicsTransmissionJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.WindingJoint))));
            Assert.Equal(TransmissionEngagement.Engaged,winding.Engagement);
            var railAxis=guide.FrameB.Orientation.Apply(new(0,0,1));
            var world=Physics(assembly); var before=world.Capture();
            foreach(var axis in new[]{new CollisionVector(1,0,0),new CollisionVector(0,1,0),new CollisionVector(0,0,1)})
            foreach(var sign in new[]{-1,1})
            {
                world.Restore(before);
                world.ApplyImpulse(head.Id,axis*(3*sign),head.Center+new CollisionVector(.1,.2,.3));
                world.Step([],[],.01);
                var speed=guide.Travel.Jacobian.Bind(guide.A,guide.B).Speed;
                Assert.True(speed<=1e-7);
                if(CollisionVector.Dot(axis*sign,railAxis)<-.01)
                {
                    Assert.True(speed<0);
                    Assert.True(winding.Input.A.AngularVelocity.Length>0);
                }
                else Assert.InRange(head.LinearVelocity.Length,0,1e-7);
                Assert.InRange(Math.Abs(winding.SpeedError),0,1e-7);
                Assert.InRange(head.AngularVelocity.Length,0,1e-7);
                Assert.InRange(guide.Error(1e-8),0,1.01e-7);
            }
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReleasedGuideMovesOnlyOutwardStopsAtRestAndRestoresLatch(bool rotated)
    {
        var scene=World();
        try
        {
            var spring=Spring(scene,rotated);
            // Construction fixture for a pre-compressed head, not a browser-state setter.
            spring.Plunger.Position=spring.Transform*new Vector3(0,.4f,0);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var head=assembly.Body(new(spring.Plunger,MachinePart.RootBody));
            var held=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.PlungerGuide))));
            var axis=held.FrameB.Orientation.Apply(new(0,0,1));
            var world=Physics(assembly); var initial=world.Capture();
            void Run()
            {
                var released=Release(world,held,WoundSpringPart.RestHeadY);
                world.ApplyImpulse(head.Id,axis*2,head.Center);
                for(var i=0;i<60;i++)
                {
                    world.Step([],[],1.0/120);
                    Assert.InRange(released.Error(1e-8),0,1.01e-7);
                    Assert.InRange(released.Travel.Error,held.TravelRange!.Lower-1e-7,WoundSpringPart.RestHeadY+1e-7);
                }
                Assert.InRange(released.Travel.Error,WoundSpringPart.RestHeadY-1e-7,WoundSpringPart.RestHeadY+1e-7);
                Assert.InRange(head.LinearVelocity.Length,0,1e-7);
            }
            Run(); var final=world.Capture();
            world.Restore(initial);
            Assert.Same(held,Assert.Single(world.Joints.ToArray(),joint=>joint.Id==held.Id));
            Run();
            Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(PhysicsMotionType.Dynamic,head.MotionType);
            Assert.Equal(0,spring.ReleaseCount); // Shared constraints, not old spring stepping.
        }
        finally {scene.Free();}
    }

    [Fact]
    public void RatchetStopsReverseImpulseWithoutAdvancingTheLowerBound()
    {
        var scene=World();
        try
        {
            var spring=Spring(scene,false); spring.Plunger.Position=spring.Transform*new Vector3(0,.3f,0);
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var held=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.PlungerGuide))));
            var head=held.A; var world=Physics(assembly);
            var released=Release(world,held,WoundSpringPart.RestHeadY);
            var axis=released.FrameB.Orientation.Apply(new(0,0,1));
            world.ApplyImpulse(head.Id,axis*.5,head.Center);
            world.Step([],[],.1);
            Assert.True(released.Travel.Error>held.TravelRange!.Lower+.09);
            var retained=released.Travel.Error;
            Assert.Equal(JointTravelDirection.Positive,released.Direction);
            Assert.True(released.TravelRange!.Lower<retained-.09);
            world.ApplyImpulse(head.Id,-axis*2,head.Center);
            world.Step([],[],.1);
            Assert.InRange(released.Travel.Error,retained-1e-7,retained+1e-7);
            Assert.InRange(head.LinearVelocity.Length,0,1e-7);
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PayloadCollisionCanWindPlungerThroughSharedContactsAndShaft(bool rotated)
    {
        var scene=World();
        try
        {
            var spring=Spring(scene,rotated);
            var axis=spring.Basis.Y.Normalized();
            var start=spring.Plunger.Position+axis*.95f;
            var ball=scene.AddPart(new(){Id="payload",Kind="ball",Position=[start.X,start.Y,start.Z]});
            ball.InitialVelocity=-axis*3;
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var head=assembly.Body(new(spring.Plunger,MachinePart.RootBody));
            var payload=assembly.Body(new(ball,MachinePart.RootBody));
            var guide=Assert.IsType<PhysicsFrameJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.PlungerGuide))));
            var world=Physics(assembly); var initial=world.Capture();
            var winding=Assert.IsType<PhysicsTransmissionJoint>(assembly.InitialJoints.ToArray().Single(candidate=>candidate.Id==assembly.JointId(new(spring,WoundSpringPart.WindingJoint))));
            var initialEnergy=assembly.Objects.ToArray().Sum(item=>item.Body.KineticEnergy);
            void Run()
            {
                world.Step([],[],.15);
                Assert.Contains(world.Impacts.ToArray(),hit=>hit.Pair.A==head.Id&&hit.Pair.B==payload.Id||
                    hit.Pair.B==head.Id&&hit.Pair.A==payload.Id);
                Assert.True(guide.Travel.Jacobian.Bind(guide.A,guide.B).Speed<0);
                Assert.True(winding.Input.A.AngularVelocity.Length>0);
                Assert.InRange(Math.Abs(winding.SpeedError),0,1e-7);
                Assert.InRange(assembly.Objects.ToArray().Sum(item=>item.Body.KineticEnergy),0,initialEnergy+1e-6);
                Assert.InRange((payload.Center-head.Center).Length,ball.Radius+spring.Plunger.Radius-1e-6,double.PositiveInfinity);
                Assert.InRange(guide.Error(1e-8),0,1.01e-7);
            }
            Run(); var final=world.Capture(); world.Restore(initial); Run();
            Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        }
        finally {scene.Free();}
    }
}
