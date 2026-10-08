using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Presentation;

public enum PoseReadSpace { World, Reference, Relative }
public enum PoseMapKind { Rigid, AxisAffine }
public enum PoseMapAxis { X, Y, Z }

/// <summary>Immutable presentation mapping, never a physical law. Affine coefficients
/// map one selected local coordinate into mesh translation and dimensionless scale.</summary>
public sealed record ScenePoseMap
{
    private (RigidPose Pose, Transform3D Transform)? Construction { get; init; }
    public PoseReadSpace Space { get; }
    public PoseMapKind Kind { get; }
    private readonly RigidPose _offset;
    private readonly PoseMapAxis _axis;
    private readonly RigidRotation _rotation;
    private readonly CollisionVector _positionOffset, _positionGain, _scaleOffset, _scaleGain;

    private ScenePoseMap(PoseReadSpace space, RigidPose offset)
    {
        ValidateSpace(space);
        ValidatePose(offset);
        Space = space; Kind = PoseMapKind.Rigid; _offset = offset;
    }
    private ScenePoseMap(PoseReadSpace space, PoseMapAxis axis, RigidRotation rotation,
        CollisionVector positionOffset, CollisionVector positionGain,
        CollisionVector scaleOffset, CollisionVector scaleGain)
    {
        ValidateSpace(space);
        if (!Enum.IsDefined(axis)) throw new ArgumentOutOfRangeException(nameof(axis));
        if (!rotation.IsValid || !positionOffset.IsFinite || !positionGain.IsFinite ||
            !scaleOffset.IsFinite || !scaleGain.IsFinite)
            throw new ArgumentException("Affine presentation coefficients must be finite with a valid rotation.");
        Space = space; Kind = PoseMapKind.AxisAffine; _axis = axis; _rotation = rotation;
        _positionOffset = positionOffset; _positionGain = positionGain;
        _scaleOffset = scaleOffset; _scaleGain = scaleGain;
    }
    public static ScenePoseMap Rigid(PoseReadSpace space, RigidPose offset) => new(space, offset);
    public static ScenePoseMap AxisAffine(PoseReadSpace space, PoseMapAxis axis, RigidRotation rotation,
        CollisionVector positionOffset, CollisionVector positionGain,
        CollisionVector scaleOffset, CollisionVector scaleGain) =>
        new(space, axis, rotation, positionOffset, positionGain, scaleOffset, scaleGain);

    private RigidPose ReadPose(BodyPoseRead read) => Space switch
        {
            PoseReadSpace.World => read.Pose,
            PoseReadSpace.Reference => read.ReferencePose,
            PoseReadSpace.Relative => read.RelativePose,
            _ => throw new InvalidOperationException("Unsupported pose space.")
        };
    public ScenePoseMap PreserveConstruction(BodyPoseRead read, Transform3D transform)
    {
        ValidateRead(read);
        SceneAnimationAdapter.ValidateTransform(transform);
        return this with { Construction=(ReadPose(read),transform) };
    }
    public Transform3D Evaluate(BodyPoseRead read)
    {
        ValidateRead(read);
        var pose=ReadPose(read);
        if(Construction is { } initial && pose==initial.Pose)return initial.Transform;
        Transform3D result;
        switch (Kind)
        {
            case PoseMapKind.Rigid:
                result = pose.Compose(_offset).ToScene();
                break;
            case PoseMapKind.AxisAffine:
                var coordinate = _axis switch
                {
                    PoseMapAxis.X => pose.Center.X,
                    PoseMapAxis.Y => pose.Center.Y,
                    PoseMapAxis.Z => pose.Center.Z,
                    _ => throw new InvalidOperationException("Unsupported pose coordinate.")
                };
                var position = _positionOffset + _positionGain * coordinate;
                var scale = _scaleOffset + _scaleGain * coordinate;
                if (!position.IsFinite || !scale.IsFinite)
                    throw new ArgumentException("Affine presentation exceeds its numeric range.");
                var basis = new RigidPose(default, _rotation).ToScene().Basis;
                basis.X *= (float)scale.X; basis.Y *= (float)scale.Y; basis.Z *= (float)scale.Z;
                result = new(basis, new((float)position.X, (float)position.Y, (float)position.Z));
                break;
            default: throw new InvalidOperationException("Unsupported pose mapping.");
        }
        SceneAnimationAdapter.ValidateTransform(result);
        return result;
    }
    internal static void ValidateRead(BodyPoseRead read)
    {
        if (!Enum.IsDefined(read.MotionType)) throw new ArgumentException("Unsupported motion kind.");
        ValidatePose(read.Pose); ValidatePose(read.ReferencePose);
    }
    private static void ValidatePose(RigidPose pose)
    {
        if (!pose.Center.IsFinite || !pose.Rotation.IsValid)
            throw new ArgumentException("Presentation pose must be finite with a valid rotation.");
    }
    private static void ValidateSpace(PoseReadSpace space)
    {
        if (!Enum.IsDefined(space)) throw new ArgumentOutOfRangeException(nameof(space));
    }
}

public sealed record ScenePoseTarget(SceneAnimationTargetHandle Target, PhysicsBodyId Body, PoseReferenceBinding Reference, ScenePoseMap Map);
public readonly record struct ScenePoseBatchWork(int Reads, int QueuedTargets);

/// <summary>Fixed-topology read-to-target batch. The caller supplies one complete pose
/// set; no mutation reaches the adapter until every mapping and target validates.</summary>
public sealed class ScenePoseBatch
{
    private readonly SceneAnimationAdapter _adapter;
    private readonly Dictionary<PhysicsBodyId, int> _bodyIndices;
    private readonly BodyPoseRead[] _reads;
    private readonly bool[] _seen;
    private readonly ScenePoseTarget[] _targets;
    private readonly int[] _sourceIndices, _referenceIndices;
    private readonly Transform3D[] _outputs;

    public ScenePoseBatch(SceneAnimationAdapter adapter, IReadOnlyList<PhysicsBodyId> bodies,
        IReadOnlyList<ScenePoseTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(targets);
        _adapter = adapter;
        _bodyIndices = new(bodies.Count);
        for (var i = 0; i < bodies.Count; i++)
            if (!_bodyIndices.TryAdd(bodies[i], i)) throw new ArgumentException("Duplicate pose body identity.");
        _reads = new BodyPoseRead[bodies.Count]; _seen = new bool[bodies.Count];
        _targets = new ScenePoseTarget[targets.Count];
        _sourceIndices = new int[targets.Count]; _referenceIndices = new int[targets.Count]; _outputs = new Transform3D[targets.Count];
        var writers = new HashSet<SceneAnimationTargetHandle>();
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(target.Map);
            if (!writers.Add(target.Target)) throw new ArgumentException("Duplicate physical target writer.");
            if (!_bodyIndices.TryGetValue(target.Body, out var index))
                throw new ArgumentException("Target refers to an undeclared pose body.");
            target.Reference.Validate();
            if(target.Reference.Kind==PoseReferenceKind.Body &&
                !_bodyIndices.TryGetValue(target.Reference.Body,out _referenceIndices[i]))
                throw new ArgumentException("Target refers to an undeclared reference body.");
            adapter.ValidatePhysicalPose(target.Target, Transform3D.Identity);
            _targets[i] = target; _sourceIndices[i] = index;
        }
    }

    public ScenePoseBatchWork Queue(ReadOnlySpan<BodyPoseRead> reads)
    {
        if (reads.Length != _reads.Length) throw new ArgumentException("Pose batch must contain the declared body set.");
        Array.Clear(_seen);
        foreach (var read in reads)
        {
            ScenePoseMap.ValidateRead(read);
            if (!_bodyIndices.TryGetValue(read.Id, out var index) || _seen[index])
                throw new ArgumentException("Pose batch has an unknown or repeated body.");
            _seen[index] = true; _reads[index] = read;
        }
        for (var i = 0; i < _targets.Length; i++)
        {
            var target = _targets[i];
            var source=_reads[_sourceIndices[i]];
            var reference=target.Reference.Resolve(target.Reference.Kind==PoseReferenceKind.Body
                ?_reads[_referenceIndices[i]].Pose:RigidPose.Identity);
            var output = target.Map.Evaluate(new(source.Id,source.MotionType,source.Pose,reference));
            _adapter.ValidatePhysicalPose(target.Target, output);
            _outputs[i] = output;
        }
        for (var i = 0; i < _targets.Length; i++)
            _adapter.QueuePhysicalPose(_targets[i].Target, _outputs[i]);
        return new(reads.Length, _targets.Length);
    }
}
