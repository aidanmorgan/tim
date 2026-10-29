using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CuriousContraptions.Gpu;

public enum PartAllowanceKind : uint { Counted, Unlimited }

/// <summary>How many more parts of one kind the palette may place. Unlimited permits any representable, valid scene population.</summary>
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
public sealed class PartInventoryDictionary : IReadOnlyDictionary<PartId, PartAllowance>
{
    private readonly (PartId Id, PartAllowance Allowance)[] _rows;
    public PartInventoryDictionary(params (PartId Id, PartAllowance Allowance)[] rows)
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

/// <summary>Palette inventory follows the admitted bundle's declared order.</summary>
public static class WorkshopInventoryPolicy
{
    public static IReadOnlyDictionary<PartId, PartAllowance> Free(IEnumerable<PartId> paletteOrder)
    {
        ArgumentNullException.ThrowIfNull(paletteOrder);
        var list = paletteOrder.Select(id => (id, PartAllowance.Unlimited)).ToArray();
        if (list.Length == 0) throw new ArgumentException("Empty registry cannot populate Free inventory.");
        return new PartInventoryDictionary(list);
    }

    public static IReadOnlyDictionary<PartId, PartAllowance> Authored(WorkshopPuzzle puzzle)
    {
        if (!puzzle.Id.IsAuthored) throw new ArgumentException("Free Workshop has no authored inventory.");
        return PuzzleCatalogue.Definition(puzzle.Id).InventoryRows();
    }
}
