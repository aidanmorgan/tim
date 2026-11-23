# Audit: element specs, batch new-identities (EL-217 to EL-241)

Independent audit, 22 November 2026. Checkout `spec-drafts`; published main read at `origin/main` `25df3cd5` (Baseball at `fa86969f`). No build or test was run. Spec paths are under `docs/planning/elements/`; line numbers are lines of the draft files.

What was checked: all 25 specs and the summary read in full; every abstraction `path:line` cited in the batch opened (all exist and name the claimed symbol; one element citation is stale, F20); every cited existing test method looked up (all exist; test bodies were opened only where a finding cites them); delivered declarations, the bundle and signal validators, the presentation reader, the four engine stories in `epics.md`, the cited requirement rows, the ledger entries, main's `NamedPackageSources.cs`, and the two ball contracts.

## 1. Verdict per spec

| Spec | Verdict | Reason in one line |
| --- | --- | --- |
| EL-217 Reload shuttle | Ready with fixes | Load sensor on a sloped sill is refused by the validator (F1); pocket admits two small balls (F6) |
| EL-218 Crank-slider | Ready | Engine-blocked; no finding beyond the shared ones (S1, G-A) |
| EL-219 Rack-and-pinion slide | Ready | Engine-blocked; shared findings only |
| EL-220 Docking lift | Ready | Shared findings only |
| EL-221 Parallel gripper | Ready | Engine-blocked; shared findings only |
| EL-222 Pinball flipper | Ready with fixes | 240 W drive against a 120 W default Battery (F9); run-control risk not listed (F16) |
| EL-223 Passive impact scoop | Ready with fixes | P2 threshold contradicts the spec's own numbers (F8); file will collide with the delivering session's spec (S2) |
| EL-224 Counterweighted scoop catapult | Ready with fixes | Re-arm row has no mechanism that lets go (F3) |
| EL-225 Latched slingshot cradle | **Not ready** | Principal row S1 cannot pass: the drawing weight opposes the launch (F3) |
| EL-226 Hinged elastic catapult | Ready with fixes | Fired-angle frame not stated (F17) |
| EL-227 Magnetic impulse relay | **Not ready** | Seat geometry does not close (F4); ferrous body has two owners (X2); story text gap (G-C) |
| EL-228 Zipline signal carriage | Ready with fixes | Return row contradicts itself (F3); the "owner item" is not one (F15) |
| EL-229 Hydraulic piston | Ready with fixes | Y2 boundary fails on the spec's own law (F11); prerequisites incomplete (F12) |
| EL-230 Spring-return pneumatic piston | Ready with fixes | Pressure units differ from the pneumatic batch (X1) |
| EL-231 Double-acting pneumatic piston | Ready | Inherits X1 only |
| EL-232 Gears | Ready with fixes | Overlapping colliders admitted (F10); loop rule and bindings unproven (F13); a requirement clause is dropped (O2) |
| EL-233 Generator | Ready with fixes | Q4 wiring not stated (F18) |
| EL-234 Mouse motor | Ready with fixes | M4 cannot drain the store: a stalled drive does no work (F7). The summary marks it ready |
| EL-235 Jack-in-the-box | **Not ready** | Head, lid and stroke geometry do not close; self-release left to the implementer (F2) |
| EL-236 Egg timer | Ready with fixes | The seconds dial is not declarable: the driver is a fraction (F5) |
| EL-237 Boxing glove | Ready with fixes | 400 W drive against a 120 W default Battery (F9) |
| EL-238 Selective trap door | Ready with fixes | Counterweight sits in the entry lane (F6b); threshold bracket ignores approach speed (F6c). The summary marks it ready |
| EL-239 Baseball | Superseded | Drop the draft file; section 3 |
| EL-240 Waterwheel | Ready with fixes | Pump prerequisite missing (F12) |
| EL-241 Ball hopper | Ready with fixes | Pocket admits two small balls (F6); stroke floor below the spec's own need (F14); 180 W (F9) |

Counts: Ready 5, Ready with fixes 16, Not ready 3, Superseded 1.

## 2. Findings by severity

### High

**F1. EL-217:22, :40, :73 — the throat load sensor is on a sill sloping 15 degrees.** `CuriousContraptions.Bundle/SignalBundleValidation.cs:189`–`:190` refuses a contact-load node unless its region collider is a `Box` with `Rotation == Quaternion.Identity`. The capability row "Contact-load sensor on a static frame: present" is therefore narrower for this use, and the interlock (the element's core rule) cannot be declared as written. Replacement for line 22: "…under the hole a throat 0.9 m wide whose floor is a level landing pad 0.9 × 0.05 × 0.9 m (the load region, an unrotated box) followed by a chute sloping 15° to the outlet; a ball that cannot leave rests on the pad." Line 40: "…on the level landing pad of body 0, local normal +Y." Add to the disqualifier: "a ball in transit over the pad must not hold `go` false longer than the sensor's release hold".

**F2. EL-235:22–25, :32, :92 — geometry does not close.** The box is 0.9 m tall (0.84 m inside), the head is 0.6 m across, and `stroke` goes to 0.6 m "bounded by the box depth": 0.6 + 0.6 > 0.84. The slider's upper limit is 0, so the head stops at the limit; to throw the lid it must pass the closed-lid plane, which means at travel 0 the head stands proud of the rim and the lid cannot be shut at Reset. J7 ("Head up, lid shut, uncharged") and J1 ("ball on the lid" at Build) contradict that. Decision needed (delegable): either (a) box 1.3 m tall, head rest position 0.3 m proud, lid rests open on the head at Reset, winding closes it, and J1's ball rolls onto the lid after winding; J7 reads "head up, lid resting on the head"; or (b) an authored initial charge. Also :27 and :70 leave self-release "if declarable"; section 3 already shows it is not (endpoints close a supply contact, not a pulse). Replace :27 with "No self-release: the latch releases on a pulse only (D19)", and delete it from the disposition at :12.

**F3. EL-225:83, EL-228:84, EL-224:87 — a load that draws or returns the mechanism stays attached and opposes the next action.**
- EL-225 S1: an 8 kg Weight (78 N) hangs on `Tie` through the launch. Returning the cradle 0.6 m lifts it 47 J, more than the 45 J stored; the cradle stops near 0.31 m and the ball leaves at about 1.5 m/s, not 5.8–9.6. Replacement: "S1: drawn by an 8 kg Weight over a Pulley, then the rope is cut (the S3 construction); one pulse → the ball leaves at ±25 % of √(k·x²/1.5 kg) for the latched draw x read in the test, and the cradle returns to 0". State in Behaviour that the drawing rope must be slack, cut or paid out before release.
- EL-228 Z6: a counterweight heavy enough to return the carriage also stops it running down, so "a later strike runs it again" cannot hold. Replacement: "…the carriage returns and re-latches; work done ≥ m·g·drop − 1 J. After the rope is cut, a later strike runs it again."
- EL-224 W7: "the weight raised by a rope on `TieB`" names no delivered mechanism (the winch is EL-063, later), and a second load on `TieB` holds the cup down. Replacement: "…a 6 kg Weight on `TieB` pulls the cup end down and the latch re-engages; its rope is cut; the third pulse throws." The Weight's range ends at 8 kg (`elements/Weight/WeightElementDeclaration.cs:33`), so W1 runs at the maximum; say so.

**F4. EL-227:22–25, :84 — seats and striker do not meet.** The striker protrudes 0.05 m and travels 0.05 m; the staged seat is 0.9 m from the field centre, leaving a 0.26 m gap to a Basketball's surface. A ball touching the striker has its centre 0.59 m from the field centre, inside the 0.6 m region. Decide: make the region a half-space on the input side (then the staged seat is "touching the striker's output tip"), or lengthen the striker. I8: the rail is a static body, so a Pressure plate under it reads nothing; replace with "the retained ball rests on a Pressure plate set in the trough floor at the input seat".

**F5. EL-236:36, :68 — the seconds dial is not declarable.** `PresentationDriver.CommittedTimerRemaining` returns the remaining fraction of the duration, 0 to 1 (`engine/gpu/WorkshopDeclaredPresentation.cs:470`–`:472`), not seconds, so "one full turn for 60 s … points at the seconds left" is wrong for every duration but 60 s. The capability row is narrower, not present. Replacement for :36: "Dial hand from `CommittedTimerRemaining`: one full sweep per countdown, whatever the duration; the authored seconds are read from the selected-part panel; no numerals are printed." A seconds scale needs a presentation capability (a value mapping scaled by a declared parameter) and its own engine slice; do not plan it silently.

**F6. EL-217:23, :57 and EL-241:23, :53 — the pocket does not hold one ball only.** Inside 0.8 × 0.9 m, two balls of diameter d fit when (0.8 − d)² + (0.9 − d)² ≥ d². That holds for d ≤ 0.50 m: two default Tennis balls fit exactly, and anything smaller (Tennis `radius` goes down to 1/16 m) fits freely. The claim "0.4–0.78 m fills the pocket alone" is false and A1/H1's Tennis boundary has no margin. Replacement: pocket 0.8 × 0.8 m (collar, chute and holes 0.8 m square); supported range "0.48–0.78 m across"; boundary case Baseball or Tennis ball, with a control "a 0.4 m ball is outside the supported range".

**F6b. EL-238:24–25 — the counterweight is in the lane.** A 0.3 × 0.2 × 0.9 m block on a 0.3 m arm "behind the hinge" at door level occupies the entry sill's space and stands across the full 0.9 m lane; with the door open it swings up into the approach. Replacement: "…counterweight block 0.3 × 0.2 × 0.9 m hung 0.3 m behind and 0.25 m below the hinge line, under the entry sill, which is a 0.1 m bridge with clear space beneath; declared centre of mass (−0.3, −0.25) m from the hinge." The closed-door threshold is unchanged.

**F6c. EL-238:86 — D2 treats a dynamic outcome as static.** A 2.0 kg ball starts to tip the door only 0.92 m out; at 1 m/s it has 0.29 s left and the door turns about 2 degrees, so it crosses. Replacement: "D2: balls released from rest on a 5° Ramp (under 1 m/s at the hinge); 1.2 kg crosses, 3 kg drops; `counterweight` 1 and 12 bracketed at 0.2/0.6 kg and 2.4/6 kg." State in Behaviour that fast bodies can cross above the threshold.

**F7. EL-234:61, :93 — M4 cannot pass.** "`stamina` 20 J against a stalled load" spends nothing: a stalled drive does no work (`MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld`, `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked`). Replacement: "M4: `stamina` 20 J, belted to a Conveyor carrying a Cannonball; cumulative drive work never exceeds 20 J and, once spent, later impacts do not turn the wheel." Add a probe row: "drive work debits a package-internal source store: present, untested".

### Medium

**F8. EL-223:73, :44, :76.** P2: at 3 m/s the spec's own capture factor gives 2.5 m/s and a 0.32 m rise, which is 47°, above the asserted "below 45°". Replacement: "arrival at about 2.5 m/s … stalls below 60°". Line 44: 4.9 m/s needs a 1.7 m drop for a rolling ball (1.2 m is the sliding figure). Line 23: state the cup floor height above the base (0.4 m) and that the lesson supplies an approach deck at that height. P6: add "+ 0.05 m" and define the kinetic energy as translational plus rotational.

**F9. EL-222:24, EL-237:25, EL-241:24 — drive power above a default Battery.** Declared power is 240 W (30 N·m × 8 rad/s), 400 W (80 N × 5 m/s) and 180 W; the Battery's default `power_limit` is 120 W (`elements/Battery/BatteryElementDeclaration.cs:44`), sized for one default Motor. The brief's delivered-part check was not made. For each: either cap the declared power at 120 W, or state in F1/G1/H1 "with a default Battery" and keep the floor speed only if the numbers still give it. Decision is delegable; the number is missing.

**F10. EL-232:35.** The mesh is admitted with centre distance within ±0.03 m, so pitch-cylinder colliders may overlap by 0.03 m on two hinged bodies that cannot move apart. Replacement: "centre distance from the sum of the pitch radii to 0.03 m more".

**F11. EL-229:81.** Y2's boundary: at 6 m the force is 47.1 N against 44.1 N, and the head falls as the rod rises, so the Bowling ball stalls after 0.38 m. Replacement: "Boundary: at 8 m head it is lifted the full stroke".

**F12. EL-229:86, :91 and EL-240:80, :86.** Y7 needs the Waterwheel and a pump; X6 needs a pump (EL-022). Neither is in the prerequisites or the summary row. Either add them or mark the rows "later-partner row in EL-022's slice".

**F13. EL-232:24, :31–:33, :62, :82.** (a) `ColliderParameterBinding` is documented for a real parameter (`SceneGeometry.cs:26`) and no delivered binding targets a hinge's moving body; mark the wheel-radius row narrower until read in the validator. (b) `MechanicalRimDescriptor.Radius` is a constant `float` (`MechanicalLinkPresentationValues.cs:10`–`:11`): a rim that follows `teeth` is a further capability to list. (c) N4's "ring of four turns" needs closed loops to be solved for consistency; the cited test only shows a closed loop at rest. Add it to gap G1 (section 5).

**F14. EL-241:30.** The reason needs 0.55 s + 0.38 s (a 0.7 m ball must fall its diameter to clear the returning wall), yet the floor is 0.8 s and the default 1 s. Replacement: range 1.2–4, default 1.5.

**F15. EL-228:96 and summary section 4 item 1.** The recommended default (a collider) needs no decision, so it is not an owner item. Replace with "Resolved: the cable has a 0.02 m box collider on body 0, length bound to `span`", move the art row to Solid, and add the collider-binding capability row.

### Low

**F16. EL-222:97.** Add to the disqualifier: "or the revolute drive's target speed must be a run control" (EL-234 lists it; the ledger says today's drive "takes only a run-control target speed").
**F17. EL-226:24, :55.** State the frame: fired position is 15° back from vertical (elevation 105°), wound is horizontal to the rear; otherwise "15° above horizontal" cannot be derived. `winding_ratio` has no matching `ParameterUnit`; say `None`.
**F18. EL-233:83; EL-217:96; EL-241:91.** Q4, A6 and H6 name parts but not the wiring (two sources on one Motor input; a Counter "gating" a level; what alternates the gate pair). Add one line each.
**F19. EL-217:95.** "Phase Blocked or the throat lamp on" is an either/or assertion. Assert the committed prismatic phase.
**F20. Stale citations.** EL-227:19 cites `SpringboardElementDeclaration.cs:62`, a blank line. EL-217:100 says the hopper is "planned after this spec" while EL-241:95 calls the shuttle later; the hopper is first (chapter 3).
**F21. EL-236:93.** Levels lock a part (`"locked"` in `content/puzzles.json`), not one parameter. Reword E8 to "a locked egg timer".
**F22. All 24.** Section 4 is titled "Sources and legacy"; `README.md:27`–`:31` calls it "Legacy harvest" and requires a "Files harvested" list. Add "Files harvested: none".

## 3. Baseball (EL-239)

The draft is untracked and collides with main's tracked file. **Drop the whole draft file at merge; main's file governs, and `spec-repair-baseball-el-239.md` task 5 rewrites it.** Superseded draft content, by line:

| Draft | Superseded by |
| --- | --- |
| :1 title suffix "— named-identity spec" | Line 1 stays `# EL-239 · Baseball` (repair task 5) |
| :3 "the slice contract is spec-family-ball-presets" | Baseball is not in the family slice (family contract Intent) |
| :12 Family-slice member; :105 Delivery (family files and lists) | Delivered by `named-el-239`; repaired by its own slice |
| :13, :103 binding by a ledger `**Package:**` line; :79 "Coverage … missing … S7" | Main's `## Package source binding` section; capability present (repair probe row 11) |
| :15 "Not started. No package" | Delivered on `fa86969f` |
| :22, :46–:47 nodes `Visual/Visual_Sphere`, `Visual/Visual_Band`, `col_0_0` | `Visual/Sphere`, `Visual/Band`, `col_sphere`, `col_visual_band` |
| :31, :96, :109 rolling resistance 0.04 | 0.03 stays (repair decision 2) |
| :39 pick radius 0.38 | 0.36 |
| :40 description | "The lightest ball. A fan carries it easily and it lands with a low bounce." |
| :46 tan `#e8b764` | Published cream (232, 220, 203), coral band (decision 3) |
| :49 new icon paths | Icon unchanged (decision 6) |
| :54, :98 predicted Fan ratios 2.3 and 6.7 | About 2.0 and 5.4 with linear slip; floors 1.5 and 3 unchanged |
| :91, :95 BB1 fall fraction against "all six presets" | R3: 0.15 m peak separation from Basketball, Bowling ball and Tennis ball; Balloon control |
| :96–:99 test names | The repair's acceptance table |
| :110 open question 2 (66/67 clash) | Already fixed: the family contract uses 69 and 70 |

Same in all three: mass 0.15 kg, radius 0.26 m, inertia 0.004056, bounce 0.35, friction 0.3, drag 0.04, order 68, band torus 0.242/0.27, plate rows, cannon 0.54 m.

**One draft row has no home.** BB4's Receiver capture (aligned captured, shifted not) is in neither main's `BaseballTests` nor the repair contract's R8, while the family contract gives the Soccer ball SB4. Add it to the repair or cite the generic test that covers a `baseball` capture.

**Ball specs against the family contract.** Palette 68/69/70, masses, radii, the 0.66 m width inside the 0.6–0.84 m window and the 0.5 kg plate agree with EL-217 and EL-238's uses. `EL-077-soccer-ball.md` and `EL-091-cannonball.md` (another batch) are behind the contract: no band (EL-077:42 still draws pentagon patches), no palette order, 0.64 m width, and a bare open question at EL-077:82 and EL-091:82.

## 4. Shared and cross-batch findings

**S1. Binding text in 24 specs is about to be stale.** The repair contract (lines 64–67) records that the EL-223 slice changes the binding's `**Decision:**` line to `**Ownership:**` and admits slice-owned packages. Replace each spec's five-line list and "Blocker" sentence with: "the section is written in the syntax current on main when the slice starts". Summary section 2 (B1), section 4 item 2 and the eight "blocked on B1 only" rows should read "ready once EL-223 publishes". B2 (no `todo` anchor for EL-240 and EL-241) stands: confirmed on main's regex (`NamedPackageSources.cs` Acceptance line) and at `requirements.md:2981`, `:2724`.
**S2. EL-223 will collide like the Baseball.** All 25 drafts are untracked. The delivering session will publish its own `EL-223-*.md`; give it this draft's findings now (F8) and drop the draft at merge. The summary row "blocked on B1 only" should read "in delivery".
**S3. Summary section 7 item 1 is stale** (the contract already uses 69 and 70).
**X1. Pressure units (batch H).** `readiness/batch-h.md:50` and `EL-039-air-reservoir.md:30` fix Pa absolute with ambient 101 325 Pa; EL-230:52–53 and EL-231 use kPa with ambient 100 kPa. Restate in Pa; thresholds move to 251 325 Pa and 201 325 Pa.
**X2. Which body is ferrous (batch I).** `EL-060-electromagnet.md:108` gives the Weight the responding property and keeps balls non-responding; EL-227:60 and gap G9 assign the Cannonball to "EL-091's slice", which the family contract forbids and which publishes before the field story. Decide once: the property is added to the Cannonball by the first field consumer's slice; EL-227 I7 must keep its (responding) Weight outside the region.
**X3. Palette.** 317–341 is clear of batch I (153–174, 272) and batch J (170–181); those two overlap each other at 170–174.

## 5. Engine-story gaps

G1, G2, G4, G5 and G6 in the summary are confirmed against `epics.md:4423`–`:4498`. Additions:
- **G-A, ENGINE-ASSEMBLY-JOINTS acceptance.** The Missing list names crank coupling, revolute retention and shaft-to-electrical conversion, but the two acceptance bullets cover sockets only, and "Unblocks" omits EL-224, EL-225, EL-228 and EL-233. Proposed: "**And** each capability named under Missing has one declared qualification fixture: a position-dependent coupling, a retained and released revolute coordinate, a retained coordinate with no winding transmission, a mesh between two placed parts, and a supply paid by shaft work that delivers no more than the work absorbed." Nine identities wait on this one story; consider splitting it.
- **G-B, addition to G1.** "…and a closed ring of meshes turns when the product of its ratios is +1 and stays at rest otherwise."
- **G-C, ENGINE-FIELD-FORCE (no summary entry).** Its text has a switched source with strength and falloff. EL-227 needs: "an unswitched source whose strength is declared as the work done on a responding body from the region's edge to its face; the work done per body is a committed finite ledger; a committed occupancy read."
- **G3** is a supply-graph capability, not a joint. Keep one owner, but say so in the story or give it its own small slice.

## 6. For the owner

- **O1.** Confirm slice-owned binding (S1) and how EL-240 and EL-241 get an acceptance anchor (B2). Default: accept a sequence-task-only acceptance.
- **O2.** EL-232:95 drops the requirement's "Ratios need original-game verification" under D15. D15 covers legacy against the row; here the row asks for it. Default: accept tooth-count ratios.
- For information, as the summary says: EL-236 refuses zero delay; EL-238's rule is by game intent.
- Not owner items: F15 (cable), F3 (how a load lets go), X2, F2.

## 7. Not verified

- Whether a belt's ratio follows rim radii, and whether a consistent closed loop turns today.
- How a drive is granted power when its demand exceeds the source limit (F9).
- Whether a logic node's output can fund or command a drive inside one package (EL-217, EL-221, EL-222); whether a revolute-to-revolute transmission takes a parameter ratio (EL-226).
- The EL-223 session's contract: it is not in this checkout or on main.
- The specs' untested rows, which each spec already names as disqualifiers.
