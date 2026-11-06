using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreRuntimeSessionCodec : IRuntimeSessionCodec
{
    public void WriteTo(RuntimeSessionId session, Span<byte> destination) => RuntimeSessionCodec.WriteTo(session, destination);
    public RuntimeSessionId Read(ReadOnlySpan<byte> source) => RuntimeSessionCodec.Read(source);
}
