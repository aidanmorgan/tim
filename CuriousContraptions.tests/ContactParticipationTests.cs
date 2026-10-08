using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ContactParticipationTests(NativeSceneFixture godot)
{
    public enum Mechanism { Bell, Trampoline }
    public enum Case { HiddenRendering, DisabledPayload, DisabledMechanism }
    private enum Fixture { Bell, Trampoline, Ball }
    private static string Wire(Fixture value)=>value switch
    {
        Fixture.Bell=>"bell",Fixture.Trampoline=>"trampoline",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Theory]
    [InlineData(Mechanism.Bell,Case.HiddenRendering)]
    [InlineData(Mechanism.Bell,Case.DisabledPayload)]
    [InlineData(Mechanism.Bell,Case.DisabledMechanism)]
    [InlineData(Mechanism.Trampoline,Case.HiddenRendering)]
    [InlineData(Mechanism.Trampoline,Case.DisabledPayload)]
    [InlineData(Mechanism.Trampoline,Case.DisabledMechanism)]
    public void SharedContactControlsBehaviourNotPresentation(Mechanism kind,Case scenario)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var fixture=kind switch {Mechanism.Bell=>Fixture.Bell,Mechanism.Trampoline=>Fixture.Trampoline,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
            var part=world.AddPart(new(){Id=Wire(fixture),Kind=Wire(fixture),Position=[0,6,0]});
            var at=kind==Mechanism.Bell?new Vector3(2,6,0):new Vector3(0,6.56f,0);
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[at.X,at.Y,at.Z]});
            ball.InitialVelocity=kind==Mechanism.Bell?Vector3.Left*4:Vector3.Down*6;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            part.Visible=false;ball.Visible=false;
            switch(scenario)
            {
                case Case.HiddenRendering: break;
                case Case.DisabledPayload: Disable(ball);break;
                case Case.DisabledMechanism: Disable(part);break;
                default: throw new ArgumentOutOfRangeException(nameof(scenario));
            }
            void Disable(MachinePart target)
            {
                var body=world.PhysicsAssembly.Body(new(target,MachinePart.RootBody));
                var collider=world.Physics.Collider(body.Id).Declaration;
                world.Physics.ApplyColliderUpdates([new(body.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            }
            for(var i=0;i<120;i++)world.Step();
            var count=kind switch
            {
                Mechanism.Bell=>((BellPart)part).PulseCount,
                Mechanism.Trampoline=>((TrampolinePart)part).ImpactCount,
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            Assert.Equal(scenario==Case.HiddenRendering?1:0,count);
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            if(kind==Mechanism.Trampoline)
            {
                var bed=(TrampolinePart)part;
                Assert.Equal(0,bed.ContactCount);
                Assert.InRange(bed.StoredElasticEnergy,0,1e-6);
                if(scenario==Case.HiddenRendering) Assert.True(solved.LinearVelocity.Y>0);
                else Assert.Equal(-6,solved.LinearVelocity.Y);
            }
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Empty(world.Events);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUnknownValues()
    {
        Assert.Equal("bell",Wire(Fixture.Bell));Assert.Equal("trampoline",Wire(Fixture.Trampoline));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
    }
}
