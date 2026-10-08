using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class DominoOwnershipTests(NativeSceneFixture godot)
{
    private enum Part { Tile, Counter }
    private static string Catalog(Part part)=>part switch
    {
        Part.Tile=>"domino",Part.Counter=>"counter",_=>throw new ArgumentOutOfRangeException(nameof(part))
    };
    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void RawPhysicsOwnsTiltBeforeAnyScenePreparationAndRestoresExactly(bool rotated,bool tip)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var tile=(DominoPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Catalog(Part.Tile),
                Position=[0,5,0],Orientation=rotated?PartOrientation.FromEulerDegrees(20,30,40):PartOrientation.FromEulerDegrees(0,0,0)});
            var counter=(CounterPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Catalog(Part.Counter),Position=[4,5,0]});
            Assert.True(world.Connect(tile,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(tile,MachinePart.RootBody));
            var spin=body.Pose.Rotation.Apply(tip?new(2,0,0):new(0,2,0));
            world.Physics.ApplyAngularImpulse(body.Id,body.LocalInertia.Rotated(body.Pose.Rotation).Apply(spin));
            var initial=world.Physics.Capture();
            tile.Position=new(20,20,20);tile.Rotation=new(2,2,2);tile.Visible=false;tile.Boxes.Clear();
            for(var i=0;i<3;i++)tile.ObservePhysics(world,100);
            Assert.False(tile.Active);Assert.Equal(0,counter.Count);
            world.Physics.Step([],[],.5);
            Assert.Equal(tip?PhysicsTiltPhase.Triggered:PhysicsTiltPhase.Waiting,world.Physics.TiltState(body.Id).Phase);
            Assert.False(tile.Active);Assert.Equal(0,counter.Count);
            var after=world.Physics.Capture();
            world.Physics.Restore(initial);world.Physics.Step([],[],.5);
            Assert.Equal(after.TiltStates.ToArray(),world.Physics.TiltStates.ToArray());
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            tile.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(tip,tile.Active);Assert.Equal(tip?1:0,counter.Count);
            Assert.Equal(tip,world.Events.ContainsKey(new(MachineEventKind.Activated,tile.Uid)));
            tile.ObservePhysics(world,100);Assert.Equal(tip?1:0,counter.Count);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.False(Assert.Single(world.Parts.OfType<DominoPart>()).Active);
            Assert.Equal(0,Assert.Single(world.Parts.OfType<CounterPart>()).Count);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUnknownPart()=>Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog((Part)int.MaxValue));
}
