using System;

namespace CuriousContraptions.Gpu;

/// <summary>Explicit authoring calibration; the shared kernel receives speed and finite joules.</summary>
public readonly record struct BumperWork(ContactSpeed Strength, WorkMass ReferenceMass, Joules Preload)
{
    public static BumperWork FromCalibration(ContactWorkCalibration calibration)
    {
        calibration.Validate();
        var work = new BumperWork(calibration.TargetSpeed, calibration.ReferenceMass, calibration.InitialEnergy);
        work.Validate();
        return work;
    }
    public static BumperWork Default => FromCanonicalStrength(8f);
    public static BumperWork FromCanonicalStrength(float strength)
    {
        new ContactSpeed(strength).Validate(20f);
        var referenceMass = new WorkMass(1f);
        var energy = .5f * (float)referenceMass.Value * (float)strength * (float)strength;
        return new(new(strength), referenceMass, new(energy));
    }
    public static BumperWork FromInput(double strength)
    {
        if (!double.IsFinite(strength) || strength < 0 || strength > 20)
            throw new ArgumentException("Bumper strength is outside its authored range.");
        return FromCanonicalStrength((float)strength);
    }
    public void Validate()
    {
        Strength.Validate(20f);
        if (ReferenceMass.Value != 1f || Preload != FromCanonicalStrength(Strength.Value).Preload)
            throw new ArgumentException("Bumper preload differs from its declared reference-mass calibration.");
    }
}
public readonly record struct WorkshopBumper(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, BumperWork Work, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.PinballBumper;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurves.PinballBumper;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Work.Validate();
    }
}

