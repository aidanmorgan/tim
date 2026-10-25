using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 (D4): a puzzle record carries exactly nine f32 assistance knots. Every half-precision-era lesson save is refused
/// with its typed reason at both decode boundaries, and a nine-knot save round-trips bit for bit.</summary>
public sealed class F32SaveBoundaryTests
{
    private static string Fixture(string name) => Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.tests/Fixtures", name);

    private static WorkshopConstruction Lesson(string id, float precision) =>
        PuzzleCatalogue.Create(new PuzzleId(id), new(2), WorkshopCadenceSettings.Default(), new(1), new(precision));

    private static AssistanceKnot[] Knots(WorkshopPuzzle puzzle) =>
    [
        puzzle.PrimaryAssistance.Forgiving, puzzle.PrimaryAssistance.Balanced, puzzle.PrimaryAssistance.Precise,
        puzzle.TargetAssistance.Forgiving, puzzle.TargetAssistance.Balanced, puzzle.TargetAssistance.Precise,
        puzzle.PlacementAssistance.Forgiving, puzzle.PlacementAssistance.Balanced, puzzle.PlacementAssistance.Precise,
    ];

    private static float[] Lanes(AssistanceKnot k) =>
    [
        k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value, k.MaximumRotationCorrectionDegrees,
        k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value, k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold
    ];

    /// <summary>A buffer with the encoded assistance vector's element count rewritten. The count is the u32 directly before the first
    /// knot's 44 bytes (its eleven little-endian f32 lanes).</summary>
    private static byte[] WithKnotCount(byte[] encoded, WorkshopPuzzle puzzle, uint count)
    {
        var first = Lanes(Knots(puzzle)[0]).SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        var at = encoded.AsSpan().IndexOf(first);
        Assert.True(at >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(at - 4)) == WorkshopWireFlatBuffers.AssistanceKnotCount,
            "the encoded puzzle's nine-knot assistance vector");
        var changed = (byte[])encoded.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(changed.AsSpan(at - 4), count);
        return changed;
    }

    private const string VerifierExplanation = "This save cannot be loaded: The FlatBuffers verifier refused a CCWS buffer.";

    [Fact]
    public void AHalfPrecisionEraLessonSaveIsAVerifierRefusalAtBothBoundaries()
    {
        // A pre-9A.44 lesson save (captured at f1ad3bd1) is not a save of this schema: its puzzle's assistance vector holds 198
        // elements, which as 44-byte knot records overrun the buffer, so the verifier refuses it before any field rule.
        var legacy = File.ReadAllBytes(Fixture("save-f1ad3bd1-authored-puzzle.bin"));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, legacy));
        Assert.Equal(VerifierExplanation, refusal.Message);
        // The command boundary: a Construct command whose puzzle carries the legacy file's own element count is the typed verifier fault.
        var count = (uint)CuriousContraptions.Wire.SavedConstruction.GetRootAsSavedConstruction(new Google.FlatBuffers.ByteBuffer(legacy, sizeof(int)))
            .Construction!.Value.Puzzle!.Value.AssistanceLength;
        Assert.Equal(198u, count);
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        Assert.Equal(WireFaultReason.VerifierRejected, Assert.Throws<WireFaultException>(() =>
            WorkshopWireFlatBuffers.DecodeCommand(WithKnotCount(encoded, construction.Puzzle, count))).Reason);
    }

    [Fact]
    public async Task ARefusedLegacyLoadLeavesTheLiveSimulationUnchangedAndShowsItsExplanation()
    {
        // The Workshop's Load decodes the stored text before it submits anything (ui/Workshop.cs LoadSaveAsync), so a refused legacy
        // save never reaches the live simulation: the refusal is the status the player reads, the simulation's committed read is the
        // same after the attempt, and it goes on advancing its own bench.
        await using var device = new WorkshopGpuDevice(new CuriousContraptions.Simulation.BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), WorkshopCadenceSettings.Default(),
            new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(Lesson("first_principles", .45f))).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
        for (var tick = 0; tick < 3; tick++) Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        var before = simulation.Committed;
        var stored = Convert.ToBase64String(await File.ReadAllBytesAsync(Fixture("save-f1ad3bd1-authored-puzzle.bin"),
            TestContext.Current.CancellationToken));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.DecodeStored(SaveFixture.Elements, stored));
        Assert.True(WorkshopUiFailure.IsRejection(refusal));
        Assert.Equal(VerifierExplanation, WorkshopUiFailure.Report(refusal));
        NoFixedCapacityTests.AssertSameState(before, simulation.Committed, "the live simulation after a refused load");
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
        Assert.Equal(before.Tick.Value + 1, simulation.Committed.Tick.Value);
    }

    [Fact]
    public void EightKnotsAreAMissingFieldAtTheSaveBoundary()
    {
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(3)));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.MissingField,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, WithKnotCount(encoded, construction.Puzzle, 8)));
        // A current save with the wrong count is told its count.
        Assert.Equal("This save cannot be loaded: The saved puzzle holds 8 assistance knots; a puzzle holds exactly nine.", refusal.Message);
    }

    // The authored knot rule (finite, non-negative, knots at precisions 0, 0.45 and 1) is applied before any domain construction:
    // a finite forged knot that breaks it is the typed OutOfRange refusal. Control: the authored knots load.
    public enum ForgedKnot { NegativeSpeed, BalancedAtHalf, ForgivingAboveZero }

    [Theory]
    [InlineData(ForgedKnot.NegativeSpeed)]
    [InlineData(ForgedKnot.BalancedAtHalf)]
    [InlineData(ForgedKnot.ForgivingAboveZero)]
    public void AFiniteKnotOutsideTheAuthoredRuleIsOutOfRangeAtTheSaveBoundary(ForgedKnot forged)
    {
        var construction = Lesson("first_principles", .45f);
        var profile = construction.Puzzle.TargetAssistance;
        Assert.Equal(new WorkshopSavedConstruction(construction, new(3)),
            WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Forged(construction, 3)));
        var changed = forged switch
        {
            ForgedKnot.NegativeSpeed => profile with { Balanced = profile.Balanced with { CaptureSpeed = new(-.5f) } },
            ForgedKnot.BalancedAtHalf => profile with { Balanced = profile.Balanced with { Precision = .5f } },
            _ => profile with { Forgiving = profile.Forgiving with { Precision = .1f } },
        };
        var save = SaveFixture.Forged(construction with { Puzzle = construction.Puzzle with { TargetAssistance = changed } }, 3);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, save));
    }

    [Fact]
    public void TenKnotsAreRefusedByTheVerifierAtTheSaveBoundary()
    {
        // In a save the knot vector is the last data before the buffer's end (FlatBuffers writes back to front): a tenth 44-byte
        // record lies past the end, so the verifier refuses the save before any field rule.
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(3)));
        var first = encoded.AsSpan().IndexOf(Lanes(Knots(construction.Puzzle)[0]).SelectMany(value => BitConverter.GetBytes(value)).ToArray());
        Assert.True(encoded.Length - (first + 9 * 44) < 44, "the tenth record would end past the save");
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, WithKnotCount(encoded, construction.Puzzle, 10)));
    }

    [Theory]
    [InlineData(8u)]
    [InlineData(10u)]
    [InlineData(198u)]
    public void AKnotCountOtherThanNineIsATypedWireFaultAtTheCommandBoundary(uint count)
    {
        var construction = Lesson("first_principles", .45f);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(command);
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(encoded)); // control: nine knots decode
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(WithKnotCount(encoded, construction.Puzzle, count)));
        // 198 records (the half-precision blob's element count) overrun the buffer; a near count reaches the nine-knot rule.
        Assert.Equal(count == 198 ? WireFaultReason.VerifierRejected : WireFaultReason.MissingField, fault.Reason);
    }

    [Fact]
    public void ANineKnotF32SaveRoundTripsEveryLaneBitForBit()
    {
        var construction = Lesson("first_principles", .45f);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new(construction, new(3))));
        var (expected, actual) = (Knots(construction.Puzzle), Knots(decoded.Construction.Puzzle));
        for (var knot = 0; knot < expected.Length; knot++)
            Assert.Equal(Lanes(expected[knot]).Select(BitConverter.SingleToUInt32Bits), Lanes(actual[knot]).Select(BitConverter.SingleToUInt32Bits));
        Assert.Equal(BitConverter.SingleToUInt32Bits(.45f), BitConverter.SingleToUInt32Bits(decoded.Construction.Puzzle.Precision.Value));
    }

    [Fact]
    public void PrecisionOneIsAdmittedAndTheNextF32AboveIsOutOfRange()
    {
        var construction = Lesson("first_principles", 1f);
        var saved = new WorkshopSavedConstruction(construction, new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved)));
        var above = construction with { Puzzle = construction.Puzzle with { Precision = new(MathF.BitIncrement(1f)) } };
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Forged(above, 3)));
    }

    [Fact]
    public void SavePrevalidationAdmitsAOneHundredTwentySecondGoalAndRefusesTheNextF32Above()
    {
        // The save's own range rule, run on the opened record: exactly 120 s passes it, and the next f32 above is its OutOfRange
        // refusal (no domain guard is reached).
        var construction = Lesson("delayed_signal", .45f);
        var goal = construction.Puzzle.Goal;
        Assert.Equal(WorkshopGoalKind.ActivatedAfter, goal.Kind);
        CuriousContraptions.Wire.SavedConstruction Opened(float seconds) => WorkshopSaveCodec.Open(SaveFixture.Forged(
            construction with { Puzzle = construction.Puzzle with { Goal = goal with { MinimumDuration = new(seconds) } } },
            construction.Instances.Max(instance => instance.Id.Value) + 1));
        WorkshopSavePrevalidation.Check(Opened(120f), SaveFixture.Elements);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSavePrevalidation.Check(Opened(MathF.BitIncrement(120f)), SaveFixture.Elements));
    }

    /// <summary>The encoded buffer with one lane of one profile's Balanced knot (the knot records are contiguous, 44 bytes each in
    /// primary, target, placement order) replaced by <paramref name="value"/>.</summary>
    private static byte[] WithKnotLane(byte[] encoded, WorkshopPuzzle puzzle, int profile, int lane, float value)
    {
        var at = encoded.AsSpan().IndexOf(Lanes(Knots(puzzle)[0]).SelectMany(v => BitConverter.GetBytes(v)).ToArray());
        Assert.True(at >= 4, "the encoded puzzle's assistance vector");
        var changed = (byte[])encoded.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(changed.AsSpan(at + (profile * WorkshopWireFlatBuffers.KnotsPerProfile + 1) * 44 + lane * 4), value);
        return changed;
    }

    /// <summary>Every lane (11) of every profile (3), each with NaN, +∞, −∞ and a finite negative value.</summary>
    public static TheoryData<int, int, float> PoisonedKnotLanes()
    {
        var data = new TheoryData<int, int, float>();
        foreach (var profile in new[] { 0, 1, 2 })
            for (var lane = 0; lane < 11; lane++)
                foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -.5f })
                    data.Add(profile, lane, value);
        return data;
    }

    // Save boundary: a non-finite lane is NonFinite and a finite negative one OutOfRange, both refused before any domain construction.
    [Theory]
    [MemberData(nameof(PoisonedKnotLanes))]
    public void APoisonedKnotLaneIsTheTypedRefusalAtTheSaveBoundary(int profile, int lane, float value)
    {
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(3)));
        Assert.Equal(new WorkshopSavedConstruction(construction, new(3)), WorkshopSaveCodec.Decode(SaveFixture.Elements, encoded)); // control
        SaveFixture.Refused(float.IsFinite(value) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, WithKnotLane(encoded, construction.Puzzle, profile, lane, value)));
    }

    // Command boundary: the same lanes break the authored knot rule, refused as the typed OutOfRange wire fault.
    [Theory]
    [MemberData(nameof(PoisonedKnotLanes))]
    public void APoisonedKnotLaneIsTheTypedWireFaultAtTheCommandBoundary(int profile, int lane, float value)
    {
        var construction = Lesson("first_principles", .45f);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(command);
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(encoded)); // control
        var fault = Assert.Throws<WireFaultException>(() =>
            WorkshopWireFlatBuffers.DecodeCommand(WithKnotLane(encoded, construction.Puzzle, profile, lane, value)));
        Assert.Equal(WireFaultReason.OutOfRange, fault.Reason);
    }

    // Command boundary, authored shape: a finite knot off its precision (Balanced at 0.5) is the same typed fault.
    [Fact]
    public void AKnotOffItsAuthoredPrecisionIsTheTypedWireFaultAtTheCommandBoundary()
    {
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            construction, Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() =>
            WorkshopWireFlatBuffers.DecodeCommand(WithKnotLane(encoded, construction.Puzzle, 1, 0, .5f))).Reason);
    }

    // A current save whose assistance vector is empty (no deprecated slot) is told its count, not that it predates f32 assistance.
    [Fact]
    public void AnEmptyKnotVectorWithoutTheDeprecatedSlotIsToldItsCount()
    {
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(3)));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.MissingField,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, WithKnotCount(encoded, construction.Puzzle, 0)));
        Assert.Equal("This save cannot be loaded: The saved puzzle holds 0 assistance knots; a puzzle holds exactly nine.", refusal.Message);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ANonFinitePrecisionIsRefusedAtTheSaveBoundary(float value)
    {
        var construction = Lesson("first_principles", .45f);
        var forged = construction with { Puzzle = construction.Puzzle with { Precision = new(value) } };
        SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Forged(forged, 3)));
    }
}
