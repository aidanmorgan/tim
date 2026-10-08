using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpactLeverFrustumObstructionTests(NativeSceneFixture godot)
{
    public enum Route { Shell, DepthMiss, Bore }
    private enum Role { Lever, Funnel, Driver }
    private static PartSpec Spec(Role role,Vector3 at) => new()
    {
        Id=role switch { Role.Lever=>"lever",Role.Funnel=>"funnel",Role.Driver=>"driver",_=>throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind=role switch { Role.Lever=>ImpactLeverPart.CatalogId,Role.Funnel=>"funnel",Role.Driver=>"bowling",_=>throw new ArgumentOutOfRangeException(nameof(role)) },
        Position=[at.X,at.Y,at.Z]
    };

    [Theory]
    [InlineData(Route.Shell)]
    [InlineData(Route.Bore)]
    [InlineData(Route.DepthMiss)]
    public void DeclaredFrustumShellStopsTheBeamWithoutFillingTheBore(Route route)
    {
        var world=new MachineWorld { Gravity=0,Pressure=0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever=(ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var funnel=(FunnelPart)world.AddPart(Spec(Role.Funnel,
                route==Route.Bore ? new(1.5f,3,0) : new(1.5f,4.6f,route==Route.DepthMiss?2:0)));
            if (route!=Route.Bore) funnel.RotationDegrees=new(0,0,180);
            // Isolate the actual sloping shell. Collar tubes must not make a
            // missing frustum implementation look like successful blocking.
            funnel.Tubes.Clear();
            world.Start();
            LeverFixture.Push(world,lever,3);
            world.Step();
            Assert.True(LeverFixture.Angle(world,lever)>0);
            Assert.True(LeverFixture.Speed(world,lever)>0);
            for(var tick=0;tick<120;tick++) world.Step();
            if(route==Route.DepthMiss) Assert.InRange(Math.Abs(LeverFixture.Angle(world,lever)-(ImpactLeverPart.LimitAngle)),0,1e-7);
            else
            {
                Assert.InRange(LeverFixture.Angle(world,lever),.15,.45);
                Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void InitialSlopingShellOverlapRejectsRunWithoutChangingConstruction()
    {
        var world=new MachineWorld { Gravity=0,Pressure=0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var funnel=(FunnelPart)world.AddPart(Spec(Role.Funnel,new(1.5f,3.8f,0)));
            funnel.Tubes.Clear();
            var before=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(Route.Shell)]
    [InlineData(Route.Bore)]
    public void FallingLoadStopsAtSlopingShellAndResetIsExact(Route route)
    {
        var world=new MachineWorld { Gravity=9.81f,Pressure=0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var lever=(ImpactLeverPart)world.AddPart(Spec(Role.Lever,new(0,3,0)));
            var funnel=(FunnelPart)world.AddPart(Spec(Role.Funnel,
                route==Route.Bore?new(1.5f,3,0):new(1.5f,4.6f,0)));
            if(route!=Route.Bore) funnel.RotationDegrees=new(0,0,180);
            world.AddPart(Spec(Role.Driver,new(-1.2f,6,0)));
            var before=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            double maximum=0;
            for(var tick=0;tick<600;tick++)
            {
                world.Step();
                maximum=Math.Max(maximum,LeverFixture.Angle(world,lever));
                // Free flight may have zero contact events; every shared substep
                // must still consume its full duration.
                Assert.InRange(world.LastPhysicsStep.Events,0,100);
                Assert.Equal((ulong)((tick+1)*MachineWorld.Substeps),world.Physics.StepIndex);
                Assert.InRange(Math.Abs(world.Physics.Time-(tick+1)*(double)MachineWorld.Tick),0,1e-9);
            }
            Assert.InRange(maximum,.15,.45);
            Assert.InRange(Math.Abs(LeverFixture.Speed(world,lever)),0,1e-7);
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }
}
