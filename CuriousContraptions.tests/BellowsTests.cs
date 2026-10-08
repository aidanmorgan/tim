using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BellowsTests(NativeSceneFixture godot,ITestOutputHelper output)
{
    private enum Role { Pump, Striker, SecondStriker, Rotor, Belt, Cargo, Wall, Chimes }
    private enum PayloadParameter { Mass }
    private enum WindmillSetup { Catalogue, LowResistance, LoadMatched }
    private enum StrikerLoad { Original, FullStroke }
    public enum ContactFace { Top, Side, Underside, Miss }
    private static string WireId(Role role)=>role switch
    {
        Role.Pump=>"pump",Role.Striker=>"striker",Role.SecondStriker=>"second_striker",
        Role.Rotor=>"rotor",Role.Belt=>"belt",Role.Cargo=>"cargo",Role.Wall=>"wall",Role.Chimes=>"chimes",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Catalog(Role role)=>role switch
    {
        Role.Pump=>"bellows",Role.Striker or Role.SecondStriker or Role.Cargo=>"ball",
        Role.Rotor=>"windmill",Role.Belt=>"conveyor",Role.Wall=>"wall",Role.Chimes=>"wind_chimes",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Add(MachineWorld world,Role role,Vector3 position,Vector3 rotation=default,float mass=1)
    {
        var spec=new PartSpec {Id=WireId(role),Kind=Catalog(role),Position=[position.X,position.Y,position.Z],
            Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z)};
        if(role is Role.Striker or Role.SecondStriker or Role.Cargo)
            spec.Properties[PartParameterName.Of(PayloadParameter.Mass)]=mass;
        return world.AddPart(spec);
    }
    private static WindmillPart AddRotor(MachineWorld world,WindmillSetup setup)
    {
        var spec=new PartSpec {Id=WireId(Role.Rotor),Kind=Catalog(Role.Rotor),Position=[-1,5.88f,0]};
        switch(setup)
        {
            case WindmillSetup.Catalogue: break;
            case WindmillSetup.LowResistance:
            case WindmillSetup.LoadMatched:
                spec.Properties[PartParameterName.Of(WindmillParameter.RadiansPerForce)]=4;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(setup));
        }
        return (WindmillPart)world.AddPart(spec);
    }
    private static MachinePart Find(MachineWorld world,Role role)=>
        world.FindPart(WireId(role))??throw new InvalidOperationException("Missing test part.");
    private MachineWorld World(float gravity=9.81f)
    {
        var world=new MachineWorld {Gravity=gravity,Pressure=1};godot.Tree.Root.AddChild(world);return world;
    }
    private static BellowsPart Pump(MachineWorld world)=>(BellowsPart)Add(world,Role.Pump,new(-4,6,0));
    private static PhysicsFrameJoint Guide(MachineWorld world,BellowsPart pump)=>
        Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(pump,BellowsPart.PlateGuide)));
    private static void CheckPlate(MachineWorld world,BellowsPart pump)
    {
        var joint=Guide(world,pump);
        var elastic=Assert.Single(world.Physics.Loads.Elastic.ToArray(),load=>load.Joint==joint.Id);
        Assert.Equal(FrameJointKind.Slider,elastic.Kind);
        Assert.Equal(BellowsPart.ReturnStiffness,elastic.Potential.Stiffness);
        Assert.Equal(BellowsPart.ReturnPreload/BellowsPart.ReturnStiffness,elastic.Potential.RestCoordinate);
        var damping=Assert.Single(world.Physics.Loads.Damping.ToArray(),load=>load.Joint==joint.Id);
        Assert.Equal(FrameJointKind.Slider,damping.Kind);
        // Compression work is extracted by paired transfers, not a duplicate damper.
        Assert.Equal(0,damping.NegativeCoefficient);
        Assert.Equal(BellowsPart.RefillDamping,damping.PositiveCoefficient);
        Assert.Equal(PhysicsMotionType.Dynamic,joint.A.MotionType);
        Assert.Equal(1/BellowsPart.PlateMass,joint.A.InverseMass);
        Assert.InRange(joint.Travel.Error,-BellowsPart.MaximumStroke-1e-7,1e-7);
        Assert.InRange(joint.Error(1e-8),0,1.01e-7);
        world.PresentFrame(0,1);
        Assert.InRange((SceneGeometryAdapter.CaptureRigidPose(pump.PlateTransform).Center-joint.A.Center).Length,0,1e-6);
        Assert.InRange(Math.Abs(pump.Compression-Math.Max(0,-joint.Travel.Error)),0,1e-7);
        Assert.Equal(Vector3.Zero,pump.Boxes.Single(b=>b.Body==BellowsPart.PlateBody).At);
    }

    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void PhysicalImpactDrivesWindmillAndConveyorOnlyWithClearAirAndBelt(bool missed,bool blocked,bool disconnected)=>
        VerifyTransfer(missed,blocked,disconnected,WindmillSetup.Catalogue,StrikerLoad.Original);

    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void LowResistanceWindmillCompletesTransferOnlyWithClearAirAndBelt(bool missed,bool blocked,bool disconnected)=>
        VerifyTransfer(missed,blocked,disconnected,WindmillSetup.LowResistance,StrikerLoad.Original);

    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void FullStrokeInputCompletesTransferWithClearAirAndBelt(bool missed,bool blocked,bool disconnected)=>
        VerifyTransfer(missed,blocked,disconnected,WindmillSetup.LowResistance,StrikerLoad.FullStroke);

    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void MatchedLightCargoCompletesTransferOnlyWithClearAirAndBelt(bool missed,bool blocked,bool disconnected)=>
        VerifyTransfer(missed,blocked,disconnected,WindmillSetup.LoadMatched,StrikerLoad.Original);

    private void VerifyTransfer(bool missed,bool blocked,bool disconnected,WindmillSetup setup,StrikerLoad strikerLoad)
    {
        var ticks=setup switch
        {
            WindmillSetup.Catalogue=>480,
            WindmillSetup.LowResistance or WindmillSetup.LoadMatched=>960,
            _=>throw new ArgumentOutOfRangeException(nameof(setup))
        };
        var world=World();
        try
        {
            var pump=Pump(world);
            var strikerMass=strikerLoad switch
            {
                StrikerLoad.Original=>1f,
                StrikerLoad.FullStroke=>2f,
                _=>throw new ArgumentOutOfRangeException(nameof(strikerLoad))
            };
            // Static full-stroke support requires m > (8+20*.4)/g-.5.
            // This does not bound the original falling striker's transient stroke.
            if(strikerLoad==StrikerLoad.FullStroke)
                Assert.True((strikerMass+BellowsPart.PlateMass)*world.Gravity>
                    BellowsPart.ReturnPreload+BellowsPart.ReturnStiffness*BellowsPart.MaximumStroke);
            Add(world,Role.Striker,new(-4,9,missed?2:0),mass:strikerMass);
            var rotor=AddRotor(world,setup);
            var beltSpec=new PartSpec {Id=WireId(Role.Belt),Kind=Catalog(Role.Belt),Position=[2,3,0],Orientation=PartOrientation.Identity};
            if(setup==WindmillSetup.LoadMatched)
                beltSpec.Properties[PartParameterName.Of(ConveyorParameter.SurfacePerRadian)]=3;
            var belt=(ConveyorPart)world.AddPart(beltSpec);
            var cargo=Add(world,Role.Cargo,new(1,3.6f,0),mass:setup==WindmillSetup.LoadMatched?.1f:1);
            if(!disconnected)
            {
                Assert.True(world.Connect(rotor,belt));
                Assert.Contains(world.Connections,c=>c.From==rotor.Uid&&c.To==belt.Uid&&
                    c.Type==ConnectionDomain.Mechanical&&c.FromPort==SocketId.Drive&&c.ToPort==SocketId.DriveIn);
            }
            if(blocked)Add(world,Role.Wall,new(-2.5f,5.88f,0),new(0,90,0));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var maximumSpeed=0f;var maximumCompression=0f;
            var dynamicBodies=world.PhysicsAssembly.Bodies.ToArray().Where(b=>b.MotionType==PhysicsMotionType.Dynamic).ToArray();
            var initialHeights=dynamicBodies.ToDictionary(b=>b.Id,b=>b.Center.Y);
            var initialKinetic=dynamicBodies.Sum(b=>b.KineticEnergy);
            var maximumEnergyCreation=double.NegativeInfinity;
            double integratedSource=0,integratedReceiver=0,sourceTravelTotal=0,activeDuration=0,maximumSourceSpeed=0;
            double sourceCapLower=0,sourceCapUpper=0;
            Assert.Equal(0,Guide(world,pump).Travel.Error);
            for(var i=0;i<ticks;i++)
            {
                world.Step();maximumSpeed=Math.Max(maximumSpeed,rotor.ShaftSpeed);
                maximumCompression=Math.Max(maximumCompression,pump.Compression);
                var compression=-Guide(world,pump).Travel.Error;
                var elastic=BellowsPart.ReturnPreload*compression+.5*BellowsPart.ReturnStiffness*compression*compression;
                var gravitationalWork=dynamicBodies.Sum(b=>world.Gravity*(initialHeights[b.Id]-b.Center.Y)/b.InverseMass);
                maximumEnergyCreation=Math.Max(maximumEnergyCreation,dynamicBodies.Sum(b=>b.KineticEnergy)+elastic-initialKinetic-gravitationalWork);
                using(var committed=world.ReadCommittedPoses())
                {
                var plateId=Guide(world,pump).A.Id;
                var rotorId=world.PhysicsAssembly.Body(new(rotor,WindmillPart.RotorBody)).Id;
                Assert.Equal(4,committed.MotionStepCount);
                for(var motionIndex=0;motionIndex<committed.MotionStepCount;motionIndex++)
                {
                var motion=committed.ReadMotionStep(motionIndex);
                foreach(var interval in motion.Intervals)
                {
                    if(interval.Body!=plateId)continue;
                    var first=interval.At(interval.StartTime);var last=interval.At(interval.EndTime);
                    var sourceTravel=-(last.Center.Y-first.Center.Y)*15;
                    var rotorInterval=motion.Intervals.ToArray().Single(p=>p.Body==rotorId&&p.StartTime==interval.StartTime&&p.EndTime==interval.EndTime);
                    var rotorFirst=rotorInterval.At(interval.StartTime);
                    var rotorLast=rotorInterval.At(interval.EndTime);
                    var rotation=(rotorLast.Rotation*rotorFirst.Rotation.Inverse()).RotationVector().X;
                    var sourceSpeed=sourceTravel/interval.Duration;
                    sourceTravelTotal+=sourceTravel;
                    if(sourceSpeed>0)activeDuration+=interval.Duration;
                    maximumSourceSpeed=Math.Max(maximumSourceSpeed,sourceSpeed);
                    var angularSpeed=rotation/interval.Duration;
                    var gain=rotor.ReadParameter(WindmillParameter.RadiansPerForce);
                    var restForce=Math.Min(18,1.5*Math.Max(0,sourceSpeed));
                    var target=Math.Min(12,gain*restForce);
                    // Independently derive the aligned four-quarter-sample law.
                    // At zero-resistance calibration the admitted pitch approaches
                    // sourceSpeed/target from below.
                    var pitch=target>0?Math.Min(.4,sourceSpeed/target):.4;
                    var force=!missed&&!blocked&&restForce>=.05?
                        Math.Min(Math.Clamp(1.5*(sourceSpeed-pitch*angularSpeed),0,18),216/sourceSpeed):0;
                    if(sourceTravel>.05)output.WriteLine($"Bellows port: step={motion.StepIndex}, duration={interval.Duration:R}, sourceTravel={sourceTravel:R}, sourceSpeed={sourceSpeed:R}, angularSpeed={angularSpeed:R}, pitch={pitch:R}, force={force:R}");
                    if(strikerLoad==StrikerLoad.FullStroke&&!missed&&!blocked&&sourceTravel>0)
                    {
                        var startSpeed=-15*interval.LinearVelocityAt(interval.StartTime).Y;
                        var endSpeed=-15*interval.LinearVelocityAt(interval.EndTime).Y;
                        var maximum=Math.BitIncrement(Math.Max(startSpeed,endSpeed));
                        var minimum=Math.BitDecrement(Math.Min(startSpeed,endSpeed));
                        var forceCeiling=216/minimum;
                        var demandFloor=Math.Min(18,1.5*(minimum-.4*rotorInterval.PhysicalAngularSpeedBound));
                        Assert.True(minimum>0&&demandFloor>=forceCeiling,
                            $"Captured source demand must exceed its peak power cap: demand={demandFloor:R}, cap={forceCeiling:R}");
                        Assert.True(endSpeed<=startSpeed,"Source speed must decline for accepted-prefix peak agreement.");
                        var travelLower=Math.BitDecrement(Math.BitDecrement(Math.BitDecrement(startSpeed+endSpeed)*.5)*interval.Duration);
                        var travelUpper=Math.BitIncrement(Math.BitIncrement(Math.BitIncrement(startSpeed+endSpeed)*.5)*interval.Duration);
                        sourceCapLower=Math.BitDecrement(sourceCapLower+
                            Math.BitDecrement(Math.BitDecrement(216/maximum)*travelLower));
                        sourceCapUpper=Math.BitIncrement(sourceCapUpper+
                            Math.BitIncrement(Math.BitIncrement(216/Math.BitDecrement(Math.Max(startSpeed,endSpeed)))*travelUpper));
                    }
                    integratedSource+=force*sourceTravel;
                    integratedReceiver+=force*pitch*rotation;
                }
                }
                }
                Assert.Empty(world.Physics.MotorUse.ToArray());
                CheckPlate(world,pump);
                if(i%60==59)
                    output.WriteLine($"Bellows transport: setup={setup}, missed={missed}, blocked={blocked}, disconnected={disconnected}, tick={i+1}, compression={pump.Compression:R}, emission={pump.EmissionForce:R}, rotor={rotor.ShaftSpeed:R}, belt={belt.SurfaceSpeed:R}, cargo={cargo.Position}, velocity={world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).LinearVelocity}, spin={world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).AngularVelocity}");
            }
            var finalCompression=-Guide(world,pump).Travel.Error;
            var finalElastic=BellowsPart.ReturnPreload*finalCompression+.5*BellowsPart.ReturnStiffness*finalCompression*finalCompression;
            var gravityWork=dynamicBodies.Sum(b=>world.Gravity*(initialHeights[b.Id]-b.Center.Y)/b.InverseMass);
            var transfers=world.Physics.TransferTotals.ToArray();
            output.WriteLine($"Bellows energy: setup={setup}, missed={missed}, blocked={blocked}, disconnected={disconnected}, maximumSpeed={maximumSpeed:R}, maximumCompression={maximumCompression:R}, initialKinetic={initialKinetic:R}, finalKinetic={dynamicBodies.Sum(b=>b.KineticEnergy):R}, finalElastic={finalElastic:R}, gravitationalWork={gravityWork:R}, maximumEnergyCreation={maximumEnergyCreation:R}, extracted={transfers.Sum(t=>t.SourceExtraction.Supplied-t.SourceExtraction.Dissipated):R}, delivered={transfers.Sum(t=>t.ReceiverDelivery.Supplied-t.ReceiverDelivery.Dissipated):R}, pairedDissipation={transfers.Sum(t=>t.PairedWork.Dissipated-t.PairedWork.Supplied):R}, independentSource={integratedSource:R}, independentReceiver={integratedReceiver:R}, strikerMass={strikerMass:R}, sourceTravel={sourceTravelTotal:R}, activeDuration={activeDuration:R}, maximumSourceSpeed={maximumSourceSpeed:R}, sourceCapLower={sourceCapLower:R}, sourceCapUpper={sourceCapUpper:R}");
            if(strikerLoad==StrikerLoad.FullStroke&&!missed&&!blocked)
            {
                var extracted=transfers.Sum(t=>t.SourceExtraction.Supplied-t.SourceExtraction.Dissipated);
                var ledgerError=transfers.Sum(t=>t.SourceExtraction.SuppliedErrorBound+t.SourceExtraction.DissipatedErrorBound);
                Assert.InRange(extracted,Math.BitDecrement(sourceCapLower-ledgerError),Math.BitIncrement(sourceCapUpper+ledgerError));
                output.WriteLine($"Peak-cap ledger enclosure: value={extracted:R}, lower={sourceCapLower:R}, upper={sourceCapUpper:R}");
            }
            Assert.Equal(!missed,pump.StrokeCount>0);
            Assert.Equal(!missed&&!blocked,maximumSpeed>1);
            Assert.Equal(!missed&&!blocked&&!disconnected,
                world.Events.ContainsKey(new(MachineEventKind.Transported,belt.Uid,cargo.Uid)));
            if(!missed&&!blocked&&!disconnected)Assert.True(cargo.Position.X>2.5f);
            else Assert.Equal(1,cargo.Position.X,3);
            if(!missed)Assert.True(maximumCompression>.05f);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            pump=(BellowsPart)Find(world,Role.Pump);
            Assert.Equal(BellowsPhase.Ready,pump.Phase);Assert.Equal(0,pump.Compression);Assert.Equal(0,pump.StrokeCount);
        }
        finally{world.Free();}
    }

    [Fact]
    public void LoadHoldsStrokeAndPhysicalRemovalAllowsRefillThenAnotherStroke()
    {
        var world=World();
        try
        {
            var pump=Pump(world);
            var body=Add(world,Role.Striker,new(-4,9,0));
            world.Start();for(var i=0;i<360;i++)world.Step();
            Assert.True(pump.Compression>.05f);Assert.Equal(1,pump.StrokeCount);
            var equilibrium=((body.Mass+BellowsPart.PlateMass)*world.Gravity-BellowsPart.ReturnPreload)/
                BellowsPart.ReturnStiffness;
            var initialError=Math.Abs(pump.Compression-equilibrium);
            // A damped finite-mass plate settles continuously, not on the old
            // scripted stroke's deadline. Keep the same stability bound below.
            double Speed()=>Math.Abs(Guide(world,pump).Travel.Jacobian.Bind(
                Guide(world,pump).A,Guide(world,pump).B).Speed);
            // Zero velocity also occurs at an oscillation's turning point.
            // Keep the existing time budget and both original settling bounds;
            // require force-equilibrium displacement as well as low speed.
            for(var i=0;i<1200&&(Speed()>1e-5||Math.Abs(pump.Compression-equilibrium)>1e-4);i++)world.Step();
            Assert.InRange(Speed(),0,1e-5);
            Assert.True(Math.Abs(pump.Compression-equilibrium)<initialError);
            Assert.InRange(Math.Abs(pump.Compression-equilibrium),0,1e-4);
            var held=pump.Compression;
            for(var i=0;i<120;i++)world.Step();
            Assert.InRange(Math.Abs(pump.Compression-held),0,1e-4);
            var payload=world.PhysicsAssembly.Body(new(body,MachinePart.RootBody));
            world.Physics.ApplyImpulse(payload.Id,new(0,0,3),payload.Center);
            var refilled=false;
            for(var i=0;i<360;i++)
            {
                world.Step();CheckPlate(world,pump);
                if(pump.Phase==BellowsPhase.Refilling)Assert.Equal(0,pump.EmissionForce);
                refilled|=pump.Phase==BellowsPhase.Ready;
            }
            Assert.True(refilled);
            Assert.InRange(pump.Compression,0,1e-7);
            var plate=Guide(world,pump).A;
            var axis=Guide(world,pump).FrameB.Orientation.Apply(new(0,0,1));
            world.Physics.ApplyImpulse(plate.Id,-axis*3,plate.Center);
            for(var i=0;i<30;i++)world.Step();
            Assert.Equal(2,pump.StrokeCount);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(25f,40f,15f)]
    public void RealLocalTopImpactWorksInAllOrientations(float x,float y,float z)
    {
        var world=World(0);
        try
        {
            var pump=(BellowsPart)Add(world,Role.Pump,new(0,6,0),new(x,y,z));
            var up=pump.Basis.Y;
            var ball=Add(world,Role.Striker,pump.Position+up*1.1f);
            ball.InitialVelocity=-up*5;
            world.Start();
            var maximum=0f;var emitted=false;
            for(var i=0;i<60;i++)
            {
                world.Step();CheckPlate(world,pump);
                maximum=Math.Max(maximum,pump.Compression);
                var emission=pump.CreateAirflowSource(world)!.Value;
                var bodies=world.PhysicsAssembly.Bodies.ToArray().ToDictionary(body=>body.Id);
                if(emission.Supply.MechanicalPort.Bind(bodies,world.Physics.Joints.ToArray()).Speed>0)
                {
                    emitted=true;
                    Assert.True((pump.Basis*emission.Direction).IsEqualApprox(pump.Basis.X));
                }
            }
            Assert.Equal(1,pump.StrokeCount);Assert.True(maximum>.01f);Assert.True(emitted);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(.5f,1f)]
    [InlineData(1f,.1f)]
    [InlineData(2f,1f)]
    [InlineData(8f,4f)]
    public void PhysicalInputEnergyBoundsSpringCompressionAndBurst(float speed,float mass)
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var ball=Add(world,Role.Striker,pump.Position+Vector3.Up*1.05f,mass:mass);
            ball.InitialVelocity=Vector3.Down*speed;
            world.Start();
            var initialEnergy=Guide(world,pump).A.KineticEnergy+
                world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).KineticEnergy;
            var maximum=0d;
            for(var i=0;i<300;i++)
            {
                world.Step();CheckPlate(world,pump);
                maximum=Math.Max(maximum,pump.Compression);
                var potential=BellowsPart.ReturnPreload*pump.Compression+
                    .5*BellowsPart.ReturnStiffness*pump.Compression*pump.Compression;
                var kinetic=Guide(world,pump).A.KineticEnergy+
                    world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).KineticEnergy;
                Assert.InRange(potential+kinetic,0,initialEnergy*1.02+.001);
            }
            Assert.True(maximum>0);
            Assert.InRange(pump.EmittedImpulse,0,
                pump.ReadParameter(BellowsParameter.Force)*maximum/BellowsPart.ReferenceCompressionSpeed+.01);
            Assert.Empty(pump.ConnectionPorts);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(ContactFace.Top)]
    [InlineData(ContactFace.Side)]
    [InlineData(ContactFace.Underside)]
    [InlineData(ContactFace.Miss)]
    public void OnlyAnActualInwardPlateStrokePumps(ContactFace face)
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var (position,velocity)=face switch
            {
                ContactFace.Top=>(new Vector3(0,1.1f,0),Vector3.Down*4),
                ContactFace.Side=>(new Vector3(-1.2f,BellowsPart.RestHeight,0),Vector3.Right*4),
                ContactFace.Underside=>(new Vector3(0,.035f,0),Vector3.Up*4),
                ContactFace.Miss=>(new Vector3(0,1.1f,2),Vector3.Down*4),
                _=>throw new ArgumentOutOfRangeException(nameof(face))
            };
            var ball=Add(world,Role.Striker,pump.Position+position);ball.InitialVelocity=velocity;
            world.Start();for(var i=0;i<90;i++){world.Step();CheckPlate(world,pump);}
            Assert.Equal(face==ContactFace.Top,pump.EmittedImpulse>0);
            Assert.Equal(face==ContactFace.Top,pump.StrokeCount>0);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousPhysicalLoadsShareTheSamePlateAndCountOneStroke(bool reversed)
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var first=Add(world,reversed?Role.SecondStriker:Role.Striker,pump.Position+new Vector3(-.35f,1.1f,0));
            var second=Add(world,reversed?Role.Striker:Role.SecondStriker,pump.Position+new Vector3(.35f,1.1f,0));
            first.InitialVelocity=second.InitialVelocity=Vector3.Down*4;
            world.Start();
            var plate=Guide(world,pump).A;
            for(var i=0;i<90;i++)
            {
                world.Step();CheckPlate(world,pump);Assert.Same(plate,Guide(world,pump).A);
            }
            Assert.Equal(1,pump.StrokeCount);Assert.True(pump.EmittedImpulse>0);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(90)]
    [InlineData(150)]
    [InlineData(240)]
    public void ResetSaveReplayAndRenderCallsCannotChangePhysics(int steps)
    {
        var world=World();
        try
        {
            var pump=Pump(world);Add(world,Role.Striker,new(-4,9,0));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            string Run()
            {
                world.Start();pump=(BellowsPart)Find(world,Role.Pump);
                for(var i=0;i<steps;i++)world.Step();
                var states=world.Physics.Capture().BodyStates.ToArray();
                var compression=pump.Compression;var phase=pump.Phase;
                pump._Process(.5);
                Assert.Equal(compression,pump.Compression);Assert.Equal(phase,pump.Phase);
                Assert.Equal(states,world.Physics.Capture().BodyStates.ToArray());
                return world.StateSignature();
            }
            var result=Run();
            world.Restore();Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            pump=(BellowsPart)Find(world,Role.Pump);
            Assert.Equal(0,pump.Compression);Assert.Equal(BellowsPhase.Ready,pump.Phase);Assert.Equal(0,pump.EmissionForce);
            Assert.Equal(0,pump.EmittedImpulse);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(result,Run());
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AirBurstRingsChimesAndWallSuppressesIt(bool blocked)
    {
        var world=World();
        try
        {
            Pump(world);Add(world,Role.Striker,new(-4,9,0));
            var chimes=(WindChimesPart)Add(world,Role.Chimes,new(-1,6.48f,0));
            if(blocked)Add(world,Role.Wall,new(-2.5f,5.88f,0),new(0,90,0));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<480;i++)
            {
                try {world.Step();}
                catch
                {
                    var bodies=world.PhysicsAssembly.Bodies.ToArray().ToDictionary(body=>body.Id);
                    var colliders=bodies.Keys.ToDictionary(id=>id,id=>world.Physics.Collider(id).Declaration);
                    output.WriteLine($"Bellows/chime failure tick={world.Ticks}, step={world.Physics.StepIndex}, time={world.Physics.Time:R}");
                    foreach(var body in bodies.Values.Where(body=>body.MotionType==PhysicsMotionType.Dynamic))
                        output.WriteLine($"Body {body.Id.Index}: center={body.Center}, velocity={body.LinearVelocity}, momentum={body.AngularMomentum}");
                    foreach(var transfer in world.Physics.Loads.Transfers)
                        output.WriteLine($"Transfer {transfer.Id.Index}: source={transfer.Source.MechanicalPort.Bind(bodies,world.Physics.Joints.ToArray()).Speed:R}, receiver={transfer.Receiver.Bind(bodies,world.Physics.Joints.ToArray()).Speed:R}, exposed={transfer.Field!.IsExposed(bodies,colliders)}");
                    throw;
                }
            }
            Assert.Equal(!blocked,chimes.PulseCount>0);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.LoadMachine(JsonSerializer.Deserialize(construction,MachineJson.Default.MachineData)!);
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }

    [Fact]
    public void ASecondAuthoredLoadCanContactTheReturningPlateWithoutTeleporting()
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var body=Add(world,Role.Striker,pump.Position+Vector3.Up*1.5f);
            body.InitialVelocity=Vector3.Down*2;
            world.Start();
            var plate=Guide(world,pump).A;
            world.Physics.ApplyImpulse(plate.Id,new(0,-3,0),plate.Center);
            var payload=world.PhysicsAssembly.Body(new(body,MachinePart.RootBody));
            var returning=false;var touched=false;
            for(var i=0;i<180;i++)
            {
                world.Step();CheckPlate(world,pump);
                returning|=pump.Phase==BellowsPhase.Refilling;
                touched|=world.TickImpacts.ToArray().Any(hit=>hit.Pair.A==plate.Id&&hit.Pair.B==payload.Id||
                    hit.Pair.B==plate.Id&&hit.Pair.A==payload.Id);
                var plateTop=plate.Center.Y+.06;
                Assert.True(payload.Center.Y-body.Radius>=plateTop-1e-6);
            }
            Assert.True(returning);Assert.True(touched);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(BellowsParameter.Force,0f)]
    [InlineData(BellowsParameter.Force,41f)]
    [InlineData(BellowsParameter.Reach,-1f)]
    [InlineData(BellowsParameter.Width,5f)]
    [InlineData(BellowsParameter.Force,float.NaN)]
    public void InvalidParametersAreRejected(BellowsParameter key,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=WireId(Role.Pump),Kind=Catalog(Role.Pump),
                Properties=new(){[PartParameterName.Of(key)]=value}}));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(BellowsParameter.Force,"force")]
    [InlineData(BellowsParameter.Reach,"reach")]
    [InlineData(BellowsParameter.Width,"width")]
    public void ParameterBoundaryPreservesCanonicalNames(BellowsParameter key,string wire)=>Assert.Equal(wire,PartParameterName.Of(key));

    [Fact]
    public void UndefinedFixtureAndParameterChoicesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((BellowsParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>WireId((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog((Role)999));
    }
}
