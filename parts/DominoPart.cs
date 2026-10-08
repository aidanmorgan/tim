using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Tile artwork only. The declared box, mass and material compile into the shared solver,
/// which owns standing, toppling and every contact response; the part has no physics or sensor members.</summary>
public partial class DominoPart : MachinePart
{
    protected override void Build()
    {
        var half = DominoMaterial.Default.HalfExtents;
        var size = new Vector3((float)half.X * 2, (float)half.Y * 2, (float)half.Z * 2);
        PickRadius = .6f;
        // The body origin is the box centre, so the artwork is centred; the two pips keep their source heights on the tile.
        PartArt.Box(Visual, size, Definition.Color);
        foreach (var height in new[] { -.25f, .25f })
            PartArt.Sphere(Visual, .045f, new("#384757"), new(.14f, height, 0));
    }
}
