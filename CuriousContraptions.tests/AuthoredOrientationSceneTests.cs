using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AuthoredOrientationSceneTests(NativeSceneFixture godot)
{
    public enum Fixture { Cannon, Motor, Windmill, Lever, Weight }
    private static string Catalog(Fixture value)=>value switch
    {
        Fixture.Cannon=>CannonPart.CatalogId,Fixture.Motor=>"motor",Fixture.Windmill=>"windmill",
        Fixture.Lever=>"impact_lever",Fixture.Weight=>"weight",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    public static TheoryData<Fixture,bool> Cases()
    {
        var cases=new TheoryData<Fixture,bool>();
        foreach(var fixture in Enum.GetValues<Fixture>())
        foreach(var singular in new[]{false,true})cases.Add(fixture,singular);
        return cases;
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [MemberData(nameof(Cases))]
    public void GizmoEditedBasisSurvivesSaveLoadRunResetAndExactPhysicsReplay(Fixture fixture,bool singular)
    {
        var world=World();
        try
        {
            var id=FixtureParts.Id(FixturePartId.First);
            var part=world.AddPart(new(){Id=id,Kind=Catalog(fixture),Position=[.25f,6,-.5f],
                Orientation=PartOrientation.FromEulerDegrees(singular?90:25,40,15)});
            part.Basis=part.Basis.Rotated(new Vector3(1,2,3).Normalized(),.173f);
            if(fixture==Fixture.Weight)part.InitialVelocity=new(.5f,-.1f,.2f);
            var transform=part.Transform;
            var saved=Saved(world);
            PhysicsBodySnapshot[]? final=null;
            for(var cycle=0;cycle<5;cycle++)
            {
                var decoded=JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!;
                world.LoadMachine(decoded);
                part=world.FindPart(id)!;
                Assert.Equal(transform,part.Transform);
                Assert.Equal(saved,Saved(world));
                world.Start();
                for(var tick=0;tick<12;tick++)world.Step();
                var current=world.Physics.Capture().BodyStates.ToArray();
                if(final is null)final=current;else Assert.Equal(final,current);
                if(fixture==Fixture.Weight)
                {
                    var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
                    Assert.True(body.Center.X>.25);
                    world.PresentFrame(0,1);
                    Assert.Equal(body.Pose.ToScene(),part.GlobalTransform);
                    Assert.NotEqual(transform,part.Transform);
                }
                // Running saves retain construction even while authoritative bodies move.
                Assert.Equal(saved,Saved(world));
                world.Restore();
                Assert.Equal(transform,world.FindPart(id)!.Transform);
                Assert.Equal(saved,Saved(world));
                Assert.Equal(0,world.Ticks);
                Assert.False(world.HasConstructionSnapshot);
            }
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompoundAssistanceUsesGeodesicWindowAndCapThroughSharedPhysics(bool strict)
    {
        var world=World();
        try
        {
            const string rampCatalog="ramp";
            var id=FixtureParts.Id(FixturePartId.First);
            var from=PartOrientation.FromEulerDegrees(89,170,170);
            var to=PartOrientation.FromEulerDegrees(89,-170,-170);
            var authored=new PartSpec {Id=id,Kind=rampCatalog,Position=[0,6,0],Orientation=from};
            var target=new PartSpec {Id=id,Kind=rampCatalog,Position=[0,6,0],Orientation=to,
                Difficulty=[new(){Precision=0,PositionWindow=0,RotationWindow=2,MaxRotationCorrection=.2f,BlendSeconds=.4f},
                    new(){Precision=1,PositionWindow=0,RotationWindow=2,MaxRotationCorrection=0,BlendSeconds=.4f}]};
            world.LoadMachine(new(){Gravity=0,Pressure=0,Parts=[authored],PlacementTargets=[target]});
            world.Precision=strict?1:0;
            var initialSave=Saved(world);
            var part=world.FindPart(id)!;
            var initial=SceneGeometryAdapter.CaptureRigidPose(part.Transform);
            var desired=SceneGeometryAdapter.CaptureRigidPose(new(SceneOrientation.Present(to),part.Position));
            var gap=(desired.Rotation*initial.Rotation.Inverse()).RotationVector().Length;
            Assert.InRange(gap,.2*Math.PI/180,2*Math.PI/180);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            Assert.Equal(strict?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,body.MotionType);
            for(var tick=0;tick<60;tick++)world.Step();
            var moved=(body.Pose.Rotation*initial.Rotation.Inverse()).RotationVector().Length;
            var remaining=(desired.Rotation*body.Pose.Rotation.Inverse()).RotationVector().Length;
            var expected=strict?0:.2f*Math.PI/180;
            Assert.InRange(Math.Abs(moved-expected),0,1e-10);
            Assert.InRange(Math.Abs(remaining-(gap-expected)),0,1e-10);
            Assert.Equal(initial.Center,body.Center);
            Assert.Equal(default,body.AngularVelocity);
            world.Restore();
            Assert.Equal(initialSave,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FixtureMappingRejectsUndefinedValues()
        =>Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog((Fixture)999));
}
