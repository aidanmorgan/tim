# Third-party notices

This repository includes third-party source code under its own license. Each entry names the component, its exact
upstream version and location, the license, and where the full license text is kept.

## Google FlatBuffers C# runtime

- **Component:** the FlatBuffers C# runtime (`net/FlatBuffers`): `ByteBuffer.cs`, `ByteBufferUtil.cs`,
  `FlatBufferBuilder.cs`, `FlatBufferConstants.cs`, `FlatBufferVerify.cs`, `IFlatbufferObject.cs`, `Offset.cs`,
  `Struct.cs`, `Table.cs`.
- **Upstream:** https://github.com/google/flatbuffers, tag `v25.2.10`.
- **Location here:** `CuriousContraptions.FlatBuffers/`, built as the trim-annotated `CuriousContraptions.FlatBuffers`
  assembly (Story 9.4b). Each file's SHA-256 is pinned by
  `CuriousContraptions.tests/VendoredFlatBuffersIdentityTests.cs`. Seven sources remain byte-identical.
  `FlatBufferVerify.cs` originally omitted the upstream BOM/trailing whitespace. Story 9A.45 records bounded correctness
  patches in that verifier and `Table.cs`: absolute vtable positions use checked 32-bit arithmetic, while vtable lengths/relative offsets are unsigned
  16-bit values in both verification and generated-accessor lookup. The former Int16 absolute-position conversion rejected valid buffers beyond 32KiB and its catch
  treated arithmetic failure as an absent field. That catch is removed; malformed buffers retain verifier bounds,
  alignment and typed refusal. Large compiled-scene gameplay and direct offset-boundary controls verify the correction.
- **Copyright:** Copyright 2014 Google Inc. and Copyright 2017 Google Inc. All rights reserved.
- **License:** Apache License, Version 2.0. The full text is `CuriousContraptions.FlatBuffers/LICENSE`, byte-identical to
  the upstream `LICENSE` at that tag.
