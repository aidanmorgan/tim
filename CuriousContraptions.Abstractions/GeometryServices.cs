using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Which surface of a hollow solid a closest point lies on: the end rim at −h (a torus's end face at a = 0), the outer wall,
/// the end rim at +h (a torus's end face at a = θ) or the bore.</summary>
public enum HollowSurface : byte { Start = 0, Outer = 1, End = 2, Bore = 3 }

/// <summary>A posed finite or plane partner for the shared hollow contact query.</summary>
public readonly record struct HollowPartner(ColliderShapeKind Shape, Vector3 Position, Vector3 U0, Vector3 U1, Vector3 U2,
    float Radius, float Hx, float Hy, float Hz);
public enum HollowContactSupport : byte { Material, Axis, DistanceOnly }
public readonly record struct HollowContact(Vector3 Normal, Vector3 Point, float Gap, int Feature, HollowContactSupport Support);

public enum RopeWinding { Clockwise = -1, CounterClockwise = 1 }

public readonly record struct RouteGuideFrame(
    Vector3 Position,
    Quaternion Rotation,
    float Radius,
    Vector3 Offset = default);

public readonly record struct RouteGuideArc(
    Vector3 Entry,
    Vector3 Exit,
    float StartAngle,
    float Sweep,
    float Length);

public readonly record struct EvaluatedRopeRoute(
    Vector3 TangentA,
    Vector3 TangentB,
    Vector3 UnitA,
    Vector3 UnitB,
    RouteGuideArc[] Arcs,
    float TotalLength);

public readonly record struct HollowSolidDeclaration(ColliderShapeKind Kind, Vector3 Position, Vector3 X, Vector3 Y, Vector3 Z,
    float Radius, float LaneX, float LaneY, float LaneZ);

public interface ICoreGeometry
{
    float PlacementSlop { get; }
    bool IsHollow(ColliderShapeKind kind);
    float HollowReach(HollowSolidDeclaration declaration);
    Vector3 HollowSupportNear(HollowSolidDeclaration declaration, Vector3 direction, Vector3 near);
    bool HollowOverlaps(HollowPartner partner, HollowSolidDeclaration declaration, float slop);
    float SurfaceDistance(HollowPartner partner, Vector3 point, out Vector3 surface, out Vector3 outward);
    Vector3[] EvaluateRope(Vector3 from, Vector3 destination, float maximum, RopeState state);
}
