# Original history recovery

On 10 October 2026 the owner approved publication after GitHub rejected seven oversized historical ZIP blobs. Only those seven paths were removed from the unpublished history; all 17 commits remain separate, with their messages, authors, timestamps and parent topology preserved. Published history through `6416b4c9ffbc9686b64526da638a9f2744bb2a19` is unchanged. The filtered candidate `9658030e7d3d99ddfbf96ce355d7cdc69278de04` has the exact approved current tree `5440678880eb91e88cb8308ad8a9ab188b8dd641`; subsequent documentation records this provenance.

**Original baseline `a6c914e367bb39316887b670e1fde1e4d0973f23` remains the citation authority.** Existing baseline facts and historical receipts retain their original commit IDs. The filtered counterpart is not a substitute baseline or an alias. A fresh clone cannot resolve original unpublished IDs until the original history is imported.

## Local recovery archive

The archive stays inside the owner's `tim` workspace; it is not uploaded:

- File: `archives/story-6-2-original-history.bundle`
- Size: 935986738 bytes
- SHA-256: `6615192b4404703f29efe3a3d46b4776f672e90ff19f5cbc918f4b477f06113b`
- Original tip: `4a510cf92c2f21393747c9fd433aac5f80636ecb`
- Required published ancestors: `7c19ef720e4d8f8610c4e4d02b5b286b1eba5214` and `6416b4c9ffbc9686b64526da638a9f2744bb2a19`

Obtain the local bundle from the workspace owner; a normal clone alone does not include it. Verify the checksum, then import it without altering the working branch:

```sh
shasum -a 256 /path/to/story-6-2-original-history.bundle
git bundle verify /path/to/story-6-2-original-history.bundle
git fetch /path/to/story-6-2-original-history.bundle refs/heads/main:refs/archive/story-6-2-original
git show a6c914e367bb39316887b670e1fde1e4d0973f23:path/to/cited/file
```

The original workspace also retains `refs/archive/story-6-2-original` at the original tip and the original merge worktree. Do not push this archive ref: its oversized blobs caused the original rejection. No automatic migration or replacement refs are involved. An independent fresh filtered clone failed to resolve the original baseline before import, then recovered its exact original tree `482cf22cec33335dfee43c8735702d01e388e22e` afterward. Independent review verified 574 cited paths and 2945 distinct ranges without mismatch.

## Removed historical payloads

All seven files were introduced by original `a165e88`, deleted by original `aaac712`, and absent from already published history. They remain byte-for-byte recoverable in the bundle:

```text
reference/p020-pages-37167090792/artifact.zip
reference/p020-pages-37167784179/artifact.zip
reference/p020-pages-37168599707/artifact.zip
reference/p054-pages-37177119773/artifact.zip
reference/p054-pages-37178846416/artifact.zip
reference/p063-pages-37183382369/artifact.zip
reference/p066-pages-37187386165/artifact.zip
```

## Original to published-history mapping

This map explains identity changes; it does not rewrite citation authority.

```text
old                                      new
33f44a85657fd4dd4c05d664e204f865632f9f44 97c5439978364739318a683cd44879d2e7e201af
34ad0ae0a64fc56336022de0ada49774098492cf bab283d7878ec183d7726e48d583b35b21348133
3aca255b464eac1956a5e0ad81e95f982e3071f6 cf46eb99e4b780752a6f617001f94c7e43d4a2d0
4a510cf92c2f21393747c9fd433aac5f80636ecb 9658030e7d3d99ddfbf96ce355d7cdc69278de04
520f7780d9ea31f02158a343b9f8d24f1d1279d3 23ed5bdbfae6283538b8e3f8f844c8ba5e40718b
738cb3e86b0ed7fd62bb34296d4e6bafd41c3670 4a96b2b71cd9da5854e77b5f11810282a0e20309
836a2ae0086227d9308eb8a7c175347aa61f14d3 bbaeb1a1d076359fa729efe805d1bc47f44ee5a3
863585d071cbf3b6a84f07bde2471bab75188095 dbd3f3923721affd792d4a1d98d87dbebac5668e
90db56b54fd4d5320cc5ce07ec2d921828951bf2 12a7f2609d116fad40d274c053a2e5b4c13a8f7a
a165e888e8bb7984dfa5933d238fb8643d767c59 6e66118ea669c8f45f7de9fdd5a442e11aca3de7
a6c914e367bb39316887b670e1fde1e4d0973f23 8e9f8d76698c0d22a72761a11ae91c443ae78d5e
a9b4d3bcccb3c103bb830676cfe919259bf87531 5851e2565f10effb3fc399daa301368d2ca1dede
aaac712a5b49f6ce8762ef2b6fcb3372d426d0d5 840b93e469a445ab15db2ed24ba64697ad04a6c5
b0a0944374d3e109f52f24cdfe29df9d172ce1bf ac34471b5aa458240c28663c0c2fa7a8bf6bdb51
ba32b115aec4f3a204581492d3daad158caa2159 e1362e1c9a68e505338d26077f15f3c768f04136
e24fea3644cb572858669bf8db7617482760eab2 a7fb5d05bd13afba26446dcddd7637c89c8ab5ef
f7a8ef00d94d96b19b12f97e9a73e2e2fa343114 5a4bf5d956f6ce03b37519e9e08fc81981c09013
```

The [integration review](../_bmad-output/implementation-artifacts/review-6-2-bumper-battery-integration.md) retains failed pushes, independent mapping/recovery checks and publication acceptance. Runtime proof remains bound to its original exact source/artifact identities; publication checks bind the final remote commit and deployed assets separately.
