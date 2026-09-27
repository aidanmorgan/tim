using Godot;

namespace CuriousContraptions;

// One definition for the visible tabletop and its finite collision geometry.
public static class Workbench
{
    public static readonly BoxProxy Base = new(new(0, -.7f, 0), new(8.5f, .175f, 5));
    public static readonly BoxProxy Deck = new(new(0, -.49f, 0), new(8.4f, .03f, 4.9f));
    public static float SurfaceY => Deck.At.Y + Deck.Half.Y;
}
