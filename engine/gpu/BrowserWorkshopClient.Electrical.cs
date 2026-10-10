using System;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public sealed partial class BrowserWorkshopClient
{
    private const int IndicatorsPerSource = 5;
    private const ulong ElectricalTargetBase = 4UL << 32;
    private readonly WorkshopAnimationControl?[] _electricalRequested = new WorkshopAnimationControl?[ElectricalSupplyPlan.SourceCapacity * IndicatorsPerSource];
    private readonly WorkshopAnimationSample?[] _electricalSamples = new WorkshopAnimationSample?[ElectricalSupplyPlan.SourceCapacity * IndicatorsPerSource];
    private readonly bool[] _electricalRetry = new bool[ElectricalSupplyPlan.SourceCapacity * IndicatorsPerSource];
    private readonly ulong[] _electricalOrdinals = new ulong[ElectricalSupplyPlan.SourceCapacity * IndicatorsPerSource];
    private static bool IsElectricalTarget(AnimationTargetId target) =>
        target.Value >= ElectricalTargetBase && target.Value < ElectricalTargetBase + ElectricalSupplyPlan.SourceCapacity * IndicatorsPerSource;
    private void RetireElectricalFeedback()
    { Array.Clear(_electricalRequested); Array.Clear(_electricalSamples); Array.Clear(_electricalOrdinals); Array.Clear(_electricalRetry); }

    private void PumpElectricalFeedback()
    {
        if (_animationPending is not null || _preparation is not null || _schedule is not { } schedule ||
            _history.Latest is not { } latest || latest.Read.Epoch != schedule.World.WorldGeneration ||
            _readConstruction is not { } construction) return;
        for (var source = 0; source < latest.Read.Electrical.Count; source++)
        {
            var read = latest.Read.Electrical[source];
            float capacity = 0;
            foreach (var instance in construction.Instances)
                if (instance is WorkshopBattery battery && battery.Id == read.Owner) capacity = battery.Settings.Capacity.Value;
            if (capacity <= 0) throw new ArgumentException("Electrical feedback has no declared capacity.");
            for (var indicator = 0; indicator < IndicatorsPerSource; indicator++)
            {
                var on = indicator == 0 ? read.Available : read.Remaining.Value >= capacity * (indicator * .25f);
                var to = on ? (Half)1 : (Half)0;
                var slot = source * IndicatorsPerSource + indicator;
                if (_electricalRequested[slot] is { } previous && previous.To == to && !_electricalRetry[slot]) continue;
                var generation = (_electricalRequested[slot]?.Generation ?? 0) + 1;
                var ordinal = latest.Read.Motion?.LastOrdinal ?? 0;
                var control = new WorkshopAnimationControl(new(ElectricalTargetBase + (ulong)slot), latest.Read.Epoch,
                    checked(_animationSequence + 1), generation, AnimationControlKind.Transition, true, _electricalSamples[slot]?.Value ?? to, to,
                    (Half).1f, AnimationCurve.SmoothStep, ordinal, default, AnimationProperty.ColourBlend);
                SendAnimation(control, schedule);
                _electricalRequested[slot] = control;
                _electricalRetry[slot] = false;
                return;
            }
        }
    }

    private void RetryElectricalFeedback(WorkshopAnimationControl rejected)
    {
        if (IsElectricalTarget(rejected.Target)) _electricalRetry[(int)(rejected.Target.Value - ElectricalTargetBase)] = true;
    }
    private void ReceiveElectricalSample(WorkshopAnimationSample sample, AnimationOutputKind kind, WorkshopSchedule active)
    {
        if (sample.World.Value < active.World.WorldGeneration.Value) return;
        var slot = (int)(sample.Target.Value - ElectricalTargetBase);
        if (_electricalRequested[slot] is not { } control) throw new ArgumentException("Unowned electrical animation.");
        if (sample.Generation < control.Generation) return;
        if (sample.World != active.World.WorldGeneration || sample.Generation != control.Generation ||
            sample.Property != AnimationProperty.ColourBlend || sample.EventOrdinal != control.EventOrdinal ||
            !HalfBits.Equal(sample.EventPhase, control.EventPhase))
            throw new ArgumentException("Electrical animation observation changed.");
        if (kind == AnimationOutputKind.Sample)
        {
            if (sample.Pulse.Value <= _electricalOrdinals[slot]) throw new ArgumentException("Electrical animation reversed.");
            _electricalOrdinals[slot] = sample.Pulse.Value;
        }
        _electricalSamples[slot] = sample;
    }

    public bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner,
        out ElectricalIndicatorSample sample)
    {
        sample = default;
        PumpCosmetics();
        if (!AdmitPresentationFrame(frame) || physical.Evidence.WorldEpoch != Epoch || _history.Latest is not { } latest) return false;
        for (var source = 0; source < latest.Read.Electrical.Count; source++)
        {
            if (latest.Read.Electrical[source].Owner != owner) continue;
            Span<float> values = stackalloc float[IndicatorsPerSource];
            for (var indicator = 0; indicator < IndicatorsPerSource; indicator++)
            {
                var slot = source * IndicatorsPerSource + indicator;
                if (_electricalSamples[slot] is not { } value || value.World != Epoch ||
                    value.AppliedAt.Value > _frameMaster.Value) return false;
                values[indicator] = (float)value.Value;
            }
            sample = new(values[0], values[1], values[2], values[3], values[4]);
            _lastPresentationApplied = _frameMaster; return true;
        }
        return false;
    }
}
