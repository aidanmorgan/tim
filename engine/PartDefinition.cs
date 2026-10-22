using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Bundle;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>One catalogue part as the game uses it: a plain runtime record built by <see cref="PartRegistry"/> from one element
/// bundle entry. The scene supplies artwork; baked bundle geometry and tube mouths supply physics and snapping.</summary>
public sealed class PartDefinition
{
    public PartDefinition(PartId partId, string title, string description, ElementCategory category, Color color, string iconPath,
        PackedScene? scene, BundleEntry physics)
    {
        if (physics.Metadata.Id != partId.Value) throw new ArgumentException("Part and baked geometry identities disagree.");
        var errors = BundleValidator.Validate([new NamedEntry(partId.Value, physics)]);
        if (errors.Count != 0) throw new ArgumentException(string.Join("; ", errors));
        _physics = physics;
        PartId = partId;
        Title = title;
        Description = description;
        Category = category;
        Color = color;
        IconPath = iconPath;
        Scene = scene;
    }

    private readonly BundleEntry _physics;

    public PartId PartId { get; }
    /// <summary>The element id at the Godot node-name and metadata boundary.</summary>
    public string Id => PartId.Value;
    public string Title { get; }
    public string Description { get; }
    public ElementCategory Category { get; }
    /// <summary>The element's approved palette colour (DESIGN.md).</summary>
    public Color Color { get; }
    /// <summary>The package's icon.svg, which the palette renders.</summary>
    public string IconPath { get; }
    public PackedScene? Scene { get; }

    public ElementPresentation Presentation => _physics.Presentation;
    public IReadOnlyList<ParameterDescriptor> Parameters => _physics.Metadata.Parameters;

    public IReadOnlyList<ExtractedCollider> Colliders => BundleGeometry.Colliders(_physics).Select(DeclaredPhysics.Admit).ToArray();

    public IReadOnlyDictionary<string, ExtractedPortLocator> PortLocators => Mouths(null);

    public IReadOnlyDictionary<string, ExtractedPortLocator> Mouths(PipeLength? length)
    {
        IReadOnlyDictionary<string, float>? parameters = length is { } pipe
            ? new Dictionary<string, float> { ["length"] = pipe.Metres } : null;
        return BundleGeometry.Mouths(_physics, parameters).ToDictionary(mouth => mouth.End switch
        {
            TubeEnd.Start => "mouth_start",
            TubeEnd.End => "mouth_end",
            _ => throw new ArgumentException("Undefined tube end.")
        }, mouth => new ExtractedPortLocator(mouth.End == TubeEnd.Start ? "mouth_start" : "mouth_end",
            new(mouth.Position.X, mouth.Position.Y, mouth.Position.Z),
            new(mouth.Rotation.X, mouth.Rotation.Y, mouth.Rotation.Z, mouth.Rotation.W),
            mouth.End == TubeEnd.Start ? TubeMouthEnd.Start : TubeMouthEnd.End, mouth.BoreRadius));
    }
}

public enum TubeMouthEnd { Start = 1, End = 2 }

public readonly record struct ExtractedPortLocator(
    string PortId, Vector3 Position, Quaternion Rotation, TubeMouthEnd? Mouth = null, float? BoreRadius = null);
