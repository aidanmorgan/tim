namespace CuriousContraptions.Gpu;

/// <summary>Authored CAT-037 Set/Reset State Latch. Memory lives in the committed activation checkpoint; the record carries placement only.
/// Its switched supply is compiled once at Run by <see cref="WorkshopPhysicsCompiler"/>.</summary>
public readonly record struct WorkshopLatch(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    bool Locked = false) : IWorkshopInstance
{
    public PartId PartId => PartId.Latch;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
    }
}
