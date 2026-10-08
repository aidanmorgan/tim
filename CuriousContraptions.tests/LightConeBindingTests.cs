using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LightConeBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Torch=new("flashlight");
    public enum Fault { MissingTarget, ForeignTarget, DuplicateTarget, Origin, Direction, Range, Angle, Intensity, TranslationTarget }
    private partial class InvalidTorch : FlashlightPart
    {
        public Fault Fault;
        public LightConeVisual? Foreign;
        public bool Corrected;
        public override IReadOnlyList<SceneLightCone> LightCones
        {
            get
            {
                var original=base.LightCones[0];if(Corrected)return [original];
                if(Fault==Fault.DuplicateTarget)return [original,original];
                return [Fault switch
                {
                    Fault.MissingTarget=>original with {Target=null!},
                    Fault.ForeignTarget=>original with {Target=Foreign!},
                    Fault.Origin=>original with {Source=original.Source with {At=new(float.NaN,0,0)}},
                    Fault.Direction=>original with {Source=original.Source with {Direction=Vector3.Zero}},
                    Fault.Range=>original with {Source=original.Source with {Range=0}},
                    Fault.Angle=>original with {Source=original.Source with {ConeCosine=2}},
                    Fault.Intensity=>original with {Source=original.Source with {Intensity=-1}},
                    Fault.TranslationTarget=>original,
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                }];
            }
        }
        public override IReadOnlyList<SceneTranslationAnimation> TranslationAnimations=>
            !Corrected&&Fault==Fault.TranslationTarget?[base.TranslationAnimations[0] with {Target=this}]:base.TranslationAnimations;
    }
    [Theory]
    [InlineData(Fault.MissingTarget)]
    [InlineData(Fault.ForeignTarget)]
    [InlineData(Fault.DuplicateTarget)]
    [InlineData(Fault.Origin)]
    [InlineData(Fault.Direction)]
    [InlineData(Fault.Range)]
    [InlineData(Fault.Angle)]
    [InlineData(Fault.Intensity)]
    [InlineData(Fault.TranslationTarget)]
    public void InvalidDeclarationRejectsAndCorrectedRetryRestoresBaseline(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=new InvalidTorch {Definition=world.Registry.Definitions[Torch.Value],Fault=fault};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Torch.Value,Position=[0,5,0]});world.AttachPart(part);
            var other=(FlashlightPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Torch.Value,Position=[4,5,0]});
            part.Foreign=Assert.Single(other.LightCones).Target;
            Assert.ThrowsAny<ArgumentException>(world.Start);Assert.False(world.Running);
            part.Corrected=true;world.Start();world.PresentFrame(.1,1);
            Assert.False(Assert.Single(part.LightCones).Target.Visible);
        }
        finally{world.Free();}
    }
}
