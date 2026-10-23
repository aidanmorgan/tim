using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Authored instances supply declarations; no instance owns an update or numerical law.</summary>
public interface IWorkshopInstance
{
    PartId PartId { get; }
    GpuBodyId Id { get; }
    CellOrigin Cell { get; }
    LocalPosition Local { get; }
    CanonicalRotation Rotation { get; }
    bool Locked { get; }
    /// <summary>Declared cosmetic curve data; never persisted. None for parts without animated artwork.</summary>
    CosmeticCurveDeclaration Cosmetic { get; }
    void Validate();
}

public interface IWorkshopDeclaredColliders : IWorkshopInstance
{
    IReadOnlyList<ExtractedCollider> DeclaredColliders { get; }
}

public readonly record struct WorkshopGenericInstance(
    PartId PartId,
    GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, bool Locked = false) : IWorkshopDeclaredColliders
{
    public InstanceParameterCollection Parameters { get; init; } = InstanceParameterCollection.Empty;
    public IReadOnlyList<ExtractedCollider> DeclaredColliders => DeclaredPhysics.Colliders(PartId, Parameters.ToBundleValues());
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);
    public void Validate()
    {
        if (PartId == default) throw new ArgumentException("Generic instance requires a valid PartId.");
        // The straight pipe declares its length: it is always a WorkshopPipe (Story 9R.7).
        if (PartId == PartId.Pipe) throw new ArgumentException("A pipe is a typed WorkshopPipe, never a generic instance.");
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        Parameters.Validate(PartId);
        _ = DeclaredColliders;
    }
}

/// <summary>One immutable collection is the construction's only instance storage. There is no part limit (Story 9R.5b, owner
/// decision 20 Oct 2026: an unlimited Free Workshop; memory is the only bound).</summary>
public sealed class WorkshopInstances : IReadOnlyList<IWorkshopInstance>, IEquatable<WorkshopInstances>
{
    private readonly IWorkshopInstance[] _items;
    public static WorkshopInstances Empty { get; } = new();
    public WorkshopInstances(params IWorkshopInstance[] items) => _items = (IWorkshopInstance[])items.Clone();
    public int Count => _items.Length;
    public IWorkshopInstance this[int index] => _items[index];
    public IEnumerator<IWorkshopInstance> GetEnumerator() => ((IEnumerable<IWorkshopInstance>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public T? Find<T>() where T : struct, IWorkshopInstance
    {
        foreach (var item in _items) if (item is T value) return value;
        return null;
    }
    public WorkshopInstances With(IWorkshopInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var index = Array.FindIndex(_items, item => item.Id == instance.Id);
        if (index < 0) return new([.. _items, instance]);
        var copy = (IWorkshopInstance[])_items.Clone(); copy[index] = instance;
        return new(copy);
    }
    public WorkshopInstances Without(GpuBodyId id) => new(_items.Where(item => item.Id != id).ToArray());
    public void Validate()
    {
        var ids = new HashSet<GpuBodyId>();
        foreach (var item in _items)
        {
            if (item is not (WorkshopLamp or WorkshopDomino or WorkshopPressurePlate or WorkshopCounter or WorkshopClock or WorkshopLatch or IWorkshopDeclaredColliders))
                throw new ArgumentException("Unsupported authored instance declaration.");
            item.Validate();
            if (!ids.Add(item.Id)) throw new ArgumentException("Authored body identities must be unique.");
        }
        // No population rule: only the compiled tables' document invariants apply.
        WorkbenchCapacity.Validate(this);
    }
    public bool Equals(WorkshopInstances? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => obj is WorkshopInstances other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode(); foreach (var item in _items) hash.Add(item); return hash.ToHashCode();
    }
}
