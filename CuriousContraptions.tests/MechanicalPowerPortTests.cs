using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalPowerPortTests
{
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity,
        CollisionVector spin,RigidRotation? rotation=null)=>new(new(id),PhysicsMotionType.Dynamic,
            new(center,rotation??RigidRotation.Identity),velocity,spin,1,new(1,1,1));
    private static Dictionary<PhysicsBodyId,PhysicsBody> Map(params PhysicsBody[] bodies)=>
        bodies.ToDictionary(b=>b.Id);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-12,expected+1e-12);

    [Fact]
    public void OffsetPointIncludesBothBodiesRotationalWork()
    {
        var body=Body(1,new(2,0,0),new(3,0,0),new(0,0,2));
        var frame=Body(0,default,new(1,0,0),new(0,0,3));
        var port=new PointPowerPort(body.Id,frame.Id,new(0,1,0),new(4,0,0));
        var gradient=port.Bind(Map(body,frame),[]);
        Assert.Equal(3,gradient.Speed);
        double power=0;
        var force=default(CollisionVector);var torque=default(CollisionVector);
        foreach(var term in gradient.Terms)
        {
            var f=term.Linear*2;var t=term.Angular*2;
            power+=CollisionVector.Dot(f,term.Body.LinearVelocity)+CollisionVector.Dot(t,term.Body.AngularVelocity);
            force+=f;torque+=CollisionVector.Cross(term.Body.Center,f)+t;
        }
        Assert.Equal(6,power);
        Assert.Equal(default,force);Assert.Equal(default,torque);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    [InlineData(2.4)]
    public void CommonRigidMotionCannotFabricateRelativePower(double angle)
    {
        var spin=new CollisionVector(2,-3,4);var drift=new CollisionVector(5,7,-2);
        var a=new CollisionVector(2,1,3);var b=new CollisionVector(-1,2,-2);
        var body=Body(1,a,drift+CollisionVector.Cross(spin,a),spin,
            RigidRotation.FromRotationVector(new(angle,0,0)));
        var frame=Body(0,b,drift+CollisionVector.Cross(spin,b),spin,
            RigidRotation.FromRotationVector(new(0,angle,0)));
        var gradient=new PointPowerPort(body.Id,frame.Id,new(.4,.5,-.2),new(1,2,3))
            .Bind(Map(body,frame),[]);
        Near(0,gradient.Speed);
        var force=default(CollisionVector);var moment=default(CollisionVector);
        foreach(var term in gradient.Terms)
        {
            force+=term.Linear;
            moment+=CollisionVector.Cross(term.Body.Center-new CollisionVector(7,-4,2),term.Linear)+term.Angular;
        }
        Near(0,force.Length);Near(0,moment.Length);
    }

    [Theory]
    [InlineData(FrameJointKind.Slider,-4,20)]
    [InlineData(FrameJointKind.Hinge,.5,3)]
    public void AxialPortUsesCurrentReboundState(FrameJointKind kind,double scale,double expected)
    {
        var a=Body(1,default,default,default);var b=Body(0,default,default,default);
        var joint=new PhysicsFrameJoint(new(0),kind,a,new(default,RigidRotation.Identity),
            b,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var currentA=Body(1,default,new(0,0,-2),new(3,4,11));
        var currentB=Body(0,default,new(0,0,3),new(3,4,5));
        var port=new AxialPowerPort(joint.Id,kind,scale);
        Assert.Equal(0,port.Bind(Map(a,b),[joint]).Speed);
        var gradient=port.Bind(Map(currentA,currentB),[joint]);
        Near(expected,gradient.Speed);
        Assert.All(gradient.Terms.ToArray(),term=>Assert.Same(
            term.Body.Id==currentA.Id?currentA:currentB,term.Body));
    }

    [Fact]
    public void MissingWrongKindAndDuplicateJointReject()
    {
        var a=Body(1,default,default,default);var b=Body(0,default,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,new(default,RigidRotation.Identity),
            b,new(default,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var port=new AxialPowerPort(joint.Id,FrameJointKind.Slider,1);
        Assert.Throws<ArgumentException>(()=>port.Bind(Map(a,b),[]));
        Assert.Throws<ArgumentException>(()=>port.Bind(Map(a,b),[joint,joint]));
        Assert.Throws<ArgumentException>(()=>new AxialPowerPort(joint.Id,FrameJointKind.Hinge,1).Bind(Map(a,b),[joint]));
        Assert.Throws<ArgumentException>(()=>port.Bind(Map(a),[joint]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidScaleRejects(double scale)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialPowerPort(new(0),FrameJointKind.Slider,scale));

    [Fact]
    public void UnsupportedJointKindRejects()=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialPowerPort(new(0),(FrameJointKind)int.MaxValue,1));

    [Fact]
    public void PointPortRejectsInvalidGeometryAndOwnership()
    {
        Assert.Throws<ArgumentException>(()=>new PointPowerPort(new(0),new(0),default,new(1,0,0)));
        Assert.Throws<ArgumentException>(()=>new PointPowerPort(new(0),new(1),default,default));
        Assert.Throws<ArgumentException>(()=>new PointPowerPort(new(0),new(1),new(double.NaN,0,0),new(1,0,0)));
        var port=new PointPowerPort(new(0),new(1),default,new(1,0,0));
        var a=Body(0,default,default,default);
        Assert.Throws<ArgumentException>(()=>port.Bind(Map(a),[]));
        var malformed=Map(a);malformed.Add(new(1),a);
        Assert.Throws<ArgumentException>(()=>port.Bind(malformed,[]));
    }
}
