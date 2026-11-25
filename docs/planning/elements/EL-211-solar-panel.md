# EL-211 · Solar panel — element readiness spec

**Current delivery (24 October 2026):** [EL-211 delivery slice](../invest/named-elements.md#element-211). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. The package, its declaration, its numbers and its acceptance rows are [CAT-059 Solar panel](CAT-059-solar_panel.md)'s: this part and CAT-059 are one package, and CAT-059's rows are the single acceptance authority for both (coordinator decision 16, 22 November 2026). Units, emission, tick order, occlusion, receivers, conserved shares and limits follow the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) (EL-176 section 10); this spec cites them and does not restate them. Propagated on 22 November 2026 to the decisions of that date: the earlier legacy-unit power scale, the store-less source and the one-Laser supply case are withdrawn, and the declaration follows CAT-059 (Batch E and Batch K hand-backs, `readiness/batch-e.md` section 5 and `readiness/batch-k.md` G8). Repaired 23 November 2026 after the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`): sections 2 and its art now point to CAT-059's single declaration text (coordinator decision 27), and the P5 and P6 restatements follow CAT-059's re-derived rows.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-211 · Solar panel |
| Type | Electrical |
| Anchor | [requirements.md#element-211](../requirements.md#element-211); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Flashlight, lamp, drawstring source, solar panel …", first use 51–60); [named-elements entry](../invest/named-elements.md#element-211); owner S508 |
| Related | Refines [CAT-059 Solar panel](CAT-059-solar_panel.md) ([current-cat-059](../requirements.md#current-cat-059)); light from [CAT-029](CAT-029-flashlight.md)/[EL-210 Flashlight](EL-210-flashlight.md) and the amber Laser ([CAT-036](CAT-036-laser.md)); supplies loads through [EL-197](EL-197-electrical-wire.md) like [EL-196](EL-196-battery.md); first consumer [CAT-042 Motor](CAT-042-motor.md). |
| Roadmap story | [EL-211 delivery slice](../invest/named-elements.md#element-211); canonical proof owner and prerequisite decisions are recorded there. Catalogue story 13.7 (CAT-059). |
| Status | not started; no package. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (the segment casts of the optical solve), `ENGINE-SIGNAL-NODES` (the threshold row for the availability node `available`) and `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; a drive's draw bounded by its grant, rows P5 and P6); `ENGINE-THERMAL` (the declared-constant credited store, CAT-059 row M10b; owner decision 18 places it before `ENGINE-OPTICAL`); then `ENGINE-OPTICAL`, amended to take its candidates from the spatial query interface; then the Flashlight (CAT-029) and, for row P10, the Laser (CAT-036). |
| Disposition | **To deliver as a single element, in the CAT-059 slice** (Story 13.7): one package satisfies both identities. EL-211 is **not a duplicate** of CAT-059 ([delivery-batches §2c](../invest/delivery-batches.md#2-duplicate-closures)): its residual is the declared efficiency, the bound "output never exceeds absorbed energy" and network acceptance with a first consumer (the Motor). Those are CAT-059 rows P5, P6 and P10, run as named tests inside the CAT-059 slice; the EL-211 ledger row cites them. |
| Planned package | id `solar_panel`, directory `elements/SolarPanel/` (`SolarPanel.csproj`, `SolarPanelElementDeclaration.cs`, `solar_panel.tscn`, `icon.svg`). Category Power; palette order 316, CAT-059's (one number for the package; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). |

## 2. Declaration

The `solar_panel` declaration, art and bindings are [CAT-059's "Declaration" section](CAT-059-solar_panel.md#declaration), the single declaration text of the package (coordinator decision 27, 23 November 2026); this identity adds none and restates none. Its residual rests on three of those declared values: the efficiency 0.2 (the absorbed-energy credit into the 0.30 J store `cell`, coordinator decisions 21 and 32), the availability node `available` (on at 31.5 W, release ratio 0.9), and the rule that no supply node reads the receiver.

**Variants.** The requirements row names no variant; CAT-059 lists no enumerated selector. The base declaration is the only required mode.

### Art and solidity

[CAT-059's art table](CAT-059-solar_panel.md#art-and-solidity) classes every node and states what moves it; this identity has no art of its own.

### Behaviour and numbers

All derivations are CAT-059's ("Behaviour and numbers"), on the optical conventions; they are not restated here. The figures that decide this identity's residual: a Flashlight at 3 m gives P_abs 81.2 W and 0.2 × 81.2 = 16.2 W converted (0.2 × 81.2 W × dt = 0.135 J credited per default 1/120 s tick, inside the 0.30 J store, which holds one 60 Hz tick of the largest credit); the conserved-share rule holds P_abs at or below the 90 W the torch radiates, so the credit never exceeds 18 W × dt (0.15 J a default tick); one amber Laser on the cells gives 21 W absorbed (two marks, no supply) and two give 42 W (four marks, 8.4 W credited). Output can never exceed 0.2 × absorbed energy, which is the requirement's "output never exceeds absorbed energy" with margin.

- **Tick order.** The [combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) of optical convention 4: one optical and radiation solve per committed tick of duration dt, after the last substep and before the thermal step; the energy booked on the panel's collider record in tick n is credited to `cell` at the end of tick n's solve (step 3), before tick n's thermal step and commit, and `available` reads tick n's reading at the start of tick n + 1, so the source draws tick n's light during tick n + 1's substeps. No row asserts the one-tick lag.
- **Consistency with delivered parts.** A free default Motor needs about 0.3 W of bearing loss after its spin-up, so 16.2 W turns it. A loaded Motor is limited by its grant under coordinator decision 27 (`ENGINE-SUPPLY-DEMAND`: a drive whose request exceeds the source's per-tick allowance has its torque scaled so its work that tick equals the energy granted): lifting a default 4 kg Weight on a 0.3 m rim tie, its load rises at no more than 16.2 W ÷ 39.2 N = 0.41 m/s (CAT-059 row P5).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-03.json), proof owner S508): ElectricalPower, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat (+ StateTransaction). The probe is [CAT-059's capability table](CAT-059-solar_panel.md) (rows P1 to P7, M9 to M11, MS and Q1), with one owner per untested, narrower or missing row: `ENGINE-SPATIAL-QUERIES` (Q1), `ENGINE-SUPPLY-DEMAND` (P6), `ENGINE-SIGNAL-NODES` (MS, the threshold row), `ENGINE-OPTICAL` (M9, M10a the continuous reading and its presentation driver, M10c the receiver credit into a store), and `ENGINE-THERMAL` for the declared-constant credited store (M10b, required: one row serving every credited store, Batch O G20) and for waste heat (M11, a later partner, not required now). The rows this identity's residual needs:

| # | Capability | Mark | Owner |
| --- | --- | --- | --- |
| 1 | Proportional sharing of one source's per-tick allowance among loads | present | `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`); `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| 2 | A supply contact closed by a committed signal state | present | `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`); `SwitchElectricalTests.UnqualifiedOrUnsuppliedContactCannotMoveConsumer` (`CuriousContraptions.tests/SwitchElectricalTests.cs:56`) |
| 3 | A supply source whose finite store has declared constant capacity, power limit, initial charge and enable, starts empty and is filled only by a credit | **narrower** (a source store takes four parameter ids only) | `ENGINE-THERMAL` (CAT-059 M10b; owner decision 18 and coordinator decision 21; Batch O G20) |
| 3b | The conserved credit, at the end of the optical and radiation solve, of a declared fraction of the energy booked on a receiver's absorbing collider record into one declared store, the remainder staying on the record for thermal T3 | **missing** (no receiver credits a store) | `ENGINE-OPTICAL` (CAT-059 M10c; coordinator decisions 21 and 26); the store it fills is row 3's, `ENGINE-THERMAL`'s |
| 4 | A threshold signal node with a release ratio over a committed scalar, nameable by the signal-state contact | **missing** | `ENGINE-SIGNAL-NODES`, threshold row (coordinator decision 16) |
| 5 | A drive's draw granted against its source's bound and its committed work debited | **narrower**: drive supply is binary | `ENGINE-SUPPLY-DEMAND` (`engine-supply-drive-demand`) |
| 6 | Optical emission, segment transport with occlusion, a receiver with weighted samples, conserved shares and a continuous committed reading exposed to presentation | **missing** | `ENGINE-OPTICAL`, with every candidate from `ENGINE-SPATIAL-QUERIES`' segment cast (optical conventions item 5) |
| 7 | Absorbed-but-unconverted energy becomes heat | **missing** | `ENGINE-THERMAL`; not required now (section 6 item 4) |

Counts: 2 present, 2 narrower, 4 missing. Nothing in this spec asks for element-specific engine code: the panel is a receiver declaration, a store credit, a threshold node, a source, a contact and bindings.

**Dependencies.** Flashlight (CAT-029, undelivered; planned before this slice), Laser (CAT-036, row P10), Motor (delivered), Wall (delivered, the occluder of `solar_shadow`), Impact switch and Delay box (delivered, `delayed_solar`).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 ([element-free engine design](../../../_bmad/custom/element-free-engine-design.md); AGENTS.md, Tri-Graph architecture). No `elements/SolarPanel` package exists yet, and no engine project names a solar panel (searched 22 November 2026). Verdict: **Conforms**: every missing or narrower capability has one owning story (rows 3 to 7).

1. **Declaration data only.** CAT-059's declaration (section 2): a scene, four colliders, one receiver with nine samples, one absorbed-energy credit, one store, one threshold signal node, one source, one contact, one socket and twelve bindings. The legacy per-part physics hook (fact P7) is not carried; nothing asks for a solver branch, callback or update loop for the panel.
2. **Generic laws.** Light: the diverging-segment distance law with occlusion and conserved shares, and the whole power of a collimated segment ending on the face (optical conventions items 1 to 8). Conversion: the absorbed-energy credit into a finite store (coordinator decision 21). Availability: the threshold row with its release ratio. Power: the supply network's reservation from an ordinary source on a store, through a signal-state contact. Motor draw: the drive-demand law of `ENGINE-SUPPLY-DEMAND`. No supply node reads the receiver (optical conventions item 9).
3. **Three layers.** *Colliders (physics graph):* four on the one static body (two boxes, one cylinder, one sphere); the panel box is also the optical occluder, because the optical solve reads colliders, never art. *Visuals (rendering graph):* the four solids plus thirteen flat prints (nine cells, four marks); no art node is a collider. *Animation (animation graph):* the four marks are recoloured by presentation bindings from the committed reading; every other art node is moved by nothing.
4. **One-way flow and typing.** Optics → store credit at the end of the solve and committed reading → threshold node → next tick's supply solve → presentation; nothing flows back from presentation. Ports are `PortId`; channels are a closed enum; reading, efficiency and store values are f32; no string in the behaviour.
5. **Flat state.** As CAT-059's engine conformance, point 5; this identity adds nothing.
6. **Performance fitness.** Static body, so it joins no island. Four primitives against the Battery's three (the post is a separate visible solid); the nine cell reliefs are printed rather than given colliders. The whole solve is bounded as optical conventions item 11 states: at most N × 128 segment casts, plus one shadow cast per sample point of each candidate receiver of each diverging segment, the candidates coming from `ENGINE-SPATIAL-QUERIES`' region query over the segment's cone and range; this panel adds nine sample casts only for diverging segments whose cone query returns it, and nothing is proportional to the scene.
7. **Tests are scenarios.** CAT-059 rows P1 to P10 run the production simulation; none depends on a panel-specific code path.

## 4. Sources and legacy

- Requirement row [element-211](../requirements.md#element-211): "Absorbed illumination converts to bounded electrical work through declared efficiency"; outcome "Darkness yields no power; output never exceeds absorbed energy". [current-cat-059](../requirements.md#current-cat-059): nine front-face samples, finite cone/range/facing, partial/full occlusion, ambient light never supplies power, paid downstream work obeys the admitted source law.
- [Story 13.7](../../../_bmad-output/planning-artifacts/epics.md#story-137-photovoltaic-solar-panel-energy-conversion-cat-059): "When illuminated by a Flashlight or Laser, then output electrical power energizes the motor."
- Named entry [element-211](../invest/named-elements.md#element-211), owner S508.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| P1 | Nine equal-area samples convert received game light into binary supply; no ambient-sky power, storage or voltage/current model. | `parts/SolarPanelPart.cs@a6c914e:L8-L9` | carry forward samples and no-ambient rule; replace binary supply with the store credit of coordinator decision 21 |
| P2 | Supply source = irradiance ≥ 1 (scalar threshold source kind). | `parts/SolarPanelPart.cs@a6c914e:L12-L12`, `parts/SolarPanelPart.cs@a6c914e:L36-L37`, `engine/SceneElectricalSource.cs@a6c914e:L26-L31` | carry forward the threshold as the availability level, converted to 17.5 W/m² (31.5 W over the face) under the optical conventions, with the 0.9 release ratio |
| P3 | Needs an active torch, facing (reversed panel reads 0), range (x = 7 reads 0) and a wire; supply does not latch — torch off gives irradiance 0 and no power. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166` | carry forward |
| P4 | A small occluder reduces irradiance without erasing all samples. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L211-L225` | carry forward |
| P5 | Flashlight emitter: lens (0.66, 0, 0), range 8, 15° half-angle, intensity 24 game units. | `parts/FlashlightPart.cs@a6c914e:L10-L13` | carry forward the geometry; the power is the optical conventions' 90 W radiant ([EL-210](EL-210-flashlight.md)) |
| P6 | Lessons: `solar_motor`, `solar_shadow` (wall between: panel must stay on the torch side), `delayed_solar` (switch → delay → torch; panel → motor; goal powered ≥ 1 s after the switch). | `content/puzzles.json@a6c914e:L5944-L6225`, `content/puzzles.json@a6c914e:L6226-L6574`, `content/puzzles.json@a6c914e:L6865-L7293`, `tools/Campaign/Program.cs@a6c914e:L225-L246`, `tools/Campaign/Program.cs@a6c914e:L266-L277` | carry forward (level inputs) |
| P7 | Activity and a Powered event set in a per-part physics hook. | `parts/SolarPanelPart.cs@a6c914e:L56-L60` | do not carry forward the per-element loop |

- Legacy art: `parts/SolarPanelPart.cs@a6c914e:L40-L54`; icon `ui/WorkshopIcons.cs@a6c914e:L94-L94`; palette row "Solar cells" ([DESIGN.md colour system](../../../DESIGN.md#colour-system)).
- **Files harvested:** `parts/SolarPanelPart.cs`, `parts/catalog/solar_panel.tres`, `parts/FlashlightPart.cs`, `engine/SceneElectricalSource.cs`, `CuriousContraptions.tests/LightTests.cs`, `content/puzzles.json` (three solar levels), `tools/Campaign/Program.cs` (solar modules), `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-211](../requirements.md#element-211), [current-cat-059](../requirements.md#current-cat-059) and Story 13.7. The rows, fixtures, tolerances (the optical family's one set, coordinator decision 21: ± 2 % power, 1 s guard) and planned class `SolarPanelTests` are [CAT-059's acceptance rows](CAT-059-solar_panel.md), the single authority for both identities; they are not copied here. This identity closes on three of them:

| EL-211 criterion | CAT-059 row | Planned test |
| --- | --- | --- |
| Declared efficiency; output never exceeds absorbed energy (requirement outcome) | P5, as CAT-059 states it: the panel's grant never exceeds 1.02 × 0.2 × the absorbed energy, the Motor's committed work never exceeds 1.02 × the grant, and the 4 kg load's rise speed stays within P5's 0.41 m/s bound | `SolarPanelTests.DeliveredWorkNeverExceedsTheDeclaredFractionOfAbsorbedEnergy` |
| Bounded output shared by several consumers | P6, as CAT-059 states it: two Motors on one panel are granted equal energy ± 2 %, and their sum obeys P5's bound | `SolarPanelTests.TwoLoadsShareThePanelsBoundedOutput` |
| "Flashlight or Laser" (Story 13.7) by the same absorbed-power law | P10: two amber Lasers on the cells give P_abs 42 W, four marks and 8.4 W to the Motor; one Laser gives 21 W, two marks and no supply | `SolarPanelTests.ALaserPowersThePanelByTheSameAbsorbedPowerLaw` (runs in whichever slice delivers second, the Laser or the panel) |

"Darkness yields no power" is CAT-059 rows P2 and P7. No row exceeds 30 s of game time.

**Chrome recipe (pending, post-catalogue).** Flashlight with a ball above its button; Solar panel 3 m in front, cells facing the lens (rotate gizmo); Connect panel `Supply` → Motor `PowerIn`. The ball presses the torch; four marks light; the motor turns.

## 6. Open questions

All resolved; needs the owner: none.

1. **Optical power scale.** Irradiance is W/m² directly under the optical conventions (decision K-1 of [readiness/batch-k.md](readiness/batch-k.md)); the earlier legacy-unit scale of this spec is withdrawn. The Flashlight radiates 90 W, so a panel at 3 m absorbs 81.2 W and converts 16.2 W (CAT-059).
2. **Finite-power model — coordinator decision 21.** Absorbed light becomes supply only as a credit of a declared fraction of absorbed energy into a finite store that an ordinary source draws from: here 0.2 × absorbed energy into the 0.30 J store `cell` (coordinator decision 32), with availability by the threshold node `available`. The earlier store-less source bounded by the reading is withdrawn.
3. **Threshold and release.** The legacy level of one unit, 17.5 W/m² (31.5 W over the face), is kept as the availability level so that four gold marks mean "supply is available"; the threshold row's release ratio 0.9 applies (CAT-059 decision; a shadow edge crossing the cells must not chatter the supply).
4. **Waste heat — not required now** (standing delegation). The row and outcome name no heating. If a later lesson needs it, the unconverted absorbed energy is a later-partner criterion owned by `ENGINE-THERMAL` (CAT-059 M11). Nothing in the requirement is dropped.
5. **Efficiency as a parameter — a declaration constant** (D13).
6. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) Palette order 316, CAT-059's (coordinator decision 36). (b) The marks move to y −0.55 when printed flush (adopted by CAT-059). (c) Coverage stays the catalogue key CAT-059. (d) The campaign authority is chapter 6, levels 51–60.

## 7. Integration

- **Collaborators.** Flashlight (CAT-029 / [EL-210](EL-210-flashlight.md), undelivered, required first); amber Laser (CAT-036, undelivered, row P10 only); Motor, Wall, Impact switch, Delay box, Battery (delivered).
- **Campaign.** Chapter 6, "A Bright Idea", levels 51–60 governs first use ([campaign chapters](../requirements.md#campaign-element-coverage): optics row first use 51–60, reuse 71–90, 111–125, 136–150). `content/puzzles.json` holds 21 `solar_motor`, 22 `solar_shadow` and 24 `delayed_solar` (numbers are authored positions before decision 24d, each paired with its plan level, 51a to 51c, in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)), the three planned levels blocked on the Flashlight and Solar panel ([delivery-batches §4](../invest/delivery-batches.md#4-early-level-proof)). `delayed_solar`'s goal ("powered ≥ 1 s after the switch") needs the goal-evaluation slices. Each level is authored into the playable shape in its own level slice (with the lesson-fixture story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22), for its locked torch).
- **Processes.** CAT-059 retained behaviour [todo-134](../requirements.md#todo-134); [IX-13 optical transport](../requirements.md#interaction-13), [IX-14 optical absorption](../requirements.md#interaction-14) ("Allocate only the absorbed fraction"), [IX-06 electrical power transfer](../requirements.md#interaction-06).
- **Coverage binding (planned, not yet live).** Catalogue key CAT-059 (`solar_panel`). A named binding (the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads) is not in the published form for a catalogue package; the coverage follow-on story S7 decides whether a refinement identity gets one, and widens the reader for companion packages and non-`EL` identities (owner decision 15). Lacking today in any case: the EL-211 ledger entry has no `todo-NNN` link and owner S508 has no anchor in `scope-corrections.md`. The EL-211 ledger row cites the part and CAT-059 rows P5, P6 and P10.

## 8. Delivery

- **Slice shape.** Single element: the CAT-059 slice delivers the package and closes EL-211 inside it by CAT-059 rows P5, P6 and P10 (a 2c identity checked and closed in the part's slice).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SIGNAL-NODES` (threshold row) and `ENGINE-SUPPLY-DEMAND` (`engine-supply-drive-demand`; required unconditionally, since owner decision 3a places it before any further element delivery, and rows P5 and P6 rest on it); `ENGINE-THERMAL` (the declared-constant credited store, row 3 of section 3; owner decision 18 places it before `ENGINE-OPTICAL`); then `ENGINE-OPTICAL` with the receiver, continuous reading and receiver credit (rows 3b and 6 of section 3) (its replacement text is proposed in `readiness/batch-k.md` G1); then the Flashlight (CAT-029); the Laser (CAT-036) for P10. S3 and S4 optional; S7 not needed for the slice.
- **Files the slice adds or edits.** As CAT-059's delivery section: `elements/SolarPanel/` (project, declaration, scene, icon); regenerated `elements/elements.bundle.json`; `CuriousContraptions.tests/SolarPanelTests.cs` (compiled by the test project's one top-level rule); the lists S3 still owns until S3 lands; ledger entry, CAT-059 spec status, sprint status, `TODO.md`. The three levels are separate level slices.
- **Review tier.** Data-only if the planning probe shows rows 3 to 6 delivered; engine tier if the slice has to add any of them (then that part is split out as its own engine slice first).
- **Disqualifiers (the slice's probe re-checks).** As CAT-059's: a receiver that reports only above or below a level; no reading driver for presentation; shares of a diverging segment not conserved; a store that cannot take fixed values or cannot be credited from a receiver's absorbed energy; any supply node reading the receiver directly; a threshold node without a release ratio or not nameable by the signal-state contact; `ENGINE-SUPPLY-DEMAND` not bounding a drive's draw (P5 and P6 cannot pass).
