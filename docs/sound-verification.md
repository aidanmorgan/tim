# Sound foundation and speaker

27 September 2026. First sound component, not a completed acoustic puzzle family.

Speaker requires independent electricity and activation. Requests coalesce within one tick, settle on the next tick after electrical supply is solved, and expire without power. A 24-tick minimum interval limits retriggering to five per second. Immutable pulses preserve emission position/direction/tone/tick, travel at 12 game units/sec, reach 8 units, and last 0.15 sec at a sample location within a 35-degree cone. Strength attenuates with distance. These are authored game rules, not a physical sound-speed claim. At most five pulses remain in flight per speaker.

The free-field sampler does not yet account for walls, receivers, ducts or acoustic feedback. A sound meter is next; no completed sound-to-activation puzzle is claimed.

Procedural mono 16-bit PCM at 22050 Hz supplies a short 440 Hz default tone (typed Low/Mid/High model supports 220/440/880). AudioStreamPlayer3D presents it; simulation never reads playback state. Each real pulse also drives cone vibration and a thin expanding/fading ring. Rings are cosmetic, not collision or reception geometry.

## Evidence

635 native tests pass, including eight new acoustic cases covering tone preservation, travel/range/cone/duration, rotation, invalid inputs, source order, next-tick clock triggers, complete world Reset, unpowered request expiry, duplicate/retrigger bounds and removal of expired pulses. The native audio test checks PCM configuration/nonzero samples and proves stopped/muted playback does not remove gameplay pulses. 39 browser-driver tests pass.

UI-only artifacts retained locally under docs/playtest-results:
- speaker-powered-v1: level-selector timeout before construction; no Run or Reset.
- speaker-powered-v2: successful construction/Run/Reset, but inspected visual was an over-heavy gold hoop.
- speaker-powered-v3: thin translucent wavefront visible; exact Run/Reset restoration, no reported browser errors.
- speaker-unpowered-v1: same clock trigger, no battery-to-speaker wire; no visible wavefront, exact Run/Reset restoration and no reported browser errors.

Holding/outcome screenshots are in .playwright-mcp with case-name prefixes. Screenshot evidence is visual only; it does not verify audible output quality, continuous animation or mobile contrast. Those remain explicit TODOs along with browser audio suspension, actual meter reception/occlusion and campaign lessons. No microphone input is used.

Anvil graph checks returned ready without inferred affected tests. Its pre-write gate was unavailable due to authentication, allow-with-warning; no passed gate is claimed.
