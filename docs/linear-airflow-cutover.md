# Conserved airflow and mechanical transfer

This current contract governs body, shaft and mixed recipients under [WGSL f16 authority](gpu-f16-physics.md). Use [rotor capture](rotor-airflow-contract.md), [finite stores](world-owned-energy-stores.md) and [finite gas](finite-gas-foundation.md) for their respective models. Geometry exposure alone is not a force or unlimited energy source.

## Sources, ports and allocation

Bind typed component source/receiver slots before Run publication independently of activation and declaration order. Point, slider, hinge and angular ports expose work-conjugate speed and reaction; composite ports combine translation/spin across all participants. An angular port names its reaction frame. Axis alignment scales speed and every corresponding reaction consistently.

One source allowance supplies every branch jointly. Declare force and independent watt limits, finite reservoir or physical source, material resistance, source participation and stable associations. Changing a rating preserves a binding; changing physical supply requires a new identity. Duplicate branches, conflicting sources, unknown/foreign identities and invalid numeric declarations reject atomically.

The fan's authored construction reservoir is 14,400 J, flow is 12 m/s, force rating is the authored force and watt rating is that force × 12. Bellows flow is −sliderSpeed × 15; its force rating is authoredForce and power rating is authoredForce × 12 W, constraining actual compression and paired reactions. Receiver conductance is Force/12 scaled by sample weight and pressure. Refill damping is 4 N·s/m, with no duplicate compression damping. All quantities require current Half range/scale admission. Disabled source plates transfer zero. No duplicate compression damper, full-force-per-receiver allocation, raw authored-strength callback or hidden ambient supply is allowed.

These named source settings are authored typed coefficients, reservoir values and port bindings for reusable WGSL f16 source/transfer laws. The fan/bellows/receiver names identify consumers and required default cases, never runtime equation dispatch. Declare the speed gain, power/force ratio, conductance scaling and damping with their units; evaluate their physical relationships in WGSL, not a part callback or C# precomputed physical correction. Preserve every value and control above.

Allocate shared force/power and stored balance without mutating predictions. Preserve finite inertia, load-dependent stall/release, engagement bounds including zero demand, signed flow and passive resistance. Simultaneous sources sharing a reservoir cannot overdraw it; near depletion, recharge, stationary flow and unused work remain explicit. Rounding cannot create energy, discard a nonzero admitted transaction or omit a branch.

## Continuous boundaries

Exposure uses current owned geometry and continuous inlet/outlet/rim, moving blocker, moving streamline and compound-union boundaries. Qualify grazing/tangent and hidden interior crossings. Source/slip/saturation, cut-in/target, engagement and ratio changes are certified on the full captured path, including full turns, opposing sources and near-zero departure/return. Reject exhausted bounds explicitly; do not drop elapsed time or defer contact correctness.

Paired nozzle/receiver reactions preserve linear and angular momentum. The load-dependent balanced transfer abstraction has zero ambient exchange and no unloaded thrust. True free-stream transport and unloaded bellows emission instead require finite gas/nozzle mass, enthalpy and momentum accounting.

## Committed accounting and controls

Report source extraction, receiver delivery, paired loss, generalized impulse, projected body linear/angular impulse and numeric error separately. Stationary receivers may accept impulse with zero receiver work. Commit only accepted intervals; discarded predictions debit nothing. Keep last-step/cumulative branch and source totals, associations and removal history, with immutable reads and exact snapshots/rollback.

Bound actual-minus-evaluated work per body without cross-body cancellation. Allocate the strictest whole-step allowance across all branches/intervals; include positive paired work, both residual signs and integration/rounding/projection error without cancelling against dissipation. The complete Run also satisfies the global f16 budget. Constraint reactions and material work remain coupled to actual accepted motion.

Qualify one/two/mixed body and rotor receivers, three/seven/eleven-way sharing, reversed order, rotations, partial/full/blocked/edge/opposing exposure, positive/negative/near-zero flow, stored/mechanical supply, depletion/recharge, collision/miss cuts, failure after prior success, reentrancy, invalid declarations, exact store/body/ledger replay and Run/Reset/save. Chime and bellows controls include disabled plates, missed/blocked/disconnected paths, refill and loaded plate holding.

The authored default bellows→rotor→conveyor transport starts cargo at X=1 and must reach X>2.5 by 480 ticks; missed, blocked and disconnected controls retain X=1. A stronger response-4, 960-tick configuration cannot substitute for this default case. Preserve loaded/closing gate/shutter, motor/pusher/conveyor and source-mode controls, truthful emission observations and physical-device/performance qualification. Current status belongs in TODO, not this model.
