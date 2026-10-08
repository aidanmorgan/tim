using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsEnergyStoreTests
{
    private static readonly CollisionVector Axis=new(1,0,0);
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Body,PhysicsBody Owner)
    {
        public PhysicsEnergyStoreState State=>World.EnergyStore(Owner.Id);
        public double Charge(double power,double seconds)=>World.ChargeEnergyStore(Owner.Id,power,seconds);
        public PoweredImpulseResult Release(double target=40,double maximumImpulse=double.MaxValue)=>
            World.ReleaseEnergyStore(Owner.Id,Body.Id,Axis,Body.Center,target,maximumImpulse);
    }
    private static Fixture Create(double capacity=20,double mass=1,double incoming=0)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            Axis*incoming,default,mass,new(mass,mass,mass));
        var owner=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(100,0,0)),default,default);
        var geometry=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(body,geometry,new(0,0,0)),new(owner,geometry,new(0,0,0))],[],new(default));
        world.InstallEnergyStores([new(owner.Id,capacity,0)]);
        return new(world,body,owner);
    }

    [Fact]
    public void OnlySuppliedWorkChargesAndCapacityRejectsExcess()
    {
        var f=Create();
        Assert.Equal(0,f.Charge(0,10));Assert.Equal(0,f.State.Energy);
        Assert.Equal(5,f.Charge(10,.5));Assert.Equal(5,f.State.Energy);
        Assert.Equal(15,f.Charge(100,1));Assert.Equal(20,f.State.Energy);
        Assert.Equal(0,f.Charge(100,1));Assert.Equal(20,f.State.AcceptedEnergy);
        Assert.Equal(0,f.Charge(10,0));Assert.Equal(20,f.State.Energy);
    }

    [Theory]
    [InlineData(.5,0)]
    [InlineData(1,0)]
    [InlineData(4,0)]
    [InlineData(8,0)]
    [InlineData(1,3)]
    [InlineData(1,-3)]
    public void SharedReleaseAccountsForSuppliedAndDissipatedWork(double mass,double incoming)
    {
        var f=Create(mass:mass,incoming:incoming);f.Charge(20,1);
        var release=f.Release();
        Assert.True(release.Impulse>0);
        var added=.5*mass*(f.Body.LinearVelocity.X-incoming)*(f.Body.LinearVelocity.X+incoming);
        Assert.Equal(added,release.SuppliedWork-release.DissipatedWork,10);
        Assert.InRange(release.SuppliedWork,19.9999,20);
        Assert.Equal(incoming<0?.5*mass*incoming*incoming:0,release.DissipatedWork,10);
        Assert.InRange(f.State.Energy,0,.0001);
        Assert.Equal(f.State.AcceptedEnergy,f.State.Energy+f.State.ReleasedEnergy,10);
    }

    [Fact]
    public void SpeedCapPreservesUnusedCharge()
    {
        var f=Create(100);f.Charge(100,1);
        var release=f.Release(4);
        Assert.Equal(4,f.Body.LinearVelocity.X);Assert.Equal(8,release.SuppliedWork);
        Assert.Equal(92,f.State.Energy);
        Assert.Equal(default,f.Release(4));Assert.Equal(92,f.State.Energy);
    }

    [Fact]
    public void NoChargeCannotAccelerateAndBrakingDoesNotRecharge()
    {
        var f=Create();
        Assert.Equal(default,f.Release());Assert.Equal(default,f.Body.LinearVelocity);
        var incoming=Create(incoming:-1);
        var braking=incoming.Release();
        Assert.Equal(0,braking.SuppliedWork);Assert.Equal(.5,braking.DissipatedWork,10);
        Assert.Equal(0,incoming.State.Energy);Assert.Equal(0,incoming.State.ReleasedEnergy);
        Assert.InRange(incoming.Body.LinearVelocity.Length,0,1e-10);
    }

    [Fact]
    public void SubPrecisionChargeCannotOverdrawOrInventWork()
    {
        var f=Create(incoming:1);f.Charge(float.Epsilon,1);
        var before=f.Body.Snapshot();
        Assert.Equal(default,f.Release());Assert.Equal(before,f.Body.Snapshot());
        Assert.Equal((double)float.Epsilon,f.State.Energy);
    }

    [Fact]
    public void RepeatedReleasesAndRechargeCannotExceedAcceptedWorkAndSnapshotRestoresAll()
    {
        var f=Create(mass:.7);var initial=f.World.Capture();
        for(var i=0;i<1000;i++)
        {
            f.Charge(3,.013);
            var release=f.Release();
            Assert.InRange(release.SuppliedWork,0,f.State.AcceptedEnergy);
            Assert.True(f.State.Energy>=0);
            Assert.InRange(.5*.7*f.Body.LinearVelocity.LengthSquared,0,f.State.AcceptedEnergy+1e-9);
        }
        Assert.InRange(Math.Abs(f.State.AcceptedEnergy-f.State.Energy-f.State.ReleasedEnergy),0,1e-9);
        f.World.Restore(initial);
        Assert.Equal(0,f.State.Energy);Assert.Equal(0,f.State.AcceptedEnergy);Assert.Equal(0,f.State.ReleasedEnergy);
        Assert.Equal(initial.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void VelocityRoundingNeverOverdrawsAcrossMassAndChargeScales()
    {
        foreach(var mass in new[]{.1f,1f,8f,64f,float.MaxValue})
        foreach(var incoming in new[]{-10f,0f,10f})
        foreach(var energy in new[]{float.Epsilon,.0001f,1f,100f,10000f})
        {
            var f=Create(energy,mass,incoming);f.Charge(energy,1);
            var shot=f.Release();
            Assert.InRange(shot.SuppliedWork,0,(double)energy);
            Assert.InRange(f.State.Energy,0,(double)energy);
            Assert.True(f.Body.LinearVelocity.IsFinite);
            var before=.5*(double)mass*incoming*incoming;
            var after=.5*(double)mass*f.Body.LinearVelocity.LengthSquared;
            Assert.True(after<=before+energy+Math.Max(1e-40,Math.Abs(before)*1e-15));
            Assert.Equal(shot.SuppliedWork,f.State.ReleasedEnergy);
        }
    }

    [Fact]
    public void ChargeStepPartitioningAndPowerLossPreserveTheBudget()
    {
        var whole=Create(100,2,1);var split=Create(100,2,1);
        whole.Charge(8,1);
        for(var i=0;i<8;i++)split.Charge(8,.125);
        Assert.Equal(whole.State.Energy,split.State.Energy);
        split.Charge(0,100);Assert.Equal(whole.State.Energy,split.State.Energy);
        Assert.Equal(whole.Release(),split.Release());
        Assert.Equal(whole.Body.Snapshot(),split.Body.Snapshot());
    }

    [Fact]
    public void WorldRejectionDoesNotDebitStoreOrMutateBodies()
    {
        var f=Create();f.Charge(5,1);var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.World.ReleaseEnergyStore(f.Owner.Id,new(99),Axis,default,40,100));
        Assert.Throws<ArgumentException>(()=>f.World.ReleaseEnergyStore(f.Owner.Id,f.Body.Id,default,default,40,100));
        Assert.Throws<ArgumentException>(()=>f.World.ReleaseEnergyStore(f.Body.Id,f.Body.Id,Axis,default,40,100));
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidCapacityIsRejected(double value)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsEnergyStoreDeclaration(new(0),value,0));

    [Theory]
    [InlineData(-1,1)]
    [InlineData(1,-1)]
    [InlineData(double.NaN,1)]
    [InlineData(1,double.PositiveInfinity)]
    [InlineData(double.MaxValue,2)]
    public void InvalidChargeIsAtomic(double power,double seconds)
    {
        var f=Create();f.Charge(5,1);var before=f.State;
        Assert.Throws<ArgumentOutOfRangeException>(()=>f.Charge(power,seconds));
        Assert.Equal(before,f.State);
    }

    [Theory]
    [InlineData(double.NaN,100)]
    [InlineData(double.PositiveInfinity,100)]
    [InlineData(40,-1)]
    [InlineData(40,double.PositiveInfinity)]
    public void InvalidReleaseIsAtomic(double target,double maximumImpulse)
    {
        var f=Create();f.Charge(5,1);var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.Release(target,maximumImpulse));
        Assert.Equal(before.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void InstallationRejectsDuplicatesForeignBodiesDefaultsAndPartialBatches()
    {
        var f=Create();f.Charge(5,1);var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.World.InstallEnergyStores([new(f.Owner.Id,50,0)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallEnergyStores([new(f.Body.Id,10,0),new(new(99),10,0)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallEnergyStores([new(f.Body.Id,10,0),new(f.Body.Id,20,0)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallEnergyStores([default]));
        Assert.Throws<ArgumentNullException>(()=>f.World.InstallEnergyStores(null!));
        Assert.Throws<ArgumentException>(()=>f.World.ChargeEnergyStore(f.Body.Id,1,1));
        Assert.Equal(before.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
        f.World.InstallEnergyStores([new(f.Body.Id,10,0)]);
        f.World.ChargeEnergyStore(f.Body.Id,3,1);
        Assert.Equal(5,f.State.Energy);Assert.Equal(3,f.World.EnergyStore(f.Body.Id).Energy);
        f.World.Restore(before);
        Assert.Equal(before.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
        Assert.Throws<ArgumentException>(()=>f.World.EnergyStore(f.Body.Id));
    }

    [Fact]
    public void SnapshotReplaysChargeReleaseFlightAndImmutableObservationsExactly()
    {
        var f=Create(incoming:-2);f.Charge(8,1);
        var initial=f.World.Capture();var observed=f.State;
        PoweredImpulseResult Run()
        {
            f.Charge(3,.5);var result=f.Release(3);
            f.World.Step([],[],.05);return result;
        }
        var first=Run();var after=f.World.Capture();
        Assert.Equal(8,observed.Energy);Assert.Equal(0,observed.ReleasedEnergy);
        f.World.Restore(initial);
        Assert.Equal(initial.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
        Assert.Equal(first,Run());
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),f.World.EnergyStores.ToArray());
        Assert.Equal(after.Time,f.World.Time);Assert.Equal(after.StepIndex,f.World.StepIndex);
        var foreign=Create();
        Assert.Throws<ArgumentException>(()=>foreign.World.Restore(initial));
    }

    public enum NestedCommand { Install, Charge, Release }
    private sealed record EffectState:PhysicsImpactEffectState;
    private sealed class CallbackEffect(PhysicsBodyId owner,Action callback):PhysicsImpactEffect(owner)
    {
        public override PhysicsImpactEffectState Capture()=>new EffectState();
        public override void Restore(PhysicsImpactEffectState state)
        {
            if(state is not EffectState)throw new ArgumentException("Unexpected effect snapshot.",nameof(state));
        }
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            callback();return new([],[],[]);
        }
    }

    [Theory]
    [InlineData(NestedCommand.Install)]
    [InlineData(NestedCommand.Charge)]
    [InlineData(NestedCommand.Release)]
    public void ReentrantEnergyMutationRejectsAndFailedStepRestoresReservoirAndMotion(NestedCommand command)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-1,0,0)),Axis*2,default,1,new(1,1,1));
        var owner=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        PhysicsWorld world=null!;
        var callback=new CallbackEffect(owner.Id,()=>
        {
            switch(command)
            {
                case NestedCommand.Install:world.InstallEnergyStores([new(body.Id,20,0)]);break;
                case NestedCommand.Charge:world.ChargeEnergyStore(owner.Id,1,1);break;
                case NestedCommand.Release:world.ReleaseEnergyStore(owner.Id,body.Id,Axis,body.Center,5,10);break;
                default:throw new ArgumentOutOfRangeException(nameof(command));
            }
        });
        var geometry=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        world=new([callback],[new(body,geometry,new(0,0,0)),new(owner,geometry,new(0,0,0))],[],
            new(default,maximumStep:.1));
        world.InstallEnergyStores([new(owner.Id,20,0)]);world.ChargeEnergyStore(owner.Id,5,1);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.5));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.EnergyStates.ToArray(),world.EnergyStores.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }

    [Theory]
    [InlineData(CannonParameter.Capacity,"capacity")]
    [InlineData(CannonParameter.ChargePower,"charge_power")]
    public void CannonParameterBoundaryIsCanonicalAndRejectsUndefined(CannonParameter parameter,string serialized)
    {
        Assert.Equal(serialized,PartParameterName.Of(parameter));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((CannonParameter)999));
    }
}
