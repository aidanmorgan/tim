using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CannonTests(NativeSceneFixture godot)
{
    private const string CannonId="cannon",BatteryId="battery",PayloadId="payload",OtherId="other",WallId="wall";
    private const string BatteryKind="battery",BallKind="ball",WallKind="wall",SwitchKind="switch";
    private const string SwitchId="supply_switch";
    private MachineWorld World()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static CannonPart Cannon(MachineWorld world,bool powered=true,Vector3 rotation=default)
    {
        var cannon=(CannonPart)world.AddPart(new(){Id=CannonId,Kind=CannonPart.CatalogId,Position=[0,5,0],
            Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z)});
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
    private static void Steps(MachineWorld world,int count){for(var i=0;i<count;i++)world.Step();world.PresentFrame(0,1);}
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
            Assert.InRange(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(cannon.Basis.X)),9.48f,9.50f);
            Assert.InRange((world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity-SceneGeometryAdapter.CaptureVector(cannon.Basis.X)*CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(cannon.Basis.X))).Length,0,.001f);
            Assert.InRange((ball.Position-before).Length(),0,.1f); // moved by integration, not teleported to muzzle
            Assert.InRange(cannon.ReleasedEnergy,44.99,45);
            Assert.InRange(.5*ball.Mass*world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.LengthSquared,0,cannon.ReleasedEnergy+.001);
        }
        finally{world.Free();}
    }

    public enum PayloadConstraint { CoupledMass, Fixed }

    [Theory]
    [InlineData(PayloadConstraint.CoupledMass)]
    [InlineData(PayloadConstraint.Fixed)]
    public void SharedConstraintResponseDeterminesLaunchWorkAndResetReplay(PayloadConstraint constraint)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            Ball(world,cannon,PayloadId,Vector3.Zero);
            Ball(world,cannon,OtherId,new(3,0,0));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBody Configure()
            {
                var payload=world.PhysicsAssembly.Body(new(world.FindPart(PayloadId)!,MachinePart.RootBody));
                var carrier=world.PhysicsAssembly.Body(new(constraint==PayloadConstraint.Fixed?cannon:
                    world.FindPart(OtherId)!,MachinePart.RootBody));
                var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,payload,
                    new(default,RigidRotation.Identity),carrier,
                    new(carrier.Pose.InverseTransformPoint(payload.Center),RigidRotation.Identity),
                    ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
                world.Physics.ReplaceJoints([joint]);
                return payload;
            }
            world.Start();Steps(world,200);
            var payload=Configure();
            Trigger(world,cannon);
            if(constraint==PayloadConstraint.Fixed)
            {
                Assert.Equal(CannonShotResult.NoResponse,cannon.LastShot);
                Assert.Equal(0,cannon.ShotCount);Assert.Equal(0,cannon.ReleasedEnergy);
                Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(default,payload.LinearVelocity);
            }
            else
            {
                var partner=world.PhysicsAssembly.Body(new(world.FindPart(OtherId)!,MachinePart.RootBody));
                var mass=1/payload.InverseMass+1/partner.InverseMass;
                var speed=Math.Sqrt(2*45/mass);
                Assert.Equal(CannonShotResult.Fired,cannon.LastShot);Assert.Equal(1,cannon.ShotCount);
                Assert.InRange(Math.Abs(payload.LinearVelocity.X-speed),0,1e-6);
                Assert.InRange(Math.Abs(partner.LinearVelocity.X-speed),0,1e-6);
                Assert.InRange(cannon.ReleasedEnergy,44.999999,45);
                var energy=.5*payload.LinearVelocity.LengthSquared/payload.InverseMass+
                    .5*partner.LinearVelocity.LengthSquared/partner.InverseMass;
                Assert.InRange(Math.Abs(energy-cannon.ReleasedEnergy),0,1e-6);
            }
            var after=world.Physics.Capture();var spent=cannon.ReleasedEnergy;var shot=cannon.LastShot;
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            cannon=(CannonPart)world.FindPart(CannonId)!;
            world.Start();Steps(world,200);Configure();Trigger(world,cannon);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(spent,cannon.ReleasedEnergy);Assert.Equal(shot,cannon.LastShot);
        }
        finally { world.Free(); }
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
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1), MachinePart.RootBody));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
            Steps(world,30);
            Assert.Same(ball,cannon.LastPayload);
            Assert.InRange(ball.Position.X,.9f,1.66f);
            Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.X<0,"The same launched payload must rebound from the distant wall.");
            Assert.InRange(.5*ball.Mass*world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.LengthSquared,0,cannon.ReleasedEnergy);
        }
        finally{world.Free();}
    }

    [Fact]
    public void TriggerCannotChargeOrQueueAnEmptyShot()
    {
        var world=World();
        try
        {
            var cannon=Cannon(world,false);
            var ball=Ball(world,cannon,PayloadId,new(1.2f,0,0));
            ball.InitialVelocity=-cannon.Basis.X*.5f;
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var initialPosition=ball.Position;
            var initialVelocity=ball.InitialVelocity;
            world.Start();Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);Assert.Equal(0,cannon.StoredEnergy);
            // The authored payload enters the bore through physical integration.
            // An empty trigger must not remain queued until that arrival.
            Steps(world,240);
            Assert.InRange((cannon.Transform.AffineInverse()*ball.Position).X,.18f,.21f);
            Assert.Equal(CannonPhase.Charging,cannon.Phase);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(0,cannon.StoredEnergy);
            Trigger(world,cannon);Assert.Equal(CannonShotResult.Uncharged,cannon.LastShot);
            Assert.Equal(0,cannon.ShotCount);Assert.Same(ball,world.FindPart(PayloadId));
            var after=world.Physics.Capture();
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            cannon=(CannonPart)world.FindPart(CannonId)!;
            ball=world.FindPart(PayloadId)!;
            Assert.Equal(initialPosition,ball.Position);
            Assert.Equal(initialVelocity,ball.InitialVelocity);
            world.Start();Trigger(world,cannon);Steps(world,240);Trigger(world,cannon);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(CannonShotResult.Uncharged,cannon.LastShot);
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
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.005f,1,1), MachinePart.RootBody));
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Obstructed,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ShotCount);
            Assert.Equal(default(CollisionVector),world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);Assert.Same(ball,world.FindPart(PayloadId));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReloadedPhysicalBallCanFireAfterRecharge(bool arrives)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            Ball(world,cannon,PayloadId,Vector3.Zero);
            var second=Ball(world,cannon,OtherId,new(3,0,0));
            second.InitialVelocity=arrives?new(-.5f,0,0):Vector3.Zero;
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            void Exercise()
            {
                cannon=(CannonPart)world.FindPart(CannonId)!;
                var first=world.FindPart(PayloadId)!;
                second=world.FindPart(OtherId)!;
                var count=world.Parts.Count;
                world.Start();Steps(world,200);Trigger(world,cannon);Steps(world,12);
                Assert.Equal(1,cannon.ShotCount);Assert.Same(first,cannon.LastPayload);
                var departing=world.PhysicsAssembly.Body(new(first,MachinePart.RootBody));
                Assert.True(departing.Center.X>CannonPart.HalfLength+first.Radius);
                // Explicit external deflection, after barrel clearance, lets the
                // separately authored incoming ball reach the bore without a head-on impact.
                var incomingBody=world.PhysicsAssembly.Body(new(second,MachinePart.RootBody));
                var deflection=new CollisionVector(0,0,15);
                var separation=incomingBody.Center-departing.Center;
                var relativeVelocity=incomingBody.LinearVelocity-departing.LinearVelocity-deflection;
                var closestTime=Math.Max(0,-CollisionVector.Dot(separation,relativeVelocity)/relativeVelocity.LengthSquared);
                Assert.True((separation+relativeVelocity*closestTime).Length>first.Radius+second.Radius+.02);
                world.Physics.ApplyImpulse(departing.Id,deflection/departing.InverseMass,departing.Center);
                Steps(world,470);
                Assert.Equal(1,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
                var incoming=world.PhysicsAssembly.Body(new(second,MachinePart.RootBody));
                if(arrives)
                    Assert.InRange(incoming.Center.X,.14,.16);
                else
                    Assert.Equal(3,incoming.Center.X);
                Trigger(world,cannon);
                Assert.Equal(arrives?2:1,cannon.ShotCount);
                Assert.Equal(arrives?CannonShotResult.Fired:CannonShotResult.Empty,cannon.LastShot);
                Assert.Same(arrives?second:first,cannon.LastPayload);
                Assert.Equal(count,world.Parts.Count);
                Assert.Same(first,world.FindPart(PayloadId));Assert.Same(second,world.FindPart(OtherId));
                Assert.NotSame(first,second);
            }
            Exercise();
            var after=world.Physics.Capture();var released=cannon.ReleasedEnergy;
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Exercise();
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(released,cannon.ReleasedEnergy);
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
            var cannon=Cannon(world);
            var ball=Ball(world,cannon,PayloadId,new(1.8f,0,0));
            ball.InitialVelocity=-cannon.Basis.X*.25f;
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);
            Steps(world,580);
            Assert.InRange((cannon.Transform.AffineInverse()*ball.Position).X,.16f,.18f);
            Assert.Equal(CannonPhase.Ready,cannon.Phase);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
            Assert.Equal(CannonShotResult.Empty,cannon.LastShot);
            Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
            Assert.Equal(1,cannon.ShotCount);Assert.Same(ball,cannon.LastPayload);
            Assert.InRange(cannon.ReleasedEnergy,44.99,45);
            var after=world.Physics.Capture();
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            cannon=(CannonPart)world.FindPart(CannonId)!;
            world.Start();Steps(world,200);Trigger(world,cannon);Steps(world,580);Trigger(world,cannon);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(1,cannon.ShotCount);
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

    public enum PayloadArrival { BeforePulse, FollowingBoundary, AfterBoundary }

    [Theory]
    [InlineData(PayloadArrival.BeforePulse)]
    [InlineData(PayloadArrival.FollowingBoundary)]
    [InlineData(PayloadArrival.AfterBoundary)]
    public void PhysicalLoadAndRepeatedTriggerUseOneFollowingBoundary(PayloadArrival arrival)
    {
        var world=World();
        try
        {
            var cannon=Cannon(world);
            var start=arrival switch
            {
                PayloadArrival.BeforePulse=>0f,
                PayloadArrival.FollowingBoundary=>1.048f,
                PayloadArrival.AfterBoundary=>1.052f,
                _=>throw new System.ArgumentOutOfRangeException(nameof(arrival))
            };
            var ball=Ball(world,cannon,PayloadId,new(start,0,0));
            if(arrival!=PayloadArrival.BeforePulse) ball.InitialVelocity=-cannon.Basis.X*.5f;
            world.Start();Steps(world,200);
            var local=(cannon.Transform.AffineInverse()*ball.Position).X;
            var seatedLimit=CannonPart.HalfLength+.002f-ball.Radius;
            Assert.Equal(arrival==PayloadArrival.BeforePulse,local<=seatedLimit);
            world.Activate(cannon);world.Activate(cannon);
            world.Step();world.PresentFrame(0,1);Assert.Equal(0,cannon.ShotCount);
            local=(cannon.Transform.AffineInverse()*ball.Position).X;
            Assert.Equal(arrival!=PayloadArrival.AfterBoundary,local<=seatedLimit);
            world.Step();
            if(arrival==PayloadArrival.AfterBoundary)
            {
                Assert.Equal(CannonShotResult.Unseated,cannon.LastShot);
                Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
                Steps(world,2);
                Assert.Equal(0,cannon.ShotCount);
                Trigger(world,cannon);
            }
            Assert.Equal(1,cannon.ShotCount);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);
            Assert.Same(ball,cannon.LastPayload);
            Assert.InRange(cannon.ReleasedEnergy,44.99,45);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(CannonParameter.Capacity,0f)]
    [InlineData(CannonParameter.Capacity,101f)]
    [InlineData(CannonParameter.ChargePower,float.NaN)]
    [InlineData(CannonParameter.ChargePower,101f)]
    public void InvalidAuthorSettingsAreRejected(CannonParameter parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=CannonId,Kind=CannonPart.CatalogId,
                Properties=new(){[PartParameterName.Of(parameter)]=value}}));
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
                Properties=new(){[PartParameterName.Of(BallParameter.Radius)]=radius}});
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Unseated,cannon.LastShot);
            Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ReleasedEnergy);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(default(CollisionVector),world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);
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
            // Explicit catalogue/instance serialization boundary; runtime uses the typed part.
            var gate=(PoweredGatePart)world.AddPart(new(){Id="muzzle_gate",Kind="powered_gate",Position=[1.06f,5,0]});
            var supply=new SupplyControl(world,world.FindPart(BatteryId)!);
            Assert.True(world.Connect(supply.Output,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var links=world.Connections.ToArray();
            world.Start();Steps(world,200);Trigger(world,cannon);
            Assert.Equal(GateState.Closed,gate.State);
            Assert.Equal(CannonShotResult.Obstructed,cannon.LastShot);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
            Assert.Equal(default(CollisionVector),world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);
            var blade=world.PhysicsAssembly.Body(new(gate,PoweredGatePart.BladeBody));
            var closed=blade.Center;
            supply.SetAndSettle(SimulationLatchPhase.On);Steps(world,180);
            // Clearance is the integration contract here; exact endpoint convergence
            // remains covered separately by PoweredGateTests.
            Assert.True(gate.State is GateState.Opening or GateState.Open);
            Assert.InRange(gate.Opening,1.1f,PoweredGatePart.Stroke);
            Assert.InRange(blade.Center.Y-closed.Y,1.1,PoweredGatePart.Stroke+.00001);
            Assert.Equal(0,cannon.ShotCount);Assert.Equal(45,cannon.StoredEnergy);
            Assert.Equal(CannonShotResult.Obstructed,cannon.LastShot);
            Assert.Equal(links,world.Connections.ToArray());
            Trigger(world,cannon);
            Assert.Equal(CannonShotResult.Fired,cannon.LastShot);Assert.Equal(1,cannon.ShotCount);
            Assert.Same(ball,cannon.LastPayload);Assert.InRange(cannon.ReleasedEnergy,44.99,45);
            Steps(world,40);
            Assert.True(ball.Position.X>gate.Position.X+.5f);
            var after=world.Physics.Capture();
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            cannon=(CannonPart)world.FindPart(CannonId)!;
            gate=Assert.Single(world.Parts.OfType<PoweredGatePart>());
            Assert.Equal(GateState.Closed,gate.State);
            world.Start();Steps(world,200);Trigger(world,cannon);
            supply.SetAndSettle(SimulationLatchPhase.On);Steps(world,180);Trigger(world,cannon);Steps(world,40);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(1,cannon.ShotCount);
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
                world.PresentFrame(1.0/240,1);
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
                world.PresentFrame(1.0/60,1);
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
