# Audit: element specs, Batch A (16 delivered catalogue parts)

Auditor: independent, read-only. Checkout `spec-drafts` (`8d1d68aa` plus drafts); published main read with `git show origin/main:` (`25df3cd5`). Nothing was built or run.

**Method.** All 16 specs and `readiness/batch-a.md` were read. Every test the specs cite as existing was checked to exist by name (script over all 16 specs). Bodies were opened for: `LaunchLessonSolveTests` (whole theory and scenario), `WorkshopPhysicsLawHollowTests` (package passage, zero-restitution and fast axial rows), `BasketballResourceTests` halo test, `VisibleSolidColliderTests` hand table, `WorkshopPhysicsEngineTests` ramp and Domino-strike rows, `ImpactLeverTests`, `FanGuidanceTests`, `WorkshopSpringboardTests` lesson test. Declarations opened: Ball, Receiver, Signal lamp, Domino, Bumper, Pipe; scenes: Ball detector, Springboard. Engine opened: `WorkshopPhysicsEngine.CommitResidence`, `WorkshopConnections.ValidateActivation`. Elsewhere tests are confirmed by name only.

## 1. Verdict per spec

| Spec | Readiness | Engine conformance (addendum, 22 Nov) | Main findings |
|---|---|---|---|
| CAT-001 Basketball | Ready with fixes | Conforms with fixes | L1, C-all |
| CAT-002 Ball detector | Ready with fixes | Conforms with fixes | M6, C-all |
| CAT-004 Receiver | Ready with fixes | Conforms with fixes (engine gap G1) | H1, M1, C-all |
| CAT-014 Bowling ball | Ready | Conforms with fixes | C-all |
| CAT-015 Bumper | Ready with fixes | Conforms with fixes | H4, M7, C2 |
| CAT-022 Delay box | Ready with fixes | Conforms with fixes | M7, M8 |
| CAT-023 Domino | Not ready | Conforms with fixes | H3, M4, M5 |
| CAT-030 Funnel | Ready with fixes | Conforms with fixes | M2 |
| CAT-035 Signal lamp | Ready with fixes | Conforms with fixes | M7, C3 |
| CAT-048 Clear pipe | Ready with fixes | Conforms with fixes | M2, M3 |
| CAT-049 45° bend | Ready with fixes | Conforms with fixes | H5, M2 |
| CAT-050 90° bend | Ready with fixes | Conforms with fixes | H5, M2 |
| CAT-054 Ramp | Ready with fixes | Conforms with fixes | M9 |
| CAT-062 Springboard | Ready with fixes | Conforms with fixes | H2, M10 |
| CAT-063 Impact switch | Ready with fixes | Conforms with fixes (owner item O1) | H2, C1, M7 |
| CAT-066 Wall | Ready | Conforms with fixes | C-all |

Counts: Ready 2, Ready with fixes 13, Not ready 1. Conformance: 16 Conform with fixes, none Does not conform. No engine code names or special-cases any of the 16 packages (section 5).

## 2. Findings, by severity

### High

**H1. CAT-004:65, :122, :132, :149 and `readiness/batch-a.md:13`, `:61`, `:84` — residence on a moving owner is not "present, untested"; the speed test is in the world frame.**
- Evidence: `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:909-912` builds the sensor pose from the frame body's current pose (so position is in the moving frame), but `:933` is `var speed = targetBody.LinearVelocity.Length();` and `:935-938` compare that world speed with the limit. The requirement row says "sampled at substep endpoints in the basket's moving frame" (`requirements.md` `current-cat-004`).
- Consequence: a ball riding a carrier faster than the limit can never be captured, and a ball at rest in the world that a carrier sweeps over has speed 0 and counts dwell. The spec's own planned control at `:122` ("a ball the container passes over at the same relative speed above the limit is not [captured]") would fail on the delivered engine.
- Replacement `:65`: status **narrower**: "position is evaluated in the frame body's current pose (`WorkshopPhysicsEngine.cs:909-912`); the speed limit reads world speed (`:933`), not speed relative to the frame. Owner: engine story G1 (section 4), before EL-069, EL-070 or EL-062." Counts become 8/0/1/1. `:132`: replace "the engine already evaluates the guide in its frame body's current pose" with the same sentence. Readiness row: prerequisites "engine story G1, then a carried-owner slice"; delete "Whether the Receiver's residence commit reads the owner's current pose" from Not verified.

**H2. CAT-062:112, :127 and CAT-063:91, :113; `readiness/batch-a.md:24`, `:25`, `:63` — EL-194 and EL-198 are not Duplicate closures.**
- EL-194: `requirements.md:5955` makes "typed signal integration" and "all fifteen composite lane instances" EL-194 qualification. `spring_signal` is not Playable; `LaunchLessonSolveTests.Lessons` (`:89-95`) plays only `spring_forward` and `ready_to_rebound`. The Batch J audit (H9) reached the same conclusion and `EL-194-springboard.md:19-20` already says "two level criteria stay open". CAT-062:33 itself says "Composite campaign lanes ... remain owed".
  - Replacement `:112`: "EL-194: preload and `spring_forward` are delivered, so the part has no delta. The part-level check is recorded in the 'Launch parts' review; the identity stays open until `spring_signal` and the fifteen composite lanes pass (after ENGINE-GOALS). It is not closed as a duplicate before then." `:127`: "then the EL-194 part check".
- EL-198: the row is "Physical **or author-set** contact state" (`requirements.md:5985`). `EL-198-switch.md:16-17` (Batch H) states "Not a duplicate": the author-set state is a `contact` Choice needing engine slice `engine-supply-parameter-contact`; the Batch H audit judged that decision defensible.
  - Replacement CAT-063:91: "EL-198: the physical, separately supplied contact is delivered by Story 9.5b. The author-set contact state is a variant residual owned by `EL-198-switch.md` and the proposed `engine-supply-parameter-contact` slice; EL-198 is not a duplicate of this part." `:113`: delete "then the EL-198 closure review".
- Readiness `:63`: replace with "EL-195: candidate closure (see H4). EL-194: part check only; identity open on `spring_signal` and the fifteen lanes. EL-198: variant, not a closure."

**H3. CAT-023:108, :126 and `readiness/batch-a.md:17`, `:52` — the offset centre of mass contradicts EL-193 and the existing story CAT-023c.**
- `epics.md:257-260` already holds story CAT-023c (`cat-023c-offset-centre-of-mass`), which owns the offset. `EL-193-domino.md:19` and `readiness/batch-j.md:61` decide (0, −0.05, 0) m under D1; the Batch J audit lists it as owner item O5 with "accept". CAT-023 recommends striking the phrase, gives the alternative with the opposite sign ("0.05 m above"), calls the slice "element tier" (CAT-023c touches the mass-property, compiler, ABI and motion-read callers: engine tier), and its readiness row says "Owning engine stories: none".
- Striking "with offset centre of mass" drops a requirement criterion and also contradicts the EL-193 row ("mass and centre of gravity", `requirements.md:5943`). It may be offered to the owner, but not as the default while another spec builds the opposite.
- Replacement `:108`: "Offset centre of mass: owned by story CAT-023c. Value (0, −0.05, 0) m in the tile frame, as `EL-193-domino.md` (D1); homogeneous moments kept. Owner confirmation is item O5 of the Batch J audit; the alternative is to strike the phrase from the CAT-023 and EL-193 rows." `:126`: "Residual: CAT-023c (engine tier, six layers); it re-authors `WorkshopDominoTests.cs:46` (`Assert.Equal(default, properties.LocalCentreOfMass)`) and re-runs `DominoGuardTests`." Readiness row: owning story CAT-023c; ready "yes, to plan CAT-023c; owner confirms the value".

**H4. CAT-015:111, :124 against `EL-195-pinball-bumper.md:19` — the two specs order the EL-195 closure differently.**
- CAT-015 closes EL-195 only after Story 15.0's assistance control; EL-195 closes it now in the "Launch parts" review. The ledger says EL-195 "also retains CAT-015 acceptance" (`named-elements.md:1785`).
- Decision needed once (planning, not owner): recommended "EL-195's part check runs now in 'Launch parts'; the CAT-015 row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control." Put the same sentence in both specs.
- Also carry the Batch J finding H10 here: CAT-015:26, :53 should say the `coil` node's 50 W is metadata and does not bound charging (`ContactWorkInstanceTests.SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts`); the refill rate is the source's `power_limit`.

**H5. CAT-049:67, :124 and CAT-050:65, :133 — the "played to its goal" rows are already tested; the planned tests duplicate them.**
- `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` runs `gentle_bend` and `quarter_bend` (`Lessons`, `:89-95`) at 60, 120 and 240 Hz, with the control removing the first unlocked instance (the bend, the only inventory part), then Reset and a save/load replay (`:111-176`). CAT-001:108 cites exactly this for both lessons, so the batch contradicts itself.
- Replacement: mark both rows **present** with that test; delete planned `PipeBend45Tests.GentleBend_TheAuthoredSolutionDeliversTheBallAndWithoutTheBendItDoesNot` and `PipeBend90Tests.QuarterBend_TheAuthoredSolutionDeliversTheBallAndWithoutTheBendItDoesNot` (AGENTS: no reruns without an uncovered failure). Counts: CAT-049 6/1/0/2, CAT-050 5/2/0/2; fix the readiness table.

### Medium

**M1. CAT-004:133, :146 — the halo repair's file list is incomplete.** Removing the event-driven `UniformScale` binding (binding 0) shifts the halo's red channel from binding 1 to binding 0; `LaunchLessonSolveTests.cs:205-206` selects `sample.Binding == (capture ? 1 : 0)` and its comment says "Receiver halo red is binding 1". Add to `:146`: "also edits `LaunchLessonSolveTests.AssertGoalCue` (capture cue becomes binding 0)". The proposed `PresentationParameterBinding("Visual/Visual_Halo", UniformScale, "capture_margin", 1, 1)` matches the record (`ElementPresentation.cs:119-120`, multiplier then offset) and the Weight precedent. The decision itself is sound and inside the delegation: `DESIGN.md:337` and the catalogue description require it. Note for the art table: at margin 1 the halo doubles (outer radius 1.93 m); it stays decorative (glow).

**M2. CAT-048:152 (and CAT-049:131, CAT-050:132, CAT-030:125); `readiness/batch-a.md:34`, `:59` — flush rails: sound, but not buildable as written.**
- Soundness: the requirement rows say "preserve its own icon/rails/collars" and "matching collars/rails"; `DESIGN.md:349` says "two thin navy rails"; Story 9R.19 says the parts "carry the navy rails". None requires a rail to be solid. Visible = solid allows flush. A flush rail is still a rail, so no criterion is weakened, provided 9R.19 keeps "carry the navy rails" and adds the flush rule (the readiness text does).
- Gap: "a navy stripe printed on the shell" gives no mesh, size or position for a cylinder, a torus segment or a frustum, and no binding to `length`.
- Replacement text: "Each rail is a navy `#293954` cylinder of radius 0.018 m centred at radial distance 0.675 m, so it lies wholly inside the 0.65 to 0.70 m glass wall and is seen through it; classified decorative, enclosed (by the shell's hollow collider). Pipe: two rails at z ±0.675, scaled with `length` by the same `ScaleY` binding as the shell. Bends: one chord per shell chord at the same radial offset. Funnel: two rails along the taper at mid-wall. `VisibleSolidColliderTests` lists them as decorative." If the author prefers a surface print, state its mesh instead; either way give numbers.

**M3. CAT-048:143-144, :165 — Story 9R.23's clause for Story 6.7 is not named.** `review-9a-pkg-2.md:159` lists for 6.7 "high entry speed and edge angles (only one pipe pose; no speed assertion)". The spec's residual list has only the run-state refusals. `FastAxialLaunchPassesOnlyTheBoreOrClearAir` (`WorkshopPhysicsLawHollowTests.cs:815-848`) proves a 64 m/s rotated pass on a bare law-scene Tube, not on the `pipe` package with its collars. Add to `:144` either "(e) entry speed and angle: proven by `FastAxialLaunchPassesOnlyTheBoreOrClearAir` (Tube law) and `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload` (package colliders, turned −90°)", or one package row. The 1,200-tick bound of 6.7 is correctly not carried (game-fidelity policy); say so. For CAT-049 and CAT-050 name `ZeroRestitutionPackageControlDoesNotReturnImpactEnergy` (`:682-725`), which drops a ball on each package's collar, as the rim and sidewall proof instead of "reuse the law tests". The replacement of Chrome proofs by native tests is sound: AGENTS forbids a browser gate before the catalogue is complete, and the Chrome proof is queued, not dropped.

**M4. CAT-023:30 — false statement.** "The package's `toppled` trigger (0.707) and `SetReset` latch mode are inert metadata" — `elements/Domino/DominoElementDeclaration.cs` contains neither. Delete the sentence.

**M5. CAT-023:34 — cited test does not exist.** `LaunchLessonSolveTests.TheAuthoredSolutionSolvesAtEveryPrecision` is not in the test project. The current test is `AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal`, which runs precision 0.45 only, at three cadences. Replace, and drop "at every precision" unless `DominoGuardTests` is cited for it.

**M6. CAT-002:22 — stale current-state text.** "Current (`ball_detector.tscn`): the collar as 16 boxes ..." contradicts `:5` and `:53` (the Tube replaced them). Replace with "Current: the collar is one Tube (bore 0.65 m, outer 0.82 m, 0.56 m long); housing box; socket and indicator spheres." Section 2 is still the harvest table; add a delivered-declaration block as CAT-048 2a has.

**M7. Stale "Exists now" tables cite files that do not exist.** `engine/gpu/DelayedSignal.cs`, `WorkshopTimerWire.cs` (CAT-022:26, :44); `engine/gpu/OrientationSensorDeclaration.cs`, `RigidMassProperties.cs`, `WorkshopPhysicsCompiler.OrientationSensorFor` (CAT-023:25, :28); `engine/gpu/ContactTriggerDeclaration.cs`, `WorkshopActivationWire.cs` (CAT-063:41, :43); `engine/gpu/ContactWorkDeclaration.cs` (CAT-015:42); `engine/gpu/WorkshopCosmetic.cs` (CAT-035:43). CAT-015:42 and CAT-063:41 also name `wwwroot/worker.js` as the contact engine. Delete these tables or re-cite; the 22 November probe tables beneath them are the authority.

**M8. CAT-022:84, :107 — "wind_then_release and saved_for_later wait for their companion parts" is stale.** Both are Playable and solved with a missing-cause control in `LaunchLessonSolveTests` (`:94`). Only `delayed_solar` waits. Same wording in CAT-035:77 for the lamp levels: name which remain.

**M9. CAT-054:104, :106 — two planned tests lack numbers.** `:104` needs the alternative pose pair ("a second authored pair of poses" is a design decision left to the implementer); give positions and angles or state that the test searches none and uses a named pair. `:106`: "the length theory" does not exist (`DeclaredRampLengthChanges...` is a Fact); give a test name and the release pose.

**M10. CAT-062:52 — overstated.** "Launch lessons (`spring_forward` and the spring lanes) solved": `WorkshopSpringboardTests.LaunchLessonsPreserveSourceFixturesAssistanceAndNamedCapture` compares authored content, it plays nothing; only `spring_forward` (four routes) and `ready_to_rebound` are played. Replace "and the spring lanes" with "and `ready_to_rebound`".

### Low

- **L1. Baseball is delivered on main** (`origin/main:elements/Baseball`, id `baseball`, palette 68, 0.65 kg, radius 0.25 m). "The three delivered balls" (CAT-002:115, CAT-030:129, CAT-050:137, CAT-048:157, `readiness/batch-a.md:28`) becomes four; it passes the 1.3 m bore and is under the Cannon's 0.6 m minimum. CAT-001:13 should say EL-239 is delivered. No palette or id collision.
- **L2. Main's README** changed in the Batch A file (Baseball section), so the readiness README proposal must be merged, not applied over it. The one compile rule and derived registration make nothing in this batch stale: no spec names a `Compile` line or a registration list. Planned `FanGuidanceTests` rows (CAT-014:100, CAT-066:110) must extend main's three-member `Payload` enum.
- **L3. CAT-014:99, CAT-001:112** tolerances (0.3 m, 0.05 m) are stated from part sizes, not from a run; acceptable, flagged as unverified.
- **L4. `readiness/batch-a.md:36`** "oversize ... already proven at 0.75 m": proven with a law-scene sphere against the package colliders, not with a placed ball package whose `radius` is set. Say so.
- **L5. CAT-002, 015, 022, 023, 035, 062, 063** keep the old header "Current delivery (24 October 2026)" and harvest section 2; the delivered declaration sits in section 5, 6 or 7. Buildable, but the reader has to find it; move each "Verified against the package" block to section 2.

## 3. Author decisions: assessment

| Decision | Assessment |
|---|---|
| Detector chevron and Springboard stripe reprinted flush | Sound. Verified: arms span y 1.045 to 1.065 over a housing top at 1.05 (`ball_detector.tscn:34-36`, `:69-78`); stripe spans 0.075 to 0.083 over a plate top at 0.075 (`springboard.tscn:24-26`, `:55-56`). Neither is in `spec-engine-art-solidity.md` (its count is ten markings in two other packages). Same precedent as `face-markings-flush`. |
| Tube rails flush, no collider | Sound; does not weaken a requirement (M2). Needs geometry. |
| Delete the lamp's electrical node | Sound and forward-only. `lamp_bulb` appears only at `SignalLampElementDeclaration.cs:54`; electrical nodes are read only by the bundle schema and `BundleRoundTripTests.cs:307`; the Delay box is the precedent for `ISignalElement` without `IElectricalElement`. The slice must re-check the derived impact mapping, since `SupportedDomains` changes. |
| Receiver halo rebound to `capture_margin` | Sound (M1). |
| 9R.23 Chrome proofs replaced by native tests | Sound under the browser deferral; one clause unnamed (M3) and two planned tests redundant (H5). |
| Receiver stays static; moving frame proven by a carried owner | Sound as scheduling, but rests on a wrong capability claim (H1). |
| Unbounded bench; ramp bounds; Wall refusal policy; axial-spin law kept | Sound, inside the delegation. |

## 4. Engine-story gaps

**G1 (new; owns H1). Proposed story text, element-free:** "*Frame-relative sensors.* A declared residence sensor, passage sensor or planar guide is evaluated at each substep endpoint in its frame body's committed pose **and velocity**: the target's position and velocity are taken relative to the frame body (including the frame's angular velocity at the target point). A static frame is the zero-velocity case of the same rule; there is no second path. Candidate targets come from a broadphase query of the sensor's bounded volume, and pair state lives in the compiled flat tables. Proof on law scenes with a kinematic frame: a body riding the frame faster than the limit dwells and is captured; a world-stationary body swept by the frame is not; the static results are unchanged." The readiness note at `:61` under ENGINE-ASSEMBLY-JOINTS should be replaced by this story; a note is not an owner.

**G2. Story 15.0** names CAT-049 and CAT-050 only; the readiness text at `:60` (CAT-048 depth-error controls, CAT-015 isolated control, CAT-054 knots) is correct and needed.

**G3. Story 9R.19 and 9R.23**: readiness texts at `:58-59` are needed; add M2's rail geometry and M3's clause.

**G4. CAT-023c** exists and is unlisted in Batch A (H3).

## 5. Engine conformance (addendum of 22 November)

**Engine search.** `rg` for the 16 quoted package ids, for `new PartId("`, for the 12 lesson ids and for the element class names over `engine/`, `CuriousContraptions.Abstractions*`, `.Animation*`, `.Bundle`, `.Core`, `.Simulation` and `ui/`: no id literal, no element-named file, no branch on a package. Residue, none of it logic:
- **C3.** `engine/gpu/WorkshopConnections.cs:150` — the rule reads declared port kinds (comment at `:146-147`), but the message says "A level output can only light a Signal lamp." Required rewrite: "A level output connects only to an input that admits a level." Record against CAT-035; fold into its hygiene slice.
- Comments use Bumper and Domino as examples (`ContactWorkImpulse.cs:11,26`, `ContactWorkDeclarationValues.cs:56`, `OrientationSensorDeclarationValues.cs:11`); `ui/Workshop.cs:884` is a free-play hint string. No finding.

**C-all (points 3 and 5; every spec).** No spec has the required statement of counts (bodies, colliders by shape, joints, sensors, domain nodes, stores, bindings) or says, per art node, which layer moves it (body pose, presentation binding, nothing). The five delegate-written specs (001, 004, 014, 054, 066) come closest. Add one "Engine conformance" block per spec. Counts read from the packages: Ball 1 body, sphere + torus; Bowling 1 sphere; Receiver 5 boxes, 1 sensor, 1 guide, 4 bindings; Ramp 3 boxes; Wall 1 box; Pipe 1 tube + 2 tori; each bend 1 torus segment + 2 tubes + 2 tori; Funnel 1 frustum + 2 tubes + 2 tori; Detector 1 tube + 1 box + 2 spheres, 1 passage node, 3 bindings; Bumper sphere + torus + cylinder, 1 store, 5 bindings; Domino box + 2 spheres, 1 orientation node; Lamp box + cylinder + sphere, 1 node, 6 bindings; Springboard 2 bodies, 1 prismatic joint; Switch and Delay as their specs list. Collider counts above one are each owed to visible = solid; say so (point 6).

**C1 (point 3). CAT-063:93 — the button is art moved by a binding and also a static collider.** After the latch the art sits 8 cm lower than its collider (cylinder top at y 0.15; pressed art top at 0.07), so a ball resting on a pressed button floats 8 cm above it. "No gameplay effect" is not right: it is visible. The addendum says art moved by a binding is decorative and anything that blocks is a body whose art follows it. Owner item O1.

**C2 (point 3). CAT-015:113 — the head is solid with a sphere collider and is also scaled 1 → 0.88 by the impact binding** for 0.32 s. Transient and cosmetic; record it as the declared exception or bind the squash to a decorative shell. Goes with O1.

**G1 also breaks points 2, 5 and 6 for CAT-004 (and the detector's passage sensor shares the pattern):** `CommitResidence` loops every sensor over `_dynamicBodies` (`WorkshopPhysicsEngine.cs:913`) and keeps pair state in an `ImmutableDictionary` builder (`:890`), that is, per-tick work proportional to the scene and managed state in the tick. Not an element fault; an engine gap owned by G1.

**Points 1, 2, 4, 7:** all 16 declarations are data, art and bindings (declarations opened for six; the rest by interface list); behaviours are named as generic laws in the probe tables; closed sets are enums and values f32 in the opened declarations; acceptance rows run the production simulation. Inert data to delete: the lamp's electrical node (planned).

## 6. Cross-batch conflicts

- EL-194 and EL-198 (H2); EL-193 offset, sign and tier (H3); EL-195 timing (H4).
- `readiness/batch-a.md:78` says another batch's EL-192 spec calls multi-Receiver open; it is delivered. Still true; belongs to that batch.
- Palette: no collision with main's Baseball (68).

## 7. Items for the owner

- **O1. Button and bumper-head art moved by bindings while their colliders stay (C1, C2).** Recommended default: keep the Bumper squash as a recorded transient exception; for the switch, show the latch by colour only and drop the 8 cm travel, unless the owner wants the button to become a prismatic body. It blocks nothing; it changes `DESIGN.md`'s switch rule.
- **O2. Domino offset** — confirm (0, −0.05, 0) m (Batch J O5) or strike the phrase from both rows. Recommended: confirm. Blocks CAT-023c planning only.
- **O3. R-N3** (CAT-001:129): real; the definition is gone and retiring a recorded Fail waives a gate. Recommended default as the author's: retire and strike. Blocks only ticking CAT-001 and EL-187.

## 8. Not verified

- Test bodies beyond those listed in Method; in particular `IndependentResidenceGuideTests`, `WorkshopWallTests` (rows only), `DominoGuardTests`, `SwitchElectricalTests`, `ContactWorkInstanceTests`, `MotorTests`, `WorkshopActivationAnimationTests`.
- Whether the passage sensor's speed and pose handling has the same frame defect as residence (pattern inferred, not opened).
- Line citations in the bend, funnel, switch, delay and lamp declarations (spot-checked: Ball, Receiver, Domino, Bumper, Pipe, Lamp are correct).
- Whether a rail enclosed in the glass wall renders acceptably (art judgement).
- Epics anchor slugs; the planned tests' tolerances against any run.
