using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class CommittedFeedbackAdapterTests(NativeSceneFixture godot)
{
    public enum ColourTarget { Canvas, Material }
    public enum Writer { Physical, ClipRotation, FollowRotation, Scale, Extent, CommittedRotation }
    public enum ColourWriter { Colour, Opacity, Committed }
    private static AnimationDefinition Clip() => new(0, 1, 1, AnimationCurve.Linear,
        AnimationRepeat.Once, AnimationClock.Presentation);
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
    public void DirectRotationCoalescesWithoutClockLagAndRestoresBaseline(AnimationRotationAxis axis)
    {
        var node = Node();
        try
        {
            node.Transform = new(new Basis(Vector3.Up, .3f).Scaled(new(2, 3, 4)), new(3, -2, 7));
            var baseline = node.Transform;
            var adapter = new SceneAnimationAdapter(1);
            var target = adapter.Register(node);
            adapter.ClaimCommittedRotation(target, axis);
            adapter.QueueCommittedRotation(target, -.25);
            adapter.QueueCommittedRotation(target, -Math.Tau * .75);
            Assert.Equal(baseline, node.Transform);
            Assert.Equal(0, adapter.AnimationCount);
            var work = adapter.Present(1, 0, 0);
            var direction = axis switch
            {
                AnimationRotationAxis.X => Vector3.Right,
                AnimationRotationAxis.Y => Vector3.Up,
                AnimationRotationAxis.Z => Vector3.Back,
                _ => throw new ArgumentOutOfRangeException(nameof(axis))
            };
            Assert.Equal(new Transform3D(baseline.Basis * new Basis(direction, (float)(Math.Tau * .25)),
                baseline.Origin), node.Transform);
            Assert.Equal(1, work.TransformWrites);
            Assert.Equal(0, adapter.Present(2, 100, 50).TransformWrites);
            adapter.SetVisible(target, false);
            var displayed = node.Transform;
            adapter.QueueCommittedRotation(target, .5);
            Assert.Equal(0, adapter.Present(3, 100, 50).TransformWrites);
            adapter.QueueCommittedRotation(target, 0);
            adapter.Present(4, 100, 50);
            Assert.Equal(displayed, node.Transform);
            adapter.SetVisible(target, true);
            Assert.Equal(1, adapter.Present(5, 100, 50).TransformWrites);
            Assert.Equal(baseline, node.Transform);
            adapter.QueueCommittedRotation(target, double.MaxValue);
            Assert.Equal(1, adapter.Present(6, 100, 50).TransformWrites);
            Assert.True(node.Basis.IsFinite());
            adapter.Reset();
            Assert.Equal(baseline, node.Transform);
            Assert.Throws<ArgumentException>(() => adapter.QueueCommittedRotation(target, 0));
        }
        finally { node.Free(); }
    }

    private static void Claim(SceneAnimationAdapter adapter, SceneAnimationTargetHandle target, Writer writer)
    {
        switch (writer)
        {
            case Writer.Physical: adapter.ClaimPhysicalPose(target); break;
            case Writer.ClipRotation: adapter.BindRotation(target, Clip(), AnimationRotationAxis.X); break;
            case Writer.FollowRotation:
                adapter.BindFollowingRotation(target, new(0, 1, 0, 18, AnimationClock.Presentation), AnimationRotationAxis.X); break;
            case Writer.Scale:
                adapter.BindScale(target, new(1, 2, 1, AnimationCurve.Linear, AnimationRepeat.Once, AnimationClock.Presentation)); break;
            case Writer.Extent: adapter.ClaimScalarExtent(target, new(ScalarExtentAxis.X, -.5, .001,1)); break;
            case Writer.CommittedRotation: adapter.ClaimCommittedRotation(target, AnimationRotationAxis.Z); break;
            default: throw new ArgumentOutOfRangeException(nameof(writer));
        }
    }

    [Theory]
    [InlineData(Writer.Physical)]
    [InlineData(Writer.ClipRotation)]
    [InlineData(Writer.FollowRotation)]
    [InlineData(Writer.Scale)]
    [InlineData(Writer.Extent)]
    [InlineData(Writer.CommittedRotation)]
    public void TransformOwnershipIsExclusiveInBothClaimOrders(Writer writer)
    {
        var node = Node();
        try
        {
            var adapter = new SceneAnimationAdapter(1);
            var target = adapter.Register(node);
            Claim(adapter, target, writer);
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimCommittedRotation(target, AnimationRotationAxis.Z));
            adapter.Reset();
            target = adapter.Register(node);
            adapter.ClaimCommittedRotation(target, AnimationRotationAxis.Z);
            Assert.Throws<InvalidOperationException>(() => Claim(adapter, target, writer));
        }
        finally { node.Free(); }
    }

    [Theory]
    [InlineData(ColourTarget.Canvas)]
    [InlineData(ColourTarget.Material)]
    public void CommittedColourCoalescesHidesResumesAndRestoresExactRgba(ColourTarget kind)
    {
        var baseline = new Color(.2f, .3f, .4f, .7f);
        var canvas = new Control { Modulate = baseline };
        using var material = new StandardMaterial3D { AlbedoColor = baseline };
        godot.Tree.Root.AddChild(canvas);
        try
        {
            var adapter = new SceneAnimationAdapter(1);
            var target = kind switch
            {
                ColourTarget.Canvas => adapter.Register(canvas),
                ColourTarget.Material => adapter.Register(material),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            Color Read() => kind switch
            {
                ColourTarget.Canvas => canvas.Modulate,
                ColourTarget.Material => material.AlbedoColor,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            adapter.ClaimCommittedColour(target);
            var first = new Color(.5f, .6f, .7f, .8f);
            var latest = new Color(.8f, .7f, .6f, .5f);
            adapter.QueueCommittedColour(target, first);
            adapter.QueueCommittedColour(target, latest);
            Assert.Equal(baseline, Read());
            Assert.Equal(1, adapter.Present(1, 0, 0).ColourWrites);
            Assert.Equal(latest, Read());
            adapter.QueueCommittedColour(target, latest);
            Assert.Equal(0, adapter.Present(2, 0, 0).ColourWrites);
            adapter.SetVisible(target, false);
            adapter.QueueCommittedColour(target, first);
            Assert.Equal(0, adapter.Present(3, 0, 0).ColourWrites);
            Assert.Equal(latest, Read());
            adapter.SetVisible(target, true);
            Assert.Equal(1, adapter.Present(4, 0, 0).ColourWrites);
            Assert.Equal(first, Read());
            adapter.RemoveTarget(target, AnimationTargetRemoval.RestoreBaseline);
            Assert.Equal(baseline, Read());
            var replacement = kind == ColourTarget.Canvas ? adapter.Register(canvas) : adapter.Register(material);
            adapter.ClaimCommittedColour(replacement);
            Assert.Throws<ArgumentException>(() => adapter.QueueCommittedColour(target, latest));
            adapter.Reset();
            Assert.Equal(baseline, Read());
        }
        finally { canvas.Free(); }
    }

    private static void ClaimColour(SceneAnimationAdapter adapter, SceneAnimationTargetHandle target, ColourWriter writer)
    {
        switch (writer)
        {
            case ColourWriter.Colour: adapter.BindColour(target, Clip(), Colors.Black, Colors.White); break;
            case ColourWriter.Opacity: adapter.BindOpacity(target, Clip()); break;
            case ColourWriter.Committed: adapter.ClaimCommittedColour(target); break;
            default: throw new ArgumentOutOfRangeException(nameof(writer));
        }
    }

    [Theory]
    [InlineData(ColourWriter.Colour)]
    [InlineData(ColourWriter.Opacity)]
    [InlineData(ColourWriter.Committed)]
    public void ColourOwnershipIsExclusiveInBothOrders(ColourWriter writer)
    {
        using var material = new StandardMaterial3D { Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        var adapter = new SceneAnimationAdapter(1);
        var target = adapter.Register(material);
        ClaimColour(adapter, target, writer);
        Assert.Throws<InvalidOperationException>(() => adapter.ClaimCommittedColour(target));
        adapter.Reset();
        target = adapter.Register(material);
        adapter.ClaimCommittedColour(target);
        Assert.Throws<InvalidOperationException>(() => ClaimColour(adapter, target, writer));
    }

    [Fact]
    public void InvalidQueuesDoNotReplacePendingValuesAndFreedTargetsDoNotConsumeFrames()
    {
        var node = Node();
        using var material = new StandardMaterial3D();
        var adapter = new SceneAnimationAdapter(2);
        var n = adapter.Register(node);
        var m = adapter.Register(material);
        try
        {
            Assert.Throws<InvalidOperationException>(() => adapter.QueueCommittedRotation(n, 0));
            Assert.Throws<InvalidOperationException>(() => adapter.QueueCommittedColour(m, Colors.Black));
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimCommittedColour(n));
            Assert.Throws<InvalidOperationException>(() => adapter.ClaimCommittedRotation(m, AnimationRotationAxis.Z));
            Assert.Throws<ArgumentOutOfRangeException>(() => adapter.ClaimCommittedRotation(n, (AnimationRotationAxis)(-1)));
            adapter.ClaimCommittedRotation(n, AnimationRotationAxis.Z);
            adapter.ClaimCommittedColour(m);
            adapter.QueueCommittedRotation(n, .25);
            adapter.QueueCommittedColour(m, Colors.Black);
            foreach (var angle in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => adapter.QueueCommittedRotation(n, angle));
            foreach (var colour in new[] { new Color(float.NaN, 0, 0), new Color(0, float.PositiveInfinity, 0),
                new Color(0, 0, float.NegativeInfinity), new Color(0, 0, 0, -.1f), new Color(0, 0, 0, 1.1f) })
                Assert.Throws<ArgumentException>(() => adapter.QueueCommittedColour(m, colour));
            adapter.Present(1, 0, 0);
            Assert.Equal(new Basis(Vector3.Back, .25f), node.Basis);
            Assert.Equal(Colors.Black, material.AlbedoColor);
            adapter.QueueCommittedRotation(n, .5);
            adapter.QueueCommittedColour(m, Colors.White);
            node.Free();
            Assert.Throws<InvalidOperationException>(() => adapter.Present(2, 0, 0));
            Assert.Equal(Colors.Black, material.AlbedoColor);
            adapter.RemoveTarget(n, AnimationTargetRemoval.Detach);
            Assert.Equal(1, adapter.Present(2, 0, 0).ColourWrites);
            adapter.Reset();
        }
        finally { if (GodotObject.IsInstanceValid(node)) node.Free(); }
    }

    [Fact]
    public void WarmedDirectFeedbackAllocatesNothing()
    {
        var node = Node();
        using var material = new StandardMaterial3D();
        try
        {
            var adapter = new SceneAnimationAdapter(2);
            var n = adapter.Register(node);
            var m = adapter.Register(material);
            adapter.ClaimCommittedRotation(n, AnimationRotationAxis.Z);
            adapter.ClaimCommittedColour(m);
            void Frame(ulong frame)
            {
                adapter.QueueCommittedRotation(n, frame * .01);
                adapter.QueueCommittedColour(m, frame % 2 == 0 ? Colors.White : Colors.Black);
                adapter.Present(frame, 0, 0);
            }
            for (ulong frame = 1; frame <= 1000; frame++) Frame(frame);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (ulong frame = 1001; frame <= 2000; frame++) Frame(frame);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
            adapter.Reset();
            Assert.Equal(Transform3D.Identity, node.Transform);
        }
        finally { node.Free(); }
    }
}
