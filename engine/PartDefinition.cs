using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>One catalogue part as the game uses it: a plain runtime record built by <see cref="PartRegistry"/> from one element
/// bundle entry (Story 9A.14). Its scene is the package's own scene; its colliders and port locators are extracted from that scene
/// once, on first use. A ball kind carries the declared material its bundle entry states.</summary>
public sealed class PartDefinition
{
    public PartDefinition(PartId partId, string title, string description, ElementCategory category, Color color, string iconPath,
        PackedScene? scene, BallMaterial? ball)
    {
        PartId = partId;
        Title = title;
        Description = description;
        Category = category;
        Color = color;
        IconPath = iconPath;
        Scene = scene;
        Ball = ball;
    }

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
    /// <summary>Declared material of a dynamic sphere; present for every ball kind.</summary>
    public BallMaterial? Ball { get; }

    private IReadOnlyList<ExtractedCollider>? _colliders;
    private IReadOnlyDictionary<string, ExtractedPortLocator>? _portLocators;

    public IReadOnlyList<ExtractedCollider> Colliders
    {
        get
        {
            EnsureExtracted();
            return _colliders!;
        }
    }

    public IReadOnlyDictionary<string, ExtractedPortLocator> PortLocators
    {
        get
        {
            EnsureExtracted();
            return _portLocators!;
        }
    }

    public IReadOnlyDictionary<string, ExtractedPortLocator> Ports => PortLocators;

    public void ExtractFromScene() => EnsureExtracted();

    public void SetExtractedData(IReadOnlyList<ExtractedCollider> colliders, IReadOnlyDictionary<string, ExtractedPortLocator> portLocators)
    {
        _colliders = colliders;
        _portLocators = portLocators;
    }

    private void EnsureExtracted()
    {
        if (_colliders is not null) return;
        if (Scene is null)
        {
            _colliders = Array.Empty<ExtractedCollider>();
            _portLocators = new Dictionary<string, ExtractedPortLocator>();
            return;
        }

        var instance = Scene.Instantiate();
        if (instance is Node3D node3D)
        {
            var (colliders, ports) = GltfPhysicsBodyExtractor.Extract(node3D);
            _colliders = colliders;
            _portLocators = ports;
            node3D.Free();
        }
        else
        {
            instance?.Free();
            _colliders = Array.Empty<ExtractedCollider>();
            _portLocators = new Dictionary<string, ExtractedPortLocator>();
        }
    }
}
