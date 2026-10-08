using Godot;

namespace CuriousContraptions;

// One definition for the visible tabletop and its finite collision geometry.
public static class Workbench
{
    public static readonly BodySlot Body=new(_=>global::CuriousContraptions.Geometry.RigidPose.Identity,_=>new(Physics.PhysicsMotionType.Static,0,default,default,default),BodyQueryPolicy.Include,_=>new(1,.1,.3),_=>[]);
    public static readonly BoxProxy Base = new(new(0, -.7f, 0), new(8.5f, .175f, 5), MachinePart.RootBody);
    public static readonly BoxProxy Deck = new(new(0, -.49f, 0), new(8.4f, .03f, 4.9f), MachinePart.RootBody);
    public static float SurfaceY => Deck.At.Y + Deck.Half.Y;
}

