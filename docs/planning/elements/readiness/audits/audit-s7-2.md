# Planning audit, second pass: S7, Coverage admits a named-identity package

**Planning verdict: Scoped Pass.** No blocking finding remains. Scope of the pass: the planning contract only, re-read in full in the `spec-drafts` checkout at `8d1d68aa`. Nothing was built or run; every test named is planned, not executed.

Contract: `_bmad-output/implementation-artifacts/spec-s7-coverage-named-identity-admission.md` (4,973 words, within the 6,000 budget).

## First-pass blocking findings: all closed

- **B1, lost artwork closure check: closed.**
  - The new Always bullet (:31) and the Code Map section "Protections removed and what replaces them" (:128-:132) name the protection.
  - Task 4 (:165) adds `ReadResourceClosure` on every admitted path in the real-repository test, and the acceptance-to-test table (:246) carries the row.
  - The Never bullet on `CapabilityInputs.Prepare` (:40) is intact.
- **B2, Always bullet against the ledger-level refusals: closed.**
  - The bullet (:29) now allows a ledger-level refusal to name the ledger or quote the line.
  - N10a to N10f (:63-:68) each have their own row and message; N10b names the path and N10d to N10f quote the line.
- **B3, inexact grammar: closed.**
  - Both regular expressions are stated (:47-:48), and a malformed identity line is defined (:49) and refused (N10d).
  - T2 has eight rows, including `IdentityNotAlone` and `TrailingText` (:243); T1 covers trailing whitespace and CRLF (:242).
  - Checked against the real ledger with the stated expressions: 318 lines match the identity expression, 318 lines contain an identity anchor (so none is malformed), no line starts with `**Package:**`, and the file has no CR bytes. The ledger as the slice leaves it parses clean.
- **B4, unowned exclusions: closed.**
  - Task 9 (:170-:185) carries exact text for stories S7 and S8.
  - Task 10 (:187-:201) carries the S8 bullet for "Structural stories queued" (`deferred-work.md:264-268` lists S2 to S6 today) and two full `deferred-work.md` entries.
  - Exclusion (a) is owned by S8. Exclusion (b) is owned by the ledger's Completion gate paragraph, which exists at `named-elements.md:30`.
  - The contract states that S7 and S8 do not exist on `8d1d68aa` and are added in this slice's candidate commit (:117).

## First-pass non-blocking findings: all applied

- N1: the concurrency list now names S2, S4, Early level proof batch 1, the electrical-gates closure review and ball presets (:150-:157).
- N2: counts are stated as pinned at `baseline_revision` (:30, :204), and T5 compares the source count with and without the package (:247).
- N3: the header exception is in task 6 (:167); `named-elements.md:5` ends with the sentence it appends to.
- N4: the mode-or-variant case is in N9, decision 3 and the ledger paragraph.
- N5: the commit-path exposure is recorded (:132) and the ledger paragraph tells the author to stage the line and run the registration check.
- N6: maintainability index at least 20 is in task 2 (:163).
- N7: Design Notes 6 and 7 are short and cite the Code Map.
- N8: the missing production reader of `NamedIdentities` is recorded with its owner (:199, :220).

## Consistency checks on the repaired text

- **Test arithmetic:** T1 1, T2 8, T3 1, two theory rows, T5 1 gives 13; the focused run is 48 plus 13, 61; AC1 and the full-round line both say plus 13.
- **Matrix coverage:** every row N1 to N11 and AC1 to AC3 has a named test with a control and a boundary.
- **Refusal coverage:** unbound (N2), doubly bound (N3, N4), CAT-colliding (N5), stale (N6) and all six N10 cases have planned executed cases.
- **Self-triggering:** the paragraph added to the ledger starts with `**Package binding.**` and mentions the line form mid-line, so it is neither a package line nor a malformed one.
- **Handoff order:** builds, maintainability gate, registration check, focused table run, whitespace check, one full round, in the protocol's order.
- **Tier and shape:** engine tier and one ordinary structural slice are right.

## Remarks, not blocking

- **N10f third variant.** The row lists three malformed forms, and T2 tests two (`Malformed`, `TrailingText`). An id outside `[a-z0-9_]+`, such as `Soccer`, has no row. The same expression refuses it, so this is optional: add a `TrailingText`-style row `UppercaseId` and raise the totals to 14 and 62 if wanted.
- **`TODO.md`.** Task 10 says "record the slice outcome" without exact text. That is the coordinator's status record, so it is acceptable.
- **Exclusion (b).** Its owner is a gate, and the slice that makes the capability audit current is not yet a named story. That is acceptable now; it must become a named story when that gate is planned.
- **Status line (:75).** It still says "awaiting second audit"; record this verdict there.
- **Publication.** The slice shares files with S2, S4, Early level proof batch 1, the closure review and ball presets, so it publishes serially: the later publisher merges main once and repeats the full round.

## Decisions for the owner

None requires a halt. Decision 1 (bind through the ledger, mint no CAT number) stays a defaultable decision and now records its reversibility and its consequence for the owner's information (:136).

## Evidence limits

- Tests are planned, not executed; nothing was built.
- The E2eRunner sources were not opened in either pass.
- "About 229" identities was not measured.
- Whether S2 is published was not checked.
- The sibling drafts' shared-file claims were read on the first pass and not re-read.
