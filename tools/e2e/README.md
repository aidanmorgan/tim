# Workshop browser tests

Run the suites serially with installed Chrome and a published diagnostic preview. TypeScript enums require a TypeScript loader; Node's strip-only invocation is insufficient.

```sh
npx --yes --package=tsx@4.23.15 tsx --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts
```

For the current Bumper slice, set `BUMPER_PREVIEW_URL` to the immutable preview and select `cat-015b.test.ts` plus `cat-015a-sidekick.test.ts`. CAT015b creates a unique evidence directory inside `.anvil`; an explicit `CAT015B_EVIDENCE_DIR` must name a new directory there. Existing directories reject rather than overwrite screenshots.

This environment's already installed tsx4.23.15 loader is `/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs`; equivalent local invocation uses `node --import <loader> --test ...`. Read-only diagnostic observations supplement actual UI construction; they are not test control hooks.

## Passive Springboard numerical controls

Build the matching current diagnostic WASM bundle before executing the real-worker numerical controls (run from the repository root):

```sh
dotnet publish CuriousContraptions.web/CuriousContraptions.web.csproj -c Release -p:PlaytestDiagnostics=true
SPRING_WASM=1 node --experimental-vm-modules --test --test-name-pattern='real WASM' tools/workshop-rigid-body.test.mjs
```

Require a nonzero executed test count, zero failures and **zero skipped tests** in that selected run. Without `SPRING_WASM=1`, the numerical cases deliberately skip and do not qualify the law. The test imports actual `AppBundle/simulation/_framework/dotnet.js` exports and current worker source; retain the source and matching bundle identities with the result. Fixtures bypass C# scene admission and supplement actual UI proof. For the UI cases use `SPRING_PREVIEW_URL` and a new `SPRING_EVIDENCE_DIR` inside `.anvil`. These tests do not introduce authorable preload.
