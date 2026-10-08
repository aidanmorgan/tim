using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LinearPusherTests(NativeSceneFixture godot)
{
    public enum FixturePart { Pusher, Battery, Supply, Extend, Retract, Cargo, Wall, Home, End }
    private MachineWorld World()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    // Explicit fixture serialization/instance boundaries. Internal choices remain typed.
    private static string InstanceId(FixturePart part)=>part switch
    {
        FixturePart.Pusher=>"pusher",FixturePart.Battery=>"battery",FixturePart.Supply=>"supply",
        FixturePart.Extend=>"extend",FixturePart.Retract=>"retract",FixturePart.Cargo=>"cargo",
        FixturePart.Wall=>"wall",FixturePart.Home=>"home",FixturePart.End=>"end",
        _=>throw new ArgumentOutOfRangeException(nameof(part))
    };
    private static string Catalogue(FixturePart part)=>part switch
    {
        FixturePart.Pusher=>LinearPusherPart.CatalogId,FixturePart.Battery=>"battery",
        FixturePart.Cargo=>"ball",FixturePart.Wall=>"wall",
        FixturePart.Supply or FixturePart.Extend or FixturePart.Retract or FixturePart.Home or FixturePart.End=>"switch",
        _=>throw new ArgumentOutOfRangeException(nameof(part))
    };
    private static MachinePart Find(MachineWorld world,FixturePart part)=>
        world.FindPart(InstanceId(part))??throw new InvalidOperationException("Missing fixture part.");
    private static MachinePart Add(MachineWorld world,FixturePart part,Vector3 at)=>
        world.AddPart(new(){Id=InstanceId(part),Kind=Catalogue(part),Position=[at.X,at.Y,at.Z]});
    private static void Wire(MachineWorld world,MachinePart from,MachinePart to,SocketId input,SocketId output=SocketId.Supply)=>
        Assert.True(world.Connect(from,output,to,input,ConnectionDomain.Electrical));
    private static (LinearPusherPart Pusher,MachinePart Supply,MachinePart Extend,MachinePart Retract) Circuit(MachineWorld world)
    {
        var pusher=(LinearPusherPart)Add(world,FixturePart.Pusher,new(0,4,0));
        var battery=Add(world,FixturePart.Battery,new(-6,1,0));
        var supply=Add(world,FixturePart.Supply,new(-6,3,0));
        var extend=Add(world,FixturePart.Extend,new(-6,5,0));
        var retract=Add(world,FixturePart.Retract,new(-6,7,0));
        foreach(var part in new[]{supply,extend,retract})Wire(world,battery,part,SocketId.PowerIn);
        Wire(world,supply,pusher,SocketId.PowerIn);Wire(world,extend,pusher,SocketId.ExtendIn);Wire(world,retract,pusher,SocketId.RetractIn);
        return(pusher,supply,extend,retract);
    }
    private static void Steps(MachineWorld world,int count){for(var i=0;i<count;i++)world.Step();}

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PresentationCannotRewriteShaftCollisionGeometry(bool paused)
    {
        var world=World();
        try
        {
            var pusher=(LinearPusherPart)Add(world,FixturePart.Pusher,new(0,4,0));
            var construction=pusher.Transform;
            var boxes=pusher.Boxes.ToArray();
            world.Start();
            world.Running=!paused;
            var root=world.PhysicsAssembly.Body(new(pusher,MachinePart.RootBody));
            var head=world.PhysicsAssembly.Body(new(pusher,LinearPusherPart.HeadBody));
            var rootPose=root.Pose;
            var key=new SceneBodyKey(pusher,MachinePart.RootBody);
            var originalGeometry=world.CollisionGeometry(key);
            var heldPose=head.Pose;
            world.Physics.ApplyImpulse(head.Id,new(LinearPusherPart.HeadMass,0,0),head.Center);
            world.Physics.Step([],[],.25);
            Assert.Equal(heldPose,head.Pose);
            Assert.Same(originalGeometry,world.CollisionGeometry(key));
            world.Physics.SetServoMode(world.PhysicsAssembly.JointId(new(pusher,LinearPusherPart.HeadGuide)),
                PhysicsServoMode.Upper);
            world.Physics.Step([],[],.25);
            Assert.True(head.Pose.Center.X>heldPose.Center.X);
            var headPose=head.Pose;
            // Shared-world stepping and presentation must not update collision inputs.
            pusher.Position+=new Vector3(3,1,2);
            pusher.RotateY(.4f);
            FixtureParts.PresentCaptured(world);
            Assert.Equal(boxes,pusher.Boxes.ToArray());
            pusher.Position+=new Vector3(3,1,2);
            pusher.RotateY(.4f);
            pusher.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(boxes,pusher.Boxes.ToArray());
            Assert.NotSame(originalGeometry,world.CollisionGeometry(key));
            var observed=world.CollisionGeometry(key);
            FixtureParts.PresentCaptured(world);
            pusher.ObservePhysics(world,MachineWorld.Tick);
            Assert.Same(observed,world.CollisionGeometry(key));
            Assert.Equal(rootPose,root.Pose);
            Assert.Equal(headPose,head.Pose);
            world.Restore();
            pusher=(LinearPusherPart)Find(world,FixturePart.Pusher);
            Assert.Equal(construction,pusher.Transform);
            Assert.Equal(boxes,pusher.Boxes.ToArray());
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false,true,false,PusherPhase.Unpowered)]
    [InlineData(true,false,false,PusherPhase.Holding)]
    [InlineData(true,true,true,PusherPhase.Conflict)]
    [InlineData(true,false,true,PusherPhase.Holding)]
    public void IndependentSupplyAndExclusiveCommandAreRequired(bool power,bool extend,bool retract,PusherPhase phase)
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();
            c.Supply.Active=power;c.Extend.Active=extend;c.Retract.Active=retract;
            Steps(world,120);
            Assert.Equal(0,c.Pusher.Extension);Assert.Equal(phase,c.Pusher.Phase);
            Assert.Equal(0,c.Pusher.DeliveredWork);
        }
        finally{world.Free();}
    }

    [Fact]
    public void FullStrokeReversalAndSuppliedEndOutputsRestoreExactly()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var atHome=Add(world,FixturePart.Home,new(5,2,0));
            var atEnd=Add(world,FixturePart.End,new(5,6,0));
            Wire(world,c.Pusher,atHome,SocketId.PowerIn,SocketId.RetractedOut);
            Wire(world,c.Pusher,atEnd,SocketId.PowerIn,SocketId.ExtendedOut);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;world.Step();
            Assert.True(atHome.HasElectricalPower(SocketId.PowerIn));Assert.False(atEnd.HasElectricalPower(SocketId.PowerIn));
            c.Extend.Active=true;Steps(world,300);
            Assert.True(c.Pusher.Extended);Assert.False(c.Pusher.Retracted);
            Assert.True(atEnd.HasElectricalPower(SocketId.PowerIn));Assert.False(atHome.HasElectricalPower(SocketId.PowerIn));
            c.Supply.Active=false;world.Step();Assert.False(atEnd.HasElectricalPower(SocketId.PowerIn));
            c.Supply.Active=true;c.Extend.Active=false;c.Retract.Active=true;Steps(world,300);
            Assert.True(c.Pusher.Retracted);Assert.True(atHome.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,((LinearPusherPart)Find(world,FixturePart.Pusher)).DeliveredWork);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterruptedSupplyOrConflictBrakesAtExactMidStroke(bool conflict)
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,40);
            var extension=c.Pusher.Extension;Assert.InRange(extension,.1f,1);
            if(conflict)c.Retract.Active=true;else c.Supply.Active=false;
            Steps(world,120);
            Assert.Equal(extension,c.Pusher.Extension);Assert.Equal(0,c.Pusher.TravelSpeed);
            Assert.Equal(conflict?PusherPhase.Conflict:PusherPhase.Unpowered,c.Pusher.Phase);
            c.Retract.Active=false;c.Supply.Active=true;Steps(world,40);Assert.True(c.Pusher.Extension>extension);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(90f)]
    [InlineData(180f)]
    public void PushesActualCargoWithBoundedImpulseAndWork(float angle)
    {
        var world=World();
        try
        {
            var c=Circuit(world);c.Pusher.RotationDegrees=new(0,angle,0);
            var axis=c.Pusher.Basis.X;
            var cargo=Add(world,FixturePart.Cargo,c.Pusher.Transform*new Vector3(1.55f,0,0));
            var start=cargo.Position;
            world.Start();c.Supply.Active=true;c.Extend.Active=true;
            var previousWork=0d;
            for(var i=0;i<180;i++)
            {
                world.Step();
                Assert.InRange(c.Pusher.LastDriveImpulse,0,40f/480+.0001f);
                Assert.InRange(c.Pusher.DeliveredWork-previousWork,0,40f*1.5f/120+.001f);
                previousWork=c.Pusher.DeliveredWork;
            }
            Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).Center-SceneGeometryAdapter.CaptureVector(start),SceneGeometryAdapter.CaptureVector(axis))>1);
            Assert.True(c.Pusher.DeliveredWork>0);
            Assert.InRange(world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).LinearVelocity.Length,0,1.501f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void PinnedCargoDoesNotLetHeadPassThroughWall()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var cargo=Add(world,FixturePart.Cargo,new(1.55f,4,0));
            var wall=Add(world,FixturePart.Wall,new(2.1f,4,0));
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1), MachinePart.RootBody));
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,360);
            Assert.True(c.Pusher.Extension<.35f);
            var cargoCenter=world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).Center;
            Assert.True(cargoCenter.X+cargo.Radius<=2.091f);
            Assert.True(c.Pusher.HeadPosition.X+LinearPusherPart.HeadRadius<=cargoCenter.X-cargo.Radius+.001f);
            Assert.False(c.Pusher.Extended);
        }
        finally{world.Free();}
    }

    [Fact]
    public void MidStrokeResetRestoresOriginalConstructionAndClearsMotion()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,40);
            Assert.True(c.Pusher.Extension>0);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=(LinearPusherPart)Find(world,FixturePart.Pusher);
            Assert.Equal(0,restored.Extension);Assert.Equal(0,restored.TravelSpeed);
            Assert.Equal(0,restored.DeliveredWork);Assert.True(restored.Retracted);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(PusherParameter.Stroke,0f)]
    [InlineData(PusherParameter.Stroke,4f)]
    [InlineData(PusherParameter.Speed,float.NaN)]
    [InlineData(PusherParameter.Speed,5f)]
    [InlineData(PusherParameter.Acceleration,0f)]
    [InlineData(PusherParameter.Acceleration,31f)]
    [InlineData(PusherParameter.Force,0f)]
    [InlineData(PusherParameter.Force,float.PositiveInfinity)]
    public void InvalidAuthorParametersAreRejected(PusherParameter parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=InstanceId(FixturePart.Pusher),Kind=LinearPusherPart.CatalogId,
                Properties=new(){[PartParameterName.Of(parameter)]=value}}));
        }
        finally{world.Free();}
    }

    [Fact]
    public void FreeMotionAccelerationIsBoundedThroughMidStrokeReversal()
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();c.Supply.Active=true;c.Extend.Active=true;
            var previous=0f;
            for(var i=0;i<60;i++)
            {
                if(i==30){c.Extend.Active=false;c.Retract.Active=true;}
                world.Step();
                Assert.InRange(Mathf.Abs(c.Pusher.TravelSpeed-previous),0,12f/120+.0001f);
                previous=c.Pusher.TravelSpeed;
            }
            Assert.True(c.Pusher.TravelSpeed<0);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(1f,false)]
    [InlineData(40f,true)]
    public void VerticalDriveRespectsAvailableForceAgainstGravity(float force,bool lifts)
    {
        var world=World();
        try
        {
            world.Gravity=9.81f;
            var c=Circuit(world);c.Pusher.RotationDegrees=new(0,0,90);
            FixtureParts.ConfigureParameter(c.Pusher,PusherParameter.Force,force);
            var cargo=Add(world,FixturePart.Cargo,c.Pusher.HeadPosition+Vector3.Up*.6f);
            var initial=cargo.Position.Y;
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,180);
            var height=world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).Center.Y;
            if(lifts)Assert.True(height>initial+.5f,
                $"Cargo {height}, initial {initial}, extension {c.Pusher.Extension}, phase {c.Pusher.Phase}, speed {world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).LinearVelocity.Y}, work {c.Pusher.DeliveredWork}");
            else Assert.True(height<initial+.1f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(4f)]
    [InlineData(8f)]
    public void HeavyCargoAcceleratesWithoutExceedingForceBudget(float mass)
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var cargo=world.AddPart(new(){Id=InstanceId(FixturePart.Cargo),Kind=Catalogue(FixturePart.Cargo),Position=[1.55f,4,0],
                Properties=new(){[PartParameterName.Of(WeightParameter.Mass)]=mass}});
            world.Start();c.Supply.Active=true;c.Extend.Active=true;
            for(var i=0;i<240;i++)
            {
                world.Step();
                Assert.InRange(c.Pusher.LastDriveImpulse,0,40f/480+.0001f);
            }
            Assert.True(world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).Center.X>2.55f);
            Assert.InRange(.5f*mass*world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody)).LinearVelocity.LengthSquared,0,c.Pusher.DeliveredWork+.01f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void InitiallyOverlappingHeadRejectsConstructionBeforeRunning()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var wall=Add(world,FixturePart.Wall,c.Pusher.HeadPosition);
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1), MachinePart.RootBody));
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(0,c.Pusher.Extension);
            Assert.Equal(0,c.Pusher.DeliveredWork);
        }
        finally{world.Free();}
    }

    [Fact]
    public void CurrentJsonReplayReproducesStrokeAndCargoExactly()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            Add(world,FixturePart.Cargo,new(1.55f,4,0));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,80);
            var extension=c.Pusher.Extension;var work=c.Pusher.DeliveredWork;
            var position=world.PhysicsAssembly.Body(new(Find(world,FixturePart.Cargo),MachinePart.RootBody)).Pose;
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();Find(world,FixturePart.Supply).Active=true;Find(world,FixturePart.Extend).Active=true;Steps(world,80);
            var replay=(LinearPusherPart)Find(world,FixturePart.Pusher);
            Assert.Equal(extension,replay.Extension);Assert.Equal(work,replay.DeliveredWork);
            Assert.Equal(position,world.PhysicsAssembly.Body(new(Find(world,FixturePart.Cargo),MachinePart.RootBody)).Pose);
        }
        finally{world.Free();}
    }

    [Fact]
    public void HeadOwnershipAndLockReplacementRemainInsidePersistentSharedWorld()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var engine=world.Physics;
            var head=world.PhysicsAssembly.Body(new(c.Pusher,LinearPusherPart.HeadBody));
            Assert.Equal(PhysicsMotionType.Dynamic,head.MotionType);
            Assert.Equal(LinearPusherPart.HeadMass,1/head.InverseMass);
            var shape=world.Physics.Collider(head.Id).Declaration.Geometry;
            Assert.Equal(1,shape.Count);
            Assert.IsType<ConvexSphere>(shape[new(0)].Geometry);
            world.Step();
            var locked=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(c.Pusher,LinearPusherPart.HeadGuide)));
            Assert.Equal(locked.TravelRange!.Lower,locked.TravelRange.Upper);
            Assert.Same(head,locked.A);
            c.Supply.Active=true;c.Extend.Active=true;
            Steps(world,40);
            var released=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(c.Pusher,LinearPusherPart.HeadGuide)));
            Assert.Equal(locked.Id,released.Id);
            Assert.NotSame(locked,released);
            Assert.Same(head,released.A);
            Assert.Same(engine,world.Physics);
            Assert.Equal(0,released.TravelRange!.Lower);
            Assert.Equal(c.Pusher.ReadParameter(PusherParameter.Stroke),released.TravelRange.Upper);
            Assert.True(c.Pusher.Extension>0);
            Assert.InRange((SceneGeometryAdapter.CaptureVector(c.Pusher.HeadPosition)-head.Center).Length,0,1e-6);
            Assert.Same(shape,world.Physics.Collider(head.Id).Declaration.Geometry);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(PusherParameter.Stroke,"stroke")]
    [InlineData(PusherParameter.Speed,"speed")]
    [InlineData(PusherParameter.Acceleration,"acceleration")]
    [InlineData(PusherParameter.Force,"force")]
    public void ParameterBoundaryIsCanonical(PusherParameter parameter,string serialized)
        =>Assert.Equal(serialized,PartParameterName.Of(parameter));

    [Fact]
    public void UndefinedBoundaryChoicesReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((PusherParameter)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>InstanceId((FixturePart)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalogue((FixturePart)999));
    }

    [Fact]
    public void ThinStaticObstructionStopsHeadWithoutClipping()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var wall=Add(world,FixturePart.Wall,new(2,4,0));
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1), MachinePart.RootBody));
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,240);
            Assert.Equal(PusherPhase.Blocked,c.Pusher.Phase);
            Assert.InRange(c.Pusher.Extension,.889f,.891f);
            // The finite head must receive work to accelerate before reaching the stop.
            Assert.True(c.Pusher.DeliveredWork>0);
            var stopped=c.Pusher.HeadPosition;
            var before=c.Pusher.DeliveredWork;
            Steps(world,120);
            Assert.Equal(stopped,c.Pusher.HeadPosition);
            Assert.InRange(c.Pusher.DeliveredWork-before,0,1e-6f);
        }
        finally{world.Free();}
    }
}
