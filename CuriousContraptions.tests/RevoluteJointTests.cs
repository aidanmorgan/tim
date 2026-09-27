using Godot;

namespace CuriousContraptions.Tests;

public class RevoluteJointTests
{
    private static RevoluteJoint Joint(double inertia = 2, double initial = 0) =>
        new(Vector3.Back, inertia, -.6, .6, initial);

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(-2f, 4f)]
    public void AngularImpulseUsesSignedLeverArm(float arm, float impulse)
    {
        var hinge = Joint();
        hinge.ApplyImpulse(new(arm, 0, 0), new(0, impulse, 0));
        Assert.Equal(arm * impulse / hinge.Inertia, hinge.AngularVelocity, 10);
        Assert.Equal(.5 * hinge.Inertia * hinge.AngularVelocity * hinge.AngularVelocity, hinge.Energy);
    }

    [Fact]
    public void BalancedLoadMomentsCancelAndOffCentreLoadDoesNot()
    {
        var hinge = Joint();
        const float gravity = 9.81f;
        var left = hinge.Moment(new(-2, 0, 0), Vector3.Down * gravity);
        var right = hinge.Moment(new(1, 0, 0), Vector3.Down * (2 * gravity));
        Assert.Equal(0, left + right, 10);
        hinge.ApplyAngularImpulse((left + right) / 120);
        Assert.Equal(0, hinge.AngularVelocity);
        var shiftedRight = hinge.Moment(new(1.2f, 0, 0), Vector3.Down * (2 * gravity));
        hinge.ApplyAngularImpulse((left + shiftedRight) / 120);
        Assert.True(hinge.AngularVelocity < 0);
    }

    [Fact]
    public void AxialAndRadialImpulsesDoNotRotateHinge()
    {
        var hinge = Joint();
        hinge.ApplyImpulse(Vector3.Right, Vector3.Right * 10);
        hinge.ApplyImpulse(Vector3.Back, Vector3.Up * 10);
        hinge.ApplyImpulse(Vector3.Zero, Vector3.Up * 10);
        Assert.Equal(0, hinge.Energy);
        Assert.Equal(0, hinge.FreeInverseMassAlong(Vector3.Zero, Vector3.Up));
    }

    [Theory]
    [InlineData(1f, 1f, 0f)]
    [InlineData(1f, 1f, 1f)]
    [InlineData(4f, 2f, .4f)]
    [InlineData(.5f, -1.5f, .8f)]
    public void FreeImpactTransfersAngularMomentumWithoutCreatingEnergy(float mass, float arm, float restitution)
    {
        // One unconstrained normal contact at a known point. World contact discovery
        // and simultaneous stop/cargo constraints are deliberately not claimed here.
        var hinge = Joint();
        var offset = new Vector3(arm, 0, 0);
        var normal = Vector3.Up;
        var beforeVelocity = new Vector3(1, -5, .4f);
        var approach = (beforeVelocity - hinge.PointVelocity(offset)).Dot(normal);
        var impulse = -(1 + restitution) * approach /
            (1 / (double)mass + hinge.FreeInverseMassAlong(offset, normal));
        var afterVelocity = beforeVelocity + normal * (float)(impulse / mass);
        hinge.ApplyImpulse(offset, -normal * (float)impulse);
        var before = .5 * mass * beforeVelocity.LengthSquared();
        var after = .5 * mass * afterVelocity.LengthSquared() + hinge.Energy;
        Assert.InRange(after, 0, before + .00002);
        if (restitution == 1) Assert.InRange(Math.Abs(after - before), 0, .00002);
        var beforeMomentum = hinge.Moment(offset, beforeVelocity * mass);
        var afterMomentum = hinge.Moment(offset, afterVelocity * mass) + hinge.Inertia * hinge.AngularVelocity;
        Assert.InRange(Math.Abs(afterMomentum - beforeMomentum), 0, .00002);
        var separation = (afterVelocity - hinge.PointVelocity(offset)).Dot(normal);
        Assert.InRange(Math.Abs(separation + restitution * approach), 0, .00002);
        Assert.Equal(beforeVelocity.X, afterVelocity.X);
        Assert.Equal(beforeVelocity.Z, afterVelocity.Z);
    }

    [Fact]
    public void RotatingTheWholeSetupPreservesResponse()
    {
        var rotation = new Basis(new Quaternion(new Vector3(1, 2, 3).Normalized(), .73f));
        var original = Joint();
        var rotated = new RevoluteJoint(rotation * Vector3.Back, 2, -.6, .6);
        var offset = new Vector3(1.2f, .1f, -.2f);
        var impulse = new Vector3(.3f, -2, .5f);
        original.ApplyImpulse(offset, impulse);
        rotated.ApplyImpulse(rotation * offset, rotation * impulse);
        Assert.InRange(Math.Abs(original.AngularVelocity - rotated.AngularVelocity), 0, .000001);
        Assert.InRange((rotation * original.PointVelocity(offset) - rotated.PointVelocity(rotation * offset)).Length(), 0, .000001f);
        Assert.InRange(Math.Abs(original.FreeInverseMassAlong(offset, Vector3.Up) -
            rotated.FreeInverseMassAlong(rotation * offset, rotation * Vector3.Up)), 0, .000001);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void StopsAbsorbEnergyAndAllowInwardRelease(int direction)
    {
        var hinge = Joint();
        hinge.ApplyAngularImpulse(direction * 4);
        var initialEnergy = hinge.Energy;
        Assert.Equal(.3, hinge.TimeToLimit, 10);
        var first = hinge.Advance(.1);
        Assert.Equal(HingeLimit.None, first.Reached);
        Assert.Equal(0, first.DissipatedEnergy);
        Assert.Equal(direction * .2, hinge.Angle, 10);
        var stop = hinge.Advance(10);
        Assert.Equal(direction > 0 ? HingeLimit.Upper : HingeLimit.Lower, stop.Reached);
        Assert.Equal(stop.Reached, hinge.Limit);
        Assert.Equal(direction * .6, hinge.Angle);
        Assert.Equal(initialEnergy, stop.DissipatedEnergy);
        Assert.Equal(0, hinge.AngularVelocity);
        hinge.ApplyAngularImpulse(direction * 10);
        Assert.Equal(0, hinge.Energy);
        hinge.ApplyAngularImpulse(-direction * 2);
        Assert.Equal(-direction, hinge.AngularVelocity);
        hinge.Advance(.1);
        Assert.Equal(direction * .5, hinge.Angle, 10);
        Assert.Equal(HingeLimit.None, hinge.Limit);
    }

    [Fact]
    public void FreeFlightIsPartitionIndependentAndResetRestoresAuthoredAngle()
    {
        var whole = Joint(initial: .1);
        var split = Joint(initial: .1);
        whole.ApplyAngularImpulse(.2); split.ApplyAngularImpulse(.2);
        whole.Advance(.5);
        for (var i = 0; i < 10; i++) split.Advance(.05);
        Assert.Equal(whole.Angle, split.Angle, 12);
        Assert.Equal(whole.Energy, split.Energy);
        split.Reset();
        Assert.Equal(.1, split.Angle);
        Assert.Equal(0, split.AngularVelocity);
        Assert.Equal(double.PositiveInfinity, split.TimeToLimit);
        split.ApplyAngularImpulse(.2); split.Advance(.5);
        Assert.Equal(whole.Angle, split.Angle, 12);
    }

    [Fact]
    public void RoundedArrivalAtAStopCanBeResolvedWithoutAdvancingTime()
    {
        var hinge = Joint(initial: Math.BitDecrement(.6));
        hinge.ApplyAngularImpulse(2);
        hinge.Advance(hinge.TimeToLimit * .75);
        Assert.Equal(.6,hinge.Angle);
        Assert.Equal(0,hinge.TimeToLimit);
        var stop = hinge.Advance(0);
        Assert.Equal(HingeLimit.Upper,stop.Reached);
        Assert.Equal(1,stop.DissipatedEnergy);
        Assert.Equal(0,hinge.AngularVelocity);
    }

    [Fact]
    public void InvalidInputsAreRejectedWithoutChangingState()
    {
        Assert.Throws<ArgumentException>(() => new RevoluteJoint(Vector3.Zero, 1, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Joint(0));
        Assert.Throws<ArgumentException>(() => new RevoluteJoint(Vector3.Back, 1, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Joint(initial: .7));
        var hinge = Joint();
        hinge.ApplyAngularImpulse(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => hinge.Advance(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => hinge.Advance(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hinge.ApplyAngularImpulse(double.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => hinge.ApplyImpulse(Vector3.Zero, new(float.NaN, 0, 0)));
        Assert.Throws<ArgumentException>(() => hinge.FreeInverseMassAlong(Vector3.Right, Vector3.Up * 2));
        Assert.Equal(0, hinge.Angle);
        Assert.Equal(.5, hinge.AngularVelocity);
    }
}
