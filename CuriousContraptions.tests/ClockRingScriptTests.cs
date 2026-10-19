using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Blind hunter 3 (Story 9.4b): clock-ring.js hand-ports the C# clock wire, so the actual module is run under
/// Node and compared with its C# owners: the record sizes, the role numbers, the reply wait against the probe lifetime,
/// the probe ownership checks and reply bytes (peer origin 32, sequence 64, sent 72, stamps 80/88, epoch 96), and the
/// nanosecond conversion across a sweep including subnormal, half-nanosecond, large and out-of-range readings.</summary>
public sealed class ClockRingScriptTests(ITestOutputHelper output)
{
    private const string Script = """
        const ring = await import(process.env.CLOCK_RING_URL);
        let text = ''; for await (const chunk of process.stdin) text += chunk;
        const request = JSON.parse(text);
        const bytes = hex => Uint8Array.from(Buffer.from(hex, 'hex'));
        const double = bits => { const view = new DataView(new ArrayBuffer(8)); view.setBigUint64(0, BigInt.asUintN(64, BigInt(bits)), true); return view.getFloat64(0, true); };
        const peer = bytes(request.peer);
        const nanoseconds = request.milliseconds.map(bits => { try { return ring.nanoseconds(double(bits)).toString(); } catch (error) { return error instanceof RangeError ? 'refused' : 'other:' + error; } });
        const owns = request.probes.map(probe => ring.probeOwnsPeer(bytes(probe), peer));
        const replies = request.replies.map(r => { const reply = ring.encodeReply(bytes(r.probe), peer, BigInt(r.received), BigInt(r.sent), r.epoch); return reply === null ? null : Buffer.from(reply).toString('hex'); });
        process.stdout.write(JSON.stringify({ constants: { PeerBytes: ring.PeerBytes, ProbeBytes: ring.ProbeBytes, ReplyBytes: ring.ReplyBytes, ReplyWaitMilliseconds: ring.ReplyWaitMilliseconds }, roles: ring.ClockRole, nanoseconds, owns, replies }));
        """;

    private static readonly WorkshopClockPeer Peer = new(new(11, 23), new(7), new(1_000), new(1), new(100_000), WorkshopRuntimeRole.Browser);

    private static string Hex(ReadOnlySpan<byte> data) => Convert.ToHexString(data).ToLowerInvariant();

    private JsonElement RunRing(object request)
    {
        var path = Path.Combine(twodog.Engine.ResolveProjectDir(), "CuriousContraptions.web/wwwroot/clock-ring.js");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("--input-type=module"); start.ArgumentList.Add("-e"); start.ArgumentList.Add(Script);
        start.Environment["CLOCK_RING_URL"] = new Uri(path).AbsoluteUri;
        using var node = Process.Start(start) ?? throw new InvalidOperationException("Node could not be started; the clock-ring cross-check needs it.");
        node.StandardInput.Write(JsonSerializer.Serialize(request)); node.StandardInput.Close();
        var stdout = node.StandardOutput.ReadToEndAsync(); var stderr = node.StandardError.ReadToEndAsync();
        Assert.True(node.WaitForExit(30_000), "The clock-ring script did not finish within 30 s.");
        output.WriteLine($"Script checked: {path}");
        Assert.True(node.ExitCode == 0, $"The clock-ring script failed ({node.ExitCode}): {stderr.Result}");
        return JsonDocument.Parse(stdout.Result).RootElement.Clone();
    }

    private static IEnumerable<double> Sweep()
    {
        yield return 0; yield return -0.0; yield return double.Epsilon; yield return BitConverter.UInt64BitsToDouble(0x000F_FFFF_FFFF_FFFFUL);
        yield return BitConverter.UInt64BitsToDouble(0x0010_0000_0000_0000UL); // smallest normal
        foreach (var half in new[] { 0.0000005, 0.0000015, 0.0000025, 0.0000035, 1e-6, 1.5e-6 }) yield return half; // half-nanosecond ties
        // Exact binary ties: k/128 ms is (k x 15625)/2 ns, so odd k lands on half a nanosecond and rounds to even.
        foreach (var tie in new[] { 1, 3, 5, 7, 157_953 }) { yield return tie / 128.0; yield return 1_234 + tie / 128.0; }
        foreach (var reading in new[] { 0.1, 1, 16.666666666666668, 1234.5678, 86_400_000.000_001, 4_294_967_296.25 }) yield return reading;
        yield return 9_223_372_036_854.774; yield return 9_223_372_036_854.776; // either side of the I64 nanosecond limit
        yield return 4_503_599_627_370_496; yield return double.MaxValue; // power >= 0 and the largest finite reading
        yield return -1; yield return double.NaN; yield return double.PositiveInfinity; yield return double.NegativeInfinity;
        var random = new Random(9_4_2);
        for (var i = 0; i < 1_000; i++) yield return random.NextDouble() * 1e7;              // ordinary session readings
        for (var i = 0; i < 1_000; i++) yield return BitConverter.Int64BitsToDouble(random.NextInt64(0, 0x7FF0_0000_0000_0000L)); // every exponent
    }

    private static string CSharpNanoseconds(double milliseconds)
    {
        try { return WorkshopNativeClock.FromMilliseconds(milliseconds).Value.ToString(CultureInfo.InvariantCulture); }
        catch (ArgumentOutOfRangeException) { return "refused"; }
    }

    private static bool CSharpOwns(byte[] probe)
    {
        try { WorkshopClockWire.DecodeProbe(probe, Peer); return true; }
        catch (ArgumentException) { return false; }
    }

    [Fact]
    public void ClockRingScriptAgreesWithTheCSharpClockWireItPorts()
    {
        var probe = WorkshopClockWire.EncodeProbe(new(Peer.Session, Peer.Generation, Peer.RequesterGeneration, Peer.RequesterRole, new(5), new(2_000)), Peer);
        byte[] Mutated(int at, long value) { var copy = (byte[])probe.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(copy.AsSpan(at), value); return copy; }
        var otherPeer = (byte[])probe.Clone(); otherPeer[33] ^= 1; // another master origin
        var probes = new[] { probe, Mutated(64, 0), Mutated(72, -1), Mutated(72, 0), otherPeer };
        var replies = new (long Received, long Sent, uint Epoch)[] { (3_000, 3_001, 0), (3_000, 3_000, 9), (1_000, 1_000, uint.MaxValue), (999, 1_000, 0), (3_001, 3_000, 0) };
        var sweep = Sweep().ToArray();

        var result = RunRing(new
        {
            peer = Hex(WorkshopClockWire.EncodePeer(Peer)),
            milliseconds = sweep.Select(value => BitConverter.DoubleToUInt64Bits(value).ToString(CultureInfo.InvariantCulture)).ToArray(),
            probes = probes.Select(p => Hex(p)).ToArray(),
            replies = replies.Select(r => new { probe = Hex(probe), received = r.Received.ToString(CultureInfo.InvariantCulture), sent = r.Sent.ToString(CultureInfo.InvariantCulture), epoch = r.Epoch }).ToArray(),
        });

        var constants = result.GetProperty("constants");
        Assert.Equal((WorkshopClockWire.PeerBytes, WorkshopClockWire.ProbeBytes, WorkshopClockWire.ReplyBytes),
            (constants.GetProperty("PeerBytes").GetInt32(), constants.GetProperty("ProbeBytes").GetInt32(), constants.GetProperty("ReplyBytes").GetInt32()));
        Assert.Equal(WorkshopClockMapping.ProbeLifetime, constants.GetProperty("ReplyWaitMilliseconds").GetInt64() * 1_000_000);
        // The script's role names are a serialization boundary: they must name exactly the C# roles with their values.
        var roles = result.GetProperty("roles").EnumerateObject().ToDictionary(role => role.Name, role => role.Value.GetUInt32());
        Assert.Equal(Enum.GetValues<WorkshopRuntimeRole>().ToDictionary(role => Enum.GetName(role)!, role => (uint)role).OrderBy(r => r.Key), roles.OrderBy(r => r.Key));

        var nanoseconds = result.GetProperty("nanoseconds").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Equal(sweep.Length, nanoseconds.Length);
        for (var i = 0; i < sweep.Length; i++)
            Assert.True(CSharpNanoseconds(sweep[i]) == nanoseconds[i],
                $"nanoseconds({sweep[i]:R}, bits {BitConverter.DoubleToUInt64Bits(sweep[i]):X16}): C# {CSharpNanoseconds(sweep[i])}, clock-ring.js {nanoseconds[i]}");
        Assert.Contains("refused", nanoseconds);
        Assert.Contains(nanoseconds, value => value is not ("0" or "refused"));

        Assert.Equal(probes.Select(CSharpOwns), result.GetProperty("owns").EnumerateArray().Select(value => value.GetBoolean()));
        var scriptReplies = result.GetProperty("replies").EnumerateArray().Select(value => value.ValueKind == JsonValueKind.Null ? null : value.GetString()).ToArray();
        for (var i = 0; i < replies.Length; i++)
        {
            string? expected;
            try { expected = Hex(WorkshopClockWire.EncodeReply(probe, Peer, new(replies[i].Received), new(replies[i].Sent), new(replies[i].Epoch))); }
            catch (ArgumentException) { expected = null; }
            Assert.True(expected == scriptReplies[i], $"reply {i}: C# {expected ?? "refused"}, clock-ring.js {scriptReplies[i] ?? "refused"}");
        }
        Assert.Contains(scriptReplies, reply => reply is null);
        Assert.Contains(scriptReplies, reply => reply is not null);
    }
}
