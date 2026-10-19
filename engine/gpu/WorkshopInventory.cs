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
public sealed class PartInventory : IReadOnlyDictionary<PartId, PartAllowance>
{
    private readonly (PartId Id, PartAllowance Allowance)[] _rows;
    public PartInventory(params (PartId Id, PartAllowance Allowance)[] rows)
    {
        if (rows.Select(row => row.Id).Distinct().Count() != rows.Length) throw new ArgumentException("Inventory rows must name distinct parts.");
        _rows = rows;
    }
    public PartAllowance this[PartId key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
    public IEnumerable<PartId> Keys => _rows.Select(row => row.Id);
    public IEnumerable<PartAllowance> Values => _rows.Select(row => row.Allowance);
    public int Count => _rows.Length;
    public bool ContainsKey(PartId key) => TryGetValue(key, out _);
    public bool TryGetValue(PartId key, [MaybeNullWhen(false)] out PartAllowance value)
    {
        foreach (var row in _rows) if (row.Id == key) { value = row.Allowance; return true; }
        value = default; return false;
    }
    public IEnumerator<KeyValuePair<PartId, PartAllowance>> GetEnumerator() =>
        _rows.Select(row => new KeyValuePair<PartId, PartAllowance>(row.Id, row.Allowance)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Palette inventory per mode, in palette row order. Free play lists every discovered part without a count; the rows
/// name delivered parts only, so an undelivered part (the game's UndeliveredParts set) never reaches the palette.</summary>
public static class WorkshopInventoryPolicy
{
    private static readonly PartId[] _canonicalOrder =
    [
        PartId.Basketball,
        PartId.Receiver,
        PartId.ImpactSwitch,
        PartId.SignalLamp,
        PartId.Wall,
        PartId.Delay,
        PartId.PinballBumper,
        PartId.Ramp,
        PartId.Domino,
        PartId.BowlingBall,
        PartId.Battery,
        PartId.Springboard,
        PartId.Pipe,
        PartId.BallDetector,
        PartId.PipeBend45,
        PartId.PipeBend90,
        PartId.Funnel,
        PartId.PoweredGate,
        PartId.PressurePlate,
        PartId.Counter,
        PartId.Clock,
        PartId.Latch
    ];

    private static IReadOnlyDictionary<PartId, PartAllowance>? _free;
    public static IReadOnlyDictionary<PartId, PartAllowance> Free => _free ??= GenerateFree(_canonicalOrder);

    public static IReadOnlyDictionary<PartId, PartAllowance> GenerateFree(IEnumerable<PartId> definitions)
    {
        var set = definitions.ToHashSet();
        var ordered = _canonicalOrder.Where(set.Contains);
        var list = ordered.Select(id => (id, PartAllowance.Unlimited)).ToArray();
        if (list.Length == 0) throw new ArgumentException("Empty registry cannot populate Free inventory.");
        _free = new PartInventory(list);
        return _free;
    }

    public static IReadOnlyDictionary<PartId, PartAllowance> Authored(WorkshopPuzzle puzzle)
    {
        if (puzzle.Id == WorkshopPuzzleId.Free) throw new ArgumentException("Free Workshop has no authored inventory.");
        if (puzzle.Id == WorkshopPuzzleId.WallAndBumper)
            return new PartInventory((PartId.Wall, PartAllowance.Counted(1)),
                (PartId.PinballBumper, PartAllowance.Counted(1)));
        return new PartInventory((puzzle.InventoryPart, PartAllowance.Counted(checked((int)puzzle.InventoryCount))));
    }
}
