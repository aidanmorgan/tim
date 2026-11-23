# Second audit, group 2: formerly Not-ready element specs (Batches F, G, K, L)

Independent, read-only re-audit, 23 November 2026, checkout `spec-drafts` at HEAD `8d1d68aa`. The 20 specs are audited as they stand in the working tree (all 20 carry uncommitted edits). Nothing was built, run or edited. Every figure below is a hand calculation from the declared values. EL-105's guidance was also checked with a throwaway 2-D point model (dt 1/480 s, one-tick eye lag).

Shared facts used throughout:

- **Contact pair laws** (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117-119`): friction √(μA·μB), restitution e_A·e_B, bounce threshold max(A, B).
- **Bench material** (`engine/gpu/WorkshopPhysicsCompiler.cs:101`): μ 0.3, e 1, threshold 0.1 m/s.
- **Delivered materials** (bundle): almost every part is μ 0.3. Exceptions: Domino 0.6, Springboard 0.1. Ball (0.3, 0.55, C_rr 0.035), Tennis (0.3, 0.85), Bowling (0.3, 0.14, 0.03), Weight (0.3, 0.08, 0.03), Wall (0.3, 1).
- **Admitted shape minima** (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:218-270`): sphere r ≥ 1/32 m; cylinder, tube and frustum r ≥ 1/16 m; box half-extent ≥ 1/1024 m; tube bore ≥ 1/64 m; torus tube ≥ 1/512 m.
- **Run length:** default 30 s (`CuriousContraptions.Abstractions/CadenceValues.cs:77`), maximum 300 s.
- **Battery:** 120 W default, 1 J per tick.
- **Water:** ρ = 16 kg/m³, conventions L1–L18 (`EL-001-finite-reservoir.md:26-43`).

Spec paths are under `docs/planning/elements/`.

## 1. Result

| Spec | Verdict | Blocking or main fixes |
|---|---|---|
| EL-007 Open gutter | Ready with fixes | W3 and W4 have no owner (Medium) |
| EL-017 Mechanical float valve | Ready with fixes | No `InitialAngle`; V4 bound below the rise transient; V3 Run length (Medium) |
| EL-021 Sluice gate | Ready with fixes | Top beam blocks the solid rope (High); `ENGINE-CONNECTION-MATERIALS` not named |
| EL-023 Archimedes screw | Ready with fixes | A6 cannot exercise the head cap (High) |
| EL-025 Tipping-bucket water clock | Ready with fixes | C1 asserts the end state at the instant of the Pulse (High) |
| EL-028 Buoyant platform | Ready with fixes | The seat collar is too low; B2 fails (High) |
| EL-029 Boat | Ready with fixes | Keel-at-surface start breaks V5; the leak port becomes a free end on the floor (two High) |
| EL-169 Cork float | Ready with fixes | O2 not averaged and fails at ζ 0.006 (Medium) |
| EL-170 Raft | **Not ready** | H4 cannot separate the members: the battens carry the side logs after a break (High; needs a geometry decision) |
| EL-175 Optical fibre | Ready with fixes | Fixture gives a diagonal cable through the couplers' own art; Y8 asks a ball to rest on a rod (Medium) |
| EL-076 Programmable ball | Ready with fixes | Low only |
| EL-078 Can opener + `sealed_can` | Ready with fixes | Low only (drive torque headroom, stale cite) |
| EL-079 Electric mixer | Ready with fixes | Low only |
| EL-080 Programmable box | Ready with fixes | Shove prediction ignores the ball's follow-through; fit list wrong (Medium) |
| EL-084 Fragile fish bowl | Ready with fixes | F7 control cannot pass (High); F1 depends on landing attitude (Medium) |
| EL-086 Obstacle-reversing walker | Ready with fixes | W7 squeeze is impossible with 0.74 m Walls (High); trailing bumper bottoms on arrival (Medium) |
| EL-088 Fish tank lure | Ready with fixes | T6: a ball wider than the interior breaks both long panes (High) |
| EL-099 Pool pocket | Ready with fixes | Low only |
| EL-101 Thumb tack | Ready with fixes | "Puncture-resistant" half of the requirement has no failing control (Medium) |
| EL-105 Toy missile | **Not ready** | Eyes inside the Nose collider; acceptance cone absent from the optical convention; threshold, aperture and beacon values unstated (High; needs a convention decision) |

**Counts:** 0 Ready, 18 Ready with fixes, 2 Not ready (EL-105, EL-170).

**Verdict rule.** "Not ready" means the fix needs a design or convention choice: for EL-170, an open geometry choice; for EL-105, a change to EL-176 convention 8 plus unstated threshold, aperture and beacon values. "Ready with fixes" means every fix is a mechanical replacement given below, even where the finding is High (EL-021, 023, 025, 028, 029, 084, 086, 088).

Every first-round blocking finding is resolved in substance, with one exception: **EL-028 H-4 is open**. The repair added a seat, but B2 still fails, now through the seat's geometry. Nothing was merely reworded. The remaining defects are mostly new, introduced by the repairs:

- **Geometry the repair did not check against collider contact:** EL-028 seat, EL-170 battens, EL-088 interior width, EL-021 beam.
- **Rows asserted at the wrong instant or without averaging:** EL-025 C1, EL-029 V5, EL-169 O2.
- **Controls that cannot pass or cannot fail:** EL-084 F7, EL-101 A5, EL-023 A6, EL-086 W7.

Every spec carries the 3a programme sentence (with capitalisation variants). None says "non-solid tether". All 40 cited `requirements.md` anchors exist (each grepped as an `<a id>` target), and no criterion was found dropped or weakened.

### The five most important findings

1. **EL-170 (Not ready).** The battens on body 0 are 0.94 m long, lie 0.005 m over the side logs, and the bodies collide (`EL-170:26`, `:29`; I confirmed this). After a joint breaks, the side log rises 5 mm onto a batten and carries the same 3.27 N by contact. So H4's "separated members change stability" is not established and is likely false, and the requirement anchor `element-170` rests on it. A geometry redesign is needed (section 2, EL-170).
2. **EL-105 (Not ready).** The eye apertures at (0.44, ±0.03, 0) lie 0.05 m from the Nose sphere's centre, inside its 0.08 m radius, so both are occluded.
   - The 25° acceptance cone that creates the 6° dead band is not in EL-176 convention 8, which has only the facing term.
   - The lit threshold is a convention-9 threshold node owned by `ENGINE-SIGNAL-NODES`, which the spec never names.
   - Aperture area, threshold and beacon power are unstated, so A1's "lit at 6.3 m" cannot be computed.
3. **EL-028 seat.** The 0.04 m seat boxes put a Basketball's bottom at 0.125 m, below the deck top at 0.15. The ball rests on the deck, rolls 0.04 m to the collar and heels the deck about 10.5°, so B2 (< 5°) fails. The claimed 36° retention is really 28°. I confirmed this independently from `EL-028:26` and `:49`.
4. **EL-029.**
   - Every boat starts keel-at-surface (`:109`). A 3.0 kg load then dips about 0.52 m on the first downswing, past the 0.35 m gunwale, so V5 and its control fail.
   - The leak port at y −0.185 lies below the keel. When the hull rests on a floor, that point is outside every interior, so the port resolves to no node. L5 leaves that case undefined (free end or dead port). As a free end it drains the bilge to Spilled, a refloat and refill limit cycle; as a dead port the boat stops leaking. Either breaks V5.
5. **Controls that cannot behave as written.**
   - EL-084 F7: a 4 kg ball at 1.5 m/s keeps 1.30 m/s after hitting the 0.6 kg bowl. It is never "stopped or turned back".
   - EL-086 W7: Walls 0.74 m apart around a 0.72 m toy leave 0.02 m of slack. Both bumpers can be in together only below 0.68 m, so the toy shuttles roughly a dozen times in 5 s instead of stalling.
   - EL-088 T6: the 0.56 m Bowling ball cannot pass the 0.52 m interior. It strikes both long panes' end edges at 25.3 N·s each, above 16, so they break.
   - EL-023 A6: the outlet sits 0.80 m above the intake. "Outlet 1.30 m above the surface" therefore leaves the intake dry, and the head cap is never exercised.

   I confirmed EL-084, EL-086 and EL-088 against the spec text.

## 2. Per-spec findings

### EL-007 Open gutter: Ready with fixes

**First round.**
- C-1 (two geometries for contents and limit): resolved in substance. One L4/L11 relation is used: wedge contents 0.006 m³ (`:54`), limit 0.095 m³/s with C(θ) 0.091 m³ (`:55`), G1 boundary (`:99`), G4 fed by two mouths (`:102`).
- H-2b (no closing end): resolved at `:56`.
- G-1 (programme): resolved (`:17`, `:128`).

**Re-derivations.**
- Datum drop: 3·sin 5° = 0.2615 m.
- Low-mouth head: (0.0482/0.21261)² = 0.0514 m. The depth is in the wedge regime (0.0516 ≤ w·tan θ = 0.262). V = 0.40·0.0516²/(2·0.0875) = 0.0061 m³.
- G1 can pass. It fails under the old flat-level rule (0.062 m³).
- Limit: tan 5° = 0.0875 > H/w = 0.0667, so the second branch applies: C(θ) = 0.24·0.2/(2·3·0.0875) = 0.0914 m³, and Q = 0.21261·√(0.2·cos 5°) = 0.0949 m³/s.
- G4: 0.167 > 0.095, so it overtops. One mouth gives head 0.154 < 0.199, so it does not. Both directions can fail.
- G3: 0.0128 m at each end.
- G7: a 0.34 m ball rides on the rim edges and dips 0.065 m. The row holds.

**Decisions and conformance.**
- Owner decision 20 is applied. Points 1–7 are met. No row is over 30 s.
- Anchors `element-007` (adverse slope stalls or spills) is G2; `sequence-task-384` is carried.

**Findings.**
- **Medium, `EL-007-open-gutter.md:69-70`, `:75`, `:128`: W3 and W4 have no owner** (coordinator decision 16: the one fluid port record is `ENGINE-GAS-NETWORK`'s).
  - In `:69`'s Use cell, write: "store and mouths; the one fluid port record is `ENGINE-GAS-NETWORK`'s (coordinator decision 16)".
  - In `:70`, write: "narrower: socket count, package-local ids, capture distance 0.20 m and angle 20° are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`".
  - In `:75`, write: "…owners `ENGINE-LIQUID`, with W3's socket form `ENGINE-CAPABILITY-INSTANCES` and W4's record `ENGINE-GAS-NETWORK`".
  - In `:128`, put "`ENGINE-GAS-NETWORK` (the one fluid port record);" before `ENGINE-LIQUID`.
- **Low, `:102` (G4): the rig has no placement.** A bare mouth's stream lands at a rig-dependent point. Add: "two full Water tanks side by side on Wall blocks, mouths facing +X 0.30 m above the high end's rim, so each stream lands about 1.1 m along the channel".

### EL-017 Mechanical float valve: Ready with fixes

**First round.**
- H-1 (rig, limits and rows contradict): resolved in substance. Limits are −45° to +5° (`:25`); the shut-off is −20° with a range of −25° to −5° (`:32`); the hinge stands 0.60 m up (`:107`). V4 uses EL-004's `initial_volume` (which exists at `EL-004:30`). V5 rests at −34.8°.
- C-4 (stream rule, arm moved sideways): resolved (`:25`, L8 interior credit).

**Re-derivations.**
- Centre of mass at 0.54 m. Required buoyancy 0.25·9.81·0.54/0.70 = 1.892 N, so V_sub = 1.892/156.96 = 0.01206 m³ (36 % of the float). Cap depth 0.162 m puts the surface 0.038 m below the float's centre.
- Held depth at −20°: 0.60 − 0.70·sin 20° − 0.038 = 0.323 m. At −25°: 0.266 m; at −15°: 0.381 m.
- Rest at −asin(0.4/0.7) = −34.85°.
- V1 can pass, and fails (overflow) without immersion. V2 (blocked arm) overflows and can fail.
- **V4 can fail on a correct build.** Starting on the floor, the fully submerged float (5.26 N) takes about 0.17 s to rise 14.8° at a mean opening of about 2/3. That passes about 0.0482·0.67·0.17 = 0.005 m³, over the 0.004 m³ bound.
- Settling: ζ ≈ 0.03, τ ≈ 4 s. V1's 5 s quiet period is reached in about 25–35 s, inside the stated 60 s.

**Decisions and conformance.**
- Anchor `element-017` (immobilised linkage prevents closure) is V2; `sequence-task-387` is met.

**Findings.**
- **Medium, `EL-017-mechanical-float-valve.md:25`: no `InitialAngle`.** `RevoluteHingeDescriptor` requires one (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:159`). Append: "Initial angle −34° (in the reference rig the float starts 0.009 m above the basin floor and settles onto it; with no vessel it falls to the −45° stop)."
- **Medium, `:114` (V4): the bound sits below the rise transient.** Replace the first clause with: "Given the rig with the basin authored at `initial_volume` 0.5 (full, depth 0.40 m), when Run advances 10 s, then from the first sampled tick at which the seat opening is 0 it stays 0 to the end, and over those ticks the tank loses less than 0.004 m³; …".
- **Medium, `:113` (V3): Run length missing** (same rig and settling as V1). Append "(stall guard 60 s; the level declares a Run length of 60 s, `ENGINE-RUN-LENGTH`)".
- **Low, `:84`: two owners for one capability.** Replace "`ENGINE-LIQUID`; the parameter term uses `ENGINE-PARAMETER-BOUND-FRAMES`" with "`ENGINE-LIQUID` (L14 b: a coordinate map with parameter terms)".
- **Low, `:80`, `:140`: add the port-record owner.** Add "the one fluid port record is `ENGINE-GAS-NETWORK`'s", and list `ENGINE-GAS-NETWORK` before `ENGINE-LIQUID` in the prerequisites.
- **Low, `:111` (V1): stall guard not tied to a hand time** (L17). Add "(hand estimate about 30 s: fill about 8 s, throttle τ ≈ 3 s, arm settling τ ≈ 4 s)".

### EL-021 Sluice gate: Ready with fixes

**First round.**
- H-2a (inverted blade opens): resolved in substance. The rest position is −0.75 m, preloading the spring to 10.5 N (`:27`). The rest-position field is owned by `ENGINE-JOINT-KINDS` (coordinator decisions 21 and 25; `:67`, `:137`). Today's `PrismaticSliderDescriptor` has only `Compression`.
- H-2b (upstream gutter drains from its far end): resolved; the gutter slopes toward the gate (`:58`, `:104`).

**Re-derivations.**
- Lift needed: 9.81 + 14·0.75 = 20.31 N closed, 23.81 N at 0.25 m.
- Weight masses: 2.5 kg (24.53 N) gives full lift; 2.25 kg gives 0.126 m; 2.14 kg gives 0.049 m; 2.0 kg stays closed. SG3 and SG4 can pass and fail.
- Inverted: net 10.5 − 9.81 = 0.69 N closing. With the rest position at 0 the blade falls open, so SG5 can fail.
- Flow: K_c/√3 = 0.1228. At s = 0.25, K = 0.0501, which needs 0.463 m of head against 0.225 m available, so it passes 0.0238 m³/s.
- SG4 overtops at about 7 s (< 12 s). SG2 reaches C(θ) 0.0914 m³ in 2.7 s.

**Decisions and conformance.**
- Anchor `element-021` (closed gate retains upstream volume) is SG2. `sequence-task-390` is carried by EL-027.

**Findings.**
- **High, `EL-021-sluice-gate.md:25` (also `:27`, `:39`, `:80`, `:84-85`): the top beam blocks the solid rope.** The beam spans x ±0.15, z ±0.26, y 0.53–0.59, directly over the tie at the blade's top centre (`:28`). Under owner decision 1 the rope is solid, so the vertical pull that every SG3/SG4 Weight figure assumes would pass through the beam. The colliderless rope today breaks visible = solid there.
  - Replace "; top beam 0.30 × 0.06 × 0.52 at (0, +0.56, 0)" with "; no top beam: the slider's 0.25 m limit is the upper stop and the tie's rope rises clear between the posts".
  - In `:27`, replace "under the beam at 0.53 m" with "under the posts' tops at 0.53 m".
  - The frame has 3 boxes, 4 colliders in all. Delete `Visual/Beam` at `:39`.
- **Medium, `:17`, `:137`, `:68`: owner decision 1 is half applied.** `:60` records the rope defect, but the spec is never "blocked on ENGINE-CONNECTION-MATERIALS (solid strand)", although SG1, SG3, SG4 and SG7 use the rope.
  - Add "`ENGINE-CONNECTION-MATERIALS` (solid strand; owner decision 1, 22 November 2026)" to Status and Prerequisites.
  - Append to the W20 row: "the rope's collider is missing: `ENGINE-CONNECTION-MATERIALS`".
- **Low, `:71-72`:** add the W3/W4 owner text as for EL-007.
- **Low, `:108` (SG1): the downstream check cannot distinguish wet from empty.** The downstream wedge holds about 0.0015 m³, under the 0.004 m³ tolerance. Replace "both gutters hold liquid" with "the basin's gain rate settles at 0.0341 m³/s within 10%".

### EL-023 Archimedes screw: Ready with fixes

**First round.**
- C-4 (rotor covers the outlet): resolved in substance. A spout ledge was added; the outlet at (1.10, 0.17) is past the tube's maximum x (0.961) and the rotor's (0.993) (`:26`, `:30`).
- H-1 (closed edge, old input rule): resolved. The edge is open when ungranted and there is one torque rule (`:28`, `:63`, `:114`).
- H-7 (post into the bench): resolved (`:26`, `:59`).
- S-1 to S-6 are applied.
- Coordinator decision 21's open edge and spout box are present.

**Re-derivations.**
- Geometry: intake at (−0.791, −0.630). The tube clears the post by 0.032 m and the basin wall by 0.107 m.
- Rate: K = K₀/√2 = 0.05904. Power binds at d = 0.40: h_p·√(h_p − 0.45) = 1.0791, so h_p = 1.225 m and Q = 0.0520 m³/s. At d = 0.20, Q = 0.0457 m³/s at 8.97 W.
- Time 9.5 s; hydraulic work 85.7 J, so 171 J of shaft work.
- Pour lands at x ≤ 2.005 < 2.07, so it is caught.
- At 2 rad/s, h_p = 0.767 and Q = 0.0332. A5 can pass and can fail (0.0528 if the power cap were ignored).
- Power: 20 W is 0.167 J per tick, against 1 J.
- **A6 cannot pass as intended.** The outlet is fixed 0.80 m above the intake. "Outlet 1.30 m above the supply surface" leaves the intake 0.50 m above the surface: dry, so the row passes for the wrong reason, and the boundary ("1.20 m delivers") fails on a correct build. The 1.25 m cap binds only when the part is pitched: Δy(φ) = 1.891·sin φ + 0.800·cos φ gives 1.40 m at φ = +20.1° and 1.30 m at +16.4°. This is Batch G's H-6 pattern again.

**Decisions and conformance.**
- Anchors `element-023` (wrong rotation or dry intake cannot deliver) are A2 and A3; `sequence-task-389` is A1.

**Findings.**
- **High, `EL-023-archimedes-screw.md:116` (A6).** Replace with: "Given the part turned a further +20° about Z (tube at 50°), placed so its intake is 0.10 m below the surface of a supply basin raised on Wall blocks (outlet 1.30 m above that surface), with the rotor driven forward, then every committed flow is under 0.0005 m³/s | … | boundary: turned +16.4° instead (outlet 1.20 m above the surface), the first sampled flow is 0.0132 m³/s within 10%".
- **Medium, `:64`, `:67`.** In `:67`'s second sentence write "the 1.25 m head cap binds only when the part is pitched steeper than its 30° (A6); upright, the intake leg bounds the lift". Append to `:64`: "(reachable only by pitching the part, A6)".
- **Low, `:114` (A4 boundary): the pose is unstated.** Add "the part turned −25.7° about Z (tube at 4.3°), intake 0.10 m below the supply surface".
- **Low, `:30`:** the rotor's lowest point is (0.941, 0.370), not (0.967, 0.385). The clearance claim is unaffected.

### EL-025 Tipping-bucket water clock: Ready with fixes

**First round.**
- C-1 (a, b) (does not tip): resolved in substance by the twin-chamber see-saw (coordinator decision 21; `:7`, `:27-32`), with contents at the liquid centroid (L12 b / A5).
- C-1 (c) (L11 branch): resolved (`:71`).
- S-6 (interior): resolved (`:30`).

**Re-derivations.**
- **Empty stability.** The centre of mass is 0.06·sin 35° = 0.0344 m toward the lowered side, so 1.5·9.81·0.0344 = 0.506 N·m holds the −35° stop. The part is bistable.
- **First tip.** Arm 0.30 − 0.1377 = 0.1623 m, giving 25.47 N·m per m³, so the tip comes at V = 0.526/25.47 = 0.0207 m³.
- **Moments as the rocker swings.**
  - At −35°: net +0.027 N·m.
  - At −25°: trapezoid branch, centroid x −0.276; net 0.905 − 0.373 = +0.53 N·m.
  - At +35°: +0.73 N·m. The moment keeps reversing the right way through the swing.
- **Retained volume** 0.01/tan θ: 0.0173 down to 0.0036 m³.
- **Return tip** at (0.506 + 0.227 + 0.02)/25.47 = 0.0296 m³.
- **Clearance and C5.** The lowest corner clears the basin wall by 0.094 m. C5's Wall blocks the tip.
- **Period × cycles.** The tap flow falls to 0.0093 m³/s by 20 s, so about 7 tips in 20 s against C3's floor of 5. This fits the default 30 s.
- **C2:** the control at 0.77× the tip volume and the boundary at 1.21× can each pass and fail.
- **C1 cannot pass as worded.** The Pulse commits in the tick the angle crosses +10°. At that instant the angle is about +10° and the chamber still holds about 0.01 m³, so "above +30°" and "at most 0.0076" become true only about 0.2 s later.

**Decisions and conformance.**
- Signal nodes are owned by `ENGINE-SIGNAL-NODES`; the contents mass kind by `ENGINE-LIQUID`. Owner decision 20 is applied. Points 1–7 are met.

**Findings.**
- **High, `EL-025-tipping-bucket-water-clock.md:120` (C1).** Replace "when Run advances until the first Pulse (stall guard 10 s), then…" with "when Run advances until the first Pulse and then 1 s more (stall guard 10 s), then…". Likewise replace "until the second Pulse (stall guard 10 s more), then…" with "until the second Pulse and then 1 s more (stall guard 10 s more), then…".
- **Low, `:59`: contradicts EL-003's three-aperture tap** (`EL-003:54`). Replace "series K = 0.08349 / √(1 + 8²) = 0.01036" with "series K = 0.08349 / √(2 + 8²) = 0.01028 (EL-003's three apertures)".
- **Low, `:121`.** Replace "K = 0.08349 / √2 = 0.05904, so the tank empties in … 4.3 s, and the boundary's 0.025 m³ in 5.4 s, tipping at about 3.1 s" with "K = 0.08349 / √3 = 0.0482, so the tank empties in 2 × √0.016 / 0.0482 = 5.3 s, and the boundary's 0.025 m³ in 6.6 s, tipping at about 3.8 s". The outcomes stand.
- **Low, `:63-70`:** add the steady-tip moment, "net +0.57 N·m at +25°, +0.64 N·m on the far stop".

### EL-028 Buoyant platform: Ready with fixes

**First round.**
- H-4 (B2: the deck does not carry a Basketball): **open**. The repair added a seat, but the seat does not seat a ball and B2 still fails (below).
- Its sub-points are resolved: basin 0.3125 m³ with level 0.35 m against the 0.40 m rim (`:93`); B3 asserts the end state (`:99`).
- L-4 (`:60`), S-1 and S-2 (`:5`, `:124`) are resolved.

**Re-derivations.**
- Draughts: 1/12.96 = 0.0772 m; with a Tennis ball 0.104 m; with a Basketball 0.154 m. Union volume 0.2664 m³.
- **Seat.** The seat boxes are 0.04 m high at y 0.17, so their tops are at 0.19 m and their inner edges 0.20 m from the axis. A Basketball on those edges would have its centre at 0.19 + √(0.34² − 0.20²) = 0.465 and its bottom at 0.125, below the deck top at 0.15.
  - It therefore rests on the deck at y 0.49 (0.64 m above the keel), with 0.04 m of play to the collar, and a centred ball on a flat deck is unstable.
  - KG = (0.17 + 0.64)/2 = 0.405, so GM = 0.0772 + 0.4374 − 0.405 = 0.110 m.
  - Heel ≈ 0.04/(2·0.110) = 0.18 rad = **10.5°**, so B2's < 5° fails.
  - Retention is atan(0.16/0.30) = 28°, not the 36° at `:50`. The Tennis ball has the same flaw.
- B1: k = 127 N/m, ω = 11.3 rad/s, ζ = 0.010. The row is averaged, so it can pass, and it fails if buoyancy is missing.
- B3: deck plus Bowling ball needs 0.58 m of depth to float; grounded at 0.40 m, lift is 4.39 kg against 5 kg. Can pass and can fail.
- B4: 0.65 kg of lift against 1.0 kg, so the deck stays aground.

**Decisions and conformance.**
- Anchor `element-028` (displacement limits; overload or lost water) is B3–B5. Points 1–7 are met. W10 is owned by `ENGINE-LIQUID`.

**Findings.**
- **High, `EL-028-buoyant-platform.md:26` (also `:27`, `:49`, `:50`, `:53-56`).** Replace the seat clause with: "A centre seat of four boxes 0.12 m high forming a square pocket 0.40 m across: two 0.04 × 0.12 × 0.48 at (±0.22, 0.21, 0) and two 0.40 × 0.12 × 0.04 at (0, 0.21, ±0.22); union volume 0.243 + 0.0206 + 0.0084 = 0.2721 m³; centre of mass (0, 0.024, 0)."
  - With this seat a Basketball sits on the edges with its centre at 0.545 (0.695 above the keel, 0.055 m clear of the deck), and GM = 0.0772 + 0.4374 − 0.434 = 0.080 m. A Tennis ball has GM 0.424 m. The load limit is 3.35 kg; retention 36°.
  - Recompute the large-angle table at `:55-56` with these values.
- **Low, `:98`:** "mean draught" has no window. Use "averaged over the last 2 s".

### EL-029 Boat: Ready with fixes

**First round.**
- C-2: resolved in substance (`:7`, `:60`, `:64`, V5 at `:117`). The solve agrees: 2.0 kg Weight (r 0.2555, h 0.406); h_leak = 0.2104 − 1.206·V_b; gunwale under at 0.0645 m³ after 2.9 s.
- C-3: resolved via A1, A2 and A4 in EL-001 (`:29-31`, `:56`, `:66`).
- M-12: two envelope boxes (`:29`, `:97`).
- L-3, S-2 to S-5 are resolved; V4 declares 75 s (`:116`).

**Re-derivations.**
- Empty draught 0.0868 m.
- With a Basketball: KB 0.087, BM across 0.307 and along 0.389, KG 0.2325, so GM is 0.161 across and 0.243 along. Heel 8.5° along and 3.9° across, under V2's 15°, so V2 can pass and can fail.
- Leak with no cargo closes at 0.1207 m³ after 23.6 s. Free-surface GM 0.072 m.
- With the 2.0 kg Weight, GM 0.141.
- **The start pose breaks V5.** With a 3.0 kg load from draught 0: k = 113 N/m, ω = 6.14 rad/s, ζ = 0.019. The first downswing reaches about 0.52 m of draught, past the 0.35 m gunwale, within about 0.3 s. V5's control fails at t = 0 and during the bob, which still has about 0.045 m of amplitude at 15 s.
- **The leak port breaks on a floor.** The immersed port at y −0.185 is 0.01 m below the keel. When the hull rests on the chamber floor (V3, V5), that point is below the floor datum and in no interior, so the port resolves to no node, a case L5 leaves undefined.
  - Read as a free end: the bilge drains to Spilled, the hull refloats (3.0 kg against 4.03 kg of lift) and refills, a limit cycle.
  - Read as a dead port: the boat stops leaking on the floor.
  - Either way V5 breaks.
- V7 can pass, and its unpowered control can fail. There is no powered drive, so the Battery limits do not apply.

**Decisions and conformance.**
- Decisions 16, 20 and 21 are applied. Anchor `element-029` is V3–V5 and V7.

**Findings.**
- **High, `EL-029-boat.md:109`.** Replace "the boat is placed with its keel at the surface" with "the boat is placed at the sound draught of its authored load (0.087 m empty, 0.174 m with a Basketball, 0.260 m with the 2.0 kg Weight), cargo set at rest at Run start". In V5's control at `:117`, replace "for 15 s" with "averaged over the last 2 s of 15 s".
- **High, `:31`, `:49`.** Replace "at (0, −0.185, 0), 0.01 m below the keel and so outside the envelope" with "at (0, −0.15, 0.405), 0.005 m outside the starboard side wall and 0.025 m above the keel, so outside the envelope and inside the surrounding store's interior whether the boat floats or rests on its floor; the bilge mouth is 0.025 m below its floor datum (A7)". Move `Visual/Plug` to that wall. The leak formulas are unchanged.
- **Low, `:93`:** "three bindings" should be "two bindings" (`:39`, `:97`).
- **Low, `:114`:** add "averaged over the last 2 s".

### EL-169 Cork float: Ready with fixes

**First round.**
- H-5: resolved in substance: r 0.40, h 0.15, 0.15 kg, with a flat 0.5 kg Weight (`:26-27`, `:46-49`).
- L-4 (`:53`) and S-2 (`:116`) are resolved.

**Re-derivations.**
- V = 0.0754 m³. Draught 0.0187 m unloaded, 0.0808 m with the Weight.
- 0.5 kg Weight: r 0.1834, h 0.291.
- KB 0.0404; BM = 0.0201/0.0406 = 0.495; KG 0.245; **GM 0.29 m**.
- GZ at 10° = 0.0517 m, a righting moment of 0.33 N·m.
- O3, overload with a 1.5 kg Weight: the pair needs 0.315 m of draught against a 0.38 m level, so the cork's top goes under. Can pass, and fails if lift is overstated.
- O4: 0.08 kg of lift against 0.15 kg, so the cork stays aground.
- **O2 fails at the starting damping.** Damping acts on the cork only (0.081 N·s/m); ω = 11.0 rad/s, ζ = 0.0057. From a 0.062 m offset, the amplitude at 10 s is 0.033 m, over the ±0.015 m tolerance.
- The Weight's slip angle is atan √(0.6·0.3) = 23°, not 31°.

**Decisions and conformance.**
- Anchor `element-169` is O2–O4. The spec conforms.

**Findings.**
- **Medium, `EL-169-cork-float.md:91`.** Replace "when Run advances 10 s, then the draught is 0.081 m within 0.015 m" with "when Run advances 20 s, then the draught averaged over the last 2 s is 0.081 m within 0.01 m". The boundary becomes "0.050 m within 0.01 m".
- **Low, `:49`.** Replace "The Weight slides at atan(0.6) = 31°" with "The Weight slides at atan(√(0.6 × 0.3)) = 23° (geometric-mean friction, `WorkshopConstraintSolver.cs:119`)".

### EL-170 Raft: Not ready

**First round.**
- M-8 is resolved on four counts: ordering (`:125`), break measure is the peak substep force (`:29`), bodies collide (`:29`), the ball is replaced by a Weight (`:49`). The collision change creates the High below.
- M-12 (`:81`) and M-2 (`:99-100`) are resolved.

**Re-derivations.**
- Submerged fraction 27.6 % unloaded, 64.5 % loaded.
- KB −0.0453, KG 0.2107. GM 0.226 across (BM 0.482) and 0.085 along (BM 0.341).
- Joint load 3.27 N floating; 7.4 N while H3 sinks, under 20 N.
- H2: amplitude 0.006 m after 20 s, about ±2.4 points, inside the 6-point tolerance. Can pass.
- **H4 is not established.** The battens on body 0 overlie the side logs by 0.005 m and the bodies collide (`:26`, `:29`; confirmed). After a joint breaks, the side log (3.27 N of upward lift, `:52`) rises 5 mm onto the batten and carries the same load by contact. The Weight stays above water and the three log heights differ by 0.005 m, inside H4's 0.02 m. H4, and with it the anchor `element-170` clause "separated members change stability", is unsupported and likely false.
- The slip angle is 23°, not 31°.

**Findings.**
- **High, `EL-170-raft.md:25-27`, `:51-54`, `:100`: a design decision is needed.** One working geometry:
  - Logs at x = −0.40, 0, +0.40 (0.10 m gaps).
  - Two battens 0.30 × 0.04 × 0.08 at (0, 0.175, ±0.18), on body 0 only.
  - The 1.0 kg Weight (r 0.207) then clears the side logs' inner edges at 0.25 m. After a break the centre log lifts 16 × 0.0585 − 0.25 = 0.69 kg against 1.0 kg, so it sinks with no contact path.

  Recompute `:46-57`, H1 and H2 (across-raft BM rises to about 0.73 m). The author must choose.
- **Medium, `:99`.** Replace "then the battens' tops have been below the surface and both joints are intact" with "then the battens' tops go below the surface at a tick when both joints are intact". A floor landing could otherwise break a joint first.
- **Low, `:51`.** Replace "slides at about 31°" with "slides at atan(√(0.6 × 0.3)) = 23°".

### EL-175 Optical fibre: Ready with fixes

**First round.**
- H-1: resolved in substance by owner decision 10: a straight solid strand, with a crossing refused (`:31-32`). Y1 is the clear-run row and Y3 the refusal row, covering edit and Run compile (`:110`, `:112`). The Wall-bypass row is deleted (`:128`).
- M-8 (count 1, length 8 m, checked at edit and Run; Y7), L-7 and C-2 are resolved.
- The solid strand is owned by `ENGINE-CONNECTION-MATERIALS` (owner decision 1).

**Re-derivations.**
- 10 W × 0.81 = 8.1 W. Amber (10, 7.8, 3.2) becomes (8.1, 6.32, 2.59); 2.59/2.5 = 1.037. The laser draws 0.083 J per tick.
- **The fixture gives a diagonal, not a vertical cable.** A's port is at (5.25, 0, 0) and B's at (4.75, 4, 0). The 4.03 m run passes through B's own foot and box and A's own box. Own colliders are exempt, but the solid art is visibly pierced. 7.22 m of range remains; Y4 passes with 0.28 m to spare.
- Y3's boundary can pass and can fail.
- **Y8 cannot pass.** A sphere on a horizontal 0.03 m rod is an unstable equilibrium, so "comes to rest on it" fails.
- The ±2 % and 1 s tolerances are used; 0.5° and ±0.02 m are not exercised. The store-credit form is not needed. Anchors `element-175` and `todo-307` are covered.

**Findings.**
- **Medium, `EL-175-optical-fibre.md:106` (fixture).** Replace with: "coupler A's lens 5 m from the laser (A's centre at (5.25, 0, 0), turned to face it), coupler B at (5.75, 0, 4) with its lens facing +X, so the two link ports are 4 m apart on one horizontal line x = 5.5 clear of both feet; a broadband detector 3 m in front of B's lens". In Y1, "4 m above the laser's line" becomes "4 m beside it".
- **Medium, `:117` (Y8).** Replace "it comes to rest on it" with "it does not pass through: its centre never comes closer to the strand axis than its radius + 0.03 − 0.005 m". Replace "it is stopped at the strand" with "it does not pass the strand".
- **Low, `:29`:** the 0.6 m foot overhangs the port face by 0.05 m. Use a foot of 0.5 × 0.1 × 0.5.
- **Low, `:53`, `:152`:** "bindings 3" should be 1 (`:35`).
- **Low, `:110`:** add "the segment leaving B is along B's +X within 0.5°" (decision 21's direction tolerance).

### EL-076 Programmable ball: Ready with fixes (Low only)

**First round.**
- E3 (two owners for Choice values): resolved. `:114` makes `ENGINE-PARAMETER-BOUND-FRAMES` (Choice clause, coordinator decision 16) the only owner; `:178` records `ENGINE-CHOICE-BOUND-VALUES` as withdrawn.
- E4 (`Visible` by Choice): resolved (`:77`).
- M6 (band): resolved by the r/32 rule at `:61` and flush inlays at `:81-88`.
- M9 (`:55`, `:113`) and H0 (`:17`, `:163`) are resolved.

**Re-derivations.**
- Band tubes 0.0078, 0.0106 and 0.0141 m, all ≥ 1/512.
- Widths across the band: 0.516, 0.701 and 0.928 m.
- Pipe bore 1.3 m: all three sizes pass. Cannon window 0.6–0.84 m (`CannonElementDeclaration.cs:32`): only Medium seats. A4 can pass and can fail.
- Bounce e² against the bench (e 1): 0.608, 0.303 and 0.020. Drag costs about 1 % each way, giving about 0.59, 0.29 and 0.02, inside the non-overlapping bands.
- Combinations 27 − 6 = 21.
- Inertia 0.04624 kg·m².

**Ownership.** `ENGINE-PARAMETER-COMBINATIONS` is a proposed story (`readiness/batch-l.md:102`). It is not on the 3a list and has no `epics.md` heading.

**Findings.**
- **Low, `EL-076-programmable-ball.md:96`.** Replace with "drag removes about 1 % of speed each way on a 2.7 m fall (k·t/3 with t = 0.74 s), giving about 0.59, 0.29 and 0.02".
- **Low, `:145`.** Replace "band-upright" with "(released with the band's axis vertical, so the band lies at the equator)".
- **Low, `:142`.** Replace "compared exactly" with "radius, mass, restitution and rolling resistance compared exactly (authored constants); moments equal to the f32 evaluation of 0.4·m·r² from those operands".

### EL-078 Can opener + companion `sealed_can`: Ready with fixes (Low only)

**First round.**
- H8 (second cutting law): resolved. One blade-pair law, the cutting clause of `ENGINE-TENSILE-EXTENSIONS` extended to a seam target (`:21`, `:110-111`). The friction work limit on `ENGINE-FRACTURE` is withdrawn (`:169`).
- Story names: the fixed joint is `ENGINE-JOINT-KINDS`'s (`:109`).
- Companion package under decision 15 (`:14`, `:168`).
- The drain rows A9 and A10 are owned by `ENGINE-SUPPLY-DEMAND` (`:112`).

**Re-derivations.**
- Open gap 0.18·tan 0.55 = 0.110 m, more than the 0.055 m tab. Engagement at 0.271 rad.
- Cut force 12/0.05 = 240 N, which is 43.2 N·m at 0.18 m. Power 5.4 W (0.045 J per tick); at the 54 N·m bound, 6.75 W. All far inside 120 W and 1 J per tick.
- Drive work 12.39 J, so A9 can fail.
- A5: 14 J needs 51.1 N·m (under 54); 16 J needs 57.6 (over). Both can pass and fail.
- But the drive's real severing limit, after bearing friction, is (54 − 1.0 + 0.26)/0.18 × 0.05 = 14.8 J. A seam of 14.8–15 J would cut by the law while the drive stalls.
- A2: the blade holds unsupplied (0.26 N·m against 1.0 N·m of bearing friction).
- A6: the Balloon lifts 0.845 N against the 0.589 N lid.

**Findings.**
- **Low, `EL-078-can-opener.md:21`, `:110`:** the cite `readiness/batch-i.md:90` is stale. Use `readiness/batch-i.md:96`.
- **Low, `:42`, `:90`: drive headroom.** At `:42` use "maximum torque 56 N·m, both fixed". End `:90` with "The torque bound covers the capacity plus the bearing: 15 J / 0.05 m × 0.18 m + 1.0 N·m = 55 N·m ≤ 56 N·m."
- **Low, `:143`:** the Battery's minimum capacity is 60 J (`BatteryElementDeclaration.cs:42`). Use "a 60 J Battery at `initial_charge` 1/12 (5 J)".
- **Low, `:35`.** Replace "centre 0.105 m out" with "centre (0.105, 0.015, 0) in the blade frame, so its lower face passes through the hinge axis".

### EL-079 Electric mixer: Ready with fixes (Low only)

**First round.**
- H6 (spindle below the minimum): resolved; it is now a box 0.08 × 0.75 × 0.08 (`:38`, `:42`).
- H7 (dead corners): resolved with a hexagon.
- Slow-boundary law: resolved; A5 is ordering only and A6 is a force argument (`:103`, `:147-148`).
- The drain rows A10 and A11 are owned by `ENGINE-SUPPLY-DEMAND`.

**Re-derivations.**
- Hexagon outer side 1.097 m. Paddle I = 0.0817 kg·m².
- Corner reach (Tennis, Bowling, Basketball): 0.4425, 0.378, 0.248 m, all under the 0.70 m tip.
- Force needed to push each ball over the step: 4.58, 46.8, 9.84 N. Outward push at torque 8: 10.0, 11.5, 15.9 N. A1 and A7 can pass and fail.
- At torque 2: push ≤ 2.5 N, under 4.6 N, so A6 can fail.
- Unpowered: restitution 0 × anything is 0, so A2 holds.
- Power: 48 W default (0.4 J per tick). At the maximum, 12 × 10 = 120 W, exactly 1 J per tick, which is allowed.

**Findings.**
- **Low, `EL-079-electric-mixer.md:153`.** Replace "less than the work A1 needs" with "a 60 J Battery at `initial_charge` 0.02 (1.2 J)".
- **Low, `:146`, `:171`.** State "a Wall 0.4 × 0.4 × 0.12 m resting on the bowl floor across radius 0.2–0.6 m", and add "placement of a part inside the bowl is admitted" to the Disqualifiers (placement refusal `IntoPart`, `engine/PlacementPose.cs:16`).

### EL-080 Programmable box: Ready with fixes

**First round.**
- As EL-076: E3 resolved (`:121`, `:184`); E4 resolved (`:83`, `:125`); flush inlays (`:87-94`); H0 resolved (`:17`, `:169`).

**Re-derivations.**
- Centre of mass at 0.3127 of the size. Moments 0.504 and 0.694 kg·m² confirmed by parallel axis.
- Slope: √(0.3·0.3) = 0.30 gives 16.7°; Wood 23.0°; Rubber 27.5°. Metal at 20° accelerates at 0.59 m/s².
- Tip angle 57.9°.
- A4 can pass and can fail.

**Findings.**
- **Medium, `EL-080-programmable-box.md:104`, `:149`: the shove prediction ignores the ball's follow-through.**
  - After impact the ball keeps 1.30 m/s against a Light box and 0.19 m/s against a Heavy one. Its retained spin restores rolling at 1.50 and 0.71 m/s, and it strikes again.
  - Replace `:149`'s tolerance cell with "predicted first-impact box speeds 1.41 and 0.30 m/s; the rolling ball strikes again, so distances exceed 0.24 m and 0.011 m and are bounded only through friction work ∝ box mass; ball–wall overlap at most 2 cm".
  - At `:104`, replace "it stops after … 0.011 m" with "…the ball, still rolling, strikes again; the friction work the box can absorb per metre is 2.1, 8.3 and 25 N, so a Light box goes furthest".
- **Medium, `:102`: the fit list is wrong.** The Large Programmable ball (0.928 m) fits the Medium box (0.96 m inside). Replace with "Medium (0.96 m) takes all of them, the Large Programmable ball with 16 mm a side; Large (1.28 m) takes all with room". The Basketball is 0.70 m across its band, not 0.68.

### EL-084 Fragile fish bowl: Ready with fixes

**First round.**
- H5, all parts resolved:
  - Break speeds 2.76, 3.43 and 5.4 m/s (`:67`).
  - The observer is a collider set, owned by `ENGINE-CONTACT-OBSERVATIONS` (`:40`, `:80`).
  - The breaking substep is re-solved with shards (coordinator decision 16; `:41`, `:93`, `:141`).
  - Shards sit inside the wall's volume (`:34`, `:36`).
- The bowl is dry (owner decision 11; `:21`, `:59`, `:87`).

**Re-derivations.**
- Mass 0.41 + 0.19 kg; centre of mass at 0.170 m; moments match.
- Shard corner at 0.3496 m, inside 0.35.
- F2: 0.78·v gives a threshold of 1.92 m/s, i.e. a 0.19 m fall. 1.09 and 2.18 N·s, so F2 can pass and can fail.
- F3: an off-centre hit (0.11 m above the centre of mass) gives an effective mass of 0.508 kg, so 0.470·v; 3.5 m/s gives 1.64 N·s, still a break with a 9 % margin.
- **F7's control cannot pass.** (4 − 0.042·0.6)/4.6 × 1.5 = 1.30 m/s forward after the hit. I confirmed this against `:31` and `:112`.
- **F1 depends on landing attitude.** A rim-first landing has an effective mass of 0.21 kg, giving 0.94 N·s, under 1.5.

**Findings.**
- **High, `EL-084-fragile-fish-bowl.md:112` (F7 control).** Replace the control cell with "at 1.5 m/s the bowl stays whole and is pushed ahead: the ball's centre stays at least 0.6 m behind the bowl's axis (0.28 + 0.35 less slop) until both stop".
- **Medium, `:106`, `:66` (F1).** Replace with "a Wall shelf whose top is 3.5 m above the bench … breaks on landing at any attitude: landing 8.29 m/s; worst-case rim-first effective mass 0.153 kg gives 0.153 × 1.3 × 8.29 = 1.65 N·s > 1.5", and add the attitude caveat at `:66`.
- **Low, `:67`.** Add "a ball meets the wall at its own centre height; for the Bowling ball (0.11 m above the bowl's centre of mass) the bowl's effective mass is 0.508 kg and the break speed 3.19 m/s; F3's 3.5 m/s keeps a 9 % margin".
- **Low, `:82`.** The fish is a free body present from Run start, not a fracture fragment, yet its admission is filed under `ENGINE-FRACTURE`. State whether it is a second body of the main package or a companion package (decision 15), with exactly one owner.

### EL-086 Obstacle-reversing walker: Ready with fixes

**First round.**
- H0: resolved (`:17`, `:160`, `:161`, `:114`, W12 and W13 at `:148-149`).
- H2: resolved in substance by redesign. Two ordinary drives (`:41`); sprung bumpers whose slider end-of-stroke contacts feed an edge node and a memory node (`:48`); no contact-speed sensor and no dynamic-owner contact observation needed (`:117`).
- H3: resolved, √(0.8·0.3) = 0.49 (`:90`).
- M7 (flush key, `:84`) and L10 are resolved.

**Re-derivations with the real friction combination.**
- **Traction.** Drive (0.18 − 0.03)/0.08 = 1.875 N; net 1.5 N. Load transfer 1.5·0.24/0.40 = 0.9 N, so 5.8 N on the driven axle. Limit 0.49·5.8 = 2.84 N > 1.875 N, so no slip in either direction. I confirmed this from `:90`.
- With roller inertia the effective mass is 1.15 kg, so acceleration is 1.30 m/s².
- **Stalled at a Wall.** N_r = 6.23 N, limit 3.05 N against 2.25 N demanded: the roller does not slip.
- **Power.** Peak 1.8 W against the toy's own 10 W cell limit (0.083 J per tick).
- **W13.** 3 J lasts about 4.2 s, inside the 12 s guard.
- **Standing Domino.** μ = √(0.6·0.3) = 0.42. The Domino slides at 1.66 N and topples at 1.1 N or more, but the bumper bottoms at 0.5 N first, so the toy reverses.
- **W7 cannot pass.** It shuttles on legs of about 0.06 m with about a dozen sign changes in 5 s. I confirmed this from `:35`, `:39` and `:143`.
- **Trailing bumper.** With 0.05 kg, 25 N/m and 1 N·s/m, ω_n = 22.4/s and ζ = 0.447. At 0.8 m/s, x_max = 20.6 mm against the 20 mm stroke, so the memory resets about 7 ticks after it was set.

**Findings.**
- **High, `EL-086-obstacle-reversing-walker.md:143` (W7), `:95`, `:174`.**
  - Replace W7 with: "Given Walls 0.74 m apart with the toy between them, then it shuttles inside the gap: every velocity sign change happens with one bumper within 0.5 mm of its stop, and the chassis never leaves the gap | `BetweenCloseWallsItShuttles` | W1 | 5 s".
  - Replace item 6 with: "Between two obstacles closer than its travel it shuttles, reversing at each bumper press; both bumpers can be in together only if the obstacles are closer than 0.68 m, which a placed Wall cannot be."
- **Medium, `:39`, `:92`: trailing bumper.**
  - Set "damping fixed 4 N·s/m" (ζ 1.79; x_max 8.6 mm).
  - Add to item 3: "the arrival stop: the trailing bumper's peak inward travel is 8.6 mm at 0.8 m/s (ζ 1.79)".
  - Add the row "W14 Given W1's first arrival, then the memory changes exactly once and the trailing bumper's travel stays under 0.015 m | `OneArrivalOneReversal`".
- **Low, `:17`, `:111`, `:160`:** give the key: "`ENGINE-CHARACTERS` (retitled 'package joints on a dynamic base', coordinator decision 16)".
- **Low, `:90`:** add "effective mass 1.15 kg: 1.30 m/s², 0.8 m/s in 0.62 s".
- **Low, `:153`:** the Domino collaborator has no row. Add "W15 Given a standing Domino 2 m ahead, then the toy reverses at it".

### EL-088 Fish tank lure: Ready with fixes

**First round.**
- H5, all parts resolved:
  - Threshold 16 N·s (`:46`).
  - Restitution product (`:79`, `:84`).
  - One observer per pane (`:46`).
  - The breaking substep is re-solved (`:47`).
  - Each fragment is exactly the collider it replaces (`:35`).
- M4 resolved: the fish transmits (`:32`), ordinary colours (`:42`), reach is the emitter's range (`:39`).
- Owner decision 11 (dry) is applied (`:161`).

**Re-derivations.**
- Stimulus: I = 0.637 W/sr. The reading is 5.6e-4 W at 3 m (14× the 40 µW threshold) and 7.8e-5 W at 8 m (2×), so reach governs.
- Bowling ball: 4.17·v gives 12.5 N·s at 3.0 m/s (holds) and 20.0 at 4.8 (breaks). Basketball: 3.5 and 9.3 N·s, both hold.
- **T6/T7 fail.** The interior between the long panes is z ±0.26 (`:31`, `:35`), 0.52 m against the ball's 0.56 m. After the end pane breaks, the ball meets both long panes' end edges with normals having an x component of 0.371. The impulse is 25.3 N·s per edge, above 16, so both long panes break. I confirmed the widths.

**Findings.**
- **High, `EL-088-fish-tank-lure.md:130`, `:131`, `:85`.**
  - Replace T6 with: "Given a Bowling ball rolled along −Z into the centre of the front long pane at 3.0 m/s, then the pane holds; at 4.8 m/s that pane becomes a free body moving away from the ball, one `IntegrityBroken` record names it, and the floor and both end panes stay | … | 12.5 and 20.0 N·s against a strict 16 N·s; the fragment moves faster than 1 m/s within 0.1 s".
  - In T7, replace "the line where the pane stood" with "the front pane's old line (z = 0.26 m)".
  - Rewrite behaviour 5 for the 0.5 kg fragment: 4.45 m/s, the ball keeps 4.24 m/s, and the back pane then takes 17.7 N·s and breaks.
- **Low, `:82`:** no optical colour receiver is delivered. Replace "A delivered colour receiver" with "A colour receiver of the optical catalogue (CAT-038 family, planned)".

### EL-099 Pool pocket: Ready with fixes (Low only)

**First round.**
- H9 resolved: opening 0.46 m (`:37-46`); centre-in-volume rule under `ENGINE-SENSOR-VOLUMES` (`:57`); `capture_margin` removed (`:61`); Pool ball control in A4 (`:145`).
- L10 resolved (`:69-73`).
- Owner decision 11 (flat stripe, no band collider; 0.03 m clearance a side) and owner decision 12 (goal slices incl. `engine-goal-count-window-hold`) are applied.

**Re-derivations.**
- Rim dips r − √(r² − 0.23²): 0.152, 0.141, 0.139 and 0.090 m. No ball wider than 0.46 m passes, so A4 can pass and can fail.
- Impact 3.43 m/s; rebound rises 5 mm. A1 can pass and can fail.
- A5: the second ball's bottom sits 4.5 mm inside the volume. That is harmless under the centre rule, but `:63` and `:179` overclaim.

**Findings.**
- **Low, `EL-099-pool-pocket.md:63`, `:179`.** Replace the "agree for every ball" claims with "both readings agree for every single ball; a second Pool ball stacked on the first (A5) overlaps the volume by up to 5 mm with its centre 0.2 m above it, so only the declared centre rule keeps it out".

### EL-101 Thumb tack: Ready with fixes

**First round.**
- H6 resolved: a pin box 0.06 × 0.30 × 0.06 and a tip sphere r 0.03125 = 1/32 m, exactly at the minimum (`:28-29`).
- E6 resolved: the stress concentration is a clause of `ENGINE-CONTACT-OBSERVATIONS` (`:82`), an extension of the one integrity measure, not a second cutting law.
- H0 resolved (`:17`, `:128`).

**Re-derivations.**
- Pair restitution 0.2 × 0.3 = 0.06.
- A1: 0.5 × 1.30 × 1.06 × 16 = 11.0 N·s, above 3, so it breaks.
- A4: 1.56 N·s holds.
- A2: 2.23 N·s against 3 at factor 1, so it holds.
- **A5 cannot fail.** No delivered ball declares an integrity observer.

**Findings.**
- **Medium, `EL-101-thumb-tack.md:112`.** Add: "A5b Given an integrity fixture declaring the Balloon's observer with threshold 12 N·s, released 0.5 m below the tip as in A1, then it stays intact (16 × 0.689 = 11.0 N·s < 12); at threshold 10 N·s it breaks | `APunctureResistantTargetHolds` (theory)".
- **Low, `:57`, `:61-63`.** Replace "between m·v and 1.3·m·v" with "1.06·m·v (pair restitution 0.2 × 0.3 = 0.06, the product law)". Measures become 11.0, 1.56 and 2.23 N·s; the threshold speed is 0.354 m/s.

### EL-105 Toy missile: Not ready

**First round.**
- H1 resolved in substance: nose jets 0.30 m ahead of the centre of mass (`:57-60`); ambient angular drag on `ENGINE-REACTION-THRUST`, coordinator decision 16's thrust story, with its text at `EL-090:151`; "×" fins (`:48`); one steering plane (`:109`).
- H4 resolved (`:100`). M9 resolved (`:114`). H0 resolved (`:19`, `:177`).

**Re-derivations.**
- Cruise: 4 − 0.25v = 0.42v gives v = 5.97 m/s, τ 0.448 s.
- Sink 0.063/0.42 = 0.15 m/s, matching `:102`. Over a 1.43 s flight it sinks about 0.12 m (the spec says about 0.2).
- Thrust-to-weight 1.36. A 6 m flight costs 67.5 J; the 90 J cell lasts 1.98 s and about 9.2 m.
- Peak power 66 W, under its own 80 W limit and 0.67 J per tick.
- Turning: 9.1 rad/s² of turn acceleration settles at 1.05 rad/s, far under 128 rad/s.
- 2-D model: A1 closest approach 0.09 m; A4 0.006 m; A2 unlit 2.12 m. All can pass and can fail.
- **A5 fails near the end.** The error exceeds 6° at t = 1.425 s, with 0.41 m left, because the line-of-sight rate rises as range closes.
- Fin corners sit 0.087 m below the axis, so the dart rests about 0.5° nose-down.

**Findings.**
- **High, `EL-105-toy-missile.md:63`: the eyes are buried in the Nose.**
  - The apertures at (0.44, ±0.03, 0) are 0.05 m from the Nose sphere's centre (0.40, 0, 0), inside its 0.08 m radius. A receiver is a node on a collider face (EL-176 convention 8, `EL-176:178`), and an undeclared face absorbs (convention 6). I confirmed this from `:44` and `:63`.
  - Replace with: "two receivers declared on the Nose collider's face (ENGINE-OPTICAL convention 8): disc apertures of radius 0.02 m centred at (0.4706, ±0.0376, 0), the sphere's surface 28° above and below +X, normal along that surface normal".
- **High, `:63`, `:132`: the acceptance cone is outside the convention.** The 25° acceptance half-angle that creates the 6° dead band is not in EL-176 convention 8 (`EL-176:178`), which has only the facing term. Without it each eye sees a hemisphere and the dead band vanishes.
  - Either the optical convention owner adopts "a sample outside the declared cone about the aperture normal reads zero" as part of ENGINE-OPTICAL's receiver, or the steering is redesigned.
  - Raise this against EL-176 §10 item 8. This is the decision that makes the spec Not ready.
- **Medium, `:19`, `:63`, `:132`, `:177`: threshold owner and values.** The "story's default threshold" contradicts convention 9 (`EL-176:179`), where the lit level is a threshold node with a release ratio owned by ENGINE-SIGNAL-NODES.
  - Add "the lit level is a threshold node over the reading, with a release ratio (ENGINE-SIGNAL-NODES, owner decision 3a)".
  - State the aperture area, the threshold, and A1's beacon power and half-angle, with the reading margin at 6.3 m.
- **Medium, `:158` (A5).** Replace "and at every later sample until closest approach" with "and at every later sample until the nose is within 1 m of the beacon".
- **Low, `:48`.** Replace "0.080 m below the axis … lies level (pitch 0°)" with "0.087 m below the axis (fin half-thickness included); it rests about 0.5° nose-down on its nose and two fin corners".
- **Low, `:163` (A10).** Replace "is not propelled and falls" with "is not propelled; it stays on the ramp".

## 3. Cross-spec contradictions

1. **Liquid fluid-port owner.** The Batch F specs (EL-007, EL-017, EL-021) never name `ENGINE-GAS-NETWORK` as owner of the one fluid port record (coordinator decision 16, L5). The Batch G specs do.
2. **Tap law.** EL-025 (`:59`, `:121`) uses a two-aperture tap; EL-003 (`:54`) gives three. EL-017 and EL-021 agree with EL-003.
3. **Cork friction.** EL-169 (`:49`) and EL-170 (`:51`) take cork's μ 0.6 as the slip coefficient. The engine combines it with the Weight's 0.3 to give 0.42 (23°).
4. **Averaging windows.** Floating rows are inconsistent. B1, O1, H1 and V1 average over the last 2 s; B2, O2, V2 and V5's control do not, or leave the window unstated. With L12's damping (ζ ≈ 0.006–0.02 for these light hulls) every un-averaged draught row is fragile.
5. **The Large Programmable ball in a Medium box.** EL-076 (`:156`) and EL-080 (`:102`) disagree on whether it fits (0.928 m against 0.96 m). EL-080 is wrong.
6. **Optical receiver.** EL-105's acceptance cone and default threshold contradict EL-176 conventions 8 and 9. EL-088 (`:82`) calls a colour receiver delivered; none is.
7. **Proposed story names outside 3a.**
   - `ENGINE-REACTION-THRUST`: text only in `EL-090` §3. Used by EL-090, 092, 100, 104, 105.
   - `ENGINE-PARAMETER-COMBINATIONS`: text only in `readiness/batch-l.md:102`. Used by EL-076, 080.

   Neither has a `### Story` heading in `epics.md` (HEAD or working tree). Both are genuinely new capabilities, not renames, so they are not 3a violations, but they need an epics entry before delivery.
8. **Companion bodies present from Run start.** The EL-084 fish and the EL-088 fish are filed under `ENGINE-FRACTURE`, which owns fragments that appear on a break. One owner for a free companion body present from Run start should be stated (decision 15 / S7).
9. **Batch I contract cite.** EL-078 and EL-079 quote `readiness/batch-i.md:90`; the clause is now at `:96-100`.
10. **`epics.md` is mid-rewrite in the working tree** (+476/−102 lines, uncommitted). It now carries the 3a stories (from `:4447`), but has no `### Story` heading for ENGINE-OPTICAL, ENGINE-FRACTURE or ENGINE-FIELD-FORCE, which HEAD has at `:4434`, `:4467` and `:4445`. Spec citations of `epics.md:44xx` line numbers resolve only against HEAD.
11. **Shared walker chassis.** The EL-086 bumper finding (trailing bumper bottoming on arrival) probably carries over to EL-082, 083 and 087, which share the chassis. Not checked.

**Convention flag.** EL-001 L9 says pending overflow leaves "with zero start speed", while L8 adds the owning body's velocity to a free end's stream. For a moving store (EL-025's swinging chamber, rim speed about 1.6 m/s) the two rules disagree. EL-025's pours land in the basin either way, but the convention owner should pick one. No other convention used by these 20 specs looked wrong.

## 4. What I could not verify

- **Collision timing and stops.** Whether a collision impulse arrives within one substep (the fracture and puncture thresholds in EL-084, EL-088 and EL-101), and the solver's stop softness and rebound (which affect EL-086's 20.6 mm).
- **Large-angle righting tables** (EL-028 `:55`, EL-169 `:49`, EL-170 `:51`) beyond spot checks at 2° and 10°.
- **EL-105 guidance** beyond the 2-D point model: no contacts, and the eye treated as a body point.
- **Mechanical couplings.** Whether a delivered rigid mechanical connection couples rims on non-parallel axes (a Motor driving EL-023's rotor tilted 30°), and the Fan's actual force for EL-029 V7.
- **Placement and poses.** Whether a part can be placed inside the static mixer bowl (EL-079), and whether a 20° Ramp pose is admissible with a 1.2 m box (EL-080 A5).
- **EL-025 first pulse.** The funnel's holdup effect on its timing (EL-005's frustum level relation was not recomputed).
- **Batch O G5** wording of the Choice clause.
- **Delivered-package engine search** (conformance "For auditors"): not repeated. None of these 20 is a delivered package.
