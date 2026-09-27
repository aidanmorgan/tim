using Godot;

namespace CuriousContraptions;

[GlobalClass]
public partial class PartDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Title { get; set; } = "";
    [Export] public string Category { get; set; } = "Structure";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public Color Color { get; set; } = new("#f4bd69");
    [Export] public Godot.Collections.Dictionary<string, float> Parameters { get; set; } = new();
}

