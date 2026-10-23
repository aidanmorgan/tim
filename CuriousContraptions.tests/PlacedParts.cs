using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The one construction adapter of the converted suites and probes: a Bumper of a strength, a Springboard of a stiffness,
/// damping and preload, a Battery of a finite store, a Signal lamp, a Ball detector and a Domino, at an f32-input pose, as generic
/// instances of their packages. (On the record path the Bumper and Springboard signatures built `WorkshopBumper`/`WorkshopSpringboard`
/// at 3fce7a03, the Battery built `WorkshopBattery` at f27cb71d, the lamp and detector built their records at 54ffd9f0, and the
/// Domino built its record at 2065d3c5: evidence-c-bumper-spring/probe/PlacedParts-record-path.cs.txt,
/// evidence-9a-39/probe/Probe9a39Adapter-base.cs.txt, evidence-c-lamp-detector/probe/PlacedParts-record-path.cs.txt and
/// evidence-9a-32/probe/PlacedParts-record-path.cs.txt.)</summary>
internal static class PlacedParts
{
    /// <summary>A Battery with its declared store: capacity (J), power limit (W), initial charge (fraction of capacity) and the
    /// enable state as its Choice index. The defaults are the package's (3600 J, 120 W, full, Enabled).</summary>
    public static WorkshopGenericInstance Battery(GpuBodyId id, double x, double y, double z, float capacity = 3600f,
        float powerLimit = 120f, float initialCharge = 1f, ElectricalEnable enabled = ElectricalEnable.Enabled) =>
        WorkshopInput.Generic(PartId.Battery, id, x, y, z, 0, 0, 0, 1, new([new(new("capacity"), capacity),
            new(new("power_limit"), powerLimit), new(new("initial_charge"), initialCharge), new(new("enabled"), enabled switch
            {
                ElectricalEnable.Disabled => 0f,
                ElectricalEnable.Enabled => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(enabled))
            })]));

    public static IWorkshopInstance Bumper(GpuBodyId id, double x, double y, double z, float strength = 8f,
        double qx = 0, double qy = 0, double qz = 0, double qw = 1) =>
        WorkshopInput.Generic(PartId.PinballBumper, id, x, y, z, qx, qy, qz, qw, new([new(new("strength"), strength)]));

    public static IWorkshopInstance Springboard(GpuBodyId id, double x, double y, double z, double qx, double qy, double qz, double qw,
        float compression = 0f, float stiffness = 400f, float damping = .2f) =>
        WorkshopInput.Generic(PartId.Springboard, id, x, y, z, qx, qy, qz, qw, new([new(new("stiffness"), stiffness),
            new(new("damping"), damping), new(new("initial_compression"), compression)]));

    /// <summary>A Signal lamp at an f32-input pose: the lamp package's generic instance (no parameters).</summary>
    public static IWorkshopInstance Lamp(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw) =>
        WorkshopInput.Generic(PartId.SignalLamp, id, x, y, z, qx, qy, qz, qw);

    /// <summary>A Ball detector at an f32-input pose: the detector package's generic instance (no parameters).</summary>
    public static IWorkshopInstance Detector(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, bool locked = false) =>
        WorkshopInput.Generic(PartId.BallDetector, id, x, y, z, qx, qy, qz, qw) with { Locked = locked };

    /// <summary>A Domino tile at an f32-input pose: the domino package's generic instance (no parameters). (On the record path at
    /// 2065d3c5 it built `WorkshopDomino`: evidence-9a-32/probe/PlacedParts-record-path.cs.txt.)</summary>
    public static IWorkshopInstance Domino(GpuBodyId id, double x, double y, double z,
        double qx, double qy, double qz, double qw, bool locked = false) =>
        WorkshopInput.Generic(PartId.Domino, id, x, y, z, qx, qy, qz, qw) with { Locked = locked };

    /// <summary>A Pressure Plate of a minimum mass (f32 kg) at an f32-input pose: the plate package's generic instance with its
    /// declared <c>minimum_mass</c>. (On the record path at 61ded7ad it built `WorkshopPressurePlate`:
    /// evidence-c-plate/probe/PlacedParts-record-path.cs.txt.)</summary>
    public static IWorkshopInstance Plate(GpuBodyId id, double x, double y, double z, float minimumMass,
        double qx = 0, double qy = 0, double qz = 0, double qw = 1, bool locked = false) =>
        WorkshopInput.Generic(PartId.PressurePlate, id, x, y, z, qx, qy, qz, qw,
            new([new(new("minimum_mass"), minimumMass)])) with { Locked = locked };
}
