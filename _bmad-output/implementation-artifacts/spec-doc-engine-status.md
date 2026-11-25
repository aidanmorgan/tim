---
title: 'The documents describe the engine that exists (DOC-ENGINE-STATUS)'
type: 'chore'
created: '2026-11-25'
status: 'done'
baseline_revision: '37a259c9a2f08669cd339e36c3f2c6b4a3f7bd68'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/implementation-artifacts/run-logs/spec-doc-engine-status.md'
  - '{project-root}/docs/planning/elements/readiness/engine-stories-index.md'
  - '{project-root}/docs/planning/elements/readiness/owner-decisions-2026-11-22.md'
  - '{project-root}/docs/planning/elements/readiness/audits/abi-interfaces.md'
  - '{project-root}/docs/planning/elements/readiness/engine-design-gap.md'
warnings: ['oversized']
deferred:
  - summary: >-
      Automated anchor check in ArchitectureDocsConsistencyTests covering fragments and the five documents outside its list.
    evidence: |-
      Review pass 1 VG-1. The test strips #fragments and reads only six documents. The intent contract's Never clause forbids test changes in this slice, so an executed one-off anchor check covers this candidate (run log E15). Recorded in deferred-work.md.
    location: >-
      CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs
    severity: medium
  - summary: >-
      Element documents still cite DOC's removed "Phase 1-5" solver diagram.
    evidence: |-
      Review passes 3 and 4 (FO3-def1, and the undercount found in pass 4). Selector: rg -l 'Phase [1-5]' docs/planning/elements, excluding readiness/audits. Outside the repair and outside DOC and ISO; breaks no rule. Recorded in deferred-work.md.
    location: >-
      docs/planning/elements/ (EL-046..052, EL-112..125, EL-159..161, CAT-009, CAT-069, EL-176 item 4, GAP-04/06/07/09, TH-04 and others)
    severity: low
  - summary: >-
      Planning documents cite stale deferred-work.md lines (:209, :209-213, :211, :212, :221) for the drive-supply entry.
    evidence: |-
      Review passes 4 to 7 (BH4-2b, EC5-4, BH6, BH7-1). Selector: rg -l 'deferred-work\.md:(209|211|212|221)' docs/planning/elements, excluding readiness/audits (14 files, run log E21). Thirteen cite :209, :209-213 or :211 for binary drive supply: eleven element files, readiness/batch-c.md:79 and readiness/new-identities.md. Two cite the entry's former ElectricalSupply.Allocate wording (now Reserve, PD-43): readiness/batch-c.md:106 cites :212, a blank line at the baseline; completion-ledger.md:288 cites :221, the P0-034 entry at the baseline. The live entry is the #engine-supply-demand anchor. Outside the repair; breaks no rule. Recorded in deferred-work.md.
    location: >-
      docs/planning/elements/ (selector in the evidence)
    severity: low
  - summary: >-
      architecture.md:230 gives a balloon buoyancy formula the code does not compute, and names an element.
    evidence: |-
      Review pass 6 (EFE6-6; flagged in run log E19). The code applies a declared acceleration (the Balloon package's buoyancy parameter) through ambient loads (Gas.cs:95-114). Outside the repair and outside DOC and ISO; breaks no rule. Recorded in deferred-work.md.
    location: >-
      _bmad-output/planning-artifacts/architecture.md:230
    severity: low
  - summary: >-
      epics.md:5240 (ENGINE-FIELD-FORCE Present today) cites SIM/WorkshopPhysicsCompiler.Gas.cs, a path that does not exist.
    evidence: |-
      Review pass 6 (BH6-5). The real file is engine/gpu/WorkshopPhysicsCompiler.Gas.cs. Present at baseline; outside the repair; breaks no rule. Recorded in deferred-work.md.
    location: >-
      _bmad-output/planning-artifacts/epics.md:5240
    severity: low
  - summary: >-
      Restitution's mixed-manifold rule (WorkshopConstraintSolver.cs:777: a manifold's supporting points join only when it has no bouncing point) has no decided target.
    evidence: |-
      Review pass 6 (VG6-1, BH6-1, EFE6-7). spec-engine-supported-impact-restitution.md:92 treated dropping those points as a defect, but ENGINE-ISLANDS-SLEEP has no contract line or acceptance row for mixed manifolds. The documents now state the rule as present behaviour only. Owner: ENGINE-ISLANDS-SLEEP planning decides it, with an acceptance case. Recorded in deferred-work.md.
    location: >-
      CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:777; epics.md ENGINE-ISLANDS-SLEEP
    severity: medium
  - summary: >-
      epics.md:4800 (ENGINE-GAS-NETWORK Present today) says "one finite gas store per body", but several stores per body are admitted.
    evidence: |-
      Review pass 7 (EFE7-F2). GasSceneValidation.cs:39-41 sums every store on a body; ISO was corrected in pass 3 (BH3-2). Outside the repair and outside DOC and ISO; breaks no rule. Recorded in deferred-work.md.
    location: >-
      _bmad-output/planning-artifacts/epics.md:4800
    severity: low
  - summary: >-
      ENGINE-ISLANDS-SLEEP's acceptance has no case that catches the upward-normal support filters (:739, :769) surviving, although its Deletes line removes them.
    evidence: |-
      Review pass 7 (EFE7-F3). AC1 is met because ruling R3 assigns the target to that story; the missing acceptance case is a gap for that story's planning. Recorded in deferred-work.md.
    location: >-
      _bmad-output/planning-artifacts/epics.md ENGINE-ISLANDS-SLEEP acceptance
    severity: low
  - summary: >-
      epics.md ENGINE-LIQUID Present today says buoyancy exists only for a gas-filled body in air.
    evidence: |-
      Review pass 5 (EFE5-d1). Ambient loads apply to any dynamic body (GasCouplingValues.cs:19-20, Gas.cs:95-114). Outside the repair and outside DOC and ISO; breaks no rule.
    location: >-
      _bmad-output/planning-artifacts/epics.md:4834
    severity: low
---

<intent-contract>

## Intent

**Problem:** Story DOC-ENGINE-STATUS (`_bmad-output/planning-artifacts/epics.md:4461-4476`) is engine programme order 1 and touches documents only.
- `docs/gpu-f32-physics.md` (DOC) states absent features as present: flat SoA tables, 4-wide SIMD batching, an incremental dual Quad-BVH, DSU islands and sleeping, work-stealing dispatch, kinematic bodies, canonical pair order, any-pair contact events, a five-phase tick, and a zero-copy pose ring feeding instanced draws (run log E2.1). It also makes other claims the code contradicts (run log E4, finding 1).
- The sole design authority, `docs/architecture/element-package-isolation.md` (ISO), says at `:147` that no unresolved finding remains. At `:9` it says no unmet invariant is described as holding anywhere else.
- Other current documents also state these features as fact (run log E2.2 D, E4 finding 2).
- As a result, every element spec's engine-conformance section cites behaviour that does not exist.

**Approach:**
- **DOC and ISO.** Every sentence either cites the running code (`path:line` at the baseline) or is marked as target, with its owning story and a link to ISO Current status. ISO Current status gains one row per unmet claim group and loses the stale sentence.
- **Other current documents.** Restate their present-tense claims as target, with a link to Current status.
- **Code comments.** Two pose-ring comments stop calling the ring zero-copy and triple-buffered.
- **Records.** Owner decision 24q and its sequence wording are recorded. `ENGINE-ISLANDS-SLEEP` gains one Present-today line.

## Boundaries & Constraints

**Always:**

**Citation rules**
- **Present-fact rule.** A sentence that describes what the engine does today cites the running code, as `path:line` at `37a259c9` or a symbol with its `path:line`, and the code there does that.
- **Target rule.**
  - A sentence about something the engine does not do is written as target ("Target:", "The engine will …").
  - It names its owning story, taken from the index finding map (`engine-stories-index.md:125`) or from epics. It links to ISO [Current status](../../docs/architecture/element-package-isolation.md#current-status).
  - No owner name is coined (owner decision 3a).
  - A contradicted claim that has no existing owner is restated to what runs, or deleted. If it needs an owner, it goes to the coordinator.
- **The whole of DOC and ISO.** The rules apply to every sentence of DOC and ISO, not only the story's topic list.
  - The implementer reads both documents in full and records each contradicted sentence and its disposition in the run log.
  - The audit's off-topic findings are dispositioned: `DOC:149` buoyancy and cone bounds, `DOC:281` deterministic replays, `DOC:339-341` reuse and island storage, `DOC:372` fast inverse square root.
- **`DOC:59`.** It is not edited before merge (ruling R5).
  - Its claim of "flat SIMD collider tables" is qualified by the status paragraph under the `compilation-model` heading. That paragraph states that every "flat SIMD-aligned table" in the section is the `ENGINE-SOA-SIMD` target, and that today's tables are array-of-structures byte records (`engine/gpu/PhysicsGpuAbi.cs:22`, `:45`).
  - At merge, the publish-token holder re-checks `:59` against lane 1's result and edits it then.

**Pose ring wording**
- **Today:**
  - The ring has three fixed slots (`engine/gpu/WorkshopPoseRing.cs:14`, `CuriousContraptions.web/wwwroot/pose-ring.ts:13`). It is written every tick and read by no production code.
  - Every shared exchange goes through at least one JavaScript-mediated copy per side. C# composes a new array (`WorkshopPoseRing.cs:58-60`) and marshals it, the worker copies it (`SIM/wwwroot/worker.ts:290`), and the ring copies it again (`pose-ring.ts:96`).
  - The renderer is fed by the whole per-tick FlatBuffers read. On the main thread it goes `CuriousContraptions.web/wwwroot/workshop-client.ts:273-275` → `engine/gpu/BrowserWorkshopClient.cs:321-339` → `engine/MachineWorld.Gpu.cs:238-242`. The read is decoded again on the animation worker: `SIM/wwwroot/worker.ts:292-293` → `CuriousContraptions.Animation.Worker/Program.cs:213-215`.
- **Target:**
  - Under `ENGINE-WIRE-ABI`, the ring is multi-buffered with its depth derived from tick rate and render delay (owner decision 24k), and it becomes the renderer's feed.
  - Under `ENGINE-WASM-THREADS`, C# writes the ring in place with no producer copy, and a consumer reads with at most one copy (`epics.md:4714`).
- "Triple-buffered", "triple buffering" and "zero-copy" never describe today's ring.

**Tick diagram**
- **Today:**
  - The substeps per committed tick are 8, 4 and 2 at 60, 120 and 240 Hz (`SIM/WorkshopPhysicsEngine.cs:297`).
  - Supply grants and airflow run inside each substep (`:1010-1011`).
  - Residence, chambers and clearances run after the last substep (`:356-358`).
  - Passage sensing and signal consumption run on the device after the tick (`SIM/WorkshopGpuDevice.cs:243`, `:254-255`).
  - Publication follows (`SIM/Program.cs:339-347`).
- **Target:** one optical step and one thermal step after the last substep, with readings acting from the next tick (coordinator decisions 16 and 21). DOC cites the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) for every domain's target slot and does not restate it.
- **Supply phase.** It is fixed at 1/120 s at every cadence (`engine/gpu/ElectricalSupply.cs:9`) and is recorded as open, owned by `ENGINE-SUPPLY-DEMAND`.

**Open items with owners**
- **Unestablished properties.** Same-build replay determinism is unestablished; pair order is owned by `ENGINE-SPATIAL-QUERIES`. Non-sphere tunnelling is unestablished; it is owned by `ENGINE-TUNNEL-QUALIFICATION`.
- **Blocking waits.** The physics worker's `Atomics.wait` blocks for at most 2 ms per wait. At startup, qualification repeats such waits until it has 16 narrow exchanges, within 500 attempts, before the worker is ready (`clock-ring.ts:243-251`, `:246`; `engine/gpu/WorkshopClockDerivation.cs:13-17`, `:37-38`). It also blocks in the 100 ms derivation check (`SIM/wwwroot/worker.ts:168`, `:179` → `clock-ring.ts:270`; constants at `engine/gpu/WorkshopClockDerivation.cs:39-40`). This is stated as present, and it is changed only if R6 shows it matters. ISO §2.4 (`:119`) states it too.
- **Layouts.** The FlatBuffers wire is the control plane and the save. Every shared-memory and worker-message layout is hand-defined until `ENGINE-WIRE-ABI`.

**Wording rules**
- **Kinematic body.** A kinematic body or motion kind is not a supported kind, because no consumer needs one. The word is removed wherever it names a body or motion kind. "Kinematic" in the Jacobian or motion-math sense stays (`DOC:323`, `engine-contracts.md:106`). The refused member `KinematicMode.Kinematic` is deleted by lane 1's `spec-delete-declaration-colliders.md` (ruling R2). Lane 1 also corrects `DOC:145` and `docs/shared-clock-cadence.md:60` to Static and Dynamic only (coordinator, 25 November 2026). This slice does not duplicate that edit: it leaves both lines unedited, takes main's version at merge, and then corrects any remaining claim on them (for example `DOC:145`'s "island index", target `ENGINE-ISLANDS-SLEEP`).
- **Distance links and `SubShapeID`.** Both are removed. DOC describes the joints that exist (revolute, prismatic, tensile routes, ratio transmissions) and the contact identity that exists (`ContactFeatureIdentity`, `SIM/WorkshopFrictionSolver.cs:9-10`).
- **Element names.** An edited sentence that states an engine rule names no element; element examples in edited lines are dropped.
- **Headings.** A DOC heading with no inbound link that names an absent feature is retitled (for example `:20` "Zero-Copy Shared Memory Pipeline", `:270` "Island Partitioning and Sleeping", `:308` "Hierarchical `SubShapeID`"). Linked headings keep their text.

**Preservation**
- Every explicit anchor id of DOC and ISO is kept.
- The text of every heading whose slug is linked is kept (run log E2.2 E and E4 finding 7).
- All correct content is unchanged.
- Every relative link resolves (`ArchitectureDocsConsistencyTests`), and that test's banned strings do not appear.
- Edits outside DOC and ISO are line-local.

**Never:**
- Required or target design text is not edited:
  - `AGENTS.md`
  - `_bmad/custom/*`
  - `docs/delivery-workflow.md`'s Tri-Graph section
  - `docs/planning/requirements.md`
  - `prd.md`
  - the epics NFRs (`epics.md:46-47`)
  - `docs/planning/invest/*`, except `current-consumers.md:3` and the 24q sequence lines named in the Code Map
- Historical records are not edited:
  - `_bmad-output/implementation-artifacts/**`, except this slice's records and `deferred-work.md`
  - `readiness/audits/**`
  - `docs/work-orders/**`
  - retros
  - `delivered-slices.md`
  - epics story texts before `:4421`
- No code, schema, test or build change, except the two comment lines in `engine/gpu/WorkshopPoseRing.cs`.
- No edit to the `tools/e2e` comments (ruling R5).
- No hash or byte assertion, no mutants and no browser gate.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| M1 | Every sentence of DOC and ISO | Cites running code at the baseline, or is target with an owner story that exists, or is restated or deleted. The story's topics and every E2.1 and E4 claim are covered. Exceptions until merge: the lane 1 lines `DOC:59` (qualified by the status paragraph) and `DOC:145` | The reviewer names the line and the claim the code contradicts |
| M2 | ISO Current status | One row per unmet claim group, each with an owner. No "no finding remains" sentence. §2.1, §2.2, §2.4 and invariant 3 are cited present or marked target | The reviewer names the missing row |
| M3 | `DOC:259` (stale supported-impact limitation), `DOC:146` ("static hollow") | The stale text is deleted. Restitution groups are described as they run (`SIM/WorkshopConstraintSolver.cs:645-669`, `:722-752`, energy bound `:709`), with the 8-point cut (`:685-688`) and the +Y support assumption (`:739`, `:769`) open under `ENGINE-ISLANDS-SLEEP`. Hollow kinds are admitted on static and dynamic bodies (`:161`) | Names the line |
| M4 | Current documents and code comments across the repository, excluding the Never list | No present-tense statement that the renderer reads or samples the pose ring, that the ring is zero-copy or triple-buffered, that draws are instanced, that tables are SoA, that islands or sleeping exist, or that hollow kinds are static only. Exceptions until merge: the lane 1 lines `DOC:145`, `docs/shared-clock-cadence.md:60` and `epics.md:4466` | The `rg` check names the file and line |
| M5 | The other current documents | Each line found by the implementer's own sweep (E2.2 D and E4 finding 2 as the starting set) is target with an owner or Current-status link, or cites present code | Names the line |
| M6 | Inbound anchors (explicit ids and heading slugs) and relative links into the edited documents | All resolve unchanged | Names the anchor or link |
| M7 | Owner decision 24q and the sequence lists | 24q follows 24p's rulings. Each listed sentence says DOC-ENGINE-STATUS runs in lane 4 from 25 November and that order 2 onwards waits for the pre-programme work. No current list still says only repairs and tooling run before order 1. Unchanged records, named: decision 22 itself (`owner-decisions-2026-11-22.md:33`), the dated propagation note `readiness/batch-e.md:5`, OS-01 (`completion-ledger.md:392`), the per-slice "When" lines (`delivery-batches.md:246-337`), decisions 24h and 24m (`owner-decisions-2026-11-22.md:61`, `:67`, dated decisions), and the element and README lines that order one slice relative to the programme (`EL-125:142`, `EL-187:123`, `EL-188:124`, `EL-189:134`, `EL-190:118`, `EL-191:121`, `EL-192:126`, `EL-204:128`, `EL-208:141`, `EL-189-tennis-ball.md:55`, `EL-208-balloon.md:50`, `docs/planning/elements/README.md:411`), which 24q does not change because they say when that slice runs, not what runs before order 1 | Names the file and line |
| M8 | Control: the same checks run on the baseline files | Each check reports the baseline claims | Names the check that is silent at the baseline |

</intent-contract>

## Code Map

**Baseline:** `37a259c9a2f08669cd339e36c3f2c6b4a3f7bd68` (`origin/main` at dispatch, coordinator decision 38).
- Worktree `.claude/worktrees/slice-doc-engine-status`, branch `slice-doc-engine-status`.
- Every `path:line` was measured at that commit (run log E2, E4).

**Status:** done in lane 4, under owner decision 24q (25 November 2026). Review passes 1 to 8 are repaired or deferred with owners, and the Final diff scope check passes (Review Triage Log). The final round is recorded in run log E23. The merge and SnapshotApproval are the publish-token holder's checks.

**Planning verdict:** Scoped Pass, 25 November 2026, at the sixth check (run log E9).
- H1–H4 are closed. The check's one remaining finding was a one-line addition it prescribed: add `epics.md:4466` to Design Notes 8. It is applied verbatim and is re-read by the step-04 informed auditors.
- Audits 1 and 2 and checks 3 to 5 failed. Their findings are repaired (E4–E8).

**Review tier:** engine. All six layers run on the first pass.

**Shape:** one documents slice, with no element and no capability.

**Implementation owner:** agent `a5d63a4e105212290` (fresh general-purpose agent, inherited model, no history; launched 25 November 2026). Handoff results: run log E10–E13.

### Owned scope

**Product documents**

1. **`docs/gpu-f32-physics.md` (DOC).**
   - A full-sentence audit of the whole document.
   - Claims C1–C43 of run log E2.1 and E4 finding 1 are disposed as the table below says.
   - Add one status paragraph under the `compilation-model` heading (it covers `:59`).
   - Add an "Implementation status" lead-in at the start of `modern-engine-patterns`, saying which patterns are target and qualifying `:281`.
   - Redraw the tick diagram (`:161-189`).
   - Delete `:259`'s limitation.
   - Rows `:146` and `:149`.
   - Patterns `:283-297`, `:308-311`, `:322`, `:332-336`, `:339-341`, `:344`, `:351-353`.
   - Envelope rows `:372`, `:375` and `:377`.
2. **`docs/architecture/element-package-isolation.md` (ISO).**
   - A full-sentence audit.
   - Invariant 3 (`:21`) and §2.2 (`:106`): the pose ring as a target query surface (`ENGINE-WIRE-ABI`).
   - §2.1 (`:102`) and §2.4 (`:119`): today and target, as stated under Always.
   - §4: refresh the heading date and `:143` to this baseline, and delete `:147`.
   - New §4.4, "Engine features stated as target", one row per group:
     - SoA and aligned tables: `ENGINE-SOA-SIMD`.
     - SIMD contact, narrowphase and broadphase lanes: `ENGINE-SOA-SIMD`.
     - Persistent dual tree, Quad-BVH, velocity fattening, use for every query, plane bounds and canonical pair order: `ENGINE-SPATIAL-QUERIES`.
     - Islands and sleeping, plus restitution's 8-point cut and +Y support: `ENGINE-ISLANDS-SLEEP`.
     - Parallel dispatch: `ENGINE-PARALLEL-ISLANDS` and `ENGINE-WASM-THREADS`.
     - Any-pair contact events: `ENGINE-CONTACT-OBSERVATIONS`.
     - In-tick, frame-relative sensing and passage timing: `ENGINE-SENSOR-VOLUMES`.
     - Supply phase at every cadence: `ENGINE-SUPPLY-DEMAND`.
     - Optical and thermal slots: `ENGINE-OPTICAL` and `ENGINE-THERMAL`, per the combined tick order.
     - Pose ring reader, derived depth, hand-defined layouts and per-read allocation: `ENGINE-WIRE-ABI`.
     - Copies between runtimes: `ENGINE-WASM-THREADS`.
     - Instanced draws: `ENGINE-INSTANCED-RENDERING`.
     - Tick allocation: `ENGINE-TICK-ALLOCATION`.
     - Region-scoped force regions and field-scaled lift: `ENGINE-FIELD-FORCE`.
     - Liquid buoyancy: `ENGINE-LIQUID`.
     - GPU names: `ENGINE-CAPABILITY-INSTANCES`.
     - Non-sphere tunnelling: `ENGINE-TUNNEL-QUALIFICATION`.
     - Same-build replay: unestablished; pair order belongs to `ENGINE-SPATIAL-QUERIES`.
     - `Atomics.wait`: recorded; changed only on R6 evidence.
     - `KinematicMode.Kinematic`: `spec-delete-declaration-colliders.md` (lane 1). Drop this row at merge if lane 1 publishes first.
3. **`engine/gpu/WorkshopPoseRing.cs:7`, `:13`.** Comments only, rewritten to the pose ring wording above.
4. **Other current documents.** Line-local edits. The starting set comes from E2.2 D, E4 finding 2 and the additions of E6 and E7:
   - `DESIGN.md:17`, `:19`, `:21`;
   - `docs/engine-contracts.md:11`, `:25-26`, `:232`;
   - `docs/general-engine-design.md:16`, `:18`, `:137`, `:145`, `:168-169`, `:191-192`, `:199`, `:202-203`, `:217`;
   - `docs/simulation-presentation-bridge.md:3`, `:26`, `:33`, `:83`, `:95`, `:99`;
   - `docs/shared-clock-cadence.md:58`, `:72` (`:60` is lane 1's; see Concurrency);
   - `docs/browser-physics-performance.md:7`, `:9`, `:65`, `:69`;
   - `docs/planning/invest/current-consumers.md:3`;
   - `_bmad-output/planning-artifacts/architecture.md:6`, `:43`, `:51`, `:72`, `:92-101`, `:106`, `:169`, `:182`, `:189`, `:240`, `:256-257`, `:313`, `:319`, `:338`;
   - `docs/planning/elements/EL-156-single-lobe-cam.md:28` and `EL-158-rise-hold-fall-cam.md:29` (static-only hollow);
   - `EL-065-*.md:89`, `EL-069-*.md:86` and `docs/planning/elements/readiness/batch-i.md:170` (the "still static only" comment remark, now stale).

   The implementer re-sweeps with the M4 `rg` and adds any further hit. A line that states a target or rule is left unchanged and named in the run log. The final edited-line count is reported.

**Planning records** (line-local; shared under 24m)

5. **`owner-decisions-2026-11-22.md`.** Add 24q after 24p's ruling (f) (`:77`), before decision 38. 24q also says what "pre-programme" means: everything before order 2.
6. **Sequence wording.** Each place gets one clause: "DOC-ENGINE-STATUS, programme order 1, documents only, runs in lane 4 from 25 November 2026 in parallel with the remaining pre-programme work (owner decision 24q); order 2 onwards waits". The places:
   - `docs/planning/invest/delivery-batches.md:13-14`;
   - `epics.md:81` and `:4426`;
   - `engine-stories-index.md:5`;
   - `docs/planning/invest/vertical-delivery.md:9` and `:22-23`;
   - `docs/planning/invest/named-elements.md:22`;
   - `completion-ledger.md:128`;
   - `TODO.md:7` and `:17`.

   OS-01 (`completion-ledger.md:392`) lists the pre-programme slices and is unchanged. The per-slice "When" lines in `delivery-batches.md:246-337` stay accurate.
7. **`epics.md:4559`.** ENGINE-ISLANDS-SLEEP *Present today* gains one sentence on the +Y support assumption (`SIM/WorkshopConstraintSolver.cs:739`, `:769`).
8. **`completion-ledger.md:268`.** PD-23 status becomes the slice outcome.
9. **`sprint-status.yaml:587`.** The `doc-engine-status` key.
10. **`deferred-work.md`.** The `tools/e2e` comments (owner S5c, or the browser phase), and any close-note items.
11. **This contract and its run log.**

### Claim disposition (run log E2.1 and E4 IDs)

**Present, cited:**
- C2
- C13 (8/4/2 at 480 Hz)
- C23
- C31 (with the cut)
- C36 (static–static pruning in one tree)
- C41
- I2
- I3 (bootstrap 16)
- A2
- A7

**Target with owner:**
- `ENGINE-SOA-SIMD`: C5, C9, C10, C14, C32.
- `ENGINE-SPATIAL-QUERIES`: C6, C11, C24, C25, C26, C35, C36 (dual tree), C39.
- `ENGINE-ISLANDS-SLEEP`: C12, C33, and C30's cut and +Y.
- `ENGINE-PARALLEL-ISLANDS` and `ENGINE-WASM-THREADS`: C34.
- `ENGINE-CONTACT-OBSERVATIONS`: C19.
- `ENGINE-SENSOR-VOLUMES`: C18, C42.
- `ENGINE-WIRE-ABI`: C3, C4, C7, C22, C40, I3, I4. C1 and C3 are also owned by `ENGINE-WASM-THREADS`.
- `ENGINE-INSTANCED-RENDERING`: C8.
- `ENGINE-TUNNEL-QUALIFICATION`: C28, C29 (non-sphere).
- Combined order with `ENGINE-OPTICAL` and `ENGINE-THERMAL`: C20, C21.
- `ENGINE-TICK-ALLOCATION`: `DOC:339-341`'s per-tick allocation.

**Words removed:**
- C15 (kinematic body kind): removed by lane 1 at `DOC:145`. The island-index part of the same line is corrected at merge.
- C17 and C38 (distance links).
- C37 (`SubShapeID`).
- C16 ("static hollow"; participation is restated as `CollisionParticipation` at `ABS/ColliderDescriptor.cs:6`, with `SensorParticipation` at `ABS/PhysicsDeclarationValues.cs:13`).

**Restated to fact:**
- C27: "closed-form, fixed-count", not "branchless".
- C30: the stale limitation is deleted.
- C43: resolved by ISO §4.4.
- I1: deleted.
- `DOC:372`: normalisation by `Quaternion.Normalize` and `MathF.Sqrt`, with no fast inverse square root.
- `DOC:149` (force regions):
  - Present, cited: gravity, guides, conical airflow (`ABS/ConicalAirflow.cs`), and declared ambient loads on any dynamic body: an acceleration (scaled by atmosphere pressure and, optionally, by an owned gas inventory's fraction) plus linear drag (scaled by atmosphere pressure only) (`ABS/GasCouplingValues.cs:19-20` → `engine/gpu/WorkshopPhysicsCompiler.Gas.cs:36-39` → `SIM/WorkshopPhysicsEngine.Gas.cs:95-114`). Amended in review pass 5: the earlier "gas-filled body" wording was too narrow.
  - Target: region-scoped force regions (box or cylinder) and field scaling of declared lift under `ENGINE-FIELD-FORCE` (`epics.md:5235`); liquid buoyancy under `ENGINE-LIQUID` (`epics.md:4829`). `BuoyancyCoefficient` is only declared and validated (`ABS/FluidContractValues.cs:19`).
  - The "clamped to ≤ 64 m/s²" claim is restated as two refusals at compile.
    - An evaluated declared acceleration whose length exceeds 16 m/s² is refused (`ABS/WorldAccelerationDeclaration.cs:25`, reached from `engine/gpu/WorkshopPhysicsCompiler.Gas.cs:37`).
    - Separately, gravity plus a pressure-scaled ambient load is refused when its combined magnitude exceeds 16 m/s² (`engine/gpu/GasSceneValidation.cs:78`).
    - The guide maximum is 12 (`ABS/PhysicsDeclarationValues.cs:316`).
  - A region shape with no running code and no owner (cone regions) is removed.
- `DOC:281`: qualified by the status lead-in.

### Capability probe

Documents only. The slice delivers no capability and uses none. The scouts' and auditor's citation inventory (run log E2, E4) is the planning evidence. No owner decision is open; rulings R1–R5 settled the ones reported (E3).

### Decisions

- **R1, scope (coordinator, 25 November 2026).** In scope: DOC, ISO, the two pose ring comments, and the other current documents under M4 and M5. Out of scope: required and target design text, and historical records. Reason: ISO:9 makes other documents' present-tense claims part of the Outcome.
- **R2.** `KinematicMode.Kinematic` is deleted by lane 1's `spec-delete-declaration-colliders.md`. This slice changes wording only.
- **R3.** Restitution +Y goes to `ENGINE-ISLANDS-SLEEP`. Distance links and `SubShapeID` are removed. The Quad-BVH goes to `ENGINE-SPATIAL-QUERIES` (index `:125`, "broadphase → 2"; E3).
- **R4.** The tick diagram shows today's order with optics and thermal as target. It cites the real renderer feed. The `WorkshopPoseRing.cs` comments are corrected.
- **R5.** The `tools/e2e` comments go to `deferred-work.md`. `DOC:59` is re-checked at merge.
- **D1, stale story citations (decided).**
  - Three of the story's static-only hollow statements were fixed before the baseline (`dbaacad1`).
  - The story's renderer citation (`workshop-client.ts:152`, `:256-257`) points at the unused reader.
  - Both are recorded here; the story text is not edited.

### Concurrency

- Lane 1 (`Colliders`) and lane 2 (S5b, S5c, S6) change code that DOC and ISO cite. Citations are at the baseline. At merge, every citation into a file that main changed is re-checked, and so is `DOC:59`.
- Lane 1's uncommitted contract (`spec-delete-declaration-colliders.md:175-176`, its ruling 5 and two patch findings) also edits `DOC:145` (the kinematic motion kind), `docs/shared-clock-cadence.md:60` and the DOC-ENGINE-STATUS story text (`epics.md:4466`). Under the coordinator's message of 25 November 2026, lane 1 owns those lines. This slice leaves `DOC:145`, `shared-clock-cadence.md:60` and `epics.md:4466` unedited and takes main's version at merge. It then corrects any claim still on `DOC:145`, for example "island index", which is owned by `ENGINE-ISLANDS-SLEEP`. If lane 1 publishes first, this slice drops the §4.4 `KinematicMode.Kinematic` row at merge.
- Shared documents are edited line-locally.

## Tasks & Acceptance

**Execution:**
1. DOC: the full-sentence audit and disposition, the status paragraphs, the tick diagram, deletions, and anchors kept.
2. ISO: the full-sentence audit, §2.1, §2.2, §2.4, invariant 3, the §4 refresh, the deletion of `:147`, and §4.4.
3. `WorkshopPoseRing.cs:7`, `:13` comments.
4. Other current documents: line-local edits, an M4 re-sweep, and the reported line count.
5. Planning records: 24q, the sequence wording, the ENGINE-ISLANDS-SLEEP line, PD-23, the sprint key, deferred work and TODO.
6. Verification, and the acceptance-to-test table.

**Acceptance Criteria:**
- **AC1.** Given DOC and ISO at the candidate, when a reviewer reads every sentence, then each cites code that does what it says at the baseline, or is target with an existing owner story. No claim is contradicted by the code. The exceptions are the lane 1 lines: `DOC:59`, which is qualified by the status paragraph, and `DOC:145`, which lane 1 corrects. Both are re-checked at merge.
- **AC2.** Given the candidate, when the M4 `rg` checks run over the current documents and code comments of the whole repository (excluding the Never list), then every hit is classified in the run log as target, rule, negation, historical, cited present or lane 1 line (exactly `DOC:145`, `shared-clock-cadence.md:60` and `epics.md:4466`, until merge), and no other hit is a present-tense false claim. "Historical" also covers facts pinned to a named commit, such as CAT-023's legacy-harvest table (`CAT-023-domino.md:105`, `:107`, at `a6c914e`). Every line of the owned-scope list (items 1–4) has its disposition in the run log. Control: the same checks on the baseline (`git grep` at `37a259c9`) are recorded with their hit count and the owned-scope lines they catch. Owned lines they do not catch are covered by name by M5.
- **AC3.** Given the full local round, when it runs, then failures are 0 and skipped are 0, and no test is added or removed by this slice. The total equals that of the base it is measured on (7407 expected at `37a259c9`). The registration check is likewise unchanged (821 expected), and the maintainability gate passes with both canaries.
- **AC4.** Given the planning records, when they are read, then 24q and the sequence clause appear in every M7 place with the same meaning. A search for current lists that say only repairs and tooling run before the programme finds none without the 24q clause.

## Spec Change Log

- 25 November 2026, planning audit 1 (run log E4, Fail, ten findings). Repairs:
  - F1: AC1 and M1 cover every sentence of DOC and ISO, with the off-topic claims dispositioned.
  - F2: the missed lines are added (ISO §2.4, `DOC:339-341`, `architecture.md:72` and `:106`, `simulation-presentation-bridge.md:99`, `general-engine-design.md:192`, `:199` and `:203`, EL-158, EL-065, EL-069, `batch-i.md:170`).
  - F3: `DOC:59` is qualified by the status paragraph and re-checked at merge.
  - F4: M4 and AC2 are widened to the repository with full phrase classes. M5 comes from the implementer's own sweep. M7 and AC4 check meaning.
  - F5: the Never list now excepts the 24q sequence lines. `named-elements.md:22` and `completion-ledger.md:128` are added, and 24q defines "pre-programme".
  - F6: AC3 is "no test added or removed".
  - F7: heading slugs are preserved and checked.
  - F8: the copy wording follows the story and `ENGINE-WASM-THREADS`.
  - F9: citations are corrected (`:273-275`, `CollisionParticipation`, `epics.md:4559`, clock-ring sites).
  - F10: "kinematic" is narrowed to the body or motion kind, element examples are dropped from edited lines, lane 1's contract is named, and the edit count is reported.
  - KEEP: rulings R1–R5, the claim dispositions, and the owner mapping.
- 25 November 2026, planning audit 2 (run log E5, Fail: F1–F10 closed except `DOC:149`; six new findings). Repairs:
  - N1: M4 uses phrase classes with stated flags and exclusions.
  - N2: Concurrency records lane 1's edits of `DOC:145`, `shared-clock-cadence.md:60` and `epics.md:4466`, and the later publisher's merge rule.
  - N3: `DOC:149` is decided (`ENGINE-FIELD-FORCE`, §4.4 row).
  - N4: the startup `Atomics.wait` loop is stated.
  - N5: M7 names the unchanged records.
  - N6: unlinked headings naming absent features are retitled.
  - Also: design answer 7 is shortened and M3 is reworded.
- 25 November 2026, bounded third check (run log E6, Fail: three repair defects). Repairs:
  - T1: M4 gains low-noise classes and full-path exclusions. The control records what the search catches. M5's named list is the inventory, and the missed lines are added to item 4 (`architecture.md:6`, `:51`, `:256`; `simulation-presentation-bridge.md:26`).
  - T2: `DOC:149` cites ambient lift as present, gives region lift to `ENGINE-FIELD-FORCE` and liquid buoyancy to `ENGINE-LIQUID`, and restates the bound as 16 m/s². Both rows are added to §4.4.
  - T3: M7 names every unchanged record.
  - Coordinator message (25 November 2026): lane 1 owns `DOC:145` and `shared-clock-cadence.md:60`. This slice does not duplicate that edit and takes main's version at merge.
- 25 November 2026, bounded fourth check (run log E7, Fail: one blocking finding, five low). Repairs:
  - G1: a "lane 1 line" exception in M1, M4, AC2, the Verification classes and Design Notes 7.
  - G2: five more lines in item 4; the inventory wording.
  - G3: two more M7 records.
  - G4: "refused at compile above 16 m/s²".
  - G5: `Atomics\.waitAsync`, `static hollow\b`.
  - G6: the verdict line.
- 25 November 2026, bounded fifth check (run log E8, Fail: three blocking low findings in the repair text, one deferrable). Repairs:
  - H1: `epics.md:4466` joins the lane 1 exception in M4, AC2 and Verification.
  - H2: the CAT-023 legacy-harvest lines are dropped from item 4, and "historical" covers commit-pinned facts.
  - H3: the two acceleration refusals are stated separately.
  - H4: item 4's label is corrected.
- 25 November 2026, review pass 5, EFE5-1 (Code Map amendment outside the intent contract). The `DOC:149` bullet under Claim disposition is amended. It now gives declared ambient loads on any dynamic body, scaled by atmosphere pressure and optionally by a gas inventory, in place of "ambient buoyant lift of a gas-filled body". This avoids the known-bad state where ambient drag is described as gas-only. KEEP: the region-lift target owned by `ENGINE-FIELD-FORCE`, the liquid buoyancy target owned by `ENGINE-LIQUID`, and the 16 m/s² refusal wording.

## Review Triage Log

### Passes 1–4, compacted (the verbatim rows of every finding are in run log E22; contract budget)
- Pass 1 (engine tier, all six layers; E14): 51 findings. high 1 (FO-1, a sleep-off control), medium 9, low 26; 15 rejected or descriptive (rulings R1 and R2, D1, the lane 1 lines); 1 deferred (VG-1, the automated anchor test, under the intent contract's Never clause). All patched (E15).
- Coordinator ruling D2 (on EFE-F8): M1 is met when every sentence on the story's topic list, changed or unchanged, cites running code or is target with an owner, and no other sentence of DOC or ISO is contradicted by the code. This is the story's own acceptance.
- Pass 2 (five layers; E16): 33 findings. high 1 (FO2-1, a second tick-order rule), medium 8, low 17, false 7. All patched (E16).
- Pass 3 (five layers, repair diff; E17): 30 findings. high 1 (FO3-1), medium 5, low 18, false 4; 2 deferred (the Phase 1–5 citations and the anchor test). Patched (E17).
- Pass 4 (five layers, repair diff; E18): 24 findings. medium 3, low 15; 3 deferred or noted. The stagnation reassessment required a sentence-by-sentence code check, with pointers in place of unverified detail. Patched (E18).

### 2026-11-25 — Review pass 5 (Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free; repair diff from the pass-4 tree `5000c608`; Re-review scope; detail in run log E19)
- verdicts: 15 findings. high 0, medium 2, low 10, false 2, maybe-false 0; 1 deferred. Forward-Only Pass. Every M4 citation in DOC and ISO resolves (270 checked).
- findings:
  - `[medium]` `[patch]` EFE5-1, BH5-3 Ambient loads are described as gas-only (ISO `:217`, `:219`; DOC `:153`; GED stage 2). They are declared on any dynamic body and scaled by atmosphere pressure, with the gas inventory fraction optional (`GasCouplingValues.cs:19-20`, `WorkshopPhysicsCompiler.Gas.cs:36-39`, `Gas.cs:95-114`). The contract's Code Map `DOC:149` bullet prescribed the narrow wording; the coordinator amended it (Spec Change Log).
  - `[medium]` `[patch]` EFE5-2, EC5-2 GED stage 2 "angular velocity changes only through impulses". The spin clamp also writes angular velocity, as does the non-finite reset (`WorkshopConstraintSolver.cs:61-68`, `:79`; `WorkshopPhysicsEngine.cs:1182`). This is the second wrong version of the sentence, so per the stagnation rule it is deleted and replaced with a pointer to DOC's Maximum Velocities row.
  - `[low]` `[patch]` EFE5-3, BH5-2, EC5-1 GED stage 2 cites a bare `(:65-66)`. Name `WorkshopPhysicsEngine.ContinuousReplay.cs:65-66`.
  - `[low]` `[patch]` EFE5-4 Run log E18 calls baseline `deferred-work.md:211` "tensile-creep". It is the Story 10.3b lever entry. Append a correction.
  - `[low]` `[patch]` BH5-1 GED stage 4 calls the cylinder solvers "closed-form", but rims use fixed-count searches (`WorkshopCollisionKernels.cs:1202-1211`, `:1347-1377`). Say "analytic, with fixed-count searches for cylinder rims".
  - `[low]` `[patch]` BH5-4, EC5-3 The pose-slot citations miss the debt-stop caller at `Program.cs:296`. Cite `EmitRead` (`:335`, `:347`) as the single site.
  - `[low]` `[patch]` BH5-5, EC5-5 `:777` (supports join only from manifolds that do not bounce) is classified inconsistently: intended in DOC, a limitation in epics, absent from ISO. State it once, consistently: present today, open under `ENGINE-ISLANDS-SLEEP` (whose island grouping replaces it). Put it in DOC's open list, the ISO islands row and `epics.md`.
  - `[low]` `[patch]` EC5-4 The stale drive-supply citations also include `deferred-work.md:209` and `:209-213` (EL-196, EL-199, EL-068, `readiness/batch-c.md:79`). Widen the deferred entry's selector.
  - `[low]` `[patch]` EC5-6 GED stage 8 cites `:279` for event staging. Finite-work staging is at `:277`, and both run when the commit is prepared (`PreparePhysicalCommit`, `:280`).
  - `[low]` `[patch]` VG5-1 GED Electrical omits that a route closes only while its source has energy left (`engine/gpu/SupplyRoute.cs:336`).
  - `[low]` `[defer]` EFE5-d1 `epics.md:4834` (ENGINE-LIQUID Present today) says "buoyancy exists only for a gas-filled body in air", the same narrowing. Outside the repair, outside DOC and ISO; frontmatter `deferred`.
  - `[false]` `[reject]` FO5 borderlines (the payer chosen by data, the `EmitRead` callers, the hollow pointer, the ANIM-1 deletion): all compliant.
  - `[false]` `[reject]` Evidence-limit notes, and AC3/E9 pending the final round and gate.

### 2026-11-25 — Review pass 6 (Blind Hunter, Verification Gap, Forward-Only, Element-Free; Edge Case Hunter not launched, because the repair changed no traced path; repair diff from the pass-5 tree `2ac5a74c`; detail in run log E20)
- verdicts: 13 findings. high 0, medium 1, low 8, false 1, maybe-false 0; 3 deferred. Forward-Only: no violation.
- findings:
  - `[medium]` `[patch]` VG6-1, BH6-1, EFE6-7 The pass-5 instruction to delete the `:777` rule and say "island grouping replaces these rules" is unsupported: no contract line and no acceptance case. Remove `:777` from ENGINE-ISLANDS-SLEEP *Deletes*, from DOC's open list and from ISO's "replaces" sentence. State it as cited present behaviour. Its target decision is deferred to ENGINE-ISLANDS-SLEEP planning (frontmatter `deferred`).
  - `[low]` `[patch]` EFE6-1 GED stage 8 splits event staging by kind, but the code splits by operation. An `ApplyFiniteWork` operation stages only finite work (`:277`); an advance stages every kind (`:279`). Both run before `PreparePhysicalCommit` (`:280`).
  - `[low]` `[patch]` BH6-3 The GED narrowphase fixed-count searches also cover box–cylinder edges (`SegmentSteps`, `WorkshopCollisionKernels.cs:1418`), not only rims.
  - `[low]` `[patch]` BH6-4 The GED pointer to the envelope should name both the Maximum Velocities and Non-Finite Quantities rows, or the envelope as a whole.
  - `[low]` `[patch]` BH6-2 The deferred stale-citation selector misses `readiness/batch-c.md:106` (`:212`). Widen the selector.
  - `[low]` `[patch]` FO6-o ISO `:219` leaves out the optional inventory fraction that `:217` states. Make the two consistent.
  - `[low]` `[patch]` EFE6-2, EFE6-3, EFE6-4, EFE6-5 Coordinator records: the frontmatter stale-citation entry is widened, the Status line is restated in this edit, the Code Map `DOC:149` bullet no longer applies the inventory fraction to drag, and the deferral records are completed. EFE4-d1 is resolved by the pass-4 rewrite of GED stage 6, so it needs no record. EFE4-d3 is a note, not a deferral. Each deferred item also goes into `deferred-work.md` through the owner's patch.
  - `[low]` `[defer]` EFE6-6 `architecture.md:230` (balloon formula) is outside the repair and outside DOC and ISO; frontmatter `deferred`.
  - `[low]` `[defer]` BH6-5 `epics.md:5240` cites a nonexistent `SIM/WorkshopPhysicsCompiler.Gas.cs`. Present at baseline and outside the repair; frontmatter `deferred`.
  - `[medium]` `[defer]` VG6-1b The target for the mixed-manifold `:777` rule goes to ENGINE-ISLANDS-SLEEP planning; frontmatter `deferred`.
  - `[false]` `[reject]` FO6 borderlines (data-selected inventory fraction, non-finite reset, `#if PLAYTEST` trace, lane 1 exceptions): compliant.
- tally correction (EFE7 note): pass 6 had 11 finding rows. By verdict: medium 2 (VG6-1 patch, VG6-1b defer), low 8, false 1. Three were deferred: EFE6-6, BH6-5 and VG6-1b.

### 2026-11-25 — Review pass 7 (Blind Hunter, Verification Gap, Forward-Only, Element-Free; repair diff from the pass-6 tree `81400915`; detail in run log E21)
- verdicts: 8 rows. medium 0, low 6, false 2, maybe-false 0; 2 deferred. Forward-Only: no violation. Verification Gap: no gap.
- findings:
  - `[low]` `[patch]` BH7-2, BH7-3 The `:777` rule has contradictory ownership. ISO says "not owned here", the deferred entry names ENGINE-ISLANDS-SLEEP planning, and epics and DOC `:277` give no pointer. Make all three agree: present behaviour, with the target decision deferred to ENGINE-ISLANDS-SLEEP planning through `deferred-work.md#restitution-mixed-manifold-rule`. DOC `:277` and `epics.md:4559` link that entry.
  - `[low]` `[patch]` BH7-1, VG7-1 The stale-citation entry misdescribes `readiness/batch-c.md:106`, which cites the wrong-symbol wording (`ElectricalSupply.Allocate`), not drive supply. It also misses `completion-ledger.md:288` (PD-43), which cites `deferred-work.md:221`. Fix the entry's wording and selector.
  - `[low]` `[patch]` EFE7-F1 The source of the new `epics.md:4834` deferred entry says pass 6. It is pass 5 (EFE5-d1).
  - `[low]` `[defer]` EFE7-F2 `epics.md:4800` "one finite gas store per body" is outside the repair; frontmatter `deferred`.
  - `[low]` `[defer]` EFE7-F3 ENGINE-ISLANDS-SLEEP has no acceptance case for the upward-normal filters. This is a gap in that story's planning; frontmatter `deferred`.
  - `[low]` `[patch]` EFE7-note The Status line is restated in this edit (coordinator).
  - `[false]` `[reject]` FO7 borderlines: the `:777` single membership test, staging by operation, the inventory fraction, and carried rulings. All compliant.
  - `[false]` `[reject]` Evidence limits, and AC3/E9 pending.

### 2026-11-25 — Review pass 8 (Verification Gap, Forward-Only, Element-Free; Blind Hunter and Edge Case Hunter not launched because the repair touched records and one link only; repair diff from the pass-7 tree `4e242aa3`; detail in run log E22)
- verdicts: 2 findings. low 2; both are records, both patched. Forward-Only: Pass. Verification Gap: no gap. The Element-Free auditor confirmed every pass-7 row repaired, and no new contradicted claim in DOC or ISO.
- findings:
  - `[low]` `[patch]` EFE8-1 The frontmatter stale-citation `deferred:` entry kept the pre-repair wording. The coordinator restated it: selector `(209|211|212|221)`, 14 files, the drive-supply citers and the `Allocate`-wording citers listed separately.
  - `[low]` `[patch]` EFE8-2 The M2 Execution cell in the acceptance table cited only E15–E17. The owner added E19–E21 (run log E21). The coordinator checked the cell against E19–E21; it is a records-only line, so no further review layer is needed.
- Review loop closes at pass 8. All confirmed findings are repaired or deferred under Re-review scope with an owner. Next: the Final diff scope check, then the maintainability gate and the full round on the final candidate.

### Final diff scope (2026-11-25)
- **Pass.** Checked by the pass-8 Element-Free auditor, re-engaged as the informed reviewer, on the full owned candidate: 28 files, +1473/−224 against `37a259c9`, every hunk inspected.
- **Kept:** every hunk serves the full-sentence audit, an M4/M5 claim class, a confirmed repair, or a planning record the contract names. Product edits outside DOC and ISO total +94/−94. R1 exclusions are respected, and the lane 1 lines are byte-identical.
- **Removed:** nothing. Simplification: no justified opportunity; three candidates were considered and rejected (run log E22).
- **Final candidate checks (run log E23):**
  - maintainability gate PASS, with both canaries;
  - registration check 821/0/0;
  - full round 7407/0/0, `Skipped: 0`.

## Design Notes

The eight design-record questions (`_bmad/custom/element-free-engine-design.md` §6):
1. **What is delivered:** documents that state the engine as it runs, with every target owned by a named story. No runtime behaviour changes.
2. **What is reusable:** nothing new. ISO Current status stays the one register of unmet invariants.
3. **What is declared:** nothing.
4. **How it executes:** not applicable. Citations trace the running tick (run log E2.2 A).
5. **Who owns changes:** each target names its engine story, and ISO §4 is the single list.
6. **What proves it:** an independent citation audit, plus `rg`, anchor and link checks, each with the baseline as control.
7. **What is deleted:** `DOC:259`'s stale limitation, `ISO:147`, the static-only hollow text, every present-tense M4 claim in scope, and the words kinematic (body kind; lane 1 removes it at `DOC:145` and `shared-clock-cadence.md:60`), distance links and `SubShapeID`.
8. **What is uncertain:** nothing required now. At merge, `DOC:59`, `DOC:145`, `shared-clock-cadence.md:60` and `epics.md:4466` are re-checked against lane 1's result.

Element-free rules: no engine behaviour changes, and edited engine rules name no element.

## Verification

**Executor:** the implementation owner, for every check and for the handoff full round.

**Environment:** the pinned toolchain on `PATH` (run log E1). Logs go under `~/.local/share/tim-toolchain/logs/slice-doc-engine-status/`. The round log has `HEAD` at the top and `HEAD-AFTER` at the bottom.

**Commands:**
- The capture builds and the full round (`docs/delivery-workflow.md:38-53`). Expected: AC3.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .`, run before the round. Expected: PASS, with both canaries.
- The registration check filter (`docs/delivery-workflow.md#new-package-registration-check`). Expected: 821/0/0.
- `ArchitectureDocsConsistencyTests` (filtered run). Expected: pass.
- The M4 checks. Phrase classes, case-insensitive PCRE, one `-e` each:
  - `zero[- ]copy`, `triple[- ]buffer`;
  - `(reads?|samples?|sampled|sampling|consumes?|from)\b[^.\n]{0,60}\b(pose|shared(ArrayBuffer)?) ring`;
  - `instanced (draw|batch)`, `drawElementsInstanced|drawIndexed|MultiMesh`, `GPU instance buffer`;
  - `Structure-of-Arrays`, `\bSoA\b`, `Quad-BVH`, `dual[- ]tree`;
  - `sleeping (island|state)`, `\bislands? (sleep|graph|index)\b`, `\b(DSU|union-find|disjoint set)\b`, `work-steal`;
  - `\bKinematic\)`, `kinematic (bod|motion|kind)`, `dynamic (or|and) kinematic`;
  - `static[- ]only`, `static hollow\b`, `SubShapeID`, `distance link`.

  - Further low-noise classes: `\binstanced\b`, `put to sleep`, `fast inverse`, `Atomics\.waitAsync`, `(pose|shared) ring[^\n]{0,120}renderer|renderer[^\n]{0,120}(pose|shared) ring`.

  Run `rg -n -i -P` with these classes over `*.md`, `*.cs` and `*.ts`. Exclude these full paths:
  - `_bmad-output/implementation-artifacts/**`
  - `docs/planning/elements/readiness/audits/**`
  - `docs/work-orders/**`
  - `docs/coverage/history/**`
  - `docs/planning/delivered-slices.md`
  - `**/*retro*`
  - `AGENTS.md`
  - `_bmad/**`
  - `docs/planning/requirements.md`
  - `_bmad-output/planning-artifacts/prd.md`
  - `tools/e2e/**`

  For `git grep`, use `:(exclude)` pathspecs for the same paths. Classify every hit in the run log as one of: cited present, target, rule, negation, historical (epics story text before `:4421`, or a fact pinned to a named commit such as CAT-023's legacy-harvest table), or lane 1 line (exactly `DOC:145`, `docs/shared-clock-cadence.md:60` and `epics.md:4466`, left unedited until merge and re-checked then). The control is `git grep -n -i -P` with the same classes at `37a259c9`; record its hit count and which owned-scope lines it catches. The search is a net that catches regressions. The inventory is the owned-scope list plus every search hit that needs an edit, each checked line by line (M5).
- The anchor check: every explicit id, and every heading whose slug is linked (E2.2 E, E4 F7), is present with unchanged text. Control: the baseline list.
- `git add -N` on new files, then `git diff --check 37a259c9`. Expected: clean.

### Acceptance-to-test table

No tolerance applies; these are text and link checks.

| Rows | Check | Control and boundary | Execution |
|---|---|---|---|
| M1, AC1 | Run-log list of every contradicted DOC and ISO sentence with its disposition, then the reviewers' citation audit (Verification Gap, Element-Free auditor) | Control: baseline DOC fails (E2.1, E4) | E10 list; E15 to E21 pass-1 to pass-7 repairs, with the ruling D2 re-check (E18 to E21 list the code opened for each rewritten sentence); reviewers' re-audit at step-04 |
| M2 | ISO §2 and §4 read against the code | Control: baseline `:147`, §2.1 and §2.4 | E10 (ISO dispositions); E15 to E17 (§4.4 now 27 rows: WebGPU, compliant reservation, membrane samples, compliant friction, acoustic, MNA and gas network added); E19 to E21 (§4.4 islands row `:202`, gas row `:217`, force-regions row `:219` and §2.1 `:102` re-edited) |
| M3, M4, AC2 | The M4 `rg` with every hit classified | Control: the baseline `git grep` lists the E2.2 D lines | E11: baseline 220 lines in 56 files, each candidate hit classified; E15: repaired candidate 218 in 52; E18: candidate 219 in 52, each hit classified, `architecture.md:66` target; E19: candidate 219 in 52, same classification; E20: candidate 219 in 52, same classification; E21: final candidate 219 in 52, same classification |
| M5 | Per-line disposition list in the run log | Boundary: target or rule lines left unchanged are named | E10, E15 to E21: 94 lines added and 94 removed outside DOC and ISO, unchanged target or rule lines named |
| M6 | `ArchitectureDocsConsistencyTests`, plus the anchor and heading check | Control: the baseline anchor list | E15 to E21: `ArchitectureDocsConsistencyTests` 4/0/0; executed fragment check (E21: 1187 links, 0 broken), with a broken-anchor control; ids identical to the baseline |
| M7, AC4 | Each listed sentence quoted in the run log, plus a search for "only repairs and tooling" or "before the engine programme" over current planning documents | Control: no 24q at the baseline | E10 places; E12 search: every current list carries 24q |
| AC3 | Full round, registration check, maintainability gate | Base 7407/0/0 and 821 | E12: 7407/0/0, `Skipped: 0` (`full-round-2.log`), registration 821/0/0 and gate PASS before the pass-1 repairs; E15 to E21: no compile input changed, `Coverage.Tests` 132/0/0; E23 on the final candidate: capture builds recaptured (`binlogs-e23/`, 0 warnings), gate PASS with both canaries, registration 821/0/0, full round 7407/0/0, `Skipped: 0` (`full-round-e23.log`, HEAD and HEAD-AFTER `37a259c9`), `git diff --check` clean |

## Auto Run Result

**Summary.** DOC-ENGINE-STATUS (engine programme order 1, documents only, lane 4, owner decision 24q).
- `docs/gpu-f32-physics.md` and the isolation authority now state every engine feature on the story's topic list in one of two ways: as running code, with a `path:line` citation at `37a259c9`, or as a target with its owning story. ISO Current status §4.4 has 27 rows.
- The present-tense claims of absent features in nine other current documents are restated the same way.
- The two pose-ring comments no longer say zero-copy or triple-buffered.
- 24q and the sequence wording are recorded in every place M7 lists.

**Files.**
- DOC and ISO.
- `engine/gpu/WorkshopPoseRing.cs`: comments only.
- The other current documents: DESIGN.md, `engine-contracts.md`, `general-engine-design.md`, `simulation-presentation-bridge.md`, `shared-clock-cadence.md`, `browser-physics-performance.md`, `invest/current-consumers.md` and `architecture.md`.
- Element documents: EL-065, EL-069, EL-156 and EL-158, plus `readiness/batch-i.md`.
- Planning records:
  - `owner-decisions-2026-11-22.md` (24q);
  - the sequence lines in `delivery-batches.md`, `epics.md`, `engine-stories-index.md`, `vertical-delivery.md`, `named-elements.md`, `completion-ledger.md` and `TODO.md`;
  - the ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-COMPLIANT-FRICTION lines in `epics.md`;
  - PD-23, `sprint-status.yaml`, `deferred-work.md`;
  - this contract and its run log.

**Review.** Eight passes, recorded in the Review Triage Log and run log E22. Every confirmed finding is repaired except nine deferrals, each with an owner and listed in frontmatter `deferred` and `deferred-work.md`. Eight are deferred under Re-review scope. VG-1, the automated anchor test, was deferred at pass 1 under the intent contract's Never clause and awaits the coordinator's ruling. No forward-only violation remains, and Verification Gap reports no gap. The Element-Free verdict: Scoped Pass for this documents slice.

**Follow-up review recommendation:** false.

**Verification (E23, final candidate):**
- maintainability gate PASS, with both canaries;
- registration check 821/0/0;
- full round 7407/0/0, `Skipped: 0`;
- `ArchitectureDocsConsistencyTests` 4/0/0;
- anchor check: 1,187 links, 0 broken;
- `git diff --check` clean.

**Residual risks:**
- The merge with `origin/main` must apply the lane 1 re-checks (run log E22).
- Remote build, artifact, deployment and browser qualification are unverified.
