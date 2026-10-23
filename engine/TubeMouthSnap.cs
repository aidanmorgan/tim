using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions;

/// <summary>The part being placed or moved. `Body` is its identity: a moved part's own committed pose is excluded from the query.
/// `Length` is the straight pipe's declared length (Story 9R.7): required for a pipe, absent for every other part.</summary>
public readonly record struct TubeMouthMover(PartId Part, GpuBodyId Body, bool Locked, PipeLength? Length = null);

/// <summary>A proposed join: the mover's `MoverMouth` meets the target's `TargetMouth` at `Joint` (the target mouth, facing `Axis`)
/// when the mover takes the pose `Position` / `Rotation`.</summary>
public readonly record struct TubeMouthSnapResult(GpuBodyId TargetBody, TubeMouthEnd TargetMouth, TubeMouthEnd MoverMouth,
    Vector3 Position, Quaternion Rotation, Vector3 Joint, Vector3 Axis);

/// <summary>Story 9R.6: typed tube-mouth snapping, a pure main-thread query (never a worker, never a command). Mouths are scene data:
/// each `Loc_Port_mouth_*` locator's position, outward local +X and bore. A part is a tube exactly when its definition declares
/// mouths; there is no per-element table. The query creates no network edge and mutates nothing.</summary>
public static class TubeMouthSnap
{
    /// <summary>DESIGN.md and CAT-048 facts 9–11: a mouth within 0.45 m and 20° of a free compatible mouth snaps.</summary>
    public const float CaptureDistance = .45f;
    public static readonly float CaptureAngle = Mathf.DegToRad(20);
    /// <summary>Bores are compatible when equal within 1e-6 m.</summary>
    public const float BoreTolerance = 1e-6f;
    /// <summary>A mouth is occupied when another placed mouth coincides with it within 1e-4 m and faces opposite.</summary>
    public const float JoinedDistance = 1e-4f;
    public const float JoinedDot = -.9999f;

    private readonly record struct WorldMouth(TubeMouthEnd End, Vector3 Position, Vector3 Outward, float Bore);

    private readonly record struct Candidate(float Distance, float Angle, GpuBodyId Target, TubeMouthEnd TargetMouth,
        TubeMouthEnd MoverMouth, Vector3 TargetPosition, Vector3 TargetOutward, Vector3 MoverOutward, Vector3 MoverLocal);

    public static TubeMouthSnapResult? Find(IReadOnlyDictionary<PartId, PartDefinition> definitions, IReadOnlyList<IWorkshopInstance> placed,
        TubeMouthMover mover, Vector3 position, Quaternion rotation)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(placed);
        if ((mover.Part == PartId.Pipe) != mover.Length.HasValue)
            throw new ArgumentException("A pipe mover declares its length; no other part has one.");
        if (mover.Locked || !definitions.TryGetValue(mover.Part, out var moverDefinition)) return null;
        var moverMouths = Mouths(moverDefinition, mover.Length);
        if (moverMouths.Count == 0) return null;
        rotation = rotation.Normalized();

        // Every other placed part's mouths in world space; the mover's own committed pose takes no part.
        var others = new List<(GpuBodyId Body, WorldMouth Mouth)>();
        foreach (var instance in placed)
        {
            if (instance.Id == mover.Body || !definitions.TryGetValue(instance.PartId, out var definition)) continue;
            var (at, turn) = Pose(instance);
            var length = instance is WorkshopPipe pipe ? pipe.Length : (PipeLength?)null;
            foreach (var mouth in Mouths(definition, length))
                others.Add((instance.Id, new(mouth.Mouth!.Value, at + turn * mouth.Position, Outward(turn * mouth.Rotation), mouth.BoreRadius!.Value)));
        }

        Candidate? best = null;
        foreach (var (body, target) in others)
        {
            if (Occupied(others, body, target)) continue;
            foreach (var mouth in moverMouths)
            {
                if (MathF.Abs(mouth.BoreRadius!.Value - target.Bore) > BoreTolerance) continue;
                var moverLocal = mouth.Position;
                var moverAt = position + rotation * moverLocal;
                var moverOutward = Outward(rotation * mouth.Rotation);
                var distance = moverAt.DistanceTo(target.Position);
                var angle = moverOutward.AngleTo(-target.Outward);
                if (distance > CaptureDistance || angle > CaptureAngle) continue;
                var candidate = new Candidate(distance, angle, body, target.End, mouth.Mouth!.Value, target.Position, target.Outward,
                    moverOutward, moverLocal);
                if (best is not { } current || Precedes(candidate, current)) best = candidate;
            }
        }
        if (best is not { } chosen) return null;

        // The shortest arc turns the mover's mouth to face the target's: it removes only the misalignment and keeps the roll.
        var aligned = (new Quaternion(chosen.MoverOutward, -chosen.TargetOutward) * rotation).Normalized();
        return new(chosen.Target, chosen.TargetMouth, chosen.MoverMouth, chosen.TargetPosition - aligned * chosen.MoverLocal, aligned,
            chosen.TargetPosition, chosen.TargetOutward);
    }

    private static bool Occupied(List<(GpuBodyId Body, WorldMouth Mouth)> mouths, GpuBodyId owner, WorldMouth mouth)
    {
        foreach (var (body, other) in mouths)
            if (body != owner && other.Position.DistanceTo(mouth.Position) <= JoinedDistance && other.Outward.Dot(mouth.Outward) < JoinedDot)
                return true;
        return false;
    }

    /// <summary>D5: the nearest wins, then the smaller angle, then the lower part id, then start before end (target, then mover).</summary>
    private static bool Precedes(Candidate a, Candidate b) =>
        a.Distance != b.Distance ? a.Distance < b.Distance
        : a.Angle != b.Angle ? a.Angle < b.Angle
        : a.Target != b.Target ? a.Target.Value < b.Target.Value
        : a.TargetMouth != b.TargetMouth ? a.TargetMouth < b.TargetMouth
        : a.MoverMouth < b.MoverMouth;

    /// <summary>A mouth's part-local position. The scene declares the pipe's mouths at the collar faces of its default length; a pipe
    /// of another length keeps their axis and moves them to ±(length/2 + 0.09) (CAT-048 §2).</summary>
    public static Vector3 LocalMouth(PartDefinition definition, TubeMouthEnd end, PipeLength? length)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if ((definition.PartId == PartId.Pipe) != length.HasValue)
            throw new ArgumentException("A pipe mouth needs its length; no other part has one.");
        foreach (var mouth in Mouths(definition, length))
            if (mouth.Mouth == end) return mouth.Position;
        throw new ArgumentException($"Part '{definition.PartId}' declares no {end} mouth.");
    }

    private static List<ExtractedPortLocator> Mouths(PartDefinition definition, PipeLength? length)
    {
        var mouths = new List<ExtractedPortLocator>(2);
        foreach (var locator in definition.Mouths(length).Values)
            if (locator.Mouth is not null) mouths.Add(locator);
        return mouths;
    }

    private static Vector3 Outward(Quaternion rotation) => (rotation * Vector3.Right).Normalized();

    /// <summary>The committed pose of a placed part: (cell + local) / 16 and its canonical f32 rotation.</summary>
    internal static (Vector3 Position, Quaternion Rotation) Pose(IWorkshopInstance instance) =>
        (new((float)((instance.Cell.X + (double)instance.Local.X) / 16), (float)((instance.Cell.Y + (double)instance.Local.Y) / 16),
            (float)((instance.Cell.Z + (double)instance.Local.Z) / 16)),
         new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W).Normalized());
}
