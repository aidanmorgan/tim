using Godot;

namespace CuriousContraptions;

[GlobalClass]
public partial class PartDefinition : Resource
{
    private string _id = "";
    [Export] public string Id
    {
        get => _id;
        set { _id = value; WorkshopKind = value switch { "ball" => WorkshopPartKind.Basketball, "basket" => WorkshopPartKind.Receiver, "ramp" => WorkshopPartKind.Ramp, "switch" => WorkshopPartKind.ImpactSwitch, "lamp" => WorkshopPartKind.SignalLamp, "wall" => WorkshopPartKind.Wall, _ => WorkshopPartKind.Unsupported }; }
    }
    public WorkshopPartKind WorkshopKind { get; private set; }
    [Export] public WallDimensionsResource? Wall { get; set; }
    [Export] public RampDimensionsResource? Ramp { get; set; }
    [Export] public BasketballMaterialResource? Basketball { get; set; }
    [Export] public string Title { get; set; } = "";
    [Export] public string Category { get; set; } = "Structure";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public Color Color { get; set; } = new("#f4bd69");
    [Export] public Godot.Collections.Dictionary<string, float> Parameters { get; set; } = new();
}
