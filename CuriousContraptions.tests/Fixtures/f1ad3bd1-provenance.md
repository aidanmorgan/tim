# f1ad3bd1 baseline fixtures (Story 9A.44)

Captured once, natively, on the half-precision-era engine at baseline commit `f1ad3bd19e27c32cb05ba34f83ccc1d6cf59fc76`, before any
Story 9A.44 edit, by a scratch capture test run in a detached worktree of that commit (its source is kept as review evidence in
`_bmad-output/implementation-artifacts/evidence-9a-44/t0/T0OutcomeCapture.cs.txt`; it is not part of this tree).

- `outcomes-f1ad3bd1.json`: for every authored lesson at precisions 0, 0.45 and 1 (1200 ticks at the default 120 Hz cadence) and
  every element package placed alone (240 ticks): goal met, goal tick, every capture and activation event with its tick, and the
  final body positions. Read by `F32BehaviourIdentityTests`.
- `save-f1ad3bd1-authored-puzzle.bin`: the baseline save codec's encoding of the `first_principles` authored solution at precision
  0.45 (its assistance as 198 bytes of half-precision knots). Read by `F32SaveBoundaryTests` (the typed legacy refusal).
- `save-f1ad3bd1-free.bin`: the baseline encoding of a Free bench, a Basketball at (0, 3, 0) and a Ramp at (1, 1, 0), next identity
  3. Read by `F32SaveBoundaryTests` (a Free save still loads as its exact construction).

These are fixed inputs, not regenerable: the half-precision engine they record was deleted by Story 9A.44, so no current build can
reproduce them. They are never pinned by hash.
