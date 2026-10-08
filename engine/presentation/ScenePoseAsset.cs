using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Presentation;

/// <summary>Explicit scene reference resolved once to a committed body identity.</summary>
public readonly record struct ScenePoseReference(SceneBodyKey Body,RigidPose Offset);

/// <summary>The declaring body slot supplies the source identity. Each asset names
/// its reference body and local frame; no live body or presentation callback is retained.</summary>
public enum PoseConstructionPolicy { Mapped, PreserveExact }
public sealed record ScenePoseAsset(Node3D Target,ScenePoseReference Reference,ScenePoseMap Map,
    PoseConstructionPolicy Construction=PoseConstructionPolicy.Mapped);
