using System.Text.Json;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ObjectiveOwnershipTests(NativeSceneFixture godot)
{
    private static readonly FixturePartId Target=FixturePartId.First;
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>
        JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Fact]
    public void LoadedAndSavedListsCannotChangeLiveGoalsAndResetPreservesThem()
    {
        var world=World();
        try
        {
            var goal=new GoalSpec {Type=GoalKind.Activated,Target=FixtureParts.Id(Target)};
            var data=new MachineData {Gravity=0,Pressure=0,Goals=[goal]};
            world.LoadMachine(data);
            var construction=Saved(world);
            data.Goals.Clear();
            var snapshot=world.Snapshot();
            snapshot.Goals[0]=goal with {Type=GoalKind.Powered};
            Assert.Equal(goal,Assert.Single(world.Objectives));
            var list=Assert.IsAssignableFrom<IList<GoalSpec>>(world.Objectives);
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(()=>list.Clear());
            Assert.Throws<NotSupportedException>(()=>list[0]=goal with {Type=GoalKind.Powered});
            world.Start();world.Step();
            Assert.False(world.Won);
            world.Events.Add(new(MachineEventKind.Activated,FixtureParts.Id(Target)),world.Ticks);
            world.Step();Assert.True(world.Won);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(GoalKind.Unknown)]
    [InlineData((GoalKind)999)]
    public void UnsupportedKindsRejectBeforeReplacingTheWorld(GoalKind kind)
    {
        var world=World();
        try
        {
            var data=new MachineData {Goals=[new(){Type=kind}]};
            Assert.Throws<ArgumentException>(()=>world.ValidateMachine(data));
            var before=Saved(world);
            Assert.ThrowsAny<Exception>(()=>world.LoadMachine(data));
            Assert.Equal(before,Saved(world));
        }
        finally {world.Free();}
    }

    public static IEnumerable<object[]> SupportedKinds()
    {
        foreach(var kind in Enum.GetValues<GoalKind>())
            if(kind!=GoalKind.Unknown)yield return [kind];
    }
    [Theory]
    [MemberData(nameof(SupportedKinds))]
    public void ImmutableGoalsRoundTripThroughCurrentSaveBoundary(GoalKind kind)
    {
        var world=World();
        try
        {
            var goal=new GoalSpec {Type=kind,Target=FixtureParts.Id(Target),
                Body=FixtureParts.Id(FixturePartId.Second),MinimumDelaySeconds=.5f};
            world.LoadMachine(new(){Goals=[goal]});
            Assert.Equal(goal,Assert.Single(world.Objectives));
            var saved=Saved(world);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }
}
