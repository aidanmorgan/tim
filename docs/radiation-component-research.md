# Component research: radiation and the 150-level campaign — source research (current adoption is in requirements)

Research date: **28 September 2026**. Status: **potential elements and campaign proposal; none implemented, authored or behaviorally verified by this research**. Repository context: HEAD `22ee03b` with substantial pre-existing working changes. Companion checklist: [TODO radiation candidates](planning/requirements.md#radiation). Existing families: [water, sound and logic](component-research.md). Visual contract: [DESIGN.md](../DESIGN.md).

**Campaign numbering update — 28 September 2026:** The authoritative [150-level campaign plan in TODO](planning/requirements.md#campaign-plan) now spans all component families, playful chapter names, positive rewards and increasingly complex combinations. It supersedes this research's preliminary slot allocation. The individual radiation recipe slots 76–110 below map in order to **101–135** in TODO; their later reuse is allocated within **136–150**. The tables below preserve the original research proposal, not a second active campaign order. Existing authored content and historical 75-level evidence are not automatically renumbered or newly verified.

## Recommendation and scope

Start with a small playable chain: gamma capsule → material shielding → rate meter → separately supplied gate. Extend that foundation with movable shutters, collimation, integrated exposure and cargo protection. These create spatial, timing and transport decisions that the existing visible-light system does not cover. Add X-ray inspection and scintillation once the foundations work. Alpha/beta distinction, magnetic steering, decay, neutron moderation and thermal conversion earn later lessons only after their dependencies.

P1 means foundation within this proposed family, P2 means composition after foundations, and P3 means substantial specialist simulation. These labels do not supersede TODO's shared-physics-first order. Every candidate below is separately tracked. Because the request is for **potential** elements, inclusion records a design option, not approval to implement all 22 now. If adopted, each element and supported mode needs its own implementation, focused evidence, commit and push before moving on. No existing required element is removed or deferred by this proposal.

### What radiation adds

| Decision | Difference from existing mechanics | Example |
| --- | --- | --- |
| Penetration and material choice | A wall can attenuate a field without completely blocking it; thickness and material both matter. | Use a thin panel to stop a nearby alpha path while retaining a photon signal. |
| Instantaneous rate versus accumulated exposure | A current reading and a history-dependent budget are different quantities. | Two short passes charge a dosimeter, but a protected parcel must stay below its total budget. |
| Source control versus field control | Switching an electrical emitter off differs from moving a shield around a continuously emitting capsule. | A power failure stops an X-ray emitter but leaves a gamma capsule emitting. |
| Non-contact inspection | Transmission reveals something about an intervening object. | Sort known sample thicknesses or detect a tank's fill level. |
| Conversion and spectral response | Detectors have explicit sensitivity; conversion requires a real transducer. | Radiation excites a scintillator, whose visible output drives compatible optics. |
| Particle identity and state | Charge controls deflection; neutron speed group controls response. | Reverse a magnetic field to route beta-minus, then use water to moderate a neutral channel. |

Visible light, infrared, UV, radio and microwaves are also radiation. This proposal concentrates on ionizing-radiation mechanics to avoid duplicating existing optical work. Infrared thermal emitters belong with the existing thermal/lens backlog; UV fluorescence could extend optical conversion after a wavelength model; radio transmitters/receivers need a separate wireless-signal/interference design. These are assessed adjacent possibilities, not included in the 22 candidates or assigned hidden campaign obligations. Sound remains a mechanical wave system. [EPA radiation overview](https://www.epa.gov/radiation/radiation-basics), [NASA infrared overview](https://science.nasa.gov/ems/07_infraredwaves/).

## Physical evidence and deliberate game abstractions

| Evidence | What the source establishes | Design implication and limit |
| --- | --- | --- |
| [EPA: radiation basics](https://www.epa.gov/radiation/radiation-basics) | Alpha, beta and photons have different interaction properties; X-rays can be machine-produced. | Separate particle identity from energy. Gamma is not universally more penetrating than every X-ray. Use a beta-minus-only initial cartridge, not an unqualified claim that all beta radiation has negative charge. |
| [NIST: photon attenuation](https://physics.nist.gov/PhysRefData/XrayMassCoef/chap2.html) | A narrow monoenergetic beam follows exponential attenuation through material; coefficients depend on photon energy. | Use material path length and authored energy groups. A zero/one opaque-wall rule misses the main puzzle opportunity. The approximation omits scatter buildup and does not justify treating alpha/beta/neutrons with the same equation. |
| [IAEA SSG-58, section 2, printed pp. 1–2](https://www-pub.iaea.org/MTCD/Publications/PDF/P1881_web.pdf) | Transmission gauges infer thickness, density or level from source-to-detector attenuation. | Build an inspection puzzle around known sample geometry. A single reading cannot uniquely recover arbitrary thickness and composition. |
| [IAEA SSG-58, paragraphs 10.21–10.23](https://www-pub.iaea.org/MTCD/Publications/PDF/P1881_web.pdf) | Electrical X-ray generation can cease when switched off; source shutters and interlocks have distinct roles. | Separate source state from actual shutter geometry. A commanded closure is not proof of a closed, unjammed blade. |
| [CERN-hosted scintillation detector lecture](https://indico.cern.ch/event/349068/contributions/1749074/attachments/689082/946337/Scintillation_Detectors_ARDENT.pdf) | A scintillator converts incident radiation energy into light; a light detector performs the readout. | A passive tile can glow; a supplied receiver can switch a load. The proposed optical aperture and deterministic brightness are game abstractions. |
| [EPA: radionuclides](https://www.epa.gov/radiation/radionuclides) | Half-life is the interval for half the radioactive atoms in a population to decay. | Use a deterministic population envelope for a fictional capsule. Individual random decays must not decide whether the player's identical construction wins. |
| [CERN-hosted track inspection](https://scienceinschool.web.cern.ch/article/2019/track-inspection-how-spot-subatomic-particles/) | Charged tracks curve in magnetic fields according to charge, momentum and field. | Provide a field chamber with readable trajectories. Opposite charge reverses curvature; equal path radius for alpha and beta is not physically implied. |
| [NIST neutron data](https://www.ncnr.nist.gov/resources/n-lengths/list.html), [NIST/NBS shielding handbook](https://nvlpubs.nist.gov/nistpubs/Legacy/hb/nbshandbook97.pdf) | Scattering and absorption are distinct, material-dependent processes; hydrogen is effective for moderation. NIST's listed absorption values are for a specified neutron speed. | Distinguish fast/slow transport, water moderation and boron-inspired absorption. Do not extrapolate a single thermal cross section into a universal fast-neutron rule. |
| [NASA: RTGs](https://science.nasa.gov/planetary-science/programs/radioisotope-power-systems/power-radioisotope-thermoelectric-generators/) | Decay heat supports thermoelectric generation across hot/cold junctions; excess heat can also be used. | Require thermal state and finite load accounting. A radiation detector is not an RTG, and an RTG is not a chain-reaction reactor. |
| [IAEA TECDOC-1617, foreword](https://www-pub.iaea.org/MTCD/Publications/PDF/te_1617_web.pdf) | Irradiation can cause polymer crosslinking or chain scission, depending on the material/process. | A weakening insert is a plausible inspiration. Its instantaneous threshold, load response and toy timescale are invented design choices, not a prediction for arbitrary plastic. |
| [IAEA: radiotracer applications](https://conferences.iaea.org/event/107/contributions/2001/), [IAEA TCS-38](https://www-pub.iaea.org/MTCD/Publications/PDF/TCS-38_web.pdf) | Tracers can reveal transport and leaks; discrete radioactive particle tracking is also used. | Prefer a sealed moving capsule for an initial transport puzzle. Dissolved tracer concentration, mixing and contamination require a separate conserved-material model. |

Sources were researched on the date above. Some NRC pages and a CERN cloud-chamber PDF could not be fetched in full; the design does not depend on their inaccessible details. These references support physical principles, not claims that these parts appeared in historical TIM games. All puzzle names, thresholds, layout slots and priorities below are our proposals.

## Candidate catalogue

Each ID links to an individual unchecked TODO record with its behavior and positive/control acceptance. Cost is a relative dependency estimate, not a schedule. Presets within a row require separately identifiable proof, even when they share one implementation.

| ID | Priority | Candidate | Distinct decision / dependency | Proposed teaching slots |
| --- | --- | --- | --- | --- |
| [RAD-01](planning/requirements.md#radiation-01) | P1 | Gamma source capsule | Low after radiation transport | 76, 78, 85 |
| [RAD-02](planning/requirements.md#radiation-02) | P1 | Powered X-ray emitter | Medium; electrical supply and photon transport | 82, 89 |
| [RAD-03](planning/requirements.md#radiation-03) | P2 | Alpha source cartridge | Medium; range/material model | 90, 93 |
| [RAD-04](planning/requirements.md#radiation-04) | P2 | Beta-minus source cartridge | Medium; range/material model | 91, 94 |
| [RAD-05](planning/requirements.md#radiation-05) | P1 | Material shield panel | Medium; material intervals and shared body mass | 77, 79, 90, 91 |
| [RAD-06](planning/requirements.md#radiation-06) | P1 | Powered radiation shutter | Medium; shared actuator/contact integration | 80, 85 |
| [RAD-07](planning/requirements.md#radiation-07) | P1 | Collimator block | Medium; finite aperture coverage | 81, 89 |
| [RAD-08](planning/requirements.md#radiation-08) | P1 | Radiation rate meter | Medium; detector response and hysteresis | 76, 83, 90, 91 |
| [RAD-09](planning/requirements.md#radiation-09) | P1 | Integrating dosimeter | Medium; integration and persistence | 84, 85 |
| [RAD-10](planning/requirements.md#radiation-10) | P1 | Exposure-sensitive cargo badge | Medium; cargo goals and motion sampling | 86, 87 |
| [RAD-11](planning/requirements.md#radiation-11) | P2 | Transmission gauge | Medium; calibration, water and sorting | 88, 89 |
| [RAD-12](planning/requirements.md#radiation-12) | P2 | Scintillator tile | High; radiation/optical energy coupling | 92, 97 |
| [RAD-13](planning/requirements.md#radiation-13) | P2 | Decay clock capsule | Medium; deterministic source age | 98, 99 |
| [RAD-14](planning/requirements.md#radiation-14) | P3 | Magnetic deflector | High; curved transport and swept interception | 93, 94 |
| [RAD-15](planning/requirements.md#radiation-15) | P3 | Track chamber | High; trajectory display and segmented sensing | 95, 96 |
| [RAD-16](planning/requirements.md#radiation-16) | P3 | Neutron source module | High; neutron transport foundation | 100, 105 |
| [RAD-17](planning/requirements.md#radiation-17) | P3 | Water moderator tank | High; water geometry and two-group conversion | 101, 103 |
| [RAD-18](planning/requirements.md#radiation-18) | P3 | Neutron absorber panel | High; group-dependent absorption | 102, 105 |
| [RAD-19](planning/requirements.md#radiation-19) | P3 | Neutron group detector | Medium after neutron transport | 100, 101, 104 |
| [RAD-20](planning/requirements.md#radiation-20) | P3 | Radioisotope thermoelectric generator | High; thermal state and finite electrical power | 106, 107 |
| [RAD-21](planning/requirements.md#radiation-21) | P3 | Radiation-responsive material latch | High; material state and load-bearing contact | 108, 109 |
| [RAD-22](planning/requirements.md#radiation-22) | P2 | Sealed tracer capsule | Medium; movable source and edge detection | 110, 125 |

### Implementation contracts

**Transport.** Begin with deterministic scalar transport at a fixed simulation cadence. Snapshot sources, geometry and controls; calculate transmissions and receiver contributions; commit outputs together. Electrical consequences take effect at the next documented sampling boundary. Insertion order, camera, audio and visual frame rate cannot select the result. Field drawings are explanatory overlays, not collision or dose inputs.

**Photon model.** For one energy group, propose `T = exp(-sum(mu(material, energy) * pathLength))`. Coefficients and lengths use consistent game units. A finite emitting surface avoids a point-source singularity. In the far field an isotropic capsule uses inverse-square geometric dilution; a directional emitter uses its defined angular distribution and receiver solid angle. Do not apply an extra distance factor after already accounting for the same spreading. Multiple independent sources add incident contributions, never source-identification counts. Collimator walls absorb rejected directions; reducing aperture cannot manufacture output power. [NIST attenuation](https://physics.nist.gov/PhysRefData/XrayMassCoef/chap2.html).

**Geometry.** Use actual traversed material intervals, shield thickness, holes, front/back surfaces and finite receiver areas. Avoid counting both an owner envelope and its child collider as duplicate shielding. A distant decorative wall must not substitute for a nearer absorber. Moving cargo and shutters need deterministic interval integration or bounded adaptive sampling across exposure transitions, so a fast crossing cannot disappear between snapshots. Set accuracy criteria with near-threshold controls before choosing sample budgets.

**Charged particles.** Initial alpha/beta use explicit range-loss/material tables and absorbed energy, not photon attenuation relabeled. Magnetic steering requires bounded curved trajectories and swept interception; field force changes direction, not kinetic energy. Alpha and beta-minus have different charge/mass/energy parameters. Exclude bremsstrahlung and secondary particle production from the initial toy model explicitly; never imply the simplified shield chart is real-world shielding guidance.

**Neutrons.** Use a separately declared two-group approximation with nonnegative transmission, moderation and absorption fractions whose outgoing sum cannot exceed incoming particle count. Moderation lowers particle energy; losses/deposited energy are accounted for. A straight-through two-group approximation omits angular scattering and capture radiation; call this out in contextual help. Do not introduce neutron multiplication, induced activation or decay chains accidentally through the material table. Full Monte Carlo transport is not needed for this deterministic puzzle contract.

**Measurement.** Keep emitted activity, incident flux, detector response, accumulated target exposure, heat and electrical power distinct. Propose `D(next) = D(now) + responseWeightedRate * elapsedTime` for exposure. Label the displayed quantity as game exposure units rather than Bq, Gy or Sv without a corresponding physical model. A rate meter uses hysteresis; a dosimeter retains accumulated value through darkness and supply interruptions but cannot drive an unpowered output. A passive badge records exposure without a signal socket. A saturated detector must indicate saturation, never wrap to zero.

**Inspection validity.** The transmission gauge has a source-valid reference plus measured sample path. Calibrate on a known empty/reference sample. Broken source/reference, out-of-range alignment and unsupported material/energy combinations produce an explicit invalid state with a visible indicator. They cannot silently become “full tank” or “thick sheet.” Sheet and tank modes share measured attenuation but each gets a separate lesson/control.

**Conversion.** Scintillation uses absorbed energy and bounded efficiency; emitted light couples only to explicitly compatible optical apertures. Resolve absorption/transmission once, so downstream tiles cannot each claim the original energy. Existing laser/flashlight receivers must not silently gain compatibility. RTG work needs a finite electrical-power/load model beyond binary source reachability; heat-source power divides into converted work, changing stored heat and losses. A fan-driven cooling loop must satisfy startup and conservation rather than power itself from nothing.

**Decay/material memory.** A decay capsule uses `A(t) = A0 * 2^(-t / halfLife)` with elapsed simulation time. Pausing cannot age it; shielding does not stop decay. Fictional short presets are distinct from the nominally steady gamma source. A polymer latch owns irreversible-in-Run material state; removing exposure cannot unbreak the insert. Reset restores both. Radiation alone does not automatically make unrelated objects emit radiation.

### Typed boundaries and connections

Proposed names below describe future C# enums, not existing APIs. Reuse compatible existing enums when implementing; never introduce alternate string protocols.

| Closed set / identity | Proposed typed representation | Boundary obligation |
| --- | --- | --- |
| Radiation identity | `RadiationKind`: Alpha, BetaMinus, Gamma, XRay, Neutron | Keep typed through emitters, transport, detectors, dictionaries, content and tests; reject unknown and undefined values. |
| Energy / speed | `PhotonEnergyBand`, `NeutronEnergyGroup` with Fast and Slow | Validate legal radiation/group combinations; no arbitrary string labels or unvalidated cross-product. |
| Shield choice | `RadiationMaterialKind`, `ShieldThicknessPreset` | Central physical table indexed by enums; material label is presentation only. |
| Instrument state | `RadiationInstrumentState`: Unpowered, Measuring, Saturated, Invalid | Explicit initialized state, power transitions and supported serialization. |
| Gauge / field / goals | `TransmissionGaugeMode`, `MagneticPolarity`, `ExposureGoalKind`, `ExposureBandState` | Presets retain types through radial UI adapters, authors, simulation, comparisons and proof fixtures. |
| Source envelope / insert | `DecayPreset`, `IrradiatedMaterialState` | No free text source modes, timer parsing, aliases or automatic migration. |
| Open identities and quantities | Typed source/part/port IDs; distinct flux/exposure/energy/time value types | IDs are not enums when genuinely extensible. Convert only at validated external/UI/save boundaries. |

Radiation normally crosses space, not cables. Emitters and passive shields have no invented radiation wire sockets. Powered meters expose separate supply and signal contacts; field generators expose supply/control; a moderator has real fluid ports; RTG output is electrical with thermal state; scintillator output is an optical aperture. Physical cargo sockets and links retain their existing typed capability rules. Reject a wire to a shield, a ball pipe as a radiation guide, an optical fiber carrying gamma, and a detector output used as an independent power source. Enumerate each supported endpoint/mode permutation in the existing [connection coverage work](planning/requirements.md#connection-permutations).

### Presentation

Retain the approved cream/navy/cyan/gold palette and Monument Valley-inspired composition. Use sealed ceramic/brass-like capsules, thick beveled shield slabs, a gold meter needle and engraved particle/energy symbols. Selected-only dotted field paths and a receiver bar make invisible transport legible; they do not suggest a literal visible glowing beam. Avoid radioactive-green recoloring, strobe effects and screen-filling particles.

Show source strength, shutter position, current rate and accumulated exposure as distinct local cues. Short contextual cards explain “measures now” versus “remembers exposure.” Detector clicks are optional presentation with identical muted outcomes. Build preview can show the initial field without advancing decay, dose, heat or material state. Hidden sample puzzles expose a measurable signal, not an opaque required guess. Desktop and touch targets must support construction, mode inspection and negative trials without a numeric placement menu.

## Proposed 150-level allocation

This is a curriculum budget, not 150 authored puzzles. Existing component and lesson requirements remain open until mapped individually. The first 75 positions are a proposed foundation sequence to reconcile with current content; do not force a current level into a new number without updating current content, typed identities, navigation, tooling and save boundaries together. Preserve old evidence unchanged.

| Slots | Count | Teaching role |
| --- | --- | --- |
| 1–15 | 15 | Placement, Run/Reset, gravity, contact, ramps, capture and simple goals. |
| 16–30 | 15 | Rope, levers, stored mechanical energy, launch/rebound and transport. |
| 31–45 | 15 | Electrical supply, timing, counting, memory and taught logic operations. |
| 46–60 | 15 | Light paths, source/receiver compatibility, sound and signal conversion. |
| 61–75 | 15 | Conserved water/air, changing loads, thermal fundamentals and mixed-system readiness. Thermal/load lessons are prerequisites for the RTG later. |
| 76–85 | 10 | Radiation foundation: source, distance, shielding, field control, rate and exposure. |
| 86–95 | 10 | Cargo budgets, inspection, particle distinctions, scintillation and track routing. |
| 96–105 | 10 | Track interpretation, decay timing and neutron source/moderation/absorption. |
| 106–110 | 5 | Thermal generation, material memory and a moving tracer. |
| 111–120 | 10 | Revisit mechanisms after a gap; diagnose and repair short mixed machines. |
| 121–135 | 15 | Open construction with water, sound, mechanical routing and radiation combinations. |
| 136–145 | 10 | Resource-constrained mastery with multiple valid arrangements. |
| 146–150 | 5 | Five capstones; no new required part, mode or physical rule. |
| **Total** | **150** | **75 foundation + 35 proposed radiation introductions/practice + 40 integration/mastery.** |

This is the broad 22-candidate scenario. Thirty-five consecutive family slots are a substantial curriculum cost: after testing, interleave their lessons with previously taught families while preserving prerequisites. If fewer candidates are selected, reclaim their slots for already-required components or mixed practice; do not ship placeholders or silently label an unselected candidate implemented. No candidate is automatically cut by this recommendation.

### Concrete radiation lessons: slots 76–110

A lesson may show a new source and its measuring instrument together as one cause-and-effect relationship. Otherwise introduce one new distinction at a time, using fixed fixtures and a small editable inventory. Every listed control is a proposed required UI trial, not a recorded pass.

| Slot | Puzzle / new distinction | Player construction and causal solution | Meaningful control |
| --- | --- | --- | --- |
| 76 | The Quiet Beacon: source → meter | Place a gamma capsule near a supplied meter; wire its contact to release a ball. | Move it beyond the marked response region; removing meter supply prevents release. |
| 77 | Behind the Slab: attenuation | Place a dense panel between the established source and meter to permit a previously taught quiet-condition gate. | Same panel beside the path leaves the meter active. |
| 78 | A Little Farther: distance | Move the source to keep a near meter active and a farther meter inactive. | A position too close activates both. |
| 79 | Two Thin Walls: thickness | Stack a limited inventory of panel presets to meet a transmitted-rate band. | One panel is too little; excessive shielding falls below the lower bound. |
| 80 | Closing Time: moving shutter | Wire a known clock to the radiation shutter, opening one useful exposure interval. | Jam the real blade; a close command alone does not clear the meter. |
| 81 | The Narrow Door: collimation | Orient an aperture block to address one receiver while excluding its neighbor. | Misalignment or a receiver in the rejected direction fails. |
| 82 | Only While Powered: X-rays | Wire supply and enable to an X-ray emitter and gate a detector pulse. | Cut supply; emission ceases despite enable. |
| 83 | Two Small Suns: addition | Combine two individually subthreshold capsule contributions at one meter. | Either source alone remains subthreshold; do not treat two hits as two votes. |
| 84 | A Little at a Time: integration | Use a clock/shutter to accumulate two separated exposures in a dosimeter. | One burst is insufficient; a gap does not erase the first. |
| 85 | Close at the Mark: feedback | Dosimeter threshold drives a supplied shutter controller and releases a ball. | Account for blade travel/latency; late closure overshoots a bounded target. |
| 86 | The Sheltered Parcel: moving dose | Build a protected conveyor route for badged cargo to its dock. | An exposed route to the identical dock exceeds the budget. |
| 87 | Briefly in the Open: speed/dwell | Use a taught queue gate to limit badge time in a narrow exposure zone. | A stopped parcel accumulates excess exposure. |
| 88 | Weighing Without Touch: sheet gauge | Route known sample thicknesses through a source/gauge and wire a diverter. | A thick sample takes the other route; dead source is invalid. |
| 89 | The Hidden Waterline: tank gauge | Position source and gauge across a conserved-water vessel; use the level contact to close its supplied tap. | Below-path water and a misaligned reference do not report a valid full state. |
| 90 | The Short Journey: alpha | Place an alpha-sensitive meter close to its cartridge and compare a thin-screen path. | Extra distance or the screen prevents response. |
| 91 | Beyond the Paper: beta-minus | Use beta-minus with the taught thin screen, then choose a polymer shield to suppress it. | Alpha fails the longer path; polymer suppresses the selected beta profile. |
| 92 | Borrowed Light: scintillation | Place a scintillator in the photon field and align its declared visible aperture with a supplied optical receiver. | Blocking incident radiation or misaligning the optical receiver prevents release. |
| 93 | Around the Corner: field direction | Align beta-minus, a powered deflector and its receiver around an obstacle. | Field-off goes straight and misses; reverse polarity bends the other way. |
| 94 | Opposite Ways: charge | Use separate known alpha/beta sources with polarity control and marked target apertures. | A photon control remains straight; do not assume equal charged radii. |
| 95 | A Line in the Mist: track chamber | Route the previously taught charged path across a selected chamber segment. | A visible trail in a different segment cannot trigger the chosen contact. |
| 96 | Read the Turn: track diagnosis | Use chamber observations to correct a reversed deflector and deliver a ball. | Decorative trail persistence after the particle leaves cannot sustain arrival pulses. |
| 97 | Hidden to Visible: conversion remix | Use a known shutter, scintillator and optical logic gate with separately supplied carrier/receiver. | Blocking one required optical control or starving the carrier fails. |
| 98 | Half as Bright: decay | Use a decay capsule and rate meter to release a gate after the activity crosses a lower threshold. | A steady capsule never produces the required falling transition. |
| 99 | Catch the Window: decay plus memory | Accumulate enough early exposure, then wait for the rate to fall before releasing cargo using a taught latch/AND. | Waiting first loses the early opportunity; shielding never freezes capsule age. |
| 100 | The Neutral Visitor: neutron detection | Place a fast-group detector for a neutron module. | Photon source and photon-only meter cannot substitute. |
| 101 | Slow Water: moderation | Fill a moderator tank in the neutron path until a slow-group detector responds. | Dry tank fails; the fast reading changes consistently with conversion/loss. |
| 102 | The Final Layer: absorption | Add the neutron absorber after the moderator to protect a downstream zone. | Water alone slows particles but leaves the taught slow response. |
| 103 | A Rising Shield: water feedback | Use a separately supplied pump and moderator level to enter a slow-flux response window. | Draining or overfilling misses the authored window; water is conserved. |
| 104 | One Speed at a Time: group selection | Couple Fast/Slow detectors to taught XOR or sequencing logic. | Both active or the wrong selected group fails the stated condition. |
| 105 | Two Kinds of Shelter: material integration | Protect photon and neutron targets with limited dense panels, moderator water and absorber. | A dense-only assembly fails the neutron condition. |
| 106 | Warm Heart, Cool Fins: RTG | Connect a taught finite-power load; provide an initially available cooling path for the thermoelectric module. | Equal hot/cold temperatures yield no output. |
| 107 | Keep the Mill Turning: startup/load | Use a finite stored startup supply to run cooling, then sustain a modest load from the RTG. | Overload or depleted startup before cooling develops fails; no perpetual loop. |
| 108 | The Weakening Pin: material memory | Expose a loaded polymer latch insert until it weakens and releases the payload. | Shielding or subthreshold dose leaves the latch supporting the load. |
| 109 | Release, Then Shelter: one-way state | Release the irradiated latch, then move its payload through a protected route. | Removing radiation cannot repair the pin; early release exposes cargo too long. |
| 110 | The Traveling Beacon: tracing | Send a sealed tracer through opaque ball-compatible transport; two meters sequence a diverter. | A stuck capsule never creates a second arrival; wrong branch misses the second meter. |

### Spaced reuse and capstones: slots 111–150

| Slots | Integration plan | Radiation dependency / retained breadth |
| --- | --- | --- |
| 111–115 | Shutter repair, source-distance redesign, two-burst dosage, cargo shelter and a reference-gauge fault. | RAD-01/02/05–11; revisit early concepts after specialist lessons. |
| 116–120 | Alpha/beta shield sorting, scintillator plus sound relay, field-polarity repair, track-segment selection, decay-window routing. | RAD-03/04/12–15; each level changes one constraint and retains an explicit control. |
| 121–125 | Neutron detector selection, moderator fill/drain, absorber placement, mixed shielding and a tracer-guided transport junction. | RAD-16–19/22; water conservation and physical routing remain central. |
| 126–130 | RTG startup/load, cooling route, material-latch timing, latch plus cargo exposure, and an acoustic-to-shutter interlock. | RAD-06/10/20/21; reuse taught thermal, sound and mechanical work. |
| 131–135 | Five primarily mechanical/water/air/light/sound puzzles, with a radiation meter as one optional alternate route where budgets allow. | Protect the other families' teaching space; avoid making radiation mandatory in every late level. |
| 136–140 | Five constrained redesigns: fewer shields, less startup energy, fewer exposure bursts, restricted field placement and less moderator water. | Optimize resources rather than require subpixel timing. Use at least two demonstrably valid layouts where intended. |
| 141–145 | Five diagnosis/assembly puzzles with a provided partial machine: inspect actual rate, dose, source validity, speed group or thermal gradient. | No hidden state or new mode; require players to identify a cause and repair it using UI controls. |
| 146 | **The Inspection Post:** scan cargo, route by known thickness, keep its badge within budget, then confirm delivery. | Gauge, collimation, exposure and mechanical sorting. |
| 147 | **The Lantern Exchange:** radiation → scintillator → optical gate → supplied speaker → meter → hatch. | Explicit transducers, finite light and electrical supply; no direct radiation-to-sound shortcut. |
| 148 | **The Water Observatory:** fill a moderator, select slow response, close the source shutter, release a protected parcel. | Fluid conservation, neutron response and real blade latency. |
| 149 | **The Quiet Engine:** cold-start the RTG system, power a modest transporter and time a material-latch release. | Finite startup energy, cooling, mechanical load and material memory. |
| 150 | **The Last Delivery:** coordinate a decay readiness window, cargo exposure, a tracer checkpoint and an already taught optical/mechanical route. | Multiple viable taught routes; no requirement to place all 22 candidates in one scene. |

Every accepted element must have introduction, an independent control and later reuse. The slot tables supply those design intentions, not evidence. After authoring, maintain an element × mode × level matrix and find unintroduced dependencies before any capstone qualifies. Level 75 is a mixed-foundation checkpoint, not a claim of prior mastery.

## Acceptance and evidence

For each RAD record, construct through actual palette/radial controls, movement/rotation gizmos and compatible sockets. Verify the actual placed material, thickness, source identity, energy/group, detector selection and typed connection endpoints. Run the positive recipe and its listed control, observe causal behavior, Reset and compare exact construction. No imported solutions, game-state setters or numeric placement menus.

Retain per-element/per-mode evidence containing tested commit plus working-tree/content hash when applicable, browser/build version, recipe/actions, assertions, measured response histories, actual connection snapshots, screenshots, motion captures and all failures. Track implementation, native checks, production build, UI positive/control, connections, Reset/save, commit and push separately; none is complete here. A single gamma demonstration cannot qualify all radiation parts.

| Area | Focused cases before completion |
| --- | --- |
| Transport/material | Zero source; exact aperture edges; grazing/thick/stacked/overlapping shields; empty space versus hole; two-source addition; moved/rotated shields; insertion-order invariance; bounded large scenes. |
| Measurement | On/off hysteresis boundaries; wrong radiation/group; equal integrated exposure from different burst patterns; rate removal without dose loss; saturation; invalid calibration and power interruption. |
| Motion | Fast crossings and slow dwell; blade jam/partial closure; moving source relative to cargo; endpoint-equivalent routes with different exposure histories. |
| Conversion/energy | Lost incident energy; serial scintillators; wrong optical aperture; no unpowered output; RTG zero gradient, startup, overload and heat balance. |
| Specialist states | Both deflector polarities and field-off; each chamber segment; fast/slow groups; dry/filling/draining moderator; absorber removal; decay pause/replay; irreversible latch; tracer edge/rearm. |
| Construction persistence | Reset during exposure, shutter travel, decay, heating, fill and latch release; current-schema save/load restores source presets, transforms, materials, links and authored initial states. Runtime quick-save, if offered, must explicitly preserve all transient state too. |
| Boundary contracts | Canonical enum mappings; reject unknown/undefined values, invalid combinations, unsupported endpoints and obsolete schemas; compile all changed authors, UI adapters, tools and tests. |
| Presentation | Muted/unmuted outcomes equal; no color-only distinctions; desktop/mobile construction; selected-only overlays; actual state synchronized with animation. |

Build/native proofs supplement real-UI behavioral evidence. Complete and publish one adopted element with its lesson and focused proof before the next; shared prerequisites must be reviewable without claiming dependent parts complete. Existing stale proof remains explicitly stale.

The eventual 150-level baseline is **150 × 3 difficulties × 4 placement variants = 1,800 distinct cells**, before repeatability and mechanism-specific probes. Keep the existing 75-level/900-cell records unchanged as historical evidence, without counting them automatically against revised content. Component implementation and focused proof come first; exhaustive difficulty/nudging sweeps follow full coverage.

## Open design decisions and delivery order

1. Prove the smallest gamma/shield/meter loop and selected-only field display. Measure whether penetration is understandable without a permanent overlay.
2. Deliver dose and cargo integration, then source control, inspection and scintillation; reconcile the proposed numeric lesson order with prerequisite implementation order.
3. Prototype alpha/beta range and magnetic routing readability before committing to their broad campaign footprint. Do not represent curved-particle physics by reusing mirror reflection.
4. Assess neutron two-group clarity and thermal/load readiness separately. Their high cost does not authorize quietly deleting adopted elements; keep unresolved candidates visible.
5. Author the detailed 150-level inventory and prerequisite matrix, then reallocate practice slots based on observed comprehension. Preserve every existing required component and the current palette.
6. Qualify the broad difficulty matrix after component coverage. This research has no native/browser/build execution claims and does not close the repository-wide enum audit.
