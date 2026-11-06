using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreClockServices : IClockServices
{
    public int ExchangeTimeoutMilliseconds => WorkshopClockDerivation.ExchangeTimeoutMilliseconds;
    public int CheckIntervalMilliseconds => WorkshopClockDerivation.CheckIntervalMilliseconds;
    public int CheckExchangeLimit => WorkshopClockDerivation.CheckExchangeLimit;
    public bool IsNarrow(ClockDerivationExchange exchange) => WorkshopClockDerivation.IsNarrow(exchange);
    public IClockMapping CreateMapping(WorkshopClockPeer peer, IWorkshopDiagnostics? diagnostics = null) => new WorkshopClockMapping(peer, diagnostics);
    public IClockDerivation CreateDerivation(IWorkshopDiagnostics? diagnostics = null) => new WorkshopClockDerivation(diagnostics);
    public ClockInterval Offset(ClockReply reply, TimestampUncertainty uncertainty) => WorkshopClockMapping.Offset(reply, uncertainty);
    public ClockInterval Expand(ClockInterval interval, long elapsed) => WorkshopClockMapping.Expand(interval, elapsed);
    public MonotonicNanoseconds FromMilliseconds(double milliseconds) => WorkshopNativeClock.FromMilliseconds(milliseconds);
    public long StartupSpacing => WorkshopClockMapping.StartupSpacing;
    public long RefreshSpacing => WorkshopClockMapping.RefreshSpacing;
    public long ProbeLifetime => WorkshopClockMapping.ProbeLifetime;
    public long MappingLifetime => WorkshopClockMapping.MappingLifetime;
    public long MaximumMappedWidth => WorkshopClockMapping.MaximumMappedWidth;
    public long MaximumOffsetSpan => WorkshopClockMapping.MaximumOffsetSpan;
    public int ProbeCapacity => WorkshopClockMapping.ProbeCapacity;
    public int BookkeepingBytes => WorkshopClockMapping.BookkeepingBytes;
    public long RecoveryLifetime => WorkshopClockMapping.RecoveryLifetime;
    public int RecoveryProbeLimit => WorkshopClockMapping.RecoveryProbeLimit;
}
