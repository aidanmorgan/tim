// Exact external boundaries used by the converted worker. The remaining clock glue
// keeps its separate conversion owner; these declarations do not emit a second runtime.
declare module '*dotnet.js' {
    export const dotnet: { create(): Promise<import('./worker-contracts.js').ManagedRuntime> };
}
