// Exact external boundaries used by the converted worker. The remaining clock glue
// keeps its separate conversion owner; these declarations do not emit a second runtime.
declare module '*dotnet.js' {
    export const dotnet: { create(): Promise<import('./worker-contracts.js').ManagedRuntime> };
}
declare module '*workshop-isolation.js' {
    export function requireIsolation(): void;
    export function watchIsolation(onFailure: (error: unknown) => void): () => void;
    export function isolationQualified(): void;
    export function isolationEvidence(): unknown;
    export function stopIsolation(error: unknown): void;
}
