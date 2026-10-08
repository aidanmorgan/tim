using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalPreviewPresentationTests(NativeSceneFixture godot)
{
    public enum Element { Mirror, Splitter, Combiner, RedFilter, GreenFilter, BlueFilter }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Laser=new("laser");
    private static CatalogueId Catalogue(Element element)=>element switch
    {
        Element.Mirror=>new("mirror"),Element.Splitter=>new("beam_splitter"),Element.Combiner=>new("beam_combiner"),
        Element.RedFilter=>new("red_filter"),Element.GreenFilter=>new("green_filter"),Element.BlueFilter=>new("blue_filter"),
        _=>throw new ArgumentOutOfRangeException(nameof(element))
    };
    private partial class Source : LaserPart
    {
        public int PreviewReads;
        public Vector3 PreviewPower=Vector3.One;
        public override OpticalEmitter? OpticalPreviewSource
        {
            get{PreviewReads++;return base.OpticalPreviewSource!.Value with {Power=PreviewPower};}
        }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static MeshInstance3D[] Lines(MachinePart part)=>part.OpticalPreview!.Value.Target.GetChildren().OfType<MeshInstance3D>().Where(n=>n.Visible).ToArray();
    private static void Match(MachinePart part,IReadOnlyList<OpticalSegment> all)
    {
        var declaration=part.OpticalPreview!.Value;
        IReadOnlyList<OpticalSegment> expected=all.Where(s=>s.OriginPart==part.OpticalIdentity).ToArray();
        if(declaration.Composition==OpticalPreviewComposition.MergeCollinear)expected=OpticalPathVisual.Merge(expected);
        var lines=Lines(part);Assert.Equal(expected.Count,lines.Length);
        for(var i=0;i<lines.Length;i++)
        {
            Assert.InRange((lines[i].GlobalPosition-lines[i].GlobalBasis.Y*.5f).DistanceTo(expected[i].From),0,2e-5);
            Assert.InRange((lines[i].GlobalPosition+lines[i].GlobalBasis.Y*.5f).DistanceTo(expected[i].To),0,2e-5);
            var ink=OpticalColours.BeamInk(expected[i].Power);var actual=((StandardMaterial3D)lines[i].MaterialOverride).AlbedoColor;
            Assert.InRange(Math.Abs(ink.R-actual.R),0,1e-6);Assert.InRange(Math.Abs(ink.G-actual.G),0,1e-6);
            Assert.InRange(Math.Abs(ink.B-actual.B),0,1e-6);
        }
    }
    [Theory]
    [InlineData(Element.Mirror)]
    [InlineData(Element.Splitter)]
    [InlineData(Element.Combiner)]
    [InlineData(Element.RedFilter)]
    [InlineData(Element.GreenFilter)]
    [InlineData(Element.BlueFilter)]
    public void SelectedConstructionPreviewMatchesTraceWithMissAndZeroPowerControlsAndExactLifecycle(Element element)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Catalogue(element).Value,Position=[0,6,0]});
            var laser=new Source {Definition=world.Registry.Definitions[Laser.Value]};
            laser.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Laser.Value,Position=[-4,6,0]});world.AttachPart(laser);
            var saved=Saved(world);world.PresentFrame(0,1);Assert.Equal(0,laser.PreviewReads);
            part.SetSelected(true);var expected=OpticalNetwork.TraceConstructionPreviews(world);
            laser.PreviewReads=0;world.PresentFrame(0,1);Assert.Equal(1,laser.PreviewReads);
            Assert.True(part.OpticalPreview!.Value.Target.Visible);Assert.NotEmpty(Lines(part));Match(part,expected);
            Assert.False(laser.Active);Assert.False(part.Active);Assert.Empty(laser.BeamPath);Assert.Empty(world.OpticalPaths);
            Assert.Empty(world.Events);Assert.Equal(saved,Saved(world));
            var position=laser.Position;laser.Position+=Vector3.Back*4;world.PresentFrame(0,1);Assert.Empty(Lines(part));
            laser.Position=position;laser.PreviewPower=Vector3.Zero;world.PresentFrame(0,1);Assert.Empty(Lines(part));
            laser.PreviewPower=Vector3.One;world.PresentFrame(0,1);Match(part,expected);
            part.SetSelected(false);laser.PreviewReads=0;world.PresentFrame(0,1);
            Assert.False(part.OpticalPreview.Value.Target.Visible);Assert.Equal(0,laser.PreviewReads);
            part.SetSelected(true);world.Start();world.PresentFrame(0,1);Assert.False(part.OpticalPreview.Value.Target.Visible);
            world.Running=false;world.PresentFrame(0,1);Assert.False(part.OpticalPreview.Value.Target.Visible);
            Assert.Throws<InvalidOperationException>(()=>OpticalNetwork.TraceConstructionPreviews(world));
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(part.OpticalIdentity,restored.OpticalIdentity);
            restored.SetSelected(true);world.PresentFrame(0,1);Match(restored,expected);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.PresentFrame(0,1);
            Assert.False(world.FindPart(FixtureParts.Id(FixturePartId.First))!.OpticalPreview!.Value.Target.Visible);
        }
        finally{world.Free();}
    }
    [Fact]
    public void MultipleSelectedTargetsShareOneSourceReadAndRemovalDoesNotRetainTargets()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var first=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Catalogue(Element.RedFilter).Value,Position=[0,6,0]});
            var second=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Catalogue(Element.Splitter).Value,Position=[2,6,0]});
            var laser=new Source {Definition=world.Registry.Definitions[Laser.Value]};
            laser.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Laser.Value,Position=[-4,6,0]});world.AttachPart(laser);
            first.SetSelected(true);second.SetSelected(true);world.PresentFrame(0,1);Assert.Equal(1,laser.PreviewReads);
            Assert.NotEmpty(Lines(first));Assert.NotEmpty(Lines(second));
            world.RemovePart(first);laser.PreviewReads=0;world.PresentFrame(0,1);Assert.Equal(1,laser.PreviewReads);
            second.Visible=false;laser.PreviewReads=0;world.PresentFrame(0,1);Assert.Equal(0,laser.PreviewReads);
            Assert.False(second.OpticalPreview!.Value.Target.Visible);
        }
        finally{world.Free();}
    }
    [Fact]
    public void OpticalIdentityRejectsUnsupportedBoundaryValues()
    {
        Assert.Throws<ArgumentNullException>(()=>new OpticalPathOwner(null!));
        Assert.Throws<ArgumentException>(()=>new OpticalPathOwner(""));
        Assert.Throws<ArgumentException>(()=>new OpticalPathOwner(" "));
        Assert.Equal(new OpticalPathOwner("authored-origin"),new OpticalPathOwner("authored-origin"));
    }
}
