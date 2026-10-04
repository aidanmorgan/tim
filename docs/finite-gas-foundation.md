# Finite gas material and coupling

All numerical physics executes in WGSL f16 under the [canonical game-value contract](gpu-f16-physics.md). Authoring/admission uses typed Half quantities and explicit integer identities/scales; C# owns discrete transactions. These are required models and controls, not implementation or qualification claims. Apply the [delivery stages](delivery-workflow.md#stage-gates) and each named part's source acceptance.

The sealed-material model is a homogeneous, single-species calorically perfect ideal gas with constant positive specific gas constant R and specific heat Cv. Material declarations supply inclusive temperature limits, maximum density and maximum absolute pressure. This is a limited law; unsupported states reject. Infer neither a default material nor an ambient state.

State is mass m (kg), volume V (m³) and internal energy U (J), with reference U=m Cv T. Derived temperature T=U/(m Cv), density=m/V and absolute pressure p=(m/V) R T have no independent setters. Cp=Cv+R. A reversible adiabatic volume proposal uses U₂=U₁(V₁/V₂)^(R/Cv), unchanged mass, and work on the gas U₂−U₁; expansion returns negative work. Freeze finite typed scales and operand/intermediate bounds before admitting each calculation.

All admitted inputs and derived quantities are finite. Mass, volume, energy and constitutive scalars are strictly positive. Vacuum/zero inventory is unsupported by this sealed law and must reject rather than become ambient air. A nonzero admitted volume change cannot claim free compression because its work rounded away; unsupported numeric resolution rejects. Overflow/underflow and invalid envelopes reject before mutation; no silent clamp.

The material proposal contains before/after state and signed work as one owned result. Higher-level callers cannot substitute a different work amount. Immutable construction state supports exact local restoration; reversed numerical integration is not Reset. Same canonical inputs reproduce the admitted result within the same qualified environment.

Acceptance independently checks pressure, temperature and signed work for compression, expansion and no motion; compares pressure-volume integration and split/whole paths across two materials using the approved f16 error budget; verifies original-state retention/repeated evaluation; rejects invalid material/state/range inputs and unsupported resolution.

World coupling uses typed gas-node/species identities, generation/revision validation and owned rollback. Pressure work and mechanical response commit atomically: gas energy gain debits mechanical work, expansion credits that same work, and simultaneous consumers share inventory. A proposal alone cannot drive a part.

Finite mass/enthalpy transport, typed supply/exhaust, nozzle momentum/energy, ambient reaction, mixtures, vacuum, heat/phase behavior and valves remain separate required source models with declared applicability; this sealed law does not claim them. Bellows must emit unloaded flow and refill from its declared environment. Receiver impulse alone cannot substitute for unloaded emission. No part script contains equations or catalogue-specific solver branches. Prove publication, exact Reset/save, actual UI, builds, lifecycle/resources and affected performance for each consuming mechanism.

## Chamber and nozzle transfer

A signed-area chamber binds permanent geometry to committed inventory at each Step; pressure at zero travel and finite-interval effort use the same gas potential. Mechanical/gas work, collision-cut reevaluation, carrier rotation and endpoint corrections commit together. Qualify both chamber orientations, rotated nonspinning and transversely spinning carriers, disconnected controls, subdivision and failure after accepted substeps. Bound residual, projection and cross-domain work over the complete step/Run without dropping elapsed time.

A converging nozzle declares no-flow, subsonic and choked regimes with mass continuity, stagnation enthalpy, momentum and pressure thrust. Finite adiabatic discharge cools a rigid reservoir and accounts for final mass/internal energy and outlet enthalpy. Integrate changing-inventory elapsed time, jet momentum, pressure impulse and kinetic/thermal enthalpy over the requested duration within declared inventory/error/work budgets. Qualify independent choked/subsonic/equalization references, choking transitions, other materials, partition consistency and no-flow/time/budget controls.

Accepted finite-duration transport updates inventory, dynamic mass/inertia, same-step pressure work and all receiver/nozzle/environment reactions atomically. Multiple consumers share finite inventory. Immutable pure proposals alone are not world transfer; reject unsupported states explicitly. Bellows unloaded emission/refill and truthful committed observations require this complete boundary rather than inferring output from receiver work.
