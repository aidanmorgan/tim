---
title: 'Story 9A.0: Architecture Authority Records the Sole Design'
type: 'chore'
created: '2026-10-18'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a0-architecture-authority-records-the-sole-design'
  - '{project-root}/_bmad-output/planning-artifacts/architecture-conformance-audit-2026-10-18.md'
  - '{project-root}/_bmad-output/planning-artifacts/retro-verification-2026-10-18.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The authority docs describe an end state as if it already exists, and they contradict owner decisions.
- **The isolation doc** (`docs/architecture/element-package-isolation.md`) names things that do not exist:
  - `CuriousContraptions.Engine` (`:8`, `:74`);
  - a nested `elements/mechanical/ramp/` layout (`:14`);
  - Optical/Pneumatic Abstractions (`:28-29`);
  - `IPartDefinition` and `PartId` "in Abstractions" (`:23`);
  - MNA (`:10`, `:38`, `:75`);
  - a "~50–80 line" `worker.js` (`:39`; actual 380 lines);
  - a compiler "guarantee" (`:35`).
- **The tracking premise is stale.** The epic calls the isolation doc untracked, but it has been tracked since `8f682f8` (`git ls-files`).
- **`.tres`/`res://parts/catalog` discovery is presented as current** in:
  - `_bmad-output/planning-artifacts/architecture.md:151,262,289,309,333,341`;
  - `docs/engine-contracts.md:39-49,57-93`;
  - `docs/general-engine-design.md:34-44,50,63,139,166,304-309`;
  - `docs/gpu-f32-physics.md:5,54,62,79,97,124,158`.
- **`.glb` is presented as the format,** against the owner's 18 Oct `.tscn` decision:
  - `architecture.md:107,112,145-150,263,304,317-318,340,344`;
  - `engine-contracts.md:64-65,103-121,150-151`;
  - `general-engine-design.md:70-71,122-143,201,307`;
  - `gpu-f32-physics.md:59,71,88,96`.
- **MNA is presented as current:**
  - `engine-contracts.md:7,34,133`;
  - `general-engine-design.md:14,29,57,203,211`;
  - `gpu-f32-physics.md:58,62,68,88,158,170`.
  
  Yet `git grep -i 'nodal\|MNA'` finds no implementation.
- **`pose-ring.js`** appears at `architecture.md:269,295`, and **`getF16`/`setF16`** at `gpu-f32-physics.md:117,122`. Neither exists.
- **`AGENTS.md` links none of this.**

**Approach:**
- Split the isolation doc into **Target invariants** (the six the AC lists) and **Current status**, which lists each open audit finding with its owning 9A story.
- Rewrite the five other authorities so they state only what is true or is the stated target.
- Add a native consistency test with a negative control.

## Boundaries & Constraints

**Always:**
- **Current status is the only place that names today's gaps.** That covers the `.tres` runtime (9A.14), `PartId` tables (9A.15/9A.45), the wholesale `engine/gpu` compile (9A.45), the inert event ring (9A.22/9A.23), the `CCGPU` channel (9A.24) and the Half lanes (9A.44).
- **The real electrical model is described as current:** compiled supply routes over a reachability graph (`engine/gpu/SwitchedSupply.cs`, `WorkshopPhysicsCompiler` routes). MNA is a named later capability.
- **`.tscn` is the authored element asset.**

**Never:**
- Edit review records. Story 9R.18 appends their addenda.
- Make a runtime change.
- Rewrite history sections of archives.

</frozen-after-approval>

## Code Map

- `docs/architecture/element-package-isolation.md`: restructure it (G2, G5, G6, G9, G10).
- `_bmad-output/planning-artifacts/architecture.md`:
  - AD-05 (`:104-107`), AD-06 (`:109-112`) and AD-11 (`:145-152`);
  - the new AD-13;
  - the diagram at `:54` and the text at `:65` (ring ≠ committed state);
  - the structural seed at `:262-269`;
  - the source map at `:288-295`;
  - the conversion section at `:302-352`.
- `docs/engine-contracts.md:7,34,39-49,57-93,103-121,133,138-142,150-151`: bundle loading, the bundle schema contract (G10) and the G9 kernel/event-kind sentence.
- `docs/general-engine-design.md`: the sections at `:34`, `:61`, `:122` and `:304`, plus the MNA lines.
- `docs/gpu-f32-physics.md`: the lines above. The f32 table (`:115-130`) names Story 9A.44 as the owner of every binary16 lane and drops `getF16`.
- `AGENTS.md` (Engineering and gameplay requirements, `:31+`): one line.
- `_bmad-output/planning-artifacts/epics.md:464`: `cat-062a.test.ts` becomes `cat-062.test.ts` (§E).
- `_bmad-output/implementation-artifacts/spec-9-4-state-latch.md:91`: append a dated "Superseded (Story 9A.0)" note. Rewrite nothing: the plate's ActivationOut is a level that drives Signal lamps only (Story 9R.5).
- New: `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, with its `Compile Include` in `CuriousContraptions.tests/CuriousContraptions.tests.csproj`.

## Tasks & Acceptance

**Execution.** Docs only. [R:9R.0] the `Program` namespace changes source-map rows. [R:9.4b] `CuriousContraptions.FlatBuffers/`, `clock-master.js` and `clock-ring.js` must appear in the source map.

1. [x] **Test first (red).** `ArchitectureDocsConsistencyTests` holds three facts:
   - **`AgentsLinksTheIsolationAuthority`:** checks for `docs/architecture/element-package-isolation.md`.
   - **`NoAuthorityPresentsRetiredDesignAsCurrent`:** over the five files, case-insensitive. Its ban list is a `static readonly` array:
     - `res://parts/catalog`;
     - `.tres` as a word;
     - `pose-ring.js`;
     - `CuriousContraptions.Engine`;
     - `getF16`;
     - `setF16`.
   - **`EveryRelativeLinkInTheAuthorityDocsResolves`:** parses `](relative)` targets in the six docs and requires each file to exist.
   
   The negative control `TheBanCheckRejectsAReinstatedPhrase` feeds the same pure function `FindRetiredPhrases(string)` a fixture containing `res://parts/catalog/*.tres` and asserts one or more hits. Read the repo through `twodog.Engine.ResolveProjectDir()`, as `WorkerAbiTests` does.
2. [x] Isolation doc. In **Target invariants**, state the AC's invariants 1–6 verbatim in meaning:
   1. project per element;
   2. one declaration source, the bundle;
   3. command/query, with the query side = `WorkshopRead`, the pose ring and typed queries (G5);
   4. event disruptor, one-shots only, with level states as committed-state queries (G6);
   5. kernels: capability-named `ActivationNodeKind` is valid, element-named event kinds such as `CounterReached` and `LatchSet` are not (G9);
   6. forcing function, the 9A.45 gate.
   
   **Current status** has one row per open audit finding (audit §8 mapping), plus the G8 record:
   - review-9-2b's PASS for AC1, AC3, AC4 and AC5 is unsupported;
   - no MNA (`git grep -i 'nodal\|MNA'` returns 0);
   - `worker.js` is 380 lines, not 50–80.
   
   Delete every stale claim listed in Intent.
3. [x] `architecture.md`:
   - AD-06's zero-identifier rule binds all non-element code (G4);
   - AD-11 is renamed "Element scenes and the element bundle" and names the bundle and `.tscn`;
   - add AD-13 "Sole architecture", linking the isolation doc;
   - in the diagram, `PG -->|"SAB Event Ring (one-shots)"| AG` and a separate `PG -->|"Committed read (levels)"| AG`;
   - the source map no longer cites `pose-ring.js` or `engine/PartId.cs` as the discovery authority.
4. [x] `docs/engine-contracts.md` and `docs/general-engine-design.md`. Describe bundle loading and the schema contract: versioned; f32 vectors `[x, y, z]` and quaternions `[x, y, z, w]`; mechanical, electrical and signal payloads; a typed parameter schema (key, unit, enum kind, f32 range and default or enum options); an enum category. Replace the `.tres` and `.glb` sections and the MNA wording.
5. [x] `docs/gpu-f32-physics.md`: the table owner is 9A.44, `getF16` is removed, and the discovery and MNA wording is replaced.
6. [x] `AGENTS.md`: one line under "Engineering and gameplay requirements" reading: "Sole design authority: [element package isolation](docs/architecture/element-package-isolation.md) (target invariants and current status)."
7. [x] The `epics.md:464` fix and the spec-9-4 superseded note.

**Acceptance Criteria (1:1 with epics 9A.0):**
- **AC1.** Given the tracked docs, when 9A.0 lands, then the isolation doc has separate **Target invariants** and **Current status**. Current status lists each open finding with its owning story, and no unmet invariant is described as holding.
- **AC2.** The target invariants state items 1–6 above.
- **AC3.** In `architecture.md`:
  - AD-06 covers all non-element code;
  - AD-11 names the bundle;
  - AD-13 links the isolation doc;
  - the diagram separates the ring from committed state;
  - no `pose-ring.js` remains.
- **AC4.** `engine-contracts.md` and `general-engine-design.md` describe bundle loading and the full schema contract (G10).
- **AC5.** The f32 table names Story 9A.44 as owner of every binary16 lane.
- **AC6.** `AGENTS.md` links the isolation doc as sole authority in one line.
- **AC7.** Current status records the unsupported 9.2b PASS: the MNA clause and the 50–80-line claim. The docs state compiled supply routes over reachability as current, and MNA as a later capability. The review file is untouched.
- **AC8.** `cat-062a` is corrected to `cat-062`, and the plate-to-`SetIn` example is recorded as superseded.
- **AC9.** `ArchitectureDocsConsistencyTests` asserts the link and the absence of the retired phrases, and its negative control fails on a reinstated phrase.
- **AC10.** Every relative link in the changed docs resolves, and `dotnet test CuriousContraptions.slnx --filter ArchitectureDocsConsistencyTests` passes. Native proof only; no Chrome.

## Design Notes

**Decisions (owner playability delegation, 18 Oct 2026):**
- **D1. The phrase ban is scoped to the five named files.** Rationale: the isolation doc's Current status must name `.tres` honestly.
- **D2. `.glb` text is corrected to `.tscn` in the same pass.** This goes beyond the epic's Deletes list. Rationale: the owner decision of 18 Oct (epics Open item i) makes `.glb` text a false current claim, and leaving it would re-teach it.
- **D3. No grep test for MNA.** MNA legitimately remains a named later capability. The reviewer checks that every MNA mention is future tense.

**Mutations:**
- Remove the AGENTS line: AC9 fails.
- Revert `architecture.md:269`: the ban test fails.
- Break one relative link: the link test fails.

**Impacted suites:** `CuriousContraptions.tests` (new class only). Doc-link and consistency checks per AGENTS.md documentation proof.

**Risks:**
- Story 9R.0 merges `.editorconfig`/analyzers. The new test must satisfy them [R:9R.0].
- Concurrent edits to `epics.md` and `architecture.md` by other agents. Rebase immediately before editing.

## Verification

- `rg -n -i 'res://parts/catalog|\.tres\b|pose-ring\.js|CuriousContraptions\.Engine|getF16|setF16' AGENTS.md _bmad-output/planning-artifacts/architecture.md docs/engine-contracts.md docs/general-engine-design.md docs/gpu-f32-physics.md`: expected 0.
- `rg -n -i 'MNA|Modified Nodal' docs _bmad-output/planning-artifacts/architecture.md`: every hit reads as a later capability.
- `dotnet test CuriousContraptions.slnx --filter ArchitectureDocsConsistencyTests`: expected pass.

## Legacy Deleted

The stale claims listed in Intent, from the six authority docs. No code.

## Implementation Notes

Amelia-14, 21 Oct 2026, on `c7d5dbb0`. All seven tasks done.
- The isolation doc has §1 Target invariants (1–6 per the AC, plus the owner rules as 7–10) and §4 Current status: open findings with owners under the 21 Oct delivery order, the findings closed since the audit, and the G8 record.
- `worker.js` is 379 lines at `c7d5dbb0`; the audit measured 347.
- The test is `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs`, with 4 facts including the negative control.
- Tier 1:
  - `dotnet build CuriousContraptions.slnx`: 0 warnings;
  - `dotnet test CuriousContraptions.slnx`: 1811 + 156 + 78 + 17, 0 failed;
  - `node --experimental-vm-modules --test tools/*.test.mjs`: 83/83;
  - `anvil_check` on every changed file: 0 warnings.
- The relative links in the six docs resolve.
- Red proof: the docs are read at run time, so each spec mutation was applied and the filtered class re-run with `--no-build`. All three failed as intended, then were reverted:
  - removing the AGENTS line failed `AgentsLinksTheIsolationAuthority`;
  - reinstating `pose-ring.js` in the `architecture.md` seed failed `NoAuthorityPresentsRetiredDesignAsCurrent`;
  - breaking the `engine-contracts.md` link in `gpu-f32-physics.md` failed `EveryRelativeLinkInTheAuthorityDocsResolves`.
  
  With the mutants reverted, the class passes 4/4.
- Gate call for this story (coordinator, 21 Oct 2026): Tier 1 only, because there is no runtime change. The authority itself restates the tiers exactly as the guide gives them.

## Spec Change Log

- **21 Oct 2026, owner rules after the spec was frozen (implementer, under the owner's pace delegation):**
  - **No format versioning** supersedes "versioned" in Task 4, AC4 and invariant 2. The bundle, wire, save and trace have one schema and no version field, and structural validation rejects bad input.
  - The remaining version enums are listed in Current status. Story 9A.2 owns the save and wire versions (its Deletes list gains them). The rest are filed as **Story F.10** (`f-10-retire-remaining-format-versions`).
  - **No fixed capacities** (Story 9R.5b): the construction caps and the lease and transport counts are deleted or voided in `engine-contracts.md`, and so is "Freeze workload limits" in `general-engine-design.md`.
  - **Delete in the same story** and the **verification tiers** are stated as invariants 9 and 10, and are summarised in AD-05, AD-10 and AD-13.
  - The **"Never edit review records; Story 9R.18 appends addenda"** constraint is superseded by the 21 Oct reopened-record closure. Story 9A.0's terminal review appends the review-9-2b addendum; the implementer does not edit it.
  - The `cat-062a` text was at `epics.md:663`, not `:464`.
  - `epics.md` lines that contradicted the no-versioning rule are each amended by one phrase: the 9A rules, 9A.2, 9A.4, 9A.14, 9A.24, 9A.26, and the conversion preamble.

## Review Triage Log

## Appendix: census commands

`for f in AGENTS.md _bmad-output/planning-artifacts/architecture.md docs/engine-contracts.md docs/general-engine-design.md docs/gpu-f32-physics.md docs/architecture/element-package-isolation.md; do rg -n 'res://parts/catalog|\.tres|pose-ring\.js|CuriousContraptions\.Engine|getF16|setF16|MNA|Nodal|\.glb|cat-062a' $f; done`

`AGENTS.md` returned 0 hits at census time. Every other hit is listed in Intent.
