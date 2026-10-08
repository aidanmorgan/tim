using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RopeOwnershipTests(NativeSceneFixture godot)
{
    private enum Fixture { LeftLoad, RightLoad, LeftGuide, RightGuide }
    private static string Id(Fixture value)=>value switch
    {
        Fixture.LeftLoad=>"left_load",Fixture.RightLoad=>"right_load",Fixture.LeftGuide=>"left_guide",
        Fixture.RightGuide=>"right_guide",_=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    private static string Kind(Fixture value)=>value switch
    {
        Fixture.LeftLoad or Fixture.RightLoad=>"weight",
        Fixture.LeftGuide or Fixture.RightGuide=>"pulley",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SolvedRopeReadingsIgnoreSceneEditsAndDriveGuideAnimation(bool unequal)
    {
        var world=new MachineWorld{Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            MachinePart Add(Fixture value,Vector3 at,float mass=1)=>world.AddPart(new()
            {
                Id=Id(value),Kind=Kind(value),Position=[at.X,at.Y,at.Z],
                Properties=value is Fixture.LeftLoad or Fixture.RightLoad?
                    new(){[PartParameterName.Of(WeightParameter.Mass)]=mass}:new()
            });
            var a=Add(Fixture.LeftLoad,new(-2,2,.12f));
            var b=Add(Fixture.RightLoad,new(2,2,.12f),unequal?4:1);
            var left=(PulleyPart)Add(Fixture.LeftGuide,new(-2,6,0));
            var right=Add(Fixture.RightGuide,new(2,6,0));
            Assert.True(world.Connect(a,SocketId.Tie,left,SocketId.Tie,ConnectionDomain.Rope));
            Assert.True(world.Connect(left,SocketId.Tie,right,SocketId.Tie,ConnectionDomain.Rope));
            Assert.True(world.Connect(right,SocketId.Tie,b,SocketId.Tie,ConnectionDomain.Rope));
            world.Start();
            var rope=Assert.Single(world.Ropes);
            var length=rope.CurrentLength(world);var distances=rope.GuideDistances(world);
            a.Position+=Vector3.Right*8;b.Position+=Vector3.Forward*5;left.Position+=Vector3.Up*3;
            Assert.Equal(length,rope.CurrentLength(world));
            Assert.Equal(distances,rope.GuideDistances(world));
            Assert.Throws<ArgumentException>(()=>rope.AnimateGuides(world,[]));
            for(var i=0;i<30;i++)world.Step();
            Assert.InRange(Math.Abs(rope.CurrentLength(world)-rope.Length),0,.001);
            Assert.Equal(RopeState.Taut,rope.State(world));
            Assert.Equal(unequal,left.WheelAngle!=0);
            var solved=world.PhysicsAssembly.Body(new(a,MachinePart.RootBody));
            var expected=unequal?world.Gravity*3/5*.25:0;
            Assert.InRange(Math.Abs(solved.LinearVelocity.Y-expected),0,.015);
            world.Restore();
            Assert.All(world.Parts.OfType<PulleyPart>(),p=>Assert.Equal(0,p.WheelAngle));
            Assert.Equal(new Vector3(-2,2,.12f),world.FindPart(Id(Fixture.LeftLoad))!.Position);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedValues()
    {
        Assert.Equal("weight",Kind(Fixture.LeftLoad));Assert.Equal("pulley",Kind(Fixture.LeftGuide));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Fixture)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Fixture)99));
    }
}
