# Springboard finite elastic plate

All numerical physics executes in WGSL f16 under the [canonical game-value contract](gpu-f16-physics.md). Authoring/admission uses typed Half quantities and explicit integer identities/scales; C# owns discrete transactions. These are required models and controls, not implementation or qualification claims. Apply the [delivery stages](delivery-workflow.md#stage-gates) and each named part's source acceptance.

EL-194 and PERF-24 require one rigid finite-mass plate on a frictionless local-Y slider attached to its construction frame, with a shared quadratic axial elastic potential, signed viscous damping and joint travel stops. It is distinct from the trampoline's separate massless contact patches. No target-velocity launch, impact-energy injection or catalogue-specific solver is permitted.

Units are metres, kilograms, seconds, newtons and joules. Plate dimensions are 1.3 × 0.15 × 1.2 m, mass 0.25 kg, with uniform-box inertia. Rest centre is local Y=0.14 m; travel is -0.25 through 0 m relative to rest. The base is fixed to its authored frame. Prescribed construction assistance can do external work, which must be included in the energy balance.

Parameters remain enum-typed: Stiffness 120–1200 N/m, Damping 0–8 N·s/m and InitialCompression 0–0.20 m; defaults are 400, 0.2 and 0. Canonical admission quantizes these declared values once. Initial compression is finite construction energy, not a latch, and releases when Run begins. A payload loads an uncharged plate through contact. There is no powered recharge or automatic contact launch. A compressed empty plate may move but cannot transfer work to a missed payload.

The potential is U=0.5*k*q²; damping opposes relative speed. Shared contact, frame reaction and stops determine the plate's motion. The part declares laws and reads committed results. The same actual pose controls collider and visible plate; the silver coil scales only as dependent artwork. Decorative recoil must not independently move the functional plate. Physical trajectory is independent of render cadence.

Acceptance includes total stored/kinetic/gravitational energy and external work within the approved f16 complete-Run budget; legitimate damping and inelastic contact/stops dissipate energy. A zero-gravity, zero-preload stationary assembly stays stationary; a missed ball receives no launch. Check contact/loading/rebound, tilted frames, all supported parameter endpoints, invalid parameters, rejection of the removed strength field, exact same-environment replay and canonical construction Reset/save.

Campaign teaching/reference content must use this finite elastic mechanism. Required proof includes independent analytical/energy controls, actual Chrome UI positive/control/integration/restoration, production build, lifecycle/performance and individual publication. No old velocity-launch behavior, tolerance or test result substitutes for these criteria.
