using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WindmillTests(NativeSceneFixture godot)
{
    private enum AirFixture { Fan, RearFan, Windmill, Wall }
    public enum AirControl { EdgeOn, Weak, Opposing }
    public enum Exposure { Full, Partial, Blocked }
    private static PartSpec AirSpec(AirFixture fixture,Vector3 position,Vector3 rotation=default)
    {
        var name=fixture switch
        {
            AirFixture.Fan or AirFixture.RearFan=>"fan",AirFixture.Windmill=>"windmill",AirFixture.Wall=>"wall",
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        };
        return new(){Id=fixture==AirFixture.RearFan?"rear_fan":name,Kind=name,Position=[position.X,position.Y,position.Z],
            Orientation=PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z)};
    }
    private static double UnloadedSpeed(MachineWorld world,WindmillPart mill,double force,double ticks)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(mill,WindmillPart.RotorJoint));
        var axis=joint.FrameA.Orientation.Apply(new(0,0,1));
        var inverseInertia=CollisionVector.Dot(axis,joint.A.InverseInertia(axis));
        var gain=(double)mill.ReadParameter(WindmillParameter.RadiansPerForce);
        var target=Math.Clamp(force*gain,-WindmillPart.MaximumSpeed,WindmillPart.MaximumSpeed);
        var resistance=target==0?WindmillPart.TorqueArm/gain:Math.Abs(force*WindmillPart.TorqueArm/target);
        var halfDecay=resistance*inverseInertia*(MachineWorld.Tick/MachineWorld.Substeps)*.5;
        var factor=(1-halfDecay)/(1+halfDecay);
        return target*(1-Math.Pow(factor,ticks*MachineWorld.Substeps));
    }
    private static double AcceptedAirForce(MachineWorld world,WindmillPart mill,double restForce,int ticks)
    {
        var end=UnloadedSpeed(world,mill,restForce,ticks);
        var start=UnloadedSpeed(world,mill,restForce,ticks-1.0/MachineWorld.Substeps);
        return restForce*(1-WindmillPart.TorqueArm*(start+end)*.5/AirflowNetwork.ReferenceFlowSpeed);
    }
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false,false,0)]
    [InlineData(false,true,0)]
    [InlineData(true,false,0)]
    [InlineData(true,true,0)]
    [InlineData(false,false,1)]
    [InlineData(true,true,1)]
    [InlineData(false,true,2)]
    public void SignedWindDrivesConveyorsAndReversersInTheSameSubstep(bool backWind,bool reverseOrder,int reversers)
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id=reverseOrder?"z-fan":"a-fan",Kind="fan",Position=[backWind?2:-4,6,0],Orientation = PartOrientation.FromEulerDegrees(0,backWind?180:0,0)});
            var windmill=(WindmillPart)world.AddPart(new(){Id=reverseOrder?"a-windmill":"z-windmill",Kind="windmill",Position=[-1,6,0]});
            var first=(ConveyorPart)world.AddPart(new(){Id="first",Kind="conveyor",Position=[2,2,2]});
            var last=(ConveyorPart)world.AddPart(new(){Id="last",Kind="conveyor",Position=[2,2,-2]});
            Assert.True(world.Connect(windmill,first));
            MachinePart previous=first;
            for(var i=0;i<reversers;i++)
            {
                var gear=world.AddPart(new(){Id="reverse_"+i,Kind="reverse_transmission",Position=[-3,2,2-i*2]});
                Assert.True(world.Connect(previous,gear));previous=gear;
            }
            Assert.True(world.Connect(previous,last));
            if(reverseOrder)
            {
                var links=world.Connections.ToArray();
                foreach(var link in links)Assert.True(world.Disconnect(link));
                foreach(var link in links.Reverse())
                    Assert.True(world.Connect(world.FindPart(link.From)!,link.FromPort!.Value,
                        world.FindPart(link.To)!,link.ToPort!.Value,link.Type));
            }
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();world.Step();
            var windSign=backWind?-1:1;var gearSign=reversers%2==0?1:-1;
            Assert.InRange(windmill.ShaftSpeed*windSign,.14f,.16f);
            Assert.Equal(windmill.ShaftSpeed,first.ShaftSpeed);
            Assert.Equal(windmill.ShaftSpeed*gearSign,last.ShaftSpeed);
            for(var i=1;i<240;i++)world.Step();
            Assert.Equal(6*windSign,windmill.ShaftSpeed,4);
            Assert.Equal(4*windSign*gearSign,last.SurfaceSpeed,4);
            Assert.Contains(new MachineEvent(MachineEventKind.Turned,windmill.Uid),world.Events.Keys);
            Assert.InRange(Math.Abs(Mathf.AngleDifference(windmill.ShaftAngle,windmill.GetNode<Node3D>("Visual/WindRotor").Rotation.X)),0,.00001f);
            Assert.InRange(Math.Abs(Mathf.AngleDifference(-windmill.ShaftAngle,windmill.GetNode<Node3D>("Visual/OutputPulley").Rotation.Z)),0,.00001f);
            var speed=windmill.ShaftSpeed;var angle=windmill.ShaftAngle;
            windmill._Process(.2);Assert.Equal(speed,windmill.ShaftSpeed);Assert.Equal(angle,windmill.ShaftAngle);
            Assert.True(MechanicalNetwork.Shaft(world,first,SocketId.DriveIn).A.KineticEnergy>0);
            fan.Active=false;world.Step();
            Assert.Equal(0,windmill.AxialForce);
            Assert.Empty(world.Physics.MotorUse.ToArray());
            Assert.InRange(windmill.ShaftSpeed*windSign,5.8f,5.9f); // coast, no instant stop
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,windmill.ShaftSpeed);Assert.Equal(0,last.SurfaceSpeed);Assert.False(windmill.Active);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            windmill=(WindmillPart)world.FindPart(windmill.Uid)!;
            Assert.Equal(0,windmill.ShaftSpeed);Assert.Equal(0,windmill.ShaftAngle);Assert.Equal(0,windmill.ShaftTravel);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6*windSign,((WindmillPart)world.FindPart(windmill.Uid)!).ShaftSpeed,4);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(30f,45f,20f)]
    public void WholeAssemblyCanTurnInAllThreeDimensions(float x,float y,float z)
    {
        var world=World();
        try
        {
            var rotation=new Vector3(x,y,z);var basis=Basis.FromEuler(rotation*Mathf.Pi/180);
            var center=new Vector3(0,6,0);var fanAt=center-basis.X*3;
            world.AddPart(AirSpec(AirFixture.Fan,fanAt,rotation));
            var windmill=(WindmillPart)world.AddPart(AirSpec(AirFixture.Windmill,center,rotation));
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.InRange(Math.Abs(windmill.AxialForce-AcceptedAirForce(world,windmill,9,120)),0,1e-6);
            Assert.InRange(Math.Abs(windmill.ShaftSpeed-UnloadedSpeed(world,windmill,9,120)),0,1e-6);
            Assert.Single(windmill.ConnectionPorts);
            Assert.Equal(ConnectionDomain.Mechanical,windmill.ConnectionPorts.Single().Domain);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(Exposure.Full,9)]
    [InlineData(Exposure.Partial,6.75)]
    [InlineData(Exposure.Blocked,0)]
    public void RotorAreaSamplesDistinguishPartialAndFullOcclusion(Exposure exposure,double expectedForce)
    {
        if(!Enum.IsDefined(exposure))throw new ArgumentOutOfRangeException(nameof(exposure));
        var world=World();
        try
        {
            world.AddPart(AirSpec(AirFixture.Fan,new(-4,6,0)));
            var windmill=(WindmillPart)world.AddPart(AirSpec(AirFixture.Windmill,new(-1,6,0)));
            if(exposure!=Exposure.Full)
            {
                var wall=(WallPart)world.AddPart(AirSpec(AirFixture.Wall,new(-2.5f,exposure==Exposure.Partial?6.5f:6,0),new(0,90,0)));
                if(exposure==Exposure.Partial)wall.SetDimensions(new(3,.4f,.25f)); // top sample only
            }
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.InRange(Math.Abs(windmill.AxialForce-AcceptedAirForce(world,windmill,expectedForce,120)),0,1e-6);
            Assert.InRange(Math.Abs(windmill.ShaftSpeed-UnloadedSpeed(world,windmill,expectedForce,120)),0,1e-6);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(true,false,false,true)]
    [InlineData(false,false,false,false)]
    [InlineData(true,true,false,false)]
    [InlineData(true,false,true,false)]
    public void BallTransportRequiresWindAndARealMechanicalBelt(bool connected,bool blocked,bool off,bool expected)
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0],Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=off?0:1}});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0]});
            var belt=(ConveyorPart)world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,3,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[1,4.5f,0]});
            if(connected)Assert.True(world.Connect(windmill,belt));
            if(blocked)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,6,0],Orientation = PartOrientation.FromEulerDegrees(0,90,0)});
            world.Start();for(var i=0;i<180;i++)world.Step();
            Assert.Equal(expected,world.Events.ContainsKey(new(MachineEventKind.Transported,belt.Uid,ball.Uid)));
            if(expected)Assert.True(ball.Position.X>2.5f);
            else Assert.Equal(1,ball.Position.X,3);
            // Ball starts below the jet and only falls farther away: fan cannot move it directly.
            Assert.True(ball.Position.Y<5);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(AirControl.EdgeOn)]
    [InlineData(AirControl.Weak)]
    [InlineData(AirControl.Opposing)]
    public void EdgeOnOpposingAndWeakAirDoNotCreateDrive(AirControl control)
    {
        if(!Enum.IsDefined(control))throw new ArgumentOutOfRangeException(nameof(control));
        var world=World();
        try
        {
            var front=AirSpec(AirFixture.Fan,new(-4,6,0));
            if(control==AirControl.Weak)front.Properties=new(){[PartParameterName.Of(FanParameter.Force)]=.01f};
            world.AddPart(front);
            var windmill=(WindmillPart)world.AddPart(AirSpec(AirFixture.Windmill,new(-1,6,0),
                control==AirControl.EdgeOn?new(0,90,0):default));
            if(control==AirControl.Opposing)
                world.AddPart(AirSpec(AirFixture.RearFan,new(2,6,0),new(0,180,0)));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,windmill.ShaftSpeed);
            Assert.All(world.Physics.SourceTotals.ToArray(),total=>Assert.Equal(0,total.Extraction.Supplied));
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }
    [Fact]
    public void ReversalCrossesZeroAlongTheFiniteInertiaTrajectory()
    {
        var world=World();
        try
        {
            var front=world.AddPart(AirSpec(AirFixture.Fan,new(-4,6,0)));
            var rearSpec=AirSpec(AirFixture.RearFan,new(2,6,0),new(0,180,0));
            rearSpec.Properties=new(){[PartParameterName.Of(FanParameter.Powered)]=0};
            var rear=world.AddPart(rearSpec);
            var windmill=(WindmillPart)world.AddPart(AirSpec(AirFixture.Windmill,new(-1,6,0)));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<120;i++)world.Step();
            Assert.InRange(Math.Abs(windmill.ShaftSpeed-UnloadedSpeed(world,windmill,9,120)),0,1e-6);
            front.Active=false;rear.Active=true;
            var previous=windmill.ShaftSpeed;var crossed=false;
            var joint=(PhysicsFrameJoint)world.CurrentJoint(new(windmill,WindmillPart.RotorJoint));
            var axis=joint.FrameA.Orientation.Apply(new(0,0,1));
            var inverseInertia=CollisionVector.Dot(axis,joint.A.InverseInertia(axis));
            var pitch=(double)WindmillPart.TorqueArm;
            var target=9*(double)windmill.ReadParameter(WindmillParameter.RadiansPerForce);
            var drive=9*pitch;
            var linearResistance=drive/target;
            var cappedResistance=linearResistance-(9.0/12)*pitch*pitch;
            double Advance(double speed,double duration,double resistance)
            {
                var halfDecay=resistance*inverseInertia*duration*.5;
                return (speed*(1-halfDecay)-drive*inverseInertia*duration)/(1+halfDecay);
            }
            double ExpectedTick(double speed)
            {
                // Opposite motion saturates the jet: slip damping enters only
                // after zero. Solve the two affine laws and their exact midpoint
                // zero crossing, independently of the production transfer solver.
                for(var substep=0;substep<MachineWorld.Substeps;substep++)
                {
                    double remaining=MachineWorld.Tick/MachineWorld.Substeps;
                    if(speed>0)
                    {
                        var crossing=speed/(inverseInertia*(drive+cappedResistance*speed*.5));
                        if(crossing>=remaining)
                        {
                            speed=Advance(speed,remaining,cappedResistance);
                            continue;
                        }
                        remaining-=crossing;speed=0;
                    }
                    speed=Advance(speed,remaining,linearResistance);
                }
                return speed;
            }
            double expected=previous;
            for(var i=0;i<120;i++)
            {
                world.Step();
                expected=ExpectedTick(expected);
                Assert.InRange(Math.Abs(windmill.ShaftSpeed-expected),0,1e-6);
                crossed|=previous>=0&&windmill.ShaftSpeed<0;previous=windmill.ShaftSpeed;
            }
            Assert.True(crossed);
            Assert.True(windmill.ShaftSpeed<0);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }
    [Fact]
    public void OutputIsBoundedAndCompetingDrivesAreRejected()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0],Properties=new(){[PartParameterName.Of(FanParameter.Force)]=40}});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0],Properties=new(){[PartParameterName.Of(WindmillParameter.RadiansPerForce)]=4}});
            var belt=world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,2,0]});
            var motor=world.AddPart(new(){Id="motor",Kind="motor",Position=[-4,2,2]});
            Assert.True(world.Connect(windmill,belt));Assert.False(world.Connect(motor,belt));
            Assert.False(world.Connect(belt,windmill));
            world.Start();for(var i=0;i<240;i++)world.Step();
            Assert.Equal(WindmillPart.MaximumSpeed,windmill.ShaftSpeed);
            foreach(var value in new[]{0f,5f,float.NaN})
                Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="invalid",Kind="windmill",Properties=new(){[PartParameterName.Of(WindmillParameter.RadiansPerForce)]=value}}));
        }
        finally{world.Free();}
    }
    private partial class InvalidSamples : MachinePart
    {
        public override BodyEnvelope CollisionEnvelope=>BodyEnvelope.Sphere;
        public override BodyDynamics InitialBodyDynamics=>BodyDynamics.SolidSphere(1,Radius,default,default);
        protected override void Build() { Dynamic=true;Drag=0; }
        public override IReadOnlyList<AirflowSample> AirflowSamples=>[new(Vector3.Zero,2,RootBody,AirflowResponse.BodyForce,AirflowReceiver,null)];
    }
    [Fact]
    public void InvalidSampleWeightsFailBeforeAnyReceiverChanges()
    {
        var world=World();var invalid=new InvalidSamples();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0]});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0]});
            FixtureParts.Attach(world,invalid,FixturePartId.First);
            world.Start();
            var before=world.Physics.Capture();
            Assert.Throws<ArgumentException>(()=>world.Step());
            Assert.Equal(before.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(0,windmill.ShaftSpeed);
        }
        finally{world.Free();}
    }
}
