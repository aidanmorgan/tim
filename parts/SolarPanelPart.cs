using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Nine equal-area samples on the front face convert received game-light into binary supply.
/// No ambient-sky power, storage or voltage/current model.</summary>
public partial class SolarPanelPart : MachinePart
{
    public const float Threshold = 1;
    public float Irradiance { get; private set; }
    private readonly List<MeshInstance3D> _meter = new();
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(.2f, -.5f, .6f))];
    public override IEnumerable<LightSample> LightSamples
    {
        get
        {
            foreach (var y in new[] { -.35f, 0f, .35f })
            foreach (var z in new[] { -.45f, 0f, .45f })
                yield return new(new(-.13f, y, z), Vector3.Left, 1f / 9);
        }
    }
    public override void ReceiveLight(float irradiance) => Irradiance = irradiance;
    public override bool SuppliesElectricity(SocketId outputPort) =>
        outputPort == SocketId.Supply && Irradiance >= Threshold;
    protected override void Build()
    {
        PickRadius = 1;
        AddBox(Vector3.Zero, new(.22f, 1.2f, 1.5f), new("#fff8e9"));
        foreach (var y in new[] { -.35f, 0f, .35f })
        foreach (var z in new[] { -.45f, 0f, .45f })
            PartArt.Box(Visual, new(.025f, .29f, .39f), Definition.Color, new(-.125f, y, z));
        AddBox(new(0, -.8f, 0), new(.9f, .16f, 1.2f), new("#293954"));
        PartArt.Cylinder(Visual, .1f, .3f, new("#f7cb52"), new(0, -.65f, 0));
        for (var i = 0; i < 4; i++)
            _meter.Add(PartArt.Box(Visual, new(.025f, .08f, .17f), new("#556573"), new(-.145f, -.51f, -.3f + i * .2f)));
        PartArt.Sphere(Visual, .085f, new("#f7cb52"), new(.2f, -.5f, .6f));
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        Active = Irradiance >= Threshold;
        for (var i = 0; i < _meter.Count; i++)
            ((StandardMaterial3D)_meter[i].MaterialOverride).AlbedoColor =
                Irradiance >= Threshold * (i + 1) / _meter.Count ? new("#f7cb52") : new("#556573");
        if (Active) world.Events.TryAdd(new(MachineEventKind.Powered, Uid), world.Ticks);
    }
}
