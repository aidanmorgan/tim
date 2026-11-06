using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreRuntimeCodecs : IRuntimeCodecs
{
    public ICommandCodec Commands { get; } = new CoreCommandCodec();
    public IClockCodec Clocks { get; } = new CoreClockCodec();
    public IAnimationCodec Animation { get; } = new CoreAnimationCodec();
    public IScheduleCodec Schedules { get; } = new CoreScheduleCodec();
    public IPoseRingCodec Poses { get; } = new CorePoseRingCodec();
    public ICanonicalBodyCodec Bodies { get; } = new CoreCanonicalBodyCodec();
    public IRuntimeSessionCodec Sessions { get; } = new CoreRuntimeSessionCodec();
}

internal sealed class CoreResponseEncoder : IWorkshopResponseEncoder
{
    private readonly Google.FlatBuffers.FlatBufferBuilder _builder = new(4096);
    public byte[] EncodeResponse(WorkshopResponse response) => WorkshopWireFlatBuffers.EncodeResponse(response, _builder);
}
