namespace CuriousContraptions.Gpu;

/// <summary>Demand-grown geometry scratch owned by one execution context, never shared across concurrent callers.</summary>
public sealed class MaterialQueryWorkspace : IMaterialQueryWorkspace
{
    private readonly MaterialSweepQuery _sweep = new();
    private readonly CylindricalContainmentQuery _containment = new();

    public MaterialQueryResult SweepMaterial(HollowPartner moving, HollowPartner obstacle, System.Numerics.Vector3 translation) =>
        _sweep.Sweep(moving, obstacle, translation);

    public MaterialContainmentResult ContainsMaterial(HollowPartner region, HollowPartner shape, float allowance) =>
        _containment.Contains(region, shape, allowance);
}
