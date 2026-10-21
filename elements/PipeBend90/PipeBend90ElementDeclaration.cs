using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PipeBend90;

/// <summary>The 90° pipe bend (CAT-050): a static hollow torus segment. Its colliders are 8 slices of 8 wall boxes around the 0.65 m bore,
/// then the start and end collar rims (2 top/bottom and 2 left/right boxes each). Each row is (offset x, y, z, rotation x, y, z, w); the
/// rotation is normalised as the runtime table normalises it.</summary>
public sealed class PipeBend90ElementDeclaration : IMechanicalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "pipe_bend_90",
        Category: ElementCategory.Motion,
        DisplayNameKey: "part_pipe_bend_90",
        IconPath: null,
        ScenePath: "res://elements/PipeBend90/pipe_bend_90.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders =>
    [
        Wall(-1.3956536f, 1.3631368f, 0.0000000f, 0.0000000f, 0.0000000f, -0.0490677f, 0.9987955f),
        Wall(-1.4150318f, 1.1663858f, 0.4772971f, 0.3822225f, -0.0187774f, -0.0453326f, 0.9227667f),
        Wall(-1.4618151f, 0.6913871f, 0.6750000f, 0.7062550f, -0.0346961f, -0.0346961f, 0.7062550f),
        Wall(-1.5085984f, 0.2163883f, 0.4772971f, 0.9227667f, -0.0453326f, -0.0187774f, 0.3822225f),
        Wall(-1.5279767f, 0.0196374f, 0.0000000f, 0.9987955f, -0.0490677f, -0.0000000f, 0.0000000f),
        Wall(-1.5085984f, 0.2163883f, -0.4772971f, 0.9227667f, -0.0453326f, 0.0187774f, -0.3822225f),
        Wall(-1.4618151f, 0.6913871f, -0.6750000f, -0.7062550f, 0.0346961f, -0.0346961f, 0.7062550f),
        Wall(-1.4150318f, 1.1663858f, -0.4772971f, -0.3822225f, 0.0187774f, -0.0453326f, 0.9227667f),
        Wall(-0.8044309f, 1.2455353f, 0.0000000f, 0.0000000f, 0.0000000f, -0.1467305f, 0.9891765f),
        Wall(-0.8618210f, 1.0563454f, 0.4772971f, 0.3785415f, -0.0561513f, -0.1355613f, 0.9138799f),
        Wall(-1.0003730f, 0.5996005f, 0.6750000f, 0.6994534f, -0.1037541f, -0.1037541f, 0.6994534f),
        Wall(-1.1389251f, 0.1428557f, 0.4772971f, 0.9138799f, -0.1355613f, -0.0561513f, 0.3785415f),
        Wall(-1.1963152f, -0.0463342f, 0.0000000f, 0.9891765f, -0.1467305f, -0.0000000f, 0.0000000f),
        Wall(-1.1389251f, 0.1428557f, -0.4772971f, 0.9138799f, -0.1355613f, 0.0561513f, -0.3785415f),
        Wall(-1.0003730f, 0.5996005f, -0.6750000f, -0.6994534f, 0.1037541f, -0.1037541f, 0.6994534f),
        Wall(-0.8618210f, 1.0563454f, -0.4772971f, -0.3785415f, 0.0561513f, -0.1355613f, 0.9138799f),
        Wall(-0.2475113f, 1.0148516f, 0.0000000f, 0.0000000f, 0.0000000f, -0.2429802f, 0.9700313f),
        Wall(-0.3407078f, 0.8404932f, 0.4772971f, 0.3712149f, -0.0929845f, -0.2244844f, 0.8961920f),
        Wall(-0.5657041f, 0.4195548f, 0.6750000f, 0.6859157f, -0.1718129f, -0.1718129f, 0.6859157f),
        Wall(-0.7907004f, -0.0013837f, 0.4772971f, 0.8961920f, -0.2244844f, -0.0929845f, 0.3712149f),
        Wall(-0.8838969f, -0.1757421f, 0.0000000f, 0.9700313f, -0.2429802f, -0.0000000f, 0.0000000f),
        Wall(-0.7907004f, -0.0013837f, -0.4772971f, 0.8961920f, -0.2244844f, 0.0929845f, -0.3712149f),
        Wall(-0.5657041f, 0.4195548f, -0.6750000f, -0.6859157f, 0.1718129f, -0.1718129f, 0.6859157f),
        Wall(-0.3407078f, 0.8404932f, -0.4772971f, -0.3712149f, 0.0929845f, -0.2244844f, 0.8961920f),
        Wall(0.2537031f, 0.6799509f, 0.0000000f, 0.0000000f, 0.0000000f, -0.3368899f, 0.9415441f),
        Wall(0.1282817f, 0.5271244f, 0.4772971f, 0.3603133f, -0.1289222f, -0.3112456f, 0.8698733f),
        Wall(-0.1745124f, 0.1581688f, 0.6750000f, 0.6657722f, -0.2382171f, -0.2382171f, 0.6657722f),
        Wall(-0.4773065f, -0.2107868f, 0.4772971f, 0.8698733f, -0.3112456f, -0.1289222f, 0.3603133f),
        Wall(-0.6027279f, -0.3636132f, 0.0000000f, 0.9415441f, -0.3368899f, -0.0000000f, 0.0000000f),
        Wall(-0.4773065f, -0.2107868f, -0.4772971f, 0.8698733f, -0.3112456f, 0.1289222f, -0.3603133f),
        Wall(-0.1745124f, 0.1581688f, -0.6750000f, -0.6657722f, 0.2382171f, -0.2382171f, 0.6657722f),
        Wall(0.1282817f, 0.5271244f, -0.4772971f, -0.3603133f, 0.1289222f, -0.3112456f, 0.8698733f),
        Wall(0.6799509f, 0.2537031f, 0.0000000f, 0.0000000f, 0.0000000f, -0.4275551f, 0.9039893f),
        Wall(0.5271244f, 0.1282817f, 0.4772971f, 0.3459417f, -0.1636183f, -0.3950094f, 0.8351772f),
        Wall(0.1581688f, -0.1745124f, 0.6750000f, 0.6392170f, -0.3023271f, -0.3023271f, 0.6392170f),
        Wall(-0.2107868f, -0.4773065f, 0.4772971f, 0.8351772f, -0.3950094f, -0.1636183f, 0.3459417f),
        Wall(-0.3636132f, -0.6027279f, 0.0000000f, 0.9039893f, -0.4275551f, -0.0000000f, 0.0000000f),
        Wall(-0.2107868f, -0.4773065f, -0.4772971f, 0.8351772f, -0.3950094f, 0.1636183f, -0.3459417f),
        Wall(0.1581688f, -0.1745124f, -0.6750000f, -0.6392170f, 0.3023271f, -0.3023271f, 0.6392170f),
        Wall(0.5271244f, 0.1282817f, -0.4772971f, -0.3459417f, 0.1636183f, -0.3950094f, 0.8351772f),
        Wall(1.0148516f, -0.2475113f, 0.0000000f, 0.0000000f, 0.0000000f, -0.5141027f, 0.8577286f),
        Wall(0.8404932f, -0.3407078f, 0.4772971f, 0.3282385f, -0.1967386f, -0.4749690f, 0.7924379f),
        Wall(0.4195548f, -0.5657041f, 0.6750000f, 0.6065057f, -0.3635255f, -0.3635255f, 0.6065057f),
        Wall(-0.0013837f, -0.7907004f, 0.4772971f, 0.7924379f, -0.4749690f, -0.1967386f, 0.3282385f),
        Wall(-0.1757421f, -0.8838969f, 0.0000000f, 0.8577286f, -0.5141027f, -0.0000000f, 0.0000000f),
        Wall(-0.0013837f, -0.7907004f, -0.4772971f, 0.7924379f, -0.4749690f, 0.1967386f, -0.3282385f),
        Wall(0.4195548f, -0.5657041f, -0.6750000f, -0.6065057f, 0.3635255f, -0.3635255f, 0.6065057f),
        Wall(0.8404932f, -0.3407078f, -0.4772971f, -0.3282385f, 0.1967386f, -0.4749690f, 0.7924379f),
        Wall(1.2455353f, -0.8044309f, 0.0000000f, 0.0000000f, 0.0000000f, -0.5956993f, 0.8032075f),
        Wall(1.0563454f, -0.8618210f, 0.4772971f, 0.3073742f, -0.2279643f, -0.5503544f, 0.7420670f),
        Wall(0.5996005f, -1.0003730f, 0.6750000f, 0.5679535f, -0.4212230f, -0.4212230f, 0.5679535f),
        Wall(0.1428557f, -1.1389251f, 0.4772971f, 0.7420670f, -0.5503544f, -0.2279643f, 0.3073742f),
        Wall(-0.0463342f, -1.1963152f, 0.0000000f, 0.8032075f, -0.5956993f, -0.0000000f, 0.0000000f),
        Wall(0.1428557f, -1.1389251f, -0.4772971f, 0.7420670f, -0.5503544f, 0.2279643f, -0.3073742f),
        Wall(0.5996005f, -1.0003730f, -0.6750000f, -0.5679535f, 0.4212230f, -0.4212230f, 0.5679535f),
        Wall(1.0563454f, -0.8618210f, -0.4772971f, -0.3073742f, 0.2279643f, -0.5503544f, 0.7420670f),
        Wall(1.3631368f, -1.3956536f, 0.0000000f, 0.0000000f, 0.0000000f, -0.6715590f, 0.7409511f),
        Wall(1.1663858f, -1.4150318f, 0.4772971f, 0.2835497f, -0.2569945f, -0.6204396f, 0.6845496f),
        Wall(0.6913871f, -1.4618151f, 0.6750000f, 0.5239316f, -0.4748639f, -0.4748639f, 0.5239316f),
        Wall(0.2163883f, -1.5085984f, 0.4772971f, 0.6845496f, -0.6204396f, -0.2569945f, 0.2835497f),
        Wall(0.0196374f, -1.5279767f, 0.0000000f, 0.7409511f, -0.6715590f, -0.0000000f, 0.0000000f),
        Wall(0.2163883f, -1.5085984f, -0.4772971f, 0.6845496f, -0.6204396f, 0.2569945f, -0.2835497f),
        Wall(0.6913871f, -1.4618151f, -0.6750000f, -0.5239316f, 0.4748639f, -0.4748639f, 0.5239316f),
        Wall(1.1663858f, -1.4150318f, -0.4772971f, -0.2835497f, 0.2569945f, -0.6204396f, 0.6845496f),
        RimTb(-1.6970563f, 1.5029437f, 0f, 0f, 0f, 0f, 1f),
        RimTb(-1.6970563f, -0.0970563f, 0f, 0f, 0f, 0f, 1f),
        RimLr(-1.6970563f, 0.7029437f, -0.8f, 0f, 0f, 0f, 1f),
        RimLr(-1.6970563f, 0.7029437f, 0.8f, 0f, 0f, 0f, 1f),
        RimTb(1.5029437f, -1.6970563f, 0f, 0f, 0f, -0.7071068f, 0.7071068f),
        RimTb(-0.0970563f, -1.6970563f, 0f, 0f, 0f, -0.7071068f, 0.7071068f),
        RimLr(0.7029437f, -1.6970563f, -0.8f, 0f, 0f, -0.7071068f, 0.7071068f),
        RimLr(0.7029437f, -1.6970563f, 0.8f, 0f, 0f, -0.7071068f, 0.7071068f),
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
