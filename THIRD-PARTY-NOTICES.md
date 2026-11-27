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
  S5c modifies `FlatBufferVerify.cs` further with the unknown-field rule: `Verifier` keeps, for each table being verified
  (a stack, cleared by `VerifyBuffer`), the highest vtable offset that table's generated `Verify` passes to any per-field
  method (both offsets of the union methods included), and `VerifyTableEnd` refuses the table when its vtable is longer,
  whether the extra entry is present or zero. Every generated `Verify` checks every field of its table in slot order, so
  the rule refuses a field the schema does not define at every table and depth with no hand-written field count. Both
  files carry a modification notice after the licence header (Apache-2.0 §4(b)). Upgrading the runtime re-applies this
  patch and Story 9A.45's; `CuriousContraptions.tests/VendoredFlatBuffersVerifierTests.cs` and the direct offset-boundary
  controls fail if either is dropped.
- **Copyright:** Copyright 2014 Google Inc. and Copyright 2017 Google Inc. All rights reserved.
- **License:** Apache License, Version 2.0. The full text is `CuriousContraptions.FlatBuffers/LICENSE`, byte-identical to
  the upstream `LICENSE` at that tag.
