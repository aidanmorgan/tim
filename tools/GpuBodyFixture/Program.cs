using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using CuriousContraptions.Gpu;

[assembly: SupportedOSPlatform("browser")]

public enum FixtureCommand { Construct, Run, Reset, Dispose }
public enum FixtureState { Ready, Running, Complete, Reset, Rejected, Unsupported, Fault }
public static partial class Program
{
    private static CanonicalBody? _construction;
    private static CanonicalBody? _committed;
    private static ulong _epoch;
    private static bool _busy;
    private static bool _deviceReady;
    public static void Main() { }

    // Explicit generated mappings at the external JS/WGSL boundary.
    [JSExport]
    public static int RecordByteLength() => CanonicalBody.ByteLength;
    [JSExport]
    public static int[] CommandAbi() => [(int)FixtureCommand.Construct, (int)FixtureCommand.Run, (int)FixtureCommand.Reset, (int)FixtureCommand.Dispose];
    [JSExport]
    public static int[] StateAbi() => [(int)FixtureState.Ready, (int)FixtureState.Running, (int)FixtureState.Complete, (int)FixtureState.Reset, (int)FixtureState.Rejected, (int)FixtureState.Unsupported, (int)FixtureState.Fault];
    [JSExport]
    public static string ShaderAbi() => FormattableString.Invariant(
        $"const RECORD_VERSION: u32 = {(uint)BodyRecordVersion.HalfPoseF32Velocity}u; const COMMITTED: u32 = {(uint)BodyCandidateStatus.Committed}u; const INVALID_RECORD: u32 = {(uint)BodyCandidateStatus.InvalidRecord}u; const OUT_OF_RANGE: u32 = {(uint)BodyCandidateStatus.OutOfRange}u; const CELL_SCALE: i32 = {(int)CellScale.Metres}; const TIME_SCALE: i32 = {(int)TimeScale.Seconds};");


    [JSImport("initialize", "gpu")]
    internal static partial Task InitializeDevice();
    [JSImport("advance", "gpu")]
    internal static partial Task Advance(byte[] input);
    [JSImport("readResult", "gpu")]
    internal static partial byte[] ReadResult();
    [JSImport("publish", "gpu")]
    internal static partial void Publish(byte[] state, int status, string detail);
    [JSImport("dispose", "gpu")]
    internal static partial void DisposeDevice();

    [JSExport]
    public static async Task Initialize()
    {
        try { await InitializeDevice(); _deviceReady = true; Publish([], (int)FixtureState.Ready, "C# simulation worker and shader-f16 device ready."); }
        catch (Exception error) { Publish([], (int)FixtureState.Unsupported, error.Message); }
    }

    // Physical UI numbers enter the canonical model only here. There is no CPU step.
    [JSExport]
    public static void Construct(double originMetres, double speedMetresPerSecond)
    {
        try
        {
            if (_busy || !_deviceReady || !double.IsFinite(originMetres) ||
                !double.IsFinite(speedMetresPerSecond) || Math.Abs(originMetres) > 64 ||
                Math.Abs(speedMetresPerSecond) > 1 ||
                (speedMetresPerSecond != 0 && Math.Abs(speedMetresPerSecond) < 0.01))
                throw new ArgumentOutOfRangeException("Unsupported fixture construction.");
            var cells = originMetres * 16;
            var origin = checked((int)Math.Floor(cells + 0.5));
            var local = (Half)(cells - origin);
            // Carry after the sole input quantization, including rounded +half cell.
            if (local == (Half)0.5) { origin++; local = (Half)(-0.5); }
            var body = new CanonicalBody(new(1), checked(++_epoch), 0,
                new(origin, 0, 0), new(local, (Half)0, (Half)0),
                new((Half)(speedMetresPerSecond / 32), (Half)0, (Half)0));
            body.Validate();
            _construction = body; _committed = body;
            Publish(body.Encode(), (int)FixtureState.Ready, "Canonical construction admitted.");
        }
        catch (Exception error) { Publish(_committed?.Encode() ?? [], (int)FixtureState.Rejected, error.Message); }
    }

    [JSExport]
    public static async Task Run(int ticks)
    {
        if (_busy || !_deviceReady || _committed is not { } initial || ticks < 1 || ticks > 7200)
        { Publish(_committed?.Encode() ?? [], (int)FixtureState.Rejected, "Run rejected."); return; }
        _busy = true;
        var generation = _epoch;
        _committed = initial with { Epoch = generation };
        var clock = Stopwatch.StartNew();
        try
        {
            for (var index = 0; index < ticks; index++)
            {
                var source = _committed!.Value;
                await Advance(source.Encode());
                var raw = ReadResult();
                if (generation != _epoch) return;
                var result = CanonicalBody.Decode(raw);
                if (result.Epoch != generation || result.Tick != source.Tick + 1 ||
                    result.Id != source.Id || result.Velocity != source.Velocity)
                    throw new InvalidOperationException("GPU result identity rejected.");
                _committed = result;
                Publish(raw, (int)(index + 1 == ticks ? FixtureState.Complete : FixtureState.Running),
                    "Committed GPU tick.");
                var remaining = (index + 1) * (1000.0 / 120) - clock.Elapsed.TotalMilliseconds;
                if (remaining >= 1) await Task.Delay((int)remaining);
                if (generation != _epoch) return;
            }
        }
        catch (Exception error)
        {
            if (generation == _epoch) Publish(_committed?.Encode() ?? [], (int)FixtureState.Fault, error.Message);
        }
        finally { _busy = false; }
    }

    [JSExport]
    public static void Reset()
    {
        checked { _epoch++; }
        if (_construction is not { } construction) return;
        // Construction bytes are restored exactly; epoch is transport ownership,
        // with the new run's source epoch installed only when it is dispatched.
        _committed = construction;
        Publish(construction.Encode(), (int)FixtureState.Reset, "Exact construction restored.");
    }

    [JSExport]
    public static void Dispose()
    {
        checked { _epoch++; }
        _deviceReady = false;
        DisposeDevice();
    }
}
