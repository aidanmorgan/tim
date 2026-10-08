using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WoundSpringOwnershipTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawWorldOwnsWindingReleaseAndExactRestorationWithoutObservers(bool wind)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var spring=(WoundSpringPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=WoundSpringPart.CatalogId,Position=[0,5,0]});
            spring.Plunger.InitialVelocity=wind?new(0,-1,0):Vector3.Zero;
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();var initial=world.Physics.Capture();
            var guide=world.PhysicsAssembly.JointId(new(spring,WoundSpringPart.PlungerGuide));
            spring.Position=new(20,20,20);spring.Visible=false;spring.Boxes.Clear();
            spring.Plunger.Position=new(-20,20,20);spring.Plunger.Visible=false;
            world.Physics.Step([],[],.5);
            var wound=world.Physics.Spring(guide);
            Assert.Equal(wind,wound.Energy>0);
            Assert.Equal(wound.Energy,spring.StoredEnergy);
            Assert.Equal(wound.AcceptedWork,spring.AcceptedWork);
            Assert.Equal(0,spring.ReleaseCount);
            var ready=world.Physics.Capture();
            var result=world.Physics.ReleaseSpring(guide);
            Assert.Equal(wind?SpringTriggerResult.Released:SpringTriggerResult.Empty,result);
            world.Physics.Step([],[],.25);
            Assert.Equal(wind?1:0,spring.ReleaseCount);
            Assert.Equal(world.Physics.Spring(guide).ReleasedWork,spring.ReleasedWork);
            var after=world.Physics.Capture();
            world.Physics.Restore(ready);
            Assert.Equal(ready.Springs.ToArray(),world.Physics.Springs.ToArray());
            Assert.Equal(result,world.Physics.ReleaseSpring(guide));world.Physics.Step([],[],.25);
            Assert.Equal(after.Springs.ToArray(),world.Physics.Springs.ToArray());
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Physics.Restore(initial);
            Assert.Equal(0,spring.StoredEnergy);Assert.Equal(0,spring.AcceptedWork);Assert.Equal(0,spring.ReleaseCount);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,Assert.Single(world.Parts.OfType<WoundSpringPart>()).StoredEnergy);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(0,0,-15,false)]
    [InlineData(0,0,-15,true)]
    [InlineData(0,0,15,false)]
    [InlineData(0,0,15,true)]
    [InlineData(15,0,0,false)]
    [InlineData(15,0,0,true)]
    [InlineData(15,30,-15,false)]
    [InlineData(15,30,-15,true)]
    public void RotatedSpringUsesDeclaredStopsAndOwnedRelease(float x,float y,float z,bool wind)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var spring=(WoundSpringPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=WoundSpringPart.CatalogId,Position=[0,5,0],Orientation=PartOrientation.FromEulerDegrees(x,y,z)});
            spring.Plunger.InitialVelocity=wind?-spring.Basis.Y:Vector3.Zero;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var guideId=world.PhysicsAssembly.JointId(new(spring,WoundSpringPart.PlungerGuide));
            var load=Assert.Single(world.Physics.Loads.Springs);
            var binding=load.Resolve(world.Physics.Joints.ToArray());
            Assert.Equal(binding.Guide.TravelRange!.Upper,load.RestCoordinate);
            Assert.Equal(binding.Guide.TravelRange.Lower,load.RestCoordinate-load.Stroke);
            var initial=world.Physics.Capture();
            // Ordinary scene ticks must retain the same world-owned load and state.
            for(var i=0;i<12;i++)world.Step();
            Assert.Same(load,Assert.Single(world.Physics.Loads.Springs));
            Assert.Equal(wind,world.Physics.Spring(guideId).IsCharged);
            var ready=world.Physics.Capture();
            var result=world.Physics.ReleaseSpring(guideId);
            Assert.Equal(wind?SpringTriggerResult.Released:SpringTriggerResult.Empty,result);
            world.Physics.Step([],[],.25);
            var after=world.Physics.Capture();
            Assert.Equal(wind?1:0,world.Physics.Spring(guideId).ReleaseCount);
            if(wind)Assert.True(world.Physics.Spring(guideId).ReleasedWork>0);
            world.Physics.Restore(ready);
            Assert.Equal(result,world.Physics.ReleaseSpring(guideId));
            world.Physics.Step([],[],.25);
            Assert.Equal(after.Springs.ToArray(),world.Physics.Springs.ToArray());
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Physics.Restore(initial);
            Assert.Equal(initial.Springs.ToArray(),world.Physics.Springs.ToArray());
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }

}
