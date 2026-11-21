using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Animation-owned phase over committed rate intervals. Sampling never predicts beyond their endpoint.</summary>
internal sealed class CommittedRatePhase(PhysicalTime initial)
{
    internal readonly record struct Segment(PhysicalTime Start, PhysicalTime End, float Phase, float Rate, float EndPhase);
    private readonly List<Segment> _segments = [];
    private PhysicalTime _committed = initial;
    private float _phase, _sampledPhase;

    /// <summary>Stage one complete committed interval. Only capacity may change before the enclosing read is accepted.</summary>
    public Segment Prepare(PhysicalTime at, float rate)
    {
        if (at < _committed || !float.IsFinite(rate)) throw new ArgumentException("Invalid committed rate interval.");
        var segment = new Segment(_committed, at, _phase, rate, At(_phase, _committed, at, rate));
        if (at != _committed)
        {
            if (_segments.Count == Array.MaxLength) throw new AnimationStorageException();
            try { _segments.EnsureCapacity(checked(_segments.Count + 1)); }
            catch (OutOfMemoryException) { throw new AnimationStorageException(); }
        }
        return segment;
    }

    public void Commit(Segment segment)
    {
        if (segment.Start != segment.End) _segments.Add(segment);
        _committed = segment.End;
        _phase = segment.EndPhase;
    }

    public float Sample(PhysicalTime at)
    {
        var consumed = 0;
        while (consumed < _segments.Count && _segments[consumed].End <= at)
            _sampledPhase = _segments[consumed++].EndPhase;
        if (consumed != 0) _segments.RemoveRange(0, consumed);
        if (_segments.Count == 0 || at <= _segments[0].Start) return _sampledPhase;
        var segment = _segments[0];
        return At(segment.Phase, segment.Start, at, segment.Rate);
    }

    private static float At(float phase, PhysicalTime start, PhysicalTime end, float rate) =>
        (phase + MathF.Tau * end.PeriodicPhaseSince(start, MathF.Tau, rate)) % MathF.Tau;
}
