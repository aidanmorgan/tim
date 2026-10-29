using CuriousContraptions.Abstractions;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 primary proof, for the lanes it names: the ball's mass, drag, gravity, radius, restitution and rolling
/// resistance, the Receiver's capture speed, dwell, upper bound, guide margin and acceleration, the switch threshold, the Domino's
/// orientation cosine and the motion pieces' drag and gravity travel authoring → compile → ABI → worker → read model as the same f32
/// bits, including values no half-precision lane holds and −0. (The save's nine knots are F32SaveBoundaryTests; other declared
/// lanes are covered by their element and law tests.) A non-finite value is refused at both entry boundaries: in a worker-written
/// lane by the candidate decode, in a declared lane by admission.</summary>
public sealed class F32LaneRoundTripTests
{
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private const float Margin = .1234567f, Speed = 2.345678f, Dwell = .2345678f, SupportMargin = .0123457f, Acceleration = 6.789012f;
    private const float Threshold = 1.234567f, Strength = 8.000001f;
    private static readonly GpuBodyId Ball = new(1), Receiver = new(2), Switch = new(3), Tile = new(4), Lamp = new(5), Bumper = new(6);

    /// <summary>A Basketball, a Receiver, an Impact switch, a wired Domino and a Bumper, each carrying values that need a full f32
    /// significand.</summary>
    private static WorkshopConstruction Bench()
    {
        var capture = InstanceParameterCollection.Defaults(new PartId("basket")).ToDictionary();
        capture[new("capture_margin")] = Margin; capture[new("capture_speed_limit")] = Speed; capture[new("capture_dwell")] = Dwell;
        capture[new("force_support_margin")] = SupportMargin; capture[new("force_maximum_acceleration")] = Acceleration;
        return new(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(new PartId("ball"), Ball, 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("basket"), Receiver, 3, 1, 0, 0, 0, 0, 1, new(capture)),
            WorkshopInput.Generic(new PartId("switch"), Switch, -3, 1, 0, 0, 0, 0, 1, new([new(new ParameterId("trigger_threshold"), Threshold)])),
            PlacedParts.Domino(Tile, -6, (float)(-.46 + .55), 0, 0, 0, 0, 1),
            PlacedParts.Lamp(Lamp, -6, 1, 2, 0, 0, 0, 1),
            PlacedParts.Bumper(Bumper, 6, 1, 0, strength: Strength)),
            Connections: new([new(Tile, WorkshopSocket.ActivationOut, Lamp, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)]));
    }

    private static uint Bits(float value) => BitConverter.SingleToUInt32Bits(value);
    private static uint Lane(byte[] world, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(offset));

    [Fact]
    public void EveryDeclaredLaneIsTheSameF32BitsInTheCompiledSceneTheAdmittedWorldAndTheWorkerState()
    {
        foreach (var value in new[] { .34f, .1f, 9.81f, .45f, .04f, .035f, Margin, Speed, Dwell, SupportMargin, Acceleration, Threshold, Strength })
            Assert.True(TestPrecision.NeedsMoreThanElevenSignificantBits(value), $"{value} needs a full f32 significand");
        var scene = WorkshopPhysicsCompiler.Compile(Bench(), new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), Profile);
        var layout = scene.Layout;

        // Basketball: radius 0.34 m, 1 kg, drag 0.04 1/s, gravity −9.81 m/s², restitution 0.55, rolling 0.035 (its package).
        var ball = scene.Bodies.ToArray().Single(body => body.Id == Ball);
        var sphere = scene.Colliders.ToArray().Single(collider => collider.Body == Ball && collider.Shape == CuriousContraptions.Gpu.ColliderShapeKind.Sphere);
        var surface = scene.Materials.ToArray().Single(material => material.Id == sphere.Material);
        Assert.Equal((Bits(.34f), Bits(1f), Bits(.04f), Bits(-9.81f), Bits(.55f), Bits(.035f)),
            (Bits(sphere.Radius.Value), Bits(ball.Mass.Value), Bits(ball.LinearDrag.Value), Bits(ball.Gravity.Y), Bits(surface.Restitution.Value),
                Bits(surface.RollingResistance.Value)));
        var body = layout.GetBodyOffset(Ball);
        Assert.Equal((Bits(1f), Bits(.04f), Bits(-9.81f)), (Lane(admitted, body + PhysicsGpuAbi.BodyMassOffset),
            Lane(admitted, body + PhysicsGpuAbi.BodyDragOffset), Lane(admitted, body + PhysicsGpuAbi.BodyGravityOffset + 4)));
        Assert.Equal(Bits(.34f), Lane(admitted, layout.GetColliderOffset(sphere.Id) + PhysicsGpuAbi.ColliderRadiusOffset));
        var material = layout.GetMaterialOffset(surface.Id);
        Assert.Equal((Bits(.55f), Bits(.035f)), (Lane(admitted, material + PhysicsGpuAbi.MaterialRestitutionOffset),
            Lane(admitted, material + PhysicsGpuAbi.MaterialRollingResistanceOffset)));

        // Receiver: its capture speed limit and dwell, the upper bound Y plus the margin (one f32 rounding), and its guide.
        var sensor = scene.Sensors.ToArray().Single(row => row.Frame == Receiver);
        var guide = scene.Guides.ToArray().Single(row => row.Frame == Receiver);
        var capture = DeclaredPhysics.Of(new PartId("basket")).Mechanical!.Capture!.Value;
        var sensorOffset = layout.GetSensorOffset(sensor.Id);
        Assert.Equal((Bits(Speed), Bits(Dwell), Bits(capture.Maximum.Y + Margin)), (Lane(admitted, sensorOffset + PhysicsGpuAbi.SensorSpeedLimitOffset),
            Lane(admitted, sensorOffset + PhysicsGpuAbi.SensorDwellOffset), Lane(admitted, sensorOffset + PhysicsGpuAbi.SensorMaximumOffset + 4)));
        var guideOffset = layout.GetGuideOffset(guide.Id);
        Assert.Equal((Bits(SupportMargin), Bits(Acceleration)), (Lane(admitted, guideOffset + PhysicsGpuAbi.GuideSupportMarginOffset),
            Lane(admitted, guideOffset + PhysicsGpuAbi.GuideMaximumAccelerationOffset)));

        // Impact switch threshold and the Domino's orientation threshold, cos 22.5°.
        var trigger = scene.Triggers.ToArray().Single(row => row.Owner == Switch);
        Assert.Equal(Bits(Threshold), Lane(admitted, layout.GetTriggerOffset(trigger.Id) + PhysicsGpuAbi.TriggerThresholdOffset));
        var orientation = Assert.Single(scene.OrientationSensors.ToArray());
        Assert.Equal(Bits((float)Math.Cos(Math.PI / 8)),
            Lane(admitted, layout.GetOrientationSensorOffset(orientation.Id) + PhysicsGpuAbi.OrientationSensorCosineOffset));

        // The worker steps one tick: every immutable lane is unchanged (the candidate validates) and its motion pieces carry the
        // declared drag and gravity bits.
        var candidate = new byte[admitted.Length];
        var interactions = new CuriousContraptions.Simulation.WorkshopPhysicsEngine().Advance(admitted, candidate, new(new ElectricalEnable[scene.World.ElectricalSources], new bool[scene.World.Prismatics]), PhysicsInteractionSnapshot.Empty);
        var read = PhysicsGpuAbi.ValidateCandidate(candidate, admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        Assert.Equal((Bits(1f), Bits(.04f)), (Lane(candidate, body + PhysicsGpuAbi.BodyMassOffset), Lane(candidate, body + PhysicsGpuAbi.BodyDragOffset)));
        var motion = scene.World.MotionOffset;
        var pieces = Enumerable.Range(0, (int)Lane(candidate, motion + PhysicsMotionRead.CountOffset))
            .Select(i => motion + PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes)
            .Where(piece => BinaryPrimitives.ReadUInt64LittleEndian(candidate.AsSpan(piece + PhysicsMotionRead.OwnerOffset)) == Ball.Value).ToArray();
        Assert.Equal(4, pieces.Length);
        Assert.All(pieces, piece => Assert.Equal((Bits(.04f), Bits(-9.81f)),
            (Lane(candidate, piece + PhysicsMotionRead.DragRateOffset), Lane(candidate, piece + PhysicsMotionRead.GravityOffset + 4))));
        // The committed read model keeps the ball's centre of mass.
        Assert.True(read.Bodies.TryGet(Ball, out var committed));
        Assert.Equal(sphere.Pose.Translation, committed.LocalCentreOfMass);
    }

    [Fact]
    public void ANegativeZeroCentreOfMassIsKeptDistinctFromPositiveZeroThroughAdmissionAndTheCommittedRead()
    {
        var scene = new LawScene();
        var ball = scene.Sphere(new(0, 2, 0), .2f, 1f, LawMaterial.Domino, default, default, 0f, LawScene.Gravity,
            colliderOffset: new(-0f, .05f, 0f));
        var world = scene.Admit();
        var offset = world.Scene.Layout.GetBodyOffset(ball) + PhysicsGpuAbi.BodyCentreOfMassOffset;
        Assert.Equal((0x80000000u, Bits(.05f), 0u), (Lane(world.Admitted, offset), Lane(world.Admitted, offset + 4), Lane(world.Admitted, offset + 8)));
        Assert.True(PhysicsGpuAbi.ReadDynamicBodies(world.Admitted).TryGet(ball, out var read));
        Assert.Equal(0x80000000u, Bits(read.LocalCentreOfMass.X));
        // Control: the positive-zero centre is a different committed value.
        Assert.False(F32Bits.Equal(read.LocalCentreOfMass, new MetreVector(0f, .05f, 0f)));
    }

    // ---- Non-finite values at both boundaries a value can enter by ----

    /// <summary>The f32 lanes the worker writes on every tick: the candidate decode reads their values (it cannot compare them with the
    /// committed world), so a NaN or infinity there is refused as a value, while a finite change of the same lane commits.</summary>
    public enum CandidateLane { BodyVelocity, BodyAngularVelocity, MotionAngularVelocity, MotionVelocity,
        BodyLocal, BodyRotation, MotionStartPhase, MotionEndPhase, MotionAnchorPhase, MotionCentreRemainder, MotionDragRate, MotionRotation,
        MotionGravity, MotionSupportedAcceleration, MotionAngularAcceleration, MotionFlaggedVector }

    private static int LaneOffset(PhysicsSceneDeclaration scene, CandidateLane lane)
    {
        var firstPiece = scene.World.MotionOffset + PhysicsMotionRead.HeaderBytes;
        var body = scene.Layout.GetBodyOffset(Ball);
        return lane switch
        {
            CandidateLane.BodyVelocity => body + PhysicsGpuAbi.BodyVelocityOffset,
            CandidateLane.BodyAngularVelocity => body + PhysicsGpuAbi.BodyAngularVelocityOffset,
            CandidateLane.BodyLocal => body + PhysicsGpuAbi.BodyLocalOffset,
            CandidateLane.BodyRotation => body + PhysicsGpuAbi.BodyRotationOffset + PhysicsGpuAbi.RotationWOffset,
            CandidateLane.MotionAngularVelocity => firstPiece + PhysicsMotionRead.AngularVelocityOffset,
            CandidateLane.MotionVelocity => firstPiece + PhysicsMotionRead.VelocityOffset,
            CandidateLane.MotionStartPhase => firstPiece + PhysicsMotionRead.StartPhaseOffset,
            CandidateLane.MotionEndPhase => firstPiece + PhysicsMotionRead.EndPhaseOffset,
            CandidateLane.MotionAnchorPhase => firstPiece + PhysicsMotionRead.AnchorPhaseOffset,
            CandidateLane.MotionCentreRemainder => firstPiece + PhysicsMotionRead.CentreRemainderOffset,
            CandidateLane.MotionDragRate => firstPiece + PhysicsMotionRead.DragRateOffset,
            CandidateLane.MotionRotation => firstPiece + PhysicsMotionRead.RotationOffset + PhysicsGpuAbi.RotationWOffset,
            CandidateLane.MotionGravity => firstPiece + PhysicsMotionRead.GravityOffset + 4,
            CandidateLane.MotionSupportedAcceleration => firstPiece + PhysicsMotionRead.SupportedAccelerationOffset,
            CandidateLane.MotionAngularAcceleration => firstPiece + PhysicsMotionRead.AngularAccelerationOffset,
            CandidateLane.MotionFlaggedVector => firstPiece + PhysicsMotionRead.FlaggedVectorOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(lane))
        };
    }

    // Every f32 lane the worker writes for a moving body. Value lanes take any finite value, so moving one by an f32 step still commits
    // (a rotation's w steps down, keeping the quaternion unit); structural lanes (an interval's start, end and anchor phases, the zero
    // supported and angular accelerations and flagged vector of a free piece) are pinned by the decode, so a finite change is refused
    // too. A NaN or infinity is refused in both.
    [Theory]
    [InlineData(CandidateLane.BodyVelocity, float.NaN, true)]
    [InlineData(CandidateLane.BodyAngularVelocity, float.PositiveInfinity, true)]
    [InlineData(CandidateLane.BodyLocal, float.NegativeInfinity, true)]
    [InlineData(CandidateLane.BodyRotation, float.NaN, true)]
    [InlineData(CandidateLane.MotionAngularVelocity, float.NegativeInfinity, true)]
    [InlineData(CandidateLane.MotionVelocity, float.NaN, true)]
    [InlineData(CandidateLane.MotionEndPhase, float.PositiveInfinity, false)]
    [InlineData(CandidateLane.MotionCentreRemainder, float.NaN, true)]
    [InlineData(CandidateLane.MotionDragRate, float.NaN, true)]
    [InlineData(CandidateLane.MotionRotation, float.NegativeInfinity, true)]
    [InlineData(CandidateLane.MotionGravity, float.NaN, true)]
    [InlineData(CandidateLane.MotionStartPhase, float.NaN, false)]
    [InlineData(CandidateLane.MotionAnchorPhase, float.PositiveInfinity, false)]
    [InlineData(CandidateLane.MotionSupportedAcceleration, float.NaN, false)]
    [InlineData(CandidateLane.MotionAngularAcceleration, float.NegativeInfinity, false)]
    [InlineData(CandidateLane.MotionFlaggedVector, float.NaN, false)]
    public void ANonFiniteValueInAWorkerWrittenLaneIsRefusedAFiniteChangeCommitsAndTheTickContinues(CandidateLane lane, float value, bool valueLane)
    {
        var scene = WorkshopPhysicsCompiler.Compile(Bench(), new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), Profile);
        (byte[] World, PhysicsInteractionSnapshot Interactions) Stepped()
        {
            var candidate = new byte[admitted.Length];
            var interactions = new CuriousContraptions.Simulation.WorkshopPhysicsEngine().Advance(admitted, candidate,
                new(new ElectricalEnable[scene.World.ElectricalSources], new bool[scene.World.Prismatics]), PhysicsInteractionSnapshot.Empty);
            return (candidate, interactions);
        }
        var offset = LaneOffset(scene, lane);
        // Finite control: a value lane moved by one f32 step still commits, so the refusal below is the value's, not the change's.
        var changedState = Stepped();
        var changed = changedState.World;
        var finite = BinaryPrimitives.ReadSingleLittleEndian(changed.AsSpan(offset));
        BinaryPrimitives.WriteSingleLittleEndian(changed.AsSpan(offset), finite == 1f ? MathF.BitDecrement(finite) : MathF.BitIncrement(finite));
        if (valueLane) PhysicsGpuAbi.ValidateCandidate(changed, admitted, new(1), changedState.Interactions, PhysicsInteractionSnapshot.Empty);
        else Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, admitted, new(1), changedState.Interactions, PhysicsInteractionSnapshot.Empty));
        var poisonedState = Stepped();
        var poisoned = poisonedState.World;
        BinaryPrimitives.WriteSingleLittleEndian(poisoned.AsSpan(offset), value);
        Assert.ThrowsAny<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(poisoned, admitted, new(1), poisonedState.Interactions, PhysicsInteractionSnapshot.Empty));
        // The refused candidate is discarded: the committed world's next tick steps and commits.
        var nextState = Stepped();
        var next = PhysicsGpuAbi.ValidateCandidate(nextState.World, admitted, new(1), nextState.Interactions, PhysicsInteractionSnapshot.Empty);
        Assert.True(next.Bodies.TryGet(Ball, out var ball) && float.IsFinite(ball.Body.Velocity.Y));
    }

    /// <summary>The f32 lanes fixed at admission: the candidate decode compares them with the committed world byte for byte, so a value
    /// enters them only through the declarations admission validates. A NaN or infinity in any of them is refused there; the same
    /// rebuilt scene with its declared value admits (control).</summary>
    public enum DeclaredLane { BodyMass, BodyDrag, BodyCentreOfMass, ColliderRadius, MaterialFriction, SensorDwell, GuideMargin,
        GuideSupportHeight, OrientationCosine, TriggerThreshold }

    [Theory]
    [InlineData(DeclaredLane.BodyMass, float.NaN)]
    [InlineData(DeclaredLane.BodyDrag, float.PositiveInfinity)]
    [InlineData(DeclaredLane.BodyCentreOfMass, float.NaN)]
    [InlineData(DeclaredLane.ColliderRadius, float.NaN)]
    [InlineData(DeclaredLane.MaterialFriction, float.NaN)]
    [InlineData(DeclaredLane.SensorDwell, float.PositiveInfinity)]
    [InlineData(DeclaredLane.GuideMargin, float.NaN)]
    [InlineData(DeclaredLane.GuideSupportHeight, float.NegativeInfinity)]
    [InlineData(DeclaredLane.OrientationCosine, float.NaN)]
    [InlineData(DeclaredLane.TriggerThreshold, float.NaN)]
    public void ANonFiniteValueInAnAdmittedDeclarationLaneIsRefusedAtAdmission(DeclaredLane lane, float value)
    {
        var scene = WorkshopPhysicsCompiler.Compile(Bench(), new(1, 2));
        var (bodies, colliders, materials) = (scene.Bodies.ToArray(), scene.Colliders.ToArray(), scene.Materials.ToArray());
        var (sensors, guides, triggers, orientations) = (scene.Sensors.ToArray(), scene.Guides.ToArray(), scene.Triggers.ToArray(),
            scene.OrientationSensors.ToArray());
        PhysicsSceneDeclaration Rebuilt() => new(scene.Document, scene.NextIdentity, bodies, colliders, materials, sensors, guides,
            triggers, scene.ContactWorks, orientations, prismatics: scene.Prismatics);
        Assert.Equal(PhysicsGpuAbi.Admission(scene, new(1), Profile), PhysicsGpuAbi.Admission(Rebuilt(), new(1), Profile)); // control
        var ball = Array.FindIndex(bodies, body => body.Id == Ball);
        var sphere = Array.FindIndex(colliders, collider => collider.Body == Ball);
        var surface = Array.FindIndex(materials, material => material.Id == colliders[sphere].Material);
        switch (lane)
        {
            case DeclaredLane.BodyMass: bodies[ball] = bodies[ball] with { Mass = new(value) }; break;
            case DeclaredLane.BodyDrag: bodies[ball] = bodies[ball] with { LinearDrag = new(value) }; break;
            case DeclaredLane.BodyCentreOfMass:
                colliders[sphere] = colliders[sphere] with { Pose = colliders[sphere].Pose with { Translation = new(value, 0f, 0f) } }; break;
            case DeclaredLane.ColliderRadius: colliders[sphere] = colliders[sphere] with { Radius = new(value) }; break;
            case DeclaredLane.MaterialFriction: materials[surface] = materials[surface] with { Friction = new(value) }; break;
            case DeclaredLane.SensorDwell: sensors[0] = sensors[0] with { Dwell = new(value) }; break;
            case DeclaredLane.GuideMargin: guides[0] = guides[0] with { SupportMargin = new(value) }; break;
            case DeclaredLane.GuideSupportHeight: guides[0] = guides[0] with { SupportHeight = new(value) }; break;
            case DeclaredLane.OrientationCosine: orientations[0] = orientations[0] with { Threshold = new(value) }; break;
            case DeclaredLane.TriggerThreshold: triggers[0] = triggers[0] with { Threshold = new(value) }; break;
            default: throw new ArgumentOutOfRangeException(nameof(lane));
        }
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.Admission(Rebuilt(), new(1), Profile));
    }

    // A residence sensor's 4-byte gap after its participation lane is zero in the admitted world: the admission check (tick 0)
    // refuses a nonzero byte at either end of it; control: the admitted world admits.
    [Fact]
    public void ASensorParticipationGapMustBeZeroAtAdmission()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Bench(), new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), Profile);
        PhysicsGpuAbi.ValidateCandidate(admitted, admitted, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        var gap = scene.Layout.GetSensorOffset(scene.Sensors[0].Id) + PhysicsGpuAbi.SensorParticipationPaddingOffset;
        foreach (var offset in new[] { gap, gap + PhysicsGpuAbi.SensorPoseOffset - PhysicsGpuAbi.SensorParticipationPaddingOffset - 1 })
        {
            var padded = (byte[])admitted.Clone();
            padded[offset] = 1;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(padded, padded, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
        }
    }
}
