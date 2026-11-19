# Epic 12 Context: Conserved Airflow, Pneumatics & Buoyancy

## Goal

Deliver conserved airflow, pressure and buoyant gas behavior through reusable physical laws. Story 12.1b Balloon is natively implemented and independently reviewed under the [native contract](spec-12-1b-balloon.md). It restores CAT-003 and EL-208 configurable rise, contact, tether, gas loss and selected-goal behavior. This context summarizes the contract; current verification and publication receipts remain in the native record. Other Epic 12 elements remain separate slices.

## Stories

- Story 12.1a: Tennis ball — published prerequisite.
- Story 12.1b: Balloon — native implementation/review complete; full6912/0/0, E9 PASS and protected review hooks134/0/0. Final publication identity is tracked in the native record/current TODO; browser qualification remains pending.
- Story 12.1: Tennis ball and Balloon — aggregate parent only.

## Requirements & Constraints

Preserve five typed BuildOnly controls: mass, bounce, reference radius, buoyancy and drag. Defaults remain 0.5 kg, 0.36 m, 0.2, 11.5 m/s² and 0.4 s⁻¹. CAT harvest controls the default net rise of 1.69 m/s² and approximate terminal rise of 4.2 m/s; it supersedes the inconsistent PRD net 1.2 description. Friction is 0.3, restitution threshold 0.1 m/s and rolling resistance zero. Product ranges and full admission rules are in the native contract.

The full Standard Balloon rises until supported by a ceiling/wall. Its harvested lift acceleration is mass-independent; the heavier no-rise control is a heavier nonbuoyant body, not a mass-only change to the same full Balloon. Tether tension opposes ascent. A qualified impact opens finite discharge, reduces lift and leads to descent. Ordinary ceiling support and weak-impact controls remain intact.

Restore authored configuration, pose, selected environment, full/intact gas state, tether and Waiting goal on Reset. Saves persist authored inputs and exact selectors, not runtime puncture or solved state. Invalid parameters, references, environment choices and stale work refuse atomically. Native production checks exercise real ticking and lifecycle. Apply [testing guidance](../../docs/testing-guidance.md#float-comparison-and-effort-rules): quantity-specific gameplay/interface tolerances, generous finite stall guards and causal occurrences; no output hashes, incidental tick goldens, skipped game tests or mutants.

## Technical Decisions

Keep Godot native .tscn artwork and explicitly constructed, deterministically owned C# behavior components with private native-resource ownership. Preserve the established alternative to the wrapper lifetime/cache path. No Godot replacement, native fork or ABI change is part of this work.

The sole architecture is [element package isolation](../../docs/architecture/element-package-isolation.md). Packages provide declarations/assets and depend on abstractions, never engines or siblings. Exact body, collider, store, outlet, port and observation references select shared capabilities. C# f32 WASM SIMD simulation owns physical state and occurrences; animation consumes committed values and never writes physics. No element callbacks, identity-selected laws, compatibility readers or fallback paths.

Approved bounded choices:

- **Environment:** Standard and Vacuum are typed persisted BuildOnly selections. New constructions explicitly start at Standard; missing or unknown serialized values refuse. Standard boundary pressure is 101325 Pa and relative factor 1; Vacuum is zero. The atmosphere is an explicit unbounded boundary, as EL-125 specifies. Finite inventory belongs to sealed stores. Unused ambient density is not added; its actual consumer and EL-125 lesson obligations remain separate.
- **Gas:** one constant positive gauge-pressure compliant ideal-gas law with finite mass/internal energy/volume, explicit envelope potential and boundary-work/enthalpy accounting. A pressure-conductance outlet opens after rupture; the package calibrates approximately one-second loss. Empty is a defined zero-inventory state, not a division or hidden refill. Reference charge is prepared adiabatically for the selected boundary, so Vacuum changes actual volume and size.
- **Mass and size:** one discriminated body mass source owns ordinary mass/moments or explicitly referenced centered gas inventories plus a rigid dry carrier. Reference gas mass is subtracted from initial total mass; current gas contributes translational mass, with fixed declared dry inertia and isotropic exhaust accounting. The same committed radius drives sphere collision/query geometry and envelope presentation. A positive dry core remains at empty. Prepared-size overlap is checked before accepting an environment edit.
- **Contact and goal:** per accepted substep, qualified contact points contribute final compressive normal impulse to their exact collider pair. Balloon declares minimum approach speed 0.1 m/s and rupture strictly above 3 N·s. No cross-pair/substep accumulation or speculative-trial occurrence is allowed. One IntegrityBroken occurrence opens the leak; discharge begins next substep. The goal selects an exact owner/integrity observation, consumes current-epoch committed truth and deduplicates its cue. Another store or stale epoch cannot solve it.
- **Tether:** the package declares an honest fixed center Tie on the dry carrier, using the existing tensile-only rope law. The visible string emerges from that internal attachment; the cosmetic neck is not a falsely advertised bottom socket. Slack, taut, disconnected and tethered-rupture cases are required.

Published Tennis supplied five-control, material, parameterized-mass and native-art patterns. Its original normalized-pressure proof remains historical scoped evidence. This slice updates affected current consumers to explicit Standard/Vacuum behavior and the single mass-source representation; it does not rewrite Tennis's published intent or receipts.

Apply primary-workspace E1–E9 and D1–D7 guides through the Auto loop protocol. The planning, six-layer implementation review and all affected repair reviews passed. Matching full local proof is6912/0/0, E9 has zero violations and protected review hooks passed134/0/0; the native record identifies exact approval and remaining completion-publication steps. This is scoped native implementation proof, not browser/release qualification.

## UX & Interaction Patterns

Balloon owns a pink envelope, tied neck, cream string and matching icon. Preserve the approved palette and readable native geometry. The radius control names its Standard reference meaning; actual prepared/committed size changes with gas state. Build controls and the environment picker edit typed authored values and reject invalid or Run-time changes truthfully. Removing cosmetics cannot alter physics.

Retain real palette/gizmo construction, configuration, connection, motion, Reset and save/load recipes for post-catalogue Chrome qualification. Browser suites remain deferred until every named element passes native unit/integration checks. Native checks do not qualify browser appearance, remote artifacts or release readiness.

## Cross-Story Dependencies

Tennis 12.1a was published at 15c8681d4db738f0adba148c4afd1ccf8595e74e. Its [native record](spec-12-1a-tennis-ball.md) preserves original scoped proof. The prior broad phrase finite atmosphere is resolved here as declared ambient boundary plus finite sealed contents, without altering historical evidence.

Existing 10.2b rope supplies unilateral tension, slack freedom, typed cut/refusal and authored restoration. Balloon first proves its bounded gas/compliance/discharge primitive and then its consumer. S470 does not become a completed domain parent: S471 connected-volume compression/valve isolation/exhaustion remains required before dependent reservoir/valve/actuator modes; S697 open-field versus sealed-port combinations remain with their consuming integrations. Named-element ordering records those actual dependencies rather than treating every future gas process as Balloon's prerequisite.

Fan 12.2 owns conserved jet guidance and no-supply/missed/occluded controls with Balloon and other bodies. Free rise does not qualify airflow. Windmill, Bellows, hot-air Balloon, Blimp and full atmosphere campaign/density consumers remain separately owned.

Scoped authorities: [catalogue ownership and parent 12.1](../planning-artifacts/epics.md#catalogue-slice-ownership), [PRD](../planning-artifacts/prd.md), [CAT-003](../../docs/planning/elements/CAT-003-balloon.md), [EL-208](../../docs/planning/elements/EL-208-balloon.md), [gas family](../../docs/finite-gas-foundation.md), [EL-125](../../docs/planning/elements/EL-125-authored-atmosphere-preset.md), [rope](../../docs/planning/elements/CAT-058-rope_anchor.md), [mode-specific capability map](../../docs/planning/general-engine-element-map.md), [decisions](../../docs/planning/invest/decisions.md) and [roadmap](../../docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). Primary TODO/custom guides govern; unrelated primary policy/Conveyor changes remain separate. Candidate checkout /Users/aidan/.codex/worktrees/tim-12-1b/tim, branch story-12-1b-balloon, retains the published Tennis revision above as its original baseline.
