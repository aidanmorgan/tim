# Finite energy stores and latched springs

Stores are the finite-store/source capability of the generic WASM SIMD128 f32 solver ([capability inventory](gpu-f16-physics.md#capability-inventory)): declared capacity, charge/discharge law and depletion, with no free energy ([envelope](gpu-f16-physics.md#game-grade-envelope)). Battery CAT-005, Cannon CAT-016, Wound spring CAT-071 and every paid element declare a store; shared allocation considers all consumers of one store together. Design owners S019/S020; delivered as ELEMENT-n roadmap slices.

## Reservoir declaration and behaviour

- Declared finite capacity and explicit initial balance at construction; Cannon and ordinary empty-store fixtures start at zero unless an authored preload is explicitly supported. Initial energy is distinct from later charging work.
- Every source and consumer binds a typed store identity before Run; duplicate/foreign identities, invalid capacity/balance and reseeding reject atomically at compile. Different reservoirs have independent ledgers. Construction has no runtime reservoir.
- Charging accepts only supplied work up to capacity. Release transfers only accepted work; braking dissipation and unused charge are preserved. Shared consumers cannot each spend the same remaining balance. A failed transfer debits neither side and moves no body.
- Committed observations are immutable values; Reset restores the construction's initial balance, never a runtime ledger.

## Latched wound spring (CAT-071)

- Declares stiffness, stroke, rest stop, head/guide and transmission identities. Compression, latch/trigger phase, release count and work are solver state using the same spring, contact and acceleration capabilities as other bodies.
- Release atomically changes guide direction and transmission engagement; the rest stop re-latches, and no callback applies displacement or a launch impulse.
- AcceptedWork records positive elastic-potential increase while latched (including gravitational/contact recharge); ReleasedWork records signed potential reduction while releasing; supplied motor winding work is a separate quantity. Stop residual energy follows the envelope's clamp-or-continue policy; no held-force accounting repair.

## Chrome-observable acceptance

- Empty/full/partial capacity, charge limits, unused work, a stationary receiver, two simultaneous consumers, removal/reinstallation and invalid identity/value rejection behave visibly and differently.
- Wound spring: rotated rest-stop binding, inward winding with shaft inertia, open/engaged release, 0.5 kg and 2 kg payloads, no trigger, obstruction then resumption, recharge.
- A paid element never fires unpaid; exact Reset and Save/Load after every Run; same construction and inputs give the same outcome.
