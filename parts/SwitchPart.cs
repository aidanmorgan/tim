using Godot;
namespace CuriousContraptions;

public partial class SwitchPart : MachinePart
{
    private MeshInstance3D _button = null!;
    public override bool CanSendPower => true;
    protected override void Build()
    {
        PickRadius = .65f;
        AddBox(new(0, -.15f, 0), new(1.1f, .25f, 1), new("#2c3a4c"));
        AddBox(new(0, .05f, 0), new(.8f, .18f, .75f), Definition.Color, false);
        _button = PartArt.Cylinder(Visual, .38f, .18f, Definition.Color, new(0, .06f, 0));
        PartArt.Sphere(Visual, .09f, new("#ffd899"), new(.45f, 0, .4f));
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (speed < Assistance(world.Precision).TriggerThreshold) return;
        world.Activate(this);
        _button.Position = new(0, -.02f, 0);
        ((StandardMaterial3D)_button.MaterialOverride).AlbedoColor = new("#bff5b0");
    }
}
