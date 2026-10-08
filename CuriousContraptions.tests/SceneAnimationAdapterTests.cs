using Godot;
using CuriousContraptions.Presentation;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneAnimationAdapterTests(NativeSceneFixture godot)
{
    private static AnimationDefinition Spin() => new(0, Math.Tau, 1,
        AnimationCurve.Linear, AnimationRepeat.Loop, AnimationClock.Presentation);
    private Node3D Node()
    {
        var node = new Node3D();
        godot.Tree.Root.AddChild(node);
        return node;
    }

    [Theory]
    [InlineData(AnimationRotationAxis.X)]
    [InlineData(AnimationRotationAxis.Y)]
    [InlineData(AnimationRotationAxis.Z)]
    public void RotationAndScaleComposeOnceAndResetRestoresExactBaseline(AnimationRotationAxis axis)
    {
        var node = Node();
        try
        {
            node.Transform = new(new Basis(Vector3.Up, .3f), new(2, 3, 4));
            var baseline = node.Transform;
            var adapter = new SceneAnimationAdapter(1);
            var target = adapter.Register(node);
            var spin = adapter.BindRotation(target, Spin(), axis);
            var scale = adapter.BindScale(target, new(1, 2, 1,
                AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation));
            adapter.Start(spin); adapter.Start(scale);
            var work = adapter.Present(1, .25, 0);
            var direction = axis switch
            {
                AnimationRotationAxis.X => Vector3.Right,
                AnimationRotationAxis.Y => Vector3.Up,
                AnimationRotationAxis.Z => Vector3.Back,
                _ => throw new ArgumentOutOfRangeException(nameof(axis))
            };
            var rotation = baseline.Basis * new Basis(direction, (float)(Math.Tau * .25));
            Assert.InRange((node.Basis.X - rotation.X * 1.25f).Length(), 0, 1e-6f);
            Assert.InRange((node.Basis.Y - rotation.Y * 1.25f).Length(), 0, 1e-6f);
            Assert.Equal(baseline.Origin, node.Position);
            Assert.Equal(1, work.TransformWrites);
            Assert.Equal(0, adapter.Present(2, .25, 0).TransformWrites);
            adapter.Reset();
            Assert.Equal(baseline, node.Transform);
            Assert.Equal(0, adapter.TargetCount);
            Assert.Equal(0, adapter.AnimationCount);
            Assert.Throws<ArgumentException>(() => adapter.Start(spin));
            Assert.Throws<ArgumentException>(() => adapter.ClaimPhysicalPose(target));
        }
        finally { node.Free(); }
    }

    [Fact]
    public void PhysicalParentAndCosmeticChildHaveSeparateWriters()
    {
        var parent = Node();
        var child = new Node3D { Position = new(1, 0, 0) };
        parent.AddChild(child);
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var p = adapter.Register(parent);
            var c = adapter.Register(child);
            adapter.ClaimPhysicalPose(p);
            Assert.Throws<InvalidOperationException>(() => adapter.BindRotation(p, Spin(), AnimationRotationAxis.X));
            var spin = adapter.BindRotation(c, Spin(), AnimationRotationAxis.Y);
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimPhysicalPose(c));
            Assert.Throws<InvalidOperationException>(() => adapter.QueuePhysicalPose(c, Transform3D.Identity));
            var pose = new Transform3D(new Basis(Vector3.Up, .5f), new(4, 5, 6));
            adapter.QueuePhysicalPose(p, pose);
            Assert.Equal(Transform3D.Identity, parent.Transform);
            adapter.Start(spin);
            var work = adapter.Present(1, .25, 0);
            Assert.Equal(pose, parent.Transform);
            Assert.InRange((child.GlobalPosition - pose * child.Position).Length(), 0, 1e-6f);
            Assert.Equal(2, work.TransformWrites);
            adapter.QueuePhysicalPose(p, pose);
            Assert.Equal(0, adapter.Present(2, .25, 0).TransformWrites);
            parent.Transform = Transform3D.Identity;
            adapter.QueuePhysicalPose(p, pose);
            Assert.Equal(1, adapter.Present(3, .25, 0).TransformWrites);
            Assert.Equal(pose, parent.Transform);
            adapter.RemoveAnimation(spin);
            adapter.Present(4, .25, 0);
            Assert.Equal(Basis.Identity, child.Basis);
            adapter.ClaimPhysicalPose(c);
        }
        finally { parent.Free(); }
    }

    [Fact]
    public void OpacityPreservesPaletteAndRestoresCanvasAndMaterialBaselines()
    {
        var canvas = new Control { Modulate = new(.2f, .3f, .4f, .8f) };
        godot.Tree.Root.AddChild(canvas);
        using var material = new StandardMaterial3D { AlbedoColor = new(.6f, .5f, .2f, .9f) };
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var c = adapter.Register(canvas);
            var m = adapter.Register(material);
            var fade = new AnimationDefinition(1, 0, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation);
            adapter.Start(adapter.BindOpacity(c, fade));
            Assert.Throws<ArgumentException>(() => adapter.BindOpacity(m, fade));
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            adapter.Start(adapter.BindOpacity(m, fade));
            var work = adapter.Present(1, .25, 0);
            Assert.Equal(new Color(.2f, .3f, .4f, .75f), canvas.Modulate);
            Assert.Equal(new Color(.6f, .5f, .2f, .75f), material.AlbedoColor);
            Assert.Equal(2, work.ColourWrites);
            material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
            Assert.Throws<InvalidOperationException>(() => adapter.Present(2, .5, 0));
            Assert.Equal(.75f, canvas.Modulate.A);
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            adapter.Present(2, .5, 0);
            Assert.Equal(.5f, canvas.Modulate.A);
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimPhysicalPose(c));
            Assert.Throws<InvalidOperationException>(() => adapter.BindOpacity(m, fade));
            adapter.Reset();
            Assert.Equal(new Color(.2f, .3f, .4f, .8f), canvas.Modulate);
            Assert.Equal(new Color(.6f, .5f, .2f, .9f), material.AlbedoColor);
        }
        finally { canvas.Free(); }
    }

    [Fact]
    public void HiddenTargetBoundLaterResumesAtCurrentPhase()
    {
        var node = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(1);
            var target = adapter.Register(node);
            adapter.SetVisible(target, false);
            var spin = adapter.BindRotation(target, Spin(), AnimationRotationAxis.X);
            adapter.Start(spin);
            Assert.Equal(0, adapter.Present(1, 2.25, 0).TransformWrites);
            Assert.Equal(Transform3D.Identity, node.Transform);
            adapter.SetVisible(target, true);
            Assert.Equal(1, adapter.Present(2, 2.25, 0).TransformWrites);
            Assert.InRange((node.Basis.Y - new Basis(Vector3.Right, (float)(Math.Tau * .25)).Y).Length(), 0, 1e-6f);
        }
        finally { node.Free(); }
    }

    [Fact]
    public void RemovalReleasesAliasesAndStaleHandlesCannotTouchReusedSlots()
    {
        var node = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(1);
            var old = adapter.Register(node);
            Assert.Throws<ArgumentException>(() => adapter.Register(node));
            var spin = adapter.BindRotation(old, Spin(), AnimationRotationAxis.X);
            adapter.Start(spin); adapter.Present(1, .25, 0);
            adapter.RemoveTarget(old, AnimationTargetRemoval.RestoreBaseline);
            Assert.Equal(Transform3D.Identity, node.Transform);
            var replacement = adapter.Register(node);
            Assert.NotEqual(old.Version, replacement.Version);
            Assert.Throws<ArgumentException>(() => adapter.SetVisible(old, true));
            Assert.Throws<ArgumentException>(() => adapter.Start(spin));
            var foreign = new SceneAnimationAdapter(1);
            Assert.Throws<ArgumentException>(() => foreign.ClaimPhysicalPose(replacement));
        }
        finally { node.Free(); }
    }

    [Fact]
    public void FreedTargetRejectsBeforeFrameConsumptionAndExplicitDetachmentRecovers()
    {
        var node = Node();
        var adapter = new SceneAnimationAdapter(1);
        var target = adapter.Register(node);
        adapter.Start(adapter.BindRotation(target, Spin(), AnimationRotationAxis.X));
        node.Free();
        Assert.Throws<InvalidOperationException>(() => adapter.Present(1, .25, 0));
        adapter.RemoveTarget(target, AnimationTargetRemoval.Detach);
        var replacement = Node();
        try
        {
            var next = adapter.Register(replacement);
            var clip = adapter.BindRotation(next, Spin(), AnimationRotationAxis.X);
            adapter.Start(clip);
            Assert.Equal(1, adapter.Present(1, .25, 0).TransformWrites);
            Assert.InRange(adapter.Read(clip).Value, 1.57079, 1.57080);
        }
        finally { replacement.Free(); }
    }

    [Fact]
    public void InvalidInputsAndConflictingClaimsReject()
    {
        var node = Node();
        var second = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(1);
            Assert.Throws<ArgumentNullException>(() => adapter.Register((Node3D)null!));
            var target = adapter.Register(node);
            Assert.Throws<InvalidOperationException>(() => adapter.Register(second));
            Assert.Throws<ArgumentOutOfRangeException>(() => adapter.BindRotation(target, Spin(), (AnimationRotationAxis)99));
            Assert.Throws<ArgumentException>(() => adapter.BindOpacity(target, Spin()));
            Assert.Throws<ArgumentOutOfRangeException>(() => adapter.BindScale(target,
                new(1, 1e7, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation)));
            adapter.ClaimPhysicalPose(target);
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimPhysicalPose(target));
            Assert.Throws<ArgumentException>(() => adapter.QueuePhysicalPose(target, new(default(Basis), Vector3.Zero)));
            Assert.Equal(Transform3D.Identity, node.Transform);
        }
        finally { node.Free(); second.Free(); }
    }

    [Fact]
    public void WarmedSceneSubmissionAllocatesNoManagedMemory()
    {
        var nodes = new Node3D[8];
        var adapter = new SceneAnimationAdapter(nodes.Length);
        try
        {
            var definition = Spin();
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = Node();
                adapter.Start(adapter.BindRotation(adapter.Register(nodes[i]), definition, AnimationRotationAxis.Y));
            }
            for (ulong frame = 1; frame <= 1000; frame++) adapter.Present(frame, frame / 60.0, 0);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (ulong frame = 1001; frame <= 2000; frame++) adapter.Present(frame, frame / 60.0, 0);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0, allocated);
        }
        finally { foreach (var node in nodes) if (GodotObject.IsInstanceValid(node)) node.Free(); }
    }
}
