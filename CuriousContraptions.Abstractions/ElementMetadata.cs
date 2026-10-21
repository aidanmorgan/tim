using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

/// <summary>An element's identity, palette presentation and authoring schema (Stories 9A.14 and 9A.18).
/// <see cref="DisplayName"/> is the player-facing palette title and <see cref="Description"/> its player-facing explanation.
/// <see cref="Color"/> is the element's approved palette colour (DESIGN.md). <see cref="PaletteOrder"/> places the element's Free
/// palette row: rows are listed by ascending order, every order is positive and no two elements share one. <see cref="IconPath"/>
/// and <see cref="ScenePath"/> name the package's own <c>icon.svg</c> and scene under <c>res://elements/&lt;Package&gt;/</c>.</summary>
public readonly record struct ElementMetadata(
    string Id,
    ElementCategory Category,
    string DisplayName,
    string Description,
    ElementColor Color,
    int PaletteOrder,
    string IconPath,
    string ScenePath,
    IReadOnlyList<ParameterDescriptor> Parameters);

/// <summary>An sRGB colour with straight alpha, each channel in [0, 1].</summary>
public readonly record struct ElementColor(float R, float G, float B, float A);
