using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreCanonicalBodyCodec : ICanonicalBodyCodec
{
    public byte[] Encode(CanonicalBody body) => CanonicalBodyCodec.Encode(body);
    public void Write(CanonicalBody body, Span<byte> data) => CanonicalBodyCodec.Write(body, data);
    public CanonicalBody Decode(ReadOnlySpan<byte> data) => CanonicalBodyCodec.Decode(data);
}
