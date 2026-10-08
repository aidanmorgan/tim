using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PressureContactOwnershipTests(NativeSceneFixture godot)
{
    public enum Load { Ball, Weight }
    private enum Fixture { Plate, Ball, Weight }
    private static string Wire(Fixture fixture)=>fixture switch
    {
        Fixture.Plate=>"pressure_plate",Fixture.Ball=>"ball",Fixture.Weight=>"weight",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    [Theory]
    [InlineData(Load.Ball,0f)]
    [InlineData(Load.Weight,0f)]
    [InlineData(Load.Ball,37f)]
    [InlineData(Load.Weight,37f)]
    public void ActualContactCountsMassOnceAndIgnoresPresentation(Load kind,float angle)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var plate=(PressurePlatePart)world.AddPart(new(){Id=Wire(Fixture.Plate),Kind=Wire(Fixture.Plate),Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(0,0,angle)});
            var fixture=kind switch {Load.Ball=>Fixture.Ball,Load.Weight=>Fixture.Weight,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
            var load=world.AddPart(new(){Id=Wire(fixture),Kind=Wire(fixture),Position=[0,9,0],Orientation = PartOrientation.FromEulerDegrees(0,0,angle)});
            // Derive placement from the declared compound support, not a radius approximation.
            var declaration=WorldGeometry.CapturePhysicsBodies(world).Single(d=>d.Geometry.Owner==load);
            var pose=SceneGeometryAdapter.CaptureRigidPose(plate.Transform);
            var footprint=SupportFootprint.Sample(declaration.Geometry.Geometry,new(pose.Center,pose.Rotation),pose);
            load.Position=plate.Transform*new Vector3(0,(float)(PressurePlatePart.Top-footprint.LowestPoint.Y),0);
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(load,MachinePart.RootBody));
            var expected=1/body.InverseMass;
            var frame=world.PhysicsAssembly.Body(new(plate,MachinePart.RootBody)).Id;
            var reading=world.Physics.ContactLoad(frame);
            Assert.Equal(expected,reading.Mass);Assert.Equal(new[]{body.Id},reading.Bodies.ToArray());
            Assert.Equal(expected,plate.SupportedMass); // No scene callback required.
            var initialPhase=plate.State;
            Assert.Throws<InvalidOperationException>(()=>FixtureParts.ConfigureParameter(plate,PressurePlateParameter.MinimumMass,16));
            Assert.Equal(initialPhase,plate.State); // Captured law, not mutable scene metadata.
            var retained=world.Physics.RetainedContactPairs;
            plate.BeforeNetworks(world);
            Assert.Equal(expected,plate.SupportedMass);
            Assert.Equal(retained,world.Physics.RetainedContactPairs);
            plate.Position+=Vector3.Right*10;load.Position+=Vector3.Back*10;
            plate.Visible=false;load.Visible=false;
            plate.BeforeNetworks(world);
            Assert.Equal(expected,plate.SupportedMass);
            var saved=world.Physics.Capture();
            var collider=world.Physics.Collider(body.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(body.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            plate.BeforeNetworks(world);Assert.Equal(PhysicsContactLoadPhase.Empty,plate.State);
            world.Physics.Restore(saved);plate.BeforeNetworks(world);Assert.Equal(expected,plate.SupportedMass);
            world.Physics.ApplyImpulse(body.Id,pose.Rotation.Apply(new(0,1/body.InverseMass,0)),body.Center);
            world.Physics.Step([],[],.03);
            Assert.Equal(PhysicsContactLoadPhase.Empty,plate.State);
            Assert.Equal(0,world.Physics.ContactLoad(frame).Mass);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=Assert.Single(world.Parts.OfType<PressurePlatePart>());
            Assert.Equal(0,restored.SupportedMass);Assert.Equal(PhysicsContactLoadPhase.Empty,restored.State);
        }
        finally{world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUnsupportedValues()
    {
        Assert.Equal("pressure_plate",Wire(Fixture.Plate));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
    }
}
