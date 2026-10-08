using System.Text.Json;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JointDiagnosticTests
{
    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.BallSocket)]
    public void RoundTripPreservesActualSharedJointState(FrameJointKind kind)
    {
        var body=new PhysicsBody(new(3),PhysicsMotionType.Dynamic,RigidPose.Identity,default,new(0,0,2),1,new(1,1,1));
        var anchor=new PhysicsBody(new(4),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(2),kind,body,frame,anchor,frame,
            ConnectedBodyCollision.Disabled,kind==FrameJointKind.BallSocket?null:new(-.6,.6),JointTravelDirection.Both);
        PhysicsObject Object(PhysicsBody source)=>new(source,
            new CompoundGeometry([new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default));
        world.Step([],[],.01);
        joint=(PhysicsFrameJoint)world.Joints[0];body=joint.A;anchor=joint.B;
        var before=body.Snapshot();
        var report=new PlaytestFrame {Tick=120,FrameJoints=[PlaytestFrameJoint.Capture(world,joint)]};
        var json=JsonSerializer.Serialize(report,PlaytestJson.Default.PlaytestFrame);
        var restored=JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!;
        var snapshot=Assert.Single(restored.FrameJoints);
        Assert.Equal(joint.Id,snapshot.Id);Assert.Equal(body.Id,snapshot.BodyA);
        Assert.Equal(anchor.Id,snapshot.BodyB);Assert.Equal(kind,snapshot.Kind);
        Assert.Equal(body.KineticEnergy,snapshot.Energy);
        if(kind==FrameJointKind.BallSocket)
        {Assert.Null(snapshot.Coordinate);Assert.Null(snapshot.Speed);Assert.Null(snapshot.Lower);Assert.Null(snapshot.Upper);}
        else
        {
            Assert.Equal(joint.Travel.Error,snapshot.Coordinate);
            Assert.Equal(joint.Travel.Jacobian.Bind(body,anchor).Speed,snapshot.Speed);
            Assert.Equal(-.6,snapshot.Lower);Assert.Equal(.6,snapshot.Upper);
        }
        if(kind==FrameJointKind.Hinge)
        {
            var travel=world.AngularTravel(joint.Id);
            Assert.True(travel.Distance>0);
            Assert.Equal(travel.Winding,snapshot.Winding);
            Assert.Equal(travel.Distance,snapshot.AngularDistance);
            Assert.Equal(travel.DistanceError,snapshot.AngularDistanceError);
        }
        else {Assert.Null(snapshot.Winding);Assert.Null(snapshot.AngularDistance);Assert.Null(snapshot.AngularDistanceError);}
        Assert.Equal(before,body.Snapshot());
    }

    [Fact]
    public void CanonicalKindsAndTypedIdentitiesRejectUnsupportedWireValues()
    {
        var options=new JsonSerializerOptions();
        options.Converters.Add(new PlaytestFrameJointKindConverter());
        options.Converters.Add(new PlaytestJointIdConverter());
        options.Converters.Add(new PlaytestBodyIdConverter());
        Assert.Equal("\"hinge\"",JsonSerializer.Serialize(FrameJointKind.Hinge,options));
        Assert.Equal("\"slider\"",JsonSerializer.Serialize(FrameJointKind.Slider,options));
        Assert.Equal("\"ball_socket\"",JsonSerializer.Serialize(FrameJointKind.BallSocket,options));
        Assert.Equal("2",JsonSerializer.Serialize(new PhysicsJointId(2),options));
        Assert.Equal(new PhysicsBodyId(3),JsonSerializer.Deserialize<PhysicsBodyId>("3",options));
        foreach(var invalid in new[]{"-1","1.5","\"3\"","null"})
        {
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PhysicsJointId>(invalid,options));
            Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<PhysicsBodyId>(invalid,options));
        }
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<FrameJointKind>("\"beam\"",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<FrameJointKind>("1",options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Serialize((FrameJointKind)999,options));
        Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize("{\"hinges\":[]}",PlaytestJson.Default.PlaytestFrame));
    }
}
