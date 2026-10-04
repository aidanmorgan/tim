# Thermal component research — source research (current adoption is in requirements)

Research and planning date: 28 September 2026. Three sub-agents researched physical principles, generic architecture and grouped-element coverage. This document records the basis for the [37 individual thermal elements](planning/requirements.md#thermal-elements), [43 generic interaction processes](planning/requirements.md#generic-interaction-register), [14 interaction scenarios](planning/requirements.md#thermal-interaction-scenarios) and [campaign allocation](planning/requirements.md#thermal-campaign-allocation). All are design work, not implementation or playtest evidence.

## Design conclusions

Heating and cooling should emerge from generic energy, material and transport capabilities. Every element is an assembly/configuration of those capabilities; no source/target catalogue pair owns a physics rule. Physical sources below inform original game proposals rather than defining exact game constants or proving a particular simulation implementation.

The user's examples map to independently testable chains:

- Fire heating water: finite chemical reaction, heat transport, sensible/latent enthalpy, pressure-dependent boiling and gas transport.
- Freezing water: heat extraction to a declared sink, latent-energy removal and solid-fraction/topology evolution.
- Focused-light ignition: finite-width optical transport, surface absorption, temperature evolution and a generic material reaction model.

Likewise, steam drives a pressure actuator through fluid work; a fan changes heat exchange through airflow; a bimetal strip changes electrical contact through differential expansion. None requires an exception for a kettle, fan, water, lens or thermostat catalogue ID.

## Source register

<a id="th-s01"></a>

### TH-S01 — heat transfer and finite thermal stores

[OpenStax: Mechanisms of Heat Transfer](https://openstax.org/books/university-physics-volume-2/pages/1-6-mechanisms-of-heat-transfer) distinguishes conduction, convection and thermal radiation. It supports heat-routing, insulation and passive rejection concepts. Proposed part geometry, controls and thresholds are our design choices. A named environmental reservoir must account for external energy exchange.

<a id="th-s02"></a>

### TH-S02 — phase change

[OpenStax: Phase Changes](https://openstax.org/books/university-physics-volume-2/pages/1-5-phase-changes) supports latent-energy accounting and pressure-dependent transitions. Melting/boiling need energy; freezing/condensation reject it. A phase fraction must not change simply because a decorative animation or timer ended. Solid water, liquid water and vapor remain conserved material through the supported representation.

<a id="th-s03"></a>

### TH-S03 — combustion and suppression

[NIST: Fire Dynamics](https://www.nist.gov/el/fire-research-division-73300/firegov-fire-service/fire-dynamics) describes coupled fuel, oxidizer, energy and transport conditions. [NIST: Fire Fighting Properties](https://www.nist.gov/publications/fire-fighting-properties-nistir-6191) motivates physical suppression mechanisms rather than an unconditional water-touch event. The game needs bounded material/reaction models, not detailed practical fire-making recipes or full flame chemistry. Striker/match assemblies deposit finite modeled energy and use the same ignition state as other heating sources.

<a id="th-s04"></a>

### TH-S04 — optical heating

[DOE: Concentrating Solar-Thermal Power Basics](https://www.energy.gov/cmei/systems/concentrating-solar-thermal-power-basics) explains concentrating radiant input for useful heat; [NASA's archived sunlight lesson](https://cdaweb.gsfc.nasa.gov/pub/documents/archived_websites/pwg.gsfc.nasa.gov/stargaze/Lsun1lit.htm) provides a focusing/heating educational reference. Optical concentration changes local irradiance, not total source power. The receiving material's absorptance, heat capacity and losses govern its resulting temperature.

<a id="th-s05"></a>

### TH-S05 — active cooling

[DOE: Heat Pump Systems](https://www.energy.gov/energysaver/heat-pump-systems) describes supplied work moving heat between surroundings. Our two-port heat-pump proposal must reject removed heat plus input work. Its operating-range/efficiency model must be explicit; disconnecting electricity cannot leave an unlimited cold surface. A finite cold pack is a different store, not a pump.

<a id="th-s06"></a>

### TH-S06 — thermal expansion

[OpenStax: Thermal Expansion](https://openstax.org/books/university-physics-volume-2/pages/1-3-thermal-expansion) supports temperature-dependent dimensions, density and differential expansion. Rods, bimetal strips, heated gas containers and hot-air lift are separate game assemblies using shared material/constraint/pressure/buoyancy processes. Constrained strain must feed real mechanical stress instead of overriding transforms.

<a id="th-s07"></a>

### TH-S07 — heat to mechanical work

[OpenStax: Heat Engines](https://openstax.org/books/university-physics-volume-2/pages/4-2-heat-engines) provides the heat-flow/work/rejection basis. Steam machinery proposals must account for pressure/enthalpy drop, load, exhaust and returned condensate. A closed fluid loop is not an independent energy source.

<a id="th-s08"></a>

### TH-S08 — evaporative cooling

[DOE: Cooling Tower Management](https://www.energy.gov/cmei/femp/best-management-practice-10-cooling-tower-management) describes heat rejection that consumes water through evaporation. Our pad is a small original puzzle implementation of that principle: finite wetting, capillary transport, ambient humidity and latent transfer. It does not require a full industrial cooling tower model.

<a id="th-s09"></a>

### TH-S09 — thermoelectric conversion

[DOE: Generating Light from Darkness](https://www.energy.gov/science/bes/articles/generating-light-darkness) discusses electrical generation from a maintained temperature difference. The generic converter must accept compatible hot/cold thermal ports independent of heat-source identity; an RTG combines decay heat with this same conversion capability.

## Generic architecture and performance

The [mandatory TODO contract](planning/requirements.md#generic-interaction-contract) requires typed capabilities, enthalpy/material state, conserved transfer proposals and deterministic coupling. [NIST's phase-field model formulation guide](https://pages.nist.gov/pf-recommended-practices/bp-guide-gh/ch1-model-formulation.html) is a reference for conservation in coupled material models, not a requirement to implement full phase-field numerics.

A practical first model may use lumped thermal nodes, finite fluid/gas parcels and bounded constitutive laws. Declare limitations and reject unsupported conditions explicitly. Numerical approximation is acceptable; changing the physics based on catalogue names, level identity or a hard-coded pair is not.

Subsystem pruning requires conservative transitive closure over placed elements, available inventory, material models, goals, environment, profile and every reachable reaction/phase/spawn product. Category labels cannot prove isolation. Ice may introduce liquid, optical energy may initiate combustion, and mechanical loss may require heat accounting. Prove equivalence to fully enabled execution and record performance savings. Authoring simplification and pruning are distinct decisions.

## Individuality and teaching

The [nonthermal individual register](planning/requirements.md#individual-element-register) expands 216 existing element obligations without replacing historical evidence. Single-element specifications elsewhere in TODO remain valid; family tables are indexes only. Unnamed parameter ranges still require typed supported-mode definitions and individual proof before closure.

Thermal first use is distributed across electricity, optics, fluids, gas and storage lessons, rather than placing every new concept in levels81–90. Each TH record supplies exact introduction, practice and reuse slots. All 150 levels, GAP obligations and radiation reservations remain accounted for; the enlarged staged lessons still require real-player pacing validation.

Follow the existing palette and Monument Valley-inspired forms in DESIGN.md. Use gauges, shape, contained phase boundaries and actual moving mechanisms. A white plume is not evidence of gaseous water; observation must inspect conserved state/flow as well as visible art. Human playtesting must establish that the concepts are readable and enjoyable.
