using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Browser durable storage only; no simulation or animation authority.</summary>
internal interface IWorkshopSaveStore
{
    Task Save(byte[] bytes);
    Task<string> Load(int expectedBytes);
}

internal sealed partial class BrowserWorkshopSaveStore : IWorkshopSaveStore
{
    [JSImport("saveConstruction", "workshopClient")]
    private static partial Task SaveBytes(byte[] bytes);
    public Task Save(byte[] bytes) => SaveBytes(bytes);

    [JSImport("loadConstruction", "workshopClient")]
    private static partial Task<string> LoadBytes(int expectedBytes);
    public Task<string> Load(int expectedBytes) => LoadBytes(expectedBytes);
}
