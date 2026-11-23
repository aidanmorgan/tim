# CAT-013 · both_gate — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-013 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-013-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-013](../requirements.md#current-cat-013). This spec holds the **facts shared by all five supplied electrical gates** (one declaration, five operations); [CAT-024](CAT-024-electrical_nand.md), [CAT-025](CAT-025-electrical_nor.md), [CAT-026](CAT-026-electrical_or.md) and [CAT-027](CAT-027-electrical_xor.md) add only their operation-specific facts. Network-wide facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid-sweep`](../../../TODO.md) the parts that stand proud of the body collide with their drawn primitives, after the body and foot boxes, on all five supplied gates: the cream face panel as a 1.45 × 1.05 × 0.04 box at z 0.35 (its top at 0.37, 4.5 cm above the body), the three lamps as spheres r 0.09 at (−0.48, ±0.3, 0.42) and (0.48, 0, 0.42), and the four port sockets as spheres r 0.075 at (−0.94, ±0.35, 0), (0, −0.75, 0.55) and (0.94, 0, 0) (`elements/BothGate/both_gate.tscn`; `VisibleSolidColliderTests` checks them against the art, rolls a ball into an upright Both gate's face panel and drops one onto it). Static restitution stays 1. Placement bounds every collider (`PlacementPose`), so the sockets and lamps now count toward the bench edge: a gate reaches x ±1.015 (the side sockets) against the foot's ±0.925, and z +0.625 (the power socket) against the foot's +0.425, as `PlacementPoseTests.ProudSocketsCountTowardTheBenchEdge` shows for the Counter and the Pressure plate.

**Face printing:** the paths and one/two input marks, plus the other supplied gates’ operation pictograms and truth dots, are flat native surfaces at z 0.37025 (0.25 mm above the cream face to avoid z-fighting). They have no solid relief or colliders. The original XY layout, navy strokes and gold truth rows remain. Native instantiated-triangle checks cover orientation, flatness and symbol gaps; browser legibility remains pending post-catalogue. No sphere-admission change is required.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-013 · `both_gate` (operation And) |
| Requirement anchor | [current-cat-013](../requirements.md#current-cat-013); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-133 Electrical AND gate](../invest/named-elements.md#element-133). No TH/RAD/GAP identity. |
| Roadmap story | 9.6 "Dual-Supply Both Gate" (Epic 9) Current [CAT-013 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/BothGate`, id `both_gate`; Story 9.6, scoped native and publication Pass `e036b7`; browser qualification pending post-catalogue. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice, the EL-133 to EL-137 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the gates' engine gaps are owned by `ENGINE-SIGNAL-NODES` (its self-edge rule row) and `ENGINE-CAPABILITY-INSTANCES` (the two-input boolean node and the per-product socket glyphs, audit E2 and E3). |

## 2. Declaration

The delivered declaration is `elements/BothGate/BothGateElementDeclaration.cs` with the scene `elements/BothGate/both_gate.tscn` (read on `8d1d68aa`). It implements `ISupplyElement` and `ISceneGeometryElement` (`:9`). This section is also the declaration of the four sibling gates, which differ from it only in the values listed under "Family delta". The legacy sources of the carried values are in section 4.

**Body and material.** One static body with no mass and no joint (`:21`). Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:31`), all f32.

**Colliders (physics graph).** Ten, baked in this order from the hidden scene proxies `col_Body` to `col_PortOutput` (`both_gate.tscn:140–188`). Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 1.7 × 1.3 × 0.65 | origin |
| 1 | box | 1.85 × 0.16 × 0.85 | (0, −0.75, 0) |
| 2 | box | 1.45 × 1.05 × 0.04 | (0, 0, 0.35) |
| 3–5 | sphere | r 0.09 | (−0.48, 0.3, 0.42), (−0.48, −0.3, 0.42), (0.48, 0, 0.42) |
| 6–9 | sphere | r 0.075 | (−0.94, 0.35, 0), (−0.94, −0.35, 0), (0, −0.75, 0.55), (0.94, 0, 0) |

Placement bounds every collider, so the gate reaches x ±1.015 and z +0.625 at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Body` | box 1.7 × 1.3 × 0.65 m, teal `#66b8c9` | solid, collider 0 | nothing |
| `Visual/Foot` | box 1.85 × 0.16 × 0.85 m, navy `#293954` | solid, collider 1 | nothing |
| `Visual/Face` | box 1.45 × 1.05 × 0.04 m, cream `#fff8e9` | solid, collider 2 | nothing |
| `Visual/FirstLamp`, `SecondLamp`, `OutputLamp` | spheres r 0.09 m, slate to gold | solid, colliders 3–5 | nothing (colour bindings only) |
| `Visual/PortFirst`, `PortSecond`, `PortPower`, `PortOutput` | spheres r 0.075 m, gold | solid, colliders 6–9 | nothing |
| `Visual/UpperPath`, `LowerPath` | `QuadMesh` 0.65 × 0.035 m at z 0.37025, turned ∓27° about Z, navy | decorative: printed flat, 0.25 mm in front of the face | nothing |
| `Visual/FirstMark`, `SecondMarkLeft`, `SecondMarkRight` | `QuadMesh` 0.025 × 0.08 m at z 0.37025, navy | decorative: printed flat | nothing |

Selection radius 1.1 (`:42`). Icon `elements/BothGate/icon.svg`. The five gates are told apart by shape, not colour: this one has two converging paths and no truth row; each sibling has its own pictogram and its own pattern of truth dots.

**Animation (animation graph).** Nine presentation bindings (`:44–52`): the red, green and blue albedo of each lamp follow `CommittedElectricalSocket` for `FirstIn`, `SecondIn` and `Supply`, slate (85, 101, 115)/255 to gold (247, 203, 82)/255, SmoothStep 0.1 s. They read committed supply reads and write nothing back.

**Ports** (`:25–28`), all Electrical.

| `PortId` | Direction | Role | Glyph | Position (m) | Facing |
|---|---|---|---|---|---|
| `FirstIn` | Input | FirstIn | one mark | (−0.94, 0.35, 0) | −X |
| `SecondIn` | Input | SecondIn | two marks | (−0.94, −0.35, 0) | −X |
| `PowerIn` | Input | PowerIn | power | (0, −0.75, 0.55) | +Z |
| `Supply` | Output | Supply | text | (0.94, 0, 0) | +X |

**Parameters, stores, events.** None (`:20`, `:32`). The operation is a fixed declared value, not a player setting.

**Supply node.** One node `logic`, a `SupplyLogicDescriptor` with `LogicGateOperation.And`; the four ports bind to its `FirstIn`, `SecondIn`, `PowerIn` and `Output` terminals (`:34–41`). `ElectricalNodes` holds one `LogicGate` row (`:33`); its ratings are unread (section 7).

**Compiled counts per instance.** 1 static body; 10 colliders (3 boxes, 7 spheres); 0 joints; 0 drives; 0 sensors; 0 signal nodes; 1 supply node; 0 stores; 4 ports; 9 bindings. All are fixed at compile.

**Catalogue.** Id `both_gate`, title "Both gate", category Control, palette order 26, colour (0.4, 0.72, 0.79), description "Both numbered controls must be powered to pass the separate bottom supply to the output. No supply means no output." (`:12–19`). Coverage key `both_gate`.

**Family delta.** `electrical_nand` (27, `Nand`), `electrical_nor` (28, `Nor`), `electrical_or` (29, `Or`), `electrical_xor` (30, `Xor`) change only id, name, description, palette order, icon path, scene path, the operation value and the printed pictogram and truth discs.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Generic laws.** The package holds no script, callback, update loop or state machine.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from its face, lamps and sockets | rigid contact with the product pair law for restitution, and friction | ten colliders; the material |
| The output is available only with supply and a true operation | supply network: a logic node passes its `PowerIn` route when its declared operation is true for its two committed condition inputs; energy comes only from `PowerIn` | `LogicGateOperation.And` |
| Loops | supply network cycle rule: a cycle of monotone nodes settles from unpowered each solve; a cycle holding a nonmonotone node is refused before any solve | the operation's monotonicity |
| Lamps | presentation bindings on committed socket reads | the nine bindings |

**Capability probe.** 6 present, 0 untested, 1 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Supplied two-input gate law | present | `SupplyLogicDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:41`); `Evaluate` (`engine/gpu/SupplyRoute.cs:389`) | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer`; `BothGateTests.MissingBottomWireNeverConvertsConditionsIntoEnergy` |
| Monotone cycles settle from unpowered; nonmonotone cycles are refused | present | `SupplyCycles.Monotone` (`engine/gpu/SupplyRoute.cs:59–61`) | `BothGateTests.ChainedReconvergenceAndMutualCyclesRetractWithoutOrderDelay`; `ElectricalNandTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve` |
| Output energy comes only from `PowerIn` | present | the same law | `BothGateTests.EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` |
| Committed socket lamps | present | `PresentationDriver.CommittedElectricalSocket` | `BothGatePresentationTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` |
| Proud parts collide; markings are flat | present | scene `col_*` nodes | `BothGateTests.EverySuppliedGateDeclaresItsBodyFootAndProudPartsInOrder`; `FaceMarkingTests.EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace` |
| Save, load and Reset | present | `WorkshopSaveCodec` | `BothGateTests.SeparateConditionsAndEnergyDriveActualConsumer` (decoded save equality, then Reset state, `BothGateTests.cs:64–81`) |
| A node's own output wired to its own input | narrower | `SupplyTopology.ValidateEdges` refuses every self-edge as `InvalidConnection` (`engine/gpu/SupplyRoute.cs:50–54`) before the cycle rule runs | Requirement CAT-013 admits And cycles. Owner: `ENGINE-SIGNAL-NODES`, its self-edge rule row (owner decision 3a; row text in batch B readiness, gap 3) |

**Conformance (owner addendum, 22 November 2026): conforms with one fix.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the five package ids, classes and namespaces found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5). The gate law is selected by the declared `LogicGateOperation`, a closed enum.
- Three layers: hold. Every static proud solid has its own collider; the markings are flat prints; the bindings change colour only.
- One-way flow, typing and flat state: hold, with the counts in section 2.
- Fix: the unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).
- Shared contracts the gates use, not the gates: the boolean supply node is fixed at two inputs (`FirstIn`, `SecondIn`) and five named operations (audit E2), and the socket glyphs `First` and `Second` are a per-product table in shared code (audit E3). Both refactors are owned by `ENGINE-CAPABILITY-INSTANCES` (an N-input boolean node with a declared input list and truth function; glyph assets shipped by the package).

**Performance fitness.** The body is static and is never integrated. Ten colliders against the two of the original housing has one reason: visible = solid. The face panel stands 4.5 cm proud of the body, and each lamp and socket is a drawn sphere standing clear of the housing, so each has its own primitive; merging them into a larger box would collide where nothing is drawn. No collider is cosmetic. The cost for many instances is a recorded engine gap, not measured: "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The logic node costs one row in the ordered supply solve per tick. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** Battery for supply and conditions; a condition switch (Impact switch supply mode, State latch, Hold timer or Pressure plate contact); a supplied consumer (Powered gate, Motor). All are delivered.

## 4. Legacy harvest

### Shared gate facts (all five operations)

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| G1 | Truth evaluation: And = a∧b, Or = a∨b, Xor = a≠b, Nor = ¬a∧¬b, Nand = ¬(a∧b); an undefined operation rejects. | `engine/LogicGate.cs@a6c914e:L8-L20` | carry forward |
| G2 | Explicit truth table rows (a, b) = 00, 01, 10, 11: And F F F T; Or F T T T; Xor F T T F; Nor T F F F; Nand T T T F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19` | carry forward (acceptance table) |
| G3 | A gate output is powered only when its supply node is powered AND the operation is true for the committed condition inputs. | `engine/BinaryCircuit.cs@a6c914e:L136-L138` | carry forward |
| G4 | Condition and supply must be three distinct nodes; undefined operation rejects. | `engine/BinaryCircuit.cs@a6c914e:L86-L96` | carry forward |
| G5 | Any cycle containing an Xor, Nor or Nand gate rejects at construction with the cycle's nodes; And/Or cycles are allowed and settle from the unpowered least fixed point. | `engine/BinaryCircuit.cs@a6c914e:L104-L112` | carry forward (delivered for all five gates once Story 9.8b adds Xor) |
| G6 | Rejected feedback message: "Break the wire loop through <part ids>. XOR, NOR and NAND outputs cannot feed their own inputs." | `engine/ElectricalNetwork.cs@a6c914e:L116-L122` | carry forward the content; DESIGN keeps it in build mode on the bottom status line |
| G7 | All 5 × 4 truth rows × supply on/off × id order: the supplied load — a Powered gate (`powered_gate`, CAT-051) in the legacy tests — is powered iff supply ∧ truth; reconnecting links in reverse order gives the same result; removing every battery link leaves the load unpowered. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62`, `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15` and `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L5-L21` | carry forward |
| G8 | Catalogue map used by all gate tests: And→`both_gate`, Or→`electrical_or`, Xor→`electrical_xor`, Nor→`electrical_nor`, Nand→`electrical_nand`. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L14-L21` | carry forward as a typed mapping |
| G9 | Ports, gate declaration, art, indicators as in section 2. | `parts/ElectricalLogicPart.cs@a6c914e:L22-L36` and `parts/ElectricalLogicPart.cs@a6c914e:L42-L95` | carry forward |
| G10 | Input state is a closed enum `Neither, FirstOnly, SecondOnly, Both` from committed availability. | `parts/ElectricalLogicPart.cs@a6c914e:L8-L8` and `parts/ElectricalLogicPart.cs@a6c914e:L18-L21` | carry forward |
| G11 | Owner active = truth ∧ PowerIn supplied; a `Powered` machine event is recorded the first tick it is active. | `parts/ElectricalLogicPart.cs@a6c914e:L37-L41` | carry forward the state; do not carry forward the per-part `PreparePhysics` update loop (the network commits it) |
| G12 | Operation is a Godot `[Export]` validated as a defined enum. | `parts/ElectricalLogicPart.cs@a6c914e:L13-L17` | carry forward the closed enum; do not carry forward the scene-export mechanism |
| G13 | Non-And gates show their toolbox pictogram (0.65 × 0.65 unshaded quad at z 0.41) and gold `#e8b764` truth-row dots r 0.035 at (−0.21 + 0.14·row, −0.4, 0.42) for each true row (row = 2a + b). | `parts/ElectricalLogicPart.cs@a6c914e:L55-L71` | carry forward |
| G14 | Lamps follow committed availability and output; a failed tick publishes nothing new; pause/hide/Reset/save restore the slate baseline; three electrical input reads exist per gate. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour; do not carry forward the CPU transaction/publication mechanism |
| G15 | An undefined input socket in an animation signal rejects at binding, before Run starts. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L157-L170` | carry forward |

### Both gate (And) specific

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| A1 | Battery→gate has three compatible pairs (FirstIn, SecondIn, PowerIn), so no suggestion is made and the player must choose. | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L30-L31` | carry forward |
| A2 | Two chained Both gates feeding a Powered gate load (`powered_gate`, CAT-051): output equals a∧b with no order delay in either id order; supply off (through the latch supply fixture) clears state to `Neither` and output; supply on restores; Reset gives `Neither`, inactive. | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L9-L55` | carry forward |
| A3 | Two Both gates feeding each other on all three inputs cannot retain power after the real source is removed (10 further ticks). | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L57-L82` | carry forward |
| A4 | And/Or monotone feedback is the least fixed point: dropping the external condition or opening the contact turns the loop off. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L23-L39` and `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157` | carry forward |
| A5 | And variant art: converging navy path bars instead of a pictogram. | `parts/ElectricalLogicPart.cs@a6c914e:L48-L54` | carry forward |
| A6 | Catalogue entry and scene (no Operation override). | `parts/catalog/both_gate.tres@a6c914e:L1-L11` and `parts/scenes/both_gate.tscn@a6c914e:L1-L4` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `both_gate` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/both_gate.tres`
- `parts/scenes/both_gate.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `engine/ElectricalNetwork.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/BothGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-013](../requirements.md#current-cat-013), [todo-362](../requirements.md#todo-362); Story 9.6 adds `tools/e2e/cat-013.test.ts`.

- **Construction (Chrome UI).** Place Battery(s), Both gate and the consumer; Connect battery→gate and pick the bottom socket from the one-mark/two-mark/supply icon choice (A1); wire the two conditions; Connect gate→consumer.
- **Positive.** Supply plus both conditions powers the consumer; output lamp gold.
- **Negative / controls.** Each other truth row (00, 01, 10); both conditions lit with supply absent leaves the output lamp slate and the consumer unpowered ([DESIGN.md](../../../DESIGN.md#supplied-electrical-logic) positive/negative pair); source-free And loop (A3).
- **Boundaries.** Reordered placement and wiring (A2, G7); reconvergence through a second gate; supply loss during Run through the latch supply fixture retracts the output on the next committed tick (A2; [CAT-005 N19](CAT-005-battery.md#4-legacy-harvest)).
- **Run/Reset; Save/Load.** Reset returns lamps to slate and state to `Neither`; the three wires and their exact socket names round-trip.
- **Integrations.** Chained gates; supply-loss control via the Latch (CAT-037) supply fixture.

## 6. Current delivery decisions (22 October 2026)

1. Powered Gate is the actual supplied consumer for Story9.6; Signal lamp remains activation-only. Named Motor integration remains pending at Motor delivery.
2. Separate batteries drive FirstIn/SecondIn; current declared Switch/Latch/Hold contacts may control power. Native proof includes Latch Set/Reset and source restoration plus Hold opening a mutual Both Gate loop.
3. Current shared presentation uses f32 and the existing 0.1s SmoothStep law. Common physical numeric migration remains9A.44; this slice does not claim it complete.
4. Topology compiles once per Run. Disconnection means current source/contact loss or editing between Runs, not mid-Run cable mutation. Duplicate identical edges reject; distinct source fan-in on typed gate inputs remains legal for seeded cycles.
5. And is the admitted operation in this slice; later operations/feedback laws remain9.7/9.8. G11 retains committed active state without resurrecting legacy per-part event delivery. Production C# unit proof is primary; actual Chrome visual/construction qualification remains queued until catalogue completion.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/BothGate`, its scene and its tests on published main `8d1d68aa`. It was also checked against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run. The delivered declaration shared by the five supplied gates is in section 2; CAT-024 to CAT-027 point there.

**Disposition.** Delivered CAT part: package `elements/BothGate`, id `both_gate`, category Control, palette order 26 (`elements/BothGate/BothGateElementDeclaration.cs:12–17`). Story 9.6 Pass `e036b7` (`_bmad-output/implementation-artifacts/sprint-status.yaml:267`); proud parts collide since `elements-visible-solid-sweep`; markings are printed since `face-markings-flush` (`6125040e`). No slice is planned for this identity. [EL-133](EL-133-electrical-and-gate.md) is a candidate Duplicate closure of this part. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Integration.**
- Delivered collaborators: Battery, Impact switch (supply mode), State latch, Hold timer and Powered gate, each exercised by `BothGateTests`. The Motor is delivered; the only gate-to-Motor test is `MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`.
- Campaign: no level in `content/puzzles.json` places any of the five gates. First use belongs to a future campaign story.
- Coverage binding: catalogue key `both_gate` in `docs/coverage/catalogue-elements.json`.
- Closure: EL-133 to EL-137 close through `spec-closure-review-electrical-gates.md` after the engine programme (owner decision 3a), once `ENGINE-SIGNAL-NODES` (its self-edge rule row) is published.

**Residuals.**
- **Self-edge (narrower, section 3).** The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`, "Choose another part with a matching input socket."), so a player cannot make this wire; only a save or a command can carry it. It is not a player-reachable defect. Owner: `ENGINE-SIGNAL-NODES` (its self-edge rule row).
- **Required forward-only refactor: unread ratings.** `ElectricalNodes` declares 12 V, 0.05 Ω and 100 W (`BothGateElementDeclaration.cs:33`). The four rating values are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873–876`) and nothing reads them. Dead declaration data is removed, not kept; the deferred-work text is in batch B readiness.
- [Current consumers](../invest/current-consumers.md#cat-013-i) names the Motor as the first visible consumer. Decided under the standing delegation: the native consumer stays the Powered gate, because a consumer reads one route law whatever part it is, and the Motor recipe is kept for the browser phase.
- Requirement row CAT-013 stays unchecked until the post-catalogue browser phase.
