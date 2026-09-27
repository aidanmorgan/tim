using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Part-local, data-authored assistance. Authored tolerances bound automatic, smoothly animated corrections.</summary>
public static class PartAssistance
{
    public static PartDifficulty Evaluate(IReadOnlyList<PartDifficulty> curve, float precision)
    {
        if (curve.Count == 0) return new() { Precision = precision };
        var ordered = curve.OrderBy(k => k.Precision).ToArray();
        precision = Mathf.Clamp(precision, 0, 1);
        var lower = ordered[0];
        var upper = ordered[^1];
        foreach (var knot in ordered)
        {
            if (knot.Precision <= precision) lower = knot;
            if (knot.Precision >= precision) { upper = knot; break; }
        }
        var weight = upper.Precision > lower.Precision
            ? Mathf.Clamp((precision - lower.Precision) / (upper.Precision - lower.Precision), 0, 1) : 0;
        float Mix(float a, float b) => Mathf.Lerp(a, b, weight);
        return new()
        {
            Precision = precision,
            PositionWindow = Mix(lower.PositionWindow, upper.PositionWindow),
            RotationWindow = Mix(lower.RotationWindow, upper.RotationWindow),
            MaxPositionCorrection = Mix(lower.MaxPositionCorrection, upper.MaxPositionCorrection),
            MaxRotationCorrection = Mix(lower.MaxRotationCorrection, upper.MaxRotationCorrection),
            BlendSeconds = Mix(lower.BlendSeconds, upper.BlendSeconds),
            CaptureMargin = Mix(lower.CaptureMargin, upper.CaptureMargin),
            CaptureSpeed = Mix(lower.CaptureSpeed, upper.CaptureSpeed),
            CaptureDwell = Mix(lower.CaptureDwell, upper.CaptureDwell),
            GuideAcceleration = Mix(lower.GuideAcceleration, upper.GuideAcceleration),
            TriggerThreshold = Mix(lower.TriggerThreshold, upper.TriggerThreshold)
        };
    }

    private static Vector3 Point(float[] values) => new(values[0], values[1], values[2]);
    private static Vector3 AngleDelta(Vector3 from, Vector3 to) => new(
        Mathf.Wrap(to.X - from.X, -180, 180), Mathf.Wrap(to.Y - from.Y, -180, 180),
        Mathf.Wrap(to.Z - from.Z, -180, 180));

    public sealed class Correction(MachinePart part, Vector3 endPosition, Quaternion endRotation, float seconds)
    {
        private readonly Vector3 _startPosition = part.Position;
        private readonly Quaternion _startRotation = part.Quaternion;
        public MachinePart Part { get; } = part;
        public Vector3 EndPosition { get; } = endPosition;
        public float Duration { get; } = Mathf.Max(.1f, seconds);

        public void Apply(float elapsed)
        {
            var t = Mathf.Clamp(elapsed / Duration, 0, 1);
            // Quintic easing: continuous position, velocity and acceleration at both ends.
            var blend = t * t * t * (10 + t * (-15 + 6 * t));
            Part.Position = _startPosition.Lerp(EndPosition, blend);
            Part.Quaternion = _startRotation.Slerp(endRotation, blend);
        }
    }

    // Assign authored slots one-to-one. Nearest matches win; stable IDs break ties.
    // Assistance is computed once at run start, never accumulated across ticks or resets.
    public static List<Correction> Prepare(IEnumerable<MachinePart> parts, IReadOnlyList<PartSpec> targets, float precision)
    {
        var candidates = from part in parts
                         where !part.Locked && !part.Dynamic
                         from target in targets
                         where target.Kind == part.Definition.Id && !target.Locked
                         let score = part.Position.DistanceTo(Point(target.Position)) +
                             AngleDelta(part.RotationDegrees, Point(target.Rotation)).Length() / 90
                         select (part, target, score);
        var assignedParts = new HashSet<string>(StringComparer.Ordinal);
        var assignedTargets = new HashSet<string>(StringComparer.Ordinal);
        var corrections = new List<Correction>();
        foreach (var (part, target, _) in candidates.OrderBy(c => c.score)
                     .ThenBy(c => c.part.Uid, StringComparer.Ordinal).ThenBy(c => c.target.Id, StringComparer.Ordinal))
        {
            if (assignedParts.Contains(part.Uid) || assignedTargets.Contains(target.Id)) continue;
            assignedParts.Add(part.Uid);
            assignedTargets.Add(target.Id);
            part.SetDifficulty(target.Difficulty);
            var settings = Evaluate(target.Difficulty, precision);
            var offset = Point(target.Position) - part.Position;
            var angle = AngleDelta(part.RotationDegrees, Point(target.Rotation));
            if (offset.Length() > settings.PositionWindow || angle.Length() > settings.RotationWindow) continue;
            offset = offset.LimitLength(Mathf.Max(0, settings.MaxPositionCorrection));
            angle = angle.LimitLength(Mathf.Max(0, settings.MaxRotationCorrection));
            if (offset.LengthSquared() < .00000001f && angle.LengthSquared() < .00000001f) continue;
            var endRotation = Quaternion.FromEuler((part.RotationDegrees + angle) * (Mathf.Pi / 180));
            corrections.Add(new(part, part.Position + offset, endRotation, settings.BlendSeconds));
        }
        return corrections;
    }
}
