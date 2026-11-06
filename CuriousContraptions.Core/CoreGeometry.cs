using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreGeometry : ICoreGeometry
{
    public float PlacementSlop => HollowContactQuery.PlacementSlop;
    private static HollowSolid Solid(HollowSolidDeclaration declaration) => new(declaration.Kind, declaration.Position,
        declaration.X, declaration.Y, declaration.Z, declaration.Radius, declaration.LaneX, declaration.LaneY, declaration.LaneZ);
    public bool IsHollow(ColliderShapeKind kind) => HollowSolid.IsHollow(kind);
    public float HollowReach(HollowSolidDeclaration declaration) => Solid(declaration).Reach;
    public Vector3 HollowSupportNear(HollowSolidDeclaration declaration, Vector3 direction, Vector3 near) => Solid(declaration).SupportNear(direction, near);
    public bool HollowOverlaps(HollowPartner partner, HollowSolidDeclaration declaration, float slop) => HollowContactQuery.Overlaps(partner, Solid(declaration), slop);
    public float SurfaceDistance(HollowPartner partner, Vector3 point, out Vector3 surface, out Vector3 outward) => HollowContactQuery.SurfaceDistance(partner, point, out surface, out outward);
    public Vector3[] EvaluateRope(Vector3 from, Vector3 destination, float maximum, RopeState state) => RopeGeometry.Evaluate(from, destination, maximum, state);
}
