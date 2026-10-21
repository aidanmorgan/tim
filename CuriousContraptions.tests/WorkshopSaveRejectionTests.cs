using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.2: a stored construction save is pre-validated structurally before any domain construction runs. Every
/// violation is one typed <see cref="WorkshopSaveRejectedException"/> carrying its closed <see cref="WorkshopSaveRejection"/>
/// reason; domain argument guards stay programmer-error faults, so an ArgumentException after pre-validation is never a
/// refusal. Pre-validation admits exactly what the domain admits (parity), and nothing but the typed refusal escapes it.</summary>
public sealed class WorkshopSaveRejectionTests
{
    // ---------- a hand-built save record whose every field the test controls ----------

    /// <summary>InstanceRecord field ids, in schema order (the vtable slot of each field).</summary>
    public enum Field
    {
        Id, PartId, Locked, Cell, Local, Rotation, Radius, Mass, Bounce, Drag, Buoyancy, Margin, SpeedLimit, Dwell, Participation,
        ForceMin, ForceMax, SupportHeight, SupportMargin, MaxAcceleration, Length, Width, Threshold, Duration,
        Stiffness, Damping, InitialCompression, Capacity, MaximumPower, InitialFraction, Enabled, Strength, ReferenceMass, Preload,
        HalfExtents, Friction, TargetCount, Parameters,
    }

    internal sealed class Instance(string part, ulong id)
    {
        public string Part { get; set; } = part;
        public Dictionary<Field, object> Values { get; } = new() { [Field.Id] = id };
        public Instance With(Field field, object value) { var copy = Clone(); copy.Values[field] = value; return copy; }
        public Instance Without(Field field) { var copy = Clone(); copy.Values.Remove(field); return copy; }
        public Instance Clone() { var copy = new Instance(Part, 0); copy.Values.Clear(); foreach (var (k, v) in Values) copy.Values[k] = v; return copy; }
    }

    internal sealed record Link(ulong Source, ulong Target, uint Domain, uint From, uint To);

    internal sealed record Save(Instance[] Instances, Link[] Links, ulong NextBodyId = 100, ulong Revision = 1, bool Settings = true,
        uint Simulation = uint.MaxValue, bool HasConstruction = true, string Identifier = WorkshopSaveCodec.Identifier);

    private static Offset<InstanceRecord> Write(FlatBufferBuilder builder, Instance instance)
    {
        var part = builder.CreateString(instance.Part);
        VectorOffset? parameters = null;
        if (instance.Values.TryGetValue(Field.Parameters, out var values))
        {
            var offsets = ((KeyValuePair<string, float>[])values).Select(pair =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(pair.Key), pair.Value)).ToArray();
            parameters = InstanceRecord.CreateParametersVector(builder, offsets);
        }
        builder.StartTable(Enum.GetValues<Field>().Length);
        if (parameters.HasValue) InstanceRecord.AddParameters(builder, parameters.Value);
        foreach (var (field, value) in instance.Values)
        {
            if (field == Field.Parameters) continue;
            var slot = (int)field;
            switch (value)
            {
                case ulong v: builder.AddUlong(slot, v, 0); break;
                case bool v: builder.AddBool(slot, v, false); break;
                case float v: builder.AddFloat(slot, v, 0f); break;
                case uint v: builder.AddUint(slot, v, 0); break;
                case byte v: builder.AddByte(slot, v, 0); break;
                case (int x, int y, int z): builder.AddStruct(slot, Vec3i.CreateVec3i(builder, x, y, z).Value, 0); break;
                case (float x, float y, float z): builder.AddStruct(slot, Vec3f.CreateVec3f(builder, x, y, z).Value, 0); break;
                case (float x, float y, float z, float w): builder.AddStruct(slot, Quat4f.CreateQuat4f(builder, x, y, z, w).Value, 0); break;
                default: throw new ArgumentException($"Unsupported fixture value for {field}.");
            }
        }
        InstanceRecord.AddPartId(builder, part);
        return new(builder.EndTable());
    }

    internal static byte[] Bytes(Save save)
    {
        var builder = new FlatBufferBuilder(1024);
        var instances = save.Instances.Select(instance => Write(builder, instance)).ToArray();
        var vector = ConstructionRecord.CreateInstancesVector(builder, instances);
        VectorOffset? links = null;
        if (save.Links.Length > 0)
        {
            ConstructionRecord.StartConnectionsVector(builder, save.Links.Length);
            for (var i = save.Links.Length - 1; i >= 0; i--)
                ConnectionRecord.CreateConnectionRecord(builder, save.Links[i].Source, save.Links[i].Target, save.Links[i].Domain, save.Links[i].From, save.Links[i].To);
            links = builder.EndVector();
        }
        var settings = WorkshopCadenceSettings.Default();
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, save.Revision);
        if (save.Settings)
            ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
                save.Simulation == uint.MaxValue ? (uint)settings.Simulation : save.Simulation, (uint)settings.Physical,
                (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, vector);
        if (links is { } offset) ConstructionRecord.AddConnections(builder, offset);
        var record = ConstructionRecord.EndConstructionRecord(builder);
        SavedConstruction.StartSavedConstruction(builder);
        SavedConstruction.AddNextBodyId(builder, save.NextBodyId);
        if (save.HasConstruction) SavedConstruction.AddConstruction(builder, record);
        builder.FinishSizePrefixed(SavedConstruction.EndSavedConstruction(builder).Value, save.Identifier);
        return builder.SizedByteArray();
    }

    /// <summary>The fields a production save writes for one valid instance of each kind, read back from its own record.</summary>
    internal static Instance FromProduction(IWorkshopInstance instance)
    {
        var bytes = WorkshopSaveCodec.Encode(new(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(instance)), new(instance.Id.Value + 1)));
        var bb = new ByteBuffer(bytes); bb.Position = 4;
        var record = SavedConstruction.GetRootAsSavedConstruction(bb).Construction!.Value.Instances(0)!.Value;
        var result = new Instance(record.PartId, record.Id);
        if (record.Locked) result.Values[Field.Locked] = true;
        var c = record.Cell!.Value; result.Values[Field.Cell] = (c.X, c.Y, c.Z);
        var l = record.Local!.Value; result.Values[Field.Local] = (l.X, l.Y, l.Z);
        var r = record.Rotation!.Value; result.Values[Field.Rotation] = (r.X, r.Y, r.Z, r.W);
        foreach (var field in Parameters(record.PartId))
        {
            object value = field switch
            {
                Field.Radius => record.Radius, Field.Mass => record.Mass, Field.Bounce => record.Bounce, Field.Drag => record.Drag,
                Field.Buoyancy => record.Buoyancy, Field.Margin => record.Margin, Field.SpeedLimit => record.SpeedLimit, Field.Dwell => record.Dwell,
                Field.Participation => record.Participation, Field.SupportHeight => record.SupportHeight, Field.SupportMargin => record.SupportMargin,
                Field.MaxAcceleration => record.MaxAcceleration, Field.Length => record.Length, Field.Width => record.Width,
                Field.Threshold => record.Threshold, Field.Duration => record.Duration, Field.Stiffness => record.Stiffness,
                Field.Damping => record.Damping, Field.InitialCompression => record.InitialCompression, Field.Capacity => record.Capacity,
                Field.MaximumPower => record.MaximumPower, Field.InitialFraction => record.InitialFraction, Field.Enabled => record.Enabled,
                Field.Strength => record.Strength, Field.ReferenceMass => record.ReferenceMass, Field.Preload => record.Preload,
                Field.Friction => record.Friction, Field.TargetCount => record.TargetCount,
                Field.ForceMin => Vec(record.ForceMin!.Value), Field.ForceMax => Vec(record.ForceMax!.Value), Field.HalfExtents => Vec(record.HalfExtents!.Value),
                _ => throw new InvalidOperationException($"No fixture reader for {field}."),
            };
            result.Values[field] = value;
        }
        if (instance is WorkshopGenericInstance generic)
            result.Values[Field.Parameters] = generic.Parameters.Select(pair => KeyValuePair.Create(pair.Key.Value, pair.Value)).ToArray();
        return result;
        static (float, float, float) Vec(Vec3f v) => (v.X, v.Y, v.Z);
    }

    /// <summary>The kind-specific fields each part's record carries.</summary>
    internal static Field[] Parameters(string part) => part switch
    {
        "ball" or "bowling" => [Field.Radius, Field.Mass, Field.Bounce, Field.Drag, Field.Buoyancy],
        "basket" => [Field.Margin, Field.SpeedLimit, Field.Dwell, Field.Participation, Field.ForceMin, Field.ForceMax, Field.SupportHeight, Field.SupportMargin, Field.MaxAcceleration],
        "ramp" => [Field.Length, Field.Width],
        "switch" or "pressure_plate" or "clock" => [Field.Threshold],
        "delay" => [Field.Duration],
        "spring" => [Field.Stiffness, Field.Damping, Field.InitialCompression],
        "battery" => [Field.Capacity, Field.MaximumPower, Field.InitialFraction, Field.Enabled],
        "bumper" => [Field.Strength, Field.ReferenceMass, Field.Preload],
        "domino" => [Field.HalfExtents, Field.Mass, Field.Bounce, Field.Friction],
        "pipe" => [Field.Length],
        "counter" => [Field.TargetCount],
        _ => [],
    };

    /// <summary>One valid instance of every decodable kind, at distinct identities.</summary>
    internal static IWorkshopInstance[] EveryKind() =>
    [
        WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1),
        WorkshopInput.Ball(PartId.BowlingBall, new(2), 1, 3, 0, 0, 0, 0, 1),
        WorkshopInput.Receiver(new(3), 4, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Ramp(new(4), -2, 1, 0, 0, 0, 0, 1, RampDimensions.Default),
        WorkshopInput.Generic(PartId.Wall, new(5), -4, 1, 0, 0, 0, 0, 1, WallFixture.Default),
        WorkshopInput.Switch(new(6), 2, 0, 0, 0, 0, 0, 1, ContactTriggerSettings.Default),
        WorkshopInput.Delay(new(7), 3, 0, 0, 0, 0, 0, 1, DelayDuration.Default),
        WorkshopInput.Springboard(new(8), 5, 0, 0, 0, 0, 0, 1, SpringboardSettings.Default),
        WorkshopInput.Battery(new(9), 6, 0, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default),
        WorkshopInput.Bumper(new(10), 7, 0, 0, 0, 0, 0, 1, BumperWork.Default),
        WorkshopInput.Domino(new(11), 8, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Lamp(new(12), 9, 0, 0, 0, 0, 0, 1),
        WorkshopInput.BallDetector(new(13), 10, 0, 0, 0, 0, 0, 1),
        WorkshopInput.PressurePlate(new(14), 11, 0, 0, 0, 0, 0, 1, WorkshopPressurePlate.PlacedMinimumMass),
        WorkshopInput.Counter(new(15), 12, 0, 0, 0, 0, 0, 1, WorkshopCounter.PlacedTargetCount),
        WorkshopInput.Clock(new(16), 13, 0, 0, 0, 0, 0, 1, 1f),
        WorkshopInput.Latch(new(17), 14, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Pipe(new(18), 15, 0, 0, 0, 0, 0, 1),
        WorkshopInput.PipeBend45(new(19), 16, 0, 0, 0, 0, 0, 1),
        WorkshopInput.PipeBend90(new(20), 17, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Funnel(new(21), 18, 0, 0, 0, 0, 0, 1),
        WorkshopInput.Generic(PartId.PoweredGate, new(22), 19, 0, 0, 0, 0, 0, 1),
    ];


    [Fact]
    public void GenericVectorsRejectMissingDuplicateUnknownAndMixedRecordPayloads()
    {
        var wall = FromProduction(WorkshopInput.Generic(PartId.Wall, new(1), 0, 2, 0, 0, 0, 0, 1));
        KeyValuePair<string, float>[] valid = [new("width", 3f), new("height", 2f), new("thickness", .25f)];
        foreach (var malformed in new KeyValuePair<string, float>[][]
        {
            [], [new("width", 3f), new("height", 2f)],
            [.. valid, new("width", 4f)], [.. valid, new("unknown", 1f)],
            [new("", 3f), new("height", 2f), new("thickness", .25f)]
        })
        {
            var bytes = Bytes(One(wall.With(Field.Parameters, malformed)));
            Assert.Equal(WorkshopSaveRejection.OutOfRange, Rejection(bytes));
            var construction = WorkshopSaveCodec.Open(bytes).Construction!.Value;
            Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeConstructionRecord(construction));
        }
        foreach (var instance in EveryKind().Where(instance => instance is not WorkshopGenericInstance))
        {
            var forged = FromProduction(instance).With(Field.Parameters, valid);
            var bytes = Bytes(One(forged));
            Assert.Equal(WorkshopSaveRejection.OutOfRange, Rejection(bytes));
            Assert.Throws<WireFaultException>(() =>
                WorkshopWireFlatBuffers.DecodeConstructionRecord(WorkshopSaveCodec.Open(bytes).Construction!.Value));
        }
    }

    private static Save One(Instance instance) => new([instance], []);

    private static WorkshopSaveRejection Rejection(byte[] bytes) =>
        Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes)).Reason;

    private static Instance Ramp() => FromProduction(WorkshopInput.Ramp(new(1), 0, 1, 0, 0, 0, 0, 1, RampDimensions.Default));
    private static Instance Counter() => FromProduction(WorkshopInput.Counter(new(1), 0, 1, 0, 0, 0, 0, 1, 3));

    // ---------- every rejection reason ----------

    [Fact]
    public void ValidHandBuiltSaveLoads() => Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,Bytes(One(Ramp()))).Construction.Instances);

    [Fact]
    public void EveryKindRoundTripsThroughTheHandBuiltRecord()
    {
        var instances = EveryKind();
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,Bytes(new(instances.Select(FromProduction).ToArray(), [])));
        Assert.Equal(new WorkshopInstances(instances), decoded.Construction.Instances);
    }

    /// <summary>Every authored lesson, as built and with its goal part deleted (CAT-062b MissingReceiver), is admitted by the domain,
    /// so its save must load: pre-validation is never stricter than domain construction.</summary>
    [Fact]
    public void EveryAuthoredLessonSaveLoadsIncludingAMissingGoalPart()
    {
        var settings = WorkshopCadenceSettings.Default();
        var precision = new PuzzlePrecision((Half)1);
        var lessons = new[]
        {
            FirstPrinciples.Create(new(1), settings, new(1), new(2), precision),
            SpringboardLessons.Create(WorkshopPuzzleId.SpringForward, new(1), settings, new(1), new(2), new(3), precision),
            SpringboardLessons.Create(WorkshopPuzzleId.ReadyToRebound, new(1), settings, new(1), new(2), new(3), precision),
            BumperSidekick.Create(new(1), settings, new(1), new(2), precision),
            BumperAdvanced.Create(WorkshopPuzzleId.BumperDepth, new(1), settings, new(1), new(2), precision),
            BumperAdvanced.Create(WorkshopPuzzleId.WallAndBumper, new(1), settings, new(1), new(2), precision),
            DominoEffect.Create(new(1), settings, new(1), new(2), new(3), precision),
            DelayedSignal.Create(new(1), settings, new(1), new(2), new(3), precision),
            ClearPipe.Create(new(1), settings, new(1), new(2), precision),
            GentleBend.Create(new(1), settings, new(1), new(2), precision),
            QuarterBend.Create(new(1), settings, new(1), new(2), precision),
            JoinedPipe.Create(new(1), settings, new(1), new(2), new(3), precision),
        };
        foreach (var lesson in lessons)
        {
            var saved = new WorkshopSavedConstruction(lesson, new(100));
            Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        }
        var ready = SpringboardLessons.WithReceiver(lessons[2], WorkshopInput.Receiver(new(4), 2, .55, 0, 0, 0, 0, 1));
        var missing = ready.WithoutInstance(new(4));
        missing.Validate(); // control: the domain admits the lesson without its receiver
        var withoutReceiver = new WorkshopSavedConstruction(missing, new(100));
        Assert.Equal(withoutReceiver, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(withoutReceiver)));
    }

    [Fact]
    public void StoredTextThatIsNotBase64IsRejected() =>
        Assert.Equal(WorkshopSaveRejection.NotBase64, Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.DecodeStored(SaveFixture.Elements,"not base64!")).Reason);

    [Fact]
    public void StoredBase64DecodesTheSave() =>
        Assert.Single(WorkshopSaveCodec.DecodeStored(SaveFixture.Elements,Convert.ToBase64String(Bytes(One(Ramp())))).Construction.Instances);

    public static TheoryData<string, WorkshopSaveRejection> Violations()
    {
        var data = new TheoryData<string, WorkshopSaveRejection>();
        foreach (var (name, reason) in ViolationRows) data.Add(name, reason);
        return data;
    }

    private static readonly (string Name, WorkshopSaveRejection Reason)[] ViolationRows =
    [
        ("no construction", WorkshopSaveRejection.MissingField),
        ("no settings", WorkshopSaveRejection.MissingField),
        ("no cell", WorkshopSaveRejection.MissingField),
        ("no local", WorkshopSaveRejection.MissingField),
        ("no rotation", WorkshopSaveRejection.MissingField),
        ("no receiver force minimum", WorkshopSaveRejection.MissingField),
        ("no domino half extents", WorkshopSaveRejection.MissingField),
        ("no counter target", WorkshopSaveRejection.MissingField),
        ("undefined simulation cadence", WorkshopSaveRejection.UndefinedEnum),
        ("undefined receiver participation", WorkshopSaveRejection.UndefinedEnum),
        ("undefined battery enable", WorkshopSaveRejection.UndefinedEnum),
        ("undefined connection domain", WorkshopSaveRejection.UndefinedEnum),
        ("undefined connection socket", WorkshopSaveRejection.UndefinedEnum),
        ("NaN ramp length", WorkshopSaveRejection.NonFinite),
        ("infinite local position", WorkshopSaveRejection.NonFinite),
        ("NaN rotation", WorkshopSaveRejection.NonFinite),
        ("long ramp", WorkshopSaveRejection.OutOfRange),
        ("ramp beyond binary16", WorkshopSaveRejection.OutOfRange),
        ("counter target ten", WorkshopSaveRejection.OutOfRange),
        ("zero revision", WorkshopSaveRejection.OutOfRange),
        ("zero instance identity", WorkshopSaveRejection.OutOfRange),
        ("cell outside the world", WorkshopSaveRejection.OutOfRange),
        ("local remainder of one half", WorkshopSaveRejection.OutOfRange),
        ("scaled rotation", WorkshopSaveRejection.OutOfRange),
        ("changed ball material", WorkshopSaveRejection.OutOfRange),
        ("unknown element", WorkshopSaveRejection.UnknownElement),
        ("no element", WorkshopSaveRejection.MissingField),
        ("duplicate instance", WorkshopSaveRejection.DuplicateInstance),
        ("dangling connection", WorkshopSaveRejection.UnresolvedReference),
        ("ramp to ramp electrical connection", WorkshopSaveRejection.InvalidConnection),
        ("allocator at an existing identity", WorkshopSaveRejection.InvalidAllocator),
    ];

    private static Save Violation(string name)
    {
        var ramp = Ramp();
        var receiver = FromProduction(WorkshopInput.Receiver(new(1), 0, 1, 0, 0, 0, 0, 1));
        var battery = FromProduction(WorkshopInput.Battery(new(2), 0, 1, 0, 0, 0, 0, 1, ElectricalSourceSettings.Default));
        var lamp = FromProduction(WorkshopInput.Lamp(new(3), 2, 1, 0, 0, 0, 0, 1));
        var electrical = (uint)WorkshopConnectionDomain.Electrical;
        return name switch
        {
            "no construction" => One(ramp) with { HasConstruction = false },
            "no settings" => One(ramp) with { Settings = false },
            "no cell" => One(ramp.Without(Field.Cell)),
            "no local" => One(ramp.Without(Field.Local)),
            "no rotation" => One(ramp.Without(Field.Rotation)),
            "no receiver force minimum" => One(receiver.Without(Field.ForceMin)),
            "no domino half extents" => One(FromProduction(WorkshopInput.Domino(new(1), 0, 1, 0, 0, 0, 0, 1)).Without(Field.HalfExtents)),
            "no counter target" => One(Counter().Without(Field.TargetCount)),
            "undefined simulation cadence" => One(ramp) with { Simulation = 99 },
            "undefined receiver participation" => One(receiver.With(Field.Participation, 99u)),
            "undefined battery enable" => One(battery.With(Field.Enabled, (byte)7)),
            "undefined connection domain" => new([battery, lamp], [new(2, 3, 99, (uint)WorkshopSocket.Supply, (uint)WorkshopSocket.PowerIn)]),
            "undefined connection socket" => new([battery, lamp], [new(2, 3, electrical, 999, (uint)WorkshopSocket.PowerIn)]),
            "NaN ramp length" => One(ramp.With(Field.Length, float.NaN)),
            "infinite local position" => One(ramp.With(Field.Local, (float.PositiveInfinity, 0f, 0f))),
            "NaN rotation" => One(ramp.With(Field.Rotation, (0f, float.NaN, 0f, 1f))),
            "long ramp" => One(ramp.With(Field.Length, 10f)),
            "ramp beyond binary16" => One(ramp.With(Field.Length, 1e6f)),
            "counter target ten" => One(Counter().With(Field.TargetCount, 10u)),
            "zero revision" => One(ramp) with { Revision = 0 },
            "zero instance identity" => One(ramp.With(Field.Id, 0UL)),
            "cell outside the world" => One(ramp.With(Field.Cell, (2000, 0, 0))),
            "local remainder of one half" => One(ramp.With(Field.Local, (.5f, 0f, 0f))),
            "scaled rotation" => One(ramp.With(Field.Rotation, (0f, 0f, 0f, .5f))),
            "changed ball material" => One(FromProduction(WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1)).With(Field.Mass, 2f)),
            "unknown element" => One(ramp.Clone().Also(i => i.Part = "tennis")),
            "no element" => One(ramp.Clone().Also(i => i.Part = "")),
            "duplicate instance" => new([ramp, ramp], []),
            "dangling connection" => new([battery], [new(2, 77, electrical, (uint)WorkshopSocket.Supply, (uint)WorkshopSocket.PowerIn)]),
            "ramp to ramp electrical connection" => new([ramp, ramp.With(Field.Id, 2UL)],
                [new(1, 2, electrical, (uint)WorkshopSocket.Supply, (uint)WorkshopSocket.PowerIn)]),
            "allocator at an existing identity" => One(ramp) with { NextBodyId = 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    [Theory]
    [MemberData(nameof(Violations))]
    public void EveryViolationIsItsTypedRefusal(string violation, WorkshopSaveRejection reason) =>
        Assert.Equal(reason, Rejection(Bytes(Violation(violation))));

    [Fact]
    public void EveryReasonIsCovered()
    {
        var covered = ViolationRows.Select(row => row.Reason).ToHashSet();
        covered.UnionWith([WorkshopSaveRejection.NotBase64, WorkshopSaveRejection.Truncated, WorkshopSaveRejection.TrailingBytes,
            WorkshopSaveRejection.UnknownIdentifier, WorkshopSaveRejection.VerifierRejected]); // WireFaultTests and the base64 tests
        Assert.Equal(Enum.GetValues<WorkshopSaveRejection>().Where(reason => reason != WorkshopSaveRejection.None).ToHashSet(), covered);
    }

    [Fact]
    public void TheRefusalIsAPlayerRejectionNotAFault()
    {
        var error = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,Bytes(Violation("long ramp"))));
        Assert.True(WorkshopUiFailure.IsRejection(error));
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void RejectionNoneIsAProgrammerError() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkshopSaveRejectedException(WorkshopSaveRejection.None, "x"));

    // ---------- a defect after pre-validation is a fault ----------

    /// <summary>A duplicate identity reaching domain construction (a defect injected after pre-validation: the domain path
    /// runs without it) surfaces as the domain guard's own ArgumentException, never as the player's refusal.</summary>
    [Fact]
    public void DuplicateKeyDefectAfterPreValidationIsAFault()
    {
        var saved = WorkshopSaveCodec.Open(Bytes(Violation("duplicate instance")));
        var error = Record.Exception(() => WorkshopSaveCodec.Construct(saved));
        Assert.IsType<ArgumentException>(error);
        Assert.False(WorkshopUiFailure.IsRejection(error));
    }

    // ---------- completeness and parity ----------

    private static bool PreValidationRejects(byte[] bytes)
    {
        // Anything other than the typed refusal escaping here fails the test: pre-validation is complete.
        try { WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes); return false; }
        catch (WorkshopSaveRejectedException) { return true; }
    }

    /// <summary>Domain construction alone (no pre-validation): any rejection counts, since parity, not its type, is asserted.</summary>
    private static bool DomainRejects(byte[] bytes)
    {
        SavedConstruction saved;
        try { saved = WorkshopSaveCodec.Open(bytes); }
        catch (WorkshopSaveRejectedException) { return true; }
        return Record.Exception(() => WorkshopSaveCodec.Construct(saved)) is not null;
    }

    private static readonly float[] Specials =
    [
        0f, -0f, 1f, -1f, .5f, -.5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, -float.Epsilon,
        float.MaxValue, float.MinValue, 65504f, 65520f, -65520f, 1e-8f, .0009765625f, 1e6f,
    ];

    /// <summary>Probe values for one f32 lane: specials, a scaled sweep of the valid value, and a dense absolute sweep.</summary>
    private static IEnumerable<float> Probes(float valid)
    {
        foreach (var value in Specials) yield return value;
        foreach (var scale in new[] { .25f, .5f, .9f, .99f, 1.01f, 1.1f, 2f, 4f, 10f }) yield return valid * scale;
        for (var i = -40; i <= 40; i++) yield return i * .5f;
        for (var e = -12; e <= 12; e++) { yield return MathF.Pow(2, e); yield return -MathF.Pow(2, e); }
        foreach (var bound in new[] { .1f, .125f, .12f, .2f, .4f, 1.2f, 2f, 3f, 4f, 6f, 8f, 9f, 10f, 12f, 16f, 20f, 30f, 60f, 64f, 100f, 120f, 480f, 1200f, 3600f, 7200f, 1024f })
            foreach (var near in new[] { bound, MathF.BitDecrement(bound), MathF.BitIncrement(bound), bound * 1.0004f, bound * .9996f, -bound })
                yield return near;
    }

    /// <summary>Between a rejected and an admitted probe the domain verdict changes; bisection on the f32 order finds the exact
    /// boundary, and both of its sides are probed too (so a pre-validation bound that differs by one rounding step fails).</summary>
    private static IEnumerable<float> Boundaries(List<float> sorted, Func<float, bool> domainRejects)
    {
        for (var i = 1; i < sorted.Count; i++)
        {
            float low = sorted[i - 1], high = sorted[i];
            if (!float.IsFinite(low) || !float.IsFinite(high) || domainRejects(low) == domainRejects(high)) continue;
            var lowRejects = domainRejects(low);
            while (MathF.BitIncrement(low) < high)
            {
                var middle = Middle(low, high);
                if (middle <= low || middle >= high) break;
                if (domainRejects(middle) == lowRejects) low = middle; else high = middle;
            }
            yield return low; yield return high;
        }
    }

    private static float Middle(float low, float high)
    {
        // Midpoint in the f32 order (by ordered bit pattern), so the bisection terminates at adjacent floats.
        static long Order(float value) { var bits = BitConverter.SingleToInt32Bits(value); return bits < 0 ? -(long)(bits & 0x7FFFFFFF) : bits; }
        static float Value(long order) => order < 0 ? BitConverter.Int32BitsToSingle(unchecked((int)(-order | 0x80000000L))) : BitConverter.Int32BitsToSingle((int)order);
        return Value((Order(low) + Order(high)) / 2);
    }

    public static TheoryData<string> Kinds()
    {
        var data = new TheoryData<string>();
        foreach (var part in EveryKind().Select(instance => instance.PartId.Value).Distinct()) data.Add(part);
        return data;
    }

    /// <summary>Every f32 lane of every kind (parameters, local position and rotation), at every probe and at every domain
    /// boundary the probes bracket: pre-validation refuses exactly what domain construction rejects.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void PreValidationAdmitsExactlyWhatTheDomainAdmits(string part)
    {
        var baseline = FromProduction(EveryKind().Single(instance => instance.PartId.Value == part));
        Assert.False(PreValidationRejects(Bytes(One(baseline))), $"{part} baseline");
        var mismatches = new List<string>();
        foreach (var (field, value) in baseline.Values.ToArray())
        {
            var lanes = value switch { float => 1, (float, float, float) => 3, (float, float, float, float) => 4, _ => 0 };
            for (var lane = 0; lane < lanes; lane++)
            {
                byte[] With(float probe) => Bytes(One(baseline.With(field, Replace(value, lane, probe))));
                var sorted = Probes(Lane(value, lane)).Where(float.IsFinite).Distinct().Order().ToList();
                bool Domain(float probe) => DomainRejects(With(probe));
                foreach (var probe in Probes(Lane(value, lane)).Concat(Boundaries(sorted, Domain)))
                {
                    var bytes = With(probe);
                    if (PreValidationRejects(bytes) != DomainRejects(bytes))
                        mismatches.Add($"{part}.{field}[{lane}] = {probe:R} (0x{BitConverter.SingleToInt32Bits(probe):X8}): pre-validation {(PreValidationRejects(bytes) ? "refuses" : "admits")}, domain {(DomainRejects(bytes) ? "rejects" : "admits")}");
                }
            }
        }
        Assert.Empty(mismatches);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void IntegerLanesMatchTheDomain(string part)
    {
        var baseline = FromProduction(EveryKind().Single(instance => instance.PartId.Value == part));
        var mismatches = new List<string>();
        foreach (var cell in new[] { -1025, -1024, 0, 1024, 1025, int.MaxValue, int.MinValue })
            for (var axis = 0; axis < 3; axis++)
            {
                var c = ((int, int, int))baseline.Values[Field.Cell];
                var changed = axis switch { 0 => (cell, c.Item2, c.Item3), 1 => (c.Item1, cell, c.Item3), _ => (c.Item1, c.Item2, cell) };
                var bytes = Bytes(One(baseline.With(Field.Cell, changed)));
                if (PreValidationRejects(bytes) != DomainRejects(bytes)) mismatches.Add($"{part} cell[{axis}] = {cell}");
            }
        foreach (var id in new[] { 0UL, 1UL, 99UL, ulong.MaxValue })
        {
            var bytes = Bytes(One(baseline.With(Field.Id, id)) with { NextBodyId = id == ulong.MaxValue ? id : 100 });
            if (PreValidationRejects(bytes) != DomainRejects(bytes)) mismatches.Add($"{part} id = {id}");
        }
        if (baseline.Values.ContainsKey(Field.TargetCount))
            foreach (var count in new[] { 0u, 1u, 9u, 10u, int.MaxValue + 1u, uint.MaxValue })
            {
                var bytes = Bytes(One(baseline.With(Field.TargetCount, count)));
                if (PreValidationRejects(bytes) != DomainRejects(bytes)) mismatches.Add($"{part} target = {count}");
            }
        if (baseline.Values.ContainsKey(Field.Participation))
            foreach (var value in new[] { 0u, 1u, 2u, 99u })
            {
                var bytes = Bytes(One(baseline.With(Field.Participation, value)));
                if (PreValidationRejects(bytes) != DomainRejects(bytes)) mismatches.Add($"{part} participation = {value}");
            }
        if (baseline.Values.ContainsKey(Field.Enabled))
            foreach (var value in new byte[] { 0, 1, 2, 255 })
            {
                var bytes = Bytes(One(baseline.With(Field.Enabled, value)));
                if (PreValidationRejects(bytes) != DomainRejects(bytes)) mismatches.Add($"{part} enabled = {value}");
            }
        Assert.Empty(mismatches);
    }

    private static float Lane(object value, int lane) => value switch
    {
        float v => v,
        (float x, float y, float z) => lane switch { 0 => x, 1 => y, _ => z },
        (float x, float y, float z, float w) => lane switch { 0 => x, 1 => y, 2 => z, _ => w },
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static object Replace(object value, int lane, float probe) => value switch
    {
        float => probe,
        (float x, float y, float z) => lane switch { 0 => (probe, y, z), 1 => (x, probe, z), _ => (x, y, probe) },
        (float x, float y, float z, float w) => lane switch { 0 => (probe, y, z, w), 1 => (x, probe, z, w), 2 => (x, y, probe, w), _ => (x, y, z, probe) },
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    /// <summary>A save of every kind, with one typed electrical connection, damaged every way: every truncation, every single-bit
    /// flip, oversized and prefixed buffers and seeded random bytes. Each input either decodes or is the typed refusal; nothing else
    /// escapes.</summary>
    [Fact]
    public void FuzzedSavesAreRefusedOrDecodedAndNothingElse()
    {
        var instances = EveryKind().Select(FromProduction).ToArray();
        var save = new Save(instances, [new(9, 22, (uint)WorkshopConnectionDomain.Electrical, (uint)WorkshopSocket.Supply, (uint)WorkshopSocket.PowerIn)]);
        var valid = Bytes(save);
        Assert.Equal(instances.Length, WorkshopSaveCodec.Decode(SaveFixture.Elements,valid).Construction.Instances.Count);
        var random = new Random(9_2);
        var corpus = new List<byte[]>();
        for (var length = 0; length < valid.Length; length++) corpus.Add(valid[..length]);
        for (var bit = 0; bit < valid.Length * 8; bit++) { var flipped = (byte[])valid.Clone(); flipped[bit / 8] ^= (byte)(1 << (bit % 8)); corpus.Add(flipped); }
        foreach (var extra in new[] { 1, 64, 4096 })
        {
            var oversized = new byte[valid.Length + extra]; valid.CopyTo(oversized, 0); random.NextBytes(oversized.AsSpan(valid.Length)); corpus.Add(oversized);
            var prefixed = new byte[valid.Length + extra]; random.NextBytes(prefixed.AsSpan(0, extra)); valid.CopyTo(prefixed, extra); corpus.Add(prefixed);
        }
        for (var i = 0; i < 2000; i++) { var noise = new byte[random.Next(0, 512)]; random.NextBytes(noise); corpus.Add(noise); }
        var escaped = new List<string>();
        foreach (var bytes in corpus)
        {
            var error = Record.Exception(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
            if (error is not (null or WorkshopSaveRejectedException)) escaped.Add($"{error.GetType().Name}: {error.Message} at {error.StackTrace?.Split('\n')[0].Trim()}");
        }
        Assert.True(escaped.Count == 0, string.Join(Environment.NewLine, escaped.Distinct()));
    }
}

internal static class InstanceFixtureExtensions
{
    public static WorkshopSaveRejectionTests.Instance Also(this WorkshopSaveRejectionTests.Instance instance, Action<WorkshopSaveRejectionTests.Instance> change)
    { change(instance); return instance; }
}
