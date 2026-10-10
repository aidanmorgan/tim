using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Generic finite contact-work authoring boundary; every numeric property is canonical f32.</summary>
[GlobalClass]
public partial class ContactWorkResource : Resource
{
    [Export] public float TargetSpeed { get; set; }
    [Export] public float ReferenceMass { get; set; }
    [Export] public float InitialEnergy { get; set; }
    public ContactWorkCalibration Capture()
    {
        var value = new ContactWorkCalibration(new(TargetSpeed), new(ReferenceMass), new(InitialEnergy));
        value.Validate();
        return value;
    }
}
