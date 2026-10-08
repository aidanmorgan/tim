using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneRopeBindingTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world); return world;
    }
    private enum Fixture { LeftLoad, RightLoad, LeftGuide, RightGuide, Anchor, OtherAnchor }
    private static MachinePart Add(MachineWorld world,Fixture fixture,float mass=1)
    {
        // Explicit PartSpec serialization boundary; choices remain typed until here.
        var (id,kind,at)=fixture switch
        {
            Fixture.LeftLoad=>("left_load","weight",new Vector3(-2,3,.12f)),
            Fixture.RightLoad=>("right_load","weight",new Vector3(2,3,.12f)),
            Fixture.LeftGuide=>("left_guide","pulley",new Vector3(-2,7,0)),
            Fixture.RightGuide=>("right_guide","pulley",new Vector3(2,7,0)),
            Fixture.Anchor=>("anchor","rope_anchor",new Vector3(0,8,0)),
            Fixture.OtherAnchor=>("other_anchor","rope_anchor",new Vector3(3,8,0)),
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        };
        return world.AddPart(new(){Id=id,Kind=kind,Position=[at.X,at.Y,at.Z],
            Properties=fixture is Fixture.LeftLoad or Fixture.RightLoad
                ?new(){[PartParameterName.Of(WeightParameter.Mass)]=mass}:new()});
    }
    private static PhysicsWorld Physics(ScenePhysicsAssembly assembly,CollisionVector gravity)
    {
        var objects=assembly.Objects.ToArray();
        return new([],objects,assembly.InitialJoints.ToArray(),new(gravity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturedRoutesAreReadOnlyAndSharePhysicsIdentityAcrossRunAndReset(bool paused)
    {
        var scene=World();
        try
        {
            var view=scene.Ropes;
            var anchor=Add(scene,Fixture.Anchor);
            var load=Add(scene,Fixture.LeftLoad);
            Assert.True(scene.Connect(anchor,load));
            Assert.Empty(view);
            scene.Start();
            if(paused)scene.Running=false;
            var route=Assert.Single(view);
            var joint=Assert.IsType<PhysicsRopeJoint>(scene.CurrentJoint(new(route.Sockets[0].Part,route.ConstraintSlot)));
            Assert.Equal(route.Length,joint.MaximumLength);
            Assert.Equal(route.Sockets.Count,joint.Route.Anchors.Length);
            var collection=Assert.IsAssignableFrom<ICollection<RopePath>>(view);
            var indexed=Assert.IsAssignableFrom<IList<RopePath>>(view);
            Assert.True(collection.IsReadOnly);
            var physics=scene.Physics;
            Assert.Throws<NotSupportedException>(()=>collection.Clear());
            Assert.Throws<NotSupportedException>(()=>collection.Add(route));
            Assert.Throws<NotSupportedException>(()=>collection.Remove(route));
            Assert.Throws<NotSupportedException>(()=>indexed[0]=route);
            Assert.Same(physics,scene.Physics);
            Assert.Same(route,Assert.Single(view));
            Assert.Same(joint,scene.CurrentJoint(new(route.Sockets[0].Part,route.ConstraintSlot)));
            scene.Restore();
            Assert.Same(view,scene.Ropes);
            Assert.Empty(view);
            scene.Start();
            var restored=Assert.Single(view);
            Assert.NotSame(route,restored);
            Assert.Equal(route.Length,restored.Length);
            Assert.NotSame(route.ConstraintSlot,restored.ConstraintSlot);
            Assert.IsType<PhysicsRopeJoint>(scene.CurrentJoint(new(restored.Sockets[0].Part,restored.ConstraintSlot)));
            scene.Restore();
            Assert.True(scene.Disconnect(Assert.Single(scene.Connections)));
            scene.Start();
            Assert.Empty(view);
            Assert.Empty(scene.Physics.Joints.ToArray());
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(1,4,false)]
    [InlineData(4,1,false)]
    [InlineData(1,1,false)]
    [InlineData(1,4,true)]
    public void AuthoredPulleyRouteTransfersTensionAccordingToMassAndReplays(float leftMass,float rightMass,bool reversed)
    {
        var scene=World();
        try
        {
            var a=Add(scene,Fixture.LeftLoad,leftMass); var b=Add(scene,Fixture.RightLoad,rightMass);
            var left=Add(scene,Fixture.LeftGuide); var right=Add(scene,Fixture.RightGuide);
            Assert.True(scene.Connect(a,left)); Assert.True(scene.Connect(left,right)); Assert.True(scene.Connect(right,b));
            if(reversed)
            {
                var construction=scene.Snapshot();
                construction.Connections=construction.Connections.Select(link=>link with
                    {From=link.To,To=link.From,FromPort=link.ToPort,ToPort=link.FromPort}).Reverse().ToList();
                var ids=new[]{a.Uid,b.Uid,left.Uid,right.Uid};
                scene.LoadMachine(construction);
                a=scene.FindPart(ids[0])!;b=scene.FindPart(ids[1])!;
                left=scene.FindPart(ids[2])!;right=scene.FindPart(ids[3])!;
            }
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var rope=Assert.IsType<PhysicsRopeJoint>(Assert.Single(assembly.InitialJoints.ToArray()));
            Assert.Equal(4,rope.Route.Anchors.Length);
            Assert.Equal(ConnectedBodyCollision.Enabled,rope.Collision);
            var actual=rope.Route.Anchors.ToArray();
            var parts=new[]{a,left,right,b};
            for(var i=0;i<parts.Length;i++)
            {
                Assert.Same(assembly.Body(new(parts[i],MachinePart.RootBody)),actual[i].Body);
                Assert.Equal(SceneGeometryAdapter.CaptureVector(parts[i].ConnectionPorts.Single(p=>p.Id==SocketId.Tie).LocalPosition),
                    actual[i].LocalPosition);
            }
            var first=assembly.Body(new(a,MachinePart.RootBody)); var last=assembly.Body(new(b,MachinePart.RootBody));
            var world=Physics(assembly,new(0,-9.81,0)); var initial=world.Capture();
            void Run()
            {
                for(var i=0;i<30;i++)
                {
                    world.Step([],[],1.0/120);
                    Assert.InRange(rope.Error(1e-8),0,1.01e-7);
                }
            }
            Run();
            var acceleration=9.81*(rightMass-leftMass)/(leftMass+rightMass);
            Assert.InRange(Math.Abs(first.LinearVelocity.Y-acceleration*.25),0,1e-5);
            Assert.InRange(Math.Abs(last.LinearVelocity.Y+acceleration*.25),0,1e-5);
            Assert.InRange(Math.Abs(first.Center.Y+last.Center.Y-6),0,1e-6);
            var final=world.Capture();
            world.Restore(initial); Run();
            Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(3,a.Position.Y); // Analytic velocities above come from shared motion without presentation.
            Assert.Equal(0,((PulleyPart)left).WheelAngle); // No old rope response or animation was invoked.
        }
        finally {scene.Free();}
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TautAnchorStopsOutwardTravelWhileSlackShorteningStaysFree(bool taut)
    {
        var scene=World();
        try
        {
            var anchor=Add(scene,Fixture.Anchor); var load=Add(scene,Fixture.LeftLoad);
            load.Position=new(0,4,.18f);
            Assert.True(scene.Connect(anchor,load));
            if(!taut)
            {
                var construction=scene.Snapshot();
                construction.Connections[0]=construction.Connections[0] with
                    {RopeLength=construction.Connections[0].RopeLength+2};
                var loadId=load.Uid;
                scene.LoadMachine(construction);
                load=scene.FindPart(loadId)!;
            }
            load.InitialVelocity=taut?Vector3.Down*5:Vector3.Up*2;
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var body=assembly.Body(new(load,MachinePart.RootBody));
            var rope=Assert.IsType<PhysicsRopeJoint>(Assert.Single(assembly.InitialJoints.ToArray()));
            var world=Physics(assembly,default);
            world.Step([],[],.1);
            if(taut)
            {
                Assert.InRange(body.LinearVelocity.Length,0,1e-7);
                Assert.InRange(rope.Error(1e-8),0,1.01e-7);
            }
            else
            {
                Assert.Equal(new CollisionVector(0,2,0),body.LinearVelocity);
                Assert.InRange(body.Center.Y,4.1999999,4.2000001);
                Assert.True(rope.Route.CurrentLength<rope.MaximumLength-1);
            }
        }
        finally {scene.Free();}
    }

    [Fact]
    public void UntiedGuideEndCannotHoldALoad()
    {
        var scene=World();
        try
        {
            var load=Add(scene,Fixture.LeftLoad); var guide=Add(scene,Fixture.LeftGuide);
            Assert.True(scene.Connect(load,guide));
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            Assert.Empty(assembly.InitialJoints.ToArray());
            var body=assembly.Body(new(load,MachinePart.RootBody));
            Physics(assembly,new(0,-9.81,0)).Step([],[],.1);
            Assert.InRange(body.LinearVelocity.Y,-.98100001,-.98099999);
        }
        finally {scene.Free();}
    }

    [Fact]
    public void FixedRoutesValidateAuthoredLengthAndDoNotInventDynamicBodies()
    {
        var scene=World();
        try
        {
            var a=Add(scene,Fixture.Anchor); var b=Add(scene,Fixture.OtherAnchor);
            Assert.True(scene.Connect(a,b));
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            Assert.Empty(assembly.InitialJoints.ToArray());
            Assert.All(assembly.Bodies.ToArray(),body=>Assert.Equal(PhysicsMotionType.Static,body.MotionType));
            var invalid=scene.Snapshot();
            invalid.Connections[0]=invalid.Connections[0] with {RopeLength=1};
            scene.LoadMachine(invalid);
            Assert.Throws<ArgumentException>(()=>WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections)));
        }
        finally {scene.Free();}
    }

    [Fact]
    public void RopeConnectionDoesNotSuppressLoadLoadCollision()
    {
        var scene=World();
        try
        {
            var a=Add(scene,Fixture.LeftLoad); var b=Add(scene,Fixture.RightLoad);
            a.Position=new(-1,5,0); b.Position=new(1,5,0);
            Assert.True(scene.Connect(a,b));
            var construction=scene.Snapshot();
            construction.Connections[0]=construction.Connections[0] with {RopeLength=5};
            var firstId=a.Uid;var lastId=b.Uid;
            scene.LoadMachine(construction);
            a=scene.FindPart(firstId)!;b=scene.FindPart(lastId)!;
            a.InitialVelocity=Vector3.Right*10;b.InitialVelocity=Vector3.Left*10;
            var assembly=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var first=assembly.Body(new(a,MachinePart.RootBody)); var last=assembly.Body(new(b,MachinePart.RootBody));
            var world=Physics(assembly,default);
            world.Step([],[],.2);
            var impact=Assert.Single(world.Impacts.ToArray(),hit=>hit.Pair.A==first.Id&&hit.Pair.B==last.Id||
                hit.Pair.B==first.Id&&hit.Pair.A==last.Id);
            // Equal masses exchange approach velocities scaled by both authored
            // materials. The slack rope must not suppress contact or this rebound.
            var restitution=a.InitialContactMaterial.Restitution*b.InitialContactMaterial.Restitution;
            Assert.InRange(Math.Abs(first.LinearVelocity.X+10*restitution),0,1e-7);
            Assert.InRange(Math.Abs(last.LinearVelocity.X-10*restitution),0,1e-7);
            var gap=(first.Center-last.Center).Length-a.Radius-b.Radius;
            var expected=ConvexSweep.ContactDistance+20*restitution*(.2-impact.Time);
            Assert.InRange(Math.Abs(gap-expected),0,1e-6);
            Assert.InRange((first.LinearVelocity+last.LinearVelocity).Length,0,1e-7);
        }
        finally {scene.Free();}
    }

    [Fact]
    public void ConstructionResetRebuildsTheSameBodiesMassesAndOrderedRoute()
    {
        var scene=World();
        try
        {
            var a=Add(scene,Fixture.LeftLoad); var b=Add(scene,Fixture.RightLoad,4);
            var left=Add(scene,Fixture.LeftGuide); var right=Add(scene,Fixture.RightGuide);
            Assert.True(scene.Connect(a,left)); Assert.True(scene.Connect(left,right)); Assert.True(scene.Connect(right,b));
            var before=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var rope=Assert.IsType<PhysicsRopeJoint>(Assert.Single(before.InitialJoints.ToArray()));
            scene.Start(); scene.Restore();
            var after=WorldGeometry.CapturePhysicsAssembly(scene,RopeNetwork.Build(scene.Parts,scene.Connections));
            var restored=Assert.IsType<PhysicsRopeJoint>(Assert.Single(after.InitialJoints.ToArray()));
            Assert.Equal(before.Bodies.ToArray().Select(body=>body.Snapshot()),
                after.Bodies.ToArray().Select(body=>body.Snapshot()));
            Assert.Equal(before.Declarations.ToArray().Select(body=>body.Dynamics),
                after.Declarations.ToArray().Select(body=>body.Dynamics));
            Assert.Equal(rope.MaximumLength,restored.MaximumLength);
            Assert.Equal(rope.Route.Anchors.ToArray().Select(at=>(at.Body.Id,at.LocalPosition)),
                restored.Route.Anchors.ToArray().Select(at=>(at.Body.Id,at.LocalPosition)));
        }
        finally {scene.Free();}
    }

    [Fact]
    public void DeclarationCopiesAnchorsAndRejectsInvalidInputsOrMissingParticipants()
    {
        var scene=World();
        try
        {
            var load=Add(scene,Fixture.LeftLoad); var anchor=Add(scene,Fixture.Anchor);
            var sockets=new[]{new RopeSocket(load,load.ConnectionPorts.Single(p=>p.Id==SocketId.Tie)),
                new RopeSocket(anchor,anchor.ConnectionPorts.Single(p=>p.Id==SocketId.Tie))};
            var path=new RopePath(sockets,8);
            sockets[0]=default;
            Assert.Same(load,path.Sockets[0].Part);
            Assert.Throws<ArgumentException>(()=>new RopePath(path.Sockets,float.NaN));
            Assert.Throws<ArgumentException>(()=>new RopePath([],8));
            var key=new SceneJointKey(load,new());
            var anchors=new[]{new SceneRopeAnchor(new(load,MachinePart.RootBody),default),
                new SceneRopeAnchor(new(anchor,MachinePart.RootBody),default)};
            var declaration=new SceneRopeJoint(key,anchors,8,ConnectedBodyCollision.Enabled);
            anchors[0]=default;
            Assert.Same(load,declaration.Anchors[0].Body.Owner);
            Assert.Throws<ArgumentException>(()=>new SceneRopeJoint(key,[],1,ConnectedBodyCollision.Enabled));
            Assert.Throws<ArgumentException>(()=>new SceneRopeJoint(key,declaration.Anchors.ToArray(),0,ConnectedBodyCollision.Enabled));
            Assert.Throws<ArgumentException>(()=>new SceneRopeJoint(key,declaration.Anchors.ToArray(),1,(ConnectedBodyCollision)99));
            Assert.Throws<ArgumentException>(()=>new SceneRopeAnchor(new(load,MachinePart.RootBody),new(double.NaN,0,0)));
            Assert.Throws<ArgumentException>(()=>new ScenePhysicsAssembly(
                WorldGeometry.CapturePhysicsBodies(scene).Where(d=>d.Geometry.Owner!=anchor),[declaration],[]));
            Assert.Equal("mass",PartParameterName.Of(WeightParameter.Mass));
            Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((WeightParameter)99));
        }
        finally {scene.Free();}
    }
}
