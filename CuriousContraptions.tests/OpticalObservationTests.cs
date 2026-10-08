using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalObservationTests(NativeSceneFixture godot)
{
    public enum Family { Combiner, And, Or, Xor, Nor, Nand }
    private readonly record struct CatalogueId(string Value);
    private static CatalogueId Catalogue(Family family)=>family switch
    {
        Family.Combiner=>new("beam_combiner"),Family.And=>new("optical_and"),Family.Or=>new("optical_or"),
        Family.Xor=>new("optical_xor"),Family.Nor=>new("optical_nor"),Family.Nand=>new("optical_nand"),
        _=>throw new ArgumentOutOfRangeException(nameof(family))
    };
    private partial class Source : LaserPart
    {
        public Vector3 Direction,Power;
        public int Visits,FailAt;
        public override OpticalEmitter? OpticalSource=>new(Vector3.Zero,Direction,12,Power);
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected spectral publication failure.");}
    }
    private static readonly CatalogueId Laser=new("laser");
    private static void AttachSource(MachineWorld world,Source source,FixturePartId id)
    {
        var at=source.Position;source.Definition=world.Registry.Definitions[Laser.Value];
        source.Configure(new(){Id=FixtureParts.Id(id),Kind=Laser.Value,Position=[at.X,at.Y,at.Z]});
        world.AttachPart(source);
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static ScalarReadKey Key(MachineWorld world,MachinePart part,ScalarObservationSlot slot)=>
        new(world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),slot);
    private static Vector3 Read(MachineWorld world,MachinePart part)
    {
        using var lease=world.ReadCommittedPoses();
        double Channel(ScalarObservationSlot slot)
        {
            var read=lease.ReadScalar(PoseSample.Current,Key(world,part,slot));
            Assert.Equal(ScalarUnit.GameOpticalPower,read.Unit);return read.Value;
        }
        var declarations=part.ScalarObservations;
        return new((float)Channel(declarations[0].Slot),(float)Channel(declarations[1].Slot),(float)Channel(declarations[2].Slot));
    }
    private static Vector3 Output(MachinePart part)=>part switch
    {
        BeamCombinerPart combiner=>combiner.OutputPower,
        OpticalLogicPart gate=>gate.OutputPower,
        _=>throw new ArgumentException("Unsupported test subject.")
    };
    private static Color Lamp(MachinePart part)=>
        ((StandardMaterial3D)Assert.Single(part.SpectralColours).Target.MaterialOverride).AlbedoColor;
    private static void LampEquals(Color expected,MachinePart part)
    {
        var actual=Lamp(part);
        Assert.InRange(Math.Abs(expected.R-actual.R),0,2e-6);Assert.InRange(Math.Abs(expected.G-actual.G),0,2e-6);
        Assert.InRange(Math.Abs(expected.B-actual.B),0,2e-6);Assert.Equal(expected.A,actual.A);
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var family in Enum.GetValues<Family>())
        foreach(var colour in Enum.GetValues<OpticalColour>())yield return [family,colour];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void ActualBeamsPublishAllChannelsAtomicallyAcrossRollbackPauseResetAndSave(Family family,OpticalColour colour)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Catalogue(family).Value,Position=[0,6,0]});
            var power=OpticalColours.Mask(colour)*.75f;
            var first=family is Family.And or Family.Or or Family.Xor;
            var second=family==Family.And;
            var a=new Source {Position=new(-4,6,0),Direction=Vector3.Right,Power=family==Family.Combiner?power:first?Vector3.One:Vector3.Zero};
            var b=new Source {Position=new(0,10,0),Direction=Vector3.Down,Power=second?Vector3.One:Vector3.Zero};
            var c=new Source {Position=new(0,6,4),Direction=Vector3.Forward,Power=family==Family.Combiner?Vector3.Zero:power};
            AttachSource(world,a,FixturePartId.First);AttachSource(world,b,FixturePartId.Second);AttachSource(world,c,FixturePartId.Third);
            var baseline=Lamp(part);var saved=Saved(world);world.Start();Assert.Equal(Vector3.Zero,Read(world,part));world.Step();world.Step();
            var output=Output(part);Assert.True(output.LengthSquared()>0);Assert.Equal(output,Read(world,part));
            Assert.InRange((output-power*.9f).Length(),0,1e-5f);
            LampEquals(baseline,part);
            var ink=OpticalColours.BeamInk(output);
            world.PresentFrame(.1,1);LampEquals(baseline.Lerp(ink,(float)(1-Math.Exp(-1.2))),part);
            part.ReceiveOpticalOutputPower(Vector3.Up);world.PresentFrame(.1,1);
            LampEquals(baseline.Lerp(ink,(float)(1-Math.Exp(-2.4))),part);
            part.ReceiveOpticalOutputPower(output);
            var carrier=family==Family.Combiner?a:c;carrier.Power=Vector3.Zero;
            var stamp=world.ControlRevision;var physical=world.Physics.Capture().BodyStates.ToArray();
            a.FailAt=a.Visits+4;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(stamp,world.ControlRevision);Assert.Equal(output,Output(part));Assert.Equal(output,Read(world,part));
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.PresentFrame(.1,1);var lit=baseline.Lerp(ink,(float)(1-Math.Exp(-3.6)));LampEquals(lit,part);
            a.FailAt=0;world.Step();Assert.Equal(Vector3.Zero,Output(part));Assert.Equal(Vector3.Zero,Read(world,part));
            part.Visible=false;world.PresentFrame(.1,1);LampEquals(lit,part);
            part.Visible=true;world.PresentFrame(0,1);LampEquals(lit.Lerp(baseline,(float)(1-Math.Exp(-1.2))),part);
            world.Running=false;world.PresentFrame(.1,1);Assert.Equal(Vector3.Zero,Read(world,part));
            LampEquals(lit.Lerp(baseline,(float)(1-Math.Exp(-2.4))),part);world.Running=true;
            carrier.Power=power;world.Step();Assert.Equal(output,Read(world,part));
            var producer=new SceneScalarObservations(world.Parts,world.PhysicsAssembly,world.Physics,world.Timers,
                new Dictionary<SceneTimerKey,SimulationTimerId>(),world.Oscillators,new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            for(var i=0;i<100;i++)producer.Capture();var allocated=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-allocated);
            var old=world.ReadCommittedPoses();var key=Key(world,part,part.ScalarObservations[0].Slot);
            world.Restore();Assert.Throws<InvalidOperationException>(()=>old.ReadScalar(PoseSample.Current,key));Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();
            var restored=world.FindPart(FixtureParts.Id(FixturePartId.Fourth))!;Assert.Equal(Vector3.Zero,Read(world,restored));
            Assert.Equal(baseline,Lamp(restored));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(ScalarVectorComponent.X)]
    [InlineData(ScalarVectorComponent.Y)]
    [InlineData(ScalarVectorComponent.Z)]
    public void VectorProjectionIsTypedFiniteAndCheckpointOwned(ScalarVectorComponent component)
    {
        var cell=new SimulationState<Vector3>(new(1,2,3));var source=new ScalarObservationSource(cell,component);
        Assert.Same(cell,source.Cell);Assert.Equal(ScalarObservationStorage.VectorComponent,source.Storage);
        var expected=component switch {ScalarVectorComponent.X=>1,ScalarVectorComponent.Y=>2,ScalarVectorComponent.Z=>3,_=>throw new ArgumentOutOfRangeException(nameof(component))};
        Assert.Equal(expected,source.Read());cell.BeginTransaction();cell.Value=new(float.NaN,float.NaN,float.NaN);
        Assert.Throws<InvalidOperationException>(()=>source.Read());cell.RollbackTransaction();Assert.Equal(expected,source.Read());
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarObservationSource(cell,(ScalarVectorComponent)999));
        Assert.Throws<ArgumentNullException>(()=>new ScalarObservationSource((SimulationState<Vector3>)null!,component));
    }
}
