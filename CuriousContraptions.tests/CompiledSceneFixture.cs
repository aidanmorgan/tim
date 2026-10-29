using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Current main compiler and runtime wire boundary used by direct runtime tests.</summary>
internal static class CompiledSceneFixture
{
    internal static WorkshopCompiledScene Compile(WorkshopConstruction construction, CompiledSceneId? identity = null)
    {
        var compiled = WorkshopSceneCompiler.Compile(construction, new(101, 206), identity ?? new(construction.Revision.Value));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, compiled,
            Session: new(101, 206), Cadence: new(1), Projection: new(1));
        return WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
    }
}
