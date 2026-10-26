# Fixed pre-conversion input fixtures

These 22 save/Construct-command pairs were captured once from the existing native Debug assembly's `OldLayoutSaves.Records` (nine) and `PreSourceStoreRecords` (thirteen) builders. All 22 save buffers match the pins in baseline `8ac611ffe81a74af7d36328bf69cc6403065b6d9`; the command encoder is unchanged. The historical rounded Domino constants encode identically to that assembly's Half conversions. Base64 is storage for fixed binary input, not an output oracle. Tests only read these files; missing or malformed input fails rather than being regenerated.

The pre-launch layout originated at `3fce7a03` (24 fields), with equality evidence in `evidence-c-bumper-spring/baseline/old-layout-equality.txt`. The pre-source-store layout originated at `f27cb71d` (18 fields), with capture evidence in `evidence-9a-39/baseline/capture/old-saves`. Those historical provenance references do not authorize loading an old format.

At cutover, all 22 captured save inputs were compared once with their existing digest pins. All 44 files were then re-read and compared once against the original builders (22 saves and 22 commands, exact equality). The builders and pins are removed; the tests preserve each existing typed save/command refusal. The other historical fixtures remain their existing fixed base64 literals.
