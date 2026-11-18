using System;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Abstractions;

public readonly record struct PrismaticPhaseValues(float Holding, float Positive, float Negative,
    float Blocked, float Conflict, float Unpowered)
{
    public bool IsFinite() => float.IsFinite(Holding) && float.IsFinite(Positive) && float.IsFinite(Negative) &&
        float.IsFinite(Blocked) && float.IsFinite(Conflict) && float.IsFinite(Unpowered);
    public float Map(PrismaticDrivePhase phase) => phase switch
    {
        PrismaticDrivePhase.Holding => Holding, PrismaticDrivePhase.Positive => Positive,
        PrismaticDrivePhase.Negative => Negative, PrismaticDrivePhase.Blocked => Blocked,
        PrismaticDrivePhase.Conflict => Conflict, PrismaticDrivePhase.Unpowered => Unpowered,
        _ => throw new ArgumentException("Undefined prismatic phase.")
    };
}
