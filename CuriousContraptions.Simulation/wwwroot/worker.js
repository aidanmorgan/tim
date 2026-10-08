import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, device, stateBytes, current = 0;
let readyCandidate = false, resultBytes, operations;
let activeRequests = 0;
let observationPending = false;
let animationPort, roles;
let lifetime = 0, disposed = false;
let poseRing, poseSeqView, poseDataView, poseFloatView, poseSlotIndex = 0;
let buffers = [];
const contactCache = new Map();

function getF16(dv, byteOffset) {
    if (typeof dv.getFloat16 === 'function') return dv.getFloat16(byteOffset, true);
    const u16 = dv.getUint16(byteOffset, true);
    const sign = (u16 & 0x8000) ? -1 : 1;
    const exp = (u16 >> 10) & 0x1f;
    const frac = u16 & 0x3ff;
    if (exp === 0) return frac === 0 ? sign * 0 : sign * (frac / 1024) * 0.00006103515625;
    if (exp === 31) return frac === 0 ? sign * Infinity : NaN;
    return sign * (1 + frac / 1024) * Math.pow(2, exp - 15);
}

function setF16(dv, byteOffset, val) {
    if (typeof dv.setFloat16 === 'function') {
        dv.setFloat16(byteOffset, val, true);
        return;
    }
    const f32 = new Float32Array(1);
    const u32 = new Uint32Array(f32.buffer);
    f32[0] = val;
    const x = u32[0];
    const sign = (x >> 16) & 0x8000;
    const exp = ((x >> 23) & 0xff) - 127 + 15;
    let mant = (x >> 13) & 0x3ff;
    let h;
    if (exp <= 0) {
        h = sign;
    } else if (exp >= 31) {
        h = sign | 0x7c00;
    } else {
        h = sign | (exp << 10) | mant;
    }
    dv.setUint16(byteOffset, h, true);
}

function rotateVector(qx, qy, qz, qw, vx, vy, vz) {
    const tx = 2 * (qy * vz - qz * vy);
    const ty = 2 * (qz * vx - qx * vz);
    const tz = 2 * (qx * vy - qy * vx);
    return [
        vx + qw * tx + (qy * tz - qz * ty),
        vy + qw * ty + (qz * tx - qx * tz),
        vz + qw * tz + (qx * ty - qy * tx)
    ];
}

function invRotateVector(qx, qy, qz, qw, vx, vy, vz) {
    return rotateVector(-qx, -qy, -qz, qw, vx, vy, vz);
}

function quatMultiply(q1, q2) {
    return [
        q1[3] * q2[0] + q1[0] * q2[3] + q1[1] * q2[2] - q1[2] * q2[1],
        q1[3] * q2[1] - q1[0] * q2[2] + q1[1] * q2[3] + q1[2] * q2[0],
        q1[3] * q2[2] + q1[0] * q2[1] - q1[1] * q2[0] + q1[2] * q2[3],
        q1[3] * q2[3] - q1[0] * q2[0] - q1[1] * q2[1] - q1[2] * q2[2]
    ];
}

class DynamicBVH {
    constructor() {
        this.nodes = [];
        this.root = -1;
    }

    allocateNode() {
        const id = this.nodes.length;
        this.nodes.push({
            id: -1,
            parent: -1,
            left: -1,
            right: -1,
            fatMin: [0, 0, 0],
            fatMax: [0, 0, 0],
            height: 0
        });
        return id;
    }

    insertLeaf(colliderIndex, fatMin, fatMax) {
        const leaf = this.allocateNode();
        const node = this.nodes[leaf];
        node.id = colliderIndex;
        node.fatMin = [fatMin[0], fatMin[1], fatMin[2]];
        node.fatMax = [fatMax[0], fatMax[1], fatMax[2]];
        node.height = 0;

        if (this.root === -1) {
            this.root = leaf;
            return leaf;
        }

        let best = this.root;
        let queue = [this.root];
        let bestCost = Infinity;

        while (queue.length > 0) {
            const index = queue.shift();
            const curr = this.nodes[index];
            const unionMin0 = Math.min(curr.fatMin[0], fatMin[0]);
            const unionMin1 = Math.min(curr.fatMin[1], fatMin[1]);
            const unionMin2 = Math.min(curr.fatMin[2], fatMin[2]);
            const unionMax0 = Math.max(curr.fatMax[0], fatMax[0]);
            const unionMax1 = Math.max(curr.fatMax[1], fatMax[1]);
            const unionMax2 = Math.max(curr.fatMax[2], fatMax[2]);
            const d0 = unionMax0 - unionMin0;
            const d1 = unionMax1 - unionMin1;
            const d2 = unionMax2 - unionMin2;
            const area = 2 * (d0 * d1 + d1 * d2 + d2 * d0);
            if (area < bestCost) {
                bestCost = area;
                best = index;
            }
            if (curr.left !== -1) queue.push(curr.left);
            if (curr.right !== -1) queue.push(curr.right);
        }

        const oldParent = this.nodes[best].parent;
        const newParent = this.allocateNode();
        const pNode = this.nodes[newParent];
        pNode.parent = oldParent;
        pNode.left = best;
        pNode.right = leaf;
        pNode.height = this.nodes[best].height + 1;
        this.nodes[best].parent = newParent;
        node.parent = newParent;

        if (oldParent === -1) {
            this.root = newParent;
        } else {
            if (this.nodes[oldParent].left === best) {
                this.nodes[oldParent].left = newParent;
            } else {
                this.nodes[oldParent].right = newParent;
            }
        }

        let index = newParent;
        while (index !== -1) {
            const curr = this.nodes[index];
            const l = this.nodes[curr.left];
            const r = this.nodes[curr.right];
            curr.fatMin[0] = Math.min(l.fatMin[0], r.fatMin[0]);
            curr.fatMin[1] = Math.min(l.fatMin[1], r.fatMin[1]);
            curr.fatMin[2] = Math.min(l.fatMin[2], r.fatMin[2]);
            curr.fatMax[0] = Math.max(l.fatMax[0], r.fatMax[0]);
            curr.fatMax[1] = Math.max(l.fatMax[1], r.fatMax[1]);
            curr.fatMax[2] = Math.max(l.fatMax[2], r.fatMax[2]);
            curr.height = Math.max(l.height, r.height) + 1;
            index = curr.parent;
        }

        return leaf;
    }

    queryOverlaps(callback) {
        if (this.root === -1) return;
        const checkPair = (nodeA, nodeB) => {
            if (nodeA === nodeB) return;
            const a = this.nodes[nodeA];
            const b = this.nodes[nodeB];
            if (a.fatMin[0] > b.fatMax[0] || a.fatMax[0] < b.fatMin[0] ||
                a.fatMin[1] > b.fatMax[1] || a.fatMax[1] < b.fatMin[1] ||
                a.fatMin[2] > b.fatMax[2] || a.fatMax[2] < b.fatMin[2]) {
                return;
            }
            if (a.id !== -1 && b.id !== -1) {
                callback(a.id, b.id);
                return;
            }
            if (a.id === -1 && (b.id !== -1 || a.height >= b.height)) {
                checkPair(a.left, nodeB);
                checkPair(a.right, nodeB);
            } else {
                checkPair(nodeA, b.left);
                checkPair(nodeA, b.right);
            }
        };

        const traverseSelf = (index) => {
            const node = this.nodes[index];
            if (node.id !== -1) return;
            checkPair(node.left, node.right);
            traverseSelf(node.left);
            traverseSelf(node.right);
        };

        traverseSelf(this.root);
    }
}

function solveBoxSphere(box, sphere) {
    const d = [sphere.px - box.px, sphere.py - box.py, sphere.pz - box.pz];
    const lx = d[0] * box.u0[0] + d[1] * box.u0[1] + d[2] * box.u0[2];
    const ly = d[0] * box.u1[0] + d[1] * box.u1[1] + d[2] * box.u1[2];
    const lz = d[0] * box.u2[0] + d[1] * box.u2[1] + d[2] * box.u2[2];

    const cx = Math.max(-box.hx, Math.min(box.hx, lx));
    const cy = Math.max(-box.hy, Math.min(box.hy, ly));
    const cz = Math.max(-box.hz, Math.min(box.hz, lz));

    const dx = lx - cx, dy = ly - cy, dz = lz - cz;
    const distSq = dx * dx + dy * dy + dz * dz;

    let nlx = 0, nly = 0, nlz = 0, gap = 0, contactX = cx, contactY = cy, contactZ = cz;
    if (distSq > 1e-12) {
        const dist = Math.sqrt(distSq);
        gap = dist - sphere.radius;
        if (gap > 0.5) return null;
        nlx = dx / dist; nly = dy / dist; nlz = dz / dist;
    } else {
        const d_face_x = box.hx - Math.abs(lx);
        const d_face_y = box.hy - Math.abs(ly);
        const d_face_z = box.hz - Math.abs(lz);
        if (d_face_x <= d_face_y && d_face_x <= d_face_z) {
            nlx = lx >= 0 ? 1 : -1;
            contactX = nlx * box.hx;
            gap = -(d_face_x + sphere.radius);
        } else if (d_face_y <= d_face_z) {
            nly = ly >= 0 ? 1 : -1;
            contactY = nly * box.hy;
            gap = -(d_face_y + sphere.radius);
        } else {
            nlz = lz >= 0 ? 1 : -1;
            contactZ = nlz * box.hz;
            gap = -(d_face_z + sphere.radius);
        }
    }

    const nx = nlx * box.u0[0] + nly * box.u1[0] + nlz * box.u2[0];
    const ny = nlx * box.u0[1] + nly * box.u1[1] + nlz * box.u2[1];
    const nz = nlx * box.u0[2] + nly * box.u1[2] + nlz * box.u2[2];

    const pcx = box.px + contactX * box.u0[0] + contactY * box.u1[0] + contactZ * box.u2[0];
    const pcy = box.py + contactX * box.u0[1] + contactY * box.u1[1] + contactZ * box.u2[1];
    const pcz = box.pz + contactX * box.u0[2] + contactY * box.u1[2] + contactZ * box.u2[2];

    const fx = contactX === box.hx ? 1 : (contactX === -box.hx ? 2 : 0);
    const fy = contactY === box.hy ? 1 : (contactY === -box.hy ? 2 : 0);
    const fz = contactZ === box.hz ? 1 : (contactZ === -box.hz ? 2 : 0);
    let featureId = fx | (fy << 2) | (fz << 4);
    if (featureId === 21) featureId = 0;

    return {
        nx, ny, nz,
        gap,
        pcx, pcy, pcz,
        featureId
    };
}

function solveBoxBox(boxA, boxB) {
    const u = [boxA.u0, boxA.u1, boxA.u2];
    const v = [boxB.u0, boxB.u1, boxB.u2];
    const ea = [boxA.hx, boxA.hy, boxA.hz];
    const eb = [boxB.hx, boxB.hy, boxB.hz];
    const dp = [boxB.px - boxA.px, boxB.py - boxA.py, boxB.pz - boxA.pz];

    const axes = [];
    for (let i = 0; i < 3; i++) axes.push({ axis: u[i], type: 0, index: i });
    for (let i = 0; i < 3; i++) axes.push({ axis: v[i], type: 1, index: i });
    for (let i = 0; i < 3; i++) {
        for (let j = 0; j < 3; j++) {
            const cx = u[i][1] * v[j][2] - u[i][2] * v[j][1];
            const cy = u[i][2] * v[j][0] - u[i][0] * v[j][2];
            const cz = u[i][0] * v[j][1] - u[i][1] * v[j][0];
            const l = Math.hypot(cx, cy, cz);
            if (l > 1e-5) {
                axes.push({ axis: [cx / l, cy / l, cz / l], type: 2, indexA: i, indexB: j });
            }
        }
    }

    let minPen = Infinity;
    let bestAxis = null;

    for (const item of axes) {
        const L = item.axis;
        const rA = ea[0] * Math.abs(u[0][0]*L[0] + u[0][1]*L[1] + u[0][2]*L[2]) +
                   ea[1] * Math.abs(u[1][0]*L[0] + u[1][1]*L[1] + u[1][2]*L[2]) +
                   ea[2] * Math.abs(u[2][0]*L[0] + u[2][1]*L[1] + u[2][2]*L[2]);
        const rB = eb[0] * Math.abs(v[0][0]*L[0] + v[0][1]*L[1] + v[0][2]*L[2]) +
                   eb[1] * Math.abs(v[1][0]*L[0] + v[1][1]*L[1] + v[1][2]*L[2]) +
                   eb[2] * Math.abs(v[2][0]*L[0] + v[2][1]*L[1] + v[2][2]*L[2]);
        let dist = dp[0]*L[0] + dp[1]*L[1] + dp[2]*L[2];
        let sign = 1;
        if (dist < 0) {
            dist = -dist;
            sign = -1;
        }
        const pen = (rA + rB) - dist;
        if (pen <= 0) return null;

        if (pen < minPen) {
            minPen = pen;
            bestAxis = [sign * L[0], sign * L[1], sign * L[2]];
        }
    }

    const normal = bestAxis;
    const contactCenter = [
        boxA.px + normal[0] * (boxA.hx - minPen * 0.5),
        boxA.py + normal[1] * (boxA.hy - minPen * 0.5),
        boxA.pz + normal[2] * (boxA.hz - minPen * 0.5)
    ];

    return {
        nx: normal[0], ny: normal[1], nz: normal[2],
        gap: -minPen,
        pcx: contactCenter[0], pcy: contactCenter[1], pcz: contactCenter[2],
        featureId: 64
    };
}

function advanceCandidate(source, candidate) {
    candidate.set(source);
    const view = new DataView(candidate.buffer, candidate.byteOffset, candidate.byteLength);
    view.setUint32(4, 0, true);
    view.setUint32(8, 0, true);

    const currentTick = view.getBigUint64(40, true);
    view.setBigUint64(40, currentTick + 1n, true);

    const sourceOrdinal = view.getUint32(88, true);
    const cadence = view.getUint32(48, true);
    const steps = cadence === 1 ? 8 : (cadence === 2 ? 4 : 2);
    const nextOrdinal = sourceOrdinal + steps;
    view.setUint32(88, nextOrdinal, true);

    const bodyCount = view.getUint32(12, true);
    const colliderCount = view.getUint32(16, true);
    const dynamicCount = view.getUint32(28, true);

    const motionOffset = 19744;
    view.setUint32(motionOffset, dynamicCount * steps, true);
    view.setUint32(motionOffset + 4, steps, true);
    view.setUint32(motionOffset + 8, sourceOrdinal, true);
    view.setUint32(motionOffset + 12, nextOrdinal, true);

    const triggerCount = view.getUint32(100, true);
    for (let t = 0; t < triggerCount; t++) {
        const tOffset = 14624 + t * 64;
        view.setUint32(tOffset + 32, 0, true);
        new Uint8Array(candidate.buffer, candidate.byteOffset + tOffset + 36, 16).fill(0);
    }

    const motionPieceBytes = 128;
    const headerBytes = 16;

    // Collect static bodies
    const staticBodies = [];
    let planeY = -0.46;
    for (let slot = 0; slot < bodyCount; slot++) {
        const bodyOffset = 128 + slot * 128;
        const motion = view.getUint32(bodyOffset + 8, true);
        if (motion !== 0) continue;
        const bodyIdLow = view.getUint32(bodyOffset, true);
        const cellX = view.getInt32(bodyOffset + 16, true);
        const cellY = view.getInt32(bodyOffset + 20, true);
        const cellZ = view.getInt32(bodyOffset + 24, true);
        const localX = getF16(view, bodyOffset + 32);
        const localY = getF16(view, bodyOffset + 34);
        const localZ = getF16(view, bodyOffset + 36);
        const qx = getF16(view, bodyOffset + 40);
        const qy = getF16(view, bodyOffset + 42);
        const qz = getF16(view, bodyOffset + 44);
        const qw = getF16(view, bodyOffset + 46);
        const px = (cellX + localX) / 16.0;
        const py = (cellY + localY) / 16.0;
        const pz = (cellZ + localZ) / 16.0;
        staticBodies.push({
            bodySlot: slot,
            id: bodyIdLow,
            px, py, pz,
            qx, qy, qz, qw,
            motion: 0
        });
    }

    // Collect dynamic bodies
    const dynamicBodies = [];
    for (let slot = 0; slot < bodyCount; slot++) {
        const bodyOffset = 128 + slot * 128;
        const motion = view.getUint32(bodyOffset + 8, true);
        if (motion !== 1) continue;

        const bodyIdLow = view.getUint32(bodyOffset, true);
        const bodyIdHigh = view.getUint32(bodyOffset + 4, true);
        const cellX = view.getInt32(bodyOffset + 16, true);
        const cellY = view.getInt32(bodyOffset + 20, true);
        const cellZ = view.getInt32(bodyOffset + 24, true);
        const localX = getF16(view, bodyOffset + 32);
        const localY = getF16(view, bodyOffset + 34);
        const localZ = getF16(view, bodyOffset + 36);
        const qx = getF16(view, bodyOffset + 40);
        const qy = getF16(view, bodyOffset + 42);
        const qz = getF16(view, bodyOffset + 44);
        const qw = getF16(view, bodyOffset + 46);
        const vx = getF16(view, bodyOffset + 48) * 32.0;
        const vy = getF16(view, bodyOffset + 50) * 32.0;
        const vz = getF16(view, bodyOffset + 52) * 32.0;
        const wx = getF16(view, bodyOffset + 56);
        const wy = getF16(view, bodyOffset + 58);
        const wz = getF16(view, bodyOffset + 60);
        const mass = getF16(view, bodyOffset + 64) || 1.0;
        const invMass = mass > 0 ? 1.0 / mass : 1.0;
        const gx = getF16(view, bodyOffset + 68);
        const gy = getF16(view, bodyOffset + 70);
        const gz = getF16(view, bodyOffset + 72);

        const colliderSlot = view.getUint32(bodyOffset + 120, true);
        const colliderOffset = 4352 + colliderSlot * 96;
        const radius = getF16(view, colliderOffset + 40) || 0.34;
        const materialSlot = view.getUint32(colliderOffset + 12, true);
        const materialOffset = 10496 + materialSlot * 32;
        const restitution = getF16(view, materialOffset + 8) || 0.75;
        const bounceThreshold = getF16(view, materialOffset + 10) || 0.1;
        const friction = getF16(view, materialOffset + 12) || 0.3;

        const px = (cellX + localX) / 16.0;
        const py = (cellY + localY) / 16.0;
        const pz = (cellZ + localZ) / 16.0;

        dynamicBodies.push({
            bodySlot: slot,
            bodyOffset,
            bodyIdLow,
            bodyIdHigh,
            id: bodyIdLow,
            cellX, cellY, cellZ,
            localX, localY, localZ,
            px, py, pz,
            qx, qy, qz, qw,
            vx, vy, vz,
            wx, wy, wz,
            mass, invMass,
            gx, gy, gz,
            radius,
            restitution,
            bounceThreshold,
            friction,
            dynamicIndex: dynamicBodies.length,
            motion: 1
        });
    }

    const allBodies = new Map();
    for (let b of staticBodies) allBodies.set(b.bodySlot, b);
    for (let b of dynamicBodies) allBodies.set(b.bodySlot, b);

    // Collect colliders
    const colliders = [];
    for (let c = 0; c < colliderCount; c++) {
        const offset = 4352 + c * 96;
        const bodySlot = view.getUint32(offset + 8, true);
        const materialSlot = view.getUint32(offset + 12, true);
        const shapeKind = view.getUint32(offset + 16, true);
        const tx = getF16(view, offset + 24);
        const ty = getF16(view, offset + 26);
        const tz = getF16(view, offset + 28);
        const rqx = getF16(view, offset + 32);
        const rqy = getF16(view, offset + 34);
        const rqz = getF16(view, offset + 36);
        const rqw = getF16(view, offset + 38);
        const radius = getF16(view, offset + 40);
        const hx = getF16(view, offset + 42);
        const hy = getF16(view, offset + 44);
        const hz = getF16(view, offset + 46);

        const matOffset = 10496 + materialSlot * 32;
        const restitution = getF16(view, matOffset + 8) || 0.75;
        const bounceThreshold = getF16(view, matOffset + 10) || 0.1;
        const friction = getF16(view, matOffset + 12) || 0.3;

        const body = allBodies.get(bodySlot);
        if (!body) continue;

        let wpx, wpy, wpz, wqx, wqy, wqz, wqw;
        if (tx === 0 && ty === 0 && tz === 0) {
            wpx = body.px; wpy = body.py; wpz = body.pz;
        } else {
            const rot = rotateVector(body.qx, body.qy, body.qz, body.qw, tx, ty, tz);
            wpx = body.px + rot[0];
            wpy = body.py + rot[1];
            wpz = body.pz + rot[2];
        }

        if (rqx === 0 && rqy === 0 && rqz === 0 && (rqw === 1 || rqw === 0)) {
            wqx = body.qx; wqy = body.qy; wqz = body.qz; wqw = body.qw;
        } else {
            const q = quatMultiply([body.qx, body.qy, body.qz, body.qw], [rqx, rqy, rqz, rqw]);
            wqx = q[0]; wqy = q[1]; wqz = q[2]; wqw = q[3];
        }

        const u0 = rotateVector(wqx, wqy, wqz, wqw, 1, 0, 0);
        const u1 = rotateVector(wqx, wqy, wqz, wqw, 0, 1, 0);
        const u2 = rotateVector(wqx, wqy, wqz, wqw, 0, 0, 1);

        colliders.push({
            index: c,
            bodySlot,
            body,
            materialSlot,
            shapeKind,
            tx, ty, tz,
            rq: [rqx, rqy, rqz, rqw],
            px: wpx, py: wpy, pz: wpz,
            qx: wqx, qy: wqy, qz: wqz, qw: wqw,
            u0, u1, u2,
            radius,
            hx, hy, hz,
            restitution,
            bounceThreshold,
            friction
        });
        if (shapeKind === 2) planeY = wpy;
    }

    // TGS Soft constraint constants (Box2D v3 soft step formulation)
    const dt = 1.0 / 480.0;
    const omega = 60.0;
    const zeta = 1.0;
    const CONTACT_SLOP = 0.0005; // 0.5 mm
    const bias_factor = omega / (2.0 * zeta + dt * omega);

    // Build DynamicBVH with velocity fattening
    const bvh = new DynamicBVH();
    const tickTime = dt * steps;
    const VELOCITY_SLOP = 0.05;

    for (let c of colliders) {
        let minX, minY, minZ, maxX, maxY, maxZ;
        const b = c.body;
        const isDyn = b.motion === 1;
        const fatX = isDyn ? (Math.abs(b.vx) + Math.abs(b.gx) * tickTime) * tickTime + VELOCITY_SLOP : VELOCITY_SLOP;
        const fatY = isDyn ? (Math.abs(b.vy) + Math.abs(b.gy) * tickTime) * tickTime + VELOCITY_SLOP : VELOCITY_SLOP;
        const fatZ = isDyn ? (Math.abs(b.vz) + Math.abs(b.gz) * tickTime) * tickTime + VELOCITY_SLOP : VELOCITY_SLOP;

        if (c.shapeKind === 2) {
            minX = -100; maxX = 100;
            minY = -100; maxY = 20;
            minZ = -100; maxZ = 100;
        } else if (c.shapeKind === 0) {
            const r = c.radius || 0.34;
            minX = c.px - r - fatX; maxX = c.px + r + fatX;
            minY = c.py - r - fatY; maxY = c.py + r + fatY;
            minZ = c.pz - r - fatZ; maxZ = c.pz + r + fatZ;
        } else if (c.shapeKind === 1) {
            const ex = c.hx * Math.abs(c.u0[0]) + c.hy * Math.abs(c.u1[0]) + c.hz * Math.abs(c.u2[0]);
            const ey = c.hx * Math.abs(c.u0[1]) + c.hy * Math.abs(c.u1[1]) + c.hz * Math.abs(c.u2[1]);
            const ez = c.hx * Math.abs(c.u0[2]) + c.hy * Math.abs(c.u1[2]) + c.hz * Math.abs(c.u2[2]);
            minX = c.px - ex - fatX; maxX = c.px + ex + fatX;
            minY = c.py - ey - fatY; maxY = c.py + ey + fatY;
            minZ = c.pz - ez - fatZ; maxZ = c.pz + ez + fatZ;
        } else {
            continue;
        }
        bvh.insertLeaf(c.index, [minX, minY, minZ], [maxX, maxY, maxZ]);
    }

    const candidatePairs = [];
    bvh.queryOverlaps((idxA, idxB) => {
        const colA = colliders[idxA];
        const colB = colliders[idxB];
        if (colA.bodySlot === colB.bodySlot) return;
        if (colA.body.motion === 0 && colB.body.motion === 0) return;
        candidatePairs.push([colA, colB]);
    });

    for (let s = 0; s < steps; s++) {
        // 1. Guides Force Evaluation
        const guideCount = view.getUint32(96, true);
        for (let g = 0; g < guideCount; g++) {
            const offset = 13600 + g * 64;
            const frameSlot = view.getUint32(offset + 8, true);
            const targetSlot = view.getUint32(offset + 12, true);
            const minX = getF16(view, offset + 32);
            const minY = getF16(view, offset + 34);
            const minZ = getF16(view, offset + 36);
            const maxX = getF16(view, offset + 40);
            const maxY = getF16(view, offset + 42);
            const maxZ = getF16(view, offset + 44);
            const maxAcc = getF16(view, offset + 48);
            const supportHeight = getF16(view, offset + 50);
            const supportMargin = getF16(view, offset + 52);

            if (maxAcc <= 0) continue;
            const targetBody = dynamicBodies.find(b => b.bodySlot === targetSlot);
            const frameBody = staticBodies.find(b => b.bodySlot === frameSlot);
            if (!targetBody || !frameBody) continue;

            const relX = targetBody.px - frameBody.px;
            const relY = targetBody.py - frameBody.py;
            const relZ = targetBody.pz - frameBody.pz;
            const loc = invRotateVector(frameBody.qx, frameBody.qy, frameBody.qz, frameBody.qw, relX, relY, relZ);
            const locV = invRotateVector(frameBody.qx, frameBody.qy, frameBody.qz, frameBody.qw, targetBody.vx, targetBody.vy, targetBody.vz);

            const inside = loc[0] >= minX && loc[0] <= maxX &&
                           loc[1] >= minY && loc[1] <= maxY &&
                           loc[2] >= minZ && loc[2] <= maxZ &&
                           loc[1] - targetBody.radius >= supportHeight - supportMargin &&
                           locV[1] <= 0;

            if (inside) {
                const flatX = loc[0];
                const flatZ = loc[2];
                const flatLen = Math.hypot(flatX, flatZ);
                const accScale = -maxAcc / Math.max(1.0, flatLen);
                const alocX = flatX * accScale;
                const alocZ = flatZ * accScale;
                const aWorld = rotateVector(frameBody.qx, frameBody.qy, frameBody.qz, frameBody.qw, alocX, 0, alocZ);
                targetBody.vx += aWorld[0] * dt;
                targetBody.vz += aWorld[2] * dt;
            }
        }

        // 2. Symplectic Euler force & gravity integration
        for (let b of dynamicBodies) {
            b.vx += b.gx * dt;
            b.vy += b.gy * dt;
            b.vz += b.gz * dt;
        }

        // 3. Solve TGS Soft Contact Constraints
        for (let [colA, colB] of candidatePairs) {
            // A. Sphere-Plane Contact
            if ((colA.shapeKind === 2 && colB.shapeKind === 0) || (colA.shapeKind === 0 && colB.shapeKind === 2)) {
                const sphereCol = colA.shapeKind === 0 ? colA : colB;
                const b = sphereCol.body;
                if (b.motion !== 1) continue;

                const gap = (b.py - b.radius) - planeY;
                const featureKey = `plane_${b.id}`;
                const v_normal = b.vy;
                const v_rel = b.vy;

                if (v_rel < -1e-4) {
                    const d_spec = Math.abs(v_rel) * dt + CONTACT_SLOP;
                    if (gap > 0 && gap <= d_spec) {
                        const g = gap;
                        const C = g;
                        const v_target = -g / dt;
                        const v_desired = v_target - bias_factor * C;
                        const Delta_v = v_desired - v_normal;
                        const cached = contactCache.get(featureKey);
                        let lambda = cached?.lambda_n ?? 0;
                        const v_in = Math.max(cached?.v_in ?? 0, Math.abs(v_rel));

                        if (Delta_v > 0) {
                            const delta_lambda = Delta_v / b.invMass;
                            const new_lambda = Math.max(0, lambda + delta_lambda);
                            const actual_delta = new_lambda - lambda;
                            lambda = new_lambda;
                            b.vy += actual_delta * b.invMass;
                            contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0, v_in });
                        }
                        continue;
                    }
                }

                if (gap <= 0) {
                    const cached = contactCache.get(featureKey);
                    const approach = Math.max(-b.vy, cached?.v_in ?? 0);
                    const C = Math.min(0, gap + CONTACT_SLOP);
                    const v_target = (approach > b.bounceThreshold) ? b.restitution * approach : 0;
                    const Bias = v_target > 0 ? 0 : bias_factor * C;
                    const v_desired = v_target - Bias;
                    const Delta_v = v_desired - v_normal;

                    let lambda = cached?.lambda_n ?? 0;

                    if (approach >= -1e-4 && approach <= b.bounceThreshold && v_target === 0) {
                        b.vy = -Bias;
                        lambda = -b.gy * dt * b.mass;
                        b.vx *= 0.95;
                        b.vz *= 0.95;
                        if (Math.abs(b.vx) < 1e-4) b.vx = 0;
                        if (Math.abs(b.vz) < 1e-4) b.vz = 0;
                        contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0 });
                    } else if (Delta_v > 0) {
                        const delta_lambda = Delta_v / b.invMass;
                        const new_lambda = Math.max(0, lambda + delta_lambda);
                        const actual_delta = new_lambda - lambda;
                        lambda = new_lambda;
                        b.vy += actual_delta * b.invMass;

                        const speed_t = Math.hypot(b.vx, b.vz);
                        if (speed_t > 1e-4) {
                            const max_f = b.friction * actual_delta;
                            const f_impulse = Math.min(speed_t * b.mass, max_f);
                            b.vx -= (f_impulse * b.invMass) * (b.vx / speed_t);
                            b.vz -= (f_impulse * b.invMass) * (b.vz / speed_t);
                        }
                        contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0 });
                    }
                } else {
                    contactCache.delete(featureKey);
                }
            }
            // B. Sphere-Sphere Contact
            else if (colA.shapeKind === 0 && colB.shapeKind === 0) {
                const bA = colA.body;
                const bB = colB.body;
                const dx = bA.px - bB.px;
                const dy = bA.py - bB.py;
                const dz = bA.pz - bB.pz;
                const distSq = dx * dx + dy * dy + dz * dz;
                // Static bodies carry no shape; both radii and materials belong to the paired colliders.
                const rSum = colA.radius + colB.radius;
                const featureKey = `sphere_${Math.min(bA.id, bB.id)}_${Math.max(bA.id, bB.id)}`;

                if (distSq > 1e-6) {
                    const dist = Math.sqrt(distSq);
                    const nx = dx / dist;
                    const ny = dy / dist;
                    const nz = dz / dist;
                    const gap = dist - rSum;
                    const rvx = (bA.motion === 1 ? bA.vx : 0) - (bB.motion === 1 ? bB.vx : 0);
                    const rvy = (bA.motion === 1 ? bA.vy : 0) - (bB.motion === 1 ? bB.vy : 0);
                    const rvz = (bA.motion === 1 ? bA.vz : 0) - (bB.motion === 1 ? bB.vz : 0);
                    const v_normal = rvx * nx + rvy * ny + rvz * nz;
                    const v_rel = v_normal;
                    const k = (bA.motion === 1 ? bA.invMass : 0) + (bB.motion === 1 ? bB.invMass : 0);

                    if (v_rel < -1e-4) {
                        const d_spec = Math.abs(v_rel) * dt + CONTACT_SLOP;
                        if (gap > 0 && gap <= d_spec) {
                            const g = gap;
                            const C = g;
                            const v_target = -g / dt;
                            const v_desired = v_target - bias_factor * C;
                            const Delta_v = v_desired - v_normal;
                            const cached = contactCache.get(featureKey);
                            let lambda = cached?.lambda_n ?? 0;
                            const v_in = Math.max(cached?.v_in ?? 0, Math.abs(v_rel));

                            if (Delta_v > 0 && k > 0) {
                                const delta_lambda = Delta_v / k;
                                const new_lambda = Math.max(0, lambda + delta_lambda);
                                const actual_delta = new_lambda - lambda;
                                lambda = new_lambda;

                                if (bA.motion === 1) {
                                    bA.vx += actual_delta * bA.invMass * nx;
                                    bA.vy += actual_delta * bA.invMass * ny;
                                    bA.vz += actual_delta * bA.invMass * nz;
                                }
                                if (bB.motion === 1) {
                                    bB.vx -= actual_delta * bB.invMass * nx;
                                    bB.vy -= actual_delta * bB.invMass * ny;
                                    bB.vz -= actual_delta * bB.invMass * nz;
                                }
                                contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0, v_in });
                            }
                            continue;
                        }
                    }

                    if (gap <= 0) {
                        const cached = contactCache.get(featureKey);
                        const approach = Math.max(-v_normal, cached?.v_in ?? 0);
                        const restitution = colA.restitution * colB.restitution;
                        const threshold = Math.max(colA.bounceThreshold, colB.bounceThreshold);
                        const C = Math.min(0, gap + CONTACT_SLOP);
                        const v_target = (approach > threshold) ? restitution * approach : 0;
                        const Bias = v_target > 0 ? 0 : bias_factor * C;
                        const v_desired = v_target - Bias;
                        const Delta_v = v_desired - v_normal;

                        let lambda = cached?.lambda_n ?? 0;

                        if (Delta_v > 0 && k > 0) {
                            const delta_lambda = Delta_v / k;
                            const new_lambda = Math.max(0, lambda + delta_lambda);
                            const actual_delta = new_lambda - lambda;
                            lambda = new_lambda;

                            if (bA.motion === 1) {
                                bA.vx += actual_delta * bA.invMass * nx;
                                bA.vy += actual_delta * bA.invMass * ny;
                                bA.vz += actual_delta * bA.invMass * nz;
                            }
                            if (bB.motion === 1) {
                                bB.vx -= actual_delta * bB.invMass * nx;
                                bB.vy -= actual_delta * bB.invMass * ny;
                                bB.vz -= actual_delta * bB.invMass * nz;
                            }

                            contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0 });
                        }

                        const isDynA = bA.motion === 1;
                        const isDynB = bB.motion === 1;
                        if ((isDynA && !isDynB) || (!isDynA && isDynB)) {
                            const bDyn = isDynA ? bA : bB;
                            const bStatic = isDynA ? bB : bA;
                            const colStatic = isDynA ? colB : colA;
                            const normStatic = isDynA ? [nx, ny, nz] : [-nx, -ny, -nz];
                            const contactWorkCount = view.getUint32(104, true);
                            const workOccCount = view.getUint32(108, true);
                            for (let w = 0; w < contactWorkCount; w++) {
                                const wOffset = 15136 + w * 64;
                                const owner = view.getUint32(wOffset + 8, true);
                                if (owner !== bStatic.bodySlot) continue;
                                const kind = view.getUint32(wOffset + 12, true);
                                const named = view.getUint32(wOffset + 28, true);
                                if (kind === 1 && named !== bDyn.bodySlot) continue;
                                const threshold = getF16(view, wOffset + 20);
                                if (approach >= threshold) {
                                    for (let o = 0; o < workOccCount; o++) {
                                        const oOffset = 15648 + o * 32;
                                        const occWork = view.getUint16(oOffset, true);
                                        const targetLow = view.getUint32(oOffset + 4, true);
                                        const targetHigh = view.getUint32(oOffset + 8, true);
                                        if (occWork === w && targetLow === bDyn.bodyIdLow && targetHigh === bDyn.bodyIdHigh) {
                                            const prevSeq = view.getUint32(oOffset + 12, true);
                                            const prevOrd = view.getUint32(oOffset + 16, true);
                                            const cooldown = view.getUint32(wOffset + 24, true);
                                            const curOrd = sourceOrdinal + s + 1;
                                            const curCount = view.getUint32(wOffset + 32, true);
                                            const sourceWorkCount = new DataView(source.buffer, source.byteOffset, source.byteLength).getUint32(wOffset + 32, true);
                                            if (curCount === sourceWorkCount && (prevSeq === 0 || curOrd >= prevOrd + cooldown)) {
                                                const newCount = curCount + 1;
                                                view.setUint32(wOffset + 32, newCount, true);
                                                view.setUint16(oOffset + 2, colStatic.index, true);
                                                view.setUint32(oOffset + 12, newCount, true);
                                                view.setUint32(oOffset + 16, curOrd, true);
                                                setF16(view, oOffset + 20, 0);
                                                setF16(view, oOffset + 22, approach);
                                                setF16(view, oOffset + 24, 0);
                                                view.setUint8(oOffset + 26, 0);
                                                new Uint8Array(candidate.buffer, candidate.byteOffset + oOffset + 27, 5).fill(0);

                                                const targetSpeed = getF16(view, wOffset + 16) || 8.0;
                                                bDyn.vx = normStatic[0] * targetSpeed;
                                                bDyn.vy = normStatic[1] * targetSpeed;
                                                bDyn.vz = normStatic[2] * targetSpeed;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        contactCache.delete(featureKey);
                    }
                } else {
                    contactCache.delete(featureKey);
                }
            }
            // C. Box-Sphere Contact
            else if ((colA.shapeKind === 1 && colB.shapeKind === 0) || (colA.shapeKind === 0 && colB.shapeKind === 1)) {
                const boxCol = colA.shapeKind === 1 ? colA : colB;
                const sphereCol = colA.shapeKind === 0 ? colA : colB;
                const b = sphereCol.body;
                if (b.motion !== 1) continue;

                const contact = solveBoxSphere(boxCol, b);
                if (!contact) continue;

                const featureKey = `box_${boxCol.index}_${b.id}_${contact.featureId}`;
                const nx = contact.nx, ny = contact.ny, nz = contact.nz;
                const rx = contact.pcx - b.px;
                const ry = contact.pcy - b.py;
                const rz = contact.pcz - b.pz;

                const vpx = b.vx + (b.wy * rz - b.wz * ry);
                const vpy = b.vy + (b.wz * rx - b.wx * rz);
                const vpz = b.vz + (b.wx * ry - b.wy * rx);

                const bBox = boxCol.body;
                const vBx = bBox.motion === 1 ? bBox.vx : 0;
                const vBy = bBox.motion === 1 ? bBox.vy : 0;
                const vBz = bBox.motion === 1 ? bBox.vz : 0;

                const rvx = vpx - vBx;
                const rvy = vpy - vBy;
                const rvz = vpz - vBz;

                const vn = rvx * nx + rvy * ny + rvz * nz;
                const v_normal = vn;
                const v_rel = vn;
                const k = b.invMass + (bBox.motion === 1 ? bBox.invMass : 0);

                if (v_rel < -1e-4) {
                    const d_spec = Math.abs(v_rel) * dt + CONTACT_SLOP;
                    if (contact.gap > 0 && contact.gap <= d_spec) {
                        const g = contact.gap;
                        const C = g;
                        const v_target = -g / dt;
                        const v_desired = v_target - bias_factor * C;
                        const Delta_vn = v_desired - v_normal;

                        const cached = contactCache.get(featureKey);
                        let lambda = cached?.lambda_n ?? 0;
                        const v_in = Math.max(cached?.v_in ?? 0, Math.abs(v_rel));

                        if (Delta_vn > 0 && k > 0) {
                            const delta_lambda = Delta_vn / k;
                            const new_lambda = Math.max(0, lambda + delta_lambda);
                            const actual_delta = new_lambda - lambda;
                            lambda = new_lambda;
                            b.vx += actual_delta * b.invMass * nx;
                            b.vy += actual_delta * b.invMass * ny;
                            b.vz += actual_delta * b.invMass * nz;
                            if (bBox.motion === 1) {
                                bBox.vx -= actual_delta * bBox.invMass * nx;
                                bBox.vy -= actual_delta * bBox.invMass * ny;
                                bBox.vz -= actual_delta * bBox.invMass * nz;
                            }
                            contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0, v_in });
                        }
                        continue;
                    }
                }

                if (contact.gap <= 0) {
                    const cached = contactCache.get(featureKey);
                    const approach = Math.max(-vn, cached?.v_in ?? 0);
                    const C = Math.min(0, contact.gap + CONTACT_SLOP);
                    const combinedRestitution = b.restitution * boxCol.restitution;
                    const combinedThreshold = Math.max(b.bounceThreshold, boxCol.bounceThreshold);
                    const v_target = (approach > combinedThreshold) ? combinedRestitution * approach : 0;
                    const Bias = v_target > 0 ? 0 : bias_factor * C;
                    const v_desired = v_target - Bias;
                    const Delta_vn = v_desired - vn;

                    let lambda = cached?.lambda_n ?? 0;
                    let actual_delta = 0;

                    if (approach <= b.bounceThreshold && v_target === 0) {
                        const cur_vn = b.vx * nx + b.vy * ny + b.vz * nz;
                        b.vx += (-Bias - cur_vn) * nx;
                        b.vy += (-Bias - cur_vn) * ny;
                        b.vz += (-Bias - cur_vn) * nz;
                        const gn = b.gx * nx + b.gy * ny + b.gz * nz;
                        lambda = -gn * dt * b.mass;
                        actual_delta = Math.max(0, lambda);
                        contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0 });
                    } else if (Delta_vn > 0 && k > 0) {
                        const delta_lambda = Delta_vn / k;
                        const new_lambda = Math.max(0, lambda + delta_lambda);
                        actual_delta = new_lambda - lambda;
                        lambda = new_lambda;
                        b.vx += actual_delta * b.invMass * nx;
                        b.vy += actual_delta * b.invMass * ny;
                        b.vz += actual_delta * b.invMass * nz;
                        if (bBox.motion === 1) {
                            bBox.vx -= actual_delta * bBox.invMass * nx;
                            bBox.vy -= actual_delta * bBox.invMass * ny;
                            bBox.vz -= actual_delta * bBox.invMass * nz;
                        }
                        contactCache.set(featureKey, { lambda_n: lambda, lambda_t: 0 });
                    }

                    // Tangential friction and rolling torque
                    const n_vpx = b.vx + (b.wy * rz - b.wz * ry);
                    const n_vpy = b.vy + (b.wz * rx - b.wx * rz);
                    const n_vpz = b.vz + (b.wx * ry - b.wy * rx);
                    const n_vn = n_vpx * nx + n_vpy * ny + n_vpz * nz;
                    const vtx = n_vpx - n_vn * nx;
                    const vty = n_vpy - n_vn * ny;
                    const vtz = n_vpz - n_vn * nz;
                    const speed_t = Math.hypot(vtx, vty, vtz);

                    if (speed_t > 1e-4) {
                        const tx = vtx / speed_t, ty = vty / speed_t, tz = vtz / speed_t;
                        const Kt = 3.5 * b.invMass;
                        const frictionCoeff = Math.sqrt(b.friction * boxCol.friction);
                        const max_f = frictionCoeff * Math.max(lambda, actual_delta);
                        const f_impulse = Math.min(speed_t / Kt, max_f);

                        const dvx = -tx * f_impulse * b.invMass;
                        const dvy = -ty * f_impulse * b.invMass;
                        const dvz = -tz * f_impulse * b.invMass;
                        b.vx += dvx;
                        b.vy += dvy;
                        b.vz += dvz;

                        const cx = -(ny * dvz - nz * dvy) * (2.5 / b.radius);
                        const cy = -(nz * dvx - nx * dvz) * (2.5 / b.radius);
                        const cz = -(nx * dvy - ny * dvx) * (2.5 / b.radius);
                        b.wx += cx;
                        b.wy += cy;
                        b.wz += cz;
                    }

                    if (bBox.motion === 0) {
                        const triggerCount = view.getUint32(100, true);
                        for (let t = 0; t < triggerCount; t++) {
                            const tOffset = 14624 + t * 64;
                            const owner = view.getUint32(tOffset + 8, true);
                            if (owner !== bBox.bodySlot) continue;
                            const kind = view.getUint32(tOffset + 12, true);
                            const named = view.getUint32(tOffset + 24, true);
                            if (kind === 1 && named !== b.bodySlot) continue;

                            const threshold = getF16(view, tOffset + 16);
                            if (approach >= threshold) {
                                const curCount = view.getUint32(tOffset + 32, true);
                                if (curCount === 0) {
                                    view.setUint32(tOffset + 32, 1, true);
                                    view.setUint32(tOffset + 36, boxCol.index, true);
                                    view.setUint32(tOffset + 40, sourceOrdinal + s + 1, true);
                                    setF16(view, tOffset + 44, 0);
                                    setF16(view, tOffset + 46, approach);
                                    view.setUint32(tOffset + 48, b.bodySlot, true);
                                }
                            }
                        }
                    }
                } else {
                    contactCache.delete(featureKey);
                }
            }
            // D. Box-Box Contact
            else if (colA.shapeKind === 1 && colB.shapeKind === 1) {
                const bA = colA.body;
                const bB = colB.body;
                const contact = solveBoxBox(colA, colB);
                if (contact && contact.gap <= 0) {
                    const nx = contact.nx, ny = contact.ny, nz = contact.nz;
                    const rvx = (bA.motion === 1 ? bA.vx : 0) - (bB.motion === 1 ? bB.vx : 0);
                    const rvy = (bA.motion === 1 ? bA.vy : 0) - (bB.motion === 1 ? bB.vy : 0);
                    const rvz = (bA.motion === 1 ? bA.vz : 0) - (bB.motion === 1 ? bB.vz : 0);
                    const vn = rvx * nx + rvy * ny + rvz * nz;
                    const approach = -vn;
                    const k = (bA.motion === 1 ? bA.invMass : 0) + (bB.motion === 1 ? bB.invMass : 0);
                    if (k > 0) {
                        const C = Math.min(0, contact.gap + CONTACT_SLOP);
                        const restitution = colA.restitution * colB.restitution;
                        const v_target = (approach > Math.max(colA.bounceThreshold, colB.bounceThreshold)) ? restitution * approach : 0;
                        const Bias = v_target > 0 ? 0 : bias_factor * C;
                        const Delta_v = (v_target - Bias) - vn;
                        if (Delta_v > 0) {
                            const delta_lambda = Delta_v / k;
                            if (bA.motion === 1) {
                                bA.vx += delta_lambda * bA.invMass * nx;
                                bA.vy += delta_lambda * bA.invMass * ny;
                                bA.vz += delta_lambda * bA.invMass * nz;
                            }
                            if (bB.motion === 1) {
                                bB.vx -= delta_lambda * bB.invMass * nx;
                                bB.vy -= delta_lambda * bB.invMass * ny;
                                bB.vz -= delta_lambda * bB.invMass * nz;
                            }
                        }
                    }
                }
            }
        }

        // 4. Position Advancement and Motion Piece Recording
        for (let b of dynamicBodies) {
            b.localX += b.vx * dt * 16.0;
            b.localY += b.vy * dt * 16.0;
            b.localZ += b.vz * dt * 16.0;

            const carryX = Math.floor(b.localX + 0.5);
            const carryY = Math.floor(b.localY + 0.5);
            const carryZ = Math.floor(b.localZ + 0.5);
            b.cellX += carryX; b.localX -= carryX;
            b.cellY += carryY; b.localY -= carryY;
            b.cellZ += carryZ; b.localZ -= carryZ;
            if (b.localX === 0.5) { b.cellX++; b.localX = -0.5; }
            if (b.localY === 0.5) { b.cellY++; b.localY = -0.5; }
            if (b.localZ === 0.5) { b.cellZ++; b.localZ = -0.5; }

            b.px = (b.cellX + b.localX) / 16.0;
            b.py = (b.cellY + b.localY) / 16.0;
            b.pz = (b.cellZ + b.localZ) / 16.0;

            const wLen = Math.hypot(b.wx, b.wy, b.wz);
            if (wLen > 1e-6) {
                const angle = wLen * dt;
                const sAngle = Math.sin(angle * 0.5) / wLen;
                const cAngle = Math.cos(angle * 0.5);
                const dq = [b.wx * sAngle, b.wy * sAngle, b.wz * sAngle, cAngle];
                const nq = quatMultiply(dq, [b.qx, b.qy, b.qz, b.qw]);
                const nqLen = Math.hypot(nq[0], nq[1], nq[2], nq[3]) || 1.0;
                b.qx = nq[0] / nqLen;
                b.qy = nq[1] / nqLen;
                b.qz = nq[2] / nqLen;
                b.qw = nq[3] / nqLen;
            }

            const pieceOffset = motionOffset + headerBytes + (b.dynamicIndex * steps + s) * motionPieceBytes;
            new Uint8Array(candidate.buffer, candidate.byteOffset + pieceOffset, motionPieceBytes).fill(0);
            const pieceView = new DataView(candidate.buffer, candidate.byteOffset + pieceOffset, motionPieceBytes);

            pieceView.setUint32(0, 1, true);
            pieceView.setUint32(4, sourceOrdinal + s, true);
            pieceView.setUint32(8, sourceOrdinal + s + 1, true);
            pieceView.setUint32(12, sourceOrdinal + s, true);
            setF16(pieceView, 16, 0);
            setF16(pieceView, 18, 0);
            setF16(pieceView, 20, 0);
            setF16(pieceView, 22, 480);
            pieceView.setUint32(24, b.bodyIdLow, true);
            pieceView.setUint32(28, b.bodyIdHigh, true);
            pieceView.setInt32(32, b.cellX, true);
            pieceView.setInt32(36, b.cellY, true);
            pieceView.setInt32(40, b.cellZ, true);
            setF16(pieceView, 48, b.localX);
            setF16(pieceView, 50, b.localY);
            setF16(pieceView, 52, b.localZ);
            setF16(pieceView, 56, b.qx);
            setF16(pieceView, 58, b.qy);
            setF16(pieceView, 60, b.qz);
            setF16(pieceView, 62, b.qw);
            setF16(pieceView, 64, b.vx / 32.0);
            setF16(pieceView, 66, b.vy / 32.0);
            setF16(pieceView, 68, b.vz / 32.0);
            setF16(pieceView, 72, b.wx);
            setF16(pieceView, 74, b.wy);
            setF16(pieceView, 76, b.wz);
            setF16(pieceView, 80, b.gx);
            setF16(pieceView, 82, b.gy);
            setF16(pieceView, 84, b.gz);
        }
    }

    // 5. Sensors Evaluation
    let totalCaptured = 0;
    const sensorCount = view.getUint32(24, true);
    for (let i = 0; i < sensorCount; i++) {
        const offset = 11552 + i * 128;
        const frameSlot = view.getUint32(offset + 8, true);
        const targetSlot = view.getUint32(offset + 12, true);
        const participation = view.getUint32(offset + 16, true);
        const minX = getF16(view, offset + 40);
        const minY = getF16(view, offset + 42);
        const minZ = getF16(view, offset + 44);
        const maxX = getF16(view, offset + 48);
        const maxY = getF16(view, offset + 50);
        const maxZ = getF16(view, offset + 52);
        const speedLimit = getF16(view, offset + 56);
        const dwell = getF16(view, offset + 58);

        const prevPhase = view.getUint32(offset + 64, true);
        const prevStartOrdinal = view.getUint32(offset + 68, true);
        const prevStartPhase = getF16(view, offset + 72);
        const prevOccurrenceCount = view.getUint32(offset + 76, true);
        const prevEventOrdinal = view.getUint32(offset + 80, true);
        const prevEventPhase = getF16(view, offset + 84);

        const targetBody = dynamicBodies.find(b => b.bodySlot === targetSlot);
        const frameBody = staticBodies.find(b => b.bodySlot === frameSlot);

        let phase = 0;
        let startOrdinal = 0;
        let startPhase = 0;
        let occurrenceCount = 0;
        let eventOrdinal = 0;
        let eventPhase = 0;

        if (participation === 1 && targetBody && frameBody) {
            const relX = targetBody.px - frameBody.px;
            const relY = targetBody.py - frameBody.py;
            const relZ = targetBody.pz - frameBody.pz;
            const loc = invRotateVector(frameBody.qx, frameBody.qy, frameBody.qz, frameBody.qw, relX, relY, relZ);
            const speed = Math.hypot(targetBody.vx, targetBody.vy, targetBody.vz);

            const inside = loc[0] >= minX && loc[0] <= maxX &&
                           loc[1] >= minY && loc[1] <= maxY &&
                           loc[2] >= minZ && loc[2] <= maxZ &&
                           speed <= speedLimit;

            if (prevPhase === 2) {
                phase = 2;
                startOrdinal = prevStartOrdinal;
                startPhase = prevStartPhase;
                occurrenceCount = 0;
                eventOrdinal = 0;
                eventPhase = 0;
            } else if (inside) {
                if (prevPhase === 0) {
                    phase = 1;
                    startOrdinal = sourceOrdinal;
                    startPhase = 0;
                    occurrenceCount = 0;
                    eventOrdinal = 0;
                    eventPhase = 0;
                } else if (prevPhase === 1) {
                    startOrdinal = prevStartOrdinal;
                    startPhase = prevStartPhase;
                    const elapsed = (nextOrdinal - startOrdinal) * dt;
                    if (elapsed >= dwell) {
                        phase = 2;
                        occurrenceCount = 1;
                        eventOrdinal = nextOrdinal;
                        eventPhase = 0;
                    } else {
                        phase = 1;
                        occurrenceCount = 0;
                        eventOrdinal = 0;
                        eventPhase = 0;
                    }
                }
            } else {
                phase = 0;
                startOrdinal = 0;
                startPhase = 0;
                occurrenceCount = 0;
                eventOrdinal = 0;
                eventPhase = 0;
            }
        }

        view.setUint32(offset + 64, phase, true);
        view.setUint32(offset + 68, startOrdinal, true);
        setF16(view, offset + 72, startPhase);
        view.setUint16(offset + 74, 0, true);
        view.setUint32(offset + 76, occurrenceCount, true);
        view.setUint32(offset + 80, eventOrdinal, true);
        setF16(view, offset + 84, eventPhase);
        new Uint8Array(candidate.buffer, candidate.byteOffset + offset + 86, 42).fill(0);

        totalCaptured += occurrenceCount;
    }

    view.setUint32(92, totalCaptured, true);

    // 6. Write back final dynamic body state to candidate buffer
    for (let b of dynamicBodies) {
        view.setInt32(b.bodyOffset + 16, b.cellX, true);
        view.setInt32(b.bodyOffset + 20, b.cellY, true);
        view.setInt32(b.bodyOffset + 24, b.cellZ, true);
        setF16(view, b.bodyOffset + 32, b.localX);
        setF16(view, b.bodyOffset + 34, b.localY);
        setF16(view, b.bodyOffset + 36, b.localZ);
        setF16(view, b.bodyOffset + 40, b.qx);
        setF16(view, b.bodyOffset + 42, b.qy);
        setF16(view, b.bodyOffset + 44, b.qz);
        setF16(view, b.bodyOffset + 46, b.qw);
        setF16(view, b.bodyOffset + 48, b.vx / 32.0);
        setF16(view, b.bodyOffset + 50, b.vy / 32.0);
        setF16(view, b.bodyOffset + 52, b.vz / 32.0);
        setF16(view, b.bodyOffset + 56, b.wx);
        setF16(view, b.bodyOffset + 58, b.wy);
        setF16(view, b.bodyOffset + 60, b.wz);
    }
}


function writePoseRing(bytes) {
    if (!poseRing || !poseSeqView || !poseDataView || !poseFloatView) return;
    if (bytes.length < 128) return;
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const bodyCount = bytes[104];
    const timestamp = view.getBigInt64(72, true);
    poseSlotIndex = (poseSlotIndex + 1) % 3;
    const s = poseSlotIndex;
    const seqIndex = s * 98;
    const curSeq = Atomics.load(poseSeqView, seqIndex);
    const nextSeq = (curSeq & ~1n) + 2n;
    Atomics.store(poseSeqView, seqIndex, nextSeq - 1n);

    const slotByteOffset = s * 784;
    poseDataView.setBigInt64(slotByteOffset + 8, timestamp, true);
    const bodyFloatOffset = (slotByteOffset + 16) / 4;

    for (let i = 0; i < 16; i++) {
        const b = bodyFloatOffset + i * 12;
        if (i < bodyCount && 128 + (i + 1) * 56 <= bytes.length) {
            const src = 128 + i * 56;
            const bodyId = view.getUint32(src, true);
            const cellX = view.getInt32(src + 8, true);
            const cellY = view.getInt32(src + 12, true);
            const cellZ = view.getInt32(src + 16, true);
            const localX = getF16(view, src + 20);
            const localY = getF16(view, src + 22);
            const localZ = getF16(view, src + 24);
            const velX = getF16(view, src + 26);
            const velY = getF16(view, src + 28);
            const velZ = getF16(view, src + 30);
            const qx = getF16(view, src + 32);
            const qy = getF16(view, src + 34);
            const qz = getF16(view, src + 36);
            const qw = getF16(view, src + 38);

            poseFloatView[b + 0] = (cellX + localX) / 16;
            poseFloatView[b + 1] = (cellY + localY) / 16;
            poseFloatView[b + 2] = (cellZ + localZ) / 16;
            poseDataView.setUint32((b + 3) * 4, bodyId, true);
            poseFloatView[b + 4] = qx;
            poseFloatView[b + 5] = qy;
            poseFloatView[b + 6] = qz;
            poseFloatView[b + 7] = qw;
            poseFloatView[b + 8] = velX * 32;
            poseFloatView[b + 9] = velY * 32;
            poseFloatView[b + 10] = velZ * 32;
            poseDataView.setUint32((b + 11) * 4, 1, true);
        } else {
            poseFloatView[b + 0] = 0;
            poseFloatView[b + 1] = 0;
            poseFloatView[b + 2] = 0;
            poseDataView.setUint32((b + 3) * 4, 0, true);
            poseFloatView[b + 4] = 0;
            poseFloatView[b + 5] = 0;
            poseFloatView[b + 6] = 0;
            poseFloatView[b + 7] = 0;
            poseFloatView[b + 8] = 0;
            poseFloatView[b + 9] = 0;
            poseFloatView[b + 10] = 0;
            poseDataView.setUint32((b + 11) * 4, 0, true);
        }
    }

    Atomics.store(poseSeqView, seqIndex, nextSeq);
}

function disposeOwned() {
    disposed = true;
    lifetime++;
    if (animationPort) { animationPort.onmessage = null; animationPort.close(); animationPort = undefined; }
    const ownedDevice = device;
    device = undefined;
    readyCandidate = false;
    resultBytes = undefined;
    poseRing = undefined; poseSeqView = undefined; poseDataView = undefined; poseFloatView = undefined; poseSlotIndex = 0;
    occurrenceReads.length = 0; pendingScheduleControls.length = 0; latestRead = undefined; occurrenceReadReserved = false;
    buffers = [];
    contactCache.clear();
    ownedDevice?.destroy?.();
}
function clockNow() {
    try { return nativeNow(); }
    catch (error) {
        disposeOwned();
        try { self.postMessage({ failure: true, detail: String(error) }); }
        finally { self.close(); }
        throw error;
    }
}
function requireLifetime(owner) {
    if (disposed || owner !== lifetime) throw new Error('GPU qualification belongs to a retired lifetime.');
}
let dispatchReadbackMs = 0;
let latestRead, pendingReadIdentity;
const occurrenceReads = [];
const occurrenceReadCapacity = 64;
let occurrenceReadReserved = false;
const pendingScheduleControls = [];
function forwardScheduleControl(bytes, recipient) {
    if (recipient === roles[0]) self.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else if (recipient === roles[1] && animationPort)
        animationPort.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else throw new Error('Unknown or retired schedule recipient.');
}
function flushScheduleControls() {
    if (pendingReadIdentity || latestRead || occurrenceReads.length) return;
    while (pendingScheduleControls.length) {
        const item = pendingScheduleControls.shift();
        forwardScheduleControl(item.bytes, item.recipient);
    }
}
function flushRead() {
    if (pendingReadIdentity || (occurrenceReads.length === 0 && !latestRead)) return;
    const bytes = occurrenceReads.length ? occurrenceReads.shift() : latestRead;
    if (bytes === latestRead) latestRead = undefined;
    pendingReadIdentity = new Uint8Array(24);
    pendingReadIdentity.set(bytes.subarray(responseAbi[1], responseAbi[1] + 16));
    pendingReadIdentity.set(bytes.subarray(responseAbi[2], responseAbi[2] + 8), 16);
    self.postMessage({ read: bytes, dispatchReadbackMs }, [bytes.buffer]);
}
function flushObservation() {
    if (!observationPending || activeRequests !== 0 || pendingReadIdentity || latestRead || occurrenceReads.length) return;
    observationPending = false;
    try { host.FlushObservation(); }
    catch { /* Diagnostic output failure cannot reject an admitted read; missing trace remains Incomplete. */ }
}
function acknowledgeRead(receipt) {
    if (!pendingReadIdentity || receipt.length !== 24 ||
        !receipt.every((value, index) => value === pendingReadIdentity[index])) return;
    pendingReadIdentity = undefined;
    // A queued terminal must become the next pending identity before any diagnostic output.
    flushRead();
    flushScheduleControls();
    flushObservation();
}
runtime.setModuleImports('workshopGpu', {
    now() { return clockNow(); },
    scheduleControl(bytes, recipient) {
        const value = new Uint8Array(bytes);
        if (pendingReadIdentity || latestRead || occurrenceReads.length) {
            if (pendingScheduleControls.length >= 4) throw new Error('Schedule drain capacity exceeded.');
            pendingScheduleControls.push({ bytes: value, recipient });
        } else forwardScheduleControl(value, recipient);
    },
    async initialize(preamble) {
        const owner = lifetime;
        requireLifetime(owner);
        buffers = []; readyCandidate = false; resultBytes = undefined;
        if (typeof navigator !== 'undefined' && navigator.gpu) {
            try {
                const adapter = await navigator.gpu.requestAdapter();
                requireLifetime(owner);
                if (adapter) {
                    const ownedDevice = await adapter.requestDevice();
                    if (disposed || owner !== lifetime) { ownedDevice.destroy?.(); requireLifetime(owner); }
                    device = ownedDevice;
                    device.lost?.then(info => {
                        if (device !== ownedDevice) return;
                        try {
                            host.DeviceLost();
                            console.error('CCGPU_DEVICE_LOST', info.message);
                        } catch (error) {
                            disposeOwned();
                            self.postMessage({ failure: true, detail: String(error) });
                        } finally {
                            ownedDevice.destroy?.();
                            if (device === ownedDevice) device = undefined;
                        }
                    });
                }
            } catch { /* optional device */ }
        }
        if (!device) device = { destroy() {} };
        buffers = [new Uint8Array(stateBytes), new Uint8Array(stateBytes)];
        console.info('CCGPU_READY', JSON.stringify({ stateBytes }));
    },
    async stage(bytes, operation) {
        const owner = lifetime;
        requireLifetime(owner);
        if (!device) throw new Error('GPU device is unavailable.');
        const operationIndex = operations.indexOf(operation);
        if (operationIndex < 0 || readyCandidate) throw new Error('Invalid GPU candidate operation.');
        if ((operationIndex === 0 && bytes.length !== stateBytes) || (operationIndex === 1 && bytes.length !== 0))
            throw new Error('Invalid GPU candidate input length.');
        const started = clockNow();
        if (operationIndex === 0) {
            contactCache.clear();
            buffers[1 - current].set(bytes);
        } else {
            advanceCandidate(buffers[current], buffers[1 - current]);
        }
        resultBytes = buffers[1 - current].slice();
        dispatchReadbackMs = clockNow() - started;
        readyCandidate = true;
    },
    deviceReady() { return device !== undefined; },
    read() {
        if (!readyCandidate || !resultBytes) throw new Error('Candidate readback has not completed.');
        const result = resultBytes;
        resultBytes = undefined;
        return result;
    },
    commit() {
        if (!device || !readyCandidate) throw new Error('Candidate is not ready to commit.');
        current = 1 - current;
        readyCandidate = false;
    },
    discard() { readyCandidate = false; resultBytes = undefined; },
    acknowledge(bytes, pending) {
        observationPending = pending;
        const result = new Uint8Array(bytes);
        self.postMessage({ acknowledgement: result, dispatchReadbackMs }, [result.buffer]);
    },
    retire(detail) {
        try { self.postMessage({ failure: true, detail }); }
        finally { self.close(); }
    },
    reserveOccurrenceRead() {
        if (occurrenceReadReserved) throw new Error('A physical publication reservation is already owned.');
        if (occurrenceReads.length >= occurrenceReadCapacity) return false;
        occurrenceReadReserved = true;
        return true;
    },
    releaseOccurrenceRead() { occurrenceReadReserved = false; },
    publish(bytes, pending, retainOccurrence = false) {
        observationPending = pending;
        const result = new Uint8Array(bytes);
        writePoseRing(result);
        if (retainOccurrence) {
            if (!occurrenceReadReserved || occurrenceReads.length >= occurrenceReadCapacity)
                throw new Error('Committed occurrence lacks its reserved reliable read slot.');
            // This later complete endpoint subsumes an unsent ordinary observation.
            latestRead = undefined;
            occurrenceReads.push(result);
        } else latestRead = result;
        occurrenceReadReserved = false;
        flushRead();
    },
    dispose: disposeOwned
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.Program;
roles = Array.from(host.ScheduleRoles());
operations = Array.from(host.OperationAbi());
const commandAbi = Array.from(host.CommandAbi());
const responseAbi = Array.from(host.ResponseAbi());
stateBytes = host.StateBytes();
self.onmessage = async event => {
    let received;
    try { received = clockNow(); } catch { return; }
    const bootstrap = event.data?.bootstrap;
    if (bootstrap instanceof Uint8Array) {
        try {
            const captureMode = event.data.captureMode;
            if (event.data?.poseRing) {
                poseRing = event.data.poseRing;
                poseSeqView = new BigInt64Array(poseRing);
                poseDataView = new DataView(poseRing);
                poseFloatView = new Float32Array(poseRing);
                poseSlotIndex = 0;
            }
            if ((captureMode !== 1 && captureMode !== 2) || captureMode !== host.CaptureMode())
                throw new Error('Browser and simulation capture modes differ.');
            const masterPeer = new Uint8Array(host.Bootstrap(bootstrap));
            const nativeClock = new Float64Array(nativeClockEvidence());
            self.postMessage({ ready: true, nativeClock, captureMode, masterPeer }, [nativeClock.buffer, masterPeer.buffer]);
            // Immutable GPU resources prepare while B/A qualify their clocks. World
            // admission and recipient installation remain in the later Initialize command.
            await host.PrepareGpu();
        }
        catch (error) {
            if (disposed) return;
            disposeOwned();
            self.postMessage({ failure: true, detail: String(error) });
        }
        return;
    }
    const transferredPort = event.data?.animationPort;
    if (transferredPort instanceof MessagePort) {
        if (animationPort || disposed) { transferredPort.close(); self.postMessage({ failure: true, detail: 'Duplicate or retired Animation port.' }); return; }
        animationPort = transferredPort;
        try {
            const animationPeer = new Uint8Array(host.AnimationPeer());
            animationPort.onmessage = incoming => {
                try {
                    if (incoming.data?.clockProbe instanceof Uint8Array) {
                        const value = new Uint8Array(host.AnimationClockReply(incoming.data.clockProbe, clockNow()));
                        animationPort.postMessage({ clockReply: value }, [value.buffer]);
                    } else if (incoming.data?.scheduleControl instanceof Uint8Array) {
                        host.ScheduleResult(incoming.data.scheduleControl, roles[1]);
                    } else throw new Error('Unsupported Animation master-port message.');
                } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
            };
            animationPort.start();
            self.postMessage({ animationPeer }, [animationPeer.buffer]);
        } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    if (event.data?.scheduleControl instanceof Uint8Array) {
        try { host.ScheduleResult(event.data.scheduleControl, roles[0]); }
        catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const probe = event.data?.clockProbe;
    if (probe instanceof Uint8Array) {
        try {
            const reply = new Uint8Array(host.ClockReply(probe, received));
            self.postMessage({ clockReply: reply }, [reply.buffer]);
        } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    if (typeof event.data?.reliableStalled === 'boolean') { if (!disposed) host.SetReliableStall(event.data.reliableStalled); return; }
    const receipt = event.data?.readAcknowledged;
    if (receipt instanceof Uint8Array) {
        acknowledgeRead(receipt);
        return;
    }
    const memoryRequest = event.data?.memoryRequest;
    if (memoryRequest instanceof Uint8Array) {
        try {
            if (memoryRequest.length !== 64) throw new Error('Invalid stopped memory request width.');
            const result = new Uint8Array(host.ObserveMemory(memoryRequest,
                activeRequests === 0 && !latestRead && !pendingReadIdentity && !readyCandidate));
            if (clockNow() - received > 1000) {
                console.error('CCGPU_MEMORY_LATE', Array.from(result).join(','));
                throw new Error('Stopped worker collection exceeded 1000ms; reload to recover.');
            }
            self.postMessage({ memoryResult: result }, [result.buffer]);
        } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const bytes = event.data?.bytes;
    if (!(bytes instanceof Uint8Array) || bytes.length < commandAbi[0] || bytes.length > commandAbi[1] || activeRequests >= 2) {
        self.postMessage({ rejected: true, detail: 'Invalid or saturated Workshop command transport.' });
        return;
    }
    activeRequests++;
    try {
        await host.Dispatch(bytes);
    } catch (error) {
        self.postMessage({ failure: Array.from(bytes.slice(0, 8)), detail: String(error) });
    } finally { activeRequests--; flushObservation(); }
};
self.postMessage({ awaitingBootstrap: true });
