using System.Security.Cryptography;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9.4b (D2): the FlatBuffers C# runtime is vendored from google/flatbuffers tag v25.2.10 (net/FlatBuffers,
/// Apache-2.0). Every vendored file is pinned by its SHA-256, so an in-place edit, a partial upgrade or a stray file
/// fails here. Seven sources and the LICENSE are byte-identical to upstream. FlatBufferVerify.cs and Table.cs are recorded
/// exceptions: the original copy lacks upstream's UTF-8 BOM and trailing whitespace (upstream sha256 fae566bc8df9d73d…;
/// evidence-9-4b/vendor-identity.txt). Story 9A.45 additionally corrects absolute vtable positions to checked Int32 and
/// relative vtable values to UInt16 in the verifier and Table accessors, and removes catch-as-absent handling. THIRD-PARTY-NOTICES.md records the bounded patch.
/// Upgrade the runtime as a complete pinned set with regenerated wire code; retain or supersede this correctness proof.
/// </summary>
public sealed class VendoredFlatBuffersIdentityTests
{
    // CSharpStaticGateTests extends this pin to every file in the directory (the gate's opt-out lives there).
    internal static readonly IReadOnlyDictionary<string, string> Pinned = new Dictionary<string, string>
    {
        ["ByteBuffer.cs"] = "f54f9f8643a9aea60913671a8df0d374b8d9b602264139b74aced0733d960660",
        ["ByteBufferUtil.cs"] = "b1f712041443cbcf9acda6a1bf8f745c6ee1872ffb7bfb9b91be92ab37e060bd",
        ["FlatBufferBuilder.cs"] = "e67577bd1e86154d1f4bc975b97f12508f1707aaae4d0b9108c570789edf7192",
        ["FlatBufferConstants.cs"] = "1df77a108bf9da81ac80027f343f4279e11f1d20bdc50a3b9d98f245c5fb8b0b",
        ["FlatBufferVerify.cs"] = "71f56f05c8638b045f19988a01fcd4ebf5b57b6e0d90ee47aa95c2b3dcb1c9c5",
        ["IFlatbufferObject.cs"] = "3741b1769a4b80bacfe977c957173c0b005c50cfb70e42381b280b65f4a2174d",
        ["Offset.cs"] = "3ca223869957cd70fdcdb9fd04b4766c14c189e499e0fd874c9bfc8e238c457a",
        ["Struct.cs"] = "08ea4836f780dcce794f86ab8548be3bffbd55754a4c9b75adabdf1f7a3b0bc7",
        ["Table.cs"] = "7f78c1fcdf368b67bbbdd24a8a2fe99625ae7b8f7349dcc7e04ef2c8b610b11b",
        ["LICENSE"] = "cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30",
    };

    private static string Directory() =>
        Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.FlatBuffers");

    [Fact]
    public void EveryVendoredFileMatchesItsPinnedHash()
    {
        foreach (var (name, expected) in Pinned)
        {
            var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(Directory(), name))));
            Assert.True(expected == actual, $"{name} differs from its vendored v25.2.10 pin: {actual}");
        }
    }

    [Fact]
    public void TheVendoredSetHasNoOtherSource()
    {
        var sources = System.IO.Directory.GetFiles(Directory(), "*.cs").Select(Path.GetFileName).Order(StringComparer.Ordinal);
        Assert.Equal(Pinned.Keys.Where(name => name.EndsWith(".cs", StringComparison.Ordinal)).Order(StringComparer.Ordinal), sources);
    }
}
