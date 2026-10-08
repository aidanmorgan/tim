using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RotaryTransmissionPartTests(NativeSceneFixture godot)
{
    public enum Configuration { Reverse, OpenClutch, EngagedClutch }
    private enum Fixture { Battery, Reverse, Clutch }
    private enum SupplyParameter { Enabled }
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>"battery",Fixture.Reverse=>"reverse_transmission",Fixture.Clutch=>"clutch",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 position,Vector3 rotation=default)=>
        world.AddPart(new(){Id=Kind(fixture),Kind=Kind(fixture),Position=[position.X,position.Y,position.Z],
            Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z)});
    private static PhysicsFrameJoint Guide(MachineWorld world,MachinePart part,JointSlot slot)=>
        (PhysicsFrameJoint)world.CurrentJoint(new(part,slot));
    private static double Speed(PhysicsFrameJoint guide)=>guide.Travel.Jacobian.Bind(guide.A,guide.B).Speed;
    private static void Impulse(MachineWorld world,PhysicsBody body)=>
        world.Physics.ApplyImpulse(body.Id,body.Pose.Rotation.Apply(new(0,-.01,0)),body.Center+body.Pose.Rotation.Apply(new(1,0,0)));
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-6);
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(Configuration.Reverse,false)]
    [InlineData(Configuration.Reverse,true)]
    [InlineData(Configuration.OpenClutch,false)]
    [InlineData(Configuration.OpenClutch,true)]
    [InlineData(Configuration.EngagedClutch,false)]
    [InlineData(Configuration.EngagedClutch,true)]
    public void SharedShaftImpulseTransfersOnlyThroughEngagedConstraintsAndResetIsExact(Configuration configuration,bool rotated)
    {
        if(!Enum.IsDefined(configuration)) throw new ArgumentOutOfRangeException(nameof(configuration));
        var world=World();
        try
        {
            var reverse=configuration==Configuration.Reverse;
            var part=Add(world,reverse?Fixture.Reverse:Fixture.Clutch,new(0,6,0),rotated?new(25,40,15):default);
            if(configuration==Configuration.EngagedClutch)
                Assert.True(world.Connect(Add(world,Fixture.Battery,new(-4,6,0)),part));
            var saved=Saved(world);var id=part.Uid;
            world.Start();
            for(var i=0;i<45;i++) world.Step();
            var input=Guide(world,part,reverse?ReverseTransmissionPart.InputJoint:ClutchPart.InputJoint);
            var output=Guide(world,part,reverse?ReverseTransmissionPart.OutputJoint:ClutchPart.OutputJoint);
            var coupling=(PhysicsTransmissionJoint)world.CurrentJoint(new(part,reverse?ReverseTransmissionPart.GearJoint:ClutchPart.CouplingJoint));
            Assert.Same(input,coupling.Input);Assert.Same(output,coupling.Output);
            Assert.Equal(configuration==Configuration.OpenClutch?TransmissionEngagement.Open:TransmissionEngagement.Engaged,coupling.Engagement);
            Impulse(world,input.A);var energy=input.A.KineticEnergy;
            world.Step();
            Assert.True(Math.Abs(Speed(input))>.1);
            if(configuration==Configuration.OpenClutch) Near(0,Speed(output));
            else Near(Speed(input)*(reverse?-1:1),Speed(output));
            Assert.InRange(input.A.KineticEnergy+output.A.KineticEnergy,0,energy+1e-8);
            if(part is ClutchPart clutch)
            {
                Near(-Speed(input),clutch.InputSpeed);Near(-Speed(output),clutch.OutputSpeed);
            }
            else if(part is ReverseTransmissionPart gear)
            {
                Near(-Speed(input),gear.InputSpeed);Near(-Speed(output),gear.OutputSpeed);
            }
            var original=world.Physics.Capture();
            world.Physics.Step([],[],.02);var after=world.Physics.Capture();
            world.Physics.Restore(original);world.Physics.Step([],[],.02);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Restore();Assert.Equal(saved,Saved(world));
            if(world.FindPart(id) is ClutchPart reset) {Assert.Equal(ClutchPhase.Open,reset.Phase);Assert.Equal(0,reset.InputSpeed);}
            else Assert.Equal(0,((ReverseTransmissionPart)world.FindPart(id)!).InputSpeed);
        }
        finally {world.Free();}
    }

    [Fact]
    public void CoilClosureSharesInertiaAndPowerLossReleasesWithoutErasingOutputMomentum()
    {
        var world=World();
        try
        {
            var battery=Add(world,Fixture.Battery,new(-4,6,0));
            var clutch=(ClutchPart)Add(world,Fixture.Clutch,new(0,6,0));
            Assert.True(world.Connect(battery,clutch));world.Start();
            var input=Guide(world,clutch,ClutchPart.InputJoint);
            var output=Guide(world,clutch,ClutchPart.OutputJoint);
            Impulse(world,input.A);world.Step();
            var inputBefore=Speed(input);Near(0,Speed(output));
            var energy=input.A.KineticEnergy+output.A.KineticEnergy;
            for(var i=0;i<45;i++) world.Step();
            Assert.Equal(ClutchPhase.Engaged,clutch.Phase);
            Near(inputBefore/2,Speed(input));Near(Speed(input),Speed(output));
            Assert.InRange(input.A.KineticEnergy+output.A.KineticEnergy,0,energy+1e-8);
            world.QueueBinaryInput(new(battery,BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            world.Step();
            Assert.Equal(ClutchPhase.Opening,clutch.Phase);
            var speed=Speed(output);
            Impulse(world,input.A);world.Step();
            Near(speed,Speed(output));Assert.True(Math.Abs(Speed(input)-speed)>.1);
            var coupling=(PhysicsTransmissionJoint)world.CurrentJoint(new(clutch,ClutchPart.CouplingJoint));
            Assert.Equal(TransmissionEngagement.Open,coupling.Engagement);
        }
        finally {world.Free();}
    }
}
