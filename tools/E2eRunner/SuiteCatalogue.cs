using CuriousContraptions.Abstractions;

namespace CuriousContraptions.E2eRunner;

/// <summary>One member per tools/e2e/*.test.ts file. The file name is converted only by <see cref="SuiteCatalogue.FileName"/>
/// and <see cref="SuiteCatalogue.TryParsePath"/>, at the file-name boundary.</summary>
internal enum SuiteId
{
    Anim1a = 1, Anim1b, Anim1c, Cat002, Cat005, Cat014, Cat015a, Cat015aSidekick, Cat015b, Cat017, Cat020, Cat023a, Cat023b,
    Cat030, Cat037, Cat048a, Cat048b, Cat049, Cat050, Cat051, Cat052, Cat062, Cat062b, ClockMaster, EngineCore2a1, EngineCore2a2,
    EngineCore2a3, EngineCore2a4, EngineCore2b1, EngineCore2b2, EngineCore2b3, EngineCore2b4, EngineCore2c, PalettePlacement,
    Capacity9r, Cat051OwnSupply, Cat052TwoPlates,
}

/// <summary>Parallel suites share the machine with N-1 other Chromes; SerialAlone suites run with no other runner Chrome or build.</summary>
internal enum SuiteTiming { Parallel = 1, SerialAlone = 2 }

/// <summary>Why a suite is SerialAlone (spec D4, criteria T1-T6).</summary>
internal enum TimingReason { InjectsStall = 1, InjectsLoad, TampersClock, MeasuresWallTime, AssertsWallClockRate, OwnerNamed }

/// <summary>A declared timing reason with its justification and the suite-file line it cites.</summary>
internal sealed record TimingJustification(TimingReason Reason, string Justification, int Line);

/// <summary>Declared: the suite places exactly <see cref="SuiteDeclaration.Places"/>. EveryPalettePart: it places every palette part.</summary>
internal enum PlacementScope { Declared = 1, EveryPalettePart }

/// <summary>The Workshop interaction areas a non-engine engine/, ui/ or scenes/ file belongs to (spec OD6). AllInteraction files are
/// on every interaction's path and select the full run.</summary>
internal enum WorkshopArea { Palette = 1, BenchPlacement, Gizmo, Guidance, Connections, Configuration, LevelPicker, Presentation, AllInteraction }

/// <summary>An authored level id (content/puzzles.json) or the Free workshop.</summary>
internal readonly record struct LevelId
{
    public string Value { get; }

    public LevelId(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '_'))
            throw new ArgumentException($"A level id is lower-case ASCII letters, digits and '_': '{value}'.", nameof(value));
        Value = value;
    }

    public static readonly LevelId FreeWorkshop = new("free_workshop");
    public override string ToString() => Value;
}

/// <summary>One suite file's typed declaration.</summary>
internal sealed record SuiteDeclaration(
    SuiteId Id,
    SuiteTiming Timing,
    IReadOnlyList<TimingJustification> Reasons,
    PartId? Primary,
    IReadOnlySet<PartId> Places,
    PlacementScope Scope,
    IReadOnlySet<LevelId> Levels,
    IReadOnlySet<SimulationDomain> Wires,
    IReadOnlySet<WorkshopArea> Areas,
    bool Smoke);

internal static class SuiteCatalogue
{
    public const string SuiteDirectory = "tools/e2e";
    private const string SuiteSuffix = ".test.ts";

    public static string FileName(SuiteId id) => id switch
    {
        SuiteId.Anim1a => "anim-1a", SuiteId.Anim1b => "anim-1b", SuiteId.Anim1c => "anim-1c",
        SuiteId.Cat002 => "cat-002", SuiteId.Cat005 => "cat-005", SuiteId.Cat014 => "cat-014",
        SuiteId.Cat015a => "cat-015a", SuiteId.Cat015aSidekick => "cat-015a-sidekick", SuiteId.Cat015b => "cat-015b",
        SuiteId.Cat017 => "cat-017", SuiteId.Cat020 => "cat-020", SuiteId.Cat023a => "cat-023a", SuiteId.Cat023b => "cat-023b",
        SuiteId.Cat030 => "cat-030", SuiteId.Cat037 => "cat-037", SuiteId.Cat048a => "cat-048a", SuiteId.Cat048b => "cat-048b",
        SuiteId.Cat049 => "cat-049", SuiteId.Cat050 => "cat-050", SuiteId.Cat051 => "cat-051", SuiteId.Cat052 => "cat-052",
        SuiteId.Cat062 => "cat-062", SuiteId.Cat062b => "cat-062b", SuiteId.ClockMaster => "clock-master",
        SuiteId.EngineCore2a1 => "engine-core-2a1", SuiteId.EngineCore2a2 => "engine-core-2a2",
        SuiteId.EngineCore2a3 => "engine-core-2a3", SuiteId.EngineCore2a4 => "engine-core-2a4",
        SuiteId.EngineCore2b1 => "engine-core-2b1", SuiteId.EngineCore2b2 => "engine-core-2b2",
        SuiteId.EngineCore2b3 => "engine-core-2b3", SuiteId.EngineCore2b4 => "engine-core-2b4",
        SuiteId.EngineCore2c => "engine-core-2c", SuiteId.PalettePlacement => "palette-placement",
        SuiteId.Capacity9r => "capacity-9r", SuiteId.Cat051OwnSupply => "cat-051-own-supply",
        SuiteId.Cat052TwoPlates => "cat-052-two-plates",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Undefined suite."),
    };

    /// <summary>The repository-relative path of a suite file.</summary>
    public static string RelativePath(SuiteId id) => $"{SuiteDirectory}/{FileName(id)}{SuiteSuffix}";

    /// <summary>The file-name boundary: a repository-relative tools/e2e/&lt;name&gt;.test.ts path to its suite, or null.</summary>
    public static SuiteId? TryParsePath(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        foreach (var id in Enum.GetValues<SuiteId>())
            if (string.Equals(RelativePath(id), relativePath, StringComparison.Ordinal)) return id;
        return null;
    }

    /// <summary>Whether a repository-relative path is a top-level Chrome suite file (tools/e2e/*.test.ts).</summary>
    public static bool IsSuitePath(string relativePath) =>
        relativePath.StartsWith(SuiteDirectory + "/", StringComparison.Ordinal) &&
        relativePath.EndsWith(SuiteSuffix, StringComparison.Ordinal) &&
        relativePath.IndexOf('/', SuiteDirectory.Length + 1) < 0;

    /// <summary>The smoke pair every Tier 2 selection includes (boot, place, Run, Reset, Save/Load).</summary>
    public static readonly IReadOnlyList<SuiteId> SmokePair = [SuiteId.EngineCore2b1, SuiteId.EngineCore2a1];

    private static readonly HashSet<PartId> None = [];

    private static HashSet<T> Set<T>(params T[] values) => [.. values];
    private static TimingJustification Owner(string why, int line) => new(TimingReason.OwnerNamed, why, line);

    private static SuiteDeclaration Parallel(SuiteId id, PartId? primary, HashSet<PartId> places, HashSet<LevelId> levels,
        HashSet<SimulationDomain> wires, HashSet<WorkshopArea> areas, bool smoke = false,
        PlacementScope scope = PlacementScope.Declared) =>
        new(id, SuiteTiming.Parallel, [], primary, places, scope, levels, wires, areas, smoke);

    private static SuiteDeclaration Serial(SuiteId id, TimingJustification[] reasons, PartId? primary, HashSet<PartId> places,
        HashSet<LevelId> levels, HashSet<SimulationDomain> wires, HashSet<WorkshopArea> areas) =>
        new(id, SuiteTiming.SerialAlone, reasons, primary, places, PlacementScope.Declared, levels, wires, areas, false);

    private static readonly LevelId FirstPrinciples = new("first_principles");
    private static readonly LevelId DelayedSignal = new("delayed_signal");
    private static readonly LevelId DominoEffect = new("domino_effect");

    private const WorkshopArea Pal = WorkshopArea.Palette, Bench = WorkshopArea.BenchPlacement, Gizmo = WorkshopArea.Gizmo,
        Guide = WorkshopArea.Guidance, Wire = WorkshopArea.Connections, Config = WorkshopArea.Configuration,
        Levels = WorkshopArea.LevelPicker, Present = WorkshopArea.Presentation;
    private const SimulationDomain Signal = SimulationDomain.Signal, Electrical = SimulationDomain.Electrical;

    /// <summary>Every suite, in catalogue order (Phase S runs SerialAlone suites in this order).</summary>
    public static readonly IReadOnlyList<SuiteDeclaration> All =
    [
        Serial(SuiteId.ClockMaster,
            [new(TimingReason.InjectsStall, "Busy-waits the Simulation worker (20 ms duty cycle, 600 ms and 4000 ms blocks).", 41),
             new(TimingReason.TampersClock, "Reassigns performance.now in the Browser and the Simulation worker.", 265),
             new(TimingReason.MeasuresWallTime, "Times each failure from the injection (Date.now() - injectedAt) and bounds it.", 91),
             Owner("Owner decision, 19 Oct 2026: the off-thread clock master's injected-load regression runs alone.", 1)],
            null, Set(PartId.Basketball, PartId.Domino), Set(DominoEffect), Set(Signal), Set(Wire, Levels, Present)),
        Serial(SuiteId.Cat017,
            [new(TimingReason.MeasuresWallTime, "Records each blade sample's wall-clock age (Date.now() - started).", 189),
             new(TimingReason.AssertsWallClockRate, "Asserts at least 20/30 distinct pose publications over a wall-clock window.", 196),
             Owner("Owner decision, 19 Oct 2026: the System Clock suite runs alone.", 1)],
            PartId.Clock, Set(PartId.Basketball, PartId.Battery, PartId.Clock, PartId.Counter, PartId.PoweredGate), [],
            Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config)),
        Serial(SuiteId.Cat023b,
            [Owner("Owner decision, 19 Oct 2026: the 4000 ms negative windows of the domino cascade run alone.", 253)],
            PartId.Domino, Set(PartId.Domino, PartId.SignalLamp), Set(DominoEffect), Set(Signal),
            Set(Gizmo, Guide, Wire, Levels, Present)),
        Serial(SuiteId.Cat037,
            [new(TimingReason.MeasuresWallTime, "Stamps samples with Date.now() - started and asserts s.t < 2200 and s.t < 400.", 180)],
            PartId.Latch, Set(PartId.Basketball, PartId.Battery, PartId.Delay, PartId.SignalLamp, PartId.Latch, PartId.PoweredGate,
                PartId.ImpactSwitch), [], Set(Signal, Electrical), Set(Gizmo, Wire, Config, Present)),
        Serial(SuiteId.Cat014,
            [new(TimingReason.MeasuresWallTime, "Paces the rest window by elapsed wall time (Date.now() - runStarted).", 159)],
            PartId.BowlingBall, Set(PartId.Basketball, PartId.BowlingBall, PartId.Domino, PartId.Receiver), [], [],
            Set(Gizmo, Levels, Present)),
        Serial(SuiteId.Anim1a,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 15 animation samples in 500 ms and >= 35 in 1000 ms of pause.", 154)],
            null, Set(PartId.Ramp), Set(FirstPrinciples), [], Set(Gizmo, Levels, Present)),
        Serial(SuiteId.Anim1b,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 10 Delay progress samples over a one-second wall window.", 100)],
            null, Set(PartId.Basketball, PartId.PinballBumper, PartId.Delay), Set(DelayedSignal), Set(Signal),
            Set(Gizmo, Wire, Levels, Present)),
        Serial(SuiteId.Anim1c,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 3 samples of one 0.5 s ramp target.", 83)],
            null, Set(PartId.Basketball, PartId.Delay, PartId.Ramp, PartId.Receiver), Set(DelayedSignal, FirstPrinciples), Set(Signal),
            Set(Gizmo, Guide, Wire, Levels, Present)),
        Serial(SuiteId.Cat023a,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 10 pose reads during one settled wall-clock second.", 133)],
            PartId.Domino, Set(PartId.Basketball, PartId.Domino), [], [], Set(Gizmo)),
        Serial(SuiteId.Cat020,
            [Owner("Coordinator decision, 20 Oct 2026: recalibrated a clock at boot under N=2 AOT workers " +
                "(evidence-9r-1/full-run-parallel); Parallel is unearned until Story F.1 recalibrates.", 1)],
            PartId.Counter, Set(PartId.BallDetector, PartId.Basketball, PartId.Battery, PartId.Counter,
            PartId.SignalLamp, PartId.PoweredGate), [], Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config, Present)),
        Serial(SuiteId.Capacity9r,
            [Owner("Coordinator decision, 20 Oct 2026: a new suite starts SerialAlone until calibration qualifies it; the heaviest " +
                "collider scene (2,305 colliders) debt-faulted before Story 9R.2's broadphase fix.", 1)],
            null, Set(PartId.Basketball, PartId.PipeBend90), [], [], Set(Gizmo, Guide)),
        Serial(SuiteId.Cat062,
            [Owner("Coordinator decision, 20 Oct 2026: test 4 timed out with no failed assertion on a Parallel worker under N=2 AOT " +
                "workers (evidence-9r-3/full-run-1); Parallel is unearned until Story F.1 recalibrates.", 1)],
            PartId.Springboard, Set(PartId.Basketball, PartId.Springboard), [], [], Set(Gizmo, Guide, Config)),
        Serial(SuiteId.Cat051OwnSupply,
            [Owner("Coordinator decision, 20 Oct 2026 (Murdoch-9, Story 9R.3): a new suite starts SerialAlone until calibration " +
                "qualifies it; it samples blade and ball poses over wall-clock windows after each in-run supply toggle.", 1)],
            // No primary: cat-051 stays the Powered Gate's primary suite; a gate change still selects this suite as a placer.
            null, Set(PartId.Basketball, PartId.Battery, PartId.PoweredGate), [], Set(Electrical),
            Set(Gizmo, Guide, Wire, Config)),
        Serial(SuiteId.Cat052TwoPlates,
            [Owner("Coordinator decision, 20 Oct 2026 (Story 9R.4): a new suite starts SerialAlone until calibration qualifies " +
                "it; it samples both blades' poses over wall-clock windows after each Run.", 1)],
            // No primary: cat-052 stays the Pressure Plate's primary suite; a plate change still selects this suite as a placer.
            null, Set(PartId.Battery, PartId.BowlingBall, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock,
                PartId.Latch), [], Set(Electrical),
            Set(Gizmo, Wire)),

        Parallel(SuiteId.Cat002, PartId.BallDetector, Set(PartId.BallDetector, PartId.Basketball, PartId.SignalLamp), [], Set(Signal),
            Set(Gizmo, Guide, Wire, Present)),
        Parallel(SuiteId.Cat005, PartId.Battery, Set(PartId.Battery, PartId.BowlingBall, PartId.PinballBumper), [], Set(Electrical),
            Set(Gizmo, Wire, Config, Present)),
        Parallel(SuiteId.Cat015a, PartId.PinballBumper, Set(PartId.Basketball, PartId.BowlingBall, PartId.PinballBumper), [], [],
            Set(Gizmo, Present)),
        Parallel(SuiteId.Cat015aSidekick, PartId.PinballBumper, Set(PartId.PinballBumper), Set(new LevelId("bumper_sidekick")), [],
            Set(Gizmo, Levels, Present)),
        Parallel(SuiteId.Cat015b, PartId.PinballBumper, Set(PartId.PinballBumper, PartId.Wall),
            Set(new LevelId("bumper_depth"), new LevelId("wall_and_bumper")), [], Set(Gizmo, Levels, Present)),
        Parallel(SuiteId.Cat030, PartId.Funnel, Set(PartId.Basketball, PartId.Funnel, PartId.Pipe, PartId.Receiver), [], [],
            Set(Gizmo, Guide)),
        Parallel(SuiteId.Cat048a, PartId.Pipe, Set(PartId.Basketball, PartId.Pipe), [], [], Set(Gizmo, Guide)),
        Parallel(SuiteId.Cat048b, PartId.Pipe, Set(PartId.Pipe), Set(new LevelId("clear_pipe")), [], Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat049, PartId.PipeBend45, Set(PartId.PipeBend45), Set(new LevelId("gentle_bend")), [],
            Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat050, PartId.PipeBend90, Set(PartId.PipeBend90), Set(new LevelId("quarter_bend")), [],
            Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat051, PartId.PoweredGate, Set(PartId.Basketball, PartId.Battery, PartId.PoweredGate), [], Set(Electrical),
            Set(Gizmo, Guide, Wire)),
        Parallel(SuiteId.Cat052, PartId.PressurePlate, Set(PartId.Basketball, PartId.Battery, PartId.BowlingBall, PartId.Domino,
            PartId.PoweredGate, PartId.PressurePlate), [], Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config)),
        Parallel(SuiteId.Cat062b, PartId.Springboard, Set(PartId.Springboard, PartId.Receiver),
            Set(new LevelId("spring_forward"), new LevelId("ready_to_rebound")), [], Set(Gizmo, Guide, Config, Levels)),
        Parallel(SuiteId.EngineCore2a1, null, Set(PartId.Basketball), [], [], [], smoke: true),
        Parallel(SuiteId.EngineCore2a2, null, Set(PartId.Basketball), [], [], []),
        Parallel(SuiteId.EngineCore2a3, null, Set(PartId.Basketball, PartId.Ramp, PartId.Wall), Set(FirstPrinciples), [],
            Set(Gizmo, Levels)),
        Parallel(SuiteId.EngineCore2a4, null, Set(PartId.Basketball, PartId.Wall), [], [], Set(Gizmo)),
        Parallel(SuiteId.EngineCore2b1, null, Set(PartId.Basketball), [], [], [], smoke: true),
        Parallel(SuiteId.EngineCore2b2, null, Set(PartId.Basketball), [], [], []),
        Parallel(SuiteId.EngineCore2b3, null, Set(PartId.Basketball, PartId.Wall), [], [], Set(Gizmo)),
        Parallel(SuiteId.EngineCore2b4, null, Set(PartId.Ramp), Set(FirstPrinciples), [], Set(Gizmo, Levels)),
        Parallel(SuiteId.EngineCore2c, null, Set(PartId.Ramp), Set(FirstPrinciples), [], Set(Gizmo, Levels)),
        Parallel(SuiteId.PalettePlacement, null, None, [], [], Set(Pal, Bench), scope: PlacementScope.EveryPalettePart),
    ];

    private static readonly Dictionary<SuiteId, SuiteDeclaration> ById = All.ToDictionary(declaration => declaration.Id);

    public static SuiteDeclaration Get(SuiteId id) =>
        ById.TryGetValue(id, out var declaration) ? declaration : throw new KeyNotFoundException($"Suite {id} is not declared.");
}
