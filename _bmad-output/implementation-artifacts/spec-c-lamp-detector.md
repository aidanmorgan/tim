---
title: "C-Lamp+Detector — Signal Lamp and Ball Detector generic instances (9A.36 + 9A.37 + 9R.9 + 9R.16)"
status: review
baseline: 54ffd9f072ffb34acaeeab02f152dabc43c8d71f
---

# Outcome and authority

Signal Lamp (CAT-035) and Ball Detector (CAT-002) become zero-parameter `WorkshopGenericInstance`s. Each package declares one signal capability:
- the lamp declares an **indicator** input, which selects `StickyFirstInput` or `LevelIndicator`;
- the detector declares a **passage aperture**, which selects `PassageSource` and its `PassageSensorDeclaration`.

`WorkshopLamp`, `WorkshopBallDetector` and every arm that names them leave the engine. Roadmap position 36, after 9A.39 (34) and 10.2 (35).

**Authority:**
- epics C-Lamp+Detector, 9A.36, 9A.37, 9R.9 and 9R.16;
- the 9R.9 reading: the detector's committed passage count goes 1→2 while a first-input lamp stays at 1. Re-pulse belongs to 9A.23;
- CAT-035 and CAT-002, §2 and §5;
- 9.4c review N2: `LatchSupplyFixtureTests.Machine` changes its builder only;
- the C-Balls and C-Gate precedents.

Native real-simulation C# proof is PRIMARY. Browser proof is pending (21 Oct policy).

**Re-derived on 54ffd9f0 (after 9A.39 merged; this replaces the 8bfddc91 draft):**
- Both records live only in `engine/gpu/WorkshopActivationParts.cs` (+ `.uid`), and neither encodes a typed wire field.
- The scenes bake 1 collider for the lamp and 17 for the detector, so the generic `_` decode arm (`Colliders.Count > 0`) admits both: proved by the record-path saves loading as generic (evidence below).
- Arms naming them: `WorkshopActivationCompiler.Compile` (2), `WorkshopWireFlatBuffers` encode `case WorkshopLamp` (1) and `MapInstance` (2), `WorkshopInstances.Validate` whitelist (1), `WorkshopInput.Lamp/BallDetector` (2), `MachineWorld.CaptureInstance` (2), and the two lamp-named rejections (`WorkshopConnections.Validate` and the enum). None in `ui/`, `WorkbenchCapacity`, warm-up or the goal evaluator; the save pre-validation has no lamp/detector arm (they already took its generic default arm). The presentation bindings are already declared.
- The lamp is in lessons: 44 `"lamp"` rows in `content/puzzles.json`; the playable `domino_effect` and `delayed_signal` carry it, with an `ActivatedAfter(lamp)` goal. The detector is in none.
- Known rows: 27 (Lamp 17, Detector 10) of 234.
- Tests: 22 files called the two factories (63 call sites); 6 more used the record types or the two rejection names.

# Finite design decisions

**D1 Declared indicator.** `SignalIndicatorDescriptor(PortSocket Input)` and `IIndicatorElement : ISignalElement`. The shared helper `WorkshopActivationCompiler.IndicatorKind(owner, indicator, construction)` (called by the production compile) emits `LevelIndicator` when an Activation link into (owner, `Input`) comes from a Level-kind output, otherwise `StickyFirstInput`. The lamp has only `ActivationIn`, so its nodes are identical. The indicator is explicit, not derived from a Level port. No hold-policy enum; 9A.23 owns re-pulse.

**D2 Declared passage aperture.** `SignalPassageDescriptor(float ApertureRadius, float RearmClearance)` in f32 metres, with `IPassageSensorElement : ISignalElement`; the package declares 0.65f and 0.02f. `WorkshopActivationCompiler.PassageSensorFor(owner, aperture)` narrows to the binary16 lanes: `(Half)0.65f`/`(Half)0.02f` are bit-equal to the former `(Half)0.65`/`(Half)0.02` (0x3933/0x251f, pinned in `PackageRuntimeParityTests`). The plane, local +X and owner frame stay the tracker law.

**D3 Schema and validator.** `SignalPayload` gains `Indicator?` and `Passage?` (round trip in `BundleRoundTripTests`). `BundleError`, never a throw, for: more than one node-selecting capability (Contact, Timer, Indicator, Passage); an undefined Indicator `Input` (UndefinedEnum) or one that is not a declared Signal input admitting Level (OutOfRange); a non-finite (NonFinite) or out-of-range Passage value (radius 0.01–10, clearance 0.001–10); a Passage element without a Pulse `ActivationOut` or a static frame.

**D4 No authorable parameters.** `Parameters: []` stays for both; the generic editor shows no panel.

**D5 Rejection renames.** `LevelDrivesLampOnly` → `LevelDrivesLevelInputOnly`, `LampFollowsOneLevel` → `LevelInputFollowsOneLevel`. Ordinals (7, 8) and message text unchanged; tests assert the text.

**D6 Wire and save.** No schema change (`schemas/workshop_wire.fbs` and the generated files untouched). Pre-conversion saves encoded at 54ffd9f0 by the base encoder decode as generic instances at both boundaries and replay identically: the layout is identical, so this is compatibility, not a shim. A lamp or detector record carrying a typed field or a parameter is refused `OutOfRange` at both boundaries. Base behaviour (recorded): the save boundary already refused both (generic default arm); the command boundary *admitted* a typed field on the record path. That command-boundary acceptance is the behaviour change.

**D7 Browser.** In-slice: the `cat-002` any-target `lights()` predicate is replaced by a lamp-target count (`readAnimationSamplesForTarget`, dark-to-lit transitions per property) that waits for exactly one *more* activation than before each Run, because the target's sample history survives Reset (anim-1b, cat-023b); `LAMP_TARGET = '4'` follows from the Workshop allocator starting at 1 (`ui/Workshop.cs` `_nextId`) and the node + 2 convention. Strict tsc only, Chrome pending. `cat-035.test.ts` is authored post-catalogue.

# Ordered tasks

- [x] **Suite-first native baseline.** One Debug build of 54ffd9f0 plus the test-only diff; `LampDetectorGuardTests` (7) added on the record path through the `PlacedParts.Lamp/Detector` adapter; guard suites (36 classes, `baseline/guards-filter.txt`) 848/848; byte-identical after conversion (sha256 1859953…cb3f at both).
  - 9R.9 one ball, one run; glancing and reverse; 9R.16 switch/latch/plate/unwired/refusal; golden per-tick traces and the 12×3 lesson probe (out-of-tree probe).
- [x] **Capability (D1–D3).** Contracts, payload, validator, round trip, both packages, parity pin including the Half bits.
- [x] **Conversion.** Compile arms read `Signal?.Indicator`/`Signal?.Passage` gated on `WorkshopGenericInstance`, in branch order; `CaptureInstance`/`MapInstance` use the generic arms; encoder case and whitelist entry dropped; D5 renames; tests moved to the adapter (`LatchSupplyFixtureTests.Machine` builder only, assertions byte-unchanged). The three `OfType<WorkshopGenericInstance>().Single` selections in tests are all filtered by part or payload (no change needed); two `Find<WorkshopLamp>` became part-id selections.
- [x] **Deletions** (0 hits, base control in brackets): `rg -w` records/renames 0 [50]; `WorkshopInput.(Lamp|BallDetector)(` 0 [63]; `"lamp"|"ball_detector"` in engine/gpu and MachineWorld 0 [4]; `(Half)0.65|0.02` in ActivationNetwork 0 [1]; `WorkshopActivationParts.cs*` 0 [2]; cat-002 predicate 0 [1].
- [x] **Known rows shrink only.** 234 → 207: the 27 Lamp/Detector rows removed, none added or reassigned; the 4 FinalIsolation `PartId` rows and `Owner.Lamp`/`Owner.Detector` stay.
- [x] **Latest-design alignment.** CAT-035 §2/§3, CAT-002 §1/§2/§3, the `ActivationNodeKind` comment, the isolation-doc A3/A5/A6 row, and the f32 migration-status row (the passage aperture lanes). Active-docs sweep: no unpinned mention of the deleted symbols remains; requirements and INVEST rows stay "Incomplete" (browser pending), consistent with the CAT status text.
- [x] **PRIMARY native proof** (see Current state).
- [x] **Lesson identity.** 12 × 3 world bytes, committed state and activations after 480 ticks, and the 12 × 3 placed solutions after 1200 ticks: identical; `lessons-484ec38.json` and `compiled-geometry-9a16.json` untouched.
- [x] **Checks.** Build 0 warnings; full test; CI tools step; Release worker; Release web publish (AppBundle moved out); strict tsc and snapshot unit; `anvil_check`; `git diff --check`.
- [ ] **SnapshotApproval → publication.** Not done in this slice by coordinator instruction (no commit): independent review first. Then mark `9a-c-lamp-detector-conversion`, `9a-36` and `9a-37` done; `9r-9` and `9r-16` stay backlog (Chrome ACs); append the 6.8 and 6.5e (9R.16) addenda, native only; terminal scoped Pass.

# Pending browser cases (post-catalogue)

- `cat-002`: run the rewritten lamp-target count (`LAMP_TARGET = '4'`); the in-run second crossing after clearance (replacing the Reset at the former `:171`); position proof for reverse and glancing.
- Author `cat-035`.
- Re-run `cat-020`, `cat-023b`, `anim-1a`, `anim-1b`, `anim-1c`, `cat-037` and `cat-052-lamp`, plus `cat-022` once 9.5a authors it.

# Code Map

- **Change:** `CuriousContraptions.Abstractions.Signal/SignalContracts.cs`; `CuriousContraptions.Bundle/{BundleSchema,BundleValidator}.cs`; `elements/{SignalLamp,BallDetector}/*ElementDeclaration.cs`; `engine/gpu/{ActivationNetwork,ElementPortCatalogue,WorkshopConstruction,WorkshopInstances,WorkshopWireFlatBuffers,WorkshopConnections,WorkshopEditRejectedException}.cs`; `engine/MachineWorld.Gpu.cs`; `tools/e2e/cat-002.test.ts`; the Known fixture; tests; CAT-035, CAT-002, the isolation doc, `TODO.md`, `sprint-status.yaml`.
- **New tests:** `CuriousContraptions.tests/LampDetectorGuardTests.cs`, `DeclaredIndicatorPassageTests.cs`; cases in `PalettePlacementTests`, `BundleValidatorTests`, `BundleRoundTripTests`, `PackageRuntimeParityTests`.
- **Delete:** `engine/gpu/WorkshopActivationParts.cs` and its `.uid`.
- **Unchanged:** `PartId.cs` and the E2eRunner `PartId` maps; `workshop_wire.fbs`; `ActivationNodeKind` values and the tracker law; the Half lanes; presentation bindings; the lamp `ElectricalNodes`; `content/puzzles.json`.

# Current state

*Implementation owner, 23 Oct 2026. Uncommitted candidate in worktree `story-c-lamp-detector` on base `54ffd9f0`. Awaiting independent review. Evidence: `evidence-c-lamp-detector/`.*

**What changed**
- Contracts `SignalIndicatorDescriptor`/`IIndicatorElement` and `SignalPassageDescriptor`/`IPassageSensorElement`; `SignalPayload.Indicator`/`Passage`; validator rules (D3). The lamp declares `Indicator => new(PortSocket.ActivationIn)`; the detector `Passage => new(.65f, .02f)`.
- Engine: `IndicatorKind` and `PassageSensorFor` (one law each, called by `WorkshopActivationCompiler.Compile`); `ElementPortCatalogue.Socket(PortSocket)` is now the one declared-to-engine socket mapping (also used by `Admit`). Deleted: both records and their file, both factories, both `CaptureInstance` arms, both `MapInstance` arms, the encoder case, the whitelist entry. D5 renames.
- `cat-002.test.ts`: lamp-target activation count (type-checked only).

**Assumptions, deviations and decisions (reviewer to confirm)**
1. **Test adapter.** Tests build the parts through `PlacedParts.Lamp/Detector` (same signatures as the deleted factories), not `WorkshopInput.Generic` directly. The migration was made on the record path before the baseline, so the guard file and every migrated suite are byte-identical across the cutover; only the two adapter bodies changed (`probe/PlacedParts-record-path.cs.txt`). Record-type sites (`new WorkshopLamp`, `Find<WorkshopLamp>`, `IsType<WorkshopBallDetector>`) were rewritten to adapter/part-id/`IWorkshopDeclaredColliders` forms on the record path too.
2. **9R.9 "distinct committed occurrences".** The detector's own committed latch holds its *first* occurrence (the `PassageSource` node latches), so it cannot show 1→2. Each committed passage is therefore read from a wired tally counter's committed memory (`Count` 0→1→2 and `Last`) and cross-checked by replaying the production `PassageSensorTracker` over the committed bodies and motion (`PassedCount` 2, fired at the same two ticks). No production read surface was added. The detector latch keeps (Passage, first ordinal); the first-input lamp keeps (detector node, first ordinal); a counter(2) lamp lights exactly at the second passage.
3. **Drive (one ball).** Detector with local +X down, plane 0.62 m above the bench; a Basketball dropped 3 m above its resting height bounces off the bench (restitution 1 × 0.55): pass 1 at tick 90, reverse up to 0.745 m (rearm line 0.52 m: 0.02 + 0.34 above the plane), pass 2 at tick 187. Reverse-only (+X up) and glancing (1.1 m off axis) benches cross the plane with positions recorded on both sides and fire nothing.
4. **Synthetic declarations.** `DeclaredPhysics.Install` refuses a second bundle per process, so the synthetic package is modelled through the shared helpers: the lamp's level wiring read through an indicator on `SetIn` (a second level-admitting input) gives `StickyFirstInput`; a (0.3 m, 0.5 m) aperture compiles its own lanes and the production tracker obeys them.
5. **Validator.** An undefined indicator input is reported once (UndefinedEnum), not also as OutOfRange.
6. **Lesson golden.** `ContentPreservesPrechangeLessonGeometryInventoryAndProfiles` normalises the historical lamp row like the ball/spring rows: its `Cosmetic` must equal the generic instance's, then `Parameters: []`. The fixture is untouched.
7. **Process disclosures.** `anvil_validate_write`/`anvil_apply_patch` ran before every code and new-file write except: the first write of `LampDetectorGuardTests.cs`, the `BundleRoundTripTests` edit, removal-only edits (27 Known rows; the wire, whitelist, factory and `CaptureInstance` deletions), the enum-rename `replace_all` edits and the docs; the 22-file adapter migration (`replace_all`) was validated as one combined added-lines diff. The same-simulation Reset case was also added without a prior validate. `anvil_check` afterwards: 0 findings on all 50 changed files. The first-freeze receipts were moved into `candidate/superseded-first-freeze/` with `mv`. Shell file operations: `git rm` of the two deleted files; `cp` of the probe into `probe/` (sha256 b636a615… equal to the compiled source); `mv` of the published `AppBundle` to the scratchpad; command receipts via `tee`. The probe compiled from the scratchpad through a temporary `Compile Include` (added and removed with Edit; absent from the candidate). Mutant M5's first form failed to build (CS1718) and ran no tests; its log was overwritten by the compiling rerun.

**Old saves** (`baseline/capture/old-saves/`, sha256 in `old-saves.sha256`; outcomes `baseline|candidate/capture/old-save-outcomes.txt`)
- Record-path lamp, detector, locked rotated detector, and wired detector/lamp/counter, switch/lamp and latch/lamp benches: load as the same generic instances at the save and command boundaries; the two payload benches replay 480 ticks hash-identical (8ABD7238…, 10187902…). Three are embedded and sha-pinned in `DeclaredIndicatorPassageTests`.
- Forged typed field (7 kinds) or parameter on a lamp/detector record: save boundary OutOfRange at base and candidate; command boundary admitted on base, OutOfRange on the candidate.

**Evidence** (`_bmad-output/implementation-artifacts/evidence-c-lamp-detector/`)

| Check | Receipt | Result |
|---|---|---|
| Base tree identity | `baseline/baseline-head.txt` | 54ffd9f0 + test-only diff |
| Base Debug build (`--no-incremental`) | `baseline/build.log` | 0 warnings |
| Base full suite | `baseline/full-test.log` | 17/163/77/189/2448 = 2894 |
| Base guard suites (36 classes) | `baseline/guards.log`, `guards-filter.txt` | 848/848 |
| Probe (traces ×10, old saves, lessons 12×3, placed solutions 12×3) | `baseline/capture/`, `candidate/capture/`, `candidate/capture-compare.txt`, `probe/` | all traces, lesson world and placed solutions byte-identical; old-save outcomes differ only as listed above |
| Deletion greps with base control | `candidate/deletion-greps.txt` (`.sh.txt`) | 0 each |
| Known list | `candidate/known-rows.txt` | 234 → 207 |
| Frozen Debug build (`--no-incremental`; tracked diff sha256 cbca98eb…) | `candidate/freeze-head.txt`, `freeze-build.txt` | 0 warnings, 0 errors |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | `candidate/freeze-test.txt` | WorkerAotGuard 17, BundleElements 164, Coverage 77, E2eRunner 189, CuriousContraptions.tests 2464 = 2911/2911 |
| Guard suites on the frozen build | `candidate/guards.txt` | 851/851 (848 baseline + 3 new editor cases) |
| CI "Test C# tools" step under bash | `candidate/ci-tools.txt` | 77/164/17/174 |
| Release worker; Release web publish (first freeze; the second freeze changed only a test-only C# file, `cat-002.test.ts` and docs, none an input of either) | `candidate/worker-release.txt`; `candidate/web-publish.txt` | 0/0; rc 0, static files present, AppBundle moved out |
| First freeze, superseded (before the same-simulation Reset case, the relative cat-002 count and the f32 doc row) | `candidate/superseded-first-freeze/` | 2910/2910, guards 851, CI tools, tsc, diff check all green |
| Strict tsc 6.0.3 and snapshot schema unit | `candidate/ts-checks.txt` | rc 0; 24/24 |
| `git diff --check` | `candidate/diff-check.txt` | clean |
| sprint-status readers (ElementIsolationTests, WorkshopPhysicsLawTests read the yaml at run time) | covered by `candidate/freeze-test.txt` (the status edit precedes the second freeze) | pass |
| Mutations (`mutation/`; control M0 217/217 + 164/164; restored, `restored-check.txt`) | `M1`–`M5` logs | all killed: M1 indicator always sticky (19 tests); M2 radius ignored (1); M3 clearance ignored (1); M4 exclusivity removed (1, bundle); M5 input filter dropped (1) |

**New and changed tests**
- `LampDetectorGuardTests` (7, suite-first, production simulation): 9R.9 one-ball 0→1→2 with tracker cross-check, lamp and counter lamps, exact Reset and Save/Load replay; glancing and reverse controls; 9R.16 switch sticky with unwired control, latch and plate level-follow on/off, both level-wiring refusals at edit and save.
- `DeclaredIndicatorPassageTests` (13): selection by declaration with a Wall control; the declared-input filter; aperture boundary 0.64/0.66 m; synthetic aperture; record-path saves at both boundaries (sha-pinned) and the loaded bench passing twice; exact Reset in one simulation (the passage cursor, pulse, latches and counter memory return to the built state and the second run replays the first tick for tick, both passes included); typed-field/parameter refusals with a bare control; D5 ordinals and text.
- `PalettePlacementTests` (+3): drawer placement as generic with no Configure panel, parameter path refused, rotate and move each one Undo, Running/Paused/locked guards; detector snaps onto a pipe mouth as one placement and one Undo.
- `BundleValidatorTests` (+1), `BundleRoundTripTests` (indicator/passage), `PackageRuntimeParityTests` (declaration ↔ compiled pin, Half bits).
- CAT-002 matrix #1–#4 (4/40/120 m/s, rotated, reverse/outside/stationary, jitter, two balls) stays in `BallDetectorTests` on the converted tracker path; detector → counter(3) → lamp in `CounterTests`.

**Risks and next check.** Browser qualification pending (cases above; the cat-002 lamp target id is inferred). Next: independent adversarial review of this uncommitted candidate, then SnapshotApproval and publication, then the 6.8 and 6.5e addenda (native only).
