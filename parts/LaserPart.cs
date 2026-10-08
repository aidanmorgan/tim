using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Externally powered laser, enabled by a trigger until workshop Reset.</summary>
public partial class LaserPart : MachinePart
{
    public const float Range=16;
    public static readonly Vector3 LensPosition=new(.72f,0,0);
    public static readonly Vector3 BeamPower=new(1,.78f,.32f);
    public bool Enabled { get; private set; }
    public IReadOnlyList<OpticalSegment> BeamPath { get; private set; }=[];
    private RuntimeCheckpoint? _checkpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_checkpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(LaserPart owner) : SimulationTransactionParticipant
    {
        private bool _enabled;
        private IReadOnlyList<OpticalSegment> _path = [];
        protected override void CaptureCheckpoint() { _enabled=owner.Enabled; _path=owner.BeamPath; }
        protected override void RestoreCheckpoint() { owner.Enabled=_enabled; owner.BeamPath=_path; }
    }
    private MeshInstance3D _lens=null!;
    private static readonly AnimationFollowDefinition LensResponse=new(0,1,0,12,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourFollow> FollowingColours=>
        [new(_lens,LensResponse,new("#556573"),new("#fff0a5"),SceneColourFollowSignal.OwnerActive)];
    public override bool CanReceiveActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.7f,0,0)),
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.52f,0))
    ];
    public override OpticalEmitter? OpticalPreviewSource=>new(LensPosition,Vector3.Right,Range,BeamPower);
    public override OpticalEmitter? OpticalSource=>Enabled&&HasElectricalPower(SocketId.PowerIn)
        ?new(LensPosition,Vector3.Right,Range,BeamPower):null;
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        if(command!=ActivationCommand.Trigger)throw new ArgumentException("Unsupported laser command.");
        Enabled=true;
        return ActivationDisposition.Deferred;
    }
    public override void ReceiveOpticalPath(IReadOnlyList<OpticalSegment> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        // Own an immutable copy: neither the sender nor readers can mutate a checkpoint.
        BeamPath=path.Count==0 ? Array.Empty<OpticalSegment>() : Array.AsReadOnly(path.ToArray());
        Active=BeamPath.Count>0;
    }
    protected override void Build()
    {
        PickRadius=1;
        AddBox(Vector3.Zero,new(1.25f,.85f,.8f),new("#e8b764"));
        AddBox(new(0,-.55f,0),new(1.5f,.16f,1),new("#293954"));
        var rim=PartArt.Cylinder(Visual,.38f,.12f,new("#fff8e9"),new(.66f,0,0));
        rim.RotationDegrees=new(0,0,90);
        var lens=PartArt.Cylinder(Visual,.23f,.035f,new("#556573"),LensPosition);
        lens.RotationDegrees=new(0,0,90);_lens=lens;
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
