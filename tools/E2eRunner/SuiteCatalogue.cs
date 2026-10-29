using CuriousContraptions.Abstractions;

namespace CuriousContraptions.E2eRunner;

/// <summary>One member per tools/e2e/*.test.ts file. The file name is converted only by <see cref="SuiteCatalogue.FileName"/>
/// and <see cref="SuiteCatalogue.TryParsePath"/>, at the file-name boundary.</summary>
internal enum SuiteId
{
    Anim1a = 1, Anim1b, Anim1c, Cat002, Cat005, Cat014, Cat015a, Cat015aSidekick, Cat015b, Cat017, Cat020, Cat023a, Cat023b,
    Cat030, Cat037, Cat048a, Cat048b, Cat049, Cat050, Cat051, Cat052, Cat062, Cat062b, ClockMaster, EngineCore2a1, EngineCore2a2,
    EngineCore2a3, EngineCore2a4, EngineCore2b1, EngineCore2b2, EngineCore2b3, EngineCore2b4, EngineCore2c, PalettePlacement,
    Capacity9r, Cat051OwnSupply, Cat052TwoPlates, Cat052Lamp, Capacity9r5b, TubeSnapping, Cat050Joined, Pkg1, Pkg3, Arch9aCatalogue,
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
        SuiteId.Cat052TwoPlates => "cat-052-two-plates", SuiteId.Cat052Lamp => "cat-052-lamp", SuiteId.Capacity9r5b => "capacity-9r5b",
        SuiteId.TubeSnapping => "tube-snapping", SuiteId.Cat050Joined => "cat-050-joined", SuiteId.Pkg1 => "pkg-1",
        SuiteId.Pkg3 => "pkg-3", SuiteId.Arch9aCatalogue => "arch-9a-catalogue",
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
            null, Set(new PartId("ball"), new PartId("domino")), Set(DominoEffect), Set(Signal), Set(Wire, Levels, Present)),
        Serial(SuiteId.Cat017,
            [new(TimingReason.MeasuresWallTime, "Records each blade sample's wall-clock age (Date.now() - started).", 189),
             new(TimingReason.AssertsWallClockRate, "Asserts at least 20/30 distinct pose publications over a wall-clock window.", 196),
             Owner("Owner decision, 19 Oct 2026: the System Clock suite runs alone.", 1)],
            new PartId("clock"), Set(new PartId("ball"), new PartId("battery"), new PartId("clock"), new PartId("counter"), new PartId("powered_gate")), [],
            Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config)),
        Serial(SuiteId.Cat023b,
            [Owner("Owner decision, 19 Oct 2026: the 4000 ms negative windows of the domino cascade run alone.", 253)],
            new PartId("domino"), Set(new PartId("domino"), new PartId("lamp")), Set(DominoEffect), Set(Signal),
            Set(Gizmo, Guide, Wire, Levels, Present)),
        Serial(SuiteId.Cat037,
            [new(TimingReason.MeasuresWallTime, "Stamps samples with Date.now() - started and asserts s.t < 2200 and s.t < 400.", 180)],
            new PartId("latch"), Set(new PartId("ball"), new PartId("battery"), new PartId("delay"), new PartId("lamp"), new PartId("latch"), new PartId("powered_gate"),
                new PartId("switch")), [], Set(Signal, Electrical), Set(Gizmo, Wire, Config, Present)),
        Serial(SuiteId.Cat014,
            [new(TimingReason.MeasuresWallTime, "Paces the rest window by elapsed wall time (Date.now() - runStarted).", 159)],
            new PartId("bowling"), Set(new PartId("ball"), new PartId("bowling"), new PartId("domino"), new PartId("basket")), [], [],
            Set(Gizmo, Levels, Present)),
        Serial(SuiteId.Anim1a,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 15 animation samples in 500 ms and >= 35 in 1000 ms of pause.", 154)],
            null, Set(new PartId("ramp")), Set(FirstPrinciples), [], Set(Gizmo, Levels, Present)),
        Serial(SuiteId.Anim1b,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 10 Delay progress samples over a one-second wall window.", 100)],
            null, Set(new PartId("ball"), new PartId("bumper"), new PartId("delay")), Set(DelayedSignal), Set(Signal),
            Set(Gizmo, Wire, Levels, Present)),
        Serial(SuiteId.Anim1c,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 3 samples of one 0.5 s ramp target.", 83)],
            null, Set(new PartId("ball"), new PartId("delay"), new PartId("ramp"), new PartId("basket")), Set(DelayedSignal, FirstPrinciples), Set(Signal),
            Set(Gizmo, Guide, Wire, Levels, Present)),
        Serial(SuiteId.Cat023a,
            [new(TimingReason.AssertsWallClockRate, "Asserts >= 10 pose reads during one settled wall-clock second.", 133)],
            new PartId("domino"), Set(new PartId("ball"), new PartId("domino")), [], [], Set(Gizmo)),
        Serial(SuiteId.Cat020,
            [Owner("Coordinator decision, 20 Oct 2026: recalibrated a clock at boot under N=2 AOT workers " +
                "(evidence-9r-1/full-run-parallel); Parallel is unearned until Story F.1 recalibrates.", 1)],
            new PartId("counter"), Set(new PartId("ball_detector"), new PartId("ball"), new PartId("battery"), new PartId("counter"),
            new PartId("lamp"), new PartId("powered_gate")), [], Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config, Present)),
        Serial(SuiteId.Capacity9r,
            [Owner("Coordinator decision, 20 Oct 2026: a new suite starts SerialAlone until calibration qualifies it; the heaviest " +
                "collider scene (2,305 colliders) debt-faulted before Story 9R.2's broadphase fix.", 1)],
            null, Set(new PartId("ball"), new PartId("pipe_bend_90")), [], [], Set(Gizmo, Guide)),
        Serial(SuiteId.Cat062,
            [Owner("Coordinator decision, 20 Oct 2026: test 4 timed out with no failed assertion on a Parallel worker under N=2 AOT " +
                "workers (evidence-9r-3/full-run-1); Parallel is unearned until Story F.1 recalibrates.", 1)],
            new PartId("spring"), Set(new PartId("ball"), new PartId("spring")), [], [], Set(Gizmo, Guide, Config)),
        Serial(SuiteId.Cat051OwnSupply,
            [Owner("Coordinator decision, 20 Oct 2026 (Murdoch-9, Story 9R.3): a new suite starts SerialAlone until calibration " +
                "qualifies it; it samples blade and ball poses over wall-clock windows after each in-run supply toggle.", 1)],
            // No primary: cat-051 stays the Powered Gate's primary suite; a gate change still selects this suite as a placer.
            null, Set(new PartId("ball"), new PartId("battery"), new PartId("powered_gate")), [], Set(Electrical),
            Set(Gizmo, Guide, Wire, Config)),
        Serial(SuiteId.Cat052TwoPlates,
            [Owner("Coordinator decision, 20 Oct 2026 (Story 9R.4): a new suite starts SerialAlone until calibration qualifies " +
                "it; it samples both blades' poses over wall-clock windows after each Run.", 1)],
            // No primary: cat-052 stays the Pressure Plate's primary suite; a plate change still selects this suite as a placer.
            null, Set(new PartId("battery"), new PartId("bowling"), new PartId("powered_gate"), new PartId("pressure_plate"), new PartId("counter"), new PartId("clock"),
                new PartId("latch")), [], Set(Electrical),
            Set(Gizmo, Wire)),
        Serial(SuiteId.Cat052Lamp,
            [Owner("Coordinator decision, 20 Oct 2026 (Story 9R.5): a new suite starts SerialAlone until calibration qualifies " +
                "it; it samples blade and ball poses and the lamp's cosmetic over wall-clock windows after each Run.", 1)],
            // No primary: cat-052 stays the Pressure Plate's primary suite; a plate or lamp change still selects this suite as a placer.
            null, Set(new PartId("ball"), new PartId("battery"), new PartId("bowling"), new PartId("counter"), new PartId("delay"), new PartId("domino"), new PartId("latch"),
                new PartId("powered_gate"), new PartId("pressure_plate"), new PartId("lamp")), [], Set(Signal, Electrical),
            Set(Gizmo, Guide, Wire, Present)),
        Serial(SuiteId.Capacity9r5b,
            [Owner("Coordinator decision, 20 Oct 2026 (Story 9R.5b): a new suite starts SerialAlone until calibration qualifies " +
                "it; it builds the largest machine in the catalogue (42 parts, 40 triggers) and samples poses over wall-clock windows.", 1)],
            // No primary: the Impact Switch and Signal lamp keep their own primary suites; a change to either still selects this placer.
            null, Set(new PartId("ball"), new PartId("switch"), new PartId("lamp")), [], Set(Signal), Set(Gizmo, Wire)),
        Serial(SuiteId.TubeSnapping,
            [Owner("Coordinator decision, 20 Oct 2026 (Story 9R.6): a new suite starts SerialAlone until calibration qualifies " +
                "it; it samples the ball's pose publications over wall-clock windows through four snapped tube joints.", 1)],
            // No primary: cat-048a stays the Pipe's primary suite; a tube or snapping change still selects this suite as a placer.
            null, Set(new PartId("ball"), new PartId("pipe"), new PartId("pipe_bend_45"), new PartId("pipe_bend_90"), new PartId("funnel")), Set(new LevelId("clear_pipe")),
            [], Set(Gizmo, Guide, Levels, Bench)),
        Serial(SuiteId.Cat050Joined,
            [Owner("Coordinator decision, 21 Oct 2026 (Story 9R.7): a new suite starts SerialAlone until calibration qualifies " +
                "it; it samples the ball's pose publications over wall-clock windows through the joined pipe and bend.", 1)],
            // No primary: cat-048a stays the Pipe's and cat-050 the 90° bend's primary suite; either change still selects this placer.
            null, Set(new PartId("pipe")), Set(new LevelId("joined_pipe")), [], Set(Gizmo, Guide, Levels)),
        Serial(SuiteId.Pkg1,
            [Owner("Coordinator decision, 21 Oct 2026 (PKG-1): a new suite starts SerialAlone until calibration qualifies it; it holds " +
                "a 1.5 s wall-clock Run window before each exact Reset check.", 1)],
            // No primary: cat-014 stays the Bowling ball's and cat-062 the Springboard's primary suite, and the Receiver's primary is
            // Story 9R.12's cat-004; a change to any of the three still selects this placer.
            null, Set(new PartId("bowling"), new PartId("basket"), new PartId("spring")), [], [], Set(Pal, Bench, Gizmo)),
        Serial(SuiteId.Pkg3,
            [Owner("Coordinator decision, 21 Oct 2026 (PKG-3): a new suite starts SerialAlone until calibration qualifies it; it holds " +
                "a 3.5 s wall-clock Run window (the drop and the Delay's 1 s duration) before each exact Reset check.", 1)],
            // No primary: the Delay's primary is Story 9R.17's cat-022 (with 9.5a) and the Impact switch's is Story 9R.15's cat-063
            // (with C-Switch); a change to either still selects this placer.
            null, Set(new PartId("ball"), new PartId("delay"), new PartId("switch"), new PartId("lamp")), [], Set(Signal), Set(Pal, Bench, Gizmo, Wire)),
        Serial(SuiteId.Arch9aCatalogue,
            [Owner("Coordinator decision, 21 Oct 2026 (Story 9A.14): a new suite starts SerialAlone until calibration qualifies it; it " +
                "holds a 1.5 s wall-clock Run window on a bench of every element before each exact Reset check, and compares palette " +
                "screenshots byte for byte.", 1)],
            // No primary: the suite places every bundle element; each element keeps its own primary suite.
            null, Set(new PartId("ball"), new PartId("basket"), new PartId("switch"), new PartId("lamp"), new PartId("wall"), new PartId("delay"),
                new PartId("bumper"), new PartId("ramp"), new PartId("domino"), new PartId("bowling"), new PartId("battery"), new PartId("spring"), new PartId("pipe"),
                new PartId("ball_detector"), new PartId("pipe_bend_45"), new PartId("pipe_bend_90"), new PartId("funnel"), new PartId("powered_gate"), new PartId("pressure_plate"),
                new PartId("counter"), new PartId("clock"), new PartId("latch")), [], [], Set(Pal, Bench)),

        Parallel(SuiteId.Cat002, new PartId("ball_detector"), Set(new PartId("ball_detector"), new PartId("ball"), new PartId("lamp")), [], Set(Signal),
            Set(Gizmo, Guide, Wire, Present)),
        Parallel(SuiteId.Cat005, new PartId("battery"), Set(new PartId("battery"), new PartId("bowling"), new PartId("bumper")), [], Set(Electrical),
            Set(Gizmo, Wire, Config, Present)),
        Parallel(SuiteId.Cat015a, new PartId("bumper"), Set(new PartId("ball"), new PartId("bowling"), new PartId("bumper")), [], [],
            Set(Gizmo, Present)),
        Parallel(SuiteId.Cat015aSidekick, new PartId("bumper"), Set(new PartId("bumper")), Set(new LevelId("bumper_sidekick")), [],
            Set(Gizmo, Levels, Present)),
        Parallel(SuiteId.Cat015b, new PartId("bumper"), Set(new PartId("bumper"), new PartId("wall")),
            Set(new LevelId("bumper_depth"), new LevelId("wall_and_bumper")), [], Set(Gizmo, Levels, Present)),
        Parallel(SuiteId.Cat030, new PartId("funnel"), Set(new PartId("ball"), new PartId("funnel"), new PartId("pipe"), new PartId("basket")), [], [],
            Set(Gizmo, Guide)),
        Parallel(SuiteId.Cat048a, new PartId("pipe"), Set(new PartId("ball"), new PartId("pipe")), [], [], Set(Gizmo, Guide)),
        Parallel(SuiteId.Cat048b, new PartId("pipe"), Set(new PartId("pipe")), Set(new LevelId("clear_pipe")), [], Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat049, new PartId("pipe_bend_45"), Set(new PartId("pipe_bend_45")), Set(new LevelId("gentle_bend")), [],
            Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat050, new PartId("pipe_bend_90"), Set(new PartId("pipe_bend_90")), Set(new LevelId("quarter_bend")), [],
            Set(Gizmo, Guide, Levels)),
        Parallel(SuiteId.Cat051, new PartId("powered_gate"), Set(new PartId("ball"), new PartId("battery"), new PartId("powered_gate")), [], Set(Electrical),
            Set(Gizmo, Guide, Wire)),
        Parallel(SuiteId.Cat052, new PartId("pressure_plate"), Set(new PartId("ball"), new PartId("battery"), new PartId("bowling"), new PartId("domino"),
            new PartId("powered_gate"), new PartId("pressure_plate")), [], Set(Signal, Electrical), Set(Gizmo, Guide, Wire, Config)),
        Parallel(SuiteId.Cat062b, new PartId("spring"), Set(new PartId("spring"), new PartId("basket")),
            Set(new LevelId("spring_forward"), new LevelId("ready_to_rebound")), [], Set(Gizmo, Guide, Config, Levels)),
        Parallel(SuiteId.EngineCore2a1, null, Set(new PartId("ball")), [], [], [], smoke: true),
        Parallel(SuiteId.EngineCore2a2, null, Set(new PartId("ball")), [], [], []),
        Parallel(SuiteId.EngineCore2a3, null, Set(new PartId("ball"), new PartId("ramp"), new PartId("wall")), Set(FirstPrinciples), [],
            Set(Gizmo, Levels)),
        Parallel(SuiteId.EngineCore2a4, null, Set(new PartId("ball"), new PartId("wall")), [], [], Set(Gizmo)),
        Parallel(SuiteId.EngineCore2b1, null, Set(new PartId("ball")), [], [], [], smoke: true),
        Parallel(SuiteId.EngineCore2b2, null, Set(new PartId("ball")), [], [], []),
        Parallel(SuiteId.EngineCore2b3, null, Set(new PartId("ball"), new PartId("wall")), [], [], Set(Gizmo)),
        Parallel(SuiteId.EngineCore2b4, null, Set(new PartId("ramp")), Set(FirstPrinciples), [], Set(Gizmo, Levels)),
        Parallel(SuiteId.EngineCore2c, null, Set(new PartId("ramp")), Set(FirstPrinciples), [], Set(Gizmo, Levels)),
        Parallel(SuiteId.PalettePlacement, null, None, [], [], Set(Pal, Bench), scope: PlacementScope.EveryPalettePart),
    ];

    private static readonly Dictionary<SuiteId, SuiteDeclaration> ById = All.ToDictionary(declaration => declaration.Id);

    public static SuiteDeclaration Get(SuiteId id) =>
        ById.TryGetValue(id, out var declaration) ? declaration : throw new KeyNotFoundException($"Suite {id} is not declared.");
}
