# P0-022 extraction before-images

Exact pre-edit bytes for the four existing evaluator inputs and affected CAT-001-I/r2 inputs. These are unshipped references under the parent `.gdignore`, not a second supported evaluator. CAT-001-I/r2 remains immutable and failed on R-N3; this preservation does not alter its verdict.

| Original path | SHA-256 |
| --- | --- |
| [engine/presentation/AnimationBatch.cs](engine/presentation/AnimationBatch.cs) | `cabeb088a970e37f461ee9c3eefa0ec2fc5f492092f7e0294f9493bf1b8d1d1f` |
| [engine/presentation/AnimationFollowDefinition.cs](engine/presentation/AnimationFollowDefinition.cs) | `72c94a07978bee11ca60b3a676bfa2969701597ab05343a3cdf78d2432096a3f` |
| [engine/presentation/AnimationImpulseDefinition.cs](engine/presentation/AnimationImpulseDefinition.cs) | `406e922e6b4f0948831a8003a3457e8d43c98203c9d8365a5a5efd4c71b792e9` |
| [engine/presentation/AnimationOscillationDefinition.cs](engine/presentation/AnimationOscillationDefinition.cs) | `de1f2f075b095647b48c736e132402eb542b8d00e15d2ed1c21471d495fef380` |
| [ui/WorkshopAnimation.cs](ui/WorkshopAnimation.cs) | `7e06a6db55c9f708d48f77d29af6fbb827a8e059b4fd6def2255ffb0ee9b52a1` |
| [CuriousContraptions.csproj](CuriousContraptions.csproj) | `4beca080e4536af4cac1a9ff6912f3e560bab851aecbc18a8c9ddbfd7a111a0c` |
| [CuriousContraptions.tests/CuriousContraptions.tests.csproj](CuriousContraptions.tests/CuriousContraptions.tests.csproj) | `b31feb05a0be921a4a695126253bd3dd3199767c06d05458c2690b274c4f8563` |
| [CuriousContraptions.tests/WorkshopHintTests.cs](CuriousContraptions.tests/WorkshopHintTests.cs) | `0b961d0de0367c5328b52a373db0e2ddcd493fa8654a0bcf323f405029457673` |
| [TODO.md](TODO.md) | `1212ce323d331ea417dbfa180a8a561d94be82b135e9f2ac1d7f3e966799749e` |

Large coverage-view before-images (preview-plus-SHA validation, exact byte copies independently hash-checked before current fingerprint edits):

| Original path | SHA-256 |
| --- | --- |
| [docs/coverage/engine/task-002.json](docs/coverage/engine/task-002.json) | `1caf7696f20eb5ff4932e8a460f1b6afcf7b9b81849f45ab1df4352f89cedda4` |
| [docs/coverage/engine/task-003.json](docs/coverage/engine/task-003.json) | `b27f4c2ee8dc7b969703f16848f0e4cc0b5a010c1b355ab18fd26ec288e44dbd` |
| [docs/coverage/engine/task-004.json](docs/coverage/engine/task-004.json) | `936c463060c1100ec8f69940c46adaaa7f9a813fe3745bb863d13a1fcc4cd252` |
| [docs/coverage/engine/task-009.json](docs/coverage/engine/task-009.json) | `0960331f40554e537c2c65ca71ddf3826e6c9a184568920eee0ba62ad16977f4` |
| [docs/coverage/engine/task-013.json](docs/coverage/engine/task-013.json) | `3d2a399e33397567891a2a961508d749dce5a35072ac0a3d47511f95c5851bd8` |
| [docs/coverage/engine/task-005.json](docs/coverage/engine/task-005.json) | `fb1131d18b708d61af24b0103b284952e1ff607afbf2a7d03bc6e0ca78ced422` |
| [docs/coverage/engine/task-007.json](docs/coverage/engine/task-007.json) | `c2eaf230987a9ccd3b50d72ff438a6732cbe3a0de4e65fe3ced9076151e06ffb` |
| [docs/coverage/engine/capabilities-02.json](docs/coverage/engine/capabilities-02.json) | `68bab29d241df85bdc74b4eef983ea2c62f71d58e1f788cdc2ff14aecbbee917` |
| [docs/coverage/engine/task-017.json](docs/coverage/engine/task-017.json) | `123138124d9191924a5e1468efd0605387fbb78ff85f4e55d4558165bf72aaa4` |

The pre-edit repository README is retained as [source-README.md](source-README.md), SHA-256 `8eebe7ec0d0bb6d4922727b62979eaf5dace02d77cc26e9aacee3456884e0754`; the archive's own README remains this index.

Historical r2 native DLL and two resolved project.assets payloads were not copied before this build/restore. Original hashes and contemporaneous raw results remain; these missing payloads are not represented as archived or re-executable. No historical rebuild or r2 manifest rewrite is performed.
