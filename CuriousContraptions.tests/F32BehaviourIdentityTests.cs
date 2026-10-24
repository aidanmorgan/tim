using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44 behaviour identity against the D5 outcome baseline: Fixtures/outcomes-f1ad3bd1.json was captured natively on
/// the half-precision-era engine at f1ad3bd1 before any 9A.44 edit. Every authored lesson at precisions 0, 0.45 and 1, and every element
/// fixture placed alone, runs on the production simulation over the worker transport. Goal met equals the baseline's and the order of
/// event kinds is identical; every event (and a lesson's goal) happens within <see cref="GoalTickTolerance"/> ticks (0.1 s) of its
/// baseline counterpart, its ordinals within the same span of substeps, and an element placed alone ends within 2 mm of its baseline
/// pose. The deltas come from the exact f32 values that half precision rounded (g 9.81 for 9.8125, ball radius 0.34 for 0.340088,
/// drag 0.04 for 0.040009, precision 0.45 for 0.449951, capture speed 2.324 for 2.32421875); each one is listed and explained in
/// evidence-9a-44/t1/outcome-comparison.txt and outcome-deltas-notes.txt.</summary>
public sealed partial class F32BehaviourIdentityTests
{
    private const int LessonTicks = 1200;
    private const int ElementTicks = 240;
    private const int GoalTickTolerance = 12;
    private const double ElementPoseTolerance = 2e-3;
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    private static readonly Lazy<JsonObject> Baseline = new(() => JsonNode.Parse(File.ReadAllText(Path.Combine(
        twodog.Engine.ResolveProjectDir(), "CuriousContraptions.tests/Fixtures/outcomes-f1ad3bd1.json")))!.AsObject());

    public static TheoryData<string, float> Lessons()
    {
        var data = new TheoryData<string, float>();
        foreach (var row in LaunchLessonSolveTests.AuthoredLessons())
            foreach (var precision in new[] { 0f, .45f, 1f })
                data.Add(row.Data, precision);
        return data;
    }

    public static TheoryData<string> Elements()
    {
        var data = new TheoryData<string>();
        foreach (var row in Baseline.Value["Elements"]!.AsArray()) data.Add((string)row!["Id"]!);
        return data;
    }

    [Fact]
    public void TheBaselineCoversEveryAuthoredLessonAtThreePrecisions() =>
        Assert.Equal(Lessons().Count, Baseline.Value["Lessons"]!.AsArray().Count);

    [Theory]
    [MemberData(nameof(Lessons))]
    public async Task ALessonMeetsItsGoalWithTheBaselineEventOrderNearItsBaselineTick(string lesson, float precision)
    {
        // The baseline admitted 0.45 as its half-precision value 0.44995117: the same authored precision within one half-precision step.
        var expected = Baseline.Value["Lessons"]!.AsArray().Single(row =>
            (string)row!["Id"]! == lesson && Math.Abs((double)row["Precision"]! - precision) <= 1.0 / 2048)!;
        var bench = LaunchLessonSolveTests.AuthoredSolution(lesson, precision);
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, LessonTicks);
        var (goalTick, events, _) = Outcome(run, bench);

        var baselineEvents = expected["Events"]!.AsArray().Select(e => (string)e!).ToArray();
        Assert.Equal(Kinds(baselineEvents), Kinds(events));
        AssertTimingsWithinTolerance(baselineEvents, events);
        Assert.Equal((bool)expected["GoalMet"]!, goalTick is not null);
        if (goalTick is { } tick)
            Assert.InRange(tick, (int)expected["GoalTick"]! - GoalTickTolerance, (int)expected["GoalTick"]! + GoalTickTolerance);
    }

    /// <summary>Each event happens within the tolerance of its baseline counterpart: its tick within <see cref="GoalTickTolerance"/>
    /// ticks and every ordinal it carries within the same span of 480 Hz substeps (four per 120 Hz tick).</summary>
    private static void AssertTimingsWithinTolerance(string[] baseline, List<string> actual)
    {
        const int OrdinalsPerTick = 4;
        for (var i = 0; i < baseline.Length; i++)
        {
            var (expectedTick, expectedOrdinals) = Timing(baseline[i]);
            var (actualTick, actualOrdinals) = Timing(actual[i]);
            Assert.True(Math.Abs(actualTick - expectedTick) <= GoalTickTolerance, $"event {i} at tick {actualTick}, baseline {expectedTick}");
            Assert.Equal(expectedOrdinals.Length, actualOrdinals.Length);
            for (var j = 0; j < expectedOrdinals.Length; j++)
                Assert.True(Math.Abs(actualOrdinals[j] - expectedOrdinals[j]) <= GoalTickTolerance * OrdinalsPerTick,
                    $"event {i} ordinal {actualOrdinals[j]}, baseline {expectedOrdinals[j]}");
        }
    }

    private static (long Tick, long[] Ordinals) Timing(string e) =>
        (long.Parse(TickPrefix().Match(e).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
            Ordinals().Matches(e).Select(m => long.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray());

    [Theory]
    [MemberData(nameof(Elements))]
    public async Task AnElementPlacedAloneKeepsItsBaselineEventOrderAndEndsAtItsBaselinePose(string element)
    {
        var expected = Baseline.Value["Elements"]!.AsArray().Single(row => (string)row!["Id"]! == element)!;
        var bench = new WorkshopConstruction(new ConstructionRevision(2), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(PackageRuntimeParityTests.Placed(new PartId(element))));
        var (_, run, _) = await NoFixedCapacityTests.RunAndReset(bench, ElementTicks);
        var (_, events, finals) = Outcome(run, bench);

        var baselineEvents = expected["Events"]!.AsArray().Select(e => (string)e!).ToArray();
        Assert.Equal(Kinds(baselineEvents), Kinds(events));
        AssertTimingsWithinTolerance(baselineEvents, events);
        var baseline = expected["FinalBodies"]!.AsArray();
        Assert.Equal(baseline.Count, finals.Count);
        for (var i = 0; i < finals.Count; i++)
        {
            var (x, y, z) = ((double)baseline[i]!["X"]!, (double)baseline[i]!["Y"]!, (double)baseline[i]!["Z"]!);
            var moved = Math.Sqrt(Math.Pow(finals[i].X - x, 2) + Math.Pow(finals[i].Y - y, 2) + Math.Pow(finals[i].Z - z, 2));
            Assert.True(moved <= ElementPoseTolerance, $"{element} body {i} ends {moved} m from its baseline pose");
        }
    }

    // Negative control: the comparison sees a changed event kind and a reordered event, and ignores only an event's time.
    [Fact]
    public void EventKindsIgnoreTimeButNotKindOrOrder()
    {
        string[] events = ["tick 1: capture sensor=7 phase=Clear ordinal=0 eventPhase=0", "tick 335: capture sensor=7 phase=Latched ordinal=1340 eventPhase=0.5"];
        Assert.Equal(Kinds(events), Kinds(["tick 1: capture sensor=7 phase=Clear ordinal=0 eventPhase=0", "tick 331: capture sensor=7 phase=Latched ordinal=1324 eventPhase=0"]));
        Assert.NotEqual(Kinds(events), Kinds(["tick 1: capture sensor=7 phase=Clear ordinal=0 eventPhase=0", "tick 335: capture sensor=8 phase=Latched ordinal=1340 eventPhase=0.5"]));
        Assert.NotEqual(Kinds(events), Kinds([events[1], events[0]]));
        // Timing control: 4 ticks (16 ordinals) early is within the 12-tick span; 13 ticks (52 ordinals) late is not.
        AssertTimingsWithinTolerance(events, ["tick 1: capture sensor=7 phase=Clear ordinal=0 eventPhase=0",
            "tick 331: capture sensor=7 phase=Latched ordinal=1324 eventPhase=0"]);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertTimingsWithinTolerance(events,
            ["tick 1: capture sensor=7 phase=Clear ordinal=0 eventPhase=0", "tick 348: capture sensor=7 phase=Latched ordinal=1392 eventPhase=0"]));
    }

    // An event's kind: its text without the time it happened (tick, ordinals, phases) and its measured approach speed.
    private static string[] Kinds(IEnumerable<string> events) => events.Select(e => TimeKeys().Replace(ApproachSpeed().Replace(
        CaptureTime().Replace(TickPrefix().Replace(e, ""), ""), ""), "")).ToArray();

    [GeneratedRegex(@"^tick (\d+): ")]
    private static partial Regex TickPrefix();
    [GeneratedRegex(@"(?:\bordinal=|""(?:Ordinal|EventOrdinal|CauseOrdinal)"":)(\d+)")]
    private static partial Regex Ordinals();
    [GeneratedRegex(@"ordinal=\d+ eventPhase=\S+")]
    private static partial Regex CaptureTime();
    [GeneratedRegex(@"""ApproachSpeed"":\{[^}]*\}")]
    private static partial Regex ApproachSpeed();
    [GeneratedRegex(@"""(?:Ordinal|EventOrdinal|CauseOrdinal|EventPhase|CausePhase)"":[^,}]+")]
    private static partial Regex TimeKeys();

    // The same event text the baseline capture recorded: each capture and activation row whenever it changes, and the final body
    // positions in metres.
    private static (int? GoalTick, List<string> Events, List<(double X, double Y, double Z)> Finals) Outcome(
        List<WorkshopRead> run, WorkshopConstruction bench)
    {
        int? goalTick = null;
        var events = new List<string>();
        var captures = new List<string>();
        var activations = new List<string>();
        foreach (var read in run)
        {
            if (goalTick is null && bench.Puzzle.Goal.Kind != WorkshopGoalKind.None &&
                WorkshopGoalEvaluator.Occurrence(bench.Puzzle.Goal, read.Captures, read.Activations) is not null)
                goalTick = (int)read.Tick.Value;
            for (var i = 0; i < read.Captures.Count; i++)
            {
                var c = read.Captures[i];
                Changed(captures, i, $"capture sensor={c.Sensor.Value} phase={c.Phase} ordinal={c.EventOrdinal} eventPhase={c.EventPhase}", read);
            }
            for (var i = 0; i < read.Activations.Count; i++)
                Changed(activations, i, "activation " + JsonSerializer.Serialize(read.Activations[i], Options), read);
        }
        var finals = new List<(double X, double Y, double Z)>();
        for (var i = 0; i < run[^1].Bodies.Count; i++)
        {
            var body = run[^1].Bodies[i].Body;
            finals.Add(((body.Cell.X + (double)body.Local.X) / 16, (body.Cell.Y + (double)body.Local.Y) / 16,
                (body.Cell.Z + (double)body.Local.Z) / 16));
        }
        return (goalTick, events, finals);

        void Changed(List<string> previous, int index, string text, WorkshopRead read)
        {
            if (index >= previous.Count) previous.Add("");
            if (previous[index] == text) return;
            events.Add($"tick {read.Tick.Value}: {text}");
            previous[index] = text;
        }
    }
}
