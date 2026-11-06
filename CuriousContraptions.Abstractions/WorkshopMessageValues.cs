using System;
using System.Linq;
using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Every Workshop command and query (closed set). <see cref="WorkshopCommandKind.ObserveMemory"/> is the stopped memory
/// observation query (Story 9A.3), answered by its acknowledgement's <see cref="WorkshopResponse.Memory"/>. Wire values are explicit:
/// 11 named the deleted electrical control and is undefined.</summary>
public enum WorkshopCommandKind : uint { Initialize = 0, Construct = 1, Run = 2, Reset = 3, Dispose = 4, Cancel = 5, ConfigureCadence = 6,
    Pause = 7, Resume = 8, Step = 9, Save = 10, ObserveMemory = 12, CutRope = 13, ConfigureRunControl = 14 }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, ICompiledScene? Scene, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact, RuntimeSessionId Session = default,
    CadenceRevision Cadence = default, ProjectionEpoch Projection = default, WorkshopRunControl? RunControl = null, RopeSpanId? Cut = null);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision, CadenceRevision Cadence, ProjectionEpoch Projection);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read, RuntimeSessionId Session = default, PublicationSequence Publication = default,
    CadenceRevision Cadence = default, ClockGeneration MasterGeneration = default,
    ProjectionEpoch Projection = default, PulseOrdinal SourcePulse = default, WorkshopMemoryMeasurement? Memory = null, ICompiledScene? Scene = null);
