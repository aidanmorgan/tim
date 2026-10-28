using CuriousContraptions.Abstractions;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopSaveTests
{

    [Theory]
    [InlineData("ramp", "length", 1.234567f)]
    [InlineData("basket", "capture_margin", .1234567f)]
    [InlineData("basket", "capture_dwell", .2345678f)]
    public void GenericConvertedParametersKeepExactSinglePrecisionThroughSave(string part, string parameter, float value)
    {
        _ = SaveFixture.Elements;
        Assert.True(TestPrecision.NeedsMoreThanElevenSignificantBits(value));
        var id = new PartId(part);
        var values = InstanceParameterCollection.Defaults(id).ToDictionary();
        values[new(parameter)] = value;
        var instance = WorkshopInput.Generic(id, new(1), 0, 1, 0, 0, 0, 0, 1, new(values));
        var saved = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), new(instance)), new(2));
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(saved, decoded);
        Assert.Equal(BitConverter.SingleToInt32Bits(value),
            BitConverter.SingleToInt32Bits(Assert.IsType<WorkshopGenericInstance>(Assert.Single(decoded.Construction.Instances)).Parameters[new(parameter)]));
        Assert.Equal(value, instance.Parameters[new(parameter)]);
    }

    public enum ContentDamage { UnknownPart, UnknownParameter, MissingParameter, OutOfRange, MissingField, WrongBoolean, ForeignGoal, UnknownField, UndefinedState }

    [Theory]
    [InlineData(ContentDamage.UnknownPart)]
    [InlineData(ContentDamage.UnknownParameter)]
    [InlineData(ContentDamage.MissingParameter)]
    [InlineData(ContentDamage.OutOfRange)]
    [InlineData(ContentDamage.MissingField)]
    [InlineData(ContentDamage.WrongBoolean)]
    [InlineData(ContentDamage.ForeignGoal)]
    [InlineData(ContentDamage.UnknownField)]
    [InlineData(ContentDamage.UndefinedState)]
    public void ContentLoadRejectsMalformedDeclarationsBeforeExposure(ContentDamage damage)
    {
        _ = SaveFixture.Elements;
        var before = PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(11), new(.45f));
        var content = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")))!.AsArray();
        var entry = content.Single(row => row!["id"]!.GetValue<string>() == "delayed_signal")!;
        var instance = entry["placements"]![1]!["instance"]!;
        switch (damage)
        {
            case ContentDamage.UnknownPart: instance["part"] = "unknown"; break;
            case ContentDamage.UnknownParameter: instance["parameters"]!["unknown"] = 1; break;
            case ContentDamage.MissingParameter: instance["parameters"]!.AsObject().Remove("trigger_threshold"); break;
            case ContentDamage.OutOfRange: instance["parameters"]!["trigger_threshold"] = 65; break;
            case ContentDamage.MissingField: instance.AsObject().Remove("rotation"); break;
            case ContentDamage.WrongBoolean: instance["locked"] = "true"; break;
            case ContentDamage.ForeignGoal: entry["goal"]!["source"] = "foreign"; break;
            case ContentDamage.UnknownField: entry["extra"] = 1; break;
            case ContentDamage.UndefinedState: instance["enabled"] = 256; break;
            default: throw new ArgumentOutOfRangeException(nameof(damage));
        }
        var error = Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions(content.ToJsonString()));
        Assert.Contains("delayed_signal", error.Message);
        Assert.Equal(before, PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(11), new(.45f)));
    }

    [Fact]
    public void ContentLoadRejectsEmptyDuplicateAndNoPlayableCatalogue()
    {
        _ = SaveFixture.Elements;
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions("[]"));
        var json = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json"));
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions(json.Replace("\"part\": \"ball\"", "\"part\": \"ball\", \"part\": \"ball\"")));
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogue.ReadDefinitions(json.Replace("\"availability\": \"Playable\"", "\"availability\": \"Planned\"")));
        Assert.Equal(12, PuzzleCatalogue.ReadDefinitions(json).Count);
    }

    public enum PlacedConfigurationDamage { Capture, Speed, Dwell, SupportMargin, Acceleration, Participation }

    [Theory]
    [InlineData(PlacedConfigurationDamage.Capture)]
    [InlineData(PlacedConfigurationDamage.Speed)]
    [InlineData(PlacedConfigurationDamage.Dwell)]
    [InlineData(PlacedConfigurationDamage.SupportMargin)]
    [InlineData(PlacedConfigurationDamage.Acceleration)]
    [InlineData(PlacedConfigurationDamage.Participation)]
    public void PlacedGoalConfigurationRejectsForgeryButPreservesPlayerPose(PlacedConfigurationDamage damage)
    {
        _ = SaveFixture.Elements;
        var initial = PuzzleCatalogue.Create(new("ready_to_rebound"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var input = WorkshopInput.Generic(PartId.Receiver, new(8), 2.25f, 1.75f, .5f, 0, 0, 0, 1);
        var valid = initial.WithInstance(input); valid.Validate();
        var receiver = ((WorkshopGenericInstance)valid.Instances.Single(instance => instance.PartId == PartId.Receiver));
        Assert.Equal(input.Cell, receiver.Cell); Assert.Equal(input.Local, receiver.Local); Assert.Equal(input.Rotation, receiver.Rotation);
        Assert.Equal(receiver.Id, valid.Puzzle.Goal.Target);
        var saved = new WorkshopSavedConstruction(valid, new(9));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved)));
        (ParameterId key, float value) = damage switch
        {
            PlacedConfigurationDamage.Capture => (new ParameterId("capture_margin"), .1f),
            PlacedConfigurationDamage.Speed => (new ParameterId("capture_speed_limit"), 10f),
            PlacedConfigurationDamage.Dwell => (new ParameterId("capture_dwell"), 2f),
            PlacedConfigurationDamage.SupportMargin => (new ParameterId("force_support_margin"), .9f),
            PlacedConfigurationDamage.Acceleration => (new ParameterId("force_maximum_acceleration"), 12f),
            PlacedConfigurationDamage.Participation => (new ParameterId("capture_participation"), 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        var values = receiver.Parameters.ToDictionary(); values[key] = value;
        var forged = receiver with { Parameters = new(values) };
        var bad = valid with { Instances = valid.Instances.With(forged) };
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, bad.Validate);
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var record = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, bad, validate: false);
        var bytes = SaveFixture.Finish(builder, record, 9);
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved)));
    }

    /// <summary>Story 9A.44 (D5): every Playable lesson, at precisions 0, 0.45 and 1, admits exactly its authored content as f32: one
    /// instance per placement with the authored part, cell, f32 local remainder, rotation and lock; the authored inventory counts; the
    /// authored goal kind and minimum duration; and every lane of its primary, target and placement knots bit for bit the f32 of the
    /// authored number (never a half-precision value). Covered elsewhere, so not repeated here: each element's compiled bodies,
    /// colliders, materials and joints against their f32 closed forms (PackageRuntimeParityTests); each lesson solving with Run, Reset
    /// and save/load (LaunchLessonSolveTests); behaviour against the half-precision baseline (F32BehaviourIdentityTests); the instance
    /// parameters an interpolated precision sets (JoinedPipeTests).</summary>
    [Fact]
    public void EveryLessonAdmitsItsAuthoredPlacementsInventoryGoalAndKnotsAsF32()
    {
        _ = SaveFixture.Elements;
        using var content = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(
            twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        static float F(System.Text.Json.JsonElement number) => (float)number.GetDouble();
        static float[] Lanes(AssistanceKnot k) =>
        [
            k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value, k.MaximumRotationCorrectionDegrees,
            k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value, k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold
        ];
        static void Profile(System.Text.Json.JsonElement authored, AssistanceProfile profile, string where)
        {
            var knots = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
            Assert.Equal(3, authored.GetArrayLength());
            for (var k = 0; k < 3; k++)
                Assert.True(authored[k].EnumerateArray().Select(F).Select(BitConverter.SingleToUInt32Bits)
                    .SequenceEqual(Lanes(knots[k]).Select(BitConverter.SingleToUInt32Bits)), $"{where} knot {k}");
        }
        var lessons = content.RootElement.EnumerateArray().Where(level => level.GetProperty("availability").GetString() == "Playable").ToArray();
        Assert.Equal(PuzzleCatalogue.Playable.Count, lessons.Length);
        foreach (var precision in new[] { 0f, .45f, 1f })
            foreach (var lesson in lessons)
            {
                var id = lesson.GetProperty("id").GetString()!;
                var construction = PuzzleCatalogue.Create(new(id), new(1), WorkshopCadenceSettings.Default(), new(11), new(precision));
                var placements = lesson.GetProperty("placements").EnumerateArray().Select(p => p.GetProperty("instance")).ToArray();
                var instances = construction.Instances.Cast<WorkshopGenericInstance>().ToArray();
                Assert.Equal(placements.Length, instances.Length);
                for (var i = 0; i < placements.Length; i++)
                {
                    var (authored, instance) = (placements[i], instances[i]);
                    var cell = authored.GetProperty("cell"); var local = authored.GetProperty("local"); var rotation = authored.GetProperty("rotation");
                    Assert.Equal(new PartId(authored.GetProperty("part").GetString()!), instance.PartId);
                    Assert.Equal(new CellOrigin(cell[0].GetInt32(), cell[1].GetInt32(), cell[2].GetInt32()), instance.Cell);
                    Assert.Equal(new LocalPosition(F(local[0]), F(local[1]), F(local[2])), instance.Local);
                    Assert.Equal(new CanonicalRotation(F(rotation[0]), F(rotation[1]), F(rotation[2]), F(rotation[3])), instance.Rotation);
                    Assert.Equal(authored.GetProperty("locked").GetBoolean(), instance.Locked);
                    // Its parameters at this precision: each authored value, and each assistance-bound one the f32 lane of the
                    // placement's own knot at this precision (0, 0.45 and 1 are exactly its three knots).
                    var expected = authored.GetProperty("parameters").EnumerateObject().ToDictionary(p => p.Name, p => F(p.Value), StringComparer.Ordinal);
                    var knot = authored.GetProperty("assistance")[precision == 0f ? 0 : precision == 1f ? 2 : 1];
                    foreach (var binding in authored.GetProperty("assistance_bindings").EnumerateArray())
                        if (binding.GetProperty("parameter").ValueKind == System.Text.Json.JsonValueKind.String)
                            expected[binding.GetProperty("parameter").GetString()!] = F(knot[1 + (int)Enum.Parse<AssistanceValue>(binding.GetProperty("value").GetString()!)]);
                    Assert.Equal(expected.Count, instance.Parameters.Count);
                    foreach (var (key, value) in expected)
                        Assert.True(BitConverter.SingleToUInt32Bits(value) == BitConverter.SingleToUInt32Bits(instance.Parameters[new(key)]),
                            $"{id}@{precision} {authored.GetProperty("part").GetString()} {key}: {instance.Parameters[new(key)]}, authored {value}");
                }
                // The goal's identities are its authored slots' instances (a goal on a part the player places names a fresh one).
                var slots = lesson.GetProperty("placements").EnumerateArray().Select(p => p.GetProperty("slot").GetString()!).ToArray();
                GpuBodyId Slot(string slot) => Array.IndexOf(slots, slot) is var index and >= 0 ? instances[index].Id : default;
                var authoredGoal = lesson.GetProperty("goal");
                var (sourceId, targetId) = (Slot(authoredGoal.GetProperty("source").GetString()!), Slot(authoredGoal.GetProperty("target").GetString()!));
                var compiledGoal = construction.Puzzle.Goal;
                Assert.NotEqual(default, sourceId);
                if (compiledGoal.Kind == WorkshopGoalKind.Captured)
                {
                    Assert.Equal((sourceId, default(ActivationNodeId), default(ActivationNodeId)), (compiledGoal.Body, compiledGoal.SourceNode, compiledGoal.TargetNode));
                    if (targetId != default) Assert.Equal(targetId, compiledGoal.Target);
                    else Assert.DoesNotContain(compiledGoal.Target, instances.Select(instance => instance.Id));
                    Assert.NotEqual(default, compiledGoal.Target);
                }
                else
                {
                    Assert.Equal((default(GpuBodyId), default(GpuBodyId), new ActivationNodeId(sourceId.Value), new ActivationNodeId(targetId.Value)),
                        (compiledGoal.Body, compiledGoal.Target, compiledGoal.SourceNode, compiledGoal.TargetNode));
                }
                var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
                foreach (var row in lesson.GetProperty("inventory").EnumerateArray())
                    Assert.Equal(PartAllowance.Counted(row.GetProperty("count").GetInt32()), inventory[new(row.GetProperty("part").GetString()!)]);
                Assert.Equal(lesson.GetProperty("inventory").GetArrayLength(), inventory.Count);
                var goal = lesson.GetProperty("goal");
                Assert.Equal(Enum.Parse<WorkshopGoalKind>(goal.GetProperty("kind").GetString()!), construction.Puzzle.Goal.Kind);
                Assert.Equal(BitConverter.SingleToUInt32Bits(F(goal.GetProperty("minimum_seconds"))),
                    BitConverter.SingleToUInt32Bits(construction.Puzzle.Goal.MinimumDuration.Value));
                var profiles = lesson.GetProperty("profiles");
                Profile(profiles.GetProperty("primary"), construction.Puzzle.PrimaryAssistance, $"{id} primary");
                Profile(profiles.GetProperty("target"), construction.Puzzle.TargetAssistance, $"{id} target");
                Profile(profiles.GetProperty("placement"), construction.Puzzle.PlacementAssistance, $"{id} placement");
            }
    }
    // Every malformed stored save, in a deterministic corpus (each truncation, each single-bit flip and seeded random
    // buffers of a valid save with parts and connections), decodes or is the typed save refusal; nothing else escapes.
    [Fact]
    public void EveryMalformedSaveIsTheTypedRefusal()
    {
        var valid = WorkshopSaveCodec.Encode(ActivationSaved());
        var random = new Random(9);
        var corpus = new List<byte[]>();
        for (var length = 0; length < valid.Length; length++) corpus.Add(valid[..length]);
        for (var bit = 0; bit < valid.Length * 8; bit++)
        {
            var flipped = (byte[])valid.Clone();
            flipped[bit / 8] ^= (byte)(1 << (bit % 8));
            corpus.Add(flipped);
        }
        for (var count = 0; count < 1000; count++)
        {
            var noise = new byte[random.Next(0, valid.Length * 2)];
            random.NextBytes(noise);
            corpus.Add(noise);
        }
        var escaped = new List<string>();
        foreach (var bytes in corpus)
        {
            var error = Record.Exception(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
            if (error is not (null or WorkshopSaveRejectedException)) escaped.Add($"{bytes.Length} bytes: {error.GetType().FullName}: {error.Message}");
        }
        Assert.Empty(escaped);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.45)]
    [InlineData(1)]
    public void ReceiverForceRegionSurvivesSaveAndCompilesWithoutPuzzleSelection(double precision)
    {
        var receiver = ((WorkshopGenericInstance)PuzzleCatalogue.Definition(new("first_principles")).Placements[1].Instance.Create(new(3), new((float)precision))) with { Locked = false };
        var construction = Saved().Construction.WithInstance(receiver);
        var saved = new WorkshopSavedConstruction(construction, new(7));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved, decoded);
        var scene = WorkshopPhysicsCompiler.Compile(decoded.Construction, new(1, 2));
        {
            var guide = Assert.Single(scene.Guides.ToArray());
            Assert.Equal(new MetreVector(-1.1f, .5f, -1.1f), guide.Minimum);
            Assert.Equal(new MetreVector(1.1f, 1.5f, 1.1f), guide.Maximum);
            Assert.Equal((float)receiver.Parameters[new("force_maximum_acceleration")], guide.MaximumAcceleration.Value);
            Assert.Equal(.5f, guide.SupportHeight.Value);
            Assert.Equal((float)receiver.Parameters[new("force_support_margin")], guide.SupportMargin.Value);
        }
        Assert.Equal(0f, Assert.Single(WorkshopPhysicsCompiler.Compile(Saved().Construction, new(1, 2)).Guides.ToArray()).MaximumAcceleration.Value);
    }

    [Theory]
    [InlineData("force_support_margin", -1f)]
    [InlineData("force_support_margin", 1.01f)]
    [InlineData("force_maximum_acceleration", -1f)]
    [InlineData("force_maximum_acceleration", 12.01f)]
    [InlineData("capture_speed_limit", 0f)]
    [InlineData("capture_dwell", 0f)]
    [InlineData("capture_margin", float.NaN)]
    [InlineData("capture_margin", float.PositiveInfinity)]
    public void InvalidDeclaredReceiverParameterRejectsWithoutChangingExistingConstruction(string parameter, float value)
    {
        var saved = Saved();
        var original = WorkshopSaveCodec.Encode(saved);
        var receiver = (WorkshopGenericInstance)saved.Construction.Instances[1];
        var values = receiver.Parameters.ToDictionary(); values[new(parameter)] = value;
        var malformed = saved.Construction with { Instances = saved.Construction.Instances.With(receiver with { Parameters = new(values) }) };
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformed, validate: false);
        var savedBytes = SaveFixture.Finish(builder, crOffset, saved.NextBodyId.Value);
        SaveFixture.Refused(float.IsFinite(value) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, savedBytes));
        Assert.Equal(original, WorkshopSaveCodec.Encode(saved));
    }

    private static WorkshopSavedConstruction Saved()
    {
        var ball = WorkshopInput.Generic(PartId.Basketball,new(2), .125f, 3, -.25f, 0, 0, 0, 1);
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(3), .5f, 1, -.5f, 0, 0, 0, 1);
        return new(new(new(9), WorkshopCadenceSettings.Default(), new(ball, receiver)), new(7));
    }

    [Fact]
    public void CanonicalBitsAndAllocatorRoundTripWithoutLiveState()
    {
        var saved = Saved();
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.True(bytes.Length >= 16);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes)));
        var empty = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty), new(1));
        Assert.Equal(empty, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(empty)));
    }

    [Theory]
    [InlineData("XXXX", 7UL, true, WorkshopSaveRejection.UnknownIdentifier)]
    [InlineData(WorkshopSaveCodec.Identifier, 0UL, true, WorkshopSaveRejection.InvalidAllocator)]
    [InlineData(WorkshopSaveCodec.Identifier, 2UL, true, WorkshopSaveRejection.InvalidAllocator)]
    [InlineData(WorkshopSaveCodec.Identifier, 7UL, false, WorkshopSaveRejection.MissingField)]
    public void UnknownHeaderCannotBeLoaded(string identifier, ulong nextBodyId, bool includeConstruction, WorkshopSaveRejection reason)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        Google.FlatBuffers.Offset<CuriousContraptions.Wire.ConstructionRecord>? crOffset = includeConstruction
            ? WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, Saved().Construction)
            : null;
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, nextBodyId);
        if (crOffset.HasValue) CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset.Value);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder).Value, identifier);
        SaveFixture.Refused(reason, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,builder.SizedByteArray()));
    }

    [Fact]
    public void TruncatedTrailingAndInvalidAllocatorReject()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        SaveFixture.Refused(WorkshopSaveRejection.Truncated, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes.AsSpan(..^1)));
        SaveFixture.Refused(WorkshopSaveRejection.Truncated, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes.AsSpan(..10)));
        SaveFixture.Refused(WorkshopSaveRejection.TrailingBytes, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,[.. bytes, 0]));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, Saved().Construction);
        var savedBytes = SaveFixture.Finish(builder, crOffset, 3);
        SaveFixture.Refused(WorkshopSaveRejection.InvalidAllocator, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,savedBytes));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(Saved() with { NextBodyId = new(3) }));
    }

    [Theory]
    [InlineData("unsupported_unknown_part")]
    [InlineData("unknown_device_kind")]
    public void UnsupportedPartPopulationRejectsBeforeAdmission(string unknownPartId)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var partIdOffset = builder.CreateString(unknownPartId);
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 2);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        var instOffset = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var instancesOffset = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instOffset]);
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        var defCadence = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)defCadence.Simulation, (uint)defCadence.Physical, (uint)defCadence.Animation, (uint)defCadence.Presentation, defCadence.PresentationRate.Numerator, defCadence.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesOffset);
        var crOffset = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        var savedBytes = SaveFixture.Finish(builder, crOffset, 10);
        SaveFixture.Refused(WorkshopSaveRejection.UnknownElement, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,savedBytes));
    }

    [Fact]
    public void SaveCommandRequiresExactRevisionAndRoundTrips()
    {
        var command = new WorkshopCommand(new(5), WorkshopCommandKind.Save, new(2), new(8), null,
            Session: new(1, 2), Cadence: new(1), Projection: new(2));
        var current = WorkshopWireFlatBuffers.EncodeCommand(command);
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(current));
        Assert.Equal(WireFaultReason.Truncated, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(current.AsSpan(..^1))).Reason);
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(command with
            { RevisionKind = ExpectedRevisionKind.Any, Revision = default }));
    }

    [Fact]
    public void TwoRampInstancesKeepDistinctCanonicalDimensionsThroughSaveAndCompile()
    {
        var saved = Saved();
        var a = WorkshopInput.Generic(PartId.Ramp, new(4), -3.4f, 4.3f, 0, 0, 0, 0, 1);
        var b = WorkshopInput.Generic(PartId.Ramp, new(5), -.7f, 2.7f, 0, 0, 0, 0, 1, new([new(new("length"), 2.5f), new(new("width"), .75f)])) with { Locked = true };
        saved = saved with { Construction = saved.Construction.WithInstance(a).WithInstance(b) };
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var scene = WorkshopPhysicsCompiler.Compile(decoded.Construction, new(1, 2));
        foreach (var ramp in new[] { a, b })
        {
            var body = Assert.Single(scene.Bodies.ToArray(), body => body.Id == ramp.Id);
            Assert.Equal(RigidMotionKind.Static, body.Motion);
            var colliders = scene.Colliders.ToArray().Where(collider => collider.Body == ramp.Id).ToArray();
            Assert.Equal(3, colliders.Length);
            var box = colliders[0];
            Assert.Equal(CuriousContraptions.Gpu.ColliderShapeKind.Box, box.Shape);
            Assert.Equal((float)(ramp.Parameters[new("length")] * .5f), box.HalfExtents.X);
            Assert.Equal((float)(ramp.Parameters[new("width")] * .5f), box.HalfExtents.Z);
            Assert.Equal(.18f * .5f, box.HalfExtents.Y);
            Assert.Equal(1f, Assert.Single(scene.Materials.ToArray(), material => material.Id == box.Material).Restitution.Value);
            // The two rails (elements-visible-solid) scale with the art: the drawn 3 × 0.055 × 0.055 rails at z ∓0.598, y 0.11 have
            // X scaled by length / 3 and Z by width / 1.3. Tolerance: a few f32 roundings of the width / 1.3 product (≤ 1e-6 m).
            var (length, width) = ((float)ramp.Parameters[new("length")], (float)ramp.Parameters[new("width")]);
            foreach (var (rail, side) in new[] { (colliders[1], -1f), (colliders[2], 1f) })
            {
                Assert.Equal(CuriousContraptions.Gpu.ColliderShapeKind.Box, rail.Shape);
                Assert.Equal((length * .5f, .055f * .5f), (rail.HalfExtents.X, rail.HalfExtents.Y));
                Assert.Equal(.0275f * width / 1.3f, rail.HalfExtents.Z, 1e-6f);
                Assert.Equal((0f, .11f), (rail.Pose.Translation.X, rail.Pose.Translation.Y));
                Assert.Equal(side * .598f * width / 1.3f, rail.Pose.Translation.Z, 1e-6f);
            }
        }
    }

    [Fact]
    public void InstanceCollectionOwnsItsArrayAndReplacementDoesNotMutatePriorConstruction()
    {
        var first = Saved().Construction;
        var input = first.Instances.ToArray();
        var copy = new WorkshopInstances(input);
        input[0] = input[1];
        Assert.Equal(first.Instances, copy);
        var receiver = ((WorkshopGenericInstance)first.Instances.Single(instance => instance.PartId == PartId.Receiver)) with { Id = Payloads.First(first).Id };
        var changed = first.WithoutInstance(((WorkshopGenericInstance)first.Instances.Single(instance => instance.PartId == PartId.Receiver)).Id).WithInstance(receiver);
        changed.Validate();
        Assert.NotNull(Payloads.Find(first));
        Assert.Null(Payloads.Find(changed));
        Assert.Equal(receiver.Id, ((WorkshopGenericInstance)changed.Instances.Single(instance => instance.PartId == PartId.Receiver)).Id);
        var saved = new WorkshopSavedConstruction(changed, new(7));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
    }

    [Fact]
    public void DuplicatePopulationAndInvalidDimensionsRejectBeforeGpuAdmission()
    {
        var saved = Saved();
        var duplicate = saved.Construction with { Instances = new(Payloads.First(saved.Construction), Payloads.First(saved.Construction)) };
        Assert.Throws<ArgumentException>(duplicate.Validate);
        var ramp = WorkshopInput.Generic(PartId.Ramp, new(4), 0, 3, 0, 0, 0, 0, 1);
        foreach (var value in new[] { 0f, (-1f), 4.5f, float.NaN, float.PositiveInfinity })
        {
            var invalid = saved with { Construction = saved.Construction.WithInstance(ramp with { Parameters = new([new(new("length"), (float)value), new(new("width"), 1f)]) }) };
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(invalid));
        }
        var excess = saved.Construction.WithInstance(ramp).WithInstance(ramp with { Id = new(5) }).WithInstance(ramp with { Id = new(6) });
        excess.Validate(); // Free play has no per-kind ramp count; WorkbenchCapacityTests cover the compiled table ceilings.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.45)]
    [InlineData(1)]
    public void FirstPrinciplesSettingsAndTwoRampsRoundTripAtEveryAuthoredKnot(double value)
    {
        var precision = new PuzzlePrecision((float)value);
        var construction = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(11), precision);
        var a = WorkshopInput.Generic(PartId.Ramp, new(13), -3.4f, 4.3f, 0, 0, 0, 0, 1);
        var b = WorkshopInput.Generic(PartId.Ramp, new(14), -.7f, 2.7f, 0, 0, 0, 0, 1, new([new(new("length"), 2.5f), new(new("width"), 1f)]));
        construction = construction.WithInstance(a).WithInstance(b);
        var saved = new WorkshopSavedConstruction(construction, new(15));
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes)));
        Assert.True(Payloads.First(construction).Locked); Assert.True(((WorkshopGenericInstance)construction.Instances.Single(instance => instance.PartId == PartId.Receiver)).Locked);
        Assert.Equal(new GpuBodyId(11), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(12), construction.Puzzle.Goal.Target);
        Assert.Equal(2u, construction.Puzzle.InventoryCount);
        var updated = PuzzleCatalogue.WithPrecision(construction, new(.5f));
        Assert.Equal(a, updated.Instances[2]); Assert.Equal(b, updated.Instances[3]);
        Assert.Equal(construction.Puzzle.Goal, updated.Puzzle.Goal);
        foreach (var knot in new[] { 0f, .45f, 1f })
        {
            updated = PuzzleCatalogue.WithPrecision(updated, new(knot));
            var expected = ((WorkshopGenericInstance)PuzzleCatalogue.Definition(new("first_principles")).Placements[1].Instance.Create(construction.Puzzle.Goal.Target, new(knot)));
            Assert.Equal(expected, updated.Instances.Single(instance => instance.PartId == PartId.Receiver));
            var guide = Assert.Single(WorkshopPhysicsCompiler.Compile(updated, new(1, 2)).Guides.ToArray());
            Assert.Equal((float)expected.Parameters[new("force_maximum_acceleration")], guide.MaximumAcceleration.Value);
            Assert.Equal((float)expected.Parameters[new("force_support_margin")], guide.SupportMargin.Value);
            Assert.Equal(a, updated.Instances[2]); Assert.Equal(b, updated.Instances[3]);
            var changedSave = new WorkshopSavedConstruction(updated, new(15));
            Assert.Equal(changedSave, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(changedSave)));
        }
    }

    public enum PuzzleDamage { Id, Mode, Precision, Inventory, Body, Target, Profile, Lock }
    [Theory]
    [InlineData(PuzzleDamage.Id)]
    [InlineData(PuzzleDamage.Mode)]
    [InlineData(PuzzleDamage.Precision)]
    [InlineData(PuzzleDamage.Inventory)]
    [InlineData(PuzzleDamage.Body)]
    [InlineData(PuzzleDamage.Target)]
    [InlineData(PuzzleDamage.Profile)]
    [InlineData(PuzzleDamage.Lock)]
    public void InvalidPuzzleContextRejectsBeforeAdmission(PuzzleDamage damage)
    {
        var construction = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var puzzle = construction.Puzzle;
        var changed = damage switch
        {
            PuzzleDamage.Id => construction with { Puzzle = puzzle with { Id = new PuzzleId("unknown") } },
            PuzzleDamage.Mode => construction with { Puzzle = puzzle with { Placement = (WorkshopPlacementMode)999 } },
            PuzzleDamage.Precision => construction with { Puzzle = puzzle with { Precision = new(float.NaN) } },
            PuzzleDamage.Inventory => construction with { Puzzle = puzzle with { InventoryCount = 3 } },
            PuzzleDamage.Body => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Body = new(9) } } },
            PuzzleDamage.Target => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Target = new(9) } } },
            PuzzleDamage.Profile => construction with { Puzzle = puzzle with { TargetAssistance = default } },
            PuzzleDamage.Lock => construction.WithInstance(Payloads.First(construction) with { Locked = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        // A puzzle rule refuses the change; a non-finite precision is a structural bound (an argument guard).
        var error = Assert.ThrowsAny<ArgumentException>(() => WorkshopSaveCodec.Encode(new(changed, new(10))));
        Assert.Equal(damage == PuzzleDamage.Precision ? typeof(ArgumentException) : typeof(WorkshopEditRejectedException), error.GetType());
        construction.Validate();
    }

    [Fact]
    public void AForeignRootOffsetRejects()
    {
        // The assistance knot count's boundaries are F32SaveBoundaryTests'.
        var construction = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(1), new(1f));
        var bytes = WorkshopSaveCodec.Encode(new(construction, new(3)));
        Assert.Equal(new WorkshopSavedConstruction(construction, new(3)), WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes)); // control
        var foreign = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(foreign.AsSpan(4), 1);
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,foreign));
    }

    [Fact]
    public void NamedGoalUsesCommittedBodySensorAndEpochWithoutAnimationFeedback()
    {
        var construction = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(1), new(1f));
        var goal = construction.Puzzle.Goal; var ball = Payloads.First(construction);
        Assert.True(construction.Instances.Identities.TryCapture(goal.Target, goal.Body, out var capture));
        var body = new CanonicalBody(ball.Id, 2, 1, ball.Cell, ball.Local, default);
        var bodies = new PhysicsBodyReadSet(new[] { new PhysicsBodyRead(body, ball.Rotation, default, default) });
        var read = new WorkshopRead(new(2), new(1), bodies, Captures: new([new(capture.Sensor, ball.Id, CaptureLatchPhase.Latched, 4, 0f)]));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2), construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2), construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, new(3), construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Bodies = new(new[] { bodies[0] with { Body = body with { Id = new(9) } } }) }, new(2), construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Captures = new([CaptureLatch.Clear(capture.Sensor, ball.Id)]) }, new(2), construction.Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Captures = new([new(new(9), ball.Id, CaptureLatchPhase.Latched, 4, 0f)]) }, new(2), construction.Instances.Identities));
    }

    [Fact]
    public void CanonicalAssistanceKeepsEveryAuthoredSourceField()
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement[0];
        Assert.Equal("first_principles", level.GetProperty("id").GetString());
        AuthoredContentAssertions.Profile(level.GetProperty("placements")[0].GetProperty("instance").GetProperty("assistance"), PuzzleCatalogue.Definition(new("first_principles")).Primary);
        AuthoredContentAssertions.Profile(level.GetProperty("placements")[1].GetProperty("instance").GetProperty("assistance"), PuzzleCatalogue.Definition(new("first_principles")).Target);
        foreach (var ramp in level.GetProperty("solution").EnumerateArray()) Check(ramp.GetProperty("difficulty"), PuzzleCatalogue.Definition(new("first_principles")).Placement);
        static void Check(System.Text.Json.JsonElement data, AssistanceProfile profile)
        {
            var values = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
            var names = new[] { "precision", "position_window", "rotation_window", "max_position_correction", "max_rotation_correction",
                "blend_seconds", "capture_margin", "capture_speed", "capture_dwell", "guide_acceleration", "trigger_threshold" };
            for (var i = 0; i < 3; i++)
            {
                var k = values[i];
                float[] actual = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                    k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                    k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
                for (var field = 0; field < names.Length; field++)
                    Assert.Equal(BitConverter.SingleToUInt32Bits((float)data[i].GetProperty(names[field]).GetDouble()), BitConverter.SingleToUInt32Bits(actual[field]));
            }
        }
    }

    private static WorkshopSavedConstruction ActivationSaved()
    {
        var trigger = WorkshopInput.Generic(PartId.ImpactSwitch, new(2), 0, 1, 0, 0, 0, 0, 1);
        var lamp = PlacedParts.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1);
        var link = new WorkshopConnection(trigger.Id, WorkshopSocket.ActivationOut, lamp.Id,
            WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        return new(new(new(4), WorkshopCadenceSettings.Default(), new(trigger, lamp), Connections: new(link)), new(4));
    }

    [Fact]
    public void ActivationInstancesThresholdAndTypedEdgeRoundTripExactly()
    {
        var save = ActivationSaved();
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        Assert.Equal(.8f, ((WorkshopGenericInstance)decoded.Construction.Instances[0]).Parameters[new ParameterId("trigger_threshold")]);
        // Authored values are canonical data, never replaced by the Free Workshop default.
        var changed = save with { Construction = save.Construction.WithInstance(
            ((WorkshopGenericInstance)save.Construction.Instances[0]) with { Parameters = new([new(new ParameterId("trigger_threshold"), .47f)]) }) };
        Assert.Equal(changed, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(changed)));
        Assert.Single(decoded.Construction.Connections);
    }

    [Fact]
    public void ConnectionsRejectWrongDomainPortDirectionIdentityAndDuplicates()
    {
        var save = ActivationSaved(); var construction = save.Construction; var link = construction.Connections[0];
        foreach (var invalid in new[]
        {
            link with { Domain = WorkshopConnectionDomain.Electrical },
            link with { Domain = (WorkshopConnectionDomain)99 },
            link with { Output = WorkshopSocket.PowerIn },
            link with { Input = WorkshopSocket.Supply },
            link with { Source = link.Target, Target = link.Source },
            link with { Target = new(99) }, link with { Target = link.Source },
            link with { Input = (WorkshopSocket)99 }
        })
            EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (construction with { Connections = new(invalid) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (construction with { Connections = new(link, link) }).Validate());
        var source = new[] { link }; var immutable = new WorkshopConnections(source); source[0] = default;
        Assert.Equal(link, immutable[0]);
        Assert.Single(construction.Connections);
        Assert.Empty(construction.WithoutInstance(link.Source).Connections);
        Assert.Empty(construction.WithoutInstance(link.Target).Connections);
    }

    [Fact]
    public void AConstructCommandWithTrailingBytesRejectsBeforeAdmission()
    {
        var save = ActivationSaved();
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), save.Construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var wire = WorkshopWireFlatBuffers.EncodeCommand(command);
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(wire));
        Assert.Equal(WireFaultReason.TrailingBytes, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand([.. wire, 9])).Reason);
    }

    [Fact]
    public void FirstPrinciplesRejectsForeignInstancesAndConnections()
    {
        var puzzle = PuzzleCatalogue.Create(new("first_principles"), new(1), WorkshopCadenceSettings.Default(), new(11), new(1f));
        var activation = ActivationSaved().Construction;
        foreach (var instance in activation.Instances)
            EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => puzzle.WithInstance(instance).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (puzzle with { Connections = activation.Connections }).Validate());
    }

    [Theory]
    [InlineData(99UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn, WorkshopSaveRejection.UnresolvedReference)]
    [InlineData(2UL, 99UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn, WorkshopSaveRejection.UnresolvedReference)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Electrical, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn, WorkshopSaveRejection.InvalidConnection)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationIn, (uint)WorkshopSocket.ActivationIn, WorkshopSaveRejection.InvalidConnection)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationOut, WorkshopSaveRejection.InvalidConnection)]
    public void SerializedInvalidConnectionsRejectBeforeAdmission(ulong source, ulong target, uint domain, uint fromSocket, uint toSocket, WorkshopSaveRejection reason)
    {
        var save = ActivationSaved();
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var instanceOffsets = new Google.FlatBuffers.Offset<CuriousContraptions.Wire.InstanceRecord>[save.Construction.Instances.Count];
        for (var i = 0; i < save.Construction.Instances.Count; i++)
        {
            var inst = save.Construction.Instances[i];
            var partIdOffset = builder.CreateString(inst.PartId.Value);
            Google.FlatBuffers.VectorOffset? parameters = inst is WorkshopGenericInstance generic
                ? CuriousContraptions.Wire.InstanceRecord.CreateParametersVector(builder, generic.Parameters.Select(pair =>
                    CuriousContraptions.Wire.ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key.Value), pair.Value)).ToArray())
                : null;
            CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
            if (parameters is { } vector) CuriousContraptions.Wire.InstanceRecord.AddParameters(builder, vector);
            CuriousContraptions.Wire.InstanceRecord.AddId(builder, inst.Id.Value);
            CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
            CuriousContraptions.Wire.InstanceRecord.AddLocked(builder, inst.Locked);
            CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, inst.Cell.X, inst.Cell.Y, inst.Cell.Z));
            CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, inst.Local.X, inst.Local.Y, inst.Local.Z));
            CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, inst.Rotation.X, inst.Rotation.Y, inst.Rotation.Z, inst.Rotation.W));
            instanceOffsets[i] = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        }
        var instancesVector = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, instanceOffsets);
        CuriousContraptions.Wire.ConstructionRecord.StartConnectionsVector(builder, 1);
        CuriousContraptions.Wire.ConnectionRecord.CreateConnectionRecord(builder, source, target, domain, fromSocket, toSocket);
        var connectionsVector = builder.EndVector();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, save.Construction.Revision.Value);
        var saveCadence = save.Construction.Settings;
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)saveCadence.Simulation, (uint)saveCadence.Physical, (uint)saveCadence.Animation, (uint)saveCadence.Presentation, saveCadence.PresentationRate.Numerator, saveCadence.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesVector);
        CuriousContraptions.Wire.ConstructionRecord.AddConnections(builder, connectionsVector);
        var crOffset = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        var savedBytes = SaveFixture.Finish(builder, crOffset, save.NextBodyId.Value);
        SaveFixture.Refused(reason, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,savedBytes));
    }
}

internal static class AuthoredContentAssertions
{
    internal static void Placement(System.Text.Json.JsonElement data, IWorkshopInstance instance)
    {
        Assert.Equal(new PartId(data.GetProperty("part").GetString()!), instance.PartId);
        var cell = data.GetProperty("cell"); var local = data.GetProperty("local"); var rotation = data.GetProperty("rotation");
        Assert.Equal(new CellOrigin(cell[0].GetInt32(), cell[1].GetInt32(), cell[2].GetInt32()), instance.Cell);
        Assert.Equal(new LocalPosition(local[0].GetSingle(), local[1].GetSingle(), local[2].GetSingle()), instance.Local);
        Assert.Equal(new CanonicalRotation(rotation[0].GetSingle(), rotation[1].GetSingle(), rotation[2].GetSingle(), rotation[3].GetSingle()), instance.Rotation);
        Assert.Equal(data.GetProperty("locked").GetBoolean(), instance.Locked);
    }

    internal static void Profile(System.Text.Json.JsonElement data, AssistanceProfile profile)
    {
        var knots = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
        Assert.Equal(knots.Length, data.GetArrayLength());
        for (var i = 0; i < knots.Length; i++)
        {
            var k = knots[i];
            float[] values = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
            Assert.Equal(values.Length, data[i].GetArrayLength());
            for (var j = 0; j < values.Length; j++) Assert.Equal(values[j], (float)data[i][j].GetSingle());
        }
    }
}
