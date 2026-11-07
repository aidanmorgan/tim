using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public interface IRunControlServices
{
    RunControlValueKind ValueKind(RunControlLane lane);
    bool Resolves(RunControlRow row, IPhysicsSceneDeclaration physics);
    bool BoundsAdmitted(RunControlRow row);
    bool WithinBounds(RunControlRow row, RunControlValue value);
    int Slot(IPhysicsSceneDeclaration physics, RunControlLane lane, GpuBodyId owner, ElectricalSourceId source = default, RevoluteConstraintId joint = default);
    RunControlValue Read(WorkshopRead read, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source = default, RevoluteConstraintId joint = default);
    RunControlValue Read(WorkshopRead read, RunControlRow row);
    RunControlRow? Find(IEnumerable<RunControlRow> rows, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source = default, int laneIndex = -1);
}
