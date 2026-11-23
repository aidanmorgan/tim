# Audit: element specs, Batch H (Pneumatic, Sound, Electrical and Control; 30)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` at `8d1d68aa` plus drafts; `origin/main` is `25df3cd5`. Nothing was built or run. All 30 specs were read; every cited `path:line` for *present* capabilities was opened in EL-037, 039, 042, 044, 045, 049, 180, 133, 181, 183, 196, 197 and 198, plus every claim another spec depends on. Spec paths are under `docs/planning/elements/`.

## 1. Verdicts

0 Ready, 21 Ready with fixes, 9 Not ready.

| Spec | Verdict | Reason |
|---|---|---|
| EL-037 Air compressor | Ready with fixes | Shaft load law, piston binding and pulley inertia missing (P-M6) |
| EL-038 Pneumatic hose | Not ready | Line start state contradicts the family's numbers; constants have no declared home; one-hose rule breaks the family (P-H1, H2, H4) |
| EL-039 Air reservoir | Ready with fixes | Family contract carries P-H1 to H3; dial stands 6 cm proud (P-M4) |
| EL-040 Release valve | Ready with fixes | Latch-open valve cannot give the exhaust-return and repeated-stroke outcomes assigned to it (P-H3) |
| EL-041 Directional valve | Ready with fixes | Check values use the opposite hose convention; exhaust has no socket (P-H1, H3) |
| EL-042 Air nozzle | Ready with fixes | Wrong rig in N-A6, failing boundary in N-A2, `MaximumPower` undeclared (P-M1, M2, M7) |
| EL-043 Pressure gauge | Not ready | P-A2 and P-A5 cannot be constructed with the declared ports (P-H2) |
| EL-044 Tuned sound meter | Ready with fixes | Prerequisites omit Story 14.3; row 5 needs later EL-179 (S-8) |
| EL-045 Air whistle | Not ready | Static airflow receiver is refused by the delivered validator; "no flow drain" is false (S-1, S-2) |
| EL-046 Listening horn | Ready with fixes | Rows 1 to 5 observe through an interface that does not exist (S-10) |
| EL-047 Exit horn | Ready with fixes | Loop test assigned to it by EL-048 is absent (S-11) |
| EL-048 Acoustic duct | Ready with fixes | Elbow geometry and loop rule underspecified (S-11, S-18) |
| EL-049 Acoustic resonator | Ready with fixes | Row 1 is false for a running clock; one value wrong (S-9) |
| EL-050 Acoustic screen | Ready with fixes | Sound; `delivery-batches.md:190` still lists reflection as open |
| EL-051 Acoustic dish | Not ready | Redirect rule cannot turn a corner; art class fails the delivered art test (S-5, S-6) |
| EL-052 Water-tuned bottle | Not ready | Same static-receiver refusal; marks contradict the band bars (S-1, S-12, S-13) |
| EL-133 to EL-137 gates | Ready with fixes | Correct against code; G6 wording and stale contract lines (E-13, E-14) |
| EL-179 Tone speaker | Ready with fixes | Partial-grant rule and `enabled` policy undefined (S-16) |
| EL-180 Pulse speaker | Not ready | Four deltas against CAT-061; the 2 J debit cannot be paid (S-3, S-4) |
| EL-181, EL-182 Edge detectors | Ready with fixes | Tick-order text contradicts the delivered network (E-3, E-10) |
| EL-183 Resettable counter | Not ready | Same-tick rule rests on latch semantics the counter lacks; collider list wrong (E-2, E-7) |
| EL-196 Battery | Ready with fixes | Unrecorded proud marks; non-existent symbol; B5 fixture (E-4, E-8, E-9) |
| EL-197 Electrical wire | Ready with fixes | Sound; colour and W3 coordinates missing |
| EL-198 Switch | Not ready | Save statement contradicts the loader and inverts forward-only; A3 unreachable (E-1, E-6) |
| EL-211 Solar panel | Not ready | Palette collision, missing art numbers, ENGINE-OPTICAL does not cover it (E-11, E-15) |

## 2. Findings, by severity

### High

**X-1. Wrong coverage binding in every package spec.** For example `EL-037:18,150`, `EL-044:15,215`, `EL-181:155`, `EL-183:151`, `readiness/batch-h.md:100-102`. They bind by a ledger `**Package:**` line. The owner decision in the author's brief says "never by a `**Package:**` ledger line"; `origin/main:tools/Coverage/NamedPackageSources.cs` reads `Marker = "## Package source binding"`. The S7 draft in this checkout (`spec-s7-coverage-named-identity-admission.md:23`) still describes the ledger line, which is where the specs took it from. Replace each with: "Final `## Package source binding` section in this spec, in the EL-239 form (Package, Named identity, Acceptance, Decision, Mode records). Preconditions not met today: one `todo-NNN` acceptance anchor and an `S###` anchor in `scope-corrections.md`; S7 widens this." The reader refuses duplicate package bindings, so EL-180 cannot bind `speaker`. The `**Connection:**` proposal for EL-038, 048 and 197 must be restated against this mechanism.

**P-H1. Hose initial state is contradictory and decides test outcomes.** `EL-038:33` restores the line "to the pressure of its upstream store on Reset". `EL-039:78` and `EL-038:59` compute with the line at ambient (247.7 kPa; 388.5 kPa sealed). `EL-041:74` (344, 298 kPa) and `EL-043:64` (286 kPa) only reproduce with the hose at 400 kPa. The gap exceeds F6 (2 kPa or 2%): under ambient, P-A1 reads 388.5 not 400 and P-A4 is Off; under upstream, A1 and H-A1 become 253.6 kPa and H-A3 shows no drop. Replacement for the F table and `EL-038:33`: "A line starts at the pressure and temperature of the store bound to its Output end; at ambient when that end has no store (valve outlet, compressor)." Then recompute every settled value and give H-A3 a new finite-capacity proof.

**P-H2. Gauge acceptance cannot be built.** `EL-038:28` says "exactly one hose per port"; `EL-043:134` says a gauge and a consumer "use the tank's two sockets", but the second socket is `GasIn` and Input to Input is refused by F3. P-A2, P-A5 and any lesson with a gauged tank feeding something cannot be constructed. The one-hose rule is new: delivered validation refuses only an identical duplicate link (`engine/gpu/WorkshopConnections.cs:134-137`). Replacement: delete "exactly one hose per port" and the second-hose clause of H-A7; a Fluid Output is a node that takes several hoses.

**P-H3. Requirement criterion weakened: exhaust return and exhaust to whistle.** `EL-040:28` latches open until Reset, with no `ResetIn`. `EL-039:179,184` (A9) and V12 need repeated strokes; `todo-426` says "visible spring returns it on exhaust … repeated depletion, venting" and "exhaust → whistle". EL-040's Exhaust mode removes `GasOut` (`EL-040:39`) and EL-041's exhaust "is not a socket" (`EL-041:141`), so nothing can feed EL-045's `GasIn`. This is not a D19 default. Replacement: A9, the `EL-039:183-185` recipe and V12 name EL-041 (`PortA` to the piston, `PortB` Sealed); EL-041 gains an `Exhaust` Fluid Output that vents when unconnected. Otherwise the owner must cut the criterion.

**P-H4. Hose constants would live in the engine.** `EL-038:26-31`: no declaration and no parameters, yet a fixed 4.0 × 10⁻⁴ m³ and 2.0 × 10⁻⁴ m². "Product constants belong to packages" (`_bmad/custom/element-free-engine-design.md:13`). Replacement: "Line volume and throat are declared data on the Fluid connection kind in the bundle, compiled to rows; the engine holds no hose constant."

**S-1. Static airflow receiver is refused.** `EL-045:31`, `EL-052:32` mark the receiver present. `CuriousContraptions.Bundle/AirflowDomain.cs:33-34` throws "Airflow receiver requires a dynamic body" for body 0 of a static element (opened). Replacement in both capability tables: "An airflow observation point on a static body: **missing**; owned by gap N6." Drop `IAirflowReceiverElement` from both interface lists.

**S-2. "No flow drain" is false.** `EL-045:84,136`, `EL-052:32`. `WorkshopPhysicsEngine.Supply.cs:48` sets demand to flow speed × impulse for every branch, largest at zero relative speed, so a stationary receiver would demand the Fan's full power. N6 must read: "excited by the committed flow speed of a declared airflow source at declared probe samples; a probe takes no force, adds no demand and may sit on a static body."

**S-3. The 2 J pulse debit cannot be paid.** `EL-180:33,94`. Each phase is capped at `power_limit × PhaseSeconds` (`engine/gpu/ElectricalSupply.cs:9`, 1/120 s; `PhysicsGpuAbi.Electrical.cs:82`): a default 120 W Battery grants 1 J per tick. Recommended: a declared 2 J pulse store charged at 10 W through the delivered `SupplyEnergyStoreTarget` (`SupplyContractValues.cs:51`); an emission needs a full store and empties it. That gives the 0.2 s re-arm from one rule. Rows 1, 4 and 5 and "Stores: none" change.

**S-4. EL-180 is not a duplicate today.** `EL-180:14`. CAT-061 has no stores (open question 4 "Owner decision"), re-arm in ticks, a rim torus 0.06 m proud (EL-180:66 redraws it flush) and category "Sound". Replacement disposition: "Refinement closed inside Story 14.3; a duplicate only after CAT-061 adopts section 2. Until then a variant with rows 1 to 7 as its own acceptance."

**S-5. The dish cannot do what it teaches.** `EL-051:30,57,66,101`. With a 20° acceptance half-angle the child beam returns within 30° of its source; "turns a corner" is impossible, and in the worked case the speaker cabinet covers the axis. Decision needed; recommended: acceptance half-angle 70°, axis beam and total-path law kept; row 1 speaker at 2 m and 60° off axis; row 3 boundaries 68° and 72°.

**E-1. EL-198 inverts forward-only.** `EL-198:151,153` say old placements "keep working because the new parameter has a default" and list refusal of an old save as a disqualifier. The loader requires the full set: `engine/gpu/InstanceParameters.cs:40` (`Count != schema.Count` → "Missing or unknown declared parameters", opened). Replace `:151`'s last sentence with: "Lessons place a `switch` with `properties: {}` and take declared defaults (`engine/gpu/WorkshopConstruction.cs:37`). A stored save written before this slice lacks `contact` and is refused whole; that is the forward-only rule. The slice regenerates committed save fixtures holding a switch and adds one refusal row." Delete the third disqualifier at `:153`.

**E-2. EL-183 same-tick rule does not match the counter.** `EL-183:74-75,93`, C4. The counter counts inline and pushes `ThresholdReached` in the same tick (`engine/gpu/ActivationNetwork.cs:531-541`, opened); only latch requests resolve at the boundary. C4 never says what happens when the coinciding arrival would cross. Use the story text in section 4 and add to C4: "at count target−1, an arrival and a reset in one tick: count 0, contact open, no `ThresholdReached`, in both source orders."

**E-3. EL-181 X6 propagation is wrong.** `EL-181:76,82`, inherited at `EL-182:53`. Routing is same-tick (`ActivationNetwork.cs:505-512`); the one-tick lag is signal state to the next electrical solve. Replacement: "The node samples the supply resolution of the current tick, where oscillators do, and its occurrence is routed in that same tick. A contact closed by that consumer passes supply from the next tick's electrical solve."

**E-4. Battery proud marks not recorded.** `EL-196:65` classes `Mark_0` to `Mark_3` and the plus sign as printed flat. They are `BoxMesh` 0.12 × 0.12 × 0.02 at z 0.40 (`elements/Battery/battery.tscn:30-32,68-70`, opened) with no collider. Replace with CAT-005's finding and repair (flush prints, bindings kept) and add it as a residual in section 8.

**E-6. EL-198 A3 is unreachable.** `EL-198:120`. Port availability only refuses wiring (`WorkshopConnections.cs:26-29`); in Open or Closed the contact still latches, publishes and moves the button. Replacement: "the contact state does not change; a wire from `ActivationOut` is refused `InactivePort`; the button does not move", with the four `Event` bindings selected by `contact == Impact`. No delivered driver reads "closed regardless of supply"; add it to the engine story.

### Medium

- **S-6.** `EL-051:46,117`: "arc chord" fails `VisibleSolidColliderTests.cs:186-232` (solid art is a box, full sphere or straight cylinder). Replacement: "`Dish`: straight cylinder r 0.75 × 0.2 m; concavity is shading on the +X face; solid, cylinder collider."
- **S-8.** `EL-044:221`, `batch-h.md:44`: prerequisites become "14.0, 14.2, 14.1, 14.3; row 5 runs in the EL-179 slice". Order: "…14.1, 14.3 (with EL-180), EL-044, EL-179…". `EL-045:150` also needs EL-044.
- **S-9.** `EL-049:84,118,122`: the law gives 0.289, 0.495, 0.643 (not 0.66). With the clock running, the level falls to 0.509 before pulse 4 and re-crosses, giving two pulses. Row 1: "a Clock stopped after its third pulse … exactly one `ActivationOut` pulse".
- **S-10.** `EL-046:115`, `EL-048:97`: "the typed observation port" matches nothing in code. Name it as a Story 14.0 read-model deliverable or observe through exit horn plus meter.
- **S-11.** `EL-046:78`, `EL-048:104`, `EL-047:107-114`: reword R5 "single traversal per duct by lineage; depth 4 bounds chains of distinct ducts", and add `ExitHornTests.ALoopDoesNotGrowAndASecondPulseRoutesAgain` to EL-047's table.
- **S-12, S-13.** `EL-052:52-57,67,118`: print bars in the regions (one, two, three bars, then a cross) and rename the state "Too full"; add an in-flight term to the conservation row or assert at rest.
- **S-16.** `EL-179:41,70,109,113`: state `enabled` as BuildOnly; define "granted less than declared power is silent and takes nothing" in gap 4 or change row 5; add one cone, range and occlusion row.
- **S-17.** Band bars "selected by `band`" (`EL-044:67`, `EL-045:67`, `EL-049:69`, `EL-180:69`) need art visibility chosen by a Choice parameter; no capability row covers it.
- **P-M1.** `EL-042:123`: replace with "Given a nozzle hosed directly to a 400 kPa / 150 kPa tank, the first committed…".
- **P-M2.** `EL-042:119`: about 0.4 N·s on a 1 kg Ball travels about 0.2 m. Replace with "the 150 kPa ball at 0.3 m travels more than 0.1 m".
- **P-M3.** `EL-043:110`: exact equality on a derived float. Use 306 000 Pa (On) and 294 000 Pa (Off).
- **P-M4.** `EL-039:32,69`: a flat r 0.18 m dial on an r 0.35 m tank stands 6 cm proud. Add a boss collider (cylinder r 0.18 m, z 0.30 to 0.36) or print the dial on the +X end cap. Foot centre to y −0.23.
- **P-M6.** `EL-037:27,29,43,74`: state the shaft resisting torque, for example min(`power_rating`/|ω|, 20 N·m), and C8's speed band; give the piston binding's cycles per kg and pulley inertia; drop "as the Fan's" (the Fan declares 480 at `FanElementDeclaration.cs:76`).
- **P-M7.** `EL-042:27`: add "maximum power T × 12 m/s"; decide a 40 N force cap, since thrust spans 4 to about 130 N.
- **P-M8.** `EL-039:105,114,187`: "within 2 × 10⁻⁶ J", "exactly zero" and "replays identically" are legacy precision. Change to "carry the law; tolerances per F6".
- **P-M9.** One 120 Hz tick of choked flow moves 1.57 g into a hose holding 0.48 g. Add to F5: "per-step transfer is clamped to the equalising amount".
- **E-7.** `EL-183:27,63-65`: the delivered fifteen colliders are housing, foot, face panel, nine dot lamps and three sockets (`CounterTests.cs:89-92`); the new package has sixteen. Events are at `CounterElementDeclaration.cs:74`.
- **E-8.** `EL-196:106` B5: 60 J lifts 1 kg about 6.1 m; a stalled drive does no work. Use the `MotorTests.cs:619` fixture with `capacity` 60 and `initial_charge` 0.25.
- **E-9.** `EL-196:69,81`, `EL-211:81`: `ElectricalSupply.Allocate` does not exist; the method is `Reserve` (`engine/gpu/ElectricalSupply.cs:11`, opened).
- **E-10.** `EL-181:98`: the cited latch test asserts no `Event` binding. Cite `ComposableSignalNodesTests.SameKindEventCuesSelectTheirExactSiblingAndReset` (`:718`). EL-183 row 6 likewise.
- **E-12.** `EL-196:108`, `EL-211:120`: the 3% is of rotor kinetic energy (`MotorTests.cs:42-45`), not of charge. Use: committed work ≤ debited energy ≤ 1.01 × work.
- **E-13.** G6 is in AC-133 only; EL-135 to 137 are silent. Add to the closure contract and the three specs: "G6 is executed once, through the Both gate; the provenance walk does not read the operation."
- **E-15.** EL-211: copy cell and mark positions from `CAT-059:27`; add selection radius; P2 reads 0.375 (one mark, no supply); bracket P4 at about 0.95 and 1.05.

### Low

- **E-14.** Stale references in `spec-closure-review-electrical-gates.md`: `:85` (row is "Catalogue"), `:114` (status wording), `:130` (text now near `EL-133:117`), `:160,210` ("44 parts" is 45). The `Compile`-line step is obsolete on main in that contract (`:111,150`), in the self-edge contract (`:95`) and in every Files line of the batch ("until S2").
- **E-16.** `EL-211:146`: chapter 6, levels 51 to 60 (`requirements.md:2291,2402`) governs; 21, 22 and 24 are ordinals in the 63-level file.
- `EL-039:60` cites `FanElementDeclaration.cs:18`; it is `:19`. `EL-050:24` Wall collider bindings are at `:40`. `EL-039:183` "Air drawer" contradicts F8. `EL-040:104` V4 is vacuous. `EL-045:28,69`: use a `Loc_Port_GasIn` locator; `EL-045:52`: name the four real outlets, in Pa. `EL-180:133,134` and `EL-047:107`: weak rows (single-hit drop; "3 or 4" emissions; move the meter to 3.3 m). `EL-197:31` colour is `#293954`.

## 3. Cross-batch conflicts

- **Palette orders collide.** Batch H uses 160 to 165, 170 to 178, 185 to 188. Batch I uses 100 + EL number, 153 to 174 (`readiness/batch-i.md:42`); Batch J uses 170 to 181 and 184 to 188 (`readiness/batch-j.md:101`). The bundle refuses duplicates. One band per batch must be assigned.
- **Gas units and ports.** `EL-230:52` uses kPa with ambient 100 kPa; Batch H uses Pa absolute with 101 325 Pa. `EL-230:32` declares `ChamberPort` Fluid Bidirectional; Batch H keeps Output to Input. EL-230 U3 vents through "the valve's exhaust" with a whistle, which P-H3 shows no Batch H valve can do. `batch-h.md:112` says EL-230 has no spec; it has.
- **One slice, two names.** Batch H: `engine-supply-drive-demand`. Batch J: ENGINE-SUPPLY-DEMAND, key `powered-part-demand-drain` (`batch-j.md:86`).

## 4. Engine-story gaps, with proposed text

- **ENGINE-GAS-NETWORK** (`epics.md:4423-4432`) covers lines, equalisation, commanded valves and pressure work only. `batch-h.md:86` says four clauses are "worded in EL-039 section 3"; that section has none. Add to *Missing*: a rigid fixed-volume store that loses and gains mass; a Fluid connection kind with line volume and throat as bundle data, a `LineEnd { Sealed, Vent }` free end, save field, Connect control and tube presentation; several hoses on one Output; one-way and routed paths with a closable exhaust port; a work target compressing ambient air with declared efficiency, rating and shaft load law; the converging-nozzle law; a gas-fed airflow source; a pressure-threshold signal node; presentation drivers for pressure, mass rate and regime; unit and port enum members; transfer clamped to equalisation. Acceptance: "a store on a Vent end falls to ambient and never below; on a Sealed end total mass is unchanged"; "mass enters only while work is supplied and stops at the rating"; "the threshold contact follows with hysteresis and passes no supply of its own"; "a receiver beyond the thrust-scaled reach receives zero impulse". Story 12.4 is an open-air gust and is non-blocking. Bellows charging of a reservoir (`sequence-task-393`) has no owner.
- **Story 14.0**, beyond the author's eleven: "an occurrence reaches a receiver at distance ÷ declared speed, lasts its declared duration, and is zero beyond its declared range"; "a receiver's level is the strongest admissible arrival, never a sum; an instance's own colliders do not occlude it and it does not hear itself"; a `WorkshopConnectionDomain` member for Audio with a per-connection setting and save; an observation-only flow probe (S-2). Gaps 5 and 10 should be generic ("a committed scalar observation of a declared kind") with the gas-rate and liquid-level observations owned by ENGINE-GAS-NETWORK and ENGINE-LIQUID, so no capability has two owners. Stories 14.0 to 14.4 each require a Chrome e2e file (`epics.md:4344` onward); replace with "Browser proof is post-catalogue."
- **Story 14.3:** "one emission per coalesced trigger on the next tick; minimum interval in seconds; each pulse paid by the declared rule; typed tone parameter; an unpowered trigger expires."
- **ENGINE-SIGNAL-EDGE:** "A signal node with a declared electrical input and a typed polarity (Rising, Falling), sampled beside the oscillators; one Pulse occurrence per transition, routed in the same tick, repeatable within a Run; `previous` is committed memory, false at admission and Reset. Acceptance: held input emits nothing; a one-tick opposite state re-arms; one fixture per polarity."
- **ENGINE-SIGNAL-COUNT-RESET:** "`Reset` is a Pulse terminal on a pulse-count node. Count and reset requests of a tick resolve once per node at the boundary, for every pulse-count node. Any reset gives count 0, phase Clear and no emission; otherwise arrivals count in order and a crossing emits `ThresholdReached` in the same tick. Existing Counter tests stay green unchanged."
- **ENGINE-SUPPLY-CONTACT-SETTING:** "A supply contact declares one control per option of a Choice parameter; a fixed open or closed control is added; a `CommittedContactClosed` presentation driver reads closure irrespective of supply."
- **ENGINE-SUPPLY-DEMAND:** add "each supplied drive declares demand = max torque or force × |target speed|; the grant scales the drive's limit; committed positive work, never above the grant, is debited; an exhausted source opens the route on the next solve", so a store-less source (EL-211) is bounded.
- **ENGINE-OPTICAL:** also needs emitter intensity and range under one distance law, a receiver with weighted sample points and a continuous committed reading exposed to presentation, and a store-less supply source bounded by that reading.
- **ENGINE-LIQUID:** free-stream capture at an open mouth (EL-052).

## 5. Items for the owner

1. P-H3: keep `todo-426` "exhaust → whistle" and exhaust return. Recommended: keep, through an `Exhaust` port on EL-041.
2. S-5: dish redirect geometry. Recommended: 70° acceptance half-angle.
3. S-3: pulse payment rule (CAT-061 question 4 already calls it an owner decision). Recommended: the 2 J pulse store.
4. Tube art without a collider. Real for the 0.16 to 0.18 m hose and duct tubes; recommended default as the summary states. Delivered wires are already uncollided cylinders (`ui/WorkshopConnections.cs:272-282`), so a duct drawn at wire width needs no decision.
5. EL-183: separate `reset_counter` or `ResetIn` on `counter`. Recommended: separate.
6. Palette bands per batch.

P-H1, P-H2 and X-1 are decidable under the delegation but must be settled once before any slice. Judged defensible: Pa absolute, the `supply` and `inlet` choices, no hose length, EL-050 attenuates only (the requirement row is a disjunction), no re-radiation, strongest arrival, own entries for EL-044 and EL-179, the `pipe_whistle` withdrawal (never a ledger identity), EL-198 build-only contact (the row says "physical or author-set"), EL-196 no recharge, EL-197 free wires.

## 6. Summary table

Package ids, counts, prerequisites and tiers in `readiness/batch-h.md` match the specs, except: S7 is missing from most sound prerequisite cells; EL-044's count omits two narrower rows; the "worded in EL-039 section 3" claim; the EL-230 "no spec yet" line; the Ledger and S7 proposals (X-1). The Fan-flow item under "Not verified" is now closed: flow is uniform inside the cone (`WorkshopPhysicsEngine.Airflow.cs:76-120`).

## 7. Not verified

- Bodies of most cited tests beyond their names and lines; about 90 methods were located, about ten bodies read.
- Legacy `a6c914e` citations; `WorkshopSaveCodec` internals; whether port availability is honoured for an output port; whether one input admits several links.
- The MotorTests rope fixture's travel (E-8); how the Fan's shaft input computes load.
- DESIGN.md authority for `#293954`, `#fff8e9` and `#e8b764` as part colours (`DESIGN.md:80` lists a different base palette).
- CAT-029 and EL-210, which EL-211 depends on; S8, which no document in this checkout defines.
- The spot-checks I repeated myself were X-1, S-1, S-3, E-1, E-2, E-4, E-9 and the spec lines of P-H1 to P-H3; the rest rests on three delegated sub-audits.
