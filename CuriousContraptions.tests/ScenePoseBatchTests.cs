using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;
using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ScenePoseBatchTests(NativeSceneFixture godot)
{
    private static BodyPoseRead Read(int id = 0, double x = 2) =>
        new(new(id), PhysicsMotionType.Dynamic, RigidPose.At(new(x, 6, 8)), RigidPose.At(new(1, 3, 4)));
    private static ScenePoseMap Rigid() => ScenePoseMap.Rigid(PoseReadSpace.World, RigidPose.Identity);
    private Node3D Node()
    {
        var node = new Node3D();
        godot.Tree.Root.AddChild(node);
        return node;
    }
    private static SceneAnimationTargetHandle Physical(SceneAnimationAdapter adapter, Node3D node)
    {
        var target = adapter.Register(node);
        adapter.ClaimPhysicalPose(target);
        return target;
    }

    [Theory]
    [InlineData(PoseReadSpace.World)]
    [InlineData(PoseReadSpace.Reference)]
    [InlineData(PoseReadSpace.Relative)]
    public void ExactConstructionMappingMovesAndRestoresWithoutAnEpsilon(PoseReadSpace space)
    {
        var basis=Basis.FromEuler(new(0,0,Mathf.DegToRad(15)));
        var construction=new Transform3D(basis,new(.2f,2,0));
        var pose=SceneGeometryAdapter.CaptureRigidPose(construction);
        var initial=new BodyPoseRead(new(0),PhysicsMotionType.Static,pose,RigidPose.Identity);
        var mapped=ScenePoseMap.Rigid(space,RigidPose.Identity);
        var exact=mapped.PreserveConstruction(initial,construction);
        Assert.Equal(construction,exact.Evaluate(initial));
        var moved=new BodyPoseRead(new(0),PhysicsMotionType.Kinematic,
            new(pose.Center+new CollisionVector(1e-8,0,0),pose.Rotation),
            RigidPose.At(new(0,1e-8,0)));
        Assert.Equal(mapped.Evaluate(moved),exact.Evaluate(moved));
        Assert.Equal(construction,exact.Evaluate(initial));
        Assert.Throws<ArgumentException>(()=>mapped.PreserveConstruction(default,construction));
        Assert.Throws<ArgumentException>(()=>mapped.PreserveConstruction(initial,default));
    }

    [Theory]
    [InlineData(PoseReadSpace.World, 2, 6, 8)]
    [InlineData(PoseReadSpace.Reference, 1, 3, 4)]
    [InlineData(PoseReadSpace.Relative, 1, 3, 4)]
    public void RigidMappingUsesDeclaredFrameAndOffset(PoseReadSpace space, float x, float y, float z)
    {
        var mapping = ScenePoseMap.Rigid(space, RigidPose.At(new(.25, .5, .75)));
        Assert.Equal(PoseMapKind.Rigid, mapping.Kind);
        Assert.Equal(new Vector3(x + .25f, y + .5f, z + .75f), mapping.Evaluate(Read()).Origin);
        var rotated = new BodyPoseRead(new(0), PhysicsMotionType.Dynamic,
            new(default, RigidRotation.FromRotationVector(new(0, 0, Math.PI / 2))), RigidPose.Identity);
        var result = ScenePoseMap.Rigid(PoseReadSpace.World, RigidPose.At(new(1, 0, 0))).Evaluate(rotated);
        Assert.InRange((result.Origin - Vector3.Up).Length(), 0, 1e-6f);
    }

    [Theory]
    [InlineData(PoseMapAxis.X, 1)]
    [InlineData(PoseMapAxis.Y, 3)]
    [InlineData(PoseMapAxis.Z, 4)]
    public void AffineMappingUsesCapturedCoordinateForTranslationAndScale(PoseMapAxis axis, double coordinate)
    {
        var mapping = ScenePoseMap.AxisAffine(PoseReadSpace.Relative, axis, RigidRotation.Identity,
            new(1, 2, 3), new(.5, 0, 0), new(1, 2, 1), new(0, .25, 0));
        var result = mapping.Evaluate(Read());
        Assert.Equal(PoseMapKind.AxisAffine, mapping.Kind);
        Assert.Equal(new Vector3((float)(1 + .5 * coordinate), 2, 3), result.Origin);
        Assert.Equal(new Vector3(0, (float)(2 + .25 * coordinate), 0), result.Basis.Y);
    }

    [Fact]
    public void InvalidEnumsCoefficientsReadsAndUnrepresentableTransformsReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScenePoseMap.Rigid((PoseReadSpace)99, RigidPose.Identity));
        Assert.Throws<ArgumentException>(() => ScenePoseMap.Rigid(PoseReadSpace.World, default));
        Assert.Throws<ArgumentException>(() => Rigid().Evaluate(default));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScenePoseMap.AxisAffine(PoseReadSpace.World,
            (PoseMapAxis)99, RigidRotation.Identity, default, default, new(1, 1, 1), default));
        Assert.Throws<ArgumentException>(() => ScenePoseMap.AxisAffine(PoseReadSpace.World,
            PoseMapAxis.X, RigidRotation.Identity, new(double.NaN, 0, 0), default, new(1, 1, 1), default));
        var singular = ScenePoseMap.AxisAffine(PoseReadSpace.World, PoseMapAxis.X,
            RigidRotation.Identity, default, default, default, default);
        Assert.Throws<ArgumentException>(() => singular.Evaluate(Read()));
        var overflow = ScenePoseMap.Rigid(PoseReadSpace.World, RigidPose.At(new(double.MaxValue, 0, 0)));
        Assert.Throws<InvalidOperationException>(() => overflow.Evaluate(Read()));
    }

    [Fact]
    public void BatchQueuesCopiedValuesAndSubmitsOnlyAtPresentationThenResetsExactly()
    {
        var node = Node();
        try
        {
            node.Position = new(10, 11, 12);
            var baseline = node.Transform;
            var adapter = new SceneAnimationAdapter(1);
            var target = Physical(adapter, node);
            var batch = new ScenePoseBatch(adapter, [new(0)], [new(target, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid())]);
            BodyPoseRead[] reads = [Read()];
            Assert.Equal(new ScenePoseBatchWork(1, 1), batch.Queue(reads));
            reads[0] = Read(x: 7);
            Assert.Equal(baseline, node.Transform);
            Assert.Equal(1, adapter.Present(1, 0, 0).TransformWrites);
            Assert.Equal(new Vector3(2, 6, 8), node.Position);
            batch.Queue([Read()]);
            Assert.Equal(0, adapter.Present(2, 0, 0).TransformWrites);
            adapter.Reset();
            Assert.Equal(baseline, node.Transform);
            Assert.Throws<ArgumentException>(() => batch.Queue([Read()]));
        }
        finally { node.Free(); }
    }

    [Fact]
    public void InvalidLaterMappingDoesNotQueueEarlierTarget()
    {
        var first = Node(); var second = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var a = Physical(adapter, first); var b = Physical(adapter, second);
            var scale = ScenePoseMap.AxisAffine(PoseReadSpace.World, PoseMapAxis.X, RigidRotation.Identity,
                default, default, new(-2, 1, 1), new(1, 0, 0));
            var batch = new ScenePoseBatch(adapter, [new(0)], [new(a, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid()), new(b, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), scale)]);
            Assert.Throws<ArgumentException>(() => batch.Queue([Read()]));
            Assert.Equal(0, adapter.Present(1, 0, 0).TransformWrites);
            Assert.Equal(Transform3D.Identity, first.Transform);
            batch.Queue([Read(x: 3)]);
            Assert.Equal(1, adapter.Present(2, 0, 0).TransformWrites);
            Assert.Equal(new Vector3(3, 6, 8), first.Position);
        }
        finally { first.Free(); second.Free(); }
    }

    [Fact]
    public void BindingAndInputIdentityConflictsRejectBeforeMutation()
    {
        var first = Node(); var second = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var a = Physical(adapter, first);
            var b = adapter.Register(second);
            var declaration = new ScenePoseTarget(a, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid());
            Assert.Throws<InvalidOperationException>(() => new ScenePoseBatch(adapter, [new(0)],
                [new(b, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid())]));
            Assert.Throws<ArgumentException>(() => new ScenePoseBatch(adapter, [new(0)], [declaration, declaration]));
            Assert.Throws<ArgumentException>(() => new ScenePoseBatch(adapter, [new(0), new(0)], [declaration]));
            Assert.Throws<ArgumentException>(() => new ScenePoseBatch(adapter, [new(1)], [declaration]));
            var batch = new ScenePoseBatch(adapter, [new(0), new(1)], [declaration]);
            Assert.Throws<ArgumentException>(() => batch.Queue([Read()]));
            Assert.Throws<ArgumentException>(() => batch.Queue([Read(), Read()]));
            Assert.Throws<ArgumentException>(() => batch.Queue([Read(), Read(2)]));
            batch.Queue([Read(1, 9), Read(0, 4)]);
            adapter.Present(1, 0, 0);
            Assert.Equal(new Vector3(4, 6, 8), first.Position);
        }
        finally { first.Free(); second.Free(); }
    }

    [Fact]
    public void FreedLaterTargetCannotPartiallyQueueAndWarmedQueueDoesNotAllocate()
    {
        var first = Node(); var second = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var a = Physical(adapter, first); var b = Physical(adapter, second);
            var batch = new ScenePoseBatch(adapter, [new(0)], [new(a, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid()), new(b, new(0), PoseReferenceBinding.Fixed(RigidPose.Identity), Rigid())]);
            BodyPoseRead[] reads = [Read()];
            for (var i = 0; i < 1000; i++) batch.Queue(reads);
            adapter.Present(1, 0, 0);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++) batch.Queue(reads);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocated);
            adapter.Present(2, 0, 0);
            var previous = first.Transform;
            second.Free();
            Assert.Throws<InvalidOperationException>(() => batch.Queue([Read(x: 8)]));
            adapter.RemoveTarget(b, AnimationTargetRemoval.Detach);
            adapter.Present(3, 0, 0);
            Assert.Equal(previous, first.Transform);
        }
        finally { first.Free(); if (GodotObject.IsInstanceValid(second)) second.Free(); }
    }
}
