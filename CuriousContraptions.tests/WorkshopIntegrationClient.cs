using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

/// <summary>Native scene transport over the owned harness. Display polling never advances physical or animation time.</summary>
internal sealed class WorkshopIntegrationClient(WorkshopIntegrationHarness harness) : IWorkshopClient
{
    private readonly WorkshopClientCursor _cursor = new(new(101, 206));
    private ulong _delivered;
    public WorkshopCadenceSettings Settings => harness.Construction.Settings;
    public SimulationEpoch Epoch => harness.Committed.Epoch;
    public AuthorityRevision Revision => harness.Committed.Revision;
    public WorkshopCommandIdentity? Pending => harness.Executor.Pending;
    public WorkshopTransportState TransportState => WorkshopTransportState.Ready;

    public async Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
    {
        var command = harness.Command(kind, construction, target, settings, electrical);
        var order = _cursor.ReadOrder;
        var response = await harness.Dispatch(command);
        var prepared = _cursor.PrepareAcknowledgement(command, response, order, false);
        _cursor.Commit(prepared);
        return new(response, prepared.Applicable);
    }
    public Task<WorkshopDelivery> CancelPending() => Execute(WorkshopCommandKind.Cancel, target: Pending);
    public bool TryRead(out WorkshopResponse response)
    {
        response = harness.Latest;
        if (response.Kind != WorkshopResponseKind.Read || response.Publication.Value <= _delivered) return false;
        var prepared = _cursor.PrepareRead(response); _cursor.Commit(prepared);
        _delivered = response.Publication.Value;
        return prepared.Applicable;
    }
    public bool TryPresent(ulong frame, out WorkshopPresentationSample sample)
    {
        var read = harness.Committed;
        var poses = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i])
            .Select(body => new PresentedBody(body.Body.Id, body.Body.Cell, body.Body.Local, body.Rotation)).ToArray();
        sample = new(new(poses), new(harness.Time.Nanoseconds),
            new(new(read.Tick.Value * Settings.PhysicalStepsPerCommit)), PresentationQuality.Terminal,
            Captures: read.Captures, Activations: read.Activations, Timers: read.Timers);
        return true;
    }
    public void SetDeclaredVisible(GpuBodyId owner, bool visible)
    {
        foreach (var target in WorkshopDeclaredPresentation.Targets(harness.Simulation.Scene))
            if (target.Owner == owner)
            {
                var part = harness.Construction.Instances.Single(instance => instance.Id == owner);
                var binding = DeclaredPhysics.Of(part.PartId).Presentation.Bindings[target.Binding];
                harness.Presentation?.SetVisible(target.Target, WorkshopDeclaredPresentation.Property(binding.Property), visible);
            }
    }
    public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner,
        out IReadOnlyList<DeclaredPresentationSample> samples)
    {
        samples = harness.Presentation?.Advance(harness.Schedule, new((ulong)harness.Time.Nanoseconds * 480 / 1_000_000_000))
            .Where(sample => sample.Owner == owner).ToArray() ?? [];
        return samples.Count > 0;
    }
    // The native harness owns no display-paced UI animation worker. These transport observations carry no claimed UI frame.
    public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) { }
    public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity)
    { opacity = default; return false; }
    public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
    public ValueTask DisposeAsync() => harness.DisposeAsync();
}
