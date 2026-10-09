# Springboard (CAT-062 · EL-194)

The Springboard is declaration data over the generic capabilities of the single WASM SIMD128 f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory)); the roadmap slice [CAT-062](planning/invest/vertical-delivery.md#rolling-playable-roadmap) adds the generic elastic spring constraint it first needs (also serving Trampoline CAT-065) and deletes parts/SpringPart.cs physics. PERF-24 applies: no catalogue-specific solver, no target-velocity launch and no impact-energy injection. The Trampoline's massless contact patches are a different declaration.

## Declaration data

| Record | Values |
| --- | --- |
| Plate body | Box 1.3 × 0.15 × 1.2 m, mass 0.25 kg, uniform-box inertia; its contact material |
| Base | Static body fixed to the authored construction frame |
| Slider constraint | Frictionless along the plate's local Y; rest centre at local Y = 0.14 m; travel stops at −0.25 m and 0 m relative to rest |
| Spring constraint | Box2D v3 TGS Soft formulation with stiffness $k$ and damping $\zeta$, potential $U = \frac{1}{2} k q^2$ |
| Parameters (enum-typed choices, canonical IEEE-754 f32, quantized once on entry) | Stiffness 120–1200 N/m (default 400); Damping 0–8 N·s/m (default 0.2); InitialCompression 0–0.20 m (default 0) |
| Animation binding | The silver coil scales with committed compression; the plate's art uses the physical pose |

Units are SI (m, kg, s, N, J). The removed strength field and any out-of-range or non-finite parameter reject at compile.

## Behaviour for the player

- Initial compression is stored construction energy, not a latch; it releases when Run begins.
- A payload loads an uncharged plate through contact and is returned its stored energy on rebound; damping and inelastic contact/stops dissipate energy. No powered recharge and no automatic launch.
- A compressed empty plate may move but cannot transfer work to a payload that misses it.
- The collider and the visible plate share one pose; decorative recoil never moves the functional plate. Authored construction assistance may do external work and is part of the energy balance.
- No free energy ([envelope](gpu-f32-physics.md#game-grade-envelope)): a zero-gravity, zero-preload stationary assembly stays stationary; a second bounce apex is lower than the first.

## Chrome-observable acceptance (CAT-062)

- `spring_forward` is solved through the UI; campaign teaching and reference content, including all fifteen composite springboard lane instances, use this mechanism.
- Controls: an unloaded board does not launch; a missed ball receives no launch; a tilted frame still works along its local axis.
- Every supported parameter endpoint is editable through the UI; invalid parameters and the removed strength field reject visibly.
- Exact Reset (including authored precompression) and Save/Load after every Run; same construction and inputs give the same outcome.
- Reviewer grep for `SpringPart` or element-specific branches in the physics solver finds nothing.
