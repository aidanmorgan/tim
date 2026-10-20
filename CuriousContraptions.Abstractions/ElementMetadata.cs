using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

/// <summary>An element's identity and authoring schema. <see cref="IconPath"/> stays null until the element ships its own icon
/// (Story 9A.18).</summary>
public readonly record struct ElementMetadata(
    string Id,
    ElementCategory Category,
    string DisplayNameKey,
    string? IconPath,
    string ScenePath,
    IReadOnlyList<ParameterDescriptor> Parameters);
