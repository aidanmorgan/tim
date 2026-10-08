using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WindChimeTests(NativeSceneFixture godot)
{
    private enum Role { Fan, Chimes, Meter, Counter, Battery, Wall, Ball, Pipe, Shell, Light, Heavy, Left, Right, Invalid }
    public enum ShellKind { Bend45, Bend90, Funnel }
    private enum Parameter { Mass, Powered, Force, Reach, Width }
    private static string Id(Role role)=>role switch
    {
        Role.Fan=>"fan",Role.Chimes=>"chimes",Role.Meter=>"meter",Role.Counter=>"counter",
        Role.Battery=>"battery",Role.Wall=>"wall",Role.Ball=>"ball",Role.Pipe=>"pipe",
        Role.Shell=>"shell",Role.Light=>"light",Role.Heavy=>"heavy",Role.Left=>"left",
        Role.Right=>"right",Role.Invalid=>"invalid",_=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Fan or Role.Invalid=>"fan",Role.Chimes=>"wind_chimes",Role.Meter=>"sound_meter",
        Role.Counter=>"counter",Role.Battery=>"battery",Role.Wall=>"wall",
        Role.Ball or Role.Light=>"ball",Role.Heavy=>"bowling",Role.Pipe=>"pipe",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(ShellKind kind)=>kind switch
    {
        ShellKind.Bend45=>"pipe_bend_45",ShellKind.Bend90=>"pipe_bend_90",ShellKind.Funnel=>"funnel",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static PhysicsBody Pendulum(MachineWorld world,WindChimesPart chimes)=>
        world.PhysicsAssembly.Body(new(chimes,WindChimesPart.PendulumBody));
    private static CollisionVector Tip(MachineWorld world,WindChimesPart chimes)=>
        Pendulum(world,chimes).Pose.TransformPoint(SceneGeometryAdapter.CaptureVector(ChimeAssembly.SailFromCenter));
    private static CollisionVector Offset(MachineWorld world,WindChimesPart chimes)=>
        Tip(world,chimes)-SceneGeometryAdapter.CaptureRigidPose(chimes.Transform).TransformPoint(SceneGeometryAdapter.CaptureVector(ChimeAssembly.Pivot));
    private static CollisionVector Speed(MachineWorld world,WindChimesPart chimes)=>
        Pendulum(world,chimes).PointVelocity(Tip(world,chimes));
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(9f,0f,false,true,false,true)]
    [InlineData(9f,0f,false,true,true,true)]
    [InlineData(.01f,0f,false,true,false,false)]
    [InlineData(0f,0f,false,true,false,false)]
    [InlineData(9f,2f,false,true,false,false)]
    [InlineData(9f,0f,true,true,false,false)]
    [InlineData(9f,0f,false,false,false,false)]
    public void AirMustMoveClapperIntoTubeBeforeSoundCanDriveMeter(float force,float depth,bool wall,bool fanOn,bool reverse,bool expected)
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id=Id(Role.Fan),Kind=Kind(Role.Fan),Position=[-4,5.4f,depth],Properties=new(){[PartParameterName.Of(Parameter.Force)]=force,[PartParameterName.Of(Parameter.Powered)]=fanOn?1:0}});
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(reverse?Role.Meter:Role.Chimes),Kind=Kind(Role.Chimes),Position=[-1,6,0]});
            var meter=(SoundMeterPart)world.AddPart(new(){Id=Id(reverse?Role.Chimes:Role.Meter),Kind=Kind(Role.Meter),Position=[2,6,0]});
            var counter=(CounterPart)world.AddPart(new(){Id=Id(Role.Counter),Kind=Kind(Role.Counter),Position=[3,2,2]});
            var battery=world.AddPart(new(){Id=Id(Role.Battery),Kind=Kind(Role.Battery),Position=[-4,2,3]});
            Assert.True(world.Connect(battery,meter));
            Assert.True(world.Connect(meter,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            if(wall)world.AddPart(new(){Id=Id(Role.Wall),Kind=Kind(Role.Wall),Position=[-2.5f,5.4f,0],Orientation = PartOrientation.FromEulerDegrees(0,90,0)});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            world.Step();Assert.Equal(0,chimes.PulseCount); // overlap is not a trigger
            var pendulum=world.PhysicsAssembly.Body(new(chimes,WindChimesPart.PendulumBody));
            var drag=Assert.Single(world.Physics.Loads.Drag.ToArray(),load=>load.Body==pendulum.Id);
            Assert.Equal(ChimeAssembly.Damping,drag.LinearRate);Assert.Equal(ChimeAssembly.Damping,drag.AngularRate);
            var moved=0d;var maxSpeed=0d;
            for(var i=1;i<360;i++)
            {
                world.Step();
                moved=Math.Max(moved,(Offset(world,chimes)-new CollisionVector(0,-ChimeAssembly.Length,0)).Length);
                maxSpeed=Math.Max(maxSpeed,Speed(world,chimes).Length);
                Assert.InRange(Offset(world,chimes).Length,1.4999f,1.5001f);
                Assert.InRange(chimes.AcousticPulses.Count,0,5);
            }
            Assert.Equal(expected,chimes.PulseCount>0);
            Assert.Equal(expected,meter.TriggerCount>0);Assert.Equal(meter.TriggerCount,counter.Count);
            Assert.InRange(maxSpeed,0,20);
            if(force>0&&depth==0&&!wall&&fanOn)Assert.True(moved>0);
            if(expected)Assert.True(moved>.35f);
            var count=chimes.PulseCount;
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            chimes=(WindChimesPart)world.FindPart(chimes.Uid)!;
            Assert.Equal(0,chimes.PulseCount);Assert.Empty(chimes.AcousticPulses);
            Assert.Equal(chimes.Transform*new Transform3D(Basis.Identity,ChimeAssembly.Pivot),chimes.PendulumTransform);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<360;i++)world.Step();
            Assert.Equal(count,((WindChimesPart)world.FindPart(chimes.Uid)!).PulseCount);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void RotatedFanAndChimeWorkThroughDepthWithoutRenderDrivenPhysics(float yaw)
    {
        var world=World();
        try
        {
            var basis=Basis.FromEuler(new(0,Mathf.DegToRad(yaw),0));
            var center=new Vector3(0,6,0);var fanPosition=center+basis*new Vector3(-3,-.6f,0);
            world.AddPart(new(){Id=Id(Role.Fan),Kind=Kind(Role.Fan),Position=[fanPosition.X,fanPosition.Y,fanPosition.Z],Orientation = PartOrientation.FromEulerDegrees(0,yaw,0)});
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(0,yaw,0)});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            var state=world.Physics.Capture().BodyStates.ToArray();var count=chimes.PulseCount;
            world.PresentFrame(.016,1);
            world.PresentFrame(.016,1);world.PresentFrame(.2,1);
            Assert.Equal(state,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(count,chimes.PulseCount);
            Assert.NotEqual(Quaternion.Identity,(chimes.Transform.AffineInverse()*chimes.PendulumTransform).Basis.GetRotationQuaternion());
            world.Restore();chimes=(WindChimesPart)world.FindPart(Id(Role.Chimes))!;
            Assert.Equal(chimes.Transform*new Transform3D(Basis.Identity,ChimeAssembly.Pivot),chimes.PendulumTransform);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(90,0,0)]
    [InlineData(0,0,90)]
    [InlineData(180,0,0)]
    public void EditingTiltKeepsTheAuthoredRestPoseUntilRunAndResetRestoresIt(float x,float y,float z)
    {
        var world=World();
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0]});
            chimes.RotationDegrees=new(x,y,z);
            world.PresentFrame(.1,1);
            Assert.Equal(Quaternion.Identity,(chimes.Transform.AffineInverse()*chimes.PendulumTransform).Basis.GetRotationQuaternion());
            world.Start();world.Step();world.PresentFrame(.016,1);
            Assert.True(Math.Abs((chimes.Transform.AffineInverse()*chimes.PendulumTransform).Basis.GetRotationQuaternion().Dot(Quaternion.Identity))>.999f);
            world.Restore();chimes=(WindChimesPart)world.FindPart(Id(Role.Chimes))!;
            world.PresentFrame(.1,1);Assert.Equal(Quaternion.Identity,(chimes.Transform.AffineInverse()*chimes.PendulumTransform).Basis.GetRotationQuaternion());
        }
        finally{world.Free();}
    }
    [Fact]
    public void TurningOffAirAllowsCoastThenRestNotAnArtificialSoundLoop()
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id=Id(Role.Fan),Kind=Kind(Role.Fan),Position=[-3,5.4f,0]});
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            fan.Active=false;
            for(var i=0;i<2400;i++)world.Step();
            var count=chimes.PulseCount;
            Assert.Equal(Vector3.Zero,chimes.LastAirForce);
            Assert.InRange(Speed(world,chimes).Length,0,.001f);
            Assert.Empty(chimes.AcousticPulses);
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(count,chimes.PulseCount);
            fan.Active=true;
            for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>count);
        }
        finally{world.Free();}
    }
    [Fact]
    public void BallCanStrikeTubeButRestingOrMissingBallDoesNotRing()
    {
        var world=World();world.Gravity=0;world.Pressure=0;
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[-2,6,0]});
            world.Start();for(var i=0;i<60;i++)world.Step();Assert.Equal(0,chimes.PulseCount);
            var payload=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            world.Physics.ApplyImpulse(payload.Id,new(5*ball.Mass,0,0),payload.Center);
            for(var i=0;i<100;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.X<0); // ordinary tube collision, no special impulse
            world.Restore();chimes=(WindChimesPart)world.FindPart(Id(Role.Chimes))!;
            ball=world.FindPart(Id(Role.Ball))!;ball.Position=new(-2,6,2);
            ball.InitialVelocity=Vector3.Right*5;world.Start();
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);
        }
        finally{world.Free();}
    }
    [Fact]
    public void SharedAirflowPushesBallsByMassAndTransparentShellsBlockAirNotLight()
    {
        var world=World();world.Gravity=0;
        try
        {
            var fan=world.AddPart(new(){Id=Id(Role.Fan),Kind=Kind(Role.Fan),Position=[-3,4,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,4,0]});
            world.Start();
            var assembly=world.PhysicsAssembly;
            var bodies=assembly.Bodies.ToArray().ToDictionary(body=>body.Id);
            var colliders=bodies.Keys.ToDictionary(id=>id,id=>world.Physics.Collider(id).Declaration);
            var emitter=fan.CreateAirflowSource(world)!.Value;
            MechanicalTransferLoad Sample(CollisionVector point)=>new NozzleCoupledJetReceiver().CreateLoad(new(0),emitter.Supply,new(assembly.Body(new(fan,MachinePart.RootBody)).Id,
                assembly.Body(new(ball,MachinePart.RootBody)).Id,SceneGeometryAdapter.CaptureVector(emitter.At-fan.LocalCenterOfMass),
                SceneGeometryAdapter.CaptureVector(emitter.Direction),point,emitter.Reach,emitter.Width,[]),emitter.Material,AirflowNetwork.TransferWorkTolerance);
            Assert.Equal(9,Sample(default).EvaluateDemand(bodies,world.Physics.Joints.ToArray(),colliders).Response.Force);
            Assert.Equal(0,Sample(new(0,2,0)).EvaluateDemand(bodies,world.Physics.Joints.ToArray(),colliders).Response.Force);
            world.Step();Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.X,.074f,.076f);
            world.Restore();
            fan=world.FindPart(Id(Role.Fan))!;ball=world.FindPart(Id(Role.Ball))!;
            world.AddPart(new(){Id=Id(Role.Pipe),Kind=Kind(Role.Pipe),Position=[0,4,0]});
            Assert.Equal(5,WorldGeometry.Trace(TraceMedium.Light,world,new(0,4,-2),Vector3.Back,5,fan,ball));
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,new(0,4,-2),Vector3.Back,5,fan,ball),1.1f,1.4f);
            Assert.Equal(10,WorldGeometry.Trace(TraceMedium.Air,world,new(-5,4,0),Vector3.Right,10,fan,ball));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Trace((TraceMedium)99,world,Vector3.Zero,Vector3.Right,2,fan));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(ShellKind.Bend45)]
    [InlineData(ShellKind.Bend90)]
    [InlineData(ShellKind.Funnel)]
    public void CurvedClearShellsBlockAirAcrossWallsButRemainOpticallyClear(ShellKind kind)
    {
        var world=World();
        try
        {
            var part=world.AddPart(new(){Id=Id(Role.Shell),Kind=Kind(kind),Position=[0,5,0],Orientation = PartOrientation.FromEulerDegrees(20,35,10)});
            var center=part.Bends.Count>0?part.Bends[0].Pose*part.Bends[0].Centre(part.Bends[0].Sweep*.5f):Vector3.Zero;
            var origin=part.Transform*(center+Vector3.Back*2);
            var direction=part.Basis*Vector3.Forward;
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,origin,direction,4,null),.7f,1.4f);
            Assert.Equal(4,WorldGeometry.Trace(TraceMedium.Light,world,origin,direction,4,null));
        }
        finally{world.Free();}
    }
    [Fact]
    public void TheSameJetAcceleratesAHeavierBallLess()
    {
        var world=World();world.Gravity=0;
        try
        {
            world.AddPart(new(){Id=Id(Role.Fan),Kind=Kind(Role.Fan),Position=[-3,4,0]});
            var light=world.AddPart(new(){Id=Id(Role.Light),Kind=Kind(Role.Ball),Position=[0,4,0]});
            var heavy=world.AddPart(new(){Id=Id(Role.Heavy),Kind=Kind(Role.Heavy),Position=[-1,4,.5f]});
            world.Start();world.Step();
            foreach(var part in new[]{light,heavy})
            {
                var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
                var drag=Assert.Single(world.Physics.Loads.Drag.ToArray(),load=>load.Body==body.Id);
                Assert.Equal(part.Drag*world.Pressure,drag.LinearRate);Assert.Equal(0,drag.AngularRate);
            }
            // Independent RK4 reference for the saturated shared 9N source:
            // each receiver gets force proportional to its remaining flow slip.
            // This fixture stays in positive flow with both demands above the cap.
            (double Light,double Heavy) Rate(double a,double b)
            {
                var totalSlip=24-a-b;
                return (9*(12-a)/totalSlip/light.Mass-light.Drag*a,
                    9*(12-b)/totalSlip/heavy.Mass-heavy.Drag*b);
            }
            var a=0.0;var b=0.0;var dt=(double)MachineWorld.Tick/1024;
            for(var i=0;i<1024;i++)
            {
                var k1=Rate(a,b);
                var k2=Rate(a+dt*k1.Light/2,b+dt*k1.Heavy/2);
                var k3=Rate(a+dt*k2.Light/2,b+dt*k2.Heavy/2);
                var k4=Rate(a+dt*k3.Light,b+dt*k3.Heavy);
                a+=dt*(k1.Light+2*k2.Light+2*k3.Light+k4.Light)/6;
                b+=dt*(k1.Heavy+2*k2.Heavy+2*k3.Heavy+k4.Heavy)/6;
            }
            Assert.InRange(a,0,.1);Assert.InRange(b,0,.1);
            Assert.InRange(Math.Abs(world.PhysicsAssembly.Body(new(light,MachinePart.RootBody)).LinearVelocity.X-a),0,1e-6);
            Assert.InRange(Math.Abs(world.PhysicsAssembly.Body(new(heavy,MachinePart.RootBody)).LinearVelocity.X-b),0,1e-6);
            Assert.True(world.PhysicsAssembly.Body(new(heavy,MachinePart.RootBody)).LinearVelocity.X<world.PhysicsAssembly.Body(new(light,MachinePart.RootBody)).LinearVelocity.X);
        }
        finally{world.Free();}
    }
    [Fact]
    public void TiltedPendulumStaysConstrainedInThreeDimensions()
    {
        var world=World();
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),
                Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(34.37747f,45.83662f,17.188734f)});
            world.Start();
            var body=Pendulum(world,chimes);
            world.Physics.ApplyImpulse(body.Id,new(0,0,1),Tip(world,chimes));
            for(var i=0;i<1200;i++)
            {
                world.Step();
                Assert.InRange(Offset(world,chimes).Length,1.4999,1.5001);
                var joint=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(chimes,WindChimesPart.Suspension)));
                Assert.InRange(joint.Error(1e-8),0,1.01e-7);
                Assert.Same(body,Pendulum(world,chimes));
            }
            Assert.True(chimes.PulseCount>0);
        }
        finally{world.Free();}
    }
    [Fact]
    public void OppositeFansCancelAndNoPressureMeansNoAirForce()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id=Id(Role.Left),Kind=Kind(Role.Fan),Position=[-3,5.4f,0]});
            world.AddPart(new(){Id=Id(Role.Right),Kind=Kind(Role.Fan),Position=[3,5.4f,0],Orientation = PartOrientation.FromEulerDegrees(0,180,0)});
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);Assert.InRange(chimes.LastAirForce.Length(),0,.00001f);
            world.FindPart(Id(Role.Right))!.Visible=false;world.Pressure=0;
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);Assert.Equal(Vector3.Zero,chimes.LastAirForce);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousBallStrikesChooseStrongestToneBeforeAudioPlayback(bool reverse)
    {
        var world=World();world.Gravity=0;world.Pressure=0;
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id=Id(Role.Chimes),Kind=Kind(Role.Chimes),Position=[0,6,0]});
            var left=world.AddPart(new(){Id=Id(reverse?Role.Right:Role.Left),Kind=Kind(Role.Ball),Position=[-.815f,6,0],Properties=new(){[PartParameterName.Of(Parameter.Mass)]=.1f}});
            var right=world.AddPart(new(){Id=Id(reverse?Role.Left:Role.Right),Kind=Kind(Role.Ball),Position=[.815f,6,0]});
            left.InitialVelocity=Vector3.Right*6;right.InitialVelocity=Vector3.Left*6;
            world.Start();
            world.Step();
            var pulse=Assert.Single(chimes.AcousticPulses);Assert.Equal(1,chimes.PulseCount);
            Assert.Equal(ToneBand.Mid,pulse.Tone);Assert.Equal(1,pulse.Strength);
            world.Step();
            var audio=chimes.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            Assert.Equal(AcousticAudio.Create(ToneBand.Mid,AcousticVoice.Bell).Data,Assert.IsType<AudioStreamWav>(audio.Stream).Data);
            audio.Stop();
            Assert.Equal(1,chimes.PulseCount);Assert.Single(chimes.AcousticPulses);
        }
        finally{world.Free();}
    }
    [Fact]
    public void FixtureAndParameterBoundariesKeepCanonicalNames()
    {
        Assert.Equal("wind_chimes",Kind(Role.Chimes));Assert.Equal("sound_meter",Kind(Role.Meter));
        Assert.Equal("pipe_bend_45",Kind(ShellKind.Bend45));
        Assert.Equal("pipe_bend_90",Kind(ShellKind.Bend90));Assert.Equal("funnel",Kind(ShellKind.Funnel));
        foreach(var (parameter,wire) in new[]{(Parameter.Mass,"mass"),(Parameter.Powered,"powered"),
            (Parameter.Force,"force"),(Parameter.Reach,"reach"),(Parameter.Width,"width")})
            Assert.Equal(wire,PartParameterName.Of(parameter));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((Parameter)999));
    }

    [Fact]
    public void InvalidFanParametersAndAssemblyIdentitiesAreRejected()
    {
        var world=World();
        try
        {
            foreach(var pair in new[]{(Parameter.Powered,.5f),(Parameter.Force,-1f),(Parameter.Reach,0f),(Parameter.Width,float.NaN)})
                Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=Id(Role.Invalid),Kind=Kind(Role.Fan),Properties=new(){[PartParameterName.Of(pair.Item1)]=pair.Item2}}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ChimeAssembly.Tube((ChimeTubeId)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ChimeAssembly.TubeGeometry((ChimeTubeId)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WindChimesPart.TubeBody((ChimeTubeId)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((ShellKind)999));
        }
        finally{world.Free();}
    }
}
