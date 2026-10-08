using Godot;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorldTraceTests
{
    public enum Mutation { Move, Rotate, Resize, Remove, Hide, Transparency }

    private static float Trace(MachineWorld world,TraceMedium medium=TraceMedium.Light)=>
        WorldGeometry.Trace(medium,world,new(-3,5,0),Vector3.Right,6,null);

    [Theory]
    [InlineData(Mutation.Move)]
    [InlineData(Mutation.Rotate)]
    [InlineData(Mutation.Resize)]
    [InlineData(Mutation.Remove)]
    [InlineData(Mutation.Hide)]
    [InlineData(Mutation.Transparency)]
    public void CompiledGeometryTracksAuthoredChanges(Mutation mutation)
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World; var part=scene.Part;
        part.Position=new(0,5,0);
        part.Boxes.Add(new(Vector3.Zero,new(1,.25f,.25f), MachinePart.RootBody));
        Assert.InRange(Trace(world),1.99999f,2.00001f);
        switch(mutation)
        {
            case Mutation.Move: part.Position+=Vector3.Back*3; break;
            case Mutation.Rotate: part.RotationDegrees=new(0,90,0); break;
            case Mutation.Resize: part.Boxes[0]=new(Vector3.Zero,new(.5f,.25f,.25f), MachinePart.RootBody); break;
            case Mutation.Remove: part.Boxes.Clear(); break;
            case Mutation.Hide: part.Visible=false; break;
            case Mutation.Transparency: part.Boxes[0]=part.Boxes[0] with { Opaque=false }; break;
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
        var expected=mutation switch { Mutation.Rotate=>2.75f,Mutation.Resize=>2.5f,_=>6f };
        Assert.InRange(Trace(world),expected-.00001f,expected+.00001f);
        Assert.Equal(Trace(world),Trace(world));
    }

    [Theory]
    [InlineData(TraceMedium.Light,6f)]
    [InlineData(TraceMedium.Sound,6f)]
    [InlineData(TraceMedium.Air,2f)]
    public void TransparencyIsMediaMetadata(TraceMedium medium,float expected)
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World; var part=scene.Part;
        part.Position=new(0,5,0);
        part.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody,false));
        Assert.InRange(Trace(world,medium),expected-.00001f,expected+.00001f);
    }

    [Fact]
    public void TubeBoreStaysOpenAndShellBlocksAir()
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World; var part=scene.Part;
        part.Position=new(0,5,0);
        part.Tubes.Add(new(Transform3D.Identity,1,.65f,.7f,false));
        Assert.Equal(6,Trace(world,TraceMedium.Air));
        var shell=WorldGeometry.Trace(TraceMedium.Air,world,new(0,7,0),Vector3.Down,4,null);
        Assert.InRange(shell,1.294f,1.306f);
        Assert.Equal(4,WorldGeometry.Trace(TraceMedium.Light,world,new(0,7,0),Vector3.Down,4,null));
        part.Tubes[0]=part.Tubes[0] with { Opaque=true };
        Assert.InRange(WorldGeometry.Trace(TraceMedium.Light,world,new(0,7,0),Vector3.Down,4,null),1.294f,1.306f);
    }

    [Theory]
    [InlineData(TraceMedium.Light)]
    [InlineData(TraceMedium.Sound)]
    [InlineData(TraceMedium.Air)]
    public void EmitterReceiverAndHiddenGeometryAreExcluded(TraceMedium medium)
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World; var part=scene.Part;
        part.Position=new(0,5,0);
        part.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody));
        Assert.InRange(Trace(world,medium),1.99999f,2.00001f);
        Assert.Equal(6,WorldGeometry.Trace(medium,world,new(-3,5,0),Vector3.Right,6,part));
        Assert.Equal(6,WorldGeometry.Trace(medium,world,new(-3,5,0),Vector3.Right,6,null,part));
        part.Visible=false;
        Assert.Equal(6,Trace(world,medium));
        part.Visible=true;
        part.Spheres[0]=new(Vector3.Zero,.5f,MachinePart.RootBody);
        Assert.InRange(Trace(world,medium),2.49999f,2.50001f);
    }

    [Fact]
    public void InvalidQueriesAreRejected()
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World;
        Assert.Throws<ArgumentOutOfRangeException>(()=>Trace(world,(TraceMedium)int.MaxValue));
        Assert.Throws<ArgumentException>(()=>WorldGeometry.Trace(TraceMedium.Light,world,Vector3.Zero,Vector3.Zero,1,null));
        Assert.Throws<ArgumentException>(()=>WorldGeometry.Trace(TraceMedium.Light,world,Vector3.Zero,Vector3.Right,-1,null));
        Assert.Throws<ArgumentException>(()=>WorldGeometry.Trace(TraceMedium.Light,world,new(float.NaN,0,0),Vector3.Right,1,null));
    }
}
