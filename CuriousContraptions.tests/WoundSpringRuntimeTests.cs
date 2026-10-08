using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WoundSpringRuntimeTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GravityCanWindThePassiveLatchWithoutElectricalWork(bool gravity)
    {
        var world=World();world.Gravity=gravity?9.81f:0;
        try
        {
            var spring=Build(world,false,false);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var bodies=world.PhysicsAssembly.Declarations.ToArray()
                .Select(d=>world.PhysicsAssembly.Body(new(d.Geometry.Owner,d.Geometry.Slot)))
                .Where(b=>b.MotionType==PhysicsMotionType.Dynamic).ToArray();
            var initialHeights=bodies.ToDictionary(b=>b.Id,b=>b.Center.Y);
            var motor=Assert.IsType<MotorPart>(Find(world,Role.Motor));
            for(var i=0;i<120;i++)
            {
                world.Step();
                var gravitationalWork=bodies.Sum(b=>world.Gravity*(initialHeights[b.Id]-b.Center.Y)/b.InverseMass);
                var storedAndKinetic=spring.StoredEnergy+bodies.Sum(b=>b.KineticEnergy);
                Assert.Equal(0,motor.SuppliedWork);
                Assert.InRange(storedAndKinetic,0,Math.Max(0,gravitationalWork)+1e-6);
            }
            if(gravity)
            {
                Assert.True(spring.Compression>0);
                Assert.True(spring.StoredEnergy>0);
                Assert.True(Guide(world,spring).A.Center.Y<initialHeights[Guide(world,spring).A.Id]);
            }
            else
            {
                Assert.Equal(0,spring.Compression);
                Assert.Equal(0,spring.StoredEnergy);
            }
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,Assert.IsType<WoundSpringPart>(Find(world,Role.Spring)).StoredEnergy);
        }
        finally {world.Free();}
    }
    private enum Role { Battery, Motor, Spring, Payload, Wall }
    private enum PayloadParameter { Mass }
    private enum SupplyParameter { Enabled }
    private static string WireId(Role role) => role switch
    {
        Role.Battery => "supply",
        Role.Motor => "motor",
        Role.Spring => "spring",
        Role.Payload => "payload",
        Role.Wall => "wall",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Catalog(Role role) => role switch
    {
        Role.Battery => "battery",
        Role.Motor => "motor",
        Role.Spring => WoundSpringPart.CatalogId,
        Role.Payload => "ball",
        Role.Wall => "wall",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Add(MachineWorld world, Role role, Vector3 position) =>
        world.AddPart(new() { Id = WireId(role), Kind = Catalog(role), Position = [position.X, position.Y, position.Z] });
    private static MachinePart Find(MachineWorld world, Role role) =>
        world.FindPart(WireId(role)) ?? throw new InvalidOperationException("Missing test part.");
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static WoundSpringPart Build(MachineWorld world, bool supplied, bool rotated)
    {
        var battery = Add(world, Role.Battery, new(-5, 8, 2));
        var motor = Add(world, Role.Motor, new(-3, 8, 2));
        var spring = (WoundSpringPart)Add(world, Role.Spring, new(0, 8, 0));
        if (rotated) spring.RotationDegrees = new(23, 37, 19);
        if (supplied) Assert.True(world.Connect(battery, motor));
        Assert.True(world.Connect(motor, spring));
        return spring;
    }
    private static PhysicsFrameJoint Guide(MachineWorld world, WoundSpringPart spring) =>
        Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(spring, WoundSpringPart.PlungerGuide)));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void WindingUsesSameOwnedBodyAndMeasuredMotorWork(bool supplied, bool rotated)
    {
        var world = World();
        try
        {
            var spring = Build(world, supplied, rotated);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            var head = world.PhysicsAssembly.Body(new(spring.Plunger, MachinePart.RootBody));
            var initial = Guide(world, spring).Travel.Error;
            for (var i = 0; i < 60; i++)
            {
                world.Step();
                Assert.Same(head, Guide(world, spring).A);
                Assert.InRange(Guide(world, spring).Error(1e-8), 0, 1.01e-7);
                Assert.InRange(Math.Abs(spring.Compression - (initial - Guide(world, spring).Travel.Error)), 0, 1e-7);
                Assert.Same(world.PhysicsAssembly.Body(new(spring,WoundSpringPart.ShaftBody)),
                    MechanicalNetwork.Shaft(world,spring,SocketId.DriveIn).A);
            }
            if (supplied)
            {
                Assert.True(Guide(world, spring).Travel.Error < initial - .1);
                Assert.True(spring.StoredEnergy > .8);
                Assert.True(spring.AcceptedWork > 0);
                // An energy budget assertion, not equality between motor work and potential.
                Assert.InRange(spring.StoredEnergy + head.KineticEnergy, 0, spring.AcceptedWork * 1.02 + .01);
            }
            else
            {
                Assert.InRange(Math.Abs(initial - Guide(world, spring).Travel.Error), 0, 1e-7);
                Assert.Equal(0, spring.AcceptedWork);
                Assert.InRange(spring.StoredEnergy, 0, 1e-12);
                world.Activate(spring);
                world.Step(); world.Step();
                Assert.Equal(SpringTriggerResult.Empty, spring.LastTrigger);
                Assert.Equal(0, spring.ReleaseCount);
            }
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            Assert.Equal(0, ((WoundSpringPart)Find(world, Role.Spring)).AcceptedWork);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PowerLossHoldsChargeAndReleaseMovesOwnedHeadWithoutRecharge(bool rotated)
    {
        var world = World();
        try
        {
            var spring = Build(world, true, rotated);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for (var i = 0; i < 60; i++) world.Step();
            world.QueueBinaryInput(new(Find(world, Role.Battery),BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            for (var i = 0; i < 30; i++) world.Step();
            var coordinate = Guide(world, spring).Travel.Error;
            var energy = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            var state=world.Physics.Spring(Guide(world,spring).Id);
            const double coordinateTolerance=1e-7;
            var energyBound=state.Declaration.Stiffness*
                (Math.Abs(state.Compression)*coordinateTolerance+.5*coordinateTolerance*coordinateTolerance);
            var motor=Assert.IsType<MotorPart>(Find(world,Role.Motor));
            var supplied=motor.SuppliedWork;
            for (var i = 0; i < 30; i++) world.Step();
            Assert.InRange(Math.Abs(Guide(world, spring).Travel.Error - coordinate), 0, 1e-7);
            Assert.InRange(Math.Abs(spring.AcceptedWork-accepted),0,energyBound);
            Assert.InRange(Math.Abs(spring.StoredEnergy-energy),0,energyBound);
            Assert.Equal(supplied,motor.SuppliedWork);
            var current=world.Physics.Spring(Guide(world,spring).Id);
            Assert.Equal(.5*current.Declaration.Stiffness*current.Compression*current.Compression,spring.StoredEnergy);
            var releaseEnergy=spring.StoredEnergy;
            world.Activate(spring);
            var head = Guide(world, spring).A;
            var maximumEnergy = energy;
            for (var i = 0; i < 90; i++)
            {
                world.Step();
                maximumEnergy = Math.Max(maximumEnergy, spring.StoredEnergy + head.KineticEnergy);
                Assert.Same(head, Guide(world, spring).A);
            }
            Assert.Equal(1, spring.ReleaseCount);
            Assert.InRange(Math.Abs(spring.AcceptedWork-accepted),0,energyBound);
            Assert.Equal(supplied,motor.SuppliedWork);
            Assert.InRange(spring.StoredEnergy, 0, 1e-12);
            Assert.InRange(spring.ReleasedWork, releaseEnergy - 1e-9, releaseEnergy + 1e-9);
            Assert.InRange(head.LinearVelocity.Length, 0, 1e-7);
            Assert.InRange(maximumEnergy, 0, energy * 1.02 + .01);
            Assert.Equal(WoundSpringPhase.Idle, spring.Phase);
            world.Activate(spring); world.Step(); world.Step();
            Assert.Equal(SpringTriggerResult.Empty, spring.LastTrigger);
            Assert.Equal(1, spring.ReleaseCount);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ConstructionRotationIsCapturedBeforeRunAndPresentationCannotMoveTheBody()
    {
        var world = World();
        try
        {
            var spring = Build(world, true, true);
            world.Start();
            for (var i = 0; i < 40; i++) world.Step();
            world.QueueBinaryInput(new(Find(world, Role.Battery),BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            world.Activate(spring); world.Step(); world.Step();
            var head = Guide(world, spring).A;
            var state = world.Physics.Capture();
            var position = head.Center;
            var energy = spring.StoredEnergy;
            world.PresentFrame(.01,1);
            Assert.InRange(spring.LatchAngle, -.081f, -.079f);
            for (var i = 0; i < 20; i++) world.PresentFrame(.01,1);
            Assert.Equal(position, head.Center);
            Assert.Equal(state.BodyStates.ToArray(), world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(energy, spring.StoredEnergy);
        }
        finally { world.Free(); }
    }

    public enum GravityScenario { Enabled, Disabled }
    [Theory]
    [InlineData(.5f,true,GravityScenario.Enabled)]
    [InlineData(2f,true,GravityScenario.Enabled)]
    [InlineData(.5f,false,GravityScenario.Enabled)]
    [InlineData(2f,false,GravityScenario.Enabled)]
    [InlineData(.5f,true,GravityScenario.Disabled)]
    [InlineData(2f,true,GravityScenario.Disabled)]
    public void ReleasedHeadLaunchesPayloadThroughSharedContacts(float mass,bool released,GravityScenario gravityScenario)
    {
        if(!Enum.IsDefined(gravityScenario))throw new ArgumentOutOfRangeException(nameof(gravityScenario));
        var world = World();
        try
        {
            world.Gravity = gravityScenario==GravityScenario.Enabled?9.81f:0;
            var spring = Build(world, true, false);
            var ball = Add(world, Role.Payload, spring.Position + Vector3.Up * 1.6f);
            // Mass is authored before capture; rebuild through the construction boundary.
            var spec = ball.Serialize();
            world.RemovePart(ball);
            spec.Properties[PartParameterName.Of(PayloadParameter.Mass)] = mass;
            ball = world.AddPart(spec);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            world.QueueBinaryInput(new(Find(world, Role.Battery),BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            for (var i = 0; i < 30; i++) world.Step();
            var payload = world.PhysicsAssembly.Body(new(ball, MachinePart.RootBody));
            var head = Guide(world, spring).A;
            var initialHeight = payload.Center.Y;
            var charge = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            var dynamicBodies=world.PhysicsAssembly.Declarations.ToArray()
                .Select(d=>world.PhysicsAssembly.Body(new(d.Geometry.Owner,d.Geometry.Slot)))
                .Where(b=>b.MotionType==PhysicsMotionType.Dynamic).ToArray();
            var initialHeights=dynamicBodies.ToDictionary(b=>b.Id,b=>b.Center.Y);
            var initialMechanical=charge+dynamicBodies.Sum(b=>b.KineticEnergy);
            var motor=Assert.IsType<MotorPart>(Find(world,Role.Motor));
            var supplied=motor.SuppliedWork;
            var springState=world.Physics.Spring(Guide(world,spring).Id);
            var stiffness=springState.Declaration.Stiffness;
            const double mechanicalError=1e-6;
            var peak = initialHeight;
            var contacted = false;
            if (released) world.Activate(spring);
            for (var i = 0; i < 90; i++)
            {
                world.Step();
                peak = Math.Max(peak, payload.Center.Y);
                var gravitationalWork=dynamicBodies.Sum(b=>world.Gravity*(initialHeights[b.Id]-b.Center.Y)/b.InverseMass);
                var mechanical=spring.StoredEnergy+dynamicBodies.Sum(b=>b.KineticEnergy);
                // All dynamic bodies participate; motor supply is off. Contact,
                // ratchet and drive losses may dissipate energy, never create it.
                Assert.Equal(supplied,motor.SuppliedWork);
                Assert.InRange(mechanical-initialMechanical-gravitationalWork,double.MinValue,mechanicalError);
                Assert.InRange(Math.Abs(charge+(spring.AcceptedWork-accepted)-spring.ReleasedWork-spring.StoredEnergy),
                    0,mechanicalError);
                var gap = (payload.Center - head.Center).Length - ball.Radius - WoundSpringPart.PlungerRadius;
                contacted |= gap >= -1e-6 && gap <= ConvexSweep.ContactDistance + 1e-6 &&
                    payload.LinearVelocity.Y > .1;
            }
            Assert.Equal(released ? 1 : 0, spring.ReleaseCount);
            var shouldContact=released&&gravityScenario==GravityScenario.Enabled;
            Assert.Equal(shouldContact,contacted);
            if (released)
            {
                if(shouldContact)Assert.True(peak > initialHeight + .5, $"mass={mass}; initial={initialHeight}; peak={peak}");
                else Assert.InRange(peak-initialHeight,0,1e-6);
                // After the rest stop relatches, gravity alone can rewind an
                // unloaded head: .5*k*q^2=m*g*q, q_turn=2*m*g/k.
                var headWeight=world.Gravity/head.InverseMass;
                var gravityRewind=2*headWeight*headWeight/stiffness;
                Assert.InRange(spring.AcceptedWork-accepted,0,gravityRewind+mechanicalError);
                if(gravityScenario==GravityScenario.Disabled)
                    Assert.InRange(spring.AcceptedWork-accepted,0,mechanicalError);
                Assert.InRange(spring.ReleasedWork, charge - 1e-8, charge + 1e-8);
            }
            else
            {
                Assert.Equal(accepted,spring.AcceptedWork);
                Assert.InRange(peak - initialHeight, 0, 1e-6);
                Assert.InRange(Math.Abs(spring.StoredEnergy - charge), 0, 1e-9);
                Assert.Equal(0, spring.ReleasedWork);
            }
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void SolidObstacleLimitsWindingThroughSharedCollision()
    {
        var world = World();
        try
        {
            var spring = Build(world, true, false);
            var wall = (WallPart)Add(world, Role.Wall, spring.Position);
            wall.RotationDegrees = new(90, 0, 0);
            wall.SetDimensions(new(3, 2, .25f));
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            var coordinate = Guide(world, spring).Travel.Error;
            Assert.InRange(coordinate, .445 - 1e-6, .445 + ConvexSweep.ContactDistance + 1e-6);
            Assert.InRange(spring.StoredEnergy, 16.55, 16.57);
            Assert.Equal(WoundSpringPhase.Blocked, spring.Phase);
            var energy = spring.StoredEnergy;
            for (var i = 0; i < 30; i++) world.Step();
            Assert.InRange(Math.Abs(spring.StoredEnergy - energy), 0, 1e-6);
            Assert.InRange(Math.Abs(Guide(world, spring).Travel.Error - coordinate), 0, 1e-7);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void FixtureBoundariesRejectUndefinedRoles()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WireId((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Catalog((Role)999));
        Assert.Equal(WoundSpringPart.CatalogId, Catalog(Role.Spring));
        Assert.Equal("spring", WireId(Role.Spring));
    }
}
