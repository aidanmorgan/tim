using Godot;
using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum GateState { Closed, Opening, Open, Closing, Blocked }

/// <summary>Slider declaration and powered controller, never a motion integrator.
/// A finite return spring closes the unpowered blade. Shared constraints alone
/// enforce travel and resolve obstruction; rendering observes the actual body.</summary>
public sealed class SlidingBlade(Vector3 half,float stroke)
{
    public const double Mass=1;
    public const double MaximumSpeed=2.8;
    private const double Acceleration=14;
    private const double SpringStiffness=14;
    private const double Damping=4;
    private const double MotorEffort=60;
    private const double MotorPower=120;
    private const double EndpointTolerance=1e-5;
    public float Stroke { get; }=stroke;
    public BodyDynamics InitialDynamics=>new(PhysicsMotionType.Dynamic,Mass,
        new(Mass*(half.Y*half.Y+half.Z*half.Z)/3,
            Mass*(half.X*half.X+half.Z*half.Z)/3,
            Mass*(half.X*half.X+half.Y*half.Y)/3),default,default);

    public SceneFrameJoint Declare(MachinePart owner,BodySlot blade,JointSlot guide)
    {
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(-Math.PI/2,0,0)));
        return new(new(owner,guide),FrameJointKind.Slider,new(owner,blade),frame,
            new(owner,MachinePart.RootBody),frame,ConnectedBodyCollision.Disabled,
            new(0,Stroke),JointTravelDirection.Both);
    }

    public void Prepare(MachineWorld world,MachinePart owner,JointSlot guide,bool powered,float delta)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(owner,guide));
        var travel=joint.Travel;
        var position=travel.Error;
        world.AddElasticLoad(new(owner,guide),new(SpringStiffness,0));
        world.AddDampingLoad(new(owner,guide),Damping,Damping);
        if(!powered) return;
        var distance=Stroke-position;
        var target=Math.Sign(distance)*Math.Min(MaximumSpeed,Math.Sqrt(2*Acceleration*Math.Abs(distance)));
        world.DriveMotor(new(owner,guide),target,MotorEffort,MotorPower*delta,MotorPower);
    }

    public GateState Classify(PhysicsAxialMotion motion,bool powered)=>
        motion.Coordinate<=EndpointTolerance?GateState.Closed:
        Stroke-motion.Coordinate<=EndpointTolerance?GateState.Open:
        Math.Abs(motion.Speed)<1e-5?GateState.Blocked:
        powered?GateState.Opening:GateState.Closing;
}
