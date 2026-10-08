using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PartRuntimeCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId BatteryCatalogue=new("battery"),ClutchCatalogue=new("clutch"),
        ReverseCatalogue=new("reverse_transmission"),SpringCatalogue=new("wound_spring");
    public enum ShaftKind { Clutch, Reverse }
    private partial class ControlledSupply : BatteryPart
    {
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
            [new(SocketId.Supply,ElectricalSourceSignal.When(ElectricalContactSignal.OwnerActive))];
    }
    private partial class ObservedClutch : ClutchPart
    {
        public Action? Before,Observed;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)
        {base.ObservePhysics(world,delta);Observed?.Invoke();}
    }
    private partial class ObservedSpring : WoundSpringPart
    {
        public Action? Before;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedTickRestoresActivationPowerVisibilityAndClosure(bool initiallyPowered)
    {
        var world=World();
        try
        {
            var supply=new ControlledSupply {Active=initiallyPowered};
            var clutch=new ObservedClutch();
            Attach(world,supply,BatteryCatalogue,FixturePartId.First,new(-4,6,0));
            Attach(world,clutch,ClutchCatalogue,FixturePartId.Second,new(0,6,0));
            Assert.True(world.Connect(supply,clutch));
            var construction=Saved(world);
            world.Start();world.Step();
            clutch.Active=true;
            var phase=clutch.Phase;var closure=clutch.Closure;
            var power=Enum.GetValues<SocketId>().Select(clutch.HasElectricalPower).ToArray();
            var physics=world.Physics.Capture();
            clutch.Before=()=>{supply.Active=!initiallyPowered;clutch.Active=false;clutch.Visible=false;};
            clutch.Observed=()=>
            {
                Assert.Equal(!initiallyPowered,clutch.HasElectricalPower(SocketId.PowerIn));
                Assert.NotEqual(closure,clutch.Closure);
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(initiallyPowered,supply.Active);
            Assert.True(clutch.Active);Assert.True(clutch.Visible);
            Assert.Equal(phase,clutch.Phase);Assert.Equal(closure,clutch.Closure);
            Assert.Equal(power,Enum.GetValues<SocketId>().Select(clutch.HasElectricalPower).ToArray());
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            clutch.Before=null;clutch.Observed=null;
            world.Step();
            Assert.Equal(initiallyPowered,clutch.HasElectricalPower(SocketId.PowerIn));
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void InternalBodiesHaveIndependentBaseCheckpoints()
    {
        var world=World();
        try
        {
            var spring=new ObservedSpring();
            Attach(world,spring,SpringCatalogue,FixturePartId.First,new(0,6,0));
            var child=Assert.Single(spring.InternalBodies);
            var construction=Saved(world);
            world.Start();
            spring.Active=false;child.Active=true;child.Visible=true;
            spring.Before=()=>{spring.Active=true;child.Active=false;child.Visible=false;throw new InvalidOperationException();};
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.False(spring.Active);Assert.True(child.Active);Assert.True(child.Visible);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(ShaftKind.Clutch)]
    [InlineData(ShaftKind.Reverse)]
    public void ShaftReadingsUseCurrentPhysicsWithoutObserverCallbacks(ShaftKind kind)
    {
        var world=World();
        try
        {
            MachinePart part;
            JointSlot input;
            Func<float> speed,angle;
            switch(kind)
            {
                case ShaftKind.Clutch:
                    var clutch=new ClutchPart();
                    Attach(world,clutch,ClutchCatalogue,FixturePartId.First,new(0,6,0));
                    part=clutch;input=ClutchPart.InputJoint;speed=()=>clutch.InputSpeed;angle=()=>clutch.InputAngle;
                    break;
                case ShaftKind.Reverse:
                    var reverse=new ReverseTransmissionPart();
                    Attach(world,reverse,ReverseCatalogue,FixturePartId.First,new(0,6,0));
                    part=reverse;input=ReverseTransmissionPart.InputJoint;speed=()=>reverse.InputSpeed;angle=()=>reverse.InputAngle;
                    break;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
            Assert.Equal(0,speed());Assert.Equal(0,angle());
            world.Start();
            var joint=(PhysicsFrameJoint)world.CurrentJoint(new(part,input));
            var initial=world.Physics.Capture();
            world.Physics.ApplyImpulse(joint.A.Id,joint.A.Pose.Rotation.Apply(new(0,.01,0)),
                joint.A.Center+joint.A.Pose.Rotation.Apply(new(1,0,0)));
            Assert.NotEqual(0,speed());
            world.Physics.Step([],[],.01);
            Assert.Equal((float)-joint.Motion.Speed,speed());
            Assert.Equal((float)-joint.Motion.Coordinate,angle());
            Assert.NotEqual(0,angle());
            world.Physics.Restore(initial);
            Assert.Equal(0,speed());Assert.Equal(0,angle());
        }
        finally {world.Free();}
    }
}
