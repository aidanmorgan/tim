using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ReactionRayLimitTests
{
    [Fact]
    public void TangentAtDiskBoundaryHasZeroConnectedFeasibleLength()=>
        Assert.Equal(ReactionRayExtent.Zero,ReactionRayLimit.Cone(1,1,0,0,0,1,1).Extent);

    [Fact]
    public void ApexInwardDirectionIsNotFrozen()=>
        Assert.Equal(ReactionRayExtent.Unbounded,ReactionRayLimit.Cone(0,0,0,1,.5,0,1).Extent);

    [Fact]
    public void ApexOutwardDirectionHasZeroLength()=>
        Assert.Equal(ReactionRayExtent.Zero,ReactionRayLimit.Cone(0,0,0,1,2,0,1).Extent);

    [Fact]
    public void ScalarAtBoundCanTravelInward()
    {
        var limit=ReactionRayLimit.Scalar(1,-2,0,1);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(.5,limit.Distance);
    }

    [Fact]
    public void UnboundedScalarDoesNotInventAProposal()=>
        Assert.Equal(ReactionRayExtent.Unbounded,ReactionRayLimit.Scalar(1,2,double.NegativeInfinity,double.PositiveInfinity).Extent);

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void InteriorDiskReachesFirstBoundary(double direction)
    {
        var limit=ReactionRayLimit.Cone(1,0,0,0,direction,0,.2);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(.2,limit.Distance,14);
    }

    [Fact]
    public void InwardBoundaryRayCrossesWholeDisk()
    {
        var limit=ReactionRayLimit.Cone(1,1,0,0,-1,0,1);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(2,limit.Distance);
    }

    [Fact]
    public void OpposingActiveConesHaveNoSharedPositiveTravel()
    {
        var first=ReactionRayLimit.Cone(1,1,0,0,-1,0,1);
        var second=ReactionRayLimit.Cone(1,-1,0,0,-1,0,1);
        Assert.Equal(ReactionRayExtent.Zero,first.Intersect(second).Extent);
    }

    [Fact]
    public void NormalHalfLineStillOwnsApexEndpoint()
    {
        var limit=ReactionRayLimit.Cone(1,0,0,-1,0,0,1);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(1,limit.Distance);
    }

    [Fact]
    public void FiniteIntersectionRestrictsUnboundedCone()
    {
        var limit=ReactionRayLimit.Cone(0,0,0,1,.5,0,1).Intersect(ReactionRayLimit.Scalar(0,1,0,3));
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(3,limit.Distance);
    }

    [Fact]
    public void FiniteScalarQuotientSurvivesOverflowingSubtraction()
    {
        var limit=ReactionRayLimit.Scalar(-1e308,2e307,-double.MaxValue,1e308);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(10,limit.Distance,13);
    }

    [Fact]
    public void NonrepresentablePositiveBoundaryRemainsExplicit()=>
        Assert.Equal(ReactionRayExtent.BeyondRange,ReactionRayLimit.Scalar(0,double.Epsilon,0,1).Extent);

    [Fact]
    public void SubnormalScalarBoundaryIsPreserved()
    {
        var limit=ReactionRayLimit.Scalar(0,1,0,double.Epsilon);
        Assert.Equal(ReactionRayExtent.Finite,limit.Extent);Assert.Equal(double.Epsilon,limit.Distance);
    }
}
