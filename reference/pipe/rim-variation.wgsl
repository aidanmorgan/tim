// PhysicsGpuAbi supplies validated numeric discriminants and fixed array capacities.
struct ContactSlot {
    collider: u32, feature: u32,
    normalImpulse: f16, tangent0: f16, tangent1: f16, reserved: f16,
}
struct RigidBody {
    id: vec2<u32>, motion: u32, pad0: u32,
    cell: vec3<i32>, pad1: u32,
    local: vec4<f16>, rotation: vec4<f16>, velocity: vec4<f16>, angular: vec4<f16>,
    massDragGravityXY: vec4<f16>, gravityZPad: vec4<f16>,
    segmentCell: vec3<i32>, segmentOrdinal: u32,
    segmentLocalPhase: vec4<f16>, segmentVelocity: vec4<f16>,
    segmentRotation: vec4<f16>, segmentAngular: vec4<f16>,
    phase: u32, supportCount: u32, impacts: u32, pad2: u32,
    pad3: vec4<u32>, support: array<ContactSlot, 4>, pad4: array<vec4<u32>, 2>,
}
struct Collider {
    id: vec2<u32>, body: u32, material: u32, shape: u32, pad0: u32,
    translation: vec4<f16>, rotation: vec4<f16>, dimensions: vec4<f16>,
    profile: vec4<f16>, profileTail: vec4<f16>, pad1: array<vec4<u32>, 2>,
}
struct Material { id: vec2<u32>, values: vec4<f16>, pad: vec4<u32> }
struct ResidenceSensor {
    id: vec2<u32>, frame: u32, targetBody: u32, enabled: u32, pad0: u32,
    translation: vec4<f16>, rotation: vec4<f16>,
    low: vec4<f16>, high: vec4<f16>, controls: vec4<f16>,
    phase: u32, startOrdinal: u32, startPhase: f16, pad1: f16, sequence: u32,
    eventOrdinal: u32, eventPhase: f16, pad2: f16, pad3: vec2<u32>,
    pad4: array<vec4<u32>, 2>,
}
struct PlanarGuide {
    id: vec2<u32>, frame: u32, targetBody: u32,
    translation: vec4<f16>, rotation: vec4<f16>,
    low: vec4<f16>, high: vec4<f16>, controls: vec4<f16>, pad: vec2<u32>,
}
struct ContactTrigger {
    id: vec2<u32>, owner: u32, targetBody: u32,
    controls: vec4<f16>, pad0: vec2<u32>,
    sequence: u32, collider: u32, eventOrdinal: u32, eventPhase: f16, approachSpeed: f16,
    pad1: vec4<u32>,
}
struct MotionPiece {
    kind: u32, startOrdinal: u32, endOrdinal: u32, anchorOrdinal: u32,
    phases: vec4<f16>, bodyId: vec2<u32>,
    cell: vec3<i32>, pad0: u32,
    localDrag: vec4<f16>, rotation: vec4<f16>, velocity: vec4<f16>,
    angular: vec4<f16>, gravity: vec4<f16>, acceleration: vec4<f16>,
    angularAcceleration: vec4<f16>, forceError: vec4<f16>, pad2: vec4<u32>,
}
struct PhysicsState {
    version: u32, status: u32, failure: u32, bodyCount: u32,
    colliderCount: u32, materialCount: u32, sensorCount: u32, dynamicBody: u32,
    epoch: vec2<u32>, tick: vec2<u32>, cadence: u32, physical: u32,
    cadenceRevision: vec2<u32>, document: vec4<u32>, nextIdentity: vec2<u32>,
    ordinal: u32, captures: u32, guideCount: u32, triggerCount: u32, reserved1: vec2<u32>, reserved2: vec4<u32>,
    bodies: array<RigidBody, 16>, colliders: array<Collider, 32>,
    materials: array<Material, 16>, sensors: array<ResidenceSensor, 8>, guides: array<PlanarGuide, 8>, triggers: array<ContactTrigger, 8>,
}
// Preserve the ABI layout while excluding previous presentation output from Advance copies.
struct PhysicsWorld {
    state: PhysicsState,
    motionHeader: vec4<u32>, motionPieces: array<MotionPiece, 72>,
}
struct MotionQuery {
    failure: u32, cell: vec3<i32>, local: vec3<f16>,
    velocity: vec3<f16>, angular: vec3<f16>, rotation: vec4<f16>,
}
struct ShapeQuery {
    failure: u32, gap: f16, normal: vec3<f16>, feature: u32, far: bool,
}
struct SurfacePair { failure: u32, restitution: f16, threshold: f16, friction: f16 }
struct VelocityResult { failure: u32, velocity: vec3<f16>, angular: vec3<f16> }
struct MotionCurve { body: RigidBody, supported: bool, forceDriven: bool, acceleration: vec3<f16>, angularAcceleration: vec3<f16>, accelerationError: f16, eventVelocityError: f16, eventPositionError: f16 }

@group(0) @binding(0) var<storage, read> committed: PhysicsWorld;
@group(0) @binding(1) var<storage, read_write> candidate: PhysicsWorld;
var<private> sensorOperations: array<u32, 8>;
var<private> guideOperations: array<u32, 8>;

const NO_BODY: u32 = 0xffffffffu;
const PHASE_SCALE: f16 = 4096h;
const SPEED_BOUND: f16 = 128h;
const GRAVITY_BOUND: f16 = 16h;
const CONTACT_BAND_FRACTION: f16 = 0.015625h;
const ADVANCEMENT_BUDGET: u32 = 64u;
const IMPACT_BUDGET: u32 = 8u;
const SENSOR_ROOT_BUDGET: u32 = 128u;

fn finite(v: f16) -> bool { return v == v && abs(v) <= 65504h; }
fn finite3(v: vec3<f16>) -> bool { return finite(v.x) && finite(v.y) && finite(v.z); }
fn positive_zero(v: f16) -> bool { return bitcast<u32>(vec2<f16>(v, 0h)) == 0u; }
fn zero4(v: vec4<f16>) -> bool {
    return positive_zero(v.x) && positive_zero(v.y) && positive_zero(v.z) && positive_zero(v.w);
}
fn fail(code: u32) {
    if (candidate.state.failure == FAILURE_NONE) {
        candidate.state.failure = code; candidate.state.status = STATUS_INVALID;
    }
}
fn steps(cadence: u32) -> u32 {
    if (cadence == CADENCE_60) { return 8u; }
    if (cadence == CADENCE_120) { return 4u; }
    if (cadence == CADENCE_240) { return 2u; }
    return 0u;
}
fn down_positive(v: f16) -> f16 {
    if (!(v > 0h)) { return 0h; }
    let bits = bitcast<u32>(vec2<f16>(v, 0h)) & 65535u;
    // Subnormal persistence is not assumed; zero is a conservative lower bound.
    if (bits <= 1024u) { return 0h; }
    return bitcast<vec2<f16>>(bits - 1u).x;
}
fn up_nonnegative(v: f16) -> f16 {
    if (!(v >= 0h && v < 65504h)) { fail(FAILURE_ARITHMETIC); return 65504h; }
    let bits = bitcast<u32>(vec2<f16>(v, 0h)) & 65535u;
    // A flushed positive product is enclosed by the first normal value.
    if (bits < 1024u) { return bitcast<vec2<f16>>(1024u).x; }
    return bitcast<vec2<f16>>(bits + 1u).x;
}
fn down_signed(v: f16) -> f16 {
    if (v >= 0h) { return down_positive(v); }
    return -up_nonnegative(-v);
}
fn length3(v: vec3<f16>) -> f16 {
    let scale = max(abs(v.x), max(abs(v.y), abs(v.z)));
    if (scale == 0h) { return 0h; }
    if (!finite(scale) || scale > 256h) { return -1h; }
    let n = v / scale;
    return scale * sqrt(dot(n, n));
}
fn unit(v: vec3<f16>) -> vec3<f16> {
    let size = length3(v);
    if (!(size >= 0.00006103515625h && size <= 256h)) { return vec3<f16>(0h); }
    return v / size;
}
fn rotate(q: vec4<f16>, v: vec3<f16>) -> vec3<f16> {
    // Canonical quaternion bits are not rewritten. The declared rigid rotation is scale invariant.
    let norm = dot(q, q);
    if (!(norm >= 0.998h && norm <= 1.002h)) { fail(FAILURE_DOMAIN); return vec3<f16>(0h); }
    return v + (2h / norm) * (q.w * cross(q.xyz, v) + cross(q.xyz, cross(q.xyz, v)));
}
fn inverse_rotate(q: vec4<f16>, v: vec3<f16>) -> vec3<f16> {
    return rotate(vec4<f16>(-q.xyz, q.w), v);
}
fn multiply_rotation(a: vec4<f16>, b: vec4<f16>) -> vec4<f16> {
    return vec4<f16>(a.w * b.xyz + b.w * a.xyz + cross(a.xyz, b.xyz), a.w * b.w - dot(a.xyz, b.xyz));
}
// Certify the stored Half quaternion against the unchanged canonical norm domain.
// Coarse components are multiples of 1/16, so their squares/sum are exact
// multiples of 1/256 below two. Only the small residual uses directed bounds.
fn rotation_unit_certified(q: vec4<f16>) -> bool {
    var coarseSquared = 0h;
    var residual = range_value(0h);
    for (var axis = 0u; axis < 4u; axis++) {
        let bits = bitcast<u32>(vec2<f16>(q[axis], 0h)) & 32767u;
        if (bits > 15360u) { return false; } // Nonfinite or magnitude greater than one.
        let magnitude = bitcast<vec2<f16>>(bits).x;
        var coarse = 0h;
        var delta = range_value(0h);
        if (bits >= 1024u) {
            coarse = floor(magnitude * 16h + 0.5h) * 0.0625h;
            delta = range_subtract(range_value(magnitude), range_value(coarse));
        } else if (bits != 0u) {
            // Preserve an enclosure even if the adapter flushes subnormal arithmetic.
            delta = ScalarRange(0h, 0.00006103515625h);
        }
        coarseSquared += coarse * coarse;
        if (coarseSquared >= 2h) { return false; }
        residual = range_add(residual, range_add(
            range_multiply(range_value(2h * coarse), delta), range_multiply(delta, delta)));
    }
    let normResidual = range_add(range_value(coarseSquared - 1h), residual);
    return normResidual.lo >= -0.0009765625h && normResidual.hi <= 0.0009765625h;
}
fn certify_rotation(q: vec4<f16>, original: vec4<f16>) -> vec4<f16> {
    if (rotation_unit_certified(q)) { return q; }
    var largest = 0u;
    for (var axis = 1u; axis < 4u; axis++) {
        if (abs(q[axis]) > abs(q[largest])) { largest = axis; }
    }
    let bits = bitcast<u32>(vec2<f16>(q[largest], 0h)) & 65535u;
    let sign = bits & 32768u; let magnitude = bits & 32767u;
    // Search nearest stored values first; retain every other component/angular bit.
    for (var distance = 1u; distance <= 4u; distance++) {
        var adjusted = q;
        if (magnitude >= distance) {
            adjusted[largest] = bitcast<vec2<f16>>(sign | (magnitude - distance)).x;
            if (rotation_unit_certified(adjusted)) { return adjusted; }
        }
        adjusted[largest] = bitcast<vec2<f16>>(sign | (magnitude + distance)).x;
        if (rotation_unit_certified(adjusted)) { return adjusted; }
    }
    fail(FAILURE_ARITHMETIC); return original;
}
fn rotation_at(q0: vec4<f16>, omega: vec3<f16>, elapsed: f16) -> vec4<f16> {
    let speed = length3(omega);
    if (speed == 0h || elapsed == 0h) { return q0; }
    if (!(speed >= 0h && speed <= 64h && elapsed <= 2h)) { fail(FAILURE_DOMAIN); return q0; }
    // The continuous small-angle limit avoids dividing by a subnormal speed.
    // With speed < 2^-14 and elapsed <= 2, halfAngle < 2^-14: omitted cosine
    // and sine terms are below 2^-29 and 4e-14 respectively. Angular state is unchanged.
    var turn = vec4<f16>(omega * (elapsed * 0.5h), 1h);
    if (speed >= 0.00006103515625h) {
        let halfAngle = speed * elapsed * 0.5h;
        turn = vec4<f16>((omega / speed) * sin(halfAngle), cos(halfAngle));
    }
    let q = multiply_rotation(turn, q0);
    let squared = dot(q, q);
    if (!(squared >= 0.98h && squared <= 1.02h)) { fail(FAILURE_ARITHMETIC); return q0; }
    let normalized = q / sqrt(squared);
    // Near one, f16 sqrt can round to one and leave the norm error untouched.
    // Refine the reciprocal length in f16 instead of widening or changing admission.
    let residual = 0.5h * (1h - dot(normalized, normalized));
    return certify_rotation(normalized + normalized * residual, q0);
}
fn gravity(body: RigidBody) -> vec3<f16> {
    return vec3<f16>(body.massDragGravityXY.zw, body.gravityZPad.x);
}
fn query_failure(code: u32) -> MotionQuery { var q: MotionQuery; q.failure = code; return q; }

fn curve_query(curve: MotionCurve, ordinal: u32, phase: f16) -> MotionQuery {
    let body = curve.body;
    let whole = i32(ordinal) - i32(body.segmentOrdinal);
    let fraction = (phase - body.segmentLocalPhase.w) / PHASE_SCALE;
    if (whole < -1 || whole > i32(PRIMARY_SEGMENT_STEPS) + 1 || fraction < -1.5h || fraction > 1.5h) {
        return query_failure(FAILURE_DOMAIN);
    }
    let elapsed = (f16(whole) + fraction) / PHYSICAL_RATE;
    if (!(elapsed >= 0h && elapsed <= f16(PRIMARY_SEGMENT_STEPS) / PHYSICAL_RATE)) { return query_failure(FAILURE_DOMAIN); }
    let velocity0 = body.segmentVelocity.xyz * 32h;
    let k = body.massDragGravityXY.y;
    if (!finite3(velocity0) || !finite3(gravity(body)) || !finite3(body.segmentLocalPhase.xyz) ||
        !finite(body.segmentLocalPhase.w) || !finite3(body.segmentAngular.xyz) ||
        length3(velocity0) > 64h || length3(gravity(body)) > GRAVITY_BOUND ||
        !(k >= 0h && k <= 0.125h)) { return query_failure(FAILURE_DOMAIN); }
    var acceleration = gravity(body) - k * velocity0;
    var x = k * elapsed;
    if (curve.supported || curve.forceDriven) {
        if (elapsed > 0.0020847320556640625h || !finite3(curve.acceleration) ||
            length3(curve.acceleration) > 64h || !finite3(curve.angularAcceleration) ||
            length3(curve.angularAcceleration / 16h) > 64h) { return query_failure(FAILURE_DOMAIN); }
        acceleration = curve.acceleration; x = 0h;
    }
    let p = 0.5h + x * (-0.1666259765625h + x * (0.041656494140625h +
        x * (-0.008331298828125h + x * 0.0013885498046875h)));
    let d = 1h + x * (-0.5h + x * (0.1666259765625h +
        x * (-0.041656494140625h + x * 0.008331298828125h)));
    var displacement = velocity0 * elapsed + acceleration * (elapsed * elapsed * p);
    if (curve.forceDriven) {
        let u = (f16(whole) + fraction) / (PHYSICAL_RATE / 32h);
        displacement = (velocity0 * u) * 0.03125h + ((acceleration * u) * u) * 0.00048828125h;
    }
    let velocity = velocity0 + acceleration * (elapsed * d);
    if (!finite3(displacement) || !finite3(velocity) || length3(velocity) > 64h) {
        return query_failure(FAILURE_DOMAIN);
    }
    let local = body.segmentLocalPhase.xyz + displacement * 16h;
    if (any(abs(local) > vec3<f16>(4096h))) { return query_failure(FAILURE_DOMAIN); }
    let carry = vec3<i32>(floor(local + vec3<f16>(0.5h)));
    let cell = body.segmentCell + carry;
    let remainder = local - vec3<f16>(carry);
    if (any(abs(cell) > vec3<i32>(1024)) ||
        any(remainder < vec3<f16>(-0.5h)) || any(remainder >= vec3<f16>(0.5h))) {
        return query_failure(FAILURE_DOMAIN);
    }
    let omega = body.segmentAngular.xyz + curve.angularAcceleration * elapsed;
    if (!finite3(omega) || length3(omega) > 64h) { return query_failure(FAILURE_DOMAIN); }
    return MotionQuery(FAILURE_NONE, cell, remainder, velocity, omega,
        rotation_at(body.segmentRotation, body.segmentAngular.xyz + curve.angularAcceleration * (elapsed * 0.5h), elapsed));
}
fn motion(body: RigidBody, ordinal: u32, phase: f16) -> MotionQuery {
    return curve_query(MotionCurve(body, false, false, vec3<f16>(0h), vec3<f16>(0h), 0h, 0h, 0h), ordinal, phase);
}
fn relative_point(point: MotionQuery, frame: RigidBody) -> vec3<f16> {
    // Integer subtraction precedes narrowing; absolute world positions never supply a small gap.
    let delta = point.cell - frame.cell;
    if (any(abs(delta) > vec3<i32>(2048))) { fail(FAILURE_DOMAIN); return vec3<f16>(0h); }
    return vec3<f16>(delta) * 0.0625h + (point.local - frame.local.xyz) * 0.0625h;
}
fn collider_point(point: MotionQuery, collider: Collider) -> vec3<f16> {
    let frame = candidate.state.bodies[collider.body];
    let inFrame = inverse_rotate(frame.rotation, relative_point(point, frame));
    return inverse_rotate(collider.rotation, inFrame - collider.translation.xyz);
}
fn collider_normal(local: vec3<f16>, collider: Collider) -> vec3<f16> {
    return unit(rotate(candidate.state.bodies[collider.body].rotation, rotate(collider.rotation, local)));
}
// Eight exposed meridian segments of the declared solid union. Endpoint feature
// lanes are stable; the continuous bore is a single segment across both collars.
struct AnnularSegment { low: vec2<f16>, high: vec2<f16>, normal: vec2<f16>, enabled: bool }
fn annular_segment(c: Collider, family: u32) -> AnnularSegment {
    let h = c.profile.x; let inner = c.profile.y; let middle = c.profile.z;
    let outer = c.profile.w; let width = c.profileTail.x;
    let end = h + width; let shoulder = h - width;
    switch family {
        case 0u: { return AnnularSegment(vec2<f16>(-end, inner), vec2<f16>(end, inner), vec2<f16>(0h,-1h), true); }
        case 1u: { return AnnularSegment(vec2<f16>(-shoulder,middle), vec2<f16>(shoulder,middle), vec2<f16>(0h,1h), true); }
        case 2u: { return AnnularSegment(vec2<f16>(-end,outer), vec2<f16>(-shoulder,outer), vec2<f16>(0h,1h), width > 0h); }
        case 3u: { return AnnularSegment(vec2<f16>(shoulder,outer), vec2<f16>(end,outer), vec2<f16>(0h,1h), width > 0h); }
        case 4u: { return AnnularSegment(vec2<f16>(-end,inner), vec2<f16>(-end,outer), vec2<f16>(-1h,0h), true); }
        case 5u: { return AnnularSegment(vec2<f16>(end,inner), vec2<f16>(end,outer), vec2<f16>(1h,0h), true); }
        case 6u: { return AnnularSegment(vec2<f16>(-shoulder,middle), vec2<f16>(-shoulder,outer), vec2<f16>(1h,0h), width > 0h); }
        default: { return AnnularSegment(vec2<f16>(shoulder,middle), vec2<f16>(shoulder,outer), vec2<f16>(-1h,0h), width > 0h); }
    }
}
fn annular_query(p: vec3<f16>, collider: Collider, radius: f16, excludedFamily: u32) -> ShapeQuery {
    let rho = length3(vec3<f16>(0h,p.y,p.z));
    if (!(rho >= 0h)) { return ShapeQuery(FAILURE_DOMAIN,0h,vec3<f16>(0h),0u,false); }
    let axial = abs(p.x); let h = collider.profile.x; let width = collider.profileTail.x;
    let radialLimit = select(collider.profile.z,collider.profile.w,axial >= h-width);
    let inside = axial <= h+width && rho >= collider.profile.y && rho <= radialLimit;
    let point = vec2<f16>(p.x,rho);
    var radial = vec2<f16>(1h,0h);
    if (rho >= 0.00006103515625h) { radial = p.yz/rho; }
    var best = 65504h; var normal = vec3<f16>(0h); var feature = NO_BODY;
    for (var family=0u; family<8u; family++) {
        if (family == excludedFamily) { continue; }
        let segment = annular_segment(collider,family);
        if (!segment.enabled) { continue; }
        let closest = clamp(point,segment.low,segment.high);
        let delta = point-closest;
        let distance = length3(vec3<f16>(delta,0h));
        if (!(distance >= 0h)) { return ShapeQuery(FAILURE_DOMAIN,0h,vec3<f16>(0h),0u,false); }
        if (distance >= best) { continue; }
        best=distance;
        var meridian = segment.normal;
        if (distance >= 0.00006103515625h) { meridian = delta/distance * select(1h,-1h,inside); }
        normal = vec3<f16>(meridian.x,meridian.y*radial.x,meridian.y*radial.y);
        let axis = select(0u,1u,family >= 4u);
        var endpoint=0u;
        if (point[axis] <= segment.low[axis]) { endpoint=1u; }
        else if (point[axis] >= segment.high[axis]) { endpoint=2u; }
        feature=family*3u+endpoint;
    }
    if (feature==NO_BODY) { return ShapeQuery(FAILURE_PAIR,0h,vec3<f16>(0h),0u,false); }
    return ShapeQuery(FAILURE_NONE,select(best,-best,inside)-radius,collider_normal(normal,collider),feature,best>4h);
}
fn shape_query(point: MotionQuery, collider: Collider, radius: f16) -> ShapeQuery {
    if (collider.body >= candidate.state.bodyCount || collider.material >= candidate.state.materialCount ||
        candidate.state.bodies[collider.body].motion != MOTION_STATIC) {
        return ShapeQuery(FAILURE_PAIR, 0h, vec3<f16>(0h), 0u, false);
    }
    let p = collider_point(point, collider);
    if (!finite3(p)) { return ShapeQuery(FAILURE_DOMAIN, 0h, vec3<f16>(0h), 0u, false); }
    if (collider.shape == SHAPE_PLANE) {
        return ShapeQuery(FAILURE_NONE, p.y - radius,
            collider_normal(vec3<f16>(0h, 1h, 0h), collider), 0u, false);
    }
    if (collider.shape == SHAPE_ANNULAR) { return annular_query(p, collider, radius, NO_BODY); }
    if (collider.shape != SHAPE_BOX) { return ShapeQuery(FAILURE_PAIR, 0h, vec3<f16>(0h), 0u, false); }
    let extents = collider.dimensions.yzw;
    let nearest = clamp(p, -extents, extents);
    let delta = p - nearest;
    let distance = length3(delta);
    if (distance < 0h) { return ShapeQuery(FAILURE_DOMAIN, 0h, vec3<f16>(0h), 0u, false); }
    var normal: vec3<f16>; var gap: f16; var feature: u32 = 0u;
    // Two bits per axis identify lower/interior/upper closest-point features; 0 is plane.
    for (var axis = 0u; axis < 3u; axis++) {
        var side = 1u;
        if (p[axis] < -extents[axis]) { side = 0u; }
        else if (p[axis] > extents[axis]) { side = 2u; }
        feature |= side << (2u * axis);
    }
    if (distance >= 0.00006103515625h) {
        normal = delta / distance; gap = distance - radius;
    } else {
        let depth = extents - abs(p);
        var axis = 0u;
        if (depth.y < depth.x) { axis = 1u; }
        if (depth.z < depth[axis]) { axis = 2u; }
        normal = vec3<f16>(0h); normal[axis] = select(-1h, 1h, p[axis] >= 0h);
        gap = -depth[axis] - radius;
        feature = 64u + axis * 2u + select(0u, 1u, p[axis] >= 0h);
    }
    return ShapeQuery(FAILURE_NONE, gap, collider_normal(normal, collider), feature, distance > 4h);
}
fn contact_position(original: MotionQuery, colliderIndex: u32, radius: f16) -> MotionQuery {
    var q = original;
    let band = radius * CONTACT_BAND_FRACTION;
    // One zero-time correction, bounded from the original hit point across all
    // passes. Closest features are queried again after canonical cell/local packing.
    for (var projection = 0u; projection < 4u; projection++) {
        let shape = shape_query(q, candidate.state.colliders[colliderIndex], radius);
        if (shape.failure != FAILURE_NONE) { return query_failure(shape.failure); }
        if (shape.gap >= 0h) { break; }
        if (shape.gap < -band) { return query_failure(FAILURE_RESIDUAL); }
        let local = q.local + shape.normal * (up_nonnegative(-shape.gap) * 16h);
        let carry = vec3<i32>(floor(local + vec3<f16>(0.5h)));
        q.cell += carry; q.local = local - vec3<f16>(carry);
        if (any(abs(q.cell) > vec3<i32>(1024)) || !finite3(q.local)) {
            return query_failure(FAILURE_DOMAIN);
        }
        let displacement = (vec3<f16>(q.cell - original.cell) + (q.local - original.local)) / 16h;
        let distance = length3(displacement);
        if (!(distance >= 0h && distance <= band)) { return query_failure(FAILURE_RESIDUAL); }
    }
    for (var i = 0u; i < candidate.state.colliderCount; i++) {
        if (candidate.state.colliders[i].body == candidate.state.dynamicBody) { continue; }
        let shape = shape_query(q, candidate.state.colliders[i], radius);
        if (shape.failure != FAILURE_NONE) { return query_failure(shape.failure); }
        if (shape.gap < -band || (i == colliderIndex && shape.gap < 0h)) {
            return query_failure(FAILURE_RESIDUAL);
        }
    }
    return q;
}

fn surface_pair(a: u32, b: u32) -> SurfacePair {
    if (a >= candidate.state.materialCount || b >= candidate.state.materialCount) {
        return SurfacePair(FAILURE_DECLARATION, 0h, 0h, 0h);
    }
    let x = candidate.state.materials[a].values; let y = candidate.state.materials[b].values;
    if (any(x.xyz < vec3<f16>(0h)) || any(y.xyz < vec3<f16>(0h)) ||
        x.x > 1h || y.x > 1h || x.y > 64h || y.y > 64h || x.z > 1h || y.z > 1h) {
        return SurfacePair(FAILURE_DOMAIN, 0h, 0h, 0h);
    }
    return SurfacePair(FAILURE_NONE, x.x * y.x, max(x.y, y.y), sqrt(x.z) * sqrt(y.z));
}
fn impact_velocity(v: vec3<f16>, omega: vec3<f16>, normal: vec3<f16>,
    radius: f16, surface: SurfacePair) -> VelocityResult {
    if (surface.failure != FAILURE_NONE || !(radius >= 0.0625h && radius <= 2h)) {
        return VelocityResult(FAILURE_DOMAIN, v, omega);
    }
    let incoming = dot(v, normal);
    if (incoming >= 0h) { return VelocityResult(FAILURE_NONE, v, omega); }
    let e = select(0h, surface.restitution, -incoming >= surface.threshold);
    // Store impulse per unit mass. Static-pair mass cancels; no tiny Half inertia is formed.
    let normalImpulse = -(1h + e) * incoming;
    let contactVelocity = v + cross(omega, -radius * normal);
    let tangent = contactVelocity - dot(contactVelocity, normal) * normal;
    let speed = length3(tangent);
    var tangentDelta = vec3<f16>(0h);
    if (speed >= 0.00006103515625h) {
        let magnitude = min(speed / 3.5h, surface.friction * normalImpulse);
        tangentDelta = -(tangent / speed) * magnitude;
    }
    let nextV = v + normal * normalImpulse + tangentDelta;
    let nextOmega = omega + cross(-normal, tangentDelta) * (2.5h / radius);
    if (!finite3(nextV) || !finite3(nextOmega) || length3(nextV) > 64h || length3(nextOmega) > 64h) {
        return VelocityResult(FAILURE_DOMAIN, v, omega);
    }
    return VelocityResult(FAILURE_NONE, nextV, nextOmega);
}

struct TimeInterval { left: f16, right: f16, depth: u32 }
struct Hit { failure: u32, found: bool, phase: f16, collider: u32, value: MotionQuery, shape: ShapeQuery }

fn no_hit() -> Hit { var h: Hit; return h; }
fn sphere_collider(body: u32) -> u32 {
    var result = NO_BODY;
    for (var i = 0u; i < candidate.state.colliderCount; i++) {
        if (candidate.state.colliders[i].body == body) {
            if (result != NO_BODY || candidate.state.colliders[i].shape != SHAPE_SPHERE) {
                fail(FAILURE_PAIR); return NO_BODY;
            }
            result = i;
        }
    }
    return result;
}
fn install_query(slot: u32, q: MotionQuery) {
    candidate.state.bodies[slot].cell = q.cell;
    candidate.state.bodies[slot].local = vec4<f16>(q.local, 0h);
    candidate.state.bodies[slot].velocity = vec4<f16>(q.velocity / 32h, 0h);
    candidate.state.bodies[slot].angular = vec4<f16>(q.angular, 0h);
    candidate.state.bodies[slot].rotation = q.rotation;
}
fn anchor(slot: u32, ordinal: u32, phase: f16) {
    var n = ordinal; var p = phase;
    if (p >= 2048h) { n++; p -= PHASE_SCALE; }
    let b = candidate.state.bodies[slot];
    candidate.state.bodies[slot].segmentCell = b.cell;
    candidate.state.bodies[slot].segmentOrdinal = n;
    candidate.state.bodies[slot].segmentLocalPhase = vec4<f16>(b.local.xyz, p);
    candidate.state.bodies[slot].segmentVelocity = b.velocity;
    candidate.state.bodies[slot].segmentAngular = b.angular;
    candidate.state.bodies[slot].segmentRotation = b.rotation;
}
// Directed bounds use the existing canonical polynomial, not the rounded shape
// distance. Coarse cell and fine local terms remain separate through face/radius
// subtraction so a large frame translation cannot erase a small clearance.
struct ScalarRange { lo: f16, hi: f16, }
struct VectorRange { lo: vec3<f16>, hi: vec3<f16>, }
fn range_lower(v: f16) -> f16 {
    if (v == 0h) { return -0.00006103515625h; }
    return down_signed(v);
}
fn range_upper(v: f16) -> f16 { return -range_lower(-v); }
fn range_value(v: f16) -> ScalarRange { return ScalarRange(v, v); }
fn range_add(a: ScalarRange, b: ScalarRange) -> ScalarRange {
    if (a.lo == 0h && a.hi == 0h) { return b; }
    if (b.lo == 0h && b.hi == 0h) { return a; }
    return ScalarRange(range_lower(a.lo + b.lo), range_upper(a.hi + b.hi));
}
fn range_negate(a: ScalarRange) -> ScalarRange { return ScalarRange(-a.hi, -a.lo); }
fn range_subtract(a: ScalarRange, b: ScalarRange) -> ScalarRange {
    if (a.lo == a.hi && b.lo == b.hi && a.lo == b.lo) { return range_value(0h); }
    return range_add(a, range_negate(b));
}
fn range_multiply(a: ScalarRange, b: ScalarRange) -> ScalarRange {
    if ((a.lo == 0h && a.hi == 0h) || (b.lo == 0h && b.hi == 0h)) { return range_value(0h); }
    let p = vec4<f16>(a.lo * b.lo, a.lo * b.hi, a.hi * b.lo, a.hi * b.hi);
    return ScalarRange(range_lower(min(min(p.x, p.y), min(p.z, p.w))),
        range_upper(max(max(p.x, p.y), max(p.z, p.w))));
}
fn vector_value(v: vec3<f16>) -> VectorRange { return VectorRange(v, v); }
fn vector_add(a: VectorRange, b: VectorRange) -> VectorRange {
    var result: VectorRange;
    for (var axis = 0u; axis < 3u; axis++) {
        let lane = range_add(ScalarRange(a.lo[axis], a.hi[axis]), ScalarRange(b.lo[axis], b.hi[axis]));
        result.lo[axis] = lane.lo; result.hi[axis] = lane.hi;
    }
    return result;
}
fn vector_scale(a: VectorRange, b: ScalarRange) -> VectorRange {
    var result: VectorRange;
    for (var axis = 0u; axis < 3u; axis++) {
        let lane = range_multiply(ScalarRange(a.lo[axis], a.hi[axis]), b);
        result.lo[axis] = lane.lo; result.hi[axis] = lane.hi;
    }
    return result;
}
fn vector_cross(a: vec3<f16>, b: VectorRange) -> VectorRange {
    var result: VectorRange;
    for (var axis = 0u; axis < 3u; axis++) {
        let j = (axis + 1u) % 3u; let k = (axis + 2u) % 3u;
        let lane = range_subtract(
            range_multiply(range_value(a[j]), ScalarRange(b.lo[k], b.hi[k])),
            range_multiply(range_value(a[k]), ScalarRange(b.lo[j], b.hi[j])));
        result.lo[axis] = lane.lo; result.hi[axis] = lane.hi;
    }
    return result;
}
fn vector_inverse_rotate(q: vec4<f16>, v: VectorRange) -> VectorRange {
    if (all(q.xyz == vec3<f16>(0h))) { return v; }
    var norm = range_value(0h);
    for (var axis = 0u; axis < 4u; axis++) {
        norm = range_add(norm, range_multiply(range_value(q[axis]), range_value(q[axis])));
    }
    // Canonical rotations have norm near one; all reciprocal operands are normal.
    let factor = ScalarRange(range_lower(2h / norm.hi), range_upper(2h / norm.lo));
    let first = vector_cross(-q.xyz, v);
    let turn = vector_add(vector_scale(first, range_value(q.w)), vector_cross(-q.xyz, first));
    return vector_add(v, vector_scale(turn, factor));
}


fn guide_divide_bounds(numerator: f16, denominator: f16) -> ScalarRange {
    if (!(denominator >= 0.00006103515625h && denominator <= 16384h)) {
        fail(FAILURE_ARITHMETIC); return range_value(0h);
    }
    if (numerator == 0h) { return range_value(0h); }
    let approximate = numerator / denominator;
    var result = ScalarRange(approximate, approximate);
    for (var attempt = 0u; attempt < 32u; attempt++) {
        let lower = range_multiply(range_value(result.lo), range_value(denominator));
        let upper = range_multiply(range_value(result.hi), range_value(denominator));
        let lowReady = lower.hi <= numerator; let highReady = upper.lo >= numerator;
        if (lowReady && highReady) { return result; }
        if (!lowReady) { result.lo = range_lower(result.lo); }
        if (!highReady) { result.hi = range_upper(result.hi); }
    }
    fail(FAILURE_ARITHMETIC); return result;
}
fn guide_root_bounds(value: f16) -> ScalarRange {
    // Only radial lengths above one need sqrt; avoid subnormal reciprocal domains.
    if (!(value >= 1h && value <= 1024h)) { fail(FAILURE_DOMAIN); return range_value(1h); }
    let approximate = sqrt(value);
    var result = ScalarRange(approximate, approximate);
    for (var attempt = 0u; attempt < 32u; attempt++) {
        let lower = range_multiply(range_value(result.lo), range_value(result.lo));
        let upper = range_multiply(range_value(result.hi), range_value(result.hi));
        let lowReady = lower.hi <= value; let highReady = upper.lo >= value;
        if (lowReady && highReady) { return result; }
        if (!lowReady) { result.lo = down_positive(result.lo); }
        if (!highReady) { result.hi = up_nonnegative(result.hi); }
    }
    fail(FAILURE_ARITHMETIC); return result;
}
fn guide_rotation_bounds(q: vec4<f16>, value: VectorRange) -> VectorRange {
    if (all(q.xyz == vec3<f16>(0h))) { return value; }
    var norm = range_value(0h);
    for (var axis = 0u; axis < 4u; axis++) {
        norm = range_add(norm, range_multiply(range_value(q[axis]), range_value(q[axis])));
    }
    let factor = ScalarRange(guide_divide_bounds(2h,norm.hi).lo, guide_divide_bounds(2h,norm.lo).hi);
    let first = vector_cross(-q.xyz,value);
    return vector_add(value,vector_scale(vector_add(vector_scale(first,range_value(q.w)),
        vector_cross(-q.xyz,first)),factor));
}

// Force samples retain the enclosure of the authored continuous law through every
// Half operation. This is an error certificate, not a second numerical state.
struct GuideForceSample { acceleration: vec3<f16>, enclosure: VectorRange }
struct GuideMidpoint { enabled: bool, acceleration: vec3<f16>, error: f16 }
fn guide_point(q: MotionQuery, guide: PlanarGuide) -> vec3<f16> {
    let frame = candidate.state.bodies[guide.frame];
    return inverse_rotate(guide.rotation, inverse_rotate(frame.rotation, relative_point(q, frame)) - guide.translation.xyz);
}
fn guide_velocity(q: MotionQuery, guide: PlanarGuide) -> vec3<f16> {
    return inverse_rotate(guide.rotation, inverse_rotate(candidate.state.bodies[guide.frame].rotation, q.velocity));
}
fn guide_point_range(q: MotionQuery, guide: PlanarGuide) -> VectorRange {
    let frame = candidate.state.bodies[guide.frame];
    let cells = q.cell - frame.cell;
    if (any(abs(cells) > vec3<i32>(2048))) { fail(FAILURE_DOMAIN); return vector_value(vec3<f16>(0h)); }
    let local = vector_add(vector_value(q.local), vector_value(-frame.local.xyz));
    let point = vector_scale(vector_add(vector_value(vec3<f16>(cells)), local), range_value(0.0625h));
    return guide_rotation_bounds(guide.rotation, vector_add(
        guide_rotation_bounds(frame.rotation, point), vector_value(-guide.translation.xyz)));
}
fn guide_inside(q: MotionQuery, guide: PlanarGuide, radius: f16) -> bool {
    let p = guide_point(q, guide);
    return guide.controls.x > 0h && all(p >= guide.low.xyz) && all(p <= guide.high.xyz) &&
        p.y - radius >= guide.controls.y - guide.controls.z && guide_velocity(q, guide).y <= 0h;
}
fn guide_departing(q: MotionQuery, guide: PlanarGuide, radius: f16, acceleration: vec3<f16>) -> bool {
    let p = guide_point(q, guide); let v = guide_velocity(q, guide);
    let a = inverse_rotate(guide.rotation, inverse_rotate(candidate.state.bodies[guide.frame].rotation, acceleration));
    for (var axis = 0u; axis < 3u; axis++) {
        let outwardLow = v[axis] < 0h || (v[axis] == 0h && a[axis] < 0h);
        let outwardHigh = v[axis] > 0h || (v[axis] == 0h && a[axis] > 0h);
        if ((p[axis] == guide.low[axis] && outwardLow) || (p[axis] == guide.high[axis] && outwardHigh)) { return true; }
    }
    if (p.y - radius == guide.controls.y - guide.controls.z && (v.y < 0h || (v.y == 0h && a.y < 0h))) { return true; }
    return v.y == 0h && a.y > 0h;
}
fn guide_force(local: vec3<f16>, enclosed: VectorRange, guide: PlanarGuide) -> GuideForceSample {
    let flat = vec3<f16>(local.x, 0h, local.z);
    let nominal = -flat * (guide.controls.x / max(1h, length3(flat)));
    var radial = enclosed; radial.lo.y = 0h; radial.hi.y = 0h;
    var squared = range_value(0h);
    for (var axis = 0u; axis < 3u; axis += 2u) {
        let value = ScalarRange(radial.lo[axis], radial.hi[axis]);
        var square = range_multiply(value, value); square.lo = max(0h, square.lo);
        squared = range_add(squared, square);
    }
    var denominator = range_value(1h);
    if (squared.lo > 1h) { denominator.lo = max(1h,guide_root_bounds(squared.lo).lo); }
    if (squared.hi > 1h) { denominator.hi = max(1h,guide_root_bounds(squared.hi).hi); }
    let scale = ScalarRange(guide_divide_bounds(-guide.controls.x,denominator.lo).lo,
        guide_divide_bounds(-guide.controls.x,denominator.hi).hi);
    let localRange = vector_scale(radial, scale);
    let frameRotation = candidate.state.bodies[guide.frame].rotation;
    let enclosedWorld = guide_rotation_bounds(vec4<f16>(-frameRotation.xyz, frameRotation.w),
        guide_rotation_bounds(vec4<f16>(-guide.rotation.xyz, guide.rotation.w), localRange));
    return GuideForceSample(rotate(frameRotation, rotate(guide.rotation, nominal)), enclosedWorld);
}
fn guide_midpoint(body: RigidBody, initial: MotionQuery, ordinal: u32, phase: f16, radius: f16, end: f16) -> GuideMidpoint {
    let base = gravity(body) - body.massDragGravityXY.y * initial.velocity;
    var result = GuideMidpoint(false, base, 0h);
    for (var index = 0u; index < candidate.state.guideCount; index++) {
        let guide = candidate.state.guides[index];
        if (guide.targetBody != candidate.state.dynamicBody || !guide_inside(initial, guide, radius) ||
            guide_departing(initial, guide, radius, base)) { continue; }
        let point = guide_point(initial, guide); let pointRange = guide_point_range(initial, guide);
        let first = guide_force(point, pointRange, guide);
        let drag = range_value(body.massDragGravityXY.y);
        let initialAcceleration = base + first.acceleration;
        let initialRange = vector_add(vector_value(gravity(body)),
            vector_add(first.enclosure, vector_scale(vector_value(-initial.velocity), drag)));
        // hUnits=512*h stays normal across the complete physical phase lattice.
        let hUnits = (end - phase) / (8h * PHYSICAL_RATE);
        let phaseRange = range_subtract(range_value(end), range_value(phase));
        let durationUnits = ScalarRange(max(0h,guide_divide_bounds(phaseRange.lo,8h * PHYSICAL_RATE).lo),
            guide_divide_bounds(phaseRange.hi,8h * PHYSICAL_RATE).hi);
        let time = range_multiply(durationUnits, range_value(0.0009765625h));
        let halfTime = hUnits * 0.0009765625h;
        let localVelocity = guide_velocity(initial, guide);
        let velocityRange = guide_rotation_bounds(guide.rotation,
            guide_rotation_bounds(candidate.state.bodies[guide.frame].rotation, vector_value(initial.velocity)));
        let midpoint = point + localVelocity * halfTime;
        let midpointRange = vector_add(pointRange, vector_scale(velocityRange, time));
        let middle = guide_force(midpoint, midpointRange, guide);
        let middleVelocity = initial.velocity + initialAcceleration * halfTime;
        let middleVelocityRange = vector_add(vector_value(initial.velocity), vector_scale(initialRange, time));
        let nominal = gravity(body) + middle.acceleration - body.massDragGravityXY.y * middleVelocity;
        let enclosure = vector_add(vector_value(gravity(body)),
            vector_add(middle.enclosure, vector_scale(middleVelocityRange, range_value(-body.massDragGravityXY.y))));
        var arithmetic = 0h;
        for (var axis = 0u; axis < 3u; axis++) {
            arithmetic = up_nonnegative(arithmetic + up_nonnegative(max(
                abs(nominal[axis] - enclosure.lo[axis]), abs(enclosure.hi[axis] - nominal[axis]))));
        }
        // |a|<=37, |v|<64.125, J<=776. Every retained subinterval is within h.
        // The midpoint predictor's acceleration error is <=(12*37+.125*776)*h²/8.
        // Use hUnits so the positive quadratic never relies on subnormal t*t.
        let jerk = up_nonnegative(up_nonnegative(0.7578125h * durationUnits.hi));
        let predictor = up_nonnegative(up_nonnegative(0.00025844573974609375h * durationUnits.hi) * durationUnits.hi);
        let error = up_nonnegative(arithmetic + up_nonnegative(jerk + predictor));
        if (!finite3(nominal) || length3(nominal) > 37h || !(error <= 4h)) {
            fail(FAILURE_DOMAIN); return result;
        }
        result = GuideMidpoint(true, nominal, error);
    }
    return result;
}

struct AnnularBounds { valid: bool, position: VectorRange, coarse: VectorRange, fine: VectorRange }
fn annular_local_range(curve: MotionCurve, ordinal: u32, interval: TimeInterval, collider: Collider) -> AnnularBounds {
    let body=curve.body; let frame=candidate.state.bodies[collider.body];
    let cells=body.segmentCell-frame.cell;
    if (any(abs(cells)>vec3<i32>(128))) { return AnnularBounds(false,vector_value(vec3<f16>(0h)),vector_value(vec3<f16>(0h)),vector_value(vec3<f16>(0h))); }
    let phases=ScalarRange(interval.left,interval.right);
    let fraction=range_multiply(range_subtract(phases,range_value(body.segmentLocalPhase.w)),range_value(0.000244140625h));
    let elapsed=range_add(range_value(f16(i32(ordinal)-i32(body.segmentOrdinal))),fraction);
    let u=ScalarRange(max(0h,guide_divide_bounds(elapsed.lo,PHYSICAL_RATE/32h).lo),
        guide_divide_bounds(elapsed.hi,PHYSICAL_RATE/32h).hi);
    if (!(u.lo>=0h && u.hi<=4.125h)) { return AnnularBounds(false,vector_value(vec3<f16>(0h)),vector_value(vec3<f16>(0h)),vector_value(vec3<f16>(0h))); }
    let velocity=body.segmentVelocity.xyz*32h;
    var acceleration=vector_add(vector_value(gravity(body)),
        vector_scale(vector_value(-velocity),range_value(body.massDragGravityXY.y)));
    var x=range_multiply(u,range_multiply(range_value(body.massDragGravityXY.y),range_value(0.03125h)));
    if (curve.supported || curve.forceDriven) { acceleration=vector_value(curve.acceleration);x=range_value(0h); }
    if (curve.forceDriven) {
        acceleration=vector_add(acceleration,VectorRange(vec3<f16>(-curve.accelerationError),vec3<f16>(curve.accelerationError)));
    }
    var polynomial=range_value(0.0013885498046875h);
    polynomial=range_add(range_value(-0.008331298828125h),range_multiply(x,polynomial));
    polynomial=range_add(range_value(0.041656494140625h),range_multiply(x,polynomial));
    polynomial=range_add(range_value(-0.1666259765625h),range_multiply(x,polynomial));
    polynomial=range_add(range_value(0.5h),range_multiply(x,polynomial));
    let quadratic=range_multiply(range_multiply(u,u),polynomial);
    let displacement=vector_add(vector_scale(vector_value(velocity*0.5h),u),
        vector_scale(vector_scale(acceleration,range_value(0.015625h)),quadratic));
    var fine=vector_add(vector_add(vector_value(body.segmentLocalPhase.xyz),vector_value(-frame.local.xyz)),displacement);
    let error=range_add(range_value(curve.eventPositionError),
        range_multiply(range_value(curve.eventVelocityError),range_multiply(u,range_value(0.03125h)))).hi;
    if (error>0h) {
        fine=vector_add(fine,VectorRange(vec3<f16>(-up_nonnegative(error*16h)),vec3<f16>(up_nonnegative(error*16h))));
    }
    // guide_rotation_bounds already encloses the inverse declared rotation.
    let frameRotation=frame.rotation;let colliderRotation=collider.rotation;
    var coarse=guide_rotation_bounds(frameRotation,vector_value(vec3<f16>(cells)));
    fine=guide_rotation_bounds(frameRotation,fine);
    fine=vector_add(fine,vector_value(-collider.translation.xyz*16h));
    coarse=guide_rotation_bounds(colliderRotation,coarse);
    fine=guide_rotation_bounds(colliderRotation,fine);
    if (curve.supported) {
        // The fixed rigid maps distribute over this same canonical quadratic.
        // Transform coefficients before time ranges so axial motion keeps its
        // shared-time cancellation; no motion coefficient or endpoint changes.
        var anchorFine=guide_rotation_bounds(frameRotation,
            vector_add(vector_value(body.segmentLocalPhase.xyz),vector_value(-frame.local.xyz)));
        anchorFine=guide_rotation_bounds(colliderRotation,
            vector_add(anchorFine,vector_value(-collider.translation.xyz*16h)));
        let localVelocity=guide_rotation_bounds(colliderRotation,
            guide_rotation_bounds(frameRotation,vector_value(velocity*0.5h)));
        let localAcceleration=guide_rotation_bounds(colliderRotation,
            guide_rotation_bounds(frameRotation,vector_scale(acceleration,range_value(0.015625h))));
        fine=vector_add(anchorFine,vector_add(vector_scale(localVelocity,u),
            vector_scale(localAcceleration,quadratic)));
        if (error>0h) {
            let worldError=VectorRange(vec3<f16>(-up_nonnegative(error*16h)),vec3<f16>(up_nonnegative(error*16h)));
            fine=vector_add(fine,guide_rotation_bounds(colliderRotation,
                guide_rotation_bounds(frameRotation,worldError)));
        }
    }
    let position=vector_scale(vector_add(coarse,fine),range_value(0.0625h));
    if (!finite3(position.lo)||!finite3(position.hi)||any(abs(position.lo)>vec3<f16>(128h))||any(abs(position.hi)>vec3<f16>(128h))) {
        return AnnularBounds(false,position,coarse,fine);
    }
    return AnnularBounds(true,position,coarse,fine);
}
fn annular_square_bounds(value: ScalarRange) -> ScalarRange {
    var low=min(abs(value.lo),abs(value.hi));let high=max(abs(value.lo),abs(value.hi));
    if (value.lo<=0h && value.hi>=0h) { low=0h; }
    let squared=range_multiply(ScalarRange(low,high),ScalarRange(low,high));
    return ScalarRange(max(0h,squared.lo),max(0h,squared.hi));
}
fn annular_sqrt_bounds(value: f16) -> ScalarRange {
    if (!(value>=0h && value<=32768h)) { fail(FAILURE_ARITHMETIC);return range_value(0h); }
    if (value==0h) { return range_value(0h); }
    if (value<=0.00006103515625h) { return ScalarRange(0h,0.0078125h); }
    let approximate=sqrt(value);var result=range_value(approximate);
    for (var attempt=0u;attempt<32u;attempt++) {
        let lower=range_multiply(range_value(result.lo),range_value(result.lo));
        let upper=range_multiply(range_value(result.hi),range_value(result.hi));
        let lowReady=lower.hi<=value;let highReady=upper.lo>=value;
        if (lowReady && highReady) { return result; }
        if (!lowReady) { result.lo=down_positive(result.lo); }
        if (!highReady) { result.hi=up_nonnegative(result.hi); }
    }
    fail(FAILURE_ARITHMETIC);return result;
}
fn annular_radial_range(position: VectorRange) -> ScalarRange {
    let squared=range_add(annular_square_bounds(ScalarRange(position.lo.y,position.hi.y)),
        annular_square_bounds(ScalarRange(position.lo.z,position.hi.z)));
    return ScalarRange(annular_sqrt_bounds(max(0h,squared.lo)).lo,annular_sqrt_bounds(max(0h,squared.hi)).hi);
}
fn annular_separation(value: ScalarRange, low: ScalarRange, high: ScalarRange) -> f16 {
    return max(0h,max(range_subtract(low,range_value(value.hi)).lo,
        range_subtract(range_value(value.lo),high).lo));
}
// Strict radial clearance: exact coarse square cancellation plus directed small
// residuals. No endpoint-only or convex-shape assumption is used.
fn annular_bore_clearance(bounds: AnnularBounds, inner: f16, radius: f16) -> bool {
    var centreRadius=range_multiply(range_subtract(range_value(inner),range_value(radius)),range_value(16h));
    // Sterbenz subtraction and a finite power-of-two scale are exact here.
    let difference=inner-radius;
    if (inner>0h && radius>0h && inner<=32752h && radius<=32752h &&
        inner<=radius*2h && radius<=inner*2h && difference>=0.00006103515625h && difference<=2047h) {
        centreRadius=range_value(difference*16h);
    }
    if (!(centreRadius.lo>0h)) { return false; }
    let anchors=vec3<f16>(round(centreRadius.lo),round(bounds.coarse.lo.y),round(bounds.coarse.lo.z));
    if (any(abs(anchors)>vec3<f16>(24h))) { return false; }
    // Integral products/signed sums have magnitude<=1728, inside Half's exact
    // integer domain. Their conversion from sixteen-distance units is exact.
    let leading=((anchors.x*anchors.x-anchors.y*anchors.y)-anchors.z*anchors.z)*0.00390625h;
    let r=range_multiply(range_subtract(centreRadius,range_value(anchors.x)),range_value(0.0625h));
    let y=range_multiply(range_add(
        range_subtract(ScalarRange(bounds.coarse.lo.y,bounds.coarse.hi.y),range_value(anchors.y)),
        ScalarRange(bounds.fine.lo.y,bounds.fine.hi.y)),range_value(0.0625h));
    let z=range_multiply(range_add(
        range_subtract(ScalarRange(bounds.coarse.lo.z,bounds.coarse.hi.z),range_value(anchors.z)),
        ScalarRange(bounds.fine.lo.z,bounds.fine.hi.z)),range_value(0.0625h));
    let radial=range_multiply(r,range_add(range_value(anchors.x*0.125h),r));
    let vertical=range_multiply(y,range_add(range_value(anchors.y*0.125h),y));
    let lateral=range_multiply(z,range_add(range_value(anchors.z*0.125h),z));
    let remaining=range_subtract(range_subtract(range_add(range_value(leading),radial),vertical),lateral);
    return remaining.lo>0h;
}
fn annular_clearance(curve: MotionCurve, ordinal: u32, interval: TimeInterval, collider: Collider, radius: f16) -> bool {
    let bounds=annular_local_range(curve,ordinal,interval,collider);
    if (!bounds.valid) { return false; }
    if (annular_bore_clearance(bounds,collider.profile.y,radius)) { return true; }
    let axial=ScalarRange(bounds.position.lo.x,bounds.position.hi.x);
    let end=range_add(range_value(collider.profile.x),range_value(collider.profileTail.x));
    let shoulder=range_subtract(range_value(collider.profile.x),range_value(collider.profileTail.x));
    let radial=annular_radial_range(bounds.position);
    let radiusSquared=range_multiply(range_value(radius),range_value(radius)).hi;
    var lower=65504h;
    for (var solid=0u;solid<3u;solid++) {
        if (solid>0u && collider.profileTail.x==0h) { continue; }
        var low=range_negate(shoulder);var high=shoulder;var outer=collider.profile.z;
        if (solid==1u) { low=range_negate(end);high=range_negate(shoulder);outer=collider.profile.w; }
        if (solid==2u) { low=shoulder;high=end;outer=collider.profile.w; }
        let dx=annular_separation(axial,low,high);
        let dr=annular_separation(radial,range_value(collider.profile.y),range_value(outer));
        let squared=range_add(range_multiply(range_value(dx),range_value(dx)),
            range_multiply(range_value(dr),range_value(dr))).lo;
        lower=min(lower,squared);
    }
    return lower>radiusSquared;
}
// The existing strictly-closing endpoint certificate also applies to a single
// cylindrical interior, once its normal radius and every competing surface are
// enclosed. This does not apply the convex Box distance/Hessian law to a bore.
fn annular_closing_radius(curve: MotionCurve, ordinal: u32, interval: TimeInterval,
    collider: Collider, radius: f16, feature: u32, excludedFamily: u32) -> f16 {
    if (feature>=24u) { return 0h; }
    let endpoint=feature%3u;
    if (endpoint==0u && feature>=12u) { return 0h; }
    if (endpoint!=0u && (!curve.supported || excludedFamily==NO_BODY || excludedFamily==feature/3u)) { return 0h; }
    let bounds = annular_local_range(curve, ordinal, interval, collider);
    if (!bounds.valid) { return 0h; }
    let axial = ScalarRange(bounds.position.lo.x, bounds.position.hi.x);
    let radial = annular_radial_range(bounds.position);
    if (!(radial.lo > 0h)) { return 0h; }
    let end = range_add(range_value(collider.profile.x), range_value(collider.profileTail.x));
    let shoulder = range_subtract(range_value(collider.profile.x), range_value(collider.profileTail.x));
    let family = feature / 3u;
    var low = range_negate(end); var high = end; var surface = collider.profile.y;
    if (family == 1u) { low = range_negate(shoulder); high = shoulder; surface = collider.profile.z; }
    if (family == 2u) { high = range_negate(shoulder); surface = collider.profile.w; }
    if (family == 3u) { low = shoulder; surface = collider.profile.w; }
    var normalRadius=down_positive(radial.lo*0.96875h);
    if (endpoint==0u) {
        if (!(axial.lo>low.hi && axial.hi<high.lo)) { return 0h; }
        if (family==0u) {
            if (!(radial.hi<surface)) { return 0h; }
        } else if (!(radial.lo>surface)) { return 0h; }
    } else {
        // A meridian endpoint rotates into a ring. Prove its clamp domain
        // and remain outside material; no whole-profile exclusion is inferred.
        var ringAxial=low;var ringRadial=range_value(surface);
        if (family<4u) {
            if (endpoint==1u) {
                if (!(axial.hi<low.lo)) { return 0h; }
            } else {
                if (!(axial.lo>high.hi)) { return 0h; }
                ringAxial=high;
            }
        } else {
            ringAxial=range_negate(end);
            var radialLow=collider.profile.y;
            if (family==5u) { ringAxial=end; }
            if (family==6u) { ringAxial=range_negate(shoulder);radialLow=collider.profile.z; }
            if (family==7u) { ringAxial=shoulder;radialLow=collider.profile.z; }
            if (endpoint==1u) {
                if (!(radial.hi<radialLow)) { return 0h; }
                ringRadial=range_value(radialLow);
            } else {
                if (!(radial.lo>collider.profile.w)) { return 0h; }
                ringRadial=range_value(collider.profile.w);
            }
        }
        if (!(radial.hi<collider.profile.y || radial.lo>collider.profile.w ||
            axial.lo>end.hi || axial.hi< -end.hi)) { return 0h; }
        let dx=annular_separation(axial,ringAxial,ringAxial);
        let dr=annular_separation(radial,ringRadial,ringRadial);
        if (dx>4h || dr>4h) { return 0h; }
        let squared=range_add(range_multiply(range_value(dx),range_value(dx)),
            range_multiply(range_value(dr),range_value(dr)));
        let distance=annular_sqrt_bounds(max(0h,squared.lo)).lo;
        let band=radius*CONTACT_BAND_FRACTION;
        if (!(distance>range_subtract(range_value(radius),range_value(band)).hi &&
            radius>band)) { return 0h; }
        // Ring normal variation is bounded by 1/distance + 1/rho.
        normalRadius=min(distance,normalRadius);
    }
    let radiusSquared = range_multiply(range_value(radius), range_value(radius)).hi;
    for (var other = 0u; other < 8u; other++) {
        if (other==family || (endpoint!=0u && other==excludedFamily) ||
            (collider.profileTail.x==0h && (other==2u || other==3u || other>=6u))) { continue; }
        var axialLow = range_negate(end); var axialHigh = end;
        var radialLow = range_value(collider.profile.y); var radialHigh = radialLow;
        switch other {
            case 0u: {}
            case 1u: {
                axialLow = range_negate(shoulder); axialHigh = shoulder;
                radialLow = range_value(collider.profile.z); radialHigh = radialLow;
            }
            case 2u: {
                axialHigh = range_negate(shoulder);
                radialLow = range_value(collider.profile.w); radialHigh = radialLow;
            }
            case 3u: {
                axialLow = shoulder;
                radialLow = range_value(collider.profile.w); radialHigh = radialLow;
            }
            case 4u: { axialHigh = axialLow; radialHigh = range_value(collider.profile.w); }
            case 5u: { axialLow = end; axialHigh = end; radialHigh = range_value(collider.profile.w); }
            case 6u: {
                axialLow = range_negate(shoulder); axialHigh = axialLow;
                radialLow = range_value(collider.profile.z); radialHigh = range_value(collider.profile.w);
            }
            default: {
                axialLow = shoulder; axialHigh = shoulder;
                radialLow = range_value(collider.profile.z); radialHigh = range_value(collider.profile.w);
            }
        }
        let dx = annular_separation(axial, axialLow, axialHigh);
        let dr = annular_separation(radial, radialLow, radialHigh);
        // One separated axis already proves clearance; remaining products are
        // bounded by radius<=2, including distant mouths and collars.
        if (dx > radius || dr > radius) { continue; }
        let squared = range_add(range_multiply(range_value(dx), range_value(dx)),
            range_multiply(range_value(dr), range_value(dr))).lo;
        if (!(squared > radiusSquared)) { return 0h; }
    }
    return normalRadius;
}
fn clearance_endpoint(curve: MotionCurve, ordinal: u32, phase: f16,
    collider: Collider, radius: f16) -> f16 {
    let body = curve.body;
    let frame = candidate.state.bodies[collider.body];
    let cells = body.segmentCell - frame.cell;
    // This optional near-field certificate never narrows large integer frames.
    if (any(abs(cells) > vec3<i32>(128))) { return -65504h; }
    let fraction = range_multiply(range_subtract(range_value(phase),
        range_value(body.segmentLocalPhase.w)), range_value(0.000244140625h));
    let elapsedSteps = range_add(range_value(f16(i32(ordinal) - i32(body.segmentOrdinal))), fraction);
    // u=32t keeps the quadratic product normal even for short physical intervals.
    let scaledRate = PHYSICAL_RATE / 32h;
    let u = ScalarRange(max(0h, range_lower(elapsedSteps.lo / scaledRate)),
        range_upper(elapsedSteps.hi / scaledRate));
    if (u.lo < 0h || u.hi > 64h) { return -65504h; }
    let v0 = body.segmentVelocity.xyz * 32h;
    var acceleration = vector_add(vector_value(gravity(body)),
        vector_scale(vector_value(-v0), range_value(body.massDragGravityXY.y)));
    var x = range_multiply(u, range_multiply(range_value(body.massDragGravityXY.y), range_value(0.03125h)));
    if (curve.supported || curve.forceDriven) { acceleration = vector_value(curve.acceleration); x = range_value(0h); }
    if (curve.forceDriven) {
        acceleration = vector_add(acceleration, VectorRange(vec3<f16>(-curve.accelerationError), vec3<f16>(curve.accelerationError)));
    }
    var polynomial = range_value(0.0013885498046875h);
    polynomial = range_add(range_value(-0.008331298828125h), range_multiply(x, polynomial));
    polynomial = range_add(range_value(0.041656494140625h), range_multiply(x, polynomial));
    polynomial = range_add(range_value(-0.1666259765625h), range_multiply(x, polynomial));
    polynomial = range_add(range_value(0.5h), range_multiply(x, polynomial));
    let quadratic = range_multiply(range_multiply(u, u), polynomial);
    // Local displacement is 16*(v0*t+a*t*t*P), expressed without tiny t*t.
    let displacement = vector_add(vector_scale(vector_value(v0 * 0.5h), u),
        vector_scale(vector_scale(acceleration, range_value(0.015625h)), quadratic));
    var fine = vector_add(vector_value(body.segmentLocalPhase.xyz),
        vector_add(vector_value(-frame.local.xyz), displacement));
    if (any(abs(fine.lo) > vec3<f16>(128h)) || any(abs(fine.hi) > vec3<f16>(128h))) {
        return -65504h;
    }
    if (curve.eventPositionError > 0h) {
        let error = up_nonnegative(curve.eventPositionError * 16h);
        fine = vector_add(fine,VectorRange(vec3<f16>(-error),vec3<f16>(error)));
    }
    var coarse = vector_inverse_rotate(frame.rotation, vector_value(vec3<f16>(cells)));
    fine = vector_inverse_rotate(frame.rotation, fine);
    fine = vector_add(fine, vector_value(-collider.translation.xyz * 16h));
    coarse = vector_inverse_rotate(collider.rotation, coarse);
    fine = vector_inverse_rotate(collider.rotation, fine);
    // Exact power-of-two scale: squared clearance below is in 1024 U^2 units.
    coarse.lo *= 2h; coarse.hi *= 2h; fine.lo *= 2h; fine.hi *= 2h;
    var outside: array<ScalarRange, 3>;
    var minusRadius: array<ScalarRange, 3>;
    var dominant = 0u;
    for (var axis = 0u; axis < 3u; axis++) {
        outside[axis] = range_value(0h);
        minusRadius[axis] = range_value(-radius * 32h);
        if (collider.shape == SHAPE_PLANE && axis != 1u) { continue; }
        var c = ScalarRange(coarse.lo[axis], coarse.hi[axis]);
        var f = ScalarRange(fine.lo[axis], fine.hi[axis]);
        var extent = 0h;
        if (collider.shape == SHAPE_BOX) {
            extent = collider.dimensions[axis + 1u] * 32h;
            let joined = range_add(c, f);
            if (joined.hi < 0h) { c = range_negate(c); f = range_negate(f); }
            else if (joined.lo < 0h) {
                // A sign-changing axis can only contribute a zero lower distance.
                continue;
            }
        }
        let face = range_subtract(c, range_value(extent));
        let d = range_add(face, f);
        outside[axis] = ScalarRange(max(0h, d.lo), max(0h, d.hi));
        // Subtract radius before adding the small local term; no nearly equal squares.
        minusRadius[axis] = range_add(range_subtract(face, range_value(radius * 32h)), f);
        if (outside[axis].lo > outside[dominant].lo) { dominant = axis; }
    }
    if (!(outside[dominant].lo > 0h)) { return -65504h; }
    for (var axis = 0u; axis < 3u; axis++) {
        if (outside[axis].hi > 128h) { return -65504h; }
    }
    let plusRadius = range_add(outside[dominant], range_value(radius * 32h));
    var lower = range_multiply(minusRadius[dominant], plusRadius).lo;
    for (var axis = 0u; axis < 3u; axis++) {
        if (axis != dominant) {
            lower = range_lower(lower + down_positive(outside[axis].lo * outside[axis].lo));
        }
    }
    return lower;
}
fn clearance_interval(curve: MotionCurve, ordinal: u32, interval: TimeInterval,
    collider: Collider, radius: f16, distanceBound: f16, speedBound: f16, accelerationBound: f16) -> bool {
    if (!(distanceBound > 0h && distanceBound <= 4h)) { return false; }
    let left = clearance_endpoint(curve, ordinal, interval.left, collider, radius);
    let right = clearance_endpoint(curve, ordinal, interval.right, collider, radius);
    if (!(min(left, right) > 0h)) { return false; }
    // Squared distance to a convex box has Hessian <=2I, including feature changes.
    // F'' <=2(V^2+D*A); the endpoint chord falls by at most M*h^2/8.
    let v = up_nonnegative(speedBound / (PHYSICAL_RATE / 16h));
    let d = up_nonnegative(distanceBound / (PHYSICAL_RATE / 16h));
    let a = up_nonnegative(accelerationBound / (PHYSICAL_RATE / 16h));
    let rate = up_nonnegative(up_nonnegative(v * v) + up_nonnegative(d * a));
    let fraction = up_nonnegative((interval.right - interval.left) / PHASE_SCALE);
    let loss = up_nonnegative(rate * up_nonnegative(fraction * fraction));
    return min(left, right) > loss;
}
fn collision(curve: MotionCurve, ordinal: u32, start: f16, end: f16,
    colliderIndex: u32, radius: f16, excludedFamily: u32) -> Hit {
    let collider = candidate.state.colliders[colliderIndex];
    let band = radius * CONTACT_BAND_FRACTION;
    var stack: array<TimeInterval, 32>;
    var count = 1u; stack[0] = TimeInterval(start, end, 0u);
    for (var iteration = 0u; iteration < ADVANCEMENT_BUDGET; iteration++) {
        if (count == 0u) { return no_hit(); }
        count--; let interval = stack[count];
        let a = curve_query(curve, ordinal, interval.left); let b = curve_query(curve, ordinal, interval.right);
        if (a.failure != FAILURE_NONE || b.failure != FAILURE_NONE) {
            var h = no_hit(); h.failure = FAILURE_DOMAIN; return h;
        }
        var da=shape_query(a,collider,radius);var db=shape_query(b,collider,radius);
        if (collider.shape==SHAPE_ANNULAR && excludedFamily!=NO_BODY) {
            da=annular_query(collider_point(a,collider),collider,radius,excludedFamily);
            db=annular_query(collider_point(b,collider),collider,radius,excludedFamily);
        }
        if (da.failure != FAILURE_NONE || db.failure != FAILURE_NONE) {
            var h = no_hit(); h.failure = FAILURE_PAIR; return h;
        }
        if (collider.shape == SHAPE_ANNULAR && annular_clearance(curve,ordinal,interval,collider,radius)) { continue; }
        let vn = dot(a.velocity, da.normal);
        if (da.gap <= 0h && vn < 0h) {
            return Hit(FAILURE_NONE, true, interval.left, colliderIndex, a, da);
        }
        let phaseWidth = interval.right - interval.left;
        let acceleration = select(gravity(curve.body) - curve.body.massDragGravityXY.y *
            (curve.body.segmentVelocity.xyz * 32h), curve.acceleration, curve.supported || curve.forceDriven);
        let accelerationBound = up_nonnegative(up_nonnegative(length3(acceleration) + curve.accelerationError) * 1.03125h);
        let accelerationDelta = up_nonnegative((phaseWidth / PHASE_SCALE) *
            up_nonnegative(accelerationBound / PHYSICAL_RATE));
        // Velocity is v0+a0*f(t), monotone f on the admitted force domain. End norms
        // plus this curve's interval acceleration/rounding allowance bound the interval.
        let localSpeed = min(SPEED_BOUND,
            up_nonnegative(max(length3(a.velocity), length3(b.velocity)) * 1.03125h + accelerationDelta + curve.eventVelocityError));
        let scale = max(radius, max(abs(da.gap), abs(db.gap)));
        let guard = up_nonnegative(up_nonnegative(scale * 0.0078125h) + curve.eventPositionError);
        let endpointLower = down_signed(max(da.gap, db.gap) - guard);
        // Even the global128U/s bound travels less than.267U per physical substep.
        if (endpointLower > 1h) { continue; }
        // Travel remains in4096 distance units; no subnormal seconds value is retained.
        let travel = up_nonnegative((phaseWidth / 8h) * up_nonnegative(localSpeed / (PHYSICAL_RATE / 8h)));
        if (endpointLower < -8h) { var h = no_hit(); h.failure = FAILURE_RESIDUAL; return h; }
        let enclosedGap = down_signed(down_signed(endpointLower * PHASE_SCALE) - travel);
        let lower = down_positive(max(0h, enclosedGap));
        // The overlap/correction band does not activate restitution at a positive gap.
        if (lower > 0h) { continue; }
        if (collider.shape == SHAPE_ANNULAR && da.gap > 0h && db.gap > 0h &&
            da.feature == db.feature && enclosedGap >= -band * PHASE_SCALE) {
            let radialDistance = annular_closing_radius(curve, ordinal, interval, collider, radius, da.feature, excludedFamily);
            let distance = min(down_positive(radius - band), radialDistance);
            if (distance > 0h) {
                let normalTravel = up_nonnegative(up_nonnegative(travel / PHASE_SCALE) +
                    up_nonnegative(2h * guard));
                let normalDelta = up_nonnegative(2h * up_nonnegative(normalTravel / distance));
                let variation = up_nonnegative(accelerationDelta +
                    up_nonnegative(normalDelta * localSpeed) + up_nonnegative(localSpeed * 0.03125h));
                if (dot(b.velocity, db.normal) < -variation) { continue; }
                if (da.feature == 0u) {
                    // For the inner cylinder g=R-rho, g''=n.a-v_circ^2/rho.
                    // Dropping its nonpositive curvature leaves a conservative
                    // upper bound; this is not the convex Box Hessian law.
                    var projected = max(dot(acceleration, da.normal), dot(acceleration, db.normal));
                    if (!curve.supported && !curve.forceDriven &&
                        curve.body.massDragGravityXY.y > 0h && projected < 0h) {
                        // The declared free polynomial has acceleration factor
                        // >=1-x>=.75 throughout its admitted x<=.25 domain.
                        projected = range_multiply(range_value(projected), range_value(0.75h)).hi;
                    }
                    let uncertainty = up_nonnegative(
                        up_nonnegative(accelerationBound * normalDelta) +
                        up_nonnegative(accelerationBound * 0.03125h) + curve.accelerationError);
                    if (range_upper(projected + uncertainty) < 0h) { continue; }
                }
            }
        }
        if (collider.shape != SHAPE_ANNULAR && da.gap > 0h && db.gap > 0h && enclosedGap >= -band * PHASE_SCALE) {
            // A strictly closing interval has its minimum at the right endpoint.
            // Keep this complementary certificate when endpoint arithmetic cannot
            // resolve a positive near-root gap; it encloses normal rotation as well.
            let distance = down_positive(radius - band);
            let normalTravel = up_nonnegative(up_nonnegative(travel / PHASE_SCALE) +
                up_nonnegative(2h * guard));
            let normalDelta = up_nonnegative(2h * up_nonnegative(normalTravel / distance));
            let variation = up_nonnegative(accelerationDelta +
                up_nonnegative(normalDelta * localSpeed) +
                up_nonnegative(localSpeed * 0.03125h));
            if (dot(b.velocity, db.normal) < -variation) { continue; }
            // A positive-gap rebound apex needs no impulse. Outside the convex
            // static shape, distance curvature is at most speed^2 / distance.
            // A negative upper bound makes the minimum an interval endpoint.
            if (localSpeed < down_positive(sqrt(accelerationBound * distance))) {
                var projected = max(dot(acceleration, da.normal), dot(acceleration, db.normal));
                // Free acceleration factor >= 1-x on x=k*t in[0,.25].
                // Derive x from this interval; k=0 and supported curves are exactly one.
                let drag = curve.body.massDragGravityXY.y;
                if (!curve.supported && !curve.forceDriven && projected < 0h && drag > 0h) {
                    let whole = i32(ordinal) - i32(curve.body.segmentOrdinal);
                    let fraction = (interval.right - curve.body.segmentLocalPhase.w) / PHASE_SCALE;
                    let elapsed = (f16(whole) + fraction) / PHYSICAL_RATE;
                    let xUpper = min(0.25h, up_nonnegative(drag * up_nonnegative(elapsed)));
                    projected *= down_positive(1h - xUpper);
                }
                let curvature = up_nonnegative(localSpeed * up_nonnegative(localSpeed / distance));
                let uncertainty = up_nonnegative(up_nonnegative(accelerationBound * normalDelta) +
                    curvature + up_nonnegative(accelerationBound * 0.03125h));
                if (projected < -uncertainty) { continue; }
            }
            let distanceBound = up_nonnegative(up_nonnegative(radius + max(da.gap, db.gap)) +
                up_nonnegative(guard + up_nonnegative(travel / PHASE_SCALE)));
            if (clearance_interval(curve, ordinal, interval, collider, radius,
                distanceBound, localSpeed, accelerationBound)) { continue; }
        }
        // Rounded tangency may plateau at zero after feature release. Retire that
        // separating interval only when its enclosure also limits any unresolved
        // penetration to the existing contact band; no exact no-root claim is made.
        if (da.gap <= band && vn >= 0h &&
            dot(b.velocity, db.normal) >= 0h && enclosedGap >= -band * PHASE_SCALE) { continue; }
        let middle = interval.left + (interval.right - interval.left) * 0.5h;
        if (middle == interval.left || middle == interval.right || interval.depth >= 20u) {
            if (db.gap <= 0h && dot(b.velocity, db.normal) < 0h) {
                return Hit(FAILURE_NONE, true, interval.right, colliderIndex, b, db);
            }
            var h = no_hit(); h.failure = FAILURE_ROOT_BUDGET; return h;
        }
        if (count + 2u > 32u) { var h = no_hit(); h.failure = FAILURE_ROOT_BUDGET; return h; }
        stack[count] = TimeInterval(middle, interval.right, interval.depth + 1u); count++;
        stack[count] = TimeInterval(interval.left, middle, interval.depth + 1u); count++;
    }
    var h = no_hit(); h.failure = FAILURE_ROOT_BUDGET; return h;
}
fn valid_header() -> bool {
    return candidate.state.version == STATE_VERSION && candidate.state.status == STATUS_COMMITTED &&
        candidate.state.failure == FAILURE_NONE && candidate.state.bodyCount <= BODY_CAPACITY &&
        candidate.state.colliderCount <= COLLIDER_CAPACITY && candidate.state.materialCount <= MATERIAL_CAPACITY &&
        candidate.state.sensorCount <= SENSOR_CAPACITY && candidate.state.guideCount <= GUIDE_CAPACITY && candidate.state.triggerCount <= TRIGGER_CAPACITY && any(candidate.state.epoch != vec2<u32>(0u)) &&
        any(candidate.state.document != vec4<u32>(0u)) && any(candidate.state.cadenceRevision != vec2<u32>(0u)) &&
        candidate.state.physical == PHYSICAL_480 && steps(candidate.state.cadence) != 0u &&
        candidate.state.tick.y == 0u && candidate.state.tick.x <= 14400u / steps(candidate.state.cadence) &&
        candidate.state.ordinal == candidate.state.tick.x * steps(candidate.state.cadence);
}
fn validate_scene() -> bool {
    if (!valid_header()) { fail(FAILURE_DECLARATION); return false; }
    var dynamics = 0u;
    for (var i = 0u; i < candidate.state.bodyCount; i++) {
        let b = candidate.state.bodies[i];
        if (all(b.id == vec2<u32>(0u)) || b.motion > MOTION_DYNAMIC ||
            any(abs(b.cell) > vec3<i32>(1024)) || any(b.local.xyz < vec3<f16>(-0.5h)) ||
            any(b.local.xyz >= vec3<f16>(0.5h)) || !finite3(b.local.xyz) ||
            !finite3(b.velocity.xyz) || length3(b.velocity.xyz) > 2h ||
            !finite3(b.angular.xyz) || length3(b.angular.xyz) > 64h ||
            !(dot(b.rotation, b.rotation) >= 0.998h && dot(b.rotation, b.rotation) <= 1.002h)) {
            fail(FAILURE_DECLARATION); return false;
        }
        if (b.motion == MOTION_DYNAMIC) {
            dynamics++;
            if (candidate.state.dynamicBody != i || b.phase > MOTION_SUPPORTED || b.supportCount > 4u ||
                !(b.massDragGravityXY.x >= 0.0009765625h && b.massDragGravityXY.x <= 1024h) ||
                !(b.massDragGravityXY.y >= 0h && b.massDragGravityXY.y <= 0.125h) ||
                !finite3(gravity(b)) || length3(gravity(b)) > 16h) { fail(FAILURE_DOMAIN); return false; }
        } else if (!zero4(b.velocity) || !zero4(b.angular) ||
            !zero4(b.massDragGravityXY) || !zero4(b.gravityZPad)) {
            fail(FAILURE_DECLARATION); return false;
        }
    }
    if (dynamics > 1u || (dynamics == 0u && candidate.state.dynamicBody != NO_BODY)) {
        fail(FAILURE_PAIR); return false;
    }
    for (var i = 0u; i < candidate.state.materialCount; i++) {
        let m = candidate.state.materials[i].values;
        if (!finite3(m.xyz) || m.x < 0h || m.x > 1h || m.y < 0h || m.y > 64h ||
            m.z < 0h || m.z > 1h || !positive_zero(m.w)) {
            fail(FAILURE_DECLARATION); return false;
        }
    }
    for (var i = 0u; i < candidate.state.colliderCount; i++) {
        let c = candidate.state.colliders[i];
        if (c.body >= candidate.state.bodyCount || c.material >= candidate.state.materialCount ||
            c.shape > SHAPE_ANNULAR || !finite3(c.translation.xyz) ||
            any(abs(c.translation.xyz) > vec3<f16>(16h)) ||
            !(dot(c.rotation, c.rotation) >= 0.998h && dot(c.rotation, c.rotation) <= 1.002h)) {
            fail(FAILURE_DECLARATION); return false;
        }
        if (c.shape == SHAPE_ANNULAR) {
            let p = c.profile; let width = c.profileTail.x;
            if (!zero4(c.dimensions) || !finite3(p.xyz) || !finite(p.w) || !finite(width) ||
                !(p.x >= 0.0625h && p.x <= 4h && p.y >= 0.0625h && p.y <= 2h &&
                  p.z > p.y && p.z <= 4h && p.w >= p.z && p.w <= 4h && width >= 0h && width <= 0.25h && p.x > width) ||
                ((width == 0h) != (p.w == p.z)) || !positive_zero(c.profileTail.y) ||
                !positive_zero(c.profileTail.z) || !positive_zero(c.profileTail.w)) {
                fail(FAILURE_DECLARATION); return false;
            }
        } else if (!zero4(c.profile) || !zero4(c.profileTail)) { fail(FAILURE_DECLARATION); return false; }
        if (candidate.state.bodies[c.body].motion == MOTION_DYNAMIC) {
            if (c.shape != SHAPE_SPHERE || !(c.dimensions.x >= 0.0625h && c.dimensions.x <= 2h) ||
                !zero4(c.translation)) { fail(FAILURE_PAIR); return false; }
        } else if (c.shape == SHAPE_SPHERE ||
            (c.shape == SHAPE_BOX && (!finite3(c.dimensions.yzw) || any(c.dimensions.yzw < vec3<f16>(0.0009765625h)) ||
                any(c.dimensions.yzw > vec3<f16>(4h))))) { fail(FAILURE_PAIR); return false; }
    }
    if (dynamics == 1u && sphere_collider(candidate.state.dynamicBody) == NO_BODY) {
        fail(FAILURE_DECLARATION); return false;
    }
    for (var i = 0u; i < candidate.state.sensorCount; i++) {
        let s = candidate.state.sensors[i];
        if (s.frame >= candidate.state.bodyCount || s.targetBody != candidate.state.dynamicBody ||
            candidate.state.bodies[s.frame].motion != MOTION_STATIC || s.frame == s.targetBody ||
            s.enabled > SENSOR_ENABLED || !finite3(s.translation.xyz) ||
            any(abs(s.translation.xyz) > vec3<f16>(16h)) ||
            !(dot(s.rotation, s.rotation) >= 0.998h && dot(s.rotation, s.rotation) <= 1.002h) ||
            !finite3(s.low.xyz) || !finite3(s.high.xyz) ||
            any(abs(s.low.xyz) > vec3<f16>(16h)) || any(abs(s.high.xyz) > vec3<f16>(16h)) ||
            any(s.low.xyz >= s.high.xyz) ||
            !(s.controls.x >= 0.0009765625h && s.controls.x <= 64h) ||
            !(s.controls.y >= 0.0009765625h && s.controls.y <= 30h) ||
            s.phase > RESIDENCE_QUALIFIED || s.sequence > 1u) { fail(FAILURE_DECLARATION); return false; }
    }
    for (var i = 0u; i < candidate.state.guideCount; i++) {
        let g = candidate.state.guides[i];
        if (g.frame >= candidate.state.bodyCount || g.targetBody != candidate.state.dynamicBody ||
            candidate.state.bodies[g.frame].motion != MOTION_STATIC || g.frame == g.targetBody ||
            all(g.id == vec2<u32>(0u)) || !finite3(g.translation.xyz) || any(abs(g.translation.xyz) > vec3<f16>(16h)) ||
            !(dot(g.rotation, g.rotation) >= 0.998h && dot(g.rotation, g.rotation) <= 1.002h) ||
            !finite3(g.low.xyz) || !finite3(g.high.xyz) || any(g.low.xyz >= g.high.xyz) ||
            any(abs(g.low.xyz) > vec3<f16>(16h)) || any(abs(g.high.xyz) > vec3<f16>(16h)) ||
            !(g.controls.x >= 0h && g.controls.x <= 12h) ||
            !(g.controls.y >= -16h && g.controls.y <= 16h) || !(g.controls.z >= 0h && g.controls.z <= 1h)) {
            fail(FAILURE_DECLARATION); return false;
        }
        for (var j = 0u; j < i; j++) {
            if (candidate.state.guides[j].targetBody == g.targetBody) { fail(FAILURE_DECLARATION); return false; }
        }
    }
    for (var i = 0u; i < candidate.state.triggerCount; i++) {
        let trigger = candidate.state.triggers[i];
        if (all(trigger.id == vec2<u32>(0u)) || trigger.owner >= candidate.state.bodyCount ||
            trigger.targetBody != candidate.state.dynamicBody || trigger.targetBody >= candidate.state.bodyCount ||
            trigger.owner == trigger.targetBody || candidate.state.bodies[trigger.owner].motion != MOTION_STATIC ||
            !(trigger.controls.x >= 0h && trigger.controls.x <= 64h) || trigger.sequence > 1u) {
            fail(FAILURE_DECLARATION); return false;
        }
    }
    return candidate.state.failure == FAILURE_NONE;
}

fn tangent_axis(normal: vec3<f16>) -> vec3<f16> {
    let axis = select(vec3<f16>(1h, 0h, 0h), vec3<f16>(0h, 1h, 0h), abs(normal.x) > 0.75h);
    return unit(cross(normal, axis));
}
fn supported_collider(slot: u32, collider: u32) -> bool {
    for (var i = 0u; i < candidate.state.bodies[slot].supportCount; i++) {
        if (candidate.state.bodies[slot].support[i].collider == collider) { return true; }
    }
    return false;
}
var<private> supportTrialRetry: bool;
var<private> supportAnnularTrial: bool;

fn collider_vector(value: vec3<f16>, collider: Collider) -> vec3<f16> {
    return inverse_rotate(collider.rotation,inverse_rotate(candidate.state.bodies[collider.body].rotation,value));
}
fn annular_curved_feature(collider: Collider, feature: u32) -> bool {
    return collider.shape==SHAPE_ANNULAR && feature<12u && feature%3u==0u;
}
fn annular_midpoint_normal(initial: MotionQuery, velocity: vec3<f16>, h: f16, collider: Collider, feature: u32) -> vec3<f16> {
    let point=collider_point(initial,collider)+collider_vector(initial.velocity+velocity,collider)*(h*0.25h);
    let radial=unit(vec3<f16>(0h,point.y,point.z));
    return collider_normal(radial*select(1h,-1h,feature==0u),collider);
}
fn annular_required_normal_acceleration(initial: MotionQuery, collider: Collider, feature: u32) -> f16 {
    let point=collider_point(initial,collider);let rho=length3(vec3<f16>(0h,point.y,point.z));
    if (!(rho>=0.00006103515625h)) { fail(FAILURE_DOMAIN);return 0h; }
    let radial=vec3<f16>(0h,point.y,point.z)/rho;
    let velocity=collider_vector(initial.velocity,collider);
    let circumferential=vec3<f16>(0h,velocity.y,velocity.z)-radial*dot(velocity,radial);
    let speed=length3(circumferential);
    // Reject unsupported curvature instead of overflowing a Half square.
    if (speed>sqrt(64h*rho)) { return select(-65504h,65504h,feature==0u); }
    return select(-1h,1h,feature==0u)*(speed/rho)*speed;
}
fn annular_owned_piece(curve: MotionCurve, ordinal: u32, begin: f16, end: f16,
    collider: Collider, feature: u32, radius: f16) -> bool {
    if (collider.shape!=SHAPE_ANNULAR || feature%3u!=0u || feature>=24u) { return false; }
    let bounds=annular_local_range(curve,ordinal,TimeInterval(begin,end,0u),collider);
    if (!bounds.valid) { return false; }
    let family=feature/3u;
    let axialEnd=range_add(range_value(collider.profile.x),range_value(collider.profileTail.x));
    let shoulder=range_subtract(range_value(collider.profile.x),range_value(collider.profileTail.x));
    var low=range_negate(axialEnd);var high=axialEnd;
    var surface=collider.profile.y;
    if (family==1u) { low=range_negate(shoulder);high=shoulder;surface=collider.profile.z; }
    if (family==2u) { high=range_negate(shoulder);surface=collider.profile.w; }
    if (family==3u) { low=shoulder;surface=collider.profile.w; }
    let radial=annular_radial_range(bounds.position);
    let band=radius*CONTACT_BAND_FRACTION;
    if (family>=4u) {
        var radialLow=collider.profile.y;
        var face=range_negate(axialEnd);var direction=-1h;
        if (family==5u) { face=axialEnd;direction=1h; }
        if (family==6u) { face=range_negate(shoulder);direction=1h;radialLow=collider.profile.z; }
        if (family==7u) { face=shoulder;direction=-1h;radialLow=collider.profile.z; }
        if (radial.lo<radialLow || radial.hi>collider.profile.w) { return false; }
        let centre=range_add(face,range_value(direction*radius));
        let deviation=range_subtract(ScalarRange(bounds.position.lo.x,bounds.position.hi.x),centre);
        return deviation.lo>=-band && deviation.hi<=band;
    }
    if (bounds.position.lo.x<low.hi || bounds.position.hi.x>high.lo) { return false; }
    var centreRadius=range_add(range_value(surface),range_value(radius));
    if (family==0u) { centreRadius=range_subtract(range_value(surface),range_value(radius)); }
    let deviation=range_subtract(radial,centreRadius);
    return deviation.lo>=-band && deviation.hi<=band;
}
fn supported_annular_family(slot: u32, collider: u32) -> u32 {
    for (var i=0u;i<candidate.state.bodies[slot].supportCount;i++) {
        let contact=candidate.state.bodies[slot].support[i];
        if (contact.collider==collider) { return contact.feature/3u; }
    }
    return NO_BODY;
}
fn support_curve(slot: u32, ordinal: u32, phase: f16, sphere: Collider, inelasticCollider: u32, end: f16) -> MotionCurve {
    supportTrialRetry=false;supportAnnularTrial=false;
    let original = candidate.state.bodies[slot];
    var result = MotionCurve(original, false, false, vec3<f16>(0h), vec3<f16>(0h), 0h, 0h, 0h);
    let initial = motion(original, ordinal, phase);
    if (initial.failure != FAILURE_NONE) { fail(initial.failure); return result; }
    candidate.state.bodies[slot].supportCount = 0u;
    candidate.state.bodies[slot].phase = MOTION_FREE;
    for (var i = 0u; i < 4u; i++) { candidate.state.bodies[slot].support[i] = ContactSlot(0u, 0u, 0h, 0h, 0h, 0h); }
    var normals: array<vec3<f16>, 4>;
    var surfaces: array<SurfacePair, 4>;
    let guide = guide_midpoint(original, initial, ordinal, phase, sphere.dimensions.x,end);
    let acceleration = guide.acceleration;
    if (guide.enabled) {
        var body = original;
        body.segmentCell = initial.cell; body.segmentOrdinal = ordinal;
        body.segmentLocalPhase = vec4<f16>(initial.local, phase);
        if (phase >= 2048h) { body.segmentOrdinal++; body.segmentLocalPhase.w -= PHASE_SCALE; }
        body.segmentVelocity = vec4<f16>(initial.velocity / 32h,0h);
        body.segmentAngular = vec4<f16>(initial.angular,0h); body.segmentRotation = initial.rotation;
        result = MotionCurve(body, false, true, acceleration, vec3<f16>(0h), guide.error, 0h, 0h);
    }
    for (var i = 0u; i < candidate.state.colliderCount; i++) {
        if (candidate.state.colliders[i].body == slot) { continue; }
        let q = shape_query(initial, candidate.state.colliders[i], sphere.dimensions.x);
        if (q.failure != FAILURE_NONE) { fail(q.failure); return result; }
        let vn = dot(initial.velocity, q.normal);
        // Outgoing impacts retain their free trajectory. Zero normal motion under a closing
        // force becomes support. An owned neutral contact remains in the coupled solve
        // because another contact can drive it; positive separating force releases it.
        // The impulse stage handles genuinely incoming velocity first.
        var ownsZeroNormal = inelasticCollider == i;
        for (var prior = 0u; prior < original.supportCount; prior++) {
            if (original.support[prior].collider == i && original.support[prior].feature == q.feature) { ownsZeroNormal = true; }
        }
        // Curvature is defined only for an eligible near-contact centre; a
        // separated point on the bore axis has no radial normal to differentiate.
        if (q.gap>sphere.dimensions.x*CONTACT_BAND_FRACTION ||
            (vn>0h && !ownsZeroNormal) || abs(vn)>0.0009765625h) { continue; }
        let curved=annular_curved_feature(candidate.state.colliders[i],q.feature);
        var normalAcceleration=0h;
        if (curved) {
            normalAcceleration=annular_required_normal_acceleration(initial,candidate.state.colliders[i],q.feature);
        }
        if (candidate.state.colliders[i].shape==SHAPE_ANNULAR && q.feature%3u!=0u) { continue; }
        if (dot(acceleration, q.normal) > normalAcceleration ||
            (dot(acceleration, q.normal) == normalAcceleration && !ownsZeroNormal) ||
            (curved && q.gap>0h && !ownsZeroNormal)) { continue; }
        if (curved && normalAcceleration>64h) { fail(FAILURE_DOMAIN);return result; }
        let count = candidate.state.bodies[slot].supportCount;
        if (count >= 4u) { fail(FAILURE_CONTACT_BUDGET); return result; }
        let material = surface_pair(sphere.material, candidate.state.colliders[i].material);
        if (material.failure != FAILURE_NONE) { fail(material.failure); return result; }
        normals[count] = q.normal; surfaces[count] = material;
        supportAnnularTrial=supportAnnularTrial || candidate.state.colliders[i].shape==SHAPE_ANNULAR;
        candidate.state.bodies[slot].support[count] = ContactSlot(i, q.feature, 0h, 0h, 0h, 0h);
        candidate.state.bodies[slot].supportCount = count + 1u;
    }
    let count = candidate.state.bodies[slot].supportCount;
    if (count == 0u) { return result; }
    // Time unit is1/512s. The remaining phase is at least two units below4096,
    // so hUnits remains normal even when its conversion to seconds would flush.
    let hUnits = (end - phase) / (8h * PHYSICAL_RATE);
    if (!(hUnits >= 0.00048828125h && hUnits <= 1.0673828125h)) { fail(FAILURE_DOMAIN); return result; }
    let h = ldexp(hUnits, -9);
    var velocity = initial.velocity + acceleration * h;
    var omega = initial.angular;
    var normalVectors:array<vec3<f16>,4>;
    var tangentVectors:array<vec3<f16>,4>;
    var angularVectors:array<vec3<f16>,4>;
    for (var sweep = 0u; sweep < 4u; sweep++) {
        for (var i = 0u; i < count; i++) {
            var contact = candidate.state.bodies[slot].support[i];
            let collider=candidate.state.colliders[contact.collider];
            if (annular_curved_feature(collider,contact.feature)) {
                let normal=annular_midpoint_normal(initial,velocity,h,collider,contact.feature);
                let freeVelocity=velocity-normalVectors[i]-tangentVectors[i];
                let freeOmega=omega-angularVectors[i];
                let normalImpulse=max(0h,-dot(initial.velocity,normal)-dot(freeVelocity,normal));
                let normalVector=normal*normalImpulse;
                let v=freeVelocity+normalVector;
                let t0=tangent_axis(normal);let t1=cross(normal,t0);
                let contactVelocity=v+cross(freeOmega,-sphere.dimensions.x*normal);
                var tangent=-vec2<f16>(dot(contactVelocity,t0),dot(contactVelocity,t1))/3.5h;
                let magnitude=length3(vec3<f16>(tangent,0h));
                let limit=surfaces[i].friction*normalImpulse;
                if (magnitude>limit && magnitude>=0.00006103515625h) { tangent*=limit/magnitude; }
                let tangentVector=t0*tangent.x+t1*tangent.y;
                let angularVector=cross(-normal,tangentVector)*(2.5h/sphere.dimensions.x);
                velocity=v+tangentVector;omega=freeOmega+angularVector;
                normalVectors[i]=normalVector;tangentVectors[i]=tangentVector;angularVectors[i]=angularVector;
                normals[i]=normal;
                contact.normalImpulse=normalImpulse;contact.tangent0=tangent.x;contact.tangent1=tangent.y;
                candidate.state.bodies[slot].support[i]=contact;
                continue;
            }
            let normal = normals[i];
            let nextNormal = max(0h, contact.normalImpulse - dot(velocity, normal));
            let deltaNormal = nextNormal - contact.normalImpulse;
            velocity += normal * deltaNormal; contact.normalImpulse = nextNormal;
            let t0 = tangent_axis(normal); let t1 = cross(normal, t0);
            let pointVelocity = velocity + cross(omega, -sphere.dimensions.x * normal);
            let previousTangent = vec2<f16>(contact.tangent0, contact.tangent1);
            var nextTangent = previousTangent -
                vec2<f16>(dot(pointVelocity, t0), dot(pointVelocity, t1)) / 3.5h;
            let tangentSize = length3(vec3<f16>(nextTangent, 0h));
            let limit = surfaces[i].friction * nextNormal;
            if (tangentSize > limit && tangentSize >= 0.00006103515625h) { nextTangent *= limit / tangentSize; }
            let change = nextTangent - previousTangent;
            let delta = t0 * change.x + t1 * change.y;
            velocity += delta; omega += cross(-normal, delta) * (2.5h / sphere.dimensions.x);
            contact.tangent0 = nextTangent.x; contact.tangent1 = nextTangent.y;
            candidate.state.bodies[slot].support[i] = contact;
        }
    }
    if (!finite3(velocity) || !finite3(omega) || length3(velocity) > 64h || length3(omega) > 64h) {
        fail(FAILURE_DOMAIN); return result;
    }
    for (var i = 0u; i < count; i++) {
        let contact=candidate.state.bodies[slot].support[i];
        let collider=candidate.state.colliders[contact.collider];
        if (annular_curved_feature(collider,contact.feature)) {
            let normal=annular_midpoint_normal(initial,velocity,h,collider,contact.feature);
            if (abs(dot(initial.velocity,normal)+dot(velocity,normal))>0.0009765625h) { supportTrialRetry=true;return result; }
        } else if (dot(velocity, normals[i]) < -0.0009765625h) { fail(FAILURE_RESIDUAL); return result; }
    }
    let dv = (velocity - initial.velocity) / hUnits;
    let dw = (omega - initial.angular) / hUnits;
    if (any(abs(dv) > vec3<f16>(0.125h)) || any(abs(dw) > vec3<f16>(2h))) {
        fail(FAILURE_DOMAIN); return result;
    }
    var body = original;
    body.segmentCell = initial.cell;
    body.segmentOrdinal = ordinal;
    body.segmentLocalPhase = vec4<f16>(initial.local, phase);
    if (phase >= 2048h) { body.segmentOrdinal++; body.segmentLocalPhase.w -= PHASE_SCALE; }
    body.segmentVelocity = vec4<f16>(initial.velocity / 32h, 0h);
    body.segmentAngular = vec4<f16>(initial.angular, 0h);
    body.segmentRotation = initial.rotation;
    candidate.state.bodies[slot].phase = MOTION_SUPPORTED;
    let curve=MotionCurve(body,true,guide.enabled,dv*512h,dw*512h,guide.error,0h,0h);
    if (supportAnnularTrial) {
        let endpoint=curve_query(curve,ordinal,end);
        if (endpoint.failure!=FAILURE_NONE) { fail(endpoint.failure);return result; }
        for (var i=0u;i<count;i++) {
            let contact=candidate.state.bodies[slot].support[i];let collider=candidate.state.colliders[contact.collider];
            if (collider.shape!=SHAPE_ANNULAR) { continue; }
            let shape=shape_query(endpoint,collider,sphere.dimensions.x);
            if (shape.failure!=FAILURE_NONE) { fail(shape.failure);return result; }
            if (shape.feature!=contact.feature || dot(endpoint.velocity,shape.normal)<-0.0009765625h) {
                supportTrialRetry=true;return result;
            }
        }
    }
    return curve;
}

fn finish_support(slot: u32, q: MotionQuery, radius: f16) -> bool {
    let original = candidate.state.bodies[slot];
    var retained = 0u;
    for (var i = 0u; i < original.supportCount; i++) {
        var contact = original.support[i];
        let shape = shape_query(q, candidate.state.colliders[contact.collider], radius);
        if (shape.failure != FAILURE_NONE) { fail(shape.failure); return false; }
        if (shape.gap < -radius * CONTACT_BAND_FRACTION) { fail(FAILURE_RESIDUAL); return false; }
        // A constraint is integrated for one physical substep. At its endpoint the
        // actual closest feature is rechecked; a departed feature carries no reaction
        // into the next substep. New feature normals are built from that endpoint.
        if (shape.feature != contact.feature || shape.gap > radius * CONTACT_BAND_FRACTION ||
            dot(q.velocity, shape.normal) > 0.0009765625h) { continue; }
        if (dot(q.velocity, shape.normal) < -0.0009765625h) { fail(FAILURE_RESIDUAL); return false; }
        candidate.state.bodies[slot].support[retained] = contact; retained++;
    }
    for (var i = retained; i < 4u; i++) {
        candidate.state.bodies[slot].support[i] = ContactSlot(0u, 0u, 0h, 0h, 0h, 0h);
    }
    candidate.state.bodies[slot].supportCount = retained;
    candidate.state.bodies[slot].phase = select(MOTION_FREE, MOTION_SUPPORTED, retained != 0u);
    return true;
}

struct CurveCuts { values: array<f16, 32>, count: u32, operations: u32, uncertainty: f16 }
fn sensor_point(q: MotionQuery, sensor: ResidenceSensor) -> vec3<f16> {
    let frame = candidate.state.bodies[sensor.frame];
    return inverse_rotate(sensor.rotation,
        inverse_rotate(frame.rotation, relative_point(q, frame)) - sensor.translation.xyz);
}
fn sensor_velocity(q: MotionQuery, sensor: ResidenceSensor) -> vec3<f16> {
    return inverse_rotate(sensor.rotation, inverse_rotate(candidate.state.bodies[sensor.frame].rotation, q.velocity));
}
fn sensor_eligible(q: MotionQuery, sensor: ResidenceSensor) -> bool {
    let p = sensor_point(q, sensor);
    return sensor.enabled == SENSOR_ENABLED && all(p > sensor.low.xyz) &&
        all(p < sensor.high.xyz) && length3(q.velocity) < sensor.controls.x;
}
fn sensor_query_value(curve: MotionCurve, sensor: ResidenceSensor, q: MotionQuery, kind: u32) -> f16 {
    if (q.failure != FAILURE_NONE) { fail(q.failure); return 0h; }
    if (kind < 3u) { return sensor_point(q, sensor)[kind]; }
    if (kind == 3u) { return length3(q.velocity); }
    if (kind < 7u) { return sensor_velocity(q, sensor)[kind - 4u]; }
    // The implemented free polynomial has v=v0+a0*f(t), f'>0 on k*t<=.25.
    // Thus the sign of d|v|^2/dt is the sign of v.a0. Supported curves have
    // constant effective acceleration. Scaled operands avoid large squared speeds.
    let a = select(gravity(curve.body) - curve.body.massDragGravityXY.y *
        (curve.body.segmentVelocity.xyz * 32h), curve.acceleration, curve.supported || curve.forceDriven);
    let vs = max(abs(q.velocity.x), max(abs(q.velocity.y), abs(q.velocity.z)));
    let ascale = max(abs(a.x), max(abs(a.y), abs(a.z)));
    if (vs == 0h || ascale == 0h) { return 0h; }
    return dot(q.velocity / vs, a / ascale);
}
fn sensor_value(curve: MotionCurve, sensor: ResidenceSensor, ordinal: u32, phase: f16, kind: u32) -> f16 {
    return sensor_query_value(curve, sensor, curve_query(curve, ordinal, phase), kind);
}
fn add_cut(cuts: ptr<function, CurveCuts>, value: f16) {
    for (var i = 0u; i < (*cuts).count; i++) {
        if ((*cuts).values[i] == value) { return; }
    }
    if ((*cuts).count >= 32u) { fail(FAILURE_ROOT_BUDGET); return; }
    var index = (*cuts).count;
    loop {
        if (index == 0u || (*cuts).values[index - 1u] < value) { break; }
        (*cuts).values[index] = (*cuts).values[index - 1u]; index--;
    }
    (*cuts).values[index] = value; (*cuts).count++;
}
fn sensor_root(curve: MotionCurve, sensor: ResidenceSensor, ordinal: u32,
    begin: f16, end: f16, beginQuery: MotionQuery, endQuery: MotionQuery,
    kind: u32, boundary: f16, cuts: ptr<function, CurveCuts>) {
    var low = begin; var high = end;
    let first = sensor_query_value(curve, sensor, beginQuery, kind);
    let last = sensor_query_value(curve, sensor, endQuery, kind);
    if (first == boundary) { add_cut(cuts, low); }
    if (last == boundary) { add_cut(cuts, high); }
    if (first == last || (first < boundary) == (last < boundary)) { return; }
    let below = first < boundary;
    loop {
        if ((*cuts).operations >= SENSOR_ROOT_BUDGET) { fail(FAILURE_ROOT_BUDGET); return; }
        let middle = low + (high - low) * 0.5h;
        if (middle == low || middle == high) { break; }
        (*cuts).operations++;
        let value = sensor_value(curve, sensor, ordinal, middle, kind);
        if (value == boundary) { add_cut(cuts, middle); return; }
        if ((value < boundary) == below) { low = middle; } else { high = middle; }
    }
    // Retain both representable sides. A sub-quantum uncertain interval cannot
    // contribute eligible time unless both sides are eligible.
    add_cut(cuts, low); add_cut(cuts, high);
}

struct GuideHorizon { phase: f16, uncertainty: f16 }
fn guide_value(q: MotionQuery, guide: PlanarGuide, component: u32) -> f16 {
    if (q.failure != FAILURE_NONE) { fail(q.failure); return 0h; }
    if (component < 3u) { return guide_point(q,guide)[component]; }
    return guide_velocity(q,guide)[component - 3u];
}

fn guide_curve_bounds(curve: MotionCurve, ordinal: u32, phase: f16, guide: PlanarGuide, component: u32) -> ScalarRange {
    let body = curve.body;
    let fraction = range_multiply(range_subtract(range_value(phase),range_value(body.segmentLocalPhase.w)),range_value(0.000244140625h));
    let elapsedSteps = range_add(range_value(f16(i32(ordinal)-i32(body.segmentOrdinal))),fraction);
    let u = ScalarRange(max(0h,guide_divide_bounds(elapsedSteps.lo,PHYSICAL_RATE/32h).lo),
        guide_divide_bounds(elapsedSteps.hi,PHYSICAL_RATE/32h).hi);
    let v0 = body.segmentVelocity.xyz*32h;
    var acceleration = vector_add(vector_value(gravity(body)),vector_scale(vector_value(-v0),range_value(body.massDragGravityXY.y)));
    var x = range_multiply(u,range_multiply(range_value(body.massDragGravityXY.y),range_value(0.03125h)));
    if (curve.supported || curve.forceDriven) { acceleration=vector_value(curve.acceleration);x=range_value(0h); }
    if (curve.accelerationError>0h) {
        acceleration=vector_add(acceleration,VectorRange(vec3<f16>(-curve.accelerationError),vec3<f16>(curve.accelerationError)));
    }
    var positionPolynomial=range_value(0.0013885498046875h);
    positionPolynomial=range_add(range_value(-0.008331298828125h),range_multiply(x,positionPolynomial));
    positionPolynomial=range_add(range_value(0.041656494140625h),range_multiply(x,positionPolynomial));
    positionPolynomial=range_add(range_value(-0.1666259765625h),range_multiply(x,positionPolynomial));
    positionPolynomial=range_add(range_value(0.5h),range_multiply(x,positionPolynomial));
    var velocityPolynomial=range_value(0.008331298828125h);
    velocityPolynomial=range_add(range_value(-0.041656494140625h),range_multiply(x,velocityPolynomial));
    velocityPolynomial=range_add(range_value(0.1666259765625h),range_multiply(x,velocityPolynomial));
    velocityPolynomial=range_add(range_value(-0.5h),range_multiply(x,velocityPolynomial));
    velocityPolynomial=range_add(range_value(1h),range_multiply(x,velocityPolynomial));
    var value:VectorRange;
    let frame=candidate.state.bodies[guide.frame];
    if (component<3u) {
        let cells=body.segmentCell-frame.cell;
        if (any(abs(cells)>vec3<i32>(2048))) { fail(FAILURE_DOMAIN);return range_value(0h); }
        let displacement=vector_add(vector_scale(vector_value(v0*.5h),u),
            vector_scale(vector_scale(acceleration,range_value(.015625h)),range_multiply(range_multiply(u,u),positionPolynomial)));
        let fine=vector_add(vector_value(body.segmentLocalPhase.xyz),vector_add(vector_value(-frame.local.xyz),displacement));
        value=vector_scale(vector_add(vector_value(vec3<f16>(cells)),fine),range_value(.0625h));
        value=vector_add(value,VectorRange(vec3<f16>(-curve.eventPositionError),vec3<f16>(curve.eventPositionError)));
        value=guide_rotation_bounds(guide.rotation,vector_add(guide_rotation_bounds(frame.rotation,value),vector_value(-guide.translation.xyz)));
    } else {
        value=vector_add(vector_value(v0),vector_scale(acceleration,range_multiply(range_multiply(u,range_value(.03125h)),velocityPolynomial)));
        value=vector_add(value,VectorRange(vec3<f16>(-curve.eventVelocityError),vec3<f16>(curve.eventVelocityError)));
        value=guide_rotation_bounds(guide.rotation,guide_rotation_bounds(frame.rotation,value));
    }
    let axis=component%3u;
    return ScalarRange(value.lo[axis],value.hi[axis]);
}



fn guide_interval_outside(curve: MotionCurve, guide: PlanarGuide, ordinal: u32, begin: f16) -> bool {
    // Every remaining interval is <=1/480s. The existing128U/s conservative
    // speed bound travels <.267U; a binary half-unit envelope includes that
    // motion in any rigid guide frame, without assuming monotonic endpoints.
    for (var axis=0u;axis<3u;axis++) {
        let point=guide_curve_bounds(curve,ordinal,begin,guide,axis);
        if (range_upper(point.hi+.5h)<guide.low[axis] ||
            range_lower(point.lo-.5h)>guide.high[axis]) { return true; }
    }
    return false;
}

fn guide_constant_component(curve: MotionCurve, guide: PlanarGuide, component: u32) -> bool {
    let v0=curve.body.segmentVelocity.xyz*32h;
    var acceleration=vector_add(vector_value(gravity(curve.body)),
        vector_scale(vector_value(-v0),range_value(curve.body.massDragGravityXY.y)));
    if (curve.supported || curve.forceDriven) { acceleration=vector_value(curve.acceleration); }
    let frame=candidate.state.bodies[guide.frame];
    let localAcceleration=guide_rotation_bounds(guide.rotation,guide_rotation_bounds(frame.rotation,acceleration));
    let axis=component%3u;
    if (localAcceleration.lo[axis]!=0h || localAcceleration.hi[axis]!=0h) { return false; }
    let localVelocity=guide_rotation_bounds(guide.rotation,guide_rotation_bounds(frame.rotation,vector_value(v0)));
    if (axis!=1u && (curve.forceDriven || curve.eventVelocityError>0h || curve.eventPositionError>0h)) {
        // A zero sampled acceleration can be cancellation, not nonlinear
        // invariance. Horizontal radial symmetry needs exact p=v=g=0.
        let anchor=MotionQuery(FAILURE_NONE,curve.body.segmentCell,curve.body.segmentLocalPhase.xyz,
            v0,curve.body.segmentAngular.xyz,curve.body.segmentRotation);
        let point=guide_point_range(anchor,guide);
        let localGravity=guide_rotation_bounds(guide.rotation,guide_rotation_bounds(frame.rotation,vector_value(gravity(curve.body))));
        if (point.lo[axis]!=0h || point.hi[axis]!=0h || localVelocity.lo[axis]!=0h || localVelocity.hi[axis]!=0h ||
            localGravity.lo[axis]!=0h || localGravity.hi[axis]!=0h) { return false; }
    }
    if (component>=3u) { return true; }
    return localVelocity.lo[axis]==0h && localVelocity.hi[axis]==0h;
}


struct GuideDeparture {
    id: vec2<u32>, component: u32, boundary: f16, phase: f16,
    increasing: bool, proved: bool,
}
var<private> pendingGuideDepartures: array<GuideDeparture,32>;
var<private> ownedGuideDepartures: array<GuideDeparture,32>;
var<private> pendingGuideDepartureCount: u32;
var<private> ownedGuideDepartureCount: u32;
fn guide_departure_owned(curve: MotionCurve, guide: PlanarGuide, ordinal: u32,
    begin: f16, end: f16, component: u32, boundary: f16) -> bool {
    if (component>=3u) { return false; }
    for (var index=0u;index<ownedGuideDepartureCount;index++) {
        let owned=ownedGuideDepartures[index];
        if (!owned.proved || any(owned.id!=guide.id) || owned.component!=component ||
            owned.boundary!=boundary || begin<owned.phase) { continue; }
        let first=guide_curve_bounds(curve,ordinal,begin,guide,component+3u);
        let last=guide_curve_bounds(curve,ordinal,end,guide,component+3u);
        if ((owned.increasing && first.lo>0h && last.lo>0h) ||
            (!owned.increasing && first.hi<0h && last.hi<0h)) { return true; }
        // Unknown direction cannot reuse a prior crossing certificate.
        ownedGuideDepartures[index].proved=false;
    }
    return false;
}
fn pending_guide_departure(curve: MotionCurve, guide: PlanarGuide, ordinal: u32,
    component: u32, boundary: f16, phase: f16, increasing: bool) {
    if (pendingGuideDepartureCount>=32u) { fail(FAILURE_ROOT_BUDGET);return; }
    let value=guide_curve_bounds(curve,ordinal,phase,guide,component);
    let proved=select((value.hi<boundary),(value.lo>boundary),increasing);
    pendingGuideDepartures[pendingGuideDepartureCount]=GuideDeparture(guide.id,component,boundary,phase,increasing,proved);
    pendingGuideDepartureCount++;
}
fn retain_guide_departures(curve: MotionCurve, ordinal: u32, phase: f16) {
    for (var index=0u;index<pendingGuideDepartureCount;index++) {
        let pending=pendingGuideDepartures[index];
        if (pending.phase==phase && pending.component>=3u) { ownedGuideDepartureCount=0u;break; }
    }
    for (var index=0u;index<pendingGuideDepartureCount;index++) {
        let pending=pendingGuideDepartures[index];
        if (pending.phase!=phase || pending.component>=3u || !pending.proved) { continue; }
        var actualSide=false;
        for (var guideIndex=0u;guideIndex<candidate.state.guideCount;guideIndex++) {
            let guide=candidate.state.guides[guideIndex];if (any(guide.id!=pending.id)) { continue; }
            let actual=guide_curve_bounds(curve,ordinal,phase,guide,pending.component);
            actualSide=select((actual.hi<pending.boundary),(actual.lo>pending.boundary),pending.increasing);
        }
        if (!actualSide) { continue; }
        var destination=ownedGuideDepartureCount;
        for (var previous=0u;previous<ownedGuideDepartureCount;previous++) {
            let owned=ownedGuideDepartures[previous];
            if (all(owned.id==pending.id) && owned.component==pending.component && owned.boundary==pending.boundary) {
                destination=previous;break;
            }
        }
        if (destination>=32u) { fail(FAILURE_ROOT_BUDGET);return; }
        ownedGuideDepartures[destination]=pending;
        if (destination==ownedGuideDepartureCount) { ownedGuideDepartureCount++; }
    }
}

fn guide_root(curve: MotionCurve, guide: PlanarGuide, ordinal: u32,
    begin: f16, end: f16, component: u32, boundary: f16, cuts: ptr<function, CurveCuts>) {
    if (guide_constant_component(curve,guide,component) ||
        guide_departure_owned(curve,guide,ordinal,begin,end,component,boundary)) { return; }
    let first=guide_curve_bounds(curve,ordinal,begin,guide,component);
    let last=guide_curve_bounds(curve,ordinal,end,guide,component);
    if ((first.hi < boundary && last.hi < boundary) || (first.lo > boundary && last.lo > boundary)) { return; }
    if (first.lo==boundary && first.hi==boundary) {
        add_cut(cuts,begin);
        if (last.lo==boundary && last.hi==boundary) { return; }
        // A monotone interval departing an exact endpoint has no interior root.
        if (last.hi < boundary || last.lo > boundary) { return; }
    }
    if (last.lo==boundary && last.hi==boundary) {
        add_cut(cuts,end);
        pending_guide_departure(curve,guide,ordinal,component,boundary,end,last.lo>=first.hi);
        return;
    }
    let increasing=guide_value(curve_query(curve,ordinal,end),guide,component)>=
        guide_value(curve_query(curve,ordinal,begin),guide,component);
    var lower=begin;var upper=end;
    // Locate the first possibly crossed point using directed side certificates.
    var low=begin;var high=end;
    loop {
        if ((*cuts).operations>=SENSOR_ROOT_BUDGET) { fail(FAILURE_ROOT_BUDGET);return; }
        let middle=low+(high-low)*.5h;if (middle==low || middle==high) { lower=low;break; }
        (*cuts).operations++;
        let value=guide_curve_bounds(curve,ordinal,middle,guide,component);
        let before=select((value.lo > boundary),(value.hi < boundary),increasing);
        if (before) { low=middle; } else { high=middle; }
    }
    // Upper ownership must survive the force impulse introduced by this very
    // bracket. Keep the original first-possible lower endpoint, but search the
    // far side with a conservative entire-remaining-step prospective impulse.
    var prospective=curve;
    let remaining=guide_divide_bounds(range_upper(PHASE_SCALE-begin)*.000244140625h,PHYSICAL_RATE).hi;
    prospective.eventVelocityError=up_nonnegative(curve.eventVelocityError+up_nonnegative(up_nonnegative(12h*remaining)*1.002h));
    prospective.eventPositionError=up_nonnegative(curve.eventPositionError+guide_divide_bounds(prospective.eventVelocityError,PHYSICAL_RATE).hi);
    // Locate the first definitely crossed point; the whole plateau is retained.
    low=begin;high=end;
    loop {
        if ((*cuts).operations>=SENSOR_ROOT_BUDGET) { fail(FAILURE_ROOT_BUDGET);return; }
        let middle=low+(high-low)*.5h;if (middle==low || middle==high) { upper=high;break; }
        (*cuts).operations++;
        let value=guide_curve_bounds(prospective,ordinal,middle,guide,component);
        let after=select((value.hi < boundary),(value.lo > boundary),increasing);
        if (after) { high=middle; } else { low=middle; }
    }
    (*cuts).uncertainty=max((*cuts).uncertainty,range_upper(upper-lower));
    add_cut(cuts,upper);
    pending_guide_departure(prospective,guide,ordinal,component,boundary,upper,increasing);
}
fn guide_horizon(curve: MotionCurve, ordinal: u32, begin: f16, radius: f16, end: f16) -> GuideHorizon {
    pendingGuideDepartureCount=0u;
    var result = GuideHorizon(end,0h);
    for (var index = 0u; index < candidate.state.guideCount; index++) {
        let guide = candidate.state.guides[index];
        if (guide.controls.x == 0h || guide_interval_outside(curve,guide,ordinal,begin)) { continue; }
        var cuts: CurveCuts; cuts.operations = guideOperations[index];
        add_cut(&cuts,begin); add_cut(&cuts,end);
        // Position components have at most one extremum on either admitted
        // polynomial/quadratic law. Bracket every extremum before spatial roots.
        for (var axis = 0u; axis < 3u; axis++) {
            guide_root(curve,guide,ordinal,begin,end,axis+3u,0h,&cuts);
        }
        let extrema = cuts;
        for (var part = 0u; part + 1u < extrema.count; part++) {
            let left = extrema.values[part]; let right = extrema.values[part+1u];
            for (var axis = 0u; axis < 3u; axis++) {
                guide_root(curve,guide,ordinal,left,right,axis,guide.low[axis],&cuts);
                guide_root(curve,guide,ordinal,left,right,axis,guide.high[axis],&cuts);
            }
            guide_root(curve,guide,ordinal,left,right,1u,
                guide.controls.y-guide.controls.z+radius,&cuts);
        }
        guideOperations[index] = cuts.operations;
        if (candidate.state.failure != FAILURE_NONE) { return result; }
        for (var cut = 0u; cut < cuts.count; cut++) {
            if (cuts.values[cut] > begin && cuts.values[cut] < result.phase) { result.phase = cuts.values[cut]; }
        }
        result.uncertainty = max(result.uncertainty,cuts.uncertainty);
    }
    return result;
}

fn reset_episode(index: u32) {
    candidate.state.sensors[index].phase = RESIDENCE_OUTSIDE;
    candidate.state.sensors[index].startOrdinal = 0u;
    candidate.state.sensors[index].startPhase = 0h;
    // The first occurrence of this commit survives a later exit.
}
fn dwell_units(dwell: f16) -> u32 {
    // Exact conversion of canonical Half seconds into the integer physical phase
    // lattice, rounding upward. This is clock metadata, not wider physical arithmetic.
    let bits = bitcast<u32>(vec2<f16>(dwell, 0h)) & 65535u;
    let exponent = (bits >> 10u) & 31u;
    let significand = (bits & 1023u) | 1024u;
    let product = significand * (u32(PHYSICAL_RATE) / 32u);
    if (exponent >= 8u) { return product << (exponent - 8u); }
    let shift = 8u - exponent;
    return (product + (1u << shift) - 1u) >> shift;
}
fn sensor_start(index: u32, absolute: u32) {
    var ordinal = absolute / 4096u;
    var phase = i32(absolute % 4096u);
    if (phase >= 2048) { ordinal++; phase -= 4096; }
    candidate.state.sensors[index].startOrdinal = ordinal;
    candidate.state.sensors[index].startPhase = f16(phase);
    candidate.state.sensors[index].phase = RESIDENCE_DWELLING;
}
fn qualify_interval(index: u32, ordinal: u32, left: f16, right: f16, rightEligible: bool) {
    let begin = ordinal * 4096u + u32(ceil(left));
    let end = ordinal * 4096u + u32(floor(right));
    if (end < begin) { return; }
    if (candidate.state.sensors[index].phase == RESIDENCE_OUTSIDE) { sensor_start(index, begin); }
    if (candidate.state.sensors[index].phase == RESIDENCE_QUALIFIED) { return; }
    let sensor = candidate.state.sensors[index];
    let start = u32(i32(sensor.startOrdinal * 4096u) + i32(sensor.startPhase));
    let deadline = start + dwell_units(sensor.controls.y);
    if (deadline > end || (deadline == end && right == floor(right) && !rightEligible)) { return; }
    candidate.state.sensors[index].phase = RESIDENCE_QUALIFIED;
    if (sensor.sequence != 0u) { return; }
    var eventOrdinal = deadline / 4096u;
    var eventPhase = i32(deadline % 4096u);
    if (eventPhase >= 2048) { eventOrdinal++; eventPhase -= 4096; }
    candidate.state.sensors[index].sequence = 1u;
    candidate.state.sensors[index].eventOrdinal = eventOrdinal;
    candidate.state.sensors[index].eventPhase = f16(eventPhase);
    candidate.state.captures++;
}
fn append_motion(curve: MotionCurve, ordinal: u32, begin: f16, end: f16) {
    if (end <= begin) { return; }
    let index = candidate.motionHeader.x;
    if (index >= 72u) { fail(FAILURE_MOTION_CAPACITY); return; }
    var piece: MotionPiece;
    piece.kind = select(select(1u, 2u, curve.supported), 3u, curve.forceDriven);
    piece.startOrdinal = ordinal; piece.endOrdinal = ordinal;
    var a = begin; var b = end;
    if (a >= 2048h) { piece.startOrdinal++; a -= PHASE_SCALE; }
    if (b >= 2048h) { piece.endOrdinal++; b -= PHASE_SCALE; }
    let body = curve.body;
    piece.anchorOrdinal = body.segmentOrdinal;
    piece.phases = vec4<f16>(a,b,body.segmentLocalPhase.w,PHYSICAL_RATE);
    piece.bodyId = body.id; piece.cell = body.segmentCell;
    piece.localDrag = vec4<f16>(body.segmentLocalPhase.xyz,body.massDragGravityXY.y);
    piece.rotation = body.segmentRotation; piece.velocity = body.segmentVelocity;
    piece.angular = body.segmentAngular; piece.gravity = vec4<f16>(gravity(body),0h);
    piece.acceleration = vec4<f16>(curve.acceleration,0h);
    piece.angularAcceleration = vec4<f16>(curve.angularAcceleration,0h);
    if (curve.forceDriven || curve.eventVelocityError > 0h) {
        piece.forceError = vec4<f16>(curve.accelerationError,
            up_nonnegative(guide_divide_bounds(curve.accelerationError,PHYSICAL_RATE).hi + curve.eventVelocityError),
            up_nonnegative(guide_divide_bounds(guide_divide_bounds(curve.accelerationError,PHYSICAL_RATE).hi,2h * PHYSICAL_RATE).hi + curve.eventPositionError),0h);
    }
    candidate.motionPieces[index] = piece;
    candidate.motionHeader.x++;
}

fn residence_interval(curve: MotionCurve, ordinal: u32, begin: f16, end: f16) {
    append_motion(curve, ordinal, begin, end);
    if (candidate.state.failure != FAILURE_NONE) { return; }
    if (!(begin < end)) { return; }
    for (var index = 0u; index < candidate.state.sensorCount; index++) {
        let sensor = candidate.state.sensors[index];
        if (sensor.enabled == SENSOR_DISABLED) { reset_episode(index); continue; }
        // A curve and its frame are immutable during this interval. Reuse the exact
        // endpoint query for each scalar root; midpoint evaluations remain unchanged.
        let first = curve_query(curve, ordinal, begin);
        if (first.failure != FAILURE_NONE) { fail(first.failure); return; }
        let last = curve_query(curve, ordinal, end);
        if (last.failure != FAILURE_NONE) { fail(last.failure); return; }
        // This exact polynomial/quadratic degeneracy has no spatial or speed roots.
        // Keep endpoint admission above and the ordinary per-interval episode transition.
        let constantTrajectory = all(curve.body.segmentVelocity.xyz == vec3<f16>(0h)) &&
            all(curve.body.segmentAngular.xyz == vec3<f16>(0h)) &&
            all(curve.angularAcceleration == vec3<f16>(0h)) &&
            all(select(gravity(curve.body), curve.acceleration, curve.supported || curve.forceDriven) == vec3<f16>(0h));
        if (constantTrajectory) {
            if (sensor_eligible(first, sensor)) { qualify_interval(index, ordinal, begin, end, true); }
            else { reset_episode(index); }
            continue;
        }
        var cuts: CurveCuts; cuts.operations = sensorOperations[index];
        add_cut(&cuts, begin); add_cut(&cuts, end);
        // Split at the three position extrema and the speed extremum of this
        // implemented polynomial/quadratic segment, then locate each open boundary.
        for (var kind = 4u; kind < 8u; kind++) {
            sensor_root(curve, sensor, ordinal, begin, end, first, last, kind, 0h, &cuts);
        }
        let pieces = cuts;
        for (var piece = 0u; piece + 1u < pieces.count; piece++) {
            let left = pieces.values[piece]; let right = pieces.values[piece + 1u];
            var a = first; var b = last;
            if (left != begin) { a = curve_query(curve, ordinal, left); }
            if (a.failure != FAILURE_NONE) { fail(a.failure); return; }
            if (right != end) { b = curve_query(curve, ordinal, right); }
            if (b.failure != FAILURE_NONE) { fail(b.failure); return; }
            for (var axis = 0u; axis < 3u; axis++) {
                sensor_root(curve, sensor, ordinal, left, right, a, b, axis, sensor.low[axis], &cuts);
                sensor_root(curve, sensor, ordinal, left, right, a, b, axis, sensor.high[axis], &cuts);
            }
            sensor_root(curve, sensor, ordinal, left, right, a, b, 3u, sensor.controls.x, &cuts);
        }
        sensorOperations[index] = cuts.operations;
        if (candidate.state.failure != FAILURE_NONE) { return; }
        for (var part = 0u; part + 1u < cuts.count; part++) {
            let left = cuts.values[part]; let right = cuts.values[part + 1u];
            var a = first; var b = last;
            if (left != begin) { a = curve_query(curve, ordinal, left); }
            if (a.failure != FAILURE_NONE) { fail(a.failure); return; }
            if (right != end) { b = curve_query(curve, ordinal, right); }
            if (b.failure != FAILURE_NONE) { fail(b.failure); return; }
            if (!sensor_eligible(a, sensor)) { reset_episode(index); }
            let middle = left + (right - left) * 0.5h;
            var eligible = sensor_eligible(curve_query(curve, ordinal, middle), sensor);
            if (middle == left || middle == right) {
                eligible = sensor_eligible(a, sensor) && sensor_eligible(b, sensor);
            }
            if (eligible) { qualify_interval(index, ordinal, left, right, sensor_eligible(b, sensor)); }
            else { reset_episode(index); }
            if (!sensor_eligible(b, sensor)) { reset_episode(index); }
        }
    }
}
fn residence_after_impulse(q: MotionQuery) {
    for (var index = 0u; index < candidate.state.sensorCount; index++) {
        if (!sensor_eligible(q, candidate.state.sensors[index])) { reset_episode(index); }
    }
}

@compute @workgroup_size(1)
fn admit() {
    candidate = committed;
    if (!validate_scene()) { return; }
    if (candidate.state.tick.x != 0u || candidate.state.tick.y != 0u || candidate.state.ordinal != 0u || candidate.state.captures != 0u || any(candidate.motionHeader != vec4<u32>(0u))) {
        fail(FAILURE_DECLARATION); return;
    }
    if (candidate.state.dynamicBody == NO_BODY) { return; }
    let sphere = candidate.state.colliders[sphere_collider(candidate.state.dynamicBody)];
    let body = candidate.state.bodies[candidate.state.dynamicBody];
    let q = motion(body, 0u, 0h);
    if (q.failure != FAILURE_NONE) { fail(q.failure); return; }
    for (var i = 0u; i < candidate.state.colliderCount; i++) {
        if (candidate.state.colliders[i].body == candidate.state.dynamicBody) { continue; }
        let shape = shape_query(q, candidate.state.colliders[i], sphere.dimensions.x);
        if (shape.failure != FAILURE_NONE || shape.gap < -sphere.dimensions.x * CONTACT_BAND_FRACTION) {
            fail(FAILURE_DECLARATION); return;
        }
    }
}
// First qualifying physical impact per trigger in this candidate. Discrete routing
// consumes only the fully validated candidate and commits with the same GPU swap.
fn contact_occurrence(collider: u32, ordinal: u32, phase: f16, velocity: vec3<f16>, normal: vec3<f16>) {
    let approach = max(0h, -dot(velocity, normal));
    for (var i = 0u; i < candidate.state.triggerCount; i++) {
        let trigger = candidate.state.triggers[i];
        if (trigger.sequence != 0u || trigger.owner != candidate.state.colliders[collider].body ||
            approach < trigger.controls.x) { continue; }
        var eventOrdinal = ordinal; var eventPhase = phase;
        if (eventPhase >= 2048h) { eventOrdinal++; eventPhase -= PHASE_SCALE; }
        candidate.state.triggers[i].sequence = 1u;
        candidate.state.triggers[i].collider = collider;
        candidate.state.triggers[i].eventOrdinal = eventOrdinal;
        candidate.state.triggers[i].eventPhase = eventPhase;
        candidate.state.triggers[i].approachSpeed = approach;
    }
}
@compute @workgroup_size(1)
fn advance() {
    candidate.state = committed.state;
    if (!validate_scene()) {
        // Rejected candidates retain the original motion bytes, as the full-copy path did.
        candidate.motionHeader = committed.motionHeader;
        candidate.motionPieces = committed.motionPieces;
        return;
    }
    let count = steps(candidate.state.cadence);
    candidate.motionHeader = vec4<u32>(0u,count,candidate.state.ordinal,candidate.state.ordinal+count);
    var emptyPiece: MotionPiece;
    for (var i=0u;i<72u;i++) { candidate.motionPieces[i]=emptyPiece; }
    if (candidate.state.ordinal >= 14400u) { fail(FAILURE_DOMAIN); return; }
    if (candidate.state.dynamicBody == NO_BODY) {
        candidate.state.tick.x++; candidate.state.ordinal += count; return;
    }
    candidate.state.captures = 0u;
    for (var i = 0u; i < candidate.state.sensorCount; i++) {
        candidate.state.sensors[i].sequence = 0u; candidate.state.sensors[i].eventOrdinal = 0u; candidate.state.sensors[i].eventPhase = 0h;
    }
    for (var i = 0u; i < candidate.state.triggerCount; i++) {
        candidate.state.triggers[i].sequence = 0u; candidate.state.triggers[i].collider = 0u;
        candidate.state.triggers[i].eventOrdinal = 0u; candidate.state.triggers[i].eventPhase = 0h;
        candidate.state.triggers[i].approachSpeed = 0h;
    }
    let slot = candidate.state.dynamicBody;
    let sphere = candidate.state.colliders[sphere_collider(slot)];
    for (var substep = 0u; substep < count; substep++) {
        let ordinal = candidate.state.ordinal + substep;
        for (var i = 0u; i < candidate.state.sensorCount; i++) { sensorOperations[i] = 0u; }
        for (var i = 0u; i < candidate.state.guideCount; i++) { guideOperations[i] = 0u; }
        var low = 0h; var complete = false; var inelasticCollider = NO_BODY;var trialEnd=PHASE_SCALE;
        var carriedVelocityError=0h;var carriedPositionError=0h;
        ownedGuideDepartureCount=0u;
        for (var event = 0u; event < IMPACT_BUDGET; event++) {
            let trialSource=candidate.state.bodies[slot];
            var curve = support_curve(slot,ordinal,low,sphere,inelasticCollider,trialEnd);
            if (candidate.state.failure!=FAILURE_NONE) { return; }
            if (supportTrialRetry) {
                candidate.state.bodies[slot]=trialSource;
                let shorter=low+(trialEnd-low)*0.5h;
                if (!(shorter>low && shorter<trialEnd)) { fail(FAILURE_CONTACT_BUDGET);return; }
                trialEnd=shorter;continue;
            }
            curve.eventVelocityError=carriedVelocityError;curve.eventPositionError=carriedPositionError;
            var horizon = guide_horizon(curve,ordinal,low,sphere.dimensions.x,trialEnd);
            horizon.phase=min(horizon.phase,trialEnd);
            if (supportAnnularTrial && horizon.phase<trialEnd) {
                candidate.state.bodies[slot]=trialSource;trialEnd=horizon.phase;continue;
            }
            if (horizon.uncertainty > 0h) {
                // Even an unresolved enter+exit sliver differs by at most A<=12.
                // Over the enclosing substep drag amplification is <1.001; use1.002.
                let dt = guide_divide_bounds(horizon.uncertainty * 0.000244140625h,PHYSICAL_RATE).hi;
                curve.eventVelocityError = up_nonnegative(carriedVelocityError + up_nonnegative(up_nonnegative(12h*dt)*1.002h));
                curve.eventPositionError = up_nonnegative(carriedPositionError + guide_divide_bounds(curve.eventVelocityError,PHYSICAL_RATE).hi);
            }
            if (candidate.state.failure != FAILURE_NONE) { return; }
            // Certify the actual error-bearing piece only after inherited and newly
            // bracketed force allowances have been attached.
            var ownedPieceValid=true;
            if (supportAnnularTrial) {
                for (var contactIndex=0u;contactIndex<candidate.state.bodies[slot].supportCount;contactIndex++) {
                    let contact=candidate.state.bodies[slot].support[contactIndex];
                    let collider=candidate.state.colliders[contact.collider];
                    if (collider.shape==SHAPE_ANNULAR &&
                        !annular_owned_piece(curve,ordinal,low,horizon.phase,collider,contact.feature,sphere.dimensions.x)) {
                        ownedPieceValid=false;
                    }
                }
            }
            if (!ownedPieceValid) {
                candidate.state.bodies[slot]=trialSource;pendingGuideDepartureCount=0u;
                let shorter=low+(trialEnd-low)*0.5h;
                if (!(shorter>low && shorter<trialEnd)) { fail(FAILURE_CONTACT_BUDGET);return; }
                trialEnd=shorter;continue;
            }
            let body = curve.body;
            var earliest = no_hit();
            for (var i = 0u; i < candidate.state.colliderCount; i++) {
                let collider=candidate.state.colliders[i];
                if (collider.body==slot || (collider.shape!=SHAPE_ANNULAR && supported_collider(slot,i))) { continue; }
                var excluded=NO_BODY;
                if (collider.shape==SHAPE_ANNULAR) { excluded=supported_annular_family(slot,i); }
                let h=collision(curve,ordinal,low,horizon.phase,i,sphere.dimensions.x,excluded);
                if (h.failure != FAILURE_NONE) { fail(h.failure); return; }
                if (h.found && (!earliest.found || h.phase < earliest.phase)) { earliest = h; }
            }
            if (supportAnnularTrial && earliest.found && earliest.phase>low && earliest.phase<trialEnd) {
                candidate.state.bodies[slot]=trialSource;trialEnd=earliest.phase;continue;
            }
            inelasticCollider=NO_BODY;
            if (!earliest.found) {
                let q = curve_query(curve, ordinal, horizon.phase);
                if (q.failure != FAILURE_NONE) { fail(q.failure); return; }
                residence_interval(curve, ordinal, low, horizon.phase);
                if (candidate.state.failure != FAILURE_NONE) { return; }
                if (curve.supported && !finish_support(slot, q, sphere.dimensions.x)) { return; }
                install_query(slot, q);
                if (horizon.phase < PHASE_SCALE) {
                    if (!(horizon.phase > low)) { fail(FAILURE_ROOT_BUDGET); return; }
                    if (curve.eventVelocityError>0h || curve.accelerationError>0h) {
                        carriedVelocityError=up_nonnegative(curve.eventVelocityError+guide_divide_bounds(curve.accelerationError,PHYSICAL_RATE).hi);
                        carriedPositionError=up_nonnegative(curve.eventPositionError+guide_divide_bounds(carriedVelocityError,PHYSICAL_RATE).hi);
                    }
                    retain_guide_departures(curve,ordinal,horizon.phase);
                    if (candidate.state.failure!=FAILURE_NONE) { return; }
                    anchor(slot,ordinal,horizon.phase); low = horizon.phase;trialEnd=PHASE_SCALE;
                    continue;
                }
                complete = true;
                if (curve.supported || curve.forceDriven || (ordinal + 1u) % PRIMARY_SEGMENT_STEPS == 0u) { anchor(slot, ordinal + 1u, 0h); }
                break;
            }
            residence_interval(curve, ordinal, low, earliest.phase);
            if (candidate.state.failure != FAILURE_NONE) { return; }
            let material = surface_pair(sphere.material, candidate.state.colliders[earliest.collider].material);
            if (material.failure != FAILURE_NONE) { fail(material.failure); return; }
            let response = impact_velocity(earliest.value.velocity, earliest.value.angular,
                earliest.shape.normal, sphere.dimensions.x, material);
            if (response.failure != FAILURE_NONE) { fail(response.failure); return; }
            if (-dot(earliest.value.velocity, earliest.shape.normal) < material.threshold || material.restitution == 0h) {
                inelasticCollider = earliest.collider;
            }
            residence_after_impulse(earliest.value);
            var q = earliest.value; q.velocity = response.velocity; q.angular = response.angular;
            residence_after_impulse(q);
            q = contact_position(q, earliest.collider, sphere.dimensions.x);
            if (q.failure != FAILURE_NONE) { fail(q.failure); return; }
            residence_after_impulse(q);
            if (candidate.state.failure != FAILURE_NONE) { return; }
            contact_occurrence(earliest.collider, ordinal, earliest.phase, earliest.value.velocity, earliest.shape.normal);
            install_query(slot, q); candidate.state.bodies[slot].impacts++;
            if (curve.eventVelocityError>0h || curve.accelerationError>0h) {
                carriedVelocityError=up_nonnegative(curve.eventVelocityError+guide_divide_bounds(curve.accelerationError,PHYSICAL_RATE).hi);
                carriedPositionError=up_nonnegative(curve.eventPositionError+guide_divide_bounds(carriedVelocityError,PHYSICAL_RATE).hi);
            }
            ownedGuideDepartureCount=0u;
            anchor(slot, ordinal, earliest.phase);
            low = earliest.phase;trialEnd=PHASE_SCALE;
            if (low == PHASE_SCALE) { complete = true; break; }
        }
        if (!complete) { fail(FAILURE_CONTACT_BUDGET); return; }
    }
    candidate.state.ordinal += count; candidate.state.tick.x++;
}
