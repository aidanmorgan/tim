using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum HoldTimerState { Ready, Holding }

/// <summary>A self-timed electrical contact, not a power source. Busy triggers are ignored.</summary>
public partial class HoldTimerPart : MachinePart
{
    public HoldTimerState State { get; private set; }
    public int StartedTick { get; private set; } = -1;
    public int DueTick { get; private set; } = -1;
    public float Duration => Properties[HoldTimerParameters.Seconds];
    public float Remaining { get; private set; }
    private MeshInstance3D _bar = null!;
    private StandardMaterial3D _indicator = null!;
    public override bool CanReceiveActivation => true;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketIds.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, new(0,.65f,.2f)),
        new(SocketIds.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(-.78f,0,0)),
        new(SocketIds.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(.78f,0,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes =>
        [new(SocketIds.PowerIn, SocketIds.Supply, State == HoldTimerState.Holding)];
    public override void ValidateParameters()
    {
        if (!float.IsFinite(Duration) || Duration < .1f || Duration > 12)
            throw new ArgumentException("Hold duration must be finite and between 0.1 and 12 seconds.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported activation command.");
        if (State == HoldTimerState.Holding) return ActivationDisposition.Deferred;
        State = HoldTimerState.Holding;
        StartedTick = world.Ticks;
        DueTick = StartedTick + (int)Math.Ceiling(Duration / MachineWorld.Tick);
        Remaining = 1;
        return ActivationDisposition.Immediate;
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        if (State == HoldTimerState.Holding && world.Ticks >= DueTick)
        {
            State = HoldTimerState.Ready;
            Active = false;
            Remaining = 0;
        }
    }
    protected override void Build()
    {
        PickRadius = .95f;
        AddBox(Vector3.Zero, new(1.4f,1.1f,.65f), new("#66b8c9"));
        AddBox(new(0,-.62f,0), new(1.6f,.14f,.85f), new("#293954"));
        PartArt.Box(Visual, new(1.18f,.72f,.045f), new("#fff8e9"), new(0,0,.35f));
        PartArt.Box(Visual, new(1,.14f,.025f), new("#293954"), new(0,.1f,.39f));
        _bar = PartArt.Box(Visual, new(1,.10f,.035f), new("#f7cb52"), new(0,.1f,.415f));
        _bar.Visible = false;
        // Two contact pads distinguish the hold relay from the round delay clock.
        foreach (var x in new[] { -.78f,.78f })
            PartArt.Sphere(Visual,.08f,new("#e8b764"),new(x,0,0));
        PartArt.Sphere(Visual,.08f,new("#e8b764"),new(0,.65f,.2f));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual,.07f,new("#556573"),new(0,-.22f,.40f)).MaterialOverride;
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (State == HoldTimerState.Holding)
            Remaining = Mathf.Clamp((float)(DueTick - world.Ticks) / (DueTick - StartedTick),0,1);
        _bar.Visible = State == HoldTimerState.Holding;
        _bar.Scale = new(Mathf.Max(.001f,Remaining),1,1);
        _bar.Position = new((Remaining-1)*.5f,.1f,.415f);
        _indicator.AlbedoColor = State == HoldTimerState.Holding ? new("#f7cb52") : new("#556573");
    }
}
