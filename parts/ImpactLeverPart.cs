using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ImpactLeverParameter { BeamMass, InitialAngle }

/// <summary>Passive fixed-pivot beam. Its pose follows finite-inertia contact, never a scripted flip.</summary>
public partial class ImpactLeverPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ImpactLeverParameter>(fields);
    public const string CatalogId = "impact_lever";
    public static readonly Vector3 BeamHalf = new(1.8f, .12f, .55f);
    public const double LimitAngle = Math.PI / 6;
    private Node3D _beamVisual = null!;
    public Transform3D BeamTransform=>Transform*_beamVisual.Transform;
    public static readonly JointSlot PivotJoint=new();
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        new SceneFrameJoint(new(this,PivotJoint),Physics.FrameJointKind.Hinge,
            new(this,BeamBody),new(default,global::CuriousContraptions.Geometry.RigidRotation.Identity),
            new(this,RootBody),new(default,global::CuriousContraptions.Geometry.RigidRotation.Identity),
            Physics.ConnectedBodyCollision.Disabled,new(-LimitAngle,LimitAngle),Physics.JointTravelDirection.Both)
    ];
    public static readonly BodySlot BeamBody=new(p=>SceneGeometryAdapter.CaptureRigidPose(((ImpactLeverPart)p)._beamVisual.Transform),
        p=>((ImpactLeverPart)p!).BeamDynamics,BodyQueryPolicy.ExcludeFromStaticQueries,p=>p!.InitialContactMaterial,p=>[new(((ImpactLeverPart)p!)._beamVisual,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,global::CuriousContraptions.Geometry.RigidPose.Identity))]);
    private BodyDynamics BeamDynamics
    {
        get
        {
            var mass=(double)ReadParameter(ImpactLeverParameter.BeamMass);
            var x=(double)BeamHalf.X; var y=(double)BeamHalf.Y; var z=(double)BeamHalf.Z;
            return new(Physics.PhysicsMotionType.Dynamic,mass,
                new(mass*(y*y+z*z)/3,mass*(x*x+z*z)/3,mass*(x*x+y*y)/3),
                default,default);
        }
    }
    public override Physics.ContactMaterial InitialContactMaterial => new(0,.1,.3);
    private readonly SimulationState<int> _count = new(0);
    public int ImpactCount => _count.Value;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_count];

    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var mass = parameters.Read(ImpactLeverParameter.BeamMass);
        var angle = parameters.Read(ImpactLeverParameter.InitialAngle);
        if (!float.IsFinite(mass) || mass < .5f || mass > 20 ||
            !float.IsFinite(angle) || angle < -30 || angle > 30)
            throw new ArgumentException("Lever beam mass must be 0.5–20 and initial angle −30–30 degrees.");
    }

    protected override void Build()
    {
        PickRadius = 2;
        var initialAngle = ReadParameter(ImpactLeverParameter.InitialAngle) * Math.PI / 180;
        Boxes.Add(new(Vector3.Zero,BeamHalf,BeamBody));
        AddBox(new(0,-1.18f,0), new(1.4f,.16f,1), new("#293954"));
        PartArt.Cylinder(Visual,.22f,1.02f,new("#e8b764"),new(0,-.59f,0));
        var pin = PartArt.Cylinder(Visual,.18f,1.25f,new("#e8b764"));
        pin.RotationDegrees = new(90,0,0);
        // Fixed physical stops meet the underside at the authored angular bounds.
        foreach (var side in new[] { -1f, 1f })
        {
            var at = new Vector3(side * 1.5f,-1.004f,0);
            PartArt.Cylinder(Visual,.13f,.3f,new("#e8b764"),at);
            Boxes.Add(new(at,new(.13f,.15f,.3f), MachinePart.RootBody));
        }
        _beamVisual = new Node3D();
        Visual.AddChild(_beamVisual);
        PartArt.Box(_beamVisual, BeamHalf * 2, new("#fff8e9"));
        PartArt.Box(_beamVisual, new(3.38f,.014f,.82f), new("#66b8c9"),new(0,.121f,0));
        // Gold witness marks expose lever arms without a ruler/inspector overlay.
        foreach (var x in new[] { -1.2f, -.6f, .6f, 1.2f })
            PartArt.Box(_beamVisual,new(.035f,.016f,.3f),new("#e8b764"),new(x,.122f,0));
        _beamVisual.Transform = new(new Basis(Vector3.Back,(float)initialAngle),Vector3.Zero);
    }

    public override void BeforeNetworks(MachineWorld world) => Active = false;
    public override void ObserveContact(SceneContact contact,MachineWorld world)
    {
        if(contact.Self.Slot!=BeamBody)return;
        var body=contact.OtherPart;
        var speed=(float)contact.ApproachSpeed;
        Active = true;
        if (speed < .45f) return;
        _count.Value++;
        world.Events.TryAdd(new(MachineEventKind.Bounced, Uid, body.Uid),world.Ticks);
    }
}
