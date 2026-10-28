export const enum PosePublicationPreparation { Ready = 0, StorageUnavailable = 1 }
export const enum EventPublicationPreparation { Ready = 0, StorageUnavailable = 1, SequenceExhausted = 2 }
export interface ManagedHost {
    ScheduleRoles(): number[];
    ClockDerivationProfile(): number[];
    ClockDerivationQualified(): boolean;
    QualifyClockDerivation(exchange: Uint8Array): void;
    ClockDerivationQualificationMissed(): void;
    AdoptClockDerivation(epoch: number): void;
    ClockDerivationStartupCorrection(): Uint8Array;
    ClockDerivationExchangeNarrow(exchange: Uint8Array): boolean;
    ClockDerivationRetried(): void;
    ClockDerivationMissed(): void;
    CheckClockDerivation(exchange: Uint8Array): Uint8Array;
    FlushObservation(): void;
    AcknowledgeRead(receipt: Uint8Array): void;
    ResumeTurn(turn: number): void;
    RejectUnknownEnvelope(keys: string[]): string;
    CaptureMode(): number;
    Bootstrap(bytes: Uint8Array): Uint8Array;
    AnimationPeer(): Uint8Array;
    PrepareGpu(): Promise<void>;
    ScheduleResult(bytes: Uint8Array, role: number): void;
    RejectSaturatedCommand(): string;
    Dispatch(bytes: Uint8Array): Promise<void>;
}
export interface ManagedImports {
    now(): number;
    scheduleTurn(turn: number, delayMilliseconds: number): void;
    scheduleControl(bytes: Uint8Array, recipient: number): void;
    acknowledge(bytes: Uint8Array, sequenceBytes: Uint8Array, pending: boolean): void;
    retire(detail: string): void;
    preparePose(identity: Uint8Array, bodies: number, capacity: number): PosePublicationPreparation;
    commitPose(identity: Uint8Array): void;
    discardPose(identity: Uint8Array): void;
    prepareEvents(batch: Uint8Array): EventPublicationPreparation;
    commitEvents(): void;
    discardEvents(): void;
    publishEvents(): void;
    disposeEvents(): void;
    publish(bytes: Uint8Array, identity: Uint8Array, poseSlot: Uint8Array, pending: boolean): void;
    dispose(): void;
}
export interface ManagedRuntime {
    setModuleImports<T = ManagedImports>(name: string, imports: T): void;
    getAssemblyExports<T = { CuriousContraptions: { Simulation: { Program: ManagedHost } } }>(name: string): Promise<T>;
    getConfig(): { mainAssemblyName: string };
}
export interface ClockRing {
    buffer: SharedArrayBuffer;
    words: Int32Array<SharedArrayBuffer>;
    bytes: Uint8Array<SharedArrayBuffer>;
    wide: BigInt64Array<SharedArrayBuffer>;
    view: DataView<SharedArrayBuffer>;
}
export interface ClockExchange { before: bigint; master: bigint; after: bigint; epoch: number; }
