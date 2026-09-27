# Sound foundation and speaker

27 September 2026. First sound component, not a completed acoustic puzzle family.

Speaker requires independent electricity and activation. Requests coalesce within one tick, settle on the next tick after electrical supply is solved, and expire without power. A 24-tick minimum interval limits retriggering to five per second. Immutable pulses preserve emission position/direction/tone/tick, travel at 12 game units/sec, reach 8 units, and last 0.15 sec at a sample location within a 35-degree cone. Strength attenuates with distance. These are authored game rules, not a physical sound-speed claim. At most five pulses remain in flight per speaker.

At the speaker-only checkpoint, the free-field sampler had no wall or receiver integration. The sound-meter follow-up below adds direct-path reception; ducts and richer acoustic materials remain pending.

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

## Sound meter follow-up

The sound meter and AcousticNetwork now sample the strongest direct pulse at each meter before the electrical solve. All readings are captured before commit. Direct reception uses the existing opaque collision trace, ignoring source/receiver bodies; this is an explicitly simplified game rule, not diffraction or a full acoustic material model. Cosmetic rings and audible playback are not clipped by this trace.

A needle displays received level. Above-threshold contact passes independently supplied electricity. A rising threshold crossing emits one activation while powered; a 90% off threshold rearms it. An unpowered crossing is discarded, not deferred to power restoration. Authored thresholds must be finite in [0.05,1]. Current meters accept all tone bands.

Seven new native cases cover actual speaker arrival, reversed source/receiver ordering, wall blockage, backwards emission, absent meter supply, repeat/rearm, downstream counter count, continuous supplied contact/retraction, world Reset, strongest-not-summed arrivals, hidden meter clearing and threshold/invalid-value checks. The first synthetic-source test failed because its source was at y=0 and the meter defaulted to y=1; corrected the fixture without changing the model.

UI-only evidence retained locally:
- sound-meter-relay-v1: speaker → meter → hold timer → powered gate; inspected outcome shows the gate open.
- sound-meter-blocked-v1: same assembly with an intervening wall; inspected holding image shows idle meter/timer and closed gate despite the visible emitted wave.
- Both have no reported browser errors, one Run/Reset and exact construction restoration. Screenshots do not prove continuous needle motion or audio quality.

This checkpoint completed the first sound-to-mechanism chain, not the entire sound family. The bell is covered below; campaign teaching, chimes, tuned meters, ducts, audio occlusion and mobile verification remain pending.

## Impact bell

The `bell` scene/catalog entry requires a real dynamic-body collision of at least 0.8 units/sec. Contact stays disarmed until that body separates by 0.04 units beyond the summed collision radii. A 24-tick minimum interval limits emission to five pulses/sec; simultaneous qualifying hits use the strongest impact, independent of body order. There is no electrical or activation input and no bumper impulse. Ordinary sphere collision supplies rebound. The fixed collision envelope is a 0.75-unit sphere; decorative rocking never moves that envelope.

Loudness is clamp(mass × normal impact speed / 6, 0.05, 1). Bell pulses use the new typed Omnidirectional pattern; speakers explicitly use Cone. Pattern and strength are mandatory constructor arguments; all callers were forward-refactored, without a compatibility overload. Range, travel speed, lifetime and direct-path blocking use the shared acoustic rules above.

A shared typed acoustic voice generator supplies a 0.7-second decaying bell tone with inharmonic partials, distinct from the speaker's 0.15-second tone. Audio playback remains presentation-only. An exact damped oscillator preserves pose continuity during retriggers and decays even when simulation stops. Three pooled, thin great-circle rings per in-flight pulse show omnidirectional propagation.

### Native verification

`dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: **661 passing tests**, including **19 new bell cases**. These cover real six-axis impacts and unchanged rebound energy, omnidirectional attenuation, actual ball→bell→meter→timer→gate integration, source order, blocked path, absent ball, absent meter supply, exact world Reset and JSON construction replay, quiet resting contact, geometric rearm, gentle impacts, mass-dependent loudness, bounded rapid strikes, simultaneous-hit coalescing, PCM samples, invalid typed modes/strength/voice, render settling and unchanged physics during presentation. Existing speaker and sound-meter tests pass after the shared pulse/audio refactor.

`node --test tools/Playtest/direct-ui.test.cjs`: **39 passing tests**.

Development failures retained here: the first bell test compile used nonexistent MachineCodec.Read/Write helpers; replaced those test calls with the existing source-generated JSON serializer. Five integration cases then rejected an ambiguous meter-to-timer connection; explicitly selecting activation sockets corrected the fixtures without changing gameplay.

Production web verification: `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet` passed. `git diff --check` passed. Anvil's pre-write gate remained authentication-unavailable (allow-with-warning); no passed gate is claimed.

### Real-UI browser verification

Both cases used local Chrome through Playwright, only toolbox clicks, 3D handles, wire choices, Run and Reset. Read-only diagnostics captured construction; no imported solutions, storage edits, game-state setters or numeric placement menus were used.

- `bell-relay-v1`: inspected motion frames show the ball rebounding and thin waves expanding around the bell; the holding frame shows the timer active and the gate open.
- `bell-miss-v1`: ball offset two units in depth; inspected frames show it falling past the bell, no wave, idle meter/timer and closed gate.
- Both attempts report zero browser errors, one Run/Reset and byte-identical construction restoration. Records are retained locally at `docs/playtest-results/<case>.json`; three motion frames plus holding/outcome screenshots are under `.playwright-mcp/<case>-*.png`.

Reproduction recipe: free workshop (currently selector row 59), Balanced; bell (-3,6,0), basketball (-3,9,0), sound meter (1,6,0), hold timer (1,3,2), powered gate (4,3,0), battery (-4,2,3). Rotate the gate 90° around Z. Wire meter activation→timer activation, battery supply→meter power, battery supply→timer power and timer supply→gate power. Run, observe, then Reset. For the negative control, change only the ball's starting Z to 2 using its move handle. Coordinates describe the intended layout, not permission to use numeric placement; retained records contain the actual UI-created positions.

Limitations: these are focused component proofs, not campaign teaching or the full repeated difficulty matrix. Wavefronts and audio still do not clip at walls, although meter reception does. PCM and simulation independence are tested, but no human listening-quality verdict or browser audio-suspension/mobile review is claimed. Sparse browser motion frames do not establish every-frame smoothness or performance on all devices.
