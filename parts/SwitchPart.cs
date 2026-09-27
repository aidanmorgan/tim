using Godot;
using System.Collections.Generic;
namespace CuriousContraptions;

public partial class SwitchPart : MachinePart
{
    private MeshInstance3D _button = null!;
    public override bool CanSendActivation => true;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketIds.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, new(.45f, 0, .4f)),
        new(SocketIds.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(-.58f, 0, 0)),
        new(SocketIds.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(.58f, 0, 0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes =>
        Active ? [new(SocketIds.PowerIn, SocketIds.Supply)] : [];

    protected override void Build()
    {
        PickRadius = .65f;
        PartArt.Sphere(Visual, .08f, new("#293954"), new(-.58f, 0, 0));
        PartArt.Sphere(Visual, .08f, new("#293954"), new(.58f, 0, 0));
        AddBox(new(0, -.15f, 0), new(1.1f, .25f, 1), new("#2c3a4c"));
        AddBox(new(0, .05f, 0), new(.8f, .18f, .75f), Definition.Color, false);
        _button = PartArt.Cylinder(Visual, .38f, .18f, Definition.Color, new(0, .06f, 0));
        PartArt.Sphere(Visual, .09f, new("#ffd899"), new(.45f, 0, .4f));
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (speed < Assistance(world.Precision).TriggerThreshold) return;
        world.Activate(this);
        _button.Position = new(0, -.02f, 0);
        ((StandardMaterial3D)_button.MaterialOverride).AlbedoColor = new("#bff5b0");
    }
}
