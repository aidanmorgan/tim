# CAT-063 · switch (Impact switch) — declaration readiness spec

**Status (22 November 2026; EL-198 absorbed 23 November 2026).** Delivered: package `elements/ImpactSwitch`, id `switch` (activation by CAT-063-I and C-Switch; the separately supplied `PowerIn` → `Supply` contact by Story 9.5b; drawn-shape colliders by `elements-visible-solid`). Sections 2 and 3 are read from the package and its tests in this checkout (`path:line`; tests are in `CuriousContraptions.tests/`). This spec is the single declaration of the `switch` package (coordinator decision 27, 23 November 2026) and holds EL-198's author-set contact variant, its acceptance and its delivery (coordinator decision 31, 23 November 2026); [EL-198](EL-198-switch.md) points here. One conformance defect is open (the button art moves away from its collider, section 3) with a decided repair. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gap is owned by `ENGINE-CONTACT-OBSERVATIONS` (contact-trigger rows found by compiled handle, on any owner); its goals wait for `ENGINE-GOALS`, and the author-set contact variant for the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` and the choice-bound node and event of `ENGINE-PARAMETER-BOUND-FRAMES`. Browser qualification is pending until the post-catalogue E2E phase. Citations of the form `path@a6c914e` in section 4 are the legacy harvest, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-063 · `switch` (display name Impact switch) |
| Requirement | [CAT-063](../requirements.md#current-cat-063); [retained behaviour](../requirements.md#todo-147) |
| Mapped identities | EL-198 Switch ([requirement element-198](../requirements.md#element-198); [named-elements entry](../invest/named-elements.md#element-198), owner S304). Its declaration, acceptance and delivery are in this spec; the [EL-198 spec](EL-198-switch.md) is a pointer |
| Disposition | Delivered |
| EL-198 | The physical, separately supplied contact is delivered by Story 9.5b. The requirement row adds an author-set contact state ("Physical or author-set contact state opens or closes an electrical path"; outcome "Closed contact cannot create supply energy"), delivered as a variant of this package with a `contact` parameter (D8): [Author-set contact variant](#author-set-contact-variant-el-198) below. So EL-198 is not a Duplicate closure, but it has no package, declaration or acceptance text of its own; it counts as implemented when the variant slice passes rows A1–A6 (section 5) beside the delivered rows. Engine owners: the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` (coordinator decision 25) and the choice-bound values of `ENGINE-PARAMETER-BOUND-FRAMES` (coordinator decision 16) |
| Levels | placed in 19 levels, inventory in 11; see [CAT-063-I consumers](../invest/current-consumers.md#cat-063-i) |

## 2. Declaration

Read from `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs` (cited as `:line`) and `elements/ImpactSwitch/switch.tscn` (cited as `tscn:line`).

**Interfaces** (`:12`): `IMechanicalElement`, `ISignalElement`, `ISupplyElement`, `ISceneGeometryElement`. Domains Mechanical, Signal and Electrical (`:55`). Kinematic mode Static (`:57`); footprint one body, none moving, no constraints (`:36`); mass frame zero (`:61-67`).

**Catalogue** (`:22-30`): id `switch`, category Power, palette order 3, colour (0.94, 0.43, 0.33), display name "Impact switch".

**Body, colliders and material.** One static body. Material (`:50`): restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Restitution 1 is the neutral value under the product pair law: a ball rebounds at its own restitution.

| Collider | Shape | Dimensions (m) | Centre (m) | Source |
| --- | --- | --- | --- | --- |
| `col_0_0` (base) | Box | 1.1 × 0.25 × 1.0 | (0, −0.15, 0) | `tscn:36-37`, `:79-83` |
| `col_0_1` (button) | Cylinder | radius 0.38, height 0.18 | (0, 0.06, 0) | `tscn:39-41`, `:85-89` |
| `col_0_2` (terminal) | Sphere | radius 0.08 | (−0.58, 0, 0) | `tscn:43-44`, `:97-101` |
| `col_0_3` (terminal) | Sphere | radius 0.08 | (0.58, 0, 0) | `tscn:43-44`, `:103-107` |
| `col_0_4` (socket) | Sphere | radius 0.09 | (0.45, 0, 0.4) | `tscn:46-47`, `:109-113` |

**Parameter** (`:33`).

| Name | Type | Unit | Default | Minimum | Maximum | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `trigger_threshold` | Real | m/s | 0.8 | 0 | 64 | Clamp | BuildOnly (the default, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:61`) |

**Ports** (`:40-42`).

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `ActivationOut` | ActivationOut | Signal | Output | Pulse | (0.45, 0, 0.4), facing +Z |
| `PowerIn` | PowerIn | Electrical | Input | none | (−0.58, 0, 0), facing −X |
| `Supply` | Supply | Electrical | Output | none | (0.58, 0, 0), facing +X |

**Signal node** (`:79`, `:81-84`): one `SignalContactDescriptor`, id `signal`, on body 0, threshold from `trigger_threshold`, default target selector (every dynamic body in free play, the lesson's payload in a puzzle); its output terminal is bound to `ActivationOut`. **Event** (`:53`): `TriggerActivated` on node `signal`.

**Supply graph** (`:71-76`): one `SupplyContactDescriptor`, id `contact`, closed while signal node `signal` is Latched; `PowerIn` is its input terminal and `Supply` its output. **Electrical node** (`:77`): `switched_contact`, role Switch, 12 V, 0.05 Ω, 0 F, 100 W; metadata, read by no solver.

**Stores:** none.

**Presentation bindings** (`:14-20`). Pick radius 0.65 m. Four bindings on `Visual/Button`, all driver `Event` on `TriggerActivated` of node `signal`, curve SmoothStep over 0.16 s, overlap Replace.

| Property | From | To |
| --- | --- | --- |
| LocalY | 0.06 | −0.02 |
| AlbedoRed | 0.94 | 191/255 |
| AlbedoGreen | 0.43 | 245/255 |
| AlbedoBlue | 0.33 | 176/255 |

The LocalY binding is the open defect (section 3); the repair deletes it.

**Numbers.** The 0.8 m/s default is reached by any ball that has fallen 3.3 cm or more, so every delivered ball triggers the switch from a normal drop. The 64 m/s maximum is the engine's speed envelope. The latch closes the supply route at the next tick's electrical solve, one tick after the strike (`SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset` asserts unpowered on the strike tick and powered on every later one).

### Author-set contact variant (EL-198)

The requirement row [element-198](../requirements.md#element-198) asks for a physical **or author-set** contact state. It is one Choice parameter on this package (D8: a mode inside one identity is a parameter of its entry); every delivered value above stands in mode Impact. The rows below are new.

| Name | Type | Unit | Default | Options | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- |
| `contact` (**required**) | Choice `[Impact, Open, Closed]`, compiled to a closed enum | none | Impact | 0 Impact, 1 Open, 2 Closed | Reject an undefined option; player-editable unless the level locks it (D11) | BuildOnly |

`trigger_threshold` is read only in mode Impact. One three-option parameter, not a `mode` plus a `fixed_state`: a second parameter meaningful for one value of the first is a combination that needs refusing, and one choice has none.

| `contact` | Contact closed when | Impact node and `TriggerActivated` | `ActivationOut` | Button colour |
| --- | --- | --- | --- | --- |
| Impact (delivered) | from the tick the impact latch fires until Reset | present: one latch per Run, one pulse at the latch | available | catalogue colour, pressed colour after the latch (the delivered colour bindings) |
| Open | never | absent: a strike latches nothing and publishes nothing | unavailable: a wire to it is refused `InactivePort` | catalogue colour |
| Closed | for the whole Run, from its first electrical solve | absent, as Open | unavailable, as Open | pressed colour |

- **Signal node and event.** The `SignalContactDescriptor` `signal` and its `TriggerActivated` event are present only when `contact` = Impact: a node and an event present by a Choice option, the choice-bound values of `ENGINE-PARAMETER-BOUND-FRAMES` (coordinator decision 16; missing, section 3). Port availability alone would refuse wiring but leave the node latching and publishing, so an event-based goal would still count a strike.
- **Supply contact.** `contact` declares one control per option: Impact → the delivered `SignalStateContactControl("signal", SignalContactState.Latched)`; Open → a fixed open control; Closed → a fixed closed control. Per-option controls and the fixed control are the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` (coordinator decision 25; missing, section 3). Contact law in every mode: `Supply` available = `PowerIn` available ∧ closed, with closed = latched (Impact), false (Open), true (Closed); a closed contact with no upstream supply passes nothing (requirement outcome).
- **Port.** `ActivationOut` is available only when `contact` = Impact (`PortDescriptor.Availability`, `CuriousContraptions.Abstractions/PortDescriptor.cs:72`, as the Fan's mode-dependent ports, `elements/Fan/FanElementDeclaration.cs:59`–`:69`). Availability refuses wiring (`engine/gpu/WorkshopConnections.cs:86`–`:93`). **Editing `contact` away from Impact while a wire leaves `ActivationOut`** is refused whole with `WorkshopEditRejection.InactivePort` and the construction is unchanged; with the wire removed first the edit is admitted. That is the Fan's delivered mode-edit rule (`FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation`, `CuriousContraptions.tests/FanAdmissionTests.cs:191`, which asserts the refusal, the unchanged construction and the admission after the wire is removed).
- **Presentation.** The three delivered colour rows are selected by `contact` = Impact (`PresentationBinding.Selection`, `CuriousContraptions.Abstractions/ElementPresentation.cs:112`), so a strike on an author-set switch changes nothing. For Open and Closed the same three albedo properties are bound, per option, to a driver that reads the contact's closure irrespective of supply: catalogue colour (0.94, 0.43, 0.33) when open, pressed (191, 245, 176)/255 when closed. No delivered driver reads closure (`CommittedSupply` follows supply, so a Closed switch with no battery would show the open colour); `CommittedContactClosed` belongs to the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` (missing, section 3). No binding moves the button in any mode (after the button repair, section 3); no art or collider is selected by `contact`.
- **Catalogue.** The description gains one sentence: "Or set it open or closed by hand before you run."
- **Compiled counts per mode.** Impact: as delivered (1 sensor; 3 domain nodes: signal, contact, electrical metadata). Open and Closed: 0 sensors; 2 domain nodes (contact, electrical metadata). Every mode: 1 static body, 5 colliders, 0 joints, 0 drives, 0 stores, 3 ports. Presentation bindings: 3 after the button repair, 9 declared after the variant slice (three colour rows per option, three selected per instance). Each count is fixed at compile time by the built `contact` value.
- **Tick order.** As delivered in mode Impact (closed from the next electrical solve after the strike). In mode Closed the contact is closed at the first electrical solve of the Run.
- **Forward-only.** `contact` is required: every stored `switch` instance carries it, and an instance or save without it is refused whole by the existing typed refusal (`engine/gpu/InstanceParameters.cs:41`; `engine/gpu/WorkshopSavePrevalidation.cs:108`–`:113`, `WorkshopSaveRejection.OutOfRange`, "Missing or unknown declared parameters."). No default-on-load, migration or version branch is written.

## 3. Engine capabilities

Capability probe, 22 November 2026, with EL-198's rows added on 23 November 2026. Counts: 6 present, 2 present but untested, 0 narrower, 4 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Contact trigger on an owner body: a contact at or above a declared normal approach speed latches a signal node once (threshold inclusive) | present | `SignalContactDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:51`), `ContactTriggerDeclaration` (`CuriousContraptions.Abstractions/ContactTriggerDeclarationValues.cs:8`), compiled at `engine/gpu/WorkshopPhysicsCompiler.cs:437-444`, evaluated at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:141-160` | `ContactActivationTests.ActualBaseBoxContactUsesInclusiveNonHalfThresholdAndExactReportedSpeed`, `.RealImpactAndMissDisconnectedControlsRestoreExactly`, `.CommittedContactLatchesSourceAndDeclaredTargetOnceWithoutMutatingPriorCheckpoint` |
| Every drawn solid collides as drawn, and a contact on any collider of the body fires the trigger | present | scene colliders; the trigger row is keyed by owner body | `ContactActivationTests.CompilerKeepsEverySwitchColliderAsDrawnWithTheInheritedStaticMaterial`; `VisibleSolidColliderTests.AnImpactOnASwitchTerminalFiresTheSwitch`, `.ABallFallingBesideTheButtonInsideTheOldBoxCornerPassesFreelyAndOnlyTheButtonFiresTheSwitch`, `.ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution` (feature `SwitchButton`) |
| Supply contact controlled by a signal node's latch: it passes supply only while closed and supplied; supply loss and restoration keep the latch; chains and cycles are source-seeded | present | `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`), `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`), `engine/gpu/SupplyRoute.cs:58-74` | `SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset`, `.UnqualifiedOrUnsuppliedContactCannotMoveConsumer` (miss, below threshold, each missing wire, disabled source), `.SupplyLossAndRestorationDoNotEraseOrReplayImpactMemory`, `.TwoContactChainsAndCyclesAreSourceSeededAndOrderIndependent`, `.SeededCycleLosesAndRestoresPowerWithoutChangingEitherLatch` |
| A pulse output drives signal consumers; a Signal wire into an Electrical input is refused | present | port typing in `engine/gpu/WorkshopConnections.cs` | `ContactActivationTests.DisconnectedAndAbsentContactCannotActivateLamp`; `MotorTests.AnActivationWireIntoThePowerInputIsRefused` |
| A trigger event cues presentation bindings in the animation worker | present | `PresentationBinding` with `PresentationDriver.Event` and `SignalNode` (`CuriousContraptions.Abstractions/ElementPresentation.cs:100`) | `BasketballResourceTests.BumperAndSwitchArtworkFollowOneDeclaredBlendAndHoldOnInvalidSamples` |
| A Motor on a switched supply (Battery → switch → Motor) | present, untested | the supply contact above and the Motor's driven hinge | Settled by opening `MotorTests`: it places a switch once, in `AnActivationWireIntoThePowerInputIsRefused` (`MotorTests.cs:316-319`), a wrong-domain refusal. No test wires a Motor behind this switch. The parts of the composition are each tested: the switch closing a route to a consumer (`SwitchElectricalTests`, with a Powered gate), a Motor on a switched route (`MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`), and the rotor coasting down on supply loss (`MotorTests.CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork`). Owner: one planned test in the button repair slice (section 5) |
| Goals for `power_trip` (an `activated` goal with no fixed predecessor node) and `switched_motor` ("powered after the switch", so that a direct-wire bypass does not win) | missing | `WorkshopGoalEvaluator` knows Captured and ActivatedAfter only (`engine/gpu/WorkshopGoalEvaluator.cs:41`) | ENGINE-GOALS (slices `engine-goal-evaluation` and `engine-goal-ordered-events`) |
| Mode-dependent port availability; a wire to an inactive port, or a mode edit that would leave a wire on one, is refused | present | `PortDescriptor.Availability` (`CuriousContraptions.Abstractions/PortDescriptor.cs:72`); `ChoicePredicate` (`CuriousContraptions.Abstractions/AirflowValues.cs:23`); `engine/gpu/WorkshopConnections.cs:86`–`:93` | `FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation` (`FanAdmissionTests.cs:191`, `WorkshopEditRejection.InactivePort`). It exercises an Electrical input; whether a Signal output port is covered is re-checked in the variant slice's probe |
| A supply contact that declares one control per option of a Choice parameter, with a fixed open or closed control among them (the author-set variant) | missing | `SupplyContactDescriptor` takes one control, and `SupplyContactControl` has two forms only, `SignalStateContactControl` and `PrismaticContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:8`–`:13`) | the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` (coordinator decision 25) |
| A presentation driver that reads a contact's closure irrespective of supply (`CommittedContactClosed`) | missing | no member of `PresentationDriver` reads closure (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:26`); `CommittedSupply` reads supply | the same row of `ENGINE-SIGNAL-NODES` |
| Presentation bindings selected by a Choice option | present, untested | `PresentationBinding.Selection` (`CuriousContraptions.Abstractions/ElementPresentation.cs:112`) | no test was opened for it; the variant slice's probe cites one or re-marks the row |
| A signal node and its event present only for one option of a Choice parameter (the impact node and `TriggerActivated` only in mode Impact) | missing | a `ChoicePredicate` selects presentation bindings, ports, energy stores and airflow inputs (`ElementPresentation.cs:112`, `PortDescriptor.cs:72`, `EnergyStoreValues.cs:75`, `AirflowValues.cs:31`); no signal node or event descriptor takes one | `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |

### Engine conformance

**Compiled counts.** Bodies 1 (static). Colliders 5: 1 box, 1 cylinder, 3 spheres. Joints 0. Drives 0. Sensors 1 (contact trigger). Domain nodes 3: one signal node, one supply contact node, one electrical node (metadata). Stores 0. Presentation bindings 4 (event-driven), 3 after the repair. Ports 3. Events 1 kind. All counts are fixed at compile time. These are mode Impact's; the author-set modes' counts are in section 2 ([Author-set contact variant](#author-set-contact-variant-el-198)).

**Art and layers.**

| Art node | Mesh (m) | Classification | Moved by |
| --- | --- | --- | --- |
| `Visual/Visual_Base` | box 1.1 × 0.25 × 1.0 at y −0.15, navy (`tscn:21-23`, `:63-65`) | solid, collider `col_0_0` | nothing |
| `Visual/Button` | cylinder r 0.38, h 0.18 at y 0.06, coral (`tscn:25-29`, `:67-70`) | solid, collider `col_0_1`. **Defect (audit C1):** the LocalY binding lowers the art 8 cm after the latch while the collider stays, so a ball resting on a pressed button floats 8 cm above it | nothing (static body); today also a presentation binding, which the repair removes. Its colour stays bound |
| `Visual/Visual_Terminal1`, `Visual_Terminal2` | spheres r 0.08 at x ∓0.58, drawn gold by a material override (`tscn:16-19`, `:53-61`) | solid, colliders `col_0_2`, `col_0_3` | nothing |
| `Visual/Visual_Socket` | sphere r 0.09 at (0.45, 0, 0.4), gold (`tscn:31-34`, `:72-74`) | solid, collider `col_0_4` | nothing |

The solid list agrees with `VisibleSolidColliderTests.cs:81-82`.

**Conformance repair (coordinator decision, 22 November 2026; reason: art moved by a binding must be decorative, and anything that blocks must stay with its collider).** The latch is shown by colour only. The `LocalY` binding (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:16`) is deleted, so the collider and the visible button stay together; the three colour bindings stay. One element-tier slice edits: that declaration line; `DESIGN.md:335` (the switch row becomes "a latched contact changes the button to pale green `#bff5b0`; the button does not move") and `DESIGN.md:319` ("depressed/green button" becomes "green button"; its "two navy supply terminals" is corrected to gold, which is what the scene draws and what the activation socket uses); the binding expectations in `PresentationBindingGoldenTests` and `BasketballResourceTests.BumperAndSwitchArtworkFollowOneDeclaredBlendAndHoldOnInvalidSamples`; and the two planned tests of section 5. The author-set modes add no travel in any mode (section 2). A button that really presses would be a prismatic body with its own collider and is not planned.

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| A ball bounces off any part of the switch | Contact with the restitution pair law and Coulomb friction | restitution 1, friction 0.3, bounce threshold 0.1 m/s |
| A hard enough hit latches once per Run | Contact trigger law: a contact of the owner body with a target body whose pre-response normal approach speed is at least the threshold records one occurrence; the signal node latches and stays latched until Reset | `trigger_threshold`; owner body 0; default target selector |
| The activation output pulses once | Signal network rule: a latched source node emits one pulse on its bound output | node `signal` → `ActivationOut` |
| Power passes only after the hit and only with supply upstream | Supply network rule: a contact node passes supply while its control state holds; supply is seeded from sources, so a contact or a loop of contacts never creates it | contact `contact`, closed while `signal` is Latched |
| Supply loss darkens the load but the switch stays closed | The latch is signal state and the route is supply state; they are separate tables | none |
| The button changes colour | Event binding law on `TriggerActivated` | the colour bindings |
| An author-set switch is open or closed for the whole Run and ignores strikes | The same supply network rule with the contact's control fixed by declared data; the impact node and its event are absent by a Choice option | `contact` (Open, Closed) |

**Why five colliders.** Visible = solid: base, button, two terminals and the activation socket are each drawn proud and each collides as drawn (owner and coordinator decisions, 25 October 2026). The earlier single button box, whose corners reached 0.55 m from the axis, was deleted for that reason.

**Performance fitness.** The body is static. The trigger is a row keyed to the owner body, not a volume. Tunnelling is excluded by the engine's general means, 480 Hz substeps with speculative contact margins (`docs/gpu-f32-physics.md:375`); the smallest collider is 16 cm across. Engine gap, not an element fault: for each static-dynamic contact point the engine scans every contact-trigger row and every contact-work row for a matching owner (`WorkshopPhysicsEngine.cs:141`, `:165`), which is work proportional to contacts times declared rows. Owned by `ENGINE-CONTACT-OBSERVATIONS` (rows found by compiled handle). The part is not designed around it.

**No engine code names the package.** The audit's engine search found no id literal, file or branch for `switch`. The `switched_contact` node's voltage, resistance and power are read by no solver.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | switched_motor: Battery Supply → switch PowerIn, switch Supply → motor PowerIn; goals Turned(motor) and PoweredAfter(motor, switch_1); Bowling striker at (−2, 5, 0) above the switch at (−2, 1, 0). | `tools/Campaign/Program.cs@a6c914e:L133-L149` | carry forward as the electrical pass-through lesson. |
| 2 | The switched lessons replay identically after Reset; a direct battery → motor bypass turns the motor and activates the switch but does not win (sequencing is checked); removing any wire fails and leaves the motor unpowered. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L42-L94` | carry forward (direct-wire-bypass lesson). |
| 3 | Closed relay chains (supply → switch 1 → switch 2 ↔ switch 1 → motor) propagate regardless of identity order; a loop alone never supplies; supply off clears both switches and the motor; supply back on restores it; Reset clears every part. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L137-L181` | carry forward the coherent network cycles and the loss and return of power. Supply loss clears power only: the activation latches are kept (delivered rule, §6). |
| 4 | A closed contact still needs upstream supply; supply loss clears power without deleting the motor's shaft momentum. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L183-L215` | carry forward. |
| 5 | Gated modules: switch at (−5.8, 1.6, 0) under a Bowling trigger at (−5.8, 2.4, 0); in gated_belt the switch is wired battery → switch → motor (electrical); in gated_air the switch's ActivationOut drives the fan. | `tools/Campaign/Program.cs@a6c914e:L64-L91` | carry forward (shows both domains on one part). |
| 6 | Signal modules replace the receiver with a switch and wire receiver ActivationOut → lamp ActivationIn; goal Activated(lamp). | `tools/Campaign/Program.cs@a6c914e:L53-L63` | carry forward. |
| 7 | Typed switch → lamp connection: a duplicate connection is rejected; activation lights the lamp and Reset clears it; ports are ActivationOut → ActivationIn; foreign parts and undefined sockets cannot connect; an invalid socket never activates. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L139-L185` | carry forward. |
| 8 | Contact probe fixtures: drop (ball from y 3 above the switch at y 1), miss (x 2), gentle (y 1.481), approach speed just below, equal to and just above the 0.8 m/s threshold, and a base-box-only hit at x 0.85. | `reference/switch-lamp/Program.cs@a6c914e:L20-L55` | carry forward the fixture matrix (threshold is inclusive at equal); do not carry forward the half-precision adjacent-bit construction of below/above speeds (f16 values). |
| 9 | Authoring defaults: switch rotation step 0° (no rotation correction); trigger threshold 0.2 / 0.47 / 0.8. | `tools/Campaign/Program.cs@a6c914e:L544-L562` | carry forward the values; do not carry forward the string `part.Kind` switch. |
| 10 | power_trip reference: Bowling at (−3, 6, 0), lamp at (3, 1, 0), switch_1 at (−3, 1, 0) with switch_1 ActivationOut → lamp ActivationIn; goal Activated(lamp). | `content/puzzles.json@a6c914e:L276-L494` | carry forward (content survives). |

Files harvested:
- `tools/Campaign/Program.cs`
- `CuriousContraptions.tests/ElectricalTests.cs`
- `CuriousContraptions.tests/ConnectionPortTests.cs`
- `reference/switch-lamp/` (Program.cs; inputs.json and csproj hold no further knowledge)

## 5. Acceptance outline

Acceptance: [CAT-063](../requirements.md#current-cat-063), [retained behaviour](../requirements.md#todo-147) and [element-198](../requirements.md#element-198). Every row runs the production simulation; none depends on a switch-specific code path.

- **Construction.** Place Impact switch and Signal lamp; connect switch `ActivationOut` → lamp `ActivationIn`; drop a ball on the button. Electrical: Battery `Supply` → switch `PowerIn`, switch `Supply` → the load's `PowerIn`.
- **Positive.** A qualifying impact latches once; the lamp lights; with supply, the load is powered from the tick after the latch.
- **Negative/control.** A missed ball, a gentle impact below the threshold, duplicate impacts, a disconnected wire, a wrong-domain wire, missing or lost supply, a direct-wire bypass not winning (owed with `switched_motor`).
- **Boundaries.** Approach speed just below, equal to and just above the threshold; a base-only contact; a terminal contact.
- **Run/Reset, Save/Load.** Reset restores the unlatched button colour, both domains and the activation cursor; connections persist through save and load.
- **Integrations.** Activation, signal and timed-logic connection audit [sequence-task-279](../requirements.md#sequence-task-279) and electrical routing [sequence-task-278](../requirements.md#sequence-task-278); [IX-01 contact impulse](../requirements.md#interaction-01), [IX-07 signal propagation](../requirements.md#interaction-07) and [IX-06 electrical power transfer](../requirements.md#interaction-06); campaign first use 11–20 in the [element coverage ledger](../requirements.md#campaign-element-coverage). Partners: Signal lamp, Delay box, Battery, Motor, Fan, Powered gate, Bumper.
- **Planned in the button repair slice.**
  - `ContactActivationTests.TheLatchedButtonChangesColourAndStaysOnItsCollider`: given a latched switch, the declaration binds no position property of `Visual/Button`, the node keeps its authored local Y of 0.06 m, and its colour reaches (191, 245, 176)/255 once the 0.16 s blend has run. Control: an unlatched switch keeps the catalogue colour. Boundary: after Reset the colour is the catalogue colour again.
  - `SwitchElectricalTests.AMotorBehindTheSwitchTurnsOnlyAfterTheImpactAndCoastsDownWhenSupplyIsCut`: given Battery → switch → Motor and a ball above the button, the rotor is at rest until the impact, then reaches its 6 rad/s default within `MotorTests`' 2% speed allowance; when the Battery is disabled the rotor comes to rest and the switch stays Latched. Control: the ball misses and the rotor never turns. Stall guard 5 s for spin-up and 5 s for coast-down, both far above the Motor's tested times.

**Author-set contact variant (EL-198).** Requirement [element-198](../requirements.md#element-198): the physical form is met by the delivered `SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset`, `.UnqualifiedOrUnsuppliedContactCannotMoveConsumer` (its outcome "a closed contact cannot create supply energy"), `.SupplyLossAndRestorationDoNotEraseOrReplayImpactMemory` and `.TwoContactChainsAndCyclesAreSourceSeededAndOrderIndependent` (section 3); the author-set form by rows A1–A6, planned in `SwitchAuthorSetContactTests` and `SwitchAuthorSetContactPresentationTests`. Typed states, counts and refusals only; absence windows are 2 s of game time; the impact rows reuse the delivered thresholds and introduce no float bound.

| Row | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given `contact` = Closed and Battery → switch → consumer, when Run, then the consumer is powered from the first committed tick with no impact | `SwitchAuthorSetContactTests.AClosedAuthorSetContactPassesSupplyFromTheFirstTick` | `contact` = Open: never powered in 2 s | — |
| A2 | Given `contact` = Closed and no battery wire, then the consumer is never powered (requirement outcome) | `SwitchAuthorSetContactTests.AClosedContactWithoutSupplyCreatesNoPower` | A1 | — |
| A3 | Given `contact` = Open or Closed, when a ball strikes the button above threshold, then the contact state does not change and no `TriggerActivated` occurrence is published; a wire from `ActivationOut` is refused `InactivePort`; the button keeps its mode's colour | `SwitchAuthorSetContactTests.AnAuthorSetSwitchIgnoresImpactsAndOffersNoActivationOutput` | mode Impact latches, publishes one `TriggerActivated`, emits and turns the button to the pressed colour on the same strike | editing `contact` from Impact to Open while a wire leaves `ActivationOut` is refused `InactivePort` and the construction is unchanged; with the wire removed first, the edit is admitted |
| A4 | Given each of the three modes, when saved, loaded and Reset, then `contact`, `trigger_threshold` and both electrical wires round-trip and the authored state is restored; an undefined option index is refused atomically | `SwitchAuthorSetContactTests.TheContactChoiceRoundTripsAndAnUndefinedOptionIsRefused` | — | options 0 and 2 admitted; 3 and −1 refused |
| A5 | Given the three modes, then the button shows the catalogue colour (Open; Impact before a strike) or the pressed colour (Closed), never moves, and returns to its mode's colour on Reset | `SwitchAuthorSetContactPresentationTests.TheButtonShowsTheAuthoredContactState` | Closed with no battery wire still shows the pressed colour (the driver reads closure, not supply) | — |
| A6 | Given a stored save whose `switch` instance holds `trigger_threshold` only, when it is loaded, then it is refused whole with `WorkshopSaveRejection.OutOfRange` ("Missing or unknown declared parameters."; `engine/gpu/WorkshopSavePrevalidation.cs:108`–`:113`, `engine/gpu/InstanceParameters.cs:41`) and the current construction is unchanged; the same record is refused at the command boundary | `SwitchAuthorSetContactTests.ASaveWithoutTheContactParameterIsRefusedWhole` (a raw record built as `OldLayoutSaves` builds its controls, in the pattern of `PreConversionLaunchSaveTests.APreConversionRecordIsRefusedWithATypedReasonAtBothBoundaries`) | the same record with `contact` added loads | — |

Generic coverage: admission, Run/Reset and Save/Load of every admitted declaration (`CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`); wrong-kind wires (`PackageLocalPortsTests.WrongDomainAndWrongDirection_RefusesAtomically`). **Chrome recipe (pending, post-catalogue):** the `switched_motor` layout (Battery → switch `PowerIn`; switch `Supply` → Motor `PowerIn`; a ball above the switch), then set the switch to Closed in its configuration control and run without the ball.

## 6. Open questions

Resolved:

- **Which contact qualifies.** The trigger belongs to the whole body: a qualifying impact on base, button, a terminal or the socket fires it (decision, 25 October 2026; tested by a drop onto a terminal).
- **One part, two domains.** In mode Impact a qualifying impact latches once per Run. Missing or lost supply clears electrical power but not the latch; restoration powers a still-closed contact; Reset clears both, the latch and the power. In the author-set modes nothing latches: Reset restores power to the authored state (Closed passes supply again from the first electrical solve; Open never does).
- **Cycles and fan-in.** A switched contact's `PowerIn` admits fan-in so that sourced cycles are coherent; duplicate and wrong-domain links are refused; cycles never create power. A finite-work store keeps one direct source; routing a store through a switch is admitted and binds the store to the upstream source (`ContactWorkInstanceTests.SwitchedStoreRoutingIsAdmittedAndBindsStorage`).
- **Button travel (audit C1, owner item O1).** Decided: colour-only latch indication, as section 3 describes.
- **EL-198 is not a Duplicate closure (audit H2), and this spec holds its declaration.** Its row reads "Physical or author-set contact state"; the author-set state is a variant with a parameter of its own, declared, accepted and delivered here (coordinator decisions 27 and 31, 23 November 2026); [EL-198](EL-198-switch.md) points here.
- **Author-set form (EL-198).** A `contact` choice on the delivered `switch` (D8, D19): players do not see two switch entries for one part, and the ledger names one identity.
- **Runtime toggling (EL-198).** No; `contact` is build-only (standing delegation). A hand-flipped switch during Run would let a player solve a timing puzzle by hand; a lesson that wants a manual supply cut has the Battery's run-controlled `enabled`.
- **One parameter or two (EL-198).** One three-option choice (standing delegation), for the reason in section 2.
- **EL-198's coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) Forward-only: `contact` is required; authored content and fixtures are updated in the variant slice and a save without it is refused with the existing typed refusal (row A6), because the loader requires the complete declared parameter set (`engine/gpu/InstanceParameters.cs:41`) and the owner's rule forbids a default-on-load or migration. (b) An author-set switch is defined by what can be observed (row A3): the contact ignores the strike, no `TriggerActivated` is published, the activation port cannot be wired and the button's colour does not change; so the impact node and its event are present only in mode Impact (`ENGINE-PARAMETER-BOUND-FRAMES`; second audit, 23 November 2026). (c) The per-option contact control and the `CommittedContactClosed` driver are the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES`. (d) Coverage stays the catalogue key `switch`. (e) The button repair applies in every mode: no `LocalY` travel.
- **Motor on a switched supply.** Settled as untested in composition (section 3); one planned test.

Needs the owner: none.

## 7. Integration

- **Collaborators.** The delivered balls and the Weight (strikers); Signal lamp, Delay box, Counter, latch and gates (signal); Battery (source); Motor, Fan, Powered gate and Bumper (supplied loads). All delivered.
- **Campaign.** Placed in 19 levels and in the inventory of 11 in `content/puzzles.json`. `spring_signal` uses delivered parts only (Early level proof batch 1, "Signals" group) and runs after the engine programme (owner decision 3a). `power_trip` and `switched_motor` wait for ENGINE-GOALS: `power_trip`'s single `activated` goal needs a distinct fixed predecessor node, and its fixed parts are a ball and the lamp (`_bmad-output/implementation-artifacts/spec-early-level-proof-batch-1.md:65`). "switch" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150). No planned level uses the author-set modes.
- **Coverage binding.** Catalogue key `switch` in `docs/coverage/catalogue-elements.json`, which also covers EL-198 (coordinator decision 27); EL-198 gets no named binding. The lessons place the switch as an ordinary part; only a level that places a pre-set switch as a locked fixture needs [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme.

## 8. Delivery

- **Disposition.** Delivered: package `switch`. Records under `_bmad-output/implementation-artifacts/`: `review-c-switch-generic-impact.md`, `review-9-5b-switch-electrical.md`, `review-9a-pkg-3.md`.
- **Residual, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. (1) The button repair slice (element tier; files in section 3, tests in section 5). (2) `power_trip` and `switched_motor` after ENGINE-GOALS. (3) The author-set contact variant (EL-198) after the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` and the choice-bound node and event of `ENGINE-PARAMETER-BOUND-FRAMES` (and after `ENGINE-CONTACT-OBSERVATIONS`, which the programme delivers); the button repair lands first or in the same slice; S7 is not needed (catalogue part). Chrome proof is queued for the post-catalogue phase.
- **Variant slice (EL-198).** One small slice on the delivered package: the `contact` parameter, the per-option controls, the impact node and event bound to mode Impact, the port availability, the selected colour bindings, the description sentence and rows A1–A6. EL-198 then counts as implemented with this part's proof of the physical form. Files: `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs`; the regenerated `elements/elements.bundle.json`; `CuriousContraptions.tests/SwitchAuthorSetContactTests.cs` and `SwitchAuthorSetContactPresentationTests.cs` (compiled by the test project's one top-level rule); every test and helper that builds a `switch` instance with an explicit parameter list gains `contact` (`SwitchElectricalTests`, `ContactActivationTests`, `ComposableSupplyRoutingTests`, `DelayedSignalTests`, `LinearPusherIntegrationTests`, `PackageRuntimeParityTests`, `PalettePlacementTests`, `F32LaneRoundTripTests`, `WorkshopSaveTests`, `WorkshopWireFlatBuffersTests` name `trigger_threshold` today; each is re-read); any committed binary save fixture under `CuriousContraptions.tests/Fixtures/` that is still expected to load and holds a switch is regenerated (the slice's probe lists them); the EL-198 ledger entry, this spec's status and sprint status. Lessons in `content/puzzles.json` place a `switch` by kind and take declared defaults at construction (`engine/gpu/WorkshopConstruction.cs:37`); the slice confirms no lesson row lists the switch's parameters explicitly and edits any that does. Review tier: data-only if the planning probe finds the three missing variant rows of section 3 delivered; otherwise engine tier.
- **Disqualifiers.** A second trigger rule beside the declared contact node; activation memory erased by supply loss; any binding that moves solid art away from its collider; port availability not honoured for a signal output, or an impact node or event that cannot be bound to a Choice option (A3); the contact-set-by-a-parameter row delivering a run-editable control only; any proposal to load an instance without `contact` (a default-on-load is a compatibility path and a review Fail).
