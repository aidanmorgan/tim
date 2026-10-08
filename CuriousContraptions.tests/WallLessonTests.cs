using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WallLessonTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData("wall_return", 0f)]
    [InlineData("wall_return", .45f)]
    [InlineData("wall_return", 1f)]
    [InlineData("wall_and_bumper", 0f)]
    [InlineData("wall_and_bumper", .45f)]
    [InlineData("wall_and_bumper", 1f)]
    public void ReferenceNeedsBothWallAndBumper(string id, float precision)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json"))
                .Single(p => p.Id == id);
            foreach (var omitted in new[] { "", "wall", "bumper" })
            {
                var machine = MachineCodec.Clone(puzzle.CreateMachine());
                machine.Parts.AddRange(puzzle.Solution);
                machine = MachineCodec.Clone(machine);
                machine.Parts.RemoveAll(p => p.Kind == omitted);
                world.Precision = precision;
                world.LoadMachine(machine);
                world.Start();
                for (var i = 0; i < 1800 && world.Running; i++) world.Step();
                Assert.Equal(omitted == "", world.Won);
                world.Restore();
                if (omitted != "wall")
                {
                    var wall = world.Parts.OfType<WallPart>().Single();
                    Assert.Equal(new Vector3(.4f, 6, 1.5f), wall.Dimensions);
                }
            }
        }
        finally { world.Free(); }
    }
}
