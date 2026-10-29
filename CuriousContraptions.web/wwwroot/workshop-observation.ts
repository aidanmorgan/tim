// The browser-facing port receives already decoded, managed-admitted DTOs.
// No command, wire decoder, shared view or presentation consumer is reachable through it.
export enum ObservationRefusal { None, Retired, ForeignSession, UnknownQuery, NotCommitted, ClosedCursor, InvalidCursor }
export enum ObservationQuery { Committed }
// Numeric interop identifiers match the canonical managed enums; C# validates them before projection.
export enum ActivationNodeKind { ContactSource = 1, StickyFirstInput = 2, Timer = 3, OrientationSource = 4, PassageSource = 5, Counter = 6, Oscillator = 7, StateLatch = 8, LevelIndicator = 9, ContactLoadSource = 10 }
export enum ActivationOccurrenceKind { Contact = 1, TimerElapsed = 2, Orientation = 3, Passage = 4, ThresholdReached = 5, OscillatorTick = 6, ContactLoad = 7 }
export enum ActivationPhase { Clear, Latched }
export enum ActivationTimerPhase { Ready, Counting, Finished }
export enum AnimationCadence { Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4 }
export enum AnimationDeferralCause { Preparation = 1, Recalibrating = 2 }
export enum AnimationOutputKind { Acknowledgement = 1, Sample = 2, Rejected = 3 }
export enum AnimationProperty { LocalRotationAngle = 1, LocalTranslation = 2, Opacity = 3, UniformScale = 4, ColourBlend = 5, ColourRed = 6, ColourGreen = 7, ColourBlue = 8 }
export enum AnimationTimerPhase { None, Ready, Counting, Finished }
export enum CaptureLatchPhase { Clear, Latched }
export enum ClockDerivationOutcome { Consistent, Slewed, Rederived, Inconclusive, Unanswered }
export enum ClockDerivationReport { Check, Startup, Summary }
export enum ClockMappingState { Calibrating, Ready, Suspect, Expired, Retired, Recovering, Renewing }
export enum ClockProbeOutcome { Accepted, WrongIdentity, Reversed, NegativeRoundTrip, Disjoint, Expired, ProfileRange, Malformed, Inconclusive }
export enum ClockRecalibrationCause { Elapsed, ProbeLimit, MappingLifetime, Disjoint, ProfileRange, MasterRederived, StallPastMappedWidth }
export enum ContactLoadPhase { Empty = 0, Underweight = 1, Loaded = 2 }
export enum ElectricalEnable { Disabled = 0, Enabled = 1 }
export enum ElectricalInputState { Neither=0, FirstOnly=1, SecondOnly=2, Both=3 }
export enum ExpectedRevisionKind { Any = 1, Exact = 2 }
export enum FaultSite { None = 0, Initialize = 1, Construct, Advance, Reset, Startup, LateDispose, RunLoop, StaleRunLoop, Presentation, Disposal, TraceFlush, Installation, UiHandler, WorkerEnvelope, Transport }
export enum OscillatorPhase { Stopped = 0, Running = 1 }
export enum PhysicalStepProfile { Canonical480Hz = 1 }
export enum PresentationCadence { AdmittedDisplay = 0, Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4, Hz144 = 5 }
export enum PresentationQuality { AwaitingHistory, Interpolated, ClockInvalid, Stale, Terminal, Faulted, Retired }
export enum SimulationCadence { Hz60 = 1, Hz120 = 2, Hz240 = 3 }
export enum SimulationDomain { None = 0, Mechanical = 1 << 0, Electrical = 1 << 1, Signal = 1 << 2, Fluid = 1 << 3, Audio = 1 << 4 }
export enum SimulationEventKind { None = 0, Impact = 1, Passage = 2, ThresholdReached = 3, MemoryChanged = 4, ContactLoad = 5, Captured = 6, ElectricalPowered = 7, TriggerActivated = 8 }
export enum TraceEnd { Complete = 0, Reset = 1, Disposed = 2, Fault = 4, InvalidRecord = 5, Overflow = 6 }
export enum TraceTimingStatus { Complete, Incomplete }
export enum WorkshopCaptureKind { Pose, Timing }
export enum WorkshopCaptureStage { Begin, Chunk, End }
export enum WorkshopClockDomain { BrowserMonotonic = 1, SimulationMonotonic = 2 }
export enum WorkshopCommandKind { Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save, ConfigureElectrical, ObserveMemory }
export enum WorkshopCommandOutcome { Applied, Rejected, Superseded, Faulted, Cancelled }
export enum WorkshopConnectionDomain { Activation = 1, Electrical = 2 }
export enum WorkshopFaultKind { Fault, Rejection, AfterRetirement }
export enum WorkshopGoalKind { None, Captured, ActivatedAfter }
export enum WorkshopPlacementMode { Manual }
export enum WorkshopRejection { None = 0, Busy = 1, InvalidConstruction = 2, WrongRevision = 3, WrongPhase = 4, GpuAdmission = 5, InvalidRead = 7, IdentityExhausted = 8, StaleGeneration = 9, Cancelled = 10, AlreadyCommitted = 11, Capacity = 12, ReliableStalled = 13, Transport = 14 }
export enum WorkshopRuntimeRole { Browser = 1, Simulation = 2, Animation = 3 }
export enum WorkshopSimulationPhase { Uninitialized, Initializing, Building, Admitting, Starting, Running, Paused, Completed, Resetting, Faulted, Disposed }
export enum WorkshopSocket { ActivationOut = 1, SetIn = 2, ResetIn = 3, ActivationIn = 4, FirstIn = 5, SecondIn = 6, Supply = 7, PowerIn = 8 }
// Event/cause phases are fractional time quantities, not closed state sets.
export type ObservationValue = null | boolean | number | string | ObservationObject | readonly ObservationValue[];
export interface ObservationObject { readonly [field: string]: ObservationValue; }
export interface ObservationResult<T> { readonly refusal: ObservationRefusal; readonly value?: T; }
export interface ObservationVector { readonly x: number; readonly y: number; readonly z: number; }
export interface ObservationRotation extends ObservationVector { readonly w: number; }
export interface ObservedBody {
    readonly id: string; readonly cell: ObservationVector; readonly local: ObservationVector;
    readonly velocity: ObservationVector; readonly rotation: ObservationRotation;
    readonly angularVelocity: ObservationVector; readonly localCentreOfMass: ObservationVector;
}
export interface ObservedCapture {
    readonly sensor: string; readonly target: string; readonly phase: CaptureLatchPhase;
    readonly ordinal: number; readonly eventPhase: number;
}
export interface ObservedElectrical {
    readonly id: string; readonly owner: string; readonly remaining: number; readonly debit: number; readonly enabled: ElectricalEnable;
}
export interface CommittedObservation {
    readonly epoch: string; readonly tick: string; readonly revision: string; readonly phase: WorkshopSimulationPhase;
    readonly cadence: string; readonly projection: string;
    readonly bodies: readonly ObservedBody[]; readonly captures: readonly ObservedCapture[];
    readonly electrical: readonly ObservedElectrical[];
    readonly sequence: string; readonly publication: string; readonly masterGeneration: string; readonly sourcePulse: string;
    readonly outcome: WorkshopCommandOutcome; readonly reason: WorkshopRejection; readonly capture?: ObservedClockStamp;
    readonly activations: readonly ObservedActivation[]; readonly timers: readonly ObservedTimer[];
    readonly contactWorks: readonly ObservedContactWork[]; readonly memory: readonly ObservedMemory[];
    readonly actuators: readonly ObservedActuator[]; readonly loads: readonly ObservedLoad[]; readonly logic: readonly ObservedLogic[];
}
export interface CommandReceiptObservation {
    readonly ordinal: string; readonly sequence: string; readonly kind: WorkshopCommandKind;
    readonly outcome: WorkshopCommandOutcome; readonly reason: WorkshopRejection; readonly appliedToPresentation: boolean;
}
export interface EventObservation {
    readonly sequence: string; readonly tick: string; readonly kind: SimulationEventKind; readonly domain: SimulationDomain;
    readonly source: string; readonly other: string; readonly ordinal: number; readonly phase: number;
    readonly magnitude: number; readonly count: number; readonly level: number;
}
export interface EventDeliveryObservation {
    readonly epoch: string; readonly skipped: string; readonly records: readonly EventObservation[];
}

export interface ObservedClockStamp { readonly domain: WorkshopClockDomain; readonly generation: string; readonly time: string; readonly uncertainty: string; }
export interface ObservedOccurrence {
    readonly kind?: ActivationOccurrenceKind; readonly emitter: string; readonly ordinal: number; readonly eventPhase: number;
    readonly source: string; readonly body: string; readonly collider: string; readonly causeOrdinal: number;
    readonly causePhase: number; readonly approachSpeed: number;
}
export interface ObservedActivation extends ObservedOccurrence { readonly node: string; readonly owner: string; readonly phase: ActivationPhase; }
export interface ObservedTimer { readonly node: string; readonly phase: ActivationTimerPhase; readonly startedTick: string; readonly dueTick: string; readonly input: ObservedOccurrence; }
export interface ObservedContactWork { readonly id: string; readonly owner: string; readonly count: number; readonly remaining: number; readonly supplied: number; }
export interface ObservedMemory { readonly node: string; readonly kind: ActivationNodeKind; readonly count: number; readonly ordinal: number; readonly eventPhase: number; readonly phase: OscillatorPhase; readonly dueTick: string; }
export interface ObservedActuator { readonly joint: string; readonly owner: string; readonly supplyClosed: boolean; readonly movingContact: boolean; readonly coordinate: number; readonly speed: number; readonly lower: number; readonly upper: number; readonly blocked: boolean; }
export interface ObservedLoad { readonly sensor: string; readonly frame: string; readonly region: string; readonly mass: number; readonly phase: ContactLoadPhase; }
export interface ObservedLogic { readonly owner: string; readonly inputs: ElectricalInputState; readonly supplied: boolean; readonly active: boolean; }
export enum DiagnosticCategory { Fault, Rejection, AfterRetirement, ClockRecalibrated, ClockReplyRejected, ClockRenewedLate, ClockOffsetWide, ClockDerivation, AnimationControlDeferred, DebtFault, TickFailed, BalanceTransfer, Capture, ClockSample, AnimationSample, Presentation }
export interface ObservedInterval { readonly lower: string; readonly upper: string; }
export interface ObservedPeer { readonly sessionLow: string; readonly sessionHigh: string; readonly generation: string; readonly requesterGeneration: string; readonly role: WorkshopRuntimeRole; readonly origin: string; readonly uncertainty: string; }
export interface ObservedProbe { readonly sessionLow: string; readonly sessionHigh: string; readonly generation: string; readonly requesterGeneration: string; readonly role: WorkshopRuntimeRole; readonly sequence: string; readonly sent: string; }
export interface ObservedReply { readonly probe: ObservedProbe; readonly masterReceived: string; readonly masterSent: string; readonly received: string; readonly derivation: number; }
export interface ObservedClockSlot extends ObservedInterval { readonly receipt: string; readonly sequence: string; }
export interface FaultDiagnostic { readonly category: DiagnosticCategory.Fault | DiagnosticCategory.Rejection | DiagnosticCategory.AfterRetirement; readonly kind: WorkshopFaultKind; readonly site: FaultSite; readonly exceptionType: string; readonly detail: string; }
export interface RecalibrationDiagnostic { readonly category: DiagnosticCategory.ClockRecalibrated; readonly peer: ObservedPeer; readonly cause: ClockRecalibrationCause; readonly count: string; readonly stateBefore: ClockMappingState; readonly derivation?: number; readonly displayEpoch: string; readonly at: string; readonly recoveryStarted: string; readonly recoverySends: number; readonly pending?: ObservedProbe; readonly reply?: ObservedReply; readonly slots: readonly ObservedClockSlot[]; }
export interface RejectedClockDiagnostic { readonly category: DiagnosticCategory.ClockReplyRejected; readonly role: WorkshopRuntimeRole; readonly count: string; readonly length: number; readonly received: string; readonly detail: string; }
export interface LateRenewalDiagnostic { readonly category: DiagnosticCategory.ClockRenewedLate; readonly role: WorkshopRuntimeRole; readonly count: string; readonly lapseAge: string; readonly exchanges: number; readonly renewedAt: string; readonly retained: ObservedInterval; readonly fresh: ObservedInterval; readonly derivation?: number; readonly displayEpoch: string; }
export interface WideOffsetDiagnostic { readonly category: DiagnosticCategory.ClockOffsetWide; readonly offset: string; readonly admittedLower: string; readonly admittedUpper: string; readonly declaredUncertainty: string; readonly count: string; }
export interface DerivationDiagnostic {
    readonly category: DiagnosticCategory.ClockDerivation; readonly report: ClockDerivationReport; readonly outcome: ClockDerivationOutcome; readonly epoch: number;
    readonly correction: string; readonly pendingCorrection?: string; readonly admittedLower?: string; readonly admittedUpper?: string;
    readonly exchange?: { readonly before: string; readonly master: string; readonly after: string; readonly epoch: number };
    readonly qualified: number; readonly startupAttempts: number; readonly startupOutliers: number; readonly startupInconclusive: number;
    readonly startupUnanswered: number; readonly checks: string; readonly slews: string; readonly rederivations: string;
    readonly inconclusive: string; readonly unanswered: string; readonly checkRetries: string;
}
export interface DeferralDiagnostic { readonly category: DiagnosticCategory.AnimationControlDeferred; readonly cause: AnimationDeferralCause; readonly count: string; }
export interface DebtDiagnostic { readonly category: DiagnosticCategory.DebtFault; readonly tick: string; readonly reason: WorkshopRejection; readonly oldestUnreceipted: string; readonly ageMilliseconds: string; }
export interface TickFailureDiagnostic { readonly category: DiagnosticCategory.TickFailed; readonly outcome: WorkshopCommandOutcome; readonly reason: WorkshopRejection; readonly fault: WorkshopRejection; }
export interface TransferDiagnostic { readonly category: DiagnosticCategory.BalanceTransfer; readonly response: CommittedObservation; }
export interface ObservedAssistanceKnot {
    readonly precision: number; readonly positionWindow: number; readonly rotationWindowDegrees: number;
    readonly maximumPositionCorrection: number; readonly maximumRotationCorrectionDegrees: number; readonly blend: number;
    readonly captureMargin: number; readonly captureSpeed: number; readonly captureDwell: number; readonly guideAcceleration: number; readonly triggerThreshold: number;
}
export interface ObservedAssistance { readonly forgiving: ObservedAssistanceKnot; readonly balanced: ObservedAssistanceKnot; readonly precise: ObservedAssistanceKnot; }
export interface ObservedPuzzle {
    readonly id: string | null; readonly placement: WorkshopPlacementMode; readonly precision: number;
    readonly inventoryPart: string | null; readonly inventoryCount: number;
    readonly goal: { readonly kind: WorkshopGoalKind; readonly body: string; readonly target: string; readonly sourceNode: string; readonly targetNode: string; readonly minimumDuration: number };
    readonly primary: ObservedAssistance; readonly target: ObservedAssistance; readonly placementAssistance: ObservedAssistance;
}
export interface ObservedConstruction {
    readonly sessionLow: string; readonly sessionHigh: string; readonly epoch: string; readonly projection: string; readonly cadence: string;
    readonly sequence: string; readonly kind: WorkshopCommandKind; readonly expectedRevision: ExpectedRevisionKind; readonly authorityRevision: string;
    readonly puzzle: ObservedPuzzle;
    readonly revision: string; readonly simulation: SimulationCadence; readonly physical: PhysicalStepProfile; readonly animation: AnimationCadence; readonly presentation: PresentationCadence;
    readonly presentationNumerator: number; readonly presentationDenominator: number;
    readonly instances: readonly { readonly id: string; readonly part: string; readonly locked: boolean; readonly cell: ObservationVector;
        readonly local: ObservationVector; readonly rotation: ObservationRotation;
        readonly parameters: readonly { readonly id: string; readonly value: number }[] }[];
    readonly connections: readonly { readonly source: string; readonly output: WorkshopSocket; readonly target: string; readonly input: WorkshopSocket; readonly domain: WorkshopConnectionDomain }[];
}
export interface ObservedTraceSample {
    readonly epoch: string; readonly tick: string;
    readonly bodies: CommittedObservation["bodies"]; readonly captures: CommittedObservation["captures"];
}
export interface CaptureDiagnostic {
    readonly category: DiagnosticCategory.Capture; readonly kind: WorkshopCaptureKind; readonly stage: WorkshopCaptureStage; readonly identity: string; readonly epoch: string;
    readonly cadence: SimulationCadence; readonly physical: PhysicalStepProfile; readonly revision: string; readonly end: TraceEnd; readonly status: TraceTimingStatus;
    readonly expectedCount: number; readonly count: number; readonly chunk: number; readonly chunks: number; readonly first: number;
    readonly samples: readonly ObservedTraceSample[]; readonly durations: readonly { readonly milliseconds: number }[];
    readonly construction?: ObservedConstruction;
}
export interface ClockSampleDiagnostic { readonly category: DiagnosticCategory.ClockSample; readonly probe: ObservedProbe; readonly reply?: ObservedReply; readonly outcome: ClockProbeOutcome; readonly offset: ObservedInterval; readonly received: string; }
export interface AnimationSampleDiagnostic {
    readonly category: DiagnosticCategory.AnimationSample; readonly cadence: string; readonly kind: AnimationOutputKind;
    readonly target: string; readonly world: string; readonly generation: string; readonly pulse: string; readonly appliedAt: string;
    readonly value: number; readonly ordinal: number; readonly eventPhase: number; readonly property: AnimationProperty;
    readonly timer: { readonly phase: AnimationTimerPhase; readonly started: string; readonly due: string; readonly observed: string; readonly inputEmitter: string };
}
export interface ObservedEndpoint { readonly tick: string; readonly capture: ObservedClockStamp; readonly interval?: ObservedInterval; readonly receipt?: string; readonly probe?: string; }
export interface PresentationDiagnostic {
    readonly category: DiagnosticCategory.Presentation; readonly cadence: string; readonly pulse: string; readonly selected: boolean; readonly appliedAt: string;
    readonly quality: PresentationQuality; readonly frame: string; readonly positionWrites: number; readonly partCount: number;
    readonly simulationOrdinal: string; readonly simulationPhase: number; readonly sampleOrdinal: string; readonly samplePhase: number;
    readonly displayWall?: string; readonly selectedAt: string; readonly clockEpoch: string; readonly displayEpoch: string; readonly world: string;
    readonly position: ObservationVector; readonly rotation: ObservationRotation;
    readonly latest?: ObservedEndpoint; readonly before?: ObservedEndpoint; readonly after?: ObservedEndpoint;
    readonly bodies: readonly { readonly id: string; readonly cell: ObservationVector; readonly local: ObservationVector; readonly rotation: ObservationRotation }[];
}
export type DiagnosticObservation = FaultDiagnostic | RecalibrationDiagnostic | RejectedClockDiagnostic | LateRenewalDiagnostic |
    WideOffsetDiagnostic | DerivationDiagnostic | DeferralDiagnostic | DebtDiagnostic | TickFailureDiagnostic | TransferDiagnostic | CaptureDiagnostic | ClockSampleDiagnostic | AnimationSampleDiagnostic | PresentationDiagnostic;
export interface DiagnosticReceiptObservation { readonly ordinal: string; readonly record: DiagnosticObservation; }

export interface WorkshopObservationPort {
    readonly session: string;
    query(query?: ObservationQuery): ObservationResult<CommittedObservation>;
    receipts(after?: string): ObservationResult<readonly CommandReceiptObservation[]>;
    diagnostics(after?: string): ObservationResult<readonly DiagnosticReceiptObservation[]>;
    openEvents(): ObservationResult<string>;
    readEvents(cursor: string): ObservationResult<EventDeliveryObservation>;
    closeEvents(cursor: string): ObservationResult<never>;
    terminal(): ObservationResult<string>;
}
let current: WorkshopObservationPort | undefined;
function freeze<T>(value: T): T {
    if (value !== null && typeof value === 'object') {
        for (const child of Object.values(value)) freeze(child);
        Object.freeze(value);
    }
    return value;
}
export function discoverObservation(): WorkshopObservationPort | undefined { return current; }
export function registerObservation(session: string, query: (kind: number) => ObservationResult<CommittedObservation>,
    receipts: (after: string) => ObservationResult<readonly CommandReceiptObservation[]>,
    diagnostics: (after: string) => ObservationResult<readonly DiagnosticReceiptObservation[]>, open: () => ObservationResult<string>,
    read: (cursor: string) => ObservationResult<EventDeliveryObservation>, close: (cursor: string) => ObservationResult<never>,
    terminal: () => ObservationResult<string>) {
    current = Object.freeze({
        session,
        query: (kind = ObservationQuery.Committed) => freeze(query(kind)),
        receipts: (after = '0') => freeze(receipts(after)),
        diagnostics: (after = '0') => freeze(diagnostics(after)),
        openEvents: () => freeze(open()),
        readEvents: (cursor: string) => freeze(read(cursor)),
        closeEvents: (cursor: string) => freeze(close(cursor)),
        terminal: () => freeze(terminal())
    });
}
// Genuine managed interop construction operations, used only to project detached domain DTOs.
export function observationObject(): ObservationObject { return {}; }
export function observationArray(): readonly ObservationValue[] { return []; }
export function observationPush(array: ObservationValue[], value: ObservationValue) { array.push(value); }
