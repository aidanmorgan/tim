# Audit: element specs, Batch K (Optical; 28)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` at `8d1d68aa` plus drafts. Nothing was built or run. All 28 specs were read (leads in full, siblings by their own rows). Code citations were opened for ten specs (EL-138, 143, 146, 153, 154, 155, 175, 176, 210, 214) and all 18 closures plus EL-210 were compared with their CAT specs. Spec paths are under `docs/planning/elements/`. `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`.

## 1. Verdicts

0 Ready, 26 Ready with fixes, 2 Not ready. Conformance: 0 Conforms, 28 Conform with fixes (X-1 applies to every spec; none asks for element-specific engine code). No Batch K package is delivered, and a search of `ABS*`, `SIM/`, `CuriousContraptions.Bundle` and `engine/` finds no optical symbol or package id, so there is no engine refactor to record.

| Spec | Verdict | Conformance | Reason |
|---|---|---|---|
| EL-176 Red laser | Not ready | with fixes | Owns section 10, which has C-1 to C-9; the element itself needs only X-1 and M-9 |
| EL-177, EL-178 | Ready with fixes | with fixes | X-1; prerequisites omit their own and foreign receivers (L-5) |
| EL-138 AND gate | Ready with fixes | with fixes (point 2: C-4) | Closure rows differ from CAT-043 (D-1); relay logic (C-4) |
| EL-139 to EL-142 | Ready with fixes | with fixes | Inherited EL-138 rows are false for XOR, NOR, NAND (D-2) |
| EL-143 to EL-145 | Ready with fixes | with fixes | D-1; pane crossing rule (C-6) |
| EL-146 to EL-152 | Ready with fixes | with fixes | D-1; inherited purity and hold rows undefined for siblings (D-3); C-4 |
| EL-153 Light sensor | Ready with fixes | with fixes | Two marginal rows (L-1); C-3, C-4 |
| EL-154 Light-charge | Ready with fixes | with fixes | One weak present row (M-9); C-3 |
| EL-155 Pull-cord light | Ready with fixes | with fixes | R18 is present-untested, wrong owner (M-5); knob geometry (M-6) |
| EL-173 Diverging lens | Ready with fixes | with fixes | C-3 decides its headline row |
| EL-174 Prism | Ready with fixes | with fixes | Caps hang below the foot; aperture 2 cm inside the pane (M-7) |
| EL-175 Optical fibre | Not ready | with fixes | The cable crosses the Wall in its own headline row (H-1); link rules open (M-8) |
| EL-210 Flashlight | Ready with fixes | with fixes | Contradicts CAT-029 on filters in a cone (H-2); latch plus supplied consumer untested (M-9) |
| EL-212 Flat mirror | Ready with fixes | with fixes | Rotation sign error (M-4); D-1 |
| EL-213 Combiner, EL-214 Detector | Ready with fixes | with fixes | D-1; EL-214 closure cannot hold as worded (D-4) |

X-1 is finding C-2 below (owner decision 3a absent from every spec).

## 2. Findings on the optical conventions (`EL-176:164-184`, `readiness/batch-k.md:46-60`)

These block Batches C, H and O as well.

### High

**C-1. The tick order cannot be built as written.** `EL-176:172`, `:107`; `batch-k.md:53`, `:97`; repeated in `RAD-08:203` and `CAT-036:68`. The text has the solve in Phase 2, before the substeps, reading "this tick's emitter grants". Code: `SIM/WorkshopGpuDevice.cs:205` only resolves routing booleans from committed state; energy is reserved and granted inside each substep (`SIM/WorkshopPhysicsEngine.cs:1011` → `SIM/WorkshopPhysicsEngine.Supply.cs:77-86`, `:107`). No grant exists before `WorkshopPhysicsEngine.cs:348`. Batch C says the opposite order: optics trace "against that committed pose in Phase 2 of the same tick" after physics (`CAT-007:138`, `readiness/batch-c.md:76`). A solve before the substeps also commits segments drawn against last tick's poses beside this tick's poses (0.5 m apart for a 64 m/s body). Replacement for item 4: "One solve per 120 Hz tick, after the last substep and before commit. It reads the energy granted to each emitter over this tick's substeps, this tick's final collider poses, and signal levels as resolved at the start of this tick. Segments, readings and poses commit together. Readings enter the signal network with the tick's other observations and act from the next tick (as contact loads do, `WorkshopGpuDevice.cs:245`), so light closes a supplied contact one tick later." The one-tick latency every spec states is unchanged; "supply lost removes the beam in that tick" stays true. `docs/gpu-f32-physics.md:169` draws optics before the substeps; DOC-ENGINE-STATUS should record the difference.

**C-2. Owner decision 3a is applied nowhere.** No Batch K spec or the summary names `ENGINE-SPATIAL-QUERIES` or "blocked on the engine programme (owner decision 3a)" (0 hits in 29 files); `batch-k.md:136` says the engine audit is not in the checkout, and it now is. The CAT specs they close on already carry it (`CAT-043:14`, `CAT-036:14`). Add to every Status and Prerequisites row: "blocked on the engine programme (owner decision 3a, 22 November 2026); ENGINE-OPTICAL follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query." Replace "through the broadphase" with that query in items 8 and 9 and in rows M2 and R9.

**C-3. The receiver law does not conserve energy, though the story text promises it.** `EL-176:181`; `batch-k.md:99` ("received power summed over receivers never exceeds emitted power"); `RAD-08:202`. The cap min(P, A × Σ wE) is per receiver. Three detectors (0.950 m² each, centres 0.64 m off axis, inside the 0.80 m cone) 3 m from the torch read about 3 × 42 = 126 W from 90 W. Replacement: "For each diverging segment the solve sums the uncapped shares A × Σ wE of all receivers; if the sum exceeds the segment's power, every share is scaled by power ÷ sum." Add a row to `FlashlightTests` (three detectors, sum at most 90 W). Also require at declaration: per channel, the sum over a Relay's outlets of τ × mask is at most 1, refused by the bundle otherwise; today only ρ + τ ≤ 1 is stated.

**C-4. Three bespoke couplings from a receiver level where one general one exists.** `EL-176:179`, `:181`: a level "may close a supply contact or enable a relay", a Relay is "enabled by a declared two-input logic operation over two receiver levels", and Batch O adds "sets and clears a declared signal memory node" (`RAD-08:200`). The delivered pattern is one: a committed observation feeds a signal node, and a supply contact is controlled by a signal state (`ABS/SignalContactControl.cs:12`; contact loads at `WorkshopGpuDevice.cs:245`). A logic evaluator inside the optical solve would be a second rule for the same behaviour (review gate) and repeats the fixed two-input shape the engine audit lists as E2. Replacement: "A receiver commits a continuous reading and strength. Its level is a signal node of the kind 'threshold over a committed scalar, with a release ratio' owned by ENGINE-SIGNAL-NODES. Supply contacts use the delivered signal-state control; a Relay or emitter enable names a signal level; gate logic is the signal network's boolean node." Then rows R4 (EL-146, EL-153, EL-214) need no new contact control, and EL-138's "logic operations 1" is a signal node. Emitter enable becomes "optional; any signal level" (EL-155 has none).

**C-5. Absorb by default breaks more delivered parts than G5 lists.** `batch-k.md:109` names the Pipe and two bends. Also translucent with a collider: the Funnel shell and stubs (`elements/Funnel/funnel.tscn:88-100`; `DESIGN.md:353` says it "transmits gameplay light"), the Powered gate sleeve (`elements/PoweredGate/powered_gate.tscn:134`; with Absorb a beam across the sleeve is dark whatever the blade does), the Cannon jacket (`cannon.tscn:132`) and the Wound spring sleeve (`wound_spring.tscn:100`). Visible but without a collider, so light passes: the Trampoline bed (`trampoline.tscn:117`, a compliant surface), the Fan blades, ring and post (`fan.tscn`, only `col_base` and `col_hub`) and the delivered rope. Replacement for G5: "Seven delivered packages declare Transmit (1, 1, 1) on their clear shell colliders: Pipe, both bends, Funnel, Powered gate, Cannon, Wound spring. A compliant surface occludes as its declared sheet. The Fan's uncollided art and the rope are existing visible-solid defects through which light passes until their owners repair them." Tests: a beam through each shell reaches a detector; a ball inside the pipe blocks it.

**C-6. The segment query is under-specified for hollow, curved and transmitting colliders.** Items 5, 6 and 8 say "nearest hit". Needed and not stated: (a) exact segment tests for all seven admitted shapes including Tube, Frustum and Torus (ball bands and the Tennis seam are tori on dynamic bodies; a torus needs a quartic root in f32); (b) all crossings in order, with entry and exit, since a shadow ray is multiplied by every Transmit surface it crosses, a pipe shell is crossed twice, and Batch O attenuates by chord length (`RAD-08:185`); (c) a rule for a segment that starts on a surface (reflection, a filter pane 0.03 m thick, a Relay outlet on its own cube): "a response is applied once per crossing of a collider; the continuing segment starts at the exit point; a reflected or relayed segment ignores the face that produced it"; (d) a sample is unoccluded when the first absorbing crossing is the receiver's own aperture face (Batch O already says a receiver skips its own body); (e) an origin inside a collider emits nothing. One owner: add to ENGINE-SPATIAL-QUERIES "a segment cast returning ordered crossings (entry, exit, point, normal, collider, feature) for every admitted shape", used by airflow line of sight, optics, acoustics and radiation, deleting `AirflowLineClear`'s own test.

### Medium

**C-7. The work bound is real but mis-stated.** `EL-176:183`. D is not a compiled count: a Transmit spread makes diverging segments at run time, so the bound is N × 128 × (1 + K). The 128 cap comes from requirement `current-cat-008`, not "legacy fact S2". Splitters can reach the cap, so the story must fix which segments are kept: "breadth-first by depth, then by emitter and surface order". Replacement: "At most N × L segment casts and N × L × K shadow casts, L the declared segment limit; each cast costs what the spatial query costs."

**C-8. Not general over channels, which Batch O needs.** Item 7 has two strength rules, "minimum of the required channels, or mean of the three", and purity over "the total". With ten channels, state: "strength is Minimum over a declared channel set or WeightedSum with declared weights (the mean is weights of 1/3); purity is taken over the receiver's declared sensed set; facing is Lambert or Omni; an emitter or receiver frame may be dynamic." Light's surface response and radiation's chord attenuation should be one per-channel rule (transmittance = declared surface τ × exp(−μL), light defaulting to τ = 0), or the solve branches on channel class.

**C-9. Build-mode preview has no home.** Item 10 and row M8 of EL-212 run "the same solve" in Build, but colliders are compiled only at Run and ENGINE-SPATIAL-QUERIES builds its static tree at Run. The story must say where the preview's collider set comes from, with no second solve.

**Checked and sound.** One legacy unit = 10 W is payable from the delivered Battery (`BatteryElementDeclaration.cs:42-45`); emitted ≤ paid; the inverse-square law with a 1 m floor and uniform cone matches the hand calculations; the requirement values survive (0.35 / 0.175 of `current-cat-038` as 3.5 W and 1.75 W; 0.25 / 0.225 of CAT-043; 16 m, 15°, 8 m, 16 interactions).

## 3. Other findings

### High

**H-1. EL-175: the solid cable passes through the Wall.** `EL-175:103` (fixture: "a Wall behind A; coupler B beyond the Wall, linked to A") with `batch-k.md:107` ("a non-tensile strand is static between its two ports") and `EL-175:31` (path = straight distance). A straight solid strand from A to B crosses the obstacle the part exists to pass. Decision needed (see section 6); recommended: the cable is a flexible strand of player-chosen length up to 8 m that drapes like a rope, and the internal path is the strand length.

**H-2. Filters in a torch cone: K contradicts E.** Conventions `EL-176:171`, `EL-143:96`, `EL-146:118` (control: "the torch through a red filter turns it on"), `EL-173:105` and decision K-9 have a transmitting surface multiply cone light. `CAT-029:72` and `:177` say "mirrors, filters, splitters, combiners and gates do nothing to this cone except occlude it". Replacement for both CAT-029 lines: "Redirecting responses (Reflect, Relay, spread) apply to collimated segments only; a cone is multiplied by each Transmit surface between the lens and a sample and blocked by everything else."

**D-1. The 18 closures have no declaration delta, but their acceptance differs from the CAT specs.** Colliders, ports, parameters, units, thresholds and catalogue text agree in every pair. The rows do not: ±1 % and 12 ticks against ±2 % and 1 s under the same method names (`EL-138:105` vs `CAT-043:141`; `EL-146:112` vs `CAT-056:150`); different fixtures and expected values (red 9.0 W vs amber (9, 7.02, 2.88) W; `EL-149:61` "off at 9.5 W" vs `CAT-072:155` "matched"); CAT-only rows (pose, lamps, preview, returning ray, release hold); different ledger citations (`EL-138:20` "G1 to G8" while G9 exists; `CAT-043:17` cites G1 and G2). Replacement for each closure's section 1 and 5: "Acceptance is the CAT spec's rows; this identity's outcomes map to <CAT row ids>; the ledger row cites those tests." Delete the duplicate row tables or mark them superseded.

### Medium

**D-2. Gate siblings inherit false rows.** `EL-140:66`, `EL-141`, `EL-142` take "rows G2 to G9 of EL-138"; G3 ("A and B lit … carries 0.9") is a closed state for XOR, NOR and NAND, G8 "the second opens" is wrong for NOR and NAND, and EL-142 inherits "the gate is closed" on Reset while NAND starts open. Fixed by D-1 (CAT-044, 045, 047 state them correctly).

**D-3. Receiver siblings.** `EL-147:53` to `EL-152:56` inherit EL-146's S5 and S6; the S5 contaminant is green, the required channel of EL-147, EL-149 and EL-150. "Rows S1 to S10 below" lists S1 to S4 only; EL-152's S11 is outside its closure check. Fixed by D-1.

**D-4. EL-214:16** says B1 to B8 pass in the CAT-038 slice; B3 needs the splitter and B6 the Flashlight (`EL-214:103`). Use the D-1 wording; no row exercises the declared release ratio.

**M-1. Requirement criterion reinterpreted: `sequence-task-398`.** `batch-k.md:67`, `:121`. "Existing laser-only reception is not silently treated as flashlight reception" becomes a proof rule, and every laser detector now answers a torch. One law is the element-free choice, but rewording a requirement row is the owner's (section 6).

**M-2. Batch H's Solar panel still uses the old scale.** `EL-211:29`, `:67` (50 W/m² per unit, 240 W from a 90 W torch). Batch K's contradiction 1 stands; EL-211 must be re-derived in W/m² (about 82 W absorbed at 3 m; threshold 17.5 W/m²).

**M-3. `DESIGN.md` edits are not proposed.** K-18 prints flat what `DESIGN.md:402` calls "raised bars" and makes rims, discs and marks flush (`DESIGN.md:346-350`). Owner decision 4 lists eight places; these are not among them. Add a gap entry with the replacement sentences.

**M-4. EL-212:91, :98.** "Turned −45° about Z … leaves along −Y" is wrong: n = (−0.707, +0.707, 0) sends +X to +Y. `CAT-041:49` has n = (−0.7071, −0.7071, 0). Replace with "+45° about Z"; M-number ids are also used twice (probe and rows).

**M-5. EL-155:70, :19, `batch-k.md:105` (G3).** Every prismatic slot publishes an actuator row, passive included (`engine/gpu/PhysicsPresentationRead.cs:25-43`; admission `CuriousContraptions.Bundle/SupplyBundleValidation.cs:78-85`). R18 is "present, untested on a passive slider"; delete the ENGINE-ASSEMBLY-JOINTS blocker and G3. If the probe fails, the owner under 3a is ENGINE-SIGNAL-NODES (threshold over a committed scalar).

**M-6. EL-155:29-30.** A 0.2 m rod has its lower end at −0.1 m in the knob frame, but the sphere and `Tie` are at (0, −0.2, 0). State the rod's centre (0, −0.1, 0).

**M-7. EL-174:30-31.** The caps (radius 0.577 m) reach y −0.577, 0.15 m below the foot's underside (−0.43): move the foot to (0, −0.637, 0). The inlet and outlet centres lie in the panes' mid-planes, not on their outer faces: add 0.02 m along each normal.

**M-8. EL-175:30.** "Exactly one connection per port" and "refused beyond 8 m" are new connection rules (delivered validation refuses only a duplicate link); nothing says what happens when a linked coupler is moved past 8 m. Add to M8: "a port may declare a maximum connection count and a connection kind a maximum length, checked at edit and at Run with a typed refusal".

**M-9. Present rows that the cited tests do not prove.** R5 "cleared by Reset" with no Reset port (`EL-176:68`): `StateLatchTests.cs:238` clears by a pulse. A latch enabling an electrically supplied consumer (`EL-176:25`, `EL-210:23`): the Fan's `ActivationIn` exists only in Self-contained mode (`FanElementDeclaration.cs:58-60`). The Signal lamp is not "a supplied indicator" (no `ISupplyElement`). R15 of EL-154 and R7 test pure functions, not a compiled source or the per-phase allowance. Mark each "present, untested" and add the row to the first slice; review tier for CAT-036 is then engine.

### Low

- **L-1.** `EL-153:116` says "exactly two marks" at 73 %, 2 points from the 75 % mark under ±5 %: move the torch to 3.3 m (62 %). `EL-153:113`: 7.3 W against an 8 W threshold under ±10 %: use `threshold` 10 W for that case.
- **L-2.** Broken anchor `delivery-batches.md#1c-open-after-an-engine-slice` at `EL-143:16`, `EL-146:16`, `EL-176:18`; the slug ends `-55-identities-not-available-today`.
- **L-3.** Shared legacy facts are cited as S1 to S7; `CAT-036:106-113` has S8.
- **L-4.** `EL-176:79` says the story does not name the broadphase query; `epics.md:4437` does ("through the existing BVH").
- **L-5.** `EL-177`, `EL-178` section 8 omit the green and blue receivers and the foreign receivers their rows use; A10 is a theory over three lasers but is listed only in EL-177.
- **L-6.** EL-210 is `ElementCategory.Power` while the pull-cord light is Optics; `delivery-batches.md:147` still says "two activation modes".
- **L-7.** `ENGINE-CONNECTION-MATERIALS`, `ENGINE-SPATIAL-QUERIES` and `ENGINE-SIGNAL-NODES` are not in `epics.md`; EL-155 and EL-175 should say "proposed" wherever they name the first.

## 4. Cross-batch conflicts

1. Tick order: K and O (before substeps, previous poses) against C (after physics, same-tick poses). C-1 settles it; edit `RAD-08:203`, `CAT-036:68` and `batch-c.md:76` together.
2. What a receiver level drives: K (contact, relay logic) against O (memory node). C-4.
3. Cone light through Transmit surfaces: K against CAT-029. H-2.
4. Power scale: K against EL-211. M-2.
5. Query model: K's nearest hit against O's chord attenuation and ENGINE-SPATIAL-QUERIES' candidates-only contract. C-6.
6. Acceptance authority for closures: EL rows against CAT rows. D-1.

## 5. Engine-story gaps, with proposed text

The proposed ENGINE-OPTICAL text (`batch-k.md:93-99`) is element-free: it names no part, kind or package. It is not complete. Changes:

- Replace "one segment solve per tick that finds the nearest hit … through the broadphase" with: "one solve per tick after the last substep, taking ordered crossings from the spatial query interface's segment cast against every participating collider at this tick's final pose".
- Replace "a relay enabled by a two-input logic operation over receiver levels" and "a supply contact controlled by a receiver level" with: "a relay or emitter enabled by a declared signal level; a receiver whose committed strength feeds a signal threshold node".
- Replace the conservation line with: "emitted energy never exceeds energy paid; for each segment, power received by all receivers together never exceeds its power; a declared response whose outputs can exceed its input per channel is refused at bundle time".
- Replace the work line with the C-7 bound and add: "the story states the segment limit, the interaction limit and which segments are kept at the limit, with a measured before and after on a fixed reference scene (owner decision 3a)".
- Add: "the default response and every delivered package that declares another (C-5) are listed and tested"; "strength rules and purity are defined over a declared channel set so that further channels add no rule"; "the Build preview's collider source".
- ENGINE-SPATIAL-QUERIES: add the segment cast of C-6. ENGINE-SIGNAL-NODES: add "threshold with release ratio over a committed scalar from any domain". ENGINE-CONNECTION-MATERIALS: decide the non-tensile strand (H-1) before G4 is adopted.

## 6. Items for the owner

1. **Laser detectors answering torch light (M-1).** Recommended: accept one receiver law and reword `sequence-task-398` as G11 proposes; the alternative is a declared "collimated only" receiver attribute, which is a branch on segment kind.
2. **The fibre cable (H-1).** Recommended: a flexible draped strand with a chosen length; alternative: a straight strand refused where it crosses a collider, which limits the part to gaps a beam could mostly cross anyway. Blocks EL-175.
3. **Light through uncollided visible art (C-5): Fan and Trampoline bed.** Recommended: record against the existing visible-solid defects; no optical exception.

## 7. Not verified

- Nothing was run; every optical row is a design.
- Legacy `@a6c914e` citations, `named-elements.md` ledger rows and palette orders.
- Code citations of the 18 specs outside the ten sampled; sibling CAT specs were read as differences from their leads.
- Whether a contact node on a child body and a package-internal signal edge compile (EL-210 R20, R22), and stability of the 0.05 kg knob and button.
- The rotation sign convention assumed in M-4 is right-handed about +Z.
