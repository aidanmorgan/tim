using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>C-Gate (Stories 9A.19 + 9A.21): the supplied prismatic drive is declaration data the generic engine law executes, the
/// Blocked reading needs something in the way, and the worker warm-up is one synthetic declaration-free scene. Every case steps the
/// production engine; gate cases go through the production compiler and WorkshopSimulation.</summary>
public sealed class DeclaredPrismaticDriveTests
{
    private const float H = 1f / 480f; // the canonical physical substep
    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

    /// <summary>One substep state of a body: its centre of mass and linear velocity, from the committed motion pieces.</summary>
    private readonly record struct Substep(Vector3 Centre, Vector3 Velocity);

    /// <summary>Every committed motion piece of <paramref name="owner"/> in one tick's payload, in substep order.</summary>
    private static List<Substep> Pieces(ReadOnlySpan<byte> motion, ulong owner)
    {
        var result = new List<Substep>();
        var count = BinaryPrimitives.ReadUInt32LittleEndian(motion);
        for (var i = 0; i < count; i++)
        {
            var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes).ToArray();
            if (BinaryPrimitives.ReadUInt64LittleEndian(piece.AsSpan(PhysicsMotionRead.OwnerOffset)) != owner) continue;
            float F(int at) => BinaryPrimitives.ReadSingleLittleEndian(piece.AsSpan(at));
            int I(int at) => BinaryPrimitives.ReadInt32LittleEndian(piece.AsSpan(at));
            const int cell = PhysicsMotionRead.CellOffset, remainder = PhysicsMotionRead.CentreRemainderOffset;
            result.Add(new(new((I(cell) + F(remainder)) / 16f, (I(cell + 4) + F(remainder + 4)) / 16f, (I(cell + 8) + F(remainder + 8)) / 16f),
                new(F(PhysicsMotionRead.VelocityOffset), F(PhysicsMotionRead.VelocityOffset + 4), F(PhysicsMotionRead.VelocityOffset + 8))));
        }
        return result;
    }

    private static ReadOnlySpan<byte> Motion(byte[] world)
    {
        var layout = PhysicsGpuAbi.Layout(world);
        return world.AsSpan(layout.MotionOffset, PhysicsMotionRead.ByteLength(layout.Dynamic));
    }

    /// <summary>The drive force each substep applied, inferred from the committed motion: m·a plus the slider's own spring and
    /// damper and minus its weight along the axis (no contact acts on the moving body), with the axial speed it acted at.
    /// Substeps touching a stop are skipped, where the stop rows act too.</summary>
    private static List<(float Force, float Speed, float Coordinate)> Inferred(IReadOnlyList<Substep> path, Vector3 origin, Vector3 axis,
        float mass, float stiffness, float damping, float gravityAlongAxis, float lower, float upper)
    {
        var result = new List<(float, float, float)>();
        for (var i = 1; i < path.Count; i++)
        {
            float X(Substep s) => Vector3.Dot(s.Centre - origin, axis);
            float V(Substep s) => Vector3.Dot(s.Velocity, axis);
            var (x0, x1, v0, v1) = (X(path[i - 1]), X(path[i]), V(path[i - 1]), V(path[i]));
            if (MathF.Min(x0, x1) < lower + 2e-3f || MathF.Max(x0, x1) > upper - 2e-3f) continue;
            var force = mass * (v1 - v0) / H + stiffness * (x0 + x1) / 2f + damping * (v0 + v1) / 2f - mass * gravityAlongAxis;
            result.Add((force, v0, x1));
        }
        return result;
    }

    // ---- 9A.19: the package declares the gate's drive, and the generic law executes it ----

    [Fact]
    public void TheGatePackageDeclaresItsDriveAndItsPowerInRouteBindsThatSlider()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var scene = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(gate, battery))
            { Connections = new(Power(battery.Id, gate.Id)) }, new(1, 2));
        var declared = TestBundle.Entry(PartId.PoweredGate).Mechanical!.Sliders.Single().Drive!.Value;
        Assert.Equal(new PrismaticDriveDescriptor(MechanicalValue.Fixed(1.55f), MechanicalValue.Fixed(2.8f), MechanicalValue.Fixed(14f),
            MechanicalValue.Fixed(60f), MechanicalValue.Fixed(120f)), declared);
        var joint = Assert.Single(scene.Prismatics.ToArray());
        Assert.Equal(new PrismaticDriveDeclaration(joint.Id, 1.55f, 2.8f, 14f, 60f, 120f), Assert.Single(scene.PrismaticDrives.ToArray()));
        Assert.Equal(joint.Id, scene.SupplyGraph.Nodes.ToArray().Single(node => node.Owner == gate.Id).Consumer);
        // The admitted world carries the same drive; a slider without one (the springboard) carries none.
        var world = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(scene.PrismaticDrives[0], PhysicsGpuAbi.ReadPrismaticDrive(world, 0));
        var spring = WorkshopInput.Generic(PartId.Springboard, new(1), 0, 3, 0, 0, 0, 0, 1);
        var passive = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(spring)), new(1, 2));
        Assert.Single(passive.Prismatics.ToArray());
        Assert.Empty(passive.PrismaticDrives.ToArray());
        Assert.Null(PhysicsGpuAbi.ReadPrismaticDrive(PhysicsGpuAbi.Admission(passive, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), 0));
    }

    [Fact]
    public async Task ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep()
    {
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var reads = await WorkshopPhysicsEngineTests.Run(new(new(2), WorkshopCadenceSettings.Default(), new(gate, battery))
            { Connections = new(Power(battery.Id, gate.Id)) }, 240);
        var blade = WorkshopPhysicsCompiler.SliderBody(gate.Id, 0);
        var path = reads.SelectMany(read => Pieces(read.Motion!.Bytes, blade.Value)).ToList();
        var forces = Inferred(path, new(0, 3, 0), Vector3.UnitY, 1f, 14f, 4f, -9.81f, 0f, 1.55f);
        Assert.True(forces.Count > 60, $"the stroke is observed on {forces.Count} substeps");
        Assert.All(path, s => Assert.True(s.Velocity.Y <= 2.8f + 1e-3f, $"speed {s.Velocity.Y} within 2.8 m/s"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force) <= 60f + .5f, $"effort {f.Force} N within 60 N"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force * f.Speed) <= 120f + 1f, $"power {f.Force * f.Speed} W within 120 W"));
        // Measured 107.5 W at the 2.8 m/s cruise (spring 14·x, damper 4·v and weight held): the unloaded gate stays under its
        // 120 W cap, so the newly enforced cap leaves its trajectory unchanged.
        Assert.InRange(forces.Max(f => f.Force * f.Speed), 60f, 120f);
        var open = reads.FindIndex(read => read.Presentation.Actuators[0].Coordinate >= 1.55f - 1e-3f);
        Assert.True(open > 0 && open < 180, $"open within 180 ticks (tick {open + 1})");
        Assert.All(reads.Skip(open), read => Assert.InRange(read.Presentation.Actuators[0].Coordinate, 1.55f - 1e-3f, 1.55f + 1e-3f));
        Assert.Equal(2.8f, path.Max(s => s.Velocity.Y), 5e-2f); // the profile reaches its declared cruise speed (measured 2.777)
    }

    /// <summary>A synthetic 1 kg slider whose every drive value differs from the gate's and binds: effort while accelerating from
    /// rest, power while cruising, the stopping profile at its own target short of the upper limit (stiffness 3 ≠ acceleration 8).</summary>
    private static (LawWorld World, GpuBodyId Moving) Synthetic(bool drive)
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 5, 0), new(.2f, .2f, .2f));
        var moving = scene.Box(new(0, 5, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino);
        var joint = scene.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled);
        if (drive) scene.Drive(joint, 1.2f, 1.6f, 8f, 16f, 12f);
        return (scene.Admit(), moving);
    }

    [Fact]
    public void ASyntheticDriveReachesItsOwnTargetOnItsOwnProfileWithItsEffortAndPowerBinding()
    {
        var (world, moving) = Synthetic(drive: true);
        var path = new List<Substep>();
        var final = world.Step(480, (_, state) => path.AddRange(Pieces(Motion(state.World), moving.Value)), supplied: true)[moving];
        var forces = Inferred(path, new(0, 5, 0), Vector3.UnitY, 1f, 3f, .5f, LawScene.Gravity, -.2f, 2f);
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force) <= 16f + .5f, $"effort {f.Force} N within 16 N"));
        Assert.All(forces, f => Assert.True(MathF.Abs(f.Force * f.Speed) <= 12f + .5f, $"power {f.Force * f.Speed} W within 12 W"));
        // Effort binds from rest: the declared 8 m/s² is not reached ((16 − 9.81)/1 ≈ 6.2 m/s²).
        var early = path.Take(24).Zip(path.Skip(1).Take(24), (a, b) => (b.Velocity.Y - a.Velocity.Y) / H).Max();
        Assert.InRange(early, 5.5f, 6.6f);
        Assert.True(forces.Take(12).All(f => f.Force > 15.5f), "the effort bound binds while accelerating from rest");
        // Power binds while cruising: 12 W at 10–13 N holds the speed well under the declared 1.6 m/s.
        var peak = path.Max(s => s.Velocity.Y);
        Assert.InRange(peak, .8f, 1.3f);
        Assert.Contains(forces, f => MathF.Abs(f.Force * f.Speed) > 11.5f);
        // Its own stopping profile, √(2·8·d): 0.08 m short of the target it still cruises (power-limited, about 0.88 m/s); a profile
        // on its stiffness, √(2·3·d) = 0.69 m/s, would already be braking there.
        var approach = path.First(s => s.Centre.Y - 5f >= 1.2f - .08f);
        Assert.True(approach.Velocity.Y > .8f, $"still cruising 0.08 m short of the target at {approach.Velocity.Y} m/s");
        // Its own target, short of its upper limit, at rest there.
        Assert.Equal(1.2f, final.Position.Y - 5f, 3e-3f);
        Assert.True(MathF.Abs(final.Velocity.Y) < 1e-2f, $"at rest on the target, {final.Velocity.Y}");
        Assert.True(path.Max(s => s.Centre.Y - 5f) <= 1.2f + 5e-3f, "the stopping profile does not overshoot");
    }

    [Fact]
    public void ASuppliedBoxWithTheGatesBladeAndLimitsButNoDeclaredDriveNeverActuates()
    {
        LawWorld Blade(bool drive)
        {
            var scene = new LawScene();
            var frame = scene.StaticBox(new(0, 5, 0), new(.4f, .1f, .4f), colliderOffset: new(0, 2.5f, 0));
            var blade = scene.Box(new(0, 5, 0), new(.06f, .72f, .72f), 1f, LawMaterial.Domino);
            var joint = scene.Prismatic(frame, blade, ConstraintFrame.Identity, ConstraintFrame.Identity, 0f, 1.55f, 14f, 4f, ConnectedCollision.Disabled);
            if (drive) scene.Drive(joint, 1.55f, 2.8f, 14f, 60f, 120f);
            return scene.Admit();
        }
        var passive = Blade(drive: false);
        var bladeId = passive.Scene.Prismatics[0].BodyB;
        passive.Step(180, (tick, state) => Assert.True(state[bladeId].Position.Y - 5f <= 1e-3f,
            $"tick {tick}: a closed route without a declared drive moves nothing ({state[bladeId].Position.Y - 5f})"), supplied: true);
        // Control: the same box with the gate's declared drive opens on the same supply.
        var driven = Blade(drive: true);
        Assert.Equal(1.55f, driven.Step(180, supplied: true)[bladeId].Position.Y - 5f, 1e-3f);
        Assert.True(driven.Step(180, supplied: false)[bladeId].Position.Y - 5f <= 1e-3f, "and stays closed unsupplied");
    }

    [Fact]
    public void AdmissionRefusesAnInvalidDriveAndARouteToAnUndrivenSlider()
    {
        PhysicsSceneDeclaration Admit(Func<PrismaticDriveDeclaration, PrismaticDriveDeclaration> change, bool dynamicMoving = true)
        {
            var scene = new LawScene(bench: false);
            var frame = scene.StaticBox(new(0, 5, 0), new(.2f, .2f, .2f));
            var moving = dynamicMoving ? scene.Box(new(0, 5, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino)
                : scene.StaticBox(new(0, 5, 0), new(.25f, .25f, .25f));
            var joint = dynamicMoving
                ? scene.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled)
                : scene.Prismatic(scene.Box(new(0, 8, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino), moving, ConstraintFrame.Identity,
                    ConstraintFrame.Identity, -.2f, 2f, 3f, .5f, ConnectedCollision.Disabled);
            scene.Drive(joint, 1.2f, 1.6f, 8f, 16f, 12f);
            var drive = new PrismaticDriveDeclaration(joint, 1.2f, 1.6f, 8f, 16f, 12f);
            return scene.Admit().Scene is var admitted && change(drive) == drive ? admitted
                : new(new(1, 2), admitted.NextIdentity, admitted.Bodies, admitted.Colliders, admitted.Materials, [], [],
                    prismatics: admitted.Prismatics, prismaticDrives: [change(drive)]);
        }
        Assert.Single(Admit(drive => drive).PrismaticDrives.ToArray());
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Target = 2.5f }));     // beyond the upper limit
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Target = -.3f }));    // below the lower limit
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxSpeed = 0f }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxSpeed = 65f }));   // beyond the envelope's 64 m/s
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { StoppingAcceleration = -1f }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxEffort = float.NaN }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { MaxPower = float.PositiveInfinity }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive with { Joint = new(999) }));
        Assert.Throws<ArgumentException>(() => Admit(drive => drive, dynamicMoving: false)); // the drive moves a static body
        // A route may drive only a slider that declares a drive: the springboard's passive slider is never a consumer.
        var spring = WorkshopInput.Generic(PartId.Springboard, new(1), 0, 3, 0, 0, 0, 0, 1);
        var passive = WorkshopPhysicsCompiler.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(spring)), new(1, 2));
        Assert.Throws<ArgumentException>(() => new CompiledSupplyGraph([new(spring.Id, SupplyNodeKind.Consumer, Consumer: passive.Prismatics[0].Id)],
            [], [], passive.Prismatics, passive.PrismaticDrives, passive.Bodies));
    }

    [Fact]
    public void TheCommittedWorldKeepsItsDriveAndAdmitsOnlyABinaryContactLane()
    {
        var (world, _) = Synthetic(drive: true);
        var layout = PhysicsGpuAbi.Layout(world.Admitted);
        var record = layout.PrismaticOffset(0);
        Assert.Equal(new PrismaticDriveDeclaration(world.Scene.Prismatics[0].Id, 1.2f, 1.6f, 8f, 16f, 12f), PhysicsGpuAbi.ReadPrismaticDrive(world.Admitted, 0));
        Assert.False(PhysicsGpuAbi.ReadPrismaticMovingContact(world.Admitted, 0));
        byte[] Changed(int offset, uint value)
        {
            var copy = (byte[])world.Admitted.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(copy.AsSpan(record + offset), value);
            return copy;
        }
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(Changed(PhysicsGpuAbi.PrismaticMovingContactOffset, 2), 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(Changed(PhysicsGpuAbi.PrismaticDriveKindOffset, 2), 0));
        Assert.True(PhysicsGpuAbi.ReadPrismaticMovingContact(Changed(PhysicsGpuAbi.PrismaticMovingContactOffset, 1), 0));
        // A drive-less record must carry zero drive values; a driven one must keep the declared values valid.
        var (passive, _) = Synthetic(drive: false);
        var undriven = (byte[])passive.Admitted.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(undriven.AsSpan(record + PhysicsGpuAbi.PrismaticDriveOffset), 1f);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismatic(undriven, 0));
        var unbounded = (byte[])world.Admitted.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(unbounded.AsSpan(record + PhysicsGpuAbi.PrismaticDriveOffset), 3f); // target beyond the limit
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadPrismaticDrive(unbounded, 0));
    }

    // ---- Blocked: something must be in the way (CAT-051 Q3, coordinator decision 2) ----

    [Theory]
    [InlineData(180.0, 9.81f / 14f)]          // inverted: the blade hangs at the spring-gravity equilibrium, 0.70 m open
    [InlineData(135.0, 9.81f * .70710677f / 14f)] // tilted: 0.50 m
    public async Task AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked(double degrees, float equilibrium)
    {
        var half = degrees * Math.PI / 360;
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 8, 0, 0, 0, Math.Sin(half), Math.Cos(half));
        var reads = await WorkshopPhysicsEngineTests.Run(new(new(2), WorkshopCadenceSettings.Default(), new(gate)), 720);
        var drives = reads.Select(read => read.Presentation.Actuators[0]).ToArray();
        var rest = drives[^1];
        // Recorded for CAT-051 Q1 (fail-closed spring): such a mount rests open, short of both stops, and nothing touches it.
        Assert.Equal(equilibrium, rest.Coordinate, 2e-3f);
        Assert.True(MathF.Abs(rest.Speed) < ActuatorDriveRead.RestSpeed, $"at rest, {rest.Speed}");
        Assert.All(drives, drive =>
        {
            Assert.False(drive.MovingContact, "nothing touches the blade");
            Assert.False(drive.Blocked, $"not blocked: {drive}");
        });
        // The kinematic part of the predicate alone would read Blocked here; only the contact term tells them apart.
        Assert.True(rest.Coordinate > rest.Lower + ActuatorDriveRead.StopSlop && rest.Coordinate < rest.Upper - ActuatorDriveRead.StopSlop);
    }

    /// <summary>Review F4: the moving-contact lane covers every substep of the tick, not only the last. A ball bounces off a sprung
    /// plate; on every tick where it struck the plate in an earlier substep and had left by the last, the lane still reads 1.</summary>
    [Fact]
    public void TheMovingContactLaneRecordsAContactInAnySubstepOfTheTick()
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 2, 0), new(.6f, .05f, .6f), colliderOffset: new(0, -.4f, 0));
        var plate = scene.Box(new(0, 2, 0), new(.5f, .05f, .5f), 1f, new(1f, 0f, .1f, 0f));
        scene.Prismatic(frame, plate, ConstraintFrame.Identity, ConstraintFrame.Identity, -.25f, 0f, 400f, .2f, ConnectedCollision.Disabled);
        var ball = scene.Sphere(new(0, 2.6f, 0), .1f, .2f, new(1f, 0f, .1f, 0f), new(0, -4f, 0), default, 0f, LawScene.Gravity);
        var world = scene.Admit();
        var early = 0;
        world.Step(120, (tick, state) =>
        {
            var path = Pieces(Motion(state.World), ball.Value);
            var flip = path.FindIndex(s => s.Velocity.Y > 0f);
            if (flip <= 0 || flip == path.Count - 1 || path[^1].Velocity.Y <= 0f) return;
            early++;
            Assert.True(PhysicsGpuAbi.ReadPrismaticMovingContact(state.World, 0),
                $"tick {tick}: the ball struck the plate in substep {flip} and left by the last; the lane must record it");
        });
        Assert.True(early > 0, "guard: at least one strike lands before the tick's last substep");
    }

    [Fact]
    public void BlockedNeedsContactInteriorAndRestTogether()
    {
        var rest = new ActuatorDriveRead(new(1), new(2), false, true, .75f, -1.1e-5f, 0f, 1.55f);
        Assert.True(rest.Blocked);
        Assert.False((rest with { MovingContact = false }).Blocked);
        Assert.False((rest with { Speed = ActuatorDriveRead.RestSpeed }).Blocked);
        Assert.False((rest with { Coordinate = ActuatorDriveRead.StopSlop }).Blocked);
        Assert.False((rest with { Coordinate = 1.55f - ActuatorDriveRead.StopSlop }).Blocked);
        Assert.True((rest with { Coordinate = 2f * ActuatorDriveRead.StopSlop, SupplyClosed = true }).Blocked); // one state, either direction
    }

    // ---- D6: a ball resting against the closed, tilted blade passes through when the gate is enabled during Run ----

    [Theory]
    [InlineData(false)] // authored Disabled, enabled in Run
    [InlineData(true)]  // authored Enabled, disabled before the ball arrives, re-enabled
    public async Task ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun(bool authoredEnabled)
    {
        const double tilt = -15;
        var half = tilt * Math.PI / 360;
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, Math.Sin(half), Math.Cos(half));
        var ball = WorkshopInput.Generic(PartId.Basketball, new(2), -.08, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(3), 3, 3, 0, enabled: authoredEnabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(gate, ball, battery))
            { Connections = new(Power(battery.Id, gate.Id)) };
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(construction)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        if (authoredEnabled)
            Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(new(battery.Id, ElectricalEnable.Disabled)).Outcome);
        var (sin, cos) = MathF.SinCos((float)(tilt * Math.PI / 180));
        float Along(WorkshopRead read)
        {
            var body = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i].Body).Single(row => row.Id == ball.Id);
            var (dx, dy) = ((body.Cell.X + (float)body.Local.X) / 16f, (body.Cell.Y + (float)body.Local.Y) / 16f - 3f);
            return cos * dx + sin * dy;
        }
        for (var tick = 0; tick < 180; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        var held = Along(simulation.Committed);
        Assert.InRange(held, -.5f, -.39f); // guard: resting upstream against the closed blade, inside the entry mouth
        Assert.False(simulation.Committed.Presentation.Actuators[0].SupplyClosed);
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.ConfigureElectrical(new(battery.Id, ElectricalEnable.Enabled)).Outcome);
        var along = new List<float>();
        for (var tick = 0; tick < 300; tick++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
            along.Add(Along(simulation.Committed));
        }
        Assert.True(along.Min() >= held - .02f, $"never thrown back toward the entry mouth (min {along.Min()}, held {held})");
        Assert.True(along[^1] > .49f, $"the ball ends downstream of the exit mouth, {along[^1]}");
    }

    // ---- 9A.21: one synthetic, declaration-free warm-up ----

    [Fact]
    public void TheWarmupSceneCarriesNoDeclaredSliderOrDriveValue()
    {
        var declared = new HashSet<float>();
        foreach (var entry in TestBundle.Entries)
            foreach (var slider in entry.Mechanical?.Sliders ?? [])
            {
                declared.UnionWith([slider.LowerLimit, slider.UpperLimit, slider.MovingBody.Mass]);
                var values = new List<MechanicalValue> { slider.Stiffness, slider.Damping, slider.Compression };
                if (slider.Drive is { } declaredDrive)
                    values.AddRange([declaredDrive.Target, declaredDrive.MaxSpeed, declaredDrive.StoppingAcceleration, declaredDrive.MaxEffort,
                        declaredDrive.MaxPower]);
                foreach (var value in values)
                    if (value.Source == MechanicalValueSource.Constant) declared.Add(value.Constant);
                    else
                        foreach (var parameter in entry.Metadata.Parameters.Where(parameter => parameter.Key == value.Parameter))
                            declared.UnionWith([parameter.Minimum, parameter.Maximum, parameter.Default]);
            }
        Assert.Contains(1.55f, declared); // control: the gate's stroke is among them
        var scene = WarmupScene.Declaration();
        var joint = Assert.Single(scene.Prismatics.ToArray());
        var drive = Assert.Single(scene.PrismaticDrives.ToArray());
        float[] warm = [joint.Lower, joint.Upper, joint.Stiffness, joint.Damping, drive.Target, drive.MaxSpeed, drive.StoppingAcceleration,
            drive.MaxEffort, drive.MaxPower];
        Assert.All(warm, value => Assert.DoesNotContain(value, declared));
        // One dynamic sphere, one static box collider, one prismatic with its drive; no element identity anywhere.
        Assert.Equal(new[] { RigidMotionKind.Static, RigidMotionKind.Dynamic }, scene.Bodies.ToArray().Select(body => body.Motion));
        Assert.Equal(new[] { ColliderShapeKind.Box, ColliderShapeKind.Sphere }, scene.Colliders.ToArray().Select(collider => collider.Shape));
        var source = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.Simulation/WarmupScene.cs"));
        Assert.DoesNotContain("PartId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWarmupAdmitsAndStepsItsSceneWithTheDriveSuppliedThenUnsupplied()
    {
        var scene = WarmupScene.Declaration();
        var world = PhysicsGpuAbi.Admission(scene, new(1), WarmupScene.Profile);
        var next = new byte[world.Length];
        var engine = new WorkshopPhysicsEngine();
        var heights = new List<float>();
        var start = LawState.Read(scene, world, WarmupScene.Rider).Position.Y;
        // The trigger record is per tick: keep the fastest strike any step recorded (the strike's tick depends on the bounce).
        var trigger = PhysicsGpuAbi.Layout(world).TriggerOffset(0);
        var fastest = 0f;
        for (var step = 0; step < WarmupScene.Steps; step++)
        {
            engine.Advance(world, next, WarmupScene.Inputs(step));
            _ = PhysicsGpuAbi.ValidateCandidate(next, world, new((ulong)step + 1));
            (world, next) = (next, world);
            if (BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(trigger + PhysicsGpuAbi.TriggerCountOffset)) > 0)
                fastest = MathF.Max(fastest, BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(trigger + PhysicsGpuAbi.TriggerApproachOffset)));
            heights.Add(LawState.Read(scene, world, WarmupScene.Rider).Position.Y - start);
            // The rider's only contact is its own base, which is never "something in the way".
            Assert.False(PhysicsGpuAbi.ReadPrismaticMovingContact(world, 0));
        }
        Assert.True(heights[WarmupScene.SuppliedSteps - 1] > .05f, $"the supplied drive lifts the rider ({heights[WarmupScene.SuppliedSteps - 1]})");
        Assert.True(heights[^1] < heights[WarmupScene.SuppliedSteps - 1], "unsupplied, it falls back");
        // It strikes the base above the trigger threshold, so the contact-event path ran too.
        Assert.True(fastest >= .5f, $"the trigger fired on a strike above its threshold ({fastest})");
        // And the contact-work payment path (Story 9.4b): the strike was paid from the store.
        var store = world.AsSpan(PhysicsGpuAbi.Layout(world).ContactWorkOffset(0), PhysicsGpuAbi.ContactWorkBytes);
        Assert.True(BinaryPrimitives.ReadSingleLittleEndian(store[PhysicsGpuAbi.ContactWorkAvailableOffset..]) <
            BinaryPrimitives.ReadSingleLittleEndian(store[PhysicsGpuAbi.ContactWorkPreloadOffset..]), "the store paid for the strike");
    }

    [Fact]
    public void AWarmedEngineStepsAWorldExactlyLikeAFreshOne()
    {
        // A static body occupies the pooled slot the warm-up's dynamic rider used; a passive slider rides on it and a ball rolls.
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 1, 0), new(1f, .1f, 1f));
        var plate = scene.Box(new(0, 1.3f, 0), new(.3f, .1f, .3f), .5f, LawMaterial.Domino);
        scene.Prismatic(frame, plate, new(new(0, .3f, 0), Quaternion.Identity), ConstraintFrame.Identity, -.15f, .1f, 50f, .5f, ConnectedCollision.Enabled);
        scene.Ball(LawBall.Basketball, new(-.6f, 1.5f, 0), velocity: new(1f, 0, 0));
        var world = scene.Admit();
        var fresh = world.Step(120).World;
        var warmed = new WorkshopPhysicsEngine();
        warmed.Warmup();
        Assert.Equal(fresh, world.Step(120, engine: warmed).World);
    }

    [Fact]
    public async Task AfterTheWarmupTheFirstRunIsAppliedAndEveryCommandOfTheEmptyBenchLifecycleIsApplied()
    {
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await device.Prepare(); // the transport's Initialize runs the engine warm-up
        var settings = WorkshopCadenceSettings.Default();
        await using (var warmup = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed))
        {
            // Program.WarmupSimulation's sequence on its empty bench.
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Initialize()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Construct(new(new(2), settings, new([])))).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Run()).Outcome);
            for (var step = 0; step < 5; step++) Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Advance()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await warmup.Reset()).Outcome);
        }
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(LatchSupplyFixtureTests.Machine(null, payload: true))).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        Assert.Equal(1UL, simulation.Committed.Tick.Value);
    }
}
