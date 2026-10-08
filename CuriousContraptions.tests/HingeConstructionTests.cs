using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class HingeConstructionTests(NativeSceneFixture godot)
{
    private enum Role { Beam, OtherBeam, Fixture, Ball }
    private static PartSpec Spec(Role role,Vector3 position)=>new()
    {
        Id=role switch {Role.Beam=>"beam",Role.OtherBeam=>"other_beam",Role.Fixture=>"fixture",Role.Ball=>"ball",_=>throw new ArgumentOutOfRangeException(nameof(role))},
        Kind=role switch {Role.Beam or Role.OtherBeam=>ImpactLeverPart.CatalogId,Role.Fixture=>"wall",Role.Ball=>"ball",_=>throw new ArgumentOutOfRangeException(nameof(role))},
        Position=[position.X,position.Y,position.Z]
    };
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialBeamToBeamOverlapIsCheckedByTheSharedSceneQuery(bool separated)
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world);
        try
        {
            var first=world.AddPart(Spec(Role.Beam,new(0,4,0)));
            var second=world.AddPart(Spec(Role.OtherBeam,new(0,4,separated?3:0)));
            var poses=new[]{first.Transform,second.Transform};
            if(separated) world.Start();
            else Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.Equal(separated,world.Running);
            Assert.Equal(poses,new[]{first.Transform,second.Transform});
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialBendShellOverlapIsNotOmittedFromConstructionValidation(bool separated)
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world);
        try
        {
            var beam=world.AddPart(Spec(Role.Beam,new(0,4,0)));
            var fixture=world.AddPart(Spec(Role.Fixture,new(-Mathf.Sqrt(2),4-Mathf.Sqrt(2),separated?4:-.25f)));
            fixture.Boxes.Clear();
            fixture.Bends.Add(new(Transform3D.Identity,2,Mathf.Pi/2,.2f,.3f));
            var before=beam.Transform;
            if(separated) world.Start();
            else Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.Equal(separated,world.Running); Assert.Equal(before,beam.Transform);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FreeBallAlsoUsesTheSharedInitialOverlapValidation(bool separated)
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(Spec(Role.Beam,new(0,4,0)));
            var ball=world.AddPart(Spec(Role.Ball,new(0,separated?5:4,0)));
            var before=ball.Transform;
            if(separated)world.Start();
            else Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.Equal(separated,world.Running);Assert.Equal(before,ball.Transform);
        }
        finally {world.Free();}
    }
}
