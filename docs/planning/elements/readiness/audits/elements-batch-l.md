# Audit: Batch L element specs (Specialist and Character, 32 specs)

Independent audit, 22 November 2026, checkout `spec-drafts` (published main `8d1d68aa` plus planning drafts). Read-only: nothing was built, run or simulated. Every dynamics figure below is a hand calculation from declared values and the code's combining rules. Spec paths are under `docs/planning/elements/`; `R-l` is `docs/planning/elements/readiness/batch-l.md`; `GAP` is `.tools/audit/engine-design-gap.md`; `SIM/` is `CuriousContraptions.Simulation/`; `ABS/` is `CuriousContraptions.Abstractions/`.

## 1. Result

- **Readiness:** 0 Ready, 22 Ready with fixes, 10 Not ready (EL-076, 078, 079, 080, 084, 086, 088, 099, 101, 105).
- **Conformance:** 2 Conform (EL-095, EL-096), 30 Conform with fixes, 0 Do not conform. The summary's "32 Conform, 0 with fixes" (`R-l:48`) is wrong.
- **Custom logic.** No spec hides a state machine, per-element update, behaviour table, perception query or undeclared memory. Every held state is a declared committed node (one memory node in EL-086, a count node in EL-075, timers, store balances, integrity phase). The sighted walkers are memoryless: receiver level, supply contact, drive. No package of this batch is delivered, so the engine search for element names has nothing to find.
- **What that scriptless design costs.** Several behaviours are met only in a reduced form (section 5), one design does not work on its own numbers (the missile), and three proposed engine capabilities are not needed or already have an owner under owner decision 3a.

## 2. Verdict per spec

| Spec | Readiness | Conformance | Main reason |
|---|---|---|---|
| EL-075 Toy pulse emitter | Ready with fixes | Conforms with fixes | Oscillator story is a row of ENGINE-SIGNAL-NODES (E1); 3a wording; inert ratings |
| EL-076 Programmable ball | Not ready | Conforms with fixes | Choice-value capability has two owners (E3); `Visible` by Choice is a second rule (E4) |
| EL-077 Soccer ball | Ready with fixes | Conforms with fixes | 3a wording only; values match the Family contract |
| EL-078 Can opener and `sealed_can` | Not ready | Conforms with fixes | Second cutting law (H8); wrong story names; companion identity missing from the spec's owner list |
| EL-079 Electric mixer | Not ready | Conforms with fixes | Inadmissible spindle collider (H6); dead corners (H7); slow boundary argued from the wrong law |
| EL-080 Programmable box | Not ready | Conforms with fixes | As EL-076 |
| EL-081 Message display | Ready with fixes | Conforms with fixes | 3a names; role decision left open; summary says ready |
| EL-082 Lured character | Ready with fixes | Conforms with fixes | Friction rule wrong (H3); self-occlusion and channel collision (M4); owner of dynamic-base qualification conditional |
| EL-083 Escaping character | Ready with fixes | Conforms with fixes | Driven roller slips on its numbers (H3) |
| EL-084 Fragile fish bowl | Not ready | Conforms with fixes | Strike numbers wrong; observer law misdescribed; shards start inside the fish (H5) |
| EL-085 Rope-driven character wheel | Ready with fixes | Conforms with fixes | Winch owner name; proud rider moved by a binding |
| EL-086 Obstacle-reversing walker | Not ready | Conforms with fixes | Asks for a capability it does not need and one 3a already owns (H2) |
| EL-087 Predator character | Ready with fixes | Conforms with fixes | As EL-082 |
| EL-088 Fish tank lure | Not ready | Conforms with fixes | Break row sits exactly on the threshold; broken panes stay standing (H5) |
| EL-089 Gravity-effect pad | Ready with fixes | Conforms with fixes | Field-force additions uncoordinated (E5); arrow binding not declarable |
| EL-090 Steerable blimp | Ready with fixes | Conforms with fixes | Thrust law sign ambiguous in the canonical text (H4); retained Balloon criteria dropped |
| EL-091 Cannonball | Ready with fixes | Conforms with fixes | 3a wording only |
| EL-092 Toy rocket | Ready with fixes | Conforms with fixes | `lit` Boolean refused by validation; exhaust row fails on sample geometry |
| EL-093 Toy dynamite charge | Ready with fixes | Conforms with fixes | Impossible test geometry; blast cap wording |
| EL-094 Detonation plunger | Ready with fixes | Conforms with fixes | Owning story is not a 3a name |
| EL-095 Toy revolver | Ready with fixes | Conforms | 3a wording; shot-count caveat |
| EL-096 Tipsy platform | Ready with fixes | Conforms | 3a wording only; every number checks |
| EL-097 Pool cue | Ready with fixes | Conforms with fixes | Tolerance band ignores slide-to-roll |
| EL-098 Pool ball | Ready with fixes | Conforms with fixes | Stripe mechanism undefined; owner item missing from the spec; tolerance read point |
| EL-099 Pool pocket | Not ready | Conforms with fixes | Opening equals the repaired Baseball; sensor rule changes under ENGINE-SENSOR-VOLUMES (H9) |
| EL-100 Vacuum nozzle | Ready with fixes | Conforms with fixes | One-consumer story to be merged with the thrust descriptor change; wording |
| EL-101 Thumb tack | Not ready | Conforms with fixes | Both colliders below the admitted minimum (H6); factor unproven |
| EL-102 Large-bore ball pipe | Ready with fixes | Conforms with fixes | Every-tick trajectory assertion; tier |
| EL-103 Accelerator tube | Ready with fixes | Conforms with fixes | Field-force additions (E5); floating socket |
| EL-104 Toy firework | Ready with fixes | Conforms with fixes | `lit` Boolean; 3a wording |
| EL-105 Toy missile | Not ready | Conforms with fixes | No attitude stability; no capability to supply it (H1) |
| EL-106 Impact-sensitive toy charge | Ready with fixes | Conforms with fixes | Integrity observer claimed by three stories; impossible test geometry |

## 3. Findings, by severity

### High

**H0. Owner decision 3a is applied by none of the 32 specs.**
- Evidence: a search of the 32 files for "decision 3a" and "engine programme" returns no file. Of the sixteen 3a story names only ENGINE-SUPPLY-DEMAND and ENGINE-PARAMETER-BOUND-FRAMES occur. Fourteen specs cite an unnamed "general engine performance stories".
- `R-l:118` (gap 13) says `.tools/audit/engine-design-gap.md` "is not in the checkout". It is, and it names the stories.
- `R-l:48` counts "10 yes once S7 lands". Under 3a no element is deliverable before the engine programme, so the count is 0 and all 32 are blocked.
- Fix, every spec: Status and Delivery rows read "blocked on the engine programme (owner decision 3a), then <specific story>, then S7". Replace the unnamed phrase by the story:
  - airflow occlusion, blast, chamber, field membership and optical queries: ENGINE-SPATIAL-QUERIES;
  - contact shock, puncture, bumper contact, contact work on a moving owner: ENGINE-CONTACT-OBSERVATIONS;
  - residence (EL-099): ENGINE-SENSOR-VOLUMES;
  - "sleeps at rest", stated as present fact at `EL-092:175`, `EL-104:169`, `EL-093:167`, `EL-106:168`, `EL-094:163`, `EL-078:182`, `EL-089:165`: ENGINE-ISLANDS-SLEEP (no sleeping exists, `GAP` section 1);
  - prismatic power bound (EL-097): ENGINE-ISLANDS-SLEEP, which confines drive-budget trials.
- ENGINE-SUPPLY-DEMAND now precedes every element. Rows that defer the drain (`EL-079:66`, `EL-078:48`, EL-097, walker rows A12, B12, P12, W11, `R-l:19`) become real acceptance rows: "the source's debit equals the drive's committed work within [work, 1.01 × work]; an exhausted source stops the drive".
- Names not in 3a that must be replaced: ENGINE-PULSE-COUNT-SATURATION and ENGINE-SIGNAL-COUNT-RESET (`EL-081:107`, `:135`, `:150`; `EL-075:184`) become "ENGINE-SIGNAL-NODES (saturating count, count reset)"; ENGINE-PRISMATIC-STROKE-OBSERVATION (`EL-094:95`, `:138`), see E2; ENGINE-ASSEMBLY-JOINTS for a winch (`EL-085:91`, `:134`) becomes ENGINE-TENSILE-EXTENSIONS, and for an in-package rigid attachment (`EL-078:112`, `:156`) becomes ENGINE-JOINT-KINDS (fixed joint); ENGINE-SPAN-CUTTER becomes ENGINE-TENSILE-EXTENSIONS (span cutting).
- Shapes that ENGINE-CAPABILITY-INSTANCES deletes are used as if permanent: `FirstIn`/`SecondIn` and two-input Xor/Nor (`EL-075:41`, `:67`, `:70`; `EL-086:52`-`:54`; `EL-097:54`-`:55`), `AllPayloads` (`EL-103:47`), the single-driven-hinge run control (`EL-079:55`), `TubeEnd` (`EL-102:35`). Each Delivery section adds: "port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's".

**H1. EL-105: the missile cannot hold an attitude, and no capability exists to make it.** `EL-105-toy-missile.md:48`, `:92`, `:133`, `:169`.
- Trace: two receivers each close a supply contact feeding one side jet through the centre of mass. That is data only and has no hidden controller. But a jet through the centre of mass translates the body; it never turns it. The design does not steer the nose toward the beacon; it shifts the body sideways while lit.
- The spec's own open question concedes "a turning dart has no declared angular damping and would swing without limit". `MassFrameDeclaration` and `AmbientLoadDescriptor` carry linear drag only (`ABS/GasCouplingValues.cs:19`).
- Launch pitch: leaving a ramp lip with the tail still supported gives about m·g·x ÷ (I + m·x²), roughly 20 rad/s² for about 0.09 s at 4 m/s, so about 1 rad/s nose-down; over the 1.3 s flight that is 60° or more, which carries the main jet, both eyes and both side jets off the beacon.
- Rest attitude: crossed "+" fins (0.30 m span, 0.02 m thick, `:35`-`:36`) cannot rest on one edge; the dart rolls 45° onto two tips. The lift jet's vertical share is then 4.5 × 0.707 = 3.18 N against 2.94 N of weight, and it rests 2° to 5.6° nose-down, at or past the 2° edge of `eye_up`.
- Lateral loop, taken alone, is stable: the feedback is negative, the jet's own (1 − v ÷ u) factor damps it at about 3.75 /s on the jet-on half, and it does not chatter destructively. The first dip is about 0.6 m, not the 0.21 m at `:93`.
- `:92` says "No extra drag is declared" against `:29` (0.04 /s) and `R-l:75` ("linear drag damps the steering"). Keep one statement.
- Needed, element-free, one owner (proposed under ENGINE-REACTION-THRUST, which already edits the ambient model, or its own rider): "An ambient load may declare an angular drag (1/s) applied to the body's angular velocity as the linear drag is applied to its linear velocity, scaled by the atmosphere's pressure scale, zero by default. Acceptance: two differently parameterised free bodies given the same spin stop within the declared time constant ratio; a body declaring zero spins on." Then draw the fins as an "×", state the resting pitch, and redo the numbers. Row A8 "wherever each is placed" becomes "with the two beacons swapped between two positions in the dart's vertical plane" (steering is one axis, about ±52°, forward only).
- This is breach of addendum point 2 as written (behaviour depends on a capability nobody owns). Owner item 1.

**H2. EL-086 and the ENGINE-CHARACTERS replacement ask for two capabilities that are not needed or already owned.** `EL-086-obstacle-reversing-walker.md:38`, `:108`, `:109`, `:153`; `R-l:106`.
- Item (b), directional selection on the revolute drive, has one consumer and is unnecessary. Code already has one `RevoluteDriveDescriptor` per hinge (`ABS/MechanicalContractValues.cs:138`) and an unsupplied drive applies no torque (`CuriousContraptions.tests/RevoluteDriveTests.cs:259`). Replace `:38` with: "Each hinge carries a supplied velocity drive (hinge 0: −10 rad/s; hinge 1: +10 rad/s; `MaxTorque` 0.18 N·m each). Consumers `driveForward` → `SupplyDriveTarget(Revolute, 0)` from `forward.Output`; `driveReverse` → `SupplyDriveTarget(Revolute, 1)` from `reverse.Output`." Delete the `positive`/`negative` nodes and the missing row. Each direction is then driven by its trailing, loaded roller, which also removes the 4 % traction margin (H3).
- Item (c), a contact node owned by a dynamic body naming one collider, is ENGINE-CONTACT-OBSERVATIONS (`GAP:303`-`:308`), a 3a name. Re-point it and add one line to that story: "a second separate strike on the same owner publishes a second occurrence; a persisting contact publishes none."
- What remains of ENGINE-CHARACTERS is item (a) only; see E0 for the text.
- Does the two-contact-plus-memory design reverse without chatter? Yes on its numbers: Set and Reset are idempotent, a persisting contact publishes nothing, rebound is at most 0.08 m/s. Squeezed with both bumpers struck in one tick, Reset wins and it drives forward into the wall and presses; that is a stall, not a chatter. Two defects of behaviour remain: a contact arriving below `bump_speed` (a start within about 3 cm of a Wall) never trips, so the walker presses forever; and `bump_speed` above 0.8 m/s gives a walker that can never reverse (top speed is lower). Cap the maximum at 0.6 m/s (`:64`) and add a row for the slow-arrival case with the stated outcome "presses; does not reverse".
- Row W2 (`:133`) "no tick with negative velocity while more than 0.02 m from the Wall" fails after the first reversal; add "before first contact".

**H3. Walkers: the friction rule used is not the engine's, and EL-083's driven roller slips.** `EL-083-escaping-character.md:74`, `:140`; the same line in EL-082, EL-086, EL-087.
- The specs use the roller's own 0.8. The engine combines friction as a geometric mean (`SIM/WorkshopConstraintSolver.cs:119`) and the bench is 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs:101`): roller on bench is √(0.8 × 0.3) = 0.49.
- EL-083: the leading driven roller carries 4.04 N, limit 1.98 N, against 2.5 N of drive. It slips, which is the spec's own disqualifier at `:140`. Fix: drive hinge 1 (trailing and loaded when backing: 7.74 N, limit 3.79 N).
- Rechecked with 0.49: EL-082 4.55 N ≥ 2.5 N; EL-087 8.7 N ≥ 5.0 N; EL-086 1.94 N ≥ 1.875 N (4 % margin, removed by H2).
- Replace "friction limit 0.8·…" in all four with "0.49·… (geometric mean with the bench's 0.3)".

**H4. The thrust law is one general law, but its canonical text has the sign wrong and two copies differ.** `EL-090-steerable-blimp.md:122`; `R-l:107`.
- "(1 − the origin's speed along the axis ÷ flow speed)": thrust is opposite the axis, so a body moving with its thrust has negative speed along the axis; read literally the factor exceeds 1, is capped, and there is no top speed. The four jet specs' numbers assume the other reading.
- The summary copy says "for every input kind"; EL-090's omits it, and the missile uses Electrical inputs.
- Replacement, both places: "…a reaction impulse on the frame body at the source origin, opposite to the axis, equal to the delivered law evaluated for the still ambient medium: (granted limit − impulse granted to exposed receivers) × pressure scale × clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1). The work paid is that impulse × flow speed from the same finite input, for every input kind (store, supply consumer, coordinate)."
- This is the delivered slip law (`ABS/ConicalAirflow.cs:19`-`:26`) with the receiver at rest, so it adds no second rule. It is coherent: force F·(1 − v ÷ u), payment F·u·(1 − v ÷ u), work on the body never exceeds payment because v ≤ u.
- Numbers under that law (all four specs): rocket 0.4 kg, 6 N, 16 m/s, 100 J: thrust-to-weight 1.53, terminal 5.54 m/s, burn 1.2 s, apex 3.37 m (spec 3.4: right). Firework 0.2 kg, 4 N, 32 J: burn 0.59 s, apex 2.33 m (right). Missile 0.3 kg, 4 N, 8 m/s: terminal 7.81 m/s (right arithmetic, premises fail under H1). Blimp 1.2 kg, 12 N at 10 m/s, drag 2 /s: cruise 3.33 m/s, 120 W static, 80 W at cruise, 30 to 45 s on 3,600 J (right). Blimp buoyancy is credible on the delivered sphere gas store; `GAP` E10 does not block a spherical envelope.

**H5. Fracture parts (EL-084, EL-088): the numbers, the observer and the fragments do not work as written.**
- Restitution combines as a product (`SIM/WorkshopConstraintSolver.cs:117`), not the 0.3 the specs use. Glass 0.3 × Bowling ball 0.14 (`elements/BowlingBall/BowlingBallElementDeclaration.cs:40`) = 0.042.
- EL-088 `:43`, `:73`, `:115`: a 4 kg ball at 4.8 m/s gives 4 × 4.8 × 1.042 = 20.0 N·s against a strict ">" 20 N·s law (`ABS/ContactIntegrity.cs:25`, `:39`). The spec says 25.0. Replace the threshold with 16 N·s (holds at 3.0 m/s: 12.5; breaks at 4.8 m/s: 20.0; a Basketball at 8 m/s gives 9.3 and holds).
- EL-084 `:63`: corrected break speeds are Bowling ball 2.76 m/s (spec 2.2), Basketball 3.43 (3.1), Tennis ball 5.4 (5.2). Row F3 at 3.0 m/s gives 1.63 N·s, a 9 % margin over 1.5; use 3.5 m/s.
- `EL-084:39`, `EL-088:43` "delivered law unchanged, one pair of bodies": `ContactIntegrityDescriptor` names one collider (`ABS/GasCouplingValues.cs:15`) and the measure is per observed collider against each other collider (`ABS/ContactIntegrity.cs:17`-`:36`). An observer over "all colliders" does not exist. Declare one observation per collider (bowl 2, tank 4), or add to the observer's owning story: "an observer may name a set of colliders; the measure stays per collider pair".
- `EL-088:43`, `:74`, `:116`: the break commits after the contact response on a static owner and fragments start at rest. Four panes 0.04 m thick then stand on edge, the ball has already rebounded, and row T6 (ball crosses the old pane line) cannot pass. Needed in ENGINE-FRACTURE, element-free: "the substep in which a limit is exceeded is re-solved with the declared fragments in place of the removed colliders, so the breaking contact acts on the fragments and momentum is conserved." This reverses the "after response" rule carried at `EL-084:89`-`:90` and `EL-088:99`. Owner item 2.
- `EL-084:33`, `:35`: the loose fish can lie against the wall; pane shards at |z| 0.13 to 0.17 can start overlapping it and eject it, adding energy. Add to ENGINE-FRACTURE: "fragment shapes lie within the removed colliders' volume, checked at bundle validation", and redesign the shards as the floor disc plus wall ring segments.
- All four threshold parts (EL-084, 088, 101, 106) assume a collision impulse arrives within one substep; the measure resets each accepted substep (`ABS/ContactIntegrity.cs:12`). Unverified; the engine slice's two-fixture test must fix it before any threshold is treated as tuned.

**H6. Three colliders are below the admitted minimum.** `EL-101-thumb-tack.md:28`, `:29`; `EL-079-electric-mixer.md:39`.
- Minimum sphere radius is 1/32 m and minimum cylinder radius 1/16 m (`ABS/PhysicsDeclarationValues.cs:223`, `:240`). The tack's pin cylinder (0.03) and tip sphere (0.03) and the mixer's spindle cylinder (0.04) are refused. `EL-101:79`, `:129` and `R-l:161` left this as "not read".
- Replacement: tack pin = box 0.06 × 0.30 × 0.06 at (0, 0.19, 0), tip = sphere radius 0.03125 at (0, 0.34, 0), height at `:31` updated; mixer spindle = box 0.08 × 0.75 × 0.08 at (0, 0.825, 0).

**H7. EL-079: the square bowl has corners the paddle cannot reach.** `EL-079-electric-mixer.md:90`, `:93`, `:135`, `:139`.
- Paddle tip sweeps 0.50 m. A Tennis ball (radius 0.25) in a corner has its nearest point 0.60 m from the axis; a Bowling ball (0.28) 0.526 m. Only the Basketball is reached. Balls are pushed along a wall into the next corner and stay. Row A1 "every ball is out" fails in ordinary use.
- Fix: a regular octagon of apothem 0.85 m: four chamfer wall boxes 0.1 × 0.5 × 0.70 turned 45° at (±0.80, 0.35, ±0.80). A Tennis ball at a 135° vertex then has its nearest point at 0.40 m. Colliders become 16 (reason: closing dead corners); update `:181`-`:182`.
- Row A5 argues from speed, but the drive is torque-bounded and pushes quasi-statically: lifting a Tennis ball over the 0.1 m sill needs 4.6 N; torque 8 gives about 4.0 N toward the sill and torque 12 about 6 N. "None leaves at speed 1" is not established. Replace A5 with an ordering only: "the balls leave later at `speed` 1 than at `speed` 6, or not at all"; delete the sentence at `:93`.

**H8. EL-078: cutting is a second cutting law with one consumer.** `EL-078-can-opener.md:113`, `:115`, `:171`; `R-l:113`.
- The proposed ENGINE-FRACTURE "work limit" (friction work by a cutting-edge material on a seam collider) and Batch I's span cutter (blade pair with capacity against a material resistance, `readiness/batch-i.md:90`) are two rules for "generic cutting work": Tin snips could not open the tin and the opener's wheel could not cut a rope. Addendum point 2; forward-only "second rule".
- Decision needed (owner item 3): one cutting-work contract with a resistance in joules on a target that is a span or an attachment seam, and one cutter declaration, owned by ENGINE-TENSILE-EXTENSIONS's cutting clause or a single named story.
- Both seam and cutting edge are on dynamic bodies; contact work needs a static owner today (`GAP` section 1, "Contact events"). Add ENGINE-CONTACT-OBSERVATIONS as a prerequisite.
- The hand sum is right: normal force 15.3 N, friction 12.3 N, 14.7 W, 2.0 s to 30 J.
- `:171` says "Needs the owner: none" while `R-l:97` raises O2 (`sealed_can` as a new ledger identity). Replace with the O2 text.

**H9. EL-099: the pocket opening and the sensor rule do not hold.** `EL-099-pool-pocket.md:37`, `:64`, `:102`, `:130`.
- Opening 0.52 m: a Tennis ball is 0.50 m today and 0.516 m once its seam collider lands (2 mm a side). The repaired Baseball (radius 0.26 m) is exactly 0.52 m; the published one (0.25 m) drops in. "Refuses the Baseball" is false today and a knife-edge after the repair.
- Occupancy is answerable from code: the body position point is tested (`SIM/WorkshopPhysicsEngine.cs:929`-`:934`). ENGINE-SENSOR-VOLUMES changes it to shape overlap "as the declaration states"; with `capture_margin` at 0.1 m a Basketball resting on the rim (bottom at −0.12 m) would then read as pocketed, against row A4.
- Replacement: opening 0.46 × 0.46 m (Pool ball clears by 0.03 m a side; every ball of 0.50 m or more rests on the rim); X walls at ±0.29, Z walls 0.46 wide at ±0.29, floor 0.70 × 0.70, sensor ±0.23. A4 control "a Pool ball drops in" plus "a Tennis ball stays out". Sensor row: "occupancy is centre-in-volume, declared so under ENGINE-SENSOR-VOLUMES".

### Medium

**M1. `lit` cannot be a Boolean.** `EL-092-toy-rocket.md:59`, `EL-104-toy-firework.md:59`. Validation refuses an enable whose initial-state parameter is not "a binary BuildOnly choice" (`CuriousContraptions.Bundle/AirflowDomain.cs:122`-`:125`). Replace with "`lit` | Choice (`Unlit`, `Lit`) | None | 0 | 0 | 1 | Reject | BuildOnly", as the Fan's `powered`. The rider in `R-l:116` ("initial-state parameter optional") is then unnecessary; drop it.

**M2. EL-092: the exhaust row fails on sample geometry.** `:41`, `:89`, `:134`. The cone (1.0 m reach, 0.2 m terminal radius) is 0.1 m wide at 0.5 m; the Tennis ball's four samples sit 0.125 m off centre (`CuriousContraptions.tests/FanGuidanceTests.cs:98`-`:99`), so none is inside and row A6 gives nothing. A lying rocket's axis is also 0.13 to 0.15 m above the bench against the ball's 0.25 m centre. Fix: terminal radius 0.4 m, state the lying axis height, assert only "ball travels over 0.5 m; control wall: no motion".

**M3. One integrity observer is claimed by three stories.** `EL-106:39`, `:93`; `R-l:113` (8a), `:114`. ENGINE-FRACTURE ("no longer requires a gas store"), ENGINE-IGNITION-BLAST ("contact-shock release reuses the law") and ENGINE-CONTACT-OBSERVATIONS (lists integrity observers) all change it. One owner; see E6. EL-106 `:39` says "at least `shock_speed`" but the law is strictly greater (`ABS/ContactIntegrity.cs:25`); the flask has two colliders and the observer watches one.

**M4. Optical design of the characters is incomplete.** `EL-082:45`-`:47`, `EL-083:42`-`:43`, `EL-087:42`-`:43`, `EL-088:39`.
- Receivers and emitters sit inside their own opaque sphere colliders; with Absorb as every collider's default (Batch K convention, `readiness/batch-k.md:55`) they are dark. Declare per collider: "eye, crest and fish spheres: transmit; chassis: absorb".
- Lure, Prey and Hazard are mapped to green, blue and red, the same closed set Batch K fixes for lasers and colour receivers (`readiness/batch-k.md:50`). A green flashlight then lures the character and a character's glow trips a delivered Blue receiver. Owner item 4; recommended default: ENGINE-OPTICAL defines marker channels that colour receivers do not accept.
- Batch K's receiver is an irradiance threshold in watts with a cosine facing term; Batch L's addition (`R-l:112`) adds a binary "lit", an emitter "reach" and a receiver "acceptance half-angle". These must be one receiver model: express reach as the range at which irradiance falls below the receiver threshold (or as the emitter's declared range, which Batch K already has), and state the acceptance cone as an addition to the facing term.
- `EL-082:47` uses `SignalStateContactControl(eye, Latched)`; `SignalContactState` has only `Latched` and `TimerCounting` (`ABS/SignalContactControl.cs:5`). The ENGINE-OPTICAL addition must add a level state rather than reuse `Latched`.

**M5. Pool tolerances ignore slide-to-roll.** `EL-098-pool-ball.md:63`, `:108`-`:109`; `EL-097-pool-cue.md:129`.
- A struck ball leaves sliding at 0.905 v and loses μ·g·t = 0.245 × 9.81 × 0.1 = 0.24 m/s in the 0.1 s before the read: 0.785 v at 2 m/s, below the 0.80 bound. The striker keeps topspin and reads 0.215 v, above the 0.20 bound.
- EL-097 A1 reads the ball 0.5 m on: hit at 2.72 m/s it is rolling at (5/7) × 2.72 = 1.94 m/s, below the 2.1 m/s bound.
- Replace: A3 and A4 "speeds read at the first committed tick after the pair separates (within 2 ticks)"; EL-097 A1 "leaves along +X at 1.6 to 3.15 m/s (lower bound 0.85 × (5/7) × 0.906 × `speed`, the rolling speed after the slide)"; `EL-098:63` "about 2.7 m/s at separation, about 1.9 m/s once rolling".
- The cue itself checks: 30 N × 4 m/s = 120 W, the Battery default (`elements/Battery/BatteryElementDeclaration.cs:44`), 1 J per tick at 120 Hz.

**M6. Pool ball stripe: the decision is right, the spec is incomplete.** `EL-098-pool-ball.md:50`, `:140`, `:143`; `R-l:98`.
- Can a ball roll true over a proud band? No. A band proud by h meets the bed at θ = acos(R ÷ (R + h)) ahead of the contact point. For R 0.20 m, h 0.01 m at 2.7 m/s: θ 17.8°, strike speed 0.86 m/s, a vertical kick of about 1.3 m/s (a hop of about 9 cm at pair restitution 0.9) and roughly 39 % of spin lost per crossing, up to twice per 1.26 m of travel. Rows A3 to A6 could not be met.
- What the Basketball does: it has a proud band. Torus inner 0.322, outer 0.35 on a 0.34 m sphere, 1 cm proud, with hidden collider `col_visual_stripe` (`elements/Ball/ball.tscn:15`-`:17`, `:40`); `CompoundMassAndRollingTests.cs:403` proves the seam stops a probe. The same sum gives it a hop of about 4 cm at 2.7 m/s, which the owner accepted; its tests assert distance and slowing only.
- Flat is therefore defensible by game intent and is not a breach of visible = solid (nothing proud is drawn; the Bowling ball is the delivered unbanded ball). It still sits beside a binding owner decision ("each ball has one proud band"), so it is correctly an owner item, with default flat.
- Defects: `:143` says "Needs the owner: none"; replace with "O3 (flat stripe; default flat; blocks nothing)". "Part of the sphere's surface texture" names a mechanism no package uses (no `elements/*/*.tscn` has a texture). Decide now: two hemisphere meshes (cream and navy, zero proud, over the one sphere collider), or the first textured material with its file named in Delivery. The same gap is at `EL-076:82`-`:83` ("discs printed on the sphere").
- `EL-076:60`: the Large band is 14 mm, above the 1 cm the owner accepted; cap the tube at 0.01 m or state that it follows the proportional seam rule.

**M7. Proud art moved by a binding with no collider.** `EL-085:66` (the rider figure), `EL-086:84` (the key). Only the Bumper squash has an owner exception (decision 8). Print both flat (rider on the wheel face; key as a flush rotating disc) or give the rider a static box collider. Addendum point 3. Owner item only if an exception is wanted.

**M8. Impossible or unsupported test geometry.**
- `EL-106:79`, `:120`: a Basketball (0.34 m) cannot rest 0.4 m from a flask (0.15 m). Use 0.6 m: 2.5 × (1 − 0.6 ÷ 1.2) = 1.25 N·s, band 0.9 to 1.6 m/s.
- `EL-093:81`, `:125`: eight Tennis balls at 0.3 m overlap the stick and each other. State "`radius` 0.0625 m, `mass` 0.01 kg, on a 0.3 m circle".
- `EL-090:155`: the ball "1 m behind the tail" is unsupported 6 m up; state a shelf or a time window.
- `EL-082:137`: a 0.20 m block does not fit a 0.16 m roller gap; use 0.12 m.

**M9. Wrong facts carried into several specs.**
- `EL-092:86`, `EL-104:83` cite a "128 m/s bound" at `ABS/PhysicsDeclarationValues.cs:116`: that constant is the angular bound, 128 rad/s. The linear envelope is 64 m/s. `R-l:140` ("128, not 64") is itself wrong.
- `EL-092:27`, `104:27`, `105:29`, `093:27`, `106:27` cite `BallElementDeclaration.cs:59` for mass-frame linear drag; that line is an `AmbientLoadDescriptor`. Add `IAmbientLoadElement` to the interface lists or say "mass-frame `linearDrag`".
- `EL-076:54` "two material bindings, as `:39`-`:40`": the Tennis ball binds restitution only; a rolling-resistance binding exists (`MaterialParameterBinding.cs:5`) but no package uses it. Mark present, untested.
- Electrical ratings quoted as power draws (`EL-075:75`, `:110`; `EL-081:49`; `EL-078:48`) are read by nothing (`readiness/batch-b.md:105`). Reword to "declared rating, read by nothing; no debit is asserted".
- Coverage sections say "no sequence task of its own". The tasks exist beside each anchor: 628 to 656 at `docs/planning/requirements.md:4997`-`:5221` (for example `:5053`-`:5101` for the characters). Correct each coverage section and `R-l:119`.

**M10. Summary table disagreements.** `R-l:13`-`:48`.
- "Ready: yes" for ten rows (H0); EL-079 additionally fails H6 and H7.
- Conformance column: 30 rows read "Conforms" and should read "Conforms with fixes".
- Tier: EL-098 has a present-untested row and its spec says engine tier (`EL-098:129`); EL-102 has two; the table says data-only for both.
- EL-082 owner "ENGINE-CHARACTERS only if the probe fails": a present-untested capability four parts rest on needs an unconditional owner.
- Capability counts match the specs in all 32 rows.

### Low

- **L1.** `EL-089:65`: arrows "flipped when `lift` exceeds 9.81" need a threshold; a presentation parameter binding is multiplier × value + offset only (`ABS/ElementPresentation.cs:119`). Use one `ScaleY` binding (multiplier 1 ÷ 9.81, offset −1), subject to a negative scale being admitted.
- **L2.** `EL-103:35`: socket sphere at y 0.84 floats 0.08 m off the shell (outer radius 0.70); move to (0, 0.75, 0).
- **L3.** `EL-100:36` "without a step": the lip is 0.05 m. `:100`: a lifted ball passes the origin, loses force and bobs; say "holds", not "lifts through". `:88`: payment is impulse × flow speed, so a held ball costs a continuous 96 W; state it.
- **L4.** `EL-081:153` "takes whatever role the validator admits": decide `SetIn` (kind is checked against the node terminal, `CuriousContraptions.Bundle/SignalBundleValidation.cs:52`) and have the probe confirm.
- **L5.** `EL-102:111`, `:115`: "centre stays 0.20 m off the axis within 0.02 m while inside" is an every-tick trajectory assertion; replace with "centre never farther than 0.22 m from the axis".
- **L6.** `EL-090:160`, `:183`: the ledger says the blimp "also retains CAT-003 acceptance" (`docs/planning/invest/named-elements.md:840`); the spec drops rupture and the tie on a D19 citation. Owner item 5.
- **L7.** `EL-095` row A4: "each shot spends the full 8 J" holds only for energy-limited payloads; a Tennis ball under 0.11 kg is speed-limited and yields more than six shots. Six shots is declarable today (a store that fires only when full, `SIM/WorkshopPhysicsEngine.FiniteWork.cs:106`).
- **L8.** `EL-094:79`: the energy line is wrong in detail (damping takes several joules); the conclusion holds (Basketball settles at 0.159 m, peaks at 0.191 m, fires in about 0.085 s; re-arm at 0.10 m gives one pulse per strike).
- **L9.** Evidence rows that can be upgraded to present after opening the bodies: signal-memory jet enable (`FanGuidanceTests.cs:63`); supply-fed airflow debit within [work, 1.01 × work] (`FanIntegrationTests.cs:507`); chamber occupancy bounds (`ChamberOccupancyTests.cs:21`).
- **L10.** Citations: `EL-082:47` (`StateLatchElementDeclaration.cs:63` should be `:67`); `EL-086:48` (`:53` should be `:56`); `epics.md:4538` should be `:4545` in EL-082, 083, 086, 087; `EL-097:29` (`:26` should be `:27`); `EL-099:62` has Default and Options transposed; `EL-098:62` models rolling loss as C·g where the engine bounds a torque (C·g ÷ 1.4 for a sphere; the comparison in A5 still holds).
- **L11.** `EL-097` should state its colliders are hidden `col_` proxies; the pusher it copies uses visible meshes (`GAP` L2).

## 4. Engine stories: generality, overlap and corrected text

**E0. ENGINE-CHARACTERS replacement (`R-l:106`).** The existing story (`_bmad-output/planning-artifacts/epics.md:4542`, "small typed behaviour table… deterministic choice") is per-agent logic in the engine and must go; the batch is right about that. The replacement is element-free but two of its three parts are wrong (H2). Corrected text, same key:

> ### Story ENGINE-CHARACTERS: Package joints on a dynamic base
> *Status: Backlog. Sprint key: `engine-characters`. After ENGINE-CONTACT-OBSERVATIONS and ENGINE-SUPPLY-DEMAND.* *Present today:* hinges and sliders on any support body are admitted by the validator and compiler; the drive reaction on a free base is tested without contact (`RevoluteDriveTests.cs:503`: zero gravity, no ground). *Missing:* qualification only. A package whose base body is dynamic and carries driven hinges rolls by contact friction, coasts and stops unsupplied, gains no momentum in isolation, and survives Reset and save/load, on two differently sized fixtures with an internal source. No agent, behaviour table, perception query or route planner. *Unblocks:* EL-082, 083, 086, 087 (EL-084, 085 and 088 do not depend on it).

It is a prerequisite of all four walkers, not "only if the probe fails". The name now misdescribes the capability; renaming is the owner's choice (the key is in `epics.md`).

**E1. ENGINE-OSCILLATOR-SUPPLY-NODE (`R-l:111`): not a new story.** It is a row of the compiled signal network, which 3a gives to ENGINE-SIGNAL-NODES, and that story already owns the self-edge and cycle rule this proposal amends. One consumer (EL-075). Fold in as: "An oscillator's enable is a declared reference to either a public `PowerIn` port or a supply node of the package's own graph; a cycle that passes through an oscillator (at least one interval) is admitted; zero-delay non-monotone feedback stays refused." EL-075's mechanism is otherwise sound: gate = Xor(fire, done) with the count-latched contact gives "fire and not finished"; the burst memory is the declared count node; resume is the delivered oscillator restart (`ComposableSignalNodesTests.cs:645`).

**E2. Stroke observation (EL-094).** ENGINE-PRISMATIC-STROKE-OBSERVATION is not a 3a name, and `GAP` section 6 item 13 folds it into ENGINE-GAS-NETWORK, a poor home for a signal source. What the plunger needs is 3a's "threshold over a committed scalar". Recommended addition to ENGINE-SIGNAL-NODES: "the scalar may be a prismatic or revolute coordinate; the threshold declares a separate re-arm level; the occurrence is a Pulse source node routed in the tick it is published". Owner item 6.

**E3. ENGINE-CHOICE-BOUND-VALUES (`R-l:109`): general, but it has a second owner.** Batch O G5 writes the same clause into the 3a-named ENGINE-PARAMETER-BOUND-FRAMES ("a Choice option may carry the bound value", `readiness/batch-o.md:114`). Consumers are EL-076, EL-080, EL-162 (Batch J gap 9), GAP-07 (Batch M gap 14) and RAD-07, so generality holds. One owner is needed (owner item 7). Recommended: fold into ENGINE-PARAMETER-BOUND-FRAMES under 3a's "do not coin another name", with the text of `R-l:109` as its Choice clause; otherwise keep the new name and strike the clause from Batch O G5.

**E4. `Visible` by Choice versus ENGINE-PARAMETER-SELECTED-PARTS.** The Choice story admits presentation `Visible` by option; Batch B's story shows or hides art with its paired collider. Without a boundary a solid node could be hidden while its collider stays. Add: "A `Visible` binding by parameter is admitted only on a node the package classifies decorative; a node with a paired collider is selected only through ENGINE-PARAMETER-SELECTED-PARTS; bundle validation refuses the other case."

**E5. ENGINE-FIELD-FORCE now carries five uncoordinated additions** (Batch I standing draw, new-identities well energy, Batch M scene gravity, Batch L acceleration kind and force kind with paid work, `R-l:115`). Batch L's two "kinds" are one law. `EL-089:94` also asks for work "on each body" as a committed read, which is source × body state (design rule D5 forbids it). Proposed single text:

> A field source declares a bounded region (box or cylinder) on a frame body; a selector (a responding property or a target selector); a constant vector whose quantity is a closed enum {Force (N), Acceleration (m/s²)}, optionally with falloff from a closed enum; membership by centre of mass, found through ENGINE-SPATIAL-QUERIES and waking sleepers; a payment kind from a closed enum {None, StandingDraw, PaidWork within a declared power bound, scaled proportionally}; the reaction on a dynamic frame; one work read per source.

Whether a supply-switched source uses standing draw, paid work or either is owner item 8.

**E6. Contact-integrity observer: one owner.** Addition to ENGINE-CONTACT-OBSERVATIONS: "An integrity observer names a body, one collider or a set of colliders; takes its impulse threshold and minimum approach speed as constants or parameter-bound values; the pair measure may be multiplied by the other collider's declared stress concentration (at least 1, default 1); it publishes a one-way committed occurrence that a declared effect consumes (gas outlet, body break, blast release); it requires no gas store." ENGINE-FRACTURE and ENGINE-IGNITION-BLAST then consume it and drop their own clauses (`R-l:113` 8a last sentence and 8b; `R-l:114` shock release).

**E7. ENGINE-REACTION-THRUST (`R-l:107`): element-free, four consumers, one law.** No overlap with Batch D's `engine-axial-gas-nozzle` (a chamber on a slider), ENGINE-GAS-NETWORK or Batch H's static sources. Needs the H4 wording, "candidates from ENGINE-SPATIAL-QUERIES", and the angular-drag rider if the owner approves H1.

**E8. ENGINE-CONVERGENT-AIRFLOW (`R-l:108`): element-free, no overlap, one consumer.** No other readiness file has a suction consumer; it is stated so any package could declare it, which the addendum allows, but record the single consumer. It is a real gap: reversing the axis tests occlusion on the wrong side (`SIM/WorkshopPhysicsEngine.Airflow.cs:120`). It edits the same `AirflowSourceDescriptor` and paired reaction as E7; land both in one descriptor change (or make it a clause of ENGINE-REACTION-THRUST renamed for airflow source extensions), state whether a converging source has an ambient share, and take candidates from ENGINE-SPATIAL-QUERIES.

**E9. ENGINE-PARAMETER-COMBINATIONS (`R-l:110`): element-free, no overlap in any readiness file, two consumers.** Acceptable as written.

**E10. ENGINE-IGNITION-BLAST additions (`R-l:114`).** The existing text (`epics.md:4522`) covers only a one-shot radial impulse with occlusion; reference impulse, falloff enum, source reaction, energy cap, signal release and release event are all additions, and all general. Corrections: define the cap as "Σ J² ÷ 2m over targets and source, evaluated from rest" ("kinetic energy the impulses would add" is ambiguous for moving bodies); exclude the source's and the target's own colliders from the occlusion segment; take the shock release from E6; lift "after ENGINE-THERMAL" for the blast half. "A blast does not break fragile parts directly" weakens no requirement row.

**E11. ENGINE-FRACTURE additions.** Reconcile with Batch M (`readiness/batch-m.md:91`, a closed set of sustained-load measures, which explicitly excludes impact) and Batch J (`readiness/batch-j.md:87`): Batch L's impulse threshold must be one more member of that declared measure enum, not a parallel limit. Add the two fragment rules of H5. The cutting work limit leaves this story (H8).

**E12. Strand story name.** Owner decision 1 names ENGINE-CONNECTION-MATERIALS for the solid strand; 3a's list has ENGINE-TENSILE-EXTENSIONS, which `GAP` section 6 item 10 says covers declared tensile materials. EL-085 cites the first. The two owner texts need one name (owner item 9).

## 5. Requirement criteria weakened to keep the designs scriptless

No row outcome at `docs/planning/requirements.md:4997`-`:5223` is dropped. These are met in a reduced form and should be recorded as decisions, not left implicit:

- EL-082: "moves toward the lure" becomes "rolls along its own X while lit". It cannot turn; an off-axis lure is never reached. It stalls on any slope above 7.8° (2.0 N net against 14.7 N·sin θ).
- EL-087: "reachable routes" becomes "stopped by contact"; it presses on a barrier indefinitely.
- EL-083: `escape_distance` and the recover timer are dropped (the S653 correction allows it).
- EL-086: reversal is on contact speed only; a stall or a slow arrival is never sensed; it stalls above 8.8° and reverses on a ramp steeper than about 12° when the bumper lip meets it.
- EL-105: "steers toward" becomes one-axis lateral translation (H1).
- EL-090: "steerable" becomes a placed heading and a signal start; retained Balloon rupture and tie criteria dropped (L6).
- EL-076, EL-080: "programmable" is preset admission in the requirement ("no runtime numeric solution editor"), so three Choices deliver it; nothing removed.
- EL-081: no text is drawn; "typed state or count" is still met.

## 6. Layers, flat state and family checks that passed

- Cosmetic animation never feeds back in any of the 32: gait, ears, fish, plume, burst, drum kick, eye and charge-band art are bindings on committed state; goals and sensors read physics only.
- Every moving solid is a body with colliders except the two items in M7. No fuse, tether or proud uncollided static solid is drawn. Flat-state counts are stated in every spec.
- Ball presets (EL-077, EL-091) against `_bmad-output/implementation-artifacts/spec-family-ball-presets.md`: radius 0.32 m, masses 0.45 and 6 kg, moments 0.018432 and 0.24576, materials, band (0.302/0.33 torus, 1 cm proud, own collider), colours, pick radius and rows SB1 to SB5 and CB1 to CB5 are identical; the members are value-only. Chamber width over all colliders is 0.64 to 0.66 m, inside the Cannon's 0.6 to 0.84 m (`SIM/WorkshopPhysicsEngine.Chambers.cs:126`-`:139`); the Soccer ball (0.45 kg) is under the plate's 0.5 kg default by design; Cannon 3.87 m/s for 6 kg. Cited tests exist with matching bodies (`TennisBallTests.cs:118`, `ChamberOccupancyTests.cs:21`, `CannonIntegrationTests.cs:550`, `WorkshopBumperTests.cs:360`, `CompoundMassAndRollingTests.cs:403`). The contract's palette orders 69 and 70 and its `Compile Include` item are stale, as the author records.
- EL-096: tip thresholds (Basketball 0.255 m, Tennis 0.728 m, Bowling 0.064 m), pivot inertia and the 2.5 N·m friction hold check; all seven cited tests exist.
- EL-085: 7.25 rad/s², 13.9 rad/s after 2 m, energy closes at 78.4 J.
- EL-089: net 2.45 m/s² down at the default; a ball floats only above 9.81. EL-103: 18 J gives the Basketball 5.07 m/s and the Bowling ball 2.54 m/s. EL-100: 12 N × 8 m/s = 96 W; no singularity at the origin (the cone narrows below the ball's samples and the ball coasts).
- No package id of the 33 collides with another batch's readiness table or a delivered package.

## 7. Cross-batch conflicts

1. Choice-bound values: Batch L new story against Batch O G5 inside ENGINE-PARAMETER-BOUND-FRAMES (E3).
2. Cutting: Batch L work limit against Batch I span cutter (H8).
3. Fracture measure: Batch L impulse against Batch M sustained-load set (E11).
4. Optical receiver model and channel set: Batch L against Batch K conventions (M4).
5. Field force: five additions from four batches (E5).
6. Integrity observer: three stories (E6).
7. Stroke observation and strand story names against 3a (E2, E12).
8. Batch L's remark on Batch J's EL-108 (15 N × 12 m/s = 180 W against a 120 W Battery, `R-l:139`) stands and is for the Batch J repair.
9. The large pipe (bore radius 1.0 m) joins no delivered tube part (Funnel inlet 1.3 m); the requirement does not ask for it. Note only.

## 8. Items for the owner

| # | Item | Recommended default | Blocks |
|---|---|---|---|
| 1 | EL-105: approve a general ambient angular drag and its owning story, or accept a redesign (H1) | Approve as a rider on ENGINE-REACTION-THRUST | EL-105 |
| 2 | Fracture commit: re-solve the breaking substep with fragments, or keep "after response" and accept standing panes (H5) | Re-solve | EL-084, EL-088 |
| 3 | One cutting-work law or two; should Tin snips open the tin (H8) | One law | EL-078, EL-066, EL-068 |
| 4 | Character stimuli on marker channels or on red, green, blue (M4) | Marker channels | Sighted walkers, EL-088 |
| 5 | EL-090: drop the retained Balloon rupture and tie criteria; "steerable" as placed heading (L6) | Confirm | Nothing mechanical |
| 6 | Stroke observation owned by ENGINE-SIGNAL-NODES (E2) | Yes | EL-094, Bellows |
| 7 | Single owner for Choice-bound values (E3) | ENGINE-PARAMETER-BOUND-FRAMES | EL-076, EL-080 |
| 8 | Field-force payment for supply-switched sources: standing draw, paid work or both (E5) | A declared payment enum | EL-103, EL-060 |
| 9 | One story name for the solid strand (E12) | ENGINE-CONNECTION-MATERIALS, as decision 1 | EL-085 wording |
| 10 | O2: `sealed_can` as a new ledger identity (`R-l:97`) | Add it, delivered in the EL-078 slice | The can's coverage binding |
| 11 | O3: flat Pool-ball stripe (M6) | Flat; two hemisphere meshes | Nothing |
| 12 | Rename ENGINE-CHARACTERS now that it holds no character logic (E0) | Keep the key, change the title | Nothing |

O1 (no water in EL-084 and EL-088) stands as the author wrote it. Decidable under the delegation without the owner: pocket opening 0.46 m (H9), octagonal mixer bowl (H7), flat rider and key (M7), `bump_speed` cap.

## 9. Not verified

- Nothing was simulated. In particular: whether a collision impulse arrives within one substep (every threshold in EL-084, 088, 101, 106); the hop and slide figures for banded and pool balls; the missile pitch estimate; mixer rotation sense against the opening side.
- Whether a package with a dynamic base, two driven hinges and an internal source compiles; whether stores, airflow sources and Signal ports compile on a dynamic package; whether a source feeding a contact directly, a memory node starting a timer, or a Level input under role `SetIn` is admitted; whether a hinge on a hinge body compiles (EL-078).
- Whether a source debits when it recharges a package store over an internal edge (`ComposableSupplyRoutingTests.cs:530` asserts only that the store gains).
- Admission of a Tube of inner radius 1.0 m beyond a positivity check; a mass-frame centre following a parameter (EL-080); a negative `ScaleY`; `Visible` with the drivers EL-081 uses; a residence sensor without a planar guide.
- The Baseball package is not in this checkout; the delivered Ramp's angles and the Conveyor's start torque were not read.
- Test bodies not opened: `CompoundMassAndRollingTests.cs:114` and `:176`, `MotorTests.cs:792`, `ElectricalXorTests.cs:263`, `ClutchGeneralityTests.cs:511`. Opened and matching: `ContactActivationTests.cs:77`, `RevoluteDriveTests.cs:259` and `:503`, `FanIntegrationTests.cs:507`, `ChamberOccupancyTests.cs:21`, `ConicalAirflowTests.cs:61`, `FanGuidanceTests.cs:63`, `ComposableSignalNodesTests.cs:645`, `TennisBallTests.cs:118`, `CompoundMassAndRollingTests.cs:200` and `:403`, `AirflowGeneralityTests.cs:162` and `:189`.
- The `audit-engine-supply-self-edge*.md` files and `docs/gpu-f32-physics.md:195`, `:258` were not opened. Individual campaign level scopes were not read.
- Whether play is confined to one plane, which decides how much "no steering" matters for the walkers.
