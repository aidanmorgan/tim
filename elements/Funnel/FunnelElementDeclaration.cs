using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.Funnel;

/// <summary>The Funnel (CAT-030): a static hollow frustum 1.8 m long that gathers balls from a 1.3 m inlet radius into the 0.65 m tube
/// bore. Its colliders are 4 rings of 16 wall boxes tilted to the taper, then 4 collar rim boxes at the inlet and 4 at the outlet.</summary>
public sealed class FunnelElementDeclaration : IMechanicalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "funnel",
        Category: ElementCategory.Motion,
        DisplayNameKey: "part_funnel",
        IconPath: null,
        ScenePath: "res://elements/Funnel/funnel.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0);

    public IReadOnlyList<PortDescriptor> Ports => [];

    public IReadOnlyList<ColliderDescriptor> Colliders
    {
        get
        {
            var colliders = new List<ColliderDescriptor>(72);
            const int rings = 4;
            const int segments = 16;
            const float length = 1.8f;
            const float inletRadius = 1.3f;
            const float outletRadius = 0.65f;
            const float slope = (inletRadius - outletRadius) / length;
            var alpha = MathF.Atan(slope);
            var cosAlpha = MathF.Cos(alpha);
            const float ringLength = length / rings;
            const float wallLength = 0.485f;
            const float thickness = 0.05f;
            var halfThickness = thickness * 0.5f;
            float[] wallWidths = [0.52f, 0.46f, 0.39f, 0.32f];
            var halfAlpha = alpha * 0.5f;
            var sinA = MathF.Sin(halfAlpha);
            var cosA = MathF.Cos(halfAlpha);
            for (var k = 0; k < rings; k++)
            {
                var x = -0.9f + (k + 0.5f) * ringLength;
                var innerRadius = 0.975f - slope * x;
                var midRadius = innerRadius + halfThickness / cosAlpha;
                var halfExtents = new Vector3(wallLength * 0.5f, thickness * 0.5f, wallWidths[k] * 0.5f);
                for (var i = 0; i < segments; i++)
                {
                    var theta = i * (MathF.Tau / segments);
                    var halfTheta = theta * 0.5f;
                    var sinT = MathF.Sin(halfTheta);
                    var cosT = MathF.Cos(halfTheta);
                    var rotation = Quaternion.Normalize(new Quaternion(sinT * cosA, sinT * sinA, -cosT * sinA, cosT * cosA));
                    colliders.Add(Box(halfExtents, new Vector3(x, midRadius * MathF.Cos(theta), midRadius * MathF.Sin(theta)), rotation));
                }
            }
            colliders.Add(Box(new Vector3(0.05f, 0.15f, 1.45f), new Vector3(-0.9f, 1.45f, 0), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 0.15f, 1.45f), new Vector3(-0.9f, -1.45f, 0), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 1.30f, 0.15f), new Vector3(-0.9f, 0, -1.45f), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 1.30f, 0.15f), new Vector3(-0.9f, 0, 1.45f), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 0.15f, 0.8f), new Vector3(0.9f, 0.8f, 0), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 0.15f, 0.8f), new Vector3(0.9f, -0.8f, 0), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 0.65f, 0.15f), new Vector3(0.9f, 0, -0.8f), Quaternion.Identity));
            colliders.Add(Box(new Vector3(0.05f, 0.65f, 0.15f), new Vector3(0.9f, 0, 0.8f), Quaternion.Identity));
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
