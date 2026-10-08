using System.Text.Json;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WoundSpringBrowserWorkloadTests(NativeSceneFixture godot)
{
    public enum SupplyConnection { Connected, Disconnected }
    private enum Role { Spring, Payload, Battery, Switch, Striker, Delay, Motor }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Spring=new("wound_spring"),Ball=new("ball"),
        Battery=new("battery"),Switch=new("switch"),Delay=new("delay"),Motor=new("motor");
    private static string WireId(Role role)=>role switch
    {
        Role.Spring=>"wound_spring_1",Role.Payload=>"ball_2",Role.Battery=>"battery_3",
        Role.Switch=>"switch_4",Role.Striker=>"ball_5",Role.Delay=>"delay_6",Role.Motor=>"motor_7",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Add(MachineWorld world,Role role,CatalogueId kind,float[] position)=>
        world.AddPart(new(){Id=WireId(role),Kind=kind.Value,Position=position});

    [Theory]
    [InlineData(SupplyConnection.Connected)]
    [InlineData(SupplyConnection.Disconnected)]
    public void ExactUiConstructionSurvivesReleaseAndReturningPayload(SupplyConnection supply)
    {
        if(!Enum.IsDefined(supply))throw new ArgumentOutOfRangeException(nameof(supply));
        var world=new MachineWorld {Precision=.45f,Realistic=false};godot.Tree.Root.AddChild(world);
        try
        {
            var spring=(WoundSpringPart)Add(world,Role.Spring,Spring,[0,3,0]);
            var payload=Add(world,Role.Payload,Ball,[0,4.999069f,0]);
            var battery=Add(world,Role.Battery,Battery,[-4,3,2]);
            var trigger=Add(world,Role.Switch,Switch,[4,1.9937533f,-2]);
            Add(world,Role.Striker,Ball,[4,6.993753f,-2]);
            var delay=Add(world,Role.Delay,Delay,[-3,5.989648f,-2]);
            var motor=Add(world,Role.Motor,Motor,[-3,4.9905586f,2]);
            if(supply==SupplyConnection.Connected)
                Assert.True(world.Connect(battery,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(trigger,SocketId.ActivationOut,delay,SocketId.ActivationIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(delay,SocketId.ActivationOut,spring,SocketId.ActivationIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(motor,SocketId.Drive,spring,SocketId.DriveIn,ConnectionDomain.Mechanical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();var peak=payload.Position.Y;
            try
            {
                for(var tick=0;tick<720;tick++)
                {
                    world.Step();
                    peak=Math.Max(peak,(float)world.PhysicsAssembly.Body(new(payload,MachinePart.RootBody)).Center.Y);
                }
                Assert.Equal(1,spring.ReleaseCount);
                if(supply==SupplyConnection.Connected)Assert.True(peak>6.5f);
                else Assert.True(peak<5.1f);
            }
            catch(Exception error)
            {
                Console.WriteLine($"Spring browser fixture failed: supply={supply}, tick={world.Ticks}, peak={peak:R}, releases={spring.ReleaseCount}; {error}");
                throw;
            }
            finally
            {
                world.Restore();
                Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            }
        }
        finally {world.Free();}
    }
}

