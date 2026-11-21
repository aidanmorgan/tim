# Element-free engine planning and acceptance gate

Owner instruction, 4 November 2026; effective immediately. Apply the [design requirements](element-free-engine-design.md) under the [sole design authority](../../docs/architecture/element-package-isolation.md#target-invariants). The design guide defines capability and element-specific assumption; this policy defines review scope, evidence and disposition.

## 1. Scope and refusal rule

Reject a proposed or implemented change that introduces, extends, reintroduces or relies on an element-specific assumption in its **affected scope**.

Affected scope means the changed behavior plus the declarations, validators, compilers, runtime paths, observations, bindings and callers required to deliver it. Trace actual dependencies; neither a filename nor shared assembly membership makes every behavior in that file/assembly affected.

Apply the [loop protocol's repair policy](bmad-loop-protocol.md#triage-re-review-and-completion), including confirmed pre-existing defects, controlled ownership and review-only limits.

Moving a special case between packages, compiler, validator, registry or runtime, registering it as a callback, or merely renaming it does not remove the violation. Use design requirements D1–D7 to decide, rather than a name blacklist.

This gate applies to behavioral planning and engine-affecting changes. A prose-only correction or review receipt needs relevant source/link/consistency checks, not a new engine implementation or runtime test round. A documentation change that changes required behavior is a planning change and must pass the planning gate. A status-only query reports existing status and blockers; it does not require a new design or test run and cannot upgrade approval.

## 2. Evidence required at each stage

| Stage | Required evidence | What a Pass means |
| --- | --- | --- |
| Planning: architecture/spec/story/readiness | Eight design answers, concrete acceptance cases, relevant code/contracts and resolved required-now design decisions/prerequisites. Identify planned tests separately from applicable existing proof. | The proposed design is sufficiently defined and complies; it does not certify implementation. |
| Implementation acceptance | Actual affected declaration-to-execution path, native production behavior and boundary/lifecycle proof, current schema/caller changes and deleted old path. Apply local full-test/hook requirements when required by the delivery workflow. | The reviewed implementation meets required-now acceptance in the stated scope. |
| Guidance/configuration or prose-only maintenance | Rule consistency, preservation, valid links/fields and resolved dispatch behavior where changed. | The documents/configuration are coherent in the inspected scope; future agent behavior and runtime compliance remain unproved. |

For runtime changes, test an unfamiliar declaration/composition using the supported primitive, relevant parameter/instance variation or reordering, a meaningful negative/control, unsupported-input refusal and applicable Run/Reset/save/load. Exercise the production compiler/simulation/animation paths that implement the changed behavior. Existing applicable proof may be reused after its inputs and contract are checked; do not rerun unrelated checks.

Passing tests for existing catalogue examples or a lexical guard alone does not establish abstraction. A missing required design decision or required-now proof is an evidence gap. Browser/performance/release proof remains at its named gate; do not turn deferred qualification into an early requirement.

## 3. Verdict and blocking action

| Verdict | Condition | Mandatory action |
| --- | --- | --- |
| **Fail** | Evidence confirms a design requirement is violated in affected scope. | Block that stage's approval. Triage the confirmed violation **high**. Correct a conflicting spec through bad_spec or the workflow's explicit spec-conflict route; otherwise return the fix to the implementation owner. Independently re-review affected fixes. |
| **Incomplete** | A required answer, dependency, instruction file or evidence is missing, stale or insufficient to decide. | Name the missing item and next check. Block approval until supplied; do not label an unconfirmed suspicion as a confirmed violation. |
| **Scoped Pass** | No unresolved violation or evidence gap remains for that stage and justified scope. | Record stage, scope, evidence identity and deferred gates. Do not imply whole-engine or release qualification. |

These verdicts describe evidence, not new workflow status enums. If a workflow exposes only PASS/CONCERNS/FAIL, map Incomplete to FAIL with reason “Incomplete”; if it exposes a blocked state, use blocked with that reason. Never map Incomplete to CONCERNS/proceed-anyway.

Fail and Incomplete both prohibit the affected plan's final/ready-for-dev approval, readiness PASS, SnapshotApproval, done status or publication approval, whichever belongs to the current stage. Draft/backlog artifacts may record unresolved work; recording it is not approving it.

The coordinator may reject a finding only with a recorded, evidence-backed refutation of its concrete defect claim. Initial scope, pre-existing origin and low severity are not rejection or deferral reasons. A confirmed in-scope violation cannot be downgraded, deferred or dismissed as cosmetic, pre-existing, a context-file issue or “the spec asked for it.” Raw lens formats may omit severity; coordinator triage must still apply high.

Workflow defaults cannot waive these rules: no optional-review/low-stakes bypass, CONCERNS/proceed-anyway decision, reviewer vote, oneshot completion shortcut or carried historical verdict. A prior verdict is reusable only if its evidence still applies and it meets current owner policy.

## 4. Duties and review record

**Coordinator**

1. Identify the stage and affected scope. Confirm the invoked BMAD workflow resolved its project customization, including the applicable guides, loop protocol and hooks/layers/templates. A resolver failure must follow that skill's documented recovery or halt path; falling back to installed defaults alone leaves this gate Incomplete. Build/Auto renderer failures must halt. Record verdicts in the native planning/validation/triage output, not a separate approval process.
2. Check the effective merged required layers and actual host no-history launch option under [the loop protocol](bmad-loop-protocol.md); empty/disabled required instructions or unavailable reviewer isolation block approval. Apply the protocol's verified blocked recovery and stagnation transitions to repairs. Apply the protocol to each handoff, including context-free and oneshot agents. Resolve paths before dispatch. Coordinators, implementers and informed engineering auditors load the complete applicable guides; specialist lenses receive their role-specific policy sections and input boundaries. Do not disclose claims to blind reviewers.
3. Run required independent review, collect every finding, verify/refute claims and apply section 3. Keep corrections with their implementation owner.
4. Record the terminal verdict and evidence limits. Do not mark approval states before required review and checks complete.

**Reviewer**

1. Read the actual artifact/code and the policy sections assigned to your role under the loop protocol. The informed engineering auditor reads the complete guides and authoritative contract/evidence; blind specialists preserve their input isolation. Trace direct and transitive behavior beyond changed lines as needed.
2. Preserve the assigned lens's purpose and output contract. Descriptive intent alignment stays descriptive; editorial lenses do not rewrite behavioral requirements. The dedicated Element-Free Engine Auditor evaluates the complete gate, and the coordinator owns verdict/triage. Where the lens returns defect findings, provide location, triggering scenario, violated requirement, consumer/consequence and a discriminating acceptance check in its prescribed fields; prescribe a repair only when that lens permits it.
3. Distinguish confirmed violations from missing evidence. Zero findings is valid; do not invent findings to meet a quota.
4. State what was inspected and what remains unqualified.

Use the existing review record; do not create a second ledger or duplicate evidence manifests. Documentation of the verdict does not create another recursive review task.

## 5. Enforcement limits

These are mandatory agent review instructions, not a proof-producing compiler or sandbox. Configuration checks can verify that the rules resolve and reach handoffs. Architecture guards can detect defined structural violations. Neither establishes semantic generality or guarantees a future reviewer follows the rules.

Independent source review and discriminating production tests supply the remaining evidence. Report which checks actually ran and their limits; never describe a prompt, lexical scan or configuration Pass as runtime compliance. Existing game-fidelity, forward-only, no-skip/no-hash/no-mutation and local verification rules continue to apply.
