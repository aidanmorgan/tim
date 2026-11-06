using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Gpu;

public enum ResidencePhase : uint { Outside, Dwelling, Qualified }
public readonly record struct ResidenceRead(GpuSensorId Id, GpuBodyId Target, ResidencePhase Phase, uint OccurrenceCount,
    uint StartOrdinal, float StartPhase, uint EventOrdinal, float EventPhase)
{
    public void Validate(uint ordinal)
    {
        if(Id.Value==0 || Target.Value==0 || !Enum.IsDefined(Phase) || OccurrenceCount>1 ||
            !PhysicalPhase.IsAdmitted(StartOrdinal,StartPhase) || !PhysicalPhase.IsAdmitted(EventOrdinal,EventPhase) ||
            StartOrdinal>ordinal || EventOrdinal>ordinal ||
            (Phase==ResidencePhase.Outside && (StartOrdinal!=0 || StartPhase!=0)) ||
            (OccurrenceCount==0 && (EventOrdinal!=0 || EventPhase!=0)) ||
            (OccurrenceCount!=0 && Phase==ResidencePhase.Qualified &&
                (EventOrdinal<StartOrdinal || (EventOrdinal==StartOrdinal && EventPhase<StartPhase))))
            throw new ArgumentException("Invalid committed residence state.");
    }
}

public readonly record struct CapturePairKey(GpuSensorId Policy, GpuBodyId Target);
