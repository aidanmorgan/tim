using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The one construction adapter of the C-Bumper+Spring suites and probes: a Bumper of a strength and a Springboard of a
/// stiffness, damping and preload, at an f32-input pose, as generic instances of their packages. (On the record path, 3fce7a03, the
/// same two signatures built `WorkshopBumper`/`WorkshopSpringboard`: evidence-c-bumper-spring/probe/PlacedParts-record-path.cs.txt.)</summary>
internal static class PlacedParts
{
    public static IWorkshopInstance Bumper(GpuBodyId id, double x, double y, double z, float strength = 8f,
        double qx = 0, double qy = 0, double qz = 0, double qw = 1) =>
        WorkshopInput.Generic(PartId.PinballBumper, id, x, y, z, qx, qy, qz, qw, new([new(new("strength"), strength)]));

    public static IWorkshopInstance Springboard(GpuBodyId id, double x, double y, double z, double qx, double qy, double qz, double qw,
        float compression = 0f, float stiffness = 400f, float damping = .2f) =>
        WorkshopInput.Generic(PartId.Springboard, id, x, y, z, qx, qy, qz, qw, new([new(new("stiffness"), stiffness),
            new(new("damping"), damping), new(new("initial_compression"), compression)]));
}
