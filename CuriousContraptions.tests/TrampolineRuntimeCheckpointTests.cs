using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineRuntimeCheckpointTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId BedCatalogue=new("trampoline"),BallCatalogue=new("ball"),ProbeCatalogue=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observed;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    private readonly record struct Reading(int Contacts,int Impacts,float Compression,float Energy,bool Active);
    private static Reading Read(TrampolinePart bed)=>new(bed.ContactCount,bed.ImpactCount,bed.Compression,bed.StoredElasticEnergy,bed.Active);
    private static float[] Skin(TrampolinePart bed)=>
        (from x in new[]{-.9f,-.55f,0,.55f,.9f}
         from z in new[]{-.6f,0,.6f}
         select bed.MembraneHeight(new(x,z))).ToArray();
    private static void Attach(MachineWorld world,MachinePart part,CatalogueId catalogue,FixturePartId id,Vector3 position)
    {
        part.Definition=world.Registry.Definitions[catalogue.Value];
        part.Configure(new(){Id=FixtureParts.Id(id),Kind=catalogue.Value,Position=[position.X,position.Y,position.Z]});
        world.AttachPart(part);
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void FailedContactRestoresEveryPatchAndObservationCursor(bool twoLoads,bool alreadyLoaded)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed=new TrampolinePart();
            Attach(world,bed,BedCatalogue,FixturePartId.First,new(0,4,0));
            var first=new BallPart();
            Attach(world,first,BallCatalogue,FixturePartId.Second,new(twoLoads?-.55f:0,4.56f,0));
            first.InitialVelocity=new(0,-2,0);
            if(twoLoads)
            {
                var second=new BallPart();
                Attach(world,second,BallCatalogue,FixturePartId.Third,new(.55f,4.56f,0));
                second.InitialVelocity=new(0,-2,0);
            }
            var probe=new Probe();
            Attach(world,probe,ProbeCatalogue,FixturePartId.Fourth,new(-5,4,0));
            var construction=Saved(world);
            world.Start();
            if(alreadyLoaded)
            {
                for(var i=0;i<30&&bed.ContactCount==0;i++)world.Step();
                Assert.Equal(twoLoads?2:1,bed.ContactCount);
                bed._Process(0);
            }
            var failed=false;
            for(var tick=0;tick<30&&!failed;tick++)
            {
                var reading=Read(bed);var skin=Skin(bed);
                var physics=world.Physics.Capture();var events=world.Events.ToArray();
                probe.Observed=()=>
                {
                    if(bed.ContactCount==0)return;
                    Assert.NotEqual(reading.Compression,bed.Compression);
                    failed=true;throw new InvalidOperationException();
                };
                try {world.Step();}
                catch(InvalidOperationException) when(failed)
                {
                    Assert.Equal(reading,Read(bed));Assert.Equal(skin,Skin(bed));
                    Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
                    Assert.Equal(events,world.Events.ToArray());
                    probe.Observed=null;
                    world.Step();
                    Assert.Equal(twoLoads?2:1,bed.ContactCount);
                    Assert.Equal(twoLoads?2:1,bed.ImpactCount);
                    Assert.NotEqual(reading.Compression,bed.Compression);
                    Assert.NotEqual(skin,Skin(bed));
                    bed._Process(0);
                }
            }
            Assert.True(failed);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
