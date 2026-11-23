# CAT-020 · counter — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-020 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-020-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-020](../requirements.md#current-cat-020). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid-sweep`](../../../TODO.md) the parts that stand proud of the housing collide with their drawn primitives, after the housing and foot boxes: the cream face panel as a 1.4 × 1.02 × 0.04 box at z 0.35, the nine dot lamps as spheres r 0.1 at x −0.36, 0, 0.36 and y 0.3, 0, −0.3 (z 0.4), and three sockets as spheres r 0.075 at (0, 0.72, 0.1) and (∓0.93, 0, 0), 15 colliders in all (`elements/Counter/counter.tscn`; `VisibleSolidColliderTests` checks them against the art and drops a ball onto a dot lamp; `CounterTests`). The `ActivationOut` socket (0, −0.72, 0.1) lies wholly inside the foot and is decorative. Placement bounds every collider (`PlacementPose`), so the sockets and lamps now count toward the bench edge: the counter reaches x ±1.005 against the foot's ±0.9, y +0.795 (the ActivationIn socket) against the housing's +0.625, and z +0.5 (the dot lamps) against the foot's +0.425; placed at x ±6, or at z 3 turned a quarter, it is refused `OutsideBench` (`PlacementPoseTests.ProudSocketsCountTowardTheBenchEdge`). Those are the delivered positions; the delivered scene floats its foot, face panel and three proud sockets off the housing, and [repair 3](#repair-3-seated-foot-face-panel-and-sockets) seats them.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-020 · `counter` |
| Requirement anchor | [current-cat-020](../requirements.md#current-cat-020); retained behaviour [todo-269](../requirements.md#todo-269) |
| Mapped identities | [EL-183 Resettable counter](../invest/named-elements.md#element-183) — partial match: EL-183 needs a separately addressed reset input, which the legacy counter lacks (open question 2). No TH/RAD/GAP identity. |
| Roadmap story | 9.2 "Pulse Counter Event Accumulator" (Epic 9) Current [CAT-020 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/Counter`, id `counter`; Story 9.2, repaired by 9R.1, generic instance by C-Memory (native Pass `f5ffdb`). Sprint key `9-2-pulse-counter-event-accumulator` stays in-progress for its retro-verification and Chrome clauses. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-SIGNAL-NODES` (its saturating-count row) and `ENGINE-CAPABILITY-INSTANCES` (the product-named node kind `Counter`, audit E1). |

## 2. Declaration

The delivered declaration is `elements/Counter/CounterElementDeclaration.cs` with the scene `elements/Counter/counter.tscn` (read on `8d1d68aa`). It implements `ISignalElement`, `ISupplyElement` and `ISceneGeometryElement` (`:9`). The legacy sources of the carried values are in section 4.

**Body and material.** One static body with no mass and no joint (`:56`). Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:71`), all f32.

**Colliders (physics graph).** Fifteen, baked in scene order from hidden proxies (`counter.tscn:156–198`). Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 1.65 × 1.25 × 0.65 | origin |
| 1 | box | 1.8 × 0.16 × 0.85 | (0, −0.72, 0) |
| 2 | box | 1.4 × 1.02 × 0.04 | (0, 0, 0.35) |
| 3–11 | sphere | r 0.1 | a 3 × 3 grid at x −0.36, 0, 0.36 and y 0.3, 0, −0.3, z 0.4 |
| 12–14 | sphere | r 0.075 | (0, 0.72, 0.1), (−0.93, 0, 0), (0.93, 0, 0) |

Placement bounds every collider, so the counter reaches x ±1.005, y +0.795 and z +0.5 at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Housing` | box 1.65 × 1.25 × 0.65 m, ochre `#e8b764` | solid, collider 0 | nothing |
| `Visual/Foot` | box 1.8 × 0.16 × 0.85 m, navy `#293954` | solid, collider 1 | nothing |
| `Visual/Face` | box 1.4 × 1.02 × 0.04 m, cream `#fff8e9` | solid, collider 2 | nothing |
| `Visual/DotLamp_0`–`8` | spheres r 0.1 m (`counter.tscn:71–105`), slate to gold | solid, colliders 3–11 | nothing (colour bindings only) |
| `Visual/Visual_Socket_ActivationIn`, `Visual_Socket_PowerIn`, `Visual_Socket_Supply` | spheres r 0.075 m, gold | solid, colliders 12–14 | nothing |
| `Visual/Visual_Socket_ActivationOut` | sphere r 0.075 m at (0, −0.72, 0.1) | decorative: enclosed, wholly inside the foot | nothing |

Selection radius 1.2 (`:11`). Icon `elements/Counter/icon.svg`. At a glance it is the ochre case with a 3 × 3 grid of dots; no other delivered part has a dot grid.

**Animation (animation graph).** Twenty-seven presentation bindings (`:13–39`): the red, green and blue albedo of lamp i follow `CommittedCount` of node `signal` with threshold i + 1, slate (85, 101, 115)/255 to gold (247, 203, 82)/255, SmoothStep 0.1 s. They read the committed count and write nothing back.

**Ports** (`:60–63`).

| `PortId` | Domain | Direction | Role | Signal kind | Position (m) | Facing |
|---|---|---|---|---|---|---|
| `ActivationIn` | Signal | Input | ActivationIn | Pulse | (0, 0.72, 0.1) | +Y |
| `ActivationOut` | Signal | Output | ActivationOut | Pulse | (0, −0.72, 0.1) | −Y |
| `PowerIn` | Electrical | Input | PowerIn | — | (−0.93, 0, 0) | −X |
| `Supply` | Electrical | Output | Supply | — | (0.93, 0, 0) | +X |

**Parameter** (`:53`). The editor refuses 0, 10, 2.5, a non-number and text; it never clamps.

| Name | Kind | Unit | Minimum | Maximum | Default | Editor policy | Run edit |
|---|---|---|---|---|---|---|---|
| `target_count` | WholeNumber | none | 1 | 9 | 3 | Reject (default) | Build-only |

**Signal node, supply node and event.** One signal node `signal`, a `SignalPulseCountDescriptor` on `target_count` (`:80`), with `ActivationIn` on its input and `ActivationOut` on its output (`:82–86`). One supply node `contact`, a `SupplyContactDescriptor` closed while `signal` is `Latched`, between `PowerIn` and `Supply` (`:88–93`). Event `ThresholdReached` (`:74`), published once by the input that reaches the target. `ElectricalNodes` holds one `Switch` row (`:95–98`); its ratings are unread (section 7).

**Compiled counts per instance.** 1 static body; 15 colliders (3 boxes, 12 spheres); 0 joints; 0 drives; 0 sensors; 1 signal node with one committed memory row; 1 supply node; 0 stores; 4 ports; 27 bindings. All are fixed at compile.

**Catalogue.** Id `counter`, title "Counter", category Control, palette order 22, colour (0.91, 0.72, 0.39), description "Counts trigger events, lighting one dot per event. When the count reaches its target (1 to 9, default 3) it emits once and keeps its electrical contact closed until Reset. Connect a supply to power downstream devices." (`:43–50`). Coverage key `counter`.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand.

**Generic laws.** The package holds no script, callback, update loop or state machine.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from its face, dots and sockets | rigid contact with the product pair law for restitution, and friction | fifteen colliders; the material |
| It counts distinct events and emits once at the target | signal network pulse-count rule: each distinct pulse occurrence on the node's input adds one; the occurrence that reaches the target emits one pulse occurrence; a held or repeated occurrence adds nothing | `target_count` |
| The count stops at the target | the same rule must hold the count at the target. **Delivered, it does not** (repair 1 below) | `target_count` |
| It closes a supplied contact until Reset | supply network contact rule: a contact is closed while its controlling signal node is in the declared state | `SignalContactState.Latched` |
| Dots | presentation bindings on the committed count | twenty-seven bindings |

**Capability probe.** 5 present, 0 untested, 1 narrower, 1 missing (not needed for the decided display).

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Pulse-count node with one emission at the target | present | `SignalPulseCountDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:63`); `engine/gpu/ActivationMemory.cs:13–14` | `MemoryGuardTests.ThreeBallsThroughTheDetectorEmitOnceOnTheTargetCrossingOnly`; `MemoryGuardTests.APulsedTargetEmitsOnItsOwnPulseAndNeverAfterSaturation` |
| A held or repeated occurrence counts once | present | the same law | `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (it asserts the contact and lamp stay latched through 20 more inputs; it does not assert the count) |
| Supplied contact closed from the target until Reset | present | `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`) | `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach`; `CounterTests.PhysicsCompiler_CompilesCollidersAndCounterPower` |
| Parameter refusal, Reset, save and load | present | `ParameterDescriptor` range | `CounterTests.TheDeclaredTargetIsAWholeNumberFromOneToNine`; `MemoryGuardTests.ResetIsExactAndASavedCounterReplaysIdentically`; `CounterTests.CounterCount_LivesInTheCommittedCheckpoint_SupersededCandidateLeavesItUnchanged_AndResetClearsIt` |
| Count presentation | present | `PresentationDriver.CommittedCount`; a lamp lights when `memory.Count >= d.Threshold` (`engine/gpu/WorkshopDeclaredPresentation.cs:446`) | dot-lamp assertions at `CounterTests.cs:41–70` and `DeclaredPresentationTests.cs:266–270` |
| The count holds at the target | narrower | `ActivationMemory.cs:13` increments until `uint.MaxValue`; `MemoryGuardTests.cs:147` pins a count of 12 | Owner: `ENGINE-SIGNAL-NODES`, its saturating-count row (repair 1) |
| Art and its paired collider present only for a parameter value | missing | every `PresentationDriver` reads committed state, none a parameter (`CuriousContraptions.Abstractions/ElementPresentation.cs:12–37`); lamp colliders are baked | Not needed for the decided nine-dot display. Owner: `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values and parameter-bound parts, coordinator decision 16; repair 2) |

**Repair 1: saturation (coordinator decision, 22 November 2026, under the owner's standing delegation).** The requirement row already says the count "saturates", so this is a confirmed defect in the generic pulse-count law, not an owner choice. Delivered, a default counter (target 3) lights a fourth to ninth dot on a fourth to ninth event, so the face stops showing that the target was reached. Emission and the contact are correct. The repair is the saturating-count row of `ENGINE-SIGNAL-NODES` (owner decision 3a), stated element-free: a pulse-count node holds its count at its target (`count = min(count + 1, target)`). No element code changes. The row's acceptance and tests are in [batch B readiness](readiness/batch-b.md) (gap 4).

**Repair 2: the display keeps nine fixed dots (same decision).** After repair 1, dots 1 to `target` light and dots `target + 1` to 9 never light. Reason: showing only `target` dots needs art and colliders selected by a parameter, a general capability that does not exist and that would today serve one element. It is recorded as a named missing capability owned by `ENGINE-PARAMETER-BOUND-FRAMES`, with acceptance text in batch B readiness (gap 5); this part does not wait for it. The nine dots stay solid whether or not they can light.

### Repair 3: seated foot, face panel and sockets

**Defect (second audit, `docs/planning/elements/readiness/audits/reaudit-group-1.md` X-8; group repairs, 23 November 2026).** The delivered scene (`elements/Counter/counter.tscn`) draws four solids clear of the housing they belong to (housing x ±0.825, y ±0.625, z ±0.325):
- the foot (0.16 m tall at y −0.72) has its top at −0.72 + 0.08 = −0.64, so the housing floats 0.64 − 0.625 = 0.015 m above it;
- the face panel (0.04 m deep at z 0.35) spans z 0.33–0.37, 0.33 − 0.325 = 0.005 m in front of the housing;
- the `ActivationIn` socket at y 0.72 leaves 0.72 − 0.075 − 0.625 = 0.02 m below it;
- the `PowerIn` and `Supply` sockets at x ∓0.93 leave 0.93 − 0.075 − 0.825 = 0.03 m.

A drawn solid may not hang clear of the part it belongs to (the rule CAT-060 and EL-183 apply). **Repair:** scene data only, art and its hidden proxy moved together, so the visible-solid matcher still pairs them one to one; no engine capability is involved and no declaration value other than the port positions changes. Positions match [EL-183](EL-183-resettable-counter.md), which copies this scene seated.

| Node | Delivered centre (m) | Seated centre (m) | Check |
|---|---|---|---|
| `Foot` and `col_0_1` | (0, −0.72, 0) | (0, −0.705, 0) | top −0.705 + 0.08 = −0.625, the housing's bottom |
| `Face` and its proxy | (0, 0, 0.35) | (0, 0, 0.345) | back 0.345 − 0.02 = 0.325, the housing's front; the dot lamps (z 0.4, r 0.1, back at 0.3) still stand 0.5 − 0.365 = 0.135 m proud of it |
| `ActivationIn` socket, locator and collider | (0, 0.72, 0.1) | (0, 0.625, 0.1) | centre on the housing top |
| `ActivationOut` socket and locator | (0, −0.72, 0.1) | (0, −0.705, 0.1) | still wholly inside the seated foot (sphere y −0.78 to −0.63 within −0.785 to −0.625; z 0.025 to 0.175 within ±0.425), so still decorative with no collider |
| `PowerIn`, `Supply` sockets, locators and colliders | (∓0.93, 0, 0) | (∓0.825, 0, 0) | centres on the housing's side faces |

**Consequences the repair slice re-derives.**
- Reach at the bench edge becomes x ±0.9 (the foot, equal to the side sockets' 0.825 + 0.075), y +0.7 (the `ActivationIn` socket, 0.075 above the housing) and z +0.5 (the dot lamps, 0.075 beyond the foot's 0.425).
- `CounterTests.CounterScene_ExtractsItsSolidPartsAsCollidersAndPortLocators` (`CuriousContraptions.tests/CounterTests.cs:104`–`:124`) and the declared-port assertions (`:154`–`:157`) take the seated values.
- `PlacementPoseTests.ProudSocketsCountTowardTheBenchEdge` (`CuriousContraptions.tests/PlacementPoseTests.cs:389`) loses its x premise, because the seated sockets no longer reach past the foot. Its counter rows become, every position on the 0.1 m placement grid (`PlacementPose.Compose` snaps the requested position to it before the bench test, so a position such as 6.15 would be placed at 6.2 or 6.1): unturned at x ±6.2 refused (reach 7.1 > 7) and at ±6.0 admitted (6.9); unturned at z 3.6 refused (3.6 + 0.5 = 4.1 > 4) and at z 3.4 admitted (3.9); turned a quarter about +Y at z 3.2 refused (3.2 + 0.9 = 4.1) and at 3.0 admitted (3.9). The x pair is the row that fails if the sockets are not seated (the delivered reach of 1.005 refuses x 6.0). The z 3.6 pair does not separate the dot lamps from the foot (the foot alone reaches 3.6 + 0.425 = 4.025), and no 0.1 m grid position does: that needs 3.5 < z ≤ 3.575. The proud-solid guard of the same test stays its Pressure plate pair.
- Slice placement: visible-solid repair part D, with the Clutch, Reverse gear and Motor index bar, which runs after the engine programme (coordinator decision 33; owner decision 22).

**EL-183 inherits all three.** [EL-183](EL-183-resettable-counter.md) reuses the pulse-count node and this face ("later ones saturate", "Nine dots is what the face shows"). It needs repair 1 before its own slice, takes the nine-dot display as decided here, and copies the scene with repair 3's seated positions.

**Conformance (owner addendum, 22 November 2026): conforms with two fixes.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5). Naming residue: the generic node kind is called `ActivationNodeKind.Counter` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`); renaming it to a capability name is a required forward-only refactor owned by `ENGINE-CAPABILITY-INSTANCES` (audit E1; section 7).
- Generic laws: fix 1 is repair 1, in the law and not in the element.
- Three layers: hold. Every static proud solid has its own collider; the bindings change colour only.
- One-way flow, typing and flat state: hold, with the counts in section 2.
- Fix 2: the unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Fifteen colliders against the two of the original housing, and against ten for a gate, has one reason: visible = solid. The face stands proud of the housing, and each of the nine dots is a drawn sphere of radius 0.1 m whose top stands 13 cm in front of the face, so each has its own primitive; one box over the grid would collide in the gaps between dots, where nothing is drawn. Three sockets stand clear of the housing; the fourth is inside the foot and has no collider. No collider is cosmetic. The cost for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034; the Counter is its largest case. The node costs one row per tick. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** A source of several distinct pulses in one Run: three balls through a Ball detector, or a Repeating clock. One Impact switch cannot do it: its signal node latches and it "sends one activation" (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:26`, `:72`, `:79`). A Signal lamp for the output; Battery and Powered gate for the contact. All are delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| K1 | Counts activation deliveries, emits once at its target and latches an electrical contact; Workshop Reset clears the count; never generates supply. | `parts/CounterPart.cs@a6c914e:L8-L9` | carry forward |
| K2 | Readable state: count, target, phase (`Counting`/`Reached`). | `parts/CounterPart.cs@a6c914e:L14-L20` | carry forward |
| K3 | Ports and contact route as in section 2. | `parts/CounterPart.cs@a6c914e:L26-L36` | carry forward |
| K4 | Target must be a finite integer 1–9. | `parts/CounterPart.cs@a6c914e:L37-L42` | carry forward |
| K5 | Only `Trigger` commands are accepted; Reached → emit immediately; Accumulated or Saturated → no emission. | `parts/CounterPart.cs@a6c914e:L43-L52` | carry forward the rule; do not carry forward the per-part `HandleActivation` callback (declaration data on the shared network) |
| K6 | Dot-lamp layout and art. | `parts/CounterPart.cs@a6c914e:L53-L71` | carry forward |
| K7 | Phase is `Reached` exactly when count = target. Increment: at target → `Saturated` (unchanged); else count + 1 → `Reached` if now equal, else `Accumulated`. | `engine/SimulationCounters.cs@a6c914e:L16-L22` and `engine/SimulationCounters.cs@a6c914e:L56-L62` | carry forward |
| K8 | Declaration requires owner, slot and target ≥ 1. | `engine/SceneCounterDeclaration.cs@a6c914e:L5-L14` | carry forward |
| K9 | For targets 1, 3, 9: every delivery counts once; only the target-th returns Reached; 20 more deliveries are Saturated and leave the state unchanged. | `CuriousContraptions.tests/SimulationCountersTests.cs@a6c914e:L5-L20` | carry forward |
| K10 | Parameter wire name `target_count`; activating the counter before Run is rejected; restores to earlier counts are exact. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L17-L37` and `engine/MachineData.cs@a6c914e:L84-L84` | carry forward |
| K11 | Targets 1, 3, 9 with and without supply; the supplied load is a Powered gate (`powered_gate`, CAT-051; `CuriousContraptions.tests/CounterTests.cs@a6c914e:L54-L54`). One activation then 4 ticks counts exactly once (a held input is not extra events); active only at the target; the contact powers the Powered gate only when supplied and Reached; the activation output starts a downstream hold timer exactly once; 20 extra activations change nothing; Reset gives count 0, Counting, inactive, authored target. Counter→hold timer has two compatible pairs, so no suggestion. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L39-L88` | carry forward |
| K12 | Physical integration: ball detector → counter → lamp with 2 or 3 balls crossing the detector: 3 crossings reach the target and light the lamp, 2 do not; replay after Reset is identical, either id order. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L89-L152` | carry forward |
| K13 | Save/load keeps the authored target and never persists a partial run count. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L153-L171` | carry forward |
| K14 | Targets 0, −1, 10, 1.5, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L172-L189` | carry forward |
| K15 | Every target 1–9 has exactly that many lamps; lamp i is keyed to threshold i + 1; a live (uncommitted) count does not light a lamp; after one tick the newest lamp is half-way at 0.05 s and fully gold at 0.1 s; supply does not affect the lamps. | `CuriousContraptions.tests/CounterAnimationTests.cs@a6c914e:L20-L60` | carry forward |
| K16 | The contact follows the committed counter state, not the part's transient active flag, and reads open again after Reset. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L38-L83` | carry forward |
| K17 | Diagnostic wire names `counting`/`reached`. | `CuriousContraptions.tests/CounterDiagnosticTests.cs@a6c914e:L7-L21` | carry forward at the diagnostics boundary only |
| K18 | Counter publication/rollback/allocation tests. | `CuriousContraptions.tests/CommittedCounterTests.cs@a6c914e:L11-L173` | do not carry forward (CPU transaction/publication mechanism; allocation proof belongs to the Epic 16 gate) |
| K19 | Catalogue entry and scene. | `parts/catalog/counter.tres@a6c914e:L1-L14` and `parts/scenes/counter.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `counter` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/CounterPart.cs`
- `parts/catalog/counter.tres`
- `parts/scenes/counter.tscn`
- `engine/SceneCounterDeclaration.cs`
- `engine/SimulationCounters.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `CuriousContraptions.tests/SimulationCountersTests.cs`
- `CuriousContraptions.tests/CounterAnimationTests.cs`
- `CuriousContraptions.tests/CounterDiagnosticTests.cs`
- `CuriousContraptions.tests/CommittedCounterTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`

## 5. Acceptance outline

Authority: [CAT-020](../requirements.md#current-cat-020), [todo-269](../requirements.md#todo-269); Story 9.2 adds `tools/e2e/cat-020.test.ts`.

- **Construction (Chrome UI).** Place a Ball detector (CAT-002, Story 6.8), Counter (target 3, the default) and Signal lamp, with balls that roll through the detector; Connect detector→counter `ActivationIn` and counter `ActivationOut`→lamp; pick the activation choice where the drawer offers trigger/power icons. Do not use a Switch as the source: it emits once per world (section 3).
- **Positive.** Three distinct balls cross the detector: the third crossing emits once and lights the lamp; three gold dots (K12).
- **Negative / controls.** Two crossings do not emit (K12); a held/resting trigger does not count every tick (K11); extra triggers after the target do not re-emit.
- **Boundaries.** Targets 1 and 9; invalid targets rejected (K14).
- **Run/Reset.** Reset clears the count to zero and the dots to slate (Story 9.2 AC; K11).
- **Save/Load.** `target_count` round-trips; partial counts are not saved (K13).
- **Integrations.** Supplied contact closes at the target and stays closed until Reset (needs Battery and a consumer); Clock→Counter (CAT-017 C17).

## 6. Open questions

1. **Supplied contact in Story 9.2.** The requirement and legacy include a supplied contact that latches until Reset; the supply network (Story 8.1) now precedes Story 9.2. **Resolved by delivery:** the contact mode shipped with the part. The package declares a `SupplyContactDescriptor` closed while the count node is `Latched` (`elements/Counter/CounterElementDeclaration.cs:88–93`; `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach`).
2. **Separately addressed reset input (EL-183).** EL-183 requires a reset input; the legacy counter is cleared only by Workshop Reset and has no `ResetIn` socket. **Resolved (D8 and D19 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); batch H readiness):** EL-183 is a separate element with its own package and a `ResetIn` socket. This Counter keeps its four sockets and is cleared only by Workshop Reset. The pulse-count node has no Reset terminal today (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:19`), so EL-183 waits for an engine slice.
3. **f32 activation and cosmetic lanes (resolved).** Story 9A.44 migrated the activation network and cosmetic curves the counter extends to f32; no migration choice remains.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/Counter`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/Counter`, id `counter`, category Control, palette order 22 (`elements/Counter/CounterElementDeclaration.cs:43–48`). No slice is planned for this identity. [EL-183](EL-183-resettable-counter.md) is a separate element, not a duplicate. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; repair 3 is part of visible-solid repair part D, which also runs after it (coordinator decision 33).

**Residuals.**
- **Count past the target.** Decided; see repairs 1 and 2 in section 3: `ENGINE-SIGNAL-NODES` (saturating-count row). It is no longer an owner item.
- **Floating foot, face panel and sockets.** Required scene-data repair, [repair 3](#repair-3-seated-foot-face-panel-and-sockets) in section 3; no engine capability, owned by repair part D after the engine programme (coordinator decision 33).
- **Required forward-only refactors.** Rename `ActivationNodeKind.Counter` to `PulseCount` (and `StateLatch` to `Memory`), owned by `ENGINE-CAPABILITY-INSTANCES` (audit E1); remove the unread `ElectricalNodes` ratings (12 V, 0.05 Ω, 100 W, `CounterElementDeclaration.cs:95–98`, only checked finite at `CuriousContraptions.Bundle/BundleValidator.cs:873–876`). The deferred-work text for the ratings is in batch B readiness.
- Requirement row CAT-020 stays unchecked until the post-catalogue browser phase.

**Integration.**
- Delivered collaborators: Ball detector (three balls, `MemoryGuardTests`), System clock (`ClockGuardTests`), Signal lamp, Battery and Powered gate.
- [Current consumers](../invest/current-consumers.md#cat-020-i) names an Impact switch sending three distinct triggers. One switch cannot (section 3, Dependencies); the recipe should use three balls through a Ball detector. The replacement text is in batch B readiness.
- Campaign: no level in `content/puzzles.json` places it.
- Coverage binding: catalogue key `counter` in `docs/coverage/catalogue-elements.json`.
