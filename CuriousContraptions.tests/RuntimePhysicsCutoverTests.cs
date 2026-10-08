using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RuntimePhysicsCutoverTests(NativeSceneFixture godot)
{
    public enum ContactMechanism { Spring, Bumper }
    private MachineWorld World()
    {
        var world=new MachineWorld {Pressure=0}; godot.Tree.Root.AddChild(world); return world;
    }
    private static MachinePart Ball(MachineWorld world,Vector3 at)=>
        world.AddPart(new(){Id="payload",Kind="ball",Position=[at.X,at.Y,at.Z]});

    [Fact]
    public void LiveRunOwnsPersistentBodiesAndNeverImportsScenePoseEdits()
    {
        var world=World();
        try
        {
            var ball=Ball(world,new(0,4,0)); var construction=ball.Transform;
            world.Start();
            var engine=world.Physics;
            var assembly=world.PhysicsAssembly;
            var body=assembly.Body(new(ball,MachinePart.RootBody));
            ball.Position=new(7,9,2); // Rendering is not the run's motion authority.
            for(var i=0;i<30;i++) world.Step();
            Assert.Same(engine,world.Physics); Assert.Same(assembly,world.PhysicsAssembly);
            Assert.Same(body,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)));
            Assert.Equal(120ul,engine.StepIndex);
            Assert.InRange(Math.Abs(engine.Time-30.0*MachineWorld.Tick),0,1e-12);
            Assert.InRange(Math.Abs(body.Center.Y-(4-.5*world.Gravity*engine.Time*engine.Time)),0,1e-10);
            Assert.InRange((SceneGeometryAdapter.CaptureVector(ball.Position)-body.Center).Length,0,1e-6);
            Assert.Equal(0,body.Center.X); Assert.Equal(0,body.Center.Z);
            world.Restore();
            Assert.Equal(construction,world.Parts.OfType<BallPart>().Single().Transform);
            Assert.False(world.Running);
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(ContactMechanism.Spring,true)]
    [InlineData(ContactMechanism.Spring,false)]
    [InlineData(ContactMechanism.Bumper,true)]
    [InlineData(ContactMechanism.Bumper,false)]
    public void LiveContactMechanismsRespondOnlyOnContactAndResetReplay(ContactMechanism kind,bool aimed)
    {
        var world=World();
        try
        {
            // Explicit catalogue serialization boundary; internal choices stay enum-typed.
            var name=kind switch
            {
                ContactMechanism.Spring=>"spring",ContactMechanism.Bumper=>"bumper",
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            var launcher=world.AddPart(new(){Id="launcher",Kind=name,Position=[0,2,0]});
            var ball=Ball(world,new(aimed?0:3,4,0));
            var ballConstruction=ball.Transform; var launcherConstruction=launcher.Transform;
            (string Signature,bool Launched) Run()
            {
                world.Start(); var launched=false;
                for(var i=0;i<100;i++)
                {
                    world.Step();
                    launched|=world.PhysicsAssembly.Body(new(world.Parts.OfType<BallPart>().Single(),MachinePart.RootBody)).LinearVelocity.Y>(kind==ContactMechanism.Spring?0:4);
                }
                return (world.StateSignature(),launched);
            }
            var first=Run(); Assert.Equal(aimed,first.Launched);
            Assert.Equal(aimed,world.Events.ContainsKey(new(
                kind==ContactMechanism.Spring?MachineEventKind.Bounced:MachineEventKind.Bumped,launcher.Uid)));
            world.Restore();
            Assert.Equal(ballConstruction,world.Parts.OfType<BallPart>().Single().Transform);
            Assert.Equal(launcherConstruction,world.Parts.Single(part=>part is SpringPart or BumperPart).Transform);
            Assert.Empty(world.Events);
            var second=Run();
            Assert.Equal(first,second);
        }
        finally {world.Free();}
    }

    [Fact]
    public void LiveLeverUsesSharedJointMotionAndPresentsItsActualBeamPose()
    {
        var world=World();
        try
        {
            var lever=(ImpactLeverPart)world.AddPart(new(){Id="lever",Kind=ImpactLeverPart.CatalogId,Position=[0,4,0]});
            Ball(world,new(1.3f,6,0));
            world.Start();
            var beam=world.PhysicsAssembly.Body(new(lever,ImpactLeverPart.BeamBody));
            var initial=beam.Pose;
            for(var i=0;i<150;i++) world.Step();
            Assert.NotEqual(initial.Rotation,beam.Pose.Rotation);
            // Inspect scene-authored presentation explicitly, not the authoritative runtime query snapshot.
            var rendered=SceneGeometryAdapter.CaptureRigidPose(lever.BeamTransform);
            Assert.InRange((rendered.Center-beam.Center).Length,0,1e-6);
            var axis=new CollisionVector(1,0,0);
            Assert.InRange((rendered.Rotation.Apply(axis)-beam.Pose.Rotation.Apply(axis)).Length,0,1e-6);
            Assert.True(lever.ImpactCount>0);
        }
        finally {world.Free();}
    }

    [Fact]
    public void SharedInitialOverlapValidationRejectsBeforeRunning()
    {
        var world=World();
        try
        {
            Ball(world,new(0,0,0));
            world.AddPart(new(){Id="bumper",Kind="bumper",Position=[0,0,0]});
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.False(world.Running);
        }
        finally {world.Free();}
    }

    [Fact]
    public void NewContactMechanismParameterBoundariesRejectUndefinedEnumValues()
    {
        Assert.Equal("stiffness",PartParameterName.Of(SpringParameter.Stiffness));
        Assert.Equal("damping",PartParameterName.Of(SpringParameter.Damping));
        Assert.Equal("initial_compression",PartParameterName.Of(SpringParameter.InitialCompression));
        Assert.Equal("strength",PartParameterName.Of(BumperParameter.Strength));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((SpringParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((BumperParameter)999));
    }
}
