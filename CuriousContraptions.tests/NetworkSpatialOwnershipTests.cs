using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class NetworkSpatialOwnershipTests(NativeSceneFixture godot)
{

    public enum BasketMotion { Translation, Rotation }
    [Theory]
    [InlineData(BasketMotion.Translation,true)]
    [InlineData(BasketMotion.Translation,false)]
    [InlineData(BasketMotion.Rotation,true)]
    [InlineData(BasketMotion.Rotation,false)]
    public void MovingBasketCapturesOnlyPayloadsSettledRelativeToItsFrame(BasketMotion motion,bool comoving)
    {
        if(!Enum.IsDefined(motion))throw new ArgumentOutOfRangeException(nameof(motion));
        var world=World();
        try
        {
            List<PartDifficulty> difficulty=[new(){Precision=0,PositionWindow=1,RotationWindow=30,
                MaxPositionCorrection=.3f,MaxRotationCorrection=25,BlendSeconds=.4f,CaptureSpeed=.05f}];
            world.LoadMachine(new(){Gravity=0,Pressure=0,
                Parts=[new(){Id=Wire(Fixture.Basket),Kind=Wire(Fixture.Basket),Position=[0,6,0],Difficulty=difficulty},
                    new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[.2f,6,0]}],
                PlacementTargets=[new(){Id=Wire(Fixture.Basket),Kind=Wire(Fixture.Basket),
                    Position=motion==BasketMotion.Translation?[.2f,6,0]:[0,6,0],
                    Orientation = motion==BasketMotion.Rotation?PartOrientation.FromEulerDegrees(0,0,20):PartOrientation.FromEulerDegrees(0,0,0),Difficulty=difficulty}]});
            world.Precision=0;
            // Derive an authored ballistic initial condition that reaches the
            // moving basket at mid-blend. No live body state is overwritten.
            const double duration=.2;
            world.Start();
            var previewBasket=Assert.Single(world.Parts.OfType<BasketPart>());
            var previewFrame=world.PhysicsAssembly.Body(new(previewBasket,MachinePart.RootBody));
            var cursor=Assert.IsType<PrescribedBodyMotion>(previewFrame.PrescribedMotion).Advance(duration);
            var targetPose=cursor.At(0);
            var targetCenter=targetPose.TransformPoint(new(.2,0,0));
            var targetVelocity=cursor.LinearVelocityAt(0)+CollisionVector.Cross(
                cursor.AngularVelocityAt(0),targetCenter-targetPose.Center);
            Assert.True(targetVelocity.Length>.05);
            var initialVelocity=comoving?targetVelocity:default;
            var initialCenter=targetCenter-initialVelocity*duration;
            world.Restore();
            var authored=world.FindPart(Wire(Fixture.Ball))!;
            authored.Position=new((float)initialCenter.X,(float)initialCenter.Y,(float)initialCenter.Z);
            authored.InitialVelocity=new((float)initialVelocity.X,(float)initialVelocity.Y,(float)initialVelocity.Z);
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBodySnapshot[]? previous=null;
            for(var replay=0;replay<2;replay++)
            {
                var basket=Assert.Single(world.Parts.OfType<BasketPart>());
                var ball=world.FindPart(Wire(Fixture.Ball))!;
                world.Start();
                var frame=world.PhysicsAssembly.Body(new(basket,MachinePart.RootBody));
                var payload=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
                Assert.Equal(PhysicsMotionType.Kinematic,frame.MotionType);
                world.Physics.Step([],[],duration);
                Assert.Equal(duration,world.Physics.Time);
                Assert.InRange((frame.Center-targetPose.Center).Length,0,1e-10);
                Assert.InRange((payload.Center-targetCenter).Length,0,1e-6);
                Assert.InRange((payload.LinearVelocity-initialVelocity).Length,0,1e-6);
                var states=world.Physics.Capture().BodyStates.ToArray();
                if(previous is not null)
                {
                    Assert.Equal(previous.Length,states.Length);
                    for(var i=0;i<states.Length;i++)
                    {
                        Assert.Equal(previous[i] with {PrescribedMotion=null},states[i] with {PrescribedMotion=null});
                        var before=previous[i].PrescribedMotion;var after=states[i].PrescribedMotion;
                        if(before is null){Assert.Null(after);continue;}
                        Assert.NotNull(after);
                        // Reset rebuilds path objects. Compare all defining values,
                        // not reference identity of a freshly authored trajectory.
                        Assert.Equal((before.Time,before.LocalPose,before.Path.StartPose,before.Path.Translation,
                            before.Path.RotationVector,before.Path.Duration),
                            (after.Time,after.LocalPose,after.Path.StartPose,after.Path.Translation,
                            after.Path.RotationVector,after.Path.Duration));
                    }
                }
                previous=states;
                DistortPresentation(basket,ball);
                basket.ObservePhysics(world,1);
                // Observation cannot invent a second of physical residence.
                Assert.False(basket.Active);
                Assert.False(world.Events.ContainsKey(new(MachineEventKind.Captured,basket.Uid,ball.Uid)));
                var residence=Assert.Single(world.Physics.ResidenceStates.ToArray());
                Assert.Equal(comoving?PhysicsResidencePhase.Dwelling:PhysicsResidencePhase.Outside,residence.Phase);
                Assert.InRange(residence.Elapsed,0,duration);
                world.Restore();
                Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            }
        }
        finally {world.Free();}
    }
    private enum Fixture { Torch, Solar, Laser, Mirror, Receiver, Fan, Windmill, Chimes, Speaker, Meter, Battery, Basket, Ball, Cannon, Detector, Combiner, Nor }
    private static string Wire(Fixture fixture)=>fixture switch
    {
        Fixture.Torch=>"flashlight",Fixture.Solar=>"solar_panel",Fixture.Laser=>"laser",
        Fixture.Mirror=>"mirror",Fixture.Receiver=>"light_receiver",Fixture.Fan=>"fan",
        Fixture.Windmill=>"windmill",Fixture.Chimes=>"wind_chimes",Fixture.Speaker=>"speaker",
        Fixture.Meter=>"sound_meter",Fixture.Battery=>"battery",
        Fixture.Basket=>"basket",Fixture.Ball=>"ball",Fixture.Cannon=>"cannon",Fixture.Detector=>"ball_detector",Fixture.Combiner=>"beam_combiner",Fixture.Nor=>"optical_nor",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=1};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 at)
    {
        var wire=Wire(fixture);
        return world.AddPart(new(){Id=wire,Kind=wire,Position=[at.X,at.Y,at.Z]});
    }
    private static void DistortPresentation(params MachinePart[] parts)
    {
        foreach(var part in parts)
        {
            part.Position+=new Vector3(10,3,7);
            part.Rotation=new(.4f,.7f,1.1f);
            part.Visible=false;
        }
    }
    private static void Disable(MachineWorld world,MachinePart part,BodySlot slot)
    {
        var body=world.PhysicsAssembly.Body(new(part,slot));
        var declaration=world.Physics.Collider(body.Id).Declaration;
        world.Physics.ApplyColliderUpdates([new(body.Id,declaration.Geometry,declaration.Material,CollisionParticipation.Disabled)]);
    }

    [Fact]
    public void ConeLightUsesSolvedEmitterAndReceiverPosesAndParticipation()
    {
        var world=World();
        try
        {
            var torch=Add(world,Fixture.Torch,new(-3,6,0));
            var solar=(SolarPanelPart)Add(world,Fixture.Solar,new(1,6,0));
            torch.Active=true;world.Start();
            LightNetwork.Solve(world);
            var expected=solar.Irradiance;
            Assert.True(expected>0);
            var snapshot=world.Physics.Capture();
            DistortPresentation(torch,solar);
            LightNetwork.Solve(world);
            Assert.Equal(expected,solar.Irradiance);
            Disable(world,solar,MachinePart.RootBody);
            LightNetwork.Solve(world);
            Assert.Equal(0,solar.Irradiance);
            world.Physics.Restore(snapshot);
            Disable(world,torch,MachinePart.RootBody);
            LightNetwork.Solve(world);
            Assert.Equal(0,solar.Irradiance);
            world.Physics.Restore(snapshot);
            LightNetwork.Solve(world);
            Assert.Equal(expected,solar.Irradiance);
        }
        finally {world.Free();}
    }

    [Fact]
    public void ReflectedOpticsUsesSolvedSourceApertureAndReceiverPoses()
    {
        var world=World();
        try
        {
            var mirror=Add(world,Fixture.Mirror,new(0,6,0));
            mirror.RotationDegrees=new(0,0,45);
            var point=mirror.Transform*mirror.OpticalSurfaces.Single().Aperture.At;
            var laser=Add(world,Fixture.Laser,point-Vector3.Right*3);
            var receiver=Add(world,Fixture.Receiver,point+Vector3.Down*3);
            receiver.RotationDegrees=new(0,0,-90);
            var source=laser.OpticalPreviewSource!.Value;
            world.Start();
            var first=OpticalNetwork.Trace(world,laser,source);
            Assert.Same(receiver,Assert.Single(first.Receptions).Receiver);
            Assert.Equal(2,first.Segments.Count);
            var saved=world.Physics.Capture();
            DistortPresentation(mirror,laser,receiver);
            var stale=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(first.Segments,stale.Segments);
            Assert.Equal(first.Receptions,stale.Receptions);
            Disable(world,mirror,MachinePart.RootBody);
            Assert.Empty(OpticalNetwork.Trace(world,laser,source).Receptions);
            world.Physics.Restore(saved);
            Disable(world,laser,MachinePart.RootBody);
            Assert.Empty(OpticalNetwork.Trace(world,laser,source).Segments);
            world.Physics.Restore(saved);
            Disable(world,receiver,MachinePart.RootBody);
            Assert.Empty(OpticalNetwork.Trace(world,laser,source).Receptions);
            world.Physics.Restore(saved);
            Assert.Equal(first.Segments,OpticalNetwork.Trace(world,laser,source).Segments);
        }
        finally {world.Free();}
    }

    private partial class AirSampleBody : MachinePart
    {
        public AirflowResponse Response { get; init; }
        public Vector3 ObservedForce { get; private set; }
        public override BodyEnvelope CollisionEnvelope=>BodyEnvelope.Sphere;
        public override BodyDynamics InitialBodyDynamics=>BodyDynamics.SolidSphere(1,Radius,default,default);
        public override IReadOnlyList<AirflowSample> AirflowSamples=>
            [new(new(0,.2f,0),1,RootBody,Response,AirflowReceiver,null)];
        protected override void Build() { Dynamic=true;Drag=0; }
        public override void ObservePhysics(MachineWorld world,float delta)=>ObservedForce=AirflowNetwork.ReadReceiverForce(world,this,delta);
    }

    [Fact]
    public void AirSamplesApplyOwnedPointForcesWithoutPartSpecificIntegration()
    {
        PhysicsBodySnapshot[]? expected=null;
        foreach(var distorted in new[]{false,true})
        {
            var world=World();
            try
            {
                var fan=Add(world,Fixture.Fan,new(-3,6,0));fan.Active=true;
                var receiver=new AirSampleBody {Response=AirflowResponse.BodyForce,Position=new(0,6,0)};
                FixtureParts.Attach(world,receiver,FixturePartId.First);
                world.Start();
                var body=world.PhysicsAssembly.Body(new(receiver,MachinePart.RootBody));
                if(distorted)DistortPresentation(receiver,fan);
                world.Step();
                Assert.True(receiver.ObservedForce.X>0);
                Assert.True(body.LinearVelocity.X>0);
                Assert.True(body.AngularMomentum.Z<0);
                Assert.InRange(Math.Abs(body.AngularMomentum.Z+.2*body.LinearVelocity.X),0,1e-8);
                var actual=world.Physics.Capture().BodyStates.ToArray();
                if(expected is null)expected=actual;else Assert.Equal(expected,actual);
            }
            finally {world.Free();}
        }
    }

    [Theory]
    [InlineData((AirflowResponse)99)]
    [InlineData(AirflowResponse.Rotary)]
    public void UndefinedOrUnboundAirflowResponseIsRejectedBeforeApplyingLoads(AirflowResponse response)
    {
        var world=World();
        try
        {
            var fan=Add(world,Fixture.Fan,new(-3,6,0));fan.Active=true;
            var receiver=new AirSampleBody {Response=response,Position=new(0,6,0)};
            FixtureParts.Attach(world,receiver,FixturePartId.First);
            world.Start();
            var before=world.Physics.Capture();
            Assert.Throws<ArgumentException>(()=>AirflowNetwork.Step(world));
            Assert.Equal(before.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(Vector3.Zero,receiver.ObservedForce);
        }
        finally {world.Free();}
    }

    [Fact]
    public void WindmillForceAndAxisIgnoreRenderedTransforms()
    {
        float? expected=null;
        foreach(var distorted in new[]{false,true})
        {
            var world=World();
            try
            {
                var fan=Add(world,Fixture.Fan,new(-4,6,0));fan.Active=true;
                var mill=(WindmillPart)Add(world,Fixture.Windmill,new(-1,6,0));
                world.Start();
                if(distorted)DistortPresentation(fan,mill);
                world.Step();
                var force=mill.AxialForce;
                Assert.True(force>0);
                if(expected is null)expected=force;else Assert.Equal(expected.Value,force);
                Disable(world,fan,MachinePart.RootBody);
                world.Step();
                Assert.Equal(0,mill.AxialForce);
            }
            finally {world.Free();}
        }
    }

    [Fact]
    public void ChimeAirSampleIsAnchoredToSolvedPendulumRatherThanRenderedSail()
    {
        Vector3? expected=null;
        foreach(var distorted in new[]{false,true})
        {
            var world=World();
            try
            {
                var chimes=(WindChimesPart)Add(world,Fixture.Chimes,new(1,6,0));
                var sample=Assert.Single(chimes.AirflowSamples);
                Assert.Same(WindChimesPart.PendulumBody,sample.Body);
                Assert.Equal(ChimeAssembly.SailFromCenter,sample.At);
                var sail=chimes.SailPosition;
                var fan=Add(world,Fixture.Fan,sail-Vector3.Right*3);fan.Active=true;
                world.Start();
                if(distorted)DistortPresentation(chimes,fan);
                world.Step();
                var force=chimes.LastAirForce;
                Assert.True(force.Length()>0);
                if(expected is null)expected=force;else Assert.Equal(expected.Value,force);
                var body=world.PhysicsAssembly.Body(new(chimes,WindChimesPart.PendulumBody));
                var pose=WorldGeometry.CaptureSpatialState(world,new(chimes,sample.Body)).Pose;
                Assert.Equal(body.Pose,pose);
                Disable(world,chimes,WindChimesPart.PendulumBody);
                world.Step();
                Assert.Equal(Vector3.Zero,chimes.LastAirForce);
                world.Restore();
                chimes=(WindChimesPart)world.FindPart(Wire(Fixture.Chimes))!;
                fan=world.FindPart(Wire(Fixture.Fan))!;fan.Active=true;
                world.Start();world.Step();
                Assert.Equal(force,chimes.LastAirForce);
            }
            finally {world.Free();}
        }
    }

    [Fact]
    public void SpeakerEmissionAndSoundReceptionIgnoreRenderedTransforms()
    {
        var world=World();
        try
        {
            var speaker=(SpeakerPart)Add(world,Fixture.Speaker,new(-3,6,0));
            var meter=(SoundMeterPart)Add(world,Fixture.Meter,new(1,6,0));
            var battery=Add(world,Fixture.Battery,new(-4,2,3));
            Assert.True(world.Connect(battery,speaker));
            world.Start();world.Activate(speaker);world.Step();
            var spatial=WorldGeometry.CaptureSpatialState(world,new(speaker,MachinePart.RootBody));
            DistortPresentation(speaker,meter);
            new ElectricalNetwork(world).Solve();
            speaker.PreparePhysics(world,MachineWorld.Tick/4);
            var pulse=Assert.Single(speaker.AcousticPulses);
            Assert.Equal(spatial.Pose.ToScene()*SpeakerPart.Mouth,pulse.Origin);
            Assert.Equal(spatial.Pose.ToScene().Basis.X,pulse.Direction);
            // Advance the simulation clock until the immutable emitted pulse reaches the meter.
            for(var i=0;i<40 && meter.Level==0;i++)world.Step();
            Assert.True(meter.Level>0);
            var expected=meter.Level;
            DistortPresentation(speaker,meter);
            AcousticNetwork.Solve(world);
            var point=WorldGeometry.CaptureSpatialState(world,new(meter,MachinePart.RootBody)).Pose.ToScene().Origin;
            Assert.Equal(pulse.Sample(point,world.Ticks),meter.Level);
            Assert.True(expected>0);
            Disable(world,meter,MachinePart.RootBody);
            AcousticNetwork.Solve(world);
            Assert.Equal(0,meter.Level);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BasketCaptureUsesSolvedPoseVelocityAndParticipation(bool enabled)
    {
        var world=World();
        try
        {
            var basket=(BasketPart)Add(world,Fixture.Basket,new(0,6,0));
            var ball=Add(world,Fixture.Ball,new(0,6,0));
            world.Start();
            DistortPresentation(basket,ball);
            Assert.Throws<InvalidOperationException>(()=>ball.InitialVelocity=new(20,0,0));
            if(!enabled) Disable(world,ball,MachinePart.RootBody);
            basket.ObservePhysics(world,1);
            Assert.False(basket.Active);
            world.Physics.Step([],[],1);
            basket.ObservePhysics(world,1);
            Assert.Equal(enabled,world.Events.ContainsKey(new(MachineEventKind.Captured,basket.Uid,ball.Uid)));
            Assert.Equal(enabled,basket.Active);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CannonUsesSolvedChamberAndLaunchAxisDespiteDistortedPresentation(bool enabled)
    {
        var world=World();world.Pressure=0;
        try
        {
            var cannon=(CannonPart)Add(world,Fixture.Cannon,new(0,6,0));
            cannon.RotationDegrees=new(30,45,60);
            var ball=Add(world,Fixture.Ball,new(0,6,0));
            var battery=Add(world,Fixture.Battery,new(-5,2,0));
            Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            world.Start();
            for(var i=0;i<200;i++)world.Step();
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var center=solved.Pose.Center;
            var axis=WorldGeometry.CaptureSpatialState(world,new(cannon,MachinePart.RootBody)).Pose.Rotation.Apply(new(1,0,0));
            world.Activate(cannon);world.Step();
            DistortPresentation(cannon,ball);
            Assert.Throws<InvalidOperationException>(()=>ball.InitialVelocity=new(20,0,0));
            if(!enabled) Disable(world,ball,MachinePart.RootBody);
            world.Step(); // Firing commits its occurrence inside the owned tick lifecycle.
            Assert.Equal(enabled?CannonShotResult.Fired:CannonShotResult.Empty,cannon.LastShot);
            Assert.Equal(enabled?1:0,cannon.ShotCount);
            if(enabled)
                Assert.InRange((solved.Pose.Center-center-solved.LinearVelocity*MachineWorld.Tick).Length,0,1e-10);
            else Assert.Equal(center,solved.Pose.Center);
            if(enabled)
            {
                Assert.Same(ball,cannon.LastPayload);
                Assert.InRange(CollisionVector.Dot(solved.LinearVelocity,axis),9.48,9.50);
                Assert.InRange(CollisionVector.Cross(solved.LinearVelocity,axis).Length,0,1e-10);
                Assert.InRange(.5*solved.LinearVelocity.LengthSquared/solved.InverseMass,0,cannon.ReleasedEnergy+1e-6);
            }
            else Assert.Equal(0,solved.LinearVelocity.Length);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DetectorCrossingUsesSolvedPositionsAndParticipation(bool enabled)
    {
        var world=World();
        try
        {
            var detector=(BallDetectorPart)Add(world,Fixture.Detector,new(0,6,0));
            detector.RotationDegrees=new(20,30,40);
            var at=detector.Transform*new Vector3(-1,0,0);
            var ball=Add(world,Fixture.Ball,at);
            ball.InitialVelocity=detector.Basis.X*10;
            world.Start();
            var frame=WorldGeometry.CaptureSpatialState(world,new(detector,MachinePart.RootBody)).Pose;
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            DistortPresentation(detector,ball);
            detector.PreparePhysics(world,MachineWorld.Tick);
            if(!enabled) Disable(world,ball,MachinePart.RootBody);
            world.Physics.Step([],[],.2);
            Assert.InRange((frame.InverseTransformPoint(solved.Center)-new CollisionVector(1,0,0)).Length,0,1e-6);
            Assert.Equal(.2,world.Physics.Time);
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(enabled?1:0,detector.CrossingCount);
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(enabled?1:0,detector.CrossingCount);
            var detectorId=world.PhysicsAssembly.Body(new(detector,MachinePart.RootBody)).Id;
            var declaration=world.Physics.Collider(detectorId).Declaration;
            var events=world.Events.ToArray();
            Disable(world,detector,MachinePart.RootBody);
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.False(detector.Active);
            Assert.Equal(0,detector.Pulse);
            world.Physics.ApplyColliderUpdates([declaration]);
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.False(detector.Active);
            Assert.Equal(events,world.Events.ToArray());
            Assert.Equal(enabled?1:0,detector.CrossingCount);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EscapeUsesSolvedBoundsEvenWhenRenderingIsHidden(bool enabled)
    {
        var world=World();world.Pressure=0;
        try
        {
            var ball=Add(world,Fixture.Ball,new(19,6,0));
            world.Start();
            ball.Position=new(0,6,0);ball.Visible=false;
            if(!enabled) Disable(world,ball,MachinePart.RootBody);
            world.Step();
            Assert.Equal(enabled,world.Events.ContainsKey(new(MachineEventKind.Escaped,ball.Uid)));
            Assert.False(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
            world.Restore();
            var restored=Assert.Single(world.Parts);
            Assert.Equal(new Vector3(19,6,0),restored.Position);
            Assert.True(restored.Visible);
            Assert.Empty(world.Events);
        }
        finally {world.Free();}
    }

    public enum Router { Combiner, Nor }
    [Theory]
    [InlineData(Router.Combiner)]
    [InlineData(Router.Nor)]
    public void RoutedOutputAccountingUsesCapturedOutletNotRenderedPose(Router kind)
    {
        var world=World();world.Pressure=0;
        try
        {
            var fixture=kind switch {Router.Combiner=>Fixture.Combiner,Router.Nor=>Fixture.Nor,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
            var router=Add(world,fixture,new(0,6,0));
            var laser=Add(world,Fixture.Laser,new(0,6,4));laser.RotationDegrees=new(0,90,0);
            var battery=Add(world,Fixture.Battery,new(-4,2,0));
            Assert.True(world.Connect(battery,SocketId.Supply,laser,SocketId.PowerIn,ConnectionDomain.Electrical));
            world.Start();world.Activate(laser);new ElectricalNetwork(world).Solve();router.BeforeNetworks(world);OpticalNetwork.Solve(world);
            Vector3 Output()=>kind switch
            {
                Router.Combiner=>((BeamCombinerPart)router).OutputPower,
                Router.Nor=>((OpticalLogicPart)router).OutputPower,
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            var expected=Output();Assert.True(expected.LengthSquared()>0);
            DistortPresentation(router,laser);
            OpticalNetwork.Solve(world);
            Assert.Equal(expected,Output());Assert.True(router.Active);
            var saved=world.Physics.Capture();
            Disable(world,router,MachinePart.RootBody);
            OpticalNetwork.Solve(world);
            Assert.Equal(Vector3.Zero,Output());Assert.False(router.Active);
            world.Physics.Restore(saved);
            OpticalNetwork.Solve(world);Assert.Equal(expected,Output());
        }
        finally {world.Free();}
    }

    [Fact]
    public void ForeignAndMissingBodyAnchorsAreRejected()
    {
        var world=World();var foreign=new MachinePart();
        try
        {
            var torch=Add(world,Fixture.Torch,new(0,6,0));
            Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureSpatialState(world,new(foreign,MachinePart.RootBody)));
            world.Start();
            Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureSpatialState(world,new(torch,WindChimesPart.PendulumBody)));
            Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureSpatialState(world,new(null,Workbench.Body)));
        }
        finally {foreign.Free();world.Free();}
    }

    [Fact]
    public void FixtureMappingRejectsUnknownValues()
    {
        Assert.Equal("flashlight",Wire(Fixture.Torch));
        Assert.Equal("wind_chimes",Wire(Fixture.Chimes));
        Assert.Equal("sound_meter",Wire(Fixture.Meter));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)999));
    }
}
