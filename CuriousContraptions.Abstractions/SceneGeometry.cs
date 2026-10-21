using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions;

/// <summary>Geometry ownership in the authored scene. Entry zero owns the base body; subsequent entries own slider bodies.
/// The base root is "."; moving roots are disjoint subtrees excluded from the base.</summary>
public readonly record struct SceneBodyGeometry(string Root, MaterialProperties Material);

public interface ISceneGeometryElement : IPuzzleElementDeclaration
{
    IReadOnlyList<SceneBodyGeometry> SceneBodies { get; }
}

public enum GeometryComponent : byte { HalfExtentX, HalfExtentY, HalfExtentZ, OffsetX, OffsetY, OffsetZ }

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
