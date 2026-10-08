# Family contract boundaries

Each physical domain is a generic capability family inside the single WebAssembly SIMD f32 physics solver on a dedicated Web Worker ([compilation model](../../gpu-f16-physics.md#compilation-model)), added by the slice that first needs it; animation is a separate compiled model in its own worker; the renderer only reads both. Elements are declaration data (capability families, parameters, art, animation bindings) and never carry their own solver, kernel branch or update loop; the [engine contracts](../../engine-contracts.md#general-data-driven-engines) are the single authority for that rule. Numerical acceptance is the [game-grade envelope](../../gpu-f16-physics.md#game-grade-envelope); the oracle of every proof below is what a player observes in Chrome through real controls at puzzle scale, with Reset and Save/Load where supported. Release-only numbers live in the [P0-034 stage gate](../../delivery-workflow.md#stage-gates). Order of delivery is the [roadmap](vertical-delivery.md#rolling-playable-roadmap).

These are reusable constraints, not INVEST scores or evidence of completion. Unresolved models get one [bounded decision](decisions.md) for the named capability, followed by the named implementation.

<a id="mechanics"></a>
## mechanics

**Boundary:** one declared body/shape/material/joint/force-region law or one element by declaration. **Negotiable:** solver details inside the shared sequential-impulse pass. **Uncertainty:** one reviewed decision before the consuming slice. **Chrome proof:** the loaded element moves as intended; separated/slack/exhausted control does nothing; no free energy (resting body stays, second bounce lower, paid element never fires unpaid); exact Reset/save.

<a id="fluids"></a>
## fluids

**Boundary:** one named open-water transport/storage/converter, each shape/mode separately visible. **Negotiable:** discretisation inside conserved mass/species/energy; no animation-only fluid. **Uncertainty:** finite capacity, head and moving-container coupling. **Chrome proof:** empty supply emits nothing; overflow spills; reversed head does not flow uphill; a full container holds its declared amount; Reset/save exact.

<a id="pneumatics"></a>
## pneumatics

**Boundary:** one sealed-gas store/valve/actuator outcome; open airflow is a different model. **Negotiable:** gas-state representation. **Uncertainty:** pressure, volume, return path and supply. **Chrome proof:** depleted pressure moves nothing; closed valve holds; blocked load stalls; the actuator returns when vented; Reset/save exact.

<a id="electrical"></a>
## electrical

**Boundary:** one finite source/load/converter or one typed logic operator. **Negotiable:** topology algorithm; power and control stay distinct domains and modes stay enums. **Uncertainty:** affected graph and timed propagation. **Chrome proof:** every truth-table row observable on a lamp/motor; a true signal alone powers nothing; an unsupported zero-delay cycle is refused at connection time.

<a id="optical"></a>
## optical

**Boundary:** one source/transport/filter/receiver/converter or logic channel. **Negotiable:** transport details inside the per-channel finite ledger and fixed A/B/carrier contract. **Uncertainty:** aperture, moving occlusion, channel and supply. **Chrome proof:** no light or wrong channel leaves the receiver off; an opaque Wall blocks, a hollow bore passes; one source shared by two receivers lights neither brighter than alone; each channel and operator separately.

<a id="acoustic"></a>
## acoustic

**Boundary:** one source, route, resonance or receiver with declared event identity. **Negotiable:** transport after the strongest-arrival versus additive decision; never both. **Uncertainty:** supply, tone band, delay, loop and overlap. **Chrome proof:** silence/wrong tone/no power does not trigger the meter; a delayed arrival triggers later; a loop does not amplify; bell/chime need actual contact; each tone separately.

<a id="thermal"></a>
## thermal

**Boundary:** one heat/phase/reaction/conversion law or named TH element. **Negotiable:** integration inside conservation and latent heat. **Uncertainty:** finite material/energy/oxidiser stores and supported phase boundary. **Chrome proof:** no source or below threshold changes nothing; onset is visible at the declared point; exhausted supply stops; Reset/save exact.

<a id="radiation"></a>
## radiation

**Boundary:** one adopted particle group/source/shield/instrument/converter. **Negotiable:** the frozen grouped fictional model only. **Uncertainty:** activity, group/material, path, rate/dose, irreversible state; unknowns go to S604 and the [S605 decisions](decisions.md#s605). **Chrome proof:** missing source reads nothing; a thicker or denser shield reads less; a decaying capsule reads less later; fast/slow conversion observable on the detector.

<a id="structure-material"></a>
## structure-material

**Boundary:** one structure, failure mode, granular operation or environment preset. **Negotiable:** representation inside the generic laws; no part-name behavior. **Uncertainty:** strength/fragment bounds or programmable presets. **Chrome proof:** below threshold holds, above breaks into bounded fragments; unsupported preset is refused; Reset/save exact.

<a id="workflow"></a>
## workflow

**Boundary:** one editor/input/save/inspection action with atomic ownership. **Negotiable:** interaction design inside real UI, typed input, palette and lifecycle rules. **Uncertainty:** current callers/content for one action. **Chrome proof:** the gesture succeeds; invalid/cancelled/stale action leaves state usable; construction and save/Reset exact.

<a id="campaign"></a>
## campaign

**Boundary:** one level's taught causal objective after its element has shipped. **Negotiable:** layout and allowed alternatives; the 150 slots and teaching prerequisites are fixed. **Uncertainty:** named level, first use, modes and human attempts. **Chrome proof:** real UI solution, a meaningful unsuccessful route, Solved/Reset, save where supported; human clarity judged separately.

<a id="engine"></a>
## engine

**Boundary:** one numerical/state/transport/presentation invariant in its sole owner. **Negotiable:** implementation; no aliases, dual ownership or migration flags. **Uncertainty:** direct/transitive callers, shared stores and clocks. **Chrome proof:** the invariant is visible in play (no tick fault, exact Reset, no stale frame); detailed performance at the P0-034 gate; engine closure (P0-035) is the programme's definition of done at CAMPAIGN.

Every part/mode proof uses actual Chrome UI placement and controls, intended behavior plus a meaningful negative/control, real typed connections, exact Run/Reset restoration, applicable save and production build. Native or fake-page tests supplement it. Future finite modes remain obligations even when the current manifest does not list them; a Configuration=Fixed placeholder is not absence-of-modes proof. A required law is proved inside its named consuming slice; no law-family audit precedes a UI interaction.
