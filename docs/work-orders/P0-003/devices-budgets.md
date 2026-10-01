# Devices, budgets and failure policy

These are frozen engineering targets, not observed device qualifications. Physical access is unavailable except the development Mac. An unavailable machine keeps its gate Incomplete. No purchase, remote access bypass, emulator substitution or invented observed version is authorized.

## Device matrix

| ID / support class | Frozen physical target | Required browser / cadence | Concrete missing prerequisite |
| --- | --- | --- | --- |
| DEV-MAC | Existing MacBook Pro M4 Max,64GB; retained macOS26.5.2 / Chrome154.0.8037.58 observation | Chrome through Playwright;60 plus90 tier on observed >=90Hz display | Re-read OS/browser/version/power/display mode, trace presentation identity and capabilities at each accepting capture. Current Chrome connection exists. |
| LAP-WIN | ThinkPad T14 Gen5 AMD, Ryzen5 PRO8540U/Radeon740M,16GB,1920×1200 60Hz panel; Windows11 | Chrome,Edge,Firefox;60 | Physical matching SKU/panel access; actual OS/browser binary/version and driver identities; actual presentation/CPU/GPU capture. Proposed configuration, not acquired hardware. |
| LAP-LINUX | Same specified ThinkPad hardware, Ubuntu24.04 LTS x86_64 | Chrome,Edge,Firefox;60 | Physical access and successful supported installation; freeze exact kernel/browser/driver binaries before capture, no emulation. |
| LAP-MAC | MacBook Air M2 (2022),8-core CPU/8-core GPU,8GB,256GB,built-in display; macOS | Chrome,Edge,Firefox,Safari;60 | Physical machine, exact OS/browser build and capabilities before capture. M4Max does not substitute. |
| ANDROID-MID | Owner-selected Pixel8Pro,12GB,built-in display | Physical Android Chrome;60 and90 on verified >=90Hz mode | Physical device/USB or authorized connector access, actual OS/browser/power/display identity. Desktop emulation cannot close ENGINE-DEVICE-01. |
| ANDROID-LOW | Pixel6a,6GB,built-in60Hz display | Physical Android Chrome;60 | Physical device access and actual OS/browser identity. This lower-resource tier is distinct from Pixel8Pro; not a claim about market pricing. |
| IOS | iPhone13,128GB,built-in display | Actual physical iOS Safari;60 | Physical device plus authorized capture/export of actual Safari behavior and presentation evidence. Record available memory identity rather than invent RAM. Playwright WebKit supplemental only. |

Model/specification choices are target constraints; obtain and verify the actual manufacturer's model/SKU readback before measurement. OS/browser versions not currently observed are explicit **external identity prerequisites**, not silently selected future choices. Freeze all installed binary/version hashes in the device manifest before the first accepting attempt; a version update invalidates affected comparisons. Chrome/Playwright remains mandatory project proof; additional browser support gates supplement it. ENGINE-DEVICE-01 owns both Android tiers, ENGINE-DEVICE-02 iOS, ENGINE-DEVICE-03 desktop browser/platform matrix. P0-034 requires every listed tier.

Capture SDK/runtime/2dog versions; source+dirty diff+all served bundle/worker hashes; renderer/backend; actual source origin and headers/MIME/cache/compression; CPU/GPU/RAM/OS/browser/drivers; WebGL/Wasm/worker capabilities; AC/battery state, power mode and temperature where observable; CSS viewport/DPR/canvas backing size/render scale; physical refresh mode and compositor trace time origin. Device-native render scale is mandatory; repeat1440×900 DPR1 desktop comparison separately. Do not lower quality, hide consumers or drop a browser to pass.

## Primary-source checks (1 October 2026)

Google's [Pixel hardware specifications](https://support.google.com/pixelphone/answer/7158570?hl=en) confirm Pixel6a's6GB RAM and up-to60Hz display, and Pixel8Pro's12GB RAM; verify the actual selected display mode at capture. Apple's [M2 Air specifications](https://support.apple.com/en-ie/111867) confirm the selected8GB/256GB configuration, and [iPhone13 specifications](https://support.apple.com/en-ie/111872) confirm128GB storage. These Apple pages do not establish our actual presentation cadence;60Hz is a qualification target requiring device readback, not a newly observed refresh fact.

The [Lenovo primary platform specification](https://psref.lenovo.com/syspool/Sys/PDF/ThinkPad/ThinkPad_T14_Gen_5_AMD/ThinkPad_T14_Gen_5_AMD_Spec.pdf) is the retained S002 source for the Ryzen5 PRO8540U/Radeon740M pairing; the selected RAM/panel/OS remain target constraints pending actual SKU readback. Official [Chrome platform requirements](https://support.google.com/chrome/answer/95346?co=GENIE.Platform%3DDesktop&hl=en), [Edge supported systems](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-supported-operating-systems) and [Firefox installation guidance](https://support.mozilla.org/en-US/topics/installation-and-updates/firefox) identify the Windows/macOS/Linux browser families. These availability checks do not establish our worker/WebGL/runtime support. Exact installed browser/OS binaries and application capabilities remain required pre-capture evidence.

## Absolute limits

| Quantity | Limit and units |
| --- | --- |
| Simulation |120Hz;four outer substeps;complete tick p95<=2.5ms, report p99/max and >8.333333ms count; simulated/wall ratio .99–1.01, no increasing debt |
| Animation | Initial60Hz; evaluation p95<=2ms; report p99/max/deadlines;0 routine stable-topology scratch bytes per evaluation |
| Bridge | All publication/pack/copies/drain/application CPU service p95<=1ms per actual presentation; disjoint service across contexts, no summed percentile or elapsed overlap |
| Presentation60 |>=59.4 distinct presentations/s;<=1% missed scheduled60Hz slots; interval p95<=18ms,p99<=25ms; aggregate attributable app CPU p95<=10ms/frame; valid GPU p95<=8ms |
| Presentation90 |>=89.1 distinct presentations/s;<=1% missed scheduled90Hz slots; interval p95<=12ms,p99<=16.7ms; app CPU p95<=7ms/frame; valid GPU p95<=6ms; actual display>=90Hz |
| Age | Physical and animation p95<=33.3ms,p99<=50ms including interpolation; clock mapping error<=1ms, carry uncertainty bounds |
| Input/lifecycle | Input-to-visible and pending indication p95<=100ms; completion measured separately, no invented completion cap or synchronous wait |
| Startup | Cold first usable construction<=5s on20Mbps/50msRTT; compressed transfer<=8MiB aggregate resources until usable; download/runtime/worker/renderer/first-use stages separated |
| Scratch |0 routine warmed simulation scratch bytes/tick and animation scratch bytes/evaluation at stable topology |
| Outputs/transport |<=64KiB per physical publication and per animation result;<=2 full-payload copies per edge, each fan-out separately;<=8MiB aggregate live payload;<=64 pending envelopes per edge; oldest reliable item<=50ms |
| Memory |<=1GiB attributable app resident memory;<=512MiB Wasm linear capacity per context; retain initial256MiB host capacity; workers count toward aggregate; reserved/live/resident categories separate |
| Retention | After20 Run/Reset+Save/Load and20 transitions: exact baseline live worlds/workers/listeners/leases;0 old-generation owned resources; aggregate post-GC managed retained increase<=1MiB |
| Hitches | No unexplained application work>50ms after warm-up; retain ordinary GC/shader/solver stalls in active distributions |

The8MiB transfer cap is a new premeasurement target within the5s network budget:8MiB at20Mbps requires3.3554432s payload transfer before protocol/RTT/startup costs. It is not an observed package size or permission to subtract download from first usable time. Unknown startup/CPU/GPU/heap/GC/presentation observability stays Incomplete, never zero. CPU, GPU, wall, RAF and actual compositor delivery are distinct. Existing Stopwatch scopes only establish inclusive elapsed service proxies; no CPU attribution from them.

## Numerical limits

No change to current physics tolerances or existing stricter tests. Freeze current PhysicsWorldSettings: maximum penetration1e-6m, position1e-7m, velocity1e-8m/s, acceleration1e-9m/s², motor-work1e-10J per prediction interval; preserve current convex-distance tolerance and full double authority. Each 120Hz tick contains four outer substeps of1/480s; this cadence is unchanged. Event/solver/convergence limits are bounded failure limits, never silent truncation.

For the generic benchmark declaration models, independent analytic residual limits are absolute+relative:
position1e-7m+1e-9×reference distance; velocity1e-8m/s+1e-9×reference speed; angle1e-8rad+1e-9×reference angle; linear momentum1e-8kg·m/s+1e-9×supplied magnitude; angular momentum1e-8kg·m²/s+1e-9×supplied magnitude; mass1e-10kg+1e-9×total supplied mass; charge1e-10C+1e-9×supplied charge; thermal/fluid/radiative/chemical energy1e-6J+1e-8×total supplied energy over a complete Run. Motor integration retains its stricter1e-10J per interval; the broader cross-domain ledger does not relax it. Reference scale is independent analytic input/output, never a measured large error or arbitrary denominator. Each law D row may require tighter limits; no looser tolerance without explicit owner instruction.

Conservation residual=initial stored+accepted external supply−final stored−delivered work−declared dissipated/exported energy. Count transfer once, kinetic/potential/rotational/elastic and phase energy explicitly; source depletion and loss cannot disappear. Closed controls have zero supplied mass/charge/energy. Finite stores cannot go negative beyond their law's existing tolerance; invalid topology/inputs reject atomically. Same-build accepted-command replay, Reset/save construction, identity/generation/event counts and rollback require **exact equality**, no numerical epsilon. Render-float differences are not authority tolerances.

## Required-now versus retained failures

P0-003 requires every design/schema/arithmetic/review criterion, zero unknown required design choices and publication receipt. It does not require unavailable physical devices or unimplemented runtime instrumentation to be reported Pass. Their concrete prerequisites above remain required at named qualification owners.

For each subsequent Native/Worker/Optimize slice freeze an identity-bound before/after ledger. Required-now: all affected correctness, exact lifecycle, allocation/algorithm invariants, controls, no new failure, no newly crossed applicable absolute budget and no established worsening of an existing failure. Unchanged inherited failures remain open with owners; inconclusive/noisy evidence is Incomplete. At P0-034 every absolute runtime/device/memory gate passes; no retained-failure exemption survives P0-035.

Retained source-bound failures: S001 native66/76, five WoundSpring and five Bellows failures→P0-012 and their existing repair children; physical/combined Chrome RAF p95 83.6/83.7ms, compliant production150.1ms and tick17.1/10.5ms→P0-033/034; unpublished integrated source prerequisite→P0-029/035; unexpected Reset/depth edit/Run attribution→S007 and P0-032. Quiet input trace or clean retry is not a fix. Preserve S001/S002 logs/hashes, not reclassify them as current independent qualification.
