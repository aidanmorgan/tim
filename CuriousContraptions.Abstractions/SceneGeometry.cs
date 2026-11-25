using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions;

/// <summary>Geometry ownership in the authored scene, in package body order: entry zero owns the base body, then one entry per slider
/// body, then one per hinge body. The base root is "."; moving roots are disjoint subtrees excluded from the base.</summary>
public readonly record struct SceneBodyGeometry(string Root, MaterialProperties Material);

public interface ISceneGeometryElement
{
    IReadOnlyList<SceneBodyGeometry> SceneBodies { get; }
}

/// <summary>A rigid shaft clipped by an explicitly named prismatic support frame; local Y is axial.</summary>
public readonly record struct ClippedExtrusionDescriptor(CoordinateReference Coordinate, PackageBodyIndex MaterialBody,
    PackageBodyIndex ClippingBody, Mechanical.BodyColliderIndex Collider, float Cut, float DistalOffset, Vector2 HalfExtents);

public interface IClippedExtrusionElement : IPuzzleElementDeclaration
{
    IReadOnlyList<ClippedExtrusionDescriptor> Extrusions { get; }
}

public enum GeometryComponent : byte { HalfExtentX, HalfExtentY, HalfExtentZ, OffsetX, OffsetY, OffsetZ, Radius }

/// <summary>A bounded declared real parameter drives one collider component: multiplier * value + offset.</summary>
public readonly record struct ColliderParameterBinding(int Collider, GeometryComponent Component, string Parameter, float Multiplier, float Offset);

public interface IParameterizedGeometryElement : IPuzzleElementDeclaration
{
    IReadOnlyList<ColliderParameterBinding> ColliderBindings { get; }
    IReadOnlyList<TubeMouthParameterBinding> TubeMouthBindings { get; }
}

public readonly record struct TubeMouthParameterBinding(TubeEnd End, string Parameter, Vector3 Multiplier, Vector3 Offset);

public enum TubeEnd : byte { Start = 1, End = 2 }

public readonly record struct TubeMouthDescriptor(TubeEnd End, Vector3 Position, Quaternion Rotation, float BoreRadius);
