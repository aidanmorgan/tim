using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ScalarObservationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Solar=new("solar_panel");
    public enum DeclarationFault { None, Duplicate, ForeignCell, UndefinedUnit, MissingCell }
    private partial class Probe : BatteryPart
    {
        public readonly SimulationState<float> Single=new(.5f);
        public readonly SimulationState<double> Double=new(1.125);
        private readonly SimulationState<double> _foreign=new(0);
        public DeclarationFault Fault;
        public bool RejectCapture;
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[Single,Double];
        public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>Fault switch
        {
            DeclarationFault.None=>[new(new(0),ScalarUnit.Dimensionless,new(Single)),new(new(1),ScalarUnit.Dimensionless,new(Double))],
            DeclarationFault.Duplicate=>[new(new(0),ScalarUnit.Dimensionless,new(Single)),new(new(0),ScalarUnit.Dimensionless,new(Double))],
            DeclarationFault.ForeignCell=>[new(new(0),ScalarUnit.Dimensionless,new(_foreign))],
            DeclarationFault.UndefinedUnit=>[new(new(0),(ScalarUnit)999,new(Single))],
            DeclarationFault.MissingCell=>[new(new(0),ScalarUnit.Dimensionless,default)],
            _=>throw new ArgumentOutOfRangeException(nameof(Fault))
        };
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            if(!RejectCapture)return;
            Single.Value=10;Double.Value=double.NaN;
        }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(DeclarationFault.Duplicate)]
    [InlineData(DeclarationFault.ForeignCell)]
    [InlineData(DeclarationFault.UndefinedUnit)]
    [InlineData(DeclarationFault.MissingCell)]
    public void InvalidObservationDeclarationsRejectBeforeRunAndRetry(DeclarationFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value],Fault=fault};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,5,0]});world.AttachPart(probe);
            var saved=Saved(world);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);Assert.Equal(saved,Saved(world));
            probe.Fault=DeclarationFault.None;world.Start();
            using(var read=world.ReadCommittedPoses())
            {
                var copy=new ScalarRead[2];read.CopyScalars(PoseSample.Current,copy);
                Assert.Equal(.5,copy[0].Value);Assert.Equal(1.125,copy[1].Value);
            }
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void LateScalarCaptureFailureRestoresCellsAndPublishesNothing()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,5,0]});world.AttachPart(probe);
            var saved=Saved(world);world.Start();world.Step();
            var physical=world.Physics.Capture();var revision=world.ControlRevision;var ticks=world.Ticks;
            probe.RejectCapture=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(.5f,probe.Single.Value);Assert.Equal(1.125,probe.Double.Value);
            Assert.Equal(revision,world.ControlRevision);Assert.Equal(ticks,world.Ticks);
            Assert.Equal(physical.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            using(var read=world.ReadCommittedPoses())
            {
                var copy=new ScalarRead[2];read.CopyScalars(PoseSample.Current,copy);
                Assert.Equal(.5,copy[0].Value);Assert.Equal(1.125,copy[1].Value);
            }
            probe.RejectCapture=false;probe.Single.Value=.75f;probe.Double.Value=2.25;world.Step();
            using(var read=world.ReadCommittedPoses())
            {
                var copy=new ScalarRead[2];read.CopyScalars(PoseSample.Current,copy);
                Assert.Equal(.75,copy[0].Value);Assert.Equal(2.25,copy[1].Value);
            }
            var producer=new SceneScalarObservations(world.Parts,world.PhysicsAssembly,world.Physics,world.Timers,new Dictionary<SceneTimerKey,SimulationTimerId>(),world.Oscillators,new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            for(var i=0;i<100;i++)producer.Capture();
            var before=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();
            var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Console.WriteLine($"Scalar producer: readings=2, captures=1000, allocated_bytes={allocated}");
            Assert.Equal(0,allocated);Assert.Equal(2.25,producer.Capture()[1].Value);
            world.Restore();Assert.Equal(saved,Saved(world));
            Assert.Throws<ArgumentNullException>(()=>new ScalarObservationSource((SimulationState<float>)null!));
            Assert.Throws<ArgumentNullException>(()=>new ScalarObservationSource((SimulationState<double>)null!));
        }
        finally{world.Free();}
    }
    public enum FeedbackFault { MissingSlot, WrongUnit, MissingOwner }
    private partial class InvalidMeter : SolarPanelPart
    {
        public FeedbackFault Fault;
        public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
            base.ColourAnimations.Select(lamp=>lamp with {Feedback=Fault switch
            {
                FeedbackFault.MissingSlot=>SceneAnimationSignal.ScalarAtLeast(new(this,new(99)),ScalarUnit.GameIrradiance,.25),
                FeedbackFault.WrongUnit=>SceneAnimationSignal.ScalarAtLeast(new(this,IrradianceOutput),ScalarUnit.Dimensionless,.25),
                FeedbackFault.MissingOwner=>SceneAnimationSignal.ScalarAtLeast(default,ScalarUnit.GameIrradiance,.25),
                _=>throw new ArgumentOutOfRangeException(nameof(Fault))
            }}).ToArray();
    }
    [Theory]
    [InlineData(FeedbackFault.MissingSlot)]
    [InlineData(FeedbackFault.WrongUnit)]
    [InlineData(FeedbackFault.MissingOwner)]
    public void FeedbackMustMatchDeclaredSlotAndUnits(FeedbackFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var panel=new InvalidMeter {Definition=world.Registry.Definitions[Solar.Value],Fault=fault};
            panel.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Solar.Value,Position=[0,5,0]});world.AttachPart(panel);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);
            Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.ScalarAtLeast(new(panel,SolarPanelPart.IrradianceOutput),(ScalarUnit)999,.25));
            Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.ScalarAtLeast(new(panel,SolarPanelPart.IrradianceOutput),ScalarUnit.GameIrradiance,double.NaN));
        }
        finally{world.Free();}
    }
}
