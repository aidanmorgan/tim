using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>Owner decisions 24d to 24f: the authored campaign holds the 34 kept levels, each subtitled with its plan level
/// (campaign coverage, section 5), and no two levels share a title.</summary>
public sealed class CampaignContentTests
{
    // Transcribed from docs/planning/elements/readiness/campaign-coverage.md, section 5 ("Chapter 1 re-authoring and
    // relocated authored levels"): id, state and plan level, in plan order. The 29 removed levels are absent.
    private static readonly (PuzzleId Id, PuzzleAvailability Availability, string PlanLevel)[] Kept =
    [
        (new("first_principles"), PuzzleAvailability.Playable, "01"),
        (new("spring_forward"), PuzzleAvailability.Playable, "02"),
        (new("a_gentle_rebound"), PuzzleAvailability.Planned, "03"),
        (new("ready_to_rebound"), PuzzleAvailability.Playable, "03a"),
        (new("domino_effect"), PuzzleAvailability.Playable, "04"),
        (new("bumper_sidekick"), PuzzleAvailability.Playable, "05"),
        (new("bumper_depth"), PuzzleAvailability.Playable, "06"),
        (new("wall_and_bumper"), PuzzleAvailability.Playable, "07"),
        (new("burst_of_motion"), PuzzleAvailability.Playable, "09a"),
        (new("power_trip"), PuzzleAvailability.Planned, "11"),
        (new("air_mail"), PuzzleAvailability.Planned, "11a"),
        (new("third_dimension"), PuzzleAvailability.Planned, "11b"),
        (new("spring_signal"), PuzzleAvailability.Planned, "11c"),
        (new("cold_start"), PuzzleAvailability.Planned, "11d"),
        (new("battery_motor"), PuzzleAvailability.Planned, "12"),
        (new("switched_motor"), PuzzleAvailability.Planned, "12a"),
        (new("counterweight"), PuzzleAvailability.Planned, "12b"),
        (new("pulley_depth"), PuzzleAvailability.Planned, "12c"),
        (new("conveyor_courier"), PuzzleAvailability.Planned, "14a"),
        (new("belt_relay"), PuzzleAvailability.Planned, "14b"),
        (new("reverse_belt"), PuzzleAvailability.Planned, "14c"),
        (new("two_deliveries"), PuzzleAvailability.Planned, "19a"),
        (new("double_cold_start"), PuzzleAvailability.Planned, "19b"),
        (new("grand_contraption"), PuzzleAvailability.Planned, "20"),
        (new("clear_pipe"), PuzzleAvailability.Playable, "21"),
        (new("gentle_bend"), PuzzleAvailability.Playable, "21a"),
        (new("quarter_bend"), PuzzleAvailability.Playable, "22"),
        (new("joined_pipe"), PuzzleAvailability.Playable, "22a"),
        (new("delayed_signal"), PuzzleAvailability.Playable, "23a"),
        (new("wind_then_release"), PuzzleAvailability.Playable, "41"),
        (new("saved_for_later"), PuzzleAvailability.Playable, "41a"),
        (new("solar_motor"), PuzzleAvailability.Planned, "51a"),
        (new("solar_shadow"), PuzzleAvailability.Planned, "51b"),
        (new("delayed_solar"), PuzzleAvailability.Planned, "51c"),
    ];

    private static string Campaign() =>
        File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), NativeTestBoundary.CampaignRelativePath));

    private static IReadOnlyList<PuzzleContentEntry> Index() => PuzzleContentReader.ReadIndex(Campaign());

    [Fact]
    public void TheAuthoredCampaignHoldsExactlyTheKeptLevels()
    {
        // Set equality: a removed level left in, a kept level dropped, or a Planned level admitted as Playable fails it.
        var expected = Kept.Select(level => (level.Id, level.Availability)).OrderBy(level => level.Id.Value, StringComparer.Ordinal);
        var actual = Index().Select(entry => (entry.Id, entry.Availability)).OrderBy(level => level.Id.Value, StringComparer.Ordinal);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EachKeptLevelIsSubtitledWithItsPlanLevel()
    {
        // The subtitle's prefix before " / " is the level's place in the campaign plan, two digits and an optional letter.
        var expected = Kept.ToDictionary(level => level.Id, level => level.PlanLevel);
        var actual = Index().ToDictionary(entry => entry.Id, entry => entry.Subtitle.Split(" / ", 2)[0]);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AuthoredTitlesAreUnique()
    {
        // Case and whitespace are ignored: " Around  The Corner" repeats "Around the corner" for a player reading it.
        // The level titles of every entry, and the picker titles (presentation.title) of the Playable levels.
        Assert.Empty(Repeated(Index().Select(entry => (entry.Id, entry.Title))));
        var definitions = PuzzleCatalogue.ReadDefinitions(RuntimeTestServices.Core, RuntimeTestServices.Bundle, Campaign());
        // The picker also lists the free workshop (no authored identity) under its own label.
        Assert.Empty(Repeated(definitions.Select(pair => (pair.Key, pair.Value.Presentation.Title)).Append((default(PuzzleId), Workshop.FreeWorkshopTitle))));

        // The grouping itself: a case- and whitespace-only repeat forms one group; a distinct pair (control) forms none;
        // a picker title that differs from the free workshop's label only by case and spacing collides with it.
        Assert.Single(Repeated([(new PuzzleId("quarter_bend"), " Around  The Corner"), (new PuzzleId("pulley_depth"), "Around the corner")]));
        Assert.Empty(Repeated([(new PuzzleId("quarter_bend"), "Around the corner"), (new PuzzleId("pulley_depth"), "The long way round")]));
        Assert.Single(Repeated([(default(PuzzleId), Workshop.FreeWorkshopTitle), (new PuzzleId("quarter_bend"), " free  WORKSHOP ")]));
    }

    private static IEnumerable<string> Repeated(IEnumerable<(PuzzleId Id, string Title)> titles) =>
        titles.GroupBy(title => string.Join(' ', title.Title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key + ": " + string.Join(", ", group.Select(title => title.Id.Value)));

    [Theory]
    [InlineData("pulley_depth", "The long way round")]
    [InlineData("grand_contraption", "One thing leads to another")]
    public void ARenamedLevelCarriesItsNewTitle(string level, string title)
    {
        var id = new PuzzleId(level);
        Assert.Equal(title, Index().Single(entry => entry.Id == id).Title);
    }
}
