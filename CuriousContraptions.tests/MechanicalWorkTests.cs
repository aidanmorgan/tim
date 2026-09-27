using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class MechanicalWorkTests(HeadlessFixture godot)
{
    private const string BatteryId = "battery";
    private const string MotorId = "motor";
    private const string FirstId = "first";
    private const string SecondId = "second";
    private const string BatteryKind = "battery";
    private const string MotorKind = "motor";
    private const string ConveyorKind = "conveyor";
    private const string ClutchKind = "clutch";
    private const string ReverseKind = "reverse_transmission";
    private const string BallKind = "ball";
    private const string BallId = "payload";
    private const string ClutchId = "coupling";
    private const string ReverseId = "reverse";
    private const string EnabledParameter = "enabled";
    private const string SwitchKind = "switch";
    private const string SwitchId = "coil_switch";
    private const float Delta = MachineWorld.Tick / MachineWorld.Substeps;

    private MachineWorld World()
    {
        var world = new MachineWorld(); godot.Tree.Root.AddChild(world); return world;
    }
    private (MachinePart Battery, MotorPart Motor) Power(MachineWorld world)
    {
        var battery = world.AddPart(new() { Id = BatteryId, Kind = BatteryKind, Position = [-5, 5, 0] });
        var motor = (MotorPart)world.AddPart(new() { Id = MotorId, Kind = MotorKind, Position = [-3, 5, 0] });
        Assert.True(world.Connect(battery, motor));
        return (battery, motor);
    }
    private ConveyorPart Conveyor(MachineWorld world, string id, float x) =>
        (ConveyorPart)world.AddPart(new() { Id = id, Kind = ConveyorKind, Position = [x, 2, 0] });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ParallelAndSerialConsumersShareRatherThanDuplicateWork(bool serial, bool reverseOrder)
    {
        var world = World();
        try
        {
            var (_, motor) = Power(world);
            var a = Conveyor(world, FirstId, -2);
            var b = Conveyor(world, SecondId, 2);
            Assert.True(world.Connect(motor, a));
            Assert.True(world.Connect(serial ? a : motor, b));
            if (reverseOrder) world.Connections.Reverse();
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(6, a.ShaftSpeed); Assert.Equal(6, b.ShaftSpeed);
            Assert.Equal(10, a.MechanicalTorque(SocketId.DriveIn));
            Assert.Equal(10, b.MechanicalTorque(SocketId.DriveIn));
            var sum = a.MechanicalWorkAvailable(SocketId.DriveIn) + b.MechanicalWorkAvailable(SocketId.DriveIn);
            Assert.Equal(6d * 20 * Delta, sum, 12);
            var spring = new LatchedSpringStore(100, 1, .125f);
            var allowed = a.MechanicalWorkAvailable(SocketId.DriveIn);
            var winding = spring.Wind(a.ShaftSpeed * Delta, (float)a.MechanicalTorque(SocketId.DriveIn), 1, allowed);
            a.ConsumeMechanicalWork(SocketId.DriveIn, winding.Work);
            Assert.InRange(spring.Energy, 0, allowed);
            Assert.Equal(allowed - winding.Work, a.MechanicalWorkAvailable(SocketId.DriveIn));
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            a = (ConveyorPart)world.FindPart(FirstId)!;
            Assert.Equal(0, a.MechanicalTorque(SocketId.DriveIn));
            Assert.Equal(0, a.MechanicalWorkAvailable(SocketId.DriveIn));
            Assert.Equal(0, a.DeliveredWork);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void OpenClutchAndDisconnectedOutputsDoNotTakeAnActiveBranchesAllowance()
    {
        var world = World();
        try
        {
            var (battery, motor) = Power(world);
            var a = Conveyor(world, FirstId, -2);
            var b = Conveyor(world, SecondId, 2);
            var clutch = world.AddPart(new() { Id = ClutchId, Kind = ClutchKind, Position = [0, 5, 0] });
            var reverse = world.AddPart(new() { Id = ReverseId, Kind = ReverseKind, Position = [3, 5, 0] });
            Assert.True(world.Connect(motor, a));
            Assert.True(world.Connect(motor, clutch));
            Assert.True(world.Connect(clutch, b));
            Assert.True(world.Connect(motor, reverse)); // no downstream consumer
            var coilSwitch = world.AddPart(new() { Id = SwitchId, Kind = SwitchKind, Position = [5, 5, 0] });
            Assert.True(world.Connect(battery, coilSwitch));
            Assert.True(world.Connect(coilSwitch, clutch));
            world.Start();
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(20, a.MechanicalTorque(SocketId.DriveIn));
            Assert.Equal(0, b.MechanicalWorkAvailable(SocketId.DriveIn));
            Assert.Equal(0, reverse.MechanicalWorkAvailable(SocketId.Drive));
            coilSwitch.Active = true;
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(10, a.MechanicalTorque(SocketId.DriveIn));
            Assert.Equal(10, b.MechanicalTorque(SocketId.DriveIn));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void CoastingMotorKeepsItsVisualMotionButCannotSupplyWork()
    {
        var world = World();
        try
        {
            var (battery, motor) = Power(world);
            var belt = Conveyor(world, FirstId, 0);
            Assert.True(world.Connect(motor, belt));
            world.Start();
            for (var i = 0; i < 120; i++) world.Step();
            Assert.True(belt.MechanicalWorkAvailable(SocketId.DriveIn) > 0);
            battery.Properties[EnabledParameter] = 0;
            world.Step();
            Assert.True(belt.ShaftSpeed > 0);
            var ball = world.AddPart(new() { Id = BallId, Kind = BallKind, Position = [0, 8, 0] });
            ball.Position = belt.Position + Vector3.Up * (.12f + ball.Radius);
            ball.Velocity = Vector3.Zero;
            belt.OnContact(ball, 1, world);
            Assert.Equal(Vector3.Zero, ball.Velocity);
            Assert.DoesNotContain(new MachineEvent(MachineEventKind.Transported, belt.Uid, ball.Uid), world.Events.Keys);
            Assert.Equal(0, belt.MechanicalTorque(SocketId.DriveIn));
            Assert.Equal(0, belt.MechanicalWorkAvailable(SocketId.DriveIn));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReverserPreservesWorkBudgetAndConsumerCannotOverdraw(bool reversed)
    {
        var world = World();
        try
        {
            var (_, motor) = Power(world);
            var belt = Conveyor(world, FirstId, 0);
            MachinePart source = motor;
            if (reversed)
            {
                source = world.AddPart(new() { Id = ReverseId, Kind = ReverseKind, Position = [0, 5, 0] });
                Assert.True(world.Connect(motor, source));
            }
            Assert.True(world.Connect(source, belt));
            world.Start(); for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(reversed ? -6 : 6, belt.ShaftSpeed);
            var available = belt.MechanicalWorkAvailable(SocketId.DriveIn);
            Assert.Equal(6d * 20 * Delta, available);
            Assert.Throws<ArgumentOutOfRangeException>(() => belt.ConsumeMechanicalWork(SocketId.DriveIn, available + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => belt.ConsumeMechanicalWork(SocketId.DriveIn, double.NaN));
            Assert.Equal(available, belt.MechanicalWorkAvailable(SocketId.DriveIn));
            belt.ConsumeMechanicalWork(SocketId.DriveIn, available);
            Assert.Equal(0, belt.MechanicalWorkAvailable(SocketId.DriveIn));
            world.Step();
            Assert.Equal(available, belt.MechanicalWorkAvailable(SocketId.DriveIn));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RepeatedCargoContactsShareBothWorkAndTractionImpulse()
    {
        var world = World();
        try
        {
            var (_, motor) = Power(world);
            var belt = Conveyor(world, FirstId, 0);
            var ball = world.AddPart(new() { Id = BallId, Kind = BallKind, Position = [0, 8, 0] });
            Assert.True(world.Connect(motor, belt));
            world.Start(); for (var i = 0; i < 60; i++) world.Step();
            // Isolate one already solved substep; repeated contacts must not refill its allowance.
            ball.Position = belt.Position + Vector3.Up * (.12f + ball.Radius);
            ball.Velocity = Vector3.Zero;
            MechanicalNetwork.Solve(world, Delta); belt.MechanicalStep(world, Delta);
            var budget = belt.MechanicalWorkAvailable(SocketId.DriveIn);
            var before = belt.DeliveredWork;
            for (var i = 0; i < 100; i++) belt.OnContact(ball, 1, world);
            Assert.InRange(ball.Velocity.X, 0, belt.ReadParameter(ConveyorParameter.Traction) * Delta / ball.Mass + 1e-7);
            Assert.InRange(belt.DeliveredWork - before, 0, budget);
            Assert.InRange(.5 * ball.Mass * ball.Velocity.LengthSquared(), 0, budget);
        }
        finally { world.Free(); }
    }

    private enum ProbeRole { Source, Gear, Load }
    private const string ProbeKind = "mechanical_work_probe";
    private partial class Probe : MachinePart
    {
        public ProbeRole Role { get; init; }
        public float Speed { get; set; } = 3;
        public float Torque { get; set; } = 7;
        public float Ratio { get; set; } = 1;
        public override IEnumerable<ConnectionPort> ConnectionPorts => Role switch
        {
            ProbeRole.Source => [new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output, Vector3.Zero)],
            ProbeRole.Gear => [
                new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, Vector3.Zero),
                new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output, Vector3.One)],
            ProbeRole.Load => [new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, Vector3.Zero)],
            _ => throw new InvalidOperationException("Unknown mechanical test role.")
        };
        public override IEnumerable<MechanicalSource> MechanicalSources =>
            Role == ProbeRole.Source ? [new(SocketId.Drive, Speed, Torque)] : [];
        public override IEnumerable<MechanicalRoute> MechanicalRoutes =>
            Role == ProbeRole.Gear ? [new(SocketId.DriveIn, SocketId.Drive, Ratio, true)] : [];
        public override IEnumerable<SocketId> MechanicalLoads =>
            Role == ProbeRole.Load ? [SocketId.DriveIn] : [];
        protected override void Build() { }
    }
    private Probe AddProbe(MachineWorld world, string id, ProbeRole role)
    {
        var probe = new Probe { Role = role, Definition = new PartDefinition { Id = ProbeKind } };
        probe.Configure(new() { Id = id, Kind = ProbeKind });
        world.AddChild(probe); world.Parts.Add(probe); return probe;
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(-2f)]
    [InlineData(.1f)]
    [InlineData(-.1f)]
    [InlineData(1.3f)]
    public void GearRatioChangesTorqueInverselyWithoutIncreasingPower(float ratio)
    {
        var world = World();
        try
        {
            var source = AddProbe(world, MotorId, ProbeRole.Source);
            var gear = AddProbe(world, ReverseId, ProbeRole.Gear); gear.Ratio = ratio;
            var load = AddProbe(world, FirstId, ProbeRole.Load);
            Assert.True(world.Connect(source, gear)); Assert.True(world.Connect(gear, load));
            MechanicalNetwork.Solve(world, Delta);
            Assert.Equal(3 * ratio, load.MechanicalSpeed(SocketId.DriveIn));
            Assert.InRange(load.MechanicalTorque(SocketId.DriveIn), 7 / Math.Abs((double)ratio) * .9999998, 7 / Math.Abs((double)ratio));
            Assert.InRange(load.MechanicalWorkAvailable(SocketId.DriveIn), 21d * Delta * .9999998, 21d * Delta);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void InvalidSourceAndOverflowRejectAtomicallyWithoutRefillingSpentWork()
    {
        var world = World();
        try
        {
            var source = AddProbe(world, MotorId, ProbeRole.Source);
            var gear = AddProbe(world, ReverseId, ProbeRole.Gear);
            var load = AddProbe(world, FirstId, ProbeRole.Load);
            Assert.True(world.Connect(source, gear)); Assert.True(world.Connect(gear, load));
            MechanicalNetwork.Solve(world, Delta);
            load.ConsumeMechanicalWork(SocketId.DriveIn, .01);
            var remaining = load.MechanicalWorkAvailable(SocketId.DriveIn);
            foreach (var torque in new[] { -1f, float.NaN, float.PositiveInfinity })
            {
                source.Torque = torque;
                Assert.Throws<ArgumentException>(() => MechanicalNetwork.Solve(world, Delta));
                Assert.Equal(remaining, load.MechanicalWorkAvailable(SocketId.DriveIn));
            }
            source.Torque = 7; source.Speed = float.MaxValue; gear.Ratio = 2;
            Assert.Throws<InvalidOperationException>(() => MechanicalNetwork.Solve(world, Delta));
            Assert.Equal(remaining, load.MechanicalWorkAvailable(SocketId.DriveIn));
            Assert.Equal(3, load.MechanicalSpeed(SocketId.DriveIn));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(ConveyorParameter.SurfacePerRadian, 0f)]
    [InlineData(ConveyorParameter.SurfacePerRadian, float.NaN)]
    [InlineData(ConveyorParameter.Length, -1f)]
    [InlineData(ConveyorParameter.Width, 0f)]
    [InlineData(ConveyorParameter.Traction, float.PositiveInfinity)]
    public void InvalidConveyorParametersCannotEnterTheDriveSolver(ConveyorParameter key, float value)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new()
                { Id = FirstId, Kind = ConveyorKind, Properties = new() { [PartParameterName.Of(key)] = value } }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(101f)]
    public void InvalidAuthoredMotorTorqueIsRejected(float torque)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new()
            {
                Id = MotorId, Kind = MotorKind, Properties = new() { [PartParameterName.Of(MotorParameter.Torque)] = torque }
            }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }
}
