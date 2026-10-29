import { parentPort, workerData } from 'node:worker_threads';
import * as ring from '../../CuriousContraptions.web/wwwroot/clock-ring.js';
const data = workerData as { buffer: SharedArrayBuffer; peers: Record<number, Uint8Array> };
const clock = ring.attachClockRing(data.buffer);
let now = 1_500_000n;
ring.serveRequests(clock, data.peers, () => (now += 1000n));
parentPort!.postMessage('ready');
for (;;) ring.awaitRequest(clock, ring.serveRequests(clock, data.peers, () => (now += 1000n)));
