# TH-34 · Timed toaster ejector — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-34 delivery slice](../invest/named-elements.md#thermal-34). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*" and "[TH-23](TH-23-expansion-rod.md#capability-probe) E*n*".

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-34 · Timed toaster ejector · Thermal |
| Anchors | [requirements.md#thermal-34](../requirements.md#thermal-34) (task `sequence-task-468`); [named-elements.md#thermal-34](../invest/named-elements.md#thermal-34) |
| Owner key / research | S599 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Reuses the heater declaration of [TH-04](TH-04-electrical-heating-plate.md); the timer contact of the delivered [CAT-033 Hold timer](CAT-033-hold_timer.md); the sprung slider of the delivered Springboard; supplied by [CAT-005 Battery](CAT-005-battery.md); its cargo can be read by [TH-21](TH-21-temperature-sensor.md) or be a [TH-37](TH-37-heat-sensitive-target.md). No CAT refines it. |
| Campaign | Intro 91; practice 92; reuse 99, 146 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SUPPLY-DEMAND (drive work debited); then ENGINE-THERMAL, and S7 for the companion package (owner decision 15). Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (A3 geometry, companion declaration, empty-cycle hazard); the slice declared not placeable, with a seat pose on the raised tray, on 23 November 2026 (owner decision 24b; coordinator decision 33). |

### Disposition

**To deliver as a single element with one companion package** (the slice). Planned package ids `toaster_ejector` (directory `elements/ToasterEjector/`, "Toaster") and `toast_slice` (`elements/ToastSlice/`, "Toast slice"), the slice declared not placeable: it has no palette entry and comes with each toaster at the toaster's seat pose on the raised tray (owner decision 24b; coordinator decision 33). It is an assembly of general capabilities only: a supplied heat target, a region link, a signal timer closing a supply contact, and a supplied slider with a return spring.

## 2. Declaration

Follows the Hold timer for the timed contact (`elements/HoldTimer/HoldTimerElementDeclaration.cs:26-28`, `:33`, `:40`), the Springboard for the sprung slider (`elements/Springboard/SpringboardElementDeclaration.cs:70`) and TH-04 for the heater.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement`, and ENGINE-THERMAL's node contract.
- **Bodies.** Body 0 static (housing). Body 1 dynamic (carriage) on slider 0 along Y. The slot runs along X: inside it is 0.48 m long, 0.08 m wide and 0.46 m deep.
- **Colliders (metres).**

  | Collider | Body | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_wall_front`, `col_wall_back` | 0 | (0, 0.25, ±0.12) | (0.30, 0.25, 0.08) | long walls |
  | `col_end_a`, `col_end_b` | 0 | (±0.27, 0.25, 0) | (0.03, 0.25, 0.04) | end walls |
  | `col_floor` | 0 | (0, 0.02, 0) | (0.24, 0.02, 0.04) | slot floor |
  | `col_carriage` | 1 | (0, 0.43, 0) at rest | (0.22, 0.015, 0.035) | tray |

  All boxes; friction 0.4, restitution 0.05. Six colliders: a slot needs four walls and a floor, and the tray is the moving body.
- **Slider 0.** Travel −0.20 m to 0 (rest, tray up); axial spring 100 N/m toward rest; damping 0.2 N·s/m; carriage 0.1 kg; `BodiesCollide: false`.
- **Drive.** A position drive on slider 0: target −0.20 m, at most 0.5 m/s, stopping acceleration 8 m/s², effort at most 40 N, power at most 12 W; free when unpowered, so the spring returns the tray.
- **Thermal node** (T2). `element`: 6.1 J/K (0.05 kg of steel at 122 J/(kg·K)), ambient conductance 0.05 W/K, no face. One **region link** (T3) fed by `element`: box centre (0, 0.345, 0), half extents (0.12, 0.10, 0.04), declared axis Z (T3: shares are projected on the 0.24 × 0.20 m XY face, nearest-first along Z, summed to at most 1), G_r = 8 W/K. A slice on the lowered tray fills that cross-section; on the raised tray it is wholly above the box.
- **Signal.** One timer node `timer` (rearming, as the Hold timer's), duration `toast_seconds`, started by `ActivationIn`.
- **Supply.** `PowerIn` → contact `timed`, closed while `timer` is counting → consumer `load` with two targets: a heat target on `element` (demand 100 W) and the drive of slider 0 (12 W). Total 112 W, within a default Battery's 120 W.
- **Ports.** `PortId.ActivationIn` (Signal, Input, `PortRole.ActivationIn`, pulse) at (0, 0.50, 0.16) facing +Y; `PortId.PowerIn` (Electrical, Input, `PortRole.PowerIn`) at (−0.30, 0.10, 0) facing −X.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `toast_seconds` | `ParameterKind.Real` | `ParameterUnit.Second` | 20 | 1 | 30 | player-editable unless the level locks it (D11); build-only; seconds of game time (D10) |

- **Presentation bindings.** `Visual/Tray` follows body 1's pose; `Visual/Window` scale ← committed timer remaining (the Hold timer's driver, `:48`); `Visual/Glow` emission ← committed temperature of `element`, 288 K to 340 K.
- **Seat pose** (coordinator decision 33). One declared seat pose for the companion slice: its centre at (0, 0.545, 0) in the toaster's frame, unturned, so it stands in the slot on the raised tray (tray top y 0.445 m; the slice is 0.20 m tall), its 0.24 m length along the slot and its 0.04 m thickness across it, 0.145 m proud of the mouth. Placing a toaster places one slice there; removing the toaster removes it.
- **Companion `toast_slice`** (owner decision 15: a companion package of TH-34, no ledger identity of its own; declared **not placeable**, owner decision 24b: it has no palette entry and appears only with its parent, at the seat pose above, and its parameters, of which it declares none, would be edited on it like any part's, coordinator decision 33; it is never spawned during a Run; it is never placed by itself and stays on its seat, but the player may delete it alone in Build and restore it from its toaster, and a level may place a toaster without it, coordinator decision 34). Directory `elements/ToastSlice/`, class `ToastSliceElementDeclaration`, display name "Toast slice". Interfaces `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` and ENGINE-THERMAL's node contract, as the Domino (`elements/Domino/DominoElementDeclaration.cs`). One free dynamic body: collider `col_slice`, box half extents (0.12, 0.10, 0.02) m; mass 0.1 kg at the box centre; inertia the solid-box rule from mass and extents (as the Domino); friction 0.5, restitution 0.1, rolling resistance 0. Node `slice`: 70 J/K (a declared value: 0.1 kg at 700 J/(kg·K), T11), initial temperature ambient, ambient conductance 0.33 W/K (2.5 × 0.132 m²), face `col_slice`, 16 W/K (soft). `Visual/Slice` albedo cream `#fff8e9` → warm brown ← its committed temperature, 288 K to 330 K. No ports or parameters. Icon: a square slice with a rounded crust edge on top. At a glance it differs from the Domino (a tall plain slab) by its square outline and the domed crust edge, a shape difference, not colour. Compiled counts: bodies 1 (dynamic); colliders 1 (box); thermal nodes 1; faces 1; ports 0; parameters 0; presentation bindings 1 (a colour binding over three channels counts as one).
- **Palette.** Toaster: `ElementCategory.Power`; order 340 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). The slice has no palette entry (not placeable: a companion, owner decision 24b).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing` cream `#fff8e9`, rounded outside | four wall boxes and a floor as the colliders, corners rounded inside them | solid, the five housing colliders | nothing |
| `Visual/Tray` gold `#f7cb52` | box 0.44 × 0.03 × 0.07 m | solid, `col_carriage` | body 1's pose |
| `Visual/Glow` | two flat strips printed flush on the slot's inner faces | decorative: printed flat, lit by a binding | presentation binding |
| `Visual/Window` cyan `#66b8c9` | a flat bar printed flush on the front wall | decorative: printed flat, scaled by a binding | presentation binding |
| sockets | shared socket gizmos, flush | decorative | nothing |
| `Visual/Slice` (companion) | box 0.24 × 0.20 × 0.04 m | solid, its collider | its body's pose |

Icon: a rounded box with a slot and a slice half out. It differs from the Heating plate by the slot and from the Springboard by the housing round the tray.

### Compiled counts

Toaster: bodies 2; colliders 6 (box); joints 1 (prismatic); drives 1; sensors 0; thermal nodes 1; region links 1; signal nodes 1; supply nodes 2 (contact, consumer); electrical nodes 2; ports 2; parameters 1; presentation bindings 2 plus one body-carried art node. Slice: bodies 1; colliders 1; thermal nodes 1; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1, R2, R5, R6, R8, R9, R11, R12 and TH-23 E1, E2. Its own rows:

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| Z1 | A supply contact closed while a signal timer counts, passing power only when a source is wired | present | `SignalStateContactControl` with `SignalContactState.TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`, `:12` | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply` (`CuriousContraptions.tests/HoldTimerTests.cs:88`, body read: powered for exactly the window; the unsupplied control counts and is never powered) |
| Z2 | A supplied slider drive that is free when unpowered, so the slider's own spring returns it | present, test not opened | `PrismaticInactiveResponse.Free`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:85` | `DeclaredPrismaticDriveTests.AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked` (not opened) |
| Z3 | One consumer with two targets of different kinds | present, untested: `Targets` is a list (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`), the delivered Fan declares one | — | none found |
| Z4 | A region link from a node to the face colliders inside a declared box | **missing** | none | none; owner ENGINE-THERMAL |
| Z5 | A drive's committed work debited from its source | narrower: a supplied drive is on or off and debits nothing (engine design audit, "Drives") | — | none; owner ENGINE-SUPPLY-DEMAND |
| Z6 | A companion package declared not placeable: no palette entry, placed with its parent at a seat pose the parent declares and removed with it | **missing**: every package declares a positive palette order today (`CuriousContraptions.Abstractions/ElementMetadata.cs:16`), and no package declares a seat pose for another | — | none; owner S7 |

Counts: present 7 (TH-04 R1, R2, R5, R6; E1, E2; Z1), untested 2 (Z2, Z3), narrower 2 (TH-04 R8, Z5), missing 5 (TH-04 R9, R11, R12; Z4, Z6). Owners: Z4, R8 and the TH-04 rows **ENGINE-THERMAL**; Z5 **ENGINE-SUPPLY-DEMAND**; Z6 **S7** (owner decision 24b; coordinator decision 33). Nothing here needs code that names this part: there is no "toaster cycle", only a timer, a contact, a heater and a sprung slider.

## 4. Sources and legacy

- **Requirement row** [thermal-34](../requirements.md#thermal-34): a supplied heater, finite thermal cargo and generic timer/latch/spring assembly perform warming and delayed ejection; each coupling is typed and energy-accounted; no variants.
- **Named entry** [thermal-34](../invest/named-elements.md#thermal-34): owner S599; "No supply cannot warm; an obstructed carriage cannot teleport the payload; time alone is not proof of temperature".
- **Research** per-element table: "heating plate + timer + spring eject"; watt rating, time; "warms cargo then pops it out"; lever ← state. Historical reference: the [todo-214](../requirements.md#todo-214) Sierra article link.
- **Element map** row TH-34: ElasticStorage, ElectricalDissipation, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, JointConstraint, RigidBodyDynamics, SensibleHeat, SignalPropagation, TemperatureSensing; composition note adds TimedCommand. Navigation only (D14).
- **Processes.** [IX-39](../requirements.md#interaction-39), [IX-18](../requirements.md#interaction-18), [IX-07](../requirements.md#interaction-07), [IX-17](../requirements.md#interaction-17).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for toaster, eject and toast terms. The timer declaration the earlier revision cited (`engine/gpu/ActivationTimers.cs@a6c914e:L66-L74`) is the harvest baseline only.

## 5. Acceptance

Point of truth: [thermal-34](../requirements.md#thermal-34) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **The cycle is a consequence.** A pulse starts the timer. While it counts, the contact is closed: if a source is wired, the drive pulls the tray down against its spring and the heater warms whatever is in the slot. When the timer ends the contact opens, the drive goes free and the spring throws the tray up. With no source the timer still counts and nothing else happens.
- **The "latch" is the powered hold-down.** The spring's energy is paid by the Battery through the drive on every cycle; nothing is preloaded at Run start. The earlier revision's authored 1.5 J preload and 256 W heater are withdrawn (the heater exceeded a default Battery).
- **Lowering.** Spring force at full travel 100 × 0.20 = 20 N, under the 40 N effort; peak power 20 × 0.5 = 10 W, under 12 W. Spring energy ½ × 100 × 0.20² = 2.0 J; gravity returns 0.2 kg × 9.81 × 0.20 = 0.39 J, so the drive supplies 1.61 J. The tray's downward acceleration is at most 8 m/s², below g, so the slice stays on it. Lowering takes about 0.5 s.
- **Warming.** 100 W into `element`, which leads the slice by 100 ÷ 8 = 12.5 K. Over 20 s: 2000 J in; the slice loses about 0.33 × 13 × 20 = 86 J and the element 0.05 × 28 × 20 = 28 J to the room; the element keeps 6.1 × 12.5 = 76 J more than if it tracked the slice: (70 + 6.1)·ΔT = 2000 − 114 − 76 gives a slice rise of 23.8 K, to 311.8 K. Over a 5 s cycle: (500 − 8 − 76) ÷ 76.1 = 5.5 K, to 293.5 K.
- **Ejection.** 2.0 J less 0.39 J of lift leaves 1.61 J in tray and slice at the rest position: v = √(2 × 1.61 ÷ 0.2) = 4.0 m/s. The tray stops at its limit; the slice (0.80 J) rises 4.0² ÷ (2 × 9.81) = 0.82 m further.
- **Time is not temperature.** With the Battery's `power_limit` at 30 W the two targets share it in proportion: the heater gets 30 × 100 ÷ 112 = 26.8 W. After the same 20 s the slice has risen (26.8 × 19.5 − 30 − 6.1 × 3.4) ÷ 76.1 = 6.2 K, to 294 K, and is ejected on time, still cold.
- **Obstruction.** A slice resting on the raised tray stands from y = 0.445 to 0.645 m (tray top 0.445, slice 0.20 m tall), 0.145 m proud of the slot's mouth at 0.50 m. A Wall placed with its underside 0.70 m above the toaster's base, across the slot, clears the resting slice by 0.055 m, so the slice can be loaded and lowered; on ejection the slice meets the Wall by contact and falls back, its top never passing 0.70 m.
- **Empty cycle, accepted hazard.** With nothing in the slot the region link has no face to heat, so `element` takes the 100 W alone: (2000 − about 150 J to the room) ÷ 6.1 J/K ≈ 300 K of rise over a 20 s cycle, about 590 K, above the tinder's 450 K (T11). As with the Heating plate's hazard (TH-04), this is a physical consequence a level may use; no cut-out is declared. The glow binding saturates at 340 K.
- **On a Heating plate (companion row).** A slice lying on a default-Battery plate: pair 64 × 16 ÷ 80 = 12.8 W/K; lumped 85.25 J/K over 0.83 W/K, time constant 102.7 s, final rise 144.6 K; at 10 s the mean rise is 144.6 × (1 − e^(−0.0974)) = 13.4 K and the slice trails the disc by about 98 W ÷ 12.8 W/K = 7.7 K, of which 15.25 ÷ 85.25 falls on the mean: the slice reads about 288 + 13.4 − 1.4 = 300 K.
- **Energy.** Battery debit over a default cycle = 2000 J of heat + 1.6 J of drive work.
- **Tick order.** The timer's state is resolved at the start of the tick with the supply routes; the grant made in tick N's substeps heats in the thermal step of tick N (T8); the drive acts in tick N's substeps. On expiry the route opens at the start of the next tick and the spring acts from that tick's first substep.
- **Run length.** Every row fits in the default 30 s (A1 needs about 22 s). A level that sets `toast_seconds` above 25 s declares a Run length of at least 40 s (ENGINE-RUN-LENGTH, owner decisions 13 and 17; maximum 300 s).
- **Tolerances** (T12): temperature 2 K; heights 25%; energy 2%. The hold window is a timer and is asserted in seconds (D10), not ticks.

### Rows

Planned class `ToasterEjectorTests` in `CuriousContraptions.tests`. Every row starts with the `toast_slice` that comes with the toaster at its seat pose on the raised tray (coordinator decision 33) unless it says otherwise; a row that needs another slice pose authors it directly, as the native tests place balls (`PayloadInstanceTests.Place`, `CuriousContraptions.tests/PayloadInstanceTests.cs:29`), the form EL-084 and EL-088 use for their companions.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a wired default Battery, the slice at its seat pose on the raised tray and one pulse on `ActivationIn`, then the tray lowers, the slice is thrown out 20 s later and is at 311.8 K as it leaves | `ATimedCycleWarmsTheSliceAndThrowsItOut` | unwired: the timer counts, the tray never lowers, the slice stays at 288 K and on the tray | `toast_seconds` 5: thrown out at 293.5 K | 2 K; release within 0.1 s of the timer's end; slice rises more than 0.4 m above the slot |
| A2 | Given A1 with the Battery's `power_limit` at 30 W, then the slice is thrown out after the same 20 s at under 297 K | `TimeAloneDoesNotWarmTheSlice` | A1 (311.8 K) | none | 2 K |
| A3 | Given A1 with a Wall across the slot, its underside 0.70 m above the toaster's base, then after the timer ends the slice's top never passes 0.70 m and the slice comes to rest below the Wall | `AnObstructedTrayDoesNotPassItsPayloadThroughAnObstacle` | A1 (the slice rises more than 0.4 m above the slot) | none | 0.01 m |
| A4 | Given A1, then the Battery's debit equals the heat stored in element and slice plus the ambient account plus the drive's work | `EveryJouleOfTheCycleIsPaidByTheBattery` | no pulse: debit 0 J | none | 2% (40 J) |
| A5 | Given a wired Battery and no pulse, then nothing warms and the tray stays up; given a level signal or a supply output wired to `ActivationIn`, the link is refused | `PowerAloneStartsNothing`; generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | A1 | none | exact; typed refusal |
| A6 | Given A1 then Reset, then tray, slice pose, both temperatures, the timer and the Battery are restored; a second Run repeats A1 | `ResetRestoresTheCycle` | none | none | as A1 |
| A7 | Given a placed, wired toaster with `toast_seconds` 12, when saved and loaded, then the value and both connections are kept | `SaveAndLoadKeepTheDurationAndConnections` | none | none | exact |
| A8 | `toast_seconds` 0.5 or 31 is refused | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 1 and 30 admitted | 0.5, 1, 30, 31 | typed refusal |
| A9 | Given a toaster's slice authored lying on a Heating plate wired to a default Battery, when 10 s pass, then the slice reads 300 K | `AToastSliceWarmsOnAnyHeatSource` (`ToastSliceTests`) | the same slice on the bench: under 0.01 K of change | none | 2 K |
| A10 | Given a placed toaster, then its slice is at the seat pose on the raised tray, centre (0, 0.545, 0) in the toaster's frame, at 288 K; given A9 then Reset, the slice is back at the pose it started the Run in and at 288 K; when the toaster is removed, its slice is removed with it; a drag of the slice in Build leaves it on its seat, deleting it alone leaves the toaster empty, and restoring it from the toaster returns it to the seat (coordinator decision 34) | `TheSliceComesWithTheToasterAndResetRestoresIt` (`ToastSliceTests`) | a toaster placed without its slice, then Reset: the tray stays empty | none | pose exact; 0.01 K |
| A11 | Given a placed toaster with its slice, when saved and loaded, then the slice's pose is kept and no run temperature is saved | `SaveAndLoadKeepTheSlicePose` (`ToastSliceTests`) | none | none | exact |
| A12 | Given a toaster with its slice authored out of the slot, on the bench beside it, on a wired default Battery and one pulse, then `element` passes 450 K before the timer ends and the Battery's debit is the heat stored plus the ambient account | `AnEmptyCycleHeatsOnlyTheElement` | A1 (with a slice the element stays below 330 K) | none | 2% of the debit |

Requirement clauses: no supply cannot warm (A1's control); an obstructed carriage (A3); time alone is not proof of temperature (A2); each coupling typed (A5) and energy-accounted (A4).

## 6. Open questions

Resolved:

1. **Game scale.** D5 as restated in the thermal conventions.
2. **Start trigger.** `ActivationIn` (D19): it is the delivered way to start a timer, and a falling ball's switch can pulse it.
3. **Cargo.** A companion `toast_slice` package, decided now: a package declares bodies joined to it, not a free second body, and any other thermal cargo that fits the slot is warmed by the same region link. The slice is not placeable (owner decision 24b, 23 November 2026): it comes with each toaster at the toaster's seat pose on the raised tray and is removed with it (coordinator decision 33); rows A9 and A12 author another slice pose directly.
4. **Map row.** TimedCommand is added by the slice's probe (D14); the "radiant slot coupling" is the region link of T3.
5. **Latch.** A powered hold-down against a return spring, decided now: it uses only delivered slider and contact capabilities and makes the spring's energy a paid quantity. A mechanical latch released by a signal would need the retention capability, which today requires a winding transmission (`PrismaticRetentionDescriptor`, `CuriousContraptions.Abstractions/PrismaticRetentionValues.cs:13`).
6. **Heater power.** 100 W, decided now, so heater and drive together stay inside a default Battery's 120 W.

Needs the owner: none. Needs the coordinator: none. Coordinator decision 34 settles the seated companion: the slice stays on its seat (the player cannot move it off), but the player may delete it alone in Build and restore it from the toaster, and a level may place the toaster without it. So a level that runs the toaster empty places it without its slice or lets the player delete it, and no lesson moves a seated slice onto a Heating plate.

## 7. Integration

- **Works with.** Battery, switches and timers that pulse (delivered); Wall (delivered); TH-21 and TH-37 to read or be the warmed cargo.
- **Campaign.** Introduction 91, practice 92; reuse 99, 146. The slice comes with the toaster at its seat pose on the raised tray, so a level needs no fixture to pre-place it; a fixed toaster needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element with its companion package, one review.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SUPPLY-DEMAND for Z5; ENGINE-CONTACT-OBSERVATIONS for the slice's touch link on a plate); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (Z4, the heat target; owner decision 18); TH-04; S7 with the companion-package rule and the not-placeable declaration with the parent's seat pose (owner decisions 15 and 24b; coordinator decision 33); S3 preferred.
- **Files.** `elements/ToasterEjector/` (`ToasterEjector.csproj`, `ToasterEjectorElementDeclaration.cs`, `toaster_ejector.tscn`, `icon.svg`); `elements/ToastSlice/` (`ToastSlice.csproj`, `ToastSliceElementDeclaration.cs`, `toast_slice.tscn`, `icon.svg`); two guarded lines in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ToasterEjectorTests.cs` and `ToastSliceTests.cs`; two rows in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if Z2 to Z6 and the thermal rows are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A consumer limited to one target; a drive that holds position when unpowered; no region link (the heater would then need a contact face, and the slot's geometry changes); a slice that leaves the tray while it lowers; a toaster placement that does not put its slice at the seat pose on the raised tray.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Seven colliders across two packages, one slider with a spring and a drive, one timer, one contact, one consumer with two targets, two nodes, one region link, two ports, one parameter.
2. **Generic laws.** The timer rule; a contact closed by a timer state; proportional supply grant; a position drive against a spring; contact; Q̇ = G·ΔT on a region link. "Toast pops when done" is these laws in sequence, and each can fail alone (A1's control, A2, A3).
3. **Three layers.** Colliders: housing walls, floor, tray, slice. Visuals: their art; printed glow strips and window. Animation: glow, window and slice colour from committed state. Nothing is written back.
4. **Flow and typing.** One Signal input (pulse) and one Electrical input; enum roles; f32 values; the duration in seconds.
5. **Flat state.** The counts in section 2.
6. **Performance.** The housing is static; one joint. The slice leaves at 4 m/s, a 0.04 m-thick box crossing 0.008 m per substep, well inside the engine's speculative contact margin. The region link costs one overlap term per face collider inside its box (T10).
7. **Tests are scenarios.** A1 to A7 and A9 to A12 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, on ENGINE-SUPPLY-DEMAND, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it; the reader also refuses two packages under one identity, which the companion slice needs.

| Field | Planned value |
| --- | --- |
| Packages | `toaster_ejector`, `toast_slice` |
| Named identity | TH-34, `../invest/named-elements.md#thermal-34` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-468` and the ledger entry has no todo link |
| Decision | S599; `scope-corrections.md` on published main has no `s599` block |
| Mode records | `Configuration=Fixed` |
