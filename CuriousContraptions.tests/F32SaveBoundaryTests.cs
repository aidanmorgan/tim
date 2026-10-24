using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 (D4): a puzzle record carries exactly nine f32 assistance knots. Every half-precision-era lesson save is refused
/// with its typed reason at both decode boundaries, a nine-knot save round-trips bit for bit, and a Free save is unaffected.</summary>
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

    private const string NoKnotsExplanation =
        "This save cannot be loaded: The saved puzzle lacks its nine assistance knots; a lesson saved before f32 assistance cannot be loaded.";

    [Fact]
    public void AHalfPrecisionEraLessonSaveIsAMissingFieldWithItsExplanation()
    {
        // A pre-9A.44 lesson save (captured at f1ad3bd1) holds its 198 bytes of half-precision knots in the deprecated raw_assistance
        // slot, which nothing reads; the save verifies and has no f32 assistance knots.
        var legacy = File.ReadAllBytes(Fixture("save-f1ad3bd1-authored-puzzle.bin"));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.MissingField, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, legacy));
        Assert.Equal(NoKnotsExplanation, refusal.Message);
    }

    [Fact]
    public async Task ARefusedLegacyLoadLeavesTheBenchUnchangedAndShowsItsExplanation()
    {
        // The Workshop's Load decodes the stored text before it submits anything (ui/Workshop.cs LoadSaveAsync): the refusal is the
        // status the player reads, and the bench that was admitted before the attempt is admitted to the same state after it.
        var bench = Lesson("first_principles", .45f);
        var (before, _, _) = await NoFixedCapacityTests.RunAndReset(bench, 2);
        var stored = Convert.ToBase64String(await File.ReadAllBytesAsync(Fixture("save-f1ad3bd1-authored-puzzle.bin"),
            TestContext.Current.CancellationToken));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.MissingField, () => WorkshopSaveCodec.DecodeStored(SaveFixture.Elements, stored));
        Assert.True(WorkshopUiFailure.IsRejection(refusal));
        Assert.Equal(NoKnotsExplanation, WorkshopUiFailure.Report(refusal));
        Assert.Equal(Lesson("first_principles", .45f), bench);
        var (after, _, _) = await NoFixedCapacityTests.RunAndReset(bench, 2);
        NoFixedCapacityTests.AssertSameState(before, after, "the bench after a refused load");
    }

    [Fact]
    public void EightKnotsAreAMissingFieldAtTheSaveBoundary()
    {
        var construction = Lesson("first_principles", .45f);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(3)));
        SaveFixture.Refused(WorkshopSaveRejection.MissingField,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, WithKnotCount(encoded, construction.Puzzle, 8)));
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
    public void ABinary16EraFreeSaveStillLoadsAsItsExactConstruction()
    {
        // The f1ad3bd1 Free save (a Basketball at (0, 3, 0) and a Ramp at (1, 1, 0), next identity 3): its fields were already f32.
        var legacy = File.ReadAllBytes(Fixture("save-f1ad3bd1-free.bin"));
        var expected = new WorkshopSavedConstruction(new WorkshopConstruction(new ConstructionRevision(2), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.Generic(PartId.Basketball, new(1), 0, 3, 0, 0, 0, 0, 1),
                WorkshopInput.Generic(PartId.Ramp, new(2), 1, 1, 0, 0, 0, 0, 1))), new(3));
        Assert.Equal(expected, WorkshopSaveCodec.Decode(SaveFixture.Elements, legacy));
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void ANonFiniteAssistanceKnotLaneIsRefusedAtTheSaveBoundary(int lane)
    {
        var construction = Lesson("first_principles", .45f);
        var k = construction.Puzzle.PrimaryAssistance.Balanced;
        var nan = float.NaN;
        var poisoned = lane switch
        {
            0 => k with { Precision = nan }, 1 => k with { PositionWindow = new(nan) }, 2 => k with { RotationWindowDegrees = nan },
            3 => k with { MaximumPositionCorrection = new(nan) }, 4 => k with { MaximumRotationCorrectionDegrees = nan },
            5 => k with { Blend = new(nan) }, 6 => k with { CaptureMargin = new(nan) }, 7 => k with { CaptureSpeed = new(nan) },
            8 => k with { CaptureDwell = new(nan) }, 9 => k with { GuideAcceleration = new(nan) }, _ => k with { TriggerThreshold = nan },
        };
        var forged = construction with { Puzzle = construction.Puzzle with { PrimaryAssistance = construction.Puzzle.PrimaryAssistance with { Balanced = poisoned } } };
        Assert.Equal(float.NaN, Lanes(poisoned)[lane]);
        SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Forged(forged, 3)));
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
