using Godot;
using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Passive finite-mass tile. Shared contacts cause tipping and propagation;
/// the output observes rotation and supplies no motion or energy.</summary>
public partial class DominoPart : MachinePart
{
    public static readonly Vector3 HalfSize=new(.125f,.55f,.325f);
    private static readonly Vector3 CenterOffset=new(0,.45f,0);
    private const float TileMass=.4f;
    private const double ToppledCosine=.7071067811865476;
    public override System.Collections.Generic.IReadOnlyList<SceneTiltSensorDeclaration> PhysicsTiltSensors=>
        [new(new(this,RootBody),new(0,1,0),ToppledCosine)];
    public override Vector3 LocalCenterOfMass=>CenterOffset;
    public override BodyDynamics InitialBodyDynamics=>new(PhysicsMotionType.Dynamic,Mass,
        new(Mass*((double)HalfSize.Y*HalfSize.Y+(double)HalfSize.Z*HalfSize.Z)/3,
            Mass*((double)HalfSize.X*HalfSize.X+(double)HalfSize.Z*HalfSize.Z)/3,
            Mass*((double)HalfSize.X*HalfSize.X+(double)HalfSize.Y*HalfSize.Y)/3),
        SceneGeometryAdapter.CaptureVector(InitialVelocity),default);
    public override ContactMaterial InitialContactMaterial=>new(.05,.6,.3);
    public override bool CanSendActivation=>true;
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
        =>throw new InvalidOperationException("A passive domino cannot receive an activation command; tip it through physical contact.");

    protected override void Build()
    {
        Dynamic=true; Mass=TileMass; Drag=0; Bounce=.05f;
        Radius=HalfSize.Length(); PickRadius=.6f;
        // Collider coordinates are relative to the declared centre of mass;
        // authored placement and artwork keep their original construction origin.
        Boxes.Add(new(Vector3.Zero,HalfSize,RootBody));
        PartArt.Box(Visual,HalfSize*2,Definition.Color,CenterOffset);
        foreach(var height in new[]{.2f,.7f})
            PartArt.Sphere(Visual,.045f,new("#384757"),new(.14f,height,0));
    }

    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var state=world.Physics.TiltState(world.PhysicsAssembly.Body(new(this,RootBody)).Id);
        if(state.Phase==PhysicsTiltPhase.Waiting)Active=false;
        else if(!Active)world.EmitActivation(this);
    }
}
