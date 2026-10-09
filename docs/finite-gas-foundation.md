# Gas and airflow capability family

Fluid/gas is one capability family inside the single generic WebAssembly SIMD f32 physics solver described by the [canonical f32 design and capability inventory](gpu-f32-physics.md#capability-inventory) under the [general data-driven engine contract](engine-contracts.md#general-data-driven-engines). The solver already carries the **force region** capability (fan/jet acceleration law) and **finite stores and sources**; this document adds the sealed gas store, gas network node, nozzle source and rotary capture records the airflow elements declare. Every Fan, Bellows, Windmill, hose, valve, reservoir and balloon is declaration data over these records; none has its own kernel branch, equation table or update loop. Status lives in [TODO](../TODO.md); element obligations live in [named-elements](planning/invest/named-elements.md).

<a id="airflow-transfer"></a>
<a id="rotary-capture"></a>
<a id="sealed-gas"></a>
## Capability family

### State variables (canonical IEEE-754 f32)

| Record | State | Derived (no independent setter) |
| --- | --- | --- |
| Airflow source | Flow speed u (m/s), force rating F (N), power rating P (W), remaining reservoir E (J), direction, reach, width, enabled flag | Allowance this substep = min(F, P/u, E/dt) shared by every branch |
| Airflow receiver (linear) | Exposure weight w ∈ [0,1] from four weighted samples at the committed/candidate pose with occlusion, receiver velocity v along the flow axis | Conductance c = (F/12)·w·pressureScale; branch force = min(c·(u − v), allowance share) |
| Rotary capture (rotor) | Hinge angular speed ω, inertia I, declared pitch p, resistance b, external load/backdrive torque | Transfer torque p·c·(u − p·ω); unloaded equilibrium ω = p·c·u/(c·p² + b) |
| Sealed gas store | Mass m (kg), volume V (m³), internal energy U (J); material R, Cv, inclusive temperature limits, maximum density, maximum absolute pressure | T = U/(m·Cv), ρ = m/V, p = ρ·R·T, Cp = Cv + R |
| Chamber | Signed-area volume bound to the carrier body pose and piston travel | Pressure at zero travel and finite-interval effort from the same gas potential |
| Nozzle source | Throat area, regime tag (NoFlow, Subsonic, Choked), stagnation enthalpy, outlet enthalpy | Mass continuity, jet momentum, pressure thrust |
| Gas network node | Typed species identity, port list (supply, exhaust, inlet, outlet), valve state | Shared inventory across simultaneous consumers |

### Laws at puzzle scale

- **Linear transfer.** One source allowance supplies every connected branch jointly; branches are allocated without mutating the candidate pose, paired nozzle/receiver reactions preserve linear and angular momentum, and the source reservoir debits exactly the work delivered plus paired loss. A disabled source plate, a blocked or missed path and an exhausted reservoir each transfer zero. Stationary receivers may accept impulse with zero receiver work. The balanced transfer abstraction has zero ambient exchange and no unloaded thrust; unloaded emission needs the nozzle record.
- **Rotary capture.** The rotor is a hinge-constrained rigid body with finite inertia, calm-air passive coast-down (resistance b) and typed mechanical output connections. For several branches the signed sum of aligned capped branch forces plus p·alignment·ω fixes cut-in and the bounded unloaded speed. Compile selects a feasible positive pitch and nonnegative resistance for the authored response; the solver never assigns or clamps shaft velocity and never adds an independent drive torque. Front/rear drive, edge-on and opposing sources follow from signed axis alignment of the four samples.
- **Sealed gas.** Homogeneous single-species calorically perfect ideal gas. A reversible adiabatic volume change gives U₂ = U₁·(V₁/V₂)^(R/Cv) with unchanged mass; work on the gas is U₂ − U₁ and expansion returns negative work. Gas energy gain debits mechanical work and expansion credits the same work in the same substep. Vacuum, zero inventory, non-finite or out-of-limit material states are compile-time admission failures, never a hidden ambient default.
- **Nozzle and finite discharge.** A converging nozzle declares NoFlow, Subsonic and Choked regimes with mass continuity, stagnation enthalpy, momentum and pressure thrust. Finite adiabatic discharge cools a rigid reservoir; final mass, internal energy and outlet enthalpy are accounted. A Bellows emits unloaded flow through its nozzle record and refills from its declared environment with 4 N·s/m refill damping; receiver impulse alone never substitutes for emission.
- **Envelope.** Region and transfer accelerations pass through the shared clamps (64 m/s² linear, 1024 rad/s² angular; authored fan/jet regions A ≤ 16 m/s²). Rounding residual in work, inventory or pressure is clamp-or-continue under the [game-grade envelope](gpu-f32-physics.md#game-grade-envelope); it never creates energy, never faults a tick and never drops elapsed time. No free energy: a Windmill in still air never starts turning, a Fan with an empty reservoir pushes nothing, and simultaneous consumers cannot overdraw one store.

### Parameters, sensors, sources, stores and network nodes

| Parameter | Unit | Canonical f32 range / scale | Notes |
| --- | --- | --- | --- |
| Fan flow speed | m/s | 12 (scale 2⁰) | Authored constant per Fan declaration |
| Fan force rating | N | 1/16–256 (scale 2⁰) | Authored; watt rating = force × 12 |
| Fan construction reservoir | J | 14,400 (scale 2⁶) | Finite; recharge only through a declared electrical supply |
| Bellows flow | m/s | −sliderSpeed × 15 | Slider speed is the committed physical velocity of the handle body |
| Bellows force / power rating | N / W | authored / authored × 12 | Constrains actual compression and paired reactions |
| Receiver conductance | N·s/m | Force/12 × sample weight × pressure | Four samples, occluded samples weigh zero |
| Refill damping | N·s/m | 4 | One damper; no duplicate compression damper |
| Windmill response | — | 0.1–4 | Selects feasible pitch/resistance at compile |
| Windmill cut-in | m/s | 0.05 | Below cut-in the branch force is zero |
| Unloaded target speed | rad/s | ≤ 12 | Bound on requested unloaded equilibrium, not a velocity clamp |
| Gas R, Cv | J/(kg·K) | material table (scale 2⁸) | Strictly positive |
| Gas m, V, U | kg, m³, J | strictly positive within material limits | Scale exponents declared per store |

Sources are the Fan (electrical supply + reservoir), Bellows (slider-driven nozzle), Air compressor (electrical supply → sealed store), Air nozzle and Sprinkler/jet regions. Stores are the Fan reservoir, Air reservoir and every sealed chamber. Network nodes are pneumatic hose ports, release/directional valves and T-junction splits that share, never copy, inventory. Sensors are the Pneumatic pressure gauge and Flow meter (threshold with separate On/Off hysteresis, sampled at substep endpoints, dwell in ticks).

## Per-element declarations

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| Fan (CAT-028 / EL-209) | Static body + airflow source + finite store + electrical network node | supply, force, reach, width; flow 12 m/s; reservoir 14,400 J | Pushes exposed bodies and rotors along its axis while supplied; stops when supply or reservoir ends | Blade spin rate ← committed source flow; housing static |
| Bellows (CAT-010) | Handle slider body + nozzle source + refill damper | force, reach, width; flow −sliderSpeed × 15 | Compressing the handle emits a finite puff that moves cargo, rings chimes or drives a rotor; the loaded plate holds | Bag squash ← committed slider travel |
| Windmill (CAT-070) | Hinge-constrained rotor body + rotary capture + mechanical output port | response 0.1–4, cut-in 0.05, cap 12 rad/s, inertia, load | Spins under a Fan or Bellows, slows in calm air, drives connected shafts; rear flow reverses, edge-on does nothing | Blade/hub pose ← committed hinge angle; no cosmetic spin |
| Balloon (EL-208) | Dynamic body + buoyancy force region + sealed gas store | lift, volume, drag | Rises with its load until lift is exhausted or it is pierced | Skin scale ← committed volume |
| Air compressor (EL-037) | Electrical node + nozzle source into a sealed store | watt rating, stroke | Charges a connected reservoir while supplied | Piston stroke ← committed charge rate |
| Pneumatic hose (EL-038) | Gas network node pair | length | Carries inventory between seated ports only | None (static) |
| Air reservoir (EL-039) | Sealed gas store | capacity, maximum pressure | Stores a bounded charge; gauge shows it | Dial ← committed pressure |
| Pneumatic release valve (EL-040) | Gas network node + controller input | opening | Releases a burst after the source stops | Flap angle ← committed opening |
| Pneumatic directional valve (EL-041) | Gas network node + controller input | selected branch | Routes one supply to one branch | Blade position ← committed state |
| Air nozzle (EL-042) | Nozzle source | throat area, direction | Converts stored pressure into a jet that moves cargo | Exhaust puff ← committed regime |
| Pneumatic pressure gauge (EL-043) | Sensor on a gas node | On/Off thresholds | Needle reads pressure; switches a supplied load at threshold | Needle ← committed pressure |
| Pneumatic piston (component research P2) | Slider body + chamber + spring return | stroke, return stiffness | Extends against load while pressurised; returns on exhaust | Rod travel ← committed slider |
| Air whistle (EL-045), Wind chimes | Airflow receiver feeding the [acoustic family](component-research.md#sound) | threshold, release threshold | Sound only above the flow threshold; stationary chimes in mere fan overlap stay silent | See acoustic bindings |
| Gas expansion bladder (TH-24), Hot-air balloon (TH-25) | Sealed gas store + [heat family](thermal-component-research.md) | material, limits | Heated gas expands/lifts; cooling reverses | Skin scale ← committed volume |

## Chrome-observable acceptance for the first airflow slice

The first slice introducing this family is the existing Fan (CAT-028) under the [roadmap's airflow family](planning/invest/vertical-delivery.md#rolling-playable-roadmap). Through actual palette, gizmo and socket controls in Chrome/Playwright:

1. Place a supplied Fan beside the Basketball on a ledge, Run: the ball leaves the ledge along the Fan axis and reaches the Receiver (Solved).
2. Controls: Fan unsupplied, Fan facing away, a Wall between Fan and ball, and reach exceeded each leave the ball resting (no free energy).
3. Bellows → Windmill → Conveyor default transport: cargo starts at X = 1 and reaches X > 2.5 within 480 ticks; missed, blocked and disconnected controls retain X = 1. A stronger response-4, 960-tick configuration cannot substitute for this default case.
4. Loaded plate holding, closing gate/shutter, motor/pusher/conveyor and source-mode controls keep their truthful emission observations.
5. Reset restores the exact construction, reservoir, slider and rotor state; Save/Load where supported restores identical canonical bits.

Run/Pause/Step lifecycle, production build and ordinary resource ownership apply per slice; detailed performance and device qualification remain at their named release gates.
