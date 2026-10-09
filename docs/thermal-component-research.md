# Heat capability family

Heat is one capability family inside the generic WASM SIMD128 f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory), [general data-driven engines](engine-contracts.md#general-data-driven-engines)). It is added as generic records in the slice that first needs it; every one of the [37 thermal elements](planning/requirements.md#thermal-elements) (TH-01–TH-37) is declaration data over those records, the [43 generic interaction processes](planning/requirements.md#generic-interaction-register) are its laws, and the [14 interaction scenarios](planning/requirements.md#thermal-interaction-scenarios) and [campaign allocation](planning/requirements.md#thermal-campaign-allocation) are its teaching content. No source/target catalogue pair owns a physics rule: fire heating water, freezing water, focused-light ignition, steam driving a piston, a fan changing heat exchange and a bimetal strip closing a contact are all compositions of the same records.

## Capability family

### State variables (canonical IEEE-754 f32, SI units)

| Record | State | Derived (no independent setter) |
| --- | --- | --- |
| Thermal node (lumped) | Internal energy U (J), mass m (kg), material (specific heat c, latent heats, melting/boiling points, absorptance, emissivity), phase fraction f ∈ [0,1] per transition | Temperature T from U, m, c and the active phase segment |
| Thermal link | Conductance G (W/K) between two nodes, or convective coefficient scaled by the airflow exposure weight, or radiative pair factor | Heat flow q = G·(T₁ − T₂) per substep, clamped to the available enthalpy difference |
| Heat source/sink | Finite fuel or electrical supply (J), power rating (W), ignition state, declared environment reservoir temperature | Power delivered this substep ≤ min(rating·dt, remaining fuel) |
| Reaction state | Ignition threshold temperature, burn rate, oxidiser availability flag, suppression input | Ignited/burning/extinguished transitions sampled at substep endpoints |
| Expansion coupling | Thermal expansion coefficient, constrained length, bimetal differential | Strain feeds a joint/contact row as stiffness-scaled displacement, never a direct transform write |
| Thermal sensor | On/Off threshold temperatures (separate), dwell in ticks | Boolean output at substep endpoints |

### Laws at puzzle scale

Each law is a shared process over declared records; the numbered sources below are evidence for the principle, not game constants. Values are clamp-or-continue under the [game-grade envelope](gpu-f32-physics.md#game-grade-envelope): a rounding residual in heat flow or phase fraction never faults a tick and never creates energy.

<a id="th-s01"></a>

**TH-S01 — Heat transfer and finite thermal stores.** Conduction, convection and radiation are three link kinds between lumped nodes ([OpenStax: Mechanisms of Heat Transfer](https://openstax.org/books/university-physics-volume-2/pages/1-6-mechanisms-of-heat-transfer)). Insulation is low conductance; a heat sink is high conductance to the environment node; routing heat is placing links. A named environment reservoir accounts for every external exchange.

<a id="th-s02"></a>

**TH-S02 — Phase change.** Melting and boiling consume latent energy; freezing and condensation reject it; the transition pressure follows the sealed-gas pressure where one is declared ([OpenStax: Phase Changes](https://openstax.org/books/university-physics-volume-2/pages/1-5-phase-changes)). A phase fraction changes only through accounted energy, never because an animation or timer ended. Ice, liquid water and vapour remain one conserved material.

<a id="th-s03"></a>

**TH-S03 — Combustion and suppression.** A reaction state couples fuel, oxidiser, energy and transport ([NIST: Fire Dynamics](https://www.nist.gov/el/fire-research-division-73300/firegov-fire-service/fire-dynamics)); suppression removes heat or oxidiser rather than acting as an unconditional water-touch event ([NIST: Fire Fighting Properties](https://www.nist.gov/publications/fire-fighting-properties-nistir-6191)). Strikers, matches and tinder deposit finite energy into the same ignition state as every other heating source.

<a id="th-s04"></a>

**TH-S04 — Optical heating.** A converging lens raises local irradiance, not total source power ([DOE: Concentrating Solar-Thermal Power Basics](https://www.energy.gov/cmei/systems/concentrating-solar-thermal-power-basics), [NASA sunlight lesson](https://cdaweb.gsfc.nasa.gov/pub/documents/archived_websites/pwg.gsfc.nasa.gov/stargaze/Lsun1lit.htm)). The receiving node's absorptance, heat capacity and link losses set its temperature.

<a id="th-s05"></a>

**TH-S05 — Active cooling.** A heat pump moves heat between two ports using supplied work and rejects removed heat plus input work ([DOE: Heat Pump Systems](https://www.energy.gov/energysaver/heat-pump-systems)); its coefficient and operating range are declared, and losing supply ends pumping. A cold pack is a finite store, not a pump.

<a id="th-s06"></a>

**TH-S06 — Thermal expansion.** Temperature-dependent length, density and differential expansion feed constraint rows ([OpenStax: Thermal Expansion](https://openstax.org/books/university-physics-volume-2/pages/1-3-thermal-expansion)). Rods, bimetal strips, heated gas containers and hot-air lift share the material, constraint, sealed-gas and buoyancy records.

<a id="th-s07"></a>

**TH-S07 — Heat to mechanical work.** Steam machinery accounts pressure/enthalpy drop, load, exhaust and returned condensate ([OpenStax: Heat Engines](https://openstax.org/books/university-physics-volume-2/pages/4-2-heat-engines)); the chamber and nozzle records of the [gas family](finite-gas-foundation.md) do the work. A closed loop is not an energy source.

<a id="th-s08"></a>

**TH-S08 — Evaporative cooling.** A wet pad rejects heat by consuming water through latent transfer, bounded by wetting and ambient humidity ([DOE: Cooling Tower Management](https://www.energy.gov/cmei/femp/best-management-practice-10-cooling-tower-management)).

<a id="th-s09"></a>

**TH-S09 — Thermoelectric conversion.** A maintained temperature difference across hot/cold ports yields finite electrical power ([DOE: Generating Light from Darkness](https://www.energy.gov/science/bes/articles/generating-light-darkness)); the converter accepts any compatible hot/cold nodes, so a radioisotope generator is decay heat plus this same record.

### Parameters, sensors, sources, stores and network nodes

| Parameter | Unit | f32 range / scale | Notes |
| --- | --- | --- | --- |
| Temperature | K | 128–2048 (scale 2³) | Environment node typically 288 |
| Specific heat c | J/(kg·K) | 1/8–8 (scale 2⁹) | Material table |
| Latent heat | J/kg | 1/16–16 (scale 2¹⁷) | Per transition |
| Conductance G | W/K | 2⁻¹⁰–2⁸ | Per link |
| Source power | W | 1/16–256 (scale 2³) | Candle, plate, bowl |
| Fuel / supply | J | finite (scale 2¹⁰) | Depletes to zero |
| Ignition threshold | K | 400–1200 | Material table |
| Expansion coefficient | 1/K | 2⁻¹⁰–2⁻⁴ (scale 2⁻¹⁰) | Rod, bimetal |
| Sensor thresholds | K | On > Off by ≥ 2 | Hysteresis, dwell in ticks |

Sources: Candle, Fire bowl, Electrical heating plate, Friction brake, Flint striker, Spring-mounted match, Solar absorber plate with Converging lens. Stores: Thermal storage block, Phase-change cartridge, Cold pack, Ice block, Kettle contents. Network nodes: Heat-conducting bar, Insulating panel, Finned heat sink, Heat exchanger, Reversible heat pump, Evaporative pad. Sensors: Temperature sensor, Bimetal thermostat, Fusible link, Heat-sensitive target.

## Per-element declarations

Every row cites the laws it composes; each element still needs its own [named-elements](planning/invest/named-elements.md) proof.

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| TH-01 Candle | Finite fuel source + reaction state + convective link (S01, S03) | power, fuel, ignition T | Burns down while lit, heats what is above it, goes out when fuel ends or is suppressed | Flame scale ← committed power; wax height ← fuel |
| TH-02 Fire bowl | Larger fuel source + reaction state (S03) | power, fuel, oxidiser | Hotter, wider heat; smothering a lid puts it out | Flame ← power |
| TH-03 Combustible block | Node with ignition threshold + fuel (S03) | ignition T, burn rate | Ignites from a hot neighbour, burns and loses mass | Char fraction ← burnt fraction |
| TH-04 Electrical heating plate | Electrical node + source (S01) | watt rating | Warms contacts only while supplied | Glow ← committed power |
| TH-05 Friction brake | Contact friction work → heat source (S01) | friction, mass | Rubbing a shaft warms the pad | Pad glow ← power |
| TH-06 Converging lens | Optical refraction + irradiance concentration (S04) | focal length | Focused beam heats a target at its focus; off-focus does not | None (static) |
| TH-07 Solar absorber plate | Node with absorptance + optical receiver (S04) | absorptance, mass | Warms under a Flashlight/laser or lens spot | Plate tint ← T |
| TH-08 Heat-conducting bar | Conduction link (S01) | conductance | Carries heat between touching nodes | None |
| TH-09 Insulating panel | Low-conductance link (S01) | conductance | Slows heat loss; protects cargo | None |
| TH-10 Finned heat sink | High-conductance link to environment + airflow scaling (S01) | conductance, exposure | Cools faster under a Fan | None |
| TH-11 Heat exchanger | Two-stream link (S01) | conductance | Moves heat between two fluid paths | Flow arrows ← q |
| TH-12 Reversible heat pump | Electrical node + two-port pump (S05) | coefficient, range | Cools one side, warms the other while supplied | Compressor spin ← power |
| TH-13 Freezing mold | Node + phase transition (S02) | volume | Water freezes into a solid cargo when cooled | Ice fraction ← f |
| TH-14 Ice block | Solid-phase node + body (S02) | mass | Melts into accounted water, shrinks | Scale ← solid fraction |
| TH-15 Ice plug | Ice block as a gate (S02) | mass | Releases a path when melted | Scale ← f |
| TH-16 Fusible link | Thermal sensor + constraint breaking (S02) | melt T | Breaks a held load above its melt point | Link snap ← state |
| TH-17 Kettle | Node + sealed gas store + nozzle (S02, S07) | capacity | Boils to steam pressure that can drive a piston or whistle | Lid rattle ← pressure |
| TH-18 Condenser | Cold link + phase transition (S02) | conductance | Turns steam back into accounted water | Drip rate ← q |
| TH-19 Steam piston | Chamber + slider body (S07) | stroke, load | Extends under steam pressure against load | Rod ← slider |
| TH-20 Steam turbine | Nozzle + rotary capture (S07) | pitch, inertia | Spins a shaft from steam flow | Rotor ← hinge angle |
| TH-21 Temperature sensor | Thermal sensor + electrical output (S01) | On/Off T | Switches a supplied load above threshold | Needle ← T |
| TH-22 Bimetal thermostat | Differential expansion → contact (S06) | bend coefficient | Opens/closes a contact as it warms; no battery needed to move | Strip bend ← strain |
| TH-23 Expansion rod | Expansion → slider constraint (S06) | coefficient, length | Pushes a lever as it warms | Rod length ← strain |
| TH-24 Gas expansion bladder | Sealed gas store + node (S06) | material | Inflates when heated, pushes cargo | Skin ← volume |
| TH-25 Hot-air balloon | Sealed gas store + buoyancy region (S06) | volume | Lifts when its air is heated | Envelope ← volume |
| TH-26 Thermal storage block | High-capacity node (S01) | mass, c | Stays warm after the source stops | Tint ← T |
| TH-27 Phase-change storage cartridge | Node + latent store (S02) | latent heat | Holds temperature during a transition | Tint ← T |
| TH-28 Evaporative cooling pad | Wet node + water store (S08) | wetting | Cools airflow while wet | Damp fraction ← water |
| TH-29 Cold pack | Finite cold store (S05) | capacity | Absorbs heat until spent | Frost ← remaining |
| TH-30 Thermoelectric generator | Two thermal ports → electrical source (S09) | coefficient | Powers a load while a temperature difference exists | Meter ← power |
| TH-31 Flint striker | Impact trigger → finite ignition energy (S03) | energy per strike | Lights tinder when struck | Spark ← occurrence |
| TH-32 Tinder pad | Low-threshold combustible (S03) | ignition T | Catches from a striker or lens, lights the next fuel | Ember ← state |
| TH-33 Spring-mounted match | Spring + friction strike + ignition (S03) | stiffness | Releases, strikes, lights | Match pose ← hinge |
| TH-34 Timed toaster ejector | Heating plate + timer + spring eject (S01) | watt rating, time | Warms cargo then pops it out | Lever ← state |
| TH-35 Coffee-pot steam vessel | Node + sealed gas + nozzle (S02, S07) | capacity | Perks and vents steam to a receiver | Lid ← pressure |
| TH-36 Lamp-trigger apparatus | Heat sensor ↔ lamp activation (S01) | On/Off T | Lights a lamp when warm | Lamp ← output |
| TH-37 Heat-sensitive target | Goal node with threshold (S01) | target T, dwell | Solved when held above temperature for the dwell | Tint ← T |

Thermal first use is distributed across electricity, optics, fluids, gas and storage lessons rather than one late chapter; each TH record keeps its introduction, practice and reuse slots in the campaign plan. Gauges, contained phase boundaries and actual moving mechanisms show state; a white plume is never evidence of gaseous water. Preserve the [DESIGN.md](../DESIGN.md) palette and forms.

## Chrome-observable acceptance for the first heat slice

The first slice introducing this family (an ELEMENT-n slice in [roadmap order](planning/invest/vertical-delivery.md#rolling-playable-roadmap)) composes TH-04 Electrical heating plate, TH-21 Temperature sensor and the existing Signal lamp. Through actual palette, gizmo and socket controls in Chrome/Playwright:

1. Battery → Switch → Heating plate under a Temperature sensor → Signal lamp: after Run the sensor needle rises and the lamp lights once the On threshold holds for its dwell.
2. Controls: unsupplied plate (needle stays at ambient, lamp dark); an Insulating panel between plate and sensor delays or prevents the rise; the lamp goes dark again only after the needle falls below the separate Off threshold.
3. No free energy: a plate that is switched off cools toward the environment node and never re-lights the lamp.
4. Reset restores every node to its authored temperature, phase fraction and fuel; Save/Load where supported restores identical canonical bits.

Lifecycle, production build and ordinary resource ownership apply per slice; detailed performance remains at its named release gate.
