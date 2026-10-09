---
title: 'Story 7.1: Reference and historical archive purge'
type: 'chore'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 1
baseline_commit: '34ad0ae0a64fc56336022de0ada49774098492cf'
source_commit: '34ad0ae0a64fc56336022de0ada49774098492cf'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/planning/elements/legacy-disposition.md'
  - '{project-root}/_bmad-output/implementation-artifacts/review-7-0-resumed.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Obsolete reference snapshots, tarballs and benchmark dumps obscure active sources and occupy about 1.96 GB. Story 7.0 preserved their element knowledge and passed independent committed-tree verification.

**Approach:** Archive the owner's two untracked app bundles inside the tim directory in a durable ignored archive, verify recovery, then delete obsolete `reference/` content and its active configuration references. Preserve working builds and all existing unit and Chrome suites.

## Boundaries & Constraints

**Always:**
- Every tracked deletion must match the Story 7.0 ledger's 26 reference rows. Reconcile actual files immediately before deletion; preserve tracked history and pinned `a6c914e` citations.
- Archive `reference/p025-production-isolated-20261004/{diagnostic,production}-appbundle/` before deleting either. Owner decision (10 Oct 2026), “keep everything inside the tim directory”: retain `/Users/aidan/dev/personal/tim/archives/story-7-1-20261009/p025-appbundles.tar.gz`, its manifest and recovered tree. This explicitly supersedes external storage; no external write or exception. Ignore this precise directory and exclude it from Godot import/export. Record absolute path, SHA-256, exact member identities and successful extracted-byte comparison. Never overwrite an existing archive; use a unique destination if occupied.
- Keep only active Markdown in `reference/`; current inspection identifies none, so the intended result is no directory. Recheck incoming current-document links before deletion. Historical evidence and baseline-pinned citations remain untouched.
- Preserve the Story 7.0 review-only receipt modification, unrelated scratch/diagnostic files and separate Bumper worktree. Root remains authoritative.
- Run mandatory pre-write validation, independent review and normal commit protections. Local commit only; no push or deployment.

**Never:** Delete tools, tests, engine/part sources or catalogue assets (Stories 7.2–7.4); modify gameplay or acceptance IDs; treat archive creation alone as recovery proof; silently delete newly discovered untracked source.

## I/O & Edge-Case Matrix

| State | Required behavior |
| --- | --- |
| Archive verifies and ledger covers current scope | Remove obsolete reference content only |
| Archive absent, corrupt, incomplete or inaccessible | Stop before deletion; retain original bundles |
| New untracked/ignored source or unmatched tracked path | Stop and reconcile preservation/ledger |
| Current consumer depends on reference content | Resolve within scope or report blocked; never silently break it |

</frozen-after-approval>

## Code Map

- `reference/`: 4,682 regular files; 392 tracked, 2,474 untracked, 1,816 ignored; no symlinks. Total 1,957,553,725 bytes. All 13 tracked Markdown files are historical.
- Two app bundles: 402,121,675 bytes total. Retain the same verified archive bytes under ignored `archives/story-7-1-20261009/`; no recompression.
- `export_presets.cfg`: replace four obsolete `reference/*` exclusions with `archives/story-7-1-20261009/*`; add a `.gdignore` inside the retained archive directory.
- `docs/planning/invest/vertical-delivery.md`: mark LEGACY-0a complete after acceptance and CAT-048b reference deletion already discharged.
- `_bmad-output/planning-artifacts/epics.md`: retain CAT-048b acceptance text; append discharge evidence for its reference-only clause, leaving annular kernel cleanup future.
- `docs/planning/elements/legacy-disposition.md`: retain historical rows; add archive/deletion receipt.
- `TODO.md`, sprint status and this spec: update current outcome and next gate.
- `CuriousContraptions.slnx`, web project and `.github/workflows/pages.yml`: unchanged build entry points. CAT-023b adopts `tools/workshop-console.mjs` with focused `tools/workshop-console.test.mjs` controls for an observed console-classification defect.

## Tasks & Acceptance

**Execution:**
- [x] `reference/` — re-enumerate scope and consumers, verify ledger coverage and classify Markdown; retain raw output.
- [x] `archives/story-7-1-20261009/` — retain the validated archive, manifest and separately recovered tree; verify relocation byte identity and fresh source hashes before deleting originals.
- [x] `reference/`, `export_presets.cfg` — validate and apply exact bounded deletions and four exclusion edits.
- [x] Roadmap, epics, ledger, TODO and sprint — record actual outcome, archive identity and fulfilled reference-only cleanup.
- [x] This spec and review record — retain one frozen candidate identity, actual diff, verification logs and independent verdict; commit only after approval.

**Acceptance Criteria:**
- Given the obsolete archive tree, when Story 7.1 is applied, then approximately 4,000 obsolete files are removed; report the actual count, presently 4,682.
- Given reference Markdown, when current-link review finishes, then only active Markdown remains; with no active files, `reference/` is absent.
- Given the untracked bundles, when deletion is permitted, then a readable ignored local archive with recorded SHA-256 reproduces all 2,474 files exactly.
- Given the active solution and unchanged runtime, when builds, unit and serial actual-Chrome Playwright suites run after deletion, then all pass with no new warnings or regressions.
- Given scoped review approval, when the local commit is created, then independent committed-tree verification matches the approved candidate; unrelated work remains preserved.

## Implementation Notes

### Resumed planning — 10 October 2026

Read-only recheck at source commit above: all 392 tracked paths match the 26 ledger rows and every row count; no gaps or drift. The tree still has 4,682 regular files, 1,957,553,725 bytes and no symlinks. Each required bundle has 1,237 files; their total is 402,121,675 bytes. All 1,816 ignored files are build/import output or two archived Godot export-template ZIPs; no additional untracked source was found. The 13 tracked Markdown files remain historical, with no incoming current-document Markdown links. All tracked non-document consumers were searched: only four export exclusions refer to this tree; other matches are prose using “reference”. CAT-048b's future cleanup clause needs its reference-only discharge annotation.

**Archive and deletion procedure (current owner-approved local retention):**
1. Validate writes inside tim with Anvil. Retain the archive, member manifest and recovery under ignored `archives/story-7-1-20261009/`, protected by `.gdignore` and export exclusions. No external destination or write exception is required.
2. Recheck real paths, free space and absence of the proposed destination. Keep the dated destination in the frozen draft unless occupied. Reject symlinks/special files and source changes during capture. Use an empty `archives/story-7-1-20261009/recovered/` inside tim, separate from the original reference bundles and the Bumper worktree.
3. Before creation, build a sorted manifest of relative bundle paths, file sizes, modes and SHA-256 hashes; include directory membership. Create the gzip tar with only the two bundle roots relative to `reference/p025-production-isolated-20261004/`, without following links or adding platform metadata. Write to a new partial archive, never truncate an existing archive.
4. Read every tar member: require the exact manifest membership, no duplicates, absolute paths, traversal, links or special entries. Stream-decompress all members; compare size/hash/mode to the original manifest. Extract only after these checks into the empty recovery directory, then independently enumerate and hash recovered files. Require exactly 2,474 matching files, no extras, and matching directory membership.
5. Recompute original bundle membership/hashes to detect concurrent drift. Only after equality, finalize the archive name, record its absolute path and archive SHA-256 alongside the member manifest and raw verification log, and have the reviewer verify recovery. Failed checks retain originals and partial evidence; no deletion follows.
6. Immediately before deletion, re-enumerate the complete reference tree and compare against the checked inventory/ledger, including a fresh exact bundle membership and SHA-256 comparison with the same pre-archive manifest after reviewer recovery checks. Validate each bounded removal and configuration edit; remove only this tree. Keep the successful ignored local archive, its manifest and recovered tree as recovery evidence.

**Historical planning checkpoint, before the owner's local-retention decision:** no new scope choice was then found; external writes were blocked by Anvil and no archive/deletion had occurred. That external prerequisite is superseded by the current local procedure above. Dirty TODO and the Story 7.0 review receipt remain preserved. Runtime regression commands execute only after purge; browser access is coordinated with the same independent reviewer.



### Historical local staging checkpoint — 10 October 2026

Authorized reversible preparation only. One deterministic archive is staged at `.anvil/story-7-1-staging/p025-appbundles.tar.gz`, 208,369,673 bytes, SHA-256 `5267bbe9fda12ee295b0b044fdc05bf14295e687eaddd740a3545356b13a1d0a`. Member manifest: `.anvil/story-7-1-staging/members.json`, SHA-256 `9ae5e8efcfee451939d05604744cbdd91dbd511ba98f061cb36f4cc1bbf3c13b`. It contains exactly 2,474 files and 12 directories, representing 402,121,675 source bytes.

`python3 .anvil/story-7-1-staging/verify-recovery.py` exited 0: exact tar membership/hashes, separately recovered membership/modes/sizes/hashes, and rechecked originals all match. Raw result: `.anvil/story-7-1-staging/recovery-result.txt`. Anvil allowed digest-bound previews for binary archive/recovery writes; the full manifest received warnings on 18 ICU data filenames, inspected as filename false positives and retained. This partial binary validation is not a full-content scan.

At that checkpoint the external copy remained blocked, originals were intact and no deletion/commit had occurred. The subsequent owner decision below superseded external storage. The same archive bytes were retained locally without repackaging; independent recovery and immediate source-drift checks remained mandatory.


### Owner archive-location decision — 10 October 2026

The owner explicitly superseded external storage with “keep everything inside the tim directory”. Amend the frozen intent only for that location decision. Retain the exact verified archive/manifest/recovered bytes at `archives/story-7-1-20261009/`, excluded by the precise Git ignore rule, local `.gdignore` and all four export presets. Earlier external-blocker notes are historical and no longer gate execution. Independent review checks the narrow location delta, unchanged bytes and fresh source drift before exact reference deletion. Build/unit/Chrome requirements remain unchanged.

### Post-deletion verification — 10 October 2026

Exactly 4,682 reference files (392 tracked, 2,474 untracked bundles, 1,816 ignored outputs) were removed after the reviewer approved retained-location/recovery/source drift. All individual file deletions and the reference root passed Anvil. Immediate full source hashes matched `.anvil/story-7-1-deletions.json` (`61ebcb39ba02962de6b1aed1db143e3684414c9c850be81125f8dff43a387d9b`). A first in-memory recheck stopped safely because its REPL closure retained the old array; an explicit-output-array checker then matched every actual file and performed the deletion. No real source drift occurred. All 13 unique baseline-pinned reference source paths remain retrievable in git; no active runtime source changed.

Release solution build passed with only five pre-existing xUnit2013 warnings in untouched animation tests. Unit tests: 643/643; Node suites: 83/83. Production Release and diagnostic PlaytestRelease publishes passed. Raw commands/results: `.anvil/story-7-1-{build,unit,node,production,diagnostic}.log`; identities: `.anvil/story-7-1-{production,diagnostic}-identity.json`. Diagnostic manifest SHA-256 `0f861841a4019e4cf3a5548859c0db8d4740506d4f8205d7442a9b9c8634dbdd`; all 221 bundle files were frozen before reviewer Chrome execution on `http://127.0.0.1:8060/`. Archive/reference paths are absent from both bundle inventories and Godot packs. One long diagnostic compiler command was tool-truncated and Anvil skipped three long command lines; final build success and output identities are retained. Changed-doc/config Anvil scan found zero warnings; diff whitespace check passed.

The same reviewer completed the full serial Chrome run and affected harness rechecks described below. No duplicate full-suite run or runtime rebuild. Scoped completion awaits final independent approval and committed-tree verification; local archive retention is not external backup or remote publication.

## Spec Change Log

## Review Triage Log

- CAT-023b's final console checks misclassified `NaN` inside valid base64 trace payloads. All gameplay assertions had passed. The bounded helper exempts only complete normal-log trace envelopes; real console errors, fault markers, malformed/trailing diagnostics and page errors still fail. Gameplay assertions and timeouts are unchanged. Focused controls: 2/2; affected Anvil scan: no warnings.
- Full independent Chrome run: 41/45 passed in 370.759 s. Its final tool report was truncated; complete affected original rerun retains all four failure arrays in `.anvil/story-7-1-cat023b-original.log` (SHA-256 `c52dbf34e1b765ba36d363074f6f5d82914e4b615ae587dd0fc09713e906f59b`). Corrected independent rerun: 4/4 in 38.209 s, `.anvil/story-7-1-cat023b-corrected.log` (SHA-256 `2ec92ef8648954f1d4f30c084303db15a5caa07daf9ece5768e9b905bd6c2755`). Together all 45 cases pass against the unchanged runtime bundle. See the single [review record](review-7-1-reference-historical-archive-purge.md).

## Verification

- `dotnet build CuriousContraptions.slnx -c Release --nologo`
- `dotnet test CuriousContraptions.slnx --nologo`
- `dotnet publish CuriousContraptions.web -c Release --nologo -p:PlaytestDiagnostics=false`
- Diagnostic export with `-p:PlaytestDiagnostics=true`; serve that exact root bundle for existing Chrome driver.
- `node --experimental-vm-modules --test tools/workshop-*.test.mjs`
- `node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts` through actual Chrome; check available connector directly and coordinate browser access with Bumper reviewer.
- `anvil check --changed`, explicit affected-path scan and `git diff --check`: no new warnings/errors.
- `git ls-files reference`, filesystem enumeration, archive member/hash comparison and current consumer search establish deletion, preservation and dependency boundaries. Historical citations must still resolve through git.

