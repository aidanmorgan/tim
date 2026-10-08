using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineOwnershipTests(NativeSceneFixture godot)
{
    private enum Role { Bed, Payload }
    private static string Id(Role role)=>role switch
    {
        Role.Bed=>"bed",Role.Payload=>"payload",_=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private const string BallCatalogue="ball";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedEngagementDoesNotNeedScenePreparationPresentationOrObservation(bool corruptPresentation)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var bed=(TrampolinePart)world.AddPart(new(){Id=Id(Role.Bed),Kind=TrampolinePart.CatalogId,Position=[0,5,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Payload),Kind=BallCatalogue,Position=[0,5.64f,0],InitialVelocity=[0,-1,0]});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();world.Step(); // Install all potential loads while still above the membrane.
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var frame=world.PhysicsAssembly.Body(new(bed,MachinePart.RootBody));
            var initial=world.Physics.Capture();
            Assert.Equal(0,bed.ContactCount);
            Assert.Equal(CompliantContactPhase.Ready,Assert.Single(world.Physics.CompliantContacts.ToArray()).Phase);
            if(corruptPresentation)
            {
                ball.Position=new(9,15,7);bed.Position=new(-9,15,-7);bed.Boxes.Clear();ball.Visible=false;
            }
            var rendered=ball.Position;
            var result=world.Physics.Step([],[],.13); // No MachineWorld/part callbacks.
            var state=Assert.Single(world.Physics.CompliantContacts.ToArray());
            Assert.Equal(new CompliantContactKey(body.Id,frame.Id),state.Key);
            Assert.Equal(CompliantContactPhase.Engaged,state.Phase);
            Assert.True(body.LinearVelocity.Y> -1);
            Assert.Equal(rendered,ball.Position);Assert.Equal(0,bed.ContactCount);
            var final=world.Physics.Capture().BodyStates.ToArray();
            var entries=world.Physics.CompliantEntries.ToArray();
            bed.ObservePhysics(world,.13f);Assert.Equal(1,bed.ContactCount);Assert.Equal(1,bed.ImpactCount);
            var observedBody=body.Snapshot();bed._Process(.1);
            Assert.Equal(observedBody,body.Snapshot()); // Rendering cannot feed motion back.
            world.Physics.Restore(initial);Assert.Equal(result,world.Physics.Step([],[],.13));
            Assert.Equal(final,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(state,Assert.Single(world.Physics.CompliantContacts.ToArray()));
            Assert.Equal(entries,world.Physics.CompliantEntries.ToArray());
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=Assert.IsType<TrampolinePart>(world.FindPart(Id(Role.Bed)));
            Assert.Equal(0,restored.ContactCount);Assert.Equal(0,restored.ImpactCount);
        }
        finally {world.Free();}
    }
}
