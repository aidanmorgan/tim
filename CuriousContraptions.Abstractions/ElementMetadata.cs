using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

public readonly record struct ElementMetadata(
    string Id,
    string Category,
    string DisplayNameKey,
    string IconPath,
    string ScenePath,
    IReadOnlyDictionary<string, object> Parameters);
