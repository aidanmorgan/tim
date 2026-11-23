# CAT-037 · latch — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-037 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-037-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-037](../requirements.md#current-cat-037). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid-sweep`](../../../TODO.md) the parts that stand proud of the housing collide with their drawn primitives, after the housing and foot boxes: the cream face panel as a 1.4 × 1.02 × 0.04 box at z 0.35, the indicator lamp as a sphere r 0.09 at (0, −0.33, 0.42), and the five sockets as spheres r 0.075 at their ports, (∓0.45, 0.72, 0), (∓0.93, 0, 0) and (0, −0.56, 0.38) (`elements/StateLatch/latch.tscn`; `VisibleSolidColliderTests` checks them against the art and drops a ball onto the indicator and the SetIn socket; `StateLatchTests`). The rocker turns with the latch's state and does not collide. Placement bounds every collider (`PlacementPose`), so the sockets now count toward the bench edge: the latch reaches x ±1.005 against the foot's ±0.9, y +0.795 (the set and reset sockets) against the housing's +0.625, and z +0.51 (the indicator lamp) against the foot's +0.425. The static markings are flat printing at z 0.37025, 0.25 mm above the cream face, without colliders. Original symbol layout and palette remain; native surface/shape proof does not claim post-catalogue browser legibility.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-037 · `latch` ("Set/Reset latch") |
| Requirement anchor | [current-cat-037](../requirements.md#current-cat-037); retained behaviour [todo-267](../requirements.md#todo-267) |
| Mapped identities | none owned. Related: [EL-109 Speed-sensitive trapdoor](../invest/named-elements.md#element-109) mentions a "supplied latch" as part of its own mechanism; its requirement row stands as the source. |
| Roadmap story | 9.4 "State Latch Bistable Memory" (Epic 9) Current [CAT-037 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/StateLatch`, id `latch`; Story 9.4 done, generic instance by C-Memory (native Pass `f5ffdb`), supply fixture by Story 9.4c (`a598a09e`); browser qualification pending post-catalogue. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are shared contracts owned by `ENGINE-CAPABILITY-INSTANCES`: the product-named node kind `StateLatch` (audit E1) and the per-product socket glyphs `Set` and `Reset` (audit E3). |

## 2. Declaration

The delivered declaration is `elements/StateLatch/StateLatchElementDeclaration.cs` with the scene `elements/StateLatch/latch.tscn` (read on `8d1d68aa`). It implements `ISignalElement`, `ISupplyElement` and `ISceneGeometryElement` (`:9`). The legacy sources of the carried values are in section 4.

**Body and material.** One static body with no mass and no joint (`:30`). The rocker is art, not a body. Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:47`), all f32.

**Colliders (physics graph).** Nine, baked in scene order from hidden proxies. Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 1.65 × 1.25 × 0.65 | origin |
| 1 | box | 1.8 × 0.16 × 0.85 | (0, −0.72, 0) |
| 2 | box | 1.4 × 1.02 × 0.04 | (0, 0, 0.35) |
| 3 | sphere | r 0.09 | (0, −0.33, 0.42) |
| 4–8 | sphere | r 0.075 | the five ports: (−0.45, 0.72, 0), (0.45, 0.72, 0), (−0.93, 0, 0), (0.93, 0, 0), (0, −0.56, 0.38) |

Placement bounds every collider, so the latch reaches x ±1.005, y +0.795 and z +0.51 at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Housing` | box 1.65 × 1.25 × 0.65 m, teal `#66b8c9` | solid, collider 0 | nothing |
| `Visual/Foot` | box 1.8 × 0.16 × 0.85 m, navy `#293954` | solid, collider 1 | nothing |
| `Visual/Face` | box 1.4 × 1.02 × 0.04 m, cream `#fff8e9` | solid, collider 2 | nothing |
| `Visual/IndicatorLamp` | sphere r 0.09 m, slate to gold | solid, collider 3 | nothing (colour binding only) |
| `Visual/Visual_Socket_SetIn`, `_ResetIn`, `_PowerIn`, `_Supply`, `_ActivationOut` | spheres r 0.075 m, gold | solid, colliders 4–8 | nothing |
| `Visual/SetMarker` | `QuadMesh` 0.055 × 0.20 m at (−0.45, 0.34, 0.37025), navy, filled | decorative: printed flat, 0.25 mm in front of the face | nothing |
| `Visual/ResetMarker/Bead_0`–`11` | twelve discs (`printed-disc.obj`) on a ring of radius 0.1 m centred (0.45, 0.34, 0.37025), navy, hollow | decorative: printed flat | nothing |
| `Visual/OutMarker/Chevron_L`, `Chevron_R` | `QuadMesh` 0.1 × 0.03 m strokes at (0, −0.465, 0.37025), a downward chevron | decorative: printed flat | nothing |
| `Visual/Rocker` | box 0.65 × 0.28 × 0.12 m at (0, 0, 0.44), navy | decorative: moved by a binding. It reaches z 0.50, 13 cm proud of the face, and takes no collider, so a ball passes through it | the rocker binding (animation graph) |

Selection radius 1.2 (`:11`). Icon `elements/StateLatch/icon.svg`. At a glance it is the teal case with a tilting rocker; the filled bar and the hollow ring tell Set from Reset without text or colour.

**Animation (animation graph).** Four presentation bindings (`:13–16`). They read the committed memory of node `signal` and write nothing back.

| Target | Property | Driver | Values |
|---|---|---|---|
| `Visual/Rocker` | rotation about Z | `CommittedActivation` | −0.25 rad (Off) to 0.25 rad (On), SmoothStep 0.125 s |
| `Visual/IndicatorLamp` | albedo red, green, blue | `CommittedActivation` | slate to gold, SmoothStep 0.125 s |

**Ports** (`:35–39`), in declared order.

| `PortId` | Domain | Direction | Role | Signal kind | Glyph | Position (m) | Facing |
|---|---|---|---|---|---|---|---|
| `SetIn` | Signal | Input | SetIn | Pulse | filled bar | (−0.45, 0.72, 0) | +Y |
| `ResetIn` | Signal | Input | ResetIn | Pulse | hollow ring | (0.45, 0.72, 0) | +Y |
| `PowerIn` | Electrical | Input | PowerIn | — | text | (−0.93, 0, 0) | −X |
| `ActivationOut` | Signal | Output | ActivationOut | Level | text | (0, −0.56, 0.38) | +Z |
| `Supply` | Electrical | Output | Supply | — | text | (0.93, 0, 0) | +X |

**Parameters and stores.** None (`:28`). The memory is one committed bit that survives supply loss.

**Signal node, supply node and event.** One signal node `signal`, a `SignalMemoryDescriptor` (`:57`), with `SetIn` on its Set terminal, `ResetIn` on its Reset terminal and `ActivationOut` on its output (`:59–64`). One supply node `contact`, a `SupplyContactDescriptor` closed while `signal` is `Latched`, between `PowerIn` and `Supply` (`:66–71`). Event `MemoryChanged` (`:51`), one record per change. `ElectricalNodes` holds one `Switch` row (`:73–76`); its ratings are unread (section 7). The bundle validator refuses any input other than the two Pulse inputs, a missing Level `ActivationOut`, a supply that is not a switched contact, a moving frame, and a second node-selecting capability.

**Compiled counts per instance.** 1 static body; 9 colliders (3 boxes, 6 spheres); 0 joints; 0 drives; 0 sensors; 1 signal node with one committed memory row; 1 supply node; 0 stores; 5 ports; 4 bindings. All are fixed at compile.

**Catalogue.** Id `latch`, title "Set/Reset latch", category Control, palette order 24, colour (0.4, 0.72, 0.79), description "Set closes the electrical contact; Reset opens it. Reset wins simultaneous triggers. Remembers its state without power, but needs a connected supply to power another device." (`:20–28`). Coverage key `latch`.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand.

**Generic laws.** The package holds no script, callback, update loop or state machine.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from it | rigid contact with the product pair law for restitution, and friction | nine colliders; the material |
| Set turns it On, Reset turns it Off, a same-tick pair resolves Off, and it settles at the next boundary | signal network memory rule: pulses on a node's Set and Reset terminals in one tick settle together at the next boundary, Reset dominant; the bit persists with no input | the terminal bindings |
| Its output is a level only a level indicator accepts | signal network level rule: a Level output connects only to a level input (`LevelDrivesLevelInputOnly`) | `PortSignalKind.Level` |
| The contact is closed while On; memory survives supply loss | supply network contact rule: a contact is closed while its controlling signal node is in the declared state | `SignalContactState.Latched` |
| Rocker and lamp | presentation bindings on committed memory | the four bindings |

**Capability probe.** 7 present, 0 untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Set/Reset memory, Reset dominant, settled at the next boundary | present | `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`) | `StateLatchTests.Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff`; `StateLatchTests.Network_SetThenResetOnAdjacentTicks_TurnsOnThenOff`; `MemoryGuardTests.OneSourceOnBothInputsAlwaysResolvesToReset` |
| Level output that only a level indicator accepts | present | `PortSignalKind.Level` | `StateLatchTests.Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes`; `MemoryGuardTests.EveryOccurrenceConsumerRefusesTheLevelOutput` |
| Supplied contact; memory survives supply loss | present | `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`) | `LatchSupplyFixtureTests.SupplyOffOnKeepsMemoryAndRepowersWithoutNewSet`; `LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange` |
| A failed or discarded tick keeps the pending request | present | committed memory row | `StateLatchTests.Network_FailedOrDiscardedTickNeverCommitsAPendingSet` |
| Wrong-kind connection refusal | present | typed sockets | `StateLatchTests.Network_RejectsUntypedOrForeignLatchEdges`; `StateLatchTests.Connections_AdmitOneSourceOnBothInputsAndRejectForeignSockets` |
| Reset, save and load | present | `WorkshopSaveCodec` | `LatchSupplyFixtureTests.RunResetRestoresLatchOffBladeClosedAuthoredBattery`; `StateLatchTests.Persistence_BinaryFlatBuffersAndSaveCodecPreserveDistinctSockets` |
| Rocker and lamp presentation | present | `PresentationDriver.CommittedActivation` | `StateLatchTests.PartRegistry_DiscoversLatchAndPresentationRestoresRestOnReset`; `StateLatchTests.Network_LevelIndicatorMirrorsLatchOnAndOff` |

**Conformance (owner addendum, 22 November 2026): conforms with fixes.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5).
- Naming residues, both required forward-only refactors (section 7): the generic memory node kind is called `ActivationNodeKind.StateLatch` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`; rename owned by `ENGINE-CAPABILITY-INSTANCES`, audit E1), and the generic level rule's refusal message names a product, "A level output can only light a Signal lamp." (`engine/gpu/WorkshopConnections.cs:150`); `ENGINE-CAPABILITY-INSTANCES` rewords it to "A level output connects only to an input that admits a level." (audit E4; the one wording, as CAT-035 states).
- Three layers: hold. Every static proud solid has its own collider; the markers are flat prints; the rocker is moved by a binding and is decorative; the lamp's binding changes colour only.
- One-way flow, typing and flat state: hold, with the counts in section 2.
- The unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Nine colliders against the two of the original housing has one reason: visible = solid. The face, the lamp and the five sockets are each drawn standing clear of the housing, so each has its own primitive. No collider is cosmetic, and the rocker, which is cosmetic, has none. The cost for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The memory costs one node row per tick. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** Pulse sources for Set and Reset (Impact switch); Battery and a supplied consumer (Powered gate); a Signal lamp for the level. All are delivered. The latch is also the supply-loss fixture for CAT-005, CAT-013 and CAT-051.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| T1 | Reset-dominant memory contact; commands settle at the next tick boundary so every delivery in a tick participates before the electrical solve. | `parts/LatchPart.cs@a6c914e:L8-L9` | carry forward |
| T2 | One memory slot; phase `Off`/`On`; reads `Off` before Run. | `parts/LatchPart.cs@a6c914e:L12-L15` | carry forward |
| T3 | Rocker and lamp bindings as in section 2. | `parts/LatchPart.cs@a6c914e:L16-L26` | carry forward |
| T4 | Ports with per-socket commands and the contact route, as in section 2. | `parts/LatchPart.cs@a6c914e:L27-L36` | carry forward |
| T5 | Set → Set request, Reset → Reset request, anything else rejects ("Latch requires an explicit Set or Reset input"); commands never act immediately. | `parts/LatchPart.cs@a6c914e:L37-L47` | carry forward the rule; do not carry forward the per-part `HandleActivation` callback |
| T6 | The part is active while On and records an `Activated` event. | `parts/LatchPart.cs@a6c914e:L48-L52` | carry forward the state; do not carry forward the per-part `BeforeNetworks` update loop |
| T7 | Geometry and art as in section 2. | `parts/LatchPart.cs@a6c914e:L53-L70` | carry forward |
| T8 | Requests are a flag set {None, Set, Reset}; state = (phase, current requests, next requests). | `engine/SimulationLatches.cs@a6c914e:L16-L21` | carry forward as typed state |
| T9 | Submit: a delivery on the current boundary tick goes to the current bucket, on the next tick to the next bucket; earlier or later deliveries reject. | `engine/SimulationLatches.cs@a6c914e:L56-L72` | carry forward |
| T10 | Advance (consecutive ticks only): Reset in the current bucket → Off; else Set → On; else keep; the next bucket becomes current. | `engine/SimulationLatches.cs@a6c914e:L73-L85` | carry forward |
| T11 | Set then Reset (or Reset then Set) delivered on the same tick, on either side of the boundary, settle Off; memory then persists for many ticks. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L5-L23` | carry forward |
| T12 | Commands on adjacent ticks stay separate (Set at t then Reset at t + 1 gives On then Off); duplicate Sets are harmless; memory persists with no commands for 97 ticks. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L25-L43` | carry forward |
| T13 | Unknown command, unknown id, negative or non-adjacent delivery ticks and non-consecutive advances reject without changing state. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L68-L91` | carry forward |
| T14 | The supplied load is a Powered gate (`powered_gate`, CAT-051; `CuriousContraptions.tests/LatchTests.cs@a6c914e:L26-L26`). With and without supply, either entity order: Set reads Off after the command, Off after one step, On after the second step; the Powered gate is powered only with supply; supply off/on keeps memory and repowers the gate; same-tick Set+Reset in either order ends Off; Reset clears a pending Set; activating the latch with a generic command is rejected. Switch→latch has two compatible pairs (Set, Reset), so no suggestion. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L10-L71` | carry forward |
| T15 | One source wired to both `SetIn` and `ResetIn` delivers both commands (no per-part deduplication), so the result is Off. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L73-L88` | carry forward |
| T16 | Commands before Run are rejected; a pending Set is part of restorable state; `Trigger` is rejected; Workshop Reset gives Off. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L90-L120` | carry forward |
| T17 | Rocker and lamp follow only committed memory: at 0.0625 s the rocker has turned 0.25 rad and the lamp is half-way; a failed tick leaves them unchanged; supplied or not. | `CuriousContraptions.tests/LatchAnimationTests.cs@a6c914e:L35-L70` | carry forward behaviour; do not carry forward the CPU failure-injection mechanism |
| T18 | The contact follows committed memory, not the transient active flag; a delivered Set reads open until the following boundary; reads open after Reset. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L38-L83` | carry forward |
| T19 | Diagnostic wire names: phase `off`/`on`; requests `none`, `set`, `reset`, `set_and_reset`. | `CuriousContraptions.tests/LatchDiagnosticTests.cs@a6c914e:L7-L38` | carry forward at the diagnostics boundary only |
| T20 | Declaration requires owner and slot. | `engine/SceneLatchDeclaration.cs@a6c914e:L5-L14` | carry forward |
| T21 | Latch used as the supply-switching fixture (battery → latch contact; switches on Set and Reset). | `CuriousContraptions.tests/SupplyControl.cs@a6c914e:L22-L43` | carry forward as a construction recipe |
| T22 | Catalogue entry and scene. | `parts/catalog/latch.tres@a6c914e:L1-L13` and `parts/scenes/latch.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `latch` only as a catalogue id: no element knowledge. `engine/SceneLatchedSpringDeclaration.cs` and `engine/physics/PhysicsLatchedSpring.cs` belong to the wound spring (CAT-071), not this element.

### Files harvested

- `parts/LatchPart.cs`
- `parts/catalog/latch.tres`
- `parts/scenes/latch.tscn`
- `engine/SceneLatchDeclaration.cs`
- `engine/SimulationLatches.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/LatchTests.cs`
- `CuriousContraptions.tests/SimulationLatchesTests.cs`
- `CuriousContraptions.tests/LatchAnimationTests.cs`
- `CuriousContraptions.tests/LatchDiagnosticTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`
- `CuriousContraptions.tests/SupplyControl.cs`

## 5. Acceptance outline

Authority: [CAT-037](../requirements.md#current-cat-037), [todo-267](../requirements.md#todo-267); Story 9.4 adds `tools/e2e/cat-037.test.ts`.

- **Construction (Chrome UI).** Place two Switches, Latch, Battery and a supplied consumer; Connect each switch to the latch choosing the filled-bar (Set) or ring (Reset) icon (T14); battery→latch `PowerIn`; latch `Supply`→consumer.
- **Positive.** A Set impact turns the memory On at the following boundary and it stays On indefinitely; rocker and lamp follow.
- **Negative / controls.** Reset impact turns it Off; simultaneous Set and Reset resolve Off (Reset dominance; T11, T15); memory On with supply absent leaves the consumer off, and restoring supply repowers it without a new command (T14).
- **Boundaries.** Commands on adjacent ticks stay separate (T12); a duplicate Set has no extra effect; generic trigger refused (T16).
- **Run/Reset.** Workshop Reset clears memory and any pending command (T14, T16).
- **Save/Load.** Both typed command links round-trip with exact socket names `set_in`/`reset_in`.
- **Integrations.** Used as the supply-loss control fixture for CAT-005/013/020/033/052 (T21).

## 6. Open questions

1. **Output domain in Story 9.4.** The Story 9.4 AC says "the latch output transitions to high", but the legacy and the requirement give the latch only a supplied electrical contact (no activation output); the supply network (Story 8.1) now precedes Story 9.4. Whether 9.4 proves the output through the Story 8.1 supplied contact, or adds an output the requirement does not define, was the question. **Decided 18 Oct 2026 (owner): supplied contact plus ActivationOut.** The latch keeps its supplied `PowerIn`→`Supply` contact and gains an Activation output that is high while memory is On and low while Off (Story 9.4; `spec-9-4-state-latch.md` D13).
2. **f32 activation and cosmetic lanes (resolved).** At a6c914e the activation network and cosmetic curves the latch extends held half-precision values (`docs/gpu-f32-physics.md@a6c914e:L96-L98`), and whether Story 9.4 migrated them to f32 first was an owner decision. **Decided 18 Oct 2026 (owner): moved to a separate Epic 9A story;** Story 9.4 extends the existing lanes and adds no new half-precision API. Story 9A.44 migrated those lanes to f32; no migration choice remains.

### Decided 18 Oct 2026 by coordinator under owner delegation (playability)

- **ActivationOut drives Signal lamps only, as a level.** A lamp wired from a latch lights while the latch is On and goes dark when it is Off; occurrence consumers (Counter, Delay, another latch) reject the link with the typed refusal `LevelDrivesLevelInputOnly`, worded "A level output connects only to an input that admits a level." once `ENGINE-CAPABILITY-INSTANCES` rewords today's text (Story 9R.5: one rule on the declared output kind, shared with the Pressure Plate's level output) — rationale: a level shows remembered state at a glance, and edge-triggered semantics for occurrence parts are not defined.
- **A lamp following a latch (any level output) takes no other signal** ("A lamp that follows a level output takes no other signal."). — rationale: one cause per indicator keeps cause and effect readable.
- **ActivationOut socket at (0, −0.56, 0.38) on the lower front, gold sphere with a small navy downward chevron above it.** — rationale: output reads as "below the memory", away from the top Set/Reset controls and the side power sockets; no text or colour coding needed.
- **Icon-only socket choices are a filled navy printed bar (Set) and a hollow navy ring (Reset), matching the on-part marks.** — rationale: the drawer choice and the part speak the same visual language.
- **Every Signal lamp's presentation follows its committed value (one Memory cosmetic path).** A sticky lamp still lights once and stays lit. — rationale: one presentation path for all lamps; no visible change for existing levels.
- **Drawer text choices read "Supply to PowerIn" instead of using "→".** — rationale: the UI font has no arrow glyph, which rendered as a missing-glyph box.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/StateLatch`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/StateLatch`, id `latch`, display name "Set/Reset latch", category Control, palette order 24 (`elements/StateLatch/StateLatchElementDeclaration.cs:20–25`). No slice is planned for this identity. It owns no named identity. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Integration.**
- Delivered collaborators: Impact switch (Set and Reset), Battery, Powered gate, Both gate (`BothGateTests.CommittedLatchSetResetAndSupplyRestorationDriveBothGate`) and Signal lamp (level). It is the supply-loss fixture for CAT-005, CAT-013 and CAT-051 (`LatchSupplyFixtureTests`).
- [Current consumers](../invest/current-consumers.md#cat-037-i) names the Motor as the first visible consumer. As decided in [CAT-013 §7](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026), the native consumer stays the Powered gate and the Motor recipe is kept for the browser phase.
- Campaign: no level in `content/puzzles.json` places it.
- Coverage binding: catalogue key `latch` in `docs/coverage/catalogue-elements.json`.

**Residuals.**
- **Required forward-only refactors.** Rename `ActivationNodeKind.StateLatch` to `Memory` (and `Counter` to `PulseCount`), and replace the shared `Set`/`Reset` socket-glyph enum members with glyph assets the package ships; both are owned by `ENGINE-CAPABILITY-INSTANCES` (audit E1, E3). Reword the level rule's refusal to "A level output connects only to an input that admits a level." (`ENGINE-CAPABILITY-INSTANCES`, audit E4; one wording with CAT-035 and CAT-052). Remove the unread `ElectricalNodes` ratings (12 V, 0.05 Ω, 100 W, `StateLatchElementDeclaration.cs:73–76`, only checked finite at `CuriousContraptions.Bundle/BundleValidator.cs:873–876`). The deferred-work text for the refusal wording and the ratings is in batch B readiness.
- Requirement row CAT-037 stays unchecked until the post-catalogue browser phase.
