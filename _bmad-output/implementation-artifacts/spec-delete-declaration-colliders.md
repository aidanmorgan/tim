---
title: 'Delete the declaration Colliders member and its bundle copy'
type: 'refactor'
created: '2026-11-25'
status: 'done'
baseline_revision: 'c345690731b1654feca945558e66ce57d43d9f1b'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/engine-contracts.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
warnings: []
deferred:
  - summary: >-
      Element-document line citations moved by this slice (the 45 declarations, the changed engine and tool files and the changed test files, including MotorTests.cs:1075 now :1111) still name baseline lines.
    evidence: |-
      Coordinator ruling 3 (run-log E5), on the footing of part B's decision 8: a stale line citation inside docs/planning/elements/ caused only by this slice goes to one carry-forward entry; each element slice refreshes its own cited lines at dispatch (decision 38). Review pass 4 BH4-1 routed there (run-log E18). deferred-work.md, "Carry-forward, from the delete-declaration-colliders slice".
    location: >-
      docs/planning/elements/
    severity: low
---

<intent-contract>

## Intent

**Problem:** Since visible-solid part B (decision 4) a package's colliders come only from its scene, and every non-empty declared collider list is refused at bake with `SceneColliderOwnership`. The declaration still has a `Colliders` member that no admissible value can fill (all 45 packages state `Colliders => []`), the declared slider and hinge moving bodies carry the same dead `Colliders` slot, and scene-body ownership sits on an optional interface whose absence the generator must refuse (a second rule). A retained member with no admissible value is a forward-only and D5 Fail (part B findings EFE7-1, FO8-1, FO8-2).

**Approach:** Delete the declaration `Colliders` member and its copy into the bundle entry; the declared slider and hinge moving bodies keep only their mass, and the bake writes each moving body's scene colliders into one bundle-only list in package body order; `IPuzzleElementDeclaration` extends `ISceneGeometryElement`, so every declaration states its scene bodies at compile time and the generator's refusal of a declaration without them is deleted with its two tests. Baked colliders, the compiled scene and every part's behaviour are unchanged.

## Boundaries & Constraints

**Always:** Forward-only: the deleted members, the data-collider refusal and the no-scene-bodies refusal leave no alias, shim, empty marker or retained slot. A bundle entry's colliders still come only from the scene bake; every package's baked colliders (per body: count, shape kind, dimensions, pose, material) and its compiled behaviour are as before. `ISceneGeometryElement` keeps its name and its one member. A count mismatch between the moving bodies and their baked collider lists, in a bundle read from disk, is refused with the existing `SceneColliderOwnership` code; no new error code. Every declaration, fixture, caller, test and current document whose content claim the deletion makes false changes in this slice. Anvil validation before each write, one per edit. Engine review tier. One full local round with zero failures and `Skipped: 0`.

**Never:** A new collider source, a change to the scene extractor's proxy matching, a change to any scene, a new parameter, port, capability or error code. Renaming or deleting `ISceneGeometryElement`. Hashes or byte-identity comparisons of the bundle. Mutants, browser runs, skipped or excluded tests. Edits to lane 2's files (`schemas/`, `tools/e2e`, `tools/runtime`, wire targets). Refreshing line citations inside `docs/planning/elements/` that only this deletion moved (coordinator ruling 3).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| R1 declaration surface | A package declaration | Has no `Colliders` member and states `SceneBodies` because `IPuzzleElementDeclaration` extends `ISceneGeometryElement` | A declaration that omits `SceneBodies` or states colliders does not compile |
| R2 baked moving bodies | The generated bundle entry of a slider or hinge package | Its declared moving body states mass only; the entry carries one baked collider list per moving body, in package body order (sliders, then hinges), each from that body's scene root | — |
| R3 count refusal | A bundle entry whose moving-body collider lists number fewer or more than its sliders plus hinges | Refused by the validator with `SceneColliderOwnership`; an entry with exactly one list per moving body is admitted | Typed refusal, no partial admission |
| R4 baked colliders unchanged | The generated bundle for all 45 packages, against the baseline's | Per package and body, the decoded colliders are equal (count, shape kind, half extents, radius, offset, rotation, material, participation) | Any difference is a failure |
| R5 deleted refusals | The generator and the bake | No check for a declaration without scene bodies and no check for declared data colliders remains; the ownership count and root refusals stay | — |
| R6 retained behaviour | Existing tests of every package | Pass, changed only where they read a deleted member | — |

</intent-contract>

## Code Map

Delivery shape: one forward-only repair slice, no element identity added or closed. Review tier: **engine** (Abstractions, Bundle, `engine/gpu`, the bundle tool and tests; probe below). Sequenced by coordinator decision 6 (25 November 2026) after part B (`c3456907`) and before part B2; scope from `deferred-work.md:371` and part B run-log E21, E22, E25.

Dispatch record: checkout `.claude/worktrees/slice-delete-declaration-colliders` (branch `slice-delete-declaration-colliders`, lane 1, owner decision 24m), baseline `c345690731b1654feca945558e66ce57d43d9f1b`, policy revision the same; baseline round 7407/0/0, registration 821 (coordinator's figures). Session environment and Auto preflight: run-log E1. Planning measurements: E2, E4. Coordinator rulings 1 to 4 of 25 November 2026: E5. Implementation owner: agent `a2cc07c87fb0e35f9` (fresh `general-purpose`, no inherited history, inherited model; run-log E8).

Coordinator rulings (run-log E5): (1) moving-body split (b), count mismatch as `SceneColliderOwnership`, the data-collider refusal and its tests deleted, the JSON shape change accepted, folding the body-to-collider lookups only if it falls out naturally; (2) `ISceneGeometryElement` kept, `IPuzzleElementDeclaration` extends it, the generator refusal and its two tests deleted, only non-element current documents that describe the optional interface and refusal updated; (3) moved citations refreshed in current documents outside `docs/planning/elements/`, one carry-forward for those inside it, and a false content claim fixed wherever it is; (4) `delivery-batches.md:298` and `TODO.md` edited here, the second publisher keeps both sides.

Planning verdict: Scoped Pass (planning stage, engine tier), on the second check by the Element-Free Engine Auditor after the E7 amendments (run-log E7, E8).

### Owned scope

- Abstractions: `CuriousContraptions.Abstractions/IPuzzleElementDeclaration.cs` (delete `Colliders` :11; extend `ISceneGeometryElement`); `CuriousContraptions.Abstractions/SceneGeometry.cs:6–7` (the summary states the order: base, then sliders, then hinges) and `:10` (`ISceneGeometryElement` no longer extends `IPuzzleElementDeclaration`, which would be a cycle); `CuriousContraptions.Abstractions/MechanicalContractValues.cs:74–77`, `:122–125` (`SliderBodyDescriptor` and `HingeBodyDescriptor` keep `MassFrame` only; summaries say where the colliders are); `CuriousContraptions.Abstractions/BundleServices.cs` (`MechanicalPayload`: the new required bundle-only list, :37–60; the `SceneColliderOwnership` summary :121–124, which then names only what is still refused; `IBundleServices.Write(IEnumerable<IPuzzleElementDeclaration>)` :140 deleted, no caller).
- Bundle: `CuriousContraptions.Bundle/BundleSchema.cs:22` (the entry's base colliders start empty; the bake fills them), `:34–37` (the moving-body list starts empty), `:57–61` (`Write(IEnumerable<IPuzzleElementDeclaration>)` deleted: it serialises unbaked declarations, never an admissible bundle, and only tests call it; they write `Entry` results through `WriteEntries`), `:67–83` (`Read` refuses a null inner moving-body list with `JsonException`, as it already does by hand for `Elements[i]`); `CuriousContraptions.Bundle/BundleServices.cs:9` (the deleted `Write`); `BundleValidator.cs:767–771`, `:809–813` (moving-body collider checks read the new list; the count refusal); `PackageBodyGeometry.cs:14–16` (the one resolver of a body's colliders, bounded by the new list's count), `BundleMaterial.cs:28–30`, `SignalBundleValidation.cs:224–226`, `ClippedExtrusionValidation.cs:31–36` (resolve through it).
- Engine: `engine/gpu/CompiledIdentityMap.cs:268`, `:275`. `engine/gpu/WorkshopPhysicsCompiler.cs` is not changed: its `body` at :169, :210 reads only `MassFrame`, and colliders reach it through `BundleMaterial.Shapes` (run-log E6).
- Bundle tool: `tools/BundleElements/SceneGeometryExtractor.cs:14–17` (the `Bake` summary no longer names a data-collider refusal), `:31–32` (delete the data-collider refusal), `:39–47` (write the baked moving-body lists); `tools/BundleElements/BundleGenerator.cs:24–29` (delete the refusal; use `declaration.SceneBodies`); `tools/BundleElements/PackageValidator.cs:118` (the always-true type test removed; the `port.Body.Value < SceneBodies.Count` bound stays).
- Packages: the 45 `elements/*/*ElementDeclaration.cs` (delete the `Colliders => [];` line); `Colliders: []` in Clutch :142, Conveyor :105, ImpactLever :118, Motor :114, Springboard :80 and the positional `[]` in Fan :40, LinearPusher :33, WoundSpring, PoweredGate, Windmill, ReverseTransmission (the scout's list is authoritative). Their base lists keep `ISceneGeometryElement`: redundant but true, and it keeps the 389 document mentions true (ruling 2).
- Tests: `tools/BundleElements.Tests/Fixtures.cs` (fixtures lose `Colliders`; every fixture implementing the declaration states `SceneBodies`); the 28 `BundleSchema.Entry(` calls in 6 test files (colliders put on the entry); `SceneGeometryExtractorTests` (delete `DeclarationDataCollidersAreRefusedOnEveryBodyWhateverTheScene` and `APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle`; the bake tests read the new list); `PortLocatorTests.AMovingBodyPortOnADeclarationWithoutSceneBodiesFailsTheBuild` (deleted); `BundleRoundTripTests.cs:262`, `:285`; `BundlePortsTests.cs:380–382`; `VisibleSolidColliderTests.cs:298`; the 18 test files that read `MovingBody` colliders (scout list); the new count-refusal test.
- Documents whose content claim becomes false (run-log E6, ruling 3): `docs/general-engine-design.md:67–68`; `docs/architecture/element-package-isolation.md:19` (and `:153` if it reads as declared colliders); `DESIGN.md:219`; `docs/engine-contracts.md:58`, `:66`; `_bmad-output/planning-artifacts/epics.md:535`, `:2825`, `:3238–3245`; `spec-engine-art-solidity.md:76`; in `docs/planning/elements/`: CAT-001:23, CAT-014:23, CAT-030:26, CAT-048:26, CAT-049:27, CAT-050:27, CAT-054:23, CAT-066:23, EL-053:96, EL-062:116, EL-085:88, EL-094:91, EL-111:95, EL-156:89, EL-157:64, EL-158:74, EL-163:93, TH-10:24. Then moved citations in current documents outside `docs/planning/elements/` (E6 list; refreshed after the code is final; dated audits and run logs are history). `_bmad-output/implementation-artifacts/deferred-work.md` (close the entry at :371; add the citation carry-forward naming the moved files). Records, coordinator-owned (the part B receipt and status lines are done, run-log E5; the implementer records its own work only in this contract's Implementation Notes, Verification and acceptance-to-test table and in new run-log entries): `TODO.md`, `docs/planning/invest/delivery-batches.md`, `docs/planning/elements/readiness/completion-ledger.md`, part B's run log (E32), this contract and its run log.

`elements/elements.bundle.json` is build output (`.gitignore:17`); the implementer keeps a baseline copy under `.tools/slice-delete-declaration-colliders-evidence/` before the first edit, for R4.

### Capability probe

Planning scout, run-log E6. Four present, two present-untested and one missing, each owned here as a bounded addition; no solver, worker ABI, wire or shared-presentation capability changes, so no engine slice precedes this one.

- **Present.** Scene bake of base and moving-body colliders: `SceneGeometryExtractor.Bake`, `BakeBodies` (`tools/BundleElements/SceneGeometryExtractor.cs:36`, `:61–71`). Tests: `SceneGeometryExtractorTests.MovingSubtreeBelongsOnlyToItsDeclaredBody`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt`; hinges through `PackageRuntimeParityTests.EveryHingePackageDeclaresTheCompiledRevoluteJointAndCarriesItsMovingBody`.
- **Present.** A moving body without a collider is refused (`BundleValidator.cs:770–771`, `:812–813`). Tests: `BundleValidatorTests.ASliderWhoseMovingBodyHasNoColliderIsRefused`; the hinge case in `HingeCapabilityValidatesLimits…` (:944–950).
- **Present.** The extractor refuses an ownership list of the wrong count or roots as `SceneColliderOwnership` (`SceneGeometryExtractor.cs:33–34`, `:157`). Tests: `AnOwnershipThatDoesNotNameTheBaseAndEachMovingBodyOnceIsRefused`, `MovingSubtreeBelongsOnlyToItsDeclaredBody`.
- **Present.** The identity map and material shapes take moving-body colliders from the bundle (`CompiledIdentityMap.cs:268`, `:275`; `BundleMaterial.cs:28`, `:30`). Tests: `PackageRuntimeParityTests.EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody`, `EveryHingePackage…`.
- **Present, untested; owned here.** Per-collider validation of a moving body's colliders (`BundleValidator.cs:769`, `:811`): the slice adds a refused malformed moving-body collider.
- **Present, untested; owned here.** The bundle JSON round trip of a nested list (none in the schema today; `BundleJsonContext` in `BundleSchema.cs` discovers nested types): the slice adds a write-and-read test with a non-empty list.
- **Missing; owned here (a validator case, not an engine capability).** The validator's count refusal of the moving-body lists (R3).

Direct consumers (E6): the four fixtures without `SceneBodies`; every test entry built from a declaration and validated without a bake; tests that resize or reorder sliders or hinges on a baked payload (each keeps the list in step at its call site; no helper; the implementer confirms the candidate set when naming consumers). Open decisions: none (rulings E5; scout defaults decided in E6).

Carry-forward entries touched (run-log E7): `deferred-work.md:369` (per-branch extractor tests and node names; owner "the next slice that changes the scene extractor") stays with `ENGINE-ART-SOLIDITY`: this slice only deletes one refusal branch and changes no surviving branch or message. The KHR entry (`ENGINE-CORE-2d` replanning) stays: the `epics.md:3238–3245` edits here remove only the data-collider claims and keep the KHR wording at :3237. `epics.md:3074` is a done story's acceptance (history, not edited).

### Design answers

1. **Behaviour delivered.** None new: the same baked colliders and compiled scenes; the declaration loses an inadmissible member and the generator a refusal no declaration can reach (R1–R6).
2. **Reusable.** The existing scene bake, bundle validator and compiler; no new primitive.
3. **Declared.** A declaration states metadata, ports, events, domains, footprint, scene bodies and (mechanical) mass frames; colliders are never declared. The baked moving-body lists are bundle data, indexed by package body (1 to n: sliders, then hinges).
4. **Execution.** Declaration → `BundleSchema.Entry` (no colliders) → `SceneGeometryExtractor.Bake` (base and per-moving-body colliders from the scene) → `BundleValidator` (count refusal) → bundle JSON → compiler and identity map read the same lists. Every reader of a body's colliders resolves through `PackageBodyGeometry.Colliders`, bounded by the new list's count, so a short list in a bundle read from disk yields the existing typed refusals plus `SceneColliderOwnership`, never an exception (planning audit finding 1, run-log E7).
5. **Ownership.** The bake is the only writer of every collider list; Run, Reset and save/load are unchanged (no state added).
6. **Generality.** R2 and R3 run over every slider and hinge package and a fixture with both a slider and a hinge; R4 over all 45 packages; the count refusal with fewer, more and the exact count.
7. **Deleted.** `IPuzzleElementDeclaration.Colliders`; `SliderBodyDescriptor.Colliders`, `HingeBodyDescriptor.Colliders`; the `BundleSchema.cs:22` copy; `IBundleServices.Write` and `BundleSchema.Write` over declarations; the data-collider refusal (`SceneGeometryExtractor.cs:31–32`); the generator refusal (`BundleGenerator.cs:24–29`); the always-true type test at `PackageValidator.cs:118`; the duplicate resolvers in `BundleMaterial` and `SignalBundleValidation`; the three tests named above.
8. **Uncertain.** None open; the four rulings are E5.

## Tasks & Acceptance

**Execution:**

0. Copy the baseline bundle (`elements/elements.bundle.json` after a baseline build) to the evidence folder.
1. `CuriousContraptions.Abstractions/IPuzzleElementDeclaration.cs`, `SceneGeometry.cs` -- delete `Colliders`; `IPuzzleElementDeclaration : ISceneGeometryElement`; `ISceneGeometryElement` extends nothing -- R1.
2. `CuriousContraptions.Abstractions/MechanicalContractValues.cs` -- the two moving-body records keep `MassFrame` only; their summaries say the colliders are baked into the bundle's moving-body lists -- R2. Where a rewritten summary can keep the record's line count, prefer it (fewer moved citations), never padding.
3. `CuriousContraptions.Abstractions/BundleServices.cs` -- a required `MechanicalPayload` property holding one collider list per moving body in package body order, with a summary; the `SceneColliderOwnership` summary lists what is still refused (an ownership list of the wrong count or roots, overlapping roots, a moving-body collider list count that differs from the moving bodies) -- R2, R3.
4. `CuriousContraptions.Bundle/*` (files in Owned scope) -- `Entry` writes no declared colliders; `PackageBodyGeometry.Colliders` is the one resolver, bounded by the new list's count, and every reader resolves through it, each keeping its current refusal (`BundleMaterial.Shapes` keeps its `Footprint.Bodies > 0` guard and its `ArgumentException` for body 0 of a part with no bodies; `SignalBundleValidation` keeps its `Body()` check; a test pins the `Shapes` case if none does); the validator refuses a count mismatch as `SceneColliderOwnership` and keeps `BodyWithoutCollider` and the per-collider checks per moving body; `Read` refuses a null inner list; the declaration `Write` is deleted -- R2, R3.
5. `engine/gpu/CompiledIdentityMap.cs` -- read moving-body collider counts from the new list -- R4, R6.
6. `tools/BundleElements/*` -- delete both refusals; the bake writes the moving-body lists; the generator passes `declaration.SceneBodies`; drop the always-true type test -- R2, R5.
7. The 45 declarations and the 11 slider and hinge packages -- delete `Colliders => [];` and the moving-body `Colliders` argument -- R1.
8. Tests -- update fixtures and readers; delete the three tests, recording each in Implementation Notes ("behaviour removed" for the two data-collider and no-scene-bodies refusals, "covered by `APortNamingNoDeclaredSceneBodyFailsTheBuild`" for the port test); move the literal paths at `BundleValidatorTests.cs:820`, `:950` to `Mechanical.MovingBodyColliders[i]`; add the count-refusal test, the malformed moving-body collider case and the nested-list round trip -- R2, R3, R5, R6.
9. Documents -- the scout's list of non-element current documents; then refresh moved citations outside `docs/planning/elements/`; add the carry-forward entry and close :371 in `deferred-work.md` -- ruling 2, 3.
10. Handoff checks in protocol order; R4 evidence: a decoded per-package, per-body comparison of the baseline and candidate bundles (`jq` or a scratch reader, field by field, never a hash or byte diff), output in the evidence folder and quoted in the run log.

**Acceptance Criteria:**

- AC1: Given the solution, when it builds with warnings as errors, then no declaration, fixture or production type has a `Colliders` member on a declaration or a declared moving body, and every declaration states `SceneBodies` through `IPuzzleElementDeclaration` (R1; compile-time).
- AC2: Given `rg -n` over `*.cs` (excluding `obj/`), when searching for `declaration\.Colliders`, `MovingBody\.Colliders`, `is not ISceneGeometryElement`, `is ISceneGeometryElement`, `IsAssignableFrom<ISceneGeometryElement>`, `hasData`, `states data colliders`, `names no scene bodies`, `Colliders => \[\]` and `Write\(IEnumerable<IPuzzleElementDeclaration>`, then there are zero hits (E5 deleted-symbol check).
- AC3: Given the full local round on the handoff candidate, then zero failures and `Skipped: 0`, with total = 7407 − 4 deleted test results + the added results, each named in the acceptance-to-test table.
- AC4: Given the registration check, then it passes (test files change), with its total stated.
- AC5: Given the maintainability gate, then PASS with both canary lines and the coupling summary.

## Spec Change Log

- 25 November 2026, planning: intent and constraints amended from the blocked draft to the coordinator's rulings 1 to 4 (run-log E5): `ISceneGeometryElement` is kept and extended by `IPuzzleElementDeclaration` instead of deleted; no new error code. Not a review loopback.
- 25 November 2026, planning: the planning audit's six findings applied (run-log E7): one bounded collider resolver, `Read` refuses a null inner list, the declaration `Write` deleted, the carry-forward entries discharged, two stale summaries and exact AC2 patterns added to scope. Not a review loopback.

## Review Triage Log

### 2026-11-25 — Review pass 1 (engine tier, all six layers; reviewers and identities in run-log E13; repair in E12)
- verdicts: 12 findings in 11 rows (BH1 and ECH2 share a root cause and a row) — high 1, medium 2, low 5, false 3, maybe-false 0; plus 5 descriptive notes without a defect (rows marked `—`)
- findings:
  - `[high]` `[patch]` FO1: `epics.md:3245` keeps the present-tense "placement … prefers the scene set; the pipe always uses its canonical table", a second collider source stated as current (forward-only, so high) — owner: past tense or delete.
  - `[medium]` `[patch]` VG1: no test compiles a part whose slider and hinge bodies have different collider counts, so a hinge-offset slip in `CompiledIdentityMap.AllocateChildren` would pass every fixture (all mixed fixtures are 1/1) — owner: slider 1 collider + hinge 2 colliders compiled, hinge body has both with baked offsets; control with counts swapped.
  - `[low]` `[reject]` VG-other: `CompiledIdentityMap` restates the body order instead of the internal `PackageBodyGeometry` — the resolver is internal to `CuriousContraptions.Bundle` (not reachable from `engine/`), the Forward-Only auditor judged it one rule without a condition, and VG1's test now pins the order; a new public resolver would be added surface, not a direct correction.
  - `[false]` `[reject]` ECH1: unguarded `moving[sliderCount + i]` on an unvalidated entry — production admission validates first (`DeclaredPhysics.Install` `:28`, `PartRegistry.Read`); `DeclaredPhysics.Scoped` is an internal test seam whose malformed input fails loudly, which is correct behaviour; a guard would be a second check.
  - `[medium]` `[patch]` BH1, ECH2 (one root cause): eight declarations (Counter, DelayBox, ImpactSwitch, PressurePlate, Receiver, Springboard, StateLatch, SystemClock) keep the summary of the deleted `Colliders` member, which now documents `SceneBodies` wrongly (`ImpactSwitchElementDeclaration.cs:45` "The base, then the button." over one body) — owner: delete those summaries; also remove the double blank lines the deletions left and the `TubeMouths =[new(` spacing slip in `SceneGeometryExtractorTests.cs`.
  - `[low]` `[patch]` BH2: `ParameterizedMassTests.AMaterialBindingOnTheBaseOfAPartWithoutBodiesIsRefused` changes footprint and colliders together, so it passes without the `Footprint.Bodies > 0` guard — owner: refused case differs from the control only in the footprint (colliders kept) and asserts the `MaterialBindings` refusal.
  - `[low]` `[patch]` IA5: R3's surface is a bundle read from disk; the count refusal is tested only on the in-memory validator — owner: one case through `WriteEntries` → `Read` → `Validate` with a wrong list count refused as `SceneColliderOwnership`.
  - `[low]` `[reject]` IA2a: the two test fixtures dropped the redundant `ISceneGeometryElement` while the 45 declarations keep it — both are correct under ruling 2; no harm named.
  - `[false]` `[reject]` IA2b: the `[]` written by `Entry` and the `MovingBodyColliders` default as a forbidden "empty marker" — they are the pre-bake value of fields only the bake fills (ruling 1); a bundle without the key is refused on read (`BundleRoundTripTests.MovingBodyColliderListsRoundTripInBodyOrderAndANullListIsRefused`).
  - `[false]` `[reject]` IA3: a declaration that still defines a `Colliders` property compiles — it is then a member of the class, not of any declaration interface; nothing reads it, and AC2's search refuses it in the tree.
  - `[low]` `[reject]` IA1: round-trip tests write slider and hinge entries without moving-body lists and do not validate them — they test serialisation of declared fields, not admission; admission of baked entries is covered by the validator tests and the full catalogue load.
  - `—` IA4: body order rests on `SceneBodies` order; checked by `VisibleSolidColliderTests` for real packages (descriptive).
  - `—` IA6: R4 not in a committed test — evidence by design (contract Task 10); the Element-Free auditor reran the decoded comparison: 45 packages equal.
  - `—` IA7: three tests deleted (contract names three) and tests added beyond R6 (planned R2/R3 rows and planning-audit cases) (descriptive).
  - `—` FO, EFE scope and evidence notes: Forward-Only found no code violation; Element-Free Scoped Pass, no findings, evidence verified from the raw logs (7410/0/0, registration 826, gate PASS, R4 equal).
  - `—` evidence limits from each layer (no builds or test runs by reviewers).

### 2026-11-25 — Review pass 2 (repair re-review; Blind Hunter, Edge Case Hunter, Verification Gap, Forward-Only, Element-Free; identities in run-log E14)
- verdicts: 6 findings in 3 rows — high 1, medium 0, low 2, false 0, maybe-false 0. Pass-1 findings VG1, BH1/ECH2, BH2 and IA5 confirmed resolved (Element-Free, Verification Gap); FO1 resolved at `:3245`.
- findings:
  - `[high]` `[patch]` FO2, EFE2-1, BH2-3 (one root cause): `epics.md:3244` (the line above FO1's) still says in the present tense that `CaptureInstance` prefers the scene ring over the canonical table and that a rotation differs "today" in the canonical table and the package — a second collider source stated as current (`MachineWorld.Gpu.cs:108` prefers nothing; no canonical table) — owner: past tense, as `:3245`.
  - `[low]` `[patch]` ECH2-1, BH2-1, EFE2-2 (one root cause): the new `MotorTests` theory was inserted between the mixed-scene comment and `InAMixedSceneEachLampReadsItsOwnDrivenSlotAndAnUndrivenHingeHasItsOwnRow`, so that comment now describes the wrong test — owner: move the theory so each comment sits above its own test.
  - `[low]` `[patch]` BH2-2, VG2-other (one root cause): `AMovingBodyListCountReadFromDiskIsRefused` reads no file; it round-trips bundle text (`WriteEntries` → `Read` → `Validate`, the calls `PartRegistry.Read` makes; the load refusal itself is covered by `PartRegistryTests`) — owner: rename and reword it as a bundle-text round trip, and update the acceptance-to-test table.

### 2026-11-25 — Review pass 3 (repair re-review; Blind Hunter, Verification Gap, Forward-Only, Element-Free; identities in run-log E16; Re-review scope applies)
- verdicts: 2 findings — high 0, medium 0, low 2, false 0, maybe-false 0. Pass-2 findings resolved (Forward-Only, Element-Free, Verification Gap); Forward-Only and Verification Gap: no findings; Element-Free: no violations, final-candidate evidence pending.
- findings:
  - `[low]` `[patch]` BH3-1: the new `MotorTests` theory sits before `InAMixedScene…` (:897), `TheMotorCompilesAContinuousDrivenHinge…` (:990), :1008, :1033, :1044 and `TheRotorArtFollows…` (:1075), so it moves lines that about 20 element-document rows cite as capability evidence — in the repair diff, so repaired here (Re-review scope) — owner: move the theory to the end of the class so no cited `MotorTests` line moves, and correct the carry-forward's file list if `MotorTests.cs` no longer moves.
  - `[low]` `[patch]` BH3-2: `epics.md:3244`'s main clause still reads as open work ("is reconciled with its package before the canonical table is deleted") although the table is deleted — owner: record the outcome as `:3245` does (its colliders are now baked from its scene).

### 2026-11-25 — Review pass 4 (repair re-review; Blind Hunter, Verification Gap, Forward-Only, Element-Free; identities in run-log E18; Re-review scope applies)
- verdicts: 2 findings in 1 row — high 0, medium 0, low 1, false 0, maybe-false 0. Pass-3 findings resolved apart from the row below; Verification Gap and Forward-Only: no findings; Element-Free: Scoped Pass (no D1–D7 or E1–E9 violation; final-candidate evidence verified: capture builds, gate PASS, full round 7413/0/0).
- findings:
  - `[low]` `[defer]` BH4-1, EFE4-1 (one root cause): the moved theory still precedes the file's second class `MotorRotorArtTests`, so `MotorTests.cs:1075` (`TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose`) is now `:1111`, and seven element documents cite `:1075` — routed by coordinator ruling 3 (element-folder citation drift caused by this slice goes to the carry-forward; `MotorTests.cs` is on its list), not a third placement repair (the stagnation trigger applies to the same test's placement); the carry-forward's wording "moved by the deletion" corrected to "moved by this slice (the deletion and its test changes)"; `deferred:` item added.

## Implementation Notes

- Probe held at every cited line; no capability returned to planning; no lane 2 or non-C# consumer (run-log E9).
- Deleted tests: `DeclarationDataCollidersAreRefusedOnEveryBodyWhateverTheScene` and both rows of `APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle`, behaviour removed; `AMovingBodyPortOnADeclarationWithoutSceneBodiesFailsTheBuild`, covered by `APortNamingNoDeclaredSceneBodyFailsTheBuild` (E9).
- Fixtures state no colliders; `FixtureBake` in `Fixtures.cs` stands in for the bake on scene-less fixtures; resize sites keep the lists in step at each call site; the Clutch hinge swap needs no list change (identical lists) (E9).
- Added `ParameterizedMassTests.AMaterialBindingOnTheBaseOfAPartWithoutBodiesIsRefused` to pin the `Shapes` body-0 refusal (Task 4) (E9).
- Owned-diff self-check: no needless branch, duplicate rule or unrelated churn found; the three resolvers fold into `PackageBodyGeometry.Colliders` (E9).
- For the coordinator: `delivery-batches.md:272` cites `PackageRuntimeParityTests.cs:596`, now `:599` (coordinator-owned record, not edited) (E9).

## Verification

Executor: the implementation owner for every handoff command below, on the pinned toolchain (`$HOME/.local/share/tim-toolchain/dotnet-10.0.403`, `node-v26.12.1-darwin-arm64/bin`); raw output under `~/.local/share/tim-toolchain/logs/slice-delete-declaration-colliders/`; any source, project or generated-input change invalidates the builds and the round.

**Commands (protocol handoff order; `docs/delivery-workflow.md:38–53`):**

- Restore; the three non-incremental capture builds with binlogs into `CSHARP_STATIC_GATE_BINLOGS`; the `CheckStagedTests.cs` and `VerifyPolicy.cs` builds -- expected: 0 warnings, 0 errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root <root>` -- expected: PASS, both canary PASS lines, `Coupling dependencies:` with violations 0.
- The registration check (`docs/delivery-workflow.md#new-package-registration-check`) -- expected: 0 failed, `Skipped: 0`; total stated against 821.
- Affected tests: `tools/BundleElements.Tests` and the `CuriousContraptions.tests` classes named in the acceptance-to-test table -- expected: 0 failed.
- `git add -N` on new files, `git diff --check c345690731b1654feca945558e66ce57d43d9f1b` -- expected: clean.
- Full round: `F=$(dotnet run --project tools/E2eRunner --no-build -c Debug -- deferred-filter)`; `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --filter "$F" -- RunConfiguration.TreatNoTestsAsError=true`, under `caffeinate -i`, HEAD at the top of the log and HEAD-AFTER with `git status --short` at the bottom -- expected: 0 failed, `Skipped: 0`, total per AC3.

### Acceptance-to-test table

| Row | Named test (planned) | Control | Boundary | Run |
|---|---|---|---|---|
| R1, AC1 | compile of the solution (capture builds) | — | a fixture without `SceneBodies` cannot compile (stated, not committed) | capture builds, 0 warnings, 0 errors (E10 item 2); AC2 `rg` zero hits (E10 item 7) |
| R2 | `SceneGeometryExtractorTests.MovingSubtreeBelongsOnlyToItsDeclaredBody` and `ASliderAndAHingeBakeTheirOwnRootsIntoTheirListsInBodyOrder` (each list from its own root, in body order; swapped roots swap the lists); `PackageRuntimeParityTests.EverySliderPackageDeclaresTheCompiledPrismaticJointAndMovingBody`, `EveryHingePackageDeclaresTheCompiledRevoluteJointAndCarriesItsMovingBody` (compiled moving-body colliders equal the baked lists); `BundleRoundTripTests.MovingBodyColliderListsRoundTripInBodyOrderAndANullListIsRefused` (`[null]` inner list and a missing key throw `JsonException`) | the base body's colliders; a non-empty nested list reads back equal | a slider and a hinge in one entry; compiled, `MotorTests.ASliderAndAHingeWithDifferentColliderCountsEachCompileTheirOwnBakedColliders` (slider 1 and hinge 2 colliders; control swapped) | passed, `affected-*.trx` (E10 item 6); compiled case passed, `repair1-game.trx` (E12), and after its move to the class end, `repair3-game.trx` (E17) |
| R3 | `BundleValidatorTests.MovingBodyColliderListsThatDoNotNumberTheMovingBodiesAreRefused` (fewer: one list for a slider and a hinge with a material binding on the hinge body, `Validate` returns `SceneColliderOwnership`); `AMalformedMovingBodyColliderIsRefusedAtItsList` (`Mechanical.MovingBodyColliders[1][0]`); `ASliderWhoseMovingBodyHasNoColliderIsRefused`, `HingeCapabilityValidates…` (`BodyWithoutCollider` at `Mechanical.MovingBodyColliders[0]`) | exactly one valid list per moving body admitted (each row) | zero lists with one moving body; one list too many; bundle-text round trip, `AMovingBodyListCountRoundTrippedThroughBundleTextIsRefused` | passed, `affected-bundle.trx` (E10 item 6); round-trip case passed, `repair2-bundle.trx` (E15) |
| R4 | decoded per-body comparison, baseline bundle against candidate (evidence, run log) and `VisibleSolidColliderTests` | — | all 45 packages, 59 bodies, 14 moving | all equal (E9, R4 line); `VisibleSolidColliderTests` passed (E10 item 6) |
| R5 | deleted: `DeclarationDataCollidersAreRefusedOnEveryBodyWhateverTheScene`, `APackageWhoseCollidersAreNotItsScenesIsRefusedAndWritesNoBundle` (2 rows; behaviour removed), `AMovingBodyPortOnADeclarationWithoutSceneBodiesFailsTheBuild` (covered by `APortNamingNoDeclaredSceneBodyFailsTheBuild`); kept: `AnOwnershipThatDoesNotNameTheBaseAndEachMovingBodyOnceIsRefused` | — | — | kept tests passed (E10 item 6) |
| R6, AC3 | the full local round | — | — | 7410 passed, 0 failed, `Skipped: 0` on HEAD `c3456907` plus the owned diff (7407 − 4 + 7) (E11); the repair adds 3 results, so the next round expects 7413 (E12) |
| Task 4 | `ParameterizedMassTests.AMaterialBindingOnTheBaseOfAPartWithoutBodiesIsRefused` (`Shapes` keeps its body-0 refusal) | the same entry, colliders kept, with a one-body footprint admitted | the footprint alone at no bodies | passed after the BH2 repair, `repair1-bundle.trx` (E12) |

## Auto Run Result

Status: as the frontmatter states (25 November 2026).

- **Change.** The declaration `Colliders` member and its copy into the bundle entry are deleted; declared slider and hinge moving bodies state mass only and the bake writes each moving body's scene colliders into `MechanicalPayload.MovingBodyColliders` (body order), whose count the validator refuses as `SceneColliderOwnership` and whose null inner list `Read` refuses; `IPuzzleElementDeclaration` extends `ISceneGeometryElement`; the data-collider and no-scene-bodies refusals, the declaration `Write` and the always-true type test are deleted; one bounded collider resolver; documents whose claims became false are corrected; moved citations outside `docs/planning/elements/` refreshed; part B's receipt and the S3/S5 status lines recorded (run-log E5).
- **Files.** 126 paths (run-log E13, E18): Abstractions 4, Bundle 7, `engine/gpu/CompiledIdentityMap.cs`, the bundle tool 3, 45 declarations, 27 test files, 39 current documents and records.
- **Review.** Four passes (run-log E13, E14, E16, E18): 10 patch rows applied (2 high, 2 medium, 6 low), 6 rejected with recorded refutations, 1 routed to the ruling-3 carry-forward (`deferred:`); Element-Free Scoped Pass at implementation acceptance (pass 4); Forward-Only no findings on passes 3 and 4.
- **Final diff scope: Pass** (Element-Free auditor `a5cb21c18688a1ae0`, re-engaged after pass 4; candidate the uncommitted tree on `c3456907`, 126 paths; production, declaration census, all test hunks and word diffs of documents inspected; nothing removed; no justified simplification; BH4-1 routing consistent with ruling 3; run-log E19).
- **Verification (final candidate).** Capture builds 0 warnings, 0 errors; maintainability gate PASS (`contract=4627, model=4, intrusive=0; violations=0`, metrics 0); full local round 7413 passed, 0 failed, `Skipped: 0`, HEAD and HEAD-AFTER `c3456907` (run-log E18); registration 826 on the handoff candidate (E10); R4 decoded comparison: 45 packages equal (E11, rerun by the Element-Free auditor in pass 1).
- **Follow-up review recommended: false.** Pass 1 patched one high and two medium entries and pass 2 one high; the mandatory independent re-reviews of passes 2 to 4 covered each (loop protocol, Triage).
- **Residual risks.** Element-document line citations moved by this slice wait for each element slice's dispatch refresh (`deferred-work.md` carry-forward). `CompiledIdentityMap` indexes the baked lists without its own bound; admission refuses a wrong count before any compile. Browser, performance, release and remote build, artifact and deployment qualification unverified.
