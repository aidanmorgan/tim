# Workshop browser tests

Run the suites serially with installed Chrome and a published diagnostic preview. TypeScript enums require a TypeScript loader; Node's strip-only invocation is insufficient.

```sh
npx --yes --package=tsx@4.23.15 tsx --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts
```

For the current Bumper slice, set `BUMPER_PREVIEW_URL` to the immutable preview and select `cat-015b.test.ts` plus `cat-015a-sidekick.test.ts`. CAT015b creates a unique evidence directory inside `.anvil`; an explicit `CAT015B_EVIDENCE_DIR` must name a new directory there. Existing directories reject rather than overwrite screenshots.

This environment's already installed tsx4.23.15 loader is `/Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs`; equivalent local invocation uses `node --import <loader> --test ...`. Read-only diagnostic observations supplement actual UI construction; they are not test control hooks.
