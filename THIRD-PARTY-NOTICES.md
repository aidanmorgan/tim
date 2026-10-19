# Third-party notices

This repository includes third-party source code under its own license. Each entry names the component, its exact
upstream version and location, the license, and where the full license text is kept.

## Google FlatBuffers C# runtime

- **Component:** the FlatBuffers C# runtime (`net/FlatBuffers`): `ByteBuffer.cs`, `ByteBufferUtil.cs`,
  `FlatBufferBuilder.cs`, `FlatBufferConstants.cs`, `FlatBufferVerify.cs`, `IFlatbufferObject.cs`, `Offset.cs`,
  `Struct.cs`, `Table.cs`.
- **Upstream:** https://github.com/google/flatbuffers, tag `v25.2.10`.
- **Location here:** `CuriousContraptions.FlatBuffers/`, built as the trim-annotated `CuriousContraptions.FlatBuffers`
  assembly (Story 9.4b). The sources are unmodified; each file's SHA-256 is pinned by
  `CuriousContraptions.tests/VendoredFlatBuffersIdentityTests.cs`, which also records the one whitespace-only byte
  difference (`FlatBufferVerify.cs`, upstream BOM and trailing whitespace).
- **Copyright:** Copyright 2014 Google Inc. and Copyright 2017 Google Inc. All rights reserved.
- **License:** Apache License, Version 2.0. The full text is `CuriousContraptions.FlatBuffers/LICENSE`, byte-identical to
  the upstream `LICENSE` at that tag.
