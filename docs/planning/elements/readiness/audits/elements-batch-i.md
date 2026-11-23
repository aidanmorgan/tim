# Audit: Batch I element specs (Mechanical 1, EL-053 to EL-074)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` (`8d1d68aa` plus drafts), published main `25df3cd5`. Nothing was built or run. All 22 specs were read. Every "present" row (symbol and test) was opened for EL-053, 054, 055, 056, 058, 059, 062, 064, 065, 069, 071 and 073; the others were spot-checked on the claims other specs depend on. Spec paths are under `docs/planning/elements/`; `Name:line` means that spec file.

## 1. Verdicts

| Spec | Verdict | Reason |
|---|---|---|
| EL-053 Rotary-to-linear converter | Ready with fixes | Citations hold. One presentation property does not exist (F5); binding (F1); energy allowance misquoted (F16). |
| EL-054 Linear-to-rotary converter | Ready with fixes | Citations hold; arithmetic checks. Binding (F1); energy allowance (F16). |
| EL-055 Mechanical brake | Ready with fixes | Citations hold. Belt rim does not match the drawn pulley (F14); binding (F1). |
| EL-056 Ratchet | Ready with fixes (blocked on engine story) | Pre-winch load in D5 does not load the shaft (F12); rim (F14); one row over-claimed (F20). |
| EL-057 Escapement | Ready with fixes (blocked) | E6 cannot pass (F11); Empty threshold undeclared (F13); wrong statement about EL-061 (F13). |
| EL-058 Size grate | Ready with fixes | Pass margins halve against band and seam colliders (F17); G1 control ill-posed. |
| EL-059 Weight tray | Ready with fixes | Sockets float; one stale statement about the plate (F22). |
| EL-060 Electromagnet | Ready with fixes (blocked) | Every holding and lift number ignores the Weight's collided eye; rows M1 and M6 false as written (F6). |
| EL-061 Indexed carousel | Not ready | No delivered drive can index it; the named Wound spring has no `Drive` port; needs an owner reading (F3). |
| EL-062 Docking ferry | Ready with fixes | Five port positions and the slider frame missing; F2 passes with the ball resting on the rail (F10). |
| EL-063 Cable winch | Ready with fixes (blocked) | W5 boundary impossible; "debit work" reading unstated; wound-end role undefined (F15). |
| EL-064 Metal loop anchor | Ready with fixes | No delta found against code and requirement; closure row C4 depends on the player cut that O2 would remove (F8). |
| EL-065 Load hook | Ready with fixes | Usable eye opening is 0.069 m at 0.25 kg, not 0.119 m; fallback does not fit (F9). |
| EL-066 Scissors | Not ready | The shared cutting contract has no engagement geometry or force law (F4); O2 omits the decision it reverses (F8). |
| EL-067 Steel cable | Not ready | A body-less, non-placeable package is a new bundle kind no story covers (F7); O1 gates acceptance (F8). |
| EL-068 Tin snips | Not ready | Inherits F4. |
| EL-069 Moving bucket | Ready with fixes | Row K2 cannot produce the spill (F18); pipe and funnel delivery claim wrong. |
| EL-070 Moving cage | Ready with fixes | C4 frame and outcome unspecified (F21). |
| EL-071 Straight metal ball pipe | Ready with fixes | A2 jam tolerance geometrically false; A1 bound gives a false failure (F19). |
| EL-072 Curved metal ball pipe | Not ready | Two packages under one identity is refused by published main and the drafted S7 (F2); inherits F19. |
| EL-073 Brick barrier | Ready with fixes | Retained CAT-066 maximum-speed boundary weakened (F23); deferral not recorded as a decision. |
| EL-074 Wood barrier | Ready with fixes | As EL-073. |

Counts: Ready 0, Ready with fixes 17, Not ready 5.

## 2. Findings, by severity

### High

**F1. Every spec binds coverage by the wrong mechanism (all 21 package specs, and the summary).**
- Where: the "Coverage binding" row of each identity table, the "Coverage binding" bullet in each section 7, and "After S7: one `**Package:**` ledger line" in each section 8 (for example EL-053:22, :166, :175; EL-069:18, :153, :160). Also `readiness/batch-i.md:7`, G7 and G8.
- Evidence: published main `fa86969f` binds a named package through a `## Package source binding` section in the identity's spec, read by `origin/main:tools/Coverage/NamedPackageSources.cs` (marker `:13`, regex `:55`-`:61`); example `origin/main:docs/planning/elements/EL-239-baseball.md:14`-`:20`. The owner decision in the author's brief says "never by a `**Package:**` ledger line". The drafted `_bmad-output/implementation-artifacts/spec-s7-coverage-named-identity-admission.md:23` is superseded on this point.
- Main's reader also requires (`NamedPackageSources.cs:93`-`:119`): one `Owner **Snnn**` and one `#todo-N` link in the ledger entry (met by all 22); an `<a id="snnn">` block in `docs/planning/invest/scope-corrections.md` (missing for all 22 owners: S339-S343, S367, S368, S642, S371, S372, S334, S332, S333, S335, S336, S357, S369, S370, S360, S361, S643, S644); and the todo anchor immediately followed by the sequence-task anchor in `requirements.md` (fails for all 22: todo-211, 375, 320, 193, 210, 192 and 199 are scope indexes far from sequence-task-606 to 627).
- Consequence: no Batch I package is admitted today. The 13 "ready to plan" rows are blocked on the coverage follow-on, the same blocker `readiness/new-identities.md:41` calls B1.
- Replacement for each "Coverage binding" sentence: "A `## Package source binding` section in this spec, in the syntax published main parses (`tools/Coverage/NamedPackageSources.cs`). This identity does not yet meet its preconditions (no scope-corrections block for its S owner; its todo and sequence-task are not co-anchored); blocked on the coverage follow-on (S7 as re-scoped)."
- Section text (EL-053 shown; the others substitute the values below):

```
## Package source binding

**Package:** rotary_to_linear_converter.
**Named identity:** [EL-053](../invest/named-elements.md#element-053).
**Acceptance:** [todo-211](../requirements.md#todo-211), [sequence-task-606](../requirements.md#sequence-task-606).
**Decision:** [S339](../invest/scope-corrections.md#s339).
**Mode records:** Configuration=Fixed.
```

| EL | Package | todo | sequence-task | Decision |
|---|---|---|---|---|
| 053 | rotary_to_linear_converter | 211 | 606 | S339 |
| 054 | linear_to_rotary_converter | 211 | 607 | S340 |
| 055 | mechanical_brake | 375 | 608 | S341 |
| 056 | ratchet | 375 | 609 | S342 |
| 057 | escapement | 375 | 610 | S343 |
| 058 | size_grate | 320 | 611 | S367 |
| 059 | weight_tray | 320 | 612 | S368 |
| 060 | electromagnet | 320 | 613 | S642 |
| 061 | indexed_carousel | 320 | 614 | S371 |
| 062 | docking_ferry | 320 | 615 | S372 |
| 063 | cable_winch | 320 | 616 | S334 |
| 064 | none (closure) | 193 | 617 | S332 |
| 065 | load_hook | 193 | 618 | S333 |
| 066 | scissors | 193 | 619 | S335 |
| 067 | steel_cable | 210 | 620 | S336 |
| 068 | tin_snips | 210 | 621 | S357 |
| 069 | moving_bucket | 192 | 622 | S369 |
| 070 | moving_cage | 192 | 623 | S370 |
| 071 | metal_pipe | 199 | 624 | S360 |
| 072 | see F2 | 199 | 625 | S361 |
| 073 | brick_barrier | 199 | 626 | S643 |
| 074 | wood_barrier | 199 | 627 | S644 |

`Mode records` is `Configuration=Fixed` for the Baseball; the right value for parameterised packages was not established (section 7).

**F2. EL-072: two packages under one identity is refused by every rule.** EL-072:16-18, :125, :129, :139-140.
- Main refuses one identity with two bindings three ways: one marker per spec (`NamedPackageSources.cs:86`), unique identity (`:45`), unique task source (`:47`). The drafted S7 refuses it too (row N3, `:56`). G7 is written against the superseded rule. It also conflicts with D8 (`delivery-batches.md:169`, "A mode inside one identity is a parameter of that entry"), which the spec does not cite.
- Decision needed, one of: (a) the coverage follow-on admits several packages per identity and task; (b) a new ledger identity for the 45-degree metal bend; (c) one package with an angle Choice, which needs choice-selected collider geometry (no such capability; engine tier).
- Replace "not an owner item" at :125 and :129 with an owner item; recommended default (a), fallback (b).

**F3. EL-061: the part cannot index with any delivered drive.** EL-061:18, :95, :119, :164, :177.
- The Wound spring is named as its finite-work drive, but its only ports are `DriveIn` and `ActivationIn` (`elements/WoundSpring/WoundSpringElementDeclaration.cs:57`-`:65`). The default Motor never completes an index by design (row C7); C1 passes only because the test cuts supply at 75 degrees. Behind EL-057, eight pulses make one index.
- Requirement: "A finite-work drive advances a carrier through indexed positions". As specified the part is a passive geared turntable.
- The story already lists "a signal-commanded strike-and-hold or position drive" for revolute coordinates (`epics.md:4459`); the spec replaced it with a periodic detent and a rest-window contact that the story does not list.
- Decision needed (owner; requirement reading). Recommended default: the carousel owns a supplied revolute position drive stepped 90 degrees per `ActivationIn` pulse, paid from `PowerIn`; obstruction stalls it; `IndexedOut` is kept. G2 then shrinks to the rest-window contact. If the passive design is kept: remove the Wound spring everywhere, state the eight-pulses relation, make EL-057 a prerequisite.
- Related: C5 and C6 (:150-151) need rotor start angles the package cannot set (move to the engine story's fixture); the cargo-contact row cites `ImpactLeverTests.cs:380`, a limit-stop test (mark present, untested; counts 9/1/1/1); the input wheel is sunk into the pedestal (:39-41, :64; use hinge frame (-1.7, 0.3, 0.32), port and rim at (-1.7, 0.3, 0.40)).

**F4. EL-066, EL-068 and gap G6: the cutting contract cannot be implemented as written.** EL-066:55, :74, rows X3, X5; EL-068:63, :82, rows T3, T8.
- Both cutter edges are lines through the pivot, so they meet a zero-thickness span only at full closure. "The work the coordinate delivers against the span" has no start angle, distance or resisting force. Yet X3 and T3 assert the blade "stops on the rope more than 0.05 rad short of closed" and X5 and T8 assert 1 J and 25 J debits.
- Proposed text for the contract and G6: "The connection material declares a strand diameter d (m). The span engages when the perpendicular gap between the two edges at the crossing point falls to d. While engaged it opposes closing with force R/d at the crossing point. If R is at most C the route is severed when the gap reaches 0, having absorbed R. If R exceeds C the gap is held at d." With d = 0.04 m the stop angle is 0.133 rad at the 0.30 m crossing, consistent with the 0.05 rad assertions.

**F5. EL-053:58: `LocalRotationX` does not exist.** `PresentationProperty` has only `LocalRotationZ` among rotations (`CuriousContraptions.Abstractions/ElementPresentation.cs:12`-`:16`). The spec counts 10/0/0/0 and data-only.
- Replacement: "`Visual/ScrewAxis` is a fixed node turned so its local Z lies along the rail; its child `Visual/ScrewAxis/Screw` takes `LocalRotationZ` from `CommittedRevoluteAngle`." Add disqualifier (4): "a `LocalRotationZ` binding on a node under a rotated parent does not turn about the parent's Z; then the screw is drawn without rotation and the pulley witness mark carries the motion."

**F6. EL-060: the Weight's collided eye is ignored.** EL-060:84-87, rows M1, M5, M6.
- The Weight touches the pole with its eye top, 0.7346 s above its base (`elements/Weight/weight.tscn:91`; `WeightElementDeclaration.cs:73`-`:75`; s = 0.136734 m + 0.45307), not with the cylinder top.
- At the default 100 N and 1.2 m: 4 kg held with factor 1.58 (spec says 1.96); 5 kg held; 6 kg not held (spec and M6 say held); break-even about 5.5 kg, not 6.9 kg. Lift from rest needs an eye-top gap under 0.274 m; M1's 0.30 m does not lift.
- Replacement: define "gap" as pole face to eye top. M1: gap 0.15 m. M5: 0.22 m lifts, 0.33 m does not. M6: 4 kg held, 8 kg not; optional pair 5 kg held, 6 kg not. State that distance is measured to the centre of mass.

**F7. EL-067: the package has no bundle shape.** EL-067:21, :31, :88, :163.
- Every package today is a placeable part with scene, palette order, icon and at least one body; the validator raises `BodyWithoutCollider`; `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` places one instance per admitted declaration. `ENGINE-ASSEMBLY-JOINTS` lists only "an elastic tensile span"; G3 adds a material but not the non-placeable declaration kind.
- Add to G3: "declared by a non-placeable bundle entry kind (no body, scene, palette entry or ports), admitted by `BundleValidator`, excluded from the palette and from every per-package placement suite, selectable in the connection UI with a declared default, and present in the generated bundle so the named-package binding admits it."
- Decision needed: whether this is part of `ENGINE-ASSEMBLY-JOINTS` or its own story (it touches bundle schema, wire, save and UI and is unrelated to player-built joints). Recommended: its own engine slice.

**F8. Owner items O1 and O2 are understated.** EL-067:150; EL-066:156; EL-064:94; summary section 4 and "None waits on the owner".
- O1: a non-solid tether needs a cited owner decision per use; shipping `steel_cable` without one is a review Fail. Replace "Blocks: nothing mechanical" with "Blocks: acceptance of this slice and of EL-205; planning may proceed."
- O2 reverses the owner's 22 October 2026 decision "10.2 keeps runtime rope cutting" and removes the cutting acceptance of Story 10.2b and CAT-058 (`epics.md:3923`, `:3937`, `:3945`). It is a scope cut and must say so. Add: "If taken, EL-064 row C4 and `RopeTests.cs:151`, `:882` are re-based on a cutter-triggered cut in the same slice." EL-064 section 8 needs the matching disqualifier.

### Medium

**F9. EL-065:71, :136, :156: eye clearance wrong.** The eye centre is at 0.5771 s and the cylinder top at 0.5585 s (`weight.tscn:67`, `:71`), so the opening above the Weight is about 0.141 s: 0.069 m at 0.25 kg, 0.083 m at 1 kg, 0.141 m at 4 kg. A 0.04 m bar leaves 0.029 m at 0.25 kg; the fallback "thicken to 0.06 m" leaves 0.009 m. Replace the fit sentence with these numbers and the disqualifier with "thicken to at most 0.05 m, or raise H1's lower boundary to the 1 kg Weight and record that the 0.25 kg Weight cannot be hooked." Also state the principal-frame rotation about Z for the asymmetric mass frame (:43).

**F10. EL-062: buildability and a row that passes on a defect.** :40, :51, :60-68, :98, :147, :154.
- No positions for the five ports, no slider base frame or moving frame. Needed: port positions on the dock A post (pattern `LinearPusherElementDeclaration.cs:43`-`:47`), slider base frame (0, 0, 0) with the pusher's axial frame.
- F2: a ball released on the centreline lands on the 0.3 m rail, 0.16 m below deck height, and stays; the 0.1 m threshold passes while the cargo still waits at the dock. Either narrow or lower the rail so nothing rests on it, or state F2 as "rests at least 0.15 m below deck height and is not aboard when the deck returns".
- Speed floor: cargo pushed by the lip has no spin and rolls on at about 5/7 of deck speed; expect a floor nearer 1.5 m/s than 1 m/s (hand estimate; the spec lists this as a disqualifier).

**F11. EL-057:128 (E6) and :76 cannot pass.** Eight steps move the belt 4.19 m; the default Conveyor is 3.0 m (`ConveyorElementDeclaration.cs:35`), and a rolling ball does not track a start-stop belt. Replacement: "belt travel (roller angle x `surface_per_radian`) is 8 x 0.524 m within 2 %; a Tennis ball on a 6 m Conveyor ends at least 1 m further along and never moves back more than 0.1 m." Also :135: replace "after 4 s is eight steps" with "completed steps equal clock pulse occurrences, checked 0.4 s after the eighth".

**F12. EL-056:100, :122, :132: the pre-winch load does not load the shaft.** A ball on a held inclined belt rolls back; no Conveyor test reacts cargo weight into the roller on an incline. Use D2's synthetic reverse-torque shaft until EL-063 exists; remove "a Conveyor carrying cargo uphill" as a delivered reverse load or mark it present, untested.

**F13. EL-057:74, :94, :156.** The 0.1 N·m Empty threshold is a product constant with no declared field or boundary row: add "minimum release load 0.1 N·m (declared on the stop)" and E3 boundary "0.05 N·m answers Empty; 0.2 N·m steps". ":94 and :156 say EL-061 shares the indexed stop": it does not; replace with "EL-061 does not use this stop".

**F14. EL-055:54-55, :72; EL-056:55-56, :73; EL-057:49-50, :67: rim radius does not match the drawn pulley.** `MechanicalRim` radius 0.28 is declared at ports where the art has r 0.12 pulleys; two 0.28 rims 0.9 m apart also overlap the central drum. Delivered parts draw the pulley at the rim radius (`ConveyorElementDeclaration.cs:47`, rim .22). The rim is presentation only (`CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10`), so no ratio changes. Replacement: rim radius 0.12 at each port. In EL-055 the pulleys at x = ±0.45 and the released shoes at x = ±0.36 also sit outside the 0.35 m housing half-width; state what carries them or move them to ±0.30.

**F15. EL-063:55, :78, :104, :132.** W5's "drum spun to 128 rad/s by an unloaded Motor" is impossible: Motor speed is capped at 20 rad/s (`MotorElementDeclaration.cs:41`). Replacement: "Motor at 20 rad/s, and an unsupplied drum back-driven by an 8 kg Weight over the full 6 m". State the reading of "stall and reverse load debit work": "the work ledger stays honest under stall and back-drive; no energy is created", and add to W3 and W6 "kinetic energy never exceeds the Weight's lost potential energy plus 5 %". The `Tie` has no attachment role (`engine/gpu/RopeTopology.cs:75`-`:84` refuses `None`) and "capacity 6 m" is ambiguous for a route over a pulley (gap text in section 4). :161: no campaign reservation names the winch.

**F16. EL-053:136 (A6), EL-054:128 (B3): "0.005 J allowance" is misquoted.** `SpringTestEnergy.Allowance` is 0.005 J plus 0.0981 J per kilogram of dynamic mass (`CuriousContraptions.tests/SpringTestEnergy.cs:44`-`:47`). A literal 0.005 J bound on a scene with a 4 kg ball would fail on float churn. Replacement: "the allowance of `SpringTestEnergy.Allowance(scene)` (0.005 J + 0.0981 J per kg of dynamic mass)".

**F17. EL-058:71, :116, :118-119.** The Basketball band is 0.70 m across and the repaired Tennis seam is radius x 33/32, so G4's "0.72 passes" has 0.01 m per side and G3's 0.29 m ball 0.011 m. State pass cases against the widest collider: G4 Basketball passes at 0.74; G3 small Tennis radius 0.28. G1's control (a ball at rest over a flat tine top) is ill-posed: use "released over a tine centre line it rests on the tine for 5 s".

**F18. EL-069:71, :75, :123, :124, :151.** K2's boundary uses K1's 3 kg counterweight; bucket 1.5 kg plus Basketball 1 kg rises and the tip rope never goes taut, and with a Bowling ball the estimated tilt (45 to 50 degrees) is short of the 74-degree spill. Replacement Given: "a bucket hung over a Pulley against a 0.25 kg Weight, holding a Bowling ball, with a rope from an anchor to `TipTie` that goes taut after 1 m of descent". Add to :75: "tipping needs the main rope near slack". The pipe and funnel claim ignores the bail across the opening: replace with "a Clear pipe or Funnel must discharge 0.41 m to the side of the bail". K3: use 1.4 m, not 1.15 m (1.5 cm clearance on a swinging bucket).

**F19. EL-071:103, :104, :106; EL-072:107, :109.** A2 and B4: "lowest point stays above the mouth plane less 0.01 m" is geometrically false; a seated oversize sphere's lowest point is 0.38 m (r 0.75) to 0.55 m (r 0.66) below the plane. Replacement: "the jammed ball's centre stays at least sqrt(r² - 0.65²) - 0.01 m outside the mouth plane, and its speed over the last 0.5 s is under 0.05 m/s". A1: use the ball's sphere radius (0.34, 0.28, 0.25), not the 0.35 band, in the containment bound. A4: replace the every-tick speed assertion with "leaves the far mouth within the guard and never rests in the joint". B2: add "route lying in a horizontal plane".

**F20. Over-claimed "present" rows.** EL-061 cargo contact (F3). EL-056:60, :96: no test shows a node shown or hidden per Choice option through `PresentationProperty.Visible`; mark present, untested (counts 3/1/1/2) or use one mirrored pawl node. EL-055:181 already lists `CommittedActuatorDrive` to `LocalX` as a disqualifier; the validator has no rule against it (`BundleValidator.cs:323`-`:326`), which should be stated.

**F21. EL-070:129, :141.** C4: a 4 kg ball at 8 to 20 m/s shoves a free 2.5 kg cage. Replacement: "launched at a barred side of a cage standing on the bench: the ball's centre, in the cage's frame, is never inside the interior box". The Build-only door narrows "closable opening" to Build time; list it for the owner with default accept.

**F22. EL-059:49, :67-69, :86, :90.** Sockets at (±0.86, -0.08, 0) float outside the rim; use pedestal-face positions (±0.575, -0.12, 0) and (0, -0.12, 0.575). :90 says the plate has uncollided studs and ring; they are collided in this checkout (`pressure_plate.tscn:119`-`:141`). Delete the clause.

**F23. EL-073:105, :114, :122; EL-074:109, :115; EL-071:74, :113; EL-072:116: retained acceptance weakened or dropped.**
- CAT-066 keeps "maximum-speed ball does not tunnel" and the Wall theory runs 64 m/s (`WorkshopWallTests.cs:86`); R1 and W4 use 20 m/s. Replacement boundary: "`thickness` 0.12 m at 64 m/s; the barrier yawed 45 degrees".
- Retained occlusion controls (CAT-048 shell and collar light and air; CAT-066 light, air, sound under IX-12 and IX-13) have no row. Add later-partner rows: "a beam the Clear pipe's shell transmits is blocked by the metal shell (after `ENGINE-OPTICAL`)"; "an upright barrier blocks a Fan's airflow now, and a beam or tone after `ENGINE-OPTICAL` / Story 14.0".
- Strength (EL-073) and combustible (EL-074) are deferred in a status cell only. Add to section 6 Resolved: "follows its engine story as a later-partner row; the identity stays open until R8 (W8) runs. Decided under `delivery-batches.md:28`", and list it for the owner.

### Low

- **F24. Stale edit lists, all section 8s.** "Until S2: tests csproj compile list" no longer applies: main compiles `*.cs` by one rule (`origin/main:CuriousContraptions.tests/CuriousContraptions.tests.csproj:77`). `PackageRuntimeParityTests.cs:596` (summary :155 and some specs) is the `"balloon"` parameter row; the switches are at `:483` and `:641`, the generic list at `:653`. `spec-family-ball-presets.md:104` should be `:124`.
- **F25. EL-060:62, :16, :133, :163.** Coil 14.4 ohm with 50 W breaks the delivered rule R = V²/MaxPower (use 2.88 ohm). The `Supply` pass-through has no descriptor and no row uses it: drop under D19. M4's "within 2 ticks" is the engine story's contract: use "falling faster than 0.05 m/s within 0.1 s of the cut". Add `sequence-task-393` and the 91-100 material-sorter lesson to the references.
- **F26. EL-061:68, :41, :47.** The bus node is unneeded (bind `PowerIn` to the contact as the plate does); wheel width 0.14 differs from the batch shaft's 0.13; give the pose of C3's Wall.
- **F27. EL-053:33-38.** The shaft pulley (z 0.255 to 0.385, y ±0.28) cuts through the navy foot (z to 0.30, y -0.25 to -0.15). Narrow the foot to 0.5 m deep or move the hinge to z 0.37.
- **F28. EL-064:25.** "Decided by domain and direction only" omits the role check (`engine/gpu/WorkshopConnections.cs:161`-`:168`). Conclusion unaffected.
- **F29. EL-066:133 (X5), EL-067:124 (K3).** X5's ±20 % band on a 1 J difference may be decided by hinge damping; prefer the engine's committed cut-work read. K3 is declaration-only: add to K1 "settled stretch equals g/(2 pi f)² within 5 mm".
- **F30. EL-071, EL-072 art.** The shared contract forbids "harsh metal" (`requirements.md:4150`): add "matte, non-metallic material" to the metal shells.
- **F31. EL-069:160.** Also name `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996` in the comment correction.

## 3. Cross-batch conflicts

- **Palette orders.** Batch I uses 100 + EL number (153 to 174, and 272). Orders 160-165, 170, 171, 173 and 174 are also used by Batch H (EL-037 to EL-049) and Batch J (EL-107, 108, 162, 163). 272 is EL-172's slot under Batch I's own rule. `requirements.md:4154` says "alphabetize within the existing fundamental-type groups", which no batch follows. Proposed: every spec states "palette order is allocated by the delivering slice; the bundle refuses a duplicate (`BundleValidator.cs:25`)", or the coordinator adopts 100 + identity number for every batch with a separate band for second packages. The summary (:42) also omits delivered orders 40, 66, 67 and main's 68.
- **EL-205 Rope.** Batch J closes EL-205 as a duplicate of the delivered connection with no package (`readiness/batch-j.md:35`, `:68`). Batch I makes it a Family slice member with EL-067 and requires the delivered rope to become the first declared material with a 1 J cutting resistance (EL-066, EL-068 prerequisites; G3). One of the two must change; the material declaration and its resistance value need one owner (see F7).
- **Coverage binding.** `readiness/new-identities.md` treats the published binding as blocker B1; Batch I assumes the superseded ledger line (F1).
- **Tensile reaction on a dynamic guide.** Batch J proposes `engine-tensile-dynamic-guide` and names EL-069 and EL-070 as possible consumers (`batch-j.md:88`); neither Batch I spec mentions it. Add to their disqualifiers.
- **EL-227 Magnetic impulse relay** depends on EL-060 and a ferrous Cannonball; EL-060 declares only the Weight as responding. Consistent, but the responding property should be declared by one rule in `ENGINE-FIELD-FORCE`, not per consumer spec.
- No duplicate package id was found between Batch I and the other batches or delivered parts.

## 4. Engine-story gaps, with proposed text

The summary's G1 to G6 are directionally right. Additions:

- **G1 (indexed stop).** Append: "a declared minimum carried torque below which a request is answered Empty, a request during a step answered AlreadyReleased, and Reset clearing a pending request."
- **G2 (carousel).** Add "and slower than a declared speed" to the rest-window contact. If F3's default is taken, drop the periodic detent; the story's existing "position drive" covers the carousel.
- **G3 (winch).** Add: "the wound end is declared (a new attachment role or a winding descriptor naming the revolute coordinate and radius); capacity bounds the route's total maximum length; at Run start the drum holds capacity minus the connect-time length."
- **G3 (material).** Add the non-placeable bundle entry text of F7, or move connection materials to their own engine slice.
- **G4.** Confirmed: a supplied zero-speed drive holds a shaft (`RevoluteDriveTests.cs:371`). A parameter-bound hinge stiffness is refused and a continuous hinge must have stiffness 0 (`BundleValidator.cs:815`-`:820`); keep that qualifier in the proposed wording.
- **G5 (field force).** Add: "force is referenced to the responding body's centre of mass; the falloff law is a closed enum (at least Linear)."
- **G6 (cutter).** Add the strand diameter, engagement gap and resisting force law of F4.
- **G7, G8.** Rewrite against the published `## Package source binding` reader (F1, F2).
- **Structure.** `ENGINE-ASSEMBLY-JOINTS` has one sprint key but this batch asks it for one-way hold, indexed stop, detent or position drive, rest-window contact, winch, connection material and (via EL-066) hinge spring. Proposed text: "The planning scout may deliver the revolute hold capabilities (one-way hold, indexed stop, rest-window contact) and the winch as separate engine slices ahead of player-made sockets."
- **Shared acceptance, "the story adds no element package"** (`epics.md:4414`): the summary's note stands; it needs a decision once EL-205's disposition is settled.

## 5. Items for the owner

| # | Item | Recommended default | Blocks |
|---|---|---|---|
| 1 | O2: remove the player's Run-time "Cut rope" control. Reverses the 22 October decision that 10.2 keeps runtime rope cutting; a scope cut | Keep the player cut until the scissors slice; decide then with the cited reversal | The meaning of cutter lessons; EL-064 row C4 |
| 2 | O1: every connection strand (wire, belt, rope, cable) drawn non-solid | Accept as non-solid drawings | Acceptance of EL-067 and EL-205 |
| 3 | EL-061: passive turntable or self-driven indexer (F3) | Self-driven supplied position drive | EL-061 planning |
| 4 | EL-072: two packages under one identity (F2) | The coverage follow-on admits several packages per identity; fallback a new ledger identity for the 45-degree bend | EL-072 |
| 5 | Coverage preconditions for 22 identities: write scope-corrections blocks and co-anchor todos, or widen the reader (F1; same as B1 in new-identities) | Widen the reader in the coverage follow-on | Every Batch I package slice |
| 6 | EL-073, EL-074: ship contact now, strength and combustible after their engine stories, identities left open | Yes | Nothing now |
| 7 | EL-070: the door cannot change during a Run | Accept; a Run-time door follows `ENGINE-ASSEMBLY-JOINTS` | Nothing now |
| 8 | O3: EL-069 and CAT-004 goal capture | No sensor (as the summary) | Nothing |
| 9 | Part-body cream `#e8dccb` (DESIGN.md:80) or the delivered `#fff8e9` | The delivered `#fff8e9`, then correct DESIGN.md | Nothing |
| 10 | Connection materials: inside `ENGINE-ASSEMBLY-JOINTS` or a separate engine slice (F7); and EL-205's disposition against Batch J | Separate slice; EL-205 closes as Batch J proposes and the slice converts the delivered rope | EL-066, EL-067, EL-068 |

Decisions under the delegation that were checked and found defensible: one shaft for EL-053 to EL-057; `rack_pitch` in rad/m (the validator requires it, `MechanicalTransmissionValidation.cs:56`-`:58`); brake applied while supplied; no carrier clamp mode (the requirement's carrier outcome keeps row C7); comb grate; threshold-only tray (the row says "measured force or threshold state"); supplied ferry; winch without parameters or ratchet; single-jaw snips; EL-064 as a duplicate; "less stretch than rope" was never a requirement criterion.

## 6. Summary table against the specs

- `batch-i.md:7` and every "Prerequisites" cell: "S7 (the `**Package:**` ledger line)" is the superseded mechanism (F1). Thirteen "Yes" cells should read "blocked on the coverage follow-on".
- EL-061 counts 10/0/1/1 should be 9/1/1/1; EL-056 4/0/1/2 should be 3/1/1/2 (F20). EL-053's 10/0/0/0 stands only after F5's fix.
- EL-072 "Blocked on the S7 amendment": an owner item, not an amendment (F2).
- "None waits on the owner" (:36) is wrong: items 1 to 5 and 10 above.
- :42 delivered orders list is incomplete; :155 cites `PackageRuntimeParityTests.cs:596` wrongly (F24).
- Counts for EL-053, 054, 055, 058, 059, 062, 064, 065, 069, 071, 073 agree with their specs.

## 7. Not verified

- `Mode records` syntax for parameterised packages (`CurrentCatalogue.ParseModes` was not read in full).
- F6, F18 and the ferry speed floor are hand arithmetic, not simulation. The K2 Basketball contradiction is exact.
- Present rows of EL-057, 060, 061, 063, 066, 067, 068, 070, 072 and 074 were spot-checked, not all opened.
- Whether a `LocalRotationZ` binding under a rotated parent turns about the parent's axis (F5).
- Whether the Conveyor reacts cargo weight into its roller on an incline; whether a Level output into a Pulse input is refused (EL-057 E10); whether the Battery's 100 W node limits a 210 W consumer (EL-068:45).
- The specs' own listed assumptions: hinge spring rest angle, a `Tie` on a continuous-hinge body, a fixed negative drive target without a run-control parameter, slider travel above 3 m, the slider stop at 4,000 N.
- Hand-estimated mass moments of the bucket, cage and hook were not recomputed.
- `general-engine-element-map.md` rows were not read.
