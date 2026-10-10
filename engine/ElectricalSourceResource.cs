using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class ElectricalSourceResource : Resource
{
    [Export] public float Capacity { get; set; }
    [Export] public float MaximumPower { get; set; }
    [Export] public float InitialFraction { get; set; }
    [Export] public ElectricalEnable Enabled { get; set; } = (ElectricalEnable)255;
    public ElectricalSourceSettings Capture()
    {
        var settings = new ElectricalSourceSettings(new(Capacity), new(MaximumPower), InitialFraction, Enabled);
        settings.Validate();
        return settings;
    }
}
