using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class PrismaticWorkBudgetTests
{
    [Theory]
    [InlineData(1f, false)]
    [InlineData(20f, true)]
    public void VerticalDriveCannotLiftBeyondItsForceAndEnergySupply(float force, bool lifts)
    {
        var source = new LawScene(false);
        var support = source.StaticBox(new(0, 5, 0), new(.1f));
        var head = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: -9.81f);
        source.Prismatic(support, head, ConstraintFrame.Identity, ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        var initial = source.Admit().Scene;
        var joint = initial.Prismatics[0].Id;
        source.Drive(joint, 1f, .5f, 2f, force, force * .5f);
        var world = source.Admit();
        var final = world.Step(600, (tick, state) =>
        {
            var motor = PhysicsGpuAbi.ReadPrismaticMotor(state.World, 0);
            Assert.InRange(MathF.Abs(motor.Impulse), 0f, force / 480f + 1e-6f); // Local impulse/force interface.
            Assert.InRange(motor.TickWork, 0f, force * .5f / 120f + 1e-6f); // Declared per-tick energy budget.
            var body = state[head];
            var gained = .5f * 9.81f * (body.Position.Y - 5f) + .25f * body.Velocity.LengthSquared();
            Assert.True(gained <= motor.Work + .005f, "The motor must account for useful gravitational and kinetic energy.");
        }, supplied: true);
        if (lifts) Assert.True(final[head].Position.Y > 5.9f, "A drive above the 4.905 N weight must complete useful lift.");
        else Assert.InRange(final[head].Position.Y, 4.98f, 5.02f);
    }

    [Theory]
    [InlineData(0f, true)]
    [InlineData(2f, false)]
    public void ExternalOverspeedGetsBrakingRatherThanPositiveMotorEnergy(float speed, bool drives)
    {
        var source = new LawScene(false);
        var support = source.StaticBox(new(0, 5, 0), new(.1f));
        var head = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino,
            velocity: new(0, speed, 0), gravity: 0f);
        var joint = source.Prismatic(support, head, ConstraintFrame.Identity, ConstraintFrame.Identity,
            0f, 4f, 0f, 0f, ConnectedCollision.Disabled);
        source.Drive(joint, 3f, .5f, 2f, 10f, 5f);
        var final = source.Admit().Step(1, supplied: true);
        var motor = PhysicsGpuAbi.ReadPrismaticMotor(final.World, 0);
        if (drives) { Assert.True(motor.Impulse > 0f); Assert.True(motor.Work > 0f); }
        else { Assert.True(motor.Impulse < 0f); Assert.Equal(0f, motor.Work); }
    }

    [Fact]
    public void BudgetLimitedDriveAndStalledDriveShareMovingSupportWithoutInventingWork()
    {
        var source = new LawScene(false);
        var support = source.Box(new(0, 5, 0), new(.1f), 2f, new(0f, .1f, 0f, 0f), gravity: 0f);
        source.ChildCollider(support, ColliderShapeKind.Box, new(-.5f, .25f, 0f), 0f, new(.1f));
        var free = source.Box(new(.5f, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0f);
        var pinned = source.Box(new(-.5f, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0f);
        var limited = source.Prismatic(support, free, new(new(.5f, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        var stalled = source.Prismatic(support, pinned, new(new(-.5f, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Enabled);
        source.Drive(limited, 1f, 1f, 30f, 100f, .1f);
        source.Drive(stalled, 1f, .5f, 2f, 20f, 10f);
        var settled = 0f;
        var committedWork = Vector2.Zero;
        var maximumPenetration = float.NegativeInfinity;
        var minimumPenetration = float.PositiveInfinity;
        var final = source.Admit().Step(1200, (tick, state) =>
        {
            var first = PhysicsGpuAbi.ReadPrismaticMotor(state.World, 0);
            var second = PhysicsGpuAbi.ReadPrismaticMotor(state.World, 1);
            Assert.InRange(first.TickWork, 0f, .1f / 120f + 1e-6f);
            Assert.InRange(second.TickWork, 0f, 10f / 120f + 1e-6f);
            PhysicsGpuAbi.ReadDynamicBodies(state.World).ValidateTime(new(1), new((ulong)tick));
            Assert.InRange(MathF.Abs(first.Impulse), 0f, 100f / 480f + 1e-6f);
            Assert.InRange(MathF.Abs(second.Impulse), 0f, 20f / 480f + 1e-6f);
            committedWork += new Vector2(first.TickWork, second.TickWork);
            if (tick == 1080) settled = second.Work;
            {
                var frame = state[support];
                var cargo = state[pinned];
                var inverse = Quaternion.Conjugate(frame.Rotation);
                var centre = Vector3.Transform(cargo.Position - frame.Position, inverse);
                var relative = inverse * cargo.Rotation;
                var upperExtent = .1f * (MathF.Abs(Vector3.Transform(Vector3.UnitX, relative).Y) +
                    MathF.Abs(Vector3.Transform(Vector3.UnitY, relative).Y) +
                    MathF.Abs(Vector3.Transform(Vector3.UnitZ, relative).Y));
                var penetration = centre.Y + upperExtent - .15f;
                Assert.True(penetration <= .01f, "The pinned head must never traverse the ceiling; temporary penetration is at most 5% of its 20 cm extent.");
                if (tick >= 1080)
                {
                    maximumPenetration = MathF.Max(maximumPenetration, penetration);
                    minimumPenetration = MathF.Min(minimumPenetration, penetration);
                }
            }
        }, supplied: true);
        var supportState = final[support];
        var axis = Vector3.Transform(Vector3.UnitY, supportState.Rotation);
        var qPinned = Vector3.Dot(final[pinned].Position - supportState.Position -
            Vector3.Transform(new(-.5f, 0, 0), supportState.Rotation), axis);
        var qFree = Vector3.Dot(final[free].Position - supportState.Position -
            Vector3.Transform(new(.5f, 0, 0), supportState.Rotation), axis);
        // Actual top face versus the support-owned ceiling's underside throughout the last second.
        // Powered indentation may reach 3 mm, leaving 197 mm of ceiling solid; 1 mm excursion excludes continuing sink.
        // Passive resting support keeps its separate 0.5 mm bound.
        Assert.True(minimumPenetration >= -.0005f && maximumPenetration <= .003f &&
            maximumPenetration - minimumPenetration <= .001f,
            $"Settled ceiling penetration [{minimumPenetration}, {maximumPenetration}] m; final axial travel {qPinned} m.");
        Assert.True(qFree > .1f, "The budget-limited motor must still produce useful relative motion.");
        Assert.InRange(MathF.Abs(PhysicsGpuAbi.ReadPrismaticMotor(final.World, 0).Work - committedWork.X), 0f, .005f);
        Assert.InRange(MathF.Abs(PhysicsGpuAbi.ReadPrismaticMotor(final.World, 1).Work - committedWork.Y), 0f, .005f);
        Assert.InRange(PhysicsGpuAbi.ReadPrismaticMotor(final.World, 1).Work - settled, 0f, .005f);
        Assert.True(Vector3.Distance(supportState.Position, new(0, 5, 0)) > .02f);
    }

    [Fact]
    public void NearbyNoncontactingMotorDoesNotThrottleAnIndependentLoadedDrive()
    {
        static float Run(float neighbourX)
        {
            var source = new LawScene(false);
            var a = source.StaticBox(new(neighbourX, 5, 0), new(.1f));
            var b = source.Box(new(neighbourX, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0f);
            var first = source.Prismatic(a, b, ConstraintFrame.Identity, ConstraintFrame.Identity,
                0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
            source.Drive(first, 1f, 1f, 30f, 100f, .1f);
            var c = source.StaticBox(new(0, 5, 0), new(.1f));
            var d = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: -9.81f);
            var second = source.Prismatic(c, d, ConstraintFrame.Identity, ConstraintFrame.Identity,
                0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
            source.Drive(second, 1f, .5f, 2f, 4.95f, 2.475f);
            return source.Admit().Step(1200, supplied: true)[d].Position.Y;
        }
        var nearby = Run(.22f); // 2 cm solid separation, inside the conservative reachable broadphase bounds.
        var distant = Run(3f);
        Assert.True(nearby > 5.9f, "The independently loaded drive must retain enough effort to lift its cargo.");
        Assert.InRange(nearby - distant, -.02f, .02f); // A false contact candidate cannot change a useful stroke by more than 2 cm.
    }

    [Fact]
    public void MovingSupportReceivesEqualReactionAndSharesTheRelativeMotionWork()
    {
        var source = new LawScene(false);
        var support = source.Box(new(0, 5, 0), new(.1f), 2f, LawMaterial.Domino, gravity: 0f);
        var head = source.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0f);
        var joint = source.Prismatic(support, head, ConstraintFrame.Identity, ConstraintFrame.Identity,
            0f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        source.Drive(joint, 1f, .5f, 2f, 10f, 5f);
        var final = source.Admit().Step(180, (_, state) =>
        {
            var a = state[support];
            var b = state[head];
            var motor = PhysicsGpuAbi.ReadPrismaticMotor(state.World, 0);
            Assert.InRange((2f * a.Position.Y + .5f * b.Position.Y) / 2.5f, 4.999f, 5.001f); // No external axial force: centre of mass stays within 1 mm.
            Assert.InRange(MathF.Abs(2f * a.Velocity.Y + .5f * b.Velocity.Y), 0f, .005f); // No spurious useful net momentum.
            var kinetic = a.Velocity.LengthSquared() + .25f * b.Velocity.LengthSquared();
            Assert.True(kinetic <= motor.Work + .005f);
        }, supplied: true);
        Assert.True(final[support].Position.Y < 4.9f);
        Assert.True(final[head].Position.Y > 5.4f);
    }
}
