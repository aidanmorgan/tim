using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Authored instances supply declarations; no instance owns an update or numerical law.</summary>
public interface IWorkshopInstance
{
    WorkshopPartKind Kind { get; }
    GpuBodyId Id { get; }
    CellOrigin Cell { get; }
    LocalPosition Local { get; }
    CanonicalRotation Rotation { get; }
    bool Locked { get; }
    void Validate();
}

public readonly record struct RampDimensions(Metres Length, Metres Width)
{
    public static RampDimensions Default => new(new((Half)3), new((Half)1.3));
    public static Metres Thickness => new((Half).18);
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(Length.Value, (Half).125, (Half)4);
        PhysicsDeclarationBounds.Range(Width.Value, (Half).125, (Half)4);
    }
}

public readonly record struct WorkshopRamp(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, RampDimensions Dimensions, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Ramp;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Dimensions.Validate();
    }
}

/// <summary>One immutable collection is the construction's only instance storage.</summary>
public sealed class WorkshopInstances : IReadOnlyList<IWorkshopInstance>, IEquatable<WorkshopInstances>
{
    public const int Capacity = 8;
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
        if (Count > Capacity) throw new ArgumentException("Construction instance capacity exceeded.");
        var ids = new HashSet<GpuBodyId>();
        var balls = 0; var receivers = 0; var ramps = 0; var switches = 0; var lamps = 0;
        foreach (var item in _items)
        {
            switch (item)
            {
                case WorkshopBall: balls++; break;
                case WorkshopReceiver: receivers++; break;
                case WorkshopRamp: ramps++; break;
                case WorkshopSwitch: switches++; break;
                case WorkshopLamp: lamps++; break;
                default: throw new ArgumentException("Unsupported authored instance declaration.");
            }
            item.Validate();
            if (!ids.Add(item.Id)) throw new ArgumentException("Authored body identities must be unique.");
        }
        if (balls > 1 || receivers > 1 || ramps > 2 || switches > 1 || lamps > 1)
            throw new ArgumentException("Current instance population exceeds admitted capabilities.");
    }
    public bool Equals(WorkshopInstances? other) => other is not null && _items.SequenceEqual(other._items);
    public override bool Equals(object? obj) => obj is WorkshopInstances other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode(); foreach (var item in _items) hash.Add(item); return hash.ToHashCode();
    }
}
