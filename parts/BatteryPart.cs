using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class BatteryPart : MachinePart
{
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(0, .62f, 0))];
    public override bool SuppliesElectricity(SocketId outputPort) =>
        outputPort == SocketId.Supply && Parameter("enabled", 1) > .5f;
    protected override void Build()
    {
        PickRadius = .8f;
        AddBox(Vector3.Zero, new(.85f, 1.05f, .75f), Definition.Color);
        PartArt.Box(Visual, new(.87f, .25f, .77f), new("#fff8e9"), new(0, .3f, 0));
        PartArt.Cylinder(Visual, .16f, .14f, new("#f7cb52"), new(0, .59f, 0));
        PartArt.Box(Visual, new(.32f, .07f, .015f), new("#293954"), new(0, .02f, .385f));
        PartArt.Box(Visual, new(.07f, .32f, .015f), new("#293954"), new(0, .02f, .395f));
    }
}
