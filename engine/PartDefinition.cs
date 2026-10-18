using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

[GlobalClass]
public partial class PartDefinition : Resource
{
    private string _id = "";
    [Export] public string Id
    {
        get => _id;
        set
        {
            _id = value;
            PartId = string.IsNullOrWhiteSpace(value) ? default : new PartId(value);
        }
    }
    public PartId PartId { get; private set; }
    /// <summary>Declared material of a dynamic sphere; required for every ball kind.</summary>
    [Export] public BallMaterialResource? Ball { get; set; }
    [Export] public string Title { get; set; } = "";
    [Export] public string Category { get; set; } = "Structure";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

    private PackedScene? _scene;
    [Export]
    public PackedScene Scene
    {
        get => _scene!;
        set
        {
            _scene = value;
            _colliders = null;
            _portLocators = null;
            if (value is not null) EnsureExtracted();
        }
    }

    private IReadOnlyList<ExtractedCollider>? _colliders;
    private IReadOnlyDictionary<string, ExtractedPortLocator>? _portLocators;

    public IReadOnlyList<ExtractedCollider> Colliders
    {
        get
        {
            EnsureExtracted();
            return _colliders ?? Array.Empty<ExtractedCollider>();
        }
    }

    public IReadOnlyDictionary<string, ExtractedPortLocator> PortLocators
    {
        get
        {
            EnsureExtracted();
            return _portLocators ?? new Dictionary<string, ExtractedPortLocator>();
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
        if (_scene is null)
        {
            _colliders = Array.Empty<ExtractedCollider>();
            _portLocators = new Dictionary<string, ExtractedPortLocator>();
            return;
        }

        var instance = _scene.Instantiate();
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

    [Export] public Color Color { get; set; } = new("#f4bd69");
    [Export] public Godot.Collections.Dictionary<string, float> Parameters { get; set; } = new();
}
