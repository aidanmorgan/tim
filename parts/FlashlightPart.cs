using Godot;
namespace CuriousContraptions;

/// <summary>A self-contained battery torch, latched on by a physical button or activation command.</summary>
public partial class FlashlightPart : MachinePart
{
    public static readonly Vector3 LensPosition = new(.66f, 0, 0);
    public const float Range = 8;
    public const float ConeCosine = .9659258f; // 15 degree half-angle.
    public const float Intensity = 24;
    private LightConeVisual _beam = null!;
    private MeshInstance3D _lens = null!;
    private MeshInstance3D _button = null!;
    private MachineWorld? _world;
    public override bool CanReceiveActivation => true;
    public override LightEmitter? LightSource => Active
        ? new(LensPosition, Vector3.Right, Range, ConeCosine, Intensity) : null;
    protected override void Build()
    {
        PickRadius = .9f;
        AddBox(new(-.1f, 0, 0), new(1, .6f, .6f), Definition.Color, false);
        var body = PartArt.Cylinder(Visual, .3f, 1, Definition.Color, new(-.1f, 0, 0));
        body.RotationDegrees = new(0, 0, 90);
        var collar = PartArt.Cylinder(Visual, .43f, .25f, new("#fff8e9"), new(.5f, 0, 0));
        collar.RotationDegrees = new(0, 0, 90);
        Boxes.Add(new(new(.5f, 0, 0), new(.125f, .43f, .43f)));
        _lens = PartArt.Cylinder(Visual, .34f, .03f, new("#556573"), LensPosition);
        _lens.RotationDegrees = new(0, 0, 90);
        AddBox(new(-.15f, .36f, 0), new(.4f, .14f, .35f), Definition.Color, false);
        _button = PartArt.Cylinder(Visual, .18f, .14f, new("#f7cb52"), new(-.15f, .36f, 0));
        PartArt.Box(Visual, new(1.2f, .12f, .85f), new("#293954"), new(0, -.38f, 0));
        _beam = new LightConeVisual { Name = "LightCone", Visible = false };
        Visual.AddChild(_beam);
    }
    public override void _Process(double delta)
    {
        _beam.Visible = Active;
        if (_world != null && LightSource is { } source) _beam.Refresh(_world, this, source);
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        var point = Transform.AffineInverse() * body.Position;
        if (speed >= Assistance(world.Precision).TriggerThreshold &&
            point.Y > .3f && Mathf.Abs(point.X + .15f) < .45f && Mathf.Abs(point.Z) < .4f)
            world.Activate(this);
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        _button.Position = new(-.15f, Mathf.MoveToward(_button.Position.Y, Active ? .3f : .36f, delta), 0);
        ((StandardMaterial3D)_lens.MaterialOverride).AlbedoColor = Active ? new("#fff0a5") : new("#556573");
        _world = world;
    }
}
