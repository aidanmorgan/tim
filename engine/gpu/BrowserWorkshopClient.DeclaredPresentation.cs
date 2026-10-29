using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
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
                        declaration.From, declaration.From, 1, AnimationCurve.Linear,
                        Property: WorkshopDeclaredPresentation.Property(declaration.Property)), schedule);
                    foreach (var owned in _declaredTargets)
                        if (owned.Value.Owner == target.Value.Owner) _declaredVisibilitySent[owned.Key] = visible;
                    return;
                }
        }
    }

    private void RetryDeclaredVisibility(WorkshopAnimationControl command)
    {
        if (command.Target.Value >= WorkshopDeclaredPresentation.TargetBase)
            if (_declaredTargets.TryGetValue(command.Target, out var target))
                foreach (var owned in _declaredTargets)
                    if (owned.Value.Owner == target.Owner) _declaredVisibilitySent.Remove(owned.Key);
    }

    private void InstallDeclaredTargets(WorkshopCompiledScene construction)
    {
        _declaredTargets.Clear(); _declaredSamples.Clear(); _declaredVisibility.Clear(); _declaredVisibilitySent.Clear();
        foreach (var target in WorkshopDeclaredPresentation.Targets(construction))
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
                if (sample.Property != WorkshopDeclaredPresentation.Property(declaration.Property))
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
            _declaredFrame.Add(new(item.Key, owner, binding.Binding, WorkshopAnimationWire.Value(sample.Property, sample.Value)));
        }
        return _declaredFrame.Count > 0;
    }
}

