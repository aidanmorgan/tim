using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Elements.PoweredGate;

/// <summary>The gate's static frame. A placed gate also compiles its moving blade, a second body on a prismatic slider (the footprint
/// counts both); declaring the blade's own collider and material belongs to Story 9A.16.</summary>
public sealed class PoweredGateElementDeclaration : IMechanicalElement, IElectricalElement
{
    private static readonly MaterialProperties Surface = new(Restitution: 1f, Friction: 0.3f, RollingResistance: 0f, BounceThreshold: 0.1f);

    public ElementMetadata Metadata => new(
        Id: "powered_gate",
        Category: ElementCategory.Motion,
        DisplayName: "Powered gate",
        Description: "Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes.",
        Color: new(0.97f, 0.796f, 0.322f, 1f),
        PaletteOrder: 20,
        IconPath: "res://elements/PoweredGate/icon.svg",
        ScenePath: "res://elements/PoweredGate/powered_gate.tscn",
        Parameters: []);

    public ElementFootprint Footprint => new(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 1);

    public IReadOnlyList<PortDescriptor> Ports =>
    [
        new("PowerIn", SimulationDomain.Electrical, PortDirection.Input, null, new Vector3(0f, 1.65f, .92f), Vector3.UnitZ)
    ];

    /// <summary>The 16-box ring around the 0.65 m bore (each box turned about +X to its angle), the motor housing above it and the
    /// two blade rails.</summary>
    public IReadOnlyList<ColliderDescriptor> Colliders
    {
        get
        {
            var colliders = new List<ColliderDescriptor>(19);
            const float ringRadius = 0.675f;
            for (var i = 0; i < 16; i++)
            {
                var theta = i * (MathF.Tau / 16f);
                var halfTheta = theta * 0.5f;
                var rotation = Quaternion.Normalize(new Quaternion(MathF.Sin(halfTheta), 0, 0, MathF.Cos(halfTheta)));
                colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.4f, 0.025f, 0.14f),
                    new Vector3(0, ringRadius * MathF.Cos(theta), ringRadius * MathF.Sin(theta)), rotation, Surface));
            }
            colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.4f, 0.325f, 0.9f), new Vector3(0, 1.65f, 0), Quaternion.Identity, Surface));
            colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.12f, 0.85f, 0.06f), new Vector3(0, 0.65f, -0.82f), Quaternion.Identity, Surface));
            colliders.Add(new(ColliderShapeKind.Box, new Vector3(0.12f, 0.85f, 0.06f), new Vector3(0, 0.65f, 0.82f), Quaternion.Identity, Surface));
            return colliders;
        }
    }

    public IReadOnlyList<EventDescriptor> Events =>
    [
        new(SimulationEventKind.PrismaticLimit, "Position:f32;Lower:bool;Upper:bool")
    ];

    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical | SimulationDomain.Electrical;

    public KinematicMode KinematicMode => KinematicMode.Static;

    public RigidBodyMassProperties MassProperties => new(
        Mass: 0f,
        CentreOfMass: Vector3.Zero,
        InertiaTensor: Vector3.Zero,
        LinearDrag: 0f,
        Gravity: Vector3.Zero);

    /// <summary>The blade slides along the frame's +Y.</summary>
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints =>
    [
        new("slider", Vector3.Zero, Quaternion.Identity, Vector3.UnitY)
    ];

    public IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes =>
    [
        new("motor", ElectricalRole.Consumer, RatedVoltage: 12f, Resistance: 24f, Capacitance: 0f, MaxPower: 20f)
    ];
}
