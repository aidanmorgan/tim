using System;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

/// <summary>An exact constraint coordinate. Raw identities are decoded only at serialization boundaries.</summary>
public readonly record struct ConstraintCoordinateReference
{
    public CoordinateKind Kind { get; }
    public PrismaticConstraintId Prismatic { get; }
    public RevoluteConstraintId Revolute { get; }

    public ConstraintCoordinateReference(PrismaticConstraintId joint)
    {
        Kind = CoordinateKind.Prismatic;
        Prismatic = joint;
        Revolute = default;
        Validate();
    }

    public ConstraintCoordinateReference(RevoluteConstraintId joint)
    {
        Kind = CoordinateKind.Revolute;
        Prismatic = default;
        Revolute = joint;
        Validate();
    }

    public void Validate()
    {
        var valid = Kind switch
        {
            CoordinateKind.Prismatic => Prismatic.Value != 0 && Revolute == default,
            CoordinateKind.Revolute => Revolute.Value != 0 && Prismatic == default,
            _ => false
        };
        if (!valid) throw new ArgumentException("Invalid constraint coordinate reference.");
    }
}
