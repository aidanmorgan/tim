using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions.Gpu;

public sealed record PuzzlePresentation(int Order, string Title, string Task, string Hint,
    string Status, string PrecisionStatus);

internal sealed record AuthoredPlacement(AuthoredSlot Slot, AuthoredInstanceTemplate Instance);
internal readonly record struct AuthoredConnection(AuthoredSlot Source, WorkshopSocket Output,
    AuthoredSlot Target, WorkshopSocket Input, WorkshopConnectionDomain Domain);
internal sealed record AuthoredInventory(PartId Part, int Count, AssistanceProfile Assistance,
    bool GoalTarget, AuthoredInstanceTemplate? Configuration)
{
    internal IWorkshopInstance Configure(IWorkshopInstance instance, PuzzlePrecision precision) =>
        Configuration is { } template ? (template with
        { Cell = instance.Cell, Local = instance.Local, Rotation = instance.Rotation, Locked = instance.Locked }).Create(instance.Id, precision) : instance;
}
internal readonly record struct AuthoredGoal(WorkshopGoalKind Kind, AuthoredSlot Source,
    AuthoredSlot Target, float MinimumSeconds);
internal sealed record PuzzleDefinition(PuzzleContentEntry Entry,
    ImmutableArray<AuthoredPlacement> Placements, ImmutableArray<AuthoredPlacement> Solution,
    ImmutableArray<AuthoredConnection> SolutionConnections, ImmutableArray<AuthoredInventory> Inventory,
    AuthoredGoal Goal, bool ConnectionsAllowed, PuzzlePresentation Presentation, AssistanceProfile Primary,
    AssistanceProfile Target, AssistanceProfile Placement, AssistanceProfile Secondary)
{
    internal ImmutableArray<IWorkshopInstance> CreateInstances(GpuBodyId first, PuzzlePrecision precision)
    {
        if (first.Value == 0) throw new ArgumentException("An authored identity range must start above zero.");
        var result = ImmutableArray.CreateBuilder<IWorkshopInstance>(Placements.Length);
        for (var i = 0; i < Placements.Length; i++)
            result.Add(Placements[i].Instance.Create(new(checked(first.Value + (uint)i)), precision));
        return result.ToImmutable();
    }

    internal WorkshopGoal CreateGoal(IReadOnlyDictionary<AuthoredSlot, GpuBodyId> identities)
    {
        if (!identities.TryGetValue(Goal.Source, out var source) ||
            !identities.TryGetValue(Goal.Target, out var target) || source == target)
            throw PuzzleContentReader.Invalid(Entry.Id.Value, "goal", "missing or overlapping owner identities");
        var goal = Goal.Kind switch
        {
            WorkshopGoalKind.Captured => new WorkshopGoal(Goal.Kind, source, target),
            WorkshopGoalKind.ActivatedAfter => new WorkshopGoal(Goal.Kind, default, default,
                new(source.Value), new(target.Value), new(Goal.MinimumSeconds)),
            _ => throw PuzzleContentReader.Invalid(Entry.Id.Value, "goal.kind", "unsupported authored goal")
        };
        goal.Validate();
        return goal;
    }

    internal WorkshopConstruction CreateSolution(ConstructionRevision revision, WorkshopCadenceSettings settings,
        GpuBodyId first, PuzzlePrecision precision)
    {
        var rows = Placements.Concat(Solution).ToArray();
        var instances = rows.Select((row, index) => row.Instance.Create(new(checked(first.Value + (ulong)index)), precision)).ToArray();
        var ids = rows.Select((row, index) => KeyValuePair.Create(row.Slot, instances[index].Id)).ToDictionary();
        var inventory = Inventory[0];
        var puzzle = new WorkshopPuzzle(Entry.Id, WorkshopPlacementMode.Manual, precision, inventory.Part,
            checked((uint)inventory.Count), CreateGoal(ids), Primary, Target, Placement);
        var connections = new WorkshopConnections(SolutionConnections.Select(row =>
            new WorkshopConnection(ids[row.Source], row.Output, ids[row.Target], row.Input, row.Domain)).ToArray());
        return new(revision, settings, new(instances), puzzle, connections);
    }

    internal PartInventoryDictionary InventoryRows() =>
        new(Inventory.Select(row => (row.Part, PartAllowance.Counted(row.Count))).ToArray());
}

internal static class PuzzleDefinitionReader
{
    internal static PuzzleDefinition Parse(PuzzleContentEntry entry)
    {
        if (entry.Availability != PuzzleAvailability.Playable)
            throw PuzzleContentReader.Invalid(entry.Id.Value, "availability", "puzzle is not playable");
        var where = entry.Id.Value;
        var fields = PuzzleContentReader.Fields(entry.Declaration, where);
        AuthoredInstanceReader.Require(fields, where, "id", "availability", "title", "subtitle", "description", "hint",
            "inventory", "solution", "solution_connections", "placements", "goal", "connections_allowed", "profiles", "presentation");
        var placements = ImmutableArray.CreateBuilder<AuthoredPlacement>();
        var slots = new HashSet<AuthoredSlot>(16);
        foreach (var row in AuthoredInstanceReader.Rows(fields["placements"], where + ".placements"))
        {
            var values = PuzzleContentReader.Fields(row, where);
            AuthoredInstanceReader.Require(values, where, "slot", "instance");
            var slot = new AuthoredSlot(PuzzleContentReader.Text(values, "slot", where));
            if (!slots.Add(slot)) throw PuzzleContentReader.Invalid(where, "slot", "duplicate placement slot");
            placements.Add(new(slot, AuthoredInstanceReader.Parse(values["instance"], where + "." + slot.Value)));
        }
        if (placements.Count == 0) throw PuzzleContentReader.Invalid(where, "placements", "empty fixed layout");
        var inventory = ImmutableArray.CreateBuilder<AuthoredInventory>();
        var parts = new HashSet<PartId>(16);
        foreach (var row in AuthoredInstanceReader.Rows(fields["inventory"], where + ".inventory"))
        {
            var values = PuzzleContentReader.Fields(row, where);
            AuthoredInstanceReader.Require(values, where, "part", "count", "assistance", "goal_target", "configuration");
            var part = new PartId(PuzzleContentReader.Text(values, "part", where));
            var count = AuthoredInstanceReader.Integer(values["count"], where + ".inventory.count");
            if (!DeclaredPhysics.Contains(part) || !parts.Add(part) || count <= 0)
                throw PuzzleContentReader.Invalid(where, "inventory", "unknown, duplicate or nonpositive allowance");
            var configuration = values["configuration"].ValueKind == JsonValueKind.Null ? null :
                AuthoredInstanceReader.Parse(values["configuration"], where + ".inventory.configuration");
            if (configuration is not null && (configuration.Part != part || configuration.Locked))
                throw PuzzleContentReader.Invalid(where, "inventory.configuration", "foreign or locked inventory template");
            inventory.Add(new(part, count, AuthoredInstanceReader.Profile(values["assistance"], where),
                AuthoredInstanceReader.Boolean(values["goal_target"], where + ".inventory.goal_target"), configuration));
        }
        if (inventory.Count == 0) throw PuzzleContentReader.Invalid(where, "inventory", "empty authored inventory");
        var goalFields = PuzzleContentReader.Fields(fields["goal"], where);
        AuthoredInstanceReader.Require(goalFields, where, "kind", "source", "target", "minimum_seconds");
        var goal = new AuthoredGoal(
            AuthoredInstanceReader.Closed<WorkshopGoalKind>(PuzzleContentReader.Text(goalFields, "kind", where), where),
            new(PuzzleContentReader.Text(goalFields, "source", where)),
            new(PuzzleContentReader.Text(goalFields, "target", where)),
            AuthoredInstanceReader.Number(goalFields["minimum_seconds"], where));
        if (!slots.Contains(goal.Source) || goal.Source == goal.Target ||
            (!slots.Contains(goal.Target) && inventory.Count(row => row.GoalTarget) != 1) ||
            goal.Kind is not (WorkshopGoalKind.Captured or WorkshopGoalKind.ActivatedAfter) ||
            goal.MinimumSeconds < 0 || goal.MinimumSeconds > 120 ||
            (goal.Kind == WorkshopGoalKind.Captured && goal.MinimumSeconds != 0))
            throw PuzzleContentReader.Invalid(where, "goal", "invalid authored references or duration");
        var solution = ImmutableArray.CreateBuilder<AuthoredPlacement>();
        var uses = new Dictionary<PartId, int>();
        foreach (var row in AuthoredInstanceReader.Rows(fields["solution"], where + ".solution"))
        {
            var values = PuzzleContentReader.Fields(row, where);
            AuthoredInstanceReader.Require(values, where, "slot", "instance");
            var slot = new AuthoredSlot(PuzzleContentReader.Text(values, "slot", where));
            if (!slots.Add(slot)) throw PuzzleContentReader.Invalid(where, "slot", "duplicate solution or fixture slot");
            var instance = AuthoredInstanceReader.Parse(values["instance"], where + "." + slot.Value);
            var allowance = inventory.SingleOrDefault(item => item.Part == instance.Part);
            uses.TryGetValue(instance.Part, out var used);
            if (instance.Locked || allowance is null || used >= allowance.Count)
                throw PuzzleContentReader.Invalid(where, "solution", "locked, unsupported or excess inventory");
            uses[instance.Part] = used + 1;
            if (allowance.GoalTarget != (slot == goal.Target && !placements.Any(item => item.Slot == goal.Target)))
                throw PuzzleContentReader.Invalid(where, "solution", "incorrect goal target slot");
            foreach (var precision in new[] { 0f, .45f, 1f })
            {
                var created = instance.Create(new(1), new(precision));
                if (!created.Equals(allowance.Configure(created, new(precision))))
                    throw PuzzleContentReader.Invalid(where, "solution", "inventory configuration differs");
            }
            solution.Add(new(slot, instance));
        }
        if (solution.Count == 0 || !slots.Contains(goal.Target))
            throw PuzzleContentReader.Invalid(where, "solution", "empty solution or absent goal target");
        var connections = ImmutableArray.CreateBuilder<AuthoredConnection>();
        foreach (var row in AuthoredInstanceReader.Rows(fields["solution_connections"], where))
        {
            var values = PuzzleContentReader.Fields(row, where);
            AuthoredInstanceReader.Require(values, where, "source", "output", "target", "input", "domain");
            var source = new AuthoredSlot(PuzzleContentReader.Text(values, "source", where));
            var target = new AuthoredSlot(PuzzleContentReader.Text(values, "target", where));
            if (!slots.Contains(source) || !slots.Contains(target))
                throw PuzzleContentReader.Invalid(where, "solution_connections", "unknown explicit slot");
            connections.Add(new(source,
                AuthoredInstanceReader.Closed<WorkshopSocket>(PuzzleContentReader.Text(values, "output", where), where),
                target, AuthoredInstanceReader.Closed<WorkshopSocket>(PuzzleContentReader.Text(values, "input", where), where),
                AuthoredInstanceReader.Closed<WorkshopConnectionDomain>(PuzzleContentReader.Text(values, "domain", where), where)));
        }
        if (connections.Count != 0 && !AuthoredInstanceReader.Boolean(fields["connections_allowed"], where))
            throw PuzzleContentReader.Invalid(where, "solution_connections", "connections forbidden");
        var profiles = PuzzleContentReader.Fields(fields["profiles"], where);
        AuthoredInstanceReader.Require(profiles, where, "primary", "target", "placement", "secondary");
        var presentation = PuzzleContentReader.Fields(fields["presentation"], where);
        AuthoredInstanceReader.Require(presentation, where, "order", "title", "task", "hint", "status", "precision_status");
        var order = AuthoredInstanceReader.Integer(presentation["order"], where + ".presentation.order");
        if (order < 0) throw PuzzleContentReader.Invalid(where, "presentation.order", "negative order");
        var display = new PuzzlePresentation(order, PuzzleContentReader.Text(presentation, "title", where),
            PuzzleContentReader.Text(presentation, "task", where), PuzzleContentReader.Text(presentation, "hint", where),
            PuzzleContentReader.Text(presentation, "status", where), PuzzleContentReader.Text(presentation, "precision_status", where));
        var definition = new PuzzleDefinition(entry, placements.ToImmutable(), solution.ToImmutable(), connections.ToImmutable(), inventory.ToImmutable(), goal,
            AuthoredInstanceReader.Boolean(fields["connections_allowed"], where + ".connections_allowed"), display,
            AuthoredInstanceReader.Profile(profiles["primary"], where),
            AuthoredInstanceReader.Profile(profiles["target"], where),
            AuthoredInstanceReader.Profile(profiles["placement"], where),
            AuthoredInstanceReader.Profile(profiles["secondary"], where));
        var complete = definition.CreateSolution(new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        complete.Instances.Validate();
        complete.Connections.Validate(complete.Instances);
        if (goal.Kind == WorkshopGoalKind.Captured)
        {
            if (!complete.Instances.Identities.TryCapture(complete.Puzzle.Goal.Target, complete.Puzzle.Goal.Body, out _))
                throw PuzzleContentReader.Invalid(where, "goal", "owners do not declare capture of this payload");
        }
        else
        {
            // Validate node ownership through the production compiler without consulting the catalogue being loaded.
            var nodes = WorkshopActivationCompiler.Compile(complete with { Puzzle = default }).Nodes.ToArray();
            if (!nodes.Any(node => node.Id == complete.Puzzle.Goal.SourceNode) ||
                !nodes.Any(node => node.Id == complete.Puzzle.Goal.TargetNode))
                throw PuzzleContentReader.Invalid(where, "goal", "owners do not declare activation nodes");
        }
        return definition;
    }
}
