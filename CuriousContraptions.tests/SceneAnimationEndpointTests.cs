using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneAnimationEndpointTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(AnimationRotationAxis.X)]
    [InlineData(AnimationRotationAxis.Y)]
    [InlineData(AnimationRotationAxis.Z)]
    public void ReversingChildClipWritesOnlyOnFramesAndResetRestoresExactAuthoredPose(AnimationRotationAxis axis)
    {
        var node=new Node3D();godot.Tree.Root.AddChild(node);
        try
        {
            node.Transform=new(new Basis(Vector3.Up,.3f),new(2,3,4));
            var baseline=node.Transform;
            var adapter=new SceneAnimationAdapter(1);
            var target=adapter.Register(node);
            var animation=adapter.BindRotation(target,
                new(-.25,.25,.125,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),axis);
            adapter.Present(1,0,0);var initial=node.Transform;
            adapter.DriveTo(animation,AnimationEndpoint.To,0);
            adapter.DriveTo(animation,AnimationEndpoint.From,.0625);
            Assert.Equal(initial,node.Transform);
            Assert.Equal(0,adapter.Read(animation).Value);
            var work=adapter.Present(2,.09375,0);
            var direction=axis switch
            {
                AnimationRotationAxis.X=>Vector3.Right,
                AnimationRotationAxis.Y=>Vector3.Up,
                AnimationRotationAxis.Z=>Vector3.Back,
                _=>throw new ArgumentOutOfRangeException(nameof(axis))
            };
            var expected=baseline.Basis*new Basis(direction,-.125f);
            Assert.InRange((expected.X-node.Basis.X).Length(),0,1e-6f);
            Assert.InRange((expected.Y-node.Basis.Y).Length(),0,1e-6f);
            Assert.InRange((expected.Z-node.Basis.Z).Length(),0,1e-6f);
            Assert.Equal(baseline.Origin,node.Position);
            Assert.Equal(1,work.TransformWrites);
            Assert.Equal(0,adapter.Present(3,.09375,0).TransformWrites);
            adapter.Present(4,.125,0);Assert.Equal(initial,node.Transform);
            Assert.Equal(AnimationPlayback.Completed,adapter.Read(animation).Playback);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.DriveTo(animation,AnimationEndpoint.To,0));
        }
        finally {node.Free();}
    }
}
