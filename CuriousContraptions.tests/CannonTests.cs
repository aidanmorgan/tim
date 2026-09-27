using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class CannonTests(HeadlessFixture godot)
{
    private const string CannonId="cannon",BatteryId="battery",PayloadId="payload",OtherId="other",WallId="wall";
    private const string BatteryKind="battery",BallKind="ball",WallKind="wall",SwitchKind="switch";
    private const string SwitchId="supply_switch",RadiusParameter="radius";
    private MachineWorld World()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static CannonPart Cannon(MachineWorld world,bool powered=true,Vector3 rotation=default)
    {
        var cannon=(CannonPart)world.AddPart(new(){Id=CannonId,Kind=CannonPart.CatalogId,Position=[0,5,0],
            Rotation=[rotation.X,rotation.Y,rotation.Z]});
        if(powered)
        {
            var battery=world.AddPart(new(){Id=BatteryId,Kind=BatteryKind,Position=[-5,1,0]});
            Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
        }
        return cannon;
    }
    private static MachinePart Ball(MachineWorld world,CannonPart cannon,string id,Vector3 local)
    {
        var at=cannon.Transform*local;
        return world.AddPart(new(){Id=id,Kind=BallKind,Position=[at.X,at.Y,at.Z]});
    }
    private static void Steps(MachineWorld world,int count){for(var i=0;i<count;i++)world.Step();}
    private static void Trigger(MachineWorld world,CannonPart cannon){world.Activate(cannon);Steps(world,2);}

    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(30f,45f,60f)]
    public void FiresSamePhysicalPayloadAlongRotatedBarrel(float x,float y,float z)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world,rotation:new(x,y,z));
            var ball=Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Start();Steps(world,200);
            var before=ball.Position;var count=world.Parts.Count;
            Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);Assert.Equal(1,cannon.ShotCount);
            Assert.Same(ball,cannon.LastPayload);Assert.Same(ball,world.FindPart(PayloadId));
            Assert.Equal(count,world.Parts.Count);Assert.True(ball.Visible);
            Assert.InRange(ball.Velocity.Dot(cannon.Basis.X),9.48f,9.50f);
            Assert.InRange((ball.Velocity-cannon.Basis.X*ball.Velocity.Dot(cannon.Basis.X)).Length(),0,.001f);
            Assert.InRange((ball.Position-before).Length(),0,.1f); // moved by integration, not teleported to muzzle
            Assert.InRange(cannon.ReleasedEnergy,44.99,45);
            Assert.InRange(.5*ball.Mass*ball.Velocity.LengthSquared(),0,cannon.ReleasedEnergy+.001);
        }
        finally{world.Free();}
    }

    [Fact]
    public void FiredPayloadReboundsFromThinWallBeyondMuzzle()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            var ball=Ball(world,cannon,PayloadId,Vector3.Zero);
            var wall=world.AddPart(new(){Id=WallId,Kind=WallKind,Position=[2,5,0]});
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1)));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
            Steps(world,30);
            Assert.Same(ball,cannon.LastPayload);
            Assert.InRange(ball.Position.X,.9f,1.66f);
            Assert.True(ball.Velocity.X<0,"The same launched payload must rebound from the distant wall.");
            Assert.InRange(.5*ball.Mass*ball.Velocity.LengthSquared(),0,cannon.ReleasedEnergy);
        }
        finally{world.Free();}
    }

    [Fact]
    public void TriggerCannotChargeOrQueueAnEmptyShot()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world,false);world.Start();Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);Assert.Equal(0,cannon.StoredEnergy);
            Ball(world,cannon,PayloadId,Vector3.Zero);
            Steps(world,30);Assert.Equal(0,cannon.ShotCount);
            Trigger(world,cannon);Assert.Equal(CannonShotResult.Uncharged,cannon.LastShot);
            Assert.Equal(0,cannon.ShotCount);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EmptyChargedAttemptRetainsEnergy()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);Assert.Equal(45,cannon.StoredEnergy);
            Assert.Equal(0,cannon.ReleasedEnergy);
        }
        finally{world.Free();}
    }

    [Fact]
    public void BlockedMuzzleRetainsChargeAndPayloadIdentity()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);var ball=Ball(world,cannon,PayloadId,Vector3.Zero);
            var wall=world.AddPart(new(){Id=WallId,Kind=WallKind,Position=[.8f,5,0]});
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.005f,1,1)));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Obstructed,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ShotCount);
            Assert.Equal(Vector3.Zero,ball.Velocity);Assert.Same(ball,world.FindPart(PayloadId));
        }
        finally{world.Free();}
    }

    [Fact]
    public void ReloadedPhysicalBallCanFireAfterRecharge()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);var first=Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Start();Steps(world,200);Trigger(world,cannon);Steps(world,200);
            Assert.Equal(1,cannon.ShotCount);
            var second=Ball(world,cannon,OtherId,Vector3.Zero);
            Trigger(world,cannon);
            Assert.Equal(2,cannon.ShotCount);Assert.Same(second,cannon.LastPayload);
            Assert.NotSame(first,second);Assert.Same(first,world.FindPart(PayloadId));
        }
        finally{world.Free();}
    }

    [Fact]
    public void TriggerDuringDepartureDoesNotLaunchSameBallAgain()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Start();Steps(world,200);Trigger(world,cannon);Trigger(world,cannon);
            Assert.Equal(1,cannon.ShotCount);Assert.Equal(CannonShotResult.Busy,cannon.LastShot);
        }
        finally{world.Free();}
    }

    [Fact]
    public void ChamberActuallyReceivesAFallingBallBeforeFiring()
    {
        var world=World();
        try
        {
            world.Gravity=9.81f;
            var cannon=Cannon(world,rotation:new(0,0,90));
            var ball=Ball(world,cannon,PayloadId,new(2,0,0));
            world.Start();Steps(world,300);
            var local=cannon.Transform.AffineInverse()*ball.Position;
            Assert.InRange(local.X,-.215f,-.205f);
            Assert.Equal(CannonPhase.Ready,cannon.Phase);
            Trigger(world,cannon);Steps(world,30);
            Assert.Equal(1,cannon.ShotCount);Assert.True(ball.Position.Y>6);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EmptyChargedPulseIsNotRememberedForALaterPayload()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);world.Start();Steps(world,200);Trigger(world,cannon);
            Ball(world,cannon,PayloadId,Vector3.Zero);Steps(world,60);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
            Trigger(world,cannon);Assert.Equal(1,cannon.ShotCount);
        }
        finally{world.Free();}
    }

    [Fact]
    public void IncompleteChargeRejectsPulseWithoutSpendingIt()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Start();Steps(world,60);var before=cannon.StoredEnergy;Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Uncharged,cannon.LastShot);
            Assert.True(cannon.StoredEnergy>=before);Assert.Equal(0,cannon.ReleasedEnergy);
            Steps(world,200);Assert.Equal(0,cannon.ShotCount);
            Trigger(world,cannon);Assert.Equal(1,cannon.ShotCount);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameTickLoadAndRepeatedTriggerUseOneFollowingBoundary(bool loadFirst)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);world.Start();Steps(world,200);
            if(loadFirst)Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Activate(cannon);world.Activate(cannon);
            if(!loadFirst)Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Step();Assert.Equal(0,cannon.ShotCount);
            world.Step();Assert.Equal(1,cannon.ShotCount);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(CannonParameters.Capacity,0f)]
    [InlineData(CannonParameters.Capacity,101f)]
    [InlineData(CannonParameters.ChargePower,float.NaN)]
    [InlineData(CannonParameters.ChargePower,101f)]
    public void InvalidAuthorSettingsAreRejected(string parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=CannonId,Kind=CannonPart.CatalogId,
                Properties=new(){[parameter]=value}}));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(.29f)]
    [InlineData(.43f)]
    public void WrongSizePayloadRetainsChargeAndIsNotLaunched(float radius)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            var ball=world.AddPart(new(){Id=PayloadId,Kind=BallKind,Position=[0,5,0],
                Properties=new(){[RadiusParameter]=radius}});
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Unseated,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ReleasedEnergy);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(Vector3.Zero,ball.Velocity);
            Assert.Same(ball,world.FindPart(PayloadId));
        }
        finally{world.Free();}
    }

    [Fact]
    public void TwoBallsInMouthRejectAmbiguousLoadWithoutDeletingEither()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            var first=Ball(world,cannon,PayloadId,new(-.21f,0,0));
            var second=Ball(world,cannon,OtherId,new(.47f,0,0));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Ambiguous,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ShotCount);
            Assert.Equal(2,world.Bodies.Count);
            Assert.Same(first,world.FindPart(PayloadId));Assert.Same(second,world.FindPart(OtherId));
            Assert.True(first.Visible&&second.Visible);
        }
        finally{world.Free();}
    }

    [Fact]
    public void ClearingObstructionRequiresNewPulseAndUsesRetainedCharge()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);var ball=Ball(world,cannon,PayloadId,Vector3.Zero);
            var wall=world.AddPart(new(){Id=WallId,Kind=WallKind,Position=[.8f,5,0]});
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.005f,1,1)));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Obstructed,cannon.LastShot);
            wall.Position+=Vector3.Up*4; // Native obstruction fixture; not UI proof.
            Steps(world,30);Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
            Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);Assert.Equal(1,cannon.ShotCount);
            Assert.Same(ball,cannon.LastPayload);Assert.InRange(cannon.ReleasedEnergy,44.99,45);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PowerInterruptionRetainsChargeWithoutFabricatingShot(bool fullyCharged)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world,false);Ball(world,cannon,PayloadId,Vector3.Zero);
            var battery=world.AddPart(new(){Id=BatteryId,Kind=BatteryKind,Position=[-5,1,0]});
            var supplySwitch=world.AddPart(new(){Id=SwitchId,Kind=SwitchKind,Position=[-3,1,0]});
            Assert.True(world.Connect(battery,SocketId.Supply,supplySwitch,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(supplySwitch,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            world.Start();supplySwitch.Active=true;Steps(world,fullyCharged?200:60);
            var retained=cannon.StoredEnergy;
            supplySwitch.Active=false;Steps(world,60);
            Assert.Equal(retained,cannon.StoredEnergy);Assert.Equal(0,cannon.ShotCount);
            Trigger(world,cannon);
            if(fullyCharged)
            {
                Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
                Assert.Equal(1,cannon.ShotCount);Assert.InRange(cannon.StoredEnergy,0,.00001);
            }
            else
            {
                Assert.Equal(CannonShotResult.Uncharged,cannon.LastShot);
                Assert.Equal(retained,cannon.StoredEnergy);
                supplySwitch.Active=true;Steps(world,200);
                Assert.Equal(0,cannon.ShotCount);
                Trigger(world,cannon);Assert.Equal(1,cannon.ShotCount);
            }
        }
        finally{world.Free();}
    }

    [Fact]
    public void CurrentJsonConstructionReplaysIdenticalShotAndFlight()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world,rotation:new(15,25,35));
            Ball(world,cannon,PayloadId,Vector3.Zero);
            var json=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();Steps(world,200);Trigger(world,cannon);Steps(world,60);
            var signature=world.StateSignature();var energy=cannon.ReleasedEnergy;
            world.LoadMachine(JsonSerializer.Deserialize(json,MachineJson.Default.MachineData)!);
            var replay=(CannonPart)world.FindPart(CannonId)!;
            world.Start();Steps(world,200);Trigger(world,replay);Steps(world,60);
            Assert.Equal(signature,world.StateSignature());Assert.Equal(energy,replay.ReleasedEnergy);
            Assert.Equal(1,replay.ShotCount);Assert.Same(world.FindPart(PayloadId),replay.LastPayload);
        }
        finally{world.Free();}
    }

    [Fact]
    public void ReturningBallPhysicallyReloadsAndCanBeFiredAgain()
    {
        var world=World();
        try
        {
            world.Gravity=9.81f;
            var cannon=Cannon(world,rotation:new(0,0,90));
            var ball=Ball(world,cannon,PayloadId,new(2,0,0));
            world.Start();Steps(world,300);Trigger(world,cannon);
            Assert.Equal(1,cannon.ShotCount);Steps(world,30);
            Assert.True(ball.Position.Y>6);
            Steps(world,350);
            Assert.Equal(1,cannon.ShotCount);Assert.Equal(CannonPhase.Ready,cannon.Phase);
            Assert.InRange((cannon.Transform.AffineInverse()*ball.Position).X,-.215f,-.205f);
            Assert.Single(world.Bodies);Assert.Same(ball,world.FindPart(PayloadId));
            Trigger(world,cannon);Steps(world,30);
            Assert.Equal(2,cannon.ShotCount);Assert.Same(ball,cannon.LastPayload);
            Assert.True(ball.Position.Y>6);Assert.InRange(cannon.ReleasedEnergy,89.99,90.01);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RecoilSleeveMovesSmoothlySettlesAfterStopAndDoesNotAlterPhysics()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);Ball(world,cannon,PayloadId,Vector3.Zero);
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(0,cannon.RecoilOffset);
            var state=world.StateSignature();var boxes=cannon.Boxes.ToArray();var tubes=cannon.Tubes.ToArray();
            world.Running=false;
            var previous=0f;var minimum=0f;
            for(var frame=0;frame<180;frame++)
            {
                cannon._Process(1.0/240);
                Assert.InRange(cannon.RecoilOffset,-CannonPart.MaximumRecoil,0);
                Assert.InRange(Mathf.Abs(cannon.RecoilOffset-previous),0,.02f);
                previous=cannon.RecoilOffset;minimum=Mathf.Min(minimum,previous);
            }
            Assert.True(minimum<-.1f);Assert.Equal(0,cannon.RecoilOffset);
            Assert.Equal(state,world.StateSignature());
            Assert.Equal(boxes,cannon.Boxes.ToArray());Assert.Equal(tubes,cannon.Tubes.ToArray());
            world.Restore();
            Assert.Equal(0,((CannonPart)world.FindPart(CannonId)!).RecoilOffset);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RejectedShotDoesNotAnimateRecoil()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);
            for(var frame=0;frame<60;frame++)
            {
                cannon._Process(1.0/60);
                Assert.Equal(0,cannon.RecoilOffset);
            }
        }
        finally{world.Free();}
    }

    [Fact]
    public void ResetRestoresConstructionAndClearsChargeAndTrigger()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);Ball(world,cannon,PayloadId,Vector3.Zero);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();Steps(world,200);Trigger(world,cannon);world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=(CannonPart)world.FindPart(CannonId)!;
            Assert.Equal(0,restored.StoredEnergy);Assert.Equal(0,restored.ShotCount);Assert.Null(restored.LastPayload);
        }
        finally{world.Free();}
    }
}
