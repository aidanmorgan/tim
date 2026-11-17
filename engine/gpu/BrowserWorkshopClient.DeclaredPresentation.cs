using System;
using System.Buffers.Binary;
using CuriousContraptions.Abstractions;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private readonly HashSet<AnimationTargetId> _mechanicalTargets = new();
    private readonly Dictionary<AnimationTargetId, MechanicalLinkSample> _mechanicalSamples = new();
    private readonly List<MechanicalLinkGeometry> _mechanicalFrame = new();

    private void ReceiveMechanicalGeometry(byte[] bytes)
    {
        var active = _schedule ?? throw new InvalidOperationException("Geometry before schedule.");
        var cadence = new CadenceRevision(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24)));
        cadence.Validate();
        var sample = _core.Codecs.Animation.ReadGeometry(bytes, Peer.Session, Peer.Generation, cadence);
        // Recognized current-format output can still be in flight when its installation retires.
        if (cadence.Value < active.Revision.Value || sample.World.Value < active.World.WorldGeneration.Value ||
            (sample.World == active.World.WorldGeneration && sample.Generation < active.World.Epoch.Value)) return;
        if (cadence != active.Revision) throw new ArgumentException("Future geometry cadence.");
        if (sample.World != active.World.WorldGeneration || sample.Generation != active.World.Epoch.Value ||
            !_mechanicalTargets.Contains(sample.Geometry.Target) ||
            !Clock.TryMasterNow(_transport.EventTime(_id.Value), out var now) ||
            sample.AppliedAt.Value < active.InstalledAt.Value || sample.AppliedAt.Value > now.Upper ||
            sample.Pulse.Value < active.FirstAnimation.Value ||
            _core.Scheduling.Deadline(active.Settings.AnimationRate, sample.Pulse).Value > sample.AppliedAt.Value ||
            sample.Geometry.Tick.Value < active.World.AnchorTick.Value ||
            sample.Geometry.Tick.Value > _core.Scheduling.DueTick(active.World, active.Settings, sample.AppliedAt).Value ||
            (_mechanicalSamples.TryGetValue(sample.Geometry.Target, out var previous) &&
                (sample.Pulse.Value <= previous.Pulse.Value || sample.Geometry.Tick.Value < previous.Geometry.Tick.Value)))
            throw new ArgumentException("Foreign or reversed geometry sample.");
        _mechanicalSamples[sample.Geometry.Target] = sample;
    }

    public IReadOnlyList<MechanicalLinkGeometry> MechanicalFrames(ulong frame, WorkshopPresentationSample physical)
    {
        _mechanicalFrame.Clear();
        if (!AdmitPresentationFrame(frame) || _schedule is not { } active) return _mechanicalFrame;
        foreach (var sample in _mechanicalSamples.Values)
            if (sample.World == active.World.WorldGeneration && sample.Generation == active.World.Epoch.Value &&
                sample.AppliedAt.Value <= _frameMaster.Value &&
                new PhysicalTime(checked(sample.Geometry.Tick.Value * active.Settings.PhysicalStepsPerCommit)) <= physical.SimulationTime.Time)
                _mechanicalFrame.Add(sample.Geometry);
        return _mechanicalFrame;
    }

    private readonly Dictionary<AnimationTargetId, (GpuBodyId Owner, int Binding)> _declaredTargets = new(16);
    private readonly Dictionary<AnimationTargetId, WorkshopAnimationSample> _declaredSamples = new(16);
    private readonly List<DeclaredPresentationSample> _declaredFrame = new(16);
    private readonly Dictionary<GpuBodyId, bool> _declaredVisibility = new(16);
    private readonly Dictionary<AnimationTargetId, bool> _declaredVisibilitySent = new(16);

    public void SetDeclaredVisible(GpuBodyId owner, bool visible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed();
        _declaredVisibility[owner] = visible;
        if (!visible)
            foreach (var target in _declaredTargets)
                if (target.Value.Owner == owner) _declaredSamples.Remove(target.Key);
        PumpDeclaredVisibility();
    }

    private void PumpDeclaredVisibility()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _readConstruction is not { } construction) return;
        foreach (var target in _declaredTargets)
        {
            var visible = !_declaredVisibility.TryGetValue(target.Value.Owner, out var requested) || requested;
            if (_declaredVisibilitySent.TryGetValue(target.Key, out var sent) ? sent == visible : visible) continue;
            foreach (var instance in construction.Instances)
                if (instance.Id == target.Value.Owner)
                {
                    var declaration = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings[target.Value.Binding];
                    SendAnimation(new(target.Key, schedule.World.WorldGeneration, checked(_animationSequence + 1),
                        schedule.World.Epoch.Value, AnimationControlKind.Visibility, visible,
                        CuriousContraptions.Abstractions.PresentationDriverRules.InitialValue(declaration),
                        CuriousContraptions.Abstractions.PresentationDriverRules.InitialValue(declaration), 1, AnimationCurve.Linear,
                        Property: _core.Presentation.PropertyFor(declaration.Property)), schedule);
                    foreach (var owned in _declaredTargets)
                        if (owned.Value.Owner == target.Value.Owner) _declaredVisibilitySent[owned.Key] = visible;
                    return;
                }
        }
    }

    private void RetryDeclaredVisibility(WorkshopAnimationControl command)
    {
        if (command.Target.Value >= _core.Presentation.TargetBase)
            if (_declaredTargets.TryGetValue(command.Target, out var target))
                foreach (var owned in _declaredTargets)
                    if (owned.Value.Owner == target.Owner) _declaredVisibilitySent.Remove(owned.Key);
    }

    private void InstallDeclaredTargets(ICompiledScene construction)
    {
        _mechanicalTargets.Clear(); _mechanicalSamples.Clear(); _mechanicalFrame.Clear();
        foreach (var link in construction.MechanicalLinks) _mechanicalTargets.Add(link.Target);
        _declaredTargets.Clear(); _declaredSamples.Clear(); _declaredVisibility.Clear(); _declaredVisibilitySent.Clear();
        foreach (var target in _core.Presentation.Targets(construction))
            _declaredTargets.Add(target.Target, (target.Owner, target.Binding));
    }

    private void ReceiveDeclaredSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value || sample.Generation < active.World.Epoch.Value) return;
        if (sample.World != active.World.WorldGeneration ||
            sample.Generation != active.World.Epoch.Value || !_declaredTargets.TryGetValue(sample.Target, out var binding))
            throw new ArgumentException("Foreign declared presentation sample.");
        if (_readConstruction is not { } construction) throw new ArgumentException("Missing declared presentation construction.");
        var found = false;
        foreach (var instance in construction.Instances)
            if (instance.Id == binding.Owner)
            {
                var declaration = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings[binding.Binding];
                if (sample.Property != _core.Presentation.PropertyFor(declaration.Property))
                    throw new ArgumentException("Declared presentation property changed.");
                found = true; break;
            }
        if (!found) throw new ArgumentException("Declared presentation owner disappeared.");
        if (kind == AnimationOutputKind.Acknowledgement) return;
        if (kind != AnimationOutputKind.Sample) throw new ArgumentException("Unsupported declared output.");
        if (_declaredSamples.TryGetValue(sample.Target, out var previous) &&
            previous.Generation == sample.Generation && sample.Pulse.Value <= previous.Pulse.Value)
            throw new ArgumentException("Declared presentation publication reversed.");
        _declaredSamples[sample.Target] = sample;
    }

    public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner,
        out IReadOnlyList<DeclaredPresentationSample> samples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ThrowIfTransportFailed();
        _declaredFrame.Clear(); samples = _declaredFrame;
        if (!AdmitPresentationFrame(frame) || _schedule is not { } active) return false;
        foreach (var item in _declaredSamples)
        {
            var binding = _declaredTargets[item.Key]; var sample = item.Value;
            if (binding.Owner != owner || sample.World != active.World.WorldGeneration ||
                sample.Generation != active.World.Epoch.Value || sample.AppliedAt.Value > _frameMaster.Value ||
                (_declaredVisibility.TryGetValue(owner, out var visible) && !visible)) continue;
            _declaredFrame.Add(new(item.Key, owner, binding.Binding, _core.Codecs.Animation.Value(sample.Property, sample.Value)));
        }
        return _declaredFrame.Count > 0;
    }
}
