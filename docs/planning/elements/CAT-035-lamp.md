# CAT-035 · lamp (Signal lamp) — declaration readiness spec

**Status (verified 22 November 2026 against `8d1d68aa`):** delivered as package `lamp` (`elements/SignalLamp/`). The declaration is data, art and bindings only. Open: one hygiene slice that deletes an inert electrical node from the package (§3, §8); the one engine refusal message that names the part is reworded by `ENGINE-CAPABILITY-INSTANCES` (audit E4). The bulb's 0.16 s ease is kept and `DESIGN.md` follows the part (owner decision 4, 22 November 2026). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's one engine gap is a shared contract: the product-named activation kind `LevelIndicator` (audit E1), owned by `ENGINE-CAPABILITY-INSTANCES`. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is pending until every named element passes. The `@a6c914e` citations in §4 record the harvest source, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-035 · `lamp` (display name Signal lamp) |
| Requirement | [CAT-035](../requirements.md#current-cat-035) |
| Mapped identities | none: no EL/TH/RAD/GAP identity is the signal lamp. TH-36 Lamp-trigger apparatus (Batch N) concerns a historical light and heat source and is a different element. |
| Disposition | **Delivered.** Package id `lamp`, directory `elements/SignalLamp/`. Records: `review-c-lamp-detector.md` (native Pass at `2065d3`); colliders by `elements-visible-solid` (done). |
| Roadmap | [CAT-063-I and CAT-035-I](../invest/vertical-delivery.md#switch-lamp); Story 4.1 (lamp glow on the animation worker) |
| Levels | two Playable lessons place it (`domino_effect`, `delayed_signal`); 5 planned levels name it in [CAT-035-I consumers](../invest/current-consumers.md#cat-035-i) |

## 2. Declaration

Delivered declaration: `elements/SignalLamp/SignalLampElementDeclaration.cs`; scene `elements/SignalLamp/lamp.tscn`.

| Item | Value and source |
| --- | --- |
| Interfaces | `IElectricalElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:6`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Declared at `SignalLampElementDeclaration.cs:9`. Domains: Electrical and Signal (`:50`). The electrical interface and domain are inert and are deleted by the hygiene slice (below). |
| Body | One static body (footprint 1 body, 0 moving, 0 constraints, `:32`). No mass frame is declared. |
| Colliders | Three, on that body, as hidden `col_` nodes. `col_0_0`: box, half extents (0.425, 0.1, 0.425) m at (0, −0.35, 0), the base. `col_0_1`: sphere, radius 0.43 m at (0, 0.22, 0), the bulb. `col_0_2`: cylinder, radius 0.18 m, height 0.3 m, axis Y, at (0, −0.17, 0), the stem (`lamp.tscn:30`–`38`, `:59`–`75`). |
| Material (f32) | restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:44`). Pair restitution is the product of the two materials, so 1 is the neutral value: a ball leaves the lamp at its own restitution. |
| Parameters | none (`:30`); the editor shows no panel. |
| Port | One: `PortId.ActivationIn`, domain Signal, direction Input, role `ActivationIn`, signal kind Level, at the origin, facing −Y (`:37`). No electrical port; the requirement forbids fabricating a `PowerIn`. |
| Signal node | One: `SignalIndicatorDescriptor` (record at `CuriousContraptions.Abstractions/SignalContractValues.cs:53`), id `signal` (`:60`), bound to `ActivationIn` as the node's input terminal (`:62`–`65`). No signal edges (`:61`). |
| Electrical node (inert) | One: `lamp_bulb`, role Consumer, rated voltage 12 V, resistance 48 Ω, capacitance 0, maximum power 5 W (`:52`–`55`). No electrical port exists, so nothing can supply it and no behaviour reads it. To be deleted. |
| Stores, joints, sensors | none. |
| Events | none (`:48`): the lit state is a committed level, not a ring record. |
| Presentation bindings | Pick radius 0.65 m, no resize axes (`:11`). Six bindings on `Visual/Bulb`, each driven by `PresentationDriver.CommittedActivation` on node `signal`, smoothstep, 0.16 s, overlap Replace (`:13`–`18`): `AlbedoRed` 85/255 to 1, `AlbedoGreen` 101/255 to 240/255, `AlbedoBlue` 115/255 to 165/255 (slate `#556573` to `#fff0a5`); `EmissionRed` 0 to 233/255, `EmissionGreen` 0 to 178/255, `EmissionBlue` 0 to 76/255 (black to `#e9b24c`). |
| Catalogue | id `lamp`, title Signal lamp, category Goals, palette order 4, colour (0.98, 0.86, 0.51) = Lamp `#fadb82` (`DESIGN.md:234`), icon `res://elements/SignalLamp/icon.svg`, scene `res://elements/SignalLamp/lamp.tscn` (`:21`–`30`). It is the usual target of an Activated or ActivatedAfter goal. |

At a glance: a round slate bulb on a short grey stem over a square navy base.

## 3. Engine capabilities

Capability probe, 22 November 2026 (tests in `CuriousContraptions.tests/`; each name checked to exist). Counts: 5 present, 0 present but untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
| --- | --- | --- | --- |
| Declared indicator: a pulse lights it on the first input and it holds until Reset; a level is followed on and off; an unwired lamp stays dark | present | `WorkshopActivationCompiler.IndicatorKind` (`engine/gpu/WorkshopActivationCompiler.cs:99`) selecting `ActivationNodeKind.StickyFirstInput` or `LevelIndicator` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`) | `LampDetectorGuardTests.ASwitchLightsItsLampOnTheFirstInputAndItStaysLitWhileAnUnwiredLampStaysDark`; `.ALampOnALatchFollowsItsLevelOnAndOffWhileAFirstInputLampHolds`; `.ALampOnAPlateFollowsItsLevelOnAndOff`; `DeclaredIndicatorPassageTests.AnIndicatorFollowsALevelOnlyOnItsDeclaredInput`; `ContactActivationTests.DisconnectedAndAbsentContactCannotActivateLamp` |
| Connection rule on declared port kinds: a level output drives only an input that admits a level, and such an input takes one source | present | `WorkshopConnections.ValidateActivation` (`engine/gpu/WorkshopConnections.cs:140`); `WorkshopEditRejection.LevelDrivesLevelInputOnly`, `LevelInputFollowsOneLevel` | `LampDetectorGuardTests.ALampThatFollowsALevelRefusesASecondSignalAndALevelLightsNoOtherInput`; `DeclaredIndicatorPassageTests.TheLevelWiringRefusalsKeepTheirOrdinalsAndText` |
| Goal on a lit indicator, including a minimum delay | present | `WorkshopGoalEvaluator` (`engine/gpu/WorkshopGoalEvaluator.cs:8`) | `DominoEffectTests.GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched`; `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch`; `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`domino_effect`, `delayed_signal`) |
| Colour and emission from committed activation, both directions, cleared by Reset | present | `PresentationDriver.CommittedActivation` | `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` |
| Reset, save and load of the wired bench; box, sphere and cylinder colliders as drawn | present | generic instance path | `DeclaredIndicatorPassageTests.ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation`; `.TheSavedAndLoadedWiredBenchPassesTwice`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (row `lamp`) |

Dependencies: a signal source (Impact switch, Delay box, Domino, Ball detector, Counter, State latch, Pressure plate and others).

### Engine conformance

Authority: the owner's engine-conformance instruction of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and the [element-free engine design](../../../_bmad/custom/element-free-engine-design.md).

**Compiled counts per placed lamp.** Bodies 1 (static). Colliders 3 (1 box, 1 sphere, 1 cylinder). Joints 0. Drives 0. Sensors 0. Domain nodes 2 today (1 signal indicator, 1 inert electrical consumer); 1 after the hygiene slice. Stores 0. Ports 1. Events 0. Presentation bindings 6. All counts are fixed at compile time.

**Why three colliders.** Visible = solid: the base, the stem and the bulb are each drawn proud and collide with their true shape (`elements-visible-solid`). None exists for a cosmetic purpose alone.

**Art layers** (`lamp.tscn:44`–`55`; classification as `VisibleSolidColliderTests.cs:80`).

| Art node | Mesh | Class | Collider | Moved by |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Base` | box 0.85 × 0.2 × 0.85 m at y −0.35, navy | solid | `col_0_0` | nothing (static body) |
| `Visual/Visual_Stem` | cylinder, radius 0.18 m, height 0.3 m at y −0.17, grey | solid | `col_0_2` | nothing |
| `Visual/Bulb` | sphere, radius 0.43 m at y 0.22, slate with emission | solid | `col_0_1` | nothing; its colour and emission are set by the six bindings, its pose is not, so it stays solid |

Physics reads no art node. The bindings read the committed level of node `signal` and write nothing back.

**Behaviour as generic laws.**

| Behaviour | Generic law | Declared values |
| --- | --- | --- |
| A ball bounces off the lamp as off any fixed solid | rigid contact with pair restitution and Coulomb friction on a static box, sphere and cylinder | restitution 1 (neutral under the product rule), friction 0.3 |
| The lamp lights and stays lit, or follows a level | signal-network rule for an indicator node: fed by a pulse output it holds its first input until Reset; fed by a level output it follows that level each committed tick. The node kind is selected from the declared kinds of the two ports, never from a part identity | one `SignalIndicatorDescriptor`; input port kind Level |
| A level output cannot be wired to a pulse-only input, and a level input takes one source | connection rule on declared port kinds | port kinds |
| The level is solved | objective evaluation reads the indicator's committed state for the named target | the level's goal, not the package |
| The bulb glows | committed-activation presentation binding | six bindings, 0.16 s smoothstep |

**Performance fitness.** The body is static. Three primitive colliders, no sensor, no store. The indicator is one node in the signal network's flat node table. Tunnelling is excluded by the engine's general means (`docs/gpu-f32-physics.md:210`–`213`, `:242`); the package adds no check of its own.

**Inert data to delete (point 1).** The electrical node `lamp_bulb`, the `IElectricalElement` implementation and the Electrical domain flag are declaration data that no behaviour reads. The Delay box is the precedent for `ISignalElement` without `IElectricalElement`.

**Engine search and residue (audit C3).** No engine code branches on the package, its id or its class. One message names the part: `engine/gpu/WorkshopConnections.cs:150` throws "A level output can only light a Signal lamp." although the rule above it reads declared port kinds (`:146`–`147`). Required rewrite: "A level output connects only to an input that admits a level." `ENGINE-CAPABILITY-INSTANCES` makes it (audit E4, player-facing strings naming parts), as the first slice to edit the line, with the tests that pin the old text. The node kinds it selects, `ActivationNodeKind.StickyFirstInput` and `LevelIndicator` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`), include a product name (audit E1); the rename to a capability name is owned by `ENGINE-CAPABILITY-INSTANCES`, not by this package.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A switch → lamp activation link lights the lamp; Reset clears it; a duplicate link, a foreign part, or an undefined socket is rejected and never lights it. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L139-L185` | carry forward. |
| 2 | Wired after a Delay, the lamp stays dark on every tick before the due tick and lights exactly at it; the Activated event records the due tick; Reset clears it. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L50-L90` | carry forward. |
| 3 | A Delay cannot connect to a motor: commands never replace electrical supply. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L63-L64` | carry forward the domain separation (lamp is activation-only). |
| 4 | A counter → lamp chain lights only when the counter reaches its target (three detector crossings), not at two. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L110-L152` | carry forward as an integration control (counter owned by CAT-020). |
| 5 | Signal modules put the lamp at (4.5, 1, 0) with goal Activated(lamp); counterweight puts it at (5, 1, 0.12). | `tools/Campaign/Program.cs@a6c914e:L53-L63`; `tools/Campaign/Program.cs@a6c914e:L189-L207` | carry forward (content in `content/puzzles.json`). |

Files harvested:
- `CuriousContraptions.tests/ConnectionPortTests.cs`
- `CuriousContraptions.tests/DelayTests.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-035](../requirements.md#current-cat-035) and [simulation and element acceptance](../requirements.md#accept-simulation). Every row runs the production simulation through defined interfaces and proves a consequence of a generic law for this declaration. `cat-035.test.ts` is authored in the post-catalogue browser phase (Story 9R.16).

| Given / When / Then | Test (exists unless marked planned) | Control and boundary |
| --- | --- | --- |
| A committed activation lights the lamp and it stays lit until Reset | `LampDetectorGuardTests.ASwitchLightsItsLampOnTheFirstInputAndItStaysLitWhileAnUnwiredLampStaysDark` | an unwired lamp stays dark |
| A missed impact or a disconnected wire cannot light it | `ContactActivationTests.DisconnectedAndAbsentContactCannotActivateLamp` | — |
| Fed by a level it follows the level on and off | `LampDetectorGuardTests.ALampOnALatchFollowsItsLevelOnAndOffWhileAFirstInputLampHolds`; `.ALampOnAPlateFollowsItsLevelOnAndOff` | a first-input lamp on the same bench holds |
| Wrong wiring is refused with a typed reason | `LampDetectorGuardTests.ALampThatFollowsALevelRefusesASecondSignalAndALevelLightsNoOtherInput`; `DeclaredIndicatorPassageTests.TheLevelWiringRefusalsKeepTheirOrdinalsAndText`; electrical-to-signal refusal is generic under S4 row 5 | the valid single level source connects |
| Duplicate triggers add no event; the goal reads the target's occurrence | `DelayedSignalTests.GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch`; `DominoEffectTests.GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched` | an early latch is rejected |
| Bulb colour and emission come only from the committed state and clear on Reset | `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` | lit and dark colours both asserted |
| Reset, save and load of the wired bench | `DeclaredIndicatorPassageTests.ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation`; `.TheSavedAndLoadedWiredBenchPassesTwice` | — |
| Lessons with a lamp solve, and not with a cause missing | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`domino_effect`, `delayed_signal`) | 60, 120 and 240 Hz |
| After the hygiene slice the package declares no electrical node or domain | **planned** `DeclaredIndicatorPassageTests.TheLampDeclaresOnlyItsSignalDomain`: the bundle entry for `lamp` has domains Signal only and no electrical node (exact, a declared value) | the Delay box entry reads the same |

Levels: `domino_effect` and `delayed_signal` are Playable and solved with a missing-cause control (`domino_effect` is re-authored without the lamp by completion-ledger OS-06, owner decisions 24f and 24o(3) and coordinator decision 41, after which only `delayed_signal` places it among the Playable levels). The other 5 levels that name the lamp are Planned and have no authored content yet: power_trip, spring_signal, counterweight, pulley_depth, grand_contraption. They belong to their campaign chapter stories; those with more than one goal wait for ENGINE-GOALS. Nothing in them is owed by this package.

Integrations named by the requirement: [sequence-task-279](../requirements.md#sequence-task-279) (ActivationIn from a switch, detector or delay); [IX-07 signal propagation](../requirements.md#interaction-07). The lamp is also listed among electrical consumers ([sequence-task-278](../requirements.md#sequence-task-278)) and optical sources ([sequence-task-281](../requirements.md#sequence-task-281)); neither is its activation contract.

## 6. Open questions

None open.

Resolved:

- **Electrical node deleted (standing delegation; coordinator decision 9; audit judged it sound and forward-only).** Delete `lamp_bulb`, the `IElectricalElement` implementation and the Electrical domain flag. Reason: declaration data that no behaviour reads misleads the next reader, and the requirement forbids an electrical input. An electrically powered light is a separate identity (EL-155, TH-36). Because `SupportedDomains` changes, the slice re-checks the derived impact mapping and the element map row, which lists ElectricalPower for this part today.
- **Engine message (audit C3 and E4).** The refusal text at `engine/gpu/WorkshopConnections.cs:150` becomes "A level output connects only to an input that admits a level.", the one wording CAT-037 and CAT-052 also cite. `ENGINE-CAPABILITY-INSTANCES` makes the change, with the tests that pin the old text (`DeclaredIndicatorPassageTests.cs:296`, `LampDetectorGuardTests.cs:304`, `StateLatchTests.cs:164`, `PressurePlateTests.cs:698`) and the deferred browser constant `tools/e2e/cat-052-lamp.test.ts:36`; the hygiene slice does not touch it.
- **Levels (audit M8).** Named above; none waits on this package.

- **Glow timing (owner decision 4, 22 November 2026).** `DESIGN.md:336` said the bulb changes with "no fade tween"; the delivered bindings ease over 0.16 s (smoothstep), as the harvest source did. Decided: the signal lamp eases over 0.16 s and `DESIGN.md` follows the part.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Impact switch, Ball detector, Domino, Delay box (pulse outputs); State latch, Pressure plate (level outputs); Counter, Hold timer, logic gates. All delivered. Every source is a Signal-domain output, matching the lamp's Signal input.
- **Campaign.** The usual goal target. The five goal engine slices (`engine-goal-evaluation` onward, the fifth `engine-goal-count-window-hold` by owner decision 12) change how goals are declared, not this package.
- **Coverage binding.** Catalogue key `lamp` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered. No slice is owed for the part's behaviour.
- **Residual work.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. One hygiene slice, after `ENGINE-CAPABILITY-INSTANCES` has reworded the level refusal:
  - `elements/SignalLamp/SignalLampElementDeclaration.cs`: delete the interface, the node and the domain flag; `elements/SignalLamp/SignalLamp.csproj`: delete the Electrical abstractions reference; regenerate `elements/elements.bundle.json` (it carries `lamp_bulb` today);
  - `docs/planning/general-engine-element-map.md` row CAT-035 and its coverage binding: drop ElectricalPower;
  - the planned test in §5.
- **Then.** Level proofs with the campaign slices; Chrome proof (Story 9R.16) post-catalogue.
- **Disqualifiers.** Any consumer found for the electrical node (the slice's probe searches tests, tools and the bundle readers; then the node stays and gets a port only by owner decision, since the requirement forbids one); a client that matches the old message text instead of the typed `WorkshopEditRejection` value.
