using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WoundSpringTests(HeadlessFixture godot, ITestOutputHelper output)
{
    private const string BatteryId = "supply", MotorId = "motor", SpringId = "launcher", PayloadId = "payload", UpperPayloadId = "upper_payload";
    private const string BatteryKind = "battery", MotorKind = "motor", BallKind = "ball";
    private enum PayloadParameter { Mass, Radius }
    public enum LoadingCase { OffCentre, Oversized, OversizedWeight, Stacked }
    private enum SupplyParameter { Enabled }
    private MachineWorld World()
    {
        var world = new MachineWorld(); godot.Tree.Root.AddChild(world); return world;
    }
    private (WoundSpringPart Spring, MachinePart Payload) Build(MachineWorld world, bool supplied, float mass = 1)
    {
        var battery = world.AddPart(new() { Id = BatteryId, Kind = BatteryKind, Position = [-5, 3, 2] });
        var motor = world.AddPart(new() { Id = MotorId, Kind = MotorKind, Position = [-3, 3, 2] });
        var spring = (WoundSpringPart)world.AddPart(new() { Id = SpringId, Kind = WoundSpringPart.CatalogId, Position = [0, 3, 0] });
        var ball = world.AddPart(new() { Id = PayloadId, Kind = BallKind, Position = [0, 5, 0], Properties = new() { [PartParameterName.Of(PayloadParameter.Mass)] = mass } });
        if (supplied) Assert.True(world.Connect(battery, motor));
        Assert.True(world.Connect(motor, spring));
        return (spring, ball);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SuppliedWindingAndTriggeredPhysicalRelease(bool supplied)
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, supplied);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            Assert.Equal(2, world.Bodies.Count);
            var stored = spring.StoredEnergy;
            var start = ball.Position.Y;
            world.Activate(spring);
            var top = start;
            for (var i = 0; i < 90; i++) { world.Step(); top = Mathf.Max(top, ball.Position.Y); }
            if (supplied)
            {
                Assert.InRange(stored, 50, 52);
                Assert.Equal(1, spring.ReleaseCount);
                Assert.True(top > start + 1, $"payload start={start}, top={top}, spring={spring.Compression}, state={spring.Phase}");
            }
            else
            {
                Assert.Equal(0, stored);
                Assert.Equal(0, spring.ReleaseCount);
                Assert.InRange(top - start, 0, .01f);
            }
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            spring = (WoundSpringPart)world.FindPart(SpringId)!;
            Assert.Equal(0, spring.StoredEnergy);
            Assert.Equal(0, spring.ReleaseCount);
            Assert.Single(spring.InternalBodies);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(.5f)]
    [InlineData(1f)]
    [InlineData(4f)]
    public void RetainedChargeDrivesMassDependentStrokeWithoutRecharge(float mass)
    {
        var world = World();
        try
        {
            world.Pressure = 0;
            var (spring, ball) = Build(world, true, mass);
            world.Start(); for (var i = 0; i < 240; i++) world.Step();
            world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
            for (var i = 0; i < 120; i++) world.Step();
            var stored = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            var initialBallY = ball.Position.Y;
            var initialHeadY = spring.Plunger.Position.Y;
            Assert.InRange(stored, 50, 52);
            world.Activate(spring);
            var peak = ball.Position.Y;
            var maximumEnergy = 0d;
            for (var i = 0; i < 150; i++)
            {
                world.Step(); peak = Mathf.Max(peak, ball.Position.Y);
                var kinetic = .5 * ball.Mass * ball.Velocity.LengthSquared() +
                    .5 * spring.Plunger.Mass * spring.Plunger.Velocity.LengthSquared();
                var gravitational = world.Gravity * (ball.Mass * (ball.Position.Y - initialBallY) +
                    spring.Plunger.Mass * (spring.Plunger.Position.Y - initialHeadY));
                maximumEnergy = Math.Max(maximumEnergy, kinetic + gravitational + spring.StoredEnergy);
                var coordinate = (spring.Plunger.Position - spring.Position).Dot(spring.Basis.Y);
                Assert.InRange(coordinate, WoundSpringPart.RestHeadY - .8001f, WoundSpringPart.RestHeadY + .0001f);
                Assert.InRange(Math.Abs(WoundSpringPart.RestHeadY - coordinate - spring.Compression), 0, .001);
            }
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.InRange(maximumEnergy, 0, stored * 1.02 + .01);
            Assert.True(peak > initialBallY + .1f, $"mass={mass}, gain={peak-initialBallY}, max-energy={maximumEnergy}");
            Assert.InRange(peak - initialBallY, 0, (float)(stored / (mass * world.Gravity) + .1));
            world.Activate(spring); for (var i = 0; i < 5; i++) world.Step();
            Assert.Equal(1, spring.ReleaseCount);
            Assert.Equal(SpringTriggerResult.Empty, spring.LastTrigger);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void EmptyReleaseMovesTheSameInternalMassAndSpendsItsCharge()
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.RemovePart(ball);
            var head = spring.Plunger;
            world.Start(); for (var i = 0; i < 240; i++) world.Step();
            world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
            world.Activate(spring);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Same(head, Assert.Single(world.Bodies));
            Assert.Equal(0, spring.StoredEnergy);
            Assert.InRange(spring.ReleasedWork, 50, 52);
            Assert.Equal(1, spring.ReleaseCount);
            Assert.Equal(WoundSpringPhase.Idle, spring.Phase);
            Assert.Equal(Vector3.Zero, head.Velocity);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void LatePhysicalObstructionStopsReleaseWithoutDiscardingStoredSpringEnergy()
    {
        const string WallId = "obstacle", WallKind = "wall";
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.RemovePart(ball);
            world.Start(); for (var i = 0; i < 240; i++) world.Step();
            world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
            var wall = (WallPart)world.AddPart(new() { Id = WallId, Kind = WallKind,
                Position = [0, 3.7f, 0], Rotation = [90, 0, 0] });
            wall.SetDimensions(new(2, 2, .2f));
            world.Activate(spring);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(WoundSpringPhase.Blocked, spring.Phase);
            Assert.InRange(spring.Plunger.Position.Y, 3.09f, 3.281f);
            Assert.InRange(spring.StoredEnergy, 25, 51.3);
            var retained = spring.StoredEnergy;
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(retained, spring.StoredEnergy);
            Assert.Equal(1, spring.ReleaseCount);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0, 0, 90)]
    [InlineData(45, 30, 20)]
    [InlineData(180, 0, 0)]
    public void RotatedPartialChargeAndRepeatedCyclesRestoreExactly(float x, float y, float z)
    {
        var world = World();
        try
        {
            world.Gravity = 0;
            var (spring, ball) = Build(world, true);
            world.RemovePart(ball);
            spring.RotationDegrees = new(x, y, z);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 1;
                for (var i = 0; i < 30; i++) world.Step();
                var stored = spring.StoredEnergy;
                Assert.InRange(stored, .01, 50);
                world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
                var accepted = spring.AcceptedWork;
                world.Activate(spring);
                for (var i = 0; i < 90; i++)
                {
                    world.Step();
                    var offset = spring.Plunger.Position - spring.Position;
                    var axis = spring.Basis.Y.Normalized();
                    Assert.InRange((offset - axis * offset.Dot(axis)).Length(), 0, .0001f);
                    Assert.InRange(offset.Dot(axis), WoundSpringPart.RestHeadY - .8001f,
                        WoundSpringPart.RestHeadY + .0001f);
                }
                Assert.Equal(cycle + 1, spring.ReleaseCount);
                Assert.Equal(0, spring.StoredEnergy);
                Assert.Equal(accepted, spring.AcceptedWork);
                Assert.Equal(Vector3.Zero, spring.Plunger.Velocity);
                Assert.Equal(WoundSpringPhase.Idle, spring.Phase);
            }
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }


    [Theory]
    [InlineData(0, 0, -15)]
    [InlineData(0, 0, 15)]
    [InlineData(15, 0, 0)]
    [InlineData(15, 30, -15)]
    public void RotatedLoadedReleaseUnderGravityUsesRetainedCharge(float x, float y, float z)
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            spring.RotationDegrees = new(x, y, z);
            var axis = spring.Basis.Y.Normalized();
            ball.Position = spring.Position + axis * 1.57f;
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
            for (var i = 0; i < 120; i++) world.Step();
            var energy = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            var start = ball.Position;
            Assert.InRange(energy, 51.19, 51.21);
            Assert.InRange((start - spring.Position).Dot(axis), .65f, .85f);
            var horizontal = new Vector3(axis.X, 0, axis.Z).Normalized();
            var peak = start.Y;
            var travel = 0f;
            world.Activate(spring);
            for (var i = 0; i < 150; i++)
            {
                world.Step();
                peak = Mathf.Max(peak, ball.Position.Y);
                travel = Mathf.Max(travel, (ball.Position - start).Dot(horizontal));
                var offset = spring.Plunger.Position - spring.Position;
                Assert.InRange((offset - axis * offset.Dot(axis)).Length(), 0, .0001f);
                Assert.InRange(offset.Dot(axis), WoundSpringPart.RestHeadY - .8001f,
                    WoundSpringPart.RestHeadY + .0001f);
            }
            Assert.True(peak > start.Y + .5f, $"rotation={spring.RotationDegrees}, rise={peak-start.Y}");
            Assert.True(travel > .3f, $"rotation={spring.RotationDegrees}, aimed travel={travel}");
            Assert.Equal(1, spring.ReleaseCount);
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.Equal(0, spring.StoredEnergy);
            Assert.InRange(spring.ReleasedWork, energy - .00001, energy + .00001);
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ImpactSwitchTriggersPartialChargeThroughTheActivationNetwork()
    {
        const string SwitchId = "trigger", SwitchKind = "switch", StrikerId = "striker";
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            var trigger = world.AddPart(new() { Id = SwitchId, Kind = SwitchKind, Position = [4, 2, -2] });
            world.AddPart(new() { Id = StrikerId, Kind = BallKind, Position = [4, 7, -2] });
            Assert.True(world.Connect(trigger, spring));
            world.Start();
            double firstCharge = 0, firstReleased = 0;
            var firstPeak = 0f;
            for (var i = 0; i < 960; i++)
            {
                world.Step();
                if (spring.ReleaseCount == 1)
                {
                    if (firstCharge == 0) firstCharge = spring.AcceptedWork;
                    firstReleased = spring.ReleasedWork;
                    firstPeak = Mathf.Max(firstPeak, ball.Position.Y);
                }
                Assert.True(double.IsFinite(spring.StoredEnergy));
                Assert.InRange(spring.Compression, 0, .80001f);
            }
            Assert.InRange(firstCharge, 1, 50);
            Assert.InRange(firstReleased, firstCharge - .0001, firstCharge + .0001);
            Assert.InRange(firstPeak, 4.6f, 7);
            Assert.True(spring.ReleaseCount >= 1);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false, 4f)]
    [InlineData(true, 4f)]
    [InlineData(false, float.Epsilon)]
    [InlineData(true, float.Epsilon)]
    public void RestingRatchetAbsorbsInwardContactInEitherBodyOrder(bool plungerFirst, float speed)
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            world.Activate(spring);
            world.Step(); // Trigger is queued for the following fixed tick.
            world.Step();
            var head = spring.Plunger;
            Assert.True(head.FreeMotion);
            head.Velocity = Vector3.Zero;
            var headPosition = head.Position;
            ball.Position = head.Position + Vector3.Up * (head.Radius + ball.Radius);
            ball.Velocity = Vector3.Down * speed;
            if (plungerFirst) BodyContact.Resolve(head, ball, Vector3.Down, 0, 0);
            else BodyContact.Resolve(ball, head, Vector3.Up, 0, 0);
            Assert.Equal(Vector3.Zero, head.Velocity);
            Assert.Equal(Vector3.Zero, ball.Velocity);
            Assert.Equal(headPosition, head.Position);
            var sweep = MovingSphereSweep.Cast(head.Position, head.Radius, head.Velocity,
                ball.Position, ball.Radius, ball.Velocity, MachineWorld.Tick);
            Assert.Equal(SphereSweepStatus.Clear, sweep.Status);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void SolidPanelLimitsWindingTravelAndAcceptedWork()
    {
        const string WallId = "winding_stop", WallKind = "wall";
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.RemovePart(ball);
            var wall = (WallPart)world.AddPart(new() { Id = WallId, Kind = WallKind,
                Position = [0, 3, 0], Rotation = [90, 0, 0] });
            wall.SetDimensions(new(3, 2, .25f));
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            var expectedHeadY = wall.Position.Y + wall.Dimensions.Z * .5f + WoundSpringPart.PlungerRadius;
            Assert.InRange(spring.Plunger.Position.Y, expectedHeadY - .0001f, expectedHeadY + .0001f);
            Assert.Equal(WoundSpringPhase.Blocked, spring.Phase);
            Assert.InRange(spring.Compression, .4549f, .4551f);
            Assert.InRange(spring.StoredEnergy, 16.55, 16.57);
            Assert.Equal(spring.AcceptedWork, spring.StoredEnergy);
            var accepted = spring.AcceptedWork;
            for (var i = 0; i < 240; i++) world.Step();
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.Equal(accepted, spring.StoredEnergy);
            Assert.Equal(0, spring.ReleasedWork);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void TimedGateClosesAfterWindingAndBlocksTheReleasedPlunger()
    {
        const string GateId = "shutter", GateKind = "powered_gate";
        const string TimerId = "gate_timer", TimerKind = "hold_timer";
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.RemovePart(ball);
            var gate = (PoweredGatePart)world.AddPart(new() { Id = GateId, Kind = GateKind,
                Position = [0, 3.6f, 0], Rotation = [0, 0, 90] });
            var timer = world.AddPart(new() { Id = TimerId, Kind = TimerKind, Position = [-5, 6, 0] });
            Assert.True(world.Connect(world.FindPart(BatteryId)!, timer));
            Assert.True(world.Connect(timer, gate));
            world.Start();
            world.Activate(timer);
            for (var i = 0; i < 420; i++) world.Step();
            Assert.InRange(spring.StoredEnergy, 50, 52);
            Assert.InRange(gate.Opening, 0, .0001f);
            world.Activate(spring);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(WoundSpringPhase.Blocked, spring.Phase);
            Assert.Equal(1, spring.ReleaseCount);
            Assert.InRange(spring.Plunger.Position.Y, 3.219f, 3.221f);
            Assert.InRange(spring.StoredEnergy, 36.9, 37.1);
            var retained = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            for (var i = 0; i < 240; i++) world.Step();
            Assert.Equal(retained, spring.StoredEnergy);
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.Equal(1, spring.ReleaseCount);
            world.FindPart(MotorId)!.Properties[PartParameterName.Of(MotorParameter.Speed)] = 0;
            for (var i = 0; i < 120; i++) world.Step();
            world.Activate(timer);
            for (var i = 0; i < 120; i++) world.Step();
            Assert.Equal(1, spring.ReleaseCount);
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.Equal(0, spring.StoredEnergy);
            Assert.InRange(spring.ReleasedWork, accepted - .00001, accepted + .00001);
            Assert.Equal(Vector3.Zero, spring.Plunger.Velocity);
            Assert.Equal(WoundSpringPhase.Idle, spring.Phase);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void LatchPresentationMovesContinuouslyWithoutChangingPhysics()
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            world.Start();
            for (var i = 0; i < 240; i++) world.Step();
            world.Activate(spring);
            world.Step(); world.Step();
            Assert.Equal(0, spring.LatchAngle);
            var headPosition = spring.Plunger.Position;
            var headVelocity = spring.Plunger.Velocity;
            var payloadPosition = ball.Position;
            var stored = spring.StoredEnergy;
            var accepted = spring.AcceptedWork;
            spring._Process(.01);
            Assert.InRange(spring.LatchAngle, -.081f, -.079f);
            for (var i = 0; i < 20; i++) spring._Process(.01);
            Assert.InRange(spring.LatchAngle, -.65001f, -.64999f);
            Assert.Equal(headPosition, spring.Plunger.Position);
            Assert.Equal(headVelocity, spring.Plunger.Velocity);
            Assert.Equal(payloadPosition, ball.Position);
            Assert.Equal(stored, spring.StoredEnergy);
            Assert.Equal(accepted, spring.AcceptedWork);
            for (var i = 0; i < 120; i++) world.Step();
            spring._Process(.01);
            Assert.InRange(spring.LatchAngle, -.571f, -.569f);
            for (var i = 0; i < 20; i++) spring._Process(.01);
            Assert.Equal(0, spring.LatchAngle);
            world.Restore();
            Assert.Equal(0, ((WoundSpringPart)world.FindPart(SpringId)!).LatchAngle);
        }
        finally { world.Free(); }
    }



    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestingPayloadContactIterationsRemainBounded(bool stacked)
    {
        var world = World();
        try
        {
            Build(world, true);
            if (stacked)
                world.AddPart(new() { Id = UpperPayloadId, Kind = BallKind, Position = [0,5.75f,0] });
            world.Start();
            var maximum = 0;
            long total = 0;
            for (var i = 0; i < 360; i++)
            {
                world.Step();
                maximum = Math.Max(maximum, world.MaximumFlightIterationsThisStep);
                total += world.MaximumFlightIterationsThisStep;
            }
            output.WriteLine($"stacked={stacked}, maximum={maximum}, total={total}, mean={total / 360.0}");
            // Performance regression budget, not a solver cutoff: every substep must still finish.
            Assert.True(maximum <= 64, $"stacked={stacked}, maximum={maximum}, total={total}, mean={total / 360.0}");
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(LoadingCase.OffCentre)]
    [InlineData(LoadingCase.Oversized)]
    [InlineData(LoadingCase.OversizedWeight)]
    [InlineData(LoadingCase.Stacked)]
    public void UnusualLoadsPreserveFiniteMotionStrokeEnergyAndReset(LoadingCase loading)
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            switch (loading)
            {
                case LoadingCase.OffCentre:
                    ball.Position += Vector3.Right * .05f;
                    break;
                case LoadingCase.Oversized:
                    var spec = ball.Serialize();
                    spec.Properties[PartParameterName.Of(PayloadParameter.Radius)] = .6f;
                    world.RemovePart(ball);
                    ball = world.AddPart(spec);
                    break;
                case LoadingCase.OversizedWeight:
                    // Catalogue boundary: use the actual default weight, not an edited ball radius.
                    const string weightCatalogId = "weight";
                    world.RemovePart(ball);
                    ball = world.AddPart(new() { Id = PayloadId, Kind = weightCatalogId, Position = [0, 5, 0] });
                    Assert.IsType<WeightPart>(ball);
                    Assert.InRange(ball.Radius, .507f, .509f);
                    break;
                case LoadingCase.Stacked:
                    world.AddPart(new() { Id = UpperPayloadId, Kind = BallKind, Position = [0,5.75f,0] });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loading));
            }
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            var bodies = world.Bodies.ToArray();
            var guide = Assert.Single(spring.Tubes);
            var oversizedMinimumAxial = guide.HalfLength + Mathf.Sqrt(Mathf.Max(0,
                ball.Radius * ball.Radius - guide.InnerRadius * guide.InnerRadius));
            void CheckBodies()
            {
                Assert.Equal(bodies.Length, world.Bodies.Count);
                foreach (var body in bodies)
                {
                    Assert.True(body.Position.IsFinite());
                    Assert.True(body.Velocity.IsFinite());
                }
                if (loading == LoadingCase.OversizedWeight)
                {
                    var local = guide.Pose.AffineInverse() * spring.ToLocal(ball.Position);
                    // Independent rim geometry: a sphere wider than the bore cannot enter it.
                    Assert.InRange(new Vector2(local.Y, local.Z).Length(), 0, guide.InnerRadius);
                    Assert.True(local.X >= oversizedMinimumAxial - 2 * SphereSweep.ContactTolerance);
                }
                var offset = spring.Plunger.Position - spring.Position;
                Assert.InRange(offset.Y, WoundSpringPart.RestHeadY - .8001f, WoundSpringPart.RestHeadY + .0001f);
                Assert.InRange(new Vector2(offset.X, offset.Z).Length(), 0, .0001f);
            }
            world.Start();
            for (var i = 0; i < 240; i++) { world.Step(); CheckBodies(); }
            world.FindPart(BatteryId)!.Properties[PartParameterName.Of(SupplyParameter.Enabled)] = 0;
            for (var i = 0; i < 120; i++) { world.Step(); CheckBodies(); }
            var accepted = spring.AcceptedWork;
            var energy = spring.StoredEnergy;
            var initialHeight = bodies.ToDictionary(body => body, body => body.Position.Y);
            var initialKinetic = bodies.Sum(body => .5 * body.Mass * body.Velocity.LengthSquared());
            var start = ball.Position.Y;
            var peak = start;
            world.Activate(spring);
            for (var i = 0; i < 180; i++)
            {
                world.Step(); CheckBodies();
                peak = Mathf.Max(peak, ball.Position.Y);
                var mechanicalEnergy = spring.StoredEnergy + bodies.Sum(body =>
                    .5 * body.Mass * body.Velocity.LengthSquared() +
                    body.Mass * world.Gravity * (body.Position.Y - initialHeight[body]));
                Assert.InRange(mechanicalEnergy, double.MinValue, (energy + initialKinetic) * 1.02 + .01);
            }
            Assert.Equal(accepted, spring.AcceptedWork);
            Assert.Equal(1, spring.ReleaseCount);
            Assert.Equal(0, spring.StoredEnergy);
            Assert.InRange(spring.ReleasedWork, energy - .00001, energy + .00001);
            if (loading == LoadingCase.Oversized)
                Assert.InRange(peak - start, 0, .01f);
            else if (loading == LoadingCase.OversizedWeight)
            {
                // The smaller catalogue weight rests on the rim but overlaps the rounded head's
                // terminal envelope. A physical tap is expected; exclusion is not immobility.
                Assert.True(peak > start + .01f);
                Assert.InRange(peak - start, 0, (energy + initialKinetic) / (ball.Mass * world.Gravity));
            }
            else
                Assert.True(peak > start + .1f, $"load={loading}, rise={peak-start}");
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InternalBodyIdCollisionIsRejectedBeforeAddingEitherOrder(bool springFirst)
    {
        var world = World();
        try
        {
            var springSpec = new PartSpec { Id = SpringId, Kind = WoundSpringPart.CatalogId, Position = [0,3,0] };
            var candidate = world.Registry.Create(springSpec);
            var internalId = candidate.InternalBodyId(InternalBodyRole.Plunger);
            candidate.Free();
            var conflicting = new PartSpec { Id = internalId, Kind = BallKind, Position = [4,5,0] };
            world.AddPart(springFirst ? springSpec : conflicting);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            var bodies = world.Bodies.ToArray();
            var children = world.GetChildCount();
            Assert.Throws<ArgumentException>(() => world.AddPart(springFirst ? conflicting : springSpec));
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            Assert.Equal(bodies, world.Bodies);
            Assert.Equal(children, world.GetChildCount());
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InternalBodyIdCollisionLoadPreservesCurrentMachine(bool springFirst)
    {
        var world = World();
        try
        {
            var (spring, _) = Build(world, true);
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            var conflict = new PartSpec { Id = spring.InternalBodyId(InternalBodyRole.Plunger),
                Kind = BallKind, Position = [4,5,0] };
            var owner = spring.Serialize();
            var invalid = new MachineData { Parts = springFirst ? [owner, conflict] : [conflict, owner] };
            var bodies = world.Bodies.ToArray();
            Assert.Throws<ArgumentException>(() => world.LoadMachine(invalid));
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            Assert.Equal(bodies, world.Bodies);
            Assert.Same(spring, world.FindPart(SpringId));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void InternalBodyRolesRejectUnknownOrUndeclaredMembers()
    {
        var world = World();
        try
        {
            var (spring, ball) = Build(world, true);
            Assert.Equal(spring.InternalBodyId(InternalBodyRole.Plunger), spring.Plunger.Uid);
            Assert.Throws<ArgumentOutOfRangeException>(() => spring.InternalBodyId((InternalBodyRole)999));
            Assert.Throws<ArgumentException>(() => ball.InternalBodyId(InternalBodyRole.Plunger));
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RemovingAssemblyRemovesItsInternalPhysicsBody()
    {
        var world = World();
        try
        {
            var (spring, _) = Build(world, true);
            var head = spring.Plunger;
            Assert.Contains(head, world.Bodies);
            world.RemovePart(spring);
            Assert.DoesNotContain(head, world.Bodies);
            Assert.Single(world.Bodies);
        }
        finally { world.Free(); }
    }
}
