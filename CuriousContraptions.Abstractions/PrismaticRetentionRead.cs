using System;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct PrismaticRetentionId(ulong Value);

/// <summary>Committed finite elastic storage, with independent source/interface observations in joules.</summary>
public readonly record struct PrismaticRetentionRead(
    PrismaticRetentionId Id, GpuBodyId Owner, PrismaticConstraintId Coordinate,
    RetentionMode Mode, RetentionPhase Phase, float Compression, float Potential,
    float AcceptedElasticEnergy, float ReleasedElasticEnergy,
    float WindingWork, float PositiveWindingTransfer, float ContactWork, float GravityWork,
    RetentionRequestResult? RequestResult)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Coordinate.Value == 0 ||
            !Enum.IsDefined(Mode) || !Enum.IsDefined(Phase) ||
            (RequestResult.HasValue && !Enum.IsDefined(RequestResult.Value)) ||
            !float.IsFinite(Compression) || Compression < 0f ||
            !float.IsFinite(Potential) || Potential < 0f ||
            !float.IsFinite(AcceptedElasticEnergy) || AcceptedElasticEnergy < 0f ||
            !float.IsFinite(ReleasedElasticEnergy) || ReleasedElasticEnergy < 0f ||
            !float.IsFinite(WindingWork) || !float.IsFinite(PositiveWindingTransfer) || PositiveWindingTransfer < 0f ||
            !float.IsFinite(ContactWork) || !float.IsFinite(GravityWork))
            throw new ArgumentException("Invalid retained-coordinate observation.");
    }
}
