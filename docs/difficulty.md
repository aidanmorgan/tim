# Authoring forgiving puzzles

## Required target and execution ownership

Complete the engine difficulty implementation before **P0-035** in the [authoritative TODO sequence](delivery-workflow.md#pipeline-priority). Authoring and product expansion follow that gate. Difficulty uses enum-typed Forgiving, Balanced and Precise profiles in the simulation worker at fixed 120 Hz, initially four outer substeps. [Canonical Half game values and WGSL f16 numerical authority](gpu-f16-physics.md) apply to authored thresholds, assistance and physical effects; C# owns typed admission and discrete policy, with no CPU physical fallback. The profile is frozen for a Run; changes require an explicit Reset/restart and current-schema save/replay identity.

Enumerate every supported physical effect and its policy in all three profiles before implementing it. Forgiving may disable or simplify declared secondary effects; Balanced uses the declared intermediate model; Precise uses the most realistic supported model. All preserve physical containment, required connections, finite-resource accounting, the taught mechanism and actual goal success. Distinguish intentional simplification from an unimplemented effect. No alternate solver, silent downgrade, backwards-compatibility shim or automatic save migration is allowed.

Placement assistance and effect policies change authoritative simulation state only through typed owned transactions. The separate C# animation worker (initially 60 Hz) and browser-main-thread display loop cannot change profile behavior, outcomes or collision geometry. Cosmetic UI motion may continue while simulation is paused. Prove the law/profile differences with independent controls, exact replay/Run–Reset/save and real-UI generic fixtures before engine closure; qualify every later element and each of the 150 lessons separately.

The [60 FPS baseline/90 FPS qualified-device budgets](planning/requirements.md#worker-performance-budgets) apply to every supported profile. Do not change the fixed simulation clock, silently skip work or fabricate goal success to meet them. Forward-update current models, callers, content, tools and tests together and delete superseded policy paths.

## Placement-assistance behavior to preserve

Assistance is automatic engine behavior controlled by the level author for each part instance. It is not a player button, hint action, hidden whole-machine replacement or requirement to reproduce the reference solution. Fixed parts retain receiver/trigger rules; eligible movable static parts may have declared placement targets and bounded correction envelopes. Fixed and dynamic parts are not repositioned by placement assistance.

Author curves declare their interpolation knots, position/rotation windows, maximum corrections and blend duration as canonical Half game values. Missing curves provide no placement correction or receiver guide. Match same-kind authored slots one-to-one by proximity/orientation, reserve exact placements, leave out-of-window placements alone and admit valid alternative solutions according to actual goal events. This preserves the existing behavior contract; current resource codecs must follow the GPU/f16 design rather than accepting the archived wide-value JSON format.

Calculate corrections once from the player's initial arrangement. They must not accumulate each tick or across Reset. Over the authored blend interval (the retained minimum is 0.1 seconds), quintic easing and short-arc quaternion interpolation move the part and collider together. No edit-mode snap, ball teleport or hidden visible/contact offset is allowed. Precise placement correction remains zero; Reset reconstructs the original admitted construction exactly.

The existing source fields retain distinct semantic roles: position_window and rotation_window define eligibility; max_position_correction and max_rotation_correction independently bound corrections; blend_seconds controls duration; guide_acceleration bounds lateral receiver guidance during descent; capture_margin, capture_speed and capture_dwell define acceptance and residence; trigger_threshold governs impact activation. Distances, angles and durations retain their declared units and canonical typed scales. A profile cannot silently alter gravity/timestep or invent supplied energy; an explicitly declared surface-friction policy is separate from placement curves.

## Qualification and authoring

For each admitted profile/fixture, freeze expected positive, negative/control and boundary behavior before testing. Preserve smooth bounded correction, strict/out-of-window controls, Reset without drift, unique slot assignment, angle wraparound, interpolation and successful-placement sweeps. Prove alternative solutions, source-specific physics, assistance monotonicity where required, real-UI construction and visible motion at their enforcing stages. Existing difficulty evidence remains evidence of its original source and environment; it does not close the future full matrix.

Every current authoring tool and codec must implement this contract in its consuming slice, with explicit current-format admission and no automatic migration. Complete component coverage before exhaustive balancing across the 150 progressively taught lessons.
