using System;

namespace CuriousContraptions.Gpu;

/// <summary>Explicit authoring calibration; the shared kernel receives speed and finite joules.</summary>
public readonly record struct BumperWork(LinearSpeed Strength, Kilograms ReferenceMass, Joules Preload)
{
    public static BumperWork Default => FromCanonicalStrength((Half)8);
    public static BumperWork FromCanonicalStrength(Half strength)
    {
        PhysicsDeclarationBounds.Range(strength, (Half)0, (Half)20);
        var referenceMass = new Kilograms((Half)1);
        var energy = (Half)((Half)((Half).5 * referenceMass.Value) * (Half)(strength * strength));
        return new(new(strength), referenceMass, new(energy));
    }
    public static BumperWork FromInput(double strength)
    {
        if (!double.IsFinite(strength) || strength < 0 || strength > 20)
            throw new ArgumentException("Bumper strength is outside its authored range.");
        return FromCanonicalStrength((Half)strength);
    }
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(Strength.Value, (Half)0, (Half)20);
        if (ReferenceMass.Value != (Half)1 || Preload != FromCanonicalStrength(Strength.Value).Preload)
            throw new ArgumentException("Bumper preload differs from its declared reference-mass calibration.");
    }
}
public readonly record struct WorkshopBumper(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, BumperWork Work, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.PinballBumper;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Work.Validate();
    }
}

