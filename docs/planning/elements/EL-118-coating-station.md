# EL-118 · Coating station — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-118 delivery slice](../invest/named-elements.md#element-118). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-118 · Coating station · Material |
| Anchor | [requirements.md#element-118](../requirements.md#element-118) (task `sequence-task-671`); [named-elements entry](../invest/named-elements.md#element-118); umbrella [gap-08](../requirements.md#gap-08) (index only); integration task [sequence-task-541](../requirements.md#sequence-task-541) |
| Owner key | S672 |
| Related identities | [EL-119 Dye station](EL-119-dye-station.md) (family sibling); [GAP-09 Fragmentation station](GAP-09-fragmentation-station.md) (fragments keep the treated value); [GAP-04 Driven wheel](GAP-04-driven-wheel.md) (slip control). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS`, and on one engine story, `ENGINE-SURFACE-STATE` (section 3). |

### Disposition

**Member of the Family slice "Treatment stations"** with EL-119, delivered after the engine programme (owner decision 3a) and one engine story. The two members differ only in declared values: which surface value the treatment sets, its treatment id, and art colour. Planned package id `coating_station`, directory `elements/CoatingStation/`, display name "Coating station". It is not a duplicate of anything delivered.

## 2. Declaration

### Declaration to write

Follows the delivered Receiver shape (a static multi-box part with one declared region and a small parameter set, `elements/Receiver/ReceiverElementDeclaration.cs:12-16`, `:59-62`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) for the static boxes, plus the treatment-region contract the engine story adds (section 3). `KinematicMode.Static`; `PayloadPolicy.None`; canonical zero static mass frame.
- **Body.** One static body. Origin at the centre of the basin floor's underside; cargo travels along local X under the arch.
- **Colliders (six boxes, metres, centre / half extents).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_floor` | (0, 0.03, 0) | (0.80, 0.03, 0.60) | basin floor, the applicator surface |
  | `col_lip_a`, `col_lip_b` | (0, 0.09, ±0.575) | (0.80, 0.03, 0.025) | side lips, keep cargo on the floor |
  | `col_post_a`, `col_post_b` | (0, 0.56, ±0.525) | (0.075, 0.50, 0.075) | arch posts |
  | `col_lintel` | (0, 1.135, 0) | (0.075, 0.075, 0.60) | arch lintel |

  Clear opening under the arch: 0.90 m wide (1.05 − 0.15) by 1.00 m high. A Basketball (0.68 m across, `elements/Ball/BallElementDeclaration.cs:30`), a Bowling ball (0.56 m) and a Domino lying flat (0.65 m wide, 0.25 m high, `elements/Domino/domino.tscn:12`) pass. Six boxes against the Receiver's five: the arch is the family silhouette the requirement's visual style names, and the lips stop a sliding box leaving sideways.
- **Material (f32).** Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s on every collider: the friction of the delivered Ramp (`elements/Ramp/RampElementDeclaration.cs:56`), so a tilted station behaves as a Ramp until the treatment acts.
- **Treatment region (the engine story's descriptor).** Applicator collider `col_floor`; target selector `TargetSelector.AllDynamic` (`CuriousContraptions.Abstractions/TargetSelector.cs:14`); dwell 0.5 s of cumulative contact with the applicator; effect: set contact friction of every collider of the treated body to 0.05; declared local treatment id `coated`; store: `doses`; cost: one dose per treated body. Each treatment publishes the story's committed first-occurrence `Treated` observation (treated body, `coated`, occurrence).
- **Store.** `doses`, a finite whole-number store, capacity and initial value from `supply`.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `supply` | `ParameterKind.WholeNumber` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:9`) | doses | 3 | 0 | 3 | player-editable unless the level locks it (D11); build-only |

  The treated friction 0.05 is fixed for this kind (D13). No `material` parameter: one coating is adopted.
- **Ports.** None. A finite supply with no refill.
- **Presentation bindings.** `Visual/Fill` local scale Y ← the committed fraction of `doses` (a store's committed fraction as a presentation driver, which the engine story adds; today's `CommittedCharge` reads an electrical source only). The stamp on treated cargo is not this package's art: it is the general surface-state overlay of the engine story.
- **Palette.** Category `ElementCategory.Structure` (`CuriousContraptions.Abstractions/ElementCategory.cs:11`; there is no Materials category). Order: 549 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Floor` cyan basin `#66b8c9` | box 1.6 × 0.06 × 1.2 | solid, `col_floor` | nothing |
| `Visual/Lip_A`, `Visual/Lip_B` gold `#f7cb52` | boxes 1.6 × 0.06 × 0.05 | solid, `col_lip_*` | nothing |
| `Visual/Post_A`, `Visual/Post_B`, `Visual/Lintel` cream `#fff8e9` | boxes as the colliders, edges chamfered inside the collider | solid, `col_post_*`, `col_lintel` | nothing |
| `Visual/Fill` navy line `#293954` | flat quad printed on the inner face of post A | decorative: printed flat, scaled by a binding | presentation binding |

Colours are the tokens the previous revision took from `DESIGN.md`. Icon: the arch over a basin with three dots (doses). At a glance it differs from the Dye station by the lintel: plain here, three flush printed bars there (pattern, not colour alone). A treated body is told apart by the story's overlay pattern (diagonal hatch), not by tint. No hose or feed line is drawn.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box colliders from package art, visible = solid | present | `ISceneGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| Pair friction is the geometric mean of the two colliders' friction | present | `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119` | `WorkshopPhysicsLawFrictionTests.SlidingUsesTheSharedNormalLoadAndSpinSharesItsBudget` |
| Declared contact material per collider, set at compile time | present | `ContactMaterialDeclaration`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133-143`; `MaterialParameterBinding`, `CuriousContraptions.Abstractions/MaterialParameterBinding.cs:8` | `ParameterizedBodyIntegrationTests.ReorderedBindingsResolveBaseSliderAndHingeMaterialsByExactBody` |
| Target selection over all dynamic bodies for a declared region | narrower: exists for residence sensors only; owner ENGINE-SURFACE-STATE | `TargetSelector`, `CuriousContraptions.Abstractions/TargetSelector.cs:12` | `IndependentResidenceGuideTests.TargetFiltering_OnlyMatchingTargetsAreSensedOrGuided` |
| A store's committed fraction as a presentation driver | narrower: the driver reads an electrical source store only; owner ENGINE-SURFACE-STATE | `PresentationDriver.CommittedCharge`, `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | not opened |
| A contact material value changed during a Run | **missing**; owner ENGINE-SURFACE-STATE | collider material lanes are written once at ingest (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:532-535`) | none |
| Cumulative contact dwell between a declared applicator collider and any selected body | **missing**; owner ENGINE-SURFACE-STATE (contacts from ENGINE-CONTACT-OBSERVATIONS) | none | none |
| A whole-number dose store debited once per treatment | **missing**; owner ENGINE-SURFACE-STATE | none | none |
| Per-body surface-state read for presentation | **missing**; owner ENGINE-SURFACE-STATE | none | none |
| A committed first-occurrence `Treated` observation the goal list can select | **missing**; owner ENGINE-SURFACE-STATE | none | none |

Counts: present 3, untested 0, narrower 2, missing 5. All five missing rows and both narrower rows have one owner: the element-free story **[`ENGINE-SURFACE-STATE`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-surface-state-treatment-regions-that-change-a-bodys-surface)** (sprint key `engine-surface-state`); the name is outside owner decision 3a's list because that list has none for this capability. The story takes its contacts from `ENGINE-CONTACT-OBSERVATIONS` and keeps per-body state in a fixed-width lane compiled at Run. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirements.** Row: "Finite contact-transferred material forms a declared surface layer"; outcome "Absent supply or missed contact leaves cargo untreated" ([element-118](../requirements.md#element-118)). Integration: treated cargo satisfies a downstream material-state goal, untreated fails; depletion and partial contact without silently granting complete coverage ([sequence-task-541](../requirements.md#sequence-task-541)).
- **Audit.** "Finite material supply and explicit material-state transition, distinct from optical colour channels" (`docs/physics-puzzle-gap-audit.md@a6c914e:L87-L87`).
- **Element map.** "Conserved coating deposition updates surface response with material identity/revision and work/source limits" ([element map](../general-engine-element-map.md)).
- **Legacy.** None. The only "coating" in the legacy is the optical beam-splitter surface (`engine/OpticalNetwork.cs@a6c914e:L96-L96`), unrelated.

## 5. Acceptance outline

Point of truth: [element-118](../requirements.md#element-118) and [sequence-task-541](../requirements.md#sequence-task-541). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

- **Law.** Coulomb friction with the pair coefficient μ = √(μ₁·μ₂). The treatment changes one declared value of the treated body; the contact law is unchanged.
- **Untreated Domino on the station floor or a Ramp.** μ = √(0.6 × 0.3) = √0.18 = 0.424 (Domino friction 0.6, `elements/Domino/DominoElementDeclaration.cs:36`; floor and Ramp 0.3). It slides only above arctan 0.424 = 23.0°.
- **Treated Domino.** μ = √(0.05 × 0.3) = √0.015 = 0.122; it slides above arctan 0.122 = 7.0°.
- **The tilted-station fixture.** The station is tilted 15° about Z with a 15° Ramp continuing its floor downhill. A flat Domino (long 1.1 m side along the slope, 0.65 m across, under the 0.90 m arch) is placed at the upper end.
  - Untreated it rests, because 15° is below 23.0°.
  - After 0.5 s of contact it is treated and accelerates at 9.81 × (sin 15° − 0.122 cos 15°) = 9.81 × (0.2588 − 0.1183) = 1.38 m/s², covering 0.5 m in √(2 × 0.5 / 1.38) = 0.85 s, so it has moved 0.5 m about 1.4 s into the Run and runs on down the Ramp.
- **Why the box lies flat.** Broad 1.1 × 0.65 m face down, 0.25 m thick. Upright it would tip once tan θ > 0.125 / 0.55 = 0.227 (12.8°), before 15°.
- **Missed contact.** On a 30° Ramp beside the station an untreated Domino slides at 9.81 × (0.5 − 0.424 × 0.866) = 1.30 m/s² and never touches the floor.
- **Contact shorter than the dwell.** A Basketball rolled at a level station first climbs the 0.06 m floor. Rolling, its speed after the climb is at most √(v² − (10/7) × 9.81 × 0.06) = √(v² − 0.84), and the edge strike can cost up to the rolling-step factor 1 − (5/7)(0.06 / 0.34) = 0.874 first.
  - At 5 m/s: between √(19.1 − 0.84) = 4.27 and √(25 − 0.84) = 4.92 m/s, so 1.6 m of floor gives 0.33 to 0.37 s of contact: under the dwell.
  - At 2 m/s: between √(3.06 − 0.84) = 1.49 and √(4 − 0.84) = 1.78 m/s, so 0.90 to 1.07 s: over the dwell.
- **Why 0.05.** The value must move the sliding angle clear of a taught Ramp angle on both sides: 7.0° and 23.0° bracket 15° by 8° each way.
- **Tick order.** Contacts are produced in Phase 3 (mechanical substeps); dwell is accumulated in Phase 5 (sensor evaluation), where the dose is debited and the new value and the `Treated` occurrence are committed with the tick. Phase 3 of the next tick is the first to use the value ([solver model](../../gpu-f32-physics.md#solver-model)). The engine story confirms this once for every consumer.
- **Persistence.** The value lasts until Reset; it does not wear off (decided, section 6).

### Acceptance rows

Planned class `CoatingStationTests` in `CuriousContraptions.tests`. Stall guard for every row: 6 s of game time (four times the 1.4 s the fixture needs).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a flat Domino on a station tilted 15°, when Run, then it rests for the dwell, is treated, and slides more than 0.5 m within 3 s | `ATreatedBoxSlidesWhereAnUntreatedOneRests` | with `supply` 0 it moves less than 0.02 m in 3 s | none (15° is 8° from each threshold) | distances as stated; a stuck treated box or a creeping untreated box is the failure |
| A2 | Given a Domino sliding down a 30° Ramp laid beside the station, when it passes without touching the floor, then it stays untreated and `doses` is unchanged | `MissedContactLeavesCargoUntreatedAndSupplyWhole` | A1 | none | exact state and dose count |
| A3 | Given a Basketball rolled at a level station at 5 m/s, then it is untreated and no dose is spent | `ContactShorterThanTheDwellGrantsNothing` | at 2 m/s it is treated and one dose is spent | 5 m/s (at most 0.37 s) against 2 m/s (at least 0.90 s) around 0.5 s | exact state and dose count |
| A4 | Given `supply` 3 and four Tennis balls at rest on a level station, then exactly three are treated, the fourth is not, and `doses` ends at 0 and never reads below 0 | `AnEmptyStoreTreatsNothingAndNeverGoesNegative` | `supply` 0 treats none | three and four bodies | exact counts |
| A5 | Given A1 has run, when Reset, then `doses` is 3 and the Domino is untreated at its authored pose; a second Run repeats A1 | `ResetRestoresTheStoreAndEveryTreatedBody` | none | none | as A1 |
| A6 | Given a placed station with `supply` 2, when saved and loaded, then `supply` is 2 and a fresh Run starts with no treated body | `SaveAndLoadKeepTheSupplyAndNoRunState` | none | none | exact |
| A7 | `supply` of −1, 4 or 2.5 is refused atomically with a typed reason | `InvalidSupplyIsRefused` (deleted when structural story S4's parameter row covers it) | 0 and 3 admitted | −1, 0, 3, 4 | typed refusal |
| A8 | Given A1's fixture with its Ramp ending in a Receiver that has a `Captured` goal, then the level is Solved | `OnlyTreatedCargoReachesTheReceiver` | with `supply` 0 the goal stays Waiting for the stall guard | none | goal phase |
| A9 | Given a goal of all-of {`Captured` by the Receiver, `Treated` (`coated`) of the Domino}, then it is Solved for the Domino of A8 | `TreatedCargoSatisfiesAMaterialStateGoal` | an untreated Domino delivered to the same Receiver down the 30° Ramp of A2 latches the capture and leaves the goal Waiting | none | goal phase and one Solved occurrence |

There is no port, so no wrong-kind connection row. Collaborators named by the requirement: a downstream material-state goal (A9), supply depletion (A4), partial contact (A2, A3).

### Integration

- **Works with.** Ramp, Domino, Basketball, Tennis ball, Receiver (all delivered); EL-119 (sibling); GAP-09 (fragments keep the value: a row in that slice); GAP-04 (a treated wheel slips: a row in that slice).
- **Campaign.** GAP-08 "Coat of Many Colours": introduction 94, practice 95, reuse 98, 136, 150 ([campaign allocation](../requirements.md#campaign-gap-allocation)). A level that fixes the station in place needs the lesson-fixture story (S8).
- **Goal.** A9 selects the committed `Treated` observation in the goal list beside `Captured`, as one more observation kind; no condition gains a qualifier (coordinator decision 16, 22 November 2026). A8 shows the same treatment mattering through contact physics alone.
- **Coverage.** Section 7.

### Delivery

- **Shape.** Family slice "Treatment stations" (EL-118, EL-119), engine tier on its first use of the new capability.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS` (the dwell's contacts); `engine-goal-evaluation`; `ENGINE-SURFACE-STATE`; structural story S7 (coverage binding); S3 preferred.
- **Files.** `elements/CoatingStation/` (`CoatingStation.csproj`, `CoatingStationElementDeclaration.cs`, `coating_station.tscn`, `icon.svg`), `CuriousContraptions.tests/CoatingStationTests.cs`, this spec, the ledger entry and `TODO.md`. Registration is derived, so no shared list is edited once S3 has landed.
- **Disqualifies the plan.** The slice's probe finds that the story sets values per collider pair rather than per body, has no whole-number store, cannot select all dynamic bodies or publishes no `Treated` observation; or the 0.05 floor is below the admitted friction range.

## 6. Open questions

Resolved:

1. **Coating set.** One coating, friction 0.05. A second coating is a later declared value, not a mode needed now (D19).
2. **Coverage model.** Contact dwell, two-valued. Contact below the dwell changes nothing and costs nothing; there is no partial state. Reason: the requirement forbids granting full coverage on partial contact, and a third state would need a second rule.
3. **Wear.** None; the value lasts until Reset. Reason: a wearing coat adds a rate no lesson uses.
4. **Which cargo.** Every dynamic body (`TargetSelector.AllDynamic`). Reason: no per-material admission exists, and GAP-04's slip control needs a treatable wheel.
5. **Liquid supply.** Discrete doses, not transported liquid (decided now: the row asks for a finite supply, and a liquid feed would add `ENGINE-LIQUID` as a prerequisite for no lesson).
6. **Material-state goal.** The committed `Treated` observation, selected by the goal list (row A9); there is no second goal condition form.
7. **Which bodies are served first.** When more bodies complete the dwell in one tick than doses remain, the engine story's canonical body order decides (row A4 asserts the count, not which).
8. **Catalogue entry.** Its own entry (D8), category Structure.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Six static boxes, one declared treatment region with a local treatment id, one store, one parameter, one binding. No update loop, handler or engine branch on this part.
2. **Generic laws.** Coulomb contact with a pair coefficient; a general region rule "cumulative contact with a declared applicator sets a declared surface value on the selected body, debits a declared store and publishes a committed observation". The rule is stated element-free and owned by `ENGINE-SURFACE-STATE`.
3. **Three layers.** Colliders: six `col_` boxes on one static body. Visuals: six solid art boxes, each with its collider, and one printed fill line. Animation: the fill line's scale, from the committed store; it writes nothing back.
4. **Flow and typing.** No ports. `supply` is a whole number; the surface value is f32; the treatment id is a typed local id; target selection is the `TargetSelectorKind` enum.
5. **Flat state.** Bodies 1 (static); colliders 6 (box); joints 0; drives 0; sensors 0; treatment regions 1; signal or electrical nodes 0; stores 1; presentation bindings 1.
6. **Performance.** Static body; the region reads the contacts of `col_floor` from `ENGINE-CONTACT-OBSERVATIONS`, found by compiled handle, so its work is bounded by that collider's contacts, never the scene. No cosmetic collider. The engine has no island sleeping today; cargo at rest on the floor relies on `ENGINE-ISLANDS-SLEEP`.
7. **Tests are scenarios.** A1 to A9 run the production simulation and goal evaluator and pass or fail on the general friction and treatment laws.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on `ENGINE-SURFACE-STATE`, a general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here, because the reader fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `coating_station` |
| Named identity | EL-118, `../invest/named-elements.md#element-118` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-671` |
| Decision | S672, `../invest/scope-corrections.md#s672` |
| Mode records | `Configuration=Adjustable` (`supply`) |

What it lacks: the ledger entry has no todo link (its scope source is the `gap-08` umbrella, and `sequence-task-671` is co-anchored with `element-118`, not with a todo); `scope-corrections.md` on published main has no `s672` block.
