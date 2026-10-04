// Reference-only exact event-boundary calls of the production contact-work routine.
// Bind a freshly admitted work-zero typed source. No Advance trajectory is replaced.
@group(0) @binding(2) var<storage, read_write> reviewWords: array<u32>;
@compute @workgroup_size(1)
fn review_geometry() {
    var collider = NO_BODY;
    for (var i = 0u; i < committed.state.colliderCount; i++) {
        if (committed.state.colliders[i].body == committed.state.contactWorks[0].owner) { collider = i; }
    }
    for (var row = 0u; row < 8u; row++) {
        candidate.state = committed.state;
        let threshold = candidate.state.contactWorks[0].controls.z;
        let packed = bitcast<u32>(vec2<f16>(threshold, 0h));
        var speed = threshold;
        if (row == 0u) { speed = bitcast<vec2<f16>>(packed - 1u).x; }
        if (row == 2u) { speed = bitcast<vec2<f16>>(packed + 1u).x; }
        var ordinal = 20u; var phase = 300h;
        if (row >= 3u) {
            candidate.state.contactWorks[0].sequence = 1u;
            candidate.state.contactWorks[0].collider = collider;
            candidate.state.contactWorks[0].eventOrdinal = 100u;
            candidate.state.contactWorks[0].eventPhase = -512h;
            candidate.state.contactWorks[0].approachSpeed = threshold;
            ordinal = 172u; phase = -512h;
            if (row == 3u) { phase = -513h; }
            if (row == 5u) { phase = -511h; }
            if (row == 6u) { ordinal = 171u; phase = 2047h; }
            if (row == 7u) { ordinal = 173u; phase = -2048h; }
        }
        let result = contact_work_response(collider, ordinal, phase,
            vec3<f16>(0h, -speed, 0h), vec3<f16>(0h, speed, 0h), vec3<f16>(0h, 1h, 0h));
        let work = candidate.state.contactWorks[0]; let offset = row * 16u;
        reviewWords[offset] = candidate.state.failure;
        reviewWords[offset+1u] = work.sequence;
        reviewWords[offset+2u] = work.eventOrdinal;
        reviewWords[offset+3u] = bitcast<u32>(vec2<f16>(work.eventPhase, work.approachSpeed));
        reviewWords[offset+4u] = bitcast<u32>(work.energy.xy);
        reviewWords[offset+5u] = bitcast<u32>(result.xy);
        reviewWords[offset+6u] = bitcast<u32>(vec2<f16>(result.z, speed));
        reviewWords[offset+7u] = ordinal;
        reviewWords[offset+8u] = bitcast<u32>(vec2<f16>(phase, threshold));
    }
}
