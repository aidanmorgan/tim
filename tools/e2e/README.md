# Workshop browser tests

Run the suites serially with installed Chrome and a published diagnostic preview. TypeScript enums require a TypeScript loader; Node's strip-only invocation is insufficient.

```sh
npx --yes --package=tsx@4.23.15 tsx --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts
```

For the current Bumper slice, set `BUMPER_PREVIEW_URL` to the immutable preview and select `cat-015b.test.ts` plus `cat-015a-sidekick.test.ts`. CAT015b creates a unique evidence directory inside `.anvil`; an explicit `CAT015B_EVIDENCE_DIR` must name a new directory there. Existing directories reject rather than overwrite screenshots.

This environment's already installed tsx4.23.15 loader is `/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs`; equivalent local invocation uses `node --import <loader> --test ...`. Read-only diagnostic observations supplement actual UI construction; they are not test control hooks.

## Springboard numerical controls

Build the matching current diagnostic WASM bundle before executing the real-worker numerical controls (run from the repository root):

```sh
dotnet publish CuriousContraptions.web/CuriousContraptions.web.csproj -c Release -p:PlaytestDiagnostics=true
SPRING_COMPILED_FIXTURES=1 dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -c Release -warnaserror --filter FullyQualifiedName~ForgivingSpringForwardReceiverBoundaryUsesCompiledSourceScene --logger 'console;verbosity=detailed' > .anvil/receiver-compiled-current.log
SPRING_WASM=1 SPRING_COMPILED_LOG=.anvil/receiver-compiled-current.log node --experimental-vm-modules --test --test-name-pattern='real WASM' tools/workshop-rigid-body.test.mjs
```

Require a nonzero executed test count, zero failures and **zero skipped tests** in that selected run. Without `SPRING_WASM=1`, the numerical cases deliberately skip and do not qualify the law. The test imports actual `AppBundle/simulation/_framework/dotnet.js` exports and current worker source; retain the source and matching bundle identities with the result. Raw numerical fixtures bypass C# scene admission; the Receiver-offset control instead consumes current compiler admission bytes emitted by the focused native fixture test. Both supplement actual UI proof. Use a fresh fixture-log path to preserve prior attempts. Fact18 currently fails its required −0.5 m miss and remains pending an explicit owner decision; do not report the selected suite as passing until resolved. For the UI cases use `SPRING_PREVIEW_URL` and a new `SPRING_EVIDENCE_DIR` inside `.anvil`. Raw compressed numerical fixtures are supplemental solver controls, not UI construction or Reset proof. Story6.5 adds a separate finite preload control to the selected real-WASM run.

For the preserved passive/precharged lessons use `PRELOAD_PREVIEW_URL` and `PRELOAD_EVIDENCE_DIR` with `cat-062b.test.ts`. The evidence directory must be a new path inside `.anvil`; an existing directory rejects to retain earlier screenshots. Tests use actual placement, rotation and settings controls, read-only saved construction/committed poses, physical energy and matched zero/heading controls.
