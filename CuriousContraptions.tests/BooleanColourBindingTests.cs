using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BooleanColourBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Gate=new("optical_and");
    public enum Fault { MissingSlot, ForeignOwner }
    private partial class InvalidGate : OpticalLogicPart
    {
        public Fault Fault;
        public MachinePart? Foreign;
        public bool Corrected;
        public override IReadOnlyList<SceneColourFollow> FollowingColours
        {
            get
            {
                var originals=base.FollowingColours;
                if(Corrected)return originals;
                var source=Fault switch
                {
                    Fault.MissingSlot=>new SceneBooleanObservationKey(this,new(99)),
                    Fault.ForeignOwner=>new SceneBooleanObservationKey(Foreign!,FirstOutput),
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                };
                return [originals[0] with {Signal=SceneColourFollowSignal.Boolean(source)},originals[1]];
            }
        }
    }
    [Theory]
    [InlineData(Fault.MissingSlot)]
    [InlineData(Fault.ForeignOwner)]
    public void UnknownOrForeignBooleanSourceRejectsBeforeRunAndCorrectedRetrySucceeds(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var gate=new InvalidGate {Definition=world.Registry.Definitions[Gate.Value],Fault=fault};
            gate.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Gate.Value,Position=[0,5,0]});world.AttachPart(gate);
            gate.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Gate.Value,Position=[4,5,0]});
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);
            gate.Corrected=true;world.Start();world.PresentFrame(.1,1);
            foreach(var declaration in gate.FollowingColours)
                Assert.Equal(declaration.From,((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor);
        }
        finally{world.Free();}
    }
    [Fact]
    public void MissingBooleanSignalOwnerRejectsAtDeclaration()
    {
        Assert.Throws<ArgumentException>(()=>SceneColourFollowSignal.Boolean(default));
        Assert.Equal(SceneColourFollowFeedback.OwnerActivity,SceneColourFollowSignal.OwnerActive.Kind);
    }
}
