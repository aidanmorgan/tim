using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpactFrameTests(NativeSceneFixture godot,ITestOutputHelper output)
{
    [Theory]
    [InlineData(Element.Spring,false)]
    [InlineData(Element.Spring,true)]
    [InlineData(Element.Bumper,false)]
    [InlineData(Element.Bumper,true)]
    public void AuthoredMovingSurfaceLaunchesThroughSharedWorldAndResetReplays(Element element,bool strict)
    {
        if(!Enum.IsDefined(element))throw new ArgumentOutOfRangeException(nameof(element));
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var spring=element==Element.Spring;
            var partSpec=new PartSpec{Id=Wire(element),Kind=Wire(element),Position=[0,4,0]};
            var payloadSpec=new PartSpec{Id="payload",Kind="ball",Position=spring?[0,4.65f,0]:[1.1f,4,0]};
            var target=new PartSpec{Id=partSpec.Id,Kind=partSpec.Kind,Position=spring?[0,4.2f,0]:[.2f,4,0],
                Difficulty=[new(){Precision=0,PositionWindow=1,MaxPositionCorrection=.3f,BlendSeconds=.4f},
                    new(){Precision=1,PositionWindow=1,MaxPositionCorrection=0,BlendSeconds=.4f}]};
            world.LoadMachine(new(){Parts=[partSpec,payloadSpec],PlacementTargets=[target],Gravity=0,Pressure=0});
            world.Precision=strict?1:0;
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBodySnapshot[] Run()
            {
                var part=world.FindPart(partSpec.Id)!;
                var payload=world.FindPart(payloadSpec.Id)!;
                world.Start();
                var frame=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
                Assert.Equal(strict?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,frame.MotionType);
                for(var tick=0;tick<60;tick++)
                {
                    try {world.Step();}
                    catch(InvalidOperationException)
                    {
                        output.WriteLine($"Failure at tick {tick}, physical time {world.Physics.Time:R}.");
                        foreach(var state in world.Physics.Capture().BodyStates)
                            output.WriteLine($"Body {state.Id.Index}: pose {state.Pose}, velocity {state.LinearVelocity}.");
                        if(part is SpringPart board)
                            output.WriteLine($"Plate coordinate {board.PlateOffset:R}, energy {board.StoredElasticEnergy:R}.");
                        throw;
                    }
                }
                var body=world.PhysicsAssembly.Body(new(payload,MachinePart.RootBody));
                var hits=element switch
                {
                    Element.Spring=>((SpringPart)part).HitCount,
                    Element.Bumper=>((BumperPart)part).HitCount,
                    _=>throw new ArgumentOutOfRangeException(nameof(element))
                };
                if(strict)
                {
                    Assert.Equal(0,hits);Assert.Equal(default,body.LinearVelocity);
                    Assert.Equal(SceneGeometryAdapter.CaptureVector(new Vector3(payloadSpec.Position[0],payloadSpec.Position[1],payloadSpec.Position[2])),body.Center);
                }
                else
                {
                    Assert.True(hits>0);
                    if(spring)Assert.True(body.LinearVelocity.Y>0);
                    else Assert.True(body.LinearVelocity.X>part.ReadParameter(BumperParameter.Strength));
                }
                return world.Physics.Capture().BodyStates.ToArray();
            }
            var first=Run();world.Restore();
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var second=Run();
            Assert.Equal(first.Length,second.Length);
            for(var i=0;i<first.Length;i++)
            {
                Assert.Equal(first[i] with {PrescribedMotion=null},second[i] with {PrescribedMotion=null});
                if(first[i].PrescribedMotion is not { } motion){Assert.Null(second[i].PrescribedMotion);continue;}
                var replay=Assert.IsType<PrescribedBodyMotion>(second[i].PrescribedMotion);
                Assert.Equal(motion.Time,replay.Time);Assert.Equal(motion.LocalPose,replay.LocalPose);
                Assert.Equal(motion.Path.StartPose,replay.Path.StartPose);Assert.Equal(motion.Path.Duration,replay.Path.Duration);
                Assert.Equal(motion.Path.Translation,replay.Path.Translation);Assert.Equal(motion.Path.RotationVector,replay.Path.RotationVector);
            }
        }
        finally {world.Free();}
    }
    public enum Element { Spring, Bumper }
    private static string Wire(Element element)=>element switch
    {
        Element.Spring=>"spring",Element.Bumper=>"bumper",
        _=>throw new ArgumentOutOfRangeException(nameof(element))
    };
    [Theory]
    [InlineData(Element.Spring,0)]
    [InlineData(Element.Spring,1)]
    [InlineData(Element.Spring,-1)]
    [InlineData(Element.Bumper,0)]
    [InlineData(Element.Bumper,1)]
    [InlineData(Element.Bumper,-1)]
    public void LaunchImpulseUsesSurfaceFrameWithoutMutatingItsInputs(Element element,int direction)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=Wire(element),Kind=Wire(element),Position=[0,5,0]});
            var strength=element switch
            {
                Element.Spring=>0,
                Element.Bumper=>part.ReadParameter(BumperParameter.Strength),
                _=>throw new ArgumentOutOfRangeException(nameof(element))
            };
            var surface=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,
                new(new(0,5,0),RigidRotation.Identity),new(3*direction,2*direction,direction),new(0,0,2*direction));
            var center=new CollisionVector(0,6,0);
            var carrier=surface.PointVelocity(center);
            var relative=new CollisionVector(4,-1,3);
            var payload=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,new(center,RigidRotation.Identity),
                carrier+relative,default,2,new(.2,.2,.2));
            var self=new PhysicsImpactBody(surface.Snapshot(),surface.Snapshot(),surface.InverseMass,surface.AngularVelocity);
            var other=new PhysicsImpactBody(payload.Snapshot(),payload.Snapshot(),payload.InverseMass,payload.AngularVelocity);
            Assert.Equal(carrier,self.PointVelocity(center));
            if(element==Element.Spring)
            {
                Assert.Empty(part.PhysicsImpact(self,other,1)); // Passive plate has no launch callback.
                Assert.Equal(self.After,surface.Snapshot());Assert.Equal(other.After,payload.Snapshot());
                return;
            }
            var command=Assert.Single(part.PhysicsImpact(self,other,1));
            Assert.Equal(payload.Id,command.Body);Assert.Equal(center,command.Point);
            var outgoing=other.After.LinearVelocity+command.Impulse*other.InverseMass-carrier;
            Assert.Equal(element==Element.Spring?new CollisionVector(0,strength,0):new(4,strength,3),outgoing);
            Assert.Equal(self.After,surface.Snapshot());Assert.Equal(other.After,payload.Snapshot());
            Assert.Equal(command,Assert.Single(part.PhysicsImpact(self,other,1)));
            Assert.Throws<ArgumentException>(()=>self.PointVelocity(new(double.NaN,0,0)));
        }
        finally {world.Free();}
    }
}
