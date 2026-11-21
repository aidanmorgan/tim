using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ConicalAirflowTests
{
    [Fact]
    public void ConeHasOpenAxialAndRadialBoundaries()
    {
        // Reach 8 m and terminal radius 4 m imply a radius of 2 m halfway down the cone.
        Assert.True(ConicalAirflow.Contains(new(4, 1, 0), Vector3.UnitX, 8, 4));
        Assert.False(ConicalAirflow.Contains(new(4, 2, 0), Vector3.UnitX, 8, 4));
        Assert.False(ConicalAirflow.Contains(Vector3.Zero, Vector3.UnitX, 8, 4));
        Assert.False(ConicalAirflow.Contains(new(8, 0, 0), Vector3.UnitX, 8, 4));
        Assert.False(ConicalAirflow.Contains(new(-1, 0, 0), Vector3.UnitX, 8, 4));
    }

    [Fact]
    public void RelativeMotionAndVacuumRemoveUnpayableBranchDemand()
    {
        // These binary-exact values test the defined local impedance law, not incidental trajectories.
        Assert.Equal(1f, ConicalAirflow.Impulse(8, 4, 0, 1, 1, .125f));
        Assert.Equal(.5f, ConicalAirflow.Impulse(8, 4, 2, 1, 1, .125f));
        Assert.Equal(0f, ConicalAirflow.Impulse(8, 4, 4, 1, 1, .125f));
        Assert.Equal(0f, ConicalAirflow.Impulse(8, 4, 8, 1, 1, .125f));
        Assert.Equal(0f, ConicalAirflow.Impulse(8, 4, 0, 1, 0, .125f));
        Assert.Equal(0f, ConicalAirflow.Impulse(8, 0, 0, 1, 1, .125f));
    }

    [Fact]
    public void AllReceiversShareOneSourcePowerAllowance()
    {
        float[] impulses = [1, 1];
        // 16 W over 1/8 s is 2 J: at 4 m/s only 1/2 N s can be transferred in total.
        ConicalAirflow.LimitSource(impulses, 8, 4, 16, .125f);
        Assert.Equal(.25f, impulses[0]);
        Assert.Equal(.25f, impulses[1]);
        Assert.Equal(2f, 4 * (impulses[0] + impulses[1]));
    }

    [Fact]
    public void UnequalSpeedRequestsShareJoulesAndRespectReordering()
    {
        // Each source requests 1 N s, but speeds 2 and 4 m/s require 2 and 4 J.
        float[] grants = new float[2];
        ProportionalAllocation.Grant([2, 4], 3, grants);
        Assert.Equal(1f, grants[0]);
        Assert.Equal(2f, grants[1]);
        Assert.Equal(.5f, grants[0] / 2);
        Assert.Equal(.5f, grants[1] / 4);
        ProportionalAllocation.Grant([4, 2], 3, grants);
        Assert.Equal(2f, grants[0]);
        Assert.Equal(1f, grants[1]);
        ProportionalAllocation.Grant([4, 2], 0, grants);
        Assert.Equal(0f, grants[0]);
        Assert.Equal(0f, grants[1]);
    }

    [Fact]
    public void NozzlePointVelocityIncludesItsAngularMotion()
    {
        // At (0,1,0), a +Z spin contributes -X velocity; a receiver moving -X with it has zero relative speed.
        var point = Vector3.UnitY;
        Assert.Equal(0f, ConicalAirflow.RelativeSpeed(point, Vector3.UnitX, point, -Vector3.UnitX,
            Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.UnitZ));
        Assert.Equal(1f, ConicalAirflow.RelativeSpeed(point, Vector3.UnitX, point, Vector3.Zero,
            Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.UnitZ));
    }
}
