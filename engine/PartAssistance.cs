using Godot;
using CuriousContraptions.Physics;
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
    private static CollisionVector RotationDelta(MachinePart part,PartSpec target)
    {
        var from=SceneGeometryAdapter.CaptureRigidPose(part.Transform).Rotation;
        var to=SceneGeometryAdapter.CaptureRigidPose(new(SceneOrientation.Present(target.Orientation),Vector3.Zero)).Rotation;
        return (to*from.Inverse()).RotationVector();
    }

    public sealed class Correction
    {
        public MachinePart Part { get; }
        public QuinticRigidTrajectory Path { get; }

        public Correction(MachinePart part,Vector3 endPosition,RigidRotation endRotation,float seconds)
        {
            ArgumentNullException.ThrowIfNull(part);
            var start=SceneGeometryAdapter.CaptureRigidPose(part.Transform);
            Part=part;
            Path=new(start,SceneGeometryAdapter.CaptureVector(endPosition)-start.Center,
                (endRotation*start.Rotation.Inverse()).RotationVector(),Math.Max(.1,seconds));
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
                             RotationDelta(part,target).Length / (Math.PI/2)
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
            var angle = RotationDelta(part,target);
            if (offset.Length() > settings.PositionWindow || angle.Length > settings.RotationWindow*Math.PI/180) continue;
            offset = offset.LimitLength(Mathf.Max(0, settings.MaxPositionCorrection));
            var maximumAngle = Math.Max(0, settings.MaxRotationCorrection)*Math.PI/180;
            if(angle.Length>maximumAngle) angle*=maximumAngle/angle.Length;
            if (offset.LengthSquared() < .00000001f && angle.LengthSquared < 1e-12) continue;
            var endRotation = RigidRotation.FromRotationVector(angle)*SceneGeometryAdapter.CaptureRigidPose(part.Transform).Rotation;
            corrections.Add(new(part, part.Position + offset, endRotation, settings.BlendSeconds));
        }
        return corrections;
    }
}
