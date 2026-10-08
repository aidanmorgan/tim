using System.Diagnostics;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class InertiaTensorTests(ITestOutputHelper output)
{
    public enum RotationWorkload { Mixed, Dense }
    [Theory]
    [InlineData(.13)]
    [InlineData(2)]
    [InlineData(1e-40)]
    [InlineData(1e-100)]
    [InlineData(1e100)]
    public void ScalarIdentityIsIndependentOfRotation(double scalar)
    {
        var tensor=new InertiaTensor(scalar,scalar,scalar);
        Assert.Equal(tensor,tensor.Rotated(RigidRotation.FromRotationVector(new(.3,.4,.5))));
    }

    [Fact]
    public void StrongAnisotropyRetainsSmallIdentityComponents()
    {
        var tensor=new InertiaTensor(1,1,1e16,.1,0,0);
        Assert.Equal(tensor,tensor.Rotated(RigidRotation.Identity));
    }

    [Fact]
    public void AdmittedSubnormalComponentsSurviveIdentityAndHalfTurn()
    {
        var tensor=new InertiaTensor(2,3,4,double.Epsilon,-double.Epsilon,double.Epsilon);
        Assert.Equal(tensor,tensor.Rotated(RigidRotation.Identity));
        Assert.Equal(new InertiaTensor(2,3,4,double.Epsilon,double.Epsilon,-double.Epsilon),
            tensor.Rotated(new RigidRotation(0,0,1,0)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(1e200)]
    [InlineData(1e-200)]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidOrUnrepresentablePositiveDefiniteInputStillRejects(double diagonal)
    {
        // Existing admission requires finite positive principal minors and determinant.
        Assert.Throws<ArgumentException>(()=>new InertiaTensor(diagonal,diagonal,diagonal));
        Assert.Throws<ArgumentException>(()=>default(InertiaTensor).Rotated(RigidRotation.Identity));
    }

    [Fact]
    public void HalfTurnPreservesOffDiagonalBilinearForm()
    {
        var tensor=new InertiaTensor(2,3,4,.1,.2,.3);
        Assert.Equal(new InertiaTensor(2,3,4,.1,-.2,-.3),
            tensor.Rotated(new RigidRotation(0,0,1,0)));
    }

    [Fact]
    public void ArbitraryRotationPreservesEnergy()
    {
        var tensor=new InertiaTensor(2,3,4,.1,.2,.3);
        var rotation=RigidRotation.FromRotationVector(new(.3,.4,.5));
        var local=new CollisionVector(.7,-.2,.4);
        var world=rotation.Apply(local);
        var expected=CollisionVector.Dot(local,tensor.Apply(local));
        var actual=CollisionVector.Dot(world,tensor.Rotated(rotation).Apply(world));
        Assert.InRange(Math.Abs(expected-actual),0,1e-12);
        Assert.Throws<ArgumentException>(()=>tensor.Rotated(default));
    }

    [Fact]
    public void AdmittedInputRejectsUnrepresentableRotatedPrincipalMinors()
    {
        var tensor=new InertiaTensor(1e200,1e-100,1e-100);
        Assert.True(tensor.IsPositiveDefinite);
        Assert.Throws<ArgumentException>(()=>tensor.Rotated(RigidRotation.FromRotationVector(new(.3,.4,.5))));
    }

    [Theory]
    [InlineData(RotationWorkload.Mixed)]
    [InlineData(RotationWorkload.Dense)]
    public void NativeRotationBatchReportsCostWithoutBrowserQualification(RotationWorkload workload)
    {
        InertiaTensor[] tensors=workload switch
        {
            RotationWorkload.Mixed=>[new(.13,.13,.13),new(2,3,4,.1,.2,.3)],
            RotationWorkload.Dense=>[new(2,3,4,.1,.2,.3),new(3,4,5,.2,.3,.4)],
            _=>throw new ArgumentOutOfRangeException(nameof(workload))
        };
        var rotation=RigidRotation.FromRotationVector(new(.3,.4,.5));
        double checksum=0;
        for(var i=0;i<256;i++)checksum+=tensors[i%tensors.Length].Rotated(rotation).XX;
        const int count=100000;
        var before=GC.GetAllocatedBytesForCurrentThread();
        var start=Stopwatch.GetTimestamp();
        for(var i=0;i<count;i++)checksum+=tensors[i%tensors.Length].Rotated(rotation).XX;
        var elapsed=Stopwatch.GetElapsedTime(start);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.True(double.IsFinite(checksum));
        Assert.Equal(0,allocated);
        output.WriteLine($"Native rotations={count}, elapsedTicks={elapsed.Ticks}, nanosecondsPerRotation={elapsed.TotalNanoseconds/count:R}, allocatedBytes={allocated}, checksum={checksum:R}");
    }
}
