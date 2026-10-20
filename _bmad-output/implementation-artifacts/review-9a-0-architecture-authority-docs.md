# Review: Story 9A.0, Architecture Authority Records the Sole Design

- **Reviewer:** Murdoch-15, independent. I edited no deliverable.
- **Date:** 21 Oct 2026.
- **Rounds:** one, under the owner rule.
- **Scope:** documentation only. The runtime is unchanged.

**Verdict: Pass, scoped to documentation.**
- No acceptance criterion of Story 9A.0 is breached, either at `9e4d6d86` or on its merge with main.
- Every finding below is filed to **Story F.12, "Architecture Doc Follow-ups"**. That story does not exist yet, so the coordinator must create it.
- The F.12 merge items M-1 to M-6 are text edits. The coordinator can fold them into the real merge resolution.

## Identities

- **Candidate:** `9e4d6d86`, on base `c7d5dbb0`, in worktree `.claude/worktrees/agent-a8440736160f237e0`.
- **Main:** `7d6dc38a`. It includes 9R.6, which was already in the base, and the 9R.7 merge `f0f2a308`.
- **Review merge:**
  - This is a local, review-only merge in a scratch worktree, of main `7d6dc38a` into the candidate `9e4d6d86`.
  - It was left staged and uncommitted. The staged tree is `9b5afb161f45f50d4e5b26804b86a0d83f24dabe`, with parents `9e4d6d86` and `7d6dc38a`.
  - A transient local commit was made with hooks disabled and then reset with `--soft`. Nothing was pushed or kept.
  - **The implementer must redo the merge resolution.** Mine is evidence only.

## Merge with main: four textual conflicts

1. **`docs/engine-contracts.md`, two hunks.**
   - The hunks are the codec paragraph and the construction-save paragraph.
   - 9R.7 removed the wire, save and cadence-settings versions and reworded both paragraphs.
   - Both sides now say "no format version". They agree in meaning.
   - I took the candidate's text. The candidate's save paragraph already drops "v4 envelope (1,624 bytes) / typed version", which 9R.7 made false.
   - 9R.7's words "refused as `UnsupportedSave`" are lost in that choice, and the implementer may restore them (filed as F.12 M-6).
2. **`docs/gpu-f32-physics.md`, the forward-schema-versions paragraph.**
   - Main lists canonical3, physics12 and trace4 as current forward versions. The candidate states the no-versioning rule and points at Current status.
   - I took the candidate's text, which is consistent with invariant 8.
3. **`epics.md`: F.10 (candidate) against F.11 (main).** Both stories are kept.
4. **`sprint-status.yaml`: the `f-10` and `f-11` keys.** Both are kept.

There are no other conflicts. Main's 9R.7 test-file additions to `CuriousContraptions.tests.csproj` merged automatically next to the new `Compile Include`.

## Tier 1 on the merge tree `9b5afb16`

Chrome was not run. Chrome is not required here:
- AC10 asks for native proof only.
- The brief says no Chrome.
- The runner reports `Mode: Full / Tier 3 required: yes` (`select --diff 7d6dc38a`) only because of one test-project `Compile Include` (`Full(Build)`, build files). That change is not reachable from the browser.
- Every other changed path is `NoBrowserImpact`.

| Check | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx` | exit 0; 0 warnings, 0 errors |
| `dotnet test CuriousContraptions.slnx` | exit 0; 1876 + 156 + 78 + 17 passed, 0 failed |
| `node --experimental-vm-modules --test tools/*.test.mjs` | exit 0; 83/83 |
| `tools/e2e/unit/*.test.ts` (tsx) | exit 0; 24/24 (first attempt exit 1: `Cannot find module 'flatbuffers'`, because the scratch worktree had no `node_modules`; passed after linking the main checkout's) |
| `dotnet build -c Release CuriousContraptions.slnx` | exit 0; 0 warnings |
| Ownership oracles | exit 0, Failures 0 (the first two attempts failed on environment only: no Release build, then no web restore) |
| LifecycleContract | exit 0; Count 510, Failures 0 |
| WireContract | exit 0; Failures 0 |
| `anvil_check` on the 11 changed non-C# files | 0 warnings |
| `--filter ArchitectureDocsConsistencyTests` | 4/4 passed |

## Mutations on the merge tree

Each mutation was applied with the Edit tool. The filtered class was re-run with `--no-build`, because the docs are read at run time. Each mutation was then reverted with `git checkout`, and no unstaged change remained afterwards.

| # | Mutation | Failing fact |
| --- | --- | --- |
| M1 | Remove the AGENTS.md "Sole design authority" line | `AgentsLinksTheIsolationAuthority` (1 failed, 3 passed) |
| M2 | Reinstate `pose-ring.js` in the `architecture.md` seed (`worker.js` row) | `NoAuthorityPresentsRetiredDesignAsCurrent` (hit `architecture.md: pose-ring.js`) |
| M3 | Change `gpu-f32-physics.md` link `engine-contracts.md` to `engine-contract.md` | `EveryRelativeLinkInTheAuthorityDocsResolves` (`docs/gpu-f32-physics.md -> engine-contract.md`) |
| M4 (reviewer's own) | Put an upper-case `Ramp.TRES` into `general-engine-design.md` | `NoAuthorityPresentsRetiredDesignAsCurrent` (hit `.TRES`; the ban is case-insensitive) |

The negative control `TheBanCheckRejectsAReinstatedPhrase` feeds the same pure `FindRetiredPhrases` a fixture string and passes. So does every live mutant, so the check can fail.

## Criterion matrix

| AC | Evidence | Result |
| --- | --- | --- |
| AC1: two parts; each open finding has an owner; no unmet invariant described as holding | See note (a) | Met |
| AC2: invariants 1–6 | §1 items 1–6 match the epic AC in meaning: project per element, one bundle source, command/query with `WorkshopRead` (G5), disruptor with level states as queries (G6), capability-named kinds (G9), 9A.45 gate | Met |
| Owner rules (brief item 2) | See note (b) | Met |
| AC3: `architecture.md` | See note (c) | Met |
| AC4: bundle loading and schema in both docs (G10) | See note (d) | Met |
| AC5: f32 table | Every row of "Remaining binary16 lane" names Story 9A.44; `getF16` and `setF16` are gone | Met |
| AC6: AGENTS.md, one line | Line 33, the first bullet of "Engineering and gameplay requirements" | Met |
| AC7: G8 record | See note (e) | Met |
| AC8: stale text | `epics.md:663` now reads `cat-062.test.ts`, and `tools/e2e/cat-062.test.ts` exists. `spec-9-4-state-latch.md:91` gains a dated, append-only Superseded note | Met |
| AC9: consistency test | The four facts above, and the mutants are killed | Met |
| AC10: links and filtered test | See note (f) | Met |
| Brief 6: "versioned" amendments and the widened 9A.2 Deletes | See note (g) | Met at the candidate; stale after the merge (M-1, M-2) |
| Brief 7: consistent with 9R.6 and 9R.7 | 9R.6 is in the base and nothing contradicts it. 9R.7 conflicts are listed under F.12 merge items | Filed |

**(a) AC1.**
- §1 "Target invariants" and §4 "Current status" are separate.
- §4.1 covers every audit §8 finding against the 21 Oct delivery order:
  - A1–A7, A9–A13, A15–A19;
  - B1, B2, B6, B7 and the B8 remainder;
  - C2–C5 and D1–D3.
- §4.2 records the closed findings: A8, A14, B3, B4, B5, B8 (directory), D4 (JS producer) and the capacity work.
- Every owner exists in the delivery order, for example "9A.14 + 9A.18" and C-Gate as 9A.19 + 9A.21.
- `engine-contracts.md:3` and `general-engine-design.md:3` frame themselves as the target, and both point to Current status.

**(b) Owner rules.** Invariants 7–10 state the rules:
- no fixed capacities;
- no format versioning, with structural rejection of bad input;
- delete in the same story, with 0 hits by `rg` or the ratchet;
- the tiers, linked to the guide.

AD-13 summarises them.

**(c) AC3, `architecture.md`.**
- AD-06 covers "no code outside `elements/`". It names the compiler, solver, workers, authoring, UI, persistence and tools.
- AD-11 is now "Element Scenes and the Element Bundle", with the bundle and `.tscn`.
- AD-13 is "Sole Architecture" and links the isolation doc.
- The diagram's lines 55–56 separate "SAB Event Ring (one-shots)" from "Committed read (levels)", and the text at line 65 separates the two channels.
- No `pose-ring.js` remains.
- The source map lists `CuriousContraptions.FlatBuffers/`, `clock-master.js` and `clock-ring.js`.

**(d) AC4.**
- `engine-contracts.md` has the §"Element Bundle Loading Contract": packages, build discovery, start-up loading, identity, palette and no versioning.
- It also has §"Element Bundle Schema Contract", which covers:
  - `[x, y, z]` vectors and `[x, y, z, w]` quaternions;
  - the enum category;
  - the typed parameter schema;
  - the mechanical, electrical and signal payloads;
  - structural rejection.
- `general-engine-design.md` §"Element Packages and the Element Bundle" restates the same and links both contracts.

**(e) AC7.**
- §4.3 records that the 9.2b PASS is unsupported, with the MNA clause and the `worker.js` clause (379 lines at `c7d5dbb0`; verified with `wc -l`).
- No MNA code exists: `git grep` finds 0 hits for nodal/MNA in `*.cs` and `*.js`.
- Every MNA mention in the six docs reads as "a named later capability". The current model is supply routes over reachability, in `engine/gpu/SupplyRoute.cs`, which exists.
- The review-9-2b file is not touched by the diff. The addendum is under "Review-9-2b addendum" below.

**(f) AC10.**
- The test checks 37 relative file links in the six docs, and they resolve.
- I also checked 36 `#fragment` links in the six docs and `epics.md`. Each anchor exists, as an `id=` or a heading slug.
- All 37 file links added by the diff resolve except one. That one is the AGENTS.md line quoted inside the spec's Task 6 text. It is a quotation, not a navigation link (F.12-7).
- The filtered test passes.

**(g) Brief 6.**
- Every "versioned" phrase in the 9A rules and in 9A.0, 9A.2, 9A.4, 9A.14, 9A.24 and 9A.26, and in the conversion preamble, now reads "no version field / structurally invalid input rejected".
- `rg 'versioned|wrong-version|bumps the save version|older saves'` over the current story text finds only the dated "superseding 'versioned'" notes.

## Findings, all filed to Story F.12 (none blocking)

**Merge reconciliation with 9R.7.** Apply these in the real merge resolution:
- **M-1.**
  - §4.1 row "Format versions … `WorkshopSaveVersion` and `WorkshopWireVersion` → 9A.2" is stale. Both symbols have 0 production hits on the merged tree, because 9R.7 commit `da91cffa` removed them.
  - Move the row to §4.2 "Closed since the audit", as closed by Story 9R.7.
- **M-2.**
  - The new 9A.2 Deletes bullet (epics, the "save and wire format versions" line) names symbols that are already deleted.
  - Drop the bullet, or reword it to "(deleted by Story 9R.7)". Otherwise the bullet's own 0-hit AC is vacuous.
- **M-3.**
  - `WorkshopSettingsVersion` has 0 hits after 9R.7, which removed the cadence-settings version.
  - Remove it from the F.10 Deletes list and from the §4.1 F.10 row.
  - The other F.10 symbols still exist on the merged tree: `WorkshopClockWire.Version`, the `WorkshopMemoryWire` `Version`, `WorkshopHint.Version`, `WorkshopTraceVersion`, `TraceTimingVersion`, `WorkshopScheduleVersion`, `PhysicsStateVersion`, `STATE_VERSION` and `BodyRecordVersion`.
- **M-4: ownership conflict.**
  - Main's 9R.7 status line (epics, Story 9R.7, "*The remaining format versions belong to 9A.1.*") contradicts the candidate's F.10 assignment.
  - The candidate assigns them in the §4.1 row, the F.10 status line and the `sprint-status.yaml` comment.
  - The coordinator must pick one owner and make the other line agree.
  - The 9R.7 line was written after the candidate. The candidate's F.10 has explicit Deletes and ACs.
- **M-5.**
  - The §4 heading is pinned "at `c7d5dbb0`", and should be updated to the merged identity.
  - "The filed follow-ups F.1–F.10 stay last" should become F.1–F.11, or F.1–F.12 once F.12 is filed.
- **M-6.** `engine-contracts.md` save paragraph: optionally keep 9R.7's typed refusal name `UnsupportedSave` in the candidate's wording.

**Documentation precision:**
- **F.12-1.** The D4 JS reader (`createEventConsumer` / `readEventRing` in `workshop-client.js`) is gone from the tree: `git grep` finds 0 hits. It appears in neither §4.1 nor §4.2. Add it to §4.2, closed (the audit owner was 9A.23).
- **F.12-2.** The `architecture.md` source map row "Element Bundle … `engine/PartRegistry.cs` (start-up loader)" reads as current, but today `PartRegistry` loads `.tres`. Add "(target; Current status A13)", as the row for `GltfPhysicsBodyExtractor` already does.
- **F.12-3.** The 9A.0 spec, the frozen Boundaries section ("Never: edit review records. Story 9R.18 appends their addenda") and its Spec Change Log both record that the closure moved to this review. No action is needed beyond noting it here.
- **F.12-4.** The 9A.0 story text in epics still says "(347 lines)" for `worker.js`. The authority correctly records 379 at `c7d5dbb0` and 347 at the audit. Optionally align the story text.
- **F.12-5.** The runner selects Full for a test-only `Compile Include`. A rule that classifies `CuriousContraptions.tests/*.csproj` Compile-only edits as `NoBrowserImpact` would avoid a manual waiver.
- **F.12-6.** `ArchitectureDocsConsistencyTests` resolves files but not `#fragment` anchors. A fragment check would catch a renamed heading such as `#current-status`.
- **F.12-7.** The spec's Task 6 quotes the AGENTS.md link text with a repo-root-relative target. That target does not resolve from `_bmad-output/implementation-artifacts/`. It is harmless as a quotation, but it could be wrapped in code ticks.

## Review-9-2b addendum

The amended AC7 assigns the dated addendum to this terminal review. It is appended, append-only, to `review-9-2b-modular-isolation.md` in the main checkout. Nothing above it is changed.

## Raw evidence

The logs are in the session scratchpad and are not retained in the repo:
- `build.log`, `test.log`, `node-harness.log`, `e2e-unit.log`, `release-build.log`;
- `ownership-oracles.log`, `lifecycle.log`, `wire.log`;
- `select-diff-main.txt`;
- `mut0-baseline.txt` and `mut1`–`mut4` `*.txt`.

The results that matter are quoted above.
