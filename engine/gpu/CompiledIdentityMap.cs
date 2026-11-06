using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CuriousContraptions.Gpu;

public sealed record CompiledSliderIdentities(GpuBodyId Body, GpuMaterialId Material,
    ImmutableArray<GpuColliderId> Colliders, PrismaticConstraintId Joint);
public sealed record CompiledHingeIdentities(GpuBodyId Body, GpuMaterialId Material,
    ImmutableArray<GpuColliderId> Colliders, RevoluteConstraintId Joint);
public readonly record struct CompiledCaptureIdentities(GpuSensorId Sensor, GpuForceId Guide);
public readonly record struct CompiledSignalIdentities(ActivationNodeId Node, GpuContactTriggerId Trigger,
    GpuOrientationSensorId Orientation, GpuContactLoadSensorId ContactLoad, GpuPassageSensorId Passage);
public sealed record CompiledPartIdentities(GpuMaterialId Material, ImmutableArray<GpuColliderId> Colliders,
    ImmutableArray<CompiledSliderIdentities> Sliders, ImmutableDictionary<SignalNodeId, CompiledSignalIdentities> Signals,
    ElectricalSourceId Source, GpuContactWorkId Work,
    CompiledCaptureIdentities Capture, ImmutableArray<CompiledHingeIdentities> Hinges,
    CompliantSurfaceId CompliantSurface, ImmutableArray<DrivenSurfaceId> DrivenSurfaces);

/// <summary>One immutable scene-sized allocation shared by every consumer of an authored instance collection.</summary>
public sealed class CompiledIdentityMap
{
    private readonly ImmutableDictionary<GpuBodyId, CompiledPartIdentities> _parts;
    private readonly ImmutableDictionary<GpuBodyId, GpuBodyId> _owners;
    private readonly ImmutableHashSet<GpuBodyId> _payloads;
    public GpuBodyId Plane { get; }
    public GpuMaterialId PlaneMaterial { get; }
    public GpuColliderId PlaneCollider { get; }
    public ulong NextIdentity { get; }
    public CompiledPartIdentities this[GpuBodyId owner] => _parts.TryGetValue(owner, out var rows) ? rows :
        throw new ArgumentException("Compiled identity owner is absent.");
    public GpuBodyId Body(GpuBodyId owner, PackageBodyIndex body)
    {
        var rows = this[owner];
        if (body.Value == 0) return owner;
        var index = body.Value - 1;
        if (index >= 0 && index < rows.Sliders.Length) return rows.Sliders[index].Body;
        index -= rows.Sliders.Length;
        if (index >= 0 && index < rows.Hinges.Length) return rows.Hinges[index].Body;
        throw new ArgumentException("Absent package body.");
    }
    public bool IsChild(GpuBodyId owner, GpuBodyId child) => _owners.TryGetValue(child, out var actual) && actual == owner;
    public bool TryCapture(GpuBodyId owner, GpuBodyId target, out CompiledCaptureIdentities capture)
    {
        capture = default;
        if (!_payloads.Contains(target) || !_parts.TryGetValue(owner,out var rows) || rows.Capture.Sensor.Value==0) return false;
        capture=rows.Capture; return true;
    }

    public bool OwnsCapture(CapturePairKey key)
    {
        if(!_payloads.Contains(key.Target)) return false;
        foreach(var rows in _parts.Values) if(rows.Capture.Sensor==key.Policy && key.Policy.Value!=0) return true;
        return false;
    }

    public CompiledIdentityMap(IReadOnlyList<IWorkshopInstance> instances, IReadOnlyDictionary<PartId, CuriousContraptions.Bundle.BundleEntry> entries)
    {
        var authored = new HashSet<GpuBodyId>(16);
        var next = 1UL;
        foreach (var instance in instances)
        {
            if (instance.Id.Value == 0 || !authored.Add(instance.Id)) throw new ArgumentException("Duplicate or absent authored identity.");
            next = Math.Max(next, checked(instance.Id.Value + 1));
        }
        ulong Allocate() { var value = next; next = checked(next + 1); return value; }
        ImmutableArray<GpuColliderId> Colliders(int count)
        {
            var result = ImmutableArray.CreateBuilder<GpuColliderId>(count);
            for (var i = 0; i < count; i++) result.Add(new(Allocate()));
            return result.MoveToImmutable();
        }
        Plane = new(Allocate()); PlaneMaterial = new(Allocate()); PlaneCollider = new(Allocate());
        _payloads = instances.Where(instance => entries[instance.PartId].Mechanical?.Payload == CuriousContraptions.Abstractions.Mechanical.PayloadPolicy.CaptureTarget).Select(instance=>instance.Id).ToImmutableHashSet();
        var parts = ImmutableDictionary.CreateBuilder<GpuBodyId, CompiledPartIdentities>();
        var owners = ImmutableDictionary.CreateBuilder<GpuBodyId, GpuBodyId>();
        foreach (var instance in instances.OrderBy(instance => instance.Id.Value))
        {
            var entry = entries[instance.PartId];
            var material = new GpuMaterialId(Allocate());
            var colliders = Colliders(entry.Colliders.Count);
            var (sliders, hinges) = AllocateChildren(instance.Id, entry, owners, Allocate, Colliders);
            var signals = AllocateSignals(entry, Allocate);
            var source = entry.Electrical?.Supply?.Store is not null ? new ElectricalSourceId(Allocate()) : default;
            var work = entry.Mechanical?.ContactWork is not null ? new GpuContactWorkId(Allocate()) : default;
            var capture = entry.Mechanical?.Capture is not null
                ? new CompiledCaptureIdentities(new(Allocate()),new(Allocate())) : default;
            var compliant = entry.Mechanical?.CompliantSurface is not null ? new CompliantSurfaceId(Allocate()) : default;
            var surfaces = ImmutableArray.CreateBuilder<DrivenSurfaceId>();
            foreach (var surface in entry.Mechanical?.DrivenSurfaces ?? []) surfaces.Add(new(Allocate()));
            parts.Add(instance.Id, new(material, colliders, sliders, signals, source, work,
                capture, hinges, compliant, surfaces.ToImmutable()));
        }
        _parts = parts.ToImmutable(); _owners = owners.ToImmutable(); NextIdentity = next;
    }

    private static (ImmutableArray<CompiledSliderIdentities>, ImmutableArray<CompiledHingeIdentities>) AllocateChildren(
        GpuBodyId owner, CuriousContraptions.Bundle.BundleEntry entry,
        ImmutableDictionary<GpuBodyId, GpuBodyId>.Builder owners, Func<ulong> Allocate,
        Func<int, ImmutableArray<GpuColliderId>> Colliders)
    {
        var sliders = ImmutableArray.CreateBuilder<CompiledSliderIdentities>(4);
        foreach (var slider in entry.Mechanical?.Sliders ?? [])
        {
            var body = new GpuBodyId(Allocate());
            owners.Add(body, owner);
            sliders.Add(new(body, new(Allocate()), Colliders(slider.MovingBody.Colliders.Count), new(Allocate())));
        }
        var hinges = ImmutableArray.CreateBuilder<CompiledHingeIdentities>(4);
        foreach (var hinge in entry.Mechanical?.Hinges ?? [])
        {
            var body = new GpuBodyId(Allocate());
            owners.Add(body, owner);
            hinges.Add(new(body, new(Allocate()), Colliders(hinge.MovingBody.Colliders.Count), new(Allocate())));
        }
        return (sliders.ToImmutable(), hinges.ToImmutable());
    }

    private static ImmutableDictionary<SignalNodeId, CompiledSignalIdentities> AllocateSignals(
        CuriousContraptions.Bundle.BundleEntry entry, Func<ulong> Allocate)
    {
        var signals = ImmutableDictionary.CreateBuilder<SignalNodeId, CompiledSignalIdentities>();
        foreach (var signal in (entry.Signal?.Nodes ?? []).OrderBy(node => node.Id))
            signals.Add(signal.Id, new(new(Allocate()),
                signal is SignalContactDescriptor ? new(Allocate()) : default,
                signal is SignalOrientationDescriptor ? new(Allocate()) : default,
                signal is SignalContactLoadDescriptor ? new(Allocate()) : default,
                signal is SignalPassageDescriptor ? new(Allocate()) : default));
        return signals.ToImmutable();
    }

}
