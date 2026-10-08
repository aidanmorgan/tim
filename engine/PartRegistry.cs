using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public sealed class PartRegistry
{
    private readonly Dictionary<WorkshopPartKind, PartDefinition> _definitions = new();
    public IReadOnlyDictionary<WorkshopPartKind, PartDefinition> Definitions => _definitions;
    public void Discover()
    {
        // Named content boundary: unsupported scene scripts are neither loaded nor shipped.
        var ball = ResourceLoader.Load<PartDefinition>("res://parts/catalog/ball.tres");
        if (ball is null || ball.Scene is null || ball.WorkshopKind != WorkshopPartKind.Basketball ||
            ball.Basketball is null || ball.Parameters.Count != 0)
            throw new ArgumentException("Canonical Basketball resource is invalid.");
        ball.Basketball.Capture();
        var receiver = ResourceLoader.Load<PartDefinition>("res://parts/catalog/basket.tres");
        if (receiver is null || receiver.Scene is null || receiver.WorkshopKind != WorkshopPartKind.Receiver || receiver.Parameters.Count != 0)
            throw new ArgumentException("Canonical Receiver resource is invalid.");
        var ramp = ResourceLoader.Load<PartDefinition>("res://parts/catalog/ramp.tres");
        if (ramp is null || ramp.Scene is null || ramp.WorkshopKind != WorkshopPartKind.Ramp ||
            ramp.Ramp is null || ramp.Parameters.Count != 0) throw new ArgumentException("Canonical Ramp resource is invalid.");
        ramp.Ramp.Capture();
        var impactSwitch = ResourceLoader.Load<PartDefinition>("res://parts/catalog/switch.tres");
        var lamp = ResourceLoader.Load<PartDefinition>("res://parts/catalog/lamp.tres");
        if (impactSwitch is null || impactSwitch.Scene is null || impactSwitch.WorkshopKind != WorkshopPartKind.ImpactSwitch || impactSwitch.Parameters.Count != 0 ||
            lamp is null || lamp.Scene is null || lamp.WorkshopKind != WorkshopPartKind.SignalLamp || lamp.Parameters.Count != 0)
            throw new ArgumentException("Canonical activation resources are invalid.");
        var wall = ResourceLoader.Load<PartDefinition>("res://parts/catalog/wall.tres");
        if (wall is null || wall.Scene is null || wall.WorkshopKind != WorkshopPartKind.Wall || wall.Wall is null || wall.Parameters.Count != 0)
            throw new ArgumentException("Canonical Wall resource is invalid.");
        wall.Wall.Capture();
        var delay = ResourceLoader.Load<PartDefinition>("res://parts/catalog/delay.tres");
        if (delay is null || delay.Scene is null || delay.WorkshopKind != WorkshopPartKind.Delay || delay.Delay is null || delay.Parameters.Count != 0)
            throw new ArgumentException("Canonical Delay resource is invalid.");
        delay.Delay.Capture();
        var bumper = ResourceLoader.Load<PartDefinition>("res://parts/catalog/bumper.tres");
        if (bumper is null || bumper.Scene is null || bumper.WorkshopKind != WorkshopPartKind.PinballBumper ||
            bumper.Bumper is null || bumper.Parameters.Count != 0)
            throw new ArgumentException("Canonical Pinball bumper resource is invalid.");
        bumper.Bumper.Capture();
        var domino = ResourceLoader.Load<PartDefinition>("res://parts/catalog/domino.tres");
        if (domino is null || domino.Scene is null || domino.WorkshopKind != WorkshopPartKind.Domino || domino.Parameters.Count != 0)
            throw new ArgumentException("Canonical Domino resource is invalid.");
        _definitions.Clear();
        _definitions.Add(WorkshopPartKind.Basketball, ball);
        _definitions.Add(WorkshopPartKind.Receiver, receiver);
        _definitions.Add(WorkshopPartKind.Ramp, ramp);
        _definitions.Add(WorkshopPartKind.ImpactSwitch, impactSwitch);
        _definitions.Add(WorkshopPartKind.SignalLamp, lamp);
        _definitions.Add(WorkshopPartKind.Wall, wall);
        _definitions.Add(WorkshopPartKind.Delay, delay);
        _definitions.Add(WorkshopPartKind.PinballBumper, bumper);
        _definitions.Add(WorkshopPartKind.Domino, domino);
    }
    public MachinePart Create(WorkshopPartKind kind)
    {
        if (kind is not (WorkshopPartKind.Basketball or WorkshopPartKind.Receiver or WorkshopPartKind.Ramp or WorkshopPartKind.ImpactSwitch or WorkshopPartKind.SignalLamp or WorkshopPartKind.Wall or WorkshopPartKind.Delay or WorkshopPartKind.PinballBumper or WorkshopPartKind.Domino) || !_definitions.TryGetValue(kind, out var definition))
            throw new ArgumentException("This catalogue part is not supported by the current GPU Workshop.");
        var part = definition.Scene.Instantiate<MachinePart>();
        try { part.Configure(definition); return part; }
        catch { part.Free(); throw; }
    }
}
