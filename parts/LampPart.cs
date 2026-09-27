using Godot;
namespace CuriousContraptions;

public partial class LampPart : MachinePart
{
    private MeshInstance3D _bulb = null!;
    public override bool CanReceivePower => true;
    protected override void Build()
    {
        PickRadius = .65f;
        AddBox(new(0, -.35f, 0), new(.85f, .2f, .85f), new("#334856"));
        PartArt.Cylinder(Visual, .18f, .3f, new("#c7c7bb"), new(0, -.17f, 0));
        _bulb = PartArt.Sphere(Visual, .43f, new("#556573"), new(0, .22f, 0));
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        if (!Active) return;
        var material = (StandardMaterial3D)_bulb.MaterialOverride;
        material.AlbedoColor = new("#fff0a5");
        material.EmissionEnabled = true;
        material.Emission = new("#e9b24c");
    }
}

