// Exact external boundaries used by the converted worker. The remaining clock glue
// keeps its separate conversion owner; these declarations do not emit a second runtime.
declare module '*dotnet.js' {
    export const dotnet: { create(): Promise<import('./worker-contracts.js').ManagedRuntime> };
}
declare module '*native-clock.js' {
    export function admitNativeClock(): Promise<void>;
    export const nativeProfile: number;
    export function nativeNow(): number;
    export function nativeClockEvidence(): number[];
}
declare module '*workshop-isolation.js' {
    export function requireIsolation(): void;
    export function watchIsolation(onFailure: (error: unknown) => void): () => void;
    export function isolationQualified(): void;
    export function isolationEvidence(): unknown;
    export function stopIsolation(error: unknown): void;
}
declare module '*clock-ring.js' {
    export const ClockRole: { readonly Browser: number };
    export function serveRequests(clock: import('./worker-contracts.js').ClockRing, peers: Record<number, Uint8Array>, now: () => number): number;
    export function createClockRing(): SharedArrayBuffer;
    export function heartbeat(clock: import('./worker-contracts.js').ClockRing): number;
    export function requestReply(clock: import('./worker-contracts.js').ClockRing, role: number, bytes: Uint8Array): Promise<Uint8Array | null>;
    export function attachClockRing(buffer: SharedArrayBuffer): import('./worker-contracts.js').ClockRing;
    export function deriveExchange(clock: import('./worker-contracts.js').ClockRing, now: () => number, timeout: number): import('./worker-contracts.js').ClockExchange | null;
    export function qualifyDerivation(clock: import('./worker-contracts.js').ClockRing, now: () => number, timeout: number,
        host: { qualified(): boolean; qualify(exchange: import('./worker-contracts.js').ClockExchange): void; missed(): void }): void;
    export function writeDerivation(clock: import('./worker-contracts.js').ClockRing, correction: number, rederive: boolean): number;
    export function beat(clock: import('./worker-contracts.js').ClockRing): void;
}
