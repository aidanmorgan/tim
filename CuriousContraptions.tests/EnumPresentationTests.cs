using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class EnumPresentationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Cannon=new("cannon"),Ball=new("ball");
    public enum Fault { None, MissingSource, ForeignSource, DuplicateRotation, DuplicateColour, PhysicalTarget, SharedMaterial }
    private partial class Probe:BatteryPart
    {
        public readonly SimulationState<CannonPhase> State=new(CannonPhase.Empty);
        public CannonPhase Next=CannonPhase.Empty;
        public Fault Fault;
        public MachinePart? Foreign;
        public int Visits,FailAt;
        public MeshInstance3D Flag=null!,Indicator=null!,Other=null!;
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[State];
        public override IReadOnlyList<SceneEnumObservation> EnumObservations=>[new SceneEnumObservation<CannonPhase>(new(0),State)];
        protected override void Build()
        {
            base.Build();
            Flag=PartArt.Box(Visual,new(.2f,.1f,.1f),Colors.White,new(0,1,0));
            Indicator=PartArt.Box(Visual,new(.1f,.1f,.1f),Colors.Gray,new(.3f,1,0));
            Other=PartArt.Box(Visual,new(.1f,.1f,.1f),Colors.Gray,new(-.3f,1,0));
        }
        public override IReadOnlyList<SceneEnumBinding> EnumBindings
        {
            get
            {
                var source=new SceneEnumObservationKey<CannonPhase>(Fault==Fault.ForeignSource?Foreign!:this,
                    new(Fault==Fault.MissingSource?99:0));
                Node3D target=Fault==Fault.PhysicalTarget?this:Flag;
                var rotation=new SceneEnumRotation<CannonPhase>(target,source,Angles(),AnimationRotationAxis.Z);
                var colour=new SceneEnumColour<CannonPhase>(Indicator,source,Colours());
                if(Fault==Fault.SharedMaterial)Other.MaterialOverride=Indicator.MaterialOverride;
                return Fault switch
                {
                    Fault.DuplicateRotation=>[rotation,rotation,colour],
                    Fault.DuplicateColour=>[rotation,colour,colour],
                    _=>[rotation,colour]
                };
            }
        }
        public override void BeforeNetworks(MachineWorld world)=>State.Value=Next;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected enum presentation rollback.");}
    }
    private static double Angle(CannonPhase phase)=>phase switch
    {
        CannonPhase.Empty=>0,CannonPhase.Loading=>.1,CannonPhase.Charging=>.2,
        CannonPhase.Ready=>.3,CannonPhase.Firing=>.4,CannonPhase.Jammed=>.5,
        _=>throw new ArgumentOutOfRangeException(nameof(phase))
    };
    private static Color Colour(CannonPhase phase)=>phase switch
    {
        CannonPhase.Empty=>Colors.Gray,CannonPhase.Loading=>Colors.Red,CannonPhase.Charging=>Colors.Blue,
        CannonPhase.Ready=>Colors.Yellow,CannonPhase.Firing=>Colors.Green,CannonPhase.Jammed=>Colors.Purple,
        _=>throw new ArgumentOutOfRangeException(nameof(phase))
    };
    private static EnumPresentationMap<CannonPhase,double> Angles()=>new(Enum.GetValues<CannonPhase>().ToDictionary(p=>p,Angle));
    private static EnumPresentationMap<CannonPhase,Color> Colours()=>new(Enum.GetValues<CannonPhase>().ToDictionary(p=>p,Colour));
    private static Color Ink(MeshInstance3D node)=>((StandardMaterial3D)node.MaterialOverride).AlbedoColor;
    private static string Saved(MachineWorld world)=>System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Probe Add(MachineWorld world)
    {
        var p=new Probe{Definition=world.Registry.Definitions[Battery.Value]};
        p.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,5,0]});
        world.AttachPart(p);return p;
    }
    public static IEnumerable<object[]> Phases()=>Enum.GetValues<CannonPhase>().Select(p=>new object[]{p});
    [Theory]
    [MemberData(nameof(Phases))]
    public void EveryTypedPhaseUsesCommittedValuesCatchesUpAfterHidingAndRestores(CannonPhase phase)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var p=Add(world);var saved=Saved(world);var baseline=p.Flag.Transform;world.Start();
            p.Next=phase;world.Step();Assert.Equal(baseline,p.Flag.Transform);Assert.Equal(Colors.Gray,Ink(p.Indicator));
            p.State.Value=CannonPhase.Jammed; // Unpublished writes must never drive presentation.
            world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(p.Flag.Rotation.Z-Angle(phase)),0,1e-6);Assert.Equal(Colour(phase),Ink(p.Indicator));
            var shown=p.Flag.Transform;var ink=Ink(p.Indicator);
            p.Visible=false;p.Next=CannonPhase.Loading;world.Step();world.PresentFrame(.3,1);
            Assert.Equal(shown,p.Flag.Transform);Assert.Equal(ink,Ink(p.Indicator));
            p.Visible=true;world.Running=false;var physical=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(0,1);Assert.InRange(Math.Abs(p.Flag.Rotation.Z-.1),0,1e-6);
            Assert.Equal(Colors.Red,Ink(p.Indicator));Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Restore();Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void FailedSubstepCannotPublishEitherProperty(int substep)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var p=Add(world);world.Start();world.PresentFrame(0,1);
            p.Next=CannonPhase.Ready;p.FailAt=substep;Assert.Throws<InvalidOperationException>(world.Step);
            world.PresentFrame(.1,1);Assert.Equal(0,p.Flag.Rotation.Z);Assert.Equal(Colors.Gray,Ink(p.Indicator));
            Assert.Equal(CannonPhase.Empty,p.State.Value);Assert.Equal(0,world.Ticks);
            p.FailAt=0;world.Step();world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(p.Flag.Rotation.Z-.3),0,1e-6);Assert.Equal(Colors.Yellow,Ink(p.Indicator));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(Fault.MissingSource)] [InlineData(Fault.ForeignSource)]
    [InlineData(Fault.DuplicateRotation)] [InlineData(Fault.DuplicateColour)]
    [InlineData(Fault.PhysicalTarget)] [InlineData(Fault.SharedMaterial)]
    public void InvalidBindingRejectsBeforeRunWithoutChangingConstruction(Fault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var p=Add(world);p.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[5,5,0]});
            var saved=Saved(world);var baseline=p.Flag.Transform;p.Fault=fault;
            Assert.Throws<ArgumentException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(saved,Saved(world));Assert.Equal(baseline,p.Flag.Transform);
            p.Fault=Fault.None;
            if(fault==Fault.SharedMaterial)p.Other.MaterialOverride=new StandardMaterial3D{AlbedoColor=Colors.Gray};
            world.Start();world.Step();world.PresentFrame(0,1);
        }
        finally{world.Free();}
    }
    [Fact]
    public void MappingIsCompleteValidatedAndCopied()
    {
        var values=Enum.GetValues<CannonPhase>().ToDictionary(p=>p,Angle);
        var map=new EnumPresentationMap<CannonPhase,double>(values);values[CannonPhase.Ready]=9;
        Assert.Equal(.3,map.Read(CannonPhase.Ready));Assert.Throws<ArgumentException>(()=>map.Read((CannonPhase)999));
        values.Remove(CannonPhase.Empty);Assert.Throws<ArgumentException>(()=>new EnumPresentationMap<CannonPhase,double>(values));
        values[CannonPhase.Empty]=0;values[(CannonPhase)999]=0;
        Assert.Throws<ArgumentException>(()=>new EnumPresentationMap<CannonPhase,double>(values));
    }
    [Fact]
    public void SharedBindingRejectsPhysicalWriterCoalescesRestoresAndRejectsReparenting()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var p=Add(world);var baseline=p.Flag.Transform;world.Start();p.Next=CannonPhase.Ready;world.Step();
            using var read=world.ReadCommittedPoses();
            var declaration=(SceneEnumRotation<CannonPhase>)p.EnumBindings[0];
            var adapter=new SceneAnimationAdapter(1);var materials=new Dictionary<EnumMaterialId,int>();
            Assert.Throws<InvalidOperationException>(()=>declaration.Bind(p,world.PhysicsAssembly,adapter,read,[p.Flag],materials));
            var binding=declaration.Bind(p,world.PhysicsAssembly,adapter,read,[],materials);
            binding.Stage(read);binding.Commit();binding.Queue();
            Assert.Equal(baseline,p.Flag.Transform);
            Assert.Equal(1,adapter.Present(1,0,0).TransformWrites);
            binding.Queue();Assert.Equal(0,adapter.Present(2,0,0).TransformWrites);
            for(var i=0;i<100;i++){binding.Stage(read);binding.Commit();binding.Queue();}
            var bytes=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++){binding.Stage(read);binding.Commit();binding.Queue();}
            Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);
            adapter.Reset();Assert.Equal(baseline,p.Flag.Transform);
            p.Flag.Reparent(p.Other);
            Assert.Throws<InvalidOperationException>(binding.Queue);
        }
        finally{world.Free();}
    }
    [Fact]
    public void MaterialReplacementAndUndefinedMappingValuesReject()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var p=Add(world);
            var bad=Enum.GetValues<CannonPhase>().ToDictionary(phase=>phase,_=>double.NaN);
            Assert.Throws<ArgumentException>(()=>new SceneEnumRotation<CannonPhase>(p.Flag,new(p,new(0)),new(bad),AnimationRotationAxis.Z));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SceneEnumRotation<CannonPhase>(p.Flag,new(p,new(0)),Angles(),(AnimationRotationAxis)999));
            world.Start();world.PresentFrame(0,1);
            p.Indicator.MaterialOverride=new StandardMaterial3D{AlbedoColor=Colors.Gray};
            Assert.Throws<InvalidOperationException>(()=>world.PresentFrame(0,1));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CannonChargeReadyAndShotHaveNoSubstepArtworkWrites(bool powered)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var spec=new PartSpec{Id=FixtureParts.Id(FixturePartId.First),Kind=Cannon.Value,Position=[0,5,0]};
            spec.Properties[PartParameterName.Of(CannonParameter.Capacity)]=1;
            spec.Properties[PartParameterName.Of(CannonParameter.ChargePower)]=100;
            var p=(CannonPart)world.AddPart(spec);
            world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Ball.Value,Position=[0,5,0]});
            if(powered)
            {
                var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[-5,1,0]});
                Assert.True(world.Connect(battery,SocketId.Supply,p,SocketId.PowerIn,ConnectionDomain.Electrical));
            }
            var rotation=Assert.IsType<SceneEnumRotation<CannonPhase>>(p.EnumBindings[0]);
            var colour=Assert.IsType<SceneEnumColour<CannonPhase>>(p.EnumBindings[1]);
            var mesh=(MeshInstance3D)colour.Target;var baseline=rotation.Target.Transform;var saved=Saved(world);
            world.Start();world.PresentFrame(0,1);var initial=rotation.Target.Transform;var initialInk=Ink(mesh);
            world.Step();world.Step();Assert.Equal(powered?CannonPhase.Ready:CannonPhase.Charging,p.Phase);
            Assert.Equal(initial,rotation.Target.Transform);Assert.Equal(initialInk,Ink(mesh));
            world.PresentFrame(0,1);
            Assert.InRange(Math.Abs(rotation.Target.Rotation.Z-(powered?0:-70*Math.PI/180)),0,1e-6);
            Assert.Equal(new Color(powered?"#f7cb52":"#66b8c9"),Ink(mesh));
            world.Activate(p);world.Step();world.Step();
            Assert.Equal(powered?CannonShotResult.Fired:CannonShotResult.Uncharged,p.LastShot);
            world.PresentFrame(0,1);Assert.Equal(colour.Colours.Read(p.Phase),Ink(mesh));
            world.Restore();Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            var restored=(CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,restored.EnumBindings[0].Target.Transform);
        }
        finally{world.Free();}
    }
}
