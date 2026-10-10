using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Passive two-body declaration and art. All motion comes from shared physical presentation.</summary>
public partial class SpringboardPart : MachinePart
{
    public SpringboardSettings CanonicalSpring { get; private set; } = SpringboardSettings.Default;
    internal void ApplySpring(SpringboardSettings value) { value.Validate(); CanonicalSpring = value; }
    protected override void Build()
    {
        ApplySpring(Definition.Springboard!.Capture()); PickRadius = .9f;
        PartArt.Box(Visual, new(1.3f, .12f, 1.2f), new("#273446"), new(0, -.36f, 0));
        var plate = new Node3D { Name = "Plate", Position = new(0, .14f, 0) }; Visual.AddChild(plate);
        PartArt.Box(plate, new(1.3f, .15f, 1.2f), Definition.Color);
        PartArt.Box(plate, new(.4f, .008f, .025f), new("#293954"), new(0, .079f, 0));
        var coil = new Node3D { Name = "Coil" }; Visual.AddChild(coil);
        const int segments = 64; const float height = .365f;
        Vector3 Point(int i)
        {
            var t = i / (float)segments; var angle = t * Mathf.Tau * 3;
            return new(.2f * Mathf.Cos(angle), (t - .5f) * height, .2f * Mathf.Sin(angle));
        }
        for (var i = 0; i < segments; i++) PartArt.Line(coil, Point(i), Point(i + 1), new("#ccd9df"), .025f);
        BindPhysicalBody(PhysicalVisualSlot.Secondary, plate);
        BindPhysicalSpan(plate, new(0, -.075f, 0), coil, new(0, -.3f, 0), height);
    }
}
