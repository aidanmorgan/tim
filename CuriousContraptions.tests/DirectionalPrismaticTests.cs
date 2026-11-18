using System.Numerics;
using System.Buffers.Binary;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class DirectionalPrismaticTests
{
    // A 2 cm positional allowance protects visibly stationary held fixtures over a one-second observation.
    private const float HoldClearance = .02f;

    [Theory]
    [InlineData(PrismaticCommand.Hold, true, PrismaticDrivePhase.Holding)]
    [InlineData(PrismaticCommand.Conflict, true, PrismaticDrivePhase.Conflict)]
    [InlineData(PrismaticCommand.Positive, false, PrismaticDrivePhase.Unpowered)]
    [InlineData(PrismaticCommand.Negative, true, PrismaticDrivePhase.Holding)]
    public void InactiveCommandsCaptureAndHoldWithoutMotorWork(PrismaticCommand command, bool supplied, PrismaticDrivePhase phase)
    {
        var fixture = new ControllerFixture(gravity: -9.81f);
        fixture.Step(120, command, supplied);
        Assert.Equal(phase, fixture.Motor.Phase);
        Assert.True(fixture.Motor.Holding);
        Assert.InRange(fixture.Position.Y, 5f - HoldClearance, 5f + HoldClearance);
        Assert.Equal(0f, fixture.Motor.Work);
        Assert.Equal(0f, fixture.Motor.TickWork);
        Assert.Equal(0f, fixture.Motor.HoldCoordinate);
    }

    [Fact]
    public void IndependentDirectionsReleaseCapturedHoldAndReverse()
    {
        var fixture = new ControllerFixture();
        fixture.Step(180, PrismaticCommand.Positive);
        var extended = fixture.Position.Y;
        Assert.True(extended > 5.4f, "positive command must produce a useful stroke");
        fixture.Step(1, PrismaticCommand.Conflict);
        var captured = fixture.Motor.HoldCoordinate;
        fixture.Step(120, PrismaticCommand.Conflict);
        Assert.Equal(captured, fixture.Motor.HoldCoordinate);
        Assert.InRange(fixture.Position.Y, extended - HoldClearance, extended + HoldClearance);
        var work = fixture.Motor.Work;
        fixture.Step(1, PrismaticCommand.Hold);
        Assert.Equal(work, fixture.Motor.Work);
        fixture.Step(120, PrismaticCommand.Negative);
        Assert.False(fixture.Motor.Holding);
        Assert.Equal(PrismaticDrivePhase.Negative, fixture.Motor.Phase);
        Assert.True(fixture.Position.Y < extended - .25f, "negative command releases the brake and retracts");
    }

    [Fact]
    public void StartupIsNotBlockedAndCommandSlewDoesNotExceedDeclaredAcceleration()
    {
        var fixture = new ControllerFixture();
        Assert.Equal(PrismaticDrivePhase.Unpowered, fixture.Motor.Phase);
        fixture.Step(1, PrismaticCommand.Positive);
        Assert.Equal(PrismaticDrivePhase.Positive, fixture.Motor.Phase);
        // Four 1/480 s substeps at 2 m/s²: local command-interface arithmetic, not an arrival-time golden.
        Assert.Equal(2f / 120f, fixture.Motor.CommandSpeed, 1e-6f);
        Assert.True(fixture.Motor.Work > 0f);
        Assert.InRange(fixture.Motor.TickWork, 0f, 10f / 120f);
        var firstRead = fixture.Motor;
        Assert.Equal(firstRead, fixture.Motor);
    }

    [Fact]
    public void PoweredMovingReversalSlewsThroughZeroBeforeUsefulRetraction()
    {
        var fixture = new ControllerFixture();
        fixture.Step(90, PrismaticCommand.Positive);
        Assert.True(fixture.Motor.CommandSpeed > .2f);
        var start = fixture.Position.Y;
        var previous = fixture.Motor.CommandSpeed;
        var crossed = false;
        for (var tick = 0; tick < 90; tick++)
        {
            fixture.Step(1, PrismaticCommand.Negative);
            var current = fixture.Motor.CommandSpeed;
            // Local commanded-speed interface: four substeps at 2 m/s², not a trajectory tolerance.
            Assert.InRange(MathF.Abs(current - previous), 0f, 2f / 120f + 1e-6f);
            Assert.False(fixture.Motor.Holding);
            Assert.InRange(fixture.Motor.TickWork, 0f, 10f / 120f + 1e-6f);
            crossed |= current < 0f;
            previous = current;
        }
        Assert.True(crossed);
        fixture.Step(90, PrismaticCommand.Negative);
        Assert.True(fixture.Position.Y < start - .2f, "Reversal must retract a useful 20 cm without interrupting supply.");
    }

    [Fact]
    public void BothEndpointsHoldAndTheOppositeCommandReleasesThem()
    {
        var fixture = new ControllerFixture();
        fixture.Step(720, PrismaticCommand.Positive);
        Assert.Equal(PrismaticDrivePhase.Holding, fixture.Motor.Phase);
        Assert.InRange(fixture.Position.Y, 7f - HoldClearance, 7f + HoldClearance);
        fixture.Step(720, PrismaticCommand.Negative);
        Assert.Equal(PrismaticDrivePhase.Holding, fixture.Motor.Phase);
        Assert.InRange(fixture.Position.Y, 5f - HoldClearance, 5f + HoldClearance);
    }

    [Fact]
    public void ZeroPassiveAxialCoefficientsLeaveAnUndrivenSliderFree()
    {
        var source = new LawScene(false);
        var frame = source.StaticBox(new(0, 5, 0), new(.1f));
        var moving = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino,
            velocity: new(0, .5f, 0), gravity: 0f);
        source.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -2f, 2f, 0f, 0f,
            ConnectedCollision.Disabled);
        var final = source.Admit().Step(120)[moving];
        Assert.InRange(final.Position.Y, 5.48f, 5.52f);
        Assert.InRange(final.Velocity.Y, .49f, .51f);
    }

    [Theory]
    [InlineData(PhysicsGpuAbi.PrismaticSelectionOffset)]
    [InlineData(PhysicsGpuAbi.PrismaticWorkOffset)]
    [InlineData(PhysicsGpuAbi.PrismaticHoldingOffset)]
    public void UndrivenAbiRecordsRefuseHiddenMotorFields(int field)
    {
        var world = MotorRecord(false);
        BinaryPrimitives.WriteUInt32LittleEndian(world.AsSpan(PhysicsGpuAbi.Layout(world).PrismaticOffset(0) + field), 1);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(world, 0));
    }

    [Fact]
    public void CommittedMotorReaderRefusesNonfiniteEnergy()
    {
        var world = MotorRecord(true);
        BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(PhysicsGpuAbi.Layout(world).PrismaticOffset(0) +
            PhysicsGpuAbi.PrismaticWorkOffset), float.NaN);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismaticMotor(world, 0));
    }

    private static byte[] MotorRecord(bool driven)
    {
        var source = new LawScene(false);
        var frame = source.StaticBox(new(0, 5, 0), new(.1f));
        var moving = source.Box(new(0, 5, 0), new(.1f), 1f, LawMaterial.Domino, gravity: 0f);
        var joint = source.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        if (driven) source.Drive(joint, 1f, 1f, 1f, 1f, 1f);
        return source.Admit().Admitted;
    }

    private sealed class ControllerFixture
    {
        private readonly PhysicsSceneDeclaration _scene;
        private readonly GpuBodyId _moving;
        private readonly PrismaticConstraintId _joint;
        private readonly WorkshopPhysicsEngine _engine = new();
        private PhysicsInteractionSnapshot _interactions = PhysicsInteractionSnapshot.Empty;
        private byte[] _current;
        private byte[] _next;

        public ControllerFixture(float gravity = 0f)
        {
            var source = new LawScene(false);
            var frame = source.StaticBox(new(0, 5, 0), new(.1f));
            _moving = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: gravity);
            _joint = source.Prismatic(frame, _moving, ConstraintFrame.Identity, ConstraintFrame.Identity, 0f, 2f, 0f, 0f,
                ConnectedCollision.Disabled);
            var baseScene = source.Admit().Scene;
            SupplyNodeId positive = new(100), negative = new(101);
            SupplyNodeDeclaration[] nodes = [new(positive, frame, SupplyNodeKind.Contact),
                new(negative, frame, SupplyNodeKind.Contact),
                new(new(102), frame, SupplyNodeKind.Consumer, Consumers: [SupplyConsumer.Prismatic(_joint)])];
            PrismaticDriveDeclaration[] drives = [new(_joint, 2f, .5f, 2f, 20f, 10f,
                PrismaticTargetSelection.Directional, PrismaticInactiveResponse.SelfLock, positive, negative)];
            _scene = new(baseScene.Document, 103, baseScene.Bodies, baseScene.Colliders, baseScene.Materials, [], [],
                prismatics: baseScene.Prismatics, prismaticDrives: drives, supplyNodes: nodes);
            _current = PhysicsGpuAbi.Admission(_scene, new(1),
                new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            _next = new byte[_current.Length];
        }

        public Vector3 Position => LawState.Read(_scene, _current, _moving).Position;
        public PrismaticMotorRead Motor => PhysicsGpuAbi.ReadPrismaticMotor(_current, 0);
        public void Step(int ticks, PrismaticCommand command, bool supplied = true)
        {
            var inputs = new PhysicsTickInputs([], [supplied]) { PrismaticCommands = [new(_joint, command)] };
            for (var tick = 0; tick < ticks; tick++)
            {
                _interactions = _engine.Advance(_current, _next, inputs, _interactions);
                (_current, _next) = (_next, _current);
            }
        }
    }
}
