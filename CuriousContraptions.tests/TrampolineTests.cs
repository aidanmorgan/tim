using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineTests(NativeSceneFixture godot)
{
    private enum Role { Bed, Cargo, Receiver, First, Second }
    private static string Id(Role role)=>role switch
    {
        Role.Bed=>"bed",Role.Cargo=>"cargo",Role.Receiver=>"receiver",Role.First=>"first",Role.Second=>"second",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Find(MachineWorld world,Role role)=>
        world.FindPart(Id(role))??throw new InvalidOperationException("Missing fixture part.");
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static TrampolinePart Bed(MachineWorld world, float tension = 180, Vector3 rotation = default) =>
        (TrampolinePart)world.AddPart(new() { Id = Id(Role.Bed), Kind = TrampolinePart.CatalogId,
            Position = [0,4,0], Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z),
            Properties = new() { [PartParameterName.Of(TrampolineParameter.Tension)] = tension } });
    private static MachinePart Ball(MachineWorld world, Role role, Vector3 at, float mass = 1) =>
        world.AddPart(new() { Id = Id(role), Kind = "ball", Position = [at.X,at.Y,at.Z],
            Properties = new() { [PartParameterName.Of(WeightParameter.Mass)] = mass } });

    [Theory]
    [InlineData(.5f,6f,180f)]
    [InlineData(1f,6f,180f)]
    [InlineData(4f,2f,180f)]
    [InlineData(1f,6f,600f)]
    [InlineData(1f,2f,120f)]
    public void ContactStoresAndReturnsOnlyPartOfIncomingEnergy(float mass, float speed, float tension)
    {
        var world = World();
        try
        {
            var bed = Bed(world, tension);
            var ball = Ball(world,Role.Cargo,new(0,4.56f,0),mass);
            ball.InitialVelocity = Vector3.Down * speed;
            var saved = JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var incoming = .5f*mass*speed*speed;
            var peak = 0f;var peakEnergy = 0d;
            for(var i=0;i<120;i++)
            {
                world.Step();
                peak = Math.Max(peak,bed.Compression);
                peakEnergy = Math.Max(peakEnergy,.5f*mass*world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.LengthSquared+bed.StoredElasticEnergy);
                if(bed.ContactCount>0)
                {
                    var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
                    var frame=world.PhysicsAssembly.Body(new(bed,MachinePart.RootBody));
                    var load=Assert.Single(world.Physics.Loads.Compliant.ToArray(),load=>load.Body==body.Id&&load.Frame==frame.Id);
                    Assert.Equal(tension,load.Stiffness);
                    Assert.Equal(bed.ReadParameter(TrampolineParameter.DampingRatio),load.DampingRatio);
                    var at=bed.ToLocal(ball.Position);
                    Assert.Equal(TrampolinePart.RestHeight-bed.Compression,bed.MembraneHeight(new(at.X,at.Z)),3);
                }
            }
            Assert.Equal(1,bed.ImpactCount);
            Assert.InRange(peak,.03f,TrampolinePart.MaximumStroke);
            Assert.InRange(peakEnergy,0,incoming*1.05f); // bounded fixed-step integration error
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Y,speed*.25f,speed*.95f);
            Assert.Equal(0,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.X);Assert.Equal(0,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Z);
            Assert.Equal(0,bed.ContactCount);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            bed=(TrampolinePart)Find(world,Role.Bed);
            Assert.Equal(0,bed.Compression);Assert.Equal(0,bed.ImpactCount);Assert.Equal(0,bed.StoredElasticEnergy);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f,0f,30f)]
    [InlineData(0f,0f,-30f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(180f,0f,0f)]
    public void ContactUsesTheRotatedSurfaceNormal(float x,float y,float z)
    {
        var world=World();
        try
        {
            var bed=Bed(world,180,new(x,y,z));
            var ball=Ball(world,Role.Cargo,bed.Transform*new Vector3(0,.56f,0));
            var normal=bed.Basis.Y.Normalized();
            ball.InitialVelocity=-normal*4;world.Start();
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(1,bed.ImpactCount);
            Assert.InRange(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal)),1,3.9f);
            Assert.InRange((world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity-SceneGeometryAdapter.CaptureVector(normal)*CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal))).Length,0,.01f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(4f)]
    public void RestingLoadsSettleWithoutRepeatedLaunches(float mass)
    {
        var world=World();world.Gravity=9.81f;
        try
        {
            var bed=Bed(world);
            var ball=Ball(world,Role.Cargo,new(0,4.5401f,0),mass);
            world.Start();
            for(var i=0;i<2400;i++)world.Step();
            Assert.Equal(1,bed.ContactCount);
            Assert.Equal(0,bed.ImpactCount);
            Assert.InRange(Math.Abs(bed.Compression-mass*world.Gravity/180),0,.015f);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,0,.03f);
            var before=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity;var position=ball.Position;var compression=bed.Compression;
            bed._Process(.5);
            Assert.Equal(before,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);Assert.Equal(position,ball.Position);Assert.Equal(compression,bed.Compression);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(1.28f,.56f,0f,0f,-4f,0f)] // rigid rim
    [InlineData(2f,.56f,0f,0f,-4f,0f)] // outside finite bed
    [InlineData(0f,-.95f,0f,0f,4f,0f)] // back: start clear of the rigid plate
    [InlineData(-2f,.58f,0f,4f,0f,0f)] // grazing above
    public void RimMissBackAndGrazingDoNotBecomeMembraneLaunches(float x,float y,float z,float vx,float vy,float vz)
    {
        var world=World();
        try
        {
            var bed=Bed(world);
            var ball=Ball(world,Role.Cargo,bed.Transform*new Vector3(x,y,z));
            ball.InitialVelocity=new(vx,vy,vz);world.Start();
            for(var i=0;i<90;i++)world.Step();
            Assert.Equal(0,bed.ImpactCount);Assert.Equal(0,bed.ContactCount);Assert.Equal(0,bed.Compression);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,0,new Vector3(vx,vy,vz).Length()+.01f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void TensionChangesRealCompressionAndContactTime()
    {
        (float Peak,int Exit) Measure(float tension)
        {
            var world=World();
            try
            {
                var bed=Bed(world,tension);var ball=Ball(world,Role.Cargo,new(0,4.56f,0));
                ball.InitialVelocity=Vector3.Down*4;world.Start();var peak=0f;
                for(var i=0;i<240;i++)
                {
                    world.Step();peak=Math.Max(peak,bed.Compression);
                    if(bed.ImpactCount>0&&bed.ContactCount==0)return(peak,i);
                }
                throw new Exception("Contact failed to release.");
            }
            finally{world.Free();}
        }
        var soft=Measure(120);var firm=Measure(600);
        Assert.True(soft.Peak>firm.Peak*1.8f);Assert.True(soft.Exit>firm.Exit*1.8f);
    }

    [Theory]
    [InlineData(-30f,0f,true)]
    [InlineData(-30f,2f,false)]
    [InlineData(0f,0f,false)]
    public void FallingBallReachesReceiverOnlyWithAimedMembrane(float angle,float depth,bool captured)
    {
        var world=World();world.Gravity=9.81f;
        try
        {
            var bed=Bed(world,180,new(0,0,angle));bed.Position=new(0,3,depth);
            var ball=Ball(world,Role.Cargo,new(0,7,0));
            var basket=world.AddPart(new(){Id=Id(Role.Receiver),Kind="basket",Position=[5.4f,1.2f,0]});
            world.Start();
            var samples=new List<string>();
            for(var i=0;i<960;i++)
            {
                world.Step();
                if(i%30==0&&i<300)samples.Add($"{i}: {ball.Position} / {world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity}; compression {bed.Compression}");
            }
            Assert.True(captured==world.Events.ContainsKey(new(MachineEventKind.Captured,basket.Uid,ball.Uid)),
                $"Expected capture {captured}; impacts {bed.ImpactCount}. "+string.Join("\n",samples));
        }
        finally{world.Free();}
    }

    [Fact]
    public void IndependentConcurrentImpactsDoNotDependOnBodyInsertionOrder()
    {
        (Vector3 A,Vector3 B,CollisionVector Va,CollisionVector Vb,int Hits) Run(bool reverse)
        {
            var world=World();
            try
            {
                var bed=Bed(world);
                MachinePart a,b;
                if(reverse){b=Ball(world,Role.Second,new(.4f,4.56f,0),2);a=Ball(world,Role.First,new(-.4f,4.56f,0));}
                else{a=Ball(world,Role.First,new(-.4f,4.56f,0));b=Ball(world,Role.Second,new(.4f,4.56f,0),2);}
                a.InitialVelocity=b.InitialVelocity=Vector3.Down*3;world.Start();
                for(var i=0;i<120;i++)world.Step();
                return(a.Position,b.Position,world.PhysicsAssembly.Body(new(a,MachinePart.RootBody)).LinearVelocity,world.PhysicsAssembly.Body(new(b,MachinePart.RootBody)).LinearVelocity,bed.ImpactCount);
            }
            finally{world.Free();}
        }
        var forward=Run(false);var reverse=Run(true);
        Assert.Equal(forward,reverse);Assert.Equal(2,forward.Hits);
        Assert.InRange(forward.Va.Y,.5f,3);Assert.InRange(forward.Vb.Y,.5f,3);
    }

    [Theory]
    [InlineData(4f)]
    [InlineData(8f)]
    public void BottomingOutAbsorbsExcessEnergyInsteadOfAddingLaunchEnergy(float mass)
    {
        var world=World();
        try
        {
            var bed=Bed(world);var ball=Ball(world,Role.Cargo,new(0,4.56f,0),mass);
            ball.InitialVelocity=Vector3.Down*12;world.Start();
            var peak=0f;
            for(var i=0;i<180;i++)
            {
                world.Step();peak=Math.Max(peak,bed.Compression);
                Assert.InRange(bed.Compression,0,TrampolinePart.MaximumStroke);
                Assert.InRange(.5f*mass*world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.LengthSquared+bed.StoredElasticEnergy,0,.5f*mass*144*1.05f);
            }
            Assert.InRange(peak,.60f,TrampolinePart.MaximumStroke);
            Assert.Equal(1,bed.ImpactCount);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    [InlineData(24)]
    public void ResetDuringContactRestoresUnloadedGeometryAndReplay(int ticks)
    {
        var world=World();
        try
        {
            var bed=Bed(world);var ball=Ball(world,Role.Cargo,new(0,4.56f,0));
            ball.InitialVelocity=Vector3.Down*4;
            var json=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<ticks;i++)world.Step();
            var position=ball.Position;var velocity=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity;
            world.Restore();bed=(TrampolinePart)Find(world,Role.Bed);ball=Find(world,Role.Cargo);
            Assert.Equal(0,bed.ContactCount);Assert.Equal(0,bed.Compression);
            Assert.Equal(TrampolinePart.RestHeight,bed.MembraneHeight(Vector2.Zero));
            Assert.Equal(json,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.LoadMachine(JsonSerializer.Deserialize(json,MachineJson.Default.MachineData)!);
            ball=Find(world,Role.Cargo);world.Start();
            for(var i=0;i<ticks;i++)world.Step();
            Assert.Equal(position,ball.Position);Assert.Equal(velocity,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.7f)]
    [InlineData(.8f)]
    public void MembraneSkinDoesNotCutThroughDeepOrOffCentreLoads(float x)
    {
        var world=World();
        try
        {
            var bed=Bed(world);var ball=Ball(world,Role.Cargo,new(x,4.56f,0));
            ball.InitialVelocity=Vector3.Down*5;world.Start();
            for(var tick=0;tick<40;tick++)
            {
                world.Step();
                if(bed.ContactCount==0)continue;
                var at=bed.ToLocal(ball.Position);
                for(var ring=0;ring<5;ring++)
                for(var step=0;step<12;step++)
                {
                    var radius=ball.Radius*ring/5;
                    var offset=Vector2.FromAngle(step*Mathf.Tau/12)*radius;
                    var point=new Vector2(at.X,at.Z)+offset;
                    var underside=at.Y-Mathf.Sqrt(ball.Radius*ball.Radius-radius*radius);
                    Assert.True(bed.MembraneHeight(point)<=underside+.001f);
                }
            }
            Assert.Equal(TrampolinePart.RestHeight,bed.MembraneHeight(TrampolinePart.BedHalf));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f,.12f)]
    [InlineData(1201f,.12f)]
    [InlineData(180f,0f)]
    [InlineData(180f,.81f)]
    [InlineData(float.NaN,.12f)]
    public void InvalidMaterialParametersAreRejected(float tension,float damping)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="bad",Kind=TrampolinePart.CatalogId,
                Properties=new(){[PartParameterName.Of(TrampolineParameter.Tension)]=tension,[PartParameterName.Of(TrampolineParameter.DampingRatio)]=damping}}));
        }
        finally{world.Free();}
    }
}
