using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PipeBend45;

/// <summary>The 45° pipe bend (CAT-049): a static hollow torus segment, a distinct element from the 90° bend. Its colliders are 4 slices of
/// 8 wall boxes around the 0.65 m bore, then the start and end collar rims (2 top/bottom and 2 left/right boxes each). Each row is
/// (offset x, y, z, rotation x, y, z, w); the rotation is normalised as the runtime table normalises it.</summary>
public sealed class PipeBend45ElementDeclaration : IMechanicalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "pipe_bend_45",
        Category: ElementCategory.Motion,
        DisplayName: "45° pipe bend",
        Description: "A 45-degree hollow curve. Turns falling balls smoothly through a gentle angle under gravity.",
        Color: new(0.4f, 0.72f, 0.79f, 1f),
        PaletteOrder: 17,
        IconPath: "res://elements/PipeBend45/icon.svg",
        ScenePath: "res://elements/PipeBend45/pipe_bend_45.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        Wall(-0.6170375f, 0.8428822f, 0f, 0.0000000f, 0.0000000f, -0.0490677f, 0.9987955f),
        Wall(-0.6364158f, 0.6461312f, 0.4772971f, 0.3822225f, -0.0187774f, -0.0453326f, 0.9227667f),
        Wall(-0.6831991f, 0.1711325f, 0.6750000f, 0.7062550f, -0.0346961f, -0.0346961f, 0.7062550f),
        Wall(-0.7299824f, -0.3038663f, 0.4772971f, 0.9227667f, -0.0453326f, -0.0187774f, 0.3822225f),
        Wall(-0.7493607f, -0.5006172f, 0f, 0.9987955f, -0.0490677f, -0.0000000f, 0.0000000f),
        Wall(-0.7299824f, -0.3038663f, -0.4772971f, 0.9227667f, -0.0453326f, 0.0187774f, -0.3822225f),
        Wall(-0.6831991f, 0.1711325f, -0.6750000f, -0.7062550f, 0.0346961f, -0.0346961f, 0.7062550f),
        Wall(-0.6364158f, 0.6461312f, -0.4772971f, -0.3822225f, 0.0187774f, -0.0453326f, 0.9227667f),
        Wall(-0.0258149f, 0.7252807f, 0f, 0.0000000f, 0.0000000f, -0.1467305f, 0.9891765f),
        Wall(-0.0832050f, 0.5360908f, 0.4772971f, 0.3785415f, -0.0561513f, -0.1355613f, 0.9138799f),
        Wall(-0.2217570f, 0.0793459f, 0.6750000f, 0.6994534f, -0.1037541f, -0.1037541f, 0.6994534f),
        Wall(-0.3603090f, -0.3773989f, 0.4772971f, 0.9138799f, -0.1355613f, -0.0561513f, 0.3785415f),
        Wall(-0.4176992f, -0.5665888f, 0f, 0.9891765f, -0.1467305f, -0.0000000f, 0.0000000f),
        Wall(-0.3603090f, -0.3773989f, -0.4772971f, 0.9138799f, -0.1355613f, 0.0561513f, -0.3785415f),
        Wall(-0.2217570f, 0.0793459f, -0.6750000f, -0.6994534f, 0.1037541f, -0.1037541f, 0.6994534f),
        Wall(-0.0832050f, 0.5360908f, -0.4772971f, -0.3785415f, 0.0561513f, -0.1355613f, 0.9138799f),
        Wall(0.5311047f, 0.4945970f, 0f, 0.0000000f, 0.0000000f, -0.2429802f, 0.9700313f),
        Wall(0.4379082f, 0.3202386f, 0.4772971f, 0.3712149f, -0.0929845f, -0.2244844f, 0.8961920f),
        Wall(0.2129119f, -0.1006998f, 0.6750000f, 0.6859157f, -0.1718129f, -0.1718129f, 0.6859157f),
        Wall(-0.0120844f, -0.5216383f, 0.4772971f, 0.8961920f, -0.2244844f, -0.0929845f, 0.3712149f),
        Wall(-0.1052809f, -0.6959967f, 0f, 0.9700313f, -0.2429802f, -0.0000000f, 0.0000000f),
        Wall(-0.0120844f, -0.5216383f, -0.4772971f, 0.8961920f, -0.2244844f, 0.0929845f, -0.3712149f),
        Wall(0.2129119f, -0.1006998f, -0.6750000f, -0.6859157f, 0.1718129f, -0.1718129f, 0.6859157f),
        Wall(0.4379082f, 0.3202386f, -0.4772971f, -0.3712149f, 0.0929845f, -0.2244844f, 0.8961920f),
        Wall(1.0323191f, 0.1596963f, 0f, 0.0000000f, 0.0000000f, -0.3368899f, 0.9415441f),
        Wall(0.9068977f, 0.0068698f, 0.4772971f, 0.3603133f, -0.1289222f, -0.3112456f, 0.8698733f),
        Wall(0.6041036f, -0.3620858f, 0.6750000f, 0.6657722f, -0.2382171f, -0.2382171f, 0.6657722f),
        Wall(0.3013096f, -0.7310414f, 0.4772971f, 0.8698733f, -0.3112456f, -0.1289222f, 0.3603133f),
        Wall(0.1758882f, -0.8838678f, 0f, 0.9415441f, -0.3368899f, -0.0000000f, 0.0000000f),
        Wall(0.3013096f, -0.7310414f, -0.4772971f, 0.8698733f, -0.3112456f, 0.1289222f, -0.3603133f),
        Wall(0.6041036f, -0.3620858f, -0.6750000f, -0.6657722f, 0.2382171f, -0.2382171f, 0.6657722f),
        Wall(0.9068977f, 0.0068698f, -0.4772971f, -0.3603133f, 0.1289222f, -0.3112456f, 0.8698733f),
        RimTb(-0.9184402f, 0.9826891f, 0f, 0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f),
        RimTb(-0.9184402f, -0.6173109f, 0f, 0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f),
        RimLr(-0.9184402f, 0.1826891f, -0.8000000f, 0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f),
        RimLr(-0.9184402f, 0.1826891f, 0.8000000f, 0.0000000f, 0.0000000f, 0.0000000f, 1.0000000f),
        RimTb(1.3443015f, 0.0454308f, 0f, 0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f),
        RimTb(0.2129306f, -1.0859400f, 0f, 0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f),
        RimLr(0.7786160f, -0.5202546f, -0.8000000f, 0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f),
        RimLr(0.7786160f, -0.5202546f, 0.8000000f, 0.0000000f, 0.0000000f, -0.3826834f, 0.9238795f),
    ];

    public IReadOnlyList<EventDescriptor> Events => [];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];

    private static ColliderDescriptor Wall(float x, float y, float z, float qx, float qy, float qz, float qw) =>
        Box(new Vector3(0.26f, 0.025f, 0.29f), x, y, z, qx, qy, qz, qw);

    private static ColliderDescriptor RimTb(float x, float y, float z, float qx, float qy, float qz, float qw) =>
        Box(new Vector3(0.05f, 0.15f, 0.8f), x, y, z, qx, qy, qz, qw);

    private static ColliderDescriptor RimLr(float x, float y, float z, float qx, float qy, float qz, float qw) =>
        Box(new Vector3(0.05f, 0.65f, 0.15f), x, y, z, qx, qy, qz, qw);

    private static ColliderDescriptor Box(Vector3 halfExtents, float x, float y, float z, float qx, float qy, float qz, float qw) =>
        new(ColliderShapeKind.Box, halfExtents, new Vector3(x, y, z), Quaternion.Normalize(new Quaternion(qx, qy, qz, qw)), Surface);
}
