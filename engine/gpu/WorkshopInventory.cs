using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CuriousContraptions.Gpu;

public enum PartAllowanceKind : uint { Counted, Unlimited }

/// <summary>How many more parts of one kind the palette may place. Unlimited defers to WorkbenchCapacity alone.</summary>
public readonly record struct PartAllowance(PartAllowanceKind Kind, int Count)
{
    public static PartAllowance Unlimited => new(PartAllowanceKind.Unlimited, 0);
    public static PartAllowance None => Counted(0);
    public static PartAllowance Counted(int count) => new(PartAllowanceKind.Counted, count);
    /// <summary>Remaining allowance, clamped at zero when placed parts exceed the authored count.</summary>
    public PartAllowance Less(int placed) => Kind == PartAllowanceKind.Unlimited ? this : Counted(Math.Max(0, Count - placed));
    public bool Exhausted => Kind == PartAllowanceKind.Counted && Count <= 0;
}

/// <summary>Palette rows in their declared order; lookups derive from the same sequence so enumeration order is the contract.</summary>
public sealed class PartInventory : IReadOnlyDictionary<WorkshopPartKind, PartAllowance>
{
    private readonly (WorkshopPartKind Kind, PartAllowance Allowance)[] _rows;
    public PartInventory(params (WorkshopPartKind Kind, PartAllowance Allowance)[] rows)
    {
        if (rows.Select(row => row.Kind).Distinct().Count() != rows.Length) throw new ArgumentException("Inventory rows must name distinct kinds.");
        _rows = rows;
    }
    public PartAllowance this[WorkshopPartKind key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
    public IEnumerable<WorkshopPartKind> Keys => _rows.Select(row => row.Kind);
    public IEnumerable<PartAllowance> Values => _rows.Select(row => row.Allowance);
    public int Count => _rows.Length;
    public bool ContainsKey(WorkshopPartKind key) => TryGetValue(key, out _);
    public bool TryGetValue(WorkshopPartKind key, [MaybeNullWhen(false)] out PartAllowance value)
    {
        foreach (var row in _rows) if (row.Kind == key) { value = row.Allowance; return true; }
        value = default; return false;
    }
    public IEnumerator<KeyValuePair<WorkshopPartKind, PartAllowance>> GetEnumerator() =>
        _rows.Select(row => new KeyValuePair<WorkshopPartKind, PartAllowance>(row.Kind, row.Allowance)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Palette inventory per mode, in palette row order. Free play lists every playable kind without a count.</summary>
public static class WorkshopInventoryPolicy
{
    // Row order is part of the UI contract (e2e palette anchors): existing rows keep their place and Ramp is the appended last row.
    public static IReadOnlyDictionary<WorkshopPartKind, PartAllowance> Free { get; } = new PartInventory(
        (WorkshopPartKind.Basketball, PartAllowance.Unlimited),
        (WorkshopPartKind.Receiver, PartAllowance.Unlimited),
        (WorkshopPartKind.ImpactSwitch, PartAllowance.Unlimited),
        (WorkshopPartKind.SignalLamp, PartAllowance.Unlimited),
        (WorkshopPartKind.Wall, PartAllowance.Unlimited),
        (WorkshopPartKind.Delay, PartAllowance.Unlimited),
        (WorkshopPartKind.PinballBumper, PartAllowance.Unlimited),
        (WorkshopPartKind.Ramp, PartAllowance.Unlimited));

    public static IReadOnlyDictionary<WorkshopPartKind, PartAllowance> Authored(WorkshopPuzzle puzzle)
    {
        if (puzzle.Id == WorkshopPuzzleId.Free) throw new ArgumentException("Free Workshop has no authored inventory.");
        return new PartInventory((puzzle.InventoryKind, PartAllowance.Counted(checked((int)puzzle.InventoryCount))));
    }
}
