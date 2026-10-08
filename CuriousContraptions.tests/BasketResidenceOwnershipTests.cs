using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BasketResidenceOwnershipTests(NativeSceneFixture godot)
{
    private enum Part { Basket, Ball }
    private static string Wire(Part part)=>part switch
    {
        Part.Basket=>"basket",Part.Ball=>"ball",_=>throw new ArgumentOutOfRangeException(nameof(part))
    };
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PhysicalDwellOwnsCaptureDespitePresentationChangesAndRestoresExactly(bool enabled)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var basket=(BasketPart)world.AddPart(new(){Id=Wire(Part.Basket),Kind=Wire(Part.Basket),Position=[0,6,0]});
            var ball=world.AddPart(new(){Id=Wire(Part.Ball),Kind=Wire(Part.Ball),Position=[0,6,0]});
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var target=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            if(!enabled)
            {
                var collider=world.Physics.Collider(target.Id).Declaration;
                world.Physics.ApplyColliderUpdates([new(target.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            }
            var initial=world.Physics.Capture();
            basket.Position=new(20,20,20);basket.Visible=false;basket.Boxes.Clear();
            ball.Position=new(-20,20,20);ball.Visible=false;
            for(var i=0;i<5;i++)basket.ObservePhysics(world,100);
            Assert.False(basket.Active);Assert.Empty(world.Events);
            world.Physics.Step([],[],.5);
            var after=world.Physics.Capture();
            Assert.Equal(enabled?PhysicsResidencePhase.Captured:PhysicsResidencePhase.Outside,
                Assert.Single(world.Physics.ResidenceStates.ToArray()).Phase);
            Assert.False(basket.Active);Assert.Empty(world.Events);
            world.Physics.Restore(initial);
            Assert.Equal(initial.ResidenceStates.ToArray(),world.Physics.ResidenceStates.ToArray());
            world.Physics.Step([],[],.5);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(after.ResidenceStates.ToArray(),world.Physics.ResidenceStates.ToArray());
            basket.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(enabled,basket.Active);
            Assert.Equal(enabled,world.Events.ContainsKey(new(MachineEventKind.Captured,basket.Uid,ball.Uid)));
            var events=world.Events.ToArray();basket.ObservePhysics(world,100);
            Assert.Equal(events,world.Events.ToArray());
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.False(Assert.Single(world.Parts.OfType<BasketPart>()).Active);
        }
        finally {world.Free();}
    }
}
