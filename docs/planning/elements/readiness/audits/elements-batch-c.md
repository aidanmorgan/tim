# Audit: Batch C element specs (catalogue specs of Epics 10 and 11; 14)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` (`8d1d68aa` plus drafts), published main `25df3cd5`. Nothing was built or run. All 14 specs were read outside section 4 (legacy harvest). Every non-legacy `path:line` citation was checked for Motor, Conveyor, Clutch, Reverse gear, Powered gate, Cannon, Linear pusher, Wound spring, Beam shutter, Trampoline and Impact lever, and every section 3 citation for Weight, Pulley and Rope anchor. Collider and row counts were recounted from each package's `.tscn` and declaration file, not from `elements/elements.bundle.json`. Three parallel verification passes did the per-spec reading; the auditor re-opened the code behind every High finding. Spec paths are under `docs/planning/elements/`; `Name:line` means that spec file. `T/` is `CuriousContraptions.tests/`, `I/` is `_bmad-output/implementation-artifacts/`, `A/` is `CuriousContraptions.Abstractions/`, `Sim/` is `CuriousContraptions.Simulation/`.

## 1. Verdicts

| Spec | Verdict | Conformance | Reason |
|---|---|---|---|
| CAT-007 Beam shutter | Ready with fixes (blocked on `ENGINE-OPTICAL`, CAT-036, CAT-038) | Conforms with fixes | Two rows marked untested are tested and one marked present is not (F12); Optics category has no engine owner (F11); closed blade does not cover the opening (F13); several numbers missing (F14). |
| CAT-016 Cannon | Ready with fixes | Conforms with fixes | Electrical node misread and "drain" wrongly claimed (F4, F5); Tennis window edge false after part B2 (F9); chamber query scans the scene (F7). |
| CAT-018 Clutch | Ready with fixes (needs a part D contract) | Conforms with fixes | Art defect real and unowned (F17); planned axle test asserts an unstable rest (F21); test class names wrong (F19). |
| CAT-019 Conveyor | Ready with fixes for part B; dimensions not ready | Conforms with fixes | `length`/`width` bind nothing and no story owns it (F1); stalled-belt control cannot pass (F20). |
| CAT-034 Impact lever | Ready with fixes | Conforms with fixes | Row L-R6 cannot pass (F8); three requirement criteria dropped or weakened (F22); rope roles enumerated in engine (F6); two present rows untested (F23). |
| CAT-039 Linear pusher | Ready with fixes | Conforms with fixes | Electrical node misread (F4); art section stale (F26); drawn-proxy list wrong (F25). |
| CAT-042 Motor | Ready with fixes | Conforms with fixes (spec says Conforms) | Index bar is 4 mm proud with no collider (F18); cited symbol does not exist (F24); sleeping claimed (F10). |
| CAT-051 Powered gate | Ready with fixes | Conforms with fixes | Two cited symbols do not exist (F24); power bound enforced by whole-world replay (F7); owner item contradicts "none needs the owner" (F15). |
| CAT-053 Pulley | Ready with fixes | Conforms with fixes | One present row untested (F23); owner misattributed (F27). |
| CAT-057 Reverse gear | Ready with fixes (needs a part D contract) | Conforms with fixes | Art defect real and unowned (F17); test class names wrong (F19); sign statement false (F28). |
| CAT-058 Rope anchor | Ready with fixes | Conforms with fixes | Two wrong geometry values (F29); rope statements conflict with Batch I and J audits (C1–C4). |
| CAT-065 Trampoline | Ready with fixes | Does not conform as written | The law it relies on carries product bounds in shared admission, dense surface × body rows and scene-proportional work per substep; the spec states the opposite or records them only against the performance gate (F2, F3). |
| CAT-067 Weight | Ready with fixes (text and counts only) | Conforms with fixes | Sleeping claimed (F10); "untested" row is tested (F23). |
| CAT-071 Wound spring | Ready with fixes | Conforms with fixes | Mark bindings contradict part B (F30); floating and exposed art not recorded (L2); Weight fit figure (L3). |

Counts: Ready 0, Ready with fixes 14 (one blocked on an engine story, one partly not ready), Not ready 0. Conformance: Conforms 0, Conforms with fixes 13, Does not conform 1.

What held: declared values, ports, parameters, palette orders and the conformance counts of all 13 delivered packages match the scenes and declarations. No engine, bundle, abstraction, animation, wire, web or `ui/` source names any of the 14 elements (the only hits are hand-written coverage and browser-tool tables under `tools/Coverage/` and `tools/E2eRunner/`). The five delegate-written specs' line citations all resolve; their defects are wrong class and symbol names.

## 2. The eight claims the brief asked to verify

| # | Claim | True? | Real defect? | Engine generality or performance? |
|---|---|---|---|---|
| 1 | Conveyor `length`, `width` validated and saved, bound to nothing | True (`elements/Conveyor/ConveyorElementDeclaration.cs:35`–`:36`; frames, rims, ports are literals at `:45`–`:51`, `:84`; only `FromParameter` is `:113`) | Yes. Player-visible, and CAT-019's criterion "Length/width/surface-per-radian define actual finite-inertia driven surface" (`docs/planning/requirements.md:570`) is unmet while the status reads delivered | Engine generality: a partial capability. Collider binding exists for the base body only (`CuriousContraptions.Bundle/BundleGeometry.cs:28`–`:48`); missing are a parameter-bound joint base frame, child-body collider binding, rim centre, and a port position from two parameters. No story. Not performance |
| 2 | Trampoline membrane samples every collider of every dynamic body with no prefilter; no direction-aware sag; no bed friction | True, and the cost is worse than stated (F3). Sag and friction both absent | Sampling: yes (performance). Sag: yes, a requirement criterion (`requirements.md:848`) deferred in a review loop without an owner decision. Friction: yes for play (a load drifts at 0.4 m/s after 8 s); deferred by the owner on 2 November | Sampling: performance and generality (dense rows). Sag and friction: missing general capabilities, no `ENGINE-*` story |
| 3 | Linear pusher and Wound spring draw solids with collider proxies | True for pusher `col_Foot`, `col_Barrel` and six Wound spring proxies; the pusher head is NOT one (F25) | Yes under the extractor's rule; owned by part B (`I/spec-elements-visible-solid-remainder-b.md:97`–`:101`, `:110`–`:117`). Specs match the contract except F25 and the Wound spring mark bindings (F30) | Neither. G12's "draw sockets flush" option reverses the 21 November owner decision recorded at part B `:145`; it is an owner item, not a contract-author choice |
| 4 | Clutch and Reverse gear art defects in no repair contract | True. Clutch spokes 35 mm proud, axle r 0.055 m × 1.3 m with no collider; Reverse gear index bars 40 mm and marks 100 mm proud | Yes (visible = solid). Also the Motor's index bar, 4 mm proud, which the spec calls flush (F18) | Neither |
| 5 | Inverted unpowered gate or shutter blade hangs 0.70 m open | True: 9.81 ÷ 14 = 0.7007 m, inside limits 0–1.55 m; pinned by `T/DeclaredPrismaticDriveTests.cs:335`–`:351` | Minor for the gate (the hanging blade still spans the bore axis and floor; only the upper 0.63 m is open). It does weaken "loss closes it" with "rotated mounting" (`requirements.md:498`) for mounts past horizontal, so it is a real owner item | Small generality gap if the owner wants fail-closed everywhere: a declared spring rest coordinate (F15). Not performance |
| 6 | Powered gate node 20 W against a 120 W drive | True (`elements/PoweredGate/PoweredGateElementDeclaration.cs:94`, `:85`) | Dead data, not behaviour. No law reads any `ElectricalNodeDescriptor` number; only `Role` is read. The same holds for every package | Generality: an inert mandatory descriptor (F4) |
| 7 | Supply drain has no engine story | True. Only `I/deferred-work.md:209`–`:213`, no sprint key | Yes for drives (Motor, gate, pusher). Wrong for the Cannon, whose charging already debits its source (F5) | Engine generality gap |
| 8 | Parameter refusal untested for Linear pusher, Wound spring, Cannon | True. The refusal code is one generic seam (`engine/gpu/InstanceParameters.cs:37`–`:49`) | Low-risk coverage gap. S4 row R2 (`I/spec-s4-catalogue-conformance-suite.md:36`, `:197`) iterates every delivered identity and covers all three; the summary should call it "R2" | Neither |

## 3. Findings, by severity

### High

**F1. Conveyor dimensions: requirement unmet, no owning story (CAT-019:35–36, :287, :301; `readiness/batch-c.md` G1).** Evidence in claim 1. Add at CAT-019:16: "CAT-019's 'length/width define the surface' criterion is not met; `length` and `width` bind nothing." Adopt the story in section 5 (E1). The spec's decision to keep and bind the parameters, not remove them, is right.

**F2. Shared admission hard-codes the Trampoline's product ranges (CAT-065:89 "the engine holds no package-sized tolerance", :273, :276).**
- Evidence: `A/PhysicsDeclarationValues.cs:89`–`:90` `Range(Tension, 120f, 1200f); Range(DampingRatio, 0.08f, 0.8f)` and `CuriousContraptions.Bundle/BundleValidator.cs:706`–`:707`, equal to the package's own minima and maxima (`elements/Trampoline/TrampolineElementDeclaration.cs:27`–`:28`). Design guide D3: "Shared admission may impose only the reusable contract's justified validity limits."
- Consequence: a second compliant part (EL-215 Damped cushion) cannot declare other values without an engine edit. Related assumption: one planar rectangular surface per element on the static root, normal fixed to local +Y (`A/MechanicalContractValues.cs:222`–`:234`).
- Fix: delete the sentence at :89; add a required-refactor row owned by E3.
- Linked compatibility defect, CAT-065:102–104 (and CAT-067:237): the stability envelope (stable above 5.6 g) is said to clear "the catalogue's lightest dynamic bodies". The Tennis ball's `mass` minimum is 1/1024 kg (`elements/TennisBall/TennisBallElementDeclaration.cs:16`). Replace with "The Tennis ball admits 0.98 g, below the bound; unproven" and add a residual row (Tennis ball at minimum mass on 1200 N/m, ζ 0.8: no tick gains energy).

**F3. Membrane cost is misdescribed and unowned (CAT-065:279–280; G7).**
- `UpdateCompliantContacts` runs every substep and calls `SampleMembrane` for every surface × every dynamic body with no test (`Sim/WorkshopPhysicsEngine.cs:1455`–`:1464`). `SampleMembrane` loops `foreach (var child in _colliders)` over the whole scene's collider list, four times, filtering by `child.Body != body` (`:1536`–`:1547`). The force pass repeats it for engaged bodies. Cost per substep: surfaces × dynamic bodies × 4 × all colliders. The BVH is unused. No allocation.
- Engagement rows are sized surfaces × dynamic bodies (`engine/gpu/PhysicsWorldLayout.cs:41`–`:42`); design guide D5 forbids "owner×target reservations". CAT-065:279 states this as conforming.
- Destination today is the performance gate P0-034 (`I/deferred-work.md:198`–`:201`), which measures and fixes nothing.
- Replacement for :280: "the contact-state pass samples every dynamic body for every surface each substep, and each sample scans the scene's whole collider list four times (`WorkshopPhysicsEngine.cs:1461`–`:1464`, `:1540`–`:1547`); engagement rows are dense. Engine gap, owner E3."

**F4. Electrical node values are misread in four specs, and no law reads them.**
- `A/ElectricalContractValues.cs:15`–`:21` is `(NodeId, Role, RatedVoltage, Resistance, Capacitance, MaxPower)`. Cannon: `("charge", Consumer, 12, 10, 0, 100)` (`elements/Cannon/CannonElementDeclaration.cs:46`) is 10 Ω and 100 W, not "10 W". Pusher motor is 24 Ω, 400 W, not "24 W"; distribution 0.05 Ω, not "0.05 W" (`elements/LinearPusher/LinearPusherElementDeclaration.cs:51`). Gate is 24 Ω, 20 W.
- `BundleValidator.cs:865`–`:877` only checks the numbers are finite; `SignalBundleValidation.cs:167` reads the role.
- Replace CAT-016:36 with "consumer `charge` (12 V, 10 Ω, maximum 100 W; validated, read by no law)"; CAT-039:38 with "`motor` 12 V, 24 Ω, max 400 W; `distribution` 12 V, 0.05 Ω, max 400 W (inert)"; delete "declares demand" at CAT-016:332 and CAT-039:271; CAT-051:34, :283 "would read the node" becomes "read by no law". CAT-007:39 must give all six fields (copy the gate's, with max power 120). `batch-c.md:81` should say the 20 W mismatch has no behavioural effect. The drain story (E2) either consumes these fields or deletes them.

**F5. "Supply drain" is wrong for the Cannon (CAT-016:332; G3).** Charging already debits the source: `T/CannonIntegrationTests.cs:83` asserts `Remaining.Value < disabled.Remaining.Value`. Replace item (3) with "Supply drain: none outstanding. Charging debits the source through the supply-to-store binding." Remove the Cannon from G3.

**F6. Rope-port admission enumerates role names only the Impact lever declares (CAT-034:118, :295 "no lever code in the engine"; `batch-c.md:26`).** `engine/gpu/WorkshopConnections.cs:163`–`:164` `outRole is not (PortRole.Tie or PortRole.TieA or PortRole.TieB)`; also `engine/gpu/ElementPortCatalogue.cs:182`, `BundleValidator.cs:543`, `:546`. A package with a third or differently named rope port needs an engine edit (D2). True by name, not by assumption. Record a required refactor, owner E5. This also contradicts CAT-058:232 "a port admits by domain and direction only" (Batch I audit F28 says the same).

**F7. Two more per-substep whole-scene costs are stated as absent or not stated.**
- Cannon, CAT-016:345 "neither scans the scene": `Sim/WorkshopPhysicsEngine.Chambers.cs:15` refreshes every collider, `:86` loops every dynamic body, `:102` every collider per region, `:156`–`:167` sweeps the payload against every collider; called inside the substep loop (`WorkshopPhysicsEngine.cs:1002`).
- Powered gate (and every bounded prismatic drive: pusher, shutter), CAT-051:288: while any prismatic motor is active each substep captures a whole-world snapshot and, when step work exceeds the bound, restores and re-solves the substep up to 11 times (`Sim/WorkshopPhysicsEngine.Motors.cs:35`–`:66`). The revolute drive clamps in its row.
- Both break addendum point 6 ("nothing requires per-tick work proportional to the whole scene"). Record both as engine gaps, owner E4.

**F8. Row L-R6 cannot pass (CAT-034:250).** "The Bowling ball tips the beam to its stop and the Basketball does not" on an empty beam: a 1 kg Basketball at 1.2 m gives 11.8 N·m against 2 N·m hinge friction (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:105`), and the spec's own bound at :261 says 4.4 N·m tips it. Replacement: "Given a 2 kg Weight resting at x = +1.2 m, When a Bowling ball, then a Basketball, is set on x = −1.2 m, Then the Bowling ball carries the beam to the opposite stop (within 2°) and the Basketball leaves it on the counter-load's stop (within 2°); 3 s."

**F9. Tennis ball window edge is false once part B2 lands (CAT-016:324, :289).** `I/spec-elements-visible-solid-remainder-b2.md:108`: with the seam collider the largest seated radius falls from 0.42 m to 0.407 m, and between them seating depends on how the ball has turned. B2 precedes part C. Replace :324 with "Tennis ball: seats for radius 0.30–0.407 m at any turn (0.407–0.42 m depends on seam orientation once part B2 lands)". CN-R2: positive at 0.35 m, refused at 0.25 m and 0.45 m; leave exact edges to `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries`. Also state widths with bands: Basketball 0.68–0.70 m, repaired Baseball 0.52–0.54 m (outcomes unchanged).

**F10. Island sleeping is claimed in six specs; the engine has none.** CAT-034:302, CAT-067:248, CAT-057:265, CAT-051:288, CAT-042:276, CAT-019:305. No match for sleep, awake or deactivation in `Sim/`; it is design text only (`docs/gpu-f32-physics.md`). No `ENGINE-*` story. Replace each with "The engine has no island sleeping today; a resting part still costs its broadphase proxies and rows every substep. Gap, owner E6."

**F11. The Optics palette category has no engine owner (CAT-007:120, :259; G8).** `A/ElementCategory.cs` has Motion, Goals, Power, Control, Structure. The spec and G8 give the enum change to the first optical element slice, against the shared acceptance "the first consuming element is declarations, art, bindings and tests only" (`_bmad-output/planning-artifacts/epics.md:4420`). Own it in `ENGINE-OPTICAL` (E7).

**F12. Beam shutter capability statuses are wrong (CAT-007:108, :115, :117, :118, :261; `batch-c.md:11`).**
- :117 indicator colour is tested: `T/DeclaredPresentationTests.cs:120`, `:184`. :118 closing blade stopped by a ball is tested: `T/LatchSupplyFixtureTests.cs:289`, `T/DeclaredPrismaticDriveTests.cs:387`.
- :115 a package rope port on a slider body is marked present but :262 admits only the hinge case is tested; `T/RopeTests.cs:102` is a law fixture.
- Corrected counts 6 / 1 / 0 / 2; conformance "Conforms with fixes".

### Medium

**F13. The closed shutter blade does not cover the opening (CAT-007:31).** Closed blade spans y −0.6 to 0.6; the opening runs from the foot top (−0.68) to the header bottom (1.025). Replace "covers any beam that fits between the rails" with the covered band, restrict S-10 beams to |y| ≤ 0.55, and record the 0.425 m upper gap for the owner.

**F14. Beam shutter numbers and rows a builder would have to decide.**
- `SupportedDomains` must add `Rope`; `Loc_Port_Tie` must be a direct child of `Visual/Blade`.
- `Tie` at blade-local (0.035, 0.5, 0) (:37) rises to y 1.8 at full travel, inside the header box. Use (0.035, −0.5, 0) with the S-8 pulley at x ≥ 0.3.
- Colour (0.97, 0.8, 0.32) is not the gate's gold (0.97, 0.796, 0.322) (`PoweredGateElementDeclaration.cs:28`).
- S-2 (:212) cites a "2%" allowance the gate test does not use, and bounds per-tick acceleration (a trajectory assertion). Replace: "speed ≤ 2.8 m/s + 1e-3 on every tick; the ramp from rest to cruise takes at least 0.2 s less one tick".
- S-6 (:216): a ball on the bench meets the 0.2 m foot, not the blade. Add an approach surface level with the foot top.
- :203, :211 invent a beam half-width; `ENGINE-OPTICAL` states none. Write "(plus the transport's beam radius, if the story declares one)".
- :225 maps "collider and visible pose agree" to a static table row. Add S-14 `BeamShutterArtTests.TheBladeArtFollowsTheCommittedBladePoseAndResets` (1 mm at closed, mid-travel, open, then Reset).
- S-8 control uses a 1 kg Weight against a 9.81 N blade: a knife edge. Use 0.5 kg. S-3: use the Battery's `RunControlLane.SourceEnable`.
- :260 files list omits, before S3, `BasketballResourceTests.cs:34`, `WorkshopIntegrationHarnessTests.cs:691`–`:699` and the slider rows at `PackageRuntimeParityTests.cs:383`–`:400` (`I/spec-s3-remaining-per-package-lists.md:52`–`:56`, `:84`).
- :117 cites `A/ElementPresentation.cs:23`; it is `:24`.

**F15. Owner item O1 is half-recorded and its alternative is overstated (CAT-051:241 against :284; `batch-c.md:61`).** CAT-051 says "none needs the owner" while raising the item. Neither spec cites the delivered test that pins the hang (claim 5). "About 265 N" is the shutter's figure; the gate needs about 314 N at the same sag. A cheaper general alternative exists: a declared spring rest coordinate (preload). With rest 0.85 m below closed at 14 N/m the inverted blade stays closed and upright opening needs about 40 N (shutter) or 43 N (gate), inside the present 60 N. Preload through limits is refused today (`CuriousContraptions.Bundle/MechanicalValueDomain.cs:81`). Also correct `PoweredGateElementDeclaration.cs:27` ("Without power it returns closed") in the next package edit.

**F16. Trampoline sag: a requirement criterion deferred without an owner decision (CAT-065:156, :237, :277).** "Direction-aware bounded sag follows committed deformation" (`requirements.md:848`) is delivered as one `LocalY` translation of a flat slab (`TrampolineElementDeclaration.cs:53`–`:54`). The deferral is a review-loop amendment (`I/spec-10-5-trampoline.md:147`, `:643`). A shallower second load visibly hovers above the slab (`:801`). Reword :156 to "open requirement criterion; deferred in review without an owner decision; no story owns it", state the hover, and move it to "Needs the owner".

**F17. Clutch and Reverse gear art repair has no contract (G4): confirmed.** Dimensions in claim 4; the only mention anywhere is "to be measured" at `I/spec-engine-art-solidity.md:200`. G4's part D text is adequate. Add: the repair keeps node names `Visual_InputSpoke`, `Visual_OutputSpoke` (`T/ClutchTests.cs:1504`–`:1505`). Unstated in the specs: the Clutch axle is directly visible over |x| 0.0325–0.16 and 0.34–0.475 when open; both Reverse gear wheels float 8 cm in front of the housing with no drawn axle.

**F18. Motor index bar is 4 mm proud, not flush (CAT-042:85, :240; `batch-c.md:17`).** `elements/Motor/motor.tscn:51`–`:53` box 0.48 × 0.07 × 0.004 at z 0.067 (`:135`) on a rotor of half-width 0.065, on a hinge body with no binding. The print band elsewhere is 0–0.5 mm (`T/FaceMarkingTests.cs:60`). Add it to part D as a quad at most 0.5 mm proud. Motor conformance becomes "Conforms with fixes".

**F19. Test "classes" that are file names (five delegate specs).** `ReverseTransmissionLoadTests`, `…StateTests`, `…LifecycleTests` are files of `partial class ReverseTransmissionTests`; `ClutchGeneralityTests` is a file of `ClutchTests`; scene tests are `ClutchArtTests`, `ConveyorArtTests`, `MotorRotorArtTests`. Affected: CAT-057:110–114, :208–214, :224, :228, :252, :266; CAT-018:116, :118, :119, :218, :221, :223, :224, :271; CAT-019:129, :239, :255; CAT-042:105, :216; `batch-c.md:92`. Write `ReverseTransmissionTests.<Method>` (file …) and so on.

**F20. Conveyor planned rows.** CAT-019:242: at 0.1 N·m the net torque is 0.03 N·m and a 1 kg ball moves about 0.07 m in 2 s, so the control ">0.5 m" fails. Replacement: "torque 1 N·m; stalled against a Wall: roller turns < 0.05 rad in 2 s and committed work ≤ 0.05 J; control without the Wall: the ball travels > 0.5 m in 2 s." CAT-019:254 records a positive-only `surface_per_radian` as delivered though the harvest has it signed (:85): add the decision "reversal is the Reverse gear's job". Rows :243–245 lack a mechanism (underside contact), a single outcome (dimensions) and a tolerance (treads).

**F21. Clutch planned axle test (CAT-018:226).** A sphere resting on top of a horizontal r 0.055 m cylinder is unstable, and both controls need the pre-repair scene. Replacement: "Drop row `ClutchAxle`: a 0.07 m Tennis ball dropped at x 0.40, z 0 has its lowest centre within 5 mm of 0.055 m + radius above the axle line; control: the same drop at z 0.2 reaches the base." Same fault at CAT-057:216 (control at "the baseline scene").

**F22. Impact lever: requirement criteria dropped or weakened.**
- :246 L-R2 uses four yaws for "four 3D orientations"; the harvest used (0,0,0), (15,30,0), (90,0,0), (0,0,90). Restore or record a decision.
- No row for overlap rejection (`requirements.md:2222`): add "L-R9 `ImpactLeverTests.ABeamOverlappingAnotherPartAtRunIsRefused`, typed `InitialOverlap`; control 1 cm clear".
- No row for a moving obstacle or the workbench, nor for artwork-follows-pose.
- :245 L-R1 "never leaves the beam" conflicts with its tolerance; use "payload height never exceeds its start by more than 5 cm". :249 uses atan(0.3); mixed friction with the Domino is √(0.3·0.6), about 23° (conclusion stands).

**F23. Present rows whose cited test does not exercise them; "both opened" is untrue.** Each probe line says symbol and test were both opened (CAT-065:144, CAT-034:118, CAT-067:107, CAT-053:105, CAT-058:103); `batch-c.md:91` says tests were found by name. Replace with the latter.
- CAT-034:126 (fulcrum, stops, base colliders): no test touches them. :125 (`beam_mass`): no test varies it. :124 should cite `RunResetRestoration_RestoresAuthoredStateExactly` (`T/ImpactLeverTests.cs:484`). Counts 6 / 2 / 0 / 0.
- CAT-053:109 and CAT-058:107: nothing contacts `col_mount` or `col_wheel`. Counts 6 / 1 / 0 / 0 each. CAT-058:108 should cite `T/RopeTests.cs:21`, `:490` for the Anchor role.
- CAT-067:120, :234: "no test reads the `Visual` scale against `mass`" is false (`T/WeightTests.cs:174`–`:178`). Narrow R1 to the evaluated node scale.
- CAT-018:115, CAT-057:109, CAT-051:117, CAT-019:127–128: cite the discriminating tests instead (`MidRunCoilCut_…` `ClutchTests.cs:567`; `EveryCurrentPairingCarriesCargoWithReversalParity`; `DeclaredPresentationTests.cs:120`, `:184`; `DrivenSurfaceBindingTests.cs:379`, `:576`; `MechanicalCoordinateBindingsTests.cs:874`).
- Wrong-kind refusal (CAT-034:239, CAT-067:208, CAT-058:203): `T/BundlePortsTests.cs:262`–`:284` has no Rope case. Add one shared row `RopeTests.ARopePortRefusesANonRopeLinkAndANonRopePortRefusesARopeLink`, typed `InvalidConnection` (Batch J audit L7 agrees).

**F24. Symbols that do not exist.** CAT-042:108 `ElectricalSupply.Allocate`: actual `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:10`), called from `Sim/WorkshopPhysicsEngine.Supply.cs:75`–`:121`, serving store charging and electrical airflow demand. CAT-051:106 `PrismaticConstraintSolver.ApplySuppliedDrives`: actual `PrismaticMotorSolver.BeginSubstep/Solve/Observe/MeasureWork` (`Sim/PrismaticMotorSolver.cs:24`, `:70`, `:99`, `:117`); `ActuatorDriveRead.Blocked` is `ActuatorObservations.IsBlocked` (`engine/gpu/PhysicsPresentationRead.cs:15`). `I/deferred-work.md:212` carries the same wrong name.

**F25. Linear pusher drawn-proxy list (CAT-039:48).** Only `col_Foot` and `col_Barrel` are drawn proxies (`elements/LinearPusher/linear_pusher.tscn:50`, `:53`); the head has separate art and a `CollisionShape3D`. Replace "foot, barrel and head" with "foot and barrel". :236 "rows B3–B9" should be "B3–B6, B8, B9".

**F26. Linear pusher art section is stale beside current text (CAT-039:100–107).** Delivered: ring inner 0.235, outer 0.255; marks 0.022 × 0.1 × 0.012 at z 0.333; indicator r 0.065 at (−0.4, 0.2, 0.34); sockets r 0.07 navy; pick radius 1.3. Replace the bullets (forward-only). Same class: CAT-067:49, :54, :56 (one cylinder; inertia compiled from the collider) against declared polynomial moments (`elements/Weight/WeightElementDeclaration.cs:95`–`:107`).

**F27. Decisions attributed to the owner that are not the owner's.** CAT-053:41, :249: the rim collider is decision D4, "coordinator decision … under the owner's standing delegation" (`I/spec-elements-visible-solid-remainder.md:122`). CAT-065:156 as F16.

**F28. Shaft sign statement is partly false (`batch-c.md:83`; CAT-018:244; CAT-057:238).** −1 per radian is declared by both Reverse gear ports, the Wound spring `DriveIn` and the Fan `DriveIn` (`elements/Fan/FanElementDeclaration.cs:66`). Replacement: "+1 on Motor, Conveyor, Clutch and Windmill; −1 on both Reverse gear ports and on the Wound spring's and Fan's `DriveIn`." CAT-057:238 "the pairing tests cover every combination" is false: `T/ReverseTransmissionTests.cs:17`–`:18` lists Motor, Clutch, Conveyor, Reverse only. CAT-042:247 omits the Fan as a drive collaborator.

**F29. Rope anchor geometry (CAT-058:29, :36).** `col_mount` is at (0, 0, −0.12), not the origin (`elements/RopeAnchor/rope_anchor.tscn:33`). The eye reaches 0.2475 m in front of the mount face, not 0.1275 m, and its back floats 0.19 m off the face.

**F30. Wound spring mark bindings contradict part B (CAT-071:274, :264).** Part B `:116` keeps the `stroke` height bindings. Replace "(0 after the marks are printed)" with "(unchanged by part B; the marks become quads at z 0.1002)".

### Low

- **L1.** CAT-016:43: part C reads the ring "from its vertices and triangles", not "by its bounds" (`I/spec-elements-visible-solid-remainder-c.md:142`). CAT-016:288 CN-R1 "within one tick of the quotient" is meaningless at 1 J ÷ 100 W; use corners (1 J, 1 W) and (100 J, 100 W).
- **L2.** Floating art not recorded: Cannon sockets (3 cm above the foot, 12.8 cm off the jacket); pusher `RetractedOut`/`ExtendedOut` sockets (7.6 cm clear); Wound spring socket, belt wheel and latch lever (0.35–0.39 m in front of the rails); about 0.4 m of Wound spring coil exposed below the sleeve with no collider.
- **L3.** CAT-071:256 "Weight up to about 5.2 kg" uses the body radius; its band collider reaches the 0.41 m bore at about 5.0 kg (band shape not re-opened).
- **L4.** Existing tests that break the float rules, to fix at their next edit: exact `Vector3` equality on replayed state (`T/ConveyorTests.cs:411`–`:412`); 1e-6 J allowance (`T/LinearPusherTests.cs:52`); fixed ticks 40, 60, 80, 100 (`T/RopeTests.cs:842`–`:879`); `Assert.Equal(3f, …CurrentLength)` (`T/RopeTests.cs:498`); speed strictly decreasing every tick (`T/LatchSupplyFixtureTests.cs:199`–`:200`); a tautology at `T/DirectionalPrismaticTests.cs:53`.
- **L5.** "The replay is identical" (CAT-065:231, CAT-051:215) reads as whole-run identity; the tests compare decoded poses or coverage of the cut. Reword.
- **L6.** Bare hedges and stale text: "candidate — Batch A confirms" (CAT-019:14, CAT-018:14, CAT-057:17) against the specs' own closures; CAT-042:88 against :260; CAT-057:262 "decided in section 6" (it is section 2); CAT-067:17 requirement still says "spherical body/socket" (`requirements.md:860`).
- **L7.** `batch-c.md:18` gate tier "Data-only" disagrees with the spec's drain row; `:95` calls the Balloon pairing "a residual row" but CAT-065 has none (add `TrampolineTests.ABalloonAtItsMinimumMassReboundsWithoutEnergyGain`); G10 should add `I/spec-c-gate.md:3` (`status: review`); G12 omits that Trampoline posts and feet are not coaxial, so merging is an art redesign.
- **L8.** Generality residues, record only: socket glyphs `Extend/Retract/Extended/Retracted` are hard-coded SVG in `ui/WorkshopConnections.cs:41`–`:44` and used by one package; `PresentationDriver.CommittedWheelAngle` is product-flavoured; `ChooseWinding` prefers the higher wrap (`engine/gpu/RopeGeometry.cs:188`–`:199`) and a guide cannot declare otherwise; finite-work release needs a full store (`Sim/WorkshopPhysicsEngine.FiniteWork.cs:106`), one occupant, cylinder regions and axial release, which a multi-shot launcher (EL-095, EL-225) cannot declare. Proposed line for the last: "a finite-work actuator declares its minimum releasable store energy (default: capacity)".
- **L9.** `RopeGeometry.EvaluateRoute` is at `engine/gpu/RopeGeometry.cs:83`, not `:36` (CAT-053:111, CAT-067:116, CAT-058:113). `ParameterDescriptor.cs:59`–`:60` should be `:60`–`:61` (CAT-034:30). CAT-058:223 should cite `LinearPusherTests.cs:179`.

## 4. Cross-batch conflicts

- **C1. Non-solid strands.** Batch C's O2 asks to confirm "the rope"; the Batch I and J audits settle on one decision for every connection strand (wire, belt, chain, rope, cable) (`.tools/audit/elements-batch-j.md:172`, `:192`). Use that wording. O2's "Blocks: nothing" conflicts with `.tools/audit/elements-batch-i.md:113` ("a review Fail" without the citation). `I/spec-engine-art-solidity.md:27`, `:113` reads package scene art only, so it never classifies the engine-drawn rope: it needs a scope line for connection art.
- **C2. EL-205 Rope.** `batch-c.md:36` and CAT-058:233 close it as a duplicate with no caveat. Both other audits require the sentence: "the connection-material story converts the delivered rope in the same change and re-runs the closure rows" (`elements-batch-i.md:168`, `elements-batch-j.md:169`). Add it.
- **C3. EL-064.** CAT-058:232 "admits by domain and direction only" is wrong (F6); its closure row C4 depends on the player cut that Batch I's O2 would remove (`elements-batch-i.md:114`).
- **C4. EL-207 Moving pulley.** CAT-053:237 omits its engine blocker: tension is not applied to a dynamic guide body (`elements-batch-j.md:182`).
- **C5. Cannon window.** No conflict: CAT-016, `I/spec-family-ball-presets.md:172`, the Baseball repair contract (`I/spec-repair-baseball-el-239.md:50`) and both ball audits agree the Baseball (0.52–0.54 m) never seats and the Cannonball and Soccer ball (0.64–0.66 m) do. The only defect is F9.
- **C6. Agreements.** EL-064 and EL-206 as duplicates with no delta, G9, EL-207 as a separate element, and runtime cutting kept all agree with Batches I and J. Palette order 47 is free among delivered packages (1–12, 15–42, 64–67, and 68 on main) and in every readiness table present; no `batch-e.md` exists to check.

## 5. Engine-story gaps, with proposed element-free text

- **E1. ENGINE-PARAMETER-BOUND-FRAMES (G1, amended).** "A declared real parameter may drive a hinge or slider base frame, a collider component of any body of the declaration, a mechanical rim centre, and a port position from more than one parameter. Bundle admission refuses a declared parameter that binds nothing. Given two differently sized fixtures, when compiled, then joints, rims, ports and colliders are at their evaluated positions; out-of-range values refuse atomically; Reset and save/load preserve them." G2 (a value mapping that wraps a committed coordinate into a declared pitch) belongs in the same story or a shared-presentation slice.
- **E2. ENGINE-SUPPLY-DEMAND (G3, amended).** "Each drive's committed positive work and any declared standing demand are a request lane of the existing source reservation (not a second allocator), debited within the source's power bound; an exhausted source opens the route. The story either defines the electrical node's rated values as that standing demand or deletes the unread fields. Acceptance: two differently rated fixtures drain a finite source at their declared rates; an unsupplied consumer debits nothing; Reset restores the source." Serves revolute and prismatic drives and coils; store charging already debits.
- **E3. ENGINE-COMPLIANT-SURFACE (replaces G5, G7 and part of G6).** "Surface admission bounds tension and damping only by the law's stability condition for the admitted body-mass range; product ranges stay in package parameters. Each declared surface queries the broadphase with its bounded stroke volume; only overlapping bodies are sampled, over their own colliders; engagement rows are sparse. A declared surface applies tangential friction, and rolling resistance for spheres, per bearing sample, energy-removing only. Acceptance: an unfamiliar surface outside today's product range is admitted and settles and an unstable one refuses with a typed reason; per-substep surface work is independent of bodies outside every surface volume; a load sliding along a surface comes to rest and a frictionless declaration does not; no tick gains energy." G6 (per-sample committed deflection read and a height-field binding) stays a shared-presentation slice and needs the owner (F16).
- **E4. ENGINE-BOUNDED-QUERIES.** "Region occupancy, exit-clearance sweeps and bounded drive power are evaluated without per-substep work proportional to the scene: regions and sweeps query the broadphase with their bounded volume; a bounded prismatic drive enforces its power bound inside its own row with no world snapshot or re-solve. Acceptance: two differently rated fixtures stay within bound under load; results for bodies inside a region are unchanged."
- **E5. ENGINE-ROPE-PORT-ROLES.** "A rope port is admitted by domain Rope, Bidirectional and its declared attachment role; port identity is the typed id, and no admission site enumerates role names. Acceptance: an unfamiliar package with three rope ports is admitted and routes; a rope port refuses a non-rope link with a typed reason."
- **E6. ENGINE-ISLAND-SLEEP.** "Islands whose bodies stay under the envelope's rest speeds for the documented dwell skip broadphase, narrowphase and constraint solve and wake on contact, joint, drive, supply or command change. Acceptance: two differently built resting assemblies do no solver work and wake within one tick of a touching body or a supplied drive." The dwell and rest speeds must come from `docs/gpu-f32-physics.md`; none is invented here.
- **E7. Additions to `ENGINE-OPTICAL` (replaces G8).** The story text (`epics.md:4434`–`:4443`) covers occlusion "through the existing BVH" and a fixed evaluation order, but not dynamic bodies, the pose used, beam width, or the category.
  - "**And** occlusion is evaluated against every participating solid collider, static or dynamic, at the pose committed by the physics phase of the same tick. A body moved by a joint, drive, rope or contact blocks or clears a path in the tick its committed pose crosses it, independent of any commanded or supplied state. Qualification: a driven prismatic body crossing a path, with the same body held clear as control."
  - "**And** the transport states its geometric model once: zero-width rays, or a declared beam radius with a defined result for partial occlusion."
  - "**And** the story adds the optical member of the closed palette-category enum with its palette heading, bundle admission and listing, proven by a qualification declaration, so no element slice edits the enum."
  - "**And** a receiver's committed reading is consumed by signal and electrical solves at the documented tick, and Reset clears integrated state."
- **E8. Optional, only if the owner wants fail-closed in every orientation (F15).** "A declared spring on a prismatic or revolute coordinate may declare its rest coordinate. Acceptance: two fixtures with different rest coordinates settle at the positions gravity and the declared spring give; a rest coordinate outside the documented range refuses."
- **Part D (G4), amended.** Add the Motor's index bar (F18).

## 6. Items for the owner

| # | Item | Recommended default | Blocks |
|---|---|---|---|
| 1 | O1: inverted gate or shutter blade hangs 0.70 m open, weakening "loss closes it" under "rotated mounting" | Accept; qualify rotated mounting at yaw 90° and roll 90°; correct both descriptions. If fail-closed everywhere is wanted, take E8, not a 265 N drive | Wording of CAT-007 S-7 and the gate description |
| 2 | O2: non-solid strands | One decision for every connection strand (C1) | Acceptance of EL-205 and EL-067; a scope line in `ENGINE-ART-SOLIDITY` |
| 3 | Trampoline direction-aware sag (F16): a requirement criterion deferred in review with no owner decision | Schedule G6 after E3; until then record the hover as a known visual defect | Closing CAT-065's requirement row |
| 4 | Whether the engine gaps E3, E4 and E6 become stories now or stay with the performance gate | Make them stories: each is a known scene-proportional cost or a design-guide breach, and the gate only measures | Conformance of CAT-065, CAT-016, CAT-051 |
| 5 | G12 "draw sockets flush" reverses the 21 November decision that sockets get sphere colliders | Keep the colliders | Nothing |
| 6 | Shutter upper gap of 0.425 m above the closed blade (F13) | Keep the legacy geometry and restrict lesson beams to the covered band | CAT-007 S-10 |

Decisions under the delegation that were checked and found defensible: no pendulum damping law; winding side by geometry; hinge limit and stop boxes both kept; `tension` name and unit kept; ideal self-locking hold; Bowling ball ineligible by the declared window; `length` and `width` kept and to be bound; gate bore equal to the pipe bore. Not recorded as decisions but should be: positive-only `surface_per_radian` (F20); travel marks as a fixed scale against "follow actual motion/work" (`requirements.md:692`, CAT-039:238); L-R2's orientation set (F22).

## 7. Published main

`origin/main` (`25df3cd5`) is two commits ahead: `fa86969f` (Baseball, named-source coverage) and `25df3cd5` (test compile glob). Neither touches the 13 packages, their tests or any cited engine file. Stale as a result: each spec's "checked on `8d1d68aa`" baseline line; any delivery section that still lists a `Compile` line for a new test file (the glob removes it; CAT-007:260 should say so); and palette order 68 is now taken by the Baseball (no clash with 47).

## 8. Not verified

- No build and no test run; pass status of every cited test is unknown.
- Test bodies not opened: `RevoluteDriveTests` 195, 238; `RevoluteHingeFrictionTests` 59; `RunControlTableTests` 222; `ClutchTests` 317, 440, 659; `MotorTests` 469–730, 865–985; `CannonIntegrationTests` 183, 230, 407–550; `LinearPusherTests` 65, 249–333; `WoundSpringControlTests` 83–250; `PrismaticRetentionTests` 104–419; `RopeTests` 261, 301, 568–644; `WeightTests` 448–805; `TrampolineTests` 660–1250, 1312–1700, 1882–2190; `CatalogueIntegrationTests` 139, 273.
- Section 4 of every spec, and `@a6c914e` citations left in sections 2 and 3.
- Whether a package rope port on a slider body compiles and couples (CAT-007 S-8); only the law fixture was read.
- Costs in F3 and F7 were read from the loops, not measured.
- The 2 November owner decision on bed friction is taken from `I/spec-10-5-trampoline.md` and `I/deferred-work.md`, not a primary owner record.
- DESIGN.md colours and icons; the EL-205, EL-206 and EL-207 specs; Batch E's palette plans (no readiness file exists).
