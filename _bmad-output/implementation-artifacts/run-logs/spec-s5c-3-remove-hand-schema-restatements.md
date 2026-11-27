# Run log: S5c-3 remove hand schema restatements

Append-only. Contract: `_bmad-output/implementation-artifacts/spec-s5c-3-remove-hand-schema-restatements.md`.

## Handled owner messages

- 26 November 2026, top-level coordinator rulings on S5c audit 6 (recorded in the S5c-1 run log, handled messages and E4): S5c is split into S5c-1, S5c-2 and S5c-3, whole rows per slice; S5c-3 is a draft, re-audited at its own dispatch from the main that contains S5c-2; its rows are S11 and S14, with the hand restatements H-T1 to H-T3 not handled by S5c-1, H-S1 to H-S6, `WireEnumMirrorTests` and A6-8's S11 closed list.

## E1 Planning: created by the split (26 November 2026, planner for lane 2)

- Source: `spec-s5c-one-wire-schema-file.md` as it stood at the dispatch re-audit (9,841 words), audits `audit-s5c-1.md` to `audit-s5c-6.md`, and the coordinator ruling of 26 November 2026. Citations were taken from audit 6's correction table (A6-11), read at `39aaba70`; S5c-1 and S5c-2 move some, so the dispatch re-audit re-reads each one.
- Carried here: rows S11 and S14; inventory rows H-T2, H-T3 (with the historical-region markers for H-T3 and H-T4), H-S1, H-S2, H-S4, H-S5 and H-S6; the full inventory table, including the rows owned elsewhere (H-R1, H-W3, H-W4, H-W6, H-W7), because AC3 closes them here; A6-7 (the suites' value form; BH3-4's `assistance` fixture); A6-8's S11 bullet; A6-11.
- Placed elsewhere by the dependency test (S5c-1 run log E4): H-T1, H-T4's walk and comment, A6-3 (k) (`WorkshopFormatTests.cs:180`) and the identifier lines of `OldLayoutSaves.cs:45` and `CompoundMassAndRollingTests.cs:896`, `:900` go to S5c-1, because S5c-1's deletions break them; H-S3 goes to S5c-2 (its run log E1). Only their region markers and remaining literal slots stay here.

### Moved bulk: snapshot design (D11)

- Values are compared with `Object.is`, so -0 still differs from 0 and NaN equals NaN. The raw-bit NaN-payload distinction is dropped, because Run/Reset restoration is judged on decoded values (AGENTS.md game fidelity) and no gameplay outcome depends on a NaN payload. Save prevalidation refuses non-finite values (`engine/gpu/WorkshopSavePrevalidation.cs:104`, `:169`, `:189-192`).
- Audit 6 evidence for A6-7: the snapshot reports values as strings, an f32 as its bit pattern (`tools/e2e/run-reset-snapshot.ts:210`), and the suites assert them: `cat-050-joined.test.ts:294` `'0x40666666'`, `:188` `'1'` for a boolean, and `cat-020.test.ts:105-107` (`===` against a string). BH3-4 (`deferred-work.md:310`) names a second cause: the test's fixture never sets the required `PuzzleRecord.assistance`.

### Moved bulk: S11 scanner (D12)

- Audit 6 (A6-8): S5b's two text scanners reopened for many passes because each repair widened what they accepted; S11's accepted forms are therefore a closed list (the matrix row), and a new restatement under a new name is review's to catch.
- History: A2-8 put comments in S11; A3-2 bounded the allow-list to regions; A3-7 stated S11's scanned roots.

### Word count

- Contract at creation: see the S5c-1 run log E4 (`wc -w`).
