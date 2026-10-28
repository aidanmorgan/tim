using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>The current authored catalogue. Planned content is indexed but never constructed.</summary>
public static class PuzzleCatalogue
{
    private static readonly Lazy<IReadOnlyDictionary<PuzzleId, PuzzleDefinition>> Definitions = new(Load);
    public static IReadOnlyList<PuzzleId> Playable => Definitions.Value.Values.OrderBy(value => value.Presentation.Order).Select(value => value.Entry.Id).ToArray();

    internal static PuzzleDefinition Definition(PuzzleId id) =>
        Definitions.Value.TryGetValue(id, out var definition) ? definition :
            throw new WorkshopEditRejectedException(WorkshopEditRejection.PuzzleSettings, "Unknown or unavailable puzzle.");

    public static PuzzlePresentation Presentation(PuzzleId id) => Definition(id).Presentation;

    public static WorkshopConstruction Create(PuzzleId id, ConstructionRevision revision,
        WorkshopCadenceSettings settings, GpuBodyId first, PuzzlePrecision precision)
    {
        var definition = Definition(id);
        var instances = definition.CreateInstances(first, precision);
        var identities = definition.Placements.Select((row, index) =>
            KeyValuePair.Create(row.Slot, instances[index].Id)).ToDictionary();
        if (!identities.ContainsKey(definition.Goal.Target))
            identities.Add(definition.Goal.Target, new(checked(first.Value + (ulong)instances.Length)));
        var inventory = definition.Inventory[0];
        var puzzle = new WorkshopPuzzle(id, WorkshopPlacementMode.Manual, precision, inventory.Part,
            checked((uint)inventory.Count), definition.CreateGoal(identities),
            definition.Primary, definition.Target, definition.Placement);
        var construction = new WorkshopConstruction(revision, settings, new(instances.ToArray()), puzzle);
        construction.Validate();
        return construction;
    }

    public static WorkshopConstruction WithInstance(WorkshopConstruction construction, IWorkshopInstance instance)
    {
        if (!construction.Puzzle.Id.IsAuthored)
            return construction with { Instances = construction.Instances.With(instance) };
        var definition = Definition(construction.Puzzle.Id);
        var row = definition.Inventory.SingleOrDefault(row => row.Part == instance.PartId);
        if (row is null) return construction with { Instances = construction.Instances.With(instance) };
        instance = row.Configure(instance, construction.Puzzle.Precision);
        var result = construction with { Instances = construction.Instances.With(instance) };
        if (row.GoalTarget)
            result = result with { Puzzle = result.Puzzle with { Goal = result.Puzzle.Goal with
            { Target = instance.Id } } };
        return result;
    }

    public static WorkshopConstruction WithPrecision(WorkshopConstruction construction, PuzzlePrecision precision)
    {
        var definition = Definition(construction.Puzzle.Id);
        var sourceIndex = Array.FindIndex(definition.Placements.ToArray(), row => row.Slot == definition.Goal.Source);
        var source = construction.Puzzle.Goal.Kind == WorkshopGoalKind.Captured ?
            construction.Puzzle.Goal.Body.Value : construction.Puzzle.Goal.SourceNode.Value;
        var fixtures = definition.CreateInstances(new(checked(source - (ulong)sourceIndex)), precision);
        var result = construction with { Puzzle = construction.Puzzle with { Precision = precision } };
        foreach (var fixture in fixtures) result = result with { Instances = result.Instances.With(fixture) };
        foreach (var instance in result.Instances.ToArray())
            if (fixtures.All(fixture => fixture.Id != instance.Id)) result = WithInstance(result, instance);
        result.Validate();
        return result;
    }

    internal static void Validate(WorkshopPuzzle puzzle, WorkshopConstruction construction)
    {
        var definition = Definition(puzzle.Id);
        PhysicsDeclarationBounds.Range(puzzle.Precision.Value, 0f, 1f);
        puzzle.Goal.Validate();
        if (puzzle.Placement != WorkshopPlacementMode.Manual ||
            puzzle.InventoryPart != definition.Inventory[0].Part ||
            puzzle.InventoryCount != (uint)definition.Inventory[0].Count ||
            puzzle.PrimaryAssistance != definition.Primary || puzzle.TargetAssistance != definition.Target ||
            puzzle.PlacementAssistance != definition.Placement ||
            (!definition.ConnectionsAllowed && construction.Connections.Count != 0))
            Reject(WorkshopEditRejection.PuzzleSettings, "Authored puzzle settings changed.");
        var sourceIndex = Array.FindIndex(definition.Placements.ToArray(), row => row.Slot == definition.Goal.Source);
        var source = puzzle.Goal.Kind == WorkshopGoalKind.Captured
            ? puzzle.Goal.Body : new GpuBodyId(puzzle.Goal.SourceNode.Value);
        if (source.Value <= (ulong)sourceIndex) Reject(WorkshopEditRejection.FixedPart, "Invalid authored owner range.");
        var first = new GpuBodyId(source.Value - (ulong)sourceIndex);
        var expected = definition.CreateInstances(first, puzzle.Precision);
        var ids = definition.Placements.Select((row, index) =>
            KeyValuePair.Create(row.Slot, expected[index].Id)).ToDictionary();
        if (!ids.ContainsKey(definition.Goal.Target))
            ids.Add(definition.Goal.Target, puzzle.Goal.Kind == WorkshopGoalKind.Captured ?
                puzzle.Goal.Target : new(puzzle.Goal.TargetNode.Value));
        if (definition.CreateGoal(ids) != puzzle.Goal)
            Reject(WorkshopEditRejection.PuzzleSettings, "Authored goal changed.");
        foreach (var fixture in expected)
        {
            var matches = construction.Instances.Where(instance => instance.Id == fixture.Id).ToArray();
            if (matches.Length != 1 || !matches[0].Equals(fixture))
                Reject(WorkshopEditRejection.FixedPart, "Fixed authored declaration changed.");
        }
        var placed = construction.Instances.Where(instance => expected.All(fixture => fixture.Id != instance.Id)).ToArray();
        foreach (var instance in placed)
            if (instance.Locked || !definition.Inventory.Any(row => row.Part == instance.PartId))
                Reject(WorkshopEditRejection.InventoryUnsupported, "Unsupported authored inventory.");
        foreach (var instance in placed)
        {
            var row = definition.Inventory.Single(row => row.Part == instance.PartId);
            if (!instance.Equals(row.Configure(instance, puzzle.Precision)))
                Reject(WorkshopEditRejection.PuzzleSettings, "Authored inventory assistance changed.");
        }
        foreach (var row in definition.Inventory)
            if (placed.Count(instance => instance.PartId == row.Part) > row.Count)
                Reject(WorkshopEditRejection.InventoryExceeded, "Authored inventory exhausted.");
        var targetInventory = definition.Inventory.Where(row => row.GoalTarget).ToArray();
        if (targetInventory.Length != 0)
        {
            var targets = placed.Where(instance => instance.PartId == targetInventory[0].Part).ToArray();
            if (targets.Length != 0 && (targets.Length != 1 || targets[0].Id != ids[definition.Goal.Target]))
                Reject(WorkshopEditRejection.PuzzleSettings, "Placed goal owner changed.");
        }
    }

    private static void Reject(WorkshopEditRejection reason, string message) =>
        throw new WorkshopEditRejectedException(reason, message);

    private static Dictionary<PuzzleId, PuzzleDefinition> Load()
    {
        using var stream = typeof(PuzzleCatalogue).Assembly.GetManifestResourceStream("CuriousContraptions.Puzzles.json")
            ?? throw new InvalidDataException("Missing authored puzzle resource.");
        using var reader = new StreamReader(stream);
        return ReadDefinitions(reader.ReadToEnd());
    }

    internal static Dictionary<PuzzleId, PuzzleDefinition> ReadDefinitions(string json)
    {
        var entries = PuzzleContentReader.ReadIndex(json);
        var definitions = new Dictionary<PuzzleId, PuzzleDefinition>(16);
        foreach (var entry in entries.Where(entry => entry.Availability == PuzzleAvailability.Playable))
        {
            try { definitions.Add(entry.Id, PuzzleDefinitionReader.Parse(entry)); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
            {
                throw PuzzleContentReader.Invalid(entry.Id.Value, "declaration", error.Message);
            }
        }
        if (definitions.Count == 0) throw new InvalidDataException("Puzzle catalogue has no playable content.");
        if (definitions.Values.Select(value => value.Presentation.Order).Distinct().Count() != definitions.Count)
            throw new InvalidDataException("Duplicate playable puzzle order.");
        return definitions;
    }
}
