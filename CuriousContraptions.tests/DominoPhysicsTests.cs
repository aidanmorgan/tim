using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class DominoPhysicsTests(NativeSceneFixture godot)
{
    private MachineWorld World(float gravity=0)
    {
        var world=new MachineWorld {Gravity=gravity,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static DominoPart Tile(MachineWorld world,int index,Vector3 position)=>
        (DominoPart)world.AddPart(new(){Id="tile_"+index,Kind="domino",Position=[position.X,position.Y,position.Z]});
    private static BallPart Striker(MachineWorld world,Vector3 position)=>
        (BallPart)world.AddPart(new(){Id="striker",Kind="bowling",Position=[position.X,position.Y,position.Z]});

    [Fact]
    public void DeclaredBoxHasOffsetMassCentreAndNoImplicitSphere()
    {
        var world=World();
        try
        {
            var tile=Tile(world,0,new(0,4,0));
            tile.Rotation=new(.2f,.3f,.4f);
            tile.InitialVelocity=new(.2f,.3f,.4f);
            var origin=tile.Transform;
            world.Start();
            var body=world.PhysicsAssembly.Body(new(tile,MachinePart.RootBody));
            var shape=world.Physics.Collider(body.Id).Declaration.Geometry;
            Assert.Single(world.PhysicsAssembly.Bodies.ToArray(),b=>b.MotionType==PhysicsMotionType.Dynamic);
            Assert.Equal(1,shape.Count);
            Assert.IsType<ConvexBox>(shape[new(0)].Geometry);
            Assert.Equal(BodyEnvelope.None,tile.CollisionEnvelope);
            Assert.Equal(tile.Mass,1/body.InverseMass);
            Assert.InRange((body.Center-SceneGeometryAdapter.CaptureVector(origin*tile.LocalCenterOfMass)).Length,0,1e-7);
            var initial=body.Center;
            for(var i=0;i<12;i++) world.Step();
            Assert.InRange((body.Center-initial-world.PhysicsAssembly.Body(new(tile,MachinePart.RootBody)).LinearVelocity*world.Physics.Time).Length,0,1e-10);
            Assert.InRange((SceneGeometryAdapter.CaptureVector(tile.Transform*tile.LocalCenterOfMass)-body.Center).Length,0,1e-6);
            world.Restore();
            Assert.Equal(origin,world.Parts.OfType<DominoPart>().Single().Transform);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SharedImpactTipsButMissDoesNotAndResetReplays(bool hit)
    {
        var world=World();
        try
        {
            var tile=Tile(world,0,new(0,4,0));
            var ball=Striker(world,new(-1.2f,4.8f,hit?0:2));
            var initial=world.Snapshot();
            (bool Activated,string Signature) Run()
            {
                var striker=world.Parts.OfType<BallPart>().Single();
                striker.InitialVelocity=Vector3.Right*3;
                world.Start();
                for(var i=0;i<160;i++) world.Step();
                var domino=world.Parts.OfType<DominoPart>().Single();
                var body=world.PhysicsAssembly.Body(new(domino,MachinePart.RootBody));
                Assert.Equal(domino.Active,world.Events.ContainsKey(new(MachineEventKind.Activated,domino.Uid)));
                if(hit) Assert.True(body.AngularMomentum.Length>0);
                return (domino.Active,world.StateSignature());
            }
            var first=Run();
            Assert.Equal(hit,first.Activated);
            world.Restore();
            Assert.Equal(initial.Parts.Select(p=>p.Position),world.Snapshot().Parts.Select(p=>p.Position));
            Assert.Equal(first,Run());
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(.75f,true)]
    [InlineData(3f,false)]
    public void GroundedChainRequiresActualTileContact(float spacing,bool reaches)
    {
        var world=World(9.81f);
        try
        {
            var first=Tile(world,0,new(0,-.36f,0));
            var second=Tile(world,1,new(spacing,-.36f,0));
            world.Start();
            var a=world.PhysicsAssembly.Body(new(first,MachinePart.RootBody));
            var b=world.PhysicsAssembly.Body(new(second,MachinePart.RootBody));
            // A finite physical impulse isolates tile-to-tile propagation from
            // a stray bowling ball reaching the second tile directly.
            world.Physics.ApplyImpulse(a.Id,new(.15,0,0),a.Center+new CollisionVector(0,.35,0));
            var contact=false;
            for(var i=0;i<240;i++)
            {
                world.Step();
                foreach(var impact in world.TickImpacts)
                    contact|=impact.Pair.A==a.Id&&impact.Pair.B==b.Id||impact.Pair.A==b.Id&&impact.Pair.B==a.Id;
            }
            Assert.Equal(reaches,contact);
            Assert.Equal(reaches,second.Active);
        }
        finally {world.Free();}
    }

    [Fact]
    public void PassiveDominoRejectsActivationInput()
    {
        var world=World();
        try
        {
            var first=Tile(world,0,new(0,4,0));
            var second=Tile(world,1,new(2,4,0));
            Assert.True(first.CanSendActivation);
            Assert.False(first.CanReceiveActivation);
            Assert.False(world.Connect(first,second));
            Assert.Throws<InvalidOperationException>(()=>world.Activate(first));
            Assert.False(first.Active);
        }
        finally {world.Free();}
    }
}
