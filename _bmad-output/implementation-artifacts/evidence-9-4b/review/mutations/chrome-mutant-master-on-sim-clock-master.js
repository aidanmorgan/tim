// The session clock master context, owned and spawned by the Simulation worker (Story 9.4b R5).
// It answers the typed ClockProbe/ClockReply exchange of every requester through the session clock ring and does no
// other work, so solver ticks on the Simulation's event loop can never delay a probe. Its stamps are in the
// Simulation's own native time domain: the shared high-resolution time origin of the two isolated contexts, plus this
// context's witnessed native reading, plus the derivation correction the Simulation maintains in the ring. The
// Simulation qualifies the derivation before the session starts and keeps checking it every 100 ms
// (WorkshopClockDerivation); each reply carries the derivation epoch it was stamped under. The master domain, its
// origin and every Simulation-side stamp are unchanged.
import { admitNativeClock, nativeNow } from '../native-clock.js';
import { attachClockRing, serveRequests, awaitRequest, ClockRole } from '../clock-ring.js';

await admitNativeClock();
self.onmessage = event => {
    self.onmessage = null; // One bootstrap per context; the Simulation terminates this context with its own session.
    try {
        const { ring, simulationOrigin, browserPeer, animationPeer } = event.data ?? {};
        if (!Number.isFinite(simulationOrigin) || !(browserPeer instanceof Uint8Array) || !(animationPeer instanceof Uint8Array))
            throw new Error('Invalid clock master bootstrap.');
        const clock = attachClockRing(ring);
        const peers = { [ClockRole.Browser]: browserPeer, [ClockRole.Animation]: animationPeer };
        const derivedNow = () => performance.timeOrigin + nativeNow() - simulationOrigin;
        self.postMessage({ ready: true });
        // REVIEW MUTANT (clock master back on the Simulation thread): a Browser (state word 8) or Animation (state word 72)
        // probe is answered only once the Simulation's event loop has turned (ring word 5); its own derivation slot
        // (state word 136) is served at once, since the Simulation waits synchronously for it.
        const gate = clock.words;
        for (;;) {
            const requesterWaiting = Atomics.load(gate, 8) === 1 || Atomics.load(gate, 72) === 1;
            if (requesterWaiting && Atomics.load(gate, 136) !== 1) {
                const turn = Atomics.load(gate, 5);
                Atomics.wait(gate, 5, turn, 1000);
            }
            awaitRequest(clock, serveRequests(clock, peers, derivedNow));
        }
    } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
};
self.postMessage({ awaitingBootstrap: true });
