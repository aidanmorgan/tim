# Audit: element specs, Batch J (Mechanical 2, 28 specs)

Independent audit, 22 November 2026, of checkout `spec-drafts` against the audit brief. Read-only: nothing was built, run or edited. Published main read with `git show origin/main:` (`25df3cd5`).

Spec paths are under `docs/planning/elements/`; test files under `CuriousContraptions.tests/`. Geometry, statics and timing figures in the findings are hand calculations from the specs' own numbers; none was simulated.

**Method.** All 28 specs were read in four shares. Code and tests were opened behind every "present" claim for the delivered parts and closures (EL-193, 194, 195, 202, 203, 205, 206, 209) and for EL-108, 110, 111, 156, 163, 164, 184, 200, 215, 216, and behind every claim another spec depends on. Elsewhere tests were confirmed by name and line only.

## 1. Verdict per spec

Counts: **Ready 1, Ready with fixes 21, Not ready 6.**

| Spec | Verdict | Findings |
|---|---|---|
| EL-107 Spiral delay tube | Ready with fixes | S1, M1, M2, L1 |
| EL-108 Powered airlift | Ready with fixes (H3 before a slice is planned) | S1, H3, L1 |
| EL-109 Speed trapdoor | **Not ready** | S1, H4, G1, M3, M4 |
| EL-110 Flywheel | Ready with fixes | S1, M5, M6, L2 |
| EL-111 Centrifugal governor | Ready with fixes | S1, H7, M7, M8, M9 |
| EL-156 Single-lobe cam | Ready with fixes | S1, H6, L3 |
| EL-157 Double-lobe cam | Ready with fixes | S1, H6 |
| EL-158 Rise-hold-fall cam | **Not ready** as written | S1, H5, H6 |
| EL-162 Fixed ball diverter | Ready with fixes | S1, M10, O4 |
| EL-163 Powered ball diverter | Ready with fixes | S1, M11 |
| EL-164 Alternating ball diverter | Ready with fixes | S1, M12, M11 |
| EL-184 Mechanical ball gate | Ready with fixes | S1, M13, M14, L4 |
| EL-185 Releasable assembly joint | Ready with fixes (blocked on ENGINE-ASSEMBLY-JOINTS) | S1, M15, M16 |
| EL-186 Temporary bridge | **Not ready** | S1, H1, H2 |
| EL-193 Domino | Ready with fixes | M17, M18, L5 |
| EL-194 Springboard | Ready with fixes (not a Duplicate closure) | H9, L6 |
| EL-195 Pinball bumper | Ready with fixes | H10 |
| EL-199 Electric motor | Ready with fixes (blocked; no story) | G2, O3 |
| EL-200 Drive belt | **Not ready** | H8, G3, C3, O1 |
| EL-201 Drive chain | **Not ready** | H8, G3, M19 |
| EL-202 Conveyor | Ready with fixes | M20 |
| EL-203 Reverse transmission | Ready with fixes | M21 |
| EL-205 Rope | Ready with fixes | C2, L7, O2 |
| EL-206 Fixed pulley | Ready | no delta found against `elements/Pulley`; depends on L7 |
| EL-207 Moving pulley | Ready with fixes (blocked; owner undecided) | S1, G4, L8 |
| EL-209 Fan | Ready with fixes | M22 |
| EL-215 Damped cushion | Ready with fixes | S1, M23, M24, L9 |
| EL-216 Capture cradle | **Not ready** | S1, H11, M25, M26 |

## 2. Findings, by severity

### Systematic

**S1. Wrong coverage binding in all 17 planned-package specs, and in summary gap 9.**
- Where: EL-107:123,129; EL-108:167,173; EL-109:162,168; EL-110:218; EL-111:213; EL-156:204; EL-157:162; EL-158:176; EL-162:125,131; EL-163:145,151; EL-164:132,138; EL-184:140,146; EL-185:23,178,185; EL-186:23,191,198; EL-207:23,182,188; EL-215:23,211,218; EL-216:23,191,198; `readiness/batch-j.md:93`.
- Defect: each binds the package by a `**Package:**` line in the ledger entry. No spec has a `## Package source binding` section.
- Evidence: the owner decision in the author brief says "never by a `**Package:**` ledger line". Published main reads a final section of the element spec: `origin/main:tools/Coverage/NamedPackageSources.cs` (`Marker = "## Package source binding"` and the regex below it); example `origin/main:docs/planning/elements/EL-239-baseball.md:14-20`.
- Replacement (Integration bullet): "Coverage binding: a final `## Package source binding` section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; S7 widens its preconditions."
- Replacement (last section of each spec, EL-110 as the example):
  ```
  ## Package source binding

  **Package:** flywheel.
  **Named identity:** [EL-110](../invest/named-elements.md#element-110).
  **Acceptance:** [todo-377](../requirements.md#todo-377), [sequence-task-663](../requirements.md#sequence-task-663).
  **Decision:** [S337](../invest/scope-corrections.md#s337).
  **Mode records:** Configuration=Fixed.
  ```
- Unmet precondition each spec must state: `scope-corrections.md` has an `s636` anchor but none for these identities (checked for s307, s337, s338, s344–s346, s381, s382 on main). EL-215 and EL-216 also have no `todo-` anchor (`requirements.md:6123`, `:6131`), which the regex requires.
- Withdraw the last sentence of summary gap 9 ("add `**Package:**` lines"). The draft `spec-s7-coverage-named-identity-admission.md:23` in this checkout still proposes the ledger line and conflicts with the same decision.
- Same Files bullets: delete "the test project's compile list until S2". Main compiles `*.cs` by one rule (`origin/main:CuriousContraptions.tests/CuriousContraptions.tests.csproj:77`).

### High

**H1. EL-186:41,167,178,180 — the 40 N default breaks under the cargo that A1 says crosses.**
- The spec's statics (`:167`) put a Basketball at one end at 19.6 N on that pin. By the same rule a 4 kg Bowling ball entering loads the entry pin to 9.8 + 39.2 = 49 N, above 40 N for the first 23% of the span.
- A1 ("a Bowling ball crosses, the deck has not moved") contradicts A3's premise that 49 N breaks.
- Replacement: A1 cargo is the Basketball (boundary: Tennis ball). A3 becomes "a Bowling ball rolling onto the deck breaks the entry pin; a Basketball does not; `end_capacity` 200 carries the Bowling ball". Raise the minimum from 20 to 25 N (20 N leaves 2% over the Basketball's end load). Replace the `:167` sentence "One Bowling ball … under the 40 N default" with the end-load figure.

**H2. EL-186:32,35 — the pins sit on the deck's upper end edges with 0.01 m end clearance, so a released deck binds.**
- Swinging about an upper corner moves the lower corner 0.12·sin θ toward the post: 0.037 m at the stated 18° (`:169`). The deck jams near 5°, or draws through the post if deck-to-post collision is off.
- Replacement: "`Left` at (−1.5, −0.06, 0) and `Right` at (+1.5, −0.06, 0)" (lower end edges), or end clearance of at least 0.04 m. State `BodiesCollide: true`.

**H3. EL-108:35,59 — the seat does not centre a ball and the cone misses its samples.**
- The Frustum's inner radius is 0.25 at y 1.0. A Basketball (r 0.34) would touch that cone on a circle of radius 0.18, below the opening, so it rests on the flat base top with about ±0.15 m of play.
- Its centre is 1.29 m above the apex, not 1.45 m; the cone radius there is 0.184 m, and airflow samples sit 0.17 m from the centre (`elements/Ball/BallElementDeclaration.cs:30-33`; `WorkshopPhysicsEngine.Airflow.cs:115-121`). With two of four samples outside, 15 N delivers 7.5 N against 9.81 N of weight, so A1 and A2 depend on where the ball settles. The Tennis ball is worse.
- Decision needed (numbers, under the delegation): narrow the seat throat below the smallest ball's contact circle and re-derive base height and reach. One unsimulated candidate: seat inner radius 0.10 to 0.65 over y 1.6–1.95 on a 1.6 m base, reach 5.15 m.
- Also missing at `:36,38`: chamber and hood box widths and positions, lintel and sill sizes.

**H4. EL-109:36-43,74,146-147 — the 1.2 m opening is too short for the speeds that must fall.**
- A ball falls its 0.34 m radius in 0.263 s. To be below the downstream lip on arrival its speed must be at most (1.2 − 0.34)/0.263 ≈ 3.3 m/s. A1's 4 m/s ball and a ball at the 5 m/s ceiling meet the lip corner and may hop onto the upper route.
- Decision needed: lengthen opening and hatch to 2.0 m (covers about 6.3 m/s) and state a maximum supported speed, or lower the `speed_threshold` maximum and A1's speed to what 1.2 m supports.

**H5. EL-158:29,33,42,117,178 — the cylinder-built dwell arc leaves 10.9 mm of proud art, not 2.1 mm.**
- 2.1 mm is the follower drop between neighbouring cylinders. The gap between the drawn 0.30 m arc and the collider outline is at the cusp, at radius 0.24·cos 7.5° + √(0.06² − (0.24·sin 7.5°)²) = 0.2891 m.
- The drafted fit ceiling is 0.005 m (`_bmad-output/implementation-artifacts/spec-engine-art-solidity.md:110`, `:114`). The spec's own condition (b) fails by arithmetic, so the summary's "yes, conditional" is wrong as written.
- Replacement for `:33`: "a base cylinder, radius 0.18, centred on the axle; and thirteen cylinders, radius 0.06, centred on a circle of radius 0.24 about the axle every 7.5° from −45° to +45° from straight down." For `:29`, `:42`: "the collider outline lies within 2.6 mm of the drawn arc (cusp radius 0.2974 m) and a flat follower rides within 0.5 mm of constant height." Probe row `:116`: "fourteen colliders on a turning body". Dwell angles and rows H1–H5 are unchanged.
- Alternative with no extra colliders: draw the band edge as the exact scalloped union.

**H6. EL-156:41-45 (shared by EL-157:34, EL-158:36) — the cam sinks 5 mm into the base at rest.**
- Hinge at y 0.45, disc centre 0.075 below, radius 0.3: lowest point y 0.075 against a base top at y 0.08, with `BodiesCollide` false.
- Replacement: hinge (0, 0.50, 0); `DriveIn` (0, 0.50, 0.12); follower rest centre y 1.025; guide plate centre (0, 0.73, −0.14); follower top 1.325 m at rest and 1.475 m at full lift (`:70`). EL-157 and EL-158 follower rest centres become y 0.98.

**H7. EL-111:43-44 — each flyball overlaps the spindle shaft by 0.04 m at rest.**
- Ball radius 0.08 centred 0.10 m from the axis; shaft radius 0.06; the pair is jointed and does not collide.
- Replacement: shaft radius 0.02 m below the crossbar. Onset speed and lift table are unchanged. (Widening the pivot to 0.15 m leaves the 0.5 m base.)

**H8. EL-200:44,58-65,190 and EL-201:40-48,189 — the Fan's mechanical port is omitted, and the default belt would slip there.**
- `elements/Fan/FanElementDeclaration.cs:64-67` declares `DriveIn` with rim radius 0.14. Default capacity there is 100.2 N × 0.14 m = 14.0 N·m, below the Motor's default 20 N·m; `FanIntegrationTests.cs:541` drives it at 20. The claim at EL-200:65 that existing constructions behave as now is unproven.
- No pitch radius is given for any port except 0.22 and 0.30. Add to EL-200 §2: "Pitch radius = the declared rim radius: Motor 0.30, Conveyor 0.22, Clutch 0.28, Reverse transmission 0.32, Windmill 0.24, Wound spring 0.26, Fan 0.14. Capacity torque = F_max × the smaller of the two pitch radii. The speed ratio stays the ports' `CoordinatePerRadian` ratio and is not derived from pitch radii."
- Default `tension`: 130 N carries 20 N·m at the Fan; 90 N does not. If it changes, rows B3–B5 need recomputing. Goes with owner item O1.
- EL-201: add a Fan row to the engagement table (recommended: Smooth only) and `elements/Fan` to Files `:189`.
- EL-200:201: add `ConveyorTests.cs:453` (Motor torque 30 N·m against 22.1 N·m capacity at 0.22 m) to the disqualifier's list.

**H9. EL-194:19,113,127 and `readiness/batch-j.md:28,42` — not a Duplicate closure.**
- `requirements.md:5955` makes "typed signal integration" and "all fifteen composite lane instances" EL-194 qualification; both are unexecuted. A closed duplicate "counts as implemented" under AGENTS.md.
- Level 7 is cited to "Early level proof batch 1"; `spec-early-level-proof-batch-1.md:66` holds only levels 03, 05, 12 and 61.
- Replacement disposition: "Refinement of CAT-062 with no part delta; part-level check recorded in the 'Launch parts' review; the identity stays open until level 7 `spring_signal` (a later Early level proof Signals batch, no contract yet) and the fifteen lanes (after ENGINE-GOALS) pass." Summary: 5 duplicate closures, not 6; row 28 "part check yes; identity blocked on the Signals batch and ENGINE-GOALS".

**H10. EL-195:35,52,103,117 — the 50 W recharge bound does not exist.**
- `ContactWorkInstanceTests.SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts` (`:275-296`) asserts a 120 W Battery delivers 1 J per 1/120 s. The Battery's `power_limit` is 10–480 W, default 120 (`elements/Battery/BatteryElementDeclaration.cs:44`).
- Replacement `:35`: "electrical node 'coil': 12 V, 10 Ω, `MaxPower` 50 W (metadata; it does not bound charging). The refill rate is the source's `power_limit`: 120 W refills the default 32 J store in 0.27 s, and 480 W in 0.07 s, inside the 0.15 s cooldown."
- `:103`: delete the "rapid repeat hits weaken … supply lesson" sentence, or restrict it to a `power_limit` below about 213 W. `:117`: "supply only refills the store, at the source's power limit". `:52`: "within the source's power bound".

**H11. EL-216:51,120,169,181,207 (and :189) — the Powered gate cannot act as the cradle's door.**
- The spec treats the gate as a free blade covering a 0.60 × 0.30 m doorway. The delivered gate is an in-pipe conduit: sleeve radius 0.7, collars 0.78, bore 0.65 (`elements/PoweredGate/powered_gate.tscn:27-39`, `:128-150`). The cup cannot sit in the bore, so the blade is at least 0.49 m from the outlet face and the bore bottom (about 0.13 m up) is above the cradle floor at the outlet (about 0.08 m).
- The "present" row at `:120` cites a compile test, not a cup release. EL-184 is the same conduit, so "EL-184 as a second release" has the same defect.
- Decision needed (planning): (a) specify the conduit arrangement — floor slab at least 0.17 m thick, gate origin 0.88 m from the cradle origin on +X, A5 reworded to "the ball rests against the closed blade in the gate's mouth"; or (b) defer A5 to a free-standing blade part. Summary row: "blocked on gate arrangement".

### Medium

- **M1. EL-107:30.** A tilt "about the diameter that joins its ends" leaves both ends level; the stated 0.8 m drop needs the other axis. Replacement: "Each arc lies in a plane tilted ±14.48° about the in-plane axis through the arc's centre and midpoint (perpendicular to the diameter joining its ends), alternately one way and the other; arc n's centre is 0.8 m below arc n−1's."
- **M2. EL-107:76.** `HollowBoundsTests.cs:11` is a bounds test; the only traversal test (`HollowElementTests.cs:356`) keeps gravity in the torus plane. Mark "tilted hollow colliders, ball riding the bore floor with gravity near the torus axis" *present, untested*; counts 4 / 2 / 0 / 0. A2's control (two equal runs within 0.05 s) cannot fail in a deterministic simulation; use "the same ball beside the tube".
- **M3. EL-109:111.** `ComposableSupplyRoutingTests.cs:91` routes across three packages; `:499` proves internal edges to a store, not a drive. Mark "a memory-closed contact enabling a drive inside one package" *present, untested*; counts 3 / 3 / 1 / 0.
- **M4. EL-109 §2.** No material for track or hatch; add "restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`PoweredGateElementDeclaration.cs:47-48`)". The electrical node (`:66`) lacks resistance and capacitance; use the gate's `("motor", Consumer, 12, 24, 0, 20)`. Hatch-rail positions (`:39`) are not given.
- **M5. EL-110:202 (A2).** The unloaded control ("alone it keeps at least 5.6 rad/s") cannot be built: the wheel has no drive and a belt cannot be removed in Run; buildable controls differ by less than the ±0.15 tolerance. Replacement: the load is a Wound spring on `Drive`; the wheel's ½Iω² falls by at least the spring's ½kx² while it winds.
- **M6. EL-110:205 (A5).** "Roller blocked by a Wall-held box" is unspecified; the Conveyor moves cargo through a driven surface (`elements/Conveyor/ConveyorElementDeclaration.cs:109-113`). Use the Wound spring at its stroke stop, or delete A5 (it is from the old outline, not the requirement).
- **M7. EL-111:142.** Marked untested, but `RetentionWorkTests.DeclaredSharedSupportTransfersSignedWorkAndBackdrivesWithoutCreatingEnergy` (`:16`, coupling at `:38-39`) runs a declared bilateral revolute-to-prismatic coupling. Mark present; counts 9 / 2 / 0 / 0 here and in the summary.
- **M8. EL-111:197 (G3).** Lift at 4.5 rad/s is about 0.023 m against a 0.02 m threshold and a ±0.03 m band. Replacement: "at 5 rad/s it exceeds 0.02 m (computed 0.04 m)".
- **M9. EL-111:60.** Nothing drawn joins the flyballs to the bracket. Make the two rods required, drawn by span bindings, with "the slice's probe confirms a span between two body-bound nodes; if it cannot, raise to the owner before building" in place of "may add".
- **M10. EL-162:54.** At `blade` 0 each arm is 0.39 m from its wall, so both lanes are blocked for about |blade| < 0.45; the wedge does not "split the stream". Replacement: "an intermediate value blocks both lanes (a visible jam)" plus a row `AHalfSetBladeBlocksBothLanes`. A1's control should be the opposite blade end. See O4.
- **M11. EL-163:57,61 and EL-164:30.** Electrical nodes lack resistance and capacitance (the pusher's are `("motor", Consumer, 12, 24, 0, 400)` and `("distribution", LogicGate, 12, .05, 0, 400)`, `LinearPusherElementDeclaration.cs:51`). The indicator is called "the pusher's binding" but its colours differ (`:67-69`). Bridge z position, housing position, bridge support and wedge material are not given. `MechanicalContractValues.cs:104` should be `:75-77`.
- **M12. EL-164:124 (A9).** `requirements.md:2734` says "changing a route or withholding an arrival prevents the goal"; A9 tests only withholding. Add the control "with `PowerIn` unwired one Counter reaches 3 and the other stays at 0".
- **M13. EL-184:34-35, :88 and `readiness/batch-j.md:95`.** Frame colliders copy a stale comment. The delivered gate has a sleeve Tube and two collar Tubes (`powered_gate.tscn:133-150`, asserted at `PoweredGateTests.cs:63-73`). Replacement: "Tube, outer 0.70, bore 0.65, half-length 0.4, axis X" and "Tubes, outer 0.78, bore 0.65, half-length 0.09 at x = ±0.4 (already solid)". Delete "not yet solid" from summary gap 11.
- **M14. EL-184:126,122-132.** A1's control uses a 1 kg Weight, exactly balancing the 1 kg blade; use 0.5 kg. No row maps to the requirement's "blocked blade" or "not instantaneous": add to A1 "the ball does not pass before x exceeds 0.75 m" and name A2 as the blocked case.
- **M15. EL-185:104 and summary gap 5.** "Every delivered package declares an empty AnchorPoints list" is false: `elements/PoweredGate/PoweredGateElementDeclaration.cs:69-72` declares one. Replacement: "declared by one package (Powered gate), validated and bundled, never compiled".
- **M16. EL-185:156,169 (A5).** A wired signal pulse has no `WorkshopCommandResult`. Replacement: "a second pulse leaves the committed released state unchanged and publishes no second released occurrence".
- **M17. EL-193:101-106,121.** No planned test fails if the declared offset is ignored, and `WorkshopDominoTests.cs:46` asserts `Assert.Equal(default, properties.LocalCentreOfMass)` while the spec says existing tests are unchanged. Add row A2b: "Given the stock tile, when compiled, then its `LocalCentreOfMass` is (0, −0.05, 0) and its principal moments are the declared homogeneous values. Test: `WorkshopDominoTests.cs:46` re-authored. Control: the box collider pose stays identity." Add the file to Files. Split capability row `:54`: solver offset present; package declaration to compiled record present, untested.
- **M18. EL-193 against CAT-023c (`epics.md:257-260`).** Three story criteria have no row. Replace A4's boundary with "±16 m admitted; just beyond refused; NaN refused; zero admitted" (`CuriousContraptions.Bundle/MassFrameDomain.cs:18-19`). Add a row for "an end-of-substep motion sample neither leads physics by a substep", or state the slice contract owns it. List `CAT-023-domino.md:88` ("unspecified — owner decision") as a document to update.
- **M19. EL-201:166 (K1).** The belt control slips about 0.24 rad at default speed, under the 0.5 rad it must show. Replacement: "Motor (`torque` 100 N·m, `speed` 20 rad/s)"; computed slip about 2.7 rad.
- **M20. EL-202:33-41,119.** `length` and `width` bind nothing (`ConveyorElementDeclaration.cs`: no collider, fixed hinge and ports), while CAT-019 requires them to define the surface (`requirements.md:570`). Add: "**CAT-019 residual, not closed here:** `length` and `width` are admitted, saved and refused out of range but bind no collider, hinge frame or port; every delivered conveyor is 3.0 × 1.2 m." Strike "answered by the delivered ranges" at `:119`. See G5.
- **M21. EL-203:100,96.** No delivered test compares work across the reverser; change "If no delivered test compares…" to "No delivered test compares…; the closure adds". The planned "Motor work ≥ ball KE + shaft KE" would pass a doubled output. Replacement: "over the same shaft travel, the Motor's committed work with one reverser equals the no-reverser bypass within 5%, and the ball's speed magnitude equals the bypass within 5% with opposite sign; work ≥ kinetic energy is kept as a second assertion." `ReverseTransmissionLifecycleTests.cs:104` does not refuse an Electrical wire into `DriveIn`; add `ConveyorTests.cs:529`.
- **M22. EL-209:147-151,143.** An empty-Battery control exists (`FanGuidanceTests.cs:112`, `:133`). Replacement: "A start-empty Battery is covered. Not covered: a Battery that runs out mid-run." In the new test replace "remaining energy is exactly zero" with "remaining is ≥ 0 and no more than 1% of authored energy" (the debit is quantised, `FanIntegrationTests.cs:526`). Add `BundlePortsTests.cs:262` for wrong domain.
- **M23. EL-215:197 (A1).** At stiffness 120 a 1 kg ball from 1 m uses the full 0.20 m stroke. Replacement: "at `damping_ratio` 0.5 (k 300) and at `stiffness` 1200 it is arrested clear of the base; at `stiffness` 120 it does not rebound above 0.05 m (reaching the base is allowed)".
- **M24. EL-215:200,205,227 (A4).** `requirements.md:6125` asks that loss balances work; A4 asserts only "never gains, ends lower". Replacement: "at rest the mechanical energy lost equals m·g·(drop + static compression) − ½·k·(m·g/k)² within the suite's energy envelope". No test simulates ζ = 0.8 (highest simulated is 0.5); say so in the probe.
- **M25. EL-216:117,123,199.** "`OffsetZ` present, untested; no delivered package binds X or Z" is false: `elements/Ramp/RampElementDeclaration.cs:46,49` binds `OffsetZ` and `VisibleSolidColliderTests.cs:813`, `:860-880` run it. Mark present; counts 6 / 0 / 0 / 0; data-only once H11 is settled.
- **M26. EL-216:44,168.** The doorway is not centred: at w = 0.6 the opening is z −0.29 to +0.31, so a ball has −0.02 m clearance on the stub side. Widen the cup to 0.66 m inner or remove "does not catch the jambs".

### Low

- **L1. EL-107:36,111; EL-108:36.** "Torus ring … outer 0.78, half-width 0.09" mixes the bend's torus collar with the gate's tube collar; name one.
- **L2. EL-110:28.** `SceneGeometry.cs:8` should be `:10`. `:203` "exactly 0": state it is an intentional zero.
- **L3. EL-156:29,131,132.** `:29` says a parameter-bound collider offset is not delivered; it is (`SceneGeometry.cs:24`). `:131` cites a free-body test for a hinge-carried case; `:132` and EL-111:135 cite a validator test for a behaviour claim.
- **L4. EL-184:36.** A drawn 0.2 × 0.2 slot has a solid box behind it; draw no slot.
- **L5. EL-193:29,32,106.** The body is a box plus two pip colliders (`domino.tscn:46-53`). m·d² is 2.4% of the centroidal moment, not "under 2%". The loading tests are `WorkshopPhysicsEngineTests` `:710` and `:438`, not `WorkshopBowlingTests`.
- **L6. EL-194:52,104.** `Visual_Stripe` stands 8 mm proud, not printed; add a flush redraw to the visible-solid remainder. `:104` cites a save-boundary test; cite `BundlePortsTests.cs:262`. EL-195:90,96 have two misplaced line citations.
- **L7. EL-205:172 (A5).** The generic wrong-domain tests cover Activation and Electrical only; make A5 definite. EL-206:157 depends on it.
- **L8. EL-207:57,108.** Decide the hook collider now (a closed torus eye like the Weight's). A Guide and a Load port on one dynamic body is a new composition: counts 3 / 3 / 0 / 1.
- **L9. EL-215:35,42-43.** Add the arrival-depth reason (0.09 ≥ the Trampoline's measured 0.046 m; closed rim). Editor policy: say `Reject` (`ParameterDescriptor.cs:39`).
- **L10. Summary table.** Corrections follow from M2, M3, M7, M25, L8, H9, H11 and H5. Contradiction 9 is wrong: fifteen delivered packages declare restitution 1 on the root body, so Pulley and Rope anchor are not outliers. The decision row "`CoordinatePerRadian` is +1, the delivered convention" is half true: Reverse transmission, Fan `DriveIn` and Wound spring declare −1.

## 3. Cross-batch conflicts

- **C1. Palette orders.** Batch J's provisional orders collide with other batches' specs: 170–178 with EL-044 to EL-052, EL-070, 071, 073, 074 and EL-179; 185 (EL-186 / EL-181), 186 (EL-207 / EL-182), 187 (EL-215 / EL-183), 188 (EL-216 / EL-211). The summary names only part of this. Decision: the coordinator assigns one band per batch.
- **C2. EL-205 Rope.** Batch J closes it as a duplicate with no package and removes it from "Rope materials" (EL-205:25,196). Batch I makes it a Family slice with EL-067 and requires the delivered rope to become the first declared connection material (`EL-067-steel-cable.md:20,94,161`; `readiness/batch-i.md` G3). Both cannot hold. Recommended: close EL-205 now against today's connection, and state in EL-205 that the connection-material story converts the delivered rope in the same change and re-runs the closure rows.
- **C3. Two mechanisms for connection variants.** Batch I declares a connection material through a package with no body (`steel_cable`; G8 names "rope, belt and chain"). Batch J gives belt and chain no package and puts a belt/chain kind with typed values on the engine connection. An engine enum of product kinds with product defaults sits badly with design guide D3. Decide one mechanism before `engine-mechanical-link-kinds` is written.
- **C4. Two names for the drain slice.** Batch H proposes `engine-supply-drive-demand`; Batch J proposes ENGINE-SUPPLY-DEMAND with key `powered-part-demand-drain`. One story, one key.
- **C5. Non-solid strands.** Batch I's owner item O1 covers wire, belt, rope and cable; Batch J's covers rope only, while EL-200 and EL-201 draw belts and chains with no collider. Use Batch I's wording.
- **C6. Ordering.** EL-202 and EL-203 close against today's rigid connection; run the "Drive train" closure before `engine-mechanical-link-kinds` (the summary says this; confirmed).

## 4. Engine-story gaps

None of the proposed slices below has text in `_bmad-output/planning-artifacts/epics.md` (twelve `ENGINE-*` stories at `:4423-4543`).

- **G1. Passage speed predicate (EL-109).** The summary's ENGINE-PASSAGE-SPEED text is adequate; add "amends REQ-12" and "proved on two differently parameterised fixtures".
- **G2. Drive work debits its source (EL-199, EL-196, EL-211).** Exists only at `_bmad-output/implementation-artifacts/deferred-work.md:209-213`. Add to the summary's text: "the debit equals the drive's committed positive work within [work, 1.01 × work]; a stalled drive debits zero; an exhausted source opens the route."
- **G3. Mechanical link kinds (EL-200, EL-201).** ENGINE-ASSEMBLY-JOINTS (`:4456-4465`) covers none of it. A standalone story is cleaner than a rider. Add to the summary's sentence: "The pitch radius is a new physical port value, distinct from the cosmetic rim; capacity uses the smaller pitch radius of the pair and the speed ratio remains the coordinate ratio. Acceptance: the same train just below and just above the declared capacity; only the second shows relative travel, and no sample gains energy." Subject to C3.
- **G4. Tensile reaction on a dynamic guide (EL-207).** EL-207:187 leaves the owner to "the coordinator". The code claim holds (`CuriousContraptions.Simulation/TensileConstraintSolver.cs:64-69`; effective mass `:56`). Proposed: "### Story ENGINE-TENSILE-GUIDE: Tensile reaction on a dynamic guide. *Sprint key: `engine-tensile-dynamic-guide`.* *Present today:* route geometry follows each guide body's pose; the length row's impulse and effective mass use the two end bodies only. *Missing:* for each guide of a taut route, the tension applied to the guide body at its entry and exit tangent points along the two unit tangents, with every dynamic guide body in the row's effective mass. *Unblocks:* EL-207. **Specific acceptance:** two differently parameterised dynamic guides each hold at half the free-end travel; an isolated two-load-plus-guide system gains no momentum or energy; a static guide behaves as today."
- **G5. Conveyor size binding (CAT-019 residual, M20).** No owner. Proposed: "a port position and a hinge base frame bound to more than one parameter, so a package can scale a carrier with its hinge and ports".
- **G6. ENGINE-ASSEMBLY-JOINTS.** The summary's gaps 4 and 5 are confirmed absent from the story text; apply M15 to the proposed wording.
- **G7. ENGINE-FRACTURE (`:4467-4475`).** Add: "the limit may be declared on an attachment inside one package as well as on a player-made connection; the story fixes the load measure and the meaning of 'sustained', and states whether a transient rolling or impact load counts". H1 turns on it.
- **G8. Profile collider.** Needed only if EL-158 fails its probe after H5; the summary's text is sufficient.
- **G9. "S8" lesson-fixture story.** Named in 24 of the specs; `delivery-batches.md` defines S1–S6 only and no draft defines S8.

## 5. Items for the owner

- **O1. EL-200 changes delivered behaviour.** Every mechanical connection becomes a slipping belt: six delivered packages and levels 16–18 are affected (the Fan at 14 N·m; a Wound spring at maximum settings, 38 N·m against 26 N·m). Forward-only in form, but beyond the delegation. Recommended: accept, with pitch radius = rim radius and default tension 130 N. EL-201's new chain refusal on Conveyor, Windmill and Governor goes with it.
- **O2. Non-solid strands (EL-205, EL-067, EL-200, EL-201).** Real: only the Balloon string has a decision. Recommended: one decision approving every connection strand (wire, belt, chain, rope, cable) as a non-solid drawing.
- **O3. EL-199 drops `efficiency`.** `delivery-batches.md:145` names it as this identity's delta. Recommended: accept bearing friction as the declared loss.
- **O4. EL-162 exposes a two-way lane choice as a Real**, because collider bindings refuse a Choice (`BundleValidator.cs:419`). Recommended: accept now with M10's text; the alternative is a small engine slice that blocks EL-162 to EL-164.
- **O5. EL-193 offset.** −0.05 m moves the tipping angle by 1.2°, which no scene test observes at a 1° allowance. Recommended: accept with the compiled-value test (M17).
- **O6. EL-194 "typed signal integration".** Confirm the `spring_signal` level satisfies it, not a port on the Springboard. Recommended: yes.
- **O7. EL-195 "bounded supplied".** Confirm source-bounded recharge is the intended bound now that 50 W is inert. Recommended: yes.

H1–H7, H11 and the numeric fixes are planning decisions under the delegation, not owner items.

## 6. Not verified

- Every dynamics figure, the authors' and this audit's (H1–H5, H8, M5, M8, M19, M23), is a hand calculation.
- Test bodies behind "present" rows of EL-107, 109, 157, 158, 162, 185, 186, 199, 201, 207, and many in the others, were checked by name and line only.
- Whether two unjointed moving bodies of one package collide (cam follower, governor pad); inferred from `engine/gpu/WorkshopPhysicsCompiler.cs:181`, `:223`.
- Whether a parameter-bound mass frame is admitted on a hinge body (EL-110); whether a span binding joins two body-bound nodes (EL-111); whether `RopeGeometry.EvaluateRoute` wraps under a guide (EL-207).
- How today's visible-solid matcher compares a mesh arc with cylinders (`Tol = 2e-6f` at `VisibleSolidColliderTests.cs:33`).
- Whether contact rows take lever arms from an offset centre of mass (CAT-023c); whether the Battery clamps to zero on exhaustion.
- Whether placement refuses overlapping static parts (H11) or accepts a 4.6 m part; minimum admitted body mass for the 0.1 kg clamp.
- Icons, `DESIGN.md` colours and campaign rows were not checked for any spec.
- Whether any delivered test other than `ConveyorTests.cs:453` and `FanIntegrationTests.cs:541` exceeds the default belt capacity.
