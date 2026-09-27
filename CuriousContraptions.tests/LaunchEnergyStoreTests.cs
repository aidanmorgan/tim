namespace CuriousContraptions.Tests;

public class LaunchEnergyStoreTests
{
    [Fact]
    public void OnlySuppliedWorkChargesAndCapacityRejectsExcess()
    {
        var store=new LaunchEnergyStore(20);
        Assert.Equal(0,store.Charge(0,10));Assert.Equal(0,store.Energy);
        Assert.Equal(5,store.Charge(10,.5f));Assert.Equal(5,store.Energy);
        Assert.Equal(15,store.Charge(100,1));Assert.Equal(20,store.Energy);
        Assert.Equal(0,store.Charge(100,1));Assert.Equal(20,store.AcceptedEnergy);
        Assert.Equal(0,store.Charge(10,0));Assert.Equal(20,store.Energy);
    }

    [Theory]
    [InlineData(.5f,0f)]
    [InlineData(1f,0f)]
    [InlineData(4f,0f)]
    [InlineData(8f,0f)]
    [InlineData(1f,3f)]
    [InlineData(1f,-3f)]
    public void ReleaseConsumesActualAddedKineticEnergy(float mass,float incoming)
    {
        var store=new LaunchEnergyStore(20);store.Charge(20,1);
        var release=store.Release(mass,incoming,40);
        Assert.Equal(LaunchReleaseStatus.Released,release.Status);
        Assert.True(release.ForwardSpeed>Math.Abs(incoming));
        var added=.5*mass*(release.ForwardSpeed-Math.Abs((double)incoming))*
            (release.ForwardSpeed+Math.Abs((double)incoming));
        Assert.Equal(added,release.EnergyUsed,10);
        Assert.InRange(added,19.9999,20);
        Assert.InRange(store.Energy,0,.0001);
        Assert.Equal(store.AcceptedEnergy,store.Energy+store.ReleasedEnergy,10);
    }

    [Fact]
    public void SpeedCapPreservesUnusedCharge()
    {
        var store=new LaunchEnergyStore(100);store.Charge(100,1);
        var release=store.Release(1,0,4);
        Assert.Equal(LaunchReleaseStatus.Released,release.Status);
        Assert.Equal(4,release.ForwardSpeed);Assert.Equal(8,release.EnergyUsed);
        Assert.Equal(92,store.Energy);
        var next=store.Release(1,4,4);
        Assert.Equal(LaunchReleaseStatus.SpeedLimited,next.Status);
        Assert.Equal(92,store.Energy);
    }

    [Fact]
    public void NoChargeAndSubPrecisionChargeCannotProduceFreeShots()
    {
        var store=new LaunchEnergyStore(20);
        Assert.Equal(LaunchReleaseStatus.Empty,store.Release(1,-1,40).Status);
        store.Charge(float.Epsilon,1);
        var before=store.Energy;
        var shot=store.Release(1,-1,40);
        Assert.Equal(LaunchReleaseStatus.InsufficientPrecision,shot.Status);
        Assert.Equal(-1,shot.ForwardSpeed);Assert.Equal(before,store.Energy);
    }

    [Fact]
    public void RepeatedReleasesAndRechargeCannotExceedAcceptedWork()
    {
        var store=new LaunchEnergyStore(20);
        double kinetic=0;
        for(var i=0;i<1000;i++)
        {
            store.Charge(3,.013f);
            var release=store.Release(.7f,0,40);
            if(release.Status==LaunchReleaseStatus.Released)
                kinetic+=.5*.7f*release.ForwardSpeed*release.ForwardSpeed;
            Assert.True(store.Energy>=0);
            Assert.InRange(kinetic,0,store.AcceptedEnergy+1e-9);
        }
        Assert.InRange(Math.Abs(store.AcceptedEnergy-store.Energy-store.ReleasedEnergy),0,1e-9);
        store.Reset();Assert.Equal(0,store.Energy);Assert.Equal(0,store.AcceptedEnergy);Assert.Equal(0,store.ReleasedEnergy);
    }

    [Fact]
    public void FloatConversionNeverOverdrawsAcrossMassAndChargeScales()
    {
        foreach(var mass in new[]{.1f,1f,8f,64f,float.MaxValue})
        foreach(var incoming in new[]{-10f,0f,10f})
        foreach(var energy in new[]{float.Epsilon,.0001f,1f,100f,10000f})
        {
            var store=new LaunchEnergyStore(energy);store.Charge(energy,1);
            var shot=store.Release(mass,incoming,40);
            Assert.InRange(shot.EnergyUsed,0,(double)energy);
            Assert.InRange(store.Energy,0,(double)energy);
            Assert.True(float.IsFinite(shot.ForwardSpeed));
            if(shot.Status!=LaunchReleaseStatus.Released)
            {
                Assert.Equal(incoming,shot.ForwardSpeed);Assert.Equal((double)energy,store.Energy);
            }
            else
            {
                var before=.5*(double)mass*incoming*incoming;
                var after=.5*(double)mass*shot.ForwardSpeed*shot.ForwardSpeed;
                Assert.True(after<=before+energy+Math.Max(1e-40,Math.Abs(before)*1e-15));
            }
        }
    }

    [Fact]
    public void ChargeStepPartitioningAndPowerLossPreserveTheBudget()
    {
        var whole=new LaunchEnergyStore(100);var split=new LaunchEnergyStore(100);
        whole.Charge(8,1);
        for(var i=0;i<8;i++)split.Charge(8,.125f);
        Assert.Equal(whole.Energy,split.Energy);
        split.Charge(0,100);Assert.Equal(whole.Energy,split.Energy);
        Assert.Equal(whole.Release(2,1,40),split.Release(2,1,40));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidCapacityIsRejected(float value)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LaunchEnergyStore(value));

    [Theory]
    [InlineData(-1f,1f)]
    [InlineData(1f,-1f)]
    [InlineData(float.NaN,1f)]
    [InlineData(1f,float.PositiveInfinity)]
    public void InvalidChargeIsAtomic(float power,float seconds)
    {
        var store=new LaunchEnergyStore(20);store.Charge(5,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>store.Charge(power,seconds));
        Assert.Equal(5,store.Energy);Assert.Equal(5,store.AcceptedEnergy);
    }

    [Theory]
    [InlineData(0f,0f,40f)]
    [InlineData(-1f,0f,40f)]
    [InlineData(float.PositiveInfinity,0f,40f)]
    [InlineData(1f,float.NaN,40f)]
    [InlineData(1f,0f,0f)]
    [InlineData(1f,0f,float.PositiveInfinity)]
    public void InvalidReleaseIsAtomic(float mass,float incoming,float maximum)
    {
        var store=new LaunchEnergyStore(20);store.Charge(5,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>store.Release(mass,incoming,maximum));
        Assert.Equal(5,store.Energy);Assert.Equal(0,store.ReleasedEnergy);
    }
}
