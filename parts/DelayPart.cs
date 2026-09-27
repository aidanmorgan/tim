using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum DelayState { Ready, Counting, Finished }

/// <summary>Self-contained one-shot control module. Trigger once, emit after a fixed-tick delay.
/// Further triggers are ignored until workshop Reset. This output is not electrical supply.</summary>
public partial class DelayPart : MachinePart
{
    public DelayState State { get; private set; }
    public int StartedTick { get; private set; } = -1;
    public int DueTick { get; private set; } = -1;
    public float Duration => Properties[DelayParameters.Seconds];
    public float Progress { get; private set; }
    private Node3D _hand = null!;
    private StandardMaterial3D _indicator = null!;
    public override bool CanReceiveActivation => true;
    public override bool CanSendActivation => true;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketIds.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, new(-.72f, 0, 0)),
        new(SocketIds.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, new(.72f, 0, 0))
    ];
    public override void ValidateParameters()
    {
        if (!float.IsFinite(Duration) || Duration < .1f || Duration > 12)
            throw new ArgumentException("Delay duration must be finite and between 0.1 and 12 seconds.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported activation command.");
        if (State == DelayState.Ready)
        {
            State = DelayState.Counting;
            StartedTick = world.Ticks;
            DueTick = StartedTick + (int)Math.Ceiling(Duration / MachineWorld.Tick);
        }
        return ActivationDisposition.Deferred;
    }
    protected override void Build()
    {
        PickRadius = .9f;
        AddBox(Vector3.Zero, new(1.25f, 1.25f, .6f), Definition.Color, false);
        var housing = PartArt.Cylinder(Visual, .65f, .6f, Definition.Color);
        housing.RotationDegrees = new(90, 0, 0);
        var face = PartArt.Cylinder(Visual, .55f, .035f, new("#fff8e9"), new(0, 0, .32f));
        face.RotationDegrees = new(90, 0, 0);
        AddBox(new(0, -.7f, 0), new(1.35f, .15f, .8f), new("#293954"));
        for (var i = 0; i < 12; i++)
        {
            var angle = i * Mathf.Tau / 12;
            PartArt.Sphere(Visual, .025f, new("#293954"), new(Mathf.Sin(angle) * .46f, Mathf.Cos(angle) * .46f, .35f));
        }
        _hand = new Node3D { Name = "CountdownHand", Position = new(0, 0, .37f) };
        Visual.AddChild(_hand);
        PartArt.Box(_hand, new(.055f, .38f, .03f), new("#293954"), new(0, .16f, 0));
        PartArt.Sphere(Visual, .075f, new("#f7cb52"), new(0, 0, .4f));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual, .065f, new("#556573"), new(0, -.35f, .37f)).MaterialOverride;
        PartArt.Sphere(Visual, .075f, new("#e8b764"), new(-.72f, 0, 0));
        PartArt.Sphere(Visual, .075f, new("#e8b764"), new(.72f, 0, 0));
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (State == DelayState.Counting)
        {
            Progress = Mathf.Clamp((float)(world.Ticks - StartedTick) / (DueTick - StartedTick), 0, 1);
            if (world.Ticks >= DueTick)
            {
                State = DelayState.Finished;
                world.EmitActivation(this);
            }
        }
        _hand.Rotation = new(0, 0, -Progress * Mathf.Tau);
        _indicator.AlbedoColor = State switch
        {
            DelayState.Ready => new("#556573"),
            DelayState.Counting => new("#e8b764"),
            DelayState.Finished => new("#f7cb52"),
            _ => throw new InvalidOperationException("Unsupported delay state.")
        };
    }
}
