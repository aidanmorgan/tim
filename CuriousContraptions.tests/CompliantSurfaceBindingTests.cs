using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CompliantSurfaceBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Ball=new("ball"),Battery=new("battery");
    public enum InvalidDeclaration { ForeignFrame, DuplicateFrame, NonFiniteLaw, UndefinedInitialState }
    private partial class InvalidSurface : BatteryPart
    {
        public InvalidDeclaration Invalid;
        public MachinePart Foreign=null!;
        public override IReadOnlyList<SceneCompliantSurface> PhysicsCompliantSurfaces
        {
            get
            {
                var surface=new SceneCompliantSurface(new(this,RootBody),1,1,.2,.65,180,.12,CompliantContactInitialState.Unloaded);
                return Invalid switch
                {
                    InvalidDeclaration.ForeignFrame=>[surface with {Frame=new(Foreign,RootBody)}],
                    InvalidDeclaration.DuplicateFrame=>[surface,surface],
                    InvalidDeclaration.NonFiniteLaw=>[surface with {Stiffness=double.NaN}],
                    InvalidDeclaration.UndefinedInitialState=>[surface with {InitialState=(CompliantContactInitialState)999}],
                    _=>throw new ArgumentOutOfRangeException(nameof(Invalid))
                };
            }
        }
    }
    private partial class FailureProbe : BatteryPart
    {
        public bool Reject;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(Reject)throw new InvalidOperationException("Injected compliant observation failure.");}
    }
    private T Attach<T>(MachineWorld world,T part,FixturePartId id,Vector3 position) where T:BatteryPart
    {
        part.Definition=world.Registry.Definitions[Battery.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=Battery.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);return part;
    }

    [Theory]
    [InlineData(InvalidDeclaration.ForeignFrame)]
    [InlineData(InvalidDeclaration.DuplicateFrame)]
    [InlineData(InvalidDeclaration.NonFiniteLaw)]
    [InlineData(InvalidDeclaration.UndefinedInitialState)]
    public void InvalidSurfaceRejectsEvenWithoutDynamicCandidates(InvalidDeclaration invalid)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=Attach(world,new InvalidSurface {Invalid=invalid},FixturePartId.First,new(0,4,0));
            part.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[5,4,0]});
            Assert.Throws<ArgumentException>(world.Start);
        }
        finally {world.Free();}
    }

    [Fact]
    public void DeclaredPairsAreStableAndContactReadsRollbackWithPhysicsAndReset()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var bed=(TrampolinePart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=TrampolinePart.CatalogId,Position=[0,4,0]});
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Ball.Value,
                Position=[0,4.56f,0],InitialVelocity=[0,-4,0]});
            var probe=Attach(world,new FailureProbe(),FixturePartId.Third,new(5,4,0));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var declaration=Assert.Single(world.Physics.Loads.Compliant);
            var key=new CompliantContactKey(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id,
                world.PhysicsAssembly.Body(new(bed,MachinePart.RootBody)).Id);
            Assert.Equal(key,declaration.Key);
            var target=new CompliantContactState[1];
            CompliantContactState Read()
            {
                using var read=world.ReadCommittedPoses();
                Assert.Equal(1,read.CompliantContactCount);read.CopyCompliantContacts(PoseSample.Current,target);
                Assert.Equal(target[0],read.ReadCompliantContact(PoseSample.Current,key));return target[0];
            }
            Assert.Equal(CompliantContactPhase.Ready,Read().Phase);
            world.Step();var committed=Read();
            Assert.Equal(CompliantContactPhase.Engaged,committed.Phase);
            Assert.Same(declaration,Assert.Single(world.Physics.Loads.Compliant));
            var physics=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            probe.Reject=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(tick,world.Ticks);Assert.Equal(committed,Read());
            Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());
            probe.Reject=false;world.Step();
            Assert.Same(declaration,Assert.Single(world.Physics.Loads.Compliant));
            world.Physics.CopyCompliantContacts(target);Assert.Equal(target[0],Read());
            var retired=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>retired.CopyCompliantContacts(PoseSample.Current,target));
            retired.Dispose();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();Assert.Equal(CompliantContactPhase.Ready,Read().Phase);
            Assert.NotSame(declaration,Assert.Single(world.Physics.Loads.Compliant));
        }
        finally {world.Free();}
    }
}

