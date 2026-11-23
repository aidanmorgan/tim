# Second audit, group 1: 27 formerly Not-ready element specs (23 November 2026)

Independent, read-only re-audit in checkout `spec-drafts` (HEAD `8d1d68aa` plus the uncommitted spec repairs and propagation pass). Nothing was built, run, edited, committed or stashed. Every figure below is a hand derivation; none is a measurement.

**Shorthand.** `EL-051:28` means `docs/planning/elements/EL-051-acoustic-dish.md` line 28, and likewise for every spec id. `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`. "Coord N" = coordinator decision N, 22 November 2026; "owner N" = owner decision N.

**Authorities read.** `readiness/owner-decisions-2026-11-22.md`, `readiness/engine-conformance.md`, `.tools/audit/engine-design-gap.md`, the first-round reports (`elements-batch-h.md`, `-i.md`, `-j.md`, `-d.md`, `-e.md`, `elements-new-identities.md`), the readiness summaries for those batches, and the conventions the specs cite: EL-001 (liquid), EL-039 (pneumatic), EL-044 (acoustic), EL-176 §10 (optical). No spec in this group depends on the TH-04 or RAD-08 conventions.

**Engine facts used for every derivation.**
- Material combination: restitution is the product, friction is √(fA·fB), and the bounce threshold is the maximum (`SIM/WorkshopConstraintSolver.cs:115-117`).
- Admitted shape minima (`ABS/PhysicsDeclarationValues.cs:221-270`):
  - sphere r ≥ 1/32 m;
  - cylinder, tube and frustum r ≥ 1/16 m;
  - box half-extent ≥ 1/1024 m;
  - tube bore ≥ 1/64 m;
  - hollow wall ≥ 1/1024 m;
  - torus tube ≥ 1/512 m and torus centreline ≥ 1/16 m.
- Battery `power_limit`: default 120 W, range 10–480 W (`elements/Battery/BatteryElementDeclaration.cs:44`). A supply phase grants at most `power_limit` ÷ 120 per tick, so 1 J at the default.
- Run length: 30 s default (`CadenceValues.cs` `RunTickLimit`), 300 s maximum (owner 17).
- Retention request: Empty only within `ActuatorStopSlopMetres` of travel 0 (`SIM/SimPrismaticRetention.cs:44-46`).
- Visible-solid matcher: a solid art node must be a box, full sphere or straight cylinder, matched one to one (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:221`); any collider left over fails at `:231`.

## 1. Verdicts

| Spec | Verdict | Conformance (points 1–7) | First-round blockers | Open High findings |
|---|---|---|---|---|
| EL-038 Pneumatic hose | Ready with fixes | with fixes (3) | all resolved | H-A12 cannot pass |
| EL-043 Pneumatic pressure gauge | Ready with fixes | with fixes (2: unowned fallback) | all resolved | — |
| EL-045 Air whistle | Ready with fixes | with fixes (2) | all resolved | — |
| EL-051 Acoustic dish | Ready with fixes | Conforms | all resolved | post below the cylinder minimum |
| EL-052 Water-tuned bottle | Ready with fixes | Conforms | all resolved | — |
| EL-180 Pulse speaker | Ready with fixes | Conforms | all resolved | second declaration text for one package |
| EL-183 Resettable counter | Ready with fixes | Conforms | all resolved | — |
| EL-198 Switch | Ready with fixes | with fixes (2) | all resolved | — |
| EL-211 Solar panel | Ready with fixes | Conforms | all resolved | (inherits CAT-059 P5) |
| EL-061 Indexed carousel | Ready with fixes | Conforms | all resolved | — |
| EL-066 Scissors | Ready with fixes (Low only) | Conforms | all resolved | — |
| EL-067 Steel cable | Ready with fixes (Low only) | Conforms | all resolved | — |
| EL-068 Tin snips | Ready with fixes | Conforms | all resolved | — |
| EL-072 Curved metal ball pipe | Ready with fixes | Conforms | F2 resolved against owner 15 (Medium); rest resolved | — |
| EL-109 Speed trapdoor | Ready with fixes | with fixes (2) | all resolved | coord 25 not applied |
| EL-158 Rise-hold-fall cam | Ready with fixes | with fixes (3) | all resolved | cam art fails the matcher |
| EL-186 Temporary bridge | Ready with fixes | with fixes (forward-only text) | all resolved | — |
| EL-200 Drive belt | Ready with fixes | with fixes (forward-only text) | all resolved | — |
| EL-201 Drive chain | Ready with fixes | with fixes (forward-only text) | all resolved | K1 needs 2 kW |
| EL-216 Capture cradle | Ready with fixes | with fixes (forward-only text) | all resolved | — |
| EL-225 Latched slingshot cradle | Ready with fixes | Conforms | all resolved | S2 cannot pass; S5 geometry |
| EL-227 Magnetic impulse relay | Ready with fixes | Conforms | all resolved | I7 cannot pass |
| EL-235 Jack-in-the-box | Ready with fixes | Conforms | all resolved | J2 cannot pass |
| CAT-010 Bellows | Ready with fixes | with fixes (2) | all resolved | driver owner not named; spring preload marked present |
| CAT-061 Speaker | Ready with fixes | Conforms | all resolved | row 3 control cannot pass |
| CAT-059 Solar panel | Ready with fixes | Conforms | all resolved | P5/P6 fixture cannot run or fail |
| CAT-008 Beam splitter | Ready with fixes | Conforms | all resolved | — |

**Counts:** Ready 0, Ready with fixes 27, Not ready 0.

Every first-round finding that made a spec Not ready is fixed in substance, not just reworded. No spec designs around an engine gap with element logic.

The fixes that remain are of four kinds:
- acceptance rows that cannot pass on a correct engine, or cannot fail on a wrong one;
- three binding decisions not applied: coord 25 in EL-109 and EL-200, and owner 15 in EL-072;
- old harvest-era text left beside the current declaration in five Batch J specs;
- small geometry against the admitted minima or the visible-solid matcher.

None of these needs an owner decision. One needs a coordinator answer: the over-allowance rule in `ENGINE-SUPPLY-DEMAND` (X-1). CAT-010's presentation driver gets a named owner from the evidence (CAT-010 finding 1).

### Mechanical checks (all 27)

- "Blocked on the engine programme (owner decision 3a)" is present in every Status line and every Delivery or Prerequisites section.
- "Non-solid tether" appears nowhere. EL-038, EL-067, EL-200 and EL-201 declare solid strands under `ENGINE-CONNECTION-MATERIALS`.
- No row runs longer than 30 s. EL-061 C8 and EL-235 use a 30 s guard, which is exactly the default, so no Run length needs stating.
- "Collimated" survives only as a segment type (CAT-008:50, :53; CAT-059:27, :63, :67, :90, :169; EL-211:67), which owner 9 allows. EL-072:126 uses it as a beam description; fix below.
- Story names:
  - no retired drain name and no `ENGINE-SPAN-CUTTER`, `ENGINE-TENSILE-GUIDE` or `ENGINE-ASSEMBLY-JOINTS` G1–G3 is used as an owner;
  - `ENGINE-ASSEMBLY-JOINTS` appears only for commanded release (EL-186, EL-225, EL-109:121), which is what the gap audit's story 9 leaves it;
  - `engine-axial-gas-nozzle` appears only as a draft file name (CAT-010:7);
  - one invalid name: `ENGINE-ART-SOLIDITY` (EL-216:199).
- No spec in the group joins a gas node to a liquid node. EL-052 is excited by open airflow, not by the gas network.
- Delivered packages: no engine code names `speaker`, `bellows`, `solar_panel`, `beam_splitter` or their class names. None of the four CAT packages exists yet, so there is no forward-only engine refactor to record.

## 2. Cross-spec findings (most important first)

**X-1 [High]: `ENGINE-SUPPLY-DEMAND` does not say what happens when a drive asks for more than the source's per-tick allowance.** Four specs depend on the answer.
- EL-068 needs a cut stroke of about 66 N·m × 3 rad/s ≈ 200 W, which is 1.64 J per tick against the default 1 J.
- EL-201 K1 asks a 100 N·m, 20 rad/s Motor for up to 2 kW (EL-201:170).
- EL-200 B1–B5 use a 20 N·m × 6 rad/s Motor, which draws exactly the 120 W default.
- CAT-059 P5 and P6 depend on how much a Motor asks.

The story's contract (gap audit §6 story 7) covers an exhausted source but not throttling. Add to the story text: "A drive whose demand in a tick exceeds the source's allowance (`power_limit` × phase) receives the allowance and its effort is limited to it for that tick; the route stays closed; the drive's committed work never exceeds the energy granted." The four specs then state their Battery `power_limit` (fixes below). If the coordinator instead chooses "stop", EL-068 T1 and EL-201 K1 must be re-derived, not widened.

**X-2 [High]: an Empty retention request from a gravity-sagged slider cannot happen under the delivered law.**
- `SimPrismaticRetention.cs:44-46` answers Empty only within `ActuatorStopSlopMetres` of travel 0.
- A tilted or upright sprung slider rests sagged:
  - EL-225: 0.061 m with a ball seated, 0.02 m empty;
  - EL-235: 0.04 m under the head and lid.
- So EL-225 S2 (`:105`) and EL-235 J2 (`:107`) cannot pass. The delivered Empty test uses an inverted spring for this reason (`WoundSpringControlTests.cs:293`).
- Fixes are per spec below. Other specs that assert Empty on a tilted or upright spring-returned slider were not opened; CAT-071, EL-224 and EL-226 should be checked by the same arithmetic.

**X-3 [High]: one package, two declaration texts (Speaker).**
- EL-180 §2 (`:22-74`) calls itself "the declaration the `speaker` package has", but it differs from CAT-061 §2:
  - EL-180 has a `Ready` lamp on `CommittedStoredFraction`;
  - EL-180 uses a damped diaphragm oscillation where CAT-061 uses one eased 0.07 m push;
  - EL-180 counts 8 bindings, CAT-061 counts 4.
- Coord 16 makes CAT-061 the single authority. Fix: CAT-061 adopts the lamp, and EL-180 §2 becomes a pointer (fixes below). EL-180 then has no remaining delta and is a Duplicate closure.

**X-4 [Medium]: "`PowerIn` is granted on that tick" is unsatisfiable for a full store** (CAT-061:28, :70, :126; EL-180:32, :116; inherited from EL-044 gap 4 at `EL-044:162`). A full store demands nothing, so its grant is zero, and CAT-061 row 1 would never emit. Replace with "`PowerIn` is powered on that tick (the supply route's powered predicate, as the System clock's oscillator reads it, `engine/gpu/ActivationNetwork.cs:445`)" in all three specs.

**X-5 [Medium]: harvest-era text is kept beside the current declaration** in EL-158 (`:60-103`, `:151-157`), EL-186 (`:60-103`, `:155-161`), EL-200 (`:77-115`, `:174`), EL-201 (`:66-103`, `:156-161`) and EL-216 (`:63-105`, `:154-159`).
- In each case it contradicts the current section: parameters that no longer exist, a "No collider" strand, welds, fan-out, an `outlet` Choice, and Chrome recipes for removed parameters.
- `engine-conformance.md:3` says "the older text is rewritten, not kept beside it". A fresh implementer could write tests for removed parameters.
- Delete each range (exact ranges per spec below).

**X-6 [Medium]: decisions published on 23 November but not applied.**
- EL-109:121 still calls the hatch spring rest position "a coordinator question"; coord 25 names EL-109's hatch.
- EL-200:56 still asks whether the rigid link's drawing must become solid; coord 25 answers no.
- EL-072:137 proposes a new ledger identity for the 45° bend; owner 15 provides the companion-package route instead.

**X-7 [Medium]: convention defects.** I take the conventions as they stand. The EL-039 and EL-176 items are for the convention owners' re-auditor. The two EL-044 items (N6 and gap 4) are outside both audits: EL-044 is not one of the five convention owners being re-audited. They must be edited in the same pass as the EL-045, EL-180 and CAT-061 fixes, or X-4 and EL-045 finding 1 return.
- **EL-039 P14 (`EL-039:115`), 200 K lower temperature limit.**
  - Adiabatic venting of a default tank reaches 288 × (101 325 / 400 000)^(0.4/1.4) = 194.6 K, and from 600 kPa it reaches 173 K.
  - So EL-038 H-A4, EL-043 P-A2 and EL-045 rows 4 and 7 leave the declared material range, and clamping would break energy conservation.
  - Recommend 150 K. EL-045's rows are stated in kg/s, so they still stand.
- **EL-039 F4 vent list** and `readiness/batch-h.md:94` ("vents … on any Output"): EL-045 vents through its `GasIn` Input. Say "any Fluid port".
- **EL-044 N6** lacks the receiver's own-collider exclusion. EL-045's samples sit on its own mouthpiece face (EL-045 finding 1).
- **EL-044 gap 4:** the "granted" wording (X-4).
- **EL-176 §10 item 9 (`EL-176:179`)** still says the owner of the credited-store row is open. Batch O G20, CAT-059 M10b and EL-211 row 3 all assign it to `ENGINE-THERMAL`, which lands before `ENGINE-OPTICAL` (owner 18), so the order is coherent. G20 names `ENGINE-SUPPLY-DEMAND` as the alternative; the coordinator should confirm the owner once.
- **EL-176 §10 item 5 (`:175`)** does not say whether the chord inside a crossed Transmit/Split collider uses up range. CAT-008 R7 needs the answer at ±0.02 m. Add "range is consumed by segment length only; a crossing's chord consumes none".

**X-8 [Medium]: proud sockets float off their faces**, the same defect Batch D M-5 found.
- EL-183: new `ResetIn` 0.02 m above the top face; copied sockets 0.02–0.03 m off.
- EL-043: `PowerIn` and `Supply` 0.05 m beyond the foot and bezel.
- EL-180 and CAT-061: the cabinet floats 0.01 m above the foot.
- The delivered Counter (CAT-020) has the same defect, which should be recorded against CAT-020 (outside this group).

**X-9 [Low]: the palette order** is "allocated by the cross-batch pass" in EL-038, EL-043, EL-045, EL-051, EL-052 and EL-211, but no allocation exists yet, and S8 (the lesson-fixture story) is cited and defined nowhere. A slice cannot finish its declaration until that pass runs.

**Consistent across the group (no finding):**
- EL-211 and CAT-059 agree on every number:
  - 81.2 W absorbed and 16.2 W electrical;
  - 0.135 J per tick into a 0.15 J store;
  - 31.5 W on and 28.35 W release;
  - 21 W from one Laser and 42 W from two.
- One optical tolerance set (±2 % power, 1 s guard, 0.5° direction, ±0.02 m end points) in CAT-059, CAT-008 and EL-211.
- The belt and chain share:
  - one mechanism (a declared material plus engagement classes) and one story (`ENGINE-MECHANICAL-LINKS`);
  - the 160 N worked tension;
  - pitch radius = rim radius;
  - the 74.8 N·m contrast;
  - the rigid link unchanged as default.
- The one cutting-work contract (EL-066) is cited unchanged by EL-067 and EL-068 (25 J, 625 N, 0.154 m reach). EL-205 now agrees with EL-067's material.
- EL-198 agrees with CAT-063: colour-only latch, 5 colliders, latch once per Run. CAT-063:97's last sentence ("EL-198 … still describes the LocalY travel") is stale and should be deleted.

## 3. Per-spec findings

Each spec lists its first-round blockers, the rows re-derived (with whether each can pass and can fail), decisions and anchors, buildability, then findings with replacement text.

### EL-038 Pneumatic hose

- **Blockers:**
  - P-H1 (line start state): `:34`, OQ5 `:141`, citing EL-039 F9.
  - P-H2 (several hoses per Output): `:28`, H-A7, new tee row H-A11.
  - P-H4 (constants become bundle data): `:32`, F10.
  - P-M9 (transfer clamped to equalisation): `:60`.
  - X-1: `:154`, the published section form, blocked on S7.
  - All resolved.
- **Rows:**
  - H-A1: 5173.25 / 0.0204 = 253 591 Pa ✓. Fails if the hose moves no gas.
  - H-A3: 1642.65 / 0.0044 = 373 330 Pa. With no line volume: 350 663 Pa. The gap is 3.0 × the 2 % tolerance, so the row passes and fails if the line has no volume.
  - H-A11: 206 055 Pa; control 253 600 Pa ✓.
  - Choked flow at 400 kPa: 0.1905 kg/s, 1.59 g per tick against a 48.4 g charge ✓.
  - H-A12 cannot pass.
- **Anchors:** `requirements.md:4703` covered. Owner 1 applied (solid, `ENGINE-CONNECTION-MATERIALS`).
- **Buildable** after fixes 1–3.
- **Findings:**
  1. **[High] `:127` H-A12.** "The Ball comes to rest on the tube" is an unstable equilibrium on a single round strand, so a correct engine fails the row. Replacement: "Given a hose lying across the bench and a Ball dropped from 0.5 m directly above its axis, then the Ball's centre never comes nearer the strand axis than Ball radius + 0.025 m less the contact slop (it does not pass through); it is deflected to one side and comes to rest on the bench against the tube; the gas rows above are unchanged by the contact | … | the Ball dropped 1 m beside the hose reaches the bench without touching it | a Ball dropped onto a Vent free end's cap is likewise deflected, not passed through".
  2. **[Medium] `:33`, `:43-44`, `:131`.** Where a free strand end lies physically is undecided, and no story covers it (`readiness/batch-h.md:96` gives the material only diameter and segment rule). Append to `:33`: "A free end is fixed at the authored bench point chosen with the Connect control; the strand's last segment is anchored there. A strand end fixed at an authored world point is a row of `ENGINE-CONNECTION-MATERIALS`."
  3. **[Medium] `:27`.** "Segment count, mass and collider shape are that story's" contradicts F10 and design rule D13 (product constants are declaration data). Replace with: "The hose connection material declares diameter 0.05 m, linear density 0.3 kg/m *proposed*, friction 0.6, restitution 0.1, bounce threshold 0.1 m/s *proposed*, and the segment rule `ENGINE-CONNECTION-MATERIALS` defines; the engine derives segment count from the rule."
  4. **[Low] `:59`.** The throat's position is unstated. Settled values do not depend on it, but H-A2 and H-A4 timing do. Replace with: "The line's volume is joined to the store on its Output end without loss; its one throat is at its Input end (or at its free end)."
  5. **[Low] `:32` against `:72`.** Ownership is split. `:32`: "The declaration format of a connection material (diameter, segment rule, linear density, contact material, line volume, throat area) is `ENGINE-CONNECTION-MATERIALS`'s; the line row that reads volume and throat is `ENGINE-GAS-NETWORK`'s." `:72` owner: "drawing a strand along its segments' committed poses: `ENGINE-CONNECTION-MATERIALS`".

### EL-043 Pneumatic pressure gauge

- **Blockers:**
  - P-H2 (P-A2 and P-A5 use a second hose on `GasOut`): `:121`, `:124`, `:141`.
  - P-H1: `:27`, `:64`, `:142`.
  - P-M3 (306 000 / 294 000 Pa): `:123`.
  - X-1: `:151`.
  - All resolved.
- **Rows:**
  - Ambient-tank case: (4000 + 320 + 1013.25) / 0.0208 = 256 406 Pa, below the 270 000 Pa off level, so the contact opens ✓.
  - 0.0065 m³ case: 287 781 Pa, with margins of 17 781 Pa and 12 219 Pa against a 5 756 Pa tolerance. Passes; fails without hysteresis.
  - P-A6: a hose that started at ambient would settle the tank near 388 500 Pa and lose about 3 % of its mass. The row can fail.
  - P-A4: 294 000 Pa lies inside the band and must stay Off. Can fail.
- **Anchors:** `requirements.md:4743` covered.
- **Buildable** after finding 1.
- **Findings:**
  1. **[Medium] `:44`, `:55`, `:84`, `:94`.** "If absent, the mark is drawn at the default and the selected-part readout shows the value" is a second rule chosen by condition (forward-only gate) with no owner. `:55` "printed flat" also contradicts `:94` "placed from `threshold`".
     - `:84` last sentences: "Missing: a presentation node posed at build from a Real parameter (the threshold mark). Owner `ENGINE-PARAMETER-BOUND-FRAMES` (coord 16)."
     - Add the capability row to §3, and the story to `:16` and `:156`.
     - `:55` Class: "decorative: flush, posed from `threshold` (`ENGINE-PARAMETER-BOUND-FRAMES`)".
     - P-A8 Boundary: "the mark sits at the needle angle for `threshold`".
  2. **[Low] `:41-42`.** The sockets float. Use `(−0.5, −0.37, 0), −X` and `(0.5, −0.37, 0), +X`.

### EL-045 Air whistle

- **Blockers:**
  - S-1 (observation-only probe N6): `:24`, `:31`, `:100`.
  - S-2 (flow drain): `:84`, row 8.
  - P-H3 (EL-041 `Exhaust`): `:16`, `:52`, row 7.
  - S-17 (tone bars, C13): `ENGINE-PARAMETER-BOUND-FRAMES`, `:95`.
  - S-8: `:106`, `:166`.
  - All resolved.
- **Rows:**
  - Subsonic rates at 105 400 / 104 600 Pa: 0.00200 / 0.00179 kg/s ✓.
  - Choked rate at 210 kPa: 0.0100 kg/s ✓.
  - Row 1: 1/(1 + 0.72) = 0.581. Fails with the Fan off.
  - Row 3: off level 5.4, so m = 3 is silent. Can fail.
  - Row 8: can fail through its Windmill control.
- **Anchors:** `requirements.md:4759` covered.
- **Buildable** after finding 1.
- **Findings:**
  1. **[Medium] `:31`.** The probe's samples at x −0.47 lie on the mouthpiece's end face. The delivered sweep tests every collider not on a receiver exclusion list (`SIM/WorkshopPhysicsEngine.Airflow.cs:141-160`), so the whistle may never sound. Append: "The whistle's own colliders are excluded from its sample lines (the receiver-exclusion form of `WorkshopPhysicsEngine.Airflow.cs:141`–`:160`, which N6 carries over)." Add the same clause to EL-044 N6.
  2. **[Medium] `:80`, `:104`.** A vent on an Input port has no declared home. `:104` gap sentence: "ENGINE-GAS-NETWORK: a vent throat to ambient may be declared on any Fluid port, Input or Output, with a committed mass-rate observation." Add EL-045's `GasIn` throat to EL-039 F4.

### EL-051 Acoustic dish

- **Blockers:**
  - S-5 (corner): a 70° acceptance half-angle; row 1 at 60°; row 3 at 68° and 72°; `:30`, `:65`, `:113`, `:115`.
  - S-6 (art): a straight cylinder with the concavity shaded; `:46`, `:129`.
  - Both resolved.
- **Rows:**
  - Speaker position (1.1, 0, 1.73) ✓.
  - Level via the dish: 0.8 / 3 = 0.267. Direct over 5 m: 0.333 ✓.
  - The meter is 79.1° off the speaker's axis, so it hears nothing direct ✓.
  - Reach: d₂ = 3.24 m ✓.
  - Row 1 boundary: an 8 m path gives 0.131, above the 0.05 threshold. Fails with the dish removed.
- **Anchors:** `requirements.md:4807` covered.
- **Buildable** after findings 1–3.
- **Findings:**
  1. **[High] `:28`, `:49`.** The post is a cylinder of r 0.06 m, below the admitted 1/16 m (`ABS/PhysicsDeclarationValues.cs:241`), so the bundle refuses it. Replace with "cylinder r 0.07 m" in both places.
  2. **[Medium] `:114` row 2.** "Rotated 25° off the meter" has no direction. Turned away from the speaker, the arrival becomes 85°, beyond the 70° acceptance, so the meter reads 0 for the wrong reason. Replace with: "Given the dish turned 25° toward the speaker (arrival 35° off its axis, still redirected), then the meter, now 25° off the dish axis, reads 0".
  3. **[Medium] `:116` row 4.** A second meter "5 m from the speaker on a direct line" along the axis is behind the opaque dish. Replace with: "a second meter at (0.23, 0, −3.19): 5 m from the speaker, 20° off its axis in the horizontal plane, with a clear straight path and outside the dish's beam".
  4. **[Low] `:113`, `:57`.** Add "and clear of the speaker-to-dish line" to the Wall. State at `:57` that the dish's own colliders, including the boss sphere, are exempt on both the incident and the child path (L6).

### EL-052 Water-tuned bottle

- **Blockers:**
  - S-1 (N6 probe): `:24`, `:32`.
  - S-12 (band marks, Too full): `:52-57`, `:67`.
  - S-13 (conservation at rest; EL-001 L8 stream credit): `:78`, row 3.
  - Owner 20 (thermal node, `ENGINE-THERMAL`): `:31`.
  - All resolved.
- **Rows:**
  - Equivalent plan area 0.25 / 1.25 = 0.2 m²; marks 0.0625 m³ ✓.
  - Neck tube: bore 0.1 m, wall 0.025 m, outer radius 0.125 m. Admitted.
  - One mark fills in 1.3 s; row 3 in about 21 s, inside 30 s.
  - Row 1: fails on a wrong tone table. Row 3: fails on lost water.
- **Anchors:** `requirements.md:4815` covered.
- **Buildable** after findings 1–2.
- **Findings:**
  1. **[Medium] `:128` row 2 boundary.** Lamps at "0.004 m³ below and above each mark" cannot show the boundary: sound takes 0.25 s plus one electrical solve, and the fill advances about 0.012 m³ in that time. Replace with: "the emitter's committed tone (acoustic read model, EL-044 N13) is the lower band at 0.004 m³ below each mark and the higher band at 0.004 m³ above; the lamps follow within the 0.25 s travel time (L2) and one electrical solve".
  2. **[Medium] `:76`, rows 2–3.** The tap geometry is unstated. Under EL-003's horizontal stream the landing point moves about 0.14 m over the fill, more than the bore radius, so the stream lands on the rim and counts as Spilled. Replace with: "A default EL-001 reservoir with its Tap outlet 0.05 m above the neck rim and 0.25 m beside the neck axis, so the stream centre lands on the axis at the start; over the 0.3125 m³ fill the landing point moves under 0.05 m, inside the 0.1 m bore less the 0.039 m stream radius." The slice re-derives the offset from the EL-003 law.
  3. **[Low] `:130`.** A 12 m/s Fan over four samples gives only 0, 3, 6, 9 and 12 m/s. Use "3 m/s (one sample, silent) and 6 m/s (two samples, strength 0.5)".
  4. **[Low] `:64`, `:80`.** "No delivered ball fits" is false: the Tennis ball's `radius` goes down to 1/16 m (`TennisBallElementDeclaration.cs:18`). Replace with: "The bore admits a tap stream; a Tennis ball set below radius 0.1 m drops in, rests on the glass top and, by the general laws, displaces liquid (EL-001 L12) and shadows stream samples."
  5. **[Low] `:32`.** State "a horizontal 0.1 m square centred at (0, 0.85, 0)".

### EL-180 Pulse speaker

- **Blockers:**
  - S-3: a 2 J pulse store, empty at start, charged at 10 W; `:33`, `:95`, `:131`.
  - S-4: the four deltas are recorded at `:14` and adopted in CAT-061 (`:15`, `:27-28`, `:49`); acceptance is delegated to CAT-061 rows (`:146-157`).
  - X-1: `:179`.
  - M-5: `:49-52`.
  - All resolved.
- **Rows:**
  - Re-arm 2 J ÷ 10 W = 0.2 s. Pulse lifetime 8/12 + 0.15 = 0.817 s, so at most 5 in flight, which is the requirement's bound.
  - 3600 J ÷ 2 J = 1800 pulses; 60 J ÷ 2 J = 30.
  - Two speakers on a 10 W limit re-arm in 0.4 s each.
  - Reach: 1/(1 + 0.08 d²) = 0.25 gives 6.12 m ✓.
- **Anchors:** `element-180` (edge contract, no infinite pulse energy) is covered via CAT-061 rows 3–4. Owner 4 (bars printed, one to three) is applied at `:70`.
- **Buildable:** no, as written; X-3 leaves the implementer to choose between two declarations.
- **Findings:**
  1. **[High] `:22-74` (X-3).** Replace §2 with: "The `speaker` declaration, art and bindings are [CAT-061 sections 2–3](CAT-061-speaker.md#2-declaration); this identity adds none." Keep K1–K10 and Behaviour.
  2. **[Medium] `:14`, `:169`.** Once CAT-061 adopts the lamp, no delta remains. `:14`: "**Duplicate closure of CAT-061.** All four earlier deltas are adopted in CAT-061; closed by CAT-061 rows 1–14 in the Speaker slice (coord 16)." `:169` item 3: "Decided: duplicate closure, since no delta remains."
  3. **[Medium] `:32`, `:116`.** "Granted" becomes "powered" (X-4).
  4. **[Low] `:185`.** Prerequisites: "Story 14.0 amended with gaps 4 (with its supply clause), 11, 12, 13 and 14".
  5. **[Low] `:159`.** Replace with "Adopted into CAT-061 rows 3–5."
  6. **[Low] `:28`.** The cabinet floats 1 cm above the foot (foot top −0.76, cabinet bottom −0.75). Foot to "(0, −0.84, 0)".

### EL-183 Resettable counter

- **Blockers:**
  - E-2: per-node resolve at `:75`; C4 `:136` covers a same-tick arrival and reset in both orders; OQ2 `:155` withdraws the old text.
  - E-7: 15 delivered colliders (`CounterTests.cs:88-92`) plus `ResetIn`, 16 in all; `:27`.
  - X-1: `:165`.
  - E-10: the citation is corrected.
  - All resolved.
- **Rows:**
  - C4: a reset dominating a same-tick arrival gives 0; control 1. The delivered inline counting (`engine/gpu/ActivationNetwork.cs:531-541`) would fail it.
  - C2: count 0, not 1. Can fail.
  - C3: needs the phase Clear (`:535`). Can fail.
  - C5: reset-dominant gives 0. Can fail.
  - C9: count 3 after 20 extra arrivals; today it would read 23. Can fail.
- **Anchors:** `current-cat-020`'s "no supply creates no electricity" has no row (finding 3).
- **Buildable** after finding 1.
- **Findings:**
  1. **[Medium] `:75`, `:94`** (and `readiness/batch-h.md` §4, Count-reset row). "Resolved once at that tick's boundary … a crossing emits `ThresholdReached` in that same tick, which is then routed onward in that tick" contradicts the router: the boundary resolve runs after `RouteOccurrences` (`ActivationNetwork.cs:306-318`). Replace with: "Count and reset requests routed to a pulse-count node in a tick are resolved once for that node, after the last occurrence routed to it in that tick and before its own output is routed. Same-tick routing is acyclic (`ComposableSignalNodesTests.InvalidLocalReferencesAndInstantaneousCyclesAreRefused`), so this order exists. Any reset gives count 0, phase Clear and no emission. Otherwise arrivals count in occurrence order, and a crossing emits one `ThresholdReached`, routed onward in that same tick."
  2. **[Medium] `:27`, `:50`, `:52`.** The sockets float (X-8). New positions: ActivationIn (0, 0.625, 0.1); ResetIn (0.62, 0.625, 0.1); PowerIn (−0.825, 0, 0); Supply (0.825, 0, 0); ActivationOut unchanged. Change "re-reads the sizes there" to "seats each socket centre on its face".
  3. **[Medium] `:133` C1.** Add to the Control: "with `PowerIn` unwired, the target is reached and the contact closes, but the gate is never powered".
  4. **[Low] `:134`, `:144`.** "240 ticks" becomes "2 s of game time".

### EL-198 Switch

- **Blockers:**
  - E-1: `contact` is required and an old save is refused whole (`:41`, `:153(a)`, `:166`, A6 `:136`). The refusal path is real: `InstanceParameters.cs:41` via `WorkshopSavePrevalidation.cs:108-113`.
  - E-6: A3 is restated as observables (`:133`): `InactivePort` at `WorkshopConnections.cs:86-93`.
  - X-1: `:160`.
  - All resolved. Matches CAT-063: colour-only latch, 5 colliders (`switch.tscn:36-113`), latch once per Run.
- **Rows:**
  - A1: Closed is powered from the first solve; Open never is. Can fail.
  - A2: Closed with no Battery is never powered. Can fail.
  - A4: options 0..2. Can fail.
- **Anchors:** `element-198` is covered by A2. The author-set contact is a row of `ENGINE-SIGNAL-NODES` (coord 25).
- **Buildable** after finding 1.
- **Findings:**
  1. **[Medium] `:60-61`, `:133`.** In modes Open and Closed the impact node still latches and publishes `TriggerActivated`, so an event-based goal would count it. Signal nodes and events have no Choice `Selection` today.
     - §2 Signal nodes: "The impact node and its `TriggerActivated` event are present only when `contact` = Impact (a node and event present by a Choice option, the choice-bound values of `ENGINE-PARAMETER-BOUND-FRAMES`, coord 16)."
     - Add to A3's Then: "and no `TriggerActivated` occurrence is published".
     - Add a §3 row marked missing, owner `ENGINE-PARAMETER-BOUND-FRAMES`, and the story at `:16` and `:165`.
  2. **[Low] `:41`/`:153`.** State what happens to an existing `ActivationOut` wire when `contact` is edited away from Impact: "as the Fan's mode edit (`FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation`, `:191`)".
  3. **[Low] `:131`, `:138`.** "240 ticks" becomes "2 s".

### EL-211 Solar panel

- **Blockers:**
  - E-11 (palette, art numbers, owner rows): `:18`, `:34`, `:51-55`, `:116`. Store row → `ENGINE-THERMAL`; receiver credit → `ENGINE-OPTICAL`.
  - E-15 (marks at y −0.55; selection radius 1 m; dim and hold-band cases): via CAT-059 P2 and P4.
  - E-9 (`Reserve`): `:49`.
  - E-12 (work ≤ 1.02 × grant): via P5.
  - E-16 (chapter 6, levels 51–60): `:116`, `:121`.
  - Batch E H-6: every number re-derived and matching CAT-059.
  - All resolved.
- **Rows:**
  - Mark levels 31.5 ÷ 4 = 7.875, 15.75 and 23.625 W, so 21 W lights two marks and 42 W lights four.
  - 0.2 × 81.2 ÷ 120 = 0.135 J per tick < 0.15 J ✓.
  - Pass/fail is inherited from CAT-059 P5, P6 and P10.
- **Anchors:** `element-211` ("darkness yields no power; output never exceeds absorbed energy") is covered by CAT-059 P2, P7 and P5. Coord 21 form exact; coord 16 deference correct.
- **Buildable** once CAT-059 is fixed.
- **Findings (all Low):**
  - `:99` restates P5 as an exact grant; CAT-059 asserts a bound. Replace with: "P5, as CAT-059 states it: the panel's grant never exceeds 1.02 × 0.2 × the absorbed energy, the Motor's committed work never exceeds 1.02 × the grant, and the load's rise speed stays within P5's bound."
  - `:100`: "P6, as CAT-059 states it: two Motors on one panel are granted equal energy ± 2 %, and their sum obeys P5's bound."
  - `:38`, `:41`: follow CAT-059's P5 load (0.41 m/s for 4 kg).

### EL-061 Indexed carousel

- **Blockers:**
  - F3: a supplied revolute position drive, π/2 per pulse, bounded at 1.5 rad/s, 3 rad/s², 20 N·m and 30 W, with self-lock (`:55-56`), owned by `ENGINE-JOINT-KINDS` (`:115-117`). The Wound spring and detent are removed. Side items moved.
  - F26: the bus node is dropped (`:68`).
  - F20: counts 9/1/1/2.
  - All resolved.
- **Rows:**
  - Axial moment 0.849 + 0.008 = 0.86 ✓.
  - Step time 1.55 s ✓.
  - Peak drive power ≈ (0.86 × 3 + 0.1) × 1.5 ≈ 4 W, well inside the 1 J per tick allowance.
  - Pocket width 1.27 m ✓. Fence chords 0.493 / 0.517 m ✓. Exit path inside C8's 30° ✓.
  - C1 and C2 can pass and fail. C3 passes vacuously. C6's lower bound fails a correct drive. C8 is ambiguous.
- **Anchors:** `requirements.md:4883`: missing power (C2) and obstruction (C3).
- **Buildable** after the fixes.
- **Findings:**
  1. **[Medium] `:160` C3.** The Wall pose overlaps the 45° paddle at Run start, so the row passes trivially. Given: "a Wall (width 0.4, height 0.6, thickness 0.12; delivered minimums) standing on the floor as a radial plate centred at radius 0.7, its near face on the 75° radial line, in the pocket ahead of the 45° paddle. Then the rotor stalls near 27° (between 10° and 60°), the gate is unsupplied once the rotor has left index 0, …".
  2. **[Medium] `:164` C8.** Replace with: "a second ball in the −Z pocket (the one 90° past the station in the turning direction) is still inside the fence after one and after two steps".
  3. **[Medium] `:163` C6.** The lower bound includes friction during deceleration, which the drive does not pay (1.09 J delivered < 1.13 J bound). Replace with: "The drive's committed positive work over one free step is at least the rotor's peak kinetic energy (½ × 0.86 × 1.5² = 0.97 J) and at most torque × pitch = 31.4 J (+5 %)".
  4. **[Low] `:70` against `:129`.** Binding counts: "Three albedo bindings (r, g, b) on `Visual/Lamp`, slate to gold, driven by `CommittedActuatorDrive` for Revolute 0 (as the Motor)".
  5. **[Low] `:43-44`.** The hub floats 0.02 m. Use "hub cylinder r 0.2, height 0.52, from y 0.15 to 0.67".

### EL-066 Scissors

- **Blockers:**
  - F4: the one cutting contract `:74-86`: engagement at gap d, force R/d, one-way, sever at gap 0 when R ≤ C, hold at d when R > C; ledgered and read.
  - F8/O2: the 22 October decision is cited (`:178`).
  - F29: X5 asserts the cut-work read.
  - Owner: the cutting clause of `ENGINE-TENSILE-EXTENSIONS` (coord 16).
  - All resolved.
- **Rows:**
  - Engagement asin(0.04/0.15) = 0.270 rad ✓.
  - X3: 0.47 J against 1 J; stops about 0.10 rad into the band. Fails if the force law is missing.
  - X1: 2.45 J against 1 J ✓.
  - X4: 25 > 4, so the gap is held. Fails if there is no capacity check.
  - X7: the blades open in about 0.25 s ✓.
- **Anchors:** `requirements.md:4923` (wrong placement X2, insufficient work X3).
- **Buildable:** yes.
- **Findings (Low):**
  - `:91` cell: "cut when `cut_work` ≥ 25 J and the crossing is within 0.154 m of the pivot ([EL-068](EL-068-tin-snips.md))".
  - `:154` X2: the boundary strand at z 0.05 is tangent to the blade face; use "Route 1 crossing 0.08 m in front of the blades in Z (its 0.02 m strand radius clears the ±0.03 m blades)".
  - `:23`: append "; the cut itself is the cutting clause of ENGINE-TENSILE-EXTENSIONS, not ENGINE-FRACTURE (section 3)".

### EL-067 Steel cable

- **Blockers:**
  - F7: the non-placeable material entry kind belongs to `ENGINE-CONNECTION-MATERIALS` (`:21`, `:90`).
  - F8/O1: solid under owner 1 (`:47`, `:61`, `:161`), with row K12.
  - F29: K1 stretch bound.
  - EL-205 conflict: settled.
  - All resolved.
- **Rows:**
  - K1: g/(2π·60)² = 0.069 mm. It can fail for any span softer than about 7 Hz.
  - K2: taut at 0.45 s, so the 0.3 s slack check is valid.
  - K12: can fail if a ball passes through.
  - 625 N = 25/0.04 ✓.
- **Anchors:** `requirements.md:4931` (stiffness and cutting resistance as material: K3; compression cannot push: K2).
- **Buildable** once `ENGINE-CONNECTION-MATERIALS` fixes the segment rule.
- **Findings (Low):**
  - `:64`: "At a glance it differs from rope by its navy twist marks and gold ferrules, not by colour alone; both strands are drawn at their declared 0.04 m".
  - `:135`: "(derived 0.07 mm at 60 Hz; the bound fails any span softer than about 7 Hz, and K3 pins the declared 60 Hz exactly)".

### EL-068 Tin snips

- **Blockers:**
  - F4: inherited from EL-066.
  - The stroke claim is corrected (`:89`).
  - `ENGINE-SUPPLY-DEMAND` row T13 is added; the capacity is parameter-bound (`:109`).
  - All resolved.
- **Rows:**
  - Engagement asin(0.4) = 0.412 rad. The cable needs 62.5 + 3.6 N·m against 100 N·m (34 % margin).
  - Reach: x ≤ 0.154 m (25 J) and x ≤ 0.096 m (40 J) ✓.
  - T5 at 0.30 m: 187.5 N·m, so it stalls ✓.
  - T8: about 25 J added over the empty stroke, inside 20–30 J. Can fail.
  - T3/T4: exact declared boundaries.
- **Anchors:** `requirements.md:4939` covered by T1, T3, T4, T6 and T9.
- **Buildable** after finding 1 and X-1.
- **Findings:**
  1. **[Medium] `:45`, T1 `:150`, T8 `:157`, T13 `:162`.** The cut stroke demands about 197 W, 1.64 J per tick, against the default 1 J (X-1), and the Battery's 100 W electrical node (`BatteryElementDeclaration.cs:76`) against the snips' 300 W consumer is unchecked. Append to `:45`: "A cut stroke demands up to 66 N·m × 3 rad/s ≈ 200 W. The standard rig uses a Battery with `power_limit` 240 W. At the default 120 W the drive is bounded by the source's per-tick allowance (ENGINE-SUPPLY-DEMAND) and closes more slowly, and the cut still completes because the needed torque (66 N·m) is below 100 N·m. If the story instead stops the drive, T1 is re-derived, not widened." Add a T1 boundary: "default 120 W Battery: still cut".
  2. **[Low] `:93`.** "The open tips are about 0.18 m apart at rest" (not 0.21).
  3. **[Low] `:35-37`.** The jaw's back-top corner swings 0.04 m into the housing art at the open pose. Use a jaw box 0.29 × 0.08 × 0.10 centred at (0.205, 0.04, 0), the fixed jaw likewise, and cutter edges from (0.06, 0, 0).
  4. **[Low] `:23`.** The same ENGINE-FRACTURE note as EL-066.

### EL-072 Curved metal ball pipe

- **Blockers:**
  - F2: EL-072 holds `metal_pipe_bend_90` only (`:137`); conflicts with owner 15, finding 1.
  - F19: B4 cites EL-071 A2; B2 lies in a horizontal plane; no every-tick assertion.
  - F23: B10 and B11 added.
  - F30: matte (`:46`).
- **Rows:**
  - B2: 0.65 − 0.28 + 0.01 = 0.38 m bound, at 144 m/s². Fails if a ball passes through the wall.
  - B4: a 0.61 ball passes (0.629 < 0.65). A 0.75 ball seats 0.39 m outside the collar plane.
  - B6: 0.3 × 0.55 = 0.165 against 0.0825 ✓. Can fail.
  - Band: 0.5 mm flatness, 2 mm proud, both under 5 mm ✓.
- **Anchors:** `requirements.md:4971` covered.
- **Findings:**
  1. **[Medium] `:15-17`, `:137`, `:165`.** Owner 15 allows declared companion packages with no new ledger identity, and S7 admits them. Replacement for `:137` item 4: "`metal_pipe_bend_45` is a declared companion package of EL-072 under owner decision 15; S7 admits it; the Family slice delivers both packages, each with its own rows; CAT-049's retained acceptance closes on the companion." If the coordinator holds that a placeable peer is not a companion, cite that ruling and name the owner of the ledger addition.
  2. **[Low] `:119` B4.** "Given a bend with its start mouth up and a Tennis ball with `radius` 0.75 m dropped 0.5 m above it, then it jams at the mouth" ("never crosses the mouth plane" contradicts the cited tolerance).
  3. **[Low] `:126` B11.** "A collimated beam" becomes "A Laser beam".

### EL-109 Speed trapdoor

- **Blockers:**
  - S1: `:167`, §7.
  - H4: 2.0 m opening, 6 m/s maximum, `:42`, `:46`, `:71`.
  - G1: minimum crossing speed as an `ENGINE-SENSOR-VOLUMES` field with two fixtures (`:121`, `:195`).
  - M3: present-untested, 3/3/1/0.
  - M4: `:40`, `:44`, `:69`.
  - All resolved.
- **Rows:**
  - Opening: (2.0 − 0.34)/0.263 = 6.31 m/s. At 6 m/s the corner clearance is 2 mm, which is on the edge.
  - Hatch: clears 0.99 m in about 0.30 s, against 0.5 s for the ball ✓.
  - Moments 0.144 / 0.474 / 0.336 ✓.
  - A2 (2.7 / 3.3 against 3 m/s) can pass and fail.
  - A6 cannot pass.
- **Anchors:** `element-109` is covered by A1.
- **Buildable** after the fixes.
- **Findings:**
  1. **[High] `:121`, `:22`, `:172` (X-6).**
     - Replace the last sentence of `:121` with: "The hatch spring declares its rest position at coordinate 0 (closed), owner decision 6 as extended by coordinator decision 25, 23 November 2026; owner ENGINE-JOINT-KINDS (spring rest position)."
     - Add `ENGINE-JOINT-KINDS` (spring rest position) at `:22` and `:172`.
     - Add row A10: "Given the hatch held open by supply, when the Battery's `enabled` is set to Disabled during Run, then the hatch coordinate is below 0.01 m within 3 s (ω = √14 = 3.74 rad/s, ζ = 0.53). Control: supply kept, hatch stays at 1.35 m ± 0.02." The row can fail.
  2. **[Medium] `:40`.** The rail tops at y −0.18 coincide with the hatch underside. With rail friction (2.9 N) the 14 N/m spring stalls at 0.21 m, which fails A10. Replace with: "tops at y = −0.185, 5 mm below the hatch, so the prismatic joint alone carries it; the rails are visible guides".
  3. **[Medium] `:156` A6.** "Under 1.5 m/s on leaving the opening" is impossible: the fall alone gives 2.58 m/s. Replace with: "horizontal speed under 1.5 m/s when its centre passes y = −0.5 (a hatch sliding at 4 m/s can drag a rolling ball to at most 2/7 × 4 = 1.14 m/s)".
  4. **[Low] `:151`.** Use 5.5 m/s for the boundary and as the stated maximum (16 mm clearance), or lengthen the opening to 2.2 m.
  5. **[Low] `:212`.** Name `sequence-task-662` (`requirements.md:5269`) instead of "no sequence task yet".

### EL-158 Rise-hold-fall cam

- **Blockers:**
  - S1: `:176`.
  - H5: thirteen cylinders at 7.5° (`:29`, `:33`, `:116`, `:188`).
  - H6: hinge at y 0.50, follower at 0.98.
  - All resolved.
- **Rows:**
  - Cusp 0.2974 m (2.6 mm scallop) ✓.
  - Ripple 0.51 mm ✓.
  - Pickup at θ = 75°, 0.208 m lateral inside a 0.22 m half-foot ✓.
  - Float limit 8.6 rad/s ✓.
  - H2 sector 2.281 rad: 2.28 s at 1 rad/s ✓. H5 0.38 s ✓.
  - H1–H5 each can pass and fail.
- **Decisions:** `ENGINE-PROFILE-COLLIDER` is conditional on the fourteen-cylinder fixture (one owner, a concrete 0.005 m test); acceptable.
- **Findings:**
  1. **[High] `:42`.** One scalloped `Visual/Cam` mesh classed Solid fails the matcher (`VisibleSolidColliderTests.cs:221`; leftovers at `:231`), so row C10 fails as written. Replace the first two sentences with: "`Visual/Cam` is fourteen cylinder meshes, each with the radius, half-width and pose of one cam collider (the 0.18 m disc and thirteen 0.06 m cylinders on the 0.24 m circle), classed Solid and matched one to one. Their union is the scalloped band. The gold dwell band is printed flat on their outer faces (Decorative)." Delete "how its hand table registers … was not read".
  2. **[Medium] `:60-103`, `:151-157` (X-5).** Delete them, or replace with "Superseded by the current declaration (no parameters, fixed 90° dwell, 0.44 m follower, `CoordinatePerRadian` +1, no `LinkageOut`)".

### EL-186 Temporary bridge

- **Blockers:**
  - S1: `:191`.
  - H1: Basketball cargo, A3 rewritten, minimum 25 N (`:41`, `:167`, `:178`, `:180`, `:210`).
  - H2: pins on the lower edges, `BodiesCollide` true (`:32`, `:35`).
  - G7: in the `ENGINE-FRACTURE` text (`:166`).
  - All resolved.
- **Rows:**
  - Load = 9.8 + W(1 − x/3).
  - Bowling ball: entry 49.0 N, 0.3 m 45.1 N, mid-span 29.4 N; two at mid-span 49.0 N.
  - Basketball: entry 19.6 N, 0.3 m 18.6 N.
  - Swing clearance 2.4 mm; landing 17.06°.
  - Swing load with a mid-span Basketball ≈ 23.4 N < 40 N ✓.
  - A2 and A3 can pass and fail.
- **Anchors:** `element-186` is covered by A2 and A3.
- **Findings:**
  1. **[Medium] `:60-103`, `:155-161` (X-5)** ("rests on", welds, the `support` enum, category Motion, Activation ports). Delete `:60-103`; at `:161` "`support`, `end_capacity`" becomes "`end_capacity`".
  2. **[Low] `:166`.** Two pins on one rigid deck leave the axial share indeterminate. State "shear perpendicular to the deck (vertical reaction)" as the measure.

### EL-200 Drive belt

- **Blockers:**
  - H8: pitch radii, Fan 0.14 m, `ConveyorTests:453` (`:46`, `:75`).
  - G3: `ENGINE-MECHANICAL-LINKS` (`:135`, `:236`).
  - C3: one mechanism (`:50`).
  - O1: owner 2 (`:25`, `:41`).
  - All resolved.
- **Rows:**
  - Capstan F = 2T·tanh(μπ/2); factor 1.1138 at μ 0.4 and 1.7003 at μ 0.8.
  - T 160 gives 178.2 N, so 24.9 / 39.2 / 53.5 N·m ✓.
  - B3 4.9 rad/s² ✓.
  - B4 17.2 / 27.0 N·m; 1.05 rad of slip > 0.5 ✓.
  - B5 6.2 rad/s² ✓.
  - All can pass and fail.
- **Anchors:** `element-200` (missing tension, disconnected) covered by B2 and B7. The rigid link is unchanged and the default (`:25`, `:48`, `:212`).
- **Findings:**
  1. **[Medium] `:56` (X-6).** "The delivered rigid link keeps its schematic drawing and stays non-solid, like a thin wire (coordinator decision 25, 23 November 2026, under owner decision 2); only the belt and chain materials are solid."
  2. **[Medium] `:77-115`, `:174` (X-5)** (a 0.22 m sheave on every socket, the radius ratio, fan-out). Delete; at `:174` "fan-out to two loads" becomes "a second belt on an occupied coordinate is refused (B10)".
  3. **[Medium] `:197` B13.** "Stopped or deflected … never ends below it" contradicts itself. Then: "its centre, while within the strand's horizontal footprint, is never more than 0.01 m below the loop's top surface (it rests on the loop or rolls off its side)".
  4. **[Low] `:185-189`.** Add "Battery `power_limit` 480 W" to B1–B5 (X-1).
  5. **[Low] `:46`.** "Each delivered port declares a pitch radius equal to its rim radius; physics never reads the cosmetic rim". List the seven packages under Files.

### EL-201 Drive chain

- **Blockers:**
  - H8: the Fan is Smooth only (`:50`, `:206`, `:195`).
  - G3: `ENGINE-MECHANICAL-LINKS`.
  - M19: applied at `:170`, but it conflicts with `ENGINE-SUPPLY-DEMAND` (finding 1).
  - All resolved.
- **Rows:** K2 and K4 can pass and fail. K1 as written cannot be produced.
- **Findings:**
  1. **[High] `:170` K1.** It needs up to 100 N·m × 20 rad/s = 2000 W, against at most 4 J per tick (480 W) once `ENGINE-SUPPLY-DEMAND` debits drive work (X-1). The belt control's 2.7 rad of slip would collapse. Replace with: "Given Battery (`power_limit` 480 W) → Motor (`torque` 100 N·m, `speed` 4 rad/s) → Chain → a declared toothed rotor of 10 kg·m², the rotor reaches 4 rad/s (≈ 10 rad/s², peak 400 W) with no slip. Control: a Belt at `tension` 200, `friction` 0.8 (74.8 N·m) on the same rig slips by about 4·0.535 − ½·7.48·0.535² = 1.07 rad (> 0.5), drawing at most 299 W."
  2. **[Medium] `:66-103`, `:156-161` (X-5)** ("No collider", Sprocket kind, `teeth_in`/`teeth_out`, Chrome T8/T16 recipe). Delete.
  3. **[Medium] `:180` K11.** The same replacement as EL-200 B13.
  4. **[Low] `:36`, `:94`.** "Marks advance at the input port's rim speed (pitch radius × committed input rotation)".

### EL-216 Capture cradle

- **Blockers:**
  - S1: `:191`.
  - H11: option (a), the gate beside the cup (`:51`, `:169`, `:181`).
  - M25: `:117`, 6/0/0/0.
  - M26: the panel retracts into the wall (`:44`).
  - All resolved.
- **Rows:**
  - Floor at the outlet −0.034 ✓.
  - Seated Bowling ball centre (0.03, 0.278), below the 0.30 rim ✓.
  - Rolling 0.61 > 0.29 m/s² ✓.
  - Gate mouth (0.392, 0.603) ✓.
- **Findings:**
  1. **[Medium] `:169`.** The lower collar reaches −0.26 m, not −0.18. The tilted mouth puts the upstream collar rim at x ≈ 0.324, up to 0.07 m inside the cradle's floor slab and wall ends. "Wholly outside the cup" is false. Replacement: gate origin at (0.95, 0.56, 0), so the collar's lowest rim is at x 0.39; the bore floor starts at x 0.405, y ≈ −0.045 (a 0.015 m gap, 0.011 m step down); the ball rests with its centre near x 0.61; the gate's lowest point is −0.26 m, so the parts share no standing surface. Delete "wholly outside the cup".
  2. **[Medium] `:177` A1.** The control cannot fail: a vertical drop has no horizontal speed. Replace with: "dropped 0.5 m with 1 m/s horizontal toward +X: with the cradle it stays; control: with the cradle removed it rolls 1²/(2·0.29) ≈ 1.7 m, leaving the footprint".
  3. **[Medium] `:63-105`, `:154-159` (X-5)** (0.1 m floor at 3°, `outlet` Choice, SideGate, cup-scale blade, Save/Load `outlet`). Delete.
  4. **[Low] `:199`.** `ENGINE-ART-SOLIDITY` is not a valid name. Use "`spec-engine-art-solidity.md` (the visible-solid repair programme, owner decision 22)".
  5. **[Low] Integration.** Add "integration proof todo-411 (`requirements.md:2930`) is a separate task, not this identity's acceptance".

### EL-225 Latched slingshot cradle

- **Blockers:** F3: the rope is cut or paid out before release (`:60`). S1 cuts it, and its Control 2 asserts an uncut rope holds. Stiffness 120 N/m (`:30`, `:126`). Resolved.
- **Rows:**
  - Axial 7.36 N gives a 0.061 m sag.
  - Draw: 64.6 N against 78.5 N (8 kg); 4 kg holds at 0.388 m ✓.
  - S1: v = √((43.2 − 8.83)/1.5) = 4.79 m/s, band 3.6–6.0. Can pass and fail.
  - Fast draw 0.39 s ✓. S4 40.6 N ✓.
  - S2 and S5 cannot pass.
- **Anchors:** todo-396 (`requirements.md:2933`) is mapped. Owner 1 applied. Hold split between `ENGINE-JOINT-KINDS` and `ENGINE-ASSEMBLY-JOINTS` per `readiness/new-identities.md:82`.
- **Findings:**
  1. **[High] `:105` S2, `:65` (X-2).** Replace with: "S2 | Never drawn; Basketball seated (cradle at its 0.06 m sag); one pulse | The request reports Released with stored energy ≤ 0.5 J (½ × 120 × 0.061² = 0.22 J); the ball moves less than 0.1 m and stays in the cup | Control: drawn to 0.2 m, the ball launches | `SlingshotTests.ZeroChargeLaunchesNothing`". Alternative: a declared minimum-charge field on the `ENGINE-JOINT-KINDS` hold, keeping Empty.
  2. **[High] `:22`, `:108` S5.** The floor's front edge is 1.0 m up, so a seated ball's centre crosses the prong line 1.35–1.6 m up, above the 0.9 m prongs. "Strikes the prongs" cannot be derived. Replace in `:22` with: "two prongs 0.1 × 1.9 × 0.1 m at z = ±0.5 m (clear gap 0.9 m); a ball's centre crosses the prong line 1.3–1.6 m above the base top, inside the prongs' height". Re-check the band routing (`:49`) and item 9 at that height.
  3. **[Low] `:104`.** Add to S1: "the band spans' committed end points follow the cradle body within 0.02 m" (todo-396 "visible band extension and return").

### EL-227 Magnetic impulse relay

- **Blockers:**
  - F4 seat geometry: region x −1.1 to −0.1, staged seat outside it (`:24-25`). The striker sits at +0.025, the Cannonball centre 0.345 m from the face, the staged Basketball at 0.49 m ✓.
  - F4 I8: own seat pad and contact-load node (`:26`, `:111`).
  - X2: the Cannonball gains the field property in the first consumer's slice (`:63`).
  - G-C: `ENGINE-FIELD-FORCE` (`:73-75`).
  - All resolved.
- **Rows:**
  - Gain 0.655 × 15 = 9.83 J ✓.
  - I1 omits rolling energy: a 6 kg rolling sphere at 1 m/s carries 4.2 J, so 14.0 J total and 1.83 m/s, not 2.07.
  - Restitutions are products: striker–Cannonball 0.04, striker–Basketball 0.275. The chain is nearly dead, so the staged ball leaves at about 1.37 m/s and the 1.5 m/s floor can fail on a correct engine.
  - I7: striker–Bowling ball restitution is 0.5 × 0.14 = 0.07, so the Cannonball leaves at about 1.45 m/s (≤ 7.2 J) against the 9.8 J needed. It cannot pass.
  - I5 can fail.
- **Anchors:** todo-399 (`requirements.md:2942`): "visible input/output occupancy" is half met.
- **Findings:**
  1. **[High] `:60` item 5, `:107` I7.** Replace with: "I7 | `well_energy` 5 (gain 0.655 × 5 = 3.3 J); the retained Cannonball knocked out by a Bowling ball striking the output tip at 5 m/s | The Cannonball leaves at about 4 × 5 ÷ 11 = 1.8 m/s (9.9 J, about 7 J after it slips into rolling), leaves the region having lost at least 3.3 − 0.5 J between the seat and the region edge, and the next Cannonball is boosted | Control: a 1 m/s blow (Cannonball 0.36 m/s, 0.4 J) does not free it". Rewrite item 5 with restitution as the product.
  2. **[Medium] `:56-57`, `:101` I1.**
     - Item 1: "rolled in at 1 m/s (4.2 J including rotation) it arrives with 14.0 J, 1.83 m/s".
     - Item 2: "the striker–Cannonball restitution is 0.04 and striker–Basketball 0.275, so the chain is nearly dead: the staged Basketball leaves at about 1.4–1.8 m/s".
     - I1: "leaves at 1.2 m/s or more … staged-ball kinetic energy ≤ 14.0 J + 0.5 J".
  3. **[Medium] `:37`, `:44-48`.** Add "Output seat pad 0.7 × 0.02 × 0.8 m flush in the trough floor next to the output face, a second `SignalContactLoadDescriptor` (minimum mass 0.1 kg, no port) driving an output-seat lamp from `CommittedContactLoadPhase`" (delivered: `ComposedPhysicalSignalTests.TwoLoadFacesOnOneStaticFrameKeepTheirOwnState`). Counts become 10 colliders, 2 load sensors and 4 driver bindings. Add the lamp to I1 and I4.
  4. **[Low] `:105` I5.** Delete "a second Basketball staged". **`:106` I6:** state that the ring's staged balls are Cannonballs.

### EL-235 Jack-in-the-box

- **Blockers:** F2: 1.3 m box, head 0.3 m proud, the lid rests on the head, the ball loaded after winding, J7 reworded, self-release removed (`:12`, `:22-23`, `:27`, `:58`, `:60`, `:112`, `:124`). Resolved. Coil room 0.34 m ✓.
- **Rows:**
  - Head 20.1 J, 8.97 m/s ✓.
  - Winding torque 9.6 N·m < 20 N·m ✓.
  - Lid: with the spec's own 0.04 m sag the lid lies at 0.56 rad, so J7's 0.64 ± 0.05 fails.
  - Ball seat at 0.77 m (not 0.65): about 2.4 m/s and a 0.65 m rise. J1's floors (2 m/s, 0.5 m) still pass and fail.
  - J2 cannot pass.
- **Anchors:** todo-195 (`requirements.md:2938`) is covered by J1–J3.
- **Findings:**
  1. **[High] `:67` item 5, `:107` J2 (X-2).** Replace with: "No belt; one pulse | The request reports Released with stored energy ≤ 0.5 J (the 0.04 m rest sag, ½ × 160 × 0.04² = 0.13 J); head and lid move less than 0.02 m and 0.05 rad; nothing is launched | Control: wound by a belt, the pulse launches". Change item 5 to match.
  2. **[Medium] `:58`, `:112` J7.** Item 1: "…compress the spring 0.04 m, so the head's centre is 0.04 m below the rim and the lid lies at asin(0.3 ÷ 0.502) − 4.6° = **0.56 rad (32°)**; the hinge axis is the rim's top back edge". J7: "lid resting on the head at 0.56 ± 0.05 rad".
  3. **[Low] `:60-65`.** "0.77 m (touching the 0.05 m lip)"; release estimate 2.4–4.4 m/s and a 0.65 m rise.

### CAT-010 Bellows

- **Blockers:**
  - H-2: only the nozzle law remains (`:29`, `:71`); the continuous-work row is gone (`:67`).
  - H-3: option (b), a general bound-art matcher class with a failing control fixture (`:52`, B12 `:95`, `:109`).
  - M-9: held depth is not asserted (`:73`, `:80`); the stroke observer is `ENGINE-GAS-NETWORK`'s with B4 as consumer (`:30`); the Fan range citation is fixed (`:39`).
  - L-4: gone.
  - GasOut: a later slice (`:103`).
  - All resolved.
- **Rows:**
  - Folds meet at rest. At q −0.4 the upper fold is 0.09 m inside the base, acceptable only because the slider disables body–body collision (`:28`).
  - The plate clears the nozzle by 0.015 m. B12 can pass and fail.
  - Plate rest: an 8 N preload against 4.9 N holds the upper stop. Without it, 0.245 m of the 0.4 m travel is lost and B4–B6 change.
- **Anchors:** `requirements.md:516` (current-cat-010) and `:2241` (sequence-task-312) "conserved gas/nozzle transfer" and "physical work/reaction" have no row.
- **Buildable** after the gas contract's two open answers (a stated block on its owning story) and the fixes below.
- **Findings:**
  1. **[High] `:18`, `:65`, `:108`, `:114`.** The continuous prismatic-travel presentation driver is marked "No 3a story owns it". The brief requires exactly one owner. Name `ENGINE-GAS-NETWORK`. The gap audit's §5 row (`.tools/audit/engine-design-gap.md:151`) lists "travel presentation" with the axial chamber, nozzle, directional damping and stroke observation, all for CAT-010. Its §6 story 13 hands the other four to `ENGINE-GAS-NETWORK` and drops this one without reassigning it. `ENGINE-CAPABILITY-INSTANCES` is not the owner: owner 3a scopes it to naming and fixed-slot refactors, and this is a new driver. Replacement for `:65`'s owner cell: "`ENGINE-GAS-NETWORK` (with the axial chamber and stroke observation it already owns for this part; engine design audit §5): a presentation driver reads a prismatic coordinate's committed travel as a continuous value". Change `:18`, `:108` and `:114` to match, and add the clause to `readiness/batch-d.md:131`. Alternative: H-3 option (a), solid fold blocks with no scale binding, removes the driver.
  2. **[High] `:28`, `:58`.** "Spring 20 N/m with 8 N preload at rest" needs a declared spring rest position beyond the upper stop. `PrismaticSliderDescriptor` has only stiffness, damping and starting compression (`ABS/MechanicalContractValues.cs:100-114`), yet the row is marked present. Add the row "A slider spring with a declared rest position beyond its upper stop (8 N preload) | missing | `MechanicalContractValues.cs:104` | `ENGINE-JOINT-KINDS` (declared spring rest position, owner decision 6, coordinator decisions 21 and 25)". Add the story to `:108`; counts 4/0/3/2.
  3. **[Medium] `:84-97`.** Add: "B13 | One Bowling-ball strike: total receiver work over the burst ≤ the chamber's committed expansion work ≤ the striker's lost kinetic plus potential energy + 0.5 J | `BellowsIntegrationTests.ABurstNeverPaysMoreThanTheStrike` | control: no strike, zero receiver work". Add "conserved transfer and work (B13)" to the map at `:97`.
  4. **[Low] `:52`, `:116`.** "Coordinator decision, 22 November 2026" is not in the decisions file. Cite it as this spec's resolution under the standing delegation, or add it to the file.
  5. **[Low] `:7`.** "That contract owns the final values and tests" becomes "this spec's rows are the acceptance authority; the 12.4 contract is re-pointed to them".

### CAT-061 Speaker

- **Blockers:**
  - H-1: store `:27`, trigger rule `:28`, reason `:78`, rows 1 and 5 restated, disposition `:14`.
  - H-4: gap 15 → `ENGINE-PARAMETER-BOUND-FRAMES` (`:67`); N13 is the read model.
  - M-2: `:83`.
  - M-4: 0.1 / 0.3 s, a 2.25 s run.
  - M-5: `:38-41`.
  - M-10: no recess (`:50`).
  - M-13: 12 V, 0.3 Ω, 0 F, 10 W (`:29`).
  - L-4: moved to the appendix (`:142`).
  - All resolved. Not delivered; no engine hits.
- **Rows:**
  - Row 1: level 0.581 at 3 m; ledger 2(N + 1) J; control 2 J. Can fail.
  - Row 4 control: 0.15 s clock, 24-tick refill, emissions every second pulse, exactly 10 by 3.1 s, phase-independent ✓.
  - Row 5: 1.8 J gives none; 2.2 J and 3 J give one each ✓.
  - Row 6: 0.167 at 7.9 m, 0 at 8.1 m ✓. Row 9: 30° in, 40° out ✓.
- **Anchors:** `current-cat-061` (`requirements.md:824`): "mute/suspension cannot change gameplay" is tested for mute only; "cone/wave/audio observe committed occurrence" has no row.
- **Findings:**
  1. **[High] `:94` row 3.** "Lifted and dropped again: a second" cannot pass: the Impact switch latches once per Run (CAT-063 `:104`, `:156`). The positive case also has no drop height against the 0.2 s store fill. Replacement: "| 3 | A ball released 1.25 m above an Impact switch (strike about 0.50 s after Run, after the store has filled) bounces and comes to rest on it: exactly one pulse | `AHeldInputMakesOnePulse` | the ball misses the switch: no pulse | released from 2.5 m (strike about 0.71 s), so it bounces more: still exactly one pulse |".
  2. **[Medium] `:101` row 10.** The control cannot fail, because the store gate gives one emission either way. Replace with: "| 10 | Two Clocks at 1 s in phase on `ActivationIn`, store full: one emission on the tick after the pulses, and the committed acoustic read model (N13) reports one request for that tick, not two | `TriggersInOneTickCoalesce` | one Clock | — |".
  3. **[Medium] `:93` row 2.** The Boundary cannot fail, and "supply connected later" is impossible during a Run. Replace with: "Unsupplied and triggered: nothing; the Battery enabled later (run control) replays nothing. A full store with the Battery disabled does not emit on a trigger | … | Boundary: store full, Battery disabled only for the tick the request is served and re-enabled on the next: no emission for that request, and none later without a new trigger".
  4. **[Medium] `:28`, `:70`, `:126`.** "Granted" becomes "powered" (X-4).
  5. **[Medium] `:103`, `:106`.**
     - Row 12: "Muted, audio-suspended and audible runs give identical emissions and ledgers".
     - Add row 14: "| 14 | A served request produces one diaphragm cue, one wavefront and one voice; an expired request (store not full) produces none | `CuesFollowOnlyCommittedEmissions` | row 1 | — |". Map "cone/wave/audio observe committed occurrence" to rows 12 and 14.
  6. **[Medium] `:41`, `:47-53`, `:138` (X-3).** Adopt the Ready lamp. Art row: "| `Ready` | disc r 0.05 m printed flush in the front face below the rim, slate, gold when the pulse store is full | decorative: printed flat | colour by `PresentationDriver.CommittedStoredFraction` on the pulse store |". Bindings 4 → 7.
  7. **[Low] `:92`.** Add to row 1: "each emission is on the tick after its clock pulse".
  8. **[Low] `:79`.** Replace with: "every second or third request, depending on the order of charging and emission within a tick, which Story 14.0 states".
  9. **[Low] `:25`.** Foot to (0, −0.84, 0).

### CAT-059 Solar panel

- **Blockers:**
  - H-4: receiver → 0.2 credit → store `cell` (0.15 J, 18 W, empty) → ordinary source → contact under `SignalStateContactControl(available)`; threshold node in `ENGINE-SIGNAL-NODES`; no supply node reads the receiver (`:27-30`, `:87-89`).
  - H-5: `ENGINE-SUPPLY-DEMAND` is unconditional (`:14`, `:185`); P5, P6 and P10 are full rows here (`:150-155`).
  - X-1–X-4, X-6: `:71`, `:27`, `:107`, `:142`.
  - M-10: `:36`. M-18: samples at x −0.11.
  - Art stud at x 0.195 (`:42`, `:55`).
  - All resolved.
- **Rows:**
  - Ω 0.2141 sr, I 420.4 W/sr. Samples 46.71 / 45.78 / 45.18 / 44.29 W/m²; mean 45.09; 81.2 W absorbed; 16.2 W electrical ✓.
  - P4: 18.37 W/m² (33.1 W) at 4.75 m and 16.60 (29.9 W) at 5.0 m. The share fixture gives 29.95 W, 5.1 % below on and 5.6 % above release. Fails without a release ratio.
  - P3: Wall minimums 0.4 / 0.4 / 0.12 (`WallElementDeclaration.cs:28-30`). Hidden share 0.670, so 54.4 W. Fails with no occlusion or with whole-panel blocking.
  - P2: 12.06 W lights exactly one mark ✓.
  - P10: 21 W radiant per Laser ✓.
  - P5/P6 cannot run or fail.
- **Anchors:** `current-cat-059` (`requirements.md:812`): paid work now has rows. The named fixtures are deferred while the checkbox is ticked (finding 3).
- **Findings:**
  1. **[High] `:150` P5, `:151` P6.** The cited `MotorTests.cs:619` fixture ties the rope to a 0.3 m rim; it does not wind, so 5 s of lifting is impossible. At its 1 rad/s the Motor asks about 3 W < 16.2 W, so the bound passes even under binary supply, and the control gives the same work. Replacement P5: "Given the base fixture with the Motor at its defaults (20 N·m, 6 rad/s) lifting a default 4 kg Weight hung from its rim tie by the rope of `MotorTests.ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts` (`CuriousContraptions.tests/MotorTests.cs:619`; rim radius 0.3 m, load torque 4 × 9.81 × 0.3 = 11.8 N·m), when Run for 0.5 s from the first lit tick (the rim turns less than 0.8 rad), then the energy the panel grants never exceeds 0.2 × the energy absorbed over the same ticks (at most 0.2 × 81.2 W × 0.5 s = 8.12 J), the Motor's committed work is at most the granted energy, and the load never rises faster than 16.2 W ÷ (4 kg × 9.81 m/s²) = 0.41 m/s | control: the same Motor and load on a default Battery: the load passes 1.0 m/s within 0.25 s | grant ≤ 1.02 × 0.2 × absorbed; work ≤ 1.02 × grant; speed ≤ 1.02 × 0.41 m/s; the control's speed is more than four times the bound". P6: "each lifting P5's 4 kg load for 0.5 s; each load rises no faster than 8.1 W ÷ 39.2 N = 0.21 m/s × 1.02". `:69`: "a 4 kg load at no more than 0.41 m/s".
  2. **[Medium] `:155` P10.** "Supplied with 8.4 W" depends on the request rule. Replace with: "the store `cell` is credited 0.2 × 42 W = 8.4 W ± 2 % (0.07 J per tick) and the Motor is supplied and passes 1 rad/s within 5 s". Add the placement: "the two Lasers 2 m in front of the face with their beams square to it, at z −0.5 and +0.5 m, hitting the cells inside the 1.5 m face".
  3. **[Medium] `:186` with `:161`.** The criterion is weakened. Replace with: "Status: this spec, the EL-211 ledger row and `TODO.md`. The `current-cat-059` checkbox is ticked only when the `solar_motor`, `solar_shadow` and `delayed_solar` level slices pass, because the requirement names those actual fixtures".

### CAT-008 Beam splitter

- **Blockers:**
  - H-7: in-line S0 feed fixture (decision 4(e) `:159`), re-enumerated below.
  - H-8: R10 `:138`, worked at `:58`.
  - H-9: once per crossing, exit and entry origins, ρ + τ > 1 refused (`:29`).
  - M-11: "+45° about +Z" (`:54`, `:123`).
  - X-1 `:61`, X-4 `:91`, X-6 `:125`; preview disqualifier `:174`.
  - All resolved.
- **Rows:**
  - R8 by segment types:
    - the types are a = S0→S, b = S→M, c = M→S, e = S→S0, f = S0→L;
    - depth counts 1, 2, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, cumulative 110 at depth 10 and 158 at depth 11;
    - so the cap keeps 110 + 18 = 128, the weakest segment carries 0.0103 W, and 11 of 16 interactions are used;
    - control 15. Fails on no cap, a larger cap or depth-first keeping.
  - R10: 1 split + 15 reflections; the 16th hit is B's 8th (CAT-041:53) ✓.
  - R1 direction (0, −1, 0) ✓. Pane half-thickness 0.015 m is admitted.
- **Anchors:** `current-cat-008` and `todo-294` are mapped (`:146`).
- **Findings:**
  1. **[Medium] `:133` R5, `:59`.** At pane-local (0.60, 0.60) in the 45° base fixture the beam enters the rail and is absorbed. "Raised 0.74 m" has no frame. Replace with: "Given the splitter square-on to the beam (not turned) and the Laser and straight receiver raised 0.74 m, then the beam ends on the top rail's near face and the receiver is dark. Given the base fixture with the Laser and both receivers moved by (−0.354, 0.354, 0.60) m, so that the beam meets the pane at pane-local (y 0.50, z 0.60), 0.78 m from the centre, outside the printed ring and 0.15 m inside the rail, then it is split as R1".
  2. **[Medium] `:135` R7, `:55`.** No datum; the 0.042 m chord exceeds ±0.02 m. Replace with: "Given the beam's entry point on the pane 6 m from the lens, then … | the reflected branch ends 10 m ± 0.02 m from the entry point and the transmitted branch 10 m ± 0.02 m from the exit point (each branch keeps the parent's remaining range from where it starts; the chord through the pane belongs to no segment)". Pair with the EL-176 §10 item 5 edit (X-7).
  3. **[Low] `:130` R2.** "Turned 180° about its own local Y axis (the pane keeps its plane and the beam meets its other face)".
  4. **[Low] `:134` R6.** "Splitter square-on, standing on a floor, default ball rolled along the pane normal".

## 4. What I could not verify

- Nothing was run. Every dynamics and optics figure is a hand derivation.
- How `ENGINE-SUPPLY-DEMAND` will treat demand above the per-tick allowance (X-1); whether the Battery's 100 W electrical node bounds a 300 W consumer (EL-068) or 120 W `MaxPower` the hatch (EL-109).
- Whether a delivered retention holds a passive gravity sag at Run start at all (X-2; the replacement rows hold either way).
- How `ENGINE-CONNECTION-MATERIALS` makes a strand sag between raised sockets and how a contact on a massless strand reacts on the span (EL-038 H-A12, EL-067 K12, EL-200 B13, EL-201 K11).
- Whether coincident static rails take a share of a prismatic body's weight (EL-109 finding 2).
- Whether the airflow material sweep counts a sample exactly on a collider face as occluded (EL-045 finding 1); whether airflow line of sight handles Torus segments (EL-072 B10).
- The order of supply charging and acoustic emission within a tick (CAT-061 `:79`); whether a native test can toggle the Battery's `enabled` for exactly one tick (CAT-061 row 2).
- The in-flight drafts `spec-12-4-pneumatic-bellows.md` and `spec-engine-axial-gas-nozzle.md` were not found in this checkout, so CAT-010's dependence on their open answers was taken from the spec.
- The CAT-041 corridor trace used by CAT-008 R10 was accepted as stated.
- Stability of the rim-tied 4 kg lift in CAT-059's replacement P5 (computed coupled inertia 0.3825 kg·m², binary spin-up about 0.28 s).
- Specs outside this group that share X-2 (CAT-071, EL-224, EL-226) were not opened.
