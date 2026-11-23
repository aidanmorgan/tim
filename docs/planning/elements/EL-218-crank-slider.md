# EL-218 · Crank-slider — named-identity spec

**Current delivery (21 November 2026):** [EL-218 ledger entry](../invest/named-elements.md#element-218), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)), and re-planned the same day under coordinator decision 29: the rod is a body pinned at both ends by `ENGINE-JOINT-KINDS`' dynamic-support hinges, with its own collider; there is no coupling law.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-218 · Crank-slider · Mechanical |
| Requirement anchor | [todo-423](../requirements.md#todo-423) (`sequence-task-368`) |
| Ledger entry / owner | [element-218](../invest/named-elements.md#element-218); slice `named-el-218`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS` (dynamic-support hinges) and `ENGINE-PARAMETER-BOUND-FRAMES` (the pin and rod bound to `radius` and `rod_length`).** Run "linkages" in delivery batches 1c. Not a duplicate of [EL-053 Rotary-to-linear converter](EL-053-rotary-to-linear-converter.md): that coupling is a constant ratio; this one is a real rod, periodic and position-dependent. |
| Planned package | id `crank_slider`, directory `elements/CrankSlider/` |
| Related | [EL-219 Rack-and-pinion slide](EL-219-rack-and-pinion-slide.md), EL-156 to EL-158 cams, [CAT-071 Wound spring](CAT-071-wound_spring.md) (hinge plus slider plus transmission shape) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (a stopped crank and carriage rest); then `ENGINE-JOINT-KINDS` (a rod pinned at both ends by dynamic-support hinges, coordinator decision 29) and `ENGINE-PARAMETER-BOUND-FRAMES` (section 3) |

## 2. Declaration

Shape: the Wound spring's hinge and slider (`elements/WoundSpring/WoundSpringElementDeclaration.cs:8`), joined by a rod body pinned at both ends instead of by its transmission.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISliderElement` (`:15`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the dynamic-support hinge `ENGINE-JOINT-KINDS` delivers (today a hinge joins a support to its own body only). No transmission.
- **Frame (body 0, static).** Bed 2.8 × 0.2 × 0.6 m with a bearing block. Restitution 0, friction 0.3.
- **Crank (body 1).** Disc radius 0.55 m, width 0.1 m, mass 0.5 kg, solid-disc inertia; continuous hinge, friction torque 0.05 N·m (holds the phase when the drive stops).
- **Carriage (body 2).** Box 0.4 × 0.3 × 0.5 m, mass 0.5 kg, on a slider along the bed; its front face is the pusher face (restitution 0.1, friction 0.4).
- **Rod (body 3).** Bar `rod_length` × 0.06 × 0.06 m, mass 0.3 kg (the rod values are adopted, coordinator decision 37, 23 November 2026), inertia about its centre m·L²/12 (0.020 kg·m² at the default 0.9 m), in the plane 0.09 m in front of the crank's axis so its back face clears the disc face (z 0.05) by 0.01 m. One end is pinned to the crank at the pin, distance r = `radius` from the crank axis at angle `phase`; the other to the centre of the carriage's crank-side face. Both pins are dynamic-support hinges, axis Z, no friction, no spring, no drive (`ENGINE-JOINT-KINDS`, coordinator decision 29); collision between the rod and the two bodies it pins is excluded by those joints. The geometry gives the carriage's pinned point x(θ) = r·cos θ + √(L² − r²·sin² θ) from the crank axis, θ measured from `phase`: stroke 2r, one stroke pair per turn. It is rigid contact and joint constraint, not a coupling law: a blocked carriage loads the shaft through the rod, and nothing can slip, so the phase is kept.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `radius` (r) | Real, m | 0.1–0.5 | 0.3 | Reject, build-only | Stroke 0.2–1.0 m; the default pushes one ball clear of a lane |
| `rod_length` (L) | Real, m | 0.75–2 | 0.9 | Reject | The floor is 1.5 × the largest radius, so every admitted pair is a valid linkage and no cross-parameter rule is needed |
| `phase` | Real, degree | −180–180 | 0 | Reject | Authored starting angle |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; rim radius 0.26 m on the crank shaft | (0, 0, 0.35) |

- **Presentation.** Crank, carriage and rod body bindings; the rod art's length and the pin boss position follow `rod_length` and `radius`, as the rod's collider and pin anchor do.
- **Palette.** Motion, "Crank-slider"; order 124 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Bed and bearing block | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Crank disc with a gold pin boss | Cylinder 0.55 × 0.1 m, navy `#1c2836` | Solid, cylinder collider on body 1; the boss is flush |
| Carriage | Box, sky blue `#7ebcd4` | Solid, body 2 |
| Rod | Bar 0.06 × 0.06 m, `rod_length` long, ochre `#d99e32` | Solid, box collider on body 3 (visible = solid; coordinator decision 29) |
| Witness marks on the bed at both stroke ends | Printed | Decorative: printed flat |

Icon: a disc, an offset pin, a rod and a block. It differs from the rack (a toothed straight rail) by the disc and angled rod.

### Behaviour and numbers

- Driven at the Motor's default 6 rad/s the carriage makes 0.95 strokes a second; peak carriage speed is about r·ω = 1.8 m/s at the defaults.
- Reversing the shaft retraces the same positions. Stopping the drive leaves the crank where it is; restarting continues from that angle with no jump.
- At the two dead centres the carriage cannot back-drive the crank; elsewhere a pushed carriage turns it.
- Tick order: one mechanical solve; no signal is read.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Continuous hinge with a `DriveIn` rim | present | `PortDescriptor.cs:20` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` |
| Slider with limits carrying a pusher body | present | `MechanicalContractValues.cs:104` | `LinearPusherTests.RotatedSphericalHeadPushesCargoAndLateralClearanceMisses` |
| Constant-ratio revolute-to-prismatic coupling | present | `MechanicalContractValues.cs:258` | `RetentionBoundaryTests.CanonicalBilateralCoordinatePairsSurviveAdmissionAndCommittedAbi` |
| Hinge friction holding a stopped shaft | present | `MechanicalContractValues.cs:158` | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` |
| **A hinge whose support is a moving body, closing a crank–rod–carriage loop** | **missing** | a hinge joins a support to its own body only, so a closed loop cannot be declared | Owner: `ENGINE-JOINT-KINDS` — a rod pinned at both ends by dynamic-support hinges, with its own collider (coordinator decision 29); no new coupling law |
| **A joint anchor, a dynamic body's collider length and its inertia bound to a parameter** (pin at `radius`, rod at `rod_length`) | **missing** | parameter-bound colliders are delivered on static bodies only (`ColliderParameterBinding`, `SceneGeometry.cs:26`) | Owner: `ENGINE-PARAMETER-BOUND-FRAMES` |

Counts: 4 present, 0 untested, 0 narrower, 2 missing. The constant-ratio coupling row is listed as the nearest delivered capability; it is not used.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static bed, a hinged crank, a sliding carriage and a rod pinned to both. The stroke is the joint constraints on that loop with the declared `radius`, `rod_length` and `phase`; holding the phase is the hinge friction law; pushing is rigid contact. The missing capabilities are stated for any package: a hinge whose support is a moving body, owned by `ENGINE-JOINT-KINDS` (coordinator decision 29); a joint anchor and a dynamic body's collider and inertia bound to a parameter, owned by `ENGINE-PARAMETER-BOUND-FRAMES`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 4 boxes (bed, bearing block, carriage, rod) and 1 cylinder (crank) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: none beyond the body poses; the rod's drawn length and the pin boss follow the parameters set in Build. What moves each art node: bed, bearing block and witness marks, nothing (static body 0); crank disc and boss, body 1's pose; carriage, body 2's pose; rod, body 3's pose.
- **Flat state.** 4 bodies; 5 colliders (4 box, 1 cylinder); 4 joints (crank hinge, carriage slider, two dynamic-support rod hinges); 0 couplings; 0 drives; 0 sensors; 0 nodes; 0 stores; 3 body-pose bindings, 2 parameter bindings.
- **Performance.** As few primitives as the Wound spring. The bed is a static body. Whether a stopped crank and carriage may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. Carriage speed peaks at radius × shaft speed: 3 m/s at the largest radius on a default Motor, within the engine's speculative-contact range.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-423](../requirements.md#todo-423): "Signed shaft input produces periodic linear strokes through visible fixed-length rod and offset crank pin. Authored radius, rod length and phase; preserve phase when stopped."
- **Campaign.** Chapter 4 "Gears of an Idea" (31–40); coverage row "… crank-slider, each cam profile, rack-and-pinion".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: carriage position ±0.02 m (a tenth of the smallest stroke); crank angle ±0.05 rad; counts exact; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| C1 | Motor → Mechanical link → crank, one full turn | The carriage travels 2r out and back and ends where it began | Control: no Mechanical link, no motion. Boundary: r 0.1 and 0.5 | `CrankSliderTests.OneTurnGivesOneStrokePair` |
| C2 | Shaft reversed | Carriage positions at equal and opposite angles match | — | `CrankSliderTests.ReverseRotationRetracesTheStroke` |
| C3 | Drive stopped mid-stroke for 5 s, then restarted | The carriage moves less than 0.02 m while stopped and continues with no step larger than peak speed × one tick + 0.01 m | — | `CrankSliderTests.StoppingPreservesPhase` |
| C4 | `rod_length` 0.7, `radius` 0.51, NaN | Refused with a typed reason | Boundary: (0.5, 0.75) admitted and turns freely | `CrankSliderTests.InvalidGeometryIsRefused` |
| C5 | A Wall in the stroke | The carriage stops at it, no penetration, and the shaft stalls | Control: Wall removed | `CrankSliderTests.ABlockedCarriageLoadsTheShaft` |
| C6 | Crank feeding balls from a Ball hopper past a Ball detector into a Counter, a Clock gating the motor | The count equals the number of completed turns | Control: hopper empty, count 0 | `CrankSliderTests.EachTurnFeedsOneBall` |
| C7 | Run, Reset; Save, Load | Authored phase, parameters and Mechanical link restored | — | `CrankSliderTests.ResetAndReloadRestoreTheAuthoredCrank` |
| C8 | An electrical wire to `DriveIn`; a second Mechanical link to `DriveIn` | Refused with a typed reason | — | `CrankSliderTests.WrongKindAndDuplicateDrivesAreRefused` |

**Integration.** Motor, Clock, Counter, Ball detector, Conveyor (delivered); EL-241 Ball hopper (C6 uses it; before it is delivered C6 uses a Funnel). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `crank_slider`; Named identity EL-218; Acceptance todo-423 and sequence-task-368; owner slice `named-el-218`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` delivering the dynamic-support hinge (coordinator decision 29) and `ENGINE-PARAMETER-BOUND-FRAMES` delivering parameter-bound joint anchors and dynamic-body colliders. Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/CrankSlider/` package (project, declaration, scene, icon), `CuriousContraptions.tests/CrankSliderTests.cs`, and the hand-edited lists until S3. Review tier: engine if the dynamic-support hinge is first consumed here; data-only if the engine story's fixtures already prove a closed loop. **Disqualified if** a closed crank–rod–carriage loop of dynamic-support hinges drifts more than the 0.02 m position tolerance from x(θ) over a 20 s run at the Motor's default speed; then the loop's stability is a row of `ENGINE-JOINT-KINDS` before this element.

## 6. Open questions

1. *Is the rod solid?* Resolved by coordinator decision 29 (23 November 2026): the rod is a body pinned at both ends by dynamic-support hinges, with its own collider. Crank, carriage and rod are solid.
2. *Cross-parameter refusal (rod shorter than the crank).* Resolved under the standing delegation: avoided by ranges, so "invalid geometry is refused" is the ordinary range refusal.

Needs the owner: nothing.
