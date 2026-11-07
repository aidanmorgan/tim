using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;
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
public readonly record struct CompiledSupplyIdentities(SupplyNodeId Node, ElectricalSourceId Source);
public sealed record CompiledPartIdentities(GpuMaterialId Material, ImmutableArray<GpuColliderId> Colliders,
    ImmutableArray<CompiledSliderIdentities> Sliders, ImmutableDictionary<SignalNodeId, CompiledSignalIdentities> Signals,
    ImmutableDictionary<LocalSupplyNodeId, CompiledSupplyIdentities> Supply, GpuContactWorkId Work,
    CompiledCaptureIdentities Capture, ImmutableArray<CompiledHingeIdentities> Hinges,
    CompliantSurfaceId CompliantSurface, ImmutableArray<DrivenSurfaceId> DrivenSurfaces, ImmutableDictionary<PortId, RopeGuideId> Guides);

/// <summary>One immutable scene-sized allocation shared by every consumer of an authored instance collection.</summary>
public sealed class CompiledIdentityMap
{
    private readonly ImmutableDictionary<GpuBodyId, CompiledPartIdentities> _parts;
    private readonly ImmutableDictionary<GpuBodyId, GpuBodyId> _owners;
    private readonly ImmutableHashSet<GpuBodyId> _payloads;
    private readonly ImmutableHashSet<GpuBodyId> _dynamicBodies;
    private readonly ImmutableDictionary<GpuBodyId, TargetSelector> _residenceTargets;
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
        if (!_parts.TryGetValue(owner, out var rows) || rows.Capture.Sensor.Value == 0) return false;
        if (!MatchesTarget(owner, target)) return false;
        capture = rows.Capture;
        return true;
    }

    public bool OwnsCapture(CapturePairKey key)
    {
        foreach (var (owner, rows) in _parts)
            if (rows.Capture.Sensor == key.Policy && key.Policy.Value != 0 && MatchesTarget(owner, key.Target))
                return true;
        return false;
    }

    private bool MatchesTarget(GpuBodyId owner, GpuBodyId target)
    {
        if (!_residenceTargets.TryGetValue(owner, out var selector)) return false;
        return selector.Kind switch
        {
            TargetSelectorKind.AllPayloads => _payloads.Contains(target),
            TargetSelectorKind.AllDynamic => _dynamicBodies.Contains(target),
            TargetSelectorKind.Named => ulong.TryParse(selector.NamedTarget, out var id)
                ? target.Value == id
                : ((selector.NamedTarget is "puzzle_target" or "interaction_target") && _dynamicBodies.Contains(target)),
            _ => false
        };
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
        var nextGuide = 1UL;
        var parts = ImmutableDictionary.CreateBuilder<GpuBodyId, CompiledPartIdentities>();
        var owners = ImmutableDictionary.CreateBuilder<GpuBodyId, GpuBodyId>();
        var dynamicBodies = ImmutableHashSet.CreateBuilder<GpuBodyId>();
        var residenceTargets = ImmutableDictionary.CreateBuilder<GpuBodyId, TargetSelector>();
        foreach (var instance in instances.OrderBy(instance => instance.Id.Value))
        {
            var entry = entries[instance.PartId];
            parts.Add(instance.Id, AllocatePart(instance, entry, owners, dynamicBodies, residenceTargets, Allocate, Colliders, ref nextGuide));
        }
        _parts = parts.ToImmutable(); _owners = owners.ToImmutable();
        _dynamicBodies = dynamicBodies.ToImmutable();
        _residenceTargets = residenceTargets.ToImmutable();
        NextIdentity = next;
    }

    private static CompiledPartIdentities AllocatePart(
        IWorkshopInstance instance,
        CuriousContraptions.Bundle.BundleEntry entry,
        ImmutableDictionary<GpuBodyId, GpuBodyId>.Builder owners,
        ImmutableHashSet<GpuBodyId>.Builder dynamicBodies,
        ImmutableDictionary<GpuBodyId, TargetSelector>.Builder residenceTargets,
        Func<ulong> Allocate,
        Func<int, ImmutableArray<GpuColliderId>> Colliders,
        ref ulong nextGuide)
    {
        var material = new GpuMaterialId(Allocate());
        var colliders = Colliders(entry.Colliders.Count);
        var (sliders, hinges) = AllocateChildren(instance.Id, entry, owners, Allocate, Colliders);
        if (entry.Mechanical?.KinematicMode == KinematicMode.Dynamic)
            dynamicBodies.Add(instance.Id);
        foreach (var s in sliders) dynamicBodies.Add(s.Body);
        foreach (var h in hinges) dynamicBodies.Add(h.Body);
        if (entry.Mechanical?.Residence is { } res)
            residenceTargets.Add(instance.Id, res.Target);

        var signals = AllocateSignals(entry, Allocate);
        var supply = AllocateSupply(entry, Allocate);
        var work = entry.Mechanical?.ContactWork is not null ? new GpuContactWorkId(Allocate()) : default;
        var capture = new CompiledCaptureIdentities(
            entry.Mechanical?.Residence is not null ? new(Allocate()) : default,
            entry.Mechanical?.PlanarGuide is not null ? new(Allocate()) : default);
        var compliant = entry.Mechanical?.CompliantSurface is not null ? new CompliantSurfaceId(Allocate()) : default;
        var surfaces = ImmutableArray.CreateBuilder<DrivenSurfaceId>();
        foreach (var surface in entry.Mechanical?.DrivenSurfaces ?? []) surfaces.Add(new(Allocate()));
        var guides = ImmutableDictionary.CreateBuilder<PortId, RopeGuideId>();
        foreach (var port in entry.Ports.Where(port => port.AttachmentRole == RopeAttachmentRole.Guide).OrderBy(port => port.Id))
            guides.Add(port.Id, new(nextGuide++));
        return new(material, colliders, sliders, signals, supply, work,
            capture, hinges, compliant, surfaces.ToImmutable(), guides.ToImmutable());
    }

    private static ImmutableDictionary<LocalSupplyNodeId, CompiledSupplyIdentities> AllocateSupply(
        CuriousContraptions.Bundle.BundleEntry entry, Func<ulong> Allocate)
    {
        var supply = ImmutableDictionary.CreateBuilder<LocalSupplyNodeId, CompiledSupplyIdentities>();
        foreach (var node in (entry.Electrical?.SupplyGraph?.Nodes ?? []).OrderBy(node => node.Id))
            supply.Add(node.Id, new(new(Allocate()), node is SupplySourceDescriptor ? new(Allocate()) : default));
        return supply.ToImmutable();
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
