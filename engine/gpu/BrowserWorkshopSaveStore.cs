using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Browser durable storage only; no simulation or animation authority.</summary>
internal static partial class BrowserWorkshopSaveStore
{
    [JSImport("saveConstruction", "workshopClient")]
    internal static partial Task Save(byte[] bytes);

    [JSImport("loadConstruction", "workshopClient")]
    internal static partial Task<string> Load(int expectedBytes);
}
