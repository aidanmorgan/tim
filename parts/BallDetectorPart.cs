using Godot;
using CuriousContraptions.Physics;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>One activation per forward centre crossing through the physical aperture.
/// A ball must clear the upstream side before it can trigger again.</summary>
public partial class BallDetectorPart : MachinePart, ITubePart
{
    public int CrossingCount
    {
        get
        {
            if(GetParent() is not MachineWorld {HasPhysicsState:true} world)return 0;
            var frame=world.PhysicsAssembly.Body(new(this,RootBody)).Id;
            ulong count=0;
            foreach(var state in world.Physics.PassageStates)
                if(state.Key.Frame==frame)count=checked(count+state.PassedCount);
            return checked((int)count);
        }
    }
    public override IReadOnlyList<ScenePassageSensorDeclaration> PhysicsPassageSensors=>
        [new(new(this,RootBody),PipePart.BoreRadius,.02)];
    public float Pulse { get; private set; }
    private ulong? _observedStep;
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(BallDetectorPart owner) : SimulationTransactionParticipant
    {
        private float _pulse;
        private ulong? _observedStep;
        protected override void CaptureCheckpoint() { _pulse=owner.Pulse; _observedStep=owner._observedStep; }
        protected override void RestoreCheckpoint() { owner.Pulse=_pulse; owner._observedStep=_observedStep; }
    }
    private StandardMaterial3D _indicator = null!;
    private const float PulseSeconds = .35f;
    public override bool CanSendActivation => true;
    public override Physics.ContactMaterial InitialContactMaterial => new(.15f,.1,.3);
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, new(0,1.12f,0))];
    public IEnumerable<TubeMouth> Mouths =>
    [
        new(TubeMouthId.Start, Vector3.Left * .28f, Vector3.Left, PipePart.BoreRadius),
        new(TubeMouthId.End, Vector3.Right * .28f, Vector3.Right, PipePart.BoreRadius)
    ];
    protected override void Build()
    {
        PickRadius = 1.15f;
        PipeArt.Cylinder(Visual,Transform3D.Identity,.28f,PipePart.BoreRadius,.82f,new("#fff8e9"),true);
        Tubes.Add(new(Transform3D.Identity,.28f,PipePart.BoreRadius,.82f,true));
        AddBox(new(0,.93f,0),new(.5f,.24f,.5f),new("#66b8c9"));
        PartArt.Line(Visual,new(-.16f,1.065f,-.10f),new(.16f,1.065f,0),new("#293954"),.022f);
        PartArt.Line(Visual,new(.16f,1.065f,0),new(-.16f,1.065f,.10f),new("#293954"),.022f);
        PartArt.Sphere(Visual,.07f,new("#e8b764"),new(0,1.12f,0));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual,.075f,new("#556573"),new(0,0,.86f)).MaterialOverride;
    }
    public override void ObservePhysics(MachineWorld world, float delta)
    {
        var frame=world.PhysicsAssembly.Body(new(this,RootBody)).Id;
        if(world.Physics.Collider(frame).Declaration.Participation==CollisionParticipation.Disabled)
            Pulse=0;
        else if(_observedStep!=world.Physics.StepIndex)
        {
            Pulse=Mathf.Max(0,Pulse-delta/PulseSeconds);
            foreach(var passage in world.Physics.PassageEvents)
                if(passage.Key.Frame==frame&&passage.Kind==PhysicsPassageEventKind.Passed)
                {
                    Pulse=1;
                    world.EmitActivation(this);
                }
        }
        _observedStep=world.Physics.StepIndex;
        Active=Pulse>0;
        var eased=Pulse*Pulse*(3-2*Pulse);
        _indicator.AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),eased);
    }
}
