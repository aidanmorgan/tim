# Project rules

- As each feature batch is implemented and verified, commit and push it. Keep unfinished requirements and failed verification evidence explicit; do not wait for the entire 75-level goal to finish before publishing verified progress.
- Always refactor forward. Never add fallback paths, backwards compatibility, legacy modes, compatibility shims, old-name aliases or automatic format migration.
- Update current implementations, callers, authored content, tooling and tests together. Reject unsupported inputs explicitly; do not silently substitute another implementation, infer obsolete fields or downgrade behaviour.
- Preserve historical playtest artifacts unchanged as evidence, not as supported current input.
- Use C# wherever possible. Use enums for closed sets and typed identities or centralized constants for extensible names; avoid magic-string protocols.
- Preserve the current colour scheme. Follow DESIGN.md for Monument Valley-inspired form, composition, lighting and motion without replacing the approved palette.
- The current campaign target is 75 progressively taught levels. Track actual implementation and verification status in TODO.md; do not equate passing reference solutions with completed repeated difficulty testing.
- Browser playtests must use real UI actions, not game-state setters, imported solutions or numeric placement menus. Retain failed attempts.
