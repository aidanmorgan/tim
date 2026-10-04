// Reference-only calls of the production PGS Coulomb projection.
@group(0) @binding(2) var<storage, read_write> reviewWords: array<u32>;
@compute @workgroup_size(1)
fn review_geometry() {
    let tiny = bitcast<vec2<f16>>(committed.state.version).x;
    for (var row = 0u; row < 5u; row++) {
        var proposal = vec2<f16>(tiny, -tiny); var limit = 0h;
        if (row == 1u) { proposal = vec2<f16>(3h, 4h); }
        if (row == 2u) { proposal = vec2<f16>(3h, 4h); limit = 2h; }
        if (row == 3u) { proposal = vec2<f16>(0.25h, 0h); limit = 0.5h; }
        if (row == 4u) { proposal = vec2<f16>(tiny, 0h); limit = 0.00006103515625h; }
        let result = friction_disk(proposal, limit);
        reviewWords[row*16u] = bitcast<u32>(proposal);
        reviewWords[row*16u+1u] = bitcast<u32>(vec2<f16>(limit, 0h));
        reviewWords[row*16u+2u] = bitcast<u32>(result);
    }
}
