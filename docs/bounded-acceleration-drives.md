# Bounded acceleration drives

All numerical physics executes in WGSL f16 under the [canonical game-value contract](gpu-f16-physics.md). Authoring/admission uses typed Half quantities and explicit integer identities/scales; C# owns discrete transactions. These are required models and controls, not implementation or qualification claims. Apply the [delivery stages](delivery-workflow.md#stage-gates) and each named part's source acceptance.

A drive declares a typed identity, generalized velocity gradient J, convective acceleration J-dot v, target generalized acceleration and a finite signed effort interval containing zero. The coupled physical solve chooses effort within that interval together with ideal joint reactions and Coulomb contact forces. Interior effort meets J a + J-dot v = target within the admitted puzzle-scale error contract; saturation permits a target deficit in the corresponding direction.

A slider uses metres/second², newtons and its force Jacobian; a hinge uses radians/second² and newton-metres. Row scaling preserves work-conjugate effort. Dynamic participants receive J-transpose times effort, preserving equal/opposite reactions for physical pair rows. Prescribed participants provide their declared acceleration and receive no integrated motion; at least one participant must be dynamic.

Candidate evaluation cannot mutate committed bodies, caches or retained declarations. Results include total wrenches, per-drive effort, achieved generalized acceleration and enum-typed limit status in canonical identity order. Unsupported/foreign/duplicate IDs, nonfinite values, reversed effort bounds and bounds that exclude zero reject atomically. There is one coupled contact/constraint authority and no part/catalogue dispatch.

Acceptance: analytical loaded single-axis balance and saturation, two-body momentum, angular inertia, coupled joint/transmission response, obstruction and force reversal at contact, independent competing drives, no-drive controls, undefined/foreign/duplicate rejection, input nonmutation and deterministic same-environment replay.

Time advancement additionally requires stage-aware rebinding, target-speed/time and horizon contracts, source power/work limits, signed work and braking accounting, rollback and actual part UI/production proof. No instantaneous drive result authorizes unbudgeted force or closes endpoint/time-integration behavior.
