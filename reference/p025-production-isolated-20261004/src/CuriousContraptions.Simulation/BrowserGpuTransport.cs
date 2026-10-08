using System.Runtime.InteropServices.JavaScript;
using CuriousContraptions.Gpu;

internal sealed partial class BrowserGpuTransport : IWorkshopGpuTransport
{
    [JSImport("initialize", "workshopGpu")]
    private static partial Task InitializeGpu(string preamble);
    [JSImport("stage", "workshopGpu")]
    private static partial Task StageGpu(byte[] input, int operation);
    [JSImport("read", "workshopGpu")]
    private static partial byte[] ReadGpu();
    [JSImport("commit", "workshopGpu")]
    private static partial void CommitGpu();
    [JSImport("discard", "workshopGpu")]
    private static partial void DiscardGpu();
    [JSImport("deviceReady", "workshopGpu")]
    private static partial bool DeviceIsReady();
    [JSImport("dispose", "workshopGpu")]
    private static partial void DisposeGpu();


    public Task Initialize(string preamble) => InitializeGpu(preamble);
    public Task Stage(byte[] input, WorkshopGpuOperation operation) => StageGpu(input, (int)operation);
    public byte[] Read() => ReadGpu();
    public void Commit() => CommitGpu();
    public void Discard() => DiscardGpu();
    public bool DeviceReady() => DeviceIsReady();
    public void Dispose() => DisposeGpu();
}
