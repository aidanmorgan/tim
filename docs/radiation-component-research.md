# Radiation capability family and its 150-level teaching content

Radiation is one capability family inside the single generic WASM SIMD f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory), [general data-driven engines](engine-contracts.md#general-data-driven-engines)), added as generic records in the slice that first needs it. The [22 radiation elements](planning/requirements.md#radiation) RAD-01–RAD-22 are declaration data over those records; none is implemented or verified yet. Companion families: [water, sound and logic](component-research.md), [heat](thermal-component-research.md). Visual contract: [DESIGN.md](../DESIGN.md).

**Campaign numbering.** The authoritative [150-level campaign plan](planning/requirements.md#campaign-plan) spans all families. The radiation lesson slots 76–110 below map in order to **101–135** there; their later reuse is allocated within **136–150**. The tables keep the lesson names and teaching order; they are not a second active campaign numbering. Existing authored content and 75-level evidence are not renumbered or newly verified.

## Capability family

Ionizing radiation adds decisions visible light does not: penetration with material and thickness, instantaneous rate versus accumulated exposure, source control versus field control, non-contact inspection, conversion through an explicit transducer, and particle identity/state. Visible light, infrared, UV, radio and microwaves stay with the optical, thermal and future wireless families ([EPA radiation overview](https://www.epa.gov/radiation/radiation-basics), [NASA infrared overview](https://science.nasa.gov/ems/07_infraredwaves/)). Sound remains a mechanical wave system.

### State variables (IEEE-754 f32)

| Record | State | Derived (no independent setter) |
| --- | --- | --- |
| Radiation source | `RadiationKind` (Alpha, BetaMinus, Gamma, XRay, Neutron), energy band/group, activity A (game units/s), finite emitting surface, angular distribution, decay preset (half-life), electrical enable where powered | Current activity A(t) = A₀·2^(−t/halfLife) over committed simulation time |
| Attenuating material | `RadiationMaterialKind`, `ShieldThicknessPreset`, per-kind coefficient μ(material, energy), charged-particle range, neutron transmission/moderation/absorption fractions | Transmission T = exp(−Σ μ·pathLength) for photons; range-loss table for alpha/beta; two-group fractions for neutrons |
| Field geometry | Collimator aperture, shutter blade pose, traversed material intervals, finite receiver area | Incident flux per receiver after inverse-square dilution and all intervals on the path |
| Detector | `RadiationInstrumentState` (Unpowered, Measuring, Saturated, Invalid), response weights per kind/group, On/Off thresholds, accumulated exposure D | D(next) = D(now) + responseWeightedRate·dt; boolean output at substep endpoints |
| Magnetic field region | `MagneticPolarity`, field strength, bounds | Curved charged-particle path; direction changes, kinetic energy does not |
| Converter | Scintillator efficiency with an optical aperture; thermoelectric hot/cold ports (RTG) | Light into the optical family; electrical power into the electrical family |
| Material memory | `IrradiatedMaterialState`, dose threshold, load capacity | Irreversible-in-Run weakening; Reset restores |

### Laws at puzzle scale

- **Transport.** Sources, geometry and controls are read from the committed state; transmissions and receiver contributions are computed for every receiver in one dispatch and committed together with the tick. Insertion order, camera, audio and render rate cannot select the result. Field drawings are overlays, never collision or dose inputs. Moving cargo and shutters are sampled at substep endpoints with the speculative margin, so a fast crossing cannot vanish between samples.
- **Photons (Gamma, XRay).** One energy band per source; T = exp(−Σ μ·pathLength) over actual traversed intervals ([NIST photon attenuation](https://physics.nist.gov/PhysRefData/XrayMassCoef/chap2.html)); isotropic capsules dilute by inverse square from a finite surface, emitters use their declared distribution; no second distance factor. Independent sources add incident contributions, never votes. Collimator walls absorb rejected directions; a narrower aperture cannot raise output power. Owner envelope and child collider never double-count as shielding; a nearer absorber always counts before a distant decorative wall.
- **Charged particles (Alpha, BetaMinus).** Range-loss tables and absorbed energy, not relabelled photon attenuation. A magnetic region bends the path by charge sign and momentum ([CERN track inspection](https://scienceinschool.web.cern.ch/article/2019/track-inspection-how-spot-subatomic-particles/)); opposite charge reverses curvature; equal radii for alpha and beta are not implied. No bremsstrahlung or secondaries.
- **Neutrons.** A two-group (Fast, Slow) approximation with nonnegative transmission, moderation and absorption fractions whose outgoing sum never exceeds incoming count ([NIST neutron data](https://www.ncnr.nist.gov/resources/n-lengths/list.html), [NIST/NBS shielding handbook](https://nvlpubs.nist.gov/nistpubs/Legacy/hb/nbshandbook97.pdf)); water moderates, boron-inspired panels absorb; no multiplication, activation or decay chains.
- **Measurement.** Activity, incident flux, detector response, accumulated exposure, heat and electrical power stay distinct quantities in game units. A rate meter has separate On/Off thresholds; a dosimeter keeps its accumulation through darkness and supply loss but drives nothing unpowered; a badge records without a signal socket; a saturated detector shows Saturated, never wraps.
- **Inspection.** The transmission gauge needs a valid source and reference path, calibrated on a known sample ([IAEA SSG-58](https://www-pub.iaea.org/MTCD/Publications/PDF/P1881_web.pdf)); a dead source, misalignment or unsupported material/energy sets Invalid with a visible indicator rather than "full tank" or "thick sheet".
- **Conversion.** Scintillation uses absorbed energy and bounded efficiency; its light couples only to explicitly compatible optical apertures, and absorption resolves once so serial tiles cannot each claim the original energy ([CERN scintillation lecture](https://indico.cern.ch/event/349068/contributions/1749074/attachments/689082/946337/Scintillation_Detectors_ARDENT.pdf)). RTG output is decay heat through the [thermoelectric record](thermal-component-research.md#th-s09) with finite load accounting ([NASA RTGs](https://science.nasa.gov/planetary-science/programs/radioisotope-power-systems/power-radioisotope-thermoelectric-generators/)); a cooling fan loop must start from a finite supply, never power itself.
- **Decay and material memory.** Half-life is a deterministic population envelope ([EPA radionuclides](https://www.epa.gov/radiation/radionuclides)); pausing cannot age a capsule and shielding does not stop decay. A polymer latch weakens irreversibly within a Run ([IAEA TECDOC-1617](https://www-pub.iaea.org/MTCD/Publications/PDF/te_1617_web.pdf)); removing exposure never repairs it; Reset restores both. Radiation never makes unrelated objects emit.
- **Envelope.** Flux, dose and activity arithmetic is clamp-or-continue under the [game-grade envelope](gpu-f32-physics.md#game-grade-envelope); residuals never fault a tick. Out-of-range declarations reject once at compile.

Sources were read on 28 September 2026; some NRC pages and a CERN cloud-chamber PDF were unavailable and nothing depends on them. These references support principles, not claims about historical TIM parts; the simplified shield chart is never real-world shielding guidance.

### Parameters, sensors, sources, stores and network nodes

| Parameter | Unit | f32 range / scale | Notes |
| --- | --- | --- | --- |
| Activity A₀ | game units/s | 1/16–256 (scale 2⁴) | Per source preset |
| Half-life | s | 1–1800 (scale 2⁴) | Decay presets; steady gamma uses none |
| μ(material, energy) | 1/cell | 2⁻⁶–8 | Material × band table |
| Charged range | cells | 1/4–64 | Alpha short, beta longer |
| Neutron fractions | — | [0,1], sum ≤ 1 | Transmission, moderation, absorption |
| Detector thresholds | game rate | On > Off | Hysteresis, dwell in ticks |
| Exposure budget | game exposure | 1–4096 (scale 2⁴) | Badge, dosimeter, latch threshold |
| Magnetic strength | — | signed, 1/16–16 | Polarity enum × magnitude |
| Scintillator efficiency | — | (0,1] | Bounded |

Sources: Gamma capsule, X-ray emitter, Alpha and Beta-minus cartridges, Neutron module, Decay capsule, Tracer capsule. Stores: dosimeter accumulation, badge budget, latch dose. Network nodes: powered meters expose supply + signal contacts, field generators expose supply + control, the moderator has fluid ports, RTG output is electrical, scintillator output is an optical aperture. Sensors: Rate meter, Dosimeter, Cargo badge, Transmission gauge, Neutron group detector, Track chamber segments. Radiation crosses space, not cables: no wire to a shield, no ball pipe as a radiation guide, no optical fibre carrying gamma, no detector output as a power source. Supported endpoint/mode permutations are enumerated in the [connection coverage work](planning/requirements.md#connection-permutations).

### Typed boundaries

| Closed set / identity | Typed representation | Boundary obligation |
| --- | --- | --- |
| Radiation identity | `RadiationKind`: Alpha, BetaMinus, Gamma, XRay, Neutron | Typed through emitters, transport, detectors, content and tests; unknown/undefined values reject |
| Energy / speed | `PhotonEnergyBand`, `NeutronEnergyGroup` (Fast, Slow) | Validate legal kind/group combinations |
| Shield choice | `RadiationMaterialKind`, `ShieldThicknessPreset` | Central table indexed by enums; label is presentation only |
| Instrument state | `RadiationInstrumentState`: Unpowered, Measuring, Saturated, Invalid | Explicit initial state and serialization |
| Gauge / field / goals | `TransmissionGaugeMode`, `MagneticPolarity`, `ExposureGoalKind`, `ExposureBandState` | Typed through UI adapters, authors, simulation and fixtures |
| Source envelope / insert | `DecayPreset`, `IrradiatedMaterialState` | No free-text modes, aliases or migration |
| Open identities and quantities | Typed source/part/port IDs; distinct flux/exposure/energy/time value types | Convert only at validated UI/save boundaries |

## Per-element declarations

Each ID links to its individual record in requirements; presets within a row need separately identifiable proof.

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding | Lesson slots |
| --- | --- | --- | --- | --- | --- |
| [RAD-01](planning/requirements.md#radiation-01) Gamma source capsule | Static body + photon source (steady) | activity, band | Continuously raises nearby meters; distance and shielding reduce it | Selected-only field path overlay | 76, 78, 85 |
| [RAD-02](planning/requirements.md#radiation-02) Powered X-ray emitter | Electrical node + photon source (enable) | activity, band, direction | Emits only while supplied and enabled | Tube glow ← committed emission | 82, 89 |
| [RAD-03](planning/requirements.md#radiation-03) Alpha source cartridge | Charged source (short range) | activity, range | Reaches only a close meter; a thin screen stops it | Cap mark | 90, 93 |
| [RAD-04](planning/requirements.md#radiation-04) Beta-minus source cartridge | Charged source (negative) | activity, range | Passes paper, stopped by polymer, bent by a field | Cap mark | 91, 94 |
| [RAD-05](planning/requirements.md#radiation-05) Material shield panel | Body + attenuating material intervals | material, thickness preset | Reduces transmitted rate by material and thickness; also a physical mass | None (static) | 77, 79, 90, 91 |
| [RAD-06](planning/requirements.md#radiation-06) Powered radiation shutter | Electrical node + slider blade body + material interval | travel, speed | Blade opens/closes an exposure interval; a jammed blade stays open | Blade pose ← committed slider | 80, 85 |
| [RAD-07](planning/requirements.md#radiation-07) Collimator block | Absorbing body with aperture geometry | aperture width, direction | Addresses one receiver, excludes its neighbour | None | 81, 89 |
| [RAD-08](planning/requirements.md#radiation-08) Radiation rate meter | Electrical node + detector (rate, hysteresis) | On/Off thresholds, response | Needle reads current rate; switches a supplied load | Needle ← committed rate | 76, 83, 90, 91 |
| [RAD-09](planning/requirements.md#radiation-09) Integrating dosimeter | Detector (accumulating) + electrical output | budget threshold | Remembers exposure across gaps and darkness | Bar ← accumulated D | 84, 85 |
| [RAD-10](planning/requirements.md#radiation-10) Exposure-sensitive cargo badge | Dynamic cargo body + passive detector + goal | budget | Cargo fails if its route accumulates too much | Badge tint ← D | 86, 87 |
| [RAD-11](planning/requirements.md#radiation-11) Transmission gauge | Source reference + detector + calibration state | mode (sheet, tank), reference | Reports thickness or waterline, or Invalid | Dial ← transmission | 88, 89 |
| [RAD-12](planning/requirements.md#radiation-12) Scintillator tile | Absorbing converter + optical aperture | efficiency | Glows into a compatible optical receiver | Glow ← absorbed energy | 92, 97 |
| [RAD-13](planning/requirements.md#radiation-13) Decay clock capsule | Photon source with decay preset | half-life, A₀ | Rate falls over the Run; a falling transition can release a gate | Overlay dims ← A(t) | 98, 99 |
| [RAD-14](planning/requirements.md#radiation-14) Magnetic deflector | Electrical node + magnetic field region | polarity, strength | Bends charged paths around obstacles; reverse bends the other way | Coil hum/lamp ← supply | 93, 94 |
| [RAD-15](planning/requirements.md#radiation-15) Track chamber | Segmented detector volume | segment list | Shows which segment a charged path crosses; only that segment's contact fires | Trail ← committed segment hits | 95, 96 |
| [RAD-16](planning/requirements.md#radiation-16) Neutron source module | Neutron source (Fast) | activity | Only neutron detectors respond | Overlay | 100, 105 |
| [RAD-17](planning/requirements.md#radiation-17) Water moderator tank | Water store + neutron material (moderation) | fill level | Converts Fast to Slow as it fills; dry tank does nothing | Waterline ← volume | 101, 103 |
| [RAD-18](planning/requirements.md#radiation-18) Neutron absorber panel | Neutron material (absorption) | group fractions | Protects a downstream zone after moderation | None | 102, 105 |
| [RAD-19](planning/requirements.md#radiation-19) Neutron group detector | Detector with group selection | Fast/Slow, thresholds | Responds to the selected group only | Needle ← rate | 100, 101, 104 |
| [RAD-20](planning/requirements.md#radiation-20) Radioisotope thermoelectric generator | Decay heat source + thermal nodes + thermoelectric converter + electrical source | heat rating, coefficient | Powers a modest load only while hot/cold sides differ; cooling must be supplied | Fins tint ← T; meter ← power | 106, 107 |
| [RAD-21](planning/requirements.md#radiation-21) Radiation-responsive material latch | Load-bearing constraint + material memory | dose threshold, load | Weakens past its dose and releases its payload; never repairs in-Run | Insert crack ← state | 108, 109 |
| [RAD-22](planning/requirements.md#radiation-22) Sealed tracer capsule | Dynamic ball-compatible body + photon source | activity | Meters along opaque transport sequence a diverter | Overlay follows body | 110, 125 |

### Presentation

Retain the approved cream/navy/cyan/gold palette and Monument Valley-inspired composition. Sealed ceramic/brass-like capsules, thick beveled shield slabs, a gold meter needle and engraved particle/energy symbols; selected-only dotted field paths and a receiver bar make invisible transport legible without a literal glowing beam. No radioactive-green recolouring, strobes or screen-filling particles. Source strength, shutter position, current rate and accumulated exposure are distinct local cues; short contextual cards explain "measures now" versus "remembers exposure". Detector clicks are optional presentation with identical muted outcomes. Build preview shows the initial field without advancing decay, dose, heat or material state. Desktop and touch targets support construction, mode inspection and negative trials without a numeric placement menu.

## Proposed 150-level allocation

This is a curriculum budget, not 150 authored puzzles; existing component and lesson requirements remain open until mapped individually, and no current level is renumbered without updating content, typed identities, navigation, tooling and save boundaries together.

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
| **Total** | **150** | **75 foundation + 35 radiation introductions/practice + 40 integration/mastery.** |

Thirty-five consecutive family slots are a substantial curriculum cost: after testing, interleave their lessons with previously taught families while preserving prerequisites. If fewer candidates are selected, reclaim their slots for already-required components or mixed practice; never ship placeholders or label an unselected candidate implemented. No candidate is cut by this allocation.

<a id="concrete-radiation-lessons-slots-76110"></a>
### Concrete radiation lessons: slots 76–110

A lesson may show a new source and its measuring instrument together as one cause-and-effect relationship; otherwise it introduces one distinction, using fixed fixtures and a small editable inventory. Every control is a required Chrome/Playwright UI trial, not a recorded pass.

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

Every accepted element has an introduction, an independent control and later reuse. After authoring, maintain an element × mode × level matrix and find unintroduced dependencies before any capstone qualifies. Level 75 is a mixed-foundation checkpoint, not a claim of prior mastery. The eventual baseline is **150 × 3 difficulties × 4 placement variants = 1,800 distinct cells**; the existing 75-level/900-cell records remain unchanged historical evidence. Exhaustive difficulty/nudging sweeps follow element coverage.

## Chrome-observable acceptance for the first radiation slice

The first slice introducing this family delivers slot 76 **The Quiet Beacon** (RAD-01 Gamma source capsule + RAD-08 Radiation rate meter, with RAD-05 Material shield panel as its control) as one ELEMENT-n slice in [roadmap order](planning/invest/vertical-delivery.md#rolling-playable-roadmap). Through actual palette, gizmo and socket controls in Chrome/Playwright:

1. Place the capsule near a Battery-supplied rate meter, wire the meter contact to a release gate, Run: the needle rises, the gate opens and the ball reaches the Receiver (Solved).
2. Controls: capsule beyond the marked response region (needle low, gate shut); meter unsupplied (no release); a dense panel on the path lowers the needle below the Off threshold while the same panel beside the path does not.
3. Insertion order and camera position do not change the outcome; selected-only field overlay appears only when the capsule is selected.
4. Reset restores the exact construction and meter state; Save/Load where supported restores identical canonical bits.

Each later RAD element needs its own construction through real controls, verified placed material/thickness/source/energy/detector selection and typed connections, positive recipe, listed control, Reset comparison and retained evidence (commit and content hash, browser/build version, recipe, assertions, response histories, connection snapshots, screenshots, motion captures, failures). A single gamma demonstration cannot qualify all radiation parts. Focused cases before completion:

| Area | Focused cases |
| --- | --- |
| Transport/material | Zero source; exact aperture edges; grazing/thick/stacked/overlapping shields; empty space versus hole; two-source addition; moved/rotated shields; insertion-order invariance; bounded large scenes. |
| Measurement | On/off hysteresis boundaries; wrong radiation/group; equal integrated exposure from different burst patterns; rate removal without dose loss; saturation; invalid calibration and power interruption. |
| Motion | Fast crossings and slow dwell; blade jam/partial closure; moving source relative to cargo; endpoint-equivalent routes with different exposure histories. |
| Conversion/energy | Lost incident energy; serial scintillators; wrong optical aperture; no unpowered output; RTG zero gradient, startup, overload and heat balance. |
| Specialist states | Both deflector polarities and field-off; each chamber segment; fast/slow groups; dry/filling/draining moderator; absorber removal; decay pause/replay; irreversible latch; tracer edge/rearm. |
| Construction persistence | Reset during exposure, shutter travel, decay, heating, fill and latch release; current-schema save/load restores presets, transforms, materials, links and authored initial states. |
| Boundary contracts | Canonical enum mappings; reject unknown/undefined values, invalid combinations, unsupported endpoints and obsolete schemas; compile all changed authors, UI adapters, tools and tests. |
| Presentation | Muted/unmuted outcomes equal; no colour-only distinctions; desktop/mobile construction; selected-only overlays; actual state synchronized with animation. |

Open decisions: prove the smallest gamma/shield/meter loop and selected-only field display first; then dose and cargo, source control, inspection and scintillation; prototype alpha/beta range and magnetic routing readability before committing their campaign footprint (never reuse mirror reflection for curved particles); assess neutron two-group clarity and thermal/load readiness separately, keeping unresolved candidates visible rather than deleting them.
