using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;
using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BooleanAnimationSignalTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    public enum InvalidSource { ForeignOwner, MissingSlot, DefaultOwner }
    private partial class Probe : BatteryPart
    {
        public static readonly BooleanObservationSlot Output=new(0);
        public readonly SimulationState<bool> Value=new(false);
        public bool NextValue,Reject;
        public SceneBooleanObservationKey FeedbackSource {get;set;}
        public Node3D Rocker=null!,Slider=null!;
        public MeshInstance3D Indicator=null!;
        private static readonly AnimationDefinition Clip=new(0,1,1,
            AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[..base.RuntimeState,Value];
        public override IReadOnlyList<SceneBooleanObservation> BooleanObservations=>[new(Output,new(Value))];
        public override IReadOnlyList<SceneRotationAnimation> RotationAnimations=>
            [new(Rocker,Clip,AnimationRotationAxis.Z,SceneAnimationSignal.Boolean(FeedbackSource),SceneAnimationDrive.Endpoint)];
        public override IReadOnlyList<SceneTranslationAnimation> TranslationAnimations=>
            [new(Slider,Clip,AnimationTranslationAxis.X,SceneAnimationSignal.Boolean(FeedbackSource),SceneAnimationDrive.Endpoint)];
        public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
            [new(Indicator,Clip,Colors.Black,Colors.White,SceneAnimationSignal.Boolean(FeedbackSource),SceneAnimationDrive.Endpoint)];
        protected override void Build()
        {
            base.Build();Rocker=new Node3D();Visual.AddChild(Rocker);
            Slider=new Node3D();Visual.AddChild(Slider);
            Indicator=PartArt.Box(Visual,new(.1f,.1f,.1f),Colors.Black);
        }
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            Value.Value=NextValue;
            if(Reject)throw new InvalidOperationException("Injected Boolean producer failure.");
        }
    }
    private Probe Add(MachineWorld world)
    {
        var part=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
        part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,4,0]});
        world.AttachPart(part);part.FeedbackSource=new(part,Probe.Output);return part;
    }

    [Fact]
    public void AllClipChannelsUseOnlyCommittedBooleanFeedbackAndResetExactly()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world);var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var rocker=part.Rocker.Transform;var slider=part.Slider.Transform;
            world.Start();part.NextValue=true;part.Reject=true;
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            Assert.False(part.Value.Value);world.PresentFrame(.25,1);
            Assert.Equal(rocker,part.Rocker.Transform);Assert.Equal(slider,part.Slider.Transform);
            part.Reject=false;world.Step();
            Assert.Equal(rocker,part.Rocker.Transform);Assert.Equal(slider,part.Slider.Transform);
            var state=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(.25,1);
            Assert.InRange(part.Rocker.Rotation.Z,.249f,.251f);
            Assert.InRange(part.Slider.Position.X,.249f,.251f);
            Assert.InRange(((StandardMaterial3D)part.Indicator.MaterialOverride).AlbedoColor.R,.249f,.251f);
            Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());
            part.Value.Value=false;world.PresentFrame(.25,1);
            Assert.InRange(part.Rocker.Rotation.Z,.499f,.501f); // Unpublished cell change is invisible.
            part.NextValue=false;world.Step();world.PresentFrame(.1,1);
            Assert.InRange(part.Rocker.Rotation.Z,.399f,.401f);
            world.PresentFrame(1,1);
            Assert.Equal(rocker,part.Rocker.Transform);Assert.Equal(slider,part.Slider.Transform);
            Assert.Equal(Colors.Black,((StandardMaterial3D)part.Indicator.MaterialOverride).AlbedoColor);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(InvalidSource.ForeignOwner)]
    [InlineData(InvalidSource.MissingSlot)]
    [InlineData(InvalidSource.DefaultOwner)]
    public void InvalidBooleanBindingsRejectWithoutSubstitution(InvalidSource invalid)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world);
            var other=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,4,0]});
            part.FeedbackSource=invalid switch
            {
                InvalidSource.ForeignOwner=>new(other,Probe.Output),
                InvalidSource.MissingSlot=>new(part,new(1)),
                InvalidSource.DefaultOwner=>default,
                _=>throw new ArgumentOutOfRangeException(nameof(invalid))
            };
            Assert.Throws<ArgumentException>(()=>world.Start());
            Assert.Equal(Transform3D.Identity,part.Rocker.Transform);
        }
        finally {world.Free();}
    }
}

