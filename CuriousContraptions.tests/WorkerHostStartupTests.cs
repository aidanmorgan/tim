using System.Reflection;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkerHostStartupTests
{
    private static readonly string[] UnexpectedKeys = ["unexpected"];
    private static readonly string[] OtherKeys = ["another"];
    [Fact]
    public void ActualWorkerMetadataPrecedesHostInitializationAndHostRefusalsRemainUsable()
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = output.Parent!.Name;
        var assembly = Assembly.LoadFrom(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "CuriousContraptions.Simulation", "bin", configuration, output.Name, "browser-wasm",
            "CuriousContraptions.Simulation.dll"));
        var host = assembly.GetType("CuriousContraptions.Simulation.Program", throwOnError: true)!;
        object? Invoke(string member, params object?[] arguments) =>
            host.GetMethod(member, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, arguments);

        Assert.Equal((int)WorkshopBuild.CaptureMode, Assert.IsType<int>(Invoke("CaptureMode")));
        var notInitialized = Assert.Throws<TargetInvocationException>(() =>
            Invoke("RejectUnknownEnvelope", (object)UnexpectedKeys));
        Assert.IsType<InvalidOperationException>(notInitialized.InnerException);
        Invoke("InitializeHost");
        Assert.Equal("Unsupported worker envelope: unexpected",
            Assert.IsType<string>(Invoke("RejectUnknownEnvelope", (object)UnexpectedKeys)));
        var duplicate = Assert.Throws<TargetInvocationException>(() => Invoke("InitializeHost"));
        Assert.IsType<InvalidOperationException>(duplicate.InnerException);
        Assert.Equal("Unsupported worker envelope: another",
            Assert.IsType<string>(Invoke("RejectUnknownEnvelope", (object)OtherKeys)));
        Assert.Equal((int)WorkshopBuild.CaptureMode, Assert.IsType<int>(Invoke("CaptureMode")));
    }

    [Fact]
    public void ActualAnimationBootstrapRefusesInvalidAndReplacementSessionsAtomically()
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var assembly = Assembly.LoadFrom(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "CuriousContraptions.Animation.Worker", "bin", output.Parent!.Name, output.Name, "browser-wasm",
            "CuriousContraptions.Animation.Worker.dll"));
        var host = assembly.GetType("CuriousContraptions.Animation.Worker.Program", throwOnError: true)!;
        void Invoke(string member, byte[] bytes) =>
            host.GetMethod(member, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [bytes]);
        Exception Refusal(string member, byte[] bytes) =>
            Assert.Throws<TargetInvocationException>(() => Invoke(member, bytes)).InnerException!;

        var core = RuntimeTestServices.Core;
        var peer = new WorkshopClockPeer(new(11, 23), new(7), new(1000), new(1), new(100_000),
            WorkshopRuntimeRole.Animation);
        Assert.IsType<ArgumentException>(Refusal("Bootstrap", []));
        var wrongRole = core.Codecs.Clocks.EncodePeer(peer with { RequesterRole = WorkshopRuntimeRole.Browser });
        Assert.Equal("Wrong Animation bootstrap role.", Assert.IsType<ArgumentException>(Refusal("Bootstrap", wrongRole)).Message);
        Invoke("Bootstrap", core.Codecs.Clocks.EncodePeer(peer));
        var replacement = peer with { Session = new(31, 47) };
        Assert.Equal("Animation master cannot be replaced.", Assert.IsType<InvalidOperationException>(
            Refusal("Bootstrap", core.Codecs.Clocks.EncodePeer(replacement))).Message);

        byte[] Abort(RuntimeSessionId session)
        {
            var codec = core.Codecs.Schedules;
            var bytes = new byte[codec.HeaderBytes + codec.AbortBytes];
            codec.WriteHeader(new(ScheduleControlKind.Abort, session, peer.Generation, new(1), new(1),
                WorkshopRuntimeRole.Simulation, WorkshopRuntimeRole.Animation), bytes.AsSpan(0, codec.HeaderBytes));
            return bytes;
        }
        // Original identity passes routing and reaches preparation ownership; replacement never passes routing.
        Assert.Equal("Animation Abort does not own preparation.", Assert.IsType<ArgumentException>(
            Refusal("ScheduleControl", Abort(peer.Session))).Message);
        Assert.Equal("Wrong Animation schedule identity or route.", Assert.IsType<ArgumentException>(
            Refusal("ScheduleControl", Abort(replacement.Session))).Message);
    }

    [Fact]
    public void HostJournalRetainsTypedFaultsWithoutASessionPublisher()
    {
        var journal = RuntimeTestServices.Core.CreateDiagnostics();
        journal.Fault(FaultSite.WorkerEnvelope,
            new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.UnknownEnvelope, ["unexpected"]));
        var first = Assert.IsType<WorkshopFaultDiagnostic>(Assert.Single(journal.Snapshot()));
        Assert.Equal(WorkshopFaultKind.Fault, first.Kind);
        Assert.Equal(FaultSite.WorkerEnvelope, first.Site);
        Assert.Contains("Unsupported worker envelope: unexpected", first.Detail, StringComparison.Ordinal);
        Assert.Null(journal.Incomplete);
        journal.Fault(FaultSite.WorkerEnvelope,
            new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.UnknownEnvelope, ["another"]));
        Assert.Equal(2, journal.Count);
        Assert.Equal(first, journal.Snapshot()[0]);
        Assert.Null(journal.Incomplete);
    }
}
