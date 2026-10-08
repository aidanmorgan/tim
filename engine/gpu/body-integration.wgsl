
// Numeric discriminants correspond to CanonicalBody's validated external ABI.
struct Body {
    version: u32, status: u32, epoch: vec2<u32>, tick: vec2<u32>,
    idLow: u32, idHigh: u32,
    cell: vec3<i32>, reserved: u32,
    cellScale: i32, timeScale: i32,
    local: vec4<f16>, velocity: vec4<f16>, padding: vec2<u32>,
}
@group(0) @binding(0) var<storage, read> committed: Body;
@group(0) @binding(1) var<storage, read_write> candidate: Body;

@compute @workgroup_size(1)
fn integrate() {
    candidate = committed;
    candidate.status = INVALID_RECORD;
    // Magnitude checks also reject NaNs through the positive conjunction.
    if (!(committed.version == RECORD_VERSION && committed.status == COMMITTED &&
          committed.cellScale == CELL_SCALE && committed.timeScale == TIME_SCALE &&
          committed.reserved == 0u && committed.local.w == 0h &&
          committed.velocity.w == 0h && all(committed.padding == vec2<u32>(0u)) &&
          !all(committed.tick == vec2<u32>(4294967295u)) &&
          (committed.idLow != 0u || committed.idHigh != 0u) &&
          all(committed.cell >= vec3<i32>(-1024)) &&
          all(committed.cell <= vec3<i32>(1024)) &&
          all(committed.local.xyz >= vec3<f16>(-0.5h)) &&
          all(committed.local.xyz < vec3<f16>(0.5h)) &&
          all(abs(committed.velocity.xyz) <= vec3<f16>(2h)))) { return; }
    if (any((committed.cell == vec3<i32>(1024)) & (committed.local.xyz > vec3<f16>(0h))) ||
        any((committed.cell == vec3<i32>(-1024)) & (committed.local.xyz < vec3<f16>(0h)))) { return; }
    // |velocity| <=2 and dt<2: every product<4, accumulated displacement<16.
    // All continuous arithmetic here is concrete f16; no retained remainder.
    let dt = f16(512.0 / 480.0);
    var displacement = vec3<f16>(0h);
    for (var substep = 0u; substep < 4u; substep++) {
        displacement += committed.velocity.xyz * dt;
    }
    let position = committed.local.xyz + displacement;
    let carry = vec3<i32>(floor(position + vec3<f16>(0.5h)));
    let cell = committed.cell + carry;
    let local = position - vec3<f16>(carry);
    candidate.status = OUT_OF_RANGE;
    if (any(cell < vec3<i32>(-1024)) || any(cell > vec3<i32>(1024)) ||
        any((cell == vec3<i32>(1024)) & (local > vec3<f16>(0h))) ||
        any((cell == vec3<i32>(-1024)) & (local < vec3<f16>(0h)))) { return; }
    candidate.cell = cell;
    candidate.local = vec4<f16>(local, 0h);
    candidate.tick.x = committed.tick.x + 1u;
    candidate.tick.y = committed.tick.y + select(0u, 1u, candidate.tick.x == 0u);
    candidate.status = COMMITTED;
}
