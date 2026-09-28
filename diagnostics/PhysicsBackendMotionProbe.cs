#if DEBUG || PLAYTEST
using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Measures the installed server's real simulation, in an isolated
/// space. It does not drive or alter the game's MachineWorld.</summary>
public partial class PhysicsBackendMotionProbe : Node
{
    public enum Experiment { FastTranslation, PureRotation, OpposingBodies }
    public readonly record struct Frame(int Step,double Delta,float[] Position,float Angle,
        float[] Velocity,float AngularSpeed,int Contacts,float[] OtherPosition);
    public Experiment Case { get; init; }
    public List<Frame> Frames { get; } = new();
    public bool Complete => Frames.Count>=4;
    public Action<PhysicsBackendMotionProbe>? Finished { get; init; }
    private readonly List<Rid> _bodies=new();
    private readonly List<Shape3D> _shapes=new();
    private Rid _space,_moving,_other;
    private bool _hasOther;

    public override void _Ready()
    {
        if(!Enum.IsDefined(Case)) throw new ArgumentOutOfRangeException(nameof(Case));
        _space=PhysicsServer3D.SpaceCreate();
        PhysicsServer3D.SpaceSetActive(_space,true);
        switch(Case)
        {
            case Experiment.FastTranslation:
                AddBody(new(.2f,4,4),new(3,0,0),PhysicsServer3D.BodyMode.Static,Vector3.Zero,0);
                _moving=AddBody(Vector3.One*.5f,Vector3.Zero,PhysicsServer3D.BodyMode.Rigid,new(1000,0,0),0);
                break;
            case Experiment.PureRotation:
                AddBody(Vector3.One*.1f,new(1.5f*Mathf.Cos(.4f),1.5f*Mathf.Sin(.4f),0),
                    PhysicsServer3D.BodyMode.Static,Vector3.Zero,0);
                _moving=AddBody(new(4,.1f,.1f),Vector3.Zero,PhysicsServer3D.BodyMode.Rigid,Vector3.Zero,120);
                break;
            case Experiment.OpposingBodies:
                _moving=AddBody(Vector3.One*.5f,new(-2,0,0),PhysicsServer3D.BodyMode.Rigid,new(400,0,0),0);
                _other=AddBody(Vector3.One*.5f,new(2,0,0),PhysicsServer3D.BodyMode.Rigid,new(-400,0,0),0);
                _hasOther=true;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(Case));
        }
        Sample(0);
    }

    private Rid AddBody(Vector3 size,Vector3 position,PhysicsServer3D.BodyMode mode,Vector3 velocity,float spin)
    {
        var shape=new BoxShape3D { Size=size }; _shapes.Add(shape);
        var body=PhysicsServer3D.BodyCreate(); _bodies.Add(body);
        PhysicsServer3D.BodySetMode(body,mode);
        PhysicsServer3D.BodySetSpace(body,_space);
        PhysicsServer3D.BodyAddShape(body,shape.GetRid());
        PhysicsServer3D.BodySetState(body,PhysicsServer3D.BodyState.Transform,new Transform3D(Basis.Identity,position));
        if(mode==PhysicsServer3D.BodyMode.Rigid)
        {
            PhysicsServer3D.BodySetParam(body,PhysicsServer3D.BodyParameter.GravityScale,0);
            PhysicsServer3D.BodySetParam(body,PhysicsServer3D.BodyParameter.LinearDampMode,(int)PhysicsServer3D.BodyDampMode.Replace);
            PhysicsServer3D.BodySetParam(body,PhysicsServer3D.BodyParameter.AngularDampMode,(int)PhysicsServer3D.BodyDampMode.Replace);
            PhysicsServer3D.BodySetParam(body,PhysicsServer3D.BodyParameter.LinearDamp,0);
            PhysicsServer3D.BodySetParam(body,PhysicsServer3D.BodyParameter.AngularDamp,0);
            PhysicsServer3D.BodySetState(body,PhysicsServer3D.BodyState.LinearVelocity,velocity);
            PhysicsServer3D.BodySetState(body,PhysicsServer3D.BodyState.AngularVelocity,Vector3.Back*spin);
            PhysicsServer3D.BodySetEnableContinuousCollisionDetection(body,true);
            PhysicsServer3D.BodySetMaxContactsReported(body,8);
        }
        return body;
    }

    public override void _PhysicsProcess(double delta)
    {
        if(Complete) return;
        Sample(delta);
        if(!Complete) return;
        SetPhysicsProcess(false);
        Finished?.Invoke(this);
    }

    private static float[] Components(Vector3 v)=>[v.X,v.Y,v.Z];
    private void Sample(double delta)
    {
        var pose=PhysicsServer3D.BodyGetState(_moving,PhysicsServer3D.BodyState.Transform).AsTransform3D();
        var velocity=PhysicsServer3D.BodyGetState(_moving,PhysicsServer3D.BodyState.LinearVelocity).AsVector3();
        var angular=PhysicsServer3D.BodyGetState(_moving,PhysicsServer3D.BodyState.AngularVelocity).AsVector3();
        var other=_hasOther?PhysicsServer3D.BodyGetState(_other,PhysicsServer3D.BodyState.Transform).AsTransform3D().Origin:Vector3.Zero;
        var state=PhysicsServer3D.BodyGetDirectState(_moving);
        Frames.Add(new(Frames.Count,delta,Components(pose.Origin),Mathf.Atan2(pose.Basis.X.Y,pose.Basis.X.X),
            Components(velocity),angular.Z,state.GetContactCount(),Components(other)));
    }

    public override void _ExitTree()
    {
        foreach(var body in _bodies) PhysicsServer3D.FreeRid(body);
        if(_space.IsValid) PhysicsServer3D.FreeRid(_space);
        foreach(var shape in _shapes) shape.Dispose();
        _bodies.Clear(); _shapes.Clear();
    }
}
#endif
