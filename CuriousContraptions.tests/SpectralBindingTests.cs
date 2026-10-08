using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SpectralBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Combiner=new("beam_combiner");
    public enum Fault { MissingSlot, ForeignOwner, WrongUnit, MissingTarget, Response, Clock, Baseline }
    private partial class InvalidCombiner : BeamCombinerPart
    {
        public Fault Fault;
        public MachinePart? Foreign;
        public bool Corrected;
        public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>
            !Corrected&&Fault==Fault.WrongUnit
                ? base.ScalarObservations.Select(s=>new SceneScalarObservation(s.Slot,ScalarUnit.Dimensionless,s.Source)).ToArray()
                : base.ScalarObservations;
        public override IReadOnlyList<SceneSpectralColour> SpectralColours
        {
            get
            {
                var original=base.SpectralColours[0];if(Corrected)return [original];
                return [Fault switch
                {
                    Fault.MissingSlot=>original with {Red=new(this,new(99))},
                    Fault.ForeignOwner=>original with {Red=new(Foreign!,RedOutput)},
                    Fault.WrongUnit=>original,
                    Fault.MissingTarget=>original with {Target=null!},
                    Fault.Response=>original with {Response=0},
                    Fault.Clock=>original with {Clock=(AnimationClock)999},
                    Fault.Baseline=>original with {Inactive=Colors.White},
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                }];
            }
        }
    }
    [Theory]
    [InlineData(Fault.MissingSlot)]
    [InlineData(Fault.ForeignOwner)]
    [InlineData(Fault.WrongUnit)]
    [InlineData(Fault.MissingTarget)]
    [InlineData(Fault.Response)]
    [InlineData(Fault.Clock)]
    [InlineData(Fault.Baseline)]
    public void InvalidDeclarationsRejectBeforeRunAndCorrectedRetrySucceeds(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=new InvalidCombiner {Definition=world.Registry.Definitions[Combiner.Value],Fault=fault};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Combiner.Value,Position=[0,5,0]});world.AttachPart(part);
            part.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Combiner.Value,Position=[4,5,0]});
            Assert.ThrowsAny<ArgumentException>(world.Start);Assert.False(world.Running);
            part.Corrected=true;world.Start();world.PresentFrame(.1,1);
            var declaration=Assert.Single(part.SpectralColours);
            Assert.Equal(declaration.Inactive,((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor);
        }
        finally{world.Free();}
    }
}
