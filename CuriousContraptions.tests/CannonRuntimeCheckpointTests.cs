using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CannonRuntimeCheckpointTests(NativeSceneFixture godot)
{
    public enum ChamberCase { Charged, Uncharged, Empty }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId CannonCatalogue=new("cannon"),BallCatalogue=new("ball"),SupplyCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observed,Before;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    public static IEnumerable<object[]> Cases=>
        from chamber in Enum.GetValues<ChamberCase>()
        from substep in new[]{1,2,3,4}
        select new object[]{chamber,substep};
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        var spec=new PartSpec {Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]};
        if(part is CannonPart)
        {
            spec.Properties[PartParameterName.Of(CannonParameter.Capacity)]=1;
            spec.Properties[PartParameterName.Of(CannonParameter.ChargePower)]=100;
        }
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(spec);world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [MemberData(nameof(Cases))]
    public void FailedShotRestoresIdentityEnergyTriggerAndRecoil(ChamberCase chamber,int failedSubstep)
    {
        var expected=chamber switch
        {
            ChamberCase.Charged=>CannonShotResult.Fired,
            ChamberCase.Uncharged=>CannonShotResult.Uncharged,
            ChamberCase.Empty=>CannonShotResult.Empty,
            _=>throw new ArgumentOutOfRangeException(nameof(chamber))
        };
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=new CannonPart();
            var supply=new Probe();
            Attach(world,cannon,CannonCatalogue,FixturePartId.First,new(0,5,0));
            MachinePart? payload=null;
            if(chamber!=ChamberCase.Empty)
            {
                payload=new BallPart();
                Attach(world,payload,BallCatalogue,FixturePartId.Second,new(0,5,0));
            }
            Attach(world,supply,SupplyCatalogue,FixturePartId.Third,new(-5,1,0));
            if(chamber!=ChamberCase.Uncharged)
                Assert.True(world.Connect(supply,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            var construction=Saved(world);
            var key=new CuriousContraptions.Presentation.SceneOccurrenceKey(cannon,CannonPart.ShotOccurrence);
            var baseline=cannon.OccurrenceAnimations[0].Target.Transform;
            world.Start();
            for(var i=0;i<4;i++)world.Step();
            Assert.Equal(chamber==ChamberCase.Uncharged?0:1,cannon.StoredEnergy);
            world.Activate(cannon);world.Step();
            Assert.Equal(CannonShotResult.None,cannon.LastShot);
            var phase=cannon.Phase;
            var physics=world.Physics.Capture();
            var ticks=world.Ticks;
            var events=world.Events.ToArray();
            var substep=0;
            supply.Observed=()=>
            {
                Assert.Equal(expected,cannon.LastShot);
                Assert.Equal(chamber==ChamberCase.Charged?1:0,cannon.ShotCount);
                if(chamber==ChamberCase.Charged)
                {
                    Assert.Same(payload,cannon.LastPayload);
                    Assert.True(cannon.ReleasedEnergy>0);
                }
                if(++substep==failedSubstep)throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(failedSubstep,substep);
            Assert.Equal(ticks,world.Ticks);
            Assert.Equal(phase,cannon.Phase);
            using(var read=world.ReadCommittedPoses())
                Assert.Equal(phase,read.ReadEnum(Bridge.PoseSample.Current,new Bridge.EnumReadKey<CannonPhase>(
                    world.PhysicsAssembly.QueryOwnerId(new(cannon,MachinePart.RootBody)),CannonPart.PhaseOutput)).Value);
            Assert.Equal(CannonShotResult.None,cannon.LastShot);
            Assert.Equal(0,cannon.ShotCount);Assert.Null(cannon.LastPayload);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(physics.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            Assert.Equal(events,world.Events.ToArray());
            world.PresentFrame(.04,1);
            Assert.Equal(0,cannon.RecoilOffset);
            Assert.Equal(0UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            Assert.Equal(baseline,cannon.OccurrenceAnimations[0].Target.Transform);
            supply.Observed=null;
            world.Step();
            Assert.Equal(expected,cannon.LastShot);
            using(var read=world.ReadCommittedPoses())
                Assert.Equal(cannon.Phase,read.ReadEnum(Bridge.PoseSample.Current,new Bridge.EnumReadKey<CannonPhase>(
                    world.PhysicsAssembly.QueryOwnerId(new(cannon,MachinePart.RootBody)),CannonPart.PhaseOutput)).Value);
            Assert.Equal(chamber==ChamberCase.Charged?1:0,cannon.ShotCount);
            if(chamber==ChamberCase.Charged)Assert.Same(payload,cannon.LastPayload);
            Assert.Equal(chamber==ChamberCase.Charged?1UL:0UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            Assert.Equal(baseline,cannon.OccurrenceAnimations[0].Target.Transform);
            world.PresentFrame(.04,1);
            Assert.Equal(chamber==ChamberCase.Charged,cannon.RecoilOffset<0);
            world.Step();
            Assert.Equal(chamber==ChamberCase.Charged?1:0,cannon.ShotCount);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FullRecoilQueueRejectsPhysicalShotAndDrainingAllowsExactRetry()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=new CannonPart();var supply=new Probe();var payload=new BallPart();
            Attach(world,cannon,CannonCatalogue,FixturePartId.First,new(0,5,0));
            Attach(world,payload,BallCatalogue,FixturePartId.Second,new(0,5,0));
            Attach(world,supply,SupplyCatalogue,FixturePartId.Third,new(-5,1,0));
            Assert.True(world.Connect(supply,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);world.Start();
            var key=new CuriousContraptions.Presentation.SceneOccurrenceKey(cannon,CannonPart.ShotOccurrence);
            supply.Before=()=>{for(var i=0;i<64;i++)world.EmitOccurrence(key,1);};
            world.Step();supply.Before=null;
            for(var i=0;i<4;i++)world.Step();
            Assert.Equal(64UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            world.Activate(cannon);world.Step();
            var ticks=world.Ticks;var before=world.Physics.Capture();
            for(var retry=0;retry<2;retry++)
            {
                Assert.Throws<InvalidOperationException>(world.Step);
                Assert.Equal(ticks,world.Ticks);Assert.Equal(0,cannon.ShotCount);
                Assert.Equal(CannonShotResult.None,cannon.LastShot);Assert.Equal(0,cannon.ReleasedEnergy);
                Assert.Equal(before.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
                Assert.Equal(before.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
                Assert.Equal(64UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            }
            world.PresentFrame(CannonPart.RecoilCompressionSeconds,1);
            Assert.InRange(Math.Abs(cannon.RecoilOffset+CannonPart.MaximumRecoil),0,1e-6);
            world.PresentFrame(CannonPart.RecoilReturnSeconds,1);
            Assert.Equal(64UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Completed);
            Assert.Equal(0,cannon.RecoilOffset);
            world.Step();Assert.Equal(CannonShotResult.Fired,cannon.LastShot);Assert.Equal(1,cannon.ShotCount);
            Assert.True(cannon.ReleasedEnergy>0);Assert.Same(payload,cannon.LastPayload);
            Assert.Equal(65UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(30f,45f,60f)]
    public void HiddenRotatedShotRetainsFeedbackAndLoadInvalidatesIt(float x,float y,float z)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=new CannonPart();var supply=new Probe();var payload=new BallPart();
            Attach(world,cannon,CannonCatalogue,FixturePartId.First,new(0,5,0));
            cannon.RotationDegrees=new(x,y,z);
            Attach(world,payload,BallCatalogue,FixturePartId.Second,new(0,5,0));
            Attach(world,supply,SupplyCatalogue,FixturePartId.Third,new(-5,1,0));
            Assert.True(world.Connect(supply,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);var sleeve=cannon.OccurrenceAnimations[0].Target;var baseline=sleeve.Transform;
            world.Start();for(var i=0;i<4;i++)world.Step();
            cannon.Visible=false;world.PresentFrame(0,1);
            world.Activate(cannon);world.Step();world.Step();
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
            var key=new CuriousContraptions.Presentation.SceneOccurrenceKey(cannon,CannonPart.ShotOccurrence);
            Assert.Equal(1UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            var physics=world.Physics.Capture();var ticks=world.Ticks;world.Running=false;
            world.PresentFrame(1,1);Assert.Equal(baseline,sleeve.Transform);
            cannon.Visible=true;world.PresentFrame(0,1);
            Assert.True(world.ReadOccurrenceFeedback(key,0).Occurrences.Playing>0);
            Assert.InRange(Math.Abs(cannon.RecoilOffset+CannonPart.MaximumRecoil*Math.Tanh(1)),0,1e-6);
            Assert.Equal(baseline.Basis,sleeve.Basis);
            world.PresentFrame(0,1);
            Assert.Equal(baseline,sleeve.Transform);
            Assert.Equal(1UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Completed);
            Assert.Equal(ticks,world.Ticks);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(physics.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            world.Restore();Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.PresentFrame(.1,1);
            var restored=(CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,restored.RecoilOffset);Assert.Equal(0,restored.ShotCount);
            Assert.Equal(baseline,restored.OccurrenceAnimations[0].Target.Transform);
            Assert.Throws<ArgumentException>(()=>world.ReadOccurrenceFeedback(key,0));
        }
        finally{world.Free();}
    }
}
