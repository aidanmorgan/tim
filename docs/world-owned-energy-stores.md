# Finite energy stores and latched springs

Numerical state and work have one [WGSL f16 owner](gpu-f16-physics.md); authoring, saves and read models use typed canonical Half values. These are current model requirements.

## Reservoirs

Declare finite capacity and an explicit initial balance at construction; cannon and ordinary empty-store fixtures start at zero unless an authored preload is explicitly supported. Initial energy is separate from later accepted charging work. Install the complete membership batch atomically; duplicate/existing/foreign identities, invalid capacity/balance and unsupported reseeding reject. Different reservoirs have independent ledgers, and every source/consumer binds a typed identity before Run publication.

Charging accepts only explicit supplied work up to capacity. Release transfers only accepted physical work, preserving braking dissipation and unused charge. Invalid/reentrant mutations and failed release cannot partly debit the store or change a body. A missing reservoir during captured Run is an error, never a local fallback. Construction has no runtime reservoir.

Snapshots and rollback include membership, capacities, initial/stored energy, accepted/released totals and source associations alongside bodies, constraints, contacts and clock. Observations are immutable detached values. Removal retains required accepted-work history; restoring a snapshot restores the whole ledger. Shared consumers cannot each spend the same remaining balance.

## Latched wound spring

Declare stiffness, stroke, rest stop, head/guide and transmission identities before Run. The world owns compression, latch/trigger phase, release count and work. Use the same elastic potential, contact and acceleration laws as other bodies. Release atomically changes guide direction and transmission engagement; the rest stop relatches and constraints are solved coherently. No scene callback applies displacement or launch impulse.

AcceptedWork records positive elastic-potential increase while latched, including gravitational/contact recharge. ReleasedWork records signed potential reduction while releasing; supplied motor work is a separate quantity. Preserve signed stop residual energy within the declared numeric model, without clamps, discarded charge or a held-force accounting repair.

Reject duplicate ownership, changed installed laws, duplicate elastic loads and independent policy mutation. Snapshots restore spring/load membership and joint policy. Qualify rotated rest-stop binding, inward winding with shaft inertia, open/engaged release, 0.5 kg and 2 kg payload masses, no trigger, obstruction/resumption, recharge, exact Run/Reset/save and failed-step restoration.

## Controls

Prove empty/full/partial capacity, charge limits, unused work, stationary receivers, simultaneous consumers, removal/reinstallation, invalid identity/value, reentrancy and failure after prior success. Complete-Run accounting uses the declared f16 budget and independent physical work oracles; historical wider-precision residuals are not the target.
