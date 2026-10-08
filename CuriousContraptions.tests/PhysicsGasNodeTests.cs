using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsGasNodeTests
{
    private static PhysicsWorld World()
    {
        var body = new PhysicsBody(new(0), PhysicsMotionType.Static, RigidPose.Identity, default, default);
        return new([], [new(body, new([new(new ConvexSphere(.1), AffineTransform.Identity)]),
            new(0, 0, 0))], [], new(default));
    }

    private static PhysicsGasNode Node(int id, int owner = 0) =>
        new(new(id), new(owner), new(new(200, 400, 100, 1000, 100, 1e7), .02, .01, 2400));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleChambersHaveCanonicalOwnedReadsAndExactConstructionRestoration(bool reverse)
    {
        var world = World();
        var empty = world.Capture();
        PhysicsGasNode[] declarations = [Node(7), Node(2)];
        if (reverse) Array.Reverse(declarations);
        world.InstallGasNodes(declarations);
        var construction = world.Capture();
        Assert.Equal(new[] {new PhysicsGasNodeId(2), new PhysicsGasNodeId(7)},
            world.GasNodes.ToArray().Select(node => node.Id));
        Assert.Equal(4800, world.GasNodes.ToArray().Sum(node => node.State.InternalEnergy));
        Assert.Equal(.04, world.GasNodes.ToArray().Sum(node => node.State.Mass));
        declarations[0] = Node(90);
        Assert.Equal(2, world.GasNodes.Length);
        Assert.Throws<ArgumentException>(() => world.GasNode(new(90)));
        world.Step([], [], .01);
        Assert.Equal(construction.GasStates.ToArray(), world.GasNodes.ToArray());
        var held = world.GasNodes.ToArray();
        world.Restore(empty);
        Assert.Empty(world.GasNodes.ToArray());
        Assert.Equal(2, held.Length);
        Assert.Equal(2, construction.GasStates.Length);
        world.Restore(construction);
        Assert.Equal(0UL, world.StepIndex);
        Assert.Equal(construction.GasStates.ToArray(), world.GasNodes.ToArray());
        world.Step([], [], .01);
        Assert.Equal(held, world.GasNodes.ToArray());
    }

    [Fact]
    public void DuplicateUnknownOwnerAndNullNodesRejectWholeBatch()
    {
        var world = World();
        world.InstallGasNodes([Node(2)]);
        var original = world.GasNodes.ToArray();
        Assert.Throws<ArgumentException>(() => world.InstallGasNodes([Node(3), Node(2)]));
        Assert.Equal(original, world.GasNodes.ToArray());
        Assert.Throws<ArgumentException>(() => world.InstallGasNodes([Node(3), Node(4, 99)]));
        Assert.Equal(original, world.GasNodes.ToArray());
        Assert.Throws<ArgumentNullException>(() => world.InstallGasNodes([Node(3), null!]));
        Assert.Equal(original, world.GasNodes.ToArray());
        Assert.Throws<ArgumentException>(() => world.InstallGasNodes([Node(3), Node(3)]));
        Assert.Equal(original, world.GasNodes.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicsGasNodeId(-1));
        Assert.Throws<ArgumentNullException>(() => new PhysicsGasNode(new(1), new(0), null!));
        Assert.Throws<ArgumentException>(() => world.GasNode(new(99)));
    }

    [Fact]
    public void InitialGasCannotBeCreatedAfterSimulationBeginsAndForeignSnapshotsReject()
    {
        var world = World();
        world.InstallGasNodes([Node(0)]);
        var initial = world.Capture();
        world.Step([], [], .01);
        var committed = world.Capture();
        Assert.Throws<InvalidOperationException>(() => world.InstallGasNodes([Node(1)]));
        Assert.Equal(committed.GasStates.ToArray(), world.GasNodes.ToArray());
        Assert.Throws<ArgumentException>(() => World().Restore(committed));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.Step([], [], double.NaN));
        Assert.Equal(committed.GasStates.ToArray(), world.GasNodes.ToArray());
        Assert.Equal(committed.Time, world.Time);
        world.Restore(initial);
        world.InstallGasNodes([Node(1)]);
        Assert.Equal(2, world.GasNodes.Length);
    }

    private sealed record EffectState : PhysicsImpactEffectState;
    private sealed class InstallDuringImpact(PhysicsBodyId owner, Action install) : PhysicsImpactEffect(owner)
    {
        public override PhysicsImpactEffectState Capture() => new EffectState();
        public override void Restore(PhysicsImpactEffectState state)
        {
            if (state is not EffectState) throw new ArgumentException("Unexpected effect snapshot.", nameof(state));
        }
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            install();
            return new([], [], []);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImpactCannotInstallGasAndRejectedStepRestoresOwnedState(bool handleRejection)
    {
        var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, RigidPose.At(new(-1, 0, 0)),
            new(2, 0, 0), default, 1, new(1, 1, 1));
        var owner = new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.Identity, default, default);
        PhysicsWorld world = null!;
        var calls = 0;
        var effect = new InstallDuringImpact(owner.Id, () =>
        {
            calls++;
            if (handleRejection)
                Assert.Throws<InvalidOperationException>(() => world.InstallGasNodes([Node(4, 1)]));
            else
                world.InstallGasNodes([Node(4, 1)]);
        });
        var geometry = new CompoundGeometry([new(new ConvexSphere(.1), AffineTransform.Identity)]);
        world = new([effect], [new(body, geometry, new(0, 0, 0)), new(owner, geometry, new(0, 0, 0))],
            [], new(default, maximumStep: .1));
        world.InstallGasNodes([Node(2, 1)]);
        var before = world.Capture();
        if (handleRejection) world.Step([], [], .5);
        else
        {
            Assert.Throws<InvalidOperationException>(() => world.Step([], [], .5));
            Assert.Equal(before.BodyStates.ToArray(), world.Capture().BodyStates.ToArray());
            Assert.Equal(before.Time, world.Time);
            Assert.Equal(before.StepIndex, world.StepIndex);
        }
        Assert.True(calls > 0);
        Assert.Equal(before.GasStates.ToArray(), world.GasNodes.ToArray());
        Assert.Equal(PhysicsWorldPhase.Idle, world.Phase);
        Assert.Throws<ArgumentException>(() => world.GasNode(new(4)));
    }
}
