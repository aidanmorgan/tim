using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Pipe;

/// <summary>The Clear pipe (CAT-048): a static hollow tube whose length is a typed parameter (1 to 8 m, default 3.6 m). Its colliders are
/// declared at the default length: 16 shell boxes around the 0.65 m bore, spanning half the length, and 8 collar rim boxes at each end.</summary>
public sealed class PipeElementDeclaration : IMechanicalElement
{
    private const float DefaultLength = 3.6f;

    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "pipe",
        Category: ElementCategory.Motion,
        DisplayNameKey: "part_pipe",
        IconPath: null,
        ScenePath: "res://elements/Pipe/pipe.tscn",
        Parameters:
        [
            new("length", ParameterUnit.Metre, ParameterKind.Real, Minimum: 1f, Maximum: 8f, Default: DefaultLength, Options: [])
        ]);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders
    {
        get
        {
            var halfLength = DefaultLength * 0.5f;
            var colliders = new List<ColliderDescriptor>(24);
            const float shellRadius = 0.675f;
            for (var i = 0; i < 16; i++)
            {
                var theta = i * (MathF.Tau / 16f);
                var halfTheta = theta * 0.5f;
                var rotation = Quaternion.Normalize(new Quaternion(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta)));
                colliders.Add(Box(new Vector3(halfLength, 0.025f, 0.14f),
                    new Vector3(0, shellRadius * MathF.Cos(theta), shellRadius * MathF.Sin(theta)), rotation));
            }
            foreach (var x in new[] { -halfLength, halfLength })
            {
                colliders.Add(Box(new Vector3(0.05f, 0.15f, 0.8f), new Vector3(x, 0.8f, 0), Quaternion.Identity));
                colliders.Add(Box(new Vector3(0.05f, 0.15f, 0.8f), new Vector3(x, -0.8f, 0), Quaternion.Identity));
                colliders.Add(Box(new Vector3(0.05f, 0.65f, 0.15f), new Vector3(x, 0, -0.8f), Quaternion.Identity));
                colliders.Add(Box(new Vector3(0.05f, 0.65f, 0.15f), new Vector3(x, 0, 0.8f), Quaternion.Identity));
            }
            return colliders;
        }
    }

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

    private static ColliderDescriptor Box(Vector3 halfExtents, Vector3 offset, Quaternion rotation) =>
        new(ColliderShapeKind.Box, halfExtents, offset, rotation, Surface);
}
