# Bounded model decisions inside existing owners

Each row settles one model question for a generic capability family before the slice that first needs it ([compilation model](../../gpu-f32-physics.md#compilation-model)). Reuse an applicable independently reviewed answer; otherwise take exactly one question, build the named construction in Chrome through real controls, and stop with the adopted/rejected model, units/ranges and the player-observable expected result at puzzle scale within the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope). Elements never get their own solver; the [engine contracts](../../engine-contracts.md#general-data-driven-engines) are the authority for that rule. A diagnostic does not pass runtime acceptance. Each decision is a supporting design criterion of its functional owner, with the same pair and required-stage review.

[Full requirement bodies](../requirements.md) · [Family boundaries](profiles.md).

<a id="s257"></a>
## S257

Exact original domain criterion and technical stage: S257. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| electrical-port: Which electrical source/load/control roles, directions and capacities are valid without conflating signal with power? | One source/load/control wired through real controls; a reversed or incompatible wire is refused at connection and the construction is unchanged; a full-capacity attempt refuses the extra wire. | S270 |
| mechanical-port: Which shaft/belt/chain attachment roles and direction/capacity constraints preserve distinct connection contracts? | One supplied shaft pair per connection kind drives its load in the declared direction; an invalid attachment is refused; chain and belt are not interchangeable in the palette. | S690 |
| rope-port: Which rope anchors/sheaves/control attachments own length and load, and which attachments reject? | One loaded routed rope lifts its load; a slack rope lifts nothing; an invalid endpoint or over-capacity attachment is refused and the construction is unchanged. | S688 |

<a id="s416"></a>
## S416

Exact original domain criterion and technical stage: S416. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| advection: Which finite-volume transport update preserves mass at empty, full and split/merge boundaries? | Two containers and one branching route: the receiving levels rise by what the source loses; an empty source sends nothing; Reset restores the levels. | S418 |
| pressure-work: How does the pressure/head model move a loaded hydraulic boundary? | One source, piston and return: the piston moves under head, stalls against a blocked load and stops when the supply is depleted. | S419 |
| buoyancy: Which displaced-volume/immersion rule applies? | One body crossing the surface: a lighter-than-water body floats at a visible waterline, a denser one sinks; the water level stays conserved. | S420 |
| capillary: Which bounded wick model and capacity limit are adopted? | One reservoir and porous path: the wick wets up to its limit, a dry reservoir wets nothing, reversed head does not lift. | S421 |

<a id="s470"></a>
## S470

Exact original domain criterion and technical stage: S470. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| gas-state: Which pressure-volume-temperature update represents the sealed-gas state? | Two connected volumes: compressing one visibly pushes the other; a closed valve isolates them; an exhausted source stops movement; Reset exact. | S471 |
| open-versus-sealed: Which declarations expose an open airflow field versus sealed pressure ports? | One fan/receiver and one sealed reservoir/valve: cross-domain wiring is refused; the fan pushes a ball, the sealed valve moves its actuator. | S697 |

<a id="s484"></a>
## S484

Exact original domain criterion and technical stage: S484. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| finite-colour: How does one optical source allocate finite per-channel output across simultaneous receivers? | One split source and two receivers: neither receiver reads more than it would alone; a wrong-channel or no-light receiver stays off. | S485 |
| optical-commit: Which snapshot boundary prevents moving occlusion or multiple receivers duplicating light? | One moving occluder across a shared beam: receivers switch together at the crossing; no receiver flickers on while occluded. | S486 |
| optical-law: Which interval transport model handles partial occlusion and transparent/hollow paths? | One beam through transparent, hollow and opaque parts: receiver lights behind transparent and hollow, off behind opaque. | S488 |
| absorption: How is absorbed light converted into material heat without duplicate power? | One illuminated absorber and a transparent control: the absorber warms visibly, the transparent part does not; unlit control stays cold. | S489 |

<a id="s528"></a>
## S528

Exact original domain criterion and technical stage: S528. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| arrival-combination: Strongest arrival or additive intensity for simultaneous paths? | One equal two-path arrival and one unequal delayed path on a Sound meter: choose one rule, observe the meter reading at the chosen threshold, record the rejected alternative. | S530 |
| event-routing: How are event identity, delay and loop rejection represented without suppressing a later pulse? | One horn/duct loop with repeated arrivals and a second strike: the meter triggers once per admissible path, the loop does not grow, the second strike triggers again. | S529 |

<a id="s543"></a>
## S543

Exact original domain criterion and technical stage: S543. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| conduction: Which contact/conductivity law transfers heat between finite stores? | Two touching bodies at unequal temperature equalise visibly; an insulated control does not. | S544 |
| convection: Which fluid-to-surface transfer model governs convection? | One flowing material over a surface warms/cools it; disabled exchange leaves it unchanged. | S545 |
| thermal-radiation: Which radiant heat exchange law applies, separate from ionizing transport? | Two facing surfaces exchange heat; an occluder between them stops it. | S546 |
| melting: How are latent heat and state represented through melting? | A solid heated below the point stays solid; at/above it loses support and melts; Reset restores the solid. | S547 |
| freezing: How does cooling cross freezing? | A liquid cooled to the point visibly solidifies; without a colder sink it stays liquid. | S548 |
| evaporation: Which surface evaporation limit debits liquid? | A wet surface dries over time; a dry or exhausted one shows no vapour. | S549 |
| boiling: Which pressure/temperature boundary permits boiling? | A heated vessel produces vapour only above its boundary; empty vessel produces none. | S550 |
| condensation: How does vapour release heat and become liquid? | A cooled vapour stream yields visible liquid; insufficient cooling yields none. | S551 |
| sublimation: Which solid-to-gas transition is supported? | A declared material sublimes directly; an unsupported material is refused. | S552 |
| deposition: Which gas-to-solid transition is supported? | A gas deposits solid on a cold surface; a warm surface grows nothing. | S553 |
| reaction: Which stoichiometry and rate limit are adopted? | Two finite reactants produce products until one is exhausted; a missing reactant produces nothing. | S554 |
| ignition: Which conditions initiate combustion? | Sufficient heat with oxidiser ignites; insufficient energy or no oxidiser does not. | S555 |
| extinction: Which boundary stops combustion? | Each declared suppression cause visibly stops the burn and leaves residue. | S556 |
| expansion: How does expansion change dimensions and exert work? | A heated free member lengthens; a constrained one pushes its load; unheated control is unchanged. | S557 |
| thermal-stress: Which gradient/constraint model reaches failure? | A constrained member fails under differential heating, not uniform heating. | S558 |
| heat-pump: Which work/heat bound applies to active cooling? | A powered pump cools its cold side and warms its hot side; unpowered does nothing. | S559 |
| thermoelectric: How does a temperature difference yield electrical work? | Hot/cold pair lights a lamp; equal temperatures or open load light nothing. | S560 |
| phase-storage: How does a phase store retain/release latent energy? | A charged store keeps a load warm after the source stops; a full store takes no more. | S561 |
| coating: Which deposited material changes surface properties? | A coated surface behaves differently (friction/heat) only where coated; unsupported pairing refused. | S562 |
| fracture: Which strength rule creates fragments? | Just above the break condition the object splits into bounded pieces; just below it holds; unsupported material refused. | S563 |
| electrical-heat: Where does dissipated electrical work enter heat? | A loaded resistor warms; disconnected or exhausted supply leaves it cold. | S564 |
| phase-topology: Which committed phase change adds/removes geometry atomically? | A melting barrier opens a path the ball can use; the old and new shapes never coexist; Reset restores. | S565 |
| temperature-strength: Which strength-versus-temperature curve applies? | A loaded hot member fails where the cold one holds; display colour never selects behavior. | S566 |

<a id="s605"></a>
## S605

Exact original domain criterion and technical stage: S605. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| photon: Which Photon attenuation/dose model maps source to receiver? | One source and two shield thicknesses: thicker shield reads less; missing source or gap reads as declared. | S606 |
| alpha: Which Alpha range/stopping law applies? | Thin shield passes, thick shield stops; no optical substitute lights the detector. | S606 |
| beta-minus: Which BetaMinus deflection model and sign apply? | Through a field the trail bends one way; reversed polarity bends the other; no field goes straight. | S606 |
| neutron: How do Fast/Slow groups moderate/absorb? | With moderator the slow detector reads, without it the fast detector reads; absent source reads nothing. | S606 |
| decay: Which deterministic activity envelope distinguishes a decaying capsule? | At successive declared intervals the detector reads less; Reset restores the initial reading; a steady source does not fade. | S607 |

<a id="s635"></a>
## S635

Exact original domain criterion and technical stage: S635. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| field-response: Which field/material force law supports substitutable responders? | A supported responder moves in the field, an unsupported one does not; zero source or reversed field reverses/halts motion. | LAW-FIELD-I |
| granular: Which grain representation preserves openings, jamming and finite material? | Finite feed passes a wide opening and jams a narrow one; the count delivered never exceeds the count fed. | LAW-GRANULAR-I |
| controller: Which typed finite-state controller expresses sensing/intent? | A sensed target produces the declared action; missing supply or unsupported command produces none; nothing teleports. | LAW-CONTROLLER-I |
| goal-occurrences: Which occurrence identity/window/order rules implement the four goal primitives? | One level per quantity/rate/order/protected-final variant: duplicate, out-of-window, wrong-order and unsafe-final routes do not solve; the intended route shows Solved once. | LAW-GOALS-I |
| environment: Which gravity/atmosphere/material ranges are immutable for a Run and persisted? | A nondefault environment changes the ball's fall visibly; out-of-range values are refused; Save/Load and Reset keep it. | LAW-ENVIRONMENT-I |
