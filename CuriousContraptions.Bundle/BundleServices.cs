using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

/// <summary>Stateless composition provider; content remains owned by the calling host.</summary>
public sealed class BundleServices : IBundleServices
{
    public BundleEntry Entry(IPuzzleElementDeclaration declaration) => BundleSchema.Entry(declaration);
    public string Write(IEnumerable<IPuzzleElementDeclaration> declarations) => BundleSchema.Write(declarations);
    public string WriteEntries(IEnumerable<BundleEntry> entries) => BundleSchema.WriteEntries(entries);
    public BundleDocument Read(string json) => BundleSchema.Read(json);
    public IReadOnlyList<BundleError> Validate(IReadOnlyList<NamedEntry> entries) => BundleValidator.Validate(entries);
    public IReadOnlyDictionary<string, float> Parameters(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null) => BundleGeometry.Parameters(entry, supplied);
    public IReadOnlyList<ColliderDescriptor> Colliders(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null) => BundleGeometry.Colliders(entry, supplied);
    public IReadOnlyList<TubeMouthDescriptor> Mouths(BundleEntry entry, IReadOnlyDictionary<string, float>? supplied = null) => BundleGeometry.Mouths(entry, supplied);
    public IReadOnlyList<RunControlLane> Lanes(BundleEntry entry, string key) => RunControlBindings.Lanes(entry, key);
}
