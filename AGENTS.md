# Project rules

- Current priority: implement the outstanding puzzle-component families first. Defer exhaustive difficulty/nudging sweeps and the full campaign playtest matrix until the component set is implemented. Continue focused correctness, build, Reset and UI smoke checks for each individual puzzle element; preserve all existing difficulty evidence. Expand/rework the 75-level campaign to teach the components, without letting difficulty testing delay component coverage.

- Goal: implement all different puzzle elements tracked in TODO.md and its linked component research, not merely one representative of each family. Track each element separately through implementation, sufficient verification, commit and push; do not silently drop or defer an element out of scope.
- For every new puzzle element, prove intended behaviour, relevant failure cases, integration with other mechanisms, Reset/save restoration, a production build and real-UI Playwright interaction. Record reproducible evidence and remaining limitations; compilation or a reference solution alone is not proof of completion.
- Commit and push each individually verified new element before moving on to the next. Keep unfinished requirements and failed verification evidence explicit; do not batch several new elements into a later commit or wait for the entire 75-level goal to finish before publishing verified progress.
- Always refactor forward. Never add fallback paths, backwards compatibility, legacy modes, compatibility shims, old-name aliases or automatic format migration.
- Update current implementations, callers, authored content, tooling and tests together. Reject unsupported inputs explicitly; do not silently substitute another implementation, infer obsolete fields or downgrade behaviour.
- Preserve historical playtest artifacts unchanged as evidence, not as supported current input.
- Use C# wherever possible. Use enums for closed sets and typed identities or centralized constants for extensible names; avoid magic-string protocols.
- Preserve the current colour scheme. Follow DESIGN.md for Monument Valley-inspired form, composition, lighting and motion without replacing the approved palette.
- The current campaign target is 75 progressively taught levels. Track actual implementation and verification status in TODO.md; do not equate passing reference solutions with completed repeated difficulty testing.
- Browser playtests must use real UI actions, not game-state setters, imported solutions or numeric placement menus. Retain failed attempts.
