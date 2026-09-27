using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum PusherPhase { Holding, Extending, Retracting, Blocked, Conflict, Unpowered }
public static class PusherParameters
{
    public const string Stroke="stroke";
    public const string Speed="speed";
    public const string Acceleration="acceleration";
    public const string Force="force";
}

/// <summary>Ideal self-locking electric linear servo. Supply is binary, not a battery-energy simulation.</summary>
public partial class LinearPusherPart : MachinePart
{
    public const string CatalogId="linear_pusher";
    public const float HeadRadius=.25f;
    public const float RestHeadX=.85f;
    public float Extension { get; private set; }
    public float TravelSpeed { get; private set; }
    public float DeliveredWork { get; private set; }
    public float LastDriveImpulse { get; private set; }
    public PusherPhase Phase { get; private set; }=PusherPhase.Unpowered;
    public bool Extended=>Extension==Properties[PusherParameters.Stroke];
    public bool Retracted=>Extension==0;
    public Vector3 HeadPosition=>Transform*new Vector3(RestHeadX+Extension,0,0);
    private Node3D _head=null!;
    private MeshInstance3D _rod=null!;
    private StandardMaterial3D _indicator=null!;
    public override float SurfaceBounce=>0;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.45f,-.4f,.5f)),
        new(SocketId.ExtendIn,ConnectionDomain.Electrical,PortDirection.Input,new(.15f,-.4f,.5f)),
        new(SocketId.RetractIn,ConnectionDomain.Electrical,PortDirection.Input,new(.55f,-.4f,.5f)),
        new(SocketId.RetractedOut,ConnectionDomain.Electrical,PortDirection.Output,new(-.45f,.4f,.45f)),
        new(SocketId.ExtendedOut,ConnectionDomain.Electrical,PortDirection.Output,new(.45f,.4f,.45f))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
    [
        new(SocketId.PowerIn,SocketId.RetractedOut,Retracted),
        new(SocketId.PowerIn,SocketId.ExtendedOut,Extended)
    ];
    public override void ValidateParameters()
    {
        Check(PusherParameters.Stroke,.25f,3);
        Check(PusherParameters.Speed,.25f,4);
        Check(PusherParameters.Acceleration,1,30);
        Check(PusherParameters.Force,1,100);
    }
    private void Check(string key,float minimum,float maximum)
    {
        var value=Properties[key];
        if(!float.IsFinite(value)||value<minimum||value>maximum)
            throw new ArgumentException($"Pusher {key} must be between {minimum} and {maximum}.");
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        LastDriveImpulse=0;
        var extend=HasElectricalPower(SocketId.ExtendIn);
        var retract=HasElectricalPower(SocketId.RetractIn);
        if(!HasElectricalPower(SocketId.PowerIn)) Brake(PusherPhase.Unpowered);
        else if(extend&&retract) Brake(PusherPhase.Conflict);
        else if(!extend&&!retract) Brake(PusherPhase.Holding);
        else Advance(world,delta,extend?1:-1);
        Active=Phase is PusherPhase.Extending or PusherPhase.Retracting;
        _indicator.AlbedoColor=Phase switch {
            PusherPhase.Extending or PusherPhase.Retracting=>new("#66b8c9"),
            PusherPhase.Blocked or PusherPhase.Conflict=>new("#f7cb52"),
            _=>new("#556573")
        };
        UpdateGeometry();
    }
    private void Brake(PusherPhase phase) { TravelSpeed=0;Phase=phase; }
    private void Advance(MachineWorld world,float delta,int sign)
    {
        var remaining=sign>0?Properties[PusherParameters.Stroke]-Extension:Extension;
        if(remaining<=0){Brake(PusherPhase.Holding);return;}
        var acceleration=Properties[PusherParameters.Acceleration];
        var speed=Mathf.Min(Properties[PusherParameters.Speed],Mathf.Sqrt(2*acceleration*remaining));
        TravelSpeed=Mathf.MoveToward(TravelSpeed,sign*speed,acceleration*delta);
        var movement=TravelSpeed*delta;
        // During reversal the old velocity may still point away from the new target.
        movement=Mathf.Clamp(movement,-Extension,Properties[PusherParameters.Stroke]-Extension);
        if(movement==0){Phase=PusherPhase.Holding;return;}
        var axis=Basis.X;
        var direction=axis*Mathf.Sign(movement);
        var hit=WorldGeometry.Sweep(world,HeadPosition,HeadRadius,direction*Mathf.Abs(movement),this);
        if(hit.Status==SphereSweepStatus.Overlapping){Brake(PusherPhase.Blocked);return;}
        var distance=hit.Status==SphereSweepStatus.Clear?Mathf.Abs(movement):hit.Distance;
        Extension=Mathf.Clamp(Extension+Mathf.Sign(movement)*distance,0,Properties[PusherParameters.Stroke]);
        if(hit.Status==SphereSweepStatus.Contact)
        {
            if(hit.Part is { Dynamic:true } cargo && Mathf.Sign(movement)==sign)
            {
                var normal=-hit.Normal;
                var alignment=Mathf.Max(0,normal.Dot(direction));
                var target=speed*alignment;
                var impulse=Mathf.Min(Properties[PusherParameters.Force]*delta*alignment,
                    cargo.Mass*Mathf.Max(0,target-cargo.Velocity.Dot(normal)));
                var before=.5f*cargo.Mass*cargo.Velocity.LengthSquared();
                cargo.Velocity+=normal*(impulse/cargo.Mass);
                DeliveredWork+=Mathf.Max(0,.5f*cargo.Mass*cargo.Velocity.LengthSquared()-before);
                LastDriveImpulse=impulse;
                // A moving load is not a rigid stop. Preserve only the commanded speed
                // that the contacted cargo can actually sustain; resetting it to zero
                // every substep makes a loaded actuator repeatedly restart its ramp.
                TravelSpeed=Mathf.Sign(movement)*Mathf.Min(Mathf.Abs(TravelSpeed),
                    Mathf.Max(0,cargo.Velocity.Dot(direction)));
                Phase=PusherPhase.Blocked;
            }
            else Brake(PusherPhase.Blocked);
        }
        else Phase=movement>0?PusherPhase.Extending:PusherPhase.Retracting;
    }
    private void UpdateGeometry()
    {
        var x=RestHeadX+Extension;
        _head.Position=new(x,0,0);
        _rod.Position=new((.6f+x)*.5f,0,0);
        _rod.Scale=new(x-.6f,1,1);
        Spheres[0]=new(new(x,0,0),HeadRadius);
        Boxes[2]=new(new((.6f+x)*.5f,0,0),new((x-.6f)*.5f,.06f,.06f));
    }
    protected override void Build()
    {
        PickRadius=1.15f;
        AddBox(new(0,-.5f,0),new(1.6f,.2f,1),new("#293954"));
        AddBox(Vector3.Zero,new(1.2f,.65f,.65f),new("#fff8e9"));
        Boxes.Add(new(Vector3.Zero,Vector3.One)); // replaced by current rod geometry below
        _rod=PartArt.Box(Visual,new(1,.12f,.12f),new("#e8b764"));
        _head=new Node3D();Visual.AddChild(_head);
        PartArt.Sphere(_head,HeadRadius,new("#fff8e9"));
        var ring=PartArt.Ring(_head,.2f,.035f,new("#66b8c9"));ring.RotationDegrees=new(0,0,90);
        Spheres.Add(new(Vector3.Zero,HeadRadius));
        for(var i=0;i<5;i++)
            PartArt.Box(Visual,new(.025f,.025f,.25f),new("#e8b764"),new(-.4f+i*.2f,.34f,0));
        foreach(var port in ConnectionPorts)
            PartArt.Sphere(Visual,.065f,port.Direction==PortDirection.Output?new("#66b8c9"):new("#e8b764"),port.LocalPosition);
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.07f,new("#556573"),new(0,.42f,0)).MaterialOverride;
        UpdateGeometry();
    }
}
