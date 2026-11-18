using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class MaterialSweepTests(ITestOutputHelper output)
{
    private readonly IMaterialQueryWorkspace _queries = RuntimeTestServices.Core.Geometry.CreateMaterialQueryWorkspace();

    private static HollowPartner Shape(ColliderShapeKind kind, Vector3 position, float radius = .3f,
        float x = .3f, float y = .3f, float z = .3f) =>
        new(kind, position, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, radius, x, y, z);

    [Fact]
    public void PerpendicularCurvedBoundaryCompletesWithoutFalseOutside()
    {
        var region = Shape(ColliderShapeKind.Cylinder, Vector3.Zero, radius: .625f, y: 1f);
        // Exact3-4-5 dimensions: sqrt(.375²+.5²)=.625. The whole finite cylinder
        // fits, with its circular rim touching the cylindrical query boundary.
        var payload = new HollowPartner(ColliderShapeKind.Cylinder, Vector3.Zero,
            -Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, .375f, 0f, .5f, 0f);
        var result = _queries.ContainsMaterial(region, payload, 0f);
        output.WriteLine($"Perpendicular curved containment: {result}");
        Assert.True(result is MaterialContainmentResult.Inside or MaterialContainmentResult.Unresolved,
            "An exactly contained curved shape may refuse an unresolved proof, but cannot report known outside material.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThinObstructionBetweenEndpointsBlocksContinuousTranslation(bool rotated)
    {
        var moving = Shape(ColliderShapeKind.Sphere, new(-1f, 0f, 0f));
        var wall = Shape(ColliderShapeKind.Box, new(.1234f, 0f, 0f), x: .0001f, y: 1f, z: 1f);
        var translation = new Vector3(2f, 0f, 0f);
        var clear = wall with { Position = new(.1234f, 2f, 0f) };
        var turn = rotated ? Quaternion.CreateFromYawPitchRoll(.7f, -.4f, .2f) : Quaternion.Identity;
        Assert.Equal(MaterialQueryResult.Intersecting, Query(Turn(moving, turn), Turn(wall, turn), Vector3.Transform(translation, turn)));
        Assert.Equal(MaterialQueryResult.Clear, Query(Turn(moving, turn), Turn(clear, turn), Vector3.Transform(translation, turn)));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.01f)]
    public void NestedMaterialCountsEvenWithoutBoundaryCrossing(float distance)
    {
        var large = Shape(ColliderShapeKind.Box, Vector3.Zero, x: 1f, y: 1f, z: 1f);
        var small = Shape(ColliderShapeKind.Sphere, new(.2f, 0f, 0f), radius: .05f);
        Assert.Equal(MaterialQueryResult.Intersecting, Query(large, small, distance * Vector3.UnitX));
        Assert.Equal(MaterialQueryResult.Intersecting, Query(small, large, distance * Vector3.UnitX));
        Assert.Equal(MaterialQueryResult.Clear, Query(small with { Position = new(2f, 0f, 0f) }, large, distance * Vector3.UnitX));
    }

    [Theory]
    [InlineData(ColliderShapeKind.Sphere)]
    [InlineData(ColliderShapeKind.Box)]
    [InlineData(ColliderShapeKind.Cylinder)]
    public void ActualAnnularBoreRemainsOpenForFinitePayloads(ColliderShapeKind kind)
    {
        var payload = Shape(kind, new(0f, -1f, 0f));
        var tube = Shape(ColliderShapeKind.Tube, Vector3.Zero, radius: .58f, x: .48f, y: .55f, z: 0f);
        Assert.Equal(MaterialQueryResult.Clear, Query(payload, tube, 2f * Vector3.UnitY));
        Assert.Equal(MaterialQueryResult.Intersecting, Query(payload with { Position = new(.4f, -1f, 0f) }, tube, 2f * Vector3.UnitY));
    }

    [Theory]
    [InlineData(ColliderShapeKind.Torus)]
    [InlineData(ColliderShapeKind.Frustum)]
    public void CurvedHollowMaterialKeepsItsOpenInterior(ColliderShapeKind kind)
    {
        var hollow = kind == ColliderShapeKind.Torus
            ? Shape(kind, Vector3.Zero, radius: .7f, x: 0f, y: .1f, z: ColliderDeclaration.FullSweep)
            : Shape(kind, Vector3.Zero, radius: .58f, x: .1f, y: .55f, z: .7f);
        var ball = Shape(ColliderShapeKind.Sphere, -Vector3.UnitY, radius: .2f);
        Assert.Equal(MaterialQueryResult.Clear, Query(ball, hollow, 2f * Vector3.UnitY));
        Assert.Equal(MaterialQueryResult.Intersecting, Query(ball with { Position = new(.7f, -1f, 0f) }, hollow, 2f * Vector3.UnitY));
    }

    [Fact]
    public void PlaneSweepUsesWholeShapeSupport()
    {
        var plane = Shape(ColliderShapeKind.Plane, Vector3.Zero);
        var box = Shape(ColliderShapeKind.Box, Vector3.UnitY, x: .4f, y: .2f, z: .3f);
        Assert.Equal(MaterialQueryResult.Clear, Query(box, plane, -.5f * Vector3.UnitY));
        Assert.Equal(MaterialQueryResult.Intersecting, Query(box, plane, -Vector3.UnitY));
    }

    [Fact]
    public void MovingPartialTorusRetainsOpenCutMouthAndActualWall()
    {
        var bend = Shape(ColliderShapeKind.Torus, Vector3.Zero, radius: .7f, x: .16f, y: .24f, z: MathF.PI * .5f);
        var throughMouth = Shape(ColliderShapeKind.Sphere, new(.7f, 0f, -.4f), radius: .08f);
        // At the initial +X mouth the tangent is +Z; the annular cap cannot seal the bore.
        Assert.Equal(MaterialQueryResult.Clear, Query(bend, throughMouth, -.35f * Vector3.UnitZ));
        var rim = throughMouth with { Position = new(.92f, 0f, -.4f) };
        Assert.Equal(MaterialQueryResult.Intersecting, Query(bend, rim, -.5f * Vector3.UnitZ));
    }

    [Theory]
    [InlineData(ColliderShapeKind.Sphere)]
    [InlineData(ColliderShapeKind.Box)]
    [InlineData(ColliderShapeKind.Cylinder)]
    [InlineData(ColliderShapeKind.Tube)]
    [InlineData(ColliderShapeKind.Torus)]
    [InlineData(ColliderShapeKind.Frustum)]
    public void FiniteCylinderContainsWholeMaterialAndRefusesProtrusion(ColliderShapeKind kind)
    {
        var region = Shape(ColliderShapeKind.Cylinder, Vector3.Zero, radius: .48f, y: .55f);
        var shape = kind switch
        {
            ColliderShapeKind.Torus => Shape(kind, Vector3.Zero, radius: .25f, x: .02f, y: .08f, z: MathF.PI),
            ColliderShapeKind.Tube => Shape(kind, Vector3.Zero, radius: .3f, x: .2f, y: .25f),
            ColliderShapeKind.Frustum => Shape(kind, Vector3.Zero, radius: .25f, x: .05f, y: .25f, z: .35f),
            _ => Shape(kind, Vector3.Zero, radius: .3f, x: .25f, y: .25f, z: .25f)
        };
        Assert.Equal(MaterialContainmentResult.Inside, _queries.ContainsMaterial(region, shape, .001f));
        Assert.Equal(MaterialContainmentResult.Outside, _queries.ContainsMaterial(region, shape with { Position = new(.4f, 0f, 0f) }, .001f));
        Assert.Equal(MaterialContainmentResult.Outside, _queries.ContainsMaterial(region, shape with { Position = new(0f, .5f, 0f) }, .001f));
    }

    [Theory]
    [InlineData(ColliderShapeKind.Tube)]
    [InlineData(ColliderShapeKind.Frustum)]
    [InlineData(ColliderShapeKind.Torus)]
    public void CoaxialHollowMaterialAtTheInclusiveRadialBoundaryIsContained(ColliderShapeKind kind)
    {
        var region = Shape(ColliderShapeKind.Cylinder, Vector3.Zero, radius: .5f, y: .55f);
        var tube = kind switch
        {
            ColliderShapeKind.Frustum => Shape(kind, Vector3.Zero, radius: .5f, x: .05f, y: .25f, z: .4f),
            ColliderShapeKind.Torus => Shape(kind, Vector3.Zero, radius: .375f, x: 0f, y: .125f, z: ColliderDeclaration.FullSweep),
            _ => Shape(kind, Vector3.Zero, radius: .5f, x: .4f, y: .25f)
        };
        // The entire outer cylindrical surface lies exactly on the admitted boundary;
        // this is inclusive geometry, not a scene tolerance or historical trajectory.
        Assert.Equal(MaterialContainmentResult.Inside, _queries.ContainsMaterial(region, tube, 0f));
        Assert.Equal(MaterialContainmentResult.Outside, _queries.ContainsMaterial(region, tube with { Position = new(.01f, 0f, 0f) }, 0f));
    }

    [Fact]
    public void RadialContainmentChecksBoxCornersBeyondBothTransverseWidths()
    {
        var region = Shape(ColliderShapeKind.Cylinder, Vector3.Zero, radius: .48f, y: .55f);
        var box = Shape(ColliderShapeKind.Box, Vector3.Zero, x: .4f, y: .3f, z: .4f);
        Assert.Equal(MaterialContainmentResult.Outside, _queries.ContainsMaterial(region, box, .001f));
        var sphere = Shape(ColliderShapeKind.Sphere, Vector3.Zero, radius: .42f);
        var geometry = RuntimeTestServices.Core.Geometry;
        Assert.Equal(.84f, Vector3.Dot(geometry.MaterialSupport(sphere, Vector3.UnitX) -
            geometry.MaterialSupport(sphere, -Vector3.UnitX), Vector3.UnitX));
        Assert.Equal(MaterialContainmentResult.Inside, _queries.ContainsMaterial(region, sphere, .001f));
    }

    [Fact]
    public void ReusedWorkspaceDiscardsPendingCellsAfterEarlyReturns()
    {
        var region = Shape(ColliderShapeKind.Cylinder, Vector3.Zero, radius: .48f, y: .55f);
        var tube = Shape(ColliderShapeKind.Tube, new(.4f, 0f, 0f), radius: .3f, x: .2f, y: .25f);
        Assert.Equal(MaterialContainmentResult.Outside, _queries.ContainsMaterial(region, tube, .001f));
        Assert.Equal(MaterialContainmentResult.Inside, _queries.ContainsMaterial(region, tube with { Position = Vector3.Zero }, .001f));
        var large = Shape(ColliderShapeKind.Box, Vector3.Zero, x: 1f, y: 1f, z: 1f);
        var small = Shape(ColliderShapeKind.Sphere, Vector3.Zero, radius: .05f);
        Assert.Equal(MaterialQueryResult.Intersecting, Query(large, small, Vector3.Zero));
        Assert.Equal(MaterialQueryResult.Clear, Query(large, small with { Position = 3f * Vector3.UnitX }, Vector3.Zero));
        var independent = RuntimeTestServices.Core.Geometry.CreateMaterialQueryWorkspace();
        Assert.Equal(MaterialQueryResult.Clear, independent.SweepMaterial(large, small with { Position = 3f * Vector3.UnitX }, Vector3.Zero));
        Assert.Equal(MaterialQueryResult.Intersecting, Query(large, small, Vector3.Zero));
    }

    private MaterialQueryResult Query(HollowPartner moving, HollowPartner obstacle, Vector3 translation) =>
        _queries.SweepMaterial(moving, obstacle, translation);

    private static HollowPartner Turn(HollowPartner shape, Quaternion turn) => shape with
    {
        Position = Vector3.Transform(shape.Position, turn), U0 = Vector3.Transform(shape.U0, turn),
        U1 = Vector3.Transform(shape.U1, turn), U2 = Vector3.Transform(shape.U2, turn)
    };
}
