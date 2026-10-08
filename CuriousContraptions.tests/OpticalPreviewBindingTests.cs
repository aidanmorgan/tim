using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalPreviewBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Mirror=new("mirror");
    public enum Fault { MissingTarget, ForeignTarget, Composition, NonPreview }
    private partial class InvalidMirror : MirrorPart
    {
        public Fault Fault;
        public OpticalPathVisual? Foreign;
        public bool Corrected;
        public override SceneOpticalPreview? OpticalPreview
        {
            get
            {
                var original=base.OpticalPreview!.Value;
                if(Corrected)return original;
                return Fault switch
                {
                    Fault.MissingTarget=>original with {Target=null!},
                    Fault.ForeignTarget=>original with {Target=Foreign!},
                    Fault.Composition=>original with {Composition=(OpticalPreviewComposition)999},
                    Fault.NonPreview=>original,
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                };
            }
        }
    }
    [Theory]
    [InlineData(Fault.MissingTarget)]
    [InlineData(Fault.ForeignTarget)]
    [InlineData(Fault.Composition)]
    [InlineData(Fault.NonPreview)]
    public void InvalidDeclarationsRejectBeforeAnyPreviewWriteAndCorrectedRetrySucceeds(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=new InvalidMirror {Fault=fault,Definition=world.Registry.Definitions[Mirror.Value]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Mirror.Value,Position=[0,6,0]});world.AttachPart(part);
            var other=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Mirror.Value,Position=[4,6,0]});
            part.Foreign=other.OpticalPreview!.Value.Target;
            part.Corrected=true;var target=part.OpticalPreview!.Value.Target;part.Corrected=false;
            if(fault==Fault.NonPreview)target.Preview=false;
            part.SetSelected(true);other.SetSelected(true);
            Assert.Throws<ArgumentException>(()=>world.PresentFrame(0,1));
            Assert.False(target.Visible);Assert.False(part.Foreign.Visible);
            target.Preview=true;part.Corrected=true;world.PresentFrame(0,1);
            Assert.True(target.Visible);Assert.True(part.Foreign.Visible);
        }
        finally{world.Free();}
    }
}
