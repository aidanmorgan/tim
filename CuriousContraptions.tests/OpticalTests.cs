using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OpticalTests(NativeSceneFixture godot)
{
    private enum Role { Laser, Receiver, Near, Far, Blocker }
    private static string InstanceBoundary(Role role)=>role switch
    {
        Role.Laser=>"laser",Role.Receiver=>"receiver",Role.Near=>"near",Role.Far=>"far",Role.Blocker=>"blocker",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static T Find<T>(MachineWorld world,Role role) where T:MachinePart=>
        Assert.IsType<T>(world.FindPart(InstanceBoundary(role)));
    private static void RunLaser(MachineWorld world)
    {
        world.Start();world.Activate(Find<LaserPart>(world,Role.Laser));world.Step();world.Step();
    }
    public enum Occluder { Wall, Ball, Pipe }
    private static string OccluderCatalog(Occluder kind)=>kind switch
    {
        Occluder.Wall=>"wall",Occluder.Ball=>"ball",Occluder.Pipe=>"pipe",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static PhysicsBody Body(MachineWorld world,MachinePart part)=>
        world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));

    [Fact]
    public void FixtureBoundariesAreCanonicalAndRejectUndefinedChoices()
    {
        Assert.Equal(new[]{"laser","receiver","near","far","blocker"},Enum.GetValues<Role>().Select(InstanceBoundary));
        Assert.Equal(new[]{"wall","ball","pipe"},Enum.GetValues<Occluder>().Select(OccluderCatalog));
        Assert.Throws<ArgumentOutOfRangeException>(()=>InstanceBoundary((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>OccluderCatalog((Occluder)999));
    }

    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData(false,false,false)]
    [InlineData(false,false,true)]
    [InlineData(false,true,false)]
    [InlineData(false,true,true)]
    [InlineData(true,false,false)]
    [InlineData(true,false,true)]
    [InlineData(true,true,false)]
    [InlineData(true,true,true)]
    public void EnableAndRealSupplyAreRequiredIndependently(bool enabled,bool sourcePower,bool receiverPower)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,4,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[3,4,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[0,8,0]});
            var load=world.AddPart(new(){Id="load",Kind="powered_gate",Position=[6,4,0]});
            var supply=new SupplyControl(world,battery);
            if(sourcePower)Assert.True(world.Connect(supply.Output,laser));
            if(receiverPower)Assert.True(world.Connect(battery,receiver));
            Assert.True(world.Connect(receiver,load));
            world.Start();
            if(enabled)world.Activate(laser);
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.False(receiver.Active); // Optical snapshot precedes the contact-closing electrical solve.
            world.Step();
            Assert.Equal(enabled&&sourcePower,receiver.Active);
            Assert.Equal(enabled&&sourcePower&&receiverPower,load.HasElectricalPower(SocketId.PowerIn));
            if(receiver.Active)
            {
                Assert.Equal(LaserPart.BeamPower,receiver.ReceivedPower);
                Assert.InRange(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)),5.09f,5.11f);
            }
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.Equal(enabled&&sourcePower,receiver.Active); // This optical snapshot still saw the previous electrical state.
            world.Step();
            Assert.False(receiver.Active);
            Assert.Equal(0,laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)));
            world.Restore();
            laser=(LaserPart)world.FindPart("laser")!;
            receiver=(LightReceiverPart)world.FindPart("receiver")!;
            Assert.False(laser.Enabled);
            Assert.Equal(0,laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)));
            Assert.Equal(Vector3.Zero,receiver.ReceivedPower);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(25f,35f,-20f)]
    [InlineData(0f,0f,90f)]
    public void ApertureAndBeamFollowFullThreeDimensionalTransforms(float x,float y,float z)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[0,7,0],Orientation = PartOrientation.FromEulerDegrees(x,y,z)});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Orientation = PartOrientation.FromEulerDegrees(x,y,z)});
            receiver.Position=laser.Position+laser.Basis.X*5;
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(receiver.Active);
            var body=Body(world,receiver);
            var initial=body.Snapshot();
            var initialPose=initial.Pose.ToScene();
            receiver.RotateObjectLocal(Vector3.Up,Mathf.Pi);
            world.Step();
            Assert.True(receiver.Active); // Presentation rotation cannot change the captured aperture.
            var reversed=initialPose*new Transform3D(new Basis(Vector3.Up,Mathf.Pi),Vector3.Zero);
            world.Restore();
            receiver=Find<LightReceiverPart>(world,Role.Receiver);
            receiver.Transform=reversed;
            RunLaser(world);
            Assert.False(receiver.Active); // Authored opaque back cannot detect.
            world.Restore();
            receiver=Find<LightReceiverPart>(world,Role.Receiver);
            receiver.Transform=new(initialPose.Basis,initialPose.Origin+initialPose.Basis.Y);
            RunLaser(world);
            Assert.False(receiver.Active); // Finite disc, not an infinite target plane.
            world.Restore();
            receiver=Find<LightReceiverPart>(world,Role.Receiver);
            receiver.Transform=initialPose;
            RunLaser(world);
            Assert.True(receiver.Active);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(Occluder.Wall)]
    [InlineData(Occluder.Ball)]
    [InlineData(Occluder.Pipe)]
    public void SharedOpaqueGeometryStopsTheBeamAndRemovalRestoresIt(Occluder kind)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,5,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[3,5,0]});
            // A pipe's centre bore transmits; placing its opaque collar at the ray blocks it.
            var blocker=world.AddPart(new(){Id="blocker",Kind=OccluderCatalog(kind),Position=[0,kind==Occluder.Pipe?4.3f:5,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.False(receiver.Active);
            Assert.True(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To))<5);
            var body=Body(world,blocker);
            var initial=body.Snapshot();
            blocker.Position+=Vector3.Up*3;
            world.Step();
            Assert.False(receiver.Active); // Presentation cannot move captured opaque geometry.
            world.Restore();
            blocker=world.FindPart(InstanceBoundary(Role.Blocker))!;
            blocker.Transform=initial.Pose.ToScene().Translated(Vector3.Up*3);
            RunLaser(world);
            Assert.True(Find<LightReceiverPart>(world,Role.Receiver).Active);
            world.Restore();
            blocker=world.FindPart(InstanceBoundary(Role.Blocker))!;
            blocker.Transform=initial.Pose.ToScene();
            RunLaser(world);
            Assert.False(Find<LightReceiverPart>(world,Role.Receiver).Active);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RangeAndNearestAbsorbingTargetPreventDoubleCounting()
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[0,5,0]});
            var near=(LightReceiverPart)world.AddPart(new(){Id="near",Kind="light_receiver",Position=[4,5,0]});
            var far=(LightReceiverPart)world.AddPart(new(){Id="far",Kind="light_receiver",Position=[8,5,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(near.Active);Assert.False(far.Active);
            near.Visible=false;world.Step();
            Assert.True(near.Active);Assert.False(far.Active); // Presentation visibility is not participation.
            var nearBody=Body(world,near);
            var collider=world.Physics.Collider(nearBody.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(nearBody.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            world.Step();
            Assert.False(near.Active);Assert.True(far.Active);
            far.Position=new(18,5,0);world.Step();
            Assert.True(far.Active); // Runtime range uses the owned pose.
            world.Restore();
            far=Find<LightReceiverPart>(world,Role.Far);
            far.Position=new(18,5,0);
            world.Start();world.Activate(Find<LaserPart>(world,Role.Laser));
            nearBody=Body(world,Find<LightReceiverPart>(world,Role.Near));
            collider=world.Physics.Collider(nearBody.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(nearBody.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            world.Step();world.Step();
            Assert.False(far.Active);
            laser=Find<LaserPart>(world,Role.Laser);
            Assert.InRange(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)),LaserPart.Range-.00001f,LaserPart.Range+.00001f);
        }
        finally{world.Free();}
    }
}
