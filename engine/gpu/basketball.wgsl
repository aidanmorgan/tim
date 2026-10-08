// Typed C# emits the canonical profile, discriminants and immutable Half coefficient table.
struct Body {
    version: u32, status: u32, epoch: vec2<u32>, tick: vec2<u32>,
    idLow: u32, idHigh: u32, cell: vec3<i32>, reserved: u32,
    cellScale: i32, timeScale: i32,
    local: vec4<f16>, velocity: vec4<f16>, padding: vec2<u32>,
}
struct World {
    body: Body,
    segmentResponse: vec4<f16>, segmentStart: u32,
    segmentPhase: f16, segmentLocalY: f16, segmentCellY: i32,
    count: u32, contacts: u32, motion: u32,
    lawVersion: u32, lawFailure: u32, cadence: u32, physicalProfile: u32,
    cadenceRevision: vec2<u32>, reserved: vec2<u32>,
}
// The sole segment parameter is a material response coefficient, not another
// velocity unit or correction lane. Body velocity remains canonical cells/time.
struct Segment { cell: i32, local: f16, response: f16, origin: i32, phase: f16 }
struct Query {
    failure: u32, region: i32, cell: i32, local: f16,
    velocity: f16, derivative: f16, eta: f16, below: bool,
}
struct Bracket { failure: u32, left: f16, right: f16 }
struct Interval { left: f16, right: f16, depth: u32 }
struct Event { failure: u32, found: bool, stage: f16, value: Query }
const HALF_STAGE = 2184h;
@group(0) @binding(0) var<storage, read> committed: World;
@group(0) @binding(1) var<storage, read_write> candidate: World;

fn physical_steps(cadence: u32) -> u32 {
    if (cadence == CADENCE_60) { return 8u; }
    if (cadence == CADENCE_120) { return 4u; }
    if (cadence == CADENCE_240) { return 2u; }
    return 0u;
}
fn finite(v: f16) -> bool { return v == v && abs(v) <= 65504h; }
fn same_half(a: f16, b: f16) -> bool {
    return bitcast<u32>(vec2<f16>(a, 0h)) == bitcast<u32>(vec2<f16>(b, 0h));
}
fn positive_zero(v: f16) -> bool { return bitcast<u32>(vec2<f16>(v, 0h)) == 0u; }
fn zero3(v: vec3<f16>) -> bool {
    return positive_zero(v.x) && positive_zero(v.y) && positive_zero(v.z);
}
fn zero4(v: vec4<f16>) -> bool { return zero3(v.xyz) && positive_zero(v.w); }
fn segment() -> Segment {
    return Segment(candidate.segmentCellY, candidate.segmentLocalY,
        candidate.segmentResponse.y, i32(candidate.segmentStart), candidate.segmentPhase);
}
fn bad_query(code: u32) -> Query { var q: Query; q.failure = code; return q; }

// All continuous operations are f16. Integer ordinals select a centered stage chart;
// eta/4096 is physical substep phase, never an arithmetic error/remainder lane.
fn query(st: Segment, n: i32, stage: f16) -> Query {
    let eta = stage / DT;
    let z = eta - st.phase;
    let offset = n - st.origin;
    if (offset < -1 || offset > 961 ||
        (offset <= 0 && z < f16(-offset * 4096)) ||
        (offset >= 960 && z > f16((960 - offset) * 4096))) {
        return bad_query(FAILURE_DOMAIN);
    }
    var j = (offset + 7) / 15;
    var d = offset - 15 * j;
    if (d == 7 && z >= 2048h) { j++; d -= 15; }
    else if (d == -7 && z < -2048h) { j--; d += 15; }
    let v0 = st.response * RESPONSE_WORLD_FACTOR;
    let m = i32(floor(v0 * 2h + 0.5h));
    if (j < 0 || j > 64 || m < 0 || m > 16 || !(v0 >= 0h && v0 <= 8h)) {
        return bad_query(FAILURE_DOMAIN);
    }
    let row = LAW_REGIONS[u32(j * 17 + m)];
    let w = v0 - f16(m) * 0.5h;
    let u = f16(d) / 8h;
    let c = st.cell - row.cell;
    let slope = row.a1 + u * row.a2;
    let cross = w * (row.b0 + u * (row.b1 + u * row.b2));
    let q = fma(-u, slope, f16(c)) - cross;
    let local = st.local - row.local;
    let r0 = q + local;
    let du = (row.a1 + (2h * row.a2) * u) +
        w * (row.b1 + (2h * row.b2) * u);
    let b = row.a2 + w * row.b2;
    let lin = z * (du / 32h);
    let zs = z / 32h;
    let zz = zs * zs;
    let quad = (zz * b) / 1024h;
    let near = abs(c) <= 32;
    var residual: f16;
    var below: bool;
    if (near) {
        let rc = (r0 * 1024h - lin) - quad;
        if (!finite(rc)) { return bad_query(FAILURE_NONFINITE); }
        if (abs(r0) > 46h) { return bad_query(FAILURE_BOUND); }
        residual = rc / 1024h;
        below = rc < 0h;
    } else {
        residual = (r0 - lin / 1024h) - quad / 1024h;
        below = c < -32;
    }
    let qpose = SUPPORT_LOCAL + residual;
    if (!finite(qpose)) { return bad_query(FAILURE_NONFINITE); }
    var carry = i32(floor(qpose + 0.5h));
    var lp = qpose - f16(carry);
    if (lp >= 0.5h) { carry++; lp -= 1h; }
    else if (lp < -0.5h) { carry--; lp += 1h; }
    let derivative = du + (b * z) / 16384h;
    let velocity = -derivative * 0.1171875h;
    if (!finite(velocity) || !finite(lp) || !finite(du) || !finite(b) ||
        !finite(z) || !finite(zz)) { return bad_query(FAILURE_NONFINITE); }
    // The Half thresholds below are the largest representable values <= the model's
    // real bounds 5.3 and .024; no additional arithmetic precision is introduced.
    if (abs(u) > 1h || abs(z) > 4096h || abs(w) > 0.25h ||
        abs(du) > 5.296875h || abs(b) > 0.02398681640625h ||
        zz > 16384h || abs(c) > 512 || abs(velocity) > 1h) {
        return bad_query(FAILURE_BOUND);
    }
    return Query(FAILURE_NONE, j, SUPPORT_CELL + carry, lp, velocity, derivative, eta, below);
}
fn predicate(q: Query, kind: u32, region: i32) -> bool {
    if (kind == ROOT_REGION) { return q.region != region; }
    if (kind == ROOT_APEX) { return q.velocity <= 0h; }
    if (kind == ROOT_CONTACT) { return q.below; }
    return false;
}
fn bracket(st: Segment, n: i32, low: f16, high: f16, kind: u32, region: i32) -> Bracket {
    if (kind != ROOT_CONTACT && kind != ROOT_REGION && kind != ROOT_APEX) {
        return Bracket(FAILURE_DOMAIN, low, high);
    }
    var a = low;
    var b = high;
    for (var iteration = 0u; iteration < 40u; iteration++) {
        let middle = (a + b) * 0.5h;
        if (middle == a || middle == b) { return Bracket(FAILURE_NONE, a, b); }
        let q = query(st, n, middle);
        if (q.failure != FAILURE_NONE) { return Bracket(q.failure, a, b); }
        if (predicate(q, kind, region)) { b = middle; } else { a = middle; }
    }
    return Bracket(FAILURE_ROOT_BUDGET, a, b);
}
fn bad_event(code: u32) -> Event { var e: Event; e.failure = code; return e; }

// Chronological depth-first partition: right is pushed before left. Depth <=4
// needs at most five pending intervals; eight slots provide bounded headroom.
fn find_event(st: Segment, n: i32, low: f16, high: f16) -> Event {
    var stack: array<Interval, 8>;
    stack[0] = Interval(low, high, 0u);
    var pending = 1u;
    loop {
        if (pending == 0u) { break; }
        pending--;
        let part = stack[pending];
        if (part.depth > 4u) { return bad_event(FAILURE_BOUND); }
        let a = query(st, n, part.left);
        let b = query(st, n, part.right);
        if (a.failure != FAILURE_NONE) { return bad_event(a.failure); }
        if (b.failure != FAILURE_NONE) { return bad_event(b.failure); }
        if (a.below) {
            if (a.velocity > 0h) { return bad_event(FAILURE_NEGATIVE_ASCENDING); }
            // A descending negative start is earliest even at a zero-width seam.
            return Event(FAILURE_NONE, true, part.left, a);
        }
        if (a.region != b.region) {
            if (b.region != a.region + 1) { return bad_event(FAILURE_DOMAIN); }
            let split = bracket(st, n, part.left, part.right, ROOT_REGION, a.region);
            if (split.failure != FAILURE_NONE) { return bad_event(split.failure); }
            if (part.depth == 4u || pending + 2u > 8u) { return bad_event(FAILURE_BOUND); }
            stack[pending] = Interval(split.right, part.right, part.depth + 1u);
            pending++;
            if (split.left != part.left) {
                stack[pending] = Interval(part.left, split.left, part.depth + 1u);
                pending++;
            }
            continue;
        }
        if (a.velocity > 0h && b.velocity <= 0h) {
            let split = bracket(st, n, part.left, part.right, ROOT_APEX, a.region);
            if (split.failure != FAILURE_NONE) { return bad_event(split.failure); }
            if (part.depth == 4u || pending == 8u) { return bad_event(FAILURE_BOUND); }
            stack[pending] = Interval(split.right, part.right, part.depth + 1u);
            pending++;
            continue;
        }
        if (b.velocity > 0h) {
            if (b.below) { return bad_event(FAILURE_NEGATIVE_ASCENDING); }
            continue;
        }
        if (!b.below) { continue; }
        let root = bracket(st, n, part.left, part.right, ROOT_CONTACT, a.region);
        if (root.failure != FAILURE_NONE) { return bad_event(root.failure); }
        let value = query(st, n, root.right);
        if (value.failure != FAILURE_NONE) { return bad_event(value.failure); }
        return Event(FAILURE_NONE, true, root.right, value);
    }
    return bad_event(FAILURE_NONE);
}
fn valid_body(body: Body) -> bool {
    return body.version == RECORD_VERSION && body.status == COMMITTED &&
        body.cellScale == CELL_SCALE && body.timeScale == TIME_SCALE &&
        body.reserved == 0u && all(body.padding == vec2<u32>(0u)) &&
        positive_zero(body.local.w) && positive_zero(body.velocity.w) &&
        all(body.cell >= vec3<i32>(-1024)) && all(body.cell <= vec3<i32>(1024)) &&
        all(body.local.xyz >= vec3<f16>(-0.5h)) && all(body.local.xyz < vec3<f16>(0.5h)) &&
        all(abs(body.velocity.xyz) <= vec3<f16>(2h)) &&
        !any((body.cell == vec3<i32>(1024)) & (body.local.xyz > vec3<f16>(0h))) &&
        !any((body.cell == vec3<i32>(-1024)) & (body.local.xyz < vec3<f16>(0h)));
}
fn admitted_axis(cell: i32, local: f16, low: i32, high: i32) -> bool {
    return cell >= low && cell <= high &&
        !(cell == low && local < 0h) && !(cell == high && local > 0h);
}
fn valid_world(world: World) -> bool {
    if (!valid_body(world.body) || world.count > 1u ||
        world.lawVersion != LAW_VERSION || world.lawFailure != FAILURE_NONE ||
        !all(world.reserved == vec2<u32>(0u)) ||
        physical_steps(world.cadence) == 0u || world.physicalProfile != PHYSICAL_480 ||
        all(world.cadenceRevision == vec2<u32>(0u)) ||
        all(world.body.epoch == vec2<u32>(0u)) ||
        world.body.tick.y != 0u || world.body.tick.x > 14400u / max(physical_steps(world.cadence), 1u) ||
        !zero3(world.body.velocity.xzw) || !zero3(world.segmentResponse.xzw) ||
        !(world.segmentResponse.y >= 0h && world.segmentResponse.y <= RESPONSE_MAX) ||
        !(world.segmentPhase >= -2048h && world.segmentPhase < 2048h) ||
        !(world.segmentLocalY >= -0.5h && world.segmentLocalY < 0.5h) ||
        world.segmentCellY < -1024 || world.segmentCellY > 1024 ||
        (world.motion != FLYING && world.motion != RESTING)) { return false; }
    let end = world.body.tick.x * physical_steps(world.cadence);
    if (world.segmentStart > end || world.contacts > end ||
        (world.segmentStart == end && world.segmentPhase > 0h)) { return false; }
    if (world.count == 0u) {
        return world.body.idLow == 1u && world.body.idHigh == 0u &&
            all(world.body.cell == vec3<i32>(0)) && zero4(world.body.local) &&
            zero4(world.body.velocity) && zero4(world.segmentResponse) &&
            world.segmentStart == 0u && positive_zero(world.segmentPhase) &&
            world.segmentCellY == 0 && positive_zero(world.segmentLocalY) &&
            world.contacts == 0u && world.motion == FLYING;
    }
    if ((world.body.idLow == 0u && world.body.idHigh == 0u) ||
        !admitted_axis(world.body.cell.x, world.body.local.x, -112, 112) ||
        !admitted_axis(world.body.cell.z, world.body.local.z, -64, 64)) { return false; }
    if (world.contacts == 0u) {
        if (!admitted_axis(world.segmentCellY, world.segmentLocalY, 0, 144) ||
            !zero4(world.segmentResponse) || world.segmentStart != 0u ||
            !positive_zero(world.segmentPhase) || world.motion != FLYING) { return false; }
        if (world.body.tick.x == 0u &&
            (world.body.cell.y != world.segmentCellY || !same_half(world.body.local.y, world.segmentLocalY) ||
             !zero4(world.body.velocity))) { return false; }
    } else if (world.segmentCellY != SUPPORT_CELL || world.segmentLocalY != SUPPORT_LOCAL ||
        (world.motion == FLYING && world.segmentResponse.y <= 0h)) { return false; }
    if (world.motion == FLYING) {
        let age = end - world.segmentStart;
        if (age > 960u || (age == 960u && world.segmentPhase < 0h)) { return false; }
    } else if (!zero4(world.body.velocity) || !zero4(world.segmentResponse) ||
        world.body.cell.y != SUPPORT_CELL || world.body.local.y != SUPPORT_LOCAL) { return false; }
    return true;
}
fn reject(code: u32) {
    candidate.body.status = INVALID_RECORD;
    candidate.lawFailure = code;
}
@compute @workgroup_size(1)
fn admit() {
    candidate = committed;
    reject(FAILURE_INVALID_DESCRIPTOR);
    if (!valid_world(committed) || committed.body.tick.x != 0u) { return; }
    candidate.lawFailure = FAILURE_NONE;
    candidate.body.status = COMMITTED;
}
@compute @workgroup_size(1)
fn advance() {
    candidate = committed;
    reject(FAILURE_INVALID_DESCRIPTOR);
    if (!valid_world(committed) || committed.body.tick.x >= 14400u / max(physical_steps(committed.cadence), 1u)) { return; }
    candidate.lawFailure = FAILURE_NONE;
    if (candidate.count == 1u && candidate.motion == FLYING) {
        for (var step = 0u; step < physical_steps(committed.cadence); step++) {
            let n = i32(committed.body.tick.x * physical_steps(committed.cadence) + step);
            var impacts = 0u;
            for (var chart = 0u; chart < 2u; chart++) {
                let center = n + i32(chart);
                var cursor = select(0h, -HALF_STAGE, chart == 1u);
                let end = select(HALF_STAGE, 0h, chart == 1u);
                // At most one accepted impact plus one remainder query in a chart.
                for (var remainderAttempt = 0u; remainderAttempt < 2u; remainderAttempt++) {
                    let event = find_event(segment(), center, cursor, end);
                    if (event.failure != FAILURE_NONE) { reject(event.failure); return; }
                    if (!event.found || (chart == 0u && event.stage == HALF_STAGE)) { break; }
                    impacts++;
                    if (impacts > 1u) { reject(FAILURE_REPEATED_IMPACT); return; }
                    let incoming = event.value.velocity * 32h;
                    if (incoming > 0h) { reject(FAILURE_POSITIVE_IMPACT); return; }
                    candidate.contacts++;
                    candidate.segmentStart = u32(center);
                    candidate.segmentPhase = event.value.eta;
                    candidate.segmentCellY = SUPPORT_CELL;
                    candidate.segmentLocalY = SUPPORT_LOCAL;
                    if (-incoming <= BOUNCE_THRESHOLD) {
                        candidate.motion = RESTING;
                        candidate.segmentResponse = vec4<f16>(0h);
                        candidate.body.cell.y = SUPPORT_CELL;
                        candidate.body.local.y = SUPPORT_LOCAL;
                        candidate.body.velocity = vec4<f16>(0h);
                        break;
                    }
                    let response = event.value.derivative * BOUNCE;
                    if (!(response > 0h && response <= RESPONSE_MAX)) { reject(FAILURE_BOUND); return; }
                    candidate.segmentResponse = vec4<f16>(0h, response, 0h, 0h);
                    cursor = event.stage;
                    if (cursor == end) { break; }
                }
                if (candidate.motion == RESTING) { break; }
            }
            if (candidate.motion == RESTING) { break; }
            let endpoint = query(segment(), n + 1, 0h);
            if (endpoint.failure != FAILURE_NONE) { reject(endpoint.failure); return; }
            candidate.body.cell.y = endpoint.cell;
            candidate.body.local.y = endpoint.local;
            candidate.body.velocity = vec4<f16>(0h, endpoint.velocity, 0h, 0h);
        }
    }
    candidate.body.tick.x = committed.body.tick.x + 1u;
    candidate.body.status = COMMITTED;
    if (!valid_world(candidate)) { reject(FAILURE_INVALID_DESCRIPTOR); }
}
