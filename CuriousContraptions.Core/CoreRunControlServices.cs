using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreRunControlServices : IRunControlServices
{
    public RunControlValueKind ValueKind(RunControlLane lane) => RunControlLanes.ValueKind(lane);
    public bool Resolves(RunControlRow row, IPhysicsSceneDeclaration physics) => RunControlLanes.Resolves(row, physics);
    public bool BoundsAdmitted(RunControlRow row) => RunControlLanes.BoundsAdmitted(row);
    public bool WithinBounds(RunControlRow row, RunControlValue value) => RunControlLanes.WithinBounds(row, value);
    public int Slot(IPhysicsSceneDeclaration physics, RunControlLane lane, GpuBodyId owner) => RunControlLanes.Slot(physics, lane, owner);
    public RunControlValue Read(WorkshopRead read, GpuBodyId owner, RunControlLane lane) => RunControlLanes.RunControl(read, owner, lane);
    public RunControlRow? Find(IEnumerable<RunControlRow> rows, GpuBodyId owner, RunControlLane lane) => RunControlLanes.Find(rows, owner, lane);
}
