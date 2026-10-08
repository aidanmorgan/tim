using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneMechanicalTransferTests(NativeSceneFixture godot)
{

    public enum DeclarationCase { Valid, DuplicateSource, DuplicateReceiver, MissingSourceSlot, MissingReceiverSlot, ForeignBody, UndeclaredBody }

    private partial class TransferProbe : MachinePart
    {
        private static readonly MechanicalSourceSlot SupplySlot=new();
        private static readonly MechanicalReceiverSlot SampleSlot=new();
        public SceneMechanicalSourceKey Supply=>new(new(this,RootBody),SupplySlot);
        public SceneMechanicalReceiverKey Sample=>new(new(this,RootBody),SampleSlot);
        public DeclarationCase Case { get; init; }
        public override IReadOnlyList<SceneMechanicalSourceKey> PhysicsTransferSources=>Case switch
        {
            DeclarationCase.DuplicateSource=>[Supply,Supply],
            DeclarationCase.MissingSourceSlot=>[new(Supply.Body,null!)],
            DeclarationCase.ForeignBody=>[new(new(null,Workbench.Body),SupplySlot)],
            DeclarationCase.UndeclaredBody=>[new(new(this,WindmillPart.RotorBody),SupplySlot)],
            DeclarationCase.Valid or DeclarationCase.DuplicateReceiver or DeclarationCase.MissingReceiverSlot=>[Supply],
            _=>throw new ArgumentOutOfRangeException()
        };
        public override IReadOnlyList<SceneMechanicalReceiverKey> PhysicsTransferReceivers=>Case switch
        {
            DeclarationCase.DuplicateReceiver=>[Sample,Sample],
            DeclarationCase.MissingReceiverSlot=>[new(Sample.Body,null!)],
            DeclarationCase.Valid or DeclarationCase.DuplicateSource or DeclarationCase.MissingSourceSlot or
                DeclarationCase.ForeignBody or DeclarationCase.UndeclaredBody=>[Sample],
            _=>throw new ArgumentOutOfRangeException()
        };
        public double InitialSupply { get; init; }
        public bool Enabled { get; set; }=true;
        public override BodyEnvelope CollisionEnvelope=>BodyEnvelope.Sphere;
        public override BodyDynamics InitialBodyDynamics=>BodyDynamics.SolidSphere(1,Radius,default,default);
        public override IReadOnlyList<SceneEnergyStoreDeclaration> PhysicsEnergyStores=>
            [new(new(this,RootBody),1,InitialSupply)];
        protected override void Build() { Dynamic=true;Drag=0; }
        public MechanicalTransferLoad Declaration(MachineWorld world)
        {
            var body=world.PhysicsAssembly.Body(new(this,RootBody)).Id;
            var frame=world.PhysicsAssembly.Body(new(null,Workbench.Body)).Id;
            return new(world.TransferBindings.Transfer(Supply,Sample),
                new(new StoredFlowSource(world.TransferBindings.Source(Supply),body,4,new(1,4))),
                new PointPowerPort(body,frame,default,new(1,0,0)),new(1,1),1e-10);
        }
        public override void PreparePhysics(MachineWorld world,float delta)
        {
            if(Enabled)world.AddTransferLoad(Declaration(world));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.001)]
    [InlineData(1)]
    public void GameplaySubstepsCommitFiniteSupplyClearDeclarationsAndReplay(double initial)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var part=new TransferProbe {InitialSupply=initial,Position=new(0,6,0)};
            FixtureParts.Attach(world,part,FixturePartId.First);
            Assert.Throws<ArgumentNullException>(()=>world.AddTransferLoad(null!));
            world.Start();
            Assert.Throws<InvalidOperationException>(()=>world.AddTransferLoad(part.Declaration(world)));
            var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            var before=world.Physics.Capture();
            world.Step();
            var after=world.Physics.Capture();
            var store=world.Physics.EnergyStore(body.Id);
            var expected=Math.Min(initial,4*(double)(MachineWorld.Tick/MachineWorld.Substeps)*MachineWorld.Substeps);
            Assert.InRange(Math.Abs(store.ReleasedEnergy-expected),0,1e-14);
            Assert.InRange(Math.Abs(body.LinearVelocity.X-expected/4),0,1e-14);
            Assert.Equal(initial,store.InitialEnergy);
            Assert.Equal(0,store.AcceptedEnergy);
            Assert.InRange(Math.Abs(store.Energy+store.ReleasedEnergy-initial),0,1e-14);
            Assert.Single(world.Physics.Loads.Transfers);
            world.Physics.Restore(before);
            world.Step();
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(after.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            part.Enabled=false;
            var velocity=body.LinearVelocity;
            world.Step();
            Assert.Empty(world.Physics.Loads.Transfers);
            Assert.Equal(velocity,body.LinearVelocity);
            Assert.Equal(store,world.Physics.EnergyStore(body.Id));
            part.Enabled=true;
            world.Step();
            Assert.Single(world.Physics.Loads.Transfers);
            Assert.Equal(world.TransferBindings.Source(part.Supply),world.Physics.Loads.Transfers[0].Source.Id);
            Assert.Equal(world.TransferBindings.Transfer(part.Supply,part.Sample),world.Physics.Loads.Transfers[0].Id);
            world.LoadMachine(new());
            Assert.False(world.HasPhysicsState);
            Assert.Throws<InvalidOperationException>(()=>world.TransferBindings);
            world.Start();
            world.Step();
            Assert.Empty(world.Physics.Loads.Transfers);
            Assert.Empty(world.Physics.EnergyStores.ToArray());
            Assert.Throws<ArgumentException>(()=>world.TransferBindings.Source(part.Supply));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructionOrderAndActivationCannotReassignSupply(bool reverse)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var first=new TransferProbe {InitialSupply=1,Position=new(0,6,0),Enabled=false};
            var second=new TransferProbe {InitialSupply=1,Position=new(3,6,0)};
            if(reverse)
            {
                FixtureParts.Attach(world,second,FixturePartId.Second);
                FixtureParts.Attach(world,first,FixturePartId.First);
            }
            else
            {
                FixtureParts.Attach(world,first,FixturePartId.First);
                FixtureParts.Attach(world,second,FixturePartId.Second);
            }
            world.Start();
            var bindings=world.TransferBindings;
            var a=bindings.Source(first.Supply);var b=bindings.Source(second.Supply);
            Assert.Equal(new MechanicalSourceId(0),a);Assert.Equal(new MechanicalSourceId(1),b);
            var pairs=new HashSet<MechanicalTransferId>
            {
                bindings.Transfer(first.Supply,first.Sample),bindings.Transfer(first.Supply,second.Sample),
                bindings.Transfer(second.Supply,first.Sample),bindings.Transfer(second.Supply,second.Sample)
            };
            Assert.Equal(4,pairs.Count);
            Assert.Equal(first.Sample,bindings.Receiver(bindings.Transfer(first.Supply,first.Sample)));
            Assert.Equal(first.Sample,bindings.Receiver(bindings.Transfer(second.Supply,first.Sample)));
            Assert.Equal(second.Sample,bindings.Receiver(bindings.Transfer(first.Supply,second.Sample)));
            Assert.Equal(second.Sample,bindings.Receiver(bindings.Transfer(second.Supply,second.Sample)));
            Assert.Throws<ArgumentException>(()=>bindings.Receiver(new(4)));
            Assert.Throws<ArgumentException>(()=>bindings.Source(new(first.Supply.Body,new())));
            Assert.Throws<ArgumentException>(()=>bindings.Transfer(first.Supply,new(first.Sample.Body,new())));
            Assert.Throws<ArgumentException>(()=>bindings.Source(default));
            Assert.Throws<ArgumentException>(()=>bindings.Transfer(first.Supply,default));
            world.Step();
            Assert.Equal(b,Assert.Single(world.Physics.Loads.Transfers).Source.Id);
            var secondBody=world.PhysicsAssembly.Body(second.Supply.Body);
            var secondStore=world.Physics.EnergyStore(secondBody.Id);
            first.Enabled=true;second.Enabled=false;
            world.Step();
            Assert.Equal(a,Assert.Single(world.Physics.Loads.Transfers).Source.Id);
            Assert.Equal(secondStore,world.Physics.EnergyStore(secondBody.Id));
            first.Enabled=false;second.Enabled=true;
            world.Step();
            Assert.Equal(b,Assert.Single(world.Physics.Loads.Transfers).Source.Id);
            Assert.True(world.Physics.EnergyStore(secondBody.Id).Energy<secondStore.Energy);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(DeclarationCase.DuplicateSource)]
    [InlineData(DeclarationCase.DuplicateReceiver)]
    [InlineData(DeclarationCase.MissingSourceSlot)]
    [InlineData(DeclarationCase.MissingReceiverSlot)]
    [InlineData(DeclarationCase.ForeignBody)]
    [InlineData(DeclarationCase.UndeclaredBody)]
    public void InvalidDeclarationsCannotPublishBindings(DeclarationCase declaration)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var part=new TransferProbe {Case=declaration,Position=new(0,6,0)};
            FixtureParts.Attach(world,part,FixturePartId.First);
            Assert.ThrowsAny<ArgumentException>(()=>world.Start());
            Assert.False(world.HasPhysicsState);Assert.False(world.Running);
            Assert.Throws<InvalidOperationException>(()=>world.TransferBindings);
        }
        finally {world.Free();}
    }
}
