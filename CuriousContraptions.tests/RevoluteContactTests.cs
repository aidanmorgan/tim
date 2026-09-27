using Godot;

namespace CuriousContraptions.Tests;

public class RevoluteContactTests
{
    [Theory]
    [InlineData(1f, 1f, 0f)]
    [InlineData(1f, 1f, 1f)]
    [InlineData(4f, 2f, .4f)]
    [InlineData(.5f, -1.5f, .8f)]
    public void FreeImpactConservesAngularMomentumAndBoundsEnergy(float mass, float arm, float restitution)
    {
        var joint = new RevoluteJoint(Vector3.Back, 2, -.6, .6);
        var offset = new Vector3(arm, 0, 0);
        var velocity = new Vector3(1, -5, .4f);
        var result = RevoluteContact.Resolve(joint, offset, velocity, 1 / (double)mass, Vector3.Up, restitution);
        var before = .5 * mass * velocity.LengthSquared();
        var after = .5 * mass * result.Velocity.LengthSquared() + joint.Energy;
        Assert.InRange(after, 0, before + .00002);
        if (restitution == 1) Assert.InRange(Math.Abs(after - before), 0, .00002);
        var beforeMomentum = joint.Moment(offset, velocity * mass);
        var afterMomentum = joint.Moment(offset, result.Velocity * mass) + joint.Inertia * joint.AngularVelocity;
        Assert.InRange(Math.Abs(afterMomentum - beforeMomentum), 0, .00002);
        var separation = (result.Velocity - joint.PointVelocity(offset)).Dot(Vector3.Up);
        Assert.InRange(Math.Abs(separation - restitution * 5), 0, .00002);
        Assert.Equal(0, result.StopDissipation);
    }

    [Theory]
    [InlineData(HingeLimit.Lower, 1f)]
    [InlineData(HingeLimit.Upper, -1f)]
    public void RestingStopActsAsImmovableSurface(HingeLimit limit, float arm)
    {
        var angle = limit == HingeLimit.Lower ? -.6 : .6;
        var joint = new RevoluteJoint(Vector3.Back, 2, -.6, .6, angle);
        var result = RevoluteContact.Resolve(joint, new(arm, 0, 0), Vector3.Down * 5, 1, Vector3.Up, .5f);
        Assert.Equal(2.5f, result.Velocity.Y);
        Assert.Equal(0, joint.AngularVelocity);
        Assert.Equal(0, joint.Energy);
        Assert.Equal(0, result.StopDissipation);
        Assert.Equal(limit, joint.Limit);
    }

    [Theory]
    [InlineData(HingeLimit.Lower, 1f, 0f)]
    [InlineData(HingeLimit.Lower, 1f, 1f)]
    [InlineData(HingeLimit.Upper, -1f, 0f)]
    [InlineData(HingeLimit.Upper, -1f, 1f)]
    public void ImpactReversingAnInwardMovingHingeAtItsStopCannotCreateEnergy(HingeLimit limit, float arm, float restitution)
    {
        var angle = limit == HingeLimit.Lower ? -.6 : .6;
        var joint = new RevoluteJoint(Vector3.Back, 1, -.6, .6, angle);
        joint.ApplyAngularImpulse(arm); // inward from the stop
        var velocity = Vector3.Down * 2;
        var before = .5 * velocity.LengthSquared() + joint.Energy;
        var result = RevoluteContact.Resolve(joint, new(arm, 0, 0), velocity, 1, Vector3.Up, restitution);
        var after = .5 * result.Velocity.LengthSquared() + joint.Energy;
        Assert.InRange(after, 0, before + .00001);
        Assert.True(result.StopDissipation > 0);
        Assert.Equal(0, joint.AngularVelocity);
        Assert.True(result.Velocity.Y >= 0);
    }

    [Theory]
    [InlineData(HingeLimit.Lower, -1f)]
    [InlineData(HingeLimit.Upper, 1f)]
    public void InwardContactReleasesTheBeamFromAStop(HingeLimit limit, float arm)
    {
        var joint = new RevoluteJoint(Vector3.Back, 1, -.6, .6, limit == HingeLimit.Lower ? -.6 : .6);
        var result = RevoluteContact.Resolve(joint, new(arm, 0, 0), Vector3.Down * 2, 1, Vector3.Up, 0);
        Assert.True(joint.AngularVelocity * arm < 0);
        joint.Advance(.01);
        Assert.Equal(HingeLimit.None, joint.Limit);
        Assert.Equal(0, result.StopDissipation);
    }

    [Fact]
    public void RotationDrivenSweptImpactSpendsBeamEnergyOnStationaryBall()
    {
        var joint = new RevoluteJoint(Vector3.Back, 2, -1, 1);
        joint.ApplyAngularImpulse(2);
        var ball = new Vector3(1, 1, 0);
        var hit = RotatingBoxSweep.Cast(ball, .2f, Vector3.Zero, Vector3.Zero, joint.Axis,
            Transform3D.Identity, new(2, .1f, .4f), joint.AngularVelocity, .8);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        var before = joint.Energy;
        joint.Advance(hit.Time);
        var result = RevoluteContact.Resolve(joint, hit.Point, Vector3.Zero, 1, hit.Normal, .5f);
        Assert.True(result.Velocity.Length() > .1);
        Assert.True(joint.Energy < before);
        Assert.InRange(.5 * result.Velocity.LengthSquared() + joint.Energy, 0, before + .00001);
        Assert.True((result.Velocity - joint.PointVelocity(hit.Point)).Dot(hit.Normal) > 0);
    }

    [Fact]
    public void PivotHitCannotApplyAngularMomentumAndSeparatingContactDoesNothing()
    {
        var joint = new RevoluteJoint(Vector3.Back, 2, -.6, .6);
        var pivot = RevoluteContact.Resolve(joint, Vector3.Zero, Vector3.Down, 1, Vector3.Up, .5f);
        Assert.Equal(.5f, pivot.Velocity.Y);
        Assert.Equal(0, joint.Energy);
        var separating = RevoluteContact.Resolve(joint, Vector3.Right, Vector3.Up, 1, Vector3.Up, 1);
        Assert.Equal(Vector3.Up, separating.Velocity);
        Assert.Equal(0, separating.Impulse);
        Assert.Equal(0, joint.Energy);
    }

    [Theory]
    [InlineData(HingeLimit.None)]
    [InlineData(HingeLimit.Lower)]
    [InlineData(HingeLimit.Upper)]
    public void VariedInwardAndStoppedContactsRemainPassive(HingeLimit limit)
    {
        var random = new Random(1729);
        for (var trial = 0; trial < 1000; trial++)
        {
            var angle = limit switch { HingeLimit.None => 0, HingeLimit.Lower => -.6, HingeLimit.Upper => .6,
                _ => throw new ArgumentOutOfRangeException(nameof(limit)) };
            var inertia = .2 + random.NextDouble() * 10;
            var joint = new RevoluteJoint(Vector3.Back, inertia, -.6, .6, angle);
            var desiredVelocity = (random.NextDouble() * 2 - 1) * 10;
            joint.ApplyAngularImpulse(desiredVelocity * inertia);
            var mass = .2f + random.NextSingle() * 8;
            var offset = new Vector3(random.NextSingle() * 4 - 2, .1f, .3f);
            var velocity = new Vector3(.4f, random.NextSingle() * 20 - 10, -.3f);
            var before = .5 * mass * velocity.LengthSquared() + joint.Energy;
            var result = RevoluteContact.Resolve(joint, offset, velocity, 1 / (double)mass, Vector3.Up, random.NextSingle());
            var after = .5 * mass * result.Velocity.LengthSquared() + joint.Energy;
            Assert.InRange(after, 0, before + .0001);
            Assert.True(result.Impulse >= 0);
            Assert.True(result.StopDissipation >= 0);
            Assert.InRange((result.Velocity - joint.PointVelocity(offset)).Dot(Vector3.Up), -.00001f, float.MaxValue);
        }
    }

    [Fact]
    public void ConstrainedNormalResponseDoesNotMoveTheBody()
    {
        var joint = new RevoluteJoint(Vector3.Back, 2, -.6, .6);
        joint.ApplyAngularImpulse(2);
        var result = RevoluteContact.Resolve(joint, Vector3.Right, Vector3.Zero, 0, Vector3.Up, .5f);
        Assert.Equal(Vector3.Zero,result.Velocity);
        Assert.Equal(-.5,joint.AngularVelocity,10);
        Assert.Equal(.25,joint.Energy,10);
    }

    [Fact]
    public void InvalidContactDoesNotMutateHinge()
    {
        var joint = new RevoluteJoint(Vector3.Back, 2, -.6, .6);
        Assert.Throws<ArgumentOutOfRangeException>(() => RevoluteContact.Resolve(joint, Vector3.Right, Vector3.Down, -1, Vector3.Up, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => RevoluteContact.Resolve(joint, Vector3.Right, Vector3.Down, 1, Vector3.Up, 2));
        Assert.Throws<ArgumentException>(() => RevoluteContact.Resolve(joint, Vector3.Right, Vector3.Down, 1, Vector3.Up * 2, 1));
        Assert.Equal(0, joint.Energy);
        Assert.Equal(0, joint.Angle);
    }
}
