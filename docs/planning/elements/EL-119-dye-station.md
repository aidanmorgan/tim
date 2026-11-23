# EL-119 · Dye station — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-119 delivery slice](../invest/named-elements.md#element-119). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-119 · Dye station · Material |
| Anchor | [requirements.md#element-119](../requirements.md#element-119) (task `sequence-task-672`); [named-elements entry](../invest/named-elements.md#element-119); umbrella [gap-08](../requirements.md#gap-08) (index only); integration task [sequence-task-541](../requirements.md#sequence-task-541) |
| Owner key | S673 |
| Related identities | [EL-118 Coating station](EL-118-coating-station.md) (family sibling); the colour filters CAT-055, CAT-031 and CAT-011 are optical channels, explicitly not dye; [GAP-09](GAP-09-fragmentation-station.md) (fragments keep the tag). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS`, and on one engine story, `ENGINE-SURFACE-STATE` (section 3). |

### Disposition

**Member of the Family slice "Treatment stations"** with EL-118, delivered after the engine programme (owner decision 3a) and one engine story. It differs from EL-118 only in declared values: the treatment sets a surface tag instead of a friction value, its treatment id is `dyed`, and the basin art is coral. Planned package id `dye_station`, directory `elements/DyeStation/`, display name "Dye station". Not a duplicate: its effect and its acceptance differ from EL-118's.

## 2. Declaration

### Declaration to write

The same declaration shape as [EL-118](EL-118-coating-station.md#declaration-to-write); only the rows below differ. The six box colliders, their dimensions, the static body, the material (restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s), the `doses` store, the `supply` parameter (whole number, 0 to 3, default 3, build-only, player-editable unless locked) and the absence of ports are identical, so the two read as one kit.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the treatment-region contract of the engine story. `KinematicMode.Static`.
- **Treatment region.** Applicator collider `col_floor`; target selector `TargetSelector.AllDynamic`; dwell 0.5 s of cumulative contact; effect: set the treated body's surface tag to this package's declared tag; declared local treatment id `dyed`; cost: one dose. A body that already carries the tag is not treated again and costs nothing. Each treatment publishes the story's committed first-occurrence `Treated` observation (treated body, `dyed`, occurrence).
- **Declared surface tag.** One tag with a declared overlay style: three parallel bars in coral `#de7058`. A tag is declaration data: a typed local id and an overlay style. The engine knows no tag by name.
- **What the tag changes.** Nothing in physics: friction, restitution, rolling resistance and mass are untouched. Committed state has exactly two general readers: the renderer's surface-state overlay, and the goal list, which may select the `Treated` observation like any other observation kind. It has no optical response; a light source neither sets nor reads it.
- **Presentation bindings.** `Visual/Fill` local scale Y ← the committed fraction of `doses`, as EL-118.
- **Palette.** Category `ElementCategory.Structure`. Order: 550, next to `coating_station` at 549 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Floor` coral basin `#de7058` | box 1.6 × 0.06 × 1.2 | solid, `col_floor` | nothing |
| `Visual/Lip_A`, `Visual/Lip_B` gold `#f7cb52` | boxes 1.6 × 0.06 × 0.05 | solid, `col_lip_*` | nothing |
| `Visual/Post_A`, `Visual/Post_B`, `Visual/Lintel` cream `#fff8e9` | boxes as the colliders | solid, `col_post_*`, `col_lintel` | nothing |
| `Visual/Bars` navy `#293954` | three bars printed flush on both lintel faces | decorative: printed flat | nothing |
| `Visual/Fill` navy line | flat quad printed on the inner face of post A | decorative: printed flat, scaled by a binding | presentation binding |

Icon: the arch with three bars over a basin. It differs from the Coating station by the three printed lintel bars (pattern) as well as the basin colour. Dyed cargo shows the three-bar overlay over its own colour; the part's catalogue colour is not repainted, so a dyed Basketball is still read as a Basketball. No hose or feed line is drawn.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box colliders from package art, visible = solid | present | `ISceneGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| Capture of a payload by a declared residence sensor, read by the goal evaluator | present | `ResidenceSensorDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:188`; `engine/gpu/WorkshopGoalEvaluator.cs:30-36` | `IndependentResidenceGuideTests.TargetFiltering_OnlyMatchingTargetsAreSensedOrGuided` (sensing); the goal read is cited from the goal contract's probe row 1, body not opened here |
| Target selection over all dynamic bodies for a declared region | narrower: residence sensors only; owner ENGINE-SURFACE-STATE | `TargetSelector`, `CuriousContraptions.Abstractions/TargetSelector.cs:12` | same test |
| A store's committed fraction as a presentation driver | narrower: electrical source stores only; owner ENGINE-SURFACE-STATE | `PresentationDriver.CommittedCharge`, `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | not opened |
| Cumulative contact dwell with a declared applicator collider | **missing**; owner ENGINE-SURFACE-STATE (contacts from ENGINE-CONTACT-OBSERVATIONS) | none | none |
| A declared surface tag set on a body during a Run and restored on Reset | **missing**; owner ENGINE-SURFACE-STATE | none | none |
| A whole-number dose store debited once per treatment | **missing**; owner ENGINE-SURFACE-STATE | none | none |
| Per-body surface-state read: renderer overlay | **missing**; owner ENGINE-SURFACE-STATE | none | none |
| A committed first-occurrence `Treated` observation the goal list can select | **missing**; owner ENGINE-SURFACE-STATE | the goal model has no such observation kind (`engine/gpu/WorkshopPuzzle.cs:9-31`) | none |

Counts: present 2, untested 0, narrower 2, missing 5. One owner for all seven: the element-free story **[`ENGINE-SURFACE-STATE`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-surface-state-treatment-regions-that-change-a-bodys-surface)** (sprint key `engine-surface-state`). Its goal side is one more observation kind in the single condition list, a first-occurrence latch like the others, read from committed state with nothing flowing back; no existing condition gains a qualifier. It is planned after `engine-goal-evaluation`, which introduces that list.

## 4. Sources and legacy

- **Requirements.** Row: "Finite pigment transport changes a supported material's colour state"; outcome "No pigment means no colour change; optical illumination is not dye" ([element-119](../requirements.md#element-119)). Integration as EL-118 ([sequence-task-541](../requirements.md#sequence-task-541)).
- **Audit.** "explicit material-state transition, distinct from optical colour channels" (`docs/physics-puzzle-gap-audit.md@a6c914e:L87-L87`).
- **Element map.** "Conserved dye changes declared material/optical appearance; do not assume structural coating/fracture changes" ([element map](../general-engine-element-map.md)).
- **Legacy.** None. Legacy colour handling was optical only (`engine/OpticalColour.cs`, `engine/OpticalNetwork.cs` at `a6c914e`); no material colour state existed.

## 5. Acceptance outline

Point of truth: [element-119](../requirements.md#element-119) and [sequence-task-541](../requirements.md#sequence-task-541). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

- **Law.** The general treatment rule of `ENGINE-SURFACE-STATE` with a tag as its effect. The tag changes no force, energy or speed.
- **Physics unchanged.** A dyed flat Domino keeps the pair friction √(0.6 × 0.3) = 0.424 on the station floor and on a Ramp (`elements/Domino/DominoElementDeclaration.cs:36`, `elements/Ramp/RampElementDeclaration.cs:56`), so it slides only above arctan 0.424 = 23.0°. On a station tilted 15° it is dyed and stays put, where the coated Domino of EL-118 slides away; that contrast is the lesson.
- **The delivery fixture.** The station is tilted 30° with a 30° Ramp continuing its floor to a Receiver. A flat Domino placed at the upper end slides from rest at 9.81 × (sin 30° − 0.424 cos 30°) = 9.81 × (0.5 − 0.367) = 1.30 m/s². Its trailing edge is still on the 1.6 m floor until √(2 × 1.6 / 1.30) = 1.57 s, so it has more than three times the 0.5 s dwell of contact and is dyed on the way; at 0.5 s it has moved only ½ × 1.30 × 0.25 = 0.16 m.
- **Contact shorter than the dwell.** As [EL-118](EL-118-coating-station.md#behaviour-and-numbers): a Basketball rolled at a level station at 5 m/s touches the floor for at most 0.37 s (not dyed); at 2 m/s for at least 0.90 s (dyed).
- **Supply.** Three doses: the smallest count that lets a level ask for "dye two, leave one".
- **Tick order.** As EL-118: contacts in Phase 3, dwell, the tag write and the `Treated` occurrence in Phase 5, committed with the tick ([solver model](../../gpu-f32-physics.md#solver-model)). The goal evaluator reads committed observations on the main thread, so a capture and a treatment that commit in the same tick are seen together.

### Acceptance rows

Planned class `DyeStationTests`. Stall guard for every row: 6 s of game time.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| B1 | Given the 30° delivery fixture and a goal of all-of {`Captured` by the Receiver, `Treated` (`dyed`) of the Domino}, when Run, then the level is Solved once | `DyedCargoSatisfiesAMaterialStateGoal` | with `supply` 0 the same delivery latches the capture and the goal stays Waiting for the stall guard | none | goal phase and one Solved occurrence |
| B2 | Given `supply` 0, when a Domino rests on a level station for 3 s, then it is not dyed | `NoPigmentMeansNoColourChange` | with `supply` 1 it is dyed | none | exact state |
| B3 | Given a flat Domino on a station tilted 15°, then it is dyed and has moved less than 0.02 m after 3 s | `DyeDoesNotChangeContactResponse` | with `supply` 0 it also moves less than 0.02 m | none | 0.02 m: a slide of 0.5 m is what a friction change would give |
| B4 | Given a Basketball rolled at a level station at 5 m/s, then it is not dyed and no dose is spent | `ContactShorterThanTheDwellGrantsNothing` | at 2 m/s it is dyed | 5 m/s (at most 0.37 s) against 2 m/s (at least 0.90 s) around 0.5 s | exact state and dose count |
| B5 | Given `supply` 3 and four Tennis balls at rest on a level station, then exactly three are dyed and `doses` ends at 0; given `supply` 3 and one ball left on the floor for 3 s, then `doses` ends at 2 | `AnEmptyStoreDyesNothingAndARepeatCostsNothing` | `supply` 0 dyes none | three and four bodies | exact counts |
| B6 | Reset restores `doses`, removes every tag and clears the `Treated` latch; a second Run solves B1 again | `ResetRestoresTheStoreAndRemovesEveryTag` | none | none | exact |
| B7 | Save and load keep `supply`; a loaded construction starts with no tag | `SaveAndLoadKeepTheSupplyAndNoRunState` | none | none | exact |
| B8 | A level whose goal selects a `Treated` observation with a treatment id that no placed package declares is refused at content load with a typed reason | `AGoalSelectingAnUndeclaredTreatmentIsRefused` | B1's level loads | none | typed refusal, installed catalogue unchanged |
| B9 | `supply` of −1, 4 or 2.5 is refused atomically | `InvalidSupplyIsRefused` (deleted when S4's parameter row covers it) | 0 and 3 admitted | −1, 0, 3, 4 | typed refusal |
| B10 | Given an undyed Domino lit by a light emitter for 3 s, then it carries no tag | `IlluminationIsNotDye`, a **later-partner row** | B1 | none | exact state |

B10 needs `ENGINE-OPTICAL` and one delivered emitter. It is written in the first slice after both exist; EL-119's ledger row stays open until it runs. No port, so no wrong-kind connection row.

### Integration

- **Works with.** Domino, Basketball, Tennis ball, Ramp, Receiver (delivered); EL-118; GAP-09 (fragments keep the tag); an optical emitter (undelivered, B10).
- **Campaign.** GAP-08: introduction 94 (dye has its own objective), practice 95, reuse 98, 136, 150. A level that fixes the station needs the lesson-fixture story (S8).
- **Coverage.** Section 7.

### Delivery

- **Shape.** Family slice "Treatment stations" with EL-118.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS` (the dwell's contacts); `engine-goal-evaluation`; `ENGINE-SURFACE-STATE`; S7; S3 preferred. B10 later, after `ENGINE-OPTICAL` and an emitter.
- **Files.** `elements/DyeStation/` (`DyeStation.csproj`, `DyeStationElementDeclaration.cs`, `dye_station.tscn`, `icon.svg`), `CuriousContraptions.tests/DyeStationTests.cs`, this spec, the ledger entry and `TODO.md`.
- **Disqualifies the plan.** The story publishes no `Treated` observation (then B1 has no general reader and the identity is cosmetic only: return to planning); or the overlay cannot draw on another package's body without that package naming the tag.

## 6. Open questions

Resolved:

1. **Pigment set.** One pigment, coral, fixed for this kind (D13: the row names no colour parameter). When a campaign level needs two colours, the pigment becomes a Choice with one declared tag and treatment id per option; the engine story's text allows that, with the per-option values under `ENGINE-PARAMETER-BOUND-FRAMES`.
2. **Mixing.** None: one tag, and a second treatment of a dyed body does nothing.
3. **Which cargo.** Every dynamic body. The overlay is drawn over the part's own colour, so ball identity colours are kept.
4. **Optical effect.** None. The tag does not change absorption or reflection (the element map says not to assume it), and Epic 13 receivers do not read it.
5. **Goal read.** The committed `Treated` observation in the goal list; no capture condition carries a tag qualifier (coordinator decision 16, 22 November 2026).
6. **Liquid pigment.** Discrete doses, as EL-118.
7. **Catalogue entry.** Its own entry (D8), category Structure.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Six static boxes, one treatment region with a local treatment id, one declared tag with an overlay style, one store, one parameter, one binding. No code names this part or its tag.
2. **Generic laws.** The general treatment rule and its committed observation, stated element-free and owned by `ENGINE-SURFACE-STATE`; the goal list's ordinary selection of an observation.
3. **Three layers.** Colliders: six `col_` boxes. Visuals: six solid art boxes with their colliders, printed bars and a printed fill line. Animation: the fill scale and the overlay on tagged bodies, both from committed state; neither writes back to physics, sensors or goals. The goal read is a separate main-thread read of the same committed state.
4. **Flow and typing.** No ports. The tag and the treatment id are typed local ids; `supply` is a whole number.
5. **Flat state.** Bodies 1 (static); colliders 6 (box); joints 0; drives 0; sensors 0; treatment regions 1; declared tags 1; stores 1; presentation bindings 1. The per-body tag is one fixed-width lane per body in the story's table.
6. **Performance.** As EL-118: the region reads the applicator's contacts from `ENGINE-CONTACT-OBSERVATIONS` by compiled handle, so its work is bounded by them and nothing is proportional to the scene. The engine has no island sleeping today; cargo at rest on the floor relies on `ENGINE-ISLANDS-SLEEP`.
7. **Tests are scenarios.** B1 to B10 run the production simulation and goal evaluator.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on `ENGINE-SURFACE-STATE`, a general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here, because the reader fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `dye_station` |
| Named identity | EL-119, `../invest/named-elements.md#element-119` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-672` |
| Decision | S673, `../invest/scope-corrections.md#s673` |
| Mode records | `Configuration=Adjustable` (`supply`) |

What it lacks: the ledger entry has no todo link (its scope source is the `gap-08` umbrella); `scope-corrections.md` on published main has no `s673` block.
