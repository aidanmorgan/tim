using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class SpringAnimationTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(30, 50, 70)]
    [InlineData(90, 0, 0)]
    public void RecoilIsLocalContinuousAndDoesNotChangePhysics(float x, float y, float z)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new()
            {
                Parts = [
                    new() { Id = "spring", Kind = "spring", Position = [0, 4, 0], Rotation = [x, y, z] },
                    new() { Id = "ball", Kind = "ball", Position = [0, 8, 0] },
                    new() { Id = "other", Kind = "ball", Position = [2, 8, 0] }
                ]
            });
            world.Start();
            var spring = (SpringPart)world.FindPart("spring")!;
            var ball = world.FindPart("ball")!;
            var box = spring.Boxes.Single();
            var plate = spring.GetNode<Node3D>("Visual/SpringPlate");
            var coil = spring.GetNode<Node3D>("Visual/SpringCoil");
            spring.OnContact(ball, .01f, world);
            Assert.Equal(0, spring.HitCount);
            spring.OnContact(ball, 2, world);
            Assert.Equal(1, spring.HitCount);
            Assert.True(ball.Velocity.DistanceTo(spring.Basis.Y.Normalized() * 8.5f) < .0001f);
            spring._Process(0);
            Assert.Equal(0, spring.PlateOffset);
            spring._Process(.07);
            Assert.InRange(spring.PlateOffset, -.2f, -.1f);
            Assert.True(coil.Scale.Y < 1);
            Assert.True(plate.GlobalPosition.DistanceTo(spring.ToGlobal(new(0, .14f + spring.PlateOffset, 0))) < .0001f);
            spring.OnContact(ball, 2, world);
            Assert.Equal(1, spring.HitCount); // Same-ball cooldown.
            var before = spring.PlateOffset;
            spring.OnContact(world.FindPart("other")!, 2, world);
            spring._Process(0);
            Assert.Equal(before, spring.PlateOffset); // New hit has no pose jump.
            Assert.Equal(2, spring.HitCount);
            world.Running = false;
            var signature = world.StateSignature();
            spring._Process(.14);
            Assert.True(spring.PlateOffset > 0);
            spring._Process(1);
            Assert.Equal(0, spring.PlateOffset);
            Assert.Equal(Vector3.One, coil.Scale);
            Assert.Equal(new Vector3(0, .14f, 0), plate.Position);
            Assert.Equal(box, spring.Boxes.Single());
            Assert.Equal(signature, world.StateSignature());
            world.Restore();
            spring = (SpringPart)world.FindPart("spring")!;
            Assert.Equal(0, spring.HitCount);
            Assert.Equal(0, spring.PlateOffset);
            Assert.Equal(Vector3.One, spring.GetNode<Node3D>("Visual/SpringCoil").Scale);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RecoilMatchesAcrossRenderRatesAndResetClearsAnActiveImpact()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(new()
            {
                Parts = [new() { Id = "spring", Kind = "spring" }, new() { Id = "ball", Kind = "ball", Position = [0, 5, 0] }]
            });
            float Sample(int frames)
            {
                world.Start();
                var spring = (SpringPart)world.FindPart("spring")!;
                spring.OnContact(world.FindPart("ball")!, 2, world);
                for (var i = 0; i < frames; i++) spring._Process(.21 / frames);
                var result = spring.PlateOffset;
                world.Restore();
                Assert.Equal(0, ((SpringPart)world.FindPart("spring")!).PlateOffset);
                Assert.Equal(0, ((SpringPart)world.FindPart("spring")!).HitCount);
                return result;
            }
            var reference = Sample(1);
            foreach (var frames in new[] { 6, 13, 30, 60 })
                Assert.InRange(Mathf.Abs(reference - Sample(frames)), 0, .00001f);
        }
        finally { world.Free(); }
    }
}
