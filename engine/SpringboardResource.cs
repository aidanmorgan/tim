using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class SpringboardResource : Resource
{
    [Export] public float Stiffness { get; set; }
    [Export] public float Damping { get; set; }
    [Export] public float InitialCompression { get; set; }
    public SpringboardSettings Capture()
    {
        var value = new SpringboardSettings(Stiffness, Damping, InitialCompression);
        value.Validate(); return value;
    }
}
