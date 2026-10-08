using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Assertions/actions against the run's owned body and joint. No
/// alternate angle, integrator or collision response is stored here.</summary>
internal static class LeverFixture
{
    internal static PhysicsBody Body(MachineWorld world,ImpactLeverPart lever)=>
        world.PhysicsAssembly.Body(new(lever,ImpactLeverPart.BeamBody));
    internal static PhysicsFrameJoint Joint(MachineWorld world,ImpactLeverPart lever)=>
        Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(lever,ImpactLeverPart.PivotJoint)));
    internal static double Angle(MachineWorld world,ImpactLeverPart lever)=>Joint(world,lever).Travel.Error;
    internal static double Speed(MachineWorld world,ImpactLeverPart lever)
    {
        var joint=Joint(world,lever);
        return joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed;
    }
    internal static double Energy(MachineWorld world,ImpactLeverPart lever)=>Body(world,lever).KineticEnergy;
    internal static void Push(MachineWorld world,ImpactLeverPart lever,double angularSpeedChange)
    {
        var body=Body(world,lever);
        var frame=Joint(world,lever).FrameB;
        var axis=frame.Orientation.Apply(new(0,0,1));
        var arm=frame.Orientation.Apply(new(1,0,0));
        var tangent=CollisionVector.Cross(axis,arm);
        // Equal/opposite point impulses deliver pure angular momentum without
        // imparting translation; the shared joint solves any active stop.
        var inertia=1/CollisionVector.Dot(axis,body.InverseInertia(axis));
        var impulse=tangent*(inertia*angularSpeedChange*.5);
        world.Physics.ApplyImpulse(body.Id,impulse,body.Center+arm);
        world.Physics.ApplyImpulse(body.Id,-impulse,body.Center-arm);
    }
}
