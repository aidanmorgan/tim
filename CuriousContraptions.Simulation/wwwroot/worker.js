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

// Shared rigid-body and contact kernels. Every pair kind produces the same point records and the same
// constraint row consumes them; no element or shape owns a solver branch beyond its manifold generator.
const SUBSTEP_SECONDS = 1.0 / 480.0;
const CONTACT_SLOP = 0.0002;          // allowed resting penetration; with the soft offset below rest stays inside the 0.5 mm envelope
const SPECULATIVE_SLOP = 0.004;       // manifold admission margin added to the relative-speed term
const MAX_LINEAR_SPEED = 64;          // m/s limit before position integration
const MAX_ANGULAR_SPEED = 128;        // rad/s limit before orientation integration
// Committed velocities are f32: rounding each component moves the magnitude by at most 2^-24 relative, so 2^-20 keeps the host bound.
const SPEED_CLAMP = MAX_LINEAR_SPEED * (1 - 2 ** -20);
const SPIN_CLAMP = MAX_ANGULAR_SPEED * (1 - 2 ** -20);
function clampVelocity(b) {
    const speed = Math.hypot(b.vx, b.vy, b.vz);
    if (speed > SPEED_CLAMP) { const k = SPEED_CLAMP / speed; b.vx *= k; b.vy *= k; b.vz *= k; }
    const spin = Math.hypot(b.wx, b.wy, b.wz);
    if (spin > SPIN_CLAMP) { const k = SPIN_CLAMP / spin; b.wx *= k; b.wy *= k; b.wz *= k; }
}
const CONTACT_HERTZ = 60;             // Box2D v3 soft contact stiffness (<= substep rate / 4)
const CONTACT_DAMPING_RATIO = 10;
const MAX_PUSHOUT_SPEED = 3;          // m/s cap on the soft position-correction bias
const SOLVER_ITERATIONS = 8;          // biased sweeps per substep (friction rows couple the manifolds of a pair chain)
const RELAX_ITERATIONS = 4;           // unbiased sweeps at the integrated positions remove the bias velocity
const BLOCK_REGULARIZATION = 1e-3;    // relative diagonal term: four coplanar rows span only three rigid DOF, so the joint system is rank-deficient
const EDGE_FACE_ALIGNMENT = 0.99;     // |dot| above this treats a cross axis as the aligned face (face clipping); below it is edge-edge

function makeSoft(hertz, zeta, h) {
    const omega = 2 * Math.PI * hertz;
    const a1 = 2 * zeta + h * omega;
    const a2 = h * omega * a1;
    const a3 = 1 / (1 + a2);
    return { biasRate: omega / a1, massScale: a2 * a3, impulseScale: a3 };
}

// Canonical cell remainders are committed as Half in [-0.5, 0.5); carry on the rounded value, not the double.
const halfScratch = new DataView(new ArrayBuffer(2));
function toHalf(value) { setF16(halfScratch, 0, value); return getF16(halfScratch, 0); }

function cross(ax, ay, az, bx, by, bz) {
    return [ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx];
}

function readPrincipalInertia(view, offset) {
    const mantissa = getF16(view, offset);
    const exponent = view.getInt32(offset + 4, true);
    return mantissa * Math.pow(2, exponent);
}

// World inverse inertia and centre of mass from the committed pose and the declared principal frame.
function updateDynamicFrame(b) {
    const q = quatMultiply([b.qx, b.qy, b.qz, b.qw], b.principal);
    const e0 = rotateVector(q[0], q[1], q[2], q[3], 1, 0, 0);
    const e1 = rotateVector(q[0], q[1], q[2], q[3], 0, 1, 0);
    const e2 = rotateVector(q[0], q[1], q[2], q[3], 0, 0, 1);
    const m = b.invIWorld;
    for (let i = 0; i < 3; i++)
        for (let j = 0; j < 3; j++)
            m[i * 3 + j] = b.invI[0] * e0[i] * e0[j] + b.invI[1] * e1[i] * e1[j] + b.invI[2] * e2[i] * e2[j];
    const c = rotateVector(b.qx, b.qy, b.qz, b.qw, b.comLocal[0], b.comLocal[1], b.comLocal[2]);
    b.cmx = b.px + c[0]; b.cmy = b.py + c[1]; b.cmz = b.pz + c[2];
}

function applyInvInertia(b, x, y, z) {
    const m = b.invIWorld;
    return [m[0] * x + m[1] * y + m[2] * z, m[3] * x + m[4] * y + m[5] * z, m[6] * x + m[7] * y + m[8] * z];
}

function applyImpulse(b, px, py, pz, rx, ry, rz) {
    if (b.motion !== 1) return;
    b.vx += px * b.invMass; b.vy += py * b.invMass; b.vz += pz * b.invMass;
    const t = cross(rx, ry, rz, px, py, pz);
    const dw = applyInvInertia(b, t[0], t[1], t[2]);
    b.wx += dw[0]; b.wy += dw[1]; b.wz += dw[2];
}

function pointVelocity(b, rx, ry, rz) {
    if (b.motion !== 1) return [0, 0, 0];
    return [b.vx + (b.wy * rz - b.wz * ry), b.vy + (b.wz * rx - b.wx * rz), b.vz + (b.wx * ry - b.wy * rx)];
}

// Collider world pose from its body and declared local pose; dynamic colliders refresh every substep.
function refreshCollider(c) {
    const body = c.body;
    if (c.tx === 0 && c.ty === 0 && c.tz === 0) {
        c.px = body.px; c.py = body.py; c.pz = body.pz;
    } else {
        const rot = rotateVector(body.qx, body.qy, body.qz, body.qw, c.tx, c.ty, c.tz);
        c.px = body.px + rot[0]; c.py = body.py + rot[1]; c.pz = body.pz + rot[2];
    }
    const rq = c.rq;
    if (rq[0] === 0 && rq[1] === 0 && rq[2] === 0) {
        c.qx = body.qx; c.qy = body.qy; c.qz = body.qz; c.qw = body.qw;
    } else {
        const q = quatMultiply([body.qx, body.qy, body.qz, body.qw], rq);
        c.qx = q[0]; c.qy = q[1]; c.qz = q[2]; c.qw = q[3];
    }
    c.u0 = rotateVector(c.qx, c.qy, c.qz, c.qw, 1, 0, 0);
    c.u1 = rotateVector(c.qx, c.qy, c.qz, c.qw, 0, 1, 0);
    c.u2 = rotateVector(c.qx, c.qy, c.qz, c.qw, 0, 0, 1);
}

// Extent used for speculative margins and angular AABB fattening.
function colliderExtent(c) {
    if (c.shapeKind === 0) return c.radius;
    if (c.shapeKind === 1) return Math.hypot(c.hx, c.hy, c.hz);
    return 0;
}

function boxVertex(box, sx, sy, sz) {
    return [
        box.px + sx * box.hx * box.u0[0] + sy * box.hy * box.u1[0] + sz * box.hz * box.u2[0],
        box.py + sx * box.hx * box.u0[1] + sy * box.hy * box.u1[1] + sz * box.hz * box.u2[1],
        box.pz + sx * box.hx * box.u0[2] + sy * box.hy * box.u1[2] + sz * box.hz * box.u2[2]
    ];
}

// Contact point record: the normal points from body b toward body a; a receives +lambda n.
function contactPoint(a, b, colA, colB, nx, ny, nz, px, py, pz, gap, feature) {
    return { a, b, colA, colB, nx, ny, nz, px, py, pz, gap, feature };
}

function collideSpherePlane(sphere, plane, margin, out) {
    const n = plane.u1;
    const gap = (sphere.px - plane.px) * n[0] + (sphere.py - plane.py) * n[1] + (sphere.pz - plane.pz) * n[2] - sphere.radius;
    if (gap > margin) return;
    out.push(contactPoint(sphere.body, plane.body, sphere, plane, n[0], n[1], n[2],
        sphere.px - sphere.radius * n[0], sphere.py - sphere.radius * n[1], sphere.pz - sphere.radius * n[2], gap, 0));
}

function collideSphereSphere(colA, colB, margin, out) {
    const dx = colA.px - colB.px, dy = colA.py - colB.py, dz = colA.pz - colB.pz;
    const distSq = dx * dx + dy * dy + dz * dz;
    if (distSq <= 1e-12) return;
    const dist = Math.sqrt(distSq);
    const gap = dist - (colA.radius + colB.radius);
    if (gap > margin) return;
    const nx = dx / dist, ny = dy / dist, nz = dz / dist;
    out.push(contactPoint(colA.body, colB.body, colA, colB, nx, ny, nz,
        colB.px + nx * (colB.radius + gap * 0.5), colB.py + ny * (colB.radius + gap * 0.5), colB.pz + nz * (colB.radius + gap * 0.5), gap, 0));
}

function solveBoxSphere(box, sphere, margin) {
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
        if (gap > margin) return null;
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
    const featureId = fx | (fy << 2) | (fz << 4);

    return { nx, ny, nz, gap, pcx, pcy, pcz, featureId };
}

function collideBoxSphere(box, sphere, margin, out) {
    const contact = solveBoxSphere(box, sphere, margin);
    if (!contact) return;
    out.push(contactPoint(sphere.body, box.body, sphere, box, contact.nx, contact.ny, contact.nz,
        contact.pcx, contact.pcy, contact.pcz, contact.gap, contact.featureId));
}

// Area-maximizing reduction to at most four support points: deepest, farthest, largest triangle,
// then the point on the far side of the first two that maximizes the quadrilateral.
function reduceManifold(points, nx, ny, nz) {
    if (points.length <= 4) return points;
    let p1 = points[0];
    for (const p of points) if (p.gap < p1.gap) p1 = p;
    let p2 = null, best = -1;
    for (const p of points) {
        if (p === p1) continue;
        const d = (p.px - p1.px) ** 2 + (p.py - p1.py) ** 2 + (p.pz - p1.pz) ** 2;
        if (d > best) { best = d; p2 = p; }
    }
    const ex = p2.px - p1.px, ey = p2.py - p1.py, ez = p2.pz - p1.pz;
    let p3 = null; best = -1; let sign3 = 0;
    for (const p of points) {
        if (p === p1 || p === p2) continue;
        const c = cross(p.px - p1.px, p.py - p1.py, p.pz - p1.pz, ex, ey, ez);
        const signed = c[0] * nx + c[1] * ny + c[2] * nz;
        if (Math.abs(signed) > best) { best = Math.abs(signed); p3 = p; sign3 = Math.sign(signed); }
    }
    let p4 = null; best = -1;
    for (const p of points) {
        if (p === p1 || p === p2 || p === p3) continue;
        const c = cross(p.px - p1.px, p.py - p1.py, p.pz - p1.pz, ex, ey, ez);
        const signed = c[0] * nx + c[1] * ny + c[2] * nz;
        if (Math.sign(signed) === sign3 && sign3 !== 0) continue;
        if (Math.abs(signed) > best) { best = Math.abs(signed); p4 = p; }
    }
    if (p4 === null) {
        for (const p of points) {
            if (p === p1 || p === p2 || p === p3) continue;
            const d = (p.px - p3.px) ** 2 + (p.py - p3.py) ** 2 + (p.pz - p3.pz) ** 2;
            if (d > best) { best = d; p4 = p; }
        }
    }
    return [p1, p2, p3, p4];
}

// Box against a half-space: the vertices within the margin, reduced to the four deepest supports.
function collideBoxPlane(box, plane, margin, out) {
    const n = plane.u1;
    const candidates = [];
    let index = 0;
    for (const sx of [-1, 1]) for (const sy of [-1, 1]) for (const sz of [-1, 1]) {
        const v = boxVertex(box, sx, sy, sz);
        const gap = (v[0] - plane.px) * n[0] + (v[1] - plane.py) * n[1] + (v[2] - plane.pz) * n[2];
        if (gap <= margin)
            candidates.push(contactPoint(box.body, plane.body, box, plane, n[0], n[1], n[2],
                v[0] - gap * 0.5 * n[0], v[1] - gap * 0.5 * n[1], v[2] - gap * 0.5 * n[2], gap, index));
        index++;
    }
    if (candidates.length === 0) return;
    for (const p of reduceManifold(candidates, n[0], n[1], n[2])) out.push(p);
}

// Single support contact along the SAT normal: used for edge-edge axes and whenever face clipping yields no point.
// Each box contributes the edge that supports it against the normal (the edge along its axis most perpendicular to n,
// through its deepest vertex); the point is the midpoint of the closest points of those two segments, the gap the SAT separation.
function supportContact(boxA, boxB, n, separation, feature, out) {
    const supportEdge = (box, dx, dy, dz) => {
        const axes = [box.u0, box.u1, box.u2], ext = [box.hx, box.hy, box.hz];
        const dots = axes.map(u => dx * u[0] + dy * u[1] + dz * u[2]);
        let along = 0;
        for (let i = 1; i < 3; i++) if (Math.abs(dots[i]) < Math.abs(dots[along])) along = i;
        const base = [box.px, box.py, box.pz];
        for (let i = 0; i < 3; i++) {
            if (i === along) continue;
            const sign = Math.sign(dots[i]) || 1;
            for (let k = 0; k < 3; k++) base[k] += sign * ext[i] * axes[i][k];
        }
        const dir = axes[along], h = ext[along];
        return { p: [base[0] - h * dir[0], base[1] - h * dir[1], base[2] - h * dir[2]], d: [2 * h * dir[0], 2 * h * dir[1], 2 * h * dir[2]] };
    };
    const ea = supportEdge(boxA, -n[0], -n[1], -n[2]);
    const eb = supportEdge(boxB, n[0], n[1], n[2]);
    const [s, t] = closestSegmentParameters(ea, eb);
    const a = [ea.p[0] + s * ea.d[0], ea.p[1] + s * ea.d[1], ea.p[2] + s * ea.d[2]];
    const b = [eb.p[0] + t * eb.d[0], eb.p[1] + t * eb.d[1], eb.p[2] + t * eb.d[2]];
    out.push(contactPoint(boxA.body, boxB.body, boxA, boxB, n[0], n[1], n[2],
        (a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5, separation, feature));
}

// Closest points of two segments p + s*d, s and t clamped to [0, 1]; parallel segments resolve from the first segment's start.
function closestSegmentParameters(ea, eb) {
    const dot3 = (x, y) => x[0] * y[0] + x[1] * y[1] + x[2] * y[2];
    const r = [ea.p[0] - eb.p[0], ea.p[1] - eb.p[1], ea.p[2] - eb.p[2]];
    const aa = dot3(ea.d, ea.d), bb = dot3(eb.d, eb.d), ab = dot3(ea.d, eb.d), ar = dot3(ea.d, r), br = dot3(eb.d, r);
    const denom = aa * bb - ab * ab;
    const clamp = v => Math.min(1, Math.max(0, v));
    let s = denom > 1e-12 * aa * bb ? clamp((ab * br - ar * bb) / denom) : 0;
    let t = (ab * s + br) / bb;
    if (t < 0) { t = 0; s = clamp(-ar / aa); }
    else if (t > 1) { t = 1; s = clamp((ab - ar) / aa); }
    return [s, t];
}

// Separating-axis test over 15 axes, then reference-face clipping of the incident face.
function collideBoxBox(boxA, boxB, margin, out) {
    const u = [boxA.u0, boxA.u1, boxA.u2];
    const v = [boxB.u0, boxB.u1, boxB.u2];
    const ea = [boxA.hx, boxA.hy, boxA.hz];
    const eb = [boxB.hx, boxB.hy, boxB.hz];
    const dp = [boxA.px - boxB.px, boxA.py - boxB.py, boxA.pz - boxB.pz];

    let bestAxis = null, bestSeparation = -Infinity, bestType = -1, bestIndex = -1;
    const consider = (L, type, index) => {
        const rA = ea[0] * Math.abs(u[0][0] * L[0] + u[0][1] * L[1] + u[0][2] * L[2]) +
                   ea[1] * Math.abs(u[1][0] * L[0] + u[1][1] * L[1] + u[1][2] * L[2]) +
                   ea[2] * Math.abs(u[2][0] * L[0] + u[2][1] * L[1] + u[2][2] * L[2]);
        const rB = eb[0] * Math.abs(v[0][0] * L[0] + v[0][1] * L[1] + v[0][2] * L[2]) +
                   eb[1] * Math.abs(v[1][0] * L[0] + v[1][1] * L[1] + v[1][2] * L[2]) +
                   eb[2] * Math.abs(v[2][0] * L[0] + v[2][1] * L[1] + v[2][2] * L[2]);
        const dist = dp[0] * L[0] + dp[1] * L[1] + dp[2] * L[2];
        const separation = Math.abs(dist) - (rA + rB);
        if (separation > bestSeparation) {
            bestSeparation = separation;
            // The contact normal points from B toward A along the separating axis.
            bestAxis = dist >= 0 ? [L[0], L[1], L[2]] : [-L[0], -L[1], -L[2]];
            bestType = type; bestIndex = index;
        }
    };
    for (let i = 0; i < 3; i++) consider(u[i], 0, i);
    for (let i = 0; i < 3; i++) consider(v[i], 1, i);
    for (let i = 0; i < 3; i++) for (let j = 0; j < 3; j++) {
        const c = cross(u[i][0], u[i][1], u[i][2], v[j][0], v[j][1], v[j][2]);
        const l = Math.hypot(c[0], c[1], c[2]);
        if (l > 1e-5) consider([c[0] / l, c[1] / l, c[2] / l], 2, i * 3 + j);
    }
    if (bestAxis === null || bestSeparation > margin) return;
    const n = bestAxis;
    // Identity of this manifold's geometry: axis type and index, reference/incident faces; clip points add their own id below.
    const axisFeature = (bestType << 4) | bestIndex;

    // Reference face: the box face most aligned with the separating axis. A cross axis that is (nearly) a face normal is a
    // face contact and clips normally; only a genuine edge-edge axis takes the single support contact.
    let referenceIsA, refAxis;
    if (bestType === 0) { referenceIsA = true; refAxis = bestIndex; }
    else if (bestType === 1) { referenceIsA = false; refAxis = bestIndex; }
    else {
        let bestDot = -1;
        for (let i = 0; i < 3; i++) {
            const da = Math.abs(u[i][0] * n[0] + u[i][1] * n[1] + u[i][2] * n[2]);
            if (da > bestDot) { bestDot = da; referenceIsA = true; refAxis = i; }
            const db = Math.abs(v[i][0] * n[0] + v[i][1] * n[1] + v[i][2] * n[2]);
            if (db > bestDot) { bestDot = db; referenceIsA = false; refAxis = i; }
        }
        if (bestDot < EDGE_FACE_ALIGNMENT) { supportContact(boxA, boxB, n, bestSeparation, axisFeature * 256 + 255, out); return; }
    }
    const ref = referenceIsA ? boxA : boxB;
    const inc = referenceIsA ? boxB : boxA;
    const refAxes = referenceIsA ? u : v, incAxes = referenceIsA ? v : u;
    const refExt = referenceIsA ? ea : eb, incExt = referenceIsA ? eb : ea;
    // The reference face's outward normal faces the incident box: toward B when A is the reference, toward A otherwise.
    const outward = referenceIsA ? [-n[0], -n[1], -n[2]] : [n[0], n[1], n[2]];
    const refDot = refAxes[refAxis][0] * outward[0] + refAxes[refAxis][1] * outward[1] + refAxes[refAxis][2] * outward[2];
    const refSign = refDot >= 0 ? 1 : -1;
    const refN = [refSign * refAxes[refAxis][0], refSign * refAxes[refAxis][1], refSign * refAxes[refAxis][2]];
    const refCentre = [ref.px + refExt[refAxis] * refN[0], ref.py + refExt[refAxis] * refN[1], ref.pz + refExt[refAxis] * refN[2]];

    // Incident face: the incident box face most anti-parallel to the reference normal.
    let incAxis = 0, incDot = -1;
    for (let i = 0; i < 3; i++) {
        const d = Math.abs(incAxes[i][0] * refN[0] + incAxes[i][1] * refN[1] + incAxes[i][2] * refN[2]);
        if (d > incDot) { incDot = d; incAxis = i; }
    }
    const incAlign = incAxes[incAxis][0] * refN[0] + incAxes[incAxis][1] * refN[1] + incAxes[incAxis][2] * refN[2];
    const incSign = incAlign <= 0 ? 1 : -1;
    const incCentre = [inc.px + incSign * incExt[incAxis] * incAxes[incAxis][0],
        inc.py + incSign * incExt[incAxis] * incAxes[incAxis][1], inc.pz + incSign * incExt[incAxis] * incAxes[incAxis][2]];
    const ia = (incAxis + 1) % 3, ib = (incAxis + 2) % 3;
    // Each polygon vertex carries a geometric id: an incident-face corner (0..3) or, once clipped, the reference side
    // plane that created it combined with the full id of the vertex that started its edge ((4 << plane) + source id), so a
    // point keeps its id across substeps and no two vertices of one manifold share an id (ids stay below 64; 255 is the support fallback).
    let polygon = [];
    [[-1, -1], [1, -1], [1, 1], [-1, 1]].forEach(([sa, sb], corner) =>
        polygon.push({ id: corner, p: [
            incCentre[0] + sa * incExt[ia] * incAxes[ia][0] + sb * incExt[ib] * incAxes[ib][0],
            incCentre[1] + sa * incExt[ia] * incAxes[ia][1] + sb * incExt[ib] * incAxes[ib][1],
            incCentre[2] + sa * incExt[ia] * incAxes[ia][2] + sb * incExt[ib] * incAxes[ib][2]
        ] }));

    const featureBase = (axisFeature << 8) | ((referenceIsA ? 0 : 1) << 7) | (refAxis << 5) | ((refSign > 0 ? 0 : 1) << 4) | (incAxis << 2) | ((incSign > 0 ? 0 : 1) << 1);
    // Sutherland-Hodgman clip against the four side planes of the reference face.
    let plane = 0;
    for (const side of [(refAxis + 1) % 3, (refAxis + 2) % 3]) {
        for (const sign of [1, -1]) {
            const axis = refAxes[side];
            const limit = refExt[side];
            const clipped = [];
            for (let i = 0; i < polygon.length; i++) {
                const p = polygon[i], q = polygon[(i + 1) % polygon.length];
                const dpp = sign * ((p.p[0] - refCentre[0]) * axis[0] + (p.p[1] - refCentre[1]) * axis[1] + (p.p[2] - refCentre[2]) * axis[2]) - limit;
                const dq = sign * ((q.p[0] - refCentre[0]) * axis[0] + (q.p[1] - refCentre[1]) * axis[1] + (q.p[2] - refCentre[2]) * axis[2]) - limit;
                if (dpp <= 0) clipped.push(p);
                if ((dpp <= 0) !== (dq <= 0)) {
                    const t = dpp / (dpp - dq);
                    clipped.push({ id: (4 << plane) + p.id, p: [p.p[0] + t * (q.p[0] - p.p[0]), p.p[1] + t * (q.p[1] - p.p[1]), p.p[2] + t * (q.p[2] - p.p[2])] });
                }
            }
            polygon = clipped;
            plane++;
            if (polygon.length === 0) { supportContact(boxA, boxB, n, bestSeparation, featureBase * 256 + 255, out); return; }
        }
    }

    const candidates = [];
    for (const { id, p } of polygon) {
        const gap = (p[0] - refCentre[0]) * refN[0] + (p[1] - refCentre[1]) * refN[1] + (p[2] - refCentre[2]) * refN[2];
        if (gap > margin) continue;
        const mid = [p[0] - gap * 0.5 * refN[0], p[1] - gap * 0.5 * refN[1], p[2] - gap * 0.5 * refN[2]];
        candidates.push(contactPoint(boxA.body, boxB.body, boxA, boxB, n[0], n[1], n[2], mid[0], mid[1], mid[2], gap, featureBase * 256 + id));
    }
    if (candidates.length === 0) { supportContact(boxA, boxB, n, bestSeparation, featureBase * 256 + 255, out); return; }
    for (const p of reduceManifold(candidates, n[0], n[1], n[2])) out.push(p);
}

// One generic Box2D v3 TGS Soft point constraint: lever arms, effective masses and a stable tangent basis.
function prepareContact(c, cache) {
    const A = c.a, B = c.b;
    c.rAx = c.px - A.cmx; c.rAy = c.py - A.cmy; c.rAz = c.pz - A.cmz;
    c.rBx = c.px - B.cmx; c.rBy = c.py - B.cmy; c.rBz = c.pz - B.cmz;
    const nx = c.nx, ny = c.ny, nz = c.nz;
    // Tangent basis derived from the normal alone so matching features keep their friction impulses.
    let ax = 0, ay = 0, az = 0;
    if (Math.abs(nx) <= Math.abs(ny) && Math.abs(nx) <= Math.abs(nz)) ax = 1;
    else if (Math.abs(ny) <= Math.abs(nz)) ay = 1;
    else az = 1;
    const t1 = cross(nx, ny, nz, ax, ay, az);
    const l1 = Math.hypot(t1[0], t1[1], t1[2]);
    c.t1 = [t1[0] / l1, t1[1] / l1, t1[2] / l1];
    c.t2 = cross(nx, ny, nz, c.t1[0], c.t1[1], c.t1[2]);
    c.normalMass = effectiveMass(c, nx, ny, nz);
    c.tangentMass1 = effectiveMass(c, c.t1[0], c.t1[1], c.t1[2]);
    c.tangentMass2 = effectiveMass(c, c.t2[0], c.t2[1], c.t2[2]);
    const vA = pointVelocity(A, c.rAx, c.rAy, c.rAz), vB = pointVelocity(B, c.rBx, c.rBy, c.rBz);
    const vn = (vA[0] - vB[0]) * nx + (vA[1] - vB[1]) * ny + (vA[2] - vB[2]) * nz;
    c.restitution = c.colA.restitution * c.colB.restitution;
    c.threshold = Math.max(c.colA.bounceThreshold, c.colB.bounceThreshold);
    c.friction = Math.sqrt(c.colA.friction * c.colB.friction);
    const lo = Math.min(c.colA.index, c.colB.index), hi = Math.max(c.colA.index, c.colB.index);
    c.key = lo + ':' + hi + ':' + c.feature;
    const warm = cache.get(c.key);
    c.lambdaN = warm ? warm.lambdaN : 0;
    c.lambdaT1 = warm ? warm.lambdaT1 : 0;
    c.lambdaT2 = warm ? warm.lambdaT2 : 0;
    // The approach speed is the deepest pre-solve closing speed seen since the contact appeared: a speculative substep
    // trims the approach before the surfaces meet, so the touching substep alone would under-read the impact.
    c.relVel = Math.min(vn, warm ? warm.approach : 0);
    c.maxImpulse = 0;
    c.bounced = false;
}

function effectiveMass(c, dx, dy, dz) {
    const A = c.a, B = c.b;
    let k = (A.motion === 1 ? A.invMass : 0) + (B.motion === 1 ? B.invMass : 0);
    if (A.motion === 1) {
        const r = cross(c.rAx, c.rAy, c.rAz, dx, dy, dz);
        const w = applyInvInertia(A, r[0], r[1], r[2]);
        k += r[0] * w[0] + r[1] * w[1] + r[2] * w[2];
    }
    if (B.motion === 1) {
        const r = cross(c.rBx, c.rBy, c.rBz, dx, dy, dz);
        const w = applyInvInertia(B, r[0], r[1], r[2]);
        k += r[0] * w[0] + r[1] * w[1] + r[2] * w[2];
    }
    return k > 0 ? 1 / k : 0;
}

function warmStartContact(c) {
    const px = c.lambdaN * c.nx + c.lambdaT1 * c.t1[0] + c.lambdaT2 * c.t2[0];
    const py = c.lambdaN * c.ny + c.lambdaT1 * c.t1[1] + c.lambdaT2 * c.t2[1];
    const pz = c.lambdaN * c.nz + c.lambdaT1 * c.t1[2] + c.lambdaT2 * c.t2[2];
    applyImpulse(c.a, px, py, pz, c.rAx, c.rAy, c.rAz);
    applyImpulse(c.b, -px, -py, -pz, c.rBx, c.rBy, c.rBz);
}

function relativeVelocity(c) {
    const vA = pointVelocity(c.a, c.rAx, c.rAy, c.rAz), vB = pointVelocity(c.b, c.rBx, c.rBy, c.rBz);
    return [vA[0] - vB[0], vA[1] - vB[1], vA[2] - vB[2]];
}

// Separation at the current body poses: the manifold gap plus the normal component of each anchor's
// displacement since the manifold was built, so the relax pass sees the integrated positions (TGS).
function anchorDisplacement(b, rx, ry, rz) {
    if (b.motion !== 1) return [0, 0, 0];
    const dq = quatMultiply([b.qx, b.qy, b.qz, b.qw], [-b.q0[0], -b.q0[1], -b.q0[2], b.q0[3]]);
    const r = rotateVector(dq[0], dq[1], dq[2], dq[3], rx, ry, rz);
    return [b.cmx - b.cm0[0] + r[0] - rx, b.cmy - b.cm0[1] + r[1] - ry, b.cmz - b.cm0[2] + r[2] - rz];
}

function currentSeparation(c) {
    const dA = anchorDisplacement(c.a, c.rAx, c.rAy, c.rAz), dB = anchorDisplacement(c.b, c.rBx, c.rBy, c.rBz);
    return c.gap + (dA[0] - dB[0]) * c.nx + (dA[1] - dB[1]) * c.ny + (dA[2] - dB[2]) * c.nz;
}

// Coulomb friction row: two tangent axes with a circular cone on the accumulated impulse.
function solveFriction(c) {
    const v = relativeVelocity(c);
    const vt1 = v[0] * c.t1[0] + v[1] * c.t1[1] + v[2] * c.t1[2];
    const vt2 = v[0] * c.t2[0] + v[1] * c.t2[1] + v[2] * c.t2[2];
    let n1 = c.lambdaT1 - c.tangentMass1 * vt1;
    let n2 = c.lambdaT2 - c.tangentMass2 * vt2;
    const maxFriction = c.friction * c.lambdaN;
    const length = Math.hypot(n1, n2);
    if (length > maxFriction) {
        const scale = length > 0 ? maxFriction / length : 0;
        n1 *= scale; n2 *= scale;
    }
    const d1 = n1 - c.lambdaT1, d2 = n2 - c.lambdaT2;
    c.lambdaT1 = n1; c.lambdaT2 = n2;
    const px = d1 * c.t1[0] + d2 * c.t2[0], py = d1 * c.t1[1] + d2 * c.t2[1], pz = d1 * c.t1[2] + d2 * c.t2[2];
    applyImpulse(c.a, px, py, pz, c.rAx, c.rAy, c.rAz);
    applyImpulse(c.b, -px, -py, -pz, c.rBx, c.rBy, c.rBz);
}

// Rolling resistance row at every contact manifold, static or dynamic partner: one angular impulse opposing the two bodies'
// relative rolling about the tangent axes, applied equal and opposite to both (Box2D v3 coefficient mixing, but not warm-started:
// the accumulated impulse restarts from zero every substep). The larger declared coefficient of the pair (material record +14)
// times the larger sphere radius times the manifold's normal impulse bounds it, and the row targets zero relative rolling, so it
// brings rolling to rest and never reverses it. Boxes, planes and walls declare zero and have no radius, so a pair without a
// sphere carries none; spin about the normal and free flight are untouched.
function prepareRolling(m) {
    const c = m.points[0];
    const radiusA = c.colA.shapeKind === 0 ? c.colA.radius : 0, radiusB = c.colB.shapeKind === 0 ? c.colB.radius : 0;
    m.rolling = Math.max(c.colA.rollingResistance, c.colB.rollingResistance) * Math.max(radiusA, radiusB);
    m.rollingMass1 = m.rolling > 0 ? angularMass(c, c.t1) : 0;
    m.rollingMass2 = m.rolling > 0 ? angularMass(c, c.t2) : 0;
    m.lambdaR1 = 0; m.lambdaR2 = 0;
}

function angularMass(c, t) {
    let k = 0;
    for (const b of [c.a, c.b]) {
        if (b.motion !== 1) continue;
        const w = applyInvInertia(b, t[0], t[1], t[2]);
        k += t[0] * w[0] + t[1] * w[1] + t[2] * w[2];
    }
    return k > 0 ? 1 / k : 0;
}

function solveRolling(m) {
    if (m.rollingMass1 === 0 && m.rollingMass2 === 0) return;
    const c = m.points[0];
    let normal = 0;
    for (const p of m.points) normal += p.lambdaN;
    const wx = c.a.wx - c.b.wx, wy = c.a.wy - c.b.wy, wz = c.a.wz - c.b.wz;
    let n1 = m.lambdaR1 - m.rollingMass1 * (wx * c.t1[0] + wy * c.t1[1] + wz * c.t1[2]);
    let n2 = m.lambdaR2 - m.rollingMass2 * (wx * c.t2[0] + wy * c.t2[1] + wz * c.t2[2]);
    const maxRolling = m.rolling * normal;
    const length = Math.hypot(n1, n2);
    if (length > maxRolling) {
        const scale = length > 0 ? maxRolling / length : 0;
        n1 *= scale; n2 *= scale;
    }
    const d1 = n1 - m.lambdaR1, d2 = n2 - m.lambdaR2;
    m.lambdaR1 = n1; m.lambdaR2 = n2;
    const tx = d1 * c.t1[0] + d2 * c.t2[0], ty = d1 * c.t1[1] + d2 * c.t2[1], tz = d1 * c.t1[2] + d2 * c.t2[2];
    applyAngularImpulse(c.a, tx, ty, tz);
    applyAngularImpulse(c.b, -tx, -ty, -tz);
}

function applyAngularImpulse(b, x, y, z) {
    if (b.motion !== 1) return;
    const dw = applyInvInertia(b, x, y, z);
    b.wx += dw[0]; b.wy += dw[1]; b.wz += dw[2];
}

// Soft normal row with a speculative target velocity when separated (projected, one point).
function solveNormalRow(c, useBias, soft, h) {
    const v = relativeVelocity(c);
    const vn = v[0] * c.nx + v[1] * c.ny + v[2] * c.nz;
    const s = currentSeparation(c) + CONTACT_SLOP;
    let bias = 0, massScale = 1, impulseScale = 0;
    if (s > 0) bias = s / h;
    else if (useBias) {
        bias = Math.max(soft.biasRate * s, -MAX_PUSHOUT_SPEED);
        massScale = soft.massScale; impulseScale = soft.impulseScale;
    }
    let impulse = -c.normalMass * massScale * (vn + bias) - impulseScale * c.lambdaN;
    const total = Math.max(c.lambdaN + impulse, 0);
    impulse = total - c.lambdaN;
    c.lambdaN = total;
    if (total > c.maxImpulse) c.maxImpulse = total;
    applyImpulse(c.a, impulse * c.nx, impulse * c.ny, impulse * c.nz, c.rAx, c.rAy, c.rAz);
    applyImpulse(c.b, -impulse * c.nx, -impulse * c.ny, -impulse * c.nz, c.rBx, c.rBy, c.rBz);
}

// Delassus coupling between two normal rows of the same pair: J_i M^-1 J_j^T.
function couplingMass(ci, cj) {
    const A = ci.a, B = ci.b;
    let k = (A.motion === 1 ? A.invMass : 0) + (B.motion === 1 ? B.invMass : 0);
    if (A.motion === 1) {
        const ri = cross(ci.rAx, ci.rAy, ci.rAz, ci.nx, ci.ny, ci.nz);
        const rj = cross(cj.rAx, cj.rAy, cj.rAz, cj.nx, cj.ny, cj.nz);
        const w = applyInvInertia(A, rj[0], rj[1], rj[2]);
        k += ri[0] * w[0] + ri[1] * w[1] + ri[2] * w[2];
    }
    if (B.motion === 1) {
        const ri = cross(ci.rBx, ci.rBy, ci.rBz, ci.nx, ci.ny, ci.nz);
        const rj = cross(cj.rBx, cj.rBy, cj.rBz, cj.nx, cj.ny, cj.nz);
        const w = applyInvInertia(B, rj[0], rj[1], rj[2]);
        k += ri[0] * w[0] + ri[1] * w[1] + ri[2] * w[2];
    }
    return k;
}

// Gaussian elimination with partial pivoting for n <= 4; null when singular.
function solveLinear(matrix, rhs, n) {
    const a = matrix.slice(), b = rhs.slice();
    for (let col = 0; col < n; col++) {
        let pivot = col;
        for (let row = col + 1; row < n; row++) if (Math.abs(a[row * n + col]) > Math.abs(a[pivot * n + col])) pivot = row;
        if (Math.abs(a[pivot * n + col]) < 1e-12) return null;
        if (pivot !== col) {
            for (let k = 0; k < n; k++) { const t = a[col * n + k]; a[col * n + k] = a[pivot * n + k]; a[pivot * n + k] = t; }
            const t = b[col]; b[col] = b[pivot]; b[pivot] = t;
        }
        for (let row = col + 1; row < n; row++) {
            const f = a[row * n + col] / a[col * n + col];
            for (let k = col; k < n; k++) a[row * n + k] -= f * a[col * n + k];
            b[row] -= f * b[col];
        }
    }
    const x = new Array(n);
    for (let row = n - 1; row >= 0; row--) {
        let sum = b[row];
        for (let k = row + 1; k < n; k++) sum -= a[row * n + k] * x[k];
        x[row] = sum / a[row * n + row];
    }
    return x;
}

// Manifold block solve: the normal rows of one pair share a normal, so their Delassus matrix is solved jointly
// (Gaussian elimination, n <= 4). A symmetric landing then receives symmetric impulses in one step instead of
// the slow sequential convergence that spins a thin box. If any accumulated impulse would go negative or the
// system is singular, the rows fall back to sequential projected solves.
function solveNormalBlock(group, useBias, soft, h) {
    const n = group.length;
    if (n === 1) { solveNormalRow(group[0], useBias, soft, h); return; }
    const rhs = new Array(n), scale = new Array(n), impulseScale = new Array(n), matrix = new Array(n * n);
    for (let i = 0; i < n; i++) {
        const c = group[i];
        const v = relativeVelocity(c);
        const vn = v[0] * c.nx + v[1] * c.ny + v[2] * c.nz;
        const s = currentSeparation(c) + CONTACT_SLOP;
        let bias = 0; scale[i] = 1; impulseScale[i] = 0;
        if (s > 0) bias = s / h;
        else if (useBias) { bias = Math.max(soft.biasRate * s, -MAX_PUSHOUT_SPEED); scale[i] = soft.massScale; impulseScale[i] = soft.impulseScale; }
        rhs[i] = -(vn + bias);
        for (let j = 0; j < n; j++) matrix[i * n + j] = couplingMass(c, group[j]);
    }
    // Four coplanar points against one body span three normal DOF, so regularize the diagonal before solving.
    let trace = 0;
    for (let i = 0; i < n; i++) trace += matrix[i * n + i];
    for (let i = 0; i < n; i++) matrix[i * n + i] += BLOCK_REGULARIZATION * trace / n;
    const solution = solveLinear(matrix, rhs, n);
    if (solution === null) { for (const c of group) solveNormalRow(c, useBias, soft, h); return; }
    const deltas = new Array(n);
    for (let i = 0; i < n; i++) {
        deltas[i] = scale[i] * solution[i] - impulseScale[i] * group[i].lambdaN;
        if (group[i].lambdaN + deltas[i] < 0) { for (const c of group) solveNormalRow(c, useBias, soft, h); return; }
    }
    for (let i = 0; i < n; i++) {
        const c = group[i];
        c.lambdaN += deltas[i];
        if (c.lambdaN > c.maxImpulse) c.maxImpulse = c.lambdaN;
        applyImpulse(c.a, deltas[i] * c.nx, deltas[i] * c.ny, deltas[i] * c.nz, c.rAx, c.rAy, c.rAz);
        applyImpulse(c.b, -deltas[i] * c.nx, -deltas[i] * c.ny, -deltas[i] * c.nz, c.rBx, c.rBy, c.rBz);
    }
}

// One sweep: per manifold, friction rows, the joint normal solve, then the rolling-resistance row bounded by the updated normal impulse.
function sweepManifolds(manifolds, useBias, soft, h) {
    for (const m of manifolds) {
        for (const c of m.points) solveFriction(c);
        solveNormalBlock(m.points, useBias, soft, h);
        solveRolling(m);
    }
}

// Dissipative restitution from the pre-solve approach speed, once the surfaces have actually met; a speculative
// row that only trimmed the approach must not bounce early, which would hand one corner the whole impact.
function applyRestitution(c) {
    if (c.restitution === 0 || c.relVel > -c.threshold || c.maxImpulse === 0 || currentSeparation(c) > 0) return;
    c.bounced = true;
    const v = relativeVelocity(c);
    const vn = v[0] * c.nx + v[1] * c.ny + v[2] * c.nz;
    let impulse = -c.normalMass * (vn + c.restitution * c.relVel);
    const total = Math.max(c.lambdaN + impulse, 0);
    impulse = total - c.lambdaN;
    c.lambdaN = total;
    if (total > c.maxImpulse) c.maxImpulse = total;
    applyImpulse(c.a, impulse * c.nx, impulse * c.ny, impulse * c.nz, c.rAx, c.rAy, c.rAz);
    applyImpulse(c.b, -impulse * c.nx, -impulse * c.ny, -impulse * c.nz, c.rBx, c.rBy, c.rBz);
}

function generateManifold(colA, colB, margin, out) {
    const a = colA.shapeKind, b = colB.shapeKind;
    if (a === 0 && b === 0) collideSphereSphere(colA, colB, margin, out);
    else if (a === 0 && b === 2) collideSpherePlane(colA, colB, margin, out);
    else if (a === 2 && b === 0) collideSpherePlane(colB, colA, margin, out);
    else if (a === 1 && b === 0) collideBoxSphere(colA, colB, margin, out);
    else if (a === 0 && b === 1) collideBoxSphere(colB, colA, margin, out);
    else if (a === 1 && b === 2) collideBoxPlane(colA, colB, margin, out);
    else if (a === 2 && b === 1) collideBoxPlane(colB, colA, margin, out);
    else if (a === 1 && b === 1) collideBoxBox(colA, colB, margin, out);
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

    const orientationOffset = 19744;
    const motionOffset = 20768;
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
            cmx: px, cmy: py, cmz: pz,
            qx, qy, qz, qw,
            vx: 0, vy: 0, vz: 0, wx: 0, wy: 0, wz: 0,
            invMass: 0,
            motion: 0
        });
    }

    // Collect dynamic bodies: mass, centre of mass, principal frame and inertia come only from the compiled record.
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
        // Committed linear (m/s) and angular (rad/s) velocity are f32; the declarations after them stay binary16.
        const vx = view.getFloat32(bodyOffset + 48, true);
        const vy = view.getFloat32(bodyOffset + 52, true);
        const vz = view.getFloat32(bodyOffset + 56, true);
        const wx = view.getFloat32(bodyOffset + 60, true);
        const wy = view.getFloat32(bodyOffset + 64, true);
        const wz = view.getFloat32(bodyOffset + 68, true);
        const mass = getF16(view, bodyOffset + 72);
        const drag = getF16(view, bodyOffset + 74);
        const gx = getF16(view, bodyOffset + 76);
        const gy = getF16(view, bodyOffset + 78);
        const gz = getF16(view, bodyOffset + 80);
        const comLocal = [getF16(view, bodyOffset + 82), getF16(view, bodyOffset + 84), getF16(view, bodyOffset + 86)];
        const principal = [getF16(view, bodyOffset + 88), getF16(view, bodyOffset + 90), getF16(view, bodyOffset + 92), getF16(view, bodyOffset + 94)];
        const inertia = [readPrincipalInertia(view, bodyOffset + 96), readPrincipalInertia(view, bodyOffset + 104), readPrincipalInertia(view, bodyOffset + 112)];

        const px = (cellX + localX) / 16.0;
        const py = (cellY + localY) / 16.0;
        const pz = (cellZ + localZ) / 16.0;

        const body = {
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
            mass, invMass: 1.0 / mass,
            gx, gy, gz, drag,
            comLocal, principal,
            // A zero or non-finite declared moment reads as infinite inertia (no rotation) rather than an infinite inverse.
            invI: inertia.map(moment => Number.isFinite(moment) && moment > 0 ? 1.0 / moment : 0),
            invIWorld: [0, 0, 0, 0, 0, 0, 0, 0, 0],
            cmx: px, cmy: py, cmz: pz,
            supportExtent: 0,
            dynamicIndex: dynamicBodies.length,
            motion: 1
        };
        updateDynamicFrame(body);
        dynamicBodies.push(body);
    }

    const allBodies = new Map();
    for (let b of staticBodies) allBodies.set(b.bodySlot, b);
    for (let b of dynamicBodies) allBodies.set(b.bodySlot, b);

    // Collect colliders; materials are the declared values with no substitution.
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
        const restitution = getF16(view, matOffset + 8);
        const bounceThreshold = getF16(view, matOffset + 10);
        const friction = getF16(view, matOffset + 12);
        const rollingResistance = getF16(view, matOffset + 14);

        const body = allBodies.get(bodySlot);
        if (!body) continue;

        const collider = {
            index: c,
            bodySlot,
            body,
            materialSlot,
            shapeKind,
            tx, ty, tz,
            rq: (rqx === 0 && rqy === 0 && rqz === 0) ? [0, 0, 0, 1] : [rqx, rqy, rqz, rqw],
            px: 0, py: 0, pz: 0,
            qx: 0, qy: 0, qz: 0, qw: 1,
            u0: [1, 0, 0], u1: [0, 1, 0], u2: [0, 0, 1],
            radius,
            hx, hy, hz,
            restitution,
            bounceThreshold,
            friction,
            rollingResistance
        };
        refreshCollider(collider);
        colliders.push(collider);
        if (body.motion === 1) {
            body.collider = collider;
            // Guides compare the body's lowest extent with their support height.
            body.supportExtent = shapeKind === 0 ? radius :
                hx * Math.abs(collider.u0[1]) + hy * Math.abs(collider.u1[1]) + hz * Math.abs(collider.u2[1]);
        }
    }

    const dt = SUBSTEP_SECONDS;
    const soft = makeSoft(CONTACT_HERTZ, CONTACT_DAMPING_RATIO, dt);

    // Build DynamicBVH with velocity fattening (linear and angular displacement over the tick).
    const bvh = new DynamicBVH();
    const tickTime = dt * steps;
    const VELOCITY_SLOP = 0.05;

    for (let c of colliders) {
        let minX, minY, minZ, maxX, maxY, maxZ;
        const b = c.body;
        const isDyn = b.motion === 1;
        const spin = isDyn ? Math.hypot(b.wx, b.wy, b.wz) * colliderExtent(c) * tickTime : 0;
        const fatX = isDyn ? (Math.abs(b.vx) + Math.abs(b.gx) * tickTime) * tickTime + spin + VELOCITY_SLOP : VELOCITY_SLOP;
        const fatY = isDyn ? (Math.abs(b.vy) + Math.abs(b.gy) * tickTime) * tickTime + spin + VELOCITY_SLOP : VELOCITY_SLOP;
        const fatZ = isDyn ? (Math.abs(b.vz) + Math.abs(b.gz) * tickTime) * tickTime + spin + VELOCITY_SLOP : VELOCITY_SLOP;

        if (c.shapeKind === 2) {
            minX = -100; maxX = 100;
            minY = -100; maxY = c.py + VELOCITY_SLOP;
            minZ = -100; maxZ = 100;
        } else if (c.shapeKind === 0) {
            const r = c.radius;
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

    // Declared contact triggers and finite contact-work stores on a static owner respond to any dynamic body.
    const emitContactEvents = (colStatic, bDyn, approach, normalTowardDyn, s) => {
        const bStatic = colStatic.body;
        const triggerCount = view.getUint32(100, true);
        for (let t = 0; t < triggerCount; t++) {
            const tOffset = 14624 + t * 64;
            const owner = view.getUint32(tOffset + 8, true);
            if (owner !== bStatic.bodySlot) continue;
            const kind = view.getUint32(tOffset + 12, true);
            const named = view.getUint32(tOffset + 24, true);
            if (kind === 1 && named !== bDyn.bodySlot) continue;
            const threshold = getF16(view, tOffset + 16);
            if (approach >= threshold) {
                const curCount = view.getUint32(tOffset + 32, true);
                if (curCount === 0) {
                    view.setUint32(tOffset + 32, 1, true);
                    view.setUint32(tOffset + 36, colStatic.index, true);
                    view.setUint32(tOffset + 40, sourceOrdinal + s + 1, true);
                    setF16(view, tOffset + 44, 0);
                    setF16(view, tOffset + 46, approach);
                    view.setUint32(tOffset + 48, bDyn.bodySlot, true);
                }
            }
        }
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
            if (approach < threshold) continue;
            for (let o = 0; o < workOccCount; o++) {
                const oOffset = 15648 + o * 32;
                const occWork = view.getUint16(oOffset, true);
                const targetLow = view.getUint32(oOffset + 4, true);
                const targetHigh = view.getUint32(oOffset + 8, true);
                if (occWork !== w || targetLow !== bDyn.bodyIdLow || targetHigh !== bDyn.bodyIdHigh) continue;
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

                    const targetSpeed = getF16(view, wOffset + 16);
                    bDyn.vx = normalTowardDyn[0] * targetSpeed;
                    bDyn.vy = normalTowardDyn[1] * targetSpeed;
                    bDyn.vz = normalTowardDyn[2] * targetSpeed;
                }
                break;
            }
        }
    };

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
                           loc[1] - targetBody.supportExtent >= supportHeight - supportMargin &&
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

        // 2. Symplectic Euler gravity integration about the centre of mass, then the declared linear drag (body record +74) as an
        //    exact exponential decay of the linear velocity; angular velocity is never damped here.
        for (let b of dynamicBodies) {
            b.vx += b.gx * dt;
            b.vy += b.gy * dt;
            b.vz += b.gz * dt;
            const decay = Math.exp(-b.drag * dt);
            b.vx *= decay; b.vy *= decay; b.vz *= decay;
        }

        // 3. Narrowphase: one manifold per candidate pair from the current poses, up to four points each
        for (let c of colliders) if (c.body.motion === 1) refreshCollider(c);
        const contacts = [];
        const manifolds = [];
        for (let [colA, colB] of candidatePairs) {
            // Stable pair order keeps the normal and tangent signs of a cached key identical between ticks.
            if (colA.index > colB.index) { const swap = colA; colA = colB; colB = swap; }
            const bA = colA.body, bB = colB.body;
            const relSpeed = Math.hypot(bA.vx - bB.vx, bA.vy - bB.vy, bA.vz - bB.vz) +
                Math.hypot(bA.wx, bA.wy, bA.wz) * colliderExtent(colA) + Math.hypot(bB.wx, bB.wy, bB.wz) * colliderExtent(colB);
            const first = contacts.length;
            generateManifold(colA, colB, SPECULATIVE_SLOP + relSpeed * dt, contacts);
            if (contacts.length > first) manifolds.push({ points: contacts.slice(first) });
        }
        for (const c of contacts) prepareContact(c, contactCache);
        for (const m of manifolds) prepareRolling(m);
        for (let b of dynamicBodies) { b.cm0 = [b.cmx, b.cmy, b.cmz]; b.q0 = [b.qx, b.qy, b.qz, b.qw]; }

        // 4. TGS Soft: warm start, biased solve, position integration, relax, restitution
        for (const c of contacts) warmStartContact(c);
        for (let iteration = 0; iteration < SOLVER_ITERATIONS; iteration++) sweepManifolds(manifolds, true, soft, dt);

        for (let b of dynamicBodies) {
            clampVelocity(b);

            const before = { cellX: b.cellX, cellY: b.cellY, cellZ: b.cellZ, localX: b.localX, localY: b.localY, localZ: b.localZ,
                qx: b.qx, qy: b.qy, qz: b.qz, qw: b.qw };
            // Orientation advances from the angular velocity; the origin follows the centre of mass.
            let nqx = b.qx, nqy = b.qy, nqz = b.qz, nqw = b.qw;
            const wLen = Math.hypot(b.wx, b.wy, b.wz);
            if (wLen > 1e-9) {
                const angle = wLen * dt;
                const sAngle = Math.sin(angle * 0.5) / wLen;
                const cAngle = Math.cos(angle * 0.5);
                const nq = quatMultiply([b.wx * sAngle, b.wy * sAngle, b.wz * sAngle, cAngle], [b.qx, b.qy, b.qz, b.qw]);
                const nqLen = Math.hypot(nq[0], nq[1], nq[2], nq[3]);
                nqx = nq[0] / nqLen; nqy = nq[1] / nqLen; nqz = nq[2] / nqLen; nqw = nq[3] / nqLen;
            }
            const cmx = b.cmx + b.vx * dt, cmy = b.cmy + b.vy * dt, cmz = b.cmz + b.vz * dt;
            const offset = rotateVector(nqx, nqy, nqz, nqw, b.comLocal[0], b.comLocal[1], b.comLocal[2]);
            const nx = cmx - offset[0], ny = cmy - offset[1], nz = cmz - offset[2];
            b.localX += (nx - b.px) * 16.0;
            b.localY += (ny - b.py) * 16.0;
            b.localZ += (nz - b.pz) * 16.0;
            b.qx = nqx; b.qy = nqy; b.qz = nqz; b.qw = nqw;

            const carryX = Math.floor(b.localX + 0.5);
            const carryY = Math.floor(b.localY + 0.5);
            const carryZ = Math.floor(b.localZ + 0.5);
            b.cellX += carryX; b.localX -= carryX;
            b.cellY += carryY; b.localY -= carryY;
            b.cellZ += carryZ; b.localZ -= carryZ;
            if (toHalf(b.localX) >= 0.5) { b.cellX++; b.localX -= 1; }
            if (toHalf(b.localY) >= 0.5) { b.cellY++; b.localY -= 1; }
            if (toHalf(b.localZ) >= 0.5) { b.cellZ++; b.localZ -= 1; }

            b.px = (b.cellX + b.localX) / 16.0;
            b.py = (b.cellY + b.localY) / 16.0;
            b.pz = (b.cellZ + b.localZ) / 16.0;

            // Game-grade: a non-finite candidate keeps the previous pose and drops its motion; the tick continues.
            if (!Number.isFinite(b.px + b.py + b.pz + b.qx + b.qy + b.qz + b.qw + b.vx + b.vy + b.vz + b.wx + b.wy + b.wz)) {
                Object.assign(b, before);
                b.px = (b.cellX + b.localX) / 16.0; b.py = (b.cellY + b.localY) / 16.0; b.pz = (b.cellZ + b.localZ) / 16.0;
                b.vx = b.vy = b.vz = b.wx = b.wy = b.wz = 0;
            }
            updateDynamicFrame(b);
        }

        for (let iteration = 0; iteration < RELAX_ITERATIONS; iteration++) sweepManifolds(manifolds, false, soft, dt);
        for (const c of contacts) applyRestitution(c);
        // Game-grade: a non-finite velocity produced by the final sweeps drops the motion; the finite pose stands. A finite one
        // is clamped again so the relax and restitution passes cannot commit a speed beyond the envelope.
        for (let b of dynamicBodies)
            if (!Number.isFinite(b.vx + b.vy + b.vz + b.wx + b.wy + b.wz)) b.vx = b.vy = b.vz = b.wx = b.wy = b.wz = 0;
            else clampVelocity(b);

        // Persist accumulated impulses by pair and feature for the next substep's warm start.
        contactCache.clear();
        // The carried approach is consumed by a bounce or by reaching the surface; a resting contact carries none.
        for (const c of contacts) contactCache.set(c.key, { lambdaN: c.lambdaN, lambdaT1: c.lambdaT1, lambdaT2: c.lambdaT2, approach: (c.bounced || currentSeparation(c) <= 0) ? 0 : c.relVel });

        // Declared static-owner responses to a qualifying approach (first touching point per manifold wins).
        for (const c of contacts) {
            if (c.maxImpulse <= 0 || currentSeparation(c) > 0) continue;
            if (c.a.motion === 1 && c.b.motion === 0) emitContactEvents(c.colB, c.a, -c.relVel, [c.nx, c.ny, c.nz], s);
            else if (c.b.motion === 1 && c.a.motion === 0) emitContactEvents(c.colA, c.b, -c.relVel, [-c.nx, -c.ny, -c.nz], s);
        }

        // Declared orientation-threshold sensors sample the substep endpoint: |<q, q0>| at or below the declared cosine
        // half-angle fires once; the fired state is sticky until a fresh admission rearms it.
        const orientationCount = view.getUint32(116, true);
        for (let o = 0; o < orientationCount; o++) {
            const oOffset = orientationOffset + o * 64;
            if (view.getUint32(oOffset + 32, true) !== 0) continue;
            const sensedSlot = view.getUint32(oOffset + 8, true);
            const sensed = dynamicBodies.find(body => body.bodySlot === sensedSlot);
            if (!sensed) continue;
            const alignment = Math.abs(sensed.qx * getF16(view, oOffset + 16) + sensed.qy * getF16(view, oOffset + 18) +
                sensed.qz * getF16(view, oOffset + 20) + sensed.qw * getF16(view, oOffset + 22));
            if (alignment <= getF16(view, oOffset + 24)) {
                view.setUint32(oOffset + 32, 1, true);
                view.setUint32(oOffset + 36, sourceOrdinal + s + 1, true);
                setF16(view, oOffset + 40, 0);
            }
        }

        // Motion piece recording
        for (let b of dynamicBodies) {
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
            pieceView.setFloat32(64, b.vx, true);
            pieceView.setFloat32(68, b.vy, true);
            pieceView.setFloat32(72, b.vz, true);
            pieceView.setFloat32(76, b.wx, true);
            pieceView.setFloat32(80, b.wy, true);
            pieceView.setFloat32(84, b.wz, true);
            setF16(pieceView, 88, b.gx);
            setF16(pieceView, 90, b.gy);
            setF16(pieceView, 92, b.gz);
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
        view.setFloat32(b.bodyOffset + 48, b.vx, true);
        view.setFloat32(b.bodyOffset + 52, b.vy, true);
        view.setFloat32(b.bodyOffset + 56, b.vz, true);
        view.setFloat32(b.bodyOffset + 60, b.wx, true);
        view.setFloat32(b.bodyOffset + 64, b.wy, true);
        view.setFloat32(b.bodyOffset + 68, b.wz, true);
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
        // PhysicsBodyWire (64 B): cell 8..20, local 20..26 and rotation 26..34 (Half), linear velocity m/s 40..52 (f32).
        if (i < bodyCount && 128 + (i + 1) * 64 <= bytes.length) {
            const src = 128 + i * 64;
            const bodyId = view.getUint32(src, true);
            const cellX = view.getInt32(src + 8, true);
            const cellY = view.getInt32(src + 12, true);
            const cellZ = view.getInt32(src + 16, true);
            const localX = getF16(view, src + 20);
            const localY = getF16(view, src + 22);
            const localZ = getF16(view, src + 24);
            const qx = getF16(view, src + 26);
            const qy = getF16(view, src + 28);
            const qz = getF16(view, src + 30);
            const qw = getF16(view, src + 32);
            const velX = view.getFloat32(src + 40, true);
            const velY = view.getFloat32(src + 44, true);
            const velZ = view.getFloat32(src + 48, true);

            poseFloatView[b + 0] = (cellX + localX) / 16;
            poseFloatView[b + 1] = (cellY + localY) / 16;
            poseFloatView[b + 2] = (cellZ + localZ) / 16;
            poseDataView.setUint32((b + 3) * 4, bodyId, true);
            poseFloatView[b + 4] = qx;
            poseFloatView[b + 5] = qy;
            poseFloatView[b + 6] = qz;
            poseFloatView[b + 7] = qw;
            poseFloatView[b + 8] = velX;
            poseFloatView[b + 9] = velY;
            poseFloatView[b + 10] = velZ;
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
