using System.Runtime.CompilerServices;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ArtworkBoundsTests(NativeSceneFixture godot)
{
    private partial class BoundsPart : MachinePart
    {
        public Node3D Nested=null!;
        public MeshInstance3D First=null!,Second=null!,Empty=null!,Hidden=null!;
        public static readonly Vector3 FirstSize=new(2,3,4),SecondSize=new(1,2,3);
        protected override void Build()
        {
            First=PartArt.Box(Visual,FirstSize,Colors.White,new(-2,1,0));
            Nested=new Node3D {Position=new(3,-1,2),RotationDegrees=new(10,25,15),Scale=new(1,2,.5f)};
            Visual.AddChild(Nested);
            Second=PartArt.Box(Nested,SecondSize,Colors.White,new(1,0,-1));
            Empty=new MeshInstance3D {Position=Vector3.One*1000};Nested.AddChild(Empty);
            Hidden=PartArt.Box(Nested,Vector3.One*100,Colors.White,Vector3.One*100);Hidden.Visible=false;
            // Selection/decorative siblings outside Visual must not enlarge construction bounds.
            PartArt.Box(this,Vector3.One*200,Colors.White,Vector3.One*200);
        }
    }
    private BoundsPart Part()
    {
        var part=new BoundsPart {Position=new(4,5,-3),RotationDegrees=new(12,23,-17),Scale=new(1.2f,.8f,1.1f)};
        godot.Tree.Root.AddChild(part);return part;
    }
    private void Release(Node part){part.Free();godot.Engine.Iteration();}
    private static Aabb Box(MeshInstance3D node,Vector3 size)=>node.GlobalTransform*new Aabb(-size*.5f,size);
    private static void Equal(Aabb expected,Aabb actual)
    {
        Assert.InRange((expected.Position-actual.Position).Length(),0,1e-5f);
        Assert.InRange((expected.Size-actual.Size).Length(),0,1e-5f);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReleaseManagedMesh(MeshInstance3D node)
    {
        var mesh=node.Mesh!;
        Assert.True(node.GetBase().IsValid);var identity=node.GetBase();
        mesh.Dispose(); // The instance retains its own native Ref<Mesh>.
        Assert.Equal(identity,node.GetBase());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedBoundsIgnoreEmptyHiddenAndNonArtworkNodesAcrossResourceLifetime(bool releaseManaged)
    {
        var part=Part();
        try
        {
            var expected=Box(part.First,BoundsPart.FirstSize).Merge(Box(part.Second,BoundsPart.SecondSize));
            Assert.False(part.Empty.GetBase().IsValid);
            if(releaseManaged){ReleaseManagedMesh(part.First);ReleaseManagedMesh(part.Second);}
            GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);GC.WaitForPendingFinalizers();
            Equal(expected,PlacementShadows.ArtworkBounds(part));
            for(var i=0;i<100;i++)Equal(expected,PlacementShadows.ArtworkBounds(part));
            part.Nested.Visible=false;Equal(Box(part.First,BoundsPart.FirstSize),PlacementShadows.ArtworkBounds(part));
            part.Nested.Visible=true;Equal(expected,PlacementShadows.ArtworkBounds(part));
            part.Hidden.Visible=true;
            Equal(expected.Merge(Box(part.Hidden,Vector3.One*100)),PlacementShadows.ArtworkBounds(part));
        }
        finally{Release(part);}
    }

    [Fact]
    public void ReplacingAndClearingNativeGeometryChangesBoundsWithoutAStaleResource()
    {
        var part=Part();
        try
        {
            var size=new Vector3(6,2,1);var shape=new BoxMesh {Size=size};
            part.First.Mesh=shape;shape.Dispose();
            Equal(Box(part.First,size).Merge(Box(part.Second,BoundsPart.SecondSize)),PlacementShadows.ArtworkBounds(part));
            part.First.Mesh=null;Assert.False(part.First.GetBase().IsValid);
            Equal(Box(part.Second,BoundsPart.SecondSize),PlacementShadows.ArtworkBounds(part));
            part.Nested.Visible=false;
            Equal(new(part.GlobalPosition-Vector3.One*.1f,Vector3.One*.2f),PlacementShadows.ArtworkBounds(part));
            part.Nested.Visible=true;
            Equal(Box(part.Second,BoundsPart.SecondSize),PlacementShadows.ArtworkBounds(part));
        }
        finally{Release(part);}
    }

    [Fact]
    public void UnconstructedDetachedAndFreedPartsRejectAtTheBoundary()
    {
        Assert.Throws<ArgumentNullException>(()=>PlacementShadows.ArtworkBounds(null!));
        var unbuilt=new BoundsPart();
        try{Assert.Throws<ArgumentException>(()=>PlacementShadows.ArtworkBounds(unbuilt));}
        finally{unbuilt.Free();}
        var part=Part();
        godot.Tree.Root.RemoveChild(part);
        Assert.Throws<ArgumentException>(()=>PlacementShadows.ArtworkBounds(part));
        part.Free();
        Assert.Throws<ArgumentException>(()=>PlacementShadows.ArtworkBounds(part));
        godot.Engine.Iteration();
    }

    [Fact]
    public void WarmedBoundsQueryRetainsExactGeometryUnderAllocationPressure()
    {
        var part=Part();
        try
        {
            var expected=Box(part.First,BoundsPart.FirstSize).Merge(Box(part.Second,BoundsPart.SecondSize));
            for(var i=0;i<100;i++)_=PlacementShadows.ArtworkBounds(part);
            var before=GC.GetAllocatedBytesForCurrentThread();
            Aabb result=default;
            for(var i=0;i<1000;i++)result=PlacementShadows.ArtworkBounds(part);
            var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Equal(expected,result);
            Console.WriteLine($"Artwork bounds queries=1000 allocated_bytes={bytes}");
            Assert.Equal(0,bytes);
        }
        finally{Release(part);}
    }
}
