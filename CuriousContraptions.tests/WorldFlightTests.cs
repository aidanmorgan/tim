using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WorldFlightTests(HeadlessFixture godot)
{
    private const string BallKind="ball", WallKind="wall";
    private const string FirstId="first", SecondId="second", WallId="wall";
    private enum BodyParameter { Radius, Bounce }
    private MachineWorld World()
    {
        var world=new MachineWorld { Gravity=0, Pressure=0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Ball(MachineWorld world,string id,Vector3 position,Vector3 velocity)
    {
        var body=world.AddPart(new() {Id=id,Kind=BallKind,
            Position=[position.X,position.Y,position.Z],
            Properties=new() {[PartParameterName.Of(BodyParameter.Radius)]=.005f,[PartParameterName.Of(BodyParameter.Bounce)]=1}});
        body.Velocity=velocity;
        return body;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(47f)]
    [InlineData(90f)]
    public void ThinWallImpactUsesRemainingTimeInWorldStep(float degrees)
    {
        var world=World();
        try
        {
            var pose=new Transform3D(new Basis(Vector3.Up,Mathf.DegToRad(degrees)),new(0,5,0));
            var wall=world.AddPart(new(){Id=WallId,Kind=WallKind,Position=[0,5,0],Rotation=[0,degrees,0]});
            wall.Boxes.Clear();
            wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1)));
            var ball=Ball(world,FirstId,pose*new Vector3(-.04f,0,0),pose.Basis*Vector3.Right*40);
            world.Start();world.Step();
            var local=pose.AffineInverse()*ball.Position;
            Assert.True(local.X<-.05f,"The ball must rebound and consume remaining flight time.");
            Assert.True(ball.Velocity.Dot(pose.Basis.X)<0);
            Assert.InRange(ball.Velocity.Length(),0,40.001f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossingMovingBodiesBounceWithoutPassingThroughEachOther(bool reverse)
    {
        var world=World();
        try
        {
            var a=Ball(world,FirstId,new(-.04f,5,0),Vector3.Right*40);
            var b=Ball(world,SecondId,new(.04f,5,0),Vector3.Left*40);
            world.Start();
            if(reverse){world.Bodies.Reverse();world.Parts.Reverse();}
            world.Step();
            Assert.InRange(a.Position.X,-.304f,-.302f);
            Assert.InRange(b.Position.X,.302f,.304f);
            Assert.InRange(a.Velocity.X,-40.001f,-39.999f);
            Assert.InRange(b.Velocity.X,39.999f,40.001f);
            Assert.InRange((a.Velocity+b.Velocity).Length(),0,.0001f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedRicochetsRemainInsideThinWallsWithoutEnergyGain(bool reverse)
    {
        var world=World();
        try
        {
            const float wallCentre=.02f, halfThickness=.001f;
            foreach(var side in new[] {-1,1})
            {
                var wall=world.AddPart(new(){Id=side<0?FirstId:SecondId,Kind=WallKind,
                    Position=[side*wallCentre,5,0]});
                wall.Boxes.Clear();
                wall.Boxes.Add(new(Vector3.Zero,new(halfThickness,1,1)));
            }
            var ball=Ball(world,WallId,new(0,5,0),Vector3.Right*40);
            world.Start();
            if(reverse){world.Parts.Reverse();world.Bodies.Reverse();}
            var limit=wallCentre-halfThickness-ball.Radius;
            var observedTravel=0f;
            for(var tick=0;tick<120;tick++)
            {
                var before=ball.Position.X;
                world.Step();
                observedTravel+=Mathf.Abs(ball.Position.X-before);
                if(tick==0)
                    Assert.InRange(ball.Position.X,-.0032f,-.0022f); // Twelve bounces; remaining time is consumed.
                Assert.InRange(ball.Position.X,-limit-.0001f,limit+.0001f);
                Assert.InRange(ball.Velocity.Length(),39.999f,40.001f);
                Assert.Equal(5f,ball.Position.Y);
                Assert.Equal(0f,ball.Position.Z);
            }
            Assert.True(observedTravel>.2f,"Repeated impacts must continue, not stall at a contact.");
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousPerpendicularWallsResolveBothNormals(bool reverse)
    {
        var world=World();
        try
        {
            var xWall=world.AddPart(new(){Id=FirstId,Kind=WallKind,Position=[0,5,0]});
            var zWall=world.AddPart(new(){Id=SecondId,Kind=WallKind,Position=[0,5,0]});
            xWall.Boxes.Clear();zWall.Boxes.Clear();
            xWall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1)));
            zWall.Boxes.Add(new(Vector3.Zero,new(1,1,.001f)));
            var ball=Ball(world,WallId,new(-.04f,5,-.04f),new Vector3(1,0,1).Normalized()*40);
            world.Start();
            if(reverse){world.Parts.Reverse();world.Bodies.Reverse();}
            world.Step();
            Assert.True(ball.Velocity.X<0 && ball.Velocity.Z<0);
            Assert.True(ball.Position.X<-.05f && ball.Position.Z<-.05f);
            Assert.InRange(ball.Velocity.Length(),39.8f,40.001f);
            Assert.InRange(Mathf.Abs(ball.Position.X-ball.Position.Z),0,.002f);
        }
        finally{world.Free();}
    }

    public enum ContactEffect { Redirect, Hide, OpenPath }
    private const string ProbeId="contact_probe";
    private partial class ContactProbe : MachinePart
    {
        public ContactEffect Effect { get; init; }
        public MachinePart? Gate { get; init; }
        public int Calls { get; private set; }
        public float IncomingSpeed { get; private set; }
        protected override void Build()=>Boxes.Add(new(Vector3.Zero,new(.001f,1,1)));
        public override void OnContact(MachinePart body,float speed,MachineWorld world)
        {
            Calls++;IncomingSpeed=speed;
            switch(Effect)
            {
                case ContactEffect.Redirect: body.Velocity=Vector3.Back*10;break;
                case ContactEffect.Hide: body.Visible=false;break;
                case ContactEffect.OpenPath:
                    Gate!.Boxes.Clear(); Boxes.Clear(); body.Velocity=Vector3.Right*40; break;
                default: throw new InvalidOperationException("Unsupported test contact effect.");
            }
        }
    }

    [Theory]
    [InlineData(ContactEffect.Redirect)]
    [InlineData(ContactEffect.Hide)]
    public void ContactCallbackRunsOnceAndItsMutationControlsRemainingFlight(ContactEffect effect)
    {
        var world=World();
        try
        {
            var probe=new ContactProbe {Effect=effect,Definition=new PartDefinition{Id=ProbeId}};
            probe.Configure(new(){Id=ProbeId,Kind=ProbeId,Position=[0,5,0]});
            world.AddChild(probe);world.Parts.Add(probe);
            var ball=Ball(world,FirstId,new(-.04f,5,0),Vector3.Right*40);
            world.Start();world.Step();
            Assert.Equal(1,probe.Calls);
            Assert.InRange(probe.IncomingSpeed,39.999f,40.001f);
            Assert.InRange(ball.Position.X,-.0062f,-.0058f);
            if(effect==ContactEffect.Redirect)
            {
                Assert.True(ball.Visible);
                Assert.InRange(ball.Position.Z,.07f,.08f);
                Assert.Equal(Vector3.Back*10,ball.Velocity);
            }
            else
            {
                Assert.False(ball.Visible);
                Assert.Equal(0,ball.Position.Z);
            }
        }
        finally{world.Free();}
    }

    [Fact]
    public void ZeroTimeCallbackInvalidatesGeometryForEveryBody()
    {
        var world = World();
        try
        {
            var gate = world.AddPart(new() { Id=WallId, Kind=WallKind, Position=[.05f,5,0] });
            gate.Boxes.Clear(); gate.Boxes.Add(new(Vector3.Zero, new(.001f,1,1)));
            var probe = new ContactProbe { Effect=ContactEffect.OpenPath, Gate=gate,
                Definition=new PartDefinition { Id=ProbeId } };
            probe.Configure(new() { Id=ProbeId, Kind=ProbeId, Position=[0,5,0] });
            world.AddChild(probe); world.Parts.Add(probe);
            probe.Boxes.Clear(); probe.Boxes.Add(new(Vector3.Zero,new(.001f,.1f,.1f)));
            var first = Ball(world,FirstId,new(-.006f,5,0),Vector3.Right*40);
            var second = Ball(world,SecondId,new(-.04f,5,.5f),Vector3.Right*40);
            world.Start(); world.Step();
            Assert.Equal(1,probe.Calls);
            Assert.InRange(first.Position.X,.326f,.328f);
            Assert.InRange(second.Position.X,.292f,.294f);
            Assert.Equal(Vector3.Right*40,first.Velocity);
            Assert.Equal(Vector3.Right*40,second.Velocity);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ImpossibleNarrowGapReportsNonConvergenceInsteadOfDiscardingTime()
    {
        var world=World();
        try
        {
            foreach(var side in new[]{-1,1})
            {
                var wall=world.AddPart(new(){Id=side<0?FirstId:SecondId,Kind=WallKind,
                    Position=[side*.0045f,5,0]});
                wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1)));
            }
            Ball(world,WallId,new(0,5,0),Vector3.Zero);
            world.Start();
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            Assert.Equal(0,world.Ticks);
        }
        finally{world.Free();}
    }

    [Fact]
    public void InitialOverlapSeparatesWithoutAddingEnergyAndResetRestoresConstruction()
    {
        var world=World();
        try
        {
            var a=Ball(world,FirstId,new(0,5,0),Vector3.Zero);
            var b=Ball(world,SecondId,new(.004f,5,0),Vector3.Zero);
            world.Start();world.Step();
            Assert.True(a.Position.DistanceTo(b.Position)>=a.Radius+b.Radius);
            Assert.Equal(Vector3.Zero,a.Velocity);Assert.Equal(Vector3.Zero,b.Velocity);
            world.Restore();
            Assert.Equal(new Vector3(0,5,0),world.FindPart(FirstId)!.Position);
            Assert.Equal(new Vector3(.004f,5,0),world.FindPart(SecondId)!.Position);
        }
        finally{world.Free();}
    }
}
