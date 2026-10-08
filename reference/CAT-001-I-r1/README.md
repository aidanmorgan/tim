# Preserved CAT-001-I/r1 inputs

These exact before-images retain the failed [r1 candidate](../../docs/verification/CAT-001-I/candidate-r1.json) while R-V2 is fixed forward in the same slice. The manifest, original review and raw attempts remain unchanged. Only overwritten source/project/status inputs and rebuilt game/test binaries are copied here; other r1 inputs remain at their original paths.

This directory is covered by the parent reference/.gdignore and excluded current compile/export boundary. It is neither a compatibility runtime nor a supported source path.

| Original input | Preserved copy | Original SHA-256 |
| --- | --- | --- |
| `ui/WorkshopAnimation.cs` | [copy](ui/WorkshopAnimation.cs) | `4fddc7a292e5c31e4db08e72796a4fe12b4d92b6aeb54893035734b5ac9acfc9` |
| `ui/Workshop.cs` | [copy](ui/Workshop.cs) | `97f8f3848a681664a092123e6f934664334da1dfa5411ab3458541eb60a0bda3` |
| `ui/WorkshopGuidance.cs` | [copy](ui/WorkshopGuidance.cs) | `eceb00204fbcc11e5b46fa63b59bc0fa14886ae1a74c3c86575f40315c202aa7` |
| `CuriousContraptions.tests/CuriousContraptions.tests.csproj` | [copy](CuriousContraptions.tests/CuriousContraptions.tests.csproj) | `020151a992ff9278197999e344bb622a7dd7d4b5306d1d3458aa2b24825f1238` |
| `TODO.md` | [copy](TODO.md) | `858162c659b3c215a16779f3581aba358397265c97dd7e94a8947ad3f22c7c64` |

The preserved [game DLL](artifacts/CuriousContraptions.dll) has SHA-256 `5ceedc95ad027894edbfce4960548f2701fbcfefc777420e860864b13d88fcac` and represents both original identical game artifact paths. The [test DLL](artifacts/CuriousContraptions.tests.dll) has SHA-256 `4ccb4ccda2b79bdc970d2e1d9ad521c28e017fc0fd750cfe5ac79bc1bdb61cfe`. Binary copies used Anvil path/hash preview validation (explicit partial scan), then exact SHA verification before and after copy (e17b15). They do not introduce changed executable content.

R1 remains Fail on R-N3 and additionally records its subsequently discovered hint regression. Its 62 native checks and ten JavaScript controls retain their original narrower scope; they did not exercise the lost hint semantics.
