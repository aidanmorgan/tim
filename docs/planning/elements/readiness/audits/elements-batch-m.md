# Audit: element specs, Batch M (Construction, Environment, Goal, Gravity, Material and GAP; 29 specs)

Audited 22 November 2026 in checkout `spec-drafts` (HEAD `8d1d68aa` plus planning drafts; `origin/main` at `25df3cd5` read where stated). Read-only: nothing was built or run. Every spec was read. Code was opened for every "present" claim in the Goal, Environment, Material and GAP specs (11) and, by two parallel readers, for the eight delivered-part specs and the ten Construction specs. Spec paths are under `docs/planning/elements/`.

## 1. Verdicts

Conformance is against the seven points of the owner addendum. No spec conforms outright, because none applies owner decision 3a (finding 1).

| Spec | Readiness | Conformance |
|---|---|---|
| EL-112 Structural beam | Ready with fixes | Conforms with fixes |
| EL-113 Structural brace | Not ready (rig interpenetrates; one requirement clause has no row) | Conforms with fixes |
| EL-114 Placeable pivot | Ready with fixes | Conforms with fixes |
| EL-115 Linkage connector | Not ready (row L5 fails by its own numbers) | Conforms with fixes |
| EL-116 Passive wheel | Ready with fixes (not "deliverable now") | Conforms with fixes |
| EL-117 Axle | Ready with fixes | Conforms with fixes |
| EL-118 Coating station | Ready with fixes | Conforms with fixes |
| EL-119 Dye station | Ready with fixes | Conforms with fixes |
| EL-120 Quantity goal | Ready with fixes | Conforms with fixes |
| EL-121 Rate-window goal | Ready with fixes | Conforms with fixes |
| EL-122 Ordered-events goal | Ready with fixes | Conforms with fixes |
| EL-123 Protected-end-state goal | Not ready (held level keeps evaluator state) | Conforms with fixes (point 4) |
| EL-124 Authored gravity preset | Not ready (law not reconciled with the region source) | Conforms with fixes (point 2) |
| EL-125 Authored atmosphere preset | Ready with fixes | Conforms with fixes |
| EL-159 Tension-limited connector | Not ready (rig geometry impossible) | Conforms with fixes |
| EL-160 Shear-limited connector | Ready with fixes | Conforms with fixes |
| EL-161 Bending-limited connector | Ready with fixes | Conforms with fixes |
| EL-187 Basketball | Ready with fixes | Conforms with fixes |
| EL-188 Bowling ball | Ready with fixes | Conforms with fixes |
| EL-189 Tennis ball (closure) | Ready with fixes; no delta verified | Conforms with fixes |
| EL-190 Ramp | Ready with fixes | Conforms with fixes |
| EL-191 Wall | Ready with fixes | Conforms with fixes |
| EL-192 Receiving basket | Ready with fixes | Conforms with fixes |
| EL-204 Weight (closure) | Ready with fixes; no declaration delta verified | Conforms with fixes |
| EL-208 Balloon (closure) | Ready with fixes; no declaration delta, one acceptance gap | Conforms with fixes |
| GAP-04 Driven wheel | Not ready (reference vehicle does not stand) | Conforms with fixes |
| GAP-06 Granular dispenser | Not ready (sleeping marked present; blade spring; owner decision) | Conforms with fixes (point 6) |
| GAP-07 Granular sieve | Not ready (follows GAP-06; typed presets dropped) | Conforms with fixes |
| GAP-09 Fragmentation station | Ready with fixes | Conforms with fixes |

Counts: Ready 0; Ready with fixes 21; Not ready 8. Conformance: 29 Conform with fixes; 0 Do not conform.

## 2. Findings, by severity

### High

**1. Owner decision 3a is not applied in any of the 29 specs, and the reason given is false.**
- Where: point 6 of each conformance section (for example `EL-120-quantity-goal.md:153`, `GAP-06-granular-dispenser.md:160`); `readiness/batch-m.md:113`, `:151`. Twenty specs say the audit file "is not in this checkout".
- Evidence: `.tools/audit/engine-design-gap.md` exists in this checkout (stories at `:171`–`:247`). A search of the 29 specs finds no "3a" and none of the fixed story names.
- Replacement text, by pass: island sleeping "owned by `ENGINE-ISLANDS-SLEEP`"; residence and guide scans "owned by `ENGINE-SENSOR-VOLUMES`, candidates from `ENGINE-SPATIAL-QUERIES`"; airflow occlusion "`ENGINE-SPATIAL-QUERIES`"; contact events, contact loads and integrity observers "`ENGINE-CONTACT-OBSERVATIONS`"; drain "`ENGINE-SUPPLY-DEMAND`".
- Add to every Status and Delivery section: "blocked on the engine programme (owner decision 3a)".
- The summary's "12 yes" is wrong under 3a. `EL-116-passive-wheel.md:19` ("now") becomes "after the engine programme (owner decision 3a); no element-specific engine story". Whether tests-only patches and closure reviews may run first is owner item 7.

**2. GAP-06 marks island sleeping as present; it does not exist.**
- Where: `GAP-06-granular-dispenser.md:80` ("present, untested ... island sleeping"); counts "missing 0, owning engine stories: none"; `readiness/batch-m.md:39`–`:40` ("none", "yes").
- Evidence: no sleep state exists in `CuriousContraptions.Simulation` or `engine/gpu` (search returns nothing; `engine-design-gap.md:19`, `:44`). The summary itself says so at `batch-m.md:126`. The broadphase is rebuilt every substep (`engine-design-gap.md:18`) and the 2.5 ms tick target is recorded as failing (`:243`).
- Replacement row: "A resting pile of two dozen small spheres that stays still and sleeps | **missing** | none | `ENGINE-ISLANDS-SLEEP`". Counts become 6 / 1 / 0 / 1. Prerequisites: the engine programme, then a measured probe (finding 3).
- `GAP-06:160` "Tunnelling is excluded by the broadphase's velocity-fattened bounds" is not what the code does (fattening is by maximum speed, `engine-design-gap.md:18`). Replace with "by the engine's swept and speculative contact, qualified for spheres at 40 m/s (`PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels`)".

**3. GAP-06 and GAP-07 as rigid grains: a real scope decision, and not ready even if accepted.** Judged against the high-performance requirement and the rows:
- Cost today: 24 dynamic spheres in one pile is roughly 60 to 100 contact points in one island that never sleeps, with every pair retested each substep, plus 24 `CaptureTarget` payloads tested against every residence sensor each tick and against the Pressure plate's load sensor. No measured budget exists; "24" is a guess (`GAP-06:106`). The requirement asks for "performance limits" and the audit source for "a measured performance budget".
- Row fit: "discrete conserved material" and "real collision openings" are met. Not met: the visual "calm visible fill line" is dropped (`GAP-06:65`); S635's test "passes a wide opening and jams a narrow one" has no row (G3 and G4 jam on a blade and a Wall, not an aperture); the map row names GranularTransport, which `ENGINE-GRANULAR` owns in `epics.md:4532`.
- Verdict: the author was right to send it to the owner (`batch-m.md:82`). It also adds two catalogue parts under one identity, which the drafted S7 contract refuses (finding 9).
- Recommended default: accept rigid grains only as a conditional plan. Order: engine programme including `ENGINE-ISLANDS-SLEEP`; then a probe on a fixed reference scene (24 grains resting 5 s, then flowing) with before and after tick time, which sets the count limit; if the probe fails, both identities return behind `ENGINE-GRANULAR`. Add row "`ANarrowOutletJamsAndAWideOnePasses`" (a Wall narrowing the outlet to one coarse diameter jams; removed, it flows).

**4. GAP-06 blade cannot meet owner decision 6 with the declared spring.**
- Where: `GAP-06:39` (spring 14 N/m, "rest position closed"), row G9 at `:123` ("within 0.01 m of closed", three orientations), `:103`.
- Arithmetic: the blade is 0.5 kg on a slider along local X. Mounted on its side with +X down, its weight is 4.9 N and the spring gives 14 × x, so it hangs 4.9 / 14 = 0.35 m open. Under a full feed the drag is 9.0 N (`:103`) against at most 7.8 N of spring, so it cannot close either.
- Replacement: "Return spring 14 N/m, damping 4 N·s/m, declared rest position −0.50 m (0.50 m beyond closed), so the closing force is 7.0 N at closed and 14.8 N fully open: above the blade's 4.9 N weight in every orientation and below the 60 N drive." Recompute `:103` with 7.0 to 14.8 N.

**5. EL-123's held level makes the evaluator stateful; the committed read already carries what is needed.**
- Where: `EL-123-protected-end-state-goal.md:150` ("the evaluator keeps one hold start time per goal"), `:45`.
- Defect: goal truth must derive only from the committed read (`spec-engine-goal-evaluation.md:28`). The goal is woken by events, so a level that drops and returns between two wakes is invisible to a start time kept on the main thread.
- Evidence: `ResidenceRead` already has `Phase`, `StartOrdinal` and `StartPhase` (`CuriousContraptions.Abstractions/ResidenceRead.cs:7-8`), but it is not on `WorkshopRead` (`WorkshopRead.cs:6-11`).
- Replacement for `:45`: "A `held` entry is true while its committed level is true, and carries the committed occurrence at which it last became true. The goal is Solved in the first committed read whose time is at least `hold_seconds` after the latest of: every latch occurrence and every held entry's became-true occurrence. The evaluator keeps no state; a time wake is scheduled for that due time." Replace `:150` with "the evaluator keeps nothing between reads".
- Control P2 (`:107`) disables the sensor, so the level is never true. Add a simulated control: a Basketball knocked out of Receiver B during the hold leaves the goal Waiting.

**6. The fifth goal slice contradicts the owner's four-slice decision and is not listed for the owner.**
- Needed: yes. `epics.md:4480` names all-of, any-of, ordered and forbidden only, and the contract leaves quantity, rate and protected state with no slice (`spec-engine-goal-evaluation.md:40`, `:139`).
- Element-free: yes. A count group, a time window and a held level name no part. A goal stays declared data on one evaluator reading committed state once finding 5 is applied; nothing feeds back.
- Defects: (a) `batch-m.md:84` says "No other item needs the owner" while `:92` adds a slice to a decided list; (b) any-of is the count group with k = 1, so delivering any-of in `engine-goal-ordered-events` and the group one slice later rewrites a form that just shipped; (c) the held level changes the committed read, a different shared contract from the evaluator-only count and window, which the contract's own split rule separates.
- Decision needed: owner item 1.

**7. EL-124: scene gravity and a bounded field region are two rules as drafted.**
- Where: `EL-124-authored-gravity-preset.md:40`, `:65`; `batch-m.md:93`.
- Batch M: a compile-time scale of every body's declared gravity and every ambient-load acceleration by `gravity ÷ 9.81`. Batch L (EL-089, `readiness/batch-l.md:115`): a bounded region adds "an acceleration vector ... mass-independent" to bodies inside. A Balloon's buoyancy scales with gravity under the first and not under the second. `epics.md:4448` covers neither.
- Calling `engine-scene-environment` the first slice of `ENGINE-FIELD-FORCE` shares a name only; Batch N also adds relative humidity to it (`readiness/batch-n.md:131`).
- One law (proposed story text in section 4): a uniform acceleration field with a region that is unbounded (the scene) or bounded; one buoyancy rule for both.
- Otherwise the spec is sound: `WorkshopConstruction.Gravity` is a static constant read by two tests (`engine/gpu/WorkshopConstruction.cs:15`), the 16 m/s² bound is at `WorldAccelerationDeclaration.cs:25`, and the engine holds no other 9.81.

**8. Construction (detail from the Construction reader; numbers checked there).**
- *Names against 3a.* The fixed joint and "a hinge on a dynamic support" belong to `ENGINE-JOINT-KINDS` (`engine-design-gap.md:229`), not `ENGINE-ASSEMBLY-JOINTS`: `EL-161:63`, `EL-114:70`, `EL-117:66`, `:68`, `EL-159:63`, `EL-160:63`, `GAP-04:75`, `:78`, `:81`. Parameter-bound socket positions and revolute limits belong to `ENGINE-PARAMETER-BOUND-FRAMES`: `EL-112:74`, `EL-113:70`, `EL-115:63`, `EL-114:69`. `EL-114:69`'s claim that `epics.md:4459` covers limits is false.
- *No collision rule for assembled parts.* `EL-113:93`: brace and linkage share `EndB` and overlap. `EL-117:29-31`: the shaft overlaps the beam end. `EL-117:111`: unattached wheels sit with the shaft through them; replace with "with the four Wheels removed it rests on its collars at 0.15 m".
- *EL-159 rig impossible* (`EL-159:89`, reused at `EL-160:89` and `EL-161:97`): `Base` is on the plinth's top, so the hung beam passes through the plinth. Add to `EL-114:32` "`Under`: a point at the centre of the underside, for hung assemblies" and fix `MountA` to it.
- *EL-115 row L5* (`EL-115:106`): the 0.2 kg link gives 1.96 N·m against 0.4 N·m of bearing friction, so the unloaded rig moves about 0.6 m in a second, not under 0.005 m. Replace with: "Given a link hanging from one connected pin beside its second point, 0.0 m apart, when the second connection is made and Run starts, no part moves more than 0.005 m in the first second." L1's boundary (`:102`) has a 4 % margin; use the 1 kg Weight.
- *GAP-04 vehicle* (`GAP-04:105`–`:113`): one driven and one passive wheel sit on one side of the beam, a Bowling ball rolls off a 0.30 m beam, the Passive wheel has no Axle, and a 3 m Ramp cannot raise the load 0.2 m. Recommended: beam, one Axle with two Passive wheels at `EndB`, two Driven wheels at `EndA` (the mirrored one at `speed` −8), a 4 kg Weight on `Mid`, a 4 m Ramp; recompute. Also `:185` Mode records `Configuration=Adjustable`; `:46` `mount_top` coincides with `PowerIn`; `:170` no "fatten" capability exists.
- *Summary acceptance* (`batch-m.md:90`): "a 2 m, 1.5 kg cantilever with 93 N·m" is wrong; the beam alone gives 14.7 N·m. Replace with "carrying 4 kg at its free end (93.2 N·m at the root) within 0.02 m for 5 s".
- Hand check of one pin-ended spec (EL-113: 0.996 m bay, 10.2 and 0.95 N·m) and one connector (EL-159: 14.7, 34.3, 93.2 N): arithmetic correct; the rigs are the defect.

### Medium

**9. Companion packages conflict with the drafted S7 contract.** `spec-s7-coverage-named-identity-admission.md:176` refuses "two packages under one identity". GAP-06 plans three and GAP-09 two. The coverage claim itself is verified: `origin/main:tools/Coverage/NamedPackageSources.cs` lists only `EL-*.md`, its pattern admits only `EL-([0-9]{3})` with an `element-NNN` anchor, and `ValidateUnique` refuses a second package on one identity. No Batch M spec contains the binding heading. Owner item 3.

**10. A Run ends at 30 s; three goal specs use longer windows and guards.**
- Evidence: `CuriousContraptions.Abstractions/CadenceValues.cs:77` (`RunTickLimit` = 30 s); `ImpactLeverTests.cs:210`.
- `EL-121:37`–`:38` admit windows to 120 s and row R5 (`:105`) admits "end exactly 120": such a goal can never be solved. Replace the bound with "`from_seconds` + `length_seconds` ≤ the Run limit (30 s today, read from the cadence settings); a later end is refused". Same for `hold_seconds` (`EL-123:41`).
- Replace "60 s guard" with "the whole Run (30 s)" at `EL-121:108`, `EL-122:114`, `EL-123:113`.

**11. ENGINE-SURFACE-STATE adds a second goal condition form; none is needed.**
- Where: `batch-m.md:94` ("one optional required-tag qualifier on the goal evaluator's capture observation"); `EL-119:30`, `:60`, row B1 at `:89`.
- A treatment lasts until Reset, so it is a first-occurrence latch like every other observation. Replacement: "The treatment publishes a committed first-occurrence observation (treated body, declared local treatment id, occurrence). The goal list selects it as one more observation kind, alone or as a predecessor; no qualifier is added to another kind." B1 becomes all-of {`Captured`, `Treated`}.
- This also closes a weakened criterion: `sequence-task-541` requires "treated cargo satisfies a downstream material-state goal", and EL-118 row A8 (`EL-118:117`, `:125`) uses only a physical capture. Add to EL-118: "A9: a goal of {`Captured`, `Treated`} is Solved for the treated Domino and Waiting for the untreated one."
- Material model in code: four f32 values per collider (restitution, friction, rolling resistance, bounce threshold; `ColliderDescriptor.cs:22-25`), written once at ingest (`WorkshopPhysicsEngine.cs:532-535`), bindable to a parameter per body (`MaterialParameterBinding.cs:5-8`), pair friction a geometric mean and restitution a product (`WorkshopConstraintSolver.cs:117-119`). There is no named material identity, and the specs rightly declare values (0.05) and a typed tag, not names. The story is stated generally. It should add: contacts come from `ENGINE-CONTACT-OBSERVATIONS`; per-body state is a fixed-width lane; "a store's committed fraction is a presentation driver" (`EL-118:74` assigns that row to the story, whose text omits it).

**12. EL-118 A1 and A8, and EL-119 B1, cannot be built as written.** "rested 1 s on the station floor, when it is then at rest on a 15° Ramp" (`EL-118:110`) needs the body moved during a Run. The station floor has the Ramp's friction (0.3), so one fixture does it. Replacement for A1: "Given a flat Domino on a station tilted 15°, when Run, then it rests for the 0.5 s dwell (pair friction 0.424, sliding above 23.0°), is treated, and slides more than 0.5 m within 3 s (0.122, sliding above 7.0°); control: `supply` 0, it moves less than 0.02 m." A8 and B1 use the same tilted station feeding a Receiver.

**13. GAP-09: the load measure is undefined and the control margin depends on it.**
- Where: `GAP-09-fragmentation-station.md:56`, `:112`, row F2 at `:126`.
- With 90 N on the head, the block's lower face carries 90 + 4.9 (head) + 15.7 (block) = 110.6 N, 8 % under the 120 N limit, not 25 %. If the measure summed both faces it would be 205 N and the control would break. The drive acts with the head's weight (`MechanicalContractValues.cs:82-83`).
- Replacement: "The measure is the largest normal load carried by any one contact of the body." Control `force` 80 N (100.6 N, 16 % under); positive 150 N unchanged.
- `:159` says an impact breaks the block "by the same law" while the only measure is held 0.25 s; delete that sentence or state that a transient never qualifies.
- `:161`: S641 says an unsupported material "is refused"; the spec's control is a body that never breaks. State which.
- The proposed `ENGINE-FRACTURE` additions are general. Unify them (section 4).

**14. GAP-07 drops "typed presets" and its travel arithmetic omits the groove.**
- `GAP-07:42`, `:133`: the row says "aperture choices use typed presets"; the spec ships a continuous Real and calls the Choice binding "not a prerequisite". Under 3a the owner is `ENGINE-PARAMETER-BOUND-FRAMES`; make it a prerequisite, or take owner item 5.
- `:93`: a coarse grain rides two rods. Each contact normal is 57° from vertical, so the total normal load is 1.85 times the weight component and the rolling radius is 0.0675 m. Resistance is about 1.76 m/s² against a 2.54 m/s² pull, the rolling factor is 0.42 not 5/7, and the 0.9 m takes about 2.3 s, not 1.3 s. The margin is thin; use a 25° tilt for S1 or state the margin. The pair's rolling resistance is the larger of the two (`WorkshopConstraintSolver.cs:231`), which the spec lists as unknown.

**15. EL-120 depends on the origin-point residence test that 3a deletes.** `EL-120:98`. `ENGINE-SENSOR-VOLUMES` replaces it with shape overlap "as the declaration states" (`engine-design-gap.md:192`–`:197`). Add: "The Receiver's sensor declares centre containment; with shape overlap a third Basketball resting on two would count." Same for EL-192 rows A1 and A2.

**16. Delivered parts (detail from the delivered-parts reader).** All 83 cited tests exist at the cited lines; the eight packages are identical on HEAD and `origin/main`; no engine code names them.
- *EL-192* coins "Frame-relative sensors" (`EL-192:22`, `:52-53`, `:94`, `:110`, `:129`, `:134`; `batch-m.md:35`): use `ENGINE-SENSOR-VOLUMES`. "No refactor is owed" (`:129`) is wrong: the package implements the single-instance `IResidenceSensorElement.ResidenceSensor` and `IPlanarGuideElement.PlanarGuide` (`MechanicalContracts.cs:25-33`), which that story deletes (E6). Row A7's test is a placeholder; name it in the story.
- *EL-208 closure*: "responds to mass" has no test; every Balloon test changes mass with buoyancy. Add `BalloonTests.AHeavierBalloonAtTheDefaultLiftRupturesOnTheCeilingThatHoldsTheDefault` (2 kg gives about 10 N·s against the 3 N·s threshold). Adding a test in a closure review is allowed (`_bmad/custom/bmad-loop-protocol.md:45`).
- *Ball closures inconsistent*: EL-187 and EL-188 stay open on "buoyancy" as liquid float; EL-189 closes on the same field as ambient lift (`EL-187:20`, `EL-188:23`, `EL-189:20`). Owner item 6.
- *EL-191 row A2* (`:73`, `:82`): the passing ball at x = 1.95 m is outside the 1.5 m pick radius, so the row does not test silhouette bounds. Add: wall yawed 45°, ball passing 0.6 m in front of the face, deflection under 0.05 m.
- *EL-204*: the CAT-067 row still says "spherical body/socket" (`requirements.md:858`); the part is a cylinder. `EL-204:60` says no test reads art scale against `mass`; `VisibleSolidColliderTests.cs:176-182` does.
- *"No refactor is owed"* (`EL-208:139`, `EL-187:124`, `EL-188:122`, `EL-189:133`) omits two product-shaped contracts these parts use: the sphere-only gas store (E10, `ENGINE-GAS-NETWORK`) and `AllPayloads` with `CaptureTarget` (E12, `ENGINE-CAPABILITY-INSTANCES`).

**17. Construction, medium.**
- "Sustained" has no number for the connectors (`EL-159:31`, `EL-160:31`, `EL-161:31`) while rows run at 20 to 24 % margins.
- `EL-113` has no row for "Moving/rotating a member updates real contact and connected mass" (`requirements.md#sequence-task-534`). Add `PinEndedMemberTests.AMovedMemberCarriesItsContactAndLoadPathWithIt`.
- The requirement's "selected-only" arc and stress cue are drawn always visible (`EL-114:57`, `EL-159:51`, `EL-160:51`, `EL-161:51`) with no owner item. Owner item 4.
- "Sockets are typed" is not true today: `AnchorPointDescriptor.Name` is a string (`MechanicalContractValues.cs:63`).
- Rigs omit pivot heights and rope lengths; a 4 kg Weight is 0.35 m in radius (`WeightElementDeclaration.cs:63`) and overlaps the plinth in `EL-113:93`, EL-114 row P3 and EL-160 rows S1 and S2. Add "ropes 1.5 m; each plinth underside at least 2.6 m above the bench".

**18. EL-125.** The code claim holds: `AtmospherePreset { Standard, Vacuum }`, the construction field, compile into gas stores, ambient loads and airflow, the Build-only picker and the save refusal are on this checkout and on `origin/main` (`AtmosphereBoundary.cs:6-15`; `WorkshopConstruction.cs:8-9`, `:21`; `WorkshopPhysicsCompiler.Gas.cs:21-38`; `WorkshopPhysicsCompiler.Airflow.cs:47`); all ten cited tests exist, and `BalloonTests.cs:88` does call Reset.
- `:36` weakens "set actual pressure and density" to a ratio. Owner item 5.
- `:39` keeps an engine enum while EL-124 argues for a declared value, and one slice then ships both forms. Convert the atmosphere to a declared row in `engine-scene-environment`, or record why not.

### Low

**19.** `EL-118:74` and EL-119: counts are right, but the "fill binding" test was not opened and has no owner sentence in the story (finding 11).
**20.** `EL-124:44` drops direction while the gap-18 visual names "direction ... symbols"; state that the arrow always points down. `EL-124:118` leaves "export" open with GAP-12, correctly.
**21.** EL-118 row A3 and EL-119 row B4 separate 0.40 s from 0.64 s of rolling contact around a 0.5 s dwell; the ball must first climb the 0.06 m floor. Use 5 m/s and 2 m/s.
**22.** `GAP-09:70`: the press rod is art stretched by a binding across a space a ball can enter when the head is down. It follows the delivered Linear pusher; record it with that part's visible-solid status.
**23.** Delivered parts: `EL-189:100` specify `drag` 0 (the default 0.04 lowers the apex to about 2.2 m); `EL-190:81` the +20° boundary is trivially true, use an uphill arrival at 1 m/s rising no more than 0.071 m plus 0.02 m; `EL-190:46` cites a sliding-impact test for rolling; `EL-187:20` palette order is at `BallElementDeclaration.cs:20`; `EL-187:52` and `EL-188:51` name EL-125 as an owner, use `ENGINE-FIELD-FORCE` (`engine-scene-environment`).
**24.** Construction: `EL-114:41` a "free" pivot on `Limited` travel stops at ±180°, state it; EL-116 row W1 boundary needs "centre within 0.05 m, speed under 0.01 m/s after 5 s"; EL-161 row M5 is an architecture guard, label it; `GAP-04:112` coast is 0.28 m/s², not 0.29.

## 3. Summary table against the specs

Capability counts and dispositions match all 29 specs. Wrong in substance: GAP-06 and GAP-07 "none" and "yes" (finding 2); EL-116 "yes" (finding 1); EL-192 owning story (finding 16); "No other item needs the owner" (`batch-m.md:84`; section 5); gap 15 and section 8 on the audit file (finding 1); gap 1's cantilever sentence (finding 8).

## 4. Engine-story gaps, with proposed text (all element-free)

- **ENGINE-GOALS.** "`engine-goal-ordered-events` delivers any-of as a count group (at least k of n declared conditions; occurrence is the k-th earliest; one subject once per group). A following slice adds a half-open game-time window from Run start on a condition or group, refused when it ends after the Run limit. A held level is a committed state with the occurrence at which it last became true, published on the committed read; the goal holds when the read's time is `hold_seconds` past the latest latch and became-true occurrence; the evaluator keeps no state and schedules one time wake." Acceptance as `batch-m.md:92`, plus "a level that drops and returns between two wakes restarts the hold".
- **ENGINE-FIELD-FORCE.** "A uniform acceleration field declares a vector and a region: unbounded (the scene environment, one validated f32 from 1.0 to 16.0 m/s² along −Y) or a bounded volume. The field at a body is the sum of the fields whose region contains its centre of mass. A body's declared gravity is its response to the Standard field; a declared buoyant ambient load scales with the same field, inside a region and in the scene alike. The scene field is folded into each body's acceleration at compile and costs nothing per tick; bounded regions take candidates from `ENGINE-SPATIAL-QUERIES` and their work is bounded by the bodies inside." Acceptance: `batch-m.md:93`, plus "a Balloon rises in a doubled-gravity scene and in a doubled-gravity region at the same rate". Batches L and N reconcile to this text.
- **ENGINE-SURFACE-STATE.** As `batch-m.md:94` with three changes: replace the capture qualifier with the committed `Treated` observation (finding 11); add "contacts come from `ENGINE-CONTACT-OBSERVATIONS`; per-body state is a fixed-width lane compiled at Run"; add "a store's committed fraction is a presentation driver". The name is new; 3a's list has none for this capability.
- **ENGINE-FRACTURE.** "Every load limit declares a measure from one closed set (axial tension, shear, bending moment, resultant, contact normal: the largest normal load on any one contact of the body) and a hold time of zero or more; the break occurs when the measure has exceeded the limit on every substep for the hold, and the settling transient at Run start does not count. Declared fragments are body and collider rows compiled at Run and inactive until the break; inactive rows take no part in the broadphase, sensors or the committed body set." Keep the rest of `batch-m.md:91`. Batch J gap 6 and Batch N gap 5 reconcile to this.
- **ENGINE-JOINT-KINDS.** "A fixed joint holds a 2 m, 1.5 kg member carrying 4 kg at its free end within 0.02 m for 5 s. Every joint kind is qualified with a dynamic support body as well as a static one."
- **ENGINE-PARAMETER-BOUND-FRAMES.** "A socket position and a revolute limit may each follow one declared Real parameter; a Choice parameter may select one declared value per option for a collider component."
- **ENGINE-ASSEMBLY-JOINTS.** "Sockets carry a typed id and a role enum (mount, point). Bodies of two parts joined by an attachment do not collide with each other, including bodies whose mounts share one point; all other pairs collide. A closed loop of three pinned members holds within 0.01 m for 10 s."
- **ENGINE-SENSOR-VOLUMES.** "A sensor declares its occupancy rule (centre containment or shape overlap)."
- **ENGINE-ISLANDS-SLEEP.** Add to Unblocks: "piles of small bodies (GAP-06, GAP-07, GAP-09 fragments); the grain count limit is set from this story's measured reference scene."

## 5. Items for the owner

| # | Item | Recommended default |
|---|---|---|
| 1 | A fifth goal slice changes the four-slice decision (finding 6) | Accept one more slice, `engine-goal-window-hold`. Deliver the count group inside `engine-goal-ordered-events` as its any-of form. Split the held level into its own slice if its committed-read change alters the worker read ABI |
| 2 | GAP-06 and GAP-07 as rigid grains, the dropped fill line, and a 24-grain limit with no measurement (finding 3) | Accept conditionally: after `ENGINE-ISLANDS-SLEEP`, with the limit set by a measured probe; otherwise behind `ENGINE-GRANULAR` |
| 3 | Companion packages (`grain_fine`, `grain_coarse`, `breakable_block`) against S7's one package per identity (finding 9) | Give each companion its own ledger identity, as was done for EL-217 to EL-241; S7's rule stays |
| 4 | "Selected-only" arc and stress cue drawn always visible (finding 17) | Accept always visible |
| 5 | EL-125 density as a ratio (finding 18); GAP-07 presets as a Real if `ENGINE-PARAMETER-BOUND-FRAMES` is not made a prerequisite (finding 14) | Accept the ratio and reword the row; make the story a prerequisite for the sieve |
| 6 | What "buoyancy" means for closing the three ball identities (finding 16) | The declared ambient lift; liquid float is `ENGINE-LIQUID`'s own acceptance, so EL-187 and EL-188 close with their one test |
| 7 | Whether tests-only patches and closure reviews of delivered parts may run before the engine programme (finding 1) | Yes; each closure check is re-run after `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP` |

## 6. Cross-batch conflicts

- Batch L (EL-089 region acceleration) and Batch N (humidity on `engine-scene-environment`) against EL-124 (finding 7).
- Names against 3a: Batch C `ENGINE-ISLAND-SLEEP` (`readiness/batch-c.md:74`) and Batch A "Frame-relative sensors" against `ENGINE-ISLANDS-SLEEP` and `ENGINE-SENSOR-VOLUMES`; Batches L and M both claim the audit file is absent.
- `engine-design-gap.md:144` lists EL-115 as a spherical-joint consumer; `EL-115:131` decides planar pins. Keep the spec and remove EL-115 from that row.
- `general-engine-element-map.md:431-432` name GranularTransport for GAP-06 and GAP-07, and `:223-224` FluidAdvection for EL-118 and EL-119; the specs use neither. The map rows need editing with whatever the owner decides.
- CAT-066's "inside a 1.5 m pick radius" is false (finding 16); CAT-001 and CAT-014 on ambient drag, and CAT-064 and CAT-003 on sleeping, as `batch-m.md:126` records.
- No package id in the batch collides with a delivered `Metadata.Id` or another readiness summary; mount and point roles agree with EL-185 and EL-186.

## 7. Not verified

- Nothing was simulated; every dynamics conclusion above is a hand calculation.
- Test bodies were opened for the goal evaluator, residence read, atmosphere save and Reset, and the Construction laws listed by that reader; most other cited tests were confirmed by class, name and line only.
- Whether publishing the residence phase on the committed read changes the worker ABI; whether a part may be placed inside the hopper; whether coarse grains bridge a two-diameter outlet; whether a static cylinder collider and a `Radius` binding on a cylinder are admitted; whether the prismatic drive's force bound is net of the head's weight.
- Whether a mechanical input may couple a coordinate that also has a supplied drive (GAP-04); whether the bench is unbounded for EL-116's 9.5 m roll.
- Campaign levels 17, 25, 29, 49, 79 and 89 are not authored. `DESIGN.md` colours and icons were not re-read.
- Requirement rows for EL-113, EL-115, EL-116 and EL-117 were taken from the specs' quotations.
